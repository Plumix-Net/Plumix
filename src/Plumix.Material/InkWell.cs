using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/ink_well.dart (InkWell subset)

public sealed class InkWell : StatefulWidget
{
    public InkWell(
        Widget? child = null,
        Action? onTap = null,
        Action? onDoubleTap = null,
        Action<PointerDownEvent>? onTapDown = null,
        Action? onTapCancel = null,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        BorderRadius? borderRadius = null,
        FocusNode? focusNode = null,
        MouseCursor? mouseCursor = null,
        bool canRequestFocus = true,
        bool autofocus = false,
        bool enableFeedback = true,
        bool excludeFromSemantics = false,
        Key? key = null) : base(key)
    {
        Child = child;
        OnTap = onTap;
        OnDoubleTap = onDoubleTap;
        OnTapDown = onTapDown;
        OnTapCancel = onTapCancel;
        OnLongPress = onLongPress;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        HighlightColor = highlightColor;
        SplashColor = splashColor;
        BorderRadius = borderRadius;
        FocusNode = focusNode;
        MouseCursor = mouseCursor;
        CanRequestFocus = canRequestFocus;
        Autofocus = autofocus;
        EnableFeedback = enableFeedback;
        ExcludeFromSemantics = excludeFromSemantics;
    }

    public Widget? Child { get; }
    public Action? OnTap { get; }
    public Action? OnDoubleTap { get; }
    public Action<PointerDownEvent>? OnTapDown { get; }
    public Action? OnTapCancel { get; }
    public Action? OnLongPress { get; }
    public Action<bool>? OnHover { get; }
    public Action<bool>? OnFocusChange { get; }
    public Color? FocusColor { get; }
    public Color? HoverColor { get; }
    public Color? HighlightColor { get; }
    public Color? SplashColor { get; }
    public BorderRadius? BorderRadius { get; }
    public FocusNode? FocusNode { get; }
    public MouseCursor? MouseCursor { get; }
    public bool CanRequestFocus { get; }
    public bool Autofocus { get; }
    public bool EnableFeedback { get; }
    public bool ExcludeFromSemantics { get; }

    public override State CreateState() => new InkWellState();

    private sealed class InkWellState : State
    {
        private static readonly Point CenterOrigin = new(double.NaN, double.NaN);
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private bool _pressed;
        private bool _hovered;
        private bool _focused;
        private Point _splashOrigin = CenterOrigin;
        private double _splashProgress;
        private Plumix.AnimationController? _splashController;
        private IDisposable? _cursorHandle;

        private InkWell CurrentWidget => (InkWell)StateWidget;
        private bool Enabled => CurrentWidget.OnTap is not null || CurrentWidget.OnDoubleTap is not null || CurrentWidget.OnLongPress is not null;

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
            _splashController = new Plumix.AnimationController(TimeSpan.FromMilliseconds(225))
            {
                Curve = Curves.EaseOut,
            };
            _splashController.Changed += HandleSplashChanged;
            _splashController.Completed += HandleSplashCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldInkWell = (InkWell)oldWidget;
            if (!ReferenceEquals(oldInkWell.FocusNode, CurrentWidget.FocusNode))
            {
                DetachFocusNode();
                AttachFocusNode(CurrentWidget.FocusNode);
            }

            if (!Enabled)
            {
                _pressed = false;
                _hovered = false;
                ReleaseCursor();
            }
        }

        public override void Dispose()
        {
            ReleaseCursor();
            DetachFocusNode();
            if (_splashController is not null)
            {
                _splashController.Changed -= HandleSplashChanged;
                _splashController.Completed -= HandleSplashCompleted;
                _splashController.Dispose();
                _splashController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            var interactionColor = ResolveInteractionColor(theme);
            var radius = widget.BorderRadius ?? Plumix.Rendering.BorderRadius.Zero;

            Widget result = widget.Child ?? new SizedBox();
            if (interactionColor.HasValue)
            {
                result = new DecoratedBox(
                    new BoxDecoration(Color: interactionColor, BorderRadius: radius),
                    result);
            }

            result = new InkSplash(
                splashColor: widget.SplashColor ?? ApplyOpacity(theme.OnSurfaceColor, 0.12),
                splashOrigin: _splashOrigin,
                splashProgress: _splashProgress,
                child: result);
            if (radius != Plumix.Rendering.BorderRadius.Zero)
            {
                result = new ClipRRect(radius, result);
            }

            Action? tap = Enabled && widget.OnTap is not null ? HandleTap : null;
            Action? longPress = Enabled && widget.OnLongPress is not null ? HandleLongPress : null;
            if (Enabled)
            {
                result = new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: tap,
                    onDoubleTap: widget.OnDoubleTap,
                    onTapDown: widget.OnTapDown,
                    onTapCancel: widget.OnTapCancel,
                    onLongPress: longPress,
                    child: result);
                result = new Listener(
                    behavior: HitTestBehavior.Opaque,
                    onPointerDown: HandlePointerDown,
                    onPointerUp: _ => SetPressed(false),
                    onPointerCancel: _ => SetPressed(false),
                    onPointerEnter: _ => SetHovered(true),
                    onPointerExit: _ => SetHovered(false),
                    child: result);
                result = new Focus(
                    focusNode: _focusNode,
                    autofocus: widget.Autofocus,
                    canRequestFocus: widget.CanRequestFocus,
                    onKeyEvent: HandleKeyEvent,
                    child: result);
            }

            if (!widget.ExcludeFromSemantics)
            {
                result = new Semantics(
                    flags: Enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None,
                    onTap: tap,
                    child: result);
            }

            return result;
        }

