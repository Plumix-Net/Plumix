using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/toggleable.dart

public abstract class ToggleableState : State
{
    private static readonly TimeSpan ToggleDuration = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan ReactionDuration = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ReactionFadeDuration = TimeSpan.FromMilliseconds(50);

    private AnimationController? _positionController;
    private AnimationController? _reactionController;
    private AnimationController? _reactionHoverFadeController;
    private AnimationController? _reactionFocusFadeController;
    private CurvedAnimation? _position;
    private CurvedAnimation? _reaction;
    private CurvedAnimation? _reactionHoverFade;
    private CurvedAnimation? _reactionFocusFade;
    private bool _hovering;
    private bool _focused;
    private Point? _downPosition;
    private Action? _onTap;

    protected abstract bool IsInteractive { get; }

    protected abstract bool IsValueSelected { get; }

    protected Animation<double> Position => _position!;

    protected CurvedAnimation PositionAnimation => _position!;

    protected AnimationController PositionController => _positionController!;

    protected Animation<double> Reaction => _reaction!;

    protected AnimationController ReactionController => _reactionController!;

    protected Animation<double> ReactionHoverFade => _reactionHoverFade!;

    protected Animation<double> ReactionFocusFade => _reactionFocusFade!;

    protected Point? DownPosition => _downPosition;

    protected bool IsFocused => _focused;

    protected bool IsHovered => _hovering;

    protected IReadOnlySet<WidgetState> CurrentWidgetStates
    {
        get
        {
            var states = new HashSet<WidgetState>();
            if (!IsInteractive)
            {
                states.Add(WidgetState.Disabled);
            }
            if (IsValueSelected)
            {
                states.Add(WidgetState.Selected);
            }
            if (_hovering)
            {
                states.Add(WidgetState.Hovered);
            }
            if (_focused)
            {
                states.Add(WidgetState.Focused);
            }
            return states;
        }
    }

    public override void InitState()
    {
        base.InitState();
        _positionController = new AnimationController(duration: ToggleDuration, vsync: this);
        _positionController.SetValue(IsValueSelected ? 1.0 : 0.0);
        _position = new CurvedAnimation(
            _positionController,
            Curves.EaseIn,
            Curves.EaseOut);

        _reactionController = new AnimationController(duration: ReactionDuration, vsync: this);
        _reaction = new CurvedAnimation(_reactionController, Curves.FastOutSlowIn);

        _reactionHoverFadeController = new AnimationController(duration: ReactionFadeDuration, vsync: this);
        _reactionHoverFade = new CurvedAnimation(
            _reactionHoverFadeController,
            Curves.FastOutSlowIn);

        _reactionFocusFadeController = new AnimationController(duration: ReactionFadeDuration, vsync: this);
        _reactionFocusFade = new CurvedAnimation(
            _reactionFocusFadeController,
            Curves.FastOutSlowIn);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (IsInteractive)
        {
            return;
        }

        _downPosition = null;
        _hovering = false;
        _reactionController?.Reverse();
        _reactionHoverFadeController?.Reverse();
    }

    public override void Dispose()
    {
        _position?.Dispose();
        _reaction?.Dispose();
        _reactionHoverFade?.Dispose();
        _reactionFocusFade?.Dispose();
        _positionController?.Dispose();
        _reactionController?.Dispose();
        _reactionHoverFadeController?.Dispose();
        _reactionFocusFadeController?.Dispose();
        _position = null;
        _reaction = null;
        _reactionHoverFade = null;
        _reactionFocusFade = null;
        _positionController = null;
        _reactionController = null;
        _reactionHoverFadeController = null;
        _reactionFocusFadeController = null;
        base.Dispose();
    }

    protected void AnimateToValue(bool? value, bool tristate)
    {
        if (_positionController is null)
        {
            return;
        }

        if (tristate && value is null)
        {
            _positionController.SetValue(0.0);
            _positionController.Forward();
            return;
        }

        if (value == true)
        {
            _positionController.Forward();
        }
        else
        {
            _positionController.Reverse();
        }
    }

    protected Widget BuildToggleable(
        CustomPainter painter,
        Size size,
        MouseCursor mouseCursor,
        Action onTap,
        FocusNode? focusNode,
        bool autofocus)
    {
        Widget result = new CustomPaint(
            painter: painter,
            size: size);

        return BuildToggleableChild(
            child: result,
            mouseCursor: mouseCursor,
            onTap: onTap,
            focusNode: focusNode,
            autofocus: autofocus);
    }

