using System.Collections.Generic;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_state.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/raw_radio.dart
public enum WidgetState
{
    Hovered,
    Focused,
    Pressed,
    Disabled,
    Selected,
    Dragged,
}

public abstract class WidgetStateProperty<T>
{
    public abstract T Resolve(IReadOnlySet<WidgetState> states);

    public static WidgetStateProperty<T> All(T value)
    {
        return new WidgetStatePropertyAll<T>(value);
    }

    public static WidgetStateProperty<T> ResolveWith(Func<IReadOnlySet<WidgetState>, T> resolver)
    {
        return new WidgetStatePropertyResolver<T>(resolver);
    }

    public static WidgetStateProperty<T>? Lerp(
        WidgetStateProperty<T>? a,
        WidgetStateProperty<T>? b,
        double t,
        Func<T, T, double, T> lerpFunction)
    {
        ArgumentNullException.ThrowIfNull(lerpFunction);
        if (a is null && b is null)
        {
            return null;
        }

        return ResolveWith(states => lerpFunction(
            a is null ? default! : a.Resolve(states),
            b is null ? default! : b.Resolve(states),
            t));
    }
}

public sealed class WidgetStatePropertyAll<T> : WidgetStateProperty<T>
{
    public WidgetStatePropertyAll(T value)
    {
        Value = value;
    }

    public T Value { get; }

    public override T Resolve(IReadOnlySet<WidgetState> states)
    {
        return Value;
    }
}

internal sealed class WidgetStatePropertyResolver<T> : WidgetStateProperty<T>
{
    private readonly Func<IReadOnlySet<WidgetState>, T> _resolver;

    public WidgetStatePropertyResolver(Func<IReadOnlySet<WidgetState>, T> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public override T Resolve(IReadOnlySet<WidgetState> states)
    {
        return _resolver(states);
    }
}

public delegate Widget RadioBuilder<T>(BuildContext context, RawRadioState<T> state);

public sealed class RawRadio<T> : StatefulWidget
{
    public RawRadio(
        T value,
        WidgetStateProperty<MouseCursor> mouseCursor,
        bool toggleable,
        FocusNode focusNode,
        bool autofocus,
        RadioGroupRegistry<T>? groupRegistry,
        bool enabled,
        RadioBuilder<T> builder,
        Key? key = null) : base(key)
    {
        if (enabled && groupRegistry is null)
        {
            throw new ArgumentException("An enabled RawRadio must have a group registry.", nameof(groupRegistry));
        }

        Value = value;
        MouseCursor = mouseCursor ?? throw new ArgumentNullException(nameof(mouseCursor));
        Toggleable = toggleable;
        FocusNode = focusNode ?? throw new ArgumentNullException(nameof(focusNode));
        Autofocus = autofocus;
        GroupRegistry = groupRegistry;
        Enabled = enabled;
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public T Value { get; }

    public WidgetStateProperty<MouseCursor> MouseCursor { get; }

    public bool Toggleable { get; }

    public FocusNode FocusNode { get; }

    public bool Autofocus { get; }

    public RadioGroupRegistry<T>? GroupRegistry { get; }

    public bool Enabled { get; }

    public RadioBuilder<T> Builder { get; }

    public override State CreateState()
    {
        return new RawRadioState<T>();
    }
}

public sealed class RawRadioState<T> : State, RadioClient<T>
{
    private readonly HashSet<WidgetState> _states = [];
    private AnimationController? _positionController;
    private AnimationController? _reactionController;
    private RadioGroupRegistry<T>? _registry;
    private IDisposable? _mouseCursorHandle;
    private bool _isHovered;
    private bool _isFocused;
    private bool _isPressed;

    private RawRadio<T> CurrentWidget => (RawRadio<T>)StateWidget;

    public IReadOnlySet<WidgetState> States => _states;

    public double Position => _positionController?.Evaluate() ?? (Selected ? 1.0 : 0.0);

    public double Reaction => _reactionController?.Evaluate() ?? 0.0;

    public bool Selected => EqualityComparer<T?>.Default.Equals(
        CurrentWidget.Value,
        _registry is null ? default : _registry.GroupValue);

    public bool Hovered => _isHovered;

    public bool Focused => _isFocused;

    public bool Pressed => _isPressed;

    public bool Tristate => CurrentWidget.Toggleable;

    public T RadioValue => CurrentWidget.Value;

    public bool Enabled => CurrentWidget.Enabled;

    public FocusNode FocusNode => CurrentWidget.FocusNode;

    public override void InitState()
    {
        _positionController = new AnimationController(TimeSpan.FromMilliseconds(200), this)
        {
            Curve = Curves.Linear
        };
        _positionController.Changed += HandleAnimationChanged;

        _reactionController = new AnimationController(TimeSpan.FromMilliseconds(100), this)
        {
            ReverseDuration = TimeSpan.FromMilliseconds(200),
            Curve = Curves.FastOutSlowIn
        };
        _reactionController.Changed += HandleAnimationChanged;

        AttachFocusNode();
        SetRegistry(CurrentWidget.GroupRegistry);
        _positionController.SetValue(Selected ? 1.0 : 0.0);
        SyncStates();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldRadio = (RawRadio<T>)oldWidget;
        if (!ReferenceEquals(oldRadio.FocusNode, CurrentWidget.FocusNode))
        {
            DetachFocusNode(oldRadio.FocusNode);
            AttachFocusNode();
        }

        SetRegistry(CurrentWidget.GroupRegistry);
        AnimateToValue();

        if (!Enabled)
        {
            _isPressed = false;
            _reactionController?.Reverse();
            ReleaseMouseCursor();
        }

        SyncStates();
    }

