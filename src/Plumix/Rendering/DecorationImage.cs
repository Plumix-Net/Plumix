using Avalonia;
using Avalonia.Media;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/decoration_image.dart

public enum ImageRepeat
{
    Repeat,
    RepeatX,
    RepeatY,
    NoRepeat,
}

public enum FilterQuality
{
    None,
    Low,
    Medium,
    High,
}

public abstract record ColorFilter
{
    public sealed record Matrix : ColorFilter
    {
        public Matrix(IReadOnlyList<double> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            if (values.Count != 20)
            {
                throw new ArgumentException("A color-filter matrix must contain 20 values.", nameof(values));
            }

            Values = values.ToArray();
        }

        public Matrix(params double[] values) : this((IReadOnlyList<double>)values) { }

        public IReadOnlyList<double> Values { get; }
    }

    public sealed record Mode(Color Color, Avalonia.Media.Imaging.BitmapBlendingMode BlendMode) : ColorFilter;
}

public sealed class DecorationImage : IEquatable<DecorationImage>
{
    public DecorationImage(
        ImageProvider image,
        ImageErrorListener? onError = null,
        ColorFilter? colorFilter = null,
        BoxFit? fit = null,
        Alignment alignment = default,
        Rect? centerSlice = null,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        double scale = 1.0,
        double opacity = 1.0,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool invertColors = false,
        bool isAntiAlias = false)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
        if (!double.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (!double.IsFinite(opacity) || opacity < 0 || opacity > 1) throw new ArgumentOutOfRangeException(nameof(opacity));
        if (centerSlice.HasValue && (fit is BoxFit.None or BoxFit.Cover))
        {
            throw new ArgumentException("centerSlice cannot be combined with BoxFit.None or BoxFit.Cover.", nameof(fit));
        }

        OnError = onError;
        ColorFilter = colorFilter;
        Fit = fit;
        Alignment = alignment;
        CenterSlice = centerSlice;
        Repeat = repeat;
        MatchTextDirection = matchTextDirection;
        Scale = scale;
        Opacity = opacity;
        FilterQuality = filterQuality;
        InvertColors = invertColors;
        IsAntiAlias = isAntiAlias;
    }

    public ImageProvider Image { get; }
    public ImageErrorListener? OnError { get; }
    public ColorFilter? ColorFilter { get; }
    public BoxFit? Fit { get; }
    public Alignment Alignment { get; }
    public Rect? CenterSlice { get; }
    public ImageRepeat Repeat { get; }
    public bool MatchTextDirection { get; }
    public double Scale { get; }
    public double Opacity { get; }
    public FilterQuality FilterQuality { get; }
    public bool InvertColors { get; }
    public bool IsAntiAlias { get; }

    public DecorationImagePainter CreatePainter(Action onChanged)
    {
        return new DecorationImagePainter(this, onChanged ?? throw new ArgumentNullException(nameof(onChanged)));
    }

    public bool Equals(DecorationImage? other)
    {
        return other is not null
               && Equals(Image, other.Image)
               && Equals(ColorFilter, other.ColorFilter)
               && Fit == other.Fit
               && Alignment == other.Alignment
               && CenterSlice == other.CenterSlice
               && Repeat == other.Repeat
               && MatchTextDirection == other.MatchTextDirection
               && Scale.Equals(other.Scale)
               && Opacity.Equals(other.Opacity)
               && FilterQuality == other.FilterQuality
               && InvertColors == other.InvertColors
               && IsAntiAlias == other.IsAntiAlias;
    }

    public override bool Equals(object? obj) => Equals(obj as DecorationImage);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Image);
        hash.Add(ColorFilter);
        hash.Add(Fit);
        hash.Add(Alignment);
        hash.Add(CenterSlice);
        hash.Add(Repeat);
        hash.Add(MatchTextDirection);
        hash.Add(Scale);
        hash.Add(Opacity);
        hash.Add(FilterQuality);
        hash.Add(InvertColors);
        hash.Add(IsAntiAlias);
        return hash.ToHashCode();
    }
}

