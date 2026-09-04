using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

// C#-only infrastructure: no Dart counterpart. A low-level splash primitive kept in core so hosts and
// the Material ink layer can share one render object; Flutter draws splashes with InkFeature painters
// on the Material widget instead.

namespace Plumix.Widgets;

public sealed class InkSplash : SingleChildRenderObjectWidget
{
    public InkSplash(
        Widget? child = null,
        Color? splashColor = null,
        Point splashOrigin = default,
        double splashProgress = 0,
        double? splashRadius = null,
        bool clipToBounds = true,
        Key? key = null) : base(child, key)
    {
        SplashColor = splashColor;
        SplashOrigin = splashOrigin;
        SplashProgress = splashProgress;
        SplashRadius = splashRadius;
        ClipToBounds = clipToBounds;
    }

    public Color? SplashColor { get; }

    public Point SplashOrigin { get; }

    public double SplashProgress { get; }

    public double? SplashRadius { get; }

    public bool ClipToBounds { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderInkSplash(
            splashColor: SplashColor,
            splashOrigin: SplashOrigin,
            splashProgress: SplashProgress,
            splashRadius: SplashRadius,
            clipToBounds: ClipToBounds);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var inkSplash = (RenderInkSplash)renderObject;
        inkSplash.SplashColor = SplashColor;
        inkSplash.SplashOrigin = SplashOrigin;
        inkSplash.SplashProgress = SplashProgress;
        inkSplash.SplashRadius = SplashRadius;
        inkSplash.ClipToBounds = ClipToBounds;
    }
}