    public override Widget Build(BuildContext context)
    {
        SyncStates();
        Widget result = CurrentWidget.Builder(context, this);
        Action? onTap = Enabled && _registry is not null ? HandleTap : null;

        if (Enabled)
        {
            result = new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: onTap,
                child: result);
            result = new Listener(
                behavior: HitTestBehavior.Opaque,
                onPointerDown: HandlePointerDown,
                onPointerUp: HandlePointerUp,
                onPointerCancel: HandlePointerCancel,
                onPointerEnter: _ => SetHovered(true),
                onPointerExit: _ => SetHovered(false),
                child: result);
        }

        result = new Focus(
            focusNode: FocusNode,
            autofocus: CurrentWidget.Autofocus,
            canRequestFocus: Enabled,
            child: result);

        SemanticsFlags flags = SemanticsFlags.IsInMutuallyExclusiveGroup;
        if (Enabled)
        {
            flags |= SemanticsFlags.IsEnabled;
        }

        return new Semantics(
            child: result,
            flags: flags,
            onTap: onTap,
            @checked: Selected);
    }

    public override void Dispose()
    {
        ReleaseMouseCursor();
        DetachFocusNode(FocusNode);
        SetRegistry(null);

        if (_positionController is not null)
        {
            _positionController.Changed -= HandleAnimationChanged;
            _positionController.Dispose();
            _positionController = null;
        }

        if (_reactionController is not null)
        {
            _reactionController.Changed -= HandleAnimationChanged;
            _reactionController.Dispose();
            _reactionController = null;
        }
    }

    public void AnimateToValue()
    {
        if (_positionController is null)
        {
            return;
        }

        if (Selected)
        {
            _positionController.Forward();
        }
        else
        {
            _positionController.Reverse();
        }
    }

    private void SetRegistry(RadioGroupRegistry<T>? registry)
    {
        if (ReferenceEquals(_registry, registry))
        {
            return;
        }

        _registry?.UnregisterClient(this);
        _registry = registry;
        _registry?.RegisterClient(this);
    }

    private void AttachFocusNode()
    {
        FocusNode.AddListener(HandleFocusChanged);
        _isFocused = FocusNode.HasFocus;
    }

    private void DetachFocusNode(FocusNode focusNode)
    {
        focusNode.RemoveListener(HandleFocusChanged);
    }

    private void HandleTap()
    {
        if (!Enabled || _registry is null)
        {
            return;
        }

        if (!Selected)
        {
            _registry.OnChanged(CurrentWidget.Value);
            return;
        }

        if (CurrentWidget.Toggleable)
        {
            _registry.OnChanged(default);
        }
    }

    private void HandlePointerDown(PointerDownEvent pointerEvent)
    {
        if (!Enabled)
        {
            return;
        }

        SetPressed(true);
        _reactionController?.Forward();
    }

    private void HandlePointerUp(PointerUpEvent pointerEvent)
    {
        SetPressed(false);
        _reactionController?.Reverse();
    }

    private void HandlePointerCancel(PointerCancelEvent pointerEvent)
    {
        SetPressed(false);
        _reactionController?.Reverse();
    }

    private void SetPressed(bool value)
    {
        if (_isPressed == value)
        {
            return;
        }

        SetState(() => _isPressed = value);
        SyncStates();
    }

    private void SetHovered(bool value)
    {
        if (_isHovered == value)
        {
            return;
        }

        SetState(() => _isHovered = value);
        SyncStates();
        if (value)
        {
            UpdateMouseCursor();
        }
        else
        {
            ReleaseMouseCursor();
        }
    }

    private void HandleFocusChanged()
    {
        bool focused = FocusNode.HasFocus;
        if (_isFocused == focused)
        {
            return;
        }

        SetState(() => _isFocused = focused);
        SyncStates();
        if (_isHovered)
        {
            UpdateMouseCursor();
        }
    }

    private void HandleAnimationChanged()
    {
        SetState(static () => { });
    }

    private void SyncStates()
    {
        _states.Clear();
        if (!Enabled)
        {
            _states.Add(WidgetState.Disabled);
        }

        if (Selected)
        {
            _states.Add(WidgetState.Selected);
        }

        if (_isHovered)
        {
            _states.Add(WidgetState.Hovered);
        }

        if (_isFocused)
        {
            _states.Add(WidgetState.Focused);
        }

        if (_isPressed)
        {
            _states.Add(WidgetState.Pressed);
        }
    }

    private void UpdateMouseCursor()
    {
        ReleaseMouseCursor();
        _mouseCursorHandle = MouseCursorManager.PushCursor(CurrentWidget.MouseCursor.Resolve(_states));
    }

    private void ReleaseMouseCursor()
    {
        _mouseCursorHandle?.Dispose();
        _mouseCursorHandle = null;
    }
}
