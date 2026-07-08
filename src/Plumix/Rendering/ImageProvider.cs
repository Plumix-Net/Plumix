using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity sources:
// flutter/packages/flutter/lib/src/painting/image_provider.dart
// flutter/packages/flutter/lib/src/painting/image_resolution.dart

public enum ImageTargetPlatform
{
    Android,
    Fuchsia,
    IOS,
    Linux,
    MacOS,
    Windows,
}

public sealed record ImageConfiguration(
    AssetBundle? Bundle = null,
    double? DevicePixelRatio = null,
    CultureInfo? Locale = null,
    TextDirection? TextDirection = null,
    Size? Size = null,
    ImageTargetPlatform? Platform = null)
{
    public static ImageConfiguration Empty { get; } = new();

    public ImageConfiguration CopyWith(
        AssetBundle? bundle = null,
        double? devicePixelRatio = null,
        CultureInfo? locale = null,
        TextDirection? textDirection = null,
        Size? size = null,
        ImageTargetPlatform? platform = null)
    {
        return new ImageConfiguration(
            Bundle: bundle ?? Bundle,
            DevicePixelRatio: devicePixelRatio ?? DevicePixelRatio,
            Locale: locale ?? Locale,
            TextDirection: textDirection ?? TextDirection,
            Size: size ?? Size,
            Platform: platform ?? Platform);
    }
}

public abstract class AssetBundle
{
    public abstract Task<Stream> LoadAsync(string key, CancellationToken cancellationToken = default);

    public virtual Task<IReadOnlyList<string>> ListAssetsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>([]);
    }
}

public sealed class PlatformAssetBundle : AssetBundle
{
    public PlatformAssetBundle(Uri? baseUri = null)
    {
        BaseUri = baseUri;
    }

    public static PlatformAssetBundle Root { get; } = new();
    public Uri? BaseUri { get; }

    public override Task<Stream> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var uri = ResolveUri(key);
        return Task.FromResult(AssetLoader.Open(uri, BaseUri));
    }

    public override Task<IReadOnlyList<string>> ListAssetsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var uri = ResolveUri(key);
        string path = uri.AbsolutePath;
        int slash = path.LastIndexOf('/');
        string directoryPath = slash >= 0 ? path[..(slash + 1)] : "/";
        var directory = new UriBuilder(uri) { Path = directoryPath }.Uri;
        string[] assets = AssetLoader.GetAssets(directory, BaseUri).Select(asset => asset.ToString()).ToArray();
        return Task.FromResult<IReadOnlyList<string>>(assets);
    }

    private Uri ResolveUri(string key)
    {
        var uri = new Uri(key, UriKind.RelativeOrAbsolute);
        if (uri.IsAbsoluteUri || BaseUri is not null)
        {
            return uri;
        }

        string assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
                              ?? throw new InvalidOperationException(
                                  "A relative asset key requires an entry assembly or an explicit PlatformAssetBundle baseUri.");
        return new Uri($"avares://{assemblyName}/{key.TrimStart('/')}");
    }
}

public abstract class ImageProvider
{
    public abstract ImageStream Resolve(ImageConfiguration configuration);
    public abstract ValueTask<object> ObtainKeyObject(ImageConfiguration configuration);
    public abstract Task<bool> Evict(
        ImageCache? cache = null,
        ImageConfiguration? configuration = null);
    public abstract Task<ImageCacheStatus?> ObtainCacheStatus(
        ImageConfiguration configuration,
        ImageErrorListener? handleError = null);
}

public abstract class ImageProvider<T> : ImageProvider where T : notnull
{
    public sealed override ImageStream Resolve(ImageConfiguration configuration)
    {
        configuration ??= ImageConfiguration.Empty;
        var stream = CreateStream(configuration);
        ValueTask<T> keyTask;
        try
        {
            keyTask = ObtainKey(configuration);
        }
        catch (Exception exception)
        {
            ResolveError(stream, exception);
            return stream;
        }

        if (keyTask.IsCompletedSuccessfully)
        {
            ResolveStreamSafely(configuration, stream, keyTask.Result);
        }
        else
        {
            _ = ResolveKeyAsync(configuration, stream, keyTask);
        }

        return stream;
    }

