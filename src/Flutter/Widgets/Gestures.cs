using Flutter.Foundation;
using Flutter.Gestures;
using Flutter.Rendering;
using Flutter.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/gesture_detector.dart (approximate)

namespace Flutter.Widgets;

public sealed class Listener : SingleChildRenderObjectWidget
{
    public Listener(
        Widget? child = null,
        Action<PointerDownEvent>? onPointerDown = null,
        Action<PointerMoveEvent>? onPointerMove = null,
        Action<PointerEnterEvent>? onPointerEnter = null,
        Action<PointerExitEvent>? onPointerExit = null,
        Action<PointerHoverEvent>? onPointerHover = null,
        Action<PointerUpEvent>? onPointerUp = null,
        Action<PointerCancelEvent>? onPointerCancel = null,
        Action<PointerSignalEvent>? onPointerSignal = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Key? key = null) : base(child, key)
    {
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerEnter = onPointerEnter;
        OnPointerExit = onPointerExit;
        OnPointerHover = onPointerHover;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnPointerSignal = onPointerSignal;
        Behavior = behavior;
    }

    public Action<PointerDownEvent>? OnPointerDown { get; }

    public Action<PointerMoveEvent>? OnPointerMove { get; }

    public Action<PointerEnterEvent>? OnPointerEnter { get; }

    public Action<PointerExitEvent>? OnPointerExit { get; }

    public Action<PointerHoverEvent>? OnPointerHover { get; }

    public Action<PointerUpEvent>? OnPointerUp { get; }

    public Action<PointerCancelEvent>? OnPointerCancel { get; }

    public Action<PointerSignalEvent>? OnPointerSignal { get; }

    public HitTestBehavior Behavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPointerListener(
            onPointerDown: OnPointerDown,
            onPointerMove: OnPointerMove,
            onPointerEnter: OnPointerEnter,
            onPointerExit: OnPointerExit,
            onPointerHover: OnPointerHover,
            onPointerUp: OnPointerUp,
            onPointerCancel: OnPointerCancel,
            onPointerSignal: OnPointerSignal,
            behavior: Behavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var listener = (RenderPointerListener)renderObject;
        listener.OnPointerDown = OnPointerDown;
        listener.OnPointerMove = OnPointerMove;
        listener.OnPointerEnter = OnPointerEnter;
        listener.OnPointerExit = OnPointerExit;
        listener.OnPointerHover = OnPointerHover;
        listener.OnPointerUp = OnPointerUp;
        listener.OnPointerCancel = OnPointerCancel;
        listener.OnPointerSignal = OnPointerSignal;
        listener.Behavior = Behavior;
    }
}

public sealed class RawGestureDetector : StatefulWidget
{
    public RawGestureDetector(
        Widget? child = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Action<PointerDownEvent>? onPointerDown = null,
        Action<PointerMoveEvent>? onPointerMove = null,
        Action<PointerUpEvent>? onPointerUp = null,
        Action<PointerCancelEvent>? onPointerCancel = null,
        Action? onTap = null,
        Action? onLongPress = null,
        Action<DragStartDetails>? onHorizontalDragStart = null,
        Action<DragUpdateDetails>? onHorizontalDragUpdate = null,
        Action<DragEndDetails>? onHorizontalDragEnd = null,
        Action? onHorizontalDragCancel = null,
        Action<DragStartDetails>? onVerticalDragStart = null,
        Action<DragUpdateDetails>? onVerticalDragUpdate = null,
        Action<DragEndDetails>? onVerticalDragEnd = null,
        Action? onVerticalDragCancel = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Behavior = behavior;
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnTap = onTap;
        OnLongPress = onLongPress;
        OnHorizontalDragStart = onHorizontalDragStart;
        OnHorizontalDragUpdate = onHorizontalDragUpdate;
        OnHorizontalDragEnd = onHorizontalDragEnd;
        OnHorizontalDragCancel = onHorizontalDragCancel;
        OnVerticalDragStart = onVerticalDragStart;
        OnVerticalDragUpdate = onVerticalDragUpdate;
        OnVerticalDragEnd = onVerticalDragEnd;
        OnVerticalDragCancel = onVerticalDragCancel;
    }

    public Widget? Child { get; }

    public HitTestBehavior Behavior { get; }

    public Action<PointerDownEvent>? OnPointerDown { get; }

    public Action<PointerMoveEvent>? OnPointerMove { get; }

    public Action<PointerUpEvent>? OnPointerUp { get; }

    public Action<PointerCancelEvent>? OnPointerCancel { get; }

    public Action? OnTap { get; }

    public Action? OnLongPress { get; }

    public Action<DragStartDetails>? OnHorizontalDragStart { get; }

    public Action<DragUpdateDetails>? OnHorizontalDragUpdate { get; }

    public Action<DragEndDetails>? OnHorizontalDragEnd { get; }

    public Action? OnHorizontalDragCancel { get; }

    public Action<DragStartDetails>? OnVerticalDragStart { get; }

    public Action<DragUpdateDetails>? OnVerticalDragUpdate { get; }

    public Action<DragEndDetails>? OnVerticalDragEnd { get; }

    public Action? OnVerticalDragCancel { get; }

    public override State CreateState()
    {
        return new RawGestureDetectorState();
    }

    private sealed class RawGestureDetectorState : State
    {
        private TapGestureRecognizer? _tap;
        private LongPressGestureRecognizer? _longPress;
        private HorizontalDragGestureRecognizer? _horizontalDrag;
        private VerticalDragGestureRecognizer? _verticalDrag;

        private RawGestureDetector CurrentWidget => (RawGestureDetector)Element.Widget;

        public override void InitState()
        {
            SyncRecognizers();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            SyncRecognizers();
        }

