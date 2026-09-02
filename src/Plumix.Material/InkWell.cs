using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/ink_well.dart

public class InkResponse : StatefulWidget
{
    public InkResponse(
        Widget? child = null,
        Action? onTap = null,
        Action<TapDownDetails>? onTapDown = null,
        Action<TapUpDetails>? onTapUp = null,
        Action? onTapCancel = null,
        Action? onDoubleTap = null,
        Action? onLongPress = null,
        Action? onLongPressUp = null,
        Action? onSecondaryTap = null,
        Action<TapUpDetails>? onSecondaryTapUp = null,
        Action<TapDownDetails>? onSecondaryTapDown = null,
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
    public Action<TapDownDetails>? OnTapDown { get; }
    public Action<TapUpDetails>? OnTapUp { get; }
    public Action? OnTapCancel { get; }
    public Action? OnDoubleTap { get; }
    public Action? OnLongPress { get; }
    public Action? OnLongPressUp { get; }
    public Action? OnSecondaryTap { get; }
    public Action<TapUpDetails>? OnSecondaryTapUp { get; }
    public Action<TapDownDetails>? OnSecondaryTapDown { get; }
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

    public virtual Func<Rect>? GetRectCallback(RenderBox referenceBox)
    {
        ArgumentNullException.ThrowIfNull(referenceBox);
        return ContainedInkWell ? () => new Rect(referenceBox.Size) : null;
    }

    public override State CreateState() => new InkResponseState();

    private sealed class InkResponseState : State, IParentInkResponseState
    {
        private static readonly Point CenterOrigin = new(double.NaN, double.NaN);
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private MaterialStatesController? _statesController;
        private bool _ownsStatesController;
        private bool _pressed;
        private bool _hovered;
        private bool _focused;
        private bool _hoverCallbackActive;
        private Point _splashOrigin = CenterOrigin;
        private double _splashProgress;
        private InteractiveInkFeatureFactory? _resolvedSplashFactory;
        private InteractiveInkFeature? _splashFeature;
        private TextDirection _textDirection = TextDirection.Ltr;
        private Color _resolvedSplashColor;
        private bool _splashConfirmed;
        private bool _splashCanceled;
        private IDisposable? _cursorHandle;
        private readonly List<SplashEntry> _splashes = [];
        private readonly List<HighlightEntry> _highlights = [];
        private SplashEntry? _currentSplash;
        private NavigationMode _navigationMode = NavigationMode.Traditional;
        private Plumix.AnimationController? _activationController;
        private readonly HashSet<object> _pressedChildren = [];
        private IParentInkResponseState? _parentState;

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
            FocusManager.Instance.AddHighlightModeListener(HandleFocusHighlightModeChanged);
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

            if (oldResponse.Radius != CurrentWidget.Radius
                || oldResponse.HighlightShape != CurrentWidget.HighlightShape
                || oldResponse.BorderRadius != CurrentWidget.BorderRadius)
            {
                ResetHighlight(InkHighlightKind.Hover);
                ResetHighlight(InkHighlightKind.Focus);
            }

            if (oldResponse.CustomBorder != CurrentWidget.CustomBorder)
            {
                foreach (SplashEntry splash in _splashes)
                {
                    splash.Feature.UpdateConfiguration(
                        splash.Feature.Configuration with { CustomBorder = CurrentWidget.CustomBorder });
                }
            }

            SyncDisabledState();
            if (!Enabled)
            {
                SetPressed(false, notifyCancel: true);
                ResetHighlight(InkHighlightKind.Hover);
                ReleaseCursor();
            }
        }

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            IParentInkResponseState? nextParent =
                Context.DependOnInherited<ParentInkResponseProvider>()?.State;
            if (ReferenceEquals(_parentState, nextParent))
            {
                return;
            }

            _parentState?.ChildPressedChanged(this, false);
            _parentState = nextParent;
            if (_pressed || _pressedChildren.Count > 0)
            {
                _parentState?.ChildPressedChanged(this, true);
            }
        }

        public override void Deactivate()
        {
            _parentState?.ChildPressedChanged(this, false);
            base.Deactivate();
        }

