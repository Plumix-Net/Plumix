using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/undo_history.dart

namespace Plumix.Widgets;

/// <summary>
/// Provides undo/redo capabilities for a <see cref="IValueListenable{T}"/>: records the value's
/// history (throttled to one entry per 500 ms) and restores it on <see cref="UndoTextIntent"/>,
/// <see cref="RedoTextIntent"/>, platform undo requests and <see cref="UndoHistoryController"/> calls.
/// </summary>
/// <remarks>
/// Dart types <c>value</c> as a <c>ValueNotifier&lt;T&gt;</c>; Plumix's <see cref="TextEditingController"/>
/// is not a <see cref="ValueNotifier{T}"/>, so the history listens to any
/// <see cref="IValueListenable{T}"/> and relies on <see cref="OnTriggered"/> to write the value back,
/// exactly as Dart's does. Dart passes <c>null</c> to <see cref="ShouldChangeUndoStack"/> before the
/// first value is recorded; for a value type <typeparamref name="T"/> that is <c>default</c>.
/// </remarks>
/// <typeparam name="T">The type of the recorded value.</typeparam>
public sealed class UndoHistory<T> : StatefulWidget where T : notnull
{
    public UndoHistory(
        IValueListenable<T> value,
        Action<T> onTriggered,
        FocusNode focusNode,
        Widget child,
        Func<T?, T, bool>? shouldChangeUndoStack = null,
        Func<T, T>? undoStackModifier = null,
        UndoHistoryController? controller = null,
        Key? key = null) : base(key)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        OnTriggered = onTriggered ?? throw new ArgumentNullException(nameof(onTriggered));
        FocusNode = focusNode ?? throw new ArgumentNullException(nameof(focusNode));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        ShouldChangeUndoStack = shouldChangeUndoStack;
        UndoStackModifier = undoStackModifier;
        Controller = controller;
    }

    /// <summary>The value to track over time.</summary>
    public IValueListenable<T> Value { get; }

    /// <summary>Called when checking whether a value change should be pushed onto the undo stack.</summary>
    public Func<T?, T, bool>? ShouldChangeUndoStack { get; }

    /// <summary>Called right before a new entry is pushed to the undo stack.</summary>
    public Func<T, T>? UndoStackModifier { get; }

    /// <summary>Called when an undo or redo causes a state change; must set <see cref="Value"/>.</summary>
    public Action<T> OnTriggered { get; }

    /// <summary>The <see cref="FocusNode"/> that decides whether the history receives platform undo.</summary>
    public FocusNode FocusNode { get; }

    /// <summary>Controls the undo state; one is created internally when null.</summary>
    public UndoHistoryController? Controller { get; }

    /// <summary>The child widget of <see cref="UndoHistory{T}"/>.</summary>
    public Widget Child { get; }

    public override State CreateState() => new UndoHistoryState<T>();
}

/// <summary>State for an <see cref="UndoHistory{T}"/>.</summary>
/// <typeparam name="T">The type of the recorded value.</typeparam>
public sealed class UndoHistoryState<T> : State<UndoHistory<T>>, IUndoManagerClient where T : notnull
{
    // This duration was chosen as a best fit for the behavior of Mac, Linux, and Windows undo/redo
    // state save durations, but it is not perfect for any of them.
    private static readonly TimeSpan KThrottleDuration = TimeSpan.FromMilliseconds(500);

    private readonly UndoStack _stack = new();
    private Func<T, GestureTimer> _throttledPush = null!;
    private GestureTimer? _throttleTimer;
    private bool _duringTrigger;

    // Record the last value to prevent pushing multiple of the same value in a row onto the undo
    // stack. For example, _push gets called both in initState and when the EditableText receives focus.
    private T? _lastValue;
    private bool _hasLastValue;
    private UndoHistoryController? _controller;

    private UndoHistoryController EffectiveController =>
        Widget.Controller ?? (_controller ??= new UndoHistoryController());

    public void Undo()
    {
        if (!_stack.HasCurrentValue)
        {
            // Returns early if there is not a first value registered in the history. This is
            // important because, if an undo is received while the initial value is being pushed
            // (a.k.a when the field gets the focus but the throttling delay is pending), the initial
            // push should not be canceled.
            return;
        }

        if (_throttleTimer?.IsActive ?? false)
        {
            _throttleTimer?.Cancel(); // Cancel ongoing push, if any.
            Update(_stack.CurrentValue, _stack.HasCurrentValue);
        }
        else
        {
            bool has = _stack.Undo(out T? value);
            Update(value, has);
        }

        UpdateState();
    }

    public void Redo()
    {
        bool has = _stack.Redo(out T? value);
        Update(value, has);
        UpdateState();
    }

    public bool CanUndo => _stack.CanUndo;

    public bool CanRedo => _stack.CanRedo;

