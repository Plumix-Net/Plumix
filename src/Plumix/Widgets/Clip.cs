using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

public sealed class ClipOval : SingleChildRenderObjectWidget
{
    public ClipOval(
        CustomClipper<Rect>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Clipper = clipper;
        ClipBehavior = clipBehavior;
    }

    public CustomClipper<Rect>? Clipper { get; }

    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderClipOval(
            clipper: Clipper,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var clipOval = (RenderClipOval)renderObject;
        clipOval.Clipper = Clipper;
        clipOval.ClipBehavior = ClipBehavior;
    }
}

public sealed class ClipPath : SingleChildRenderObjectWidget
{
    public ClipPath(
        CustomClipper<Path>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Clipper = clipper;
        ClipBehavior = clipBehavior;
    }

    public CustomClipper<Path>? Clipper { get; }

    public Clip ClipBehavior { get; }

    public static Widget Shape(
        ShapeBorder shape,
        Clip clipBehavior = Clip.AntiAlias,
        Widget? child = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(shape);
        return new Builder(
            key: key,
            builder: context => new ClipPath(
                clipper: new ShapeBorderClipper(
                    shape,
                    Directionality.MaybeOf(context)),
                clipBehavior: clipBehavior,
                child: child));
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderClipPath(
            clipper: Clipper,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var clipPath = (RenderClipPath)renderObject;
        clipPath.Clipper = Clipper;
        clipPath.ClipBehavior = ClipBehavior;
    }
}
