using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (Baseline, IgnoreBaseline)

public sealed class Baseline : SingleChildRenderObjectWidget
{
    public Baseline(
        double baseline,
        TextBaseline baselineType,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        BaselineOffset = baseline;
        BaselineType = baselineType;
    }

    public double BaselineOffset { get; }

    public TextBaseline BaselineType { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderBaseline(baseline: BaselineOffset, baselineType: BaselineType);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var baseline = (RenderBaseline)renderObject;
        baseline.Baseline = BaselineOffset;
        baseline.BaselineType = BaselineType;
    }
}

public sealed class IgnoreBaseline : SingleChildRenderObjectWidget
{
    public IgnoreBaseline(Widget? child = null, Key? key = null) : base(child, key)
    {
    }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderIgnoreBaseline();
}
