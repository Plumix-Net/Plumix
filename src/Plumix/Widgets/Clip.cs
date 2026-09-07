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

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderClipOval(
            clipper: Clipper,
            clipBehavior: ClipBehavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
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

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderClipPath(
            clipper: Clipper,
            clipBehavior: ClipBehavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var clipPath = (RenderClipPath)renderObject;
        clipPath.Clipper = Clipper;
        clipPath.ClipBehavior = ClipBehavior;
    }
}

/// <summary>Clips its child to an iOS-style rounded superellipse.</summary>
public sealed class ClipRSuperellipse : SingleChildRenderObjectWidget
{
    public ClipRSuperellipse(
        BorderRadiusGeometry? borderRadius = null,
        CustomClipper<RSuperellipse>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        BorderRadius = borderRadius ?? Plumix.Rendering.BorderRadius.Zero;
        Clipper = clipper;
        ClipBehavior = clipBehavior;
    }

    public BorderRadiusGeometry BorderRadius { get; }

    public CustomClipper<RSuperellipse>? Clipper { get; }

    public Clip ClipBehavior { get; }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderClipRSuperellipse(
        borderRadius: BorderRadius,
        clipper: Clipper,
        clipBehavior: ClipBehavior,
        textDirection: Directionality.MaybeOf(context));

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var clip = (RenderClipRSuperellipse)renderObject;
        clip.BorderRadius = BorderRadius;
        clip.Clipper = Clipper;
        clip.ClipBehavior = ClipBehavior;
        clip.TextDirection = Directionality.MaybeOf(context);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<BorderRadiusGeometry>(
            "borderRadius",
            BorderRadius,
            showName: false,
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}
