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
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private bool _hasFocus;
        private bool _isHovered;

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
            }

            if (CurrentWidget.OnChanged is null && _dragPosition.HasValue)
            {
                _dragPosition = null;
            }

            if (CurrentWidget.OnChanged is null && _isHovered)
            {
                _isHovered = false;
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
            var config = ResolveConfig(theme.UseMaterial3);
            var enabled = CurrentWidget.OnChanged is not null;

            // Cupertino switch primitives are not yet in framework scope.
            var isAdaptive = CurrentWidget._switchType == SwitchType.Adaptive;
            _ = isAdaptive;

            var effectivePadding = ResolvePadding(theme, switchTheme);
            var totalWidth = config.BaseWidth + effectivePadding.Left + effectivePadding.Right;
            var totalHeight = config.BaseHeight + effectivePadding.Top + effectivePadding.Bottom;
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? switchTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            var splashRadius = ResolveSplashRadius(switchTheme);

            var activeStates = BuildVisualStates(enabled, selected: true);
            var inactiveStates = BuildVisualStates(enabled, selected: false);
            var position = CurrentPosition();

            var activeThumbColor = ResolveThumbColor(theme, switchTheme, activeStates);
            var inactiveThumbColor = ResolveThumbColor(theme, switchTheme, inactiveStates);
            var thumbColor = LerpColor(inactiveThumbColor, activeThumbColor, position);

            var activeTrackColor = ResolveTrackColor(theme, switchTheme, activeStates);
            var inactiveTrackColor = ResolveTrackColor(theme, switchTheme, inactiveStates);
            var trackColor = LerpColor(inactiveTrackColor, activeTrackColor, position);

            var activeOutline = ResolveTrackOutlineSide(theme, switchTheme, activeStates);
            var inactiveOutline = ResolveTrackOutlineSide(theme, switchTheme, inactiveStates);
            var trackOutline = LerpSide(inactiveOutline, activeOutline, position);

            var activeIcon = ResolveThumbIcon(switchTheme, activeStates);
            var inactiveIcon = ResolveThumbIcon(switchTheme, inactiveStates);
            var activeIconColor = ResolveThumbIconColor(theme, activeStates);
            var inactiveIconColor = ResolveThumbIconColor(theme, inactiveStates);
            var iconColor = LerpColor(inactiveIconColor, activeIconColor, position);
            var currentIcon = position < 0.5 ? inactiveIcon : activeIcon;

            var activeThumbDiameter = activeIcon is null
                ? config.ActiveThumbDiameter
                : config.ThumbDiameterWithIcon;
            var inactiveThumbDiameter = inactiveIcon is null
                ? config.InactiveThumbDiameter
                : config.ThumbDiameterWithIcon;
            var thumbDiameter = LerpDouble(inactiveThumbDiameter, activeThumbDiameter, position);

            Widget thumbChild = new SizedBox(width: thumbDiameter, height: thumbDiameter);
            if (currentIcon is not null)
            {
                thumbChild = new Center(
                    child: new IconTheme(
                        data: new IconThemeData(
                            Color: iconColor,
                            Size: config.IconSize),
                        child: currentIcon));
            }

            var thumb = new Container(
                width: thumbDiameter,
                height: thumbDiameter,
                decoration: new BoxDecoration(
                    Color: thumbColor,
                    BorderRadius: BorderRadius.Circular(thumbDiameter / 2)),
                child: thumbChild);

            var track = new Container(
                width: config.TrackWidth,
                height: config.TrackHeight,
                decoration: new BoxDecoration(
                    Color: trackColor,
                    Border: trackOutline,
                    BorderRadius: BorderRadius.Circular(config.TrackHeight / 2)),
                child: new Align(
                    alignment: new Alignment((position * 2) - 1, 0),
                    child: thumb));

            Widget child = new SizedBox(
                width: totalWidth,
                height: totalHeight,
                child: new Padding(
                    effectivePadding,
                    new Center(child: track)));

            var style = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveOverlayColor(theme, switchTheme, states)),
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

            return new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onHorizontalDragStart: HandleDragStart,
                onHorizontalDragUpdate: HandleDragUpdate,
                onHorizontalDragEnd: HandleDragEnd,
                child: button);
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

        private void HandleDragStart(DragStartDetails details)
        {
            if (CurrentWidget.OnChanged is null)
            {
                return;
            }

            _positionController?.Stop();
            SetState(() => _dragPosition = CurrentPosition());
        }

        private void HandleDragUpdate(DragUpdateDetails details)
        {
            if (!(_dragPosition.HasValue && CurrentWidget.OnChanged is not null))
            {
                return;
            }

            var theme = Theme.Of(Context);
            var config = ResolveConfig(theme.UseMaterial3);
            var trackInnerLength = Math.Max(1.0, config.TrackWidth - config.TrackHeight);
            var direction = Directionality.Of(Context);
            var directionMultiplier = direction == TextDirection.Rtl ? -1 : 1;
            var next = _dragPosition.Value + ((details.PrimaryDelta / trackInnerLength) * directionMultiplier);
            SetState(() => _dragPosition = Math.Clamp(next, 0, 1));
        }

        private void HandleDragEnd(DragEndDetails details)
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

            return states;
        }

        private Color ResolveThumbColor(ThemeData theme, SwitchThemeData switchTheme, MaterialState states)
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

                if (CurrentWidget.ActiveColor.HasValue)
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

            return ResolveDefaultThumbColor(theme, states);
        }

        private Color ResolveTrackColor(ThemeData theme, SwitchThemeData switchTheme, MaterialState states)
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

            return ResolveDefaultTrackColor(theme, states);
        }

        private BorderSide? ResolveTrackOutlineSide(ThemeData theme, SwitchThemeData switchTheme, MaterialState states)
        {
            var outlineColor = CurrentWidget.TrackOutlineColor?.Resolve(states)
                               ?? switchTheme.TrackOutlineColor?.Resolve(states)
                               ?? ResolveDefaultTrackOutlineColor(theme, states);
            var outlineWidth = CurrentWidget.TrackOutlineWidth?.Resolve(states)
                               ?? switchTheme.TrackOutlineWidth?.Resolve(states)
                               ?? ResolveDefaultTrackOutlineWidth(theme, states);

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

        private Color ResolveThumbIconColor(ThemeData theme, MaterialState states)
        {
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

        private Color? ResolveOverlayColor(ThemeData theme, SwitchThemeData switchTheme, MaterialState states)
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

            return ResolveDefaultOverlayColor(theme, states);
        }

        private double ResolveSplashRadius(SwitchThemeData switchTheme)
        {
            var resolved = CurrentWidget.SplashRadius
                           ?? switchTheme.SplashRadius
                           ?? DefaultSplashRadius;
            return NormalizePositiveValue(resolved, DefaultSplashRadius);
        }

        private Thickness ResolvePadding(ThemeData theme, SwitchThemeData switchTheme)
        {
            var fallback = theme.UseMaterial3 ? new Thickness(4, 0, 4, 0) : default;
            var source = CurrentWidget.Padding ?? switchTheme.Padding ?? fallback;
            return NormalizePadding(source);
        }

        private static SwitchConfig ResolveConfig(bool useMaterial3)
        {
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

        private static Color ResolveDefaultThumbColor(ThemeData theme, MaterialState states)
        {
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

        private static Color ResolveDefaultTrackColor(ThemeData theme, MaterialState states)
        {
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

        private static Color? ResolveDefaultTrackOutlineColor(ThemeData theme, MaterialState states)
        {
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

        private static double? ResolveDefaultTrackOutlineWidth(ThemeData theme, MaterialState states)
        {
            return theme.UseMaterial3 ? 2.0 : 0.0;
        }

        private static Color? ResolveDefaultOverlayColor(ThemeData theme, MaterialState states)
        {
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