public sealed class DecorationImagePainter : IDisposable
{
    private readonly DecorationImage _details;
    private readonly Action _onChanged;
    private readonly ImageStreamListener _listener;
    private ImageStream? _imageStream;
    private ImageInfo? _image;
    private bool _disposed;

    internal DecorationImagePainter(DecorationImage details, Action onChanged)
    {
        _details = details;
        _onChanged = onChanged;
        _listener = new ImageStreamListener(HandleImage, OnError: details.OnError);
    }

    public void Paint(
        PaintingContext context,
        Rect rect,
        ImageConfiguration configuration,
        BorderRadius? clipRadius = null,
        double blend = 1.0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_details.MatchTextDirection && configuration.TextDirection is null)
        {
            throw new InvalidOperationException(
                "DecorationImage.matchTextDirection requires a TextDirection in ImageConfiguration.");
        }

        var newStream = _details.Image.Resolve(configuration);
        if (!Equals(newStream.Key, _imageStream?.Key))
        {
            _imageStream?.RemoveListener(_listener);
            _imageStream = newStream;
            _imageStream.AddListener(_listener);
        }

        if (_image is null)
        {
            return;
        }

        ImagePainting.PaintImage(
            context: context,
            rect: rect,
            image: _image.Image,
            scale: _details.Scale * _image.Scale,
            opacity: _details.Opacity * blend,
            fit: _details.Fit,
            alignment: _details.Alignment,
            centerSlice: _details.CenterSlice,
            repeat: _details.Repeat,
            flipHorizontally: _details.MatchTextDirection && configuration.TextDirection == Plumix.UI.TextDirection.Rtl,
            clipRadius: clipRadius,
            colorFilter: _details.ColorFilter,
            filterQuality: _details.FilterQuality,
            invertColors: _details.InvertColors,
            isAntiAlias: _details.IsAntiAlias);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _imageStream?.RemoveListener(_listener);
        _imageStream = null;
        _image?.Dispose();
        _image = null;
    }

    private void HandleImage(ImageInfo image, bool synchronousCall)
    {
        if (_image is not null && _image.IsCloneOf(image))
        {
            image.Dispose();
            return;
        }

        _image?.Dispose();
        _image = image;
        if (!synchronousCall)
        {
            _onChanged();
        }
    }
}

public static class ImagePainting
{
    public static void PaintImage(
        PaintingContext context,
        Rect rect,
        IImage image,
        double scale = 1.0,
        double opacity = 1.0,
        ColorFilter? colorFilter = null,
        BoxFit? fit = null,
        Alignment alignment = default,
        Rect? centerSlice = null,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool flipHorizontally = false,
        bool invertColors = false,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool isAntiAlias = false,
        BorderRadius? clipRadius = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(image);
        if (rect.Width <= 0 || rect.Height <= 0) return;
        if (!double.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));

        var plan = CreatePaintPlan(
            rect,
            image.Size,
            scale,
            fit,
            alignment,
            centerSlice,
            repeat,
            flipHorizontally);
        foreach (var destination in plan.DestinationRects)
        {
            var effectiveClipRect = plan.ClipRect ?? (clipRadius.HasValue ? rect : null);
            if (plan.CenterSlicePixels.HasValue)
            {
                DrawNinePatch(
                    context,
                    image,
                    plan.SourceImageSize,
                    plan.CenterSlicePixels.Value,
                    destination,
                    scale,
                    opacity,
                    effectiveClipRect,
                    clipRadius,
                    flipHorizontally,
                    plan.FlipAxisX);
            }
            else
            {
                context.DrawImage(
                    image,
                    plan.SourceRect,
                    destination,
                    opacity,
                    effectiveClipRect,
                    clipRadius,
                    flipHorizontally,
                    plan.FlipAxisX);
            }
        }

