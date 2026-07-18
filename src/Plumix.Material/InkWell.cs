using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/ink_well.dart

public class InkResponse : StatefulWidget
{
    public InkResponse(
        Widget? child = null,
        Action? onTap = null,
        Action<PointerDownEvent>? onTapDown = null,
        Action<PointerUpEvent>? onTapUp = null,
        Action? onTapCancel = null,
        Action? onDoubleTap = null,
        Action? onLongPress = null,
        Action? onLongPressUp = null,
        Action? onSecondaryTap = null,
        Action<PointerUpEvent>? onSecondaryTapUp = null,
        Action<PointerDownEvent>? onSecondaryTapDown = null,
        Action? onSecondaryTapCancel = null,
        Action<bool>? onHighlightChanged = null,
        Action<bool>? onHover = null,
        MouseCursor? mouseCursor = null,
        bool containedInkWell = false,
        BoxShape highlightShape = BoxShape.Circle,
        double? radius = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? customBorder = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? splashColor = null,
        bool enableFeedback = true,
        bool excludeFromSemantics = false,
        FocusNode? focusNode = null,
        bool canRequestFocus = true,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        MaterialStatesController? statesController = null,
        TimeSpan? hoverDuration = null,
        Key? key = null,
        InteractiveInkFeatureFactory? splashFactory = null) : base(key)
    {
        if (radius.HasValue && (!double.IsFinite(radius.Value) || radius.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Ink radius must be finite and greater than zero.");
        }

        if (hoverDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(hoverDuration));
        }

        Child = child;
        OnTap = onTap;
        OnTapDown = onTapDown;
        OnTapUp = onTapUp;
        OnTapCancel = onTapCancel;
        OnDoubleTap = onDoubleTap;
        OnLongPress = onLongPress;
        OnLongPressUp = onLongPressUp;
        OnSecondaryTap = onSecondaryTap;
        OnSecondaryTapUp = onSecondaryTapUp;
        OnSecondaryTapDown = onSecondaryTapDown;
        OnSecondaryTapCancel = onSecondaryTapCancel;
        OnHighlightChanged = onHighlightChanged;
        OnHover = onHover;
        MouseCursor = mouseCursor;
        ContainedInkWell = containedInkWell;
        HighlightShape = highlightShape;
        Radius = radius;
        BorderRadius = borderRadius;
        CustomBorder = customBorder;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        HighlightColor = highlightColor;
        OverlayColor = overlayColor;
        SplashColor = splashColor;
        EnableFeedback = enableFeedback;
        ExcludeFromSemantics = excludeFromSemantics;
        FocusNode = focusNode;
        CanRequestFocus = canRequestFocus;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        StatesController = statesController;
        HoverDuration = hoverDuration;
        SplashFactory = splashFactory;
    }