    private void UpdateState()
    {
        EffectiveController.Value = new UndoHistoryValue(canUndo: CanUndo, canRedo: CanRedo);

        if (PlatformDefaults.TargetPlatform != TargetPlatform.IOS)
        {
            return;
        }

        if (ReferenceEquals(UndoManager.Client, this))
        {
            UndoManager.SetUndoState(canUndo: CanUndo, canRedo: CanRedo);
        }
    }

    private object? UndoFromIntent(UndoTextIntent intent)
    {
        Undo();
        return null;
    }

    private object? RedoFromIntent(RedoTextIntent intent)
    {
        Redo();
        return null;
    }

    private void Update(T? nextValue, bool hasValue)
    {
        if (!hasValue)
        {
            return;
        }

        if (_hasLastValue && EqualityComparer<T>.Default.Equals(nextValue!, _lastValue!))
        {
            return;
        }

        _lastValue = nextValue;
        _hasLastValue = true;
        _duringTrigger = true;
        try
        {
            Widget.OnTriggered(nextValue!);
            if (Constants.KDebugMode && !EqualityComparer<T>.Default.Equals(Widget.Value.Value, nextValue!))
            {
                throw new AssertionError("onTriggered must set the value to the one it was given.");
            }
        }
        finally
        {
            _duringTrigger = false;
        }
    }

    private void Push()
    {
        if (_hasLastValue && EqualityComparer<T>.Default.Equals(Widget.Value.Value, _lastValue!))
        {
            return;
        }

        if (_duringTrigger)
        {
            return;
        }

        if (!(Widget.ShouldChangeUndoStack?.Invoke(_hasLastValue ? _lastValue : default, Widget.Value.Value)
              ?? true))
        {
            return;
        }

        T nextValue = Widget.UndoStackModifier is { } modifier
            ? modifier(Widget.Value.Value)
            : Widget.Value.Value;
        if (_hasLastValue && EqualityComparer<T>.Default.Equals(nextValue, _lastValue!))
        {
            return;
        }

        _lastValue = nextValue;
        _hasLastValue = true;
        _throttleTimer = _throttledPush(nextValue);
    }

    private void HandleFocus()
    {
        if (!Widget.FocusNode.HasFocus)
        {
            if (ReferenceEquals(UndoManager.Client, this))
            {
                UndoManager.Client = null;
            }

            return;
        }

        UndoManager.Client = this;
        UpdateState();
    }

    public void HandlePlatformUndo(UndoDirection direction)
    {
        switch (direction)
        {
            case UndoDirection.Undo:
                Undo();
                break;
            case UndoDirection.Redo:
                Redo();
                break;
        }
    }

    public override void InitState()
    {
        base.InitState();
        _throttledPush = Throttle<T>(
            duration: KThrottleDuration,
            function: currentValue =>
            {
                _stack.Push(currentValue);
                UpdateState();
            });
        Push();
        Widget.Value.AddListener(Push);
        HandleFocus();
        Widget.FocusNode.AddListener(HandleFocus);
        EffectiveController.OnUndo.AddListener(Undo);
        EffectiveController.OnRedo.AddListener(Redo);
    }

    public override void DidUpdateWidget(UndoHistory<T> oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (!ReferenceEquals(Widget.Value, oldWidget.Value))
        {
            _stack.Clear();
            oldWidget.Value.RemoveListener(Push);
            Widget.Value.AddListener(Push);
        }

        if (!ReferenceEquals(Widget.FocusNode, oldWidget.FocusNode))
        {
            oldWidget.FocusNode.RemoveListener(HandleFocus);
            Widget.FocusNode.AddListener(HandleFocus);
        }

        if (!ReferenceEquals(Widget.Controller, oldWidget.Controller))
        {
            UndoHistoryController previous = oldWidget.Controller ?? _controller!;
            previous.OnUndo.RemoveListener(Undo);
            previous.OnRedo.RemoveListener(Redo);
            _controller?.Dispose();
            _controller = null;
            EffectiveController.OnUndo.AddListener(Undo);
            EffectiveController.OnRedo.AddListener(Redo);
        }
    }

    public override void Dispose()
    {
        if (ReferenceEquals(UndoManager.Client, this))
        {
            UndoManager.Client = null;
        }

        Widget.Value.RemoveListener(Push);
        Widget.FocusNode.RemoveListener(HandleFocus);
        EffectiveController.OnUndo.RemoveListener(Undo);
        EffectiveController.OnRedo.RemoveListener(Redo);
        _controller?.Dispose();
        _throttleTimer?.Cancel();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Actions(
            actions: new Dictionary<Type, FlutterAction>
            {
                [typeof(UndoTextIntent)] = FlutterAction.Overridable(
                    context: context,
                    defaultAction: new CallbackAction<UndoTextIntent>(UndoFromIntent)),
                [typeof(RedoTextIntent)] = FlutterAction.Overridable(
                    context: context,
                    defaultAction: new CallbackAction<RedoTextIntent>(RedoFromIntent)),
            },
            child: Widget.Child);
    }

