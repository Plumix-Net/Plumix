using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (ListBody)

public sealed class ListBody : MultiChildRenderObjectWidget
{
    public ListBody(
        IReadOnlyList<Widget>? children = null,
        Axis mainAxis = Axis.Vertical,
        bool reverse = false,
        Key? key = null) : base(children, key)
    {
        MainAxis = mainAxis;
        Reverse = reverse;
    }

    public Axis MainAxis { get; }

    public bool Reverse { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderListBody(ResolveAxisDirection(context));

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject) =>
        ((RenderListBody)renderObject).AxisDirection = ResolveAxisDirection(context);

    private AxisDirection ResolveAxisDirection(BuildContext context)
    {
        if (MainAxis == Axis.Vertical) return Reverse ? AxisDirection.Up : AxisDirection.Down;
        bool rtl = Directionality.Of(context) == TextDirection.Rtl;
        return rtl ^ Reverse ? AxisDirection.Left : AxisDirection.Right;
    }
}