    public sealed override async ValueTask<object> ObtainKeyObject(ImageConfiguration configuration)
    {
        return await ObtainKey(configuration).ConfigureAwait(false);
    }

    public sealed override async Task<bool> Evict(
        ImageCache? cache = null,
        ImageConfiguration? configuration = null)
    {
        var key = await ObtainKey(configuration ?? ImageConfiguration.Empty).ConfigureAwait(false);
        return (cache ?? ImageCache.Shared).Evict(key);
    }

    public sealed override async Task<ImageCacheStatus?> ObtainCacheStatus(
        ImageConfiguration configuration,
        ImageErrorListener? handleError = null)
    {
        try
        {
            var key = await ObtainKey(configuration).ConfigureAwait(false);
            return ImageCache.Shared.StatusForKey(key);
        }
        catch (Exception exception)
        {
            handleError?.Invoke(exception, new StackTrace(exception, true));
            return null;
        }
    }

    protected virtual ImageStream CreateStream(ImageConfiguration configuration) => new();

    protected virtual void ResolveStreamForKey(
        ImageConfiguration configuration,
        ImageStream stream,
        T key,
        ImageErrorListener handleError)
    {
        if (stream.Completer is not null)
        {
            ImageCache.Shared.PutIfAbsent(key, () => stream.Completer, handleError);
            return;
        }

        var completer = ImageCache.Shared.PutIfAbsent(
            key,
            () => LoadImage(key),
            handleError);
        if (completer is not null)
        {
            stream.SetCompleter(completer);
        }
    }

    public abstract ValueTask<T> ObtainKey(ImageConfiguration configuration);
    protected abstract ImageStreamCompleter LoadImage(T key);

    private async Task ResolveKeyAsync(
        ImageConfiguration configuration,
        ImageStream stream,
        ValueTask<T> keyTask)
    {
        try
        {
            ResolveStreamSafely(configuration, stream, await keyTask.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            ResolveError(stream, exception);
        }
    }

    private void ResolveStreamSafely(ImageConfiguration configuration, ImageStream stream, T key)
    {
        try
        {
            ResolveStreamForKey(
                configuration,
                stream,
                key,
                (exception, stack) => EnsureErrorCompleter(stream).ReportError(exception, stack));
        }
        catch (Exception exception)
        {
            ResolveError(stream, exception);
        }
    }

    private static void ResolveError(ImageStream stream, Exception exception)
    {
        _ = ReportErrorAfterListenerTurnAsync(stream, exception);
    }

    private static async Task ReportErrorAfterListenerTurnAsync(ImageStream stream, Exception exception)
    {
        await Task.Yield();
        EnsureErrorCompleter(stream).ReportError(exception, new StackTrace(exception, true));
    }

    private static ImageStreamCompleter EnsureErrorCompleter(ImageStream stream)
    {
        if (stream.Completer is not null)
        {
            return stream.Completer;
        }

        var completer = new ErrorImageStreamCompleter();
        stream.SetCompleter(completer);
        return completer;
    }

    private sealed class ErrorImageStreamCompleter : ImageStreamCompleter
    {
    }
}

public sealed class MemoryImage : ImageProvider<MemoryImage>, IEquatable<MemoryImage>
{
    public MemoryImage(byte[] bytes, double scale = 1.0)
    {
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        Scale = ValidateScale(scale);
    }

    public byte[] Bytes { get; }
    public double Scale { get; }

    public override ValueTask<MemoryImage> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(this);

    protected override ImageStreamCompleter LoadImage(MemoryImage key)
    {
        return new OneFrameImageStreamCompleter(
            DecodeBytesAsync(key.Bytes, key.Scale, $"MemoryImage({key.Bytes.Length} bytes)"),
            debugLabel: $"MemoryImage({key.Bytes.Length} bytes)");
    }

