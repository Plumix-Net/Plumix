using Avalonia;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/no_splash.dart

internal sealed class NoSplashFactory : InteractiveInkFeatureFactory
{
    public override InteractiveInkFeature Create(
        MaterialInkController controller,
        RenderBox referenceBox,
        Point position,
        Color color,
        TextDirection textDirection,
        bool containedInkWell = false,
        RectCallback? rectCallback = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        double? radius = null,
        Action? onRemoved = null)
    {
        return new NoSplash(
            controller: controller,
            referenceBox: referenceBox,
            color: color,
            onRemoved: onRemoved);
    }
}

/// <summary>An <see cref="InteractiveInkFeature"/> that doesn't paint a splash.</summary>
public class NoSplash : InteractiveInkFeature
{
    /// <summary>Create an <see cref="InteractiveInkFeature"/> that doesn't paint a splash.</summary>
    public NoSplash(
        MaterialInkController controller,
        RenderBox referenceBox,
        Color color,
        Action? onRemoved = null)
        : base(controller, referenceBox, color, onRemoved: onRemoved)
    {
    }

    /// <summary>Used to specify this type of ink splash for an <see cref="InkWell"/>, <see cref="InkResponse"/>,
    /// material <see cref="Theme"/>, or <see cref="ButtonStyle"/>.</summary>
    public static InteractiveInkFeatureFactory SplashFactory { get; } = new NoSplashFactory();

    protected override void PaintFeature(Canvas canvas, Matrix4 transform)
    {
    }

    public override void Confirm()
    {
        base.Confirm();
        Dispose();
    }

    public override void Cancel()
    {
        base.Cancel();
        Dispose();
    }
}