    protected Widget BuildToggleable(
        CustomPainter painter,
        Size size,
        WidgetStateProperty<MouseCursor>? mouseCursor,
        Action onTap,
        FocusNode? focusNode,
        Action<bool>? onFocusChange,
        bool autofocus)
    {
        Widget result = new CustomPaint(
            painter: painter,
            size: size);

        return BuildToggleableChild(
            child: result,
            mouseCursor: mouseCursor,
            onTap: onTap,
            focusNode: focusNode,
            onFocusChange: onFocusChange,
            autofocus: autofocus);
    }

    protected Widget BuildToggleableChild(
        Widget child,
        MouseCursor mouseCursor,
        Action onTap,
        FocusNode? focusNode,
        bool autofocus)
    {
        _onTap = onTap;
        Widget result = child;

        if (IsInteractive)
        {
            result = new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTapDown: HandleTapDown,
                onTap: HandleTap,
                onTapUp: HandleTapUp,
                onTapCancel: HandleTapCancel,
                child: result);
        }

        var shortcuts = new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.Space)] = new ActivateIntent(),
        };
        if (!OperatingSystem.IsBrowser())
        {
            shortcuts[new SingleActivator(LogicalKeyboardKey.Enter)] = new ActivateIntent();
        }
        var actions = new Dictionary<Type, FlutterAction>
        {
            [typeof(ActivateIntent)] = new CallbackAction<ActivateIntent>(_ =>
            {
                HandleTap();
                return null;
            }),
        };

        result = new FocusableActionDetector(
            enabled: IsInteractive,
            focusNode: focusNode,
            autofocus: autofocus,
            shortcuts: shortcuts,
            actions: actions,
            onShowFocusHighlight: HandleFocusHighlightChanged,
            onShowHoverHighlight: HandleHoverChanged,
            mouseCursor: mouseCursor,
            child: result);

        return new Semantics(
            flags: IsInteractive ? SemanticsFlags.IsEnabled : SemanticsFlags.None,
            onTap: IsInteractive ? HandleTap : null,
            child: result);
    }

    protected Widget BuildToggleableChild(
        Widget child,
        WidgetStateProperty<MouseCursor>? mouseCursor,
        Action onTap,
        FocusNode? focusNode,
        Action<bool>? onFocusChange,
        bool autofocus)
    {
        _onTap = onTap;
        Widget result = new Semantics(
            enabled: IsInteractive,
            child: child);
        result = new GestureDetector(
            behavior: HitTestBehavior.Opaque,
            excludeFromSemantics: !IsInteractive,
            onTapDown: IsInteractive ? HandleTapDown : null,
            onTap: IsInteractive ? HandleTap : null,
            onTapUp: IsInteractive ? HandleTapUp : null,
            onTapCancel: IsInteractive ? HandleTapCancel : null,
            child: result);

        var shortcuts = new Dictionary<ShortcutActivator, Intent>
        {
            [new SingleActivator(LogicalKeyboardKey.Space)] = new ActivateIntent(),
        };
        if (!OperatingSystem.IsBrowser())
        {
            shortcuts[new SingleActivator(LogicalKeyboardKey.Enter)] = new ActivateIntent();
        }
        var actions = new Dictionary<Type, FlutterAction>
        {
            [typeof(ActivateIntent)] = new CallbackAction<ActivateIntent>(_ =>
            {
                HandleTap();
                return null;
            }),
        };
        MouseCursor effectiveCursor = mouseCursor?.Resolve(CurrentWidgetStates)
                                      ?? SystemMouseCursors.Basic;

        return new FocusableActionDetector(
            enabled: IsInteractive,
            focusNode: focusNode,
            autofocus: autofocus,
            shortcuts: shortcuts,
            actions: actions,
            onShowFocusHighlight: HandleFocusHighlightChanged,
            onShowHoverHighlight: HandleHoverChanged,
            onFocusChange: onFocusChange,
            mouseCursor: effectiveCursor,
            child: result);
    }

    private void HandleTapDown(PointerDownEvent details)
    {
        SetState(() => _downPosition = details.Position);
        _reactionController?.Forward();
    }

    private void HandleTapUp(PointerUpEvent details)
    {
        _ = details;
        SetState(() => _downPosition = null);
        _reactionController?.Reverse();
    }

    private void HandleTapCancel()
    {
        if (!Mounted)
        {
            return;
        }

        SetState(() => _downPosition = null);
        _reactionController?.Reverse();
    }

    private void HandleTap()
    {
        if (IsInteractive)
        {
            _onTap?.Invoke();
        }
    }

    private void HandleHoverChanged(bool value)
    {
        if (!Mounted || _hovering == value)
        {
            return;
        }

        SetState(() => _hovering = value);
        if (value)
        {
            _reactionHoverFadeController?.Forward();
        }
        else
        {
            _reactionHoverFadeController?.Reverse();
        }
    }

    private void HandleFocusHighlightChanged(bool value)
    {
        if (!Mounted || _focused == value)
        {
            return;
        }

        SetState(() => _focused = value);
        if (value)
        {
            _reactionFocusFadeController?.Forward();
        }
        else
        {
            _reactionFocusFadeController?.Reverse();
        }
    }
}