    public bool Equals(MemoryImage? other)
    {
        return other is not null && ReferenceEquals(Bytes, other.Bytes) && Scale.Equals(other.Scale);
    }

    public override bool Equals(object? obj) => Equals(obj as MemoryImage);
    public override int GetHashCode() => HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Bytes), Scale);
    public override string ToString() => $"MemoryImage({Bytes.Length} bytes, scale: {Scale:0.0})";

    internal static async Task<ImageInfo> DecodeBytesAsync(byte[] bytes, double scale, string? debugLabel)
    {
        await Task.Yield();
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("Image bytes are empty and cannot be decoded.");
        }

        using var stream = new MemoryStream(bytes, writable: false);
        return new ImageInfo(new Bitmap(stream), scale, debugLabel);
    }

    internal static double ValidateScale(double scale)
    {
        if (!double.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Image scale must be finite and positive.");
        }

        return scale;
    }
}

public sealed class FileImage : ImageProvider<FileImage>, IEquatable<FileImage>
{
    public FileImage(string filePath, double scale = 1.0)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
        FilePath = Path.GetFullPath(filePath);
        Scale = MemoryImage.ValidateScale(scale);
    }

    public string FilePath { get; }
    public double Scale { get; }

    public override ValueTask<FileImage> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(this);

    protected override ImageStreamCompleter LoadImage(FileImage key)
    {
        return new OneFrameImageStreamCompleter(LoadAsync(key), debugLabel: key.FilePath);
    }

    private static async Task<ImageInfo> LoadAsync(FileImage key)
    {
        byte[] bytes = await File.ReadAllBytesAsync(key.FilePath).ConfigureAwait(false);
        if (bytes.Length == 0)
        {
            ImageCache.Shared.Evict(key);
            throw new InvalidOperationException($"{key.FilePath} is empty and cannot be loaded as an image.");
        }

        return await MemoryImage.DecodeBytesAsync(bytes, key.Scale, key.FilePath).ConfigureAwait(false);
    }

    public bool Equals(FileImage? other)
    {
        return other is not null
               && StringComparer.Ordinal.Equals(FilePath, other.FilePath)
               && Scale.Equals(other.Scale);
    }

    public override bool Equals(object? obj) => Equals(obj as FileImage);
    public override int GetHashCode() => HashCode.Combine(StringComparer.Ordinal.GetHashCode(FilePath), Scale);
    public override string ToString() => $"FileImage(\"{FilePath}\", scale: {Scale:0.0})";
}

public sealed class NetworkImage : ImageProvider<NetworkImage>, IEquatable<NetworkImage>
{
    private static readonly HttpClient SharedClient = new();

