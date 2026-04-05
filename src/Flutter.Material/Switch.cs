using Avalonia;
using Avalonia.Media;
using Flutter;
using Flutter.Foundation;
using Flutter.Gestures;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/switch.dart (approximate)

public sealed class Switch : StatefulWidget
{
    private const double DefaultSplashRadius = 20.0;
    private const double CupertinoDisabledOpacity = 0.5;
    private const double CupertinoThumbExtension = 7.0;
    private const double CupertinoDragCommitThreshold = 0.7;
    private const double CupertinoDragReverseThreshold = 0.2;
    private static readonly Color CupertinoInactiveTrackColor = Color.FromArgb(0x52, 0x78, 0x78, 0x80);
    private static readonly BoxShadows CupertinoThumbShadows = new(
        new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 3,
            Blur = 8,
            Spread = 0,
            Color = Color.FromArgb(0x26, 0x00, 0x00, 0x00),
            IsInset = false
        },
        [
            new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 3,
                Blur = 1,
                Spread = 0,
                Color = Color.FromArgb(0x0F, 0x00, 0x00, 0x00),
                IsInset = false
            }
        ]);
    private readonly SwitchType _switchType;

    private enum SwitchType
    {
        Material,
        Adaptive
    }

    public Switch(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<double?>? trackOutlineWidth = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        double? splashRadius = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        Thickness? padding = null,
        string? semanticLabel = null,
        Key? key = null) : this(
            value: value,
            onChanged: onChanged,
            activeColor: activeColor,
            activeThumbColor: activeThumbColor,
            activeTrackColor: activeTrackColor,
            inactiveThumbColor: inactiveThumbColor,
            inactiveTrackColor: inactiveTrackColor,
            thumbColor: thumbColor,
            trackColor: trackColor,
            trackOutlineColor: trackOutlineColor,
            trackOutlineWidth: trackOutlineWidth,
            thumbIcon: thumbIcon,
            materialTapTargetSize: materialTapTargetSize,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashRadius: splashRadius,
            focusNode: focusNode,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            padding: padding,
            semanticLabel: semanticLabel,
            switchType: SwitchType.Material,
            key: key)
    {
    }

    private Switch(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor,
        Color? activeThumbColor,
        Color? activeTrackColor,
        Color? inactiveThumbColor,
        Color? inactiveTrackColor,
        MaterialStateProperty<Color?>? thumbColor,
        MaterialStateProperty<Color?>? trackColor,
        MaterialStateProperty<Color?>? trackOutlineColor,
        MaterialStateProperty<double?>? trackOutlineWidth,
        MaterialStateProperty<Icon?>? thumbIcon,
        MaterialTapTargetSize? materialTapTargetSize,
        MaterialStateProperty<Color?>? overlayColor,
        Color? focusColor,
        Color? hoverColor,
        double? splashRadius,
        FocusNode? focusNode,
        Action<bool>? onFocusChange,
        bool autofocus,
        Thickness? padding,
        string? semanticLabel,
        SwitchType switchType,
        Key? key = null) : base(key)
    {
        Value = value;
        OnChanged = onChanged;
        ActiveColor = activeColor;
        ActiveThumbColor = activeThumbColor;
        ActiveTrackColor = activeTrackColor;
        InactiveThumbColor = inactiveThumbColor;
        InactiveTrackColor = inactiveTrackColor;
        ThumbColor = thumbColor;
        TrackColor = trackColor;
        TrackOutlineColor = trackOutlineColor;
        TrackOutlineWidth = trackOutlineWidth;
        ThumbIcon = thumbIcon;
        MaterialTapTargetSize = materialTapTargetSize;
        OverlayColor = overlayColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        SplashRadius = splashRadius;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        Padding = padding;
        SemanticLabel = semanticLabel;
        _switchType = switchType;
    }

    public bool Value { get; }

    public Action<bool>? OnChanged { get; }

    public Color? ActiveColor { get; }

    public Color? ActiveThumbColor { get; }

    public Color? ActiveTrackColor { get; }

    public Color? InactiveThumbColor { get; }

    public Color? InactiveTrackColor { get; }

    public MaterialStateProperty<Color?>? ThumbColor { get; }

    public MaterialStateProperty<Color?>? TrackColor { get; }

    public MaterialStateProperty<Color?>? TrackOutlineColor { get; }

    public MaterialStateProperty<double?>? TrackOutlineWidth { get; }

    public MaterialStateProperty<Icon?>? ThumbIcon { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public double? SplashRadius { get; }

    public FocusNode? FocusNode { get; }

    public Action<bool>? OnFocusChange { get; }

    public bool Autofocus { get; }

    public Thickness? Padding { get; }

    public string? SemanticLabel { get; }

    public static Switch Adaptive(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<double?>? trackOutlineWidth = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        double? splashRadius = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        Thickness? padding = null,
        string? semanticLabel = null,
        Key? key = null)
    {
        return new Switch(
            value: value,
            onChanged: onChanged,
            activeColor: activeColor,
            activeThumbColor: activeThumbColor,
            activeTrackColor: activeTrackColor,
            inactiveThumbColor: inactiveThumbColor,
            inactiveTrackColor: inactiveTrackColor,
            thumbColor: thumbColor,
            trackColor: trackColor,
            trackOutlineColor: trackOutlineColor,
            trackOutlineWidth: trackOutlineWidth,
            thumbIcon: thumbIcon,
            materialTapTargetSize: materialTapTargetSize,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashRadius: splashRadius,
            focusNode: focusNode,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            padding: padding,
            semanticLabel: semanticLabel,
            switchType: SwitchType.Adaptive,
            key: key);
    }

    public override State CreateState()
    {
        return new SwitchState();
    }

    private sealed class SwitchState : State
    {
        private AnimationController? _positionController;
        private double _fromPosition;
        private double _toPosition;
        private double _animatedPosition;
        private double? _dragPosition;
        private bool? _adaptiveDragValue;
        private double _adaptiveDragDelta;
        private Point _pointerDownPosition;
        private bool _hasPointerDownPosition;
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private bool _hasFocus;
        private bool _isHovered;
        private bool _isPressed;

        private Switch CurrentWidget => (Switch)StateWidget;

        public override void InitState()
        {
            _animatedPosition = CurrentWidget.Value ? 1.0 : 0.0;
            _fromPosition = _animatedPosition;
            _toPosition = _animatedPosition;

            _positionController = new AnimationController(TimeSpan.FromMilliseconds(220))
            {
                Curve = Curves.EaseInOut
            };
            _positionController.Changed += HandlePositionTick;
            _positionController.Completed += HandlePositionCompleted;

            AttachFocusNode(CurrentWidget.FocusNode);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldSwitch = (Switch)oldWidget;
            if (!ReferenceEquals(oldSwitch.FocusNode, CurrentWidget.FocusNode))
            {
                DetachFocusNode(disposeOwned: true);
                AttachFocusNode(CurrentWidget.FocusNode);
            }

            if (oldSwitch.Value != CurrentWidget.Value)
            {
                AnimateTo(CurrentWidget.Value);
                _adaptiveDragValue = null;
                _adaptiveDragDelta = 0;
                _hasPointerDownPosition = false;
            }

            if (CurrentWidget.OnChanged is null && _dragPosition.HasValue)
            {
                _dragPosition = null;
            }

            if (CurrentWidget.OnChanged is null && _adaptiveDragValue.HasValue)
            {
                _adaptiveDragValue = null;
                _adaptiveDragDelta = 0;
                _hasPointerDownPosition = false;
            }

            if (CurrentWidget.OnChanged is null && _isHovered)
            {
                _isHovered = false;
            }

            if (CurrentWidget.OnChanged is null && _isPressed)
            {
                _isPressed = false;
            }
        }

        public override void Dispose()
        {
            DetachFocusNode(disposeOwned: true);

            if (_positionController != null)
            {
                _positionController.Changed -= HandlePositionTick;
                _positionController.Completed -= HandlePositionCompleted;
                _positionController.Dispose();
                _positionController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var switchTheme = SwitchTheme.Of(context);
            var isCupertinoAdaptive = IsAdaptiveCupertino(theme);
            var config = ResolveConfig(theme.UseMaterial3, isCupertinoAdaptive);
            var enabled = CurrentWidget.OnChanged is not null;

            var effectivePadding = ResolvePadding(theme, switchTheme, isCupertinoAdaptive);
            var totalWidth = config.BaseWidth + effectivePadding.Left + effectivePadding.Right;
            var totalHeight = config.BaseHeight + effectivePadding.Top + effectivePadding.Bottom;
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? switchTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            var splashRadius = ResolveSplashRadius(switchTheme, isCupertinoAdaptive);

            var activeStates = BuildVisualStates(enabled, selected: true);
            var inactiveStates = BuildVisualStates(enabled, selected: false);
            var position = CurrentPosition();
            var selectedStates = CurrentWidget.Value ? activeStates : inactiveStates;

            var activeThumbColor = ResolveThumbColor(theme, switchTheme, activeStates, isCupertinoAdaptive);
            var inactiveThumbColor = ResolveThumbColor(theme, switchTheme, inactiveStates, isCupertinoAdaptive);
            var thumbColor = LerpColor(inactiveThumbColor, activeThumbColor, position);

            var activeTrackColor = ResolveTrackColor(theme, switchTheme, activeStates, isCupertinoAdaptive);
            var inactiveTrackColor = ResolveTrackColor(theme, switchTheme, inactiveStates, isCupertinoAdaptive);
            var trackColor = LerpColor(inactiveTrackColor, activeTrackColor, position);

            var activeOutline = ResolveTrackOutlineSide(theme, switchTheme, activeStates, isCupertinoAdaptive);
            var inactiveOutline = ResolveTrackOutlineSide(theme, switchTheme, inactiveStates, isCupertinoAdaptive);
            var trackOutline = LerpSide(inactiveOutline, activeOutline, position);

            var activeIcon = ResolveThumbIcon(switchTheme, activeStates);
            var inactiveIcon = ResolveThumbIcon(switchTheme, inactiveStates);
            var activeIconColor = ResolveThumbIconColor(theme, activeStates, isCupertinoAdaptive);
            var inactiveIconColor = ResolveThumbIconColor(theme, inactiveStates, isCupertinoAdaptive);
            var iconColor = LerpColor(inactiveIconColor, activeIconColor, position);
            var currentIcon = position < 0.5 ? inactiveIcon : activeIcon;
            var overlayColor = ResolveOverlayColor(theme, switchTheme, selectedStates, isCupertinoAdaptive);

            var activeThumbDiameter = activeIcon is null
                ? config.ActiveThumbDiameter
                : config.ThumbDiameterWithIcon;
            var inactiveThumbDiameter = inactiveIcon is null
                ? config.InactiveThumbDiameter
                : config.ThumbDiameterWithIcon;
            var thumbDiameter = LerpDouble(inactiveThumbDiameter, activeThumbDiameter, position);
            var thumbWidth = isCupertinoAdaptive
                ? ResolveCupertinoThumbWidth(thumbDiameter, enabled)
                : thumbDiameter;

            Widget thumbChild = new SizedBox(width: thumbWidth, height: thumbDiameter);
            if (currentIcon is not null)
            {
                thumbChild = new SizedBox(
                    width: thumbWidth,
                    height: thumbDiameter,
                    child: new Center(
                        child: new IconTheme(
                            data: new IconThemeData(
                                Color: iconColor,
                                Size: config.IconSize),
                            child: currentIcon)));
            }

            var thumb = new Container(
                width: thumbWidth,
                height: thumbDiameter,
                decoration: new BoxDecoration(
                    Color: thumbColor,
                    BorderRadius: BorderRadius.Circular(thumbDiameter / 2),
                    BoxShadows: isCupertinoAdaptive ? CupertinoThumbShadows : null),
                child: thumbChild);

            Widget trackBody = new Align(
                alignment: new Alignment((position * 2) - 1, 0),
                child: thumb);

            if (isCupertinoAdaptive && overlayColor.HasValue && overlayColor.Value.A > 0)
            {
                trackBody = new Stack(
                    alignment: Alignment.Center,
                    children:
                    [
                        trackBody,
                        new Container(
                            width: config.TrackWidth,
                            height: config.TrackHeight,
                            color: overlayColor.Value)
                    ]);
            }

            if (isCupertinoAdaptive)
            {
                trackBody = new ClipRRect(
                    borderRadius: BorderRadius.Circular(config.TrackHeight / 2),
                    child: trackBody);
            }

            var track = new Container(
                width: config.TrackWidth,
                height: config.TrackHeight,
                decoration: new BoxDecoration(
                    Color: trackColor,
                    Border: trackOutline,
                    BorderRadius: BorderRadius.Circular(config.TrackHeight / 2)),
                child: trackBody);

            Widget child = new SizedBox(
                width: totalWidth,
                height: totalHeight,
                child: new Padding(
                    effectivePadding,
                    new Center(child: track)));

            if (isCupertinoAdaptive)
            {
                Widget adaptiveResult = new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: enabled ? HandleTap : null,
                    onHorizontalDragStart: enabled ? HandleAdaptiveDragStart : null,
                    onHorizontalDragUpdate: enabled ? HandleAdaptiveDragUpdate : null,
                    onHorizontalDragEnd: enabled ? HandleAdaptiveDragEnd : null,
                    child: new Listener(
                        behavior: HitTestBehavior.Opaque,
                        onPointerDown: enabled ? HandlePointerDown : null,
                        onPointerUp: enabled ? HandlePointerUp : null,
                        onPointerCancel: enabled ? HandlePointerCancel : null,
                        onPointerEnter: enabled ? _ => HandleHoverChanged(true) : null,
                        onPointerExit: enabled ? _ => HandleHoverChanged(false) : null,
                        child: child));

                adaptiveResult = new Focus(
                    child: adaptiveResult,
                    focusNode: _focusNode,
                    autofocus: CurrentWidget.Autofocus,
                    canRequestFocus: enabled,
                    onKeyEvent: HandleKeyEvent);

                if (!enabled)
                {
                    adaptiveResult = new Opacity(CupertinoDisabledOpacity, adaptiveResult);
                }

                return adaptiveResult;
            }

            var style = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveOverlayColor(theme, switchTheme, states, isCupertinoAdaptive: false)),
                SplashColor: null,
                Elevation: MaterialStateProperty<double?>.All(0),
                IconColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                IconSize: MaterialStateProperty<double?>.All(config.IconSize),
                Side: MaterialStateProperty<BorderSide?>.All(null),
                Padding: MaterialStateProperty<Thickness?>.All(new Thickness(0)),
                Shape: MaterialStateProperty<BorderRadius?>.All(BorderRadius.Circular(totalHeight / 2)),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(totalWidth, totalHeight)),
                FixedSize: MaterialStateProperty<Size?>.All(new Size(totalWidth, totalHeight)),
                MaximumSize: MaterialStateProperty<Size?>.All(new Size(totalWidth, totalHeight)),
                Alignment: Alignment.Center,
                TapTargetSize: tapTargetSize);

            var button = new MaterialButtonCore(
                child: child,
                onPressed: enabled ? HandleTap : null,
                style: style,
                onHoverChanged: HandleHoverChanged,
                focusNode: _focusNode,
                isSelected: CurrentWidget.Value,
                splashRadius: splashRadius,
                autofocus: CurrentWidget.Autofocus);

            Widget result = new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onHorizontalDragStart: HandleMaterialDragStart,
                onHorizontalDragUpdate: HandleMaterialDragUpdate,
                onHorizontalDragEnd: HandleMaterialDragEnd,
                child: button);

            return result;
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

        private void HandleFocusChanged()
        {
            var hasFocus = _focusNode?.HasFocus ?? false;
            if (_hasFocus == hasFocus)
            {
                return;
            }

            SetState(() => _hasFocus = hasFocus);
            CurrentWidget.OnFocusChange?.Invoke(hasFocus);
        }

        private void HandleHoverChanged(bool hovered)
        {
            if (_isHovered == hovered)
            {
                return;
            }

            SetState(() => _isHovered = hovered);
        }

        private void HandleTap()
        {
            CurrentWidget.OnChanged?.Invoke(!CurrentWidget.Value);
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            if (CurrentWidget.OnChanged is null)
            {
                return;
            }

            _pointerDownPosition = @event.Position;
            _hasPointerDownPosition = true;
            SetPressed(true);
        }

        private void HandlePointerUp(PointerUpEvent @event)
        {
            _hasPointerDownPosition = false;
            SetPressed(false);
        }

        private void HandlePointerCancel(PointerCancelEvent @event)
        {
            _hasPointerDownPosition = false;
            _adaptiveDragDelta = 0;
            _adaptiveDragValue = null;
            SetPressed(false);
            AnimateTo(CurrentWidget.Value);
        }

        private void SetPressed(bool pressed)
        {
            if (_isPressed == pressed)
            {
                return;
            }

            SetState(() => _isPressed = pressed);
        }

        private void HandleMaterialDragStart(DragStartDetails details)
        {
            if (CurrentWidget.OnChanged is null)
            {
                return;
            }

            _positionController?.Stop();
            SetState(() => _dragPosition = CurrentPosition());
        }

        private void HandleMaterialDragUpdate(DragUpdateDetails details)
        {
            if (!(_dragPosition.HasValue && CurrentWidget.OnChanged is not null))
            {
                return;
            }

            var theme = Theme.Of(Context);
            var config = ResolveConfig(theme.UseMaterial3, IsAdaptiveCupertino(theme));
            var trackInnerLength = Math.Max(1.0, config.TrackWidth - config.TrackHeight);
            var direction = Directionality.Of(Context);
            var directionMultiplier = direction == TextDirection.Rtl ? -1 : 1;
            var next = _dragPosition.Value + ((details.PrimaryDelta / trackInnerLength) * directionMultiplier);
            SetState(() => _dragPosition = Math.Clamp(next, 0, 1));
        }

        private void HandleMaterialDragEnd(DragEndDetails details)
        {
            if (!(_dragPosition.HasValue && CurrentWidget.OnChanged is not null))
            {
                return;
            }

            var from = Math.Clamp(_dragPosition.Value, 0, 1);
            var nextValue = from >= 0.5;
            SetState(() => _dragPosition = null);

            if (nextValue != CurrentWidget.Value)
            {
                CurrentWidget.OnChanged?.Invoke(nextValue);
            }

            AnimateTo(CurrentWidget.Value, fromOverride: from);
        }

        private void HandleAdaptiveDragStart(DragStartDetails details)
        {
            if (CurrentWidget.OnChanged is null)
            {
                return;
            }

            _positionController?.Stop();
            var direction = Directionality.Of(Context);
            var directionMultiplier = direction == TextDirection.Rtl ? -1 : 1;
            var initialDelta = 0.0;
            if (_hasPointerDownPosition)
            {
                var theme = Theme.Of(Context);
                var config = ResolveConfig(theme.UseMaterial3, isCupertinoAdaptive: true);
                initialDelta = ((details.GlobalPosition.X - _pointerDownPosition.X) / config.TrackWidth) * directionMultiplier;
            }

            SetState(() =>
            {
                _adaptiveDragValue = CurrentWidget.Value;
                _adaptiveDragDelta = initialDelta;
            });
        }

        private void HandleAdaptiveDragUpdate(DragUpdateDetails details)
        {
            if (!(_adaptiveDragValue.HasValue && CurrentWidget.OnChanged is not null))
            {
                return;
            }

            var theme = Theme.Of(Context);
            var config = ResolveConfig(theme.UseMaterial3, isCupertinoAdaptive: true);
            var direction = Directionality.Of(Context);
            var directionMultiplier = direction == TextDirection.Rtl ? -1 : 1;
            _adaptiveDragDelta += (details.PrimaryDelta / config.TrackWidth) * directionMultiplier;

            var valueChangedWhileDragging = CurrentWidget.Value != _adaptiveDragValue.Value;
            var threshold = valueChangedWhileDragging
                ? CupertinoDragReverseThreshold
                : CupertinoDragCommitThreshold;
            var effectiveThreshold = CurrentWidget.Value ? -threshold : threshold;
            var newDragValue = _adaptiveDragDelta >= effectiveThreshold;

            if (_adaptiveDragValue.Value == newDragValue)
            {
                return;
            }

            _adaptiveDragValue = newDragValue;
            AnimateTo(newDragValue);
        }

        private void HandleAdaptiveDragEnd(DragEndDetails details)
        {
            if (!(_adaptiveDragValue.HasValue && CurrentWidget.OnChanged is not null))
            {
                return;
            }

            var nextValue = _adaptiveDragValue.Value;
            _adaptiveDragValue = null;
            _adaptiveDragDelta = 0;
            _hasPointerDownPosition = false;
            SetPressed(false);

            if (nextValue != CurrentWidget.Value)
            {
                CurrentWidget.OnChanged?.Invoke(nextValue);
            }

            AnimateTo(nextValue);
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!IsActivateKey(@event))
            {
                return KeyEventResult.Ignored;
            }

            if (CurrentWidget.OnChanged is null)
            {
                return KeyEventResult.Handled;
            }

            if (@event.IsDown)
            {
                HandleTap();
            }

            return KeyEventResult.Handled;
        }

        private static bool IsActivateKey(KeyEvent @event)
        {
            if (@event.IsShiftPressed
                || @event.IsControlPressed
                || @event.IsAltPressed
                || @event.IsMetaPressed)
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

        private void AnimateTo(bool value, double? fromOverride = null)
        {
            var target = value ? 1.0 : 0.0;
            var from = Math.Clamp(fromOverride ?? CurrentPosition(), 0, 1);
            _fromPosition = from;
            _toPosition = target;

            if (Math.Abs(_toPosition - _fromPosition) <= 0.0001 || _positionController is null)
            {
                SetState(() => _animatedPosition = _toPosition);
                return;
            }

            _positionController.Forward(0);
        }

        private void HandlePositionTick()
        {
            if (_positionController is null || _dragPosition.HasValue)
            {
                return;
            }

            var t = Math.Clamp(_positionController.Evaluate(), 0, 1);
            SetState(() => _animatedPosition = LerpDouble(_fromPosition, _toPosition, t));
        }

        private void HandlePositionCompleted()
        {
            if (_dragPosition.HasValue)
            {
                return;
            }

            SetState(() => _animatedPosition = _toPosition);
        }

        private double CurrentPosition()
        {
            return Math.Clamp(_dragPosition ?? _animatedPosition, 0, 1);
        }

        private bool IsAdaptiveCupertino(ThemeData theme)
        {
            if (CurrentWidget._switchType != SwitchType.Adaptive)
            {
                return false;
            }

            return theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
        }

        private MaterialState BuildVisualStates(bool enabled, bool selected)
        {
            var states = enabled
                ? MaterialState.None
                : MaterialState.Disabled;

            if (selected)
            {
                states |= MaterialState.Selected;
            }

            if (enabled && _hasFocus)
            {
                states |= MaterialState.Focused;
            }

            if (enabled && _isHovered)
            {
                states |= MaterialState.Hovered;
            }

            if (enabled && _isPressed)
            {
                states |= MaterialState.Pressed;
            }

            return states;
        }

        private Color ResolveThumbColor(ThemeData theme, SwitchThemeData switchTheme, MaterialState states, bool isCupertinoAdaptive)
        {
            var widgetThumb = CurrentWidget.ThumbColor?.Resolve(states);
            if (widgetThumb.HasValue)
            {
                return widgetThumb.Value;
            }

            if (states.HasFlag(MaterialState.Selected))
            {
                if (CurrentWidget.ActiveThumbColor.HasValue)
                {
                    return CurrentWidget.ActiveThumbColor.Value;
                }

                if (CurrentWidget.ActiveColor.HasValue && !isCupertinoAdaptive)
                {
                    return CurrentWidget.ActiveColor.Value;
                }
            }
            else if (CurrentWidget.InactiveThumbColor.HasValue)
            {
                return CurrentWidget.InactiveThumbColor.Value;
            }

            var themedThumb = switchTheme.ThumbColor?.Resolve(states);
            if (themedThumb.HasValue)
            {
                return themedThumb.Value;
            }

            return ResolveDefaultThumbColor(theme, states, isCupertinoAdaptive);
        }

        private Color ResolveTrackColor(ThemeData theme, SwitchThemeData switchTheme, MaterialState states, bool isCupertinoAdaptive)
        {
            var widgetTrack = CurrentWidget.TrackColor?.Resolve(states);
            if (widgetTrack.HasValue)
            {
                return widgetTrack.Value;
            }

            if (states.HasFlag(MaterialState.Selected))
            {
                if (CurrentWidget.ActiveTrackColor.HasValue)
                {
                    return CurrentWidget.ActiveTrackColor.Value;
                }

                if (CurrentWidget.ActiveColor.HasValue && isCupertinoAdaptive)
                {
                    return CurrentWidget.ActiveColor.Value;
                }
            }
            else if (CurrentWidget.InactiveTrackColor.HasValue)
            {
                return CurrentWidget.InactiveTrackColor.Value;
            }

            var themedTrack = switchTheme.TrackColor?.Resolve(states);
            if (themedTrack.HasValue)
            {
                return themedTrack.Value;
            }

            return ResolveDefaultTrackColor(theme, states, isCupertinoAdaptive);
        }

        private BorderSide? ResolveTrackOutlineSide(ThemeData theme, SwitchThemeData switchTheme, MaterialState states, bool isCupertinoAdaptive)
        {
            var outlineColor = CurrentWidget.TrackOutlineColor?.Resolve(states)
                               ?? switchTheme.TrackOutlineColor?.Resolve(states)
                               ?? ResolveDefaultTrackOutlineColor(theme, states, isCupertinoAdaptive);
            var outlineWidth = CurrentWidget.TrackOutlineWidth?.Resolve(states)
                               ?? switchTheme.TrackOutlineWidth?.Resolve(states)
                               ?? ResolveDefaultTrackOutlineWidth(theme, states, isCupertinoAdaptive);

            if (!outlineColor.HasValue || !outlineWidth.HasValue)
            {
                return null;
            }

            var width = NormalizeWidth(outlineWidth.Value);
            if (width <= 0)
            {
                return null;
            }

            return new BorderSide(outlineColor.Value, width);
        }

        private Icon? ResolveThumbIcon(SwitchThemeData switchTheme, MaterialState states)
        {
            return CurrentWidget.ThumbIcon?.Resolve(states)
                   ?? switchTheme.ThumbIcon?.Resolve(states);
        }

        private Color ResolveThumbIconColor(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38);
                }

                return theme.OnPrimaryColor;
            }

            if (!theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38);
                }

                return states.HasFlag(MaterialState.Selected)
                    ? theme.OnPrimaryColor
                    : theme.OnSurfaceColor;
            }

            if (states.HasFlag(MaterialState.Disabled))
            {
                return states.HasFlag(MaterialState.Selected)
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                    : MaterialButtonCore.ApplyOpacity(theme.SurfaceContainerHighestColor, 0.38);
            }

            return states.HasFlag(MaterialState.Selected)
                ? theme.OnPrimaryColor
                : theme.SurfaceContainerHighestColor;
        }

        private Color? ResolveOverlayColor(ThemeData theme, SwitchThemeData switchTheme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return null;
            }

            var widgetOverlay = CurrentWidget.OverlayColor?.Resolve(states);
            if (widgetOverlay.HasValue)
            {
                return widgetOverlay.Value;
            }

            if (states.HasFlag(MaterialState.Hovered) && CurrentWidget.HoverColor.HasValue)
            {
                return CurrentWidget.HoverColor.Value;
            }

            if (states.HasFlag(MaterialState.Focused) && CurrentWidget.FocusColor.HasValue)
            {
                return CurrentWidget.FocusColor.Value;
            }

            var themedOverlay = switchTheme.OverlayColor?.Resolve(states);
            if (themedOverlay.HasValue)
            {
                return themedOverlay.Value;
            }

            return ResolveDefaultOverlayColor(theme, states, isCupertinoAdaptive);
        }

        private double ResolveSplashRadius(SwitchThemeData switchTheme, bool isCupertinoAdaptive)
        {
            var resolved = CurrentWidget.SplashRadius
                           ?? switchTheme.SplashRadius
                           ?? (isCupertinoAdaptive ? 0.0 : DefaultSplashRadius);
            var fallback = isCupertinoAdaptive ? 0.0 : DefaultSplashRadius;
            return NormalizePositiveValue(resolved, fallback);
        }

        private Thickness ResolvePadding(ThemeData theme, SwitchThemeData switchTheme, bool isCupertinoAdaptive)
        {
            var fallback = isCupertinoAdaptive
                ? default
                : theme.UseMaterial3
                    ? new Thickness(4, 0, 4, 0)
                    : default;
            var source = CurrentWidget.Padding ?? switchTheme.Padding ?? fallback;
            return NormalizePadding(source);
        }

        private static SwitchConfig ResolveConfig(bool useMaterial3, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                return new SwitchConfig(
                    BaseWidth: 59,
                    BaseHeight: 39,
                    TrackWidth: 51,
                    TrackHeight: 31,
                    ActiveThumbDiameter: 28,
                    InactiveThumbDiameter: 28,
                    ThumbDiameterWithIcon: 28,
                    IconSize: 16);
            }

            return useMaterial3
                ? new SwitchConfig(
                    BaseWidth: 52,
                    BaseHeight: 40,
                    TrackWidth: 52,
                    TrackHeight: 32,
                    ActiveThumbDiameter: 24,
                    InactiveThumbDiameter: 16,
                    ThumbDiameterWithIcon: 24,
                    IconSize: 16)
                : new SwitchConfig(
                    BaseWidth: 59,
                    BaseHeight: 40,
                    TrackWidth: 33,
                    TrackHeight: 14,
                    ActiveThumbDiameter: 20,
                    InactiveThumbDiameter: 20,
                    ThumbDiameterWithIcon: 20,
                    IconSize: 14);
        }

        private double ResolveCupertinoThumbWidth(double baseDiameter, bool enabled)
        {
            if (!enabled)
            {
                return baseDiameter;
            }

            if (!(_isPressed || _adaptiveDragValue.HasValue))
            {
                return baseDiameter;
            }

            return baseDiameter + CupertinoThumbExtension;
        }

        private static Color ResolveDefaultThumbColor(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                return Colors.White;
            }

            if (theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return states.HasFlag(MaterialState.Selected)
                        ? theme.CanvasColor
                        : MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38);
                }

                return states.HasFlag(MaterialState.Selected)
                    ? theme.OnPrimaryColor
                    : theme.OutlineColor;
            }

            if (states.HasFlag(MaterialState.Disabled))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38);
            }

            return states.HasFlag(MaterialState.Selected)
                ? theme.PrimaryColor
                : theme.CanvasColor;
        }

        private static Color ResolveDefaultTrackColor(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                return states.HasFlag(MaterialState.Selected)
                    ? theme.PrimaryColor
                    : CupertinoInactiveTrackColor;
            }

            if (theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return states.HasFlag(MaterialState.Selected)
                        ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)
                        : MaterialButtonCore.ApplyOpacity(theme.SurfaceContainerHighestColor, 0.12);
                }

                return states.HasFlag(MaterialState.Selected)
                    ? theme.PrimaryColor
                    : theme.SurfaceContainerHighestColor;
            }

            if (states.HasFlag(MaterialState.Disabled))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12);
            }

            return states.HasFlag(MaterialState.Selected)
                ? MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.50)
                : MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.32);
        }

        private static Color? ResolveDefaultTrackOutlineColor(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                return Colors.Transparent;
            }

            if (!theme.UseMaterial3)
            {
                return Colors.Transparent;
            }

            if (states.HasFlag(MaterialState.Selected))
            {
                return Colors.Transparent;
            }

            if (states.HasFlag(MaterialState.Disabled))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12);
            }

            return theme.OutlineColor;
        }

        private static double? ResolveDefaultTrackOutlineWidth(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                return 0.0;
            }

            return theme.UseMaterial3 ? 2.0 : 0.0;
        }

        private static Color? ResolveDefaultOverlayColor(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                if (!states.HasFlag(MaterialState.Focused))
                {
                    return Colors.Transparent;
                }

                return MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.55);
            }

            if (theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Selected))
                {
                    if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
                    {
                        return MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.10);
                    }

                    if (states.HasFlag(MaterialState.Hovered))
                    {
                        return MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.08);
                    }

                    return null;
                }

                if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.10);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.08);
                }

                return null;
            }

            var color = states.HasFlag(MaterialState.Selected)
                ? theme.PrimaryColor
                : theme.OnSurfaceColor;
            if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
            {
                return MaterialButtonCore.ApplyOpacity(color, 0.12);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return MaterialButtonCore.ApplyOpacity(color, 0.08);
            }

            return null;
        }

        private static Thickness NormalizePadding(Thickness padding)
        {
            return new Thickness(
                NormalizeFiniteNonNegative(padding.Left),
                NormalizeFiniteNonNegative(padding.Top),
                NormalizeFiniteNonNegative(padding.Right),
                NormalizeFiniteNonNegative(padding.Bottom));
        }

        private static double NormalizeWidth(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0;
            }

            return Math.Max(0, value);
        }

        private static double NormalizeFiniteNonNegative(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0;
            }

            return Math.Max(0, value);
        }

        private static double NormalizePositiveValue(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                return fallback;
            }

            return value;
        }

        private static Color LerpColor(Color from, Color to, double t)
        {
            var clamped = Math.Clamp(t, 0, 1);
            byte LerpByte(byte start, byte end)
            {
                return (byte)Math.Clamp((int)(start + ((end - start) * clamped)), 0, 255);
            }

            return Color.FromArgb(
                LerpByte(from.A, to.A),
                LerpByte(from.R, to.R),
                LerpByte(from.G, to.G),
                LerpByte(from.B, to.B));
        }

        private static BorderSide? LerpSide(BorderSide? from, BorderSide? to, double t)
        {
            var clamped = Math.Clamp(t, 0, 1);
            if (clamped <= 0.001)
            {
                return from;
            }

            if (clamped >= 0.999)
            {
                return to;
            }

            if (from is null && to is null)
            {
                return null;
            }

            var fromSide = from ?? new BorderSide(Colors.Transparent, 0);
            var toSide = to ?? new BorderSide(Colors.Transparent, 0);
            var width = fromSide.Width + ((toSide.Width - fromSide.Width) * clamped);
            var color = LerpColor(fromSide.Color, toSide.Color, clamped);
            return new BorderSide(color, width);
        }

        private static double LerpDouble(double from, double to, double t)
        {
            var clamped = Math.Clamp(t, 0, 1);
            return from + ((to - from) * clamped);
        }
    }

    private readonly record struct SwitchConfig(
        double BaseWidth,
        double BaseHeight,
        double TrackWidth,
        double TrackHeight,
        double ActiveThumbDiameter,
        double InactiveThumbDiameter,
        double ThumbDiameterWithIcon,
        double IconSize);
}