        // Avalonia's public DrawingContext does not currently expose per-draw
        // sampling or color-matrix effects. The parity fields are retained so
        // the backend can adopt them without another public API change.
        _ = colorFilter;
        _ = invertColors;
        _ = filterQuality;
        _ = isAntiAlias;
    }

    internal static ImagePaintPlan CreatePaintPlan(
        Rect rect,
        Size rawImageSize,
        double scale,
        BoxFit? fit,
        Alignment alignment,
        Rect? centerSlice,
        ImageRepeat repeat,
        bool flipHorizontally)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || rawImageSize.Width <= 0 || rawImageSize.Height <= 0)
        {
            return ImagePaintPlan.Empty;
        }

        var outputSize = rect.Size;
        var inputSize = rawImageSize;
        Vector? sliceBorder = null;
        if (centerSlice.HasValue)
        {
            sliceBorder = new Vector(
                (inputSize.Width / scale) - centerSlice.Value.Width,
                (inputSize.Height / scale) - centerSlice.Value.Height);
            outputSize = new Size(
                Math.Max(0, outputSize.Width - sliceBorder.Value.X),
                Math.Max(0, outputSize.Height - sliceBorder.Value.Y));
            inputSize = new Size(
                Math.Max(0, inputSize.Width - sliceBorder.Value.X * scale),
                Math.Max(0, inputSize.Height - sliceBorder.Value.Y * scale));
        }

        var effectiveFit = fit ?? (centerSlice.HasValue ? BoxFit.Fill : BoxFit.ScaleDown);
        if (centerSlice.HasValue && effectiveFit is BoxFit.None or BoxFit.Cover)
        {
            throw new InvalidOperationException("centerSlice requires a fit that keeps the complete source image visible.");
        }

        var fitted = BoxFitUtils.ApplyBoxFit(
            effectiveFit,
            new Size(inputSize.Width / scale, inputSize.Height / scale),
            outputSize);
        var sourceSize = new Size(fitted.Source.Width * scale, fitted.Source.Height * scale);
        var destinationSize = fitted.Destination;
        if (sliceBorder.HasValue)
        {
            destinationSize = new Size(
                destinationSize.Width + sliceBorder.Value.X,
                destinationSize.Height + sliceBorder.Value.Y);
            if (!SizesClose(sourceSize, inputSize))
            {
                throw new InvalidOperationException(
                    "centerSlice requires a fit that keeps the complete source image visible.");
            }
        }

        if (repeat != ImageRepeat.NoRepeat && destinationSize == rect.Size)
        {
            repeat = ImageRepeat.NoRepeat;
        }

        var effectiveDestinationAlignment = flipHorizontally
            ? new Alignment(-alignment.X, alignment.Y)
            : alignment;
        var destinationOffset = effectiveDestinationAlignment.AlongOffset(rect.Size, destinationSize);
        var destinationRect = new Rect(rect.Position + destinationOffset, destinationSize);
        var sourceOffset = alignment.AlongOffset(rawImageSize, sourceSize);
        var sourceRect = new Rect(sourceOffset, sourceSize);
        var destinations = GenerateImageTileRects(rect, destinationRect, repeat);
        Rect? centerSlicePixels = centerSlice.HasValue
            ? ScaleRect(centerSlice.Value, scale)
            : null;
        return new ImagePaintPlan(
            SourceImageSize: rawImageSize,
            SourceRect: sourceRect,
            DestinationRects: destinations,
            ClipRect: repeat == ImageRepeat.NoRepeat ? null : rect,
            CenterSlicePixels: centerSlicePixels,
            FlipAxisX: flipHorizontally ? rect.Center.X : null);
    }

    internal static IReadOnlyList<Rect> GenerateImageTileRects(
        Rect outputRect,
        Rect fundamentalRect,
        ImageRepeat repeat)
    {
        if (fundamentalRect.Width <= 0 || fundamentalRect.Height <= 0)
        {
            return [];
        }

        var startX = 0;
        var stopX = 0;
        var startY = 0;
        var stopY = 0;
        if (repeat is ImageRepeat.Repeat or ImageRepeat.RepeatX)
        {
            startX = (int)Math.Floor((outputRect.Left - fundamentalRect.Left) / fundamentalRect.Width);
            stopX = (int)Math.Ceiling((outputRect.Right - fundamentalRect.Right) / fundamentalRect.Width);
        }
        if (repeat is ImageRepeat.Repeat or ImageRepeat.RepeatY)
        {
            startY = (int)Math.Floor((outputRect.Top - fundamentalRect.Top) / fundamentalRect.Height);
            stopY = (int)Math.Ceiling((outputRect.Bottom - fundamentalRect.Bottom) / fundamentalRect.Height);
        }

        var result = new List<Rect>();
        for (var x = startX; x <= stopX; x++)
        {
            for (var y = startY; y <= stopY; y++)
            {
                result.Add(fundamentalRect.Translate(x * fundamentalRect.Width, y * fundamentalRect.Height));
            }
        }
        return result;
    }

    private static void DrawNinePatch(
        PaintingContext context,
        IImage image,
        Size imageSize,
        Rect centerSlice,
        Rect destination,
        double scale,
        double opacity,
        Rect? clipRect,
        BorderRadius? clipRadius,
        bool flipHorizontally,
        double? flipAxisX)
    {
        foreach (var patch in GenerateNinePatchRects(imageSize, centerSlice, destination, scale))
        {
            context.DrawImage(
                image,
                patch.Source,
                patch.Destination,
                opacity,
                clipRect,
                clipRadius,
                flipHorizontally,
                flipAxisX);
        }
    }

    internal static IReadOnlyList<ImagePatch> GenerateNinePatchRects(
        Size imageSize,
        Rect centerSlice,
        Rect destination,
        double scale)
    {
        if (!double.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        var sourceX = new[] { 0.0, centerSlice.Left, centerSlice.Right, imageSize.Width };
        var sourceY = new[] { 0.0, centerSlice.Top, centerSlice.Bottom, imageSize.Height };
        var left = centerSlice.Left / scale;
        var right = (imageSize.Width - centerSlice.Right) / scale;
        var top = centerSlice.Top / scale;
        var bottom = (imageSize.Height - centerSlice.Bottom) / scale;
        NormalizeBorders(destination.Width, ref left, ref right);
        NormalizeBorders(destination.Height, ref top, ref bottom);
        var destinationX = new[] { destination.Left, destination.Left + left, destination.Right - right, destination.Right };
        var destinationY = new[] { destination.Top, destination.Top + top, destination.Bottom - bottom, destination.Bottom };
        var result = new List<ImagePatch>(9);

        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                result.Add(new ImagePatch(
                    Source: new Rect(
                        sourceX[x], sourceY[y],
                        sourceX[x + 1] - sourceX[x], sourceY[y + 1] - sourceY[y]),
                    Destination: new Rect(
                        destinationX[x], destinationY[y],
                        destinationX[x + 1] - destinationX[x], destinationY[y + 1] - destinationY[y])));
            }
        }

        return result;
    }

    private static void NormalizeBorders(double extent, ref double leading, ref double trailing)
    {
        var sum = leading + trailing;
        if (sum <= extent || sum <= 0) return;
        var factor = extent / sum;
        leading *= factor;
        trailing *= factor;
    }

    private static Rect ScaleRect(Rect rect, double scale)
    {
        return new Rect(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
    }

    private static bool SizesClose(Size first, Size second)
    {
        const double epsilon = 1e-9;
        return Math.Abs(first.Width - second.Width) <= epsilon
               && Math.Abs(first.Height - second.Height) <= epsilon;
    }
}

internal sealed record ImagePaintPlan(
    Size SourceImageSize,
    Rect SourceRect,
    IReadOnlyList<Rect> DestinationRects,
    Rect? ClipRect,
    Rect? CenterSlicePixels,
    double? FlipAxisX)
{
    public static ImagePaintPlan Empty { get; } = new(new Size(), new Rect(), [], null, null, null);
}

internal readonly record struct ImagePatch(Rect Source, Rect Destination);

internal static class RectImageExtensions
{
    public static Rect Translate(this Rect rect, double x, double y)
    {
        return new Rect(rect.X + x, rect.Y + y, rect.Width, rect.Height);
    }
}