    public NetworkImage(string url, double scale = 1.0, IReadOnlyDictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL is required.", nameof(url));
        Url = url;
        Scale = MemoryImage.ValidateScale(scale);
        Headers = headers is null
            ? null
            : new ReadOnlyDictionary<string, string>(headers.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal));
    }

    public string Url { get; }
    public double Scale { get; }
    public IReadOnlyDictionary<string, string>? Headers { get; }

    public override ValueTask<NetworkImage> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(this);

    protected override ImageStreamCompleter LoadImage(NetworkImage key)
    {
        OneFrameImageStreamCompleter? completer = null;
        var task = LoadAsync(key, @event => completer?.ReportImageChunkEvent(@event));
        completer = new OneFrameImageStreamCompleter(task, debugLabel: key.Url);
        return completer;
    }

    private static async Task<ImageInfo> LoadAsync(NetworkImage key, Action<ImageChunkEvent> onChunk)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, key.Url);
            if (key.Headers is not null)
            {
                foreach (var header in key.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var response = await SharedClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetworkImageLoadException((int)response.StatusCode, request.RequestUri!);
            }

            long? expected = response.Content.Headers.ContentLength;
            await using var network = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var bytes = new MemoryStream();
            byte[] buffer = new byte[16 * 1024];
            long loaded = 0;
            while (true)
            {
                int count = await network.ReadAsync(buffer).ConfigureAwait(false);
                if (count == 0) break;
                await bytes.WriteAsync(buffer.AsMemory(0, count)).ConfigureAwait(false);
                loaded += count;
                onChunk(new ImageChunkEvent(loaded, expected));
            }

            return await MemoryImage.DecodeBytesAsync(bytes.ToArray(), key.Scale, key.Url).ConfigureAwait(false);
        }
        catch
        {
            ImageCache.Shared.Evict(key);
            throw;
        }
    }

    public bool Equals(NetworkImage? other)
    {
        if (other is null || !StringComparer.Ordinal.Equals(Url, other.Url) || !Scale.Equals(other.Scale)) return false;
        if (ReferenceEquals(Headers, other.Headers)) return true;
        if (Headers is null || other.Headers is null || Headers.Count != other.Headers.Count) return false;
        return Headers.All(pair => other.Headers.TryGetValue(pair.Key, out string? value) && value == pair.Value);
    }

    public override bool Equals(object? obj) => Equals(obj as NetworkImage);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Url, StringComparer.Ordinal);
        hash.Add(Scale);
        if (Headers is not null)
        {
            foreach (var pair in Headers.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                hash.Add(pair.Key, StringComparer.Ordinal);
                hash.Add(pair.Value, StringComparer.Ordinal);
            }
        }
        return hash.ToHashCode();
    }

    public override string ToString() => $"NetworkImage(\"{Url}\", scale: {Scale:0.0})";
}

public sealed class NetworkImageLoadException : Exception
{
    public NetworkImageLoadException(int statusCode, Uri uri)
        : base($"HTTP request failed, statusCode: {statusCode}, {uri}")
    {
        StatusCode = statusCode;
        Uri = uri;
    }

    public int StatusCode { get; }
    public Uri Uri { get; }
}

public sealed record AssetBundleImageKey(AssetBundle Bundle, string Name, double Scale);

public abstract class AssetBundleImageProvider : ImageProvider<AssetBundleImageKey>
{
    protected override ImageStreamCompleter LoadImage(AssetBundleImageKey key)
    {
        return new OneFrameImageStreamCompleter(LoadAsync(key), debugLabel: key.Name);
    }

    private static async Task<ImageInfo> LoadAsync(AssetBundleImageKey key)
    {
        await using var stream = await key.Bundle.LoadAsync(key.Name).ConfigureAwait(false);
        using var bytes = new MemoryStream();
        await stream.CopyToAsync(bytes).ConfigureAwait(false);
        return await MemoryImage.DecodeBytesAsync(bytes.ToArray(), key.Scale, key.Name).ConfigureAwait(false);
    }
}

public sealed class ExactAssetImage : AssetBundleImageProvider, IEquatable<ExactAssetImage>
{
    public ExactAssetImage(
        string assetName,
        double scale = 1.0,
        AssetBundle? bundle = null,
        string? package = null)
    {
        if (string.IsNullOrWhiteSpace(assetName)) throw new ArgumentException("Asset name is required.", nameof(assetName));
        AssetName = assetName;
        Scale = MemoryImage.ValidateScale(scale);
        Bundle = bundle;
        Package = package;
    }

    public string AssetName { get; }
    public double Scale { get; }
    public AssetBundle? Bundle { get; }
    public string? Package { get; }
    public string KeyName => Package is null ? AssetName : $"avares://{Package}/{AssetName.TrimStart('/')}";

    public override ValueTask<AssetBundleImageKey> ObtainKey(ImageConfiguration configuration)
    {
        return ValueTask.FromResult(new AssetBundleImageKey(
            Bundle ?? configuration.Bundle ?? PlatformAssetBundle.Root,
            KeyName,
            Scale));
    }

    public bool Equals(ExactAssetImage? other)
    {
        return other is not null
               && KeyName == other.KeyName
               && Scale.Equals(other.Scale)
               && Equals(Bundle, other.Bundle);
    }

