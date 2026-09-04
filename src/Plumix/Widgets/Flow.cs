using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

namespace Plumix.Widgets;

/// <summary>Sizes and positions children during paint according to a <see cref="FlowDelegate"/>.</summary>
public sealed class Flow : MultiChildRenderObjectWidget
{
    public Flow(
        FlowDelegate @delegate,
        IReadOnlyList<Widget>? children = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : this(
        @delegate,
        children ?? [],
        clipBehavior,
        key,
        wrapChildren: true)
    {
    }

    private Flow(
        FlowDelegate @delegate,
        IReadOnlyList<Widget> children,
        Clip clipBehavior,
        Key? key,
        bool wrapChildren) : base(wrapChildren ? RepaintBoundary.WrapAll(children) : children, key)
    {
        Delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        ClipBehavior = clipBehavior;
    }

    public FlowDelegate Delegate { get; }

    public Clip ClipBehavior { get; }

    public static Flow Unwrapped(
        FlowDelegate @delegate,
        IReadOnlyList<Widget>? children = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null)
    {
        return new Flow(@delegate, children ?? [], clipBehavior, key, wrapChildren: false);
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFlow(Delegate, clipBehavior: ClipBehavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var flow = (RenderFlow)renderObject;
        flow.Delegate = Delegate;
        flow.ClipBehavior = ClipBehavior;
    }
}