public abstract class ToggleablePainter : CustomPainter
{
    private readonly MergedListenable _mergedRepaint;

    protected ToggleablePainter(
        Animation<double> position,
        Animation<double> reaction,
        Animation<double> reactionHoverFade,
        Animation<double> reactionFocusFade)
        : this(
            new MergedListenable(
                position,
                reaction,
                reactionHoverFade,
                reactionFocusFade),
            position,
            reaction,
            reactionHoverFade,
            reactionFocusFade)
    {
    }

    private ToggleablePainter(
        MergedListenable repaint,
        Animation<double> position,
        Animation<double> reaction,
        Animation<double> reactionHoverFade,
        Animation<double> reactionFocusFade) : base(repaint)
    {
        _mergedRepaint = repaint;
        Position = position;
        Reaction = reaction;
        ReactionHoverFade = reactionHoverFade;
        ReactionFocusFade = reactionFocusFade;
    }

    protected Animation<double> Position { get; }

    protected Animation<double> Reaction { get; }

    protected Animation<double> ReactionHoverFade { get; }

    protected Animation<double> ReactionFocusFade { get; }

    protected Color ReactionColor { get; set; }

    protected Color HoverColor { get; set; }

    protected Color FocusColor { get; set; }

    protected double SplashRadius { get; set; }

    protected void NotifyPainterChanged()
    {
        _mergedRepaint.NotifyConfigurationChanged();
    }

    protected void PaintRadialReaction(
        PaintingContext context,
        Point origin,
        Color? inactiveReactionColor = null)
    {
        Color color = LerpColor(
            inactiveReactionColor ?? Colors.Transparent,
            ReactionColor,
            Position.Value);
        color = LerpColor(color, HoverColor, ReactionHoverFade.Value);
        color = LerpColor(color, FocusColor, ReactionFocusFade.Value);
        double radius = ReactionHoverFade.Value > 0.0 || ReactionFocusFade.Value > 0.0
            ? SplashRadius
            : SplashRadius * Reaction.Value;
        if (radius <= 0.0 || color.A == 0)
        {
            return;
        }

        context.DrawCircle(new SolidColorBrush(color), null, origin, radius);
    }

    protected static Color LerpColor(Color from, Color to, double t)
    {
        double clampedT = Math.Clamp(t, 0.0, 1.0);
        byte LerpChannel(byte start, byte end) =>
            (byte)Math.Clamp((int)Math.Round(start + ((end - start) * clampedT)), 0, byte.MaxValue);

        return Color.FromArgb(
            LerpChannel(from.A, to.A),
            LerpChannel(from.R, to.R),
            LerpChannel(from.G, to.G),
            LerpChannel(from.B, to.B));
    }

    public override void Dispose()
    {
        _mergedRepaint.Dispose();
        base.Dispose();
    }

    private sealed class MergedListenable(params IListenable[] sources) : ChangeNotifier
    {
        public override void AddListener(Action listener)
        {
            base.AddListener(listener);
            foreach (IListenable source in sources)
            {
                source.AddListener(listener);
            }
        }

        public override void RemoveListener(Action listener)
        {
            foreach (IListenable source in sources)
            {
                source.RemoveListener(listener);
            }
            base.RemoveListener(listener);
        }

        public void NotifyConfigurationChanged()
        {
            NotifyListeners();
        }
    }
}