    public override bool Equals(object? obj) => Equals(obj as ExactAssetImage);
    public override int GetHashCode() => HashCode.Combine(KeyName, Scale, Bundle);
}

public sealed class AssetImage : AssetBundleImageProvider, IEquatable<AssetImage>
{
    private const double NaturalResolution = 1.0;
    private const double LowDprLimit = 2.0;

    public AssetImage(string assetName, AssetBundle? bundle = null, string? package = null)
    {
        if (string.IsNullOrWhiteSpace(assetName)) throw new ArgumentException("Asset name is required.", nameof(assetName));
        AssetName = assetName;
        Bundle = bundle;
        Package = package;
    }

    public string AssetName { get; }
    public AssetBundle? Bundle { get; }
    public string? Package { get; }
    public string KeyName => Package is null ? AssetName : $"avares://{Package}/{AssetName.TrimStart('/')}";

    public override async ValueTask<AssetBundleImageKey> ObtainKey(ImageConfiguration configuration)
    {
        var bundle = Bundle ?? configuration.Bundle ?? PlatformAssetBundle.Root;
        double? dpr = configuration.DevicePixelRatio;
        if (!dpr.HasValue)
        {
            return new AssetBundleImageKey(bundle, KeyName, NaturalResolution);
        }

        var variants = await bundle.ListAssetsAsync(KeyName).ConfigureAwait(false);
        var candidates = new SortedDictionary<double, string>();
        candidates[NaturalResolution] = KeyName;
        foreach (string variant in variants)
        {
            if (TryParseVariantScale(KeyName, variant, out double scale))
            {
                candidates[scale] = variant;
            }
        }

        double selected = FindBestVariant(candidates.Keys.ToArray(), dpr.Value);
        return new AssetBundleImageKey(bundle, candidates[selected], selected);
    }

    internal static double FindBestVariant(IReadOnlyList<double> sortedCandidates, double value)
    {
        if (sortedCandidates.Count == 0) return NaturalResolution;
        if (sortedCandidates.Contains(value)) return value;
        double lower = sortedCandidates.Where(candidate => candidate < value).LastOrDefault(double.NaN);
        double upper = sortedCandidates.FirstOrDefault(candidate => candidate > value, double.NaN);
        if (double.IsNaN(lower)) return upper;
        if (double.IsNaN(upper)) return lower;
        return value < LowDprLimit || value > (lower + upper) / 2.0 ? upper : lower;
    }

    private static bool TryParseVariantScale(string keyName, string candidate, out double scale)
    {
        scale = 0;
        string fileName = Path.GetFileName(keyName);
        if (!candidate.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) return false;
        string[] segments = candidate.Replace('\\', '/').Split('/');
        if (segments.Length < 2) return false;
        string density = segments[^2];
        return density.EndsWith('x')
               && double.TryParse(density[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out scale)
               && scale > 0;
    }

    public bool Equals(AssetImage? other)
    {
        return other is not null && KeyName == other.KeyName && Equals(Bundle, other.Bundle);
    }

    public override bool Equals(object? obj) => Equals(obj as AssetImage);
    public override int GetHashCode() => HashCode.Combine(KeyName, Bundle);
}

public sealed class ResizeImageKey : IEquatable<ResizeImageKey>
{
    public ResizeImageKey(
        object providerCacheKey,
        int? width,
        int? height,
        bool allowUpscaling,
        ImageProvider provider,
        ImageConfiguration configuration)
    {
        ProviderCacheKey = providerCacheKey;
        Width = width;
        Height = height;
        AllowUpscaling = allowUpscaling;
        Provider = provider;
        Configuration = configuration;
    }

    public object ProviderCacheKey { get; }
    public int? Width { get; }
    public int? Height { get; }
    public bool AllowUpscaling { get; }
    internal ImageProvider Provider { get; }
    internal ImageConfiguration Configuration { get; }

    public bool Equals(ResizeImageKey? other)
    {
        return other is not null
               && Equals(ProviderCacheKey, other.ProviderCacheKey)
               && Width == other.Width
               && Height == other.Height
               && AllowUpscaling == other.AllowUpscaling;
    }

