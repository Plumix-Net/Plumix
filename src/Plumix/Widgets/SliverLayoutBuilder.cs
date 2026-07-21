using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_layout_builder.dart

/// <summary>The signature of a <see cref="SliverLayoutBuilder"/> builder callback.</summary>
public delegate Widget SliverLayoutWidgetBuilder(BuildContext context, SliverConstraints constraints);

/// <summary>Builds a sliver subtree during layout with the incoming sliver constraints.</summary>
public sealed class SliverLayoutBuilder : RenderObjectWidget
{
    public SliverLayoutBuilder(SliverLayoutWidgetBuilder builder, Key? key = null) : base(key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public SliverLayoutWidgetBuilder Builder { get; }

    internal override Element CreateElement() => new SliverLayoutBuilderElement(this);

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderSliverLayoutBuilder();

    internal bool UpdateShouldRebuild(SliverLayoutBuilder oldWidget) => true;
}

internal sealed class SliverLayoutBuilderElement : RenderObjectElement
{
    private Element? _child;
    private SliverConstraints? _previousConstraints;
    private bool _needsBuild = true;

    public SliverLayoutBuilderElement(SliverLayoutBuilder widget) : base(widget)
    {
    }

    private SliverLayoutBuilder LayoutBuilderWidget => (SliverLayoutBuilder)Widget;

    private RenderSliverLayoutBuilder LayoutRenderObject => (RenderSliverLayoutBuilder)RequireRenderObject();

    protected override void OnMount()
    {
        base.OnMount();
        LayoutRenderObject.UpdateCallback(RebuildWithConstraints);
    }

    internal override void Update(Widget newWidget)
    {
        var oldWidget = LayoutBuilderWidget;
        base.Update(newWidget);
        LayoutRenderObject.UpdateCallback(RebuildWithConstraints);

        if (LayoutBuilderWidget.UpdateShouldRebuild(oldWidget))
        {
            _needsBuild = true;
            LayoutRenderObject.ScheduleLayoutCallback();
        }
    }

    internal override void MarkNeedsBuild()
    {
        if (!IsActive)
        {
            return;
        }

        Dirty = false;
        _needsBuild = true;
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    internal override void Rebuild()
    {
        Dirty = false;
        _needsBuild = true;
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    private void RebuildWithConstraints(SliverConstraints constraints)
    {
        if (!_needsBuild && _previousConstraints == constraints)
        {
            return;
        }

        Widget built = LayoutBuilderWidget.Builder(new BuildContext(this), constraints)
            ?? throw new InvalidOperationException("SliverLayoutBuilder.Builder must return a widget.");
        _child = UpdateChild(_child, built, null);
        _needsBuild = false;
        _previousConstraints = constraints;
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    internal override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot != null)
        {
            throw new InvalidOperationException("SliverLayoutBuilder expects a null child slot.");
        }

        LayoutRenderObject.Child = (RenderSliver)child;
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        throw new InvalidOperationException("SliverLayoutBuilder does not support moving its single child.");
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(LayoutRenderObject.Child, child))
        {
            LayoutRenderObject.Child = null;
        }
    }

    internal override void Unmount()
    {
        LayoutRenderObject.UpdateCallback(null);
        if (_child != null)
        {
            UnmountChild(_child);
            _child = null;
        }

        base.Unmount();
    }
}

internal sealed class RenderSliverLayoutBuilder : RenderProxySliver
{
    private Action<SliverConstraints>? _callback;

    internal void UpdateCallback(Action<SliverConstraints>? callback)
    {
        if (_callback == callback)
        {
            return;
        }

        _callback = callback;
        ScheduleLayoutCallback();
    }

    internal void ScheduleLayoutCallback() => MarkNeedsLayout();

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (_callback != null)
        {
            InvokeLayoutCallback(_callback, constraints);
        }

        if (Child == null)
        {
            Geometry = default;
            return;
        }

        Child.LayoutWithSliverConstraints(constraints);
        ((SliverPhysicalParentData)Child.parentData!).offset = new Point();
        Geometry = Child.Geometry;
    }
}
