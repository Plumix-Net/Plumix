using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Material;

// C#-only infrastructure: `MaterialButtonCore` is the shared button core the Material `Switch` still
// builds on — the last consumer. Flutter has no counterpart: there, `Switch` builds `_MaterialSwitch`
// on `ToggleableStateMixin` + `_SwitchPainter`, and every other control that used to sit here either
// extends `ButtonStyleButton` or composes `Material` + `InkWell` directly.
// See `docs/ai/BACKLOG.md` for the port that removes it.

internal sealed class MaterialButtonCore : StatefulWidget
{
    public MaterialButtonCore(
        Widget child,
        Action? onPressed,
        ButtonStyle style,
        Action? onLongPress = null,
        Action<bool>? onHighlightChanged = null,
        Action<bool>? onHoverChanged = null,
        Action<bool>? onFocusChange = null,
        FocusNode? focusNode = null,
        MaterialStatesController? statesController = null,
        bool isSelected = false,
        bool includeSemanticSelected = true,
        bool isSemanticButton = true,
        bool? isSemanticChecked = null,
        string? semanticLabel = null,
        double? splashRadius = null,
        MouseCursor? mouseCursor = null,
        Clip clipBehavior = Clip.HardEdge,
        bool? enableFeedback = null,
        bool autofocus = false,
        Size? tapTargetMinimumSize = null,
        bool? enabled = null,
        bool? semanticEnabled = null,
        Key? key = null) : base(key)
    {
        Child = child;
        OnPressed = onPressed;
        Style = style ?? throw new ArgumentNullException(nameof(style));
        OnLongPress = onLongPress;
        OnHighlightChanged = onHighlightChanged;
        OnHoverChanged = onHoverChanged;
        OnFocusChange = onFocusChange;
        FocusNode = focusNode;
        StatesController = statesController;
        IsSelected = isSelected;
        IncludeSemanticSelected = includeSemanticSelected;
        IsSemanticButton = isSemanticButton;
        IsSemanticChecked = isSemanticChecked;
        SemanticLabel = semanticLabel;
        SplashRadius = splashRadius;
        MouseCursor = mouseCursor;
        ClipBehavior = clipBehavior;
        EnableFeedback = enableFeedback;
        Autofocus = autofocus;
        TapTargetMinimumSize = tapTargetMinimumSize;
        Enabled = enabled;
        SemanticEnabled = semanticEnabled;
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public ButtonStyle Style { get; }

    public Action? OnLongPress { get; }

    public Action<bool>? OnHighlightChanged { get; }

    public Action<bool>? OnHoverChanged { get; }

    public Action<bool>? OnFocusChange { get; }

    public FocusNode? FocusNode { get; }

    public MaterialStatesController? StatesController { get; }

    public bool IsSelected { get; }

    public bool IncludeSemanticSelected { get; }

    public bool IsSemanticButton { get; }

    public bool? IsSemanticChecked { get; }

    public string? SemanticLabel { get; }

    public double? SplashRadius { get; }

    public MouseCursor? MouseCursor { get; }

    public Clip ClipBehavior { get; }

    public bool? EnableFeedback { get; }

    public bool Autofocus { get; }

    public Size? TapTargetMinimumSize { get; }

    public bool? Enabled { get; }

    public bool? SemanticEnabled { get; }

    public override State CreateState()
    {
        return new MaterialButtonCoreState();
    }

    private sealed class MaterialButtonCoreState : State
    {
        private static readonly Point CenterSplashOrigin = new(double.NaN, double.NaN);

        private bool _isPressed;
        private bool _hasFocus;
        private bool _isHovered;
        private bool _suppressFocusOverlay;
        private bool _isKeyboardPressed;
        private bool _isSplashActive;
        private double _splashProgress;
        private Point _splashOrigin = CenterSplashOrigin;
        private Color? _splashBaseColor;
        private InteractiveInkFeature? _splashFeature;
        private InteractiveInkFeatureFactory? _resolvedSplashFactory;
        private BorderRadius _resolvedBorderRadius = BorderRadius.Zero;
        private TextDirection _resolvedTextDirection = TextDirection.Ltr;
        private bool _splashConfirmed;
        private bool _splashCanceled;
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private AnimationController? _splashController;
        private AnimationController? _keyboardPressController;
        private IDisposable? _mouseCursorHandle;
        private MaterialStatesController? _statesController;

        private MaterialButtonCore CurrentWidget => (MaterialButtonCore)StateWidget;

        private bool Enabled => CurrentWidget.Enabled
                                ?? (CurrentWidget.OnPressed != null || CurrentWidget.OnLongPress != null);

        private bool Interactive => Enabled
                                    && (CurrentWidget.OnPressed != null || CurrentWidget.OnLongPress != null);

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
            AttachStatesController(CurrentWidget.StatesController);

            _splashController = new AnimationController(duration: TimeSpan.FromMilliseconds(225), vsync: this)
            {
                Curve = Curves.Linear
            };
            _splashController.Changed += HandleSplashTick;
            _splashController.Completed += HandleSplashCompleted;

            _keyboardPressController = new AnimationController(duration: TimeSpan.FromMilliseconds(100), vsync: this);
            _keyboardPressController.Completed += HandleKeyboardPressCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldButtonWidget = (MaterialButtonCore)oldWidget;
            bool shouldClearPressedState = !Interactive && (_isPressed || _isKeyboardPressed);
            if (!ReferenceEquals(oldButtonWidget.FocusNode, CurrentWidget.FocusNode))
            {
                DetachFocusNode(disposeOwned: true);
                AttachFocusNode(CurrentWidget.FocusNode);
            }

            if (!ReferenceEquals(oldButtonWidget.StatesController, CurrentWidget.StatesController))
            {
                DetachStatesController();
                AttachStatesController(CurrentWidget.StatesController);
            }

            if (!Interactive && _isPressed)
            {
                _isPressed = false;
            }

            if (!Interactive && _mouseCursorHandle is not null)
            {
                ReleaseMouseCursor();
            }

            if (!Interactive && _suppressFocusOverlay)
            {
                _suppressFocusOverlay = false;
            }

            if (!Interactive && _isSplashActive)
            {
                _isSplashActive = false;
                _splashProgress = 0;
                _splashBaseColor = null;
                _splashController?.Stop();
            }

            if (!Interactive && _isKeyboardPressed)
            {
                _isKeyboardPressed = false;
                _keyboardPressController?.Stop();
            }

            if (shouldClearPressedState)
            {
                _statesController?.Update(MaterialState.Pressed, false);
            }

            if (!Interactive && _focusNode != null && _focusNode.HasFocus)
            {
                _focusNode.Unfocus();
            }

            if (Interactive
                && _isHovered
                && !Equals(oldButtonWidget.MouseCursor, CurrentWidget.MouseCursor))
            {
                UpdateMouseCursor();
            }

            SyncFixedControllerStates();
        }

        public override void Dispose()
        {
            ReleaseMouseCursor();
            DetachFocusNode(disposeOwned: true);
            DetachStatesController();

            if (_splashController != null)
            {
                _splashController.Changed -= HandleSplashTick;
                _splashController.Completed -= HandleSplashCompleted;
                _splashController.Dispose();
                _splashController = null;
            }

            if (_keyboardPressController != null)
            {
                _keyboardPressController.Completed -= HandleKeyboardPressCompleted;
                _keyboardPressController.Dispose();
                _keyboardPressController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            bool enabled = Enabled;
            var style = widget.Style;
            var theme = Theme.Of(context);
            var baseStates = BuildMaterialStates(enabled, includeFocus: true);
            var overlayStates = BuildMaterialStates(enabled, includeFocus: !_suppressFocusOverlay);

            var foreground = ResolveForegroundColor(style, baseStates);
            var iconColor = ResolveIconColor(style, baseStates, foreground);
            double? iconSize = ResolveIconSize(style, baseStates);
            var splashColor = ResolveSplashColor();
            double elevation = ResolveElevation(style, baseStates);
            var shadowColor = ResolveShadowColor(style, baseStates, elevation, theme.ShadowColor);
            var background = ResolveBackgroundColor(
                style,
                baseStates,
                overlayStates,
                elevation,
                theme.UseMaterial3);
            var border = style.ResolveSide(baseStates);
            var padding = style.ResolvePadding(baseStates) ?? default;
            OutlinedBorder resolvedShape = style.ResolveShape(baseStates) ?? new RoundedRectangleBorder();
            var borderRadius = ShapeBorderGeometry.ResolveRadius(resolvedShape);
            _resolvedSplashFactory = style.SplashFactory ?? theme.SplashFactory;
            _resolvedBorderRadius = borderRadius;
            _resolvedTextDirection = Directionality.Of(context);
            var minimumSize = style.ResolveMinimumSize(baseStates) ?? new Size(64, 40);
            ValidateMinimumSize(minimumSize);
            var densityAdjustment = (style.VisualDensity ?? theme.VisualDensity).BaseSizeAdjustment;
            minimumSize = new Size(
                Math.Max(0, minimumSize.Width + densityAdjustment.X),
                Math.Max(0, minimumSize.Height + densityAdjustment.Y));
            var maximumSize = style.ResolveMaximumSize(baseStates)
                              ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
            ValidateMaximumSize(maximumSize);
            var fixedSize = style.ResolveFixedSize(baseStates);
            ValidateFixedSize(fixedSize);
            var effectiveConstraints = CreateEffectiveConstraints(minimumSize, maximumSize, fixedSize);
            Alignment alignment = style.Alignment?.Resolve(Directionality.Of(context)) ?? Alignment.Center;
            var tapTargetSize = style.ResolveTapTargetSize() ?? MaterialTapTargetSize.Padded;
            var resolvedTextStyle = style.ResolveTextStyle(baseStates);
            var baseTextStyle = theme.TextTheme.LabelLarge with
            {
                Color = foreground
            };
            var textStyle = MergeTextStyle(
                baseTextStyle,
                resolvedTextStyle);

            Widget foregroundChild = style.ForegroundBuilder is null
                ? widget.Child
                : style.ForegroundBuilder(context, baseStates, widget.Child);
            Widget content = new Align(
                alignment: alignment,
                widthFactor: 1,
                heightFactor: 1,
                child: foregroundChild);

            content = new Container(
                padding: padding,
                child: content);

            if (style.BackgroundBuilder is not null)
            {
                content = style.BackgroundBuilder(context, baseStates, content);
            }

            var resolvedIconTheme = theme.IconTheme.Merge(
                new IconThemeData(
                    Color: iconColor,
                    Size: iconSize));
            if (style.AnimationDuration is { } styleDuration && styleDuration > TimeSpan.Zero)
            {
                content = new AnimatedIconTheme(
                    data: resolvedIconTheme,
                    duration: styleDuration,
                    child: content);
                content = new AnimatedDefaultTextStyle(
                    style: textStyle,
                    duration: styleDuration,
                    child: content);
            }
            else
            {
                content = new IconTheme(
                    data: resolvedIconTheme,
                    child: content);
                content = new DefaultTextStyle(
                    style: textStyle,
                    child: content);
            }

            content = new ConstrainedBox(
                constraints: effectiveConstraints,
                child: content);

            content = new InkResponsePaint(
                highlightColor: null,
                highlightShape: BoxShape.Rectangle,
                borderRadius: borderRadius,
                splashColor: splashColor,
                splashOrigin: _splashOrigin,
                splashProgress: _splashProgress,
                splashRadius: widget.SplashRadius,
                containedInkWell: true,
                splashFeature: _splashFeature,
                splashConfirmed: _splashConfirmed,
                splashCanceled: _splashCanceled,
                rectCallbackFactory: referenceBox => () => new Rect(referenceBox.Size),
                child: content);

            if (widget.ClipBehavior != Clip.None)
            {
                content = new ClipRRect(
                    borderRadius: borderRadius,
                    child: content);
            }

            var decoration = new BoxDecoration(
                Color: background,
                Border: border is { } borderSide ? Plumix.Rendering.Border.FromBorderSide(borderSide) : null,
                BorderRadius: borderRadius,
                BoxShadows: ResolveBoxShadows(elevation, shadowColor));
            content = style.AnimationDuration is { } animationDuration && animationDuration > TimeSpan.Zero
                ? new AnimatedContainer(
                    duration: animationDuration,
                    decoration: decoration,
                    child: content)
                : new DecoratedBox(
                    decoration: decoration,
                    child: content);

            Widget result = content;
            Action? tapCallback = Enabled && widget.OnPressed is not null ? HandleTap : null;
            Action? longPressCallback = Enabled && widget.OnLongPress is not null ? HandleLongPress : null;

            if (Interactive)
            {
                result = new GestureDetector(
                   excludeFromSemantics: true,
                    behavior: HitTestBehavior.Opaque,
                    onTap: tapCallback,
                    onLongPress: longPressCallback,
                    child: result);
            }

            if (Interactive || _isHovered)
            {
                result = new Listener(
                    behavior: HitTestBehavior.Opaque,
                    onPointerDown: HandlePointerDown,
                    onPointerUp: HandlePointerUp,
                    onPointerCancel: HandlePointerCancel,
                    onPointerEnter: _ => SetHovered(true),
                    onPointerExit: _ => SetHovered(false),
                    child: result);
            }

            if (Interactive)
            {
                result = new Focus(
                    focusNode: _focusNode,
                    autofocus: widget.Autofocus,
                    canRequestFocus: true,
                    onKeyEvent: HandleKeyEvent,
                    child: result);
            }

            // Dart's `_InputPadding`: grows the tap target around the visual material without
            // moving it.
            var tapTargetResult = new InputPadding(
                minSize: widget.TapTargetMinimumSize ?? ResolveTapTargetPaddingMinSize(tapTargetSize),
                child: result);

            return new Semantics(
                label: widget.SemanticLabel,
                flags: ResolveSemanticsFlags(widget, enabled),
                onTap: tapCallback,
                onLongPress: longPressCallback,
                child: tapTargetResult);
        }

        private static Size ResolveTapTargetPaddingMinSize(MaterialTapTargetSize tapTargetSize)
        {
            return tapTargetSize switch
            {
                MaterialTapTargetSize.ShrinkWrap => new Size(0, 0),
                _ => new Size(48, 48)
            };
        }

        private static SemanticsFlags ResolveSemanticsFlags(MaterialButtonCore widget, bool enabled)
        {
            var flags = SemanticsFlags.HasEnabledState;
            if (widget.IsSemanticButton)
            {
                flags |= SemanticsFlags.IsButton;
            }

            if (widget.SemanticEnabled ?? enabled)
            {
                flags |= SemanticsFlags.IsEnabled;
            }

            if (widget.IncludeSemanticSelected && widget.IsSelected)
            {
                flags |= SemanticsFlags.IsSelected;
            }

            if (widget.IsSemanticChecked == true)
            {
                flags |= SemanticsFlags.IsChecked;
            }

            return flags;
        }

        private void AttachFocusNode(FocusNode? externalNode)
        {
            _focusNode = externalNode ?? new FocusNode();
            _ownsFocusNode = externalNode is null;
            _focusNode.AddListener(HandleFocusChanged);
            _hasFocus = _focusNode.HasFocus;
        }

        private void DetachFocusNode(bool disposeOwned)
        {
            if (_focusNode is null)
            {
                return;
            }

            _focusNode.RemoveListener(HandleFocusChanged);
            if (disposeOwned && _ownsFocusNode)
            {
                _focusNode.Dispose();
            }

            _focusNode = null;
            _ownsFocusNode = false;
            _hasFocus = false;
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!IsActivateKey(@event))
            {
                return KeyEventResult.Ignored;
            }

            if (!Interactive)
            {
                return KeyEventResult.Handled;
            }

            if (@event is KeyDownEvent)
            {
                SetFocusOverlaySuppressed(false);
                StartKeyboardPress();
                StartSplash(CenterSplashOrigin);
                HandleTap();
            }

            return KeyEventResult.Handled;
        }

        private void HandleTap()
        {
            if (!Enabled)
            {
                return;
            }

            ConfirmSplash();
            if (IsFeedbackEnabled())
            {
                Feedback.ForTap();
            }

            CurrentWidget.OnPressed?.Invoke();
        }

        private void HandleLongPress()
        {
            if (!Interactive || CurrentWidget.OnLongPress is null)
            {
                return;
            }

            if (IsFeedbackEnabled())
            {
                Feedback.ForLongPress();
            }

            CurrentWidget.OnLongPress.Invoke();
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            SetPressed(true, suppressFocusOverlay: true);
            StartSplash(@event.LocalPosition);
        }

        private void HandlePointerUp(PointerUpEvent @event)
        {
            SetPressed(false);
        }

        private void HandlePointerCancel(PointerCancelEvent @event)
        {
            CancelSplash();
            SetPressed(false);
        }

        private void HandleFocusChanged()
        {
            bool hasFocus = _focusNode?.HasFocus ?? false;
            bool shouldClearFocusSuppression = !hasFocus && _suppressFocusOverlay;
            if (_hasFocus == hasFocus && !shouldClearFocusSuppression)
            {
                return;
            }

            SetState(() =>
            {
                _hasFocus = hasFocus;
                if (!hasFocus)
                {
                    _suppressFocusOverlay = false;
                }
            });
            _statesController?.Update(MaterialState.Focused, hasFocus);
            CurrentWidget.OnFocusChange?.Invoke(hasFocus);
        }

        private void SetPressed(bool value, bool suppressFocusOverlay = false)
        {
            if (!Interactive)
            {
                return;
            }

            bool nextSuppressFocusOverlay = _suppressFocusOverlay || suppressFocusOverlay;
            if (_isPressed == value && _suppressFocusOverlay == nextSuppressFocusOverlay)
            {
                return;
            }

            SetState(() =>
            {
                _isPressed = value;
                _suppressFocusOverlay = nextSuppressFocusOverlay;
            });
            _statesController?.Update(MaterialState.Pressed, value || _isKeyboardPressed);
            CurrentWidget.OnHighlightChanged?.Invoke(value);
        }

        private void SetHovered(bool value)
        {
            if (!Interactive)
            {
                if (!value && _isHovered)
                {
                    SetState(() => _isHovered = false);
                    _statesController?.Update(MaterialState.Hovered, false);
                    CurrentWidget.OnHoverChanged?.Invoke(false);
                    ReleaseMouseCursor();
                }

                return;
            }

            if (_isHovered == value)
            {
                return;
            }

            SetState(() => _isHovered = value);
            _statesController?.Update(MaterialState.Hovered, value);
            if (value)
            {
                UpdateMouseCursor();
            }
            else
            {
                ReleaseMouseCursor();
            }

            CurrentWidget.OnHoverChanged?.Invoke(value);
        }

        private void UpdateMouseCursor()
        {
            ReleaseMouseCursor();
            var states = BuildMaterialStates(enabled: Enabled, includeFocus: true);
            _mouseCursorHandle = MouseCursorManager.PushCursor(
                CurrentWidget.MouseCursor
                ?? CurrentWidget.Style.ResolveMouseCursor(states)
                ?? SystemMouseCursors.Click);
        }

        private void ReleaseMouseCursor()
        {
            _mouseCursorHandle?.Dispose();
            _mouseCursorHandle = null;
        }

        private bool IsFeedbackEnabled()
        {
            return CurrentWidget.EnableFeedback ?? CurrentWidget.Style.EnableFeedback ?? true;
        }

        private void SetFocusOverlaySuppressed(bool value)
        {
            if (_suppressFocusOverlay == value)
            {
                return;
            }

            SetState(() => _suppressFocusOverlay = value);
        }

        private void StartKeyboardPress()
        {
            if (!Interactive || _keyboardPressController is null)
            {
                return;
            }

            if (!_isKeyboardPressed)
            {
                SetState(() => _isKeyboardPressed = true);
                _statesController?.Update(MaterialState.Pressed, true);
            }

            _keyboardPressController.Forward(0);
        }

        private void StartSplash(Point origin)
        {
            if (!Interactive || _splashController is null)
            {
                return;
            }

            var splashStates = BuildMaterialStates(enabled: true, includeFocus: !_suppressFocusOverlay);
            var style = CurrentWidget.Style;
            Color? splashBaseColor = style.ResolveOverlayColor(splashStates);
            InteractiveInkFeature? feature = splashBaseColor.HasValue
                ? (_resolvedSplashFactory ?? InkSplash.SplashFactory).Create(
                    new InkFeatureConfiguration(
                        Position: origin,
                        Color: splashBaseColor.Value,
                        TextDirection: _resolvedTextDirection,
                        ContainedInkWell: true,
                        BorderRadius: _resolvedBorderRadius,
                        Radius: CurrentWidget.SplashRadius))
                : null;

            SetState(() =>
            {
                _isSplashActive = true;
                _splashProgress = 0;
                _splashOrigin = origin;
                _splashBaseColor = splashBaseColor;
                _splashFeature = feature;
                _splashConfirmed = false;
                _splashCanceled = false;
            });

            if (feature is not null)
            {
                _splashController.Duration = feature.UnconfirmedDuration;
            }
            _splashController.Forward(0);
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

        private MaterialState BuildMaterialStates(bool enabled, bool includeFocus)
        {
            if (!enabled)
            {
                MaterialState disabledStates = CurrentWidget.IsSelected
                    ? MaterialState.Disabled | MaterialState.Selected
                    : MaterialState.Disabled;
                return disabledStates | (_statesController?.Value ?? MaterialState.None);
            }

            var states = MaterialState.None;
            if (CurrentWidget.IsSelected)
            {
                states |= MaterialState.Selected;
            }

            if (_isPressed)
            {
                states |= MaterialState.Pressed;
            }

            if (_isKeyboardPressed)
            {
                states |= MaterialState.Pressed;
            }

            if (_isHovered)
            {
                states |= MaterialState.Hovered;
            }

            if (includeFocus && _hasFocus)
            {
                states |= MaterialState.Focused;
            }

            return states | (_statesController?.Value ?? MaterialState.None);
        }

        private void AttachStatesController(MaterialStatesController? controller)
        {
            _statesController = controller;
            if (_statesController is not null)
            {
                _statesController.AddListener(HandleStatesControllerChanged);
                SyncFixedControllerStates();
            }
        }

        private void DetachStatesController()
        {
            if (_statesController is not null)
            {
                _statesController.RemoveListener(HandleStatesControllerChanged);
                _statesController = null;
            }
        }

        private void HandleStatesControllerChanged()
        {
            SetState(static () => { });
        }

        private void SyncFixedControllerStates()
        {
            _statesController?.Update(MaterialState.Disabled, !Enabled);
            _statesController?.Update(MaterialState.Selected, CurrentWidget.IsSelected);
        }

        private Color ResolveForegroundColor(ButtonStyle style, MaterialState states)
        {
            var color = style.ResolveForegroundColor(states);
            if (!color.HasValue && states.HasFlag(MaterialState.Disabled))
            {
                color = style.ResolveForegroundColor(MaterialState.None);
            }

            return color ?? Colors.Black;
        }

        private static Color ResolveIconColor(
            ButtonStyle style,
            MaterialState states,
            Color fallbackForeground)
        {
            var color = style.ResolveIconColor(states);
            if (!color.HasValue && states.HasFlag(MaterialState.Disabled))
            {
                color = style.ResolveIconColor(MaterialState.None);
            }

            if (!color.HasValue)
            {
                color = style.ResolveForegroundColor(states);
            }

            if (!color.HasValue && states.HasFlag(MaterialState.Disabled))
            {
                color = style.ResolveForegroundColor(MaterialState.None);
            }

            return color ?? fallbackForeground;
        }

        private static double? ResolveIconSize(ButtonStyle style, MaterialState states)
        {
            double? size = style.ResolveIconSize(states);
            if (!size.HasValue && states.HasFlag(MaterialState.Disabled))
            {
                size = style.ResolveIconSize(MaterialState.None);
            }

            if (!size.HasValue)
            {
                return null;
            }

            double resolved = size.Value;
            if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
            {
                return null;
            }

            return resolved;
        }

        private static double ResolveElevation(ButtonStyle style, MaterialState states)
        {
            double? elevation = style.ResolveElevation(states);
            if (!elevation.HasValue && states.HasFlag(MaterialState.Disabled))
            {
                elevation = style.ResolveElevation(MaterialState.None);
            }

            if (!elevation.HasValue)
            {
                return 0;
            }

            double resolved = elevation.Value;
            if (double.IsNaN(resolved) || double.IsInfinity(resolved))
            {
                return 0;
            }

            return Math.Max(0, resolved);
        }

        private static Color? ResolveShadowColor(
            ButtonStyle style,
            MaterialState states,
            double elevation,
            Color themeShadowColor)
        {
            var shadowColor = style.ResolveShadowColor(states);
            if (!shadowColor.HasValue && states.HasFlag(MaterialState.Disabled))
            {
                shadowColor = style.ResolveShadowColor(MaterialState.None);
            }

            // Plumix.Sample Material falls back to theme shadow color when elevation is active
            // and no explicit shadow color is provided by button style layers.
            if (!shadowColor.HasValue && elevation > 0)
            {
                shadowColor = themeShadowColor;
            }

            return shadowColor;
        }

        private static IReadOnlyList<BoxShadow>? ResolveBoxShadows(double elevation, Color? shadowColor)
        {
            if (elevation <= 0 || !shadowColor.HasValue || shadowColor.Value.A == 0)
            {
                return null;
            }

            var keyShadow = new BoxShadow(
            color: ApplyShadowOpacity(shadowColor.Value, 0.20),
            offset: new Point(0, Math.Max(1, Math.Round(elevation))),
            blurRadius: Math.Max(2, elevation * 2.4));

            var ambientShadow = new BoxShadow(
            color: ApplyShadowOpacity(shadowColor.Value, 0.14),
            offset: new Point(0, Math.Max(1, Math.Round(elevation * 0.5))),
            blurRadius: Math.Max(3, elevation * 3.2));

            return [keyShadow, ambientShadow];
        }

        private static Color ApplyShadowOpacity(Color color, double opacityMultiplier)
        {
            double baseOpacity = color.A / 255.0;
            double effectiveOpacity = Math.Clamp(baseOpacity * opacityMultiplier, 0, 1);
            byte alpha = (byte)Math.Clamp((int)(effectiveOpacity * 255), 0, 255);
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        private static Color? ResolveBackgroundColor(
            ButtonStyle style,
            MaterialState baseStates,
            MaterialState overlayStates,
            double elevation,
            bool useMaterial3)
        {
            var background = style.ResolveBackgroundColor(baseStates);
            if (!background.HasValue && baseStates.HasFlag(MaterialState.Disabled))
            {
                background = style.ResolveBackgroundColor(MaterialState.None);
            }

            if (background.HasValue && useMaterial3)
            {
                var surfaceTintColor = style.ResolveSurfaceTintColor(baseStates);
                if (!surfaceTintColor.HasValue && baseStates.HasFlag(MaterialState.Disabled))
                {
                    surfaceTintColor = style.ResolveSurfaceTintColor(MaterialState.None);
                }

                if (surfaceTintColor.HasValue)
                {
                    background = ElevationOverlay.ApplySurfaceTint(
                        background.Value,
                        surfaceTintColor,
                        elevation);
                }
            }

            var overlay = HasOverlayState(overlayStates)
                ? style.ResolveOverlayColor(overlayStates)
                : null;

            if (!background.HasValue)
            {
                return overlay;
            }

            if (!overlay.HasValue)
            {
                return background;
            }

            return BlendColorOverlay(background.Value, overlay.Value);
        }

        private static bool HasOverlayState(MaterialState states)
        {
            return states.HasFlag(MaterialState.Pressed)
                   || states.HasFlag(MaterialState.Hovered)
                   || states.HasFlag(MaterialState.Focused);
        }

        private Color? ResolveSplashColor()
        {
            if (!_isSplashActive)
            {
                return null;
            }

            if (!_splashBaseColor.HasValue)
            {
                return null;
            }

            double fade = ResolveSplashFade(_splashProgress);
            double opacity = Math.Clamp((_splashBaseColor.Value.A / 255.0) * fade, 0, 1);
            if (opacity <= 0.001)
            {
                return null;
            }

            byte alpha = (byte)Math.Clamp((int)(opacity * 255), 0, 255);
            return Color.FromArgb(alpha, _splashBaseColor.Value.R, _splashBaseColor.Value.G, _splashBaseColor.Value.B);
        }

        private static void ValidateMinimumSize(Size minimumSize)
        {
            if (double.IsNaN(minimumSize.Width) || double.IsInfinity(minimumSize.Width) || minimumSize.Width < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumSize),
                    "Minimum width must be non-negative and finite.");
            }

            if (double.IsNaN(minimumSize.Height) || double.IsInfinity(minimumSize.Height) || minimumSize.Height < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumSize),
                    "Minimum height must be non-negative and finite.");
            }
        }

        private static void ValidateMaximumSize(Size maximumSize)
        {
            if (double.IsNaN(maximumSize.Width) || maximumSize.Width < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumSize),
                    "Maximum width must be non-negative and not NaN.");
            }

            if (double.IsNaN(maximumSize.Height) || maximumSize.Height < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumSize),
                    "Maximum height must be non-negative and not NaN.");
            }
        }

        private static void ValidateFixedSize(Size? fixedSize)
        {
            if (!fixedSize.HasValue)
            {
                return;
            }

            var value = fixedSize.Value;
            if (double.IsNaN(value.Width) || value.Width < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fixedSize),
                    "Fixed width must be non-negative and not NaN.");
            }

            if (double.IsNaN(value.Height) || value.Height < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fixedSize),
                    "Fixed height must be non-negative and not NaN.");
            }
        }

        private static BoxConstraints CreateEffectiveConstraints(
            Size minimumSize,
            Size maximumSize,
            Size? fixedSize)
        {
            double normalizedMaxWidth = double.IsPositiveInfinity(maximumSize.Width)
                ? double.PositiveInfinity
                : Math.Max(maximumSize.Width, minimumSize.Width);
            double normalizedMaxHeight = double.IsPositiveInfinity(maximumSize.Height)
                ? double.PositiveInfinity
                : Math.Max(maximumSize.Height, minimumSize.Height);

            var effectiveConstraints = new BoxConstraints(
                MinWidth: minimumSize.Width,
                MaxWidth: normalizedMaxWidth,
                MinHeight: minimumSize.Height,
                MaxHeight: normalizedMaxHeight);

            if (!fixedSize.HasValue)
            {
                return effectiveConstraints;
            }

            var constrainedFixedSize = effectiveConstraints.Constrain(fixedSize.Value);
            if (double.IsFinite(constrainedFixedSize.Width))
            {
                effectiveConstraints = effectiveConstraints with
                {
                    MinWidth = constrainedFixedSize.Width,
                    MaxWidth = constrainedFixedSize.Width
                };
            }

            if (double.IsFinite(constrainedFixedSize.Height))
            {
                effectiveConstraints = effectiveConstraints with
                {
                    MinHeight = constrainedFixedSize.Height,
                    MaxHeight = constrainedFixedSize.Height
                };
            }

            return effectiveConstraints;
        }

        private static TextStyle MergeTextStyle(TextStyle baseStyle, TextStyle? style)
        {
            if (style is null)
            {
                return baseStyle;
            }

            return new TextStyle(
                FontFamily: style.FontFamily ?? baseStyle.FontFamily,
                FontSize: style.FontSize ?? baseStyle.FontSize,
                // Plumix.Sample ButtonStyleButton ignores textStyle color and uses foregroundColor instead.
                Color: baseStyle.Color,
                FontWeight: style.FontWeight ?? baseStyle.FontWeight,
                FontStyle: style.FontStyle ?? baseStyle.FontStyle,
                Height: style.Height ?? baseStyle.Height,
                LetterSpacing: style.LetterSpacing ?? baseStyle.LetterSpacing);
        }

        private static double ResolveSplashFade(double progress)
        {
            double clamped = Math.Clamp(progress, 0, 1);
            const double fadeStart = 0.72;
            if (clamped <= fadeStart)
            {
                return 1;
            }

            double tailProgress = (clamped - fadeStart) / (1 - fadeStart);
            return Math.Clamp(1 - tailProgress, 0, 1);
        }

        private void HandleSplashTick()
        {
            if (!_isSplashActive || _splashController is null)
            {
                return;
            }

            SetState(() =>
            {
                _splashProgress = Math.Clamp(_splashController.Evaluate(), 0, 1);
            });
        }

        private void HandleSplashCompleted()
        {
            if (!_isSplashActive)
            {
                return;
            }

            SetState(() =>
            {
                _isSplashActive = false;
                _splashProgress = 0;
                _splashOrigin = CenterSplashOrigin;
                _splashBaseColor = null;
                _splashFeature = null;
                _splashConfirmed = false;
                _splashCanceled = false;
            });
        }

        private void HandleKeyboardPressCompleted()
        {
            if (!_isKeyboardPressed)
            {
                return;
            }

            SetState(() => _isKeyboardPressed = false);
            _statesController?.Update(MaterialState.Pressed, _isPressed);
        }

        private static bool IsActivateKey(LogicalKeyboardKey key)
        {
            return key.Equals(LogicalKeyboardKey.Enter)
                   || key.Equals(LogicalKeyboardKey.Enter)
                   || key.Equals(LogicalKeyboardKey.NumpadEnter)
                   || key.Equals(LogicalKeyboardKey.NumpadEnter)
                   || key.Equals(LogicalKeyboardKey.Space)
                   || key.Equals(LogicalKeyboardKey.Space);
        }

        private static bool IsActivateKey(KeyEvent @event)
        {
            if (HardwareKeyboard.Instance.IsShiftPressed
                || HardwareKeyboard.Instance.IsControlPressed
                || HardwareKeyboard.Instance.IsAltPressed
                || HardwareKeyboard.Instance.IsMetaPressed)
            {
                return false;
            }

            return IsActivateKey(@event.LogicalKey);
        }
    }

    private static Color BlendColorOverlay(Color baseColor, Color overlayColor)
    {
        if (baseColor.A == 0)
        {
            return overlayColor;
        }

        static byte Blend(byte from, byte to, double t)
        {
            return (byte)Math.Clamp((int)(from + ((to - from) * t)), 0, 255);
        }

        double clampedOpacity = Math.Clamp(overlayColor.A / 255.0, 0, 1);
        return Color.FromArgb(
            baseColor.A,
            Blend(baseColor.R, overlayColor.R, clampedOpacity),
            Blend(baseColor.G, overlayColor.G, clampedOpacity),
            Blend(baseColor.B, overlayColor.B, clampedOpacity));
    }

}