    public Widget? Child { get; }
    public Action? OnTap { get; }
    public Action<PointerDownEvent>? OnTapDown { get; }
    public Action<PointerUpEvent>? OnTapUp { get; }
    public Action? OnTapCancel { get; }
    public Action? OnDoubleTap { get; }
    public Action? OnLongPress { get; }
    public Action? OnLongPressUp { get; }
    public Action? OnSecondaryTap { get; }
    public Action<PointerUpEvent>? OnSecondaryTapUp { get; }
    public Action<PointerDownEvent>? OnSecondaryTapDown { get; }
    public Action? OnSecondaryTapCancel { get; }
    public Action<bool>? OnHighlightChanged { get; }
    public Action<bool>? OnHover { get; }
    public MouseCursor? MouseCursor { get; }
    public bool ContainedInkWell { get; }
    public BoxShape HighlightShape { get; }
    public double? Radius { get; }
    public BorderRadius? BorderRadius { get; }
    public ShapeBorder? CustomBorder { get; }
    public Color? FocusColor { get; }
    public Color? HoverColor { get; }
    public Color? HighlightColor { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public Color? SplashColor { get; }
    public bool EnableFeedback { get; }
    public bool ExcludeFromSemantics { get; }
    public FocusNode? FocusNode { get; }
    public bool CanRequestFocus { get; }
    public Action<bool>? OnFocusChange { get; }
    public bool Autofocus { get; }
    public MaterialStatesController? StatesController { get; }
    public TimeSpan? HoverDuration { get; }
    public InteractiveInkFeatureFactory? SplashFactory { get; }

    public override State CreateState() => new InkResponseState();

    private sealed class InkResponseState : State
    {
        private static readonly Point CenterOrigin = new(double.NaN, double.NaN);
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private MaterialStatesController? _statesController;
        private bool _ownsStatesController;
        private bool _pressed;
        private bool _hovered;
        private bool _focused;
        private Point _splashOrigin = CenterOrigin;
        private double _splashProgress;
        private Plumix.AnimationController? _splashController;
        private InteractiveInkFeatureFactory? _resolvedSplashFactory;
        private InteractiveInkFeature? _splashFeature;
        private TextDirection _textDirection = TextDirection.Ltr;
        private Color _resolvedSplashColor;
        private bool _splashConfirmed;
        private bool _splashCanceled;
        private IDisposable? _cursorHandle;

        private InkResponse CurrentWidget => (InkResponse)StateWidget;
        private bool PrimaryEnabled => CurrentWidget.OnTap is not null
                                       || CurrentWidget.OnDoubleTap is not null
                                       || CurrentWidget.OnLongPress is not null
                                       || CurrentWidget.OnLongPressUp is not null
                                       || CurrentWidget.OnTapDown is not null
                                       || CurrentWidget.OnTapUp is not null;
        private bool SecondaryEnabled => CurrentWidget.OnSecondaryTap is not null
                                         || CurrentWidget.OnSecondaryTapDown is not null
                                         || CurrentWidget.OnSecondaryTapUp is not null;
        private bool Enabled => PrimaryEnabled || SecondaryEnabled;

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
            AttachStatesController(CurrentWidget.StatesController);
            _splashController = new Plumix.AnimationController(TimeSpan.FromMilliseconds(225))
            {
                Curve = Curves.Linear,
            };
            _splashController.Changed += HandleSplashChanged;
            _splashController.Completed += HandleSplashCompleted;
            SyncDisabledState();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldResponse = (InkResponse)oldWidget;
            if (!ReferenceEquals(oldResponse.FocusNode, CurrentWidget.FocusNode))
            {
                DetachFocusNode();
                AttachFocusNode(CurrentWidget.FocusNode);
            }

            if (!ReferenceEquals(oldResponse.StatesController, CurrentWidget.StatesController))
            {
                DetachStatesController();
                AttachStatesController(CurrentWidget.StatesController);
            }

            SyncDisabledState();
            if (!Enabled)
            {
                SetPressed(false, notifyCancel: true);
                SetHovered(false, notify: false);
                ReleaseCursor();
            }
        }

        public override void Dispose()
        {
            ReleaseCursor();
            DetachFocusNode();
            DetachStatesController();
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
            _resolvedSplashFactory = widget.SplashFactory ?? theme.SplashFactory;
            _textDirection = Directionality.Of(context);
            var states = _statesController?.Value ?? MaterialState.None;
            var highlightColor = ResolveHighlightColor(theme, states);
            var splashColor = widget.OverlayColor?.Resolve(states | MaterialState.Pressed)
                              ?? widget.SplashColor
                              ?? theme.SplashColor;
            _resolvedSplashColor = splashColor;
            var borderRadius = widget.CustomBorder?.BorderRadius
                               ?? widget.BorderRadius
                               ?? Plumix.Rendering.BorderRadius.Zero;

            Widget result = new InkResponsePaint(
                highlightColor: highlightColor,
                highlightShape: widget.HighlightShape,
                borderRadius: borderRadius,
                splashColor: splashColor,
                splashOrigin: _splashOrigin,
                splashProgress: _splashProgress,
                splashRadius: widget.Radius,
                containedInkWell: widget.ContainedInkWell,
                splashFeature: _splashFeature,
                splashConfirmed: _splashConfirmed,
                splashCanceled: _splashCanceled,
                child: widget.Child ?? new SizedBox());

            if (Enabled)
            {
                result = new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTapDown: PrimaryEnabled ? HandleTapDown : null,
                    onTapUp: PrimaryEnabled ? HandleTapUp : null,
                    onTap: PrimaryEnabled ? HandleTap : null,
                    onTapCancel: PrimaryEnabled ? HandleTapCancel : null,
                    onDoubleTap: widget.OnDoubleTap is null ? null : HandleDoubleTap,
                    onLongPress: widget.OnLongPress is null ? null : HandleLongPress,
                    onLongPressUp: widget.OnLongPressUp is null ? null : HandleLongPressUp,
                    onSecondaryTapDown: SecondaryEnabled ? HandleSecondaryTapDown : null,
                    onSecondaryTapUp: SecondaryEnabled ? HandleSecondaryTapUp : null,
                    onSecondaryTap: SecondaryEnabled ? HandleSecondaryTap : null,
                    onSecondaryTapCancel: SecondaryEnabled ? HandleSecondaryTapCancel : null,
                    child: result);

                result = new Listener(
                    behavior: HitTestBehavior.Opaque,
                    onPointerEnter: _ => SetHovered(true),
                    onPointerExit: _ => SetHovered(false),
                    child: result);
            }

            result = new Focus(
                focusNode: _focusNode,
                autofocus: widget.Autofocus,
                canRequestFocus: Enabled && widget.CanRequestFocus,
                onKeyEvent: HandleKeyEvent,
                child: result);

            if (!widget.ExcludeFromSemantics)
            {
                result = new Semantics(
                    flags: Enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None,
                    onTap: widget.OnTap is null ? null : HandleSemanticTap,
                    onLongPress: widget.OnLongPress is null ? null : HandleSemanticLongPress,
                    child: result);
            }

            return result;
        }