    public override bool Equals(object? obj) => Equals(obj as ResizeImageKey);
    public override int GetHashCode() => HashCode.Combine(ProviderCacheKey, Width, Height, AllowUpscaling);
}

public sealed class ResizeImage : ImageProvider<ResizeImageKey>, IEquatable<ResizeImage>
{
    public ResizeImage(
        ImageProvider imageProvider,
        int? width = null,
        int? height = null,
        bool allowUpscaling = false)
    {
        if (width is <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height is <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (!width.HasValue && !height.HasValue)
        {
            throw new ArgumentException("ResizeImage requires width, height, or both.");
        }

        ImageProvider = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));
        Width = width;
        Height = height;
        AllowUpscaling = allowUpscaling;
    }

    public ImageProvider ImageProvider { get; }
    public int? Width { get; }
    public int? Height { get; }
    public bool AllowUpscaling { get; }

    public static ImageProvider ResizeIfNeeded(int? cacheWidth, int? cacheHeight, ImageProvider provider)
    {
        return cacheWidth.HasValue || cacheHeight.HasValue
            ? new ResizeImage(provider, cacheWidth, cacheHeight)
            : provider;
    }

    public override async ValueTask<ResizeImageKey> ObtainKey(ImageConfiguration configuration)
    {
        return new ResizeImageKey(
            providerCacheKey: await ImageProvider.ObtainKeyObject(configuration).ConfigureAwait(false),
            width: Width,
            height: Height,
            allowUpscaling: AllowUpscaling,
            provider: ImageProvider,
            configuration: configuration);
    }

    protected override ImageStreamCompleter LoadImage(ResizeImageKey key)
    {
        var result = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream = key.Provider.Resolve(key.Configuration);
        ImageStreamListener? listener = null;
        listener = new ImageStreamListener(
            OnImage: (image, _) =>
            {
                try
                {
                    result.TrySetResult(Resize(image, key));
                }
                catch (Exception exception)
                {
                    result.TrySetException(exception);
                }
                finally
                {
                    image.Dispose();
                    stream.RemoveListener(listener!);
                }
            },
            OnError: (exception, _) =>
            {
                result.TrySetException(exception);
                stream.RemoveListener(listener!);
            });
        stream.AddListener(listener);
        return new OneFrameImageStreamCompleter(result.Task, debugLabel: key.ProviderCacheKey.ToString());
    }

    private static ImageInfo Resize(ImageInfo image, ResizeImageKey key)
    {
        if (image.Image is not Bitmap bitmap)
        {
            return image.Clone();
        }

        var source = bitmap.PixelSize;
        int targetWidth = key.Width ?? Math.Max(1, (int)Math.Round(source.Width * (key.Height!.Value / (double)source.Height)));
        int targetHeight = key.Height ?? Math.Max(1, (int)Math.Round(source.Height * (key.Width!.Value / (double)source.Width)));
        if (!key.AllowUpscaling)
        {
            double factor = Math.Min(1.0, Math.Min(targetWidth / (double)source.Width, targetHeight / (double)source.Height));
            targetWidth = Math.Max(1, (int)Math.Round(source.Width * factor));
            targetHeight = Math.Max(1, (int)Math.Round(source.Height * factor));
        }

        if (targetWidth == source.Width && targetHeight == source.Height)
        {
            return image.Clone();
        }

        var resized = bitmap.CreateScaledBitmap(
            new PixelSize(targetWidth, targetHeight),
            Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
        return new ImageInfo(resized, image.Scale, image.DebugLabel);
    }

    public bool Equals(ResizeImage? other)
    {
        return other is not null
               && Equals(ImageProvider, other.ImageProvider)
               && Width == other.Width
               && Height == other.Height
               && AllowUpscaling == other.AllowUpscaling;
    }

    public override bool Equals(object? obj) => Equals(obj as ResizeImage);
    public override int GetHashCode() => HashCode.Combine(ImageProvider, Width, Height, AllowUpscaling);
}