        public override void Dispose()
        {
            ReleaseCursor();
            FocusManager.Instance.RemoveHighlightModeListener(HandleFocusHighlightModeChanged);
            DetachFocusNode();
            DetachStatesController();
            foreach (SplashEntry splash in _splashes.ToArray())
            {
                splash.Controller.Dispose();
            }
            _splashes.Clear();
            foreach (HighlightEntry highlight in _highlights.ToArray())
            {
                highlight.Controller.Dispose();
            }
            _highlights.Clear();
            _activationController?.Dispose();
            _activationController = null;
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var theme = Theme.Of(context);
            _resolvedSplashFactory = widget.SplashFactory ?? theme.SplashFactory;
            _textDirection = Directionality.Of(context);
            _navigationMode = MediaQuery.MaybeNavigationModeOf(context) ?? NavigationMode.Traditional;
            var states = _statesController?.Value ?? MaterialState.None;
            UpdateHighlights(theme, states);
            var splashColor = widget.OverlayColor?.Resolve(states | MaterialState.Pressed)
                              ?? widget.SplashColor
                              ?? theme.SplashColor;
            _resolvedSplashColor = splashColor;
            if (_currentSplash is not null)
            {
                _currentSplash.Feature.UpdateConfiguration(
                    _currentSplash.Feature.Configuration with { Color = splashColor });
            }
            var borderRadius = ShapeBorderGeometry.ResolveRadiusOrNull(widget.CustomBorder)
                               ?? widget.BorderRadius
                               ?? Plumix.Rendering.BorderRadius.Zero;

            Widget result = new InkResponsePaint(
                highlightColor: null,
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
                rectCallbackFactory: widget.GetRectCallback,
                controller: Material.MaybeOf(context),
                splashes: _splashes
                    .Select(splash => new InkSplashVisual(
                        splash.Feature,
                        splash.Controller.Evaluate(),
                        splash.Confirmed,
                        splash.Canceled))
                    .ToArray(),
                highlights: _highlights
                    .Select(highlight => new InkHighlightVisual(
                        highlight.Kind,
                        highlight.Color,
                        highlight.Controller.Evaluate()))
                    .ToArray(),
                child: widget.Child ?? new SizedBox());

            if (Enabled)
            {
                result = new GestureDetector(
                   excludeFromSemantics: true,
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

            }

            if (!widget.ExcludeFromSemantics)
            {
                result = new Semantics(
                    onTap: widget.OnTap is null ? null : HandleSemanticTap,
                    onLongPress: widget.OnLongPress is null ? null : HandleSemanticLongPress,
                    child: result);
            }

            result = new Listener(
                behavior: HitTestBehavior.Opaque,
                onPointerEnter: _ => SetHovered(true),
                onPointerExit: _ => SetHovered(false),
                child: result);

            result = new Focus(
                focusNode: _focusNode,
                autofocus: widget.Autofocus,
                canRequestFocus: _navigationMode == NavigationMode.Directional
                                 || (Enabled && widget.CanRequestFocus),
                onKeyEvent: HandleKeyEvent,
                child: result);

            return new ParentInkResponseProvider(this, result);
        }

        private Color ResolveHighlightColor(
            ThemeData theme,
            MaterialState states,
            InkHighlightKind kind)
        {
            MaterialState nonHighlightStates = states
                                               & ~(MaterialState.Pressed
                                                   | MaterialState.Hovered
                                                   | MaterialState.Focused);
            return kind switch
            {
                InkHighlightKind.Pressed => CurrentWidget.OverlayColor?.Resolve(
                                                nonHighlightStates | MaterialState.Pressed)
                                            ?? CurrentWidget.HighlightColor
                                            ?? theme.HighlightColor,
                InkHighlightKind.Hover => CurrentWidget.OverlayColor?.Resolve(
                                              nonHighlightStates | MaterialState.Hovered)
                                          ?? CurrentWidget.HoverColor
                                          ?? theme.HoverColor,
                _ => CurrentWidget.OverlayColor?.Resolve(nonHighlightStates | MaterialState.Focused)
                     ?? CurrentWidget.FocusColor
                     ?? theme.FocusColor,
            };
        }

        private void HandleTapDown(TapDownDetails details)
        {
            StartSplash(details.LocalPosition);
            CurrentWidget.OnTapDown?.Invoke(details);
        }

        private void HandleTapUp(TapUpDetails details) => CurrentWidget.OnTapUp?.Invoke(details);

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

        private void HandleSecondaryTapDown(TapDownDetails details)
        {
            StartSplash(details.LocalPosition);
            CurrentWidget.OnSecondaryTapDown?.Invoke(details);
        }

        private void HandleSecondaryTapUp(TapUpDetails details) => CurrentWidget.OnSecondaryTapUp?.Invoke(details);

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
            if (_pressedChildren.Count > 0)
            {
                return;
            }

            if (_currentSplash is not null)
            {
                CancelEntry(_currentSplash);
            }

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
            if (feature is NoSplash)
            {
                SetState(() =>
                {
                    _splashOrigin = origin;
                    _splashProgress = 0.0;
                    _splashFeature = feature;
                    _splashConfirmed = false;
                    _splashCanceled = false;
                });
                SetPressed(true);
                return;
            }

            var controller = new Plumix.AnimationController(duration: feature.UnconfirmedDuration, vsync: this)
            {
                Curve = Curves.Linear,
            };
            var entry = new SplashEntry(feature, controller);
            controller.Changed += () => HandleSplashChanged(entry);
            controller.Completed += () => HandleSplashCompleted(entry);
            _splashes.Add(entry);
            _currentSplash = entry;
            SyncLegacySplashFields(entry, origin);
            SetPressed(true);
            controller.Forward(0);
        }