    /// Dart's file-private `_throttle`: the first call starts a timer; calls while it is active only
    /// replace the argument the timer will push.
    private static Func<TArg, GestureTimer> Throttle<TArg>(TimeSpan duration, Action<TArg> function)
    {
        GestureTimer? timer = null;
        TArg arg = default!;
        return currentArg =>
        {
            arg = currentArg;
            if (timer is { IsActive: true })
            {
                return timer;
            }

            timer = GestureTimer.Start(duration, () =>
            {
                function(arg);
                timer = null;
            });
            return timer;
        };
    }

    /// Dart's file-private `_UndoStack<T>`.
    private sealed class UndoStack
    {
        private readonly List<T> _list = [];

        // The index of the current value, or -1 if the list is empty.
        private int _index = -1;

        public bool HasCurrentValue => _list.Count != 0;

        public T? CurrentValue => _list.Count == 0 ? default : _list[_index];

        public bool CanUndo => _list.Count != 0 && _index > 0;

        public bool CanRedo => _list.Count != 0 && _index < _list.Count - 1;

        public void Push(T value)
        {
            if (_list.Count == 0)
            {
                _index = 0;
                _list.Add(value);
                return;
            }

            AssertIndex();

            if (EqualityComparer<T>.Default.Equals(value, CurrentValue!))
            {
                return;
            }

            // If anything has been undone in this stack, remove those irrelevant states before
            // adding the new one.
            if (_index != _list.Count - 1)
            {
                _list.RemoveRange(_index + 1, _list.Count - _index - 1);
            }

            _list.Add(value);
            _index = _list.Count - 1;
        }

        public bool Undo(out T? value)
        {
            if (_list.Count == 0)
            {
                value = default;
                return false;
            }

            AssertIndex();

            if (_index != 0)
            {
                _index--;
            }

            value = CurrentValue;
            return true;
        }

        public bool Redo(out T? value)
        {
            if (_list.Count == 0)
            {
                value = default;
                return false;
            }

            AssertIndex();

            if (_index < _list.Count - 1)
            {
                _index++;
            }

            value = CurrentValue;
            return true;
        }

        private void AssertIndex()
        {
            if (Constants.KDebugMode && !(_index < _list.Count && _index >= 0))
            {
                throw new AssertionError("_index < _list.length && _index >= 0");
            }
        }

        public void Clear()
        {
            _list.Clear();
            _index = -1;
        }

        public override string ToString() => $"_UndoStack [{string.Join(", ", _list)}]";
    }
}

/// <summary>Represents whether the current undo history can undo or redo.</summary>
public sealed class UndoHistoryValue : IEquatable<UndoHistoryValue>
{
    public UndoHistoryValue(bool canUndo = false, bool canRedo = false)
    {
        CanUndo = canUndo;
        CanRedo = canRedo;
    }

    /// <summary>A value corresponding to an undo history where neither undo nor redo is possible.</summary>
    public static UndoHistoryValue Empty { get; } = new();

    /// <summary>Whether the current undo history can perform an undo operation.</summary>
    public bool CanUndo { get; }

    /// <summary>Whether the current undo history can perform a redo operation.</summary>
    public bool CanRedo { get; }

    public override string ToString() => $"UndoHistoryValue(canUndo: {CanUndo}, canRedo: {CanRedo})";

    public bool Equals(UndoHistoryValue? other) =>
        other is not null && (ReferenceEquals(this, other) || (other.CanUndo == CanUndo && other.CanRedo == CanRedo));

    public override bool Equals(object? obj) => Equals(obj as UndoHistoryValue);

    public override int GetHashCode() => HashCode.Combine(CanUndo.GetHashCode(), CanRedo.GetHashCode());
}

/// <summary>A controller for the <see cref="UndoHistory{T}"/> widget: exposes whether undo/redo is
/// possible and triggers them.</summary>
public class UndoHistoryController : ValueNotifier<UndoHistoryValue>
{
    public UndoHistoryController(UndoHistoryValue? value = null) : base(value ?? UndoHistoryValue.Empty)
    {
    }

    /// <summary>Notifies listeners that <see cref="Undo"/> has been called.</summary>
    public ChangeNotifier OnUndo { get; } = new();

    /// <summary>Notifies listeners that <see cref="Redo"/> has been called.</summary>
    public ChangeNotifier OnRedo { get; } = new();

    /// <summary>Reverts the value on the stack to the previous value.</summary>
    public void Undo()
    {
        if (!Value.CanUndo)
        {
            return;
        }

        OnUndo.NotifyListeners();
    }

    /// <summary>Updates the value on the stack to the next value.</summary>
    public void Redo()
    {
        if (!Value.CanRedo)
        {
            return;
        }

        OnRedo.NotifyListeners();
    }

    public override void Dispose()
    {
        OnUndo.Dispose();
        OnRedo.Dispose();
        base.Dispose();
    }
}