        private Color? ResolveHighlightColor(ThemeData theme, MaterialState states)
        {
            if (_pressed)
            {
                return CurrentWidget.OverlayColor?.Resolve(states | MaterialState.Pressed)
                       ?? CurrentWidget.HighlightColor
                       ?? theme.HighlightColor;
            }

            if (_hovered)
            {
                return CurrentWidget.OverlayColor?.Resolve((states & ~MaterialState.Focused) | MaterialState.Hovered)
                       ?? CurrentWidget.HoverColor
                       ?? theme.HoverColor;
            }

            if (_focused)
            {
                return CurrentWidget.OverlayColor?.Resolve((states & ~MaterialState.Hovered) | MaterialState.Focused)
                       ?? CurrentWidget.FocusColor
                       ?? theme.FocusColor;
            }

            return null;
        }

        private void HandleTapDown(PointerDownEvent details)
        {
            StartSplash(details.LocalPosition);
            CurrentWidget.OnTapDown?.Invoke(details);
        }

        private void HandleTapUp(PointerUpEvent details) => CurrentWidget.OnTapUp?.Invoke(details);

        private void HandleTap()
        {
            ConfirmSplash();
            SetPressed(false);
            if (CurrentWidget.OnTap is not null && CurrentWidget.EnableFeedback) Feedback.ForTap();
            CurrentWidget.OnTap?.Invoke();
        }

        private void HandleTapCancel()
        {
            CancelSplash();
            SetPressed(false);
            CurrentWidget.OnTapCancel?.Invoke();
        }

        private void HandleDoubleTap()
        {
            ConfirmSplash();
            SetPressed(false);
            CurrentWidget.OnDoubleTap?.Invoke();
        }

        private void HandleLongPress()
        {
            ConfirmSplash();
            if (CurrentWidget.OnLongPress is not null && CurrentWidget.EnableFeedback) Feedback.ForLongPress();
            CurrentWidget.OnLongPress?.Invoke();
        }

        private void HandleLongPressUp()
        {
            SetPressed(false);
            CurrentWidget.OnLongPressUp?.Invoke();
        }

