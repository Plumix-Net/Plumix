using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/layout_builder.dart

/// <summary>The signature of a <see cref="LayoutBuilder"/> builder callback.</summary>
public delegate Widget LayoutWidgetBuilder(BuildContext context, BoxConstraints constraints);

/// <summary>Builds a widget subtree during layout with the incoming box constraints.</summary>
public sealed class LayoutBuilder : RenderObjectWidget
{
    public LayoutBuilder(LayoutWidgetBuilder builder, Key? key = null) : base(key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public LayoutWidgetBuilder Builder { get; }

    public override Element CreateElement() => new LayoutBuilderElement(this);

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderLayoutBuilder();

    internal bool UpdateShouldRebuild(LayoutBuilder oldWidget) => true;
}

internal sealed class LayoutBuilderElement : RenderObjectElement
{
    private Element? _child;
    private BoxConstraints? _previousConstraints;
    private bool _needsBuild = true;

    public LayoutBuilderElement(LayoutBuilder widget) : base(widget)
    {
    }

    private LayoutBuilder LayoutBuilderWidget => (LayoutBuilder)Widget;

    private RenderLayoutBuilder LayoutRenderObject => (RenderLayoutBuilder)RequireRenderObject();

    protected override void OnMount()
    {
        base.OnMount();
        LayoutRenderObject.UpdateCallback(RebuildWithConstraints);
    }

    public override void Update(Widget newWidget)
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

    public override void MarkNeedsBuild()
    {
        if (!IsActive)
        {
            return;
        }

        base.PerformRebuild();
        _needsBuild = true;
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    protected override void PerformRebuild()
    {
        base.PerformRebuild();
        _needsBuild = true;
        LayoutRenderObject.ScheduleLayoutCallback();
    }

    private void RebuildWithConstraints(BoxConstraints constraints)
    {
        if (!_needsBuild && _previousConstraints == constraints)
        {
            return;
        }

        Widget built = LayoutBuilderWidget.Builder(this, constraints)
            ?? throw new InvalidOperationException("LayoutBuilder.Builder must return a widget.");
        _child = UpdateChild(_child, built, null);
        _needsBuild = false;
        _previousConstraints = constraints;
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void ForgetChild(Element child)
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
            throw new InvalidOperationException("LayoutBuilder expects a null child slot.");
        }

        LayoutRenderObject.Child = (RenderBox)child;
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        throw new InvalidOperationException("LayoutBuilder does not support moving its single child.");
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(LayoutRenderObject.Child, child))
        {
            LayoutRenderObject.Child = null;
        }
    }

    public override void Unmount()
    {
        LayoutRenderObject.ClearCallback();
        if (_child != null)
        {
            UnmountChild(_child);
            _child = null;
        }

        base.Unmount();
    }
}

internal sealed class RenderLayoutBuilder : RenderProxyBox, IRenderObjectWithLayoutCallback
{
    private Action<BoxConstraints>? _callback;

    internal void UpdateCallback(Action<BoxConstraints> callback)
    {
        if (_callback == callback)
        {
            return;
        }

        _callback = callback;
        ScheduleLayoutCallback();
    }

    /// <remarks>Flutter's <c>_LayoutBuilderElement.unmount</c> assigns <c>_callback = null</c> directly,
    /// without scheduling another callback run.</remarks>
    internal void ClearCallback() => _callback = null;

    void IRenderObjectWithLayoutCallback.LayoutCallback() => _callback!(Constraints);

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        RunLayoutCallback();

        if (Child != null)
        {
            Child.Layout(constraints, parentUsesSize: true);
            Size = constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = new Point();
        }
        else
        {
            Size = constraints.Biggest;
        }
    }
}
