using System.Collections;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/painting/binding.dart

namespace Plumix.Painting;

/// <summary>
/// Owns image caching, engine codec entry points, shader warm-up and system-font notifications.
/// </summary>
public class PaintingBinding
{
    private static PaintingBinding? _instance;

    public PaintingBinding()
    {
        _instance = this;
        ImageCache = CreateImageCache();
        if (ShaderWarmUp is { } warmUp)
        {
            EventLoop.RunAsync(warmUp.Execute);
        }
    }

    public static PaintingBinding Instance
    {
        get => _instance ?? new PaintingBinding();
        set => _instance = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static ShaderWarmUp? ShaderWarmUp { get; set; }

    public virtual ImageCache ImageCache { get; }

    protected virtual ImageCache CreateImageCache() => new();

    /// <summary>Decodes an image, capping requested dimensions to the intrinsic size by default.</summary>
    [Obsolete("Use InstantiateImageCodecWithSize instead.")]
    public virtual Task<Codec> InstantiateImageCodecFromBuffer(
        ImmutableBuffer buffer,
        int? cacheWidth = null,
        int? cacheHeight = null,
        bool allowUpscaling = false)
    {
        if (Constants.KDebugMode && (cacheWidth <= 0 || cacheHeight <= 0))
        {
            throw new AssertionError("Cache image dimensions must be positive or null.");
        }

        return ImageDecoding.InstantiateImageCodecFromBuffer(buffer, cacheWidth, cacheHeight, allowUpscaling);
    }

    public virtual Task<Codec> InstantiateImageCodecWithSize(
        ImmutableBuffer buffer,
        TargetImageSizeCallback? getTargetSize = null) =>
        ImageDecoding.InstantiateImageCodecWithSize(buffer, getTargetSize);

    public virtual void Evict(string asset)
    {
        ServicesBinding.Instance.Evict(asset);
        ImageCache.Clear();
        ImageCache.ClearLiveImages();
    }

    public virtual void HandleMemoryPressure()
    {
        ServicesBinding.Instance.HandleMemoryPressure();
        ImageCache.Clear();
    }

    private readonly SystemFontsNotifier _systemFonts = new();

    public IListenable SystemFonts => _systemFonts;

    public virtual async Task HandleSystemMessage(object systemMessage)
    {
        await ServicesBinding.Instance.HandleSystemMessage(systemMessage);
        var message = (IDictionary)systemMessage;
        if ((string)message["type"]! == "fontsChange")
        {
            _systemFonts.NotifyListeners();
        }
    }

    private sealed class SystemFontsNotifier : IListenable
    {
        private readonly HashSet<Action> _callbacks = [];

        public void AddListener(Action listener) => _callbacks.Add(listener);

        public void RemoveListener(Action listener) => _callbacks.Remove(listener);

        public void NotifyListeners()
        {
            foreach (Action callback in _callbacks)
            {
                callback();
            }
        }
    }
}