        private void ConfirmSplash()
        {
            SplashEntry? entry = _currentSplash;
            _currentSplash = null;
            if (entry is null || entry.Canceled)
            {
                if (_splashFeature is NoSplash)
                {
                    SetState(ClearLegacySplashFieldsIfIdle);
                }
                return;
            }

            entry.Confirmed = true;
            entry.Controller.Duration = entry.Feature.ConfirmDuration;
            entry.Controller.Forward();
            SyncLegacySplashFields(entry, entry.Feature.Configuration.Position);
        }

        private void CancelSplash()
        {
            SplashEntry? entry = _currentSplash;
            _currentSplash = null;
            if (entry is null)
            {
                if (_splashFeature is NoSplash)
                {
                    SetState(ClearLegacySplashFieldsIfIdle);
                }
                return;
            }

            CancelEntry(entry);
        }

        private void CancelEntry(SplashEntry entry)
        {
            if (entry.Canceled)
            {
                return;
            }

            entry.Canceled = true;
            entry.Controller.Duration = entry.Feature.CancelDuration;
            entry.Controller.Forward();
            SyncLegacySplashFields(entry, entry.Feature.Configuration.Position);
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!IsActivateKey(@event)) return KeyEventResult.Ignored;
            if (@event is KeyDownEvent && CurrentWidget.OnTap is not null)
            {
                HandleActivation();
            }
            return KeyEventResult.Handled;
        }

        private void HandleActivation()
        {
            StartSplash(CenterOrigin);
            ConfirmSplash();
            if (CurrentWidget.EnableFeedback)
            {
                Feedback.ForTap();
            }
            CurrentWidget.OnTap?.Invoke();

            if (_activationController is null)
            {
                _activationController = new Plumix.AnimationController(
                    duration: TimeSpan.FromMilliseconds(100.0),
                    vsync: this);
                _activationController.Completed += () =>
                {
                    if (Mounted)
                    {
                        SetPressed(false);
                    }
                };
            }
            _activationController.Forward(0.0);
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

        private void HandleFocusHighlightModeChanged(FocusHighlightMode mode)
        {
            if (Mounted)
            {
                SetState(() => { });
            }
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

        private void UpdateHighlights(ThemeData theme, MaterialState states)
        {
            TimeSpan hoverDuration = CurrentWidget.HoverDuration ?? TimeSpan.FromMilliseconds(50.0);
            EnsureHighlight(
                InkHighlightKind.Pressed,
                _pressed,
                ResolveHighlightColor(theme, states, InkHighlightKind.Pressed),
                TimeSpan.FromMilliseconds(200.0));

            Color hoverColor = ResolveHighlightColor(theme, states, InkHighlightKind.Hover);
            if (!Enabled)
            {
                hoverColor = Color.FromArgb(0, hoverColor.R, hoverColor.G, hoverColor.B);
            }
            EnsureHighlight(InkHighlightKind.Hover, _hovered, hoverColor, hoverDuration);

            bool shouldShowFocus = FocusManager.Instance.HighlightMode == FocusHighlightMode.Traditional
                                   && _focused
                                   && (_navigationMode == NavigationMode.Directional || Enabled);
            EnsureHighlight(
                InkHighlightKind.Focus,
                shouldShowFocus,
                ResolveHighlightColor(theme, states, InkHighlightKind.Focus),
                hoverDuration);
        }

        private void EnsureHighlight(
            InkHighlightKind kind,
            bool active,
            Color color,
            TimeSpan duration)
        {
            HighlightEntry? entry = _highlights.FirstOrDefault(candidate => candidate.Kind == kind);
            if (entry is null)
            {
                if (!active)
                {
                    return;
                }

                var controller = new Plumix.AnimationController(duration: duration, vsync: this)
                {
                    Curve = Curves.Linear,
                };
                entry = new HighlightEntry(kind, color, controller);
                HighlightEntry capturedEntry = entry;
                controller.Changed += () => HandleHighlightChanged(capturedEntry);
                controller.Dismissed += () => HandleHighlightDismissed(capturedEntry);
                _highlights.Add(entry);
                controller.Forward(0.0);
                return;
            }

            entry.Color = color;
            entry.Controller.Duration = duration;
            if (active == entry.Active)
            {
                return;
            }

            entry.Active = active;
            if (active)
            {
                entry.Controller.Forward();
            }
            else
            {
                entry.Controller.Reverse();
            }
        }

        private void ResetHighlight(InkHighlightKind kind)
        {
            HighlightEntry? entry = _highlights.FirstOrDefault(candidate => candidate.Kind == kind);
            if (entry is null)
            {
                return;
            }

            _highlights.Remove(entry);
            entry.Controller.Dispose();
        }

        private void HandleHighlightChanged(HighlightEntry entry)
        {
            if (Mounted && _highlights.Contains(entry))
            {
                SetState(() => { });
            }
        }

        private void HandleHighlightDismissed(HighlightEntry entry)
        {
            if (entry.Active || !_highlights.Remove(entry))
            {
                return;
            }

            entry.Controller.Dispose();
            if (Mounted)
            {
                SetState(() => { });
            }
        }

        private void SetPressed(bool value, bool notifyCancel = false)
        {
            if (_pressed == value) return;
            SetState(() => _pressed = value);
            _statesController?.Update(MaterialState.Pressed, value);
            _parentState?.ChildPressedChanged(this, value || _pressedChildren.Count > 0);
            CurrentWidget.OnHighlightChanged?.Invoke(value);
            if (!value && notifyCancel) CurrentWidget.OnTapCancel?.Invoke();
        }

        void IParentInkResponseState.ChildPressedChanged(object child, bool pressed)
        {
            bool wasActive = _pressedChildren.Count > 0;
            if (pressed)
            {
                _pressedChildren.Add(child);
            }
            else
            {
                _pressedChildren.Remove(child);
            }

            bool isActive = _pressedChildren.Count > 0;
            if (wasActive != isActive)
            {
                _parentState?.ChildPressedChanged(this, _pressed || isActive);
            }
        }

        private void SetHovered(bool value, bool notify = true)
        {
            if (_hovered == value) return;
            SetState(() => _hovered = value);
            _statesController?.Update(MaterialState.Hovered, value);
            if (value)
            {
                ReleaseCursor();
                MaterialState states = _statesController?.Value ?? MaterialState.None;
                MouseCursor? cursor = CurrentWidget.MouseCursor is WidgetStateMouseCursor stateCursor
                    ? stateCursor.Resolve(states)
                    : CurrentWidget.MouseCursor;
                cursor ??= Enabled ? SystemMouseCursors.Click : SystemMouseCursors.Basic;
                _cursorHandle = MouseCursorManager.PushCursor(cursor);
                if (notify && Enabled)
                {
                    _hoverCallbackActive = true;
                    CurrentWidget.OnHover?.Invoke(true);
                }
            }
            else
            {
                ReleaseCursor();
                if (!value && notify && _hoverCallbackActive)
                {
                    _hoverCallbackActive = false;
                    CurrentWidget.OnHover?.Invoke(false);
                }
            }
        }

        private void HandleSplashChanged(SplashEntry entry)
        {
            if (!Mounted || !_splashes.Contains(entry))
            {
                return;
            }

            SetState(() =>
            {
                if (ReferenceEquals(_currentSplash, entry) || _splashes.Count == 1)
                {
                    _splashProgress = entry.Controller.Evaluate();
                }
            });
        }

        private void HandleSplashCompleted(SplashEntry entry)
        {
            if (!_splashes.Remove(entry))
            {
                return;
            }

            if (ReferenceEquals(_currentSplash, entry))
            {
                _currentSplash = null;
            }

            entry.Controller.Dispose();
            if (Mounted)
            {
                SetState(ClearLegacySplashFieldsIfIdle);
            }
        }

        private void SyncLegacySplashFields(SplashEntry entry, Point origin)
        {
            SetState(() =>
            {
                _splashOrigin = origin;
                _splashProgress = entry.Controller.Evaluate();
                _splashFeature = entry.Feature;
                _splashConfirmed = entry.Confirmed;
                _splashCanceled = entry.Canceled;
            });
        }

        private void ClearLegacySplashFieldsIfIdle()
        {
            SplashEntry? latest = _splashes.LastOrDefault();
            _splashProgress = latest?.Controller.Evaluate() ?? 0.0;
            _splashOrigin = latest?.Feature.Configuration.Position ?? CenterOrigin;
            _splashFeature = latest?.Feature;
            _splashConfirmed = latest?.Confirmed ?? false;
            _splashCanceled = latest?.Canceled ?? false;
        }

        private void ReleaseCursor()
        {
            _cursorHandle?.Dispose();
            _cursorHandle = null;
        }

        private static bool IsActivateKey(KeyEvent @event)
        {
            HardwareKeyboard state = HardwareKeyboard.Instance;
            if (state.IsShiftPressed || state.IsControlPressed || state.IsAltPressed || state.IsMetaPressed)
            {
                return false;
            }

            return @event.LogicalKey.Equals(LogicalKeyboardKey.Enter)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.Enter)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.NumpadEnter)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.NumpadEnter)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.Space)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.Space);
        }

        private sealed class SplashEntry
        {
            public SplashEntry(InteractiveInkFeature feature, Plumix.AnimationController controller)
            {
                Feature = feature;
                Controller = controller;
            }

            public InteractiveInkFeature Feature { get; }

            public Plumix.AnimationController Controller { get; }

            public bool Confirmed { get; set; }

            public bool Canceled { get; set; }
        }

        private sealed class HighlightEntry
        {
            public HighlightEntry(
                InkHighlightKind kind,
                Color color,
                Plumix.AnimationController controller)
            {
                Kind = kind;
                Color = color;
                Controller = controller;
                Active = true;
            }

            public InkHighlightKind Kind { get; }

            public Color Color { get; set; }

            public Plumix.AnimationController Controller { get; }

            public bool Active { get; set; }
        }
    }
}