        private Color? ResolveInteractionColor(ThemeData theme)
        {
            if (_pressed) return CurrentWidget.HighlightColor ?? ApplyOpacity(theme.OnSurfaceColor, 0.12);
            if (_hovered) return CurrentWidget.HoverColor ?? ApplyOpacity(theme.OnSurfaceColor, 0.08);
            if (_focused) return CurrentWidget.FocusColor ?? ApplyOpacity(theme.OnSurfaceColor, 0.10);
            return null;
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            SetState(() =>
            {
                _pressed = true;
                _splashOrigin = @event.LocalPosition;
                _splashProgress = 0;
            });
            _splashController?.Forward(0);
        }

        private void HandleTap()
        {
            SetPressed(false);
            if (CurrentWidget.EnableFeedback) Feedback.ForTap();
            CurrentWidget.OnTap?.Invoke();
        }

        private void HandleLongPress()
        {
            SetPressed(false);
            if (CurrentWidget.EnableFeedback) Feedback.ForLongPress();
            CurrentWidget.OnLongPress?.Invoke();
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!IsActivateKey(@event)) return KeyEventResult.Ignored;
            if (@event.IsDown && CurrentWidget.OnTap is not null)
            {
                SetState(() =>
                {
                    _splashOrigin = CenterOrigin;
                    _splashProgress = 0;
                });
                _splashController?.Forward(0);
                HandleTap();
            }
            return KeyEventResult.Handled;
        }

        private void AttachFocusNode(FocusNode? externalNode)
        {
            _focusNode = externalNode ?? new FocusNode();
            _ownsFocusNode = externalNode is null;
            _focusNode.AddListener(HandleFocusChanged);
            _focused = _focusNode.HasFocus;
        }

        private void DetachFocusNode()
        {
            if (_focusNode is null) return;
            _focusNode.RemoveListener(HandleFocusChanged);
            if (_ownsFocusNode) _focusNode.Dispose();
            _focusNode = null;
            _ownsFocusNode = false;
            _focused = false;
        }

        private void HandleFocusChanged()
        {
            var focused = _focusNode?.HasFocus ?? false;
            if (_focused == focused) return;
            SetState(() => _focused = focused);
            CurrentWidget.OnFocusChange?.Invoke(focused);
        }

        private void SetPressed(bool value)
        {
            if (_pressed == value) return;
            SetState(() => _pressed = value);
        }

        private void SetHovered(bool value)
        {
            if (_hovered == value) return;
            SetState(() => _hovered = value);
            if (value)
            {
                ReleaseCursor();
                _cursorHandle = MouseCursorManager.PushCursor(CurrentWidget.MouseCursor ?? SystemMouseCursors.Click);
            }
            else
            {
                ReleaseCursor();
            }
            CurrentWidget.OnHover?.Invoke(value);
        }

        private void HandleSplashChanged()
        {
            if (_splashController is null) return;
            SetState(() => _splashProgress = _splashController.Evaluate());
        }

        private void HandleSplashCompleted()
        {
            SetState(() =>
            {
                _splashProgress = 0;
                _splashOrigin = CenterOrigin;
            });
        }

        private void ReleaseCursor()
        {
            _cursorHandle?.Dispose();
            _cursorHandle = null;
        }

        private static bool IsActivateKey(KeyEvent @event)
        {
            if (@event.IsShiftPressed || @event.IsControlPressed || @event.IsAltPressed || @event.IsMetaPressed)
            {
                return false;
            }

            return string.Equals(@event.Key, "Enter", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "Return", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "NumPadEnter", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "NumpadEnter", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "Space", StringComparison.Ordinal)
                   || string.Equals(@event.Key, "Spacebar", StringComparison.Ordinal);
        }

        private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
            (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
            color.R,
            color.G,
            color.B);
    }
}