        private void HandleSecondaryTapDown(PointerDownEvent details)
        {
            StartSplash(details.LocalPosition);
            CurrentWidget.OnSecondaryTapDown?.Invoke(details);
        }

        private void HandleSecondaryTapUp(PointerUpEvent details) => CurrentWidget.OnSecondaryTapUp?.Invoke(details);

        private void HandleSecondaryTap()
        {
            ConfirmSplash();
            SetPressed(false);
            CurrentWidget.OnSecondaryTap?.Invoke();
        }

        private void HandleSecondaryTapCancel()
        {
            CancelSplash();
            SetPressed(false);
            CurrentWidget.OnSecondaryTapCancel?.Invoke();
        }

        private void HandleSemanticTap()
        {
            StartSplash(CenterOrigin);
            HandleTap();
        }

        private void HandleSemanticLongPress()
        {
            StartSplash(CenterOrigin);
            HandleLongPress();
            SetPressed(false);
        }

        private void StartSplash(Point origin)
        {
            var widget = CurrentWidget;
            var configuration = new InkFeatureConfiguration(
                Position: origin,
                Color: _resolvedSplashColor,
                TextDirection: _textDirection,
                ContainedInkWell: widget.ContainedInkWell,
                BorderRadius: widget.BorderRadius,
                CustomBorder: widget.CustomBorder,
                Radius: widget.Radius);
            InteractiveInkFeatureFactory factory = _resolvedSplashFactory ?? InkSplash.SplashFactory;
            InteractiveInkFeature feature = factory.Create(configuration);
            SetState(() =>
            {
                _splashOrigin = origin;
                _splashProgress = 0;
                _splashFeature = feature;
                _splashConfirmed = false;
                _splashCanceled = false;
            });
            SetPressed(true);
            if (_splashController is not null)
            {
                _splashController.Duration = feature.UnconfirmedDuration;
            }
            _splashController?.Forward(0);
        }

        private void ConfirmSplash()
        {
            if (_splashFeature is null || _splashController is null || _splashCanceled)
            {
                return;
            }

            _splashConfirmed = true;
            _splashController.Duration = _splashFeature.ConfirmDuration;
            _splashController.Forward();
        }

        private void CancelSplash()
        {
            if (_splashFeature is null || _splashController is null)
            {
                return;
            }

            _splashCanceled = true;
            _splashController.Duration = _splashFeature.CancelDuration;
            _splashController.Forward();
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!IsActivateKey(@event)) return KeyEventResult.Ignored;
            if (@event.IsDown && CurrentWidget.OnTap is not null)
            {
                StartSplash(CenterOrigin);
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
        }

        private void HandleFocusChanged()
        {
            bool focused = _focusNode?.HasFocus ?? false;
            if (_focused == focused) return;
            SetState(() => _focused = focused);
            _statesController?.Update(MaterialState.Focused, focused);
            CurrentWidget.OnFocusChange?.Invoke(focused);
        }

        private void AttachStatesController(MaterialStatesController? externalController)
        {
            _statesController = externalController ?? new MaterialStatesController();
            _ownsStatesController = externalController is null;
            _statesController.AddListener(HandleStatesChanged);
        }

        private void DetachStatesController()
        {
            if (_statesController is null) return;
            _statesController.RemoveListener(HandleStatesChanged);
            if (_ownsStatesController) _statesController.Dispose();
            _statesController = null;
            _ownsStatesController = false;
        }

        private void HandleStatesChanged() => SetState(() => { });

        private void SyncDisabledState() => _statesController?.Update(MaterialState.Disabled, !Enabled);

        private void SetPressed(bool value, bool notifyCancel = false)
        {
            if (_pressed == value) return;
            SetState(() => _pressed = value);
            _statesController?.Update(MaterialState.Pressed, value);
            CurrentWidget.OnHighlightChanged?.Invoke(value);
            if (!value && notifyCancel) CurrentWidget.OnTapCancel?.Invoke();
        }

