using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

public sealed class IntrinsicWidth : SingleChildRenderObjectWidget
{
    public IntrinsicWidth(
        Widget? child = null,
        double? stepWidth = null,
        double? stepHeight = null,
        Key? key = null) : base(child, key)
    {
        StepWidth = ValidateWidgetStep(stepWidth, nameof(stepWidth));
        StepHeight = ValidateWidgetStep(stepHeight, nameof(stepHeight));
    }

    public double? StepWidth { get; }
    public double? StepHeight { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderIntrinsicWidth(stepWidth: EffectiveStep(StepWidth), stepHeight: EffectiveStep(StepHeight));

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var intrinsicWidth = (RenderIntrinsicWidth)renderObject;
        intrinsicWidth.StepWidth = EffectiveStep(StepWidth);
        intrinsicWidth.StepHeight = EffectiveStep(StepHeight);
    }

    private static double? ValidateWidgetStep(double? value, string parameterName)
    {
        if (Constants.KDebugMode && value is not (null or >= 0.0))
        {
            throw new AssertionError($"{parameterName} must be non-negative.");
        }

        return value;
    }

    private static double? EffectiveStep(double? value) => value == 0.0 ? null : value;
}

public sealed class IntrinsicHeight : SingleChildRenderObjectWidget
{
    public IntrinsicHeight(Widget? child = null, Key? key = null) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderIntrinsicHeight();
}