internal interface IParentInkResponseState
{
    void ChildPressedChanged(object child, bool pressed);
}

internal sealed class ParentInkResponseProvider : InheritedWidget
{
    public ParentInkResponseProvider(
        IParentInkResponseState state,
        Widget child) : base()
    {
        State = state;
        Child = child;
    }

    public IParentInkResponseState State { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;
}

public sealed class InkWell : InkResponse
{
    public InkWell(
        Widget? child = null,
        Action? onTap = null,
        Action? onDoubleTap = null,
        Action<TapDownDetails>? onTapDown = null,
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
        Action<TapUpDetails>? onTapUp = null,
        Action? onSecondaryTap = null,
        Action<TapUpDetails>? onSecondaryTapUp = null,
        Action<TapDownDetails>? onSecondaryTapDown = null,
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

/// <summary>An ink response whose highlight and splash are clipped to its nearest table row.</summary>
/// <remarks>Dart parity source: material_ui/lib/src/data_table.dart.</remarks>
public sealed class TableRowInkWell : InkResponse
{
    public TableRowInkWell(
        Widget? child = null,
        Action? onTap = null,
        Action? onDoubleTap = null,
        Action? onLongPress = null,
        Action<bool>? onHighlightChanged = null,
        Action<bool>? onHover = null,
        Action? onSecondaryTap = null,
        Action<TapDownDetails>? onSecondaryTapDown = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        MouseCursor? mouseCursor = null,
        Key? key = null)
        : base(
            child: child,
            onTap: onTap,
            onDoubleTap: onDoubleTap,
            onLongPress: onLongPress,
            onHighlightChanged: onHighlightChanged,
            onHover: onHover,
            onSecondaryTap: onSecondaryTap,
            onSecondaryTapDown: onSecondaryTapDown,
            overlayColor: overlayColor,
            mouseCursor: mouseCursor,
            containedInkWell: true,
            highlightShape: BoxShape.Rectangle,
            key: key)
    {
    }

    public override Func<Rect> GetRectCallback(RenderBox referenceBox)
    {
        ArgumentNullException.ThrowIfNull(referenceBox);
        return () => ResolveTableRowRect(referenceBox);
    }

    private static Rect ResolveTableRowRect(RenderBox referenceBox)
    {
        Matrix4 transform = Matrix4.Identity();
        RenderObject cell = referenceBox;
        RenderObject? table = cell.Parent;
        while (table is not null && table is not RenderTable)
        {
            MatrixUtils.MultiplyInPlace(ResolveChildTransform(cell, table), transform);
            cell = table;
            table = table.Parent;
        }

        if (table is not RenderTable renderTable
            || cell.parentData is not TableCellParentData { Y: { } rowIndex })
        {
            return new Rect();
        }

        MatrixUtils.MultiplyInPlace(ResolveChildTransform(cell, renderTable), transform);
        Point origin = MatrixUtils.TransformPoint(transform, default);
        Point horizontal = MatrixUtils.TransformPoint(transform, new Point(1.0, 0.0));
        Point vertical = MatrixUtils.TransformPoint(transform, new Point(0.0, 1.0));
        const double epsilon = 0.000001;
        bool isTranslation = Math.Abs(horizontal.X - origin.X - 1.0) < epsilon
                             && Math.Abs(horizontal.Y - origin.Y) < epsilon
                             && Math.Abs(vertical.X - origin.X) < epsilon
                             && Math.Abs(vertical.Y - origin.Y - 1.0) < epsilon;
        if (!isTranslation)
        {
            return new Rect();
        }

        Rect row = renderTable.GetRowBox(rowIndex);
        return row.Translate(new Vector(-origin.X, -origin.Y));
    }

    private static Matrix4 ResolveChildTransform(RenderObject child, RenderObject parent)
    {
        Point childOffset = child.parentData is BoxParentData data ? data.offset : default;
        Matrix4 transform = Matrix4.TranslationValues(childOffset.X, childOffset.Y, 0.0);
        if (parent is RenderTransform renderTransform)
        {
            transform.Multiply(renderTransform.Transform);
        }

        return transform;
    }
}

internal enum InkHighlightKind
{
    Pressed,
    Hover,
    Focus,
}

internal sealed record InkSplashVisual(
    InteractiveInkFeature Feature,
    double Progress,
    bool Confirmed,
    bool Canceled);

internal sealed record InkHighlightVisual(
    InkHighlightKind Kind,
    Color Color,
    double Opacity);

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
        Func<RenderBox, Func<Rect>?> rectCallbackFactory,
        Widget child,
        MaterialInkController? controller = null,
        IReadOnlyList<InkSplashVisual>? splashes = null,
        IReadOnlyList<InkHighlightVisual>? highlights = null) : base(child)
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
        RectCallbackFactory = rectCallbackFactory
                              ?? throw new ArgumentNullException(nameof(rectCallbackFactory));
        Controller = controller;
        Splashes = splashes;
        Highlights = highlights;
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
    public Func<RenderBox, Func<Rect>?> RectCallbackFactory { get; }
    public MaterialInkController? Controller { get; }
    public IReadOnlyList<InkSplashVisual>? Splashes { get; }
    public IReadOnlyList<InkHighlightVisual>? Highlights { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var paint = new RenderInkResponsePaint(
            HighlightColor,
            HighlightShape,
            BorderRadius,
            SplashColor,
            SplashOrigin,
            SplashProgress,
            SplashRadius,
            ContainedInkWell,
            SplashFeature,
            SplashConfirmed,
            SplashCanceled,
            Controller,
            Splashes,
            Highlights);
        paint.RectCallback = RectCallbackFactory(paint);
        return paint;
    }

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
        paint.Controller = Controller;
        paint.Splashes = Splashes;
        paint.Highlights = Highlights;
        paint.RectCallback = RectCallbackFactory(paint);
    }
}