        private void SetHovered(bool value, bool notify = true)
        {
            if (_hovered == value) return;
            SetState(() => _hovered = value);
            _statesController?.Update(MaterialState.Hovered, value);
            if (value)
            {
                ReleaseCursor();
                var cursor = CurrentWidget.MouseCursor ?? (Enabled ? SystemMouseCursors.Click : SystemMouseCursors.Basic);
                _cursorHandle = MouseCursorManager.PushCursor(cursor);
            }
            else
            {
                ReleaseCursor();
            }
            if (notify) CurrentWidget.OnHover?.Invoke(value);
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
                _splashFeature = null;
                _splashConfirmed = false;
                _splashCanceled = false;
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
    }
}

public sealed class InkWell : InkResponse
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
        Key? key = null,
        Action? onLongPressUp = null,
        Action<PointerUpEvent>? onTapUp = null,
        Action? onSecondaryTap = null,
        Action<PointerUpEvent>? onSecondaryTapUp = null,
        Action<PointerDownEvent>? onSecondaryTapDown = null,
        Action? onSecondaryTapCancel = null,
        Action<bool>? onHighlightChanged = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? radius = null,
        ShapeBorder? customBorder = null,
        MaterialStatesController? statesController = null,
        TimeSpan? hoverDuration = null,
        InteractiveInkFeatureFactory? splashFactory = null)
        : base(
            child: child,
            onTap: onTap,
            onTapDown: onTapDown,
            onTapUp: onTapUp,
            onTapCancel: onTapCancel,
            onDoubleTap: onDoubleTap,
            onLongPress: onLongPress,
            onLongPressUp: onLongPressUp,
            onSecondaryTap: onSecondaryTap,
            onSecondaryTapUp: onSecondaryTapUp,
            onSecondaryTapDown: onSecondaryTapDown,
            onSecondaryTapCancel: onSecondaryTapCancel,
            onHighlightChanged: onHighlightChanged,
            onHover: onHover,
            mouseCursor: mouseCursor,
            containedInkWell: true,
            highlightShape: BoxShape.Rectangle,
            radius: radius,
            borderRadius: borderRadius,
            customBorder: customBorder,
            focusColor: focusColor,
            hoverColor: hoverColor,
            highlightColor: highlightColor,
            overlayColor: overlayColor,
            splashColor: splashColor,
            enableFeedback: enableFeedback,
            excludeFromSemantics: excludeFromSemantics,
            focusNode: focusNode,
            canRequestFocus: canRequestFocus,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            statesController: statesController,
            hoverDuration: hoverDuration,
            splashFactory: splashFactory,
            key: key)
    {
    }
}

internal sealed class InkResponsePaint : SingleChildRenderObjectWidget
{
    public InkResponsePaint(
        Color? highlightColor,
        BoxShape highlightShape,
        BorderRadius borderRadius,
        Color? splashColor,
        Point splashOrigin,
        double splashProgress,
        double? splashRadius,
        bool containedInkWell,
        InteractiveInkFeature? splashFeature,
        bool splashConfirmed,
        bool splashCanceled,
        Widget child) : base(child)
    {
        HighlightColor = highlightColor;
        HighlightShape = highlightShape;
        BorderRadius = borderRadius;
        SplashColor = splashColor;
        SplashOrigin = splashOrigin;
        SplashProgress = splashProgress;
        SplashRadius = splashRadius;
        ContainedInkWell = containedInkWell;
        SplashFeature = splashFeature;
        SplashConfirmed = splashConfirmed;
        SplashCanceled = splashCanceled;
    }

