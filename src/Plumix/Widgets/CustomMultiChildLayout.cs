using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

public sealed class LayoutId : ParentDataWidget<MultiChildLayoutParentData>
{
    public LayoutId(
        object id,
        Widget child,
        Key? key = null) : base(
            child,
            key ?? new ValueKey<object>(id ?? throw new ArgumentNullException(nameof(id))))
    {
        Id = id;
    }

    public object Id { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(CustomMultiChildLayout);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (MultiChildLayoutParentData)renderObject.parentData!;
        if (Equals(parentData.Id, Id))
        {
            return;
        }

        parentData.Id = Id;
        renderObject.Parent?.MarkNeedsLayout();
    }
}

public sealed class CustomMultiChildLayout : MultiChildRenderObjectWidget
{
    public CustomMultiChildLayout(
        MultiChildLayoutDelegate @delegate,
        IReadOnlyList<Widget>? children = null,
        Key? key = null) : base(children, key)
    {
        Delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
    }

    public MultiChildLayoutDelegate Delegate { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCustomMultiChildLayoutBox(Delegate);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderCustomMultiChildLayoutBox)renderObject).Delegate = Delegate;
    }
}
