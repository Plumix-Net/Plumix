using Avalonia;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/image_filter_config.dart

public readonly record struct ImageFilterContext(Rect Bounds);

public record ImageFilterConfig
{
    private readonly ImageFilter? _filter;

    protected ImageFilterConfig()
    {
    }

    public ImageFilterConfig(ImageFilter filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public virtual ImageFilter? Filter => _filter;

    public virtual ImageFilter Resolve(ImageFilterContext context)
    {
        return _filter
            ?? throw new InvalidOperationException("A specialized image-filter config must override Resolve.");
    }

    public sealed record Blur : ImageFilterConfig
    {
        public Blur(
            double sigmaX = 0.0,
            double sigmaY = 0.0,
            TileMode tileMode = TileMode.Clamp,
            bool bounded = false)
        {
            if (!double.IsFinite(sigmaX) || sigmaX < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(sigmaX));
            }

            if (!double.IsFinite(sigmaY) || sigmaY < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(sigmaY));
            }

            SigmaX = sigmaX;
            SigmaY = sigmaY;
            TileMode = tileMode;
            Bounded = bounded;
        }

        public double SigmaX { get; }

        public double SigmaY { get; }

        public TileMode TileMode { get; }

        public bool Bounded { get; }

        public override ImageFilter Resolve(ImageFilterContext context)
        {
            return new ImageFilter.Blur(
                SigmaX,
                SigmaY,
                TileMode,
                Bounded ? context.Bounds : null);
        }
    }

    public sealed record Compose : ImageFilterConfig
    {
        public Compose(ImageFilterConfig outer, ImageFilterConfig inner)
        {
            Outer = outer ?? throw new ArgumentNullException(nameof(outer));
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public ImageFilterConfig Outer { get; }

        public ImageFilterConfig Inner { get; }

        public override ImageFilter Resolve(ImageFilterContext context)
        {
            return new ImageFilter.Compose(
                Outer.Resolve(context),
                Inner.Resolve(context));
        }
    }

}