internal sealed class RenderInkResponsePaint : RenderProxyBox, IMaterialInkFeature
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
    private Func<Rect>? _rectCallback;
    private MaterialInkController? _controller;
    private IReadOnlyList<InkSplashVisual>? _splashes;
    private IReadOnlyList<InkHighlightVisual>? _highlights;

    public RenderInkResponsePaint(Color? highlightColor, BoxShape highlightShape, BorderRadius borderRadius,
        Color? splashColor,
        Point splashOrigin,
        double splashProgress,
        double? splashRadius,
        bool containedInkWell,
        InteractiveInkFeature? splashFeature,
        bool splashConfirmed,
        bool splashCanceled,
        MaterialInkController? controller = null,
        IReadOnlyList<InkSplashVisual>? splashes = null,
        IReadOnlyList<InkHighlightVisual>? highlights = null)
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
        _controller = controller;
        _splashes = splashes;
        _highlights = highlights;
        _controller?.AddInkFeature(this);
    }

    public Color? HighlightColor
    {
        get => _highlights?.FirstOrDefault(highlight => highlight.Kind == InkHighlightKind.Pressed)?.Color
               ?? _highlights?.FirstOrDefault(highlight => highlight.Kind == InkHighlightKind.Hover)?.Color
               ?? _highlights?.FirstOrDefault(highlight => highlight.Kind == InkHighlightKind.Focus)?.Color
               ?? _highlightColor;
        set => SetPaintValue(ref _highlightColor, value);
    }
    public BoxShape HighlightShape { get => _highlightShape; set => SetPaintValue(ref _highlightShape, value); }
    public BorderRadius BorderRadius { get => _borderRadius; set => SetPaintValue(ref _borderRadius, value); }
    public Color? SplashColor { get => _splashColor; set => SetPaintValue(ref _splashColor, value); }
    public Point SplashOrigin { get => _splashOrigin; set => SetPaintValue(ref _splashOrigin, value); }
    public double SplashProgress { get => _splashProgress; set => SetPaintValue(ref _splashProgress, Math.Clamp(value, 0, 1)); }
    public double? SplashRadius { get => _splashRadius; set => SetPaintValue(ref _splashRadius, value); }
    public bool ContainedInkWell { get => _containedInkWell; set => SetPaintValue(ref _containedInkWell, value); }
    public InteractiveInkFeature? SplashFeature
    {
        get => _splashes?.LastOrDefault()?.Feature ?? _splashFeature;
        set => SetPaintValue(ref _splashFeature, value);
    }
    public bool SplashConfirmed { get => _splashConfirmed; set => SetPaintValue(ref _splashConfirmed, value); }
    public bool SplashCanceled { get => _splashCanceled; set => SetPaintValue(ref _splashCanceled, value); }
    public Func<Rect>? RectCallback
    {
        get => _rectCallback;
        set => SetPaintValue(ref _rectCallback, value);
    }
    public MaterialInkController? Controller
    {
        get => _controller;
        set
        {
            if (ReferenceEquals(_controller, value))
            {
                return;
            }

            _controller?.RemoveInkFeature(this);
            _controller = value;
            _controller?.AddInkFeature(this);
            MarkNeedsPaint();
        }
    }
    public IReadOnlyList<InkSplashVisual>? Splashes
    {
        get => _splashes;
        set => SetPaintValue(ref _splashes, value);
    }
    public IReadOnlyList<InkHighlightVisual>? Highlights
    {
        get => _highlights;
        set => SetPaintValue(ref _highlights, value);
    }

    RenderBox IMaterialInkFeature.ReferenceBox => this;

    internal int SplashCount => _splashes?.Count ?? (_splashFeature is null ? 0 : 1);

    internal Rect ResolvedInkRect => ResolveInkRect();

    public override void Paint(PaintingContext context, Point offset)
    {
        if (_controller is not null)
        {
            base.Paint(context, offset);
            return;
        }

        PaintInk(context, offset);
        base.Paint(context, offset);
    }

    void IMaterialInkFeature.PaintFeature(PaintingContext context)
    {
        PaintInk(context, default);
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _controller?.AddInkFeature(this);
    }

    protected override void OnDetach()
    {
        _controller?.RemoveInkFeature(this);
        base.OnDetach();
    }

    private void PaintInk(PaintingContext context, Point offset)
    {
        Rect inkRect = ResolveInkRect();

        void PaintInk(PaintingContext target)
        {
            if (_highlights is not null)
            {
                foreach (InkHighlightVisual highlight in _highlights.OrderBy(HighlightPaintOrder))
                {
                    PaintHighlight(
                        target,
                        offset,
                        inkRect,
                        ApplyOpacity(highlight.Color, highlight.Opacity));
                }
            }
            else if (_highlightColor.HasValue)
            {
                PaintHighlight(target, offset, inkRect, _highlightColor.Value);
            }

            if (_splashes is not null)
            {
                foreach (InkSplashVisual splash in _splashes)
                {
                    InkFeatureFrame frame = splash.Feature.ResolveFrame(
                        inkRect,
                        splash.Progress,
                        splash.Confirmed,
                        splash.Canceled);
                    PaintFeature(target, offset, splash.Feature.Configuration.Color, frame);
                }
            }
            else if (_splashFeature is not null && _splashProgress >= 0.0)
            {
                InkFeatureFrame frame = _splashFeature.ResolveFrame(
                    inkRect,
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
                target.Canvas.DrawCircle(
                    new SolidColorBrush(_splashColor.Value),
                    null,
                    offset + origin,
                    maxRadius * _splashProgress);
            }
        }

        if (_containedInkWell)
        {
            context.Canvas.Save();
            if (_highlightShape == BoxShape.Circle)
            {
                var ovalPath = new Plumix.UI.Path();
                ovalPath.AddOval(new Rect(offset, Size));
                context.Canvas.ClipPath(ovalPath);
            }
            else
            {
                context.Canvas.ClipRRect(
                    RRect.FromRectAndCorners(inkRect.Translate((Vector)offset), _borderRadius));
            }

            PaintInk(context);
            context.Canvas.Restore();
        }
        else
        {
            PaintInk(context);
        }
    }

    private void PaintHighlight(PaintingContext context, Point offset, Rect inkRect, Color color)
    {
        var brush = new SolidColorBrush(color);
        if (_highlightShape == BoxShape.Circle)
        {
            double radius = _splashRadius ?? 35.0;
            context.Canvas.DrawCircle(
                brush,
                null,
                offset + inkRect.Center,
                radius);
            return;
        }

        context.Canvas.DrawRectangle(
            brush,
            null,
            inkRect.Translate((Vector)offset),
            _borderRadius);
    }

    private static int HighlightPaintOrder(InkHighlightVisual highlight)
    {
        return highlight.Kind switch
        {
            InkHighlightKind.Focus => 0,
            InkHighlightKind.Hover => 1,
            _ => 2,
        };
    }

    private Rect ResolveInkRect()
    {
        Rect rect = _rectCallback?.Invoke() ?? new Rect(Size);
        return rect.Width < 0.0 || rect.Height < 0.0 ? new Rect() : rect;
    }

    private static void PaintFeature(
        PaintingContext context,
        Point offset,
        Color color,
        InkFeatureFrame frame)
    {
        if (frame.Kind == InkFeatureKind.None)
        {
            return;
        }

        Color featureColor = ApplyOpacity(color, frame.Opacity);
        if (frame.Kind != InkFeatureKind.Sparkle)
        {
            context.Canvas.DrawCircle(
                new SolidColorBrush(featureColor),
                null,
                offset + frame.Center,
                frame.Radius);
            return;
        }

        context.Canvas.DrawCircle(
            new SolidColorBrush(featureColor),
            null,
            offset + frame.Center,
            frame.Radius);
        Color haloColor = ApplyOpacity(color, frame.Opacity * 0.32);
        context.Canvas.DrawCircle(
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
            context.Canvas.DrawCircle(sparkleBrush, null, offset + dotCenter, dotRadius);
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
