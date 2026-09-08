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
    private bool _deferredCallbackScheduled;

    // Dart's `late final BuildScope _buildScope = BuildScope(scheduleRebuild: _scheduleRebuild)`.
    // A LayoutBuilder builds its children during layout, so its descendants must not be rebuilt by
    // the ambient build pass; they get their own scope, flushed from RebuildWithConstraints.
    private readonly BuildScope _ownBuildScope;

    public LayoutBuilderElement(LayoutBuilder widget) : base(widget)
    {
        _ownBuildScope = new BuildScope(ScheduleRebuild);
    }

    /// <inheritdoc />
    public override BuildScope BuildScope => _ownBuildScope;

    /// <summary>
    /// Dart's <c>_LayoutBuilderElement._scheduleRebuild</c>: asks the render object to run its layout
    /// callback again. Marking the render object dirty outside a frame would dirty the render tree
    /// during the idle phase, so the request is deferred to the start of the next frame instead.
    /// </summary>
    private void ScheduleRebuild()
    {
        if (_deferredCallbackScheduled)
        {
            return;
        }

        bool deferMarkNeedsLayout = Scheduler.Phase
            is SchedulerPhase.Idle or SchedulerPhase.PostFrameCallbacks;
        if (!deferMarkNeedsLayout)
        {
            LayoutRenderObject.ScheduleLayoutCallback();
            return;
        }

        _deferredCallbackScheduled = true;
        Scheduler.ScheduleFrameCallback(_ =>
        {
            _deferredCallbackScheduled = false;
            if (Mounted)
            {
                LayoutRenderObject.ScheduleLayoutCallback();
            }
        });
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
        void UpdateChildCallback()
        {
            try
            {
                Widget built = LayoutBuilderWidget.Builder(this, constraints)
                    ?? throw new InvalidOperationException("LayoutBuilder.Builder must return a widget.");
                _child = UpdateChild(_child, built, null);
            }
            finally
            {
                _needsBuild = false;
                _previousConstraints = constraints;
            }
        }

        // Dart runs the builder inside `owner.buildScope(this, callback)`, and enters the scope even
        // when nothing needs building: descendants dirtied since the last layout live in this scope's
        // dirty list and nothing else will ever flush them. `BuildScope` returns immediately when the
        // callback is null and the list is empty.
        Action? callback = _needsBuild || _previousConstraints != constraints ? UpdateChildCallback : null;
        Owner!.BuildScope(this, callback);
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
        }
        else
        {
            Size = constraints.Biggest;
        }
    }
}