    public Color? HighlightColor { get; }
    public BoxShape HighlightShape { get; }
    public BorderRadius BorderRadius { get; }
    public Color? SplashColor { get; }
    public Point SplashOrigin { get; }
    public double SplashProgress { get; }
    public double? SplashRadius { get; }
    public bool ContainedInkWell { get; }
    public InteractiveInkFeature? SplashFeature { get; }
    public bool SplashConfirmed { get; }
    public bool SplashCanceled { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderInkResponsePaint(
        HighlightColor, HighlightShape, BorderRadius, SplashColor, SplashOrigin, SplashProgress,
        SplashRadius, ContainedInkWell, SplashFeature, SplashConfirmed, SplashCanceled);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var paint = (RenderInkResponsePaint)renderObject;
        paint.HighlightColor = HighlightColor;
        paint.HighlightShape = HighlightShape;
        paint.BorderRadius = BorderRadius;
        paint.SplashColor = SplashColor;
        paint.SplashOrigin = SplashOrigin;
        paint.SplashProgress = SplashProgress;
        paint.SplashRadius = SplashRadius;
        paint.ContainedInkWell = ContainedInkWell;
        paint.SplashFeature = SplashFeature;
        paint.SplashConfirmed = SplashConfirmed;
        paint.SplashCanceled = SplashCanceled;
    }
}

internal sealed class RenderInkResponsePaint : RenderProxyBox
{
    private Color? _highlightColor;
    private BoxShape _highlightShape;
    private BorderRadius _borderRadius;
    private Color? _splashColor;
    private Point _splashOrigin;
    private double _splashProgress;
    private double? _splashRadius;
    private bool _containedInkWell;
    private InteractiveInkFeature? _splashFeature;
    private bool _splashConfirmed;
    private bool _splashCanceled;

    public RenderInkResponsePaint(Color? highlightColor, BoxShape highlightShape, BorderRadius borderRadius,
        Color? splashColor,
        Point splashOrigin,
        double splashProgress,
        double? splashRadius,
        bool containedInkWell,
        InteractiveInkFeature? splashFeature,
        bool splashConfirmed,
        bool splashCanceled)
    {
        _highlightColor = highlightColor;
        _highlightShape = highlightShape;
        _borderRadius = borderRadius;
        _splashColor = splashColor;
        _splashOrigin = splashOrigin;
        _splashProgress = Math.Clamp(splashProgress, 0, 1);
        _splashRadius = splashRadius;
        _containedInkWell = containedInkWell;
        _splashFeature = splashFeature;
        _splashConfirmed = splashConfirmed;
        _splashCanceled = splashCanceled;
    }

    public Color? HighlightColor { get => _highlightColor; set => SetPaintValue(ref _highlightColor, value); }
    public BoxShape HighlightShape { get => _highlightShape; set => SetPaintValue(ref _highlightShape, value); }
    public BorderRadius BorderRadius { get => _borderRadius; set => SetPaintValue(ref _borderRadius, value); }
    public Color? SplashColor { get => _splashColor; set => SetPaintValue(ref _splashColor, value); }
    public Point SplashOrigin { get => _splashOrigin; set => SetPaintValue(ref _splashOrigin, value); }
    public double SplashProgress { get => _splashProgress; set => SetPaintValue(ref _splashProgress, Math.Clamp(value, 0, 1)); }
    public double? SplashRadius { get => _splashRadius; set => SetPaintValue(ref _splashRadius, value); }
    public bool ContainedInkWell { get => _containedInkWell; set => SetPaintValue(ref _containedInkWell, value); }
    public InteractiveInkFeature? SplashFeature
    {
        get => _splashFeature;
        set => SetPaintValue(ref _splashFeature, value);
    }
    public bool SplashConfirmed { get => _splashConfirmed; set => SetPaintValue(ref _splashConfirmed, value); }
    public bool SplashCanceled { get => _splashCanceled; set => SetPaintValue(ref _splashCanceled, value); }

