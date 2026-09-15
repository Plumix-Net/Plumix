using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: cupertino_ui/lib/src/dialog.dart

namespace Plumix.Cupertino;

/// <summary>Dart's `_SlidingTapGestureRecognizer`: responsive selection beside vertical drags.</summary>
internal sealed class SlidingTapGestureRecognizer : VerticalDragGestureRecognizer
{
    private int? _primaryPointer;

    public SlidingTapGestureRecognizer(object? debugOwner = null) : base(debugOwner: debugOwner)
    {
        DragStartBehavior = DragStartBehavior.Down;
    }

    public override string DebugDescription => "tap slide";

    public Action<Point>? OnResponsiveUpdate { get; set; }

    public Action<Point>? OnResponsiveEnd { get; set; }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        _primaryPointer ??= @event.Pointer;
        base.AddAllowedPointer(@event);
    }

    public override void RejectGesture(int pointer)
    {
        if (pointer == _primaryPointer)
        {
            _primaryPointer = null;
        }

        base.RejectGesture(pointer);
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (@event.Pointer == _primaryPointer)
        {
            if (@event is PointerMoveEvent)
            {
                OnResponsiveUpdate?.Invoke(@event.Position);
            }
            else if (@event is PointerUpEvent)
            {
                // Keep the arena entry until DidStopTrackingLastPointer resolves it. The final
                // responsive hit test runs after any cancellation caused by stopping tracking.
                StopTrackingPointer(@event.Pointer);
                OnResponsiveEnd?.Invoke(@event.Position);
                _primaryPointer = null;
                return;
            }
            else if (@event is PointerCancelEvent)
            {
                _primaryPointer = null;
            }
        }

        base.HandleEvent(@event);
    }
}

/// <summary>Dart's `_TargetSelectionGestureRecognizer`: talks to metadata in the view's hit path.</summary>
internal sealed class TargetSelectionGestureRecognizer : GestureRecognizer
{
    private readonly SlidingTapGestureRecognizer _slidingTap;
    private readonly List<ISlideTarget> _currentTargets = [];
    private readonly Func<Point, HitTestResult> _hitTest;

    public TargetSelectionGestureRecognizer(Func<Point, HitTestResult> hitTest, object? debugOwner = null)
        : base(debugOwner: debugOwner)
    {
        _hitTest = hitTest;
        _slidingTap = new SlidingTapGestureRecognizer(debugOwner)
        {
            OnDown = OnDown,
            OnResponsiveUpdate = OnUpdate,
            OnResponsiveEnd = OnEnd,
            OnCancel = OnCancel,
        };
    }

    public override void AddPointer(PointerDownEvent @event) => _slidingTap.AddPointer(@event);

    public override void AddPointerPanZoom(PointerPanZoomStartEvent @event) => _slidingTap.AddPointerPanZoom(@event);

    public override void AcceptGesture(int pointer) => _slidingTap.AcceptGesture(pointer);

    public override void RejectGesture(int pointer) => _slidingTap.RejectGesture(pointer);

    public override void Dispose()
    {
        _slidingTap.Dispose();
        base.Dispose();
    }

    public override string DebugDescription => "target selection";

    private void OnDown(DragDownDetails details) => Update(details.GlobalPosition, fromPointerDown: true);

    private void OnUpdate(Point position) => Update(position, fromPointerDown: false);

    private void OnEnd(Point position)
    {
        Update(position, fromPointerDown: false);
        foreach (ISlideTarget target in _currentTargets)
        {
            target.DidConfirm();
        }

        _currentTargets.Clear();
    }

    private void OnCancel()
    {
        foreach (ISlideTarget target in _currentTargets)
        {
            target.DidLeave();
        }

        _currentTargets.Clear();
    }

    private void Update(Point position, bool fromPointerDown)
    {
        var foundTargets = new List<ISlideTarget>();
        foreach (HitTestEntry entry in _hitTest(position).Path)
        {
            if (entry.Target is RenderMetaData { MetaData: ISlideTarget target })
            {
                foundTargets.Add(target);
            }
        }

        ISlideTarget? oldTarget = _currentTargets.FirstOrDefault();
        ISlideTarget? newTarget = foundTargets.FirstOrDefault();
        if (ReferenceEquals(oldTarget, newTarget))
        {
            return;
        }

        foreach (ISlideTarget target in _currentTargets)
        {
            target.DidLeave();
        }

        _currentTargets.Clear();
        _currentTargets.AddRange(foundTargets);
        bool innerEnabled = true;
        foreach (ISlideTarget target in _currentTargets)
        {
            innerEnabled = target.DidEnter(fromPointerDown, innerEnabled);
        }
    }
}

/// <summary>Dart's `_ActionSheetGestureDetector`, shared by alerts and action sheets.</summary>
internal sealed class ActionSheetGestureDetector : StatelessWidget
{
    public ActionSheetGestureDetector(Widget child, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget Child { get; }

    private static HitTestResult HitTest(BuildContext context, Point globalPosition)
    {
        int viewId = View.Of(context).ViewId;
        return GestureBinding.Instance.HitTestInView(globalPosition, viewId);
    }

    public override Widget Build(BuildContext context)
    {
        return new RawGestureDetector(
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TargetSelectionGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<TargetSelectionGestureRecognizer>(
                        () => new TargetSelectionGestureRecognizer(position => HitTest(context, position), this),
                        _ => { }),
            },
            excludeFromSemantics: true,
            child: Child);
    }
}