        public override void Dispose()
        {
            DisposeRecognizer(ref _tap);
            DisposeRecognizer(ref _longPress);
            DisposeRecognizer(ref _horizontalDrag);
            DisposeRecognizer(ref _verticalDrag);
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            return new Listener(
                child: widget.Child,
                behavior: widget.Behavior,
                onPointerDown: HandlePointerDown,
                onPointerMove: widget.OnPointerMove,
                onPointerUp: widget.OnPointerUp,
                onPointerCancel: widget.OnPointerCancel);
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            var widget = CurrentWidget;
            widget.OnPointerDown?.Invoke(@event);
            _tap?.AddPointer(@event);
            _longPress?.AddPointer(@event);
            _horizontalDrag?.AddPointer(@event);
            _verticalDrag?.AddPointer(@event);
        }

        private void SyncRecognizers()
        {
            var widget = CurrentWidget;

            if (widget.OnTap != null)
            {
                _tap ??= new TapGestureRecognizer();
                _tap.OnTap = widget.OnTap;
            }
            else
            {
                DisposeRecognizer(ref _tap);
            }

            if (widget.OnLongPress != null)
            {
                _longPress ??= new LongPressGestureRecognizer();
                _longPress.OnLongPress = widget.OnLongPress;
            }
            else
            {
                DisposeRecognizer(ref _longPress);
            }

            if (widget.OnHorizontalDragStart != null
                || widget.OnHorizontalDragUpdate != null
                || widget.OnHorizontalDragEnd != null
                || widget.OnHorizontalDragCancel != null)
            {
                _horizontalDrag ??= new HorizontalDragGestureRecognizer();
                _horizontalDrag.OnStart = widget.OnHorizontalDragStart;
                _horizontalDrag.OnUpdate = widget.OnHorizontalDragUpdate;
                _horizontalDrag.OnEnd = widget.OnHorizontalDragEnd;
                _horizontalDrag.OnCancel = widget.OnHorizontalDragCancel;
            }
            else
            {
                DisposeRecognizer(ref _horizontalDrag);
            }

            if (widget.OnVerticalDragStart != null
                || widget.OnVerticalDragUpdate != null
                || widget.OnVerticalDragEnd != null
                || widget.OnVerticalDragCancel != null)
            {
                _verticalDrag ??= new VerticalDragGestureRecognizer();
                _verticalDrag.OnStart = widget.OnVerticalDragStart;
                _verticalDrag.OnUpdate = widget.OnVerticalDragUpdate;
                _verticalDrag.OnEnd = widget.OnVerticalDragEnd;
                _verticalDrag.OnCancel = widget.OnVerticalDragCancel;
            }
            else
            {
                DisposeRecognizer(ref _verticalDrag);
            }
        }

        private static void DisposeRecognizer<T>(ref T? recognizer) where T : GestureRecognizer
        {
            recognizer?.Dispose();
            recognizer = null;
        }
    }
}

public sealed class GestureDetector : StatelessWidget
{
    public GestureDetector(
        Widget? child = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Action? onTap = null,
        Action? onLongPress = null,
        Action<DragStartDetails>? onHorizontalDragStart = null,
        Action<DragUpdateDetails>? onHorizontalDragUpdate = null,
        Action<DragEndDetails>? onHorizontalDragEnd = null,
        Action? onHorizontalDragCancel = null,
        Action<DragStartDetails>? onVerticalDragStart = null,
        Action<DragUpdateDetails>? onVerticalDragUpdate = null,
        Action<DragEndDetails>? onVerticalDragEnd = null,
        Action? onVerticalDragCancel = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Behavior = behavior;
        OnTap = onTap;
        OnLongPress = onLongPress;
        OnHorizontalDragStart = onHorizontalDragStart;
        OnHorizontalDragUpdate = onHorizontalDragUpdate;
        OnHorizontalDragEnd = onHorizontalDragEnd;
        OnHorizontalDragCancel = onHorizontalDragCancel;
        OnVerticalDragStart = onVerticalDragStart;
        OnVerticalDragUpdate = onVerticalDragUpdate;
        OnVerticalDragEnd = onVerticalDragEnd;
        OnVerticalDragCancel = onVerticalDragCancel;
    }

    public Widget? Child { get; }

    public HitTestBehavior Behavior { get; }

    public Action? OnTap { get; }

    public Action? OnLongPress { get; }

    public Action<DragStartDetails>? OnHorizontalDragStart { get; }

    public Action<DragUpdateDetails>? OnHorizontalDragUpdate { get; }

    public Action<DragEndDetails>? OnHorizontalDragEnd { get; }

    public Action? OnHorizontalDragCancel { get; }

    public Action<DragStartDetails>? OnVerticalDragStart { get; }

    public Action<DragUpdateDetails>? OnVerticalDragUpdate { get; }

    public Action<DragEndDetails>? OnVerticalDragEnd { get; }

    public Action? OnVerticalDragCancel { get; }

    public override Widget Build(BuildContext context)
    {
        return new RawGestureDetector(
            child: Child,
            behavior: Behavior,
            onTap: OnTap,
            onLongPress: OnLongPress,
            onHorizontalDragStart: OnHorizontalDragStart,
            onHorizontalDragUpdate: OnHorizontalDragUpdate,
            onHorizontalDragEnd: OnHorizontalDragEnd,
            onHorizontalDragCancel: OnHorizontalDragCancel,
            onVerticalDragStart: OnVerticalDragStart,
            onVerticalDragUpdate: OnVerticalDragUpdate,
            onVerticalDragEnd: OnVerticalDragEnd,
            onVerticalDragCancel: OnVerticalDragCancel);
    }
}