    public override void Paint(PaintingContext context, Point offset)
    {
        void PaintInk(PaintingContext target)
        {
            if (_highlightColor.HasValue)
            {
                var brush = new SolidColorBrush(_highlightColor.Value);
                if (_highlightShape == BoxShape.Circle)
                {
                    double radius = _splashRadius ?? Math.Min(Size.Width, Size.Height) / 2.0;
                    target.DrawCircle(brush, null, offset + new Point(Size.Width / 2.0, Size.Height / 2.0), radius);
                }
                else
                {
                    target.DrawRectangle(brush, null, new Rect(offset, Size), _borderRadius.Radius, _borderRadius.Radius);
                }
            }

            if (_splashFeature is not null && _splashProgress >= 0.0)
            {
                InkFeatureFrame frame = _splashFeature.ResolveFrame(
                    Size,
                    _splashProgress,
                    confirmed: _splashConfirmed,
                    canceled: _splashCanceled);
                PaintFeature(target, offset, _splashFeature.Configuration.Color, frame);
            }
            else if (_splashColor.HasValue && _splashProgress > 0)
            {
                var center = new Point(Size.Width / 2.0, Size.Height / 2.0);
                var origin = double.IsNaN(_splashOrigin.X) || double.IsNaN(_splashOrigin.Y)
                    ? center
                    : _splashOrigin;
                if (!_containedInkWell)
                {
                    origin = new Point(
                        origin.X + ((center.X - origin.X) * _splashProgress),
                        origin.Y + ((center.Y - origin.Y) * _splashProgress));
                }
                double maxRadius = _splashRadius ?? ResolveSplashRadius(origin);
                target.DrawCircle(new SolidColorBrush(_splashColor.Value), null, offset + origin, maxRadius * _splashProgress);
            }

            base.Paint(target, offset);
        }

        if (_containedInkWell)
        {
            if (_highlightShape == BoxShape.Circle)
            {
                context.PushClipGeometry(new EllipseGeometry(new Rect(offset, Size)), PaintInk);
            }
            else
            {
                context.PushClipRRect(new Rect(offset, Size), _borderRadius, PaintInk);
            }
        }
        else
        {
            PaintInk(context);
        }
    }

    private static void PaintFeature(
        PaintingContext context,
        Point offset,
        Color color,
        InkFeatureFrame frame)
    {
        Color featureColor = ApplyOpacity(color, frame.Opacity);
        if (frame.Kind != InkFeatureKind.Sparkle)
        {
            context.DrawCircle(
                new SolidColorBrush(featureColor),
                null,
                offset + frame.Center,
                frame.Radius);
            return;
        }

        context.DrawCircle(
            new SolidColorBrush(featureColor),
            null,
            offset + frame.Center,
            frame.Radius);
        Color haloColor = ApplyOpacity(color, frame.Opacity * 0.32);
        context.DrawCircle(
            new SolidColorBrush(haloColor),
            null,
            offset + frame.Center + new Vector(frame.Radius * 0.08, -frame.Radius * 0.04),
            frame.Radius * 0.72);

        Random random = new(unchecked((int)Math.Round(frame.TurbulenceSeed * 1000.0)));
        Color sparkleColor = ApplyOpacity(Colors.White, frame.SparkleOpacity);
        var sparkleBrush = new SolidColorBrush(sparkleColor);
        for (int index = 0; index < 18; index++)
        {
            double angle = random.NextDouble() * Math.PI * 2.0;
            double distance = random.NextDouble() * frame.Radius * 0.82;
            double dotRadius = 0.75 + (random.NextDouble() * 1.5);
            var dotCenter = new Point(
                frame.Center.X + (Math.Cos(angle) * distance),
                frame.Center.Y + (Math.Sin(angle) * distance));
            context.DrawCircle(sparkleBrush, null, offset + dotCenter, dotRadius);
        }
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp(
            (int)Math.Round(color.A * Math.Clamp(opacity, 0.0, 1.0)),
            0,
            255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private double ResolveSplashRadius(Point origin)
    {
        if (!_containedInkWell)
        {
            return Math.Sqrt((Size.Width * Size.Width) + (Size.Height * Size.Height)) / 2.0;
        }

        double[] distances = new[]
        {
            Distance(origin, new Point(0, 0)),
            Distance(origin, new Point(Size.Width, 0)),
            Distance(origin, new Point(0, Size.Height)),
            Distance(origin, new Point(Size.Width, Size.Height)),
        };
        return distances.Max();
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private void SetPaintValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        MarkNeedsPaint();
    }
}
