using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/switch.dart

public sealed class Switch : StatefulWidget
{
    private const double DefaultSplashRadius = 20.0;
    private const double CupertinoDisabledOpacity = 0.5;
    private const double CupertinoThumbExtension = 7.0;
    private const double CupertinoDragCommitThreshold = 0.7;
    private const double CupertinoDragReverseThreshold = 0.2;
    private static readonly Color CupertinoInactiveTrackColor = Color.FromArgb(0x28, 0x78, 0x78, 0x80);
    private static readonly Color CupertinoInactiveTrackColorDark = Color.FromArgb(0x51, 0x78, 0x78, 0x80);
    private static readonly Color CupertinoActiveTrackColor = Color.FromRgb(0x34, 0xC7, 0x59);
    private static readonly Color CupertinoActiveTrackColorDark = Color.FromRgb(0x30, 0xD1, 0x58);
    private static readonly IReadOnlyList<BoxShadow> CupertinoThumbShadows =
    [
        new BoxShadow(color: Color.FromArgb(0x26, 0x00, 0x00, 0x00), offset: new Point(0, 3), blurRadius: 8),
        new BoxShadow(color: Color.FromArgb(0x0F, 0x00, 0x00, 0x00), offset: new Point(0, 3), blurRadius: 1),
    ];
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
        ImageProvider? activeThumbImage = null,
        ImageErrorListener? onActiveThumbImageError = null,
        ImageProvider? inactiveThumbImage = null,
        ImageErrorListener? onInactiveThumbImageError = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<double?>? trackOutlineWidth = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MouseCursor? mouseCursor = null,
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
            activeThumbImage: activeThumbImage,
            onActiveThumbImageError: onActiveThumbImageError,
            inactiveThumbImage: inactiveThumbImage,
            onInactiveThumbImageError: onInactiveThumbImageError,
            thumbColor: thumbColor,
            trackColor: trackColor,
            trackOutlineColor: trackOutlineColor,
            trackOutlineWidth: trackOutlineWidth,
            thumbIcon: thumbIcon,
            materialTapTargetSize: materialTapTargetSize,
            dragStartBehavior: dragStartBehavior,
            mouseCursor: mouseCursor,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashRadius: splashRadius,
            focusNode: focusNode,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            padding: padding,
            applyCupertinoTheme: false,
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
        ImageProvider? activeThumbImage,
        ImageErrorListener? onActiveThumbImageError,
        ImageProvider? inactiveThumbImage,
        ImageErrorListener? onInactiveThumbImageError,
        MaterialStateProperty<Color?>? thumbColor,
        MaterialStateProperty<Color?>? trackColor,
        MaterialStateProperty<Color?>? trackOutlineColor,
        MaterialStateProperty<double?>? trackOutlineWidth,
        MaterialStateProperty<Icon?>? thumbIcon,
        MaterialTapTargetSize? materialTapTargetSize,
        DragStartBehavior dragStartBehavior,
        MouseCursor? mouseCursor,
        MaterialStateProperty<Color?>? overlayColor,
        Color? focusColor,
        Color? hoverColor,
        double? splashRadius,
        FocusNode? focusNode,
        Action<bool>? onFocusChange,
        bool autofocus,
        Thickness? padding,
        bool? applyCupertinoTheme,
        string? semanticLabel,
        SwitchType switchType,
        Key? key = null) : base(key)
    {
        if (activeThumbImage is null && onActiveThumbImageError is not null)
        {
            throw new ArgumentException(
                "onActiveThumbImageError requires activeThumbImage.",
                nameof(onActiveThumbImageError));
        }

        if (inactiveThumbImage is null && onInactiveThumbImageError is not null)
        {
            throw new ArgumentException(
                "onInactiveThumbImageError requires inactiveThumbImage.",
                nameof(onInactiveThumbImageError));
        }

        Value = value;
        OnChanged = onChanged;
        ActiveColor = activeColor;
        ActiveThumbColor = activeThumbColor;
        ActiveTrackColor = activeTrackColor;
        InactiveThumbColor = inactiveThumbColor;
        InactiveTrackColor = inactiveTrackColor;
        ActiveThumbImage = activeThumbImage;
        OnActiveThumbImageError = onActiveThumbImageError;
        InactiveThumbImage = inactiveThumbImage;
        OnInactiveThumbImageError = onInactiveThumbImageError;
        ThumbColor = thumbColor;
        TrackColor = trackColor;
        TrackOutlineColor = trackOutlineColor;
        TrackOutlineWidth = trackOutlineWidth;
        ThumbIcon = thumbIcon;
        MaterialTapTargetSize = materialTapTargetSize;
        DragStartBehavior = dragStartBehavior;
        MouseCursor = mouseCursor;
        OverlayColor = overlayColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        SplashRadius = splashRadius;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        Padding = padding;
        ApplyCupertinoTheme = applyCupertinoTheme;
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

    public ImageProvider? ActiveThumbImage { get; }

    public ImageErrorListener? OnActiveThumbImageError { get; }

    public ImageProvider? InactiveThumbImage { get; }

    public ImageErrorListener? OnInactiveThumbImageError { get; }

    public MaterialStateProperty<Color?>? ThumbColor { get; }

    public MaterialStateProperty<Color?>? TrackColor { get; }

    public MaterialStateProperty<Color?>? TrackOutlineColor { get; }

    public MaterialStateProperty<double?>? TrackOutlineWidth { get; }

    public MaterialStateProperty<Icon?>? ThumbIcon { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public MouseCursor? MouseCursor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public double? SplashRadius { get; }

    public FocusNode? FocusNode { get; }

    public Action<bool>? OnFocusChange { get; }

    public bool Autofocus { get; }

    public Thickness? Padding { get; }

    public bool? ApplyCupertinoTheme { get; }

    public string? SemanticLabel { get; }

    public static Switch Adaptive(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        ImageProvider? activeThumbImage = null,
        ImageErrorListener? onActiveThumbImageError = null,
        ImageProvider? inactiveThumbImage = null,
        ImageErrorListener? onInactiveThumbImageError = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<double?>? trackOutlineWidth = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MouseCursor? mouseCursor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        double? splashRadius = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        Thickness? padding = null,
        bool? applyCupertinoTheme = null,
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
            activeThumbImage: activeThumbImage,
            onActiveThumbImageError: onActiveThumbImageError,
            inactiveThumbImage: inactiveThumbImage,
            onInactiveThumbImageError: onInactiveThumbImageError,
            thumbColor: thumbColor,
            trackColor: trackColor,
            trackOutlineColor: trackOutlineColor,
            trackOutlineWidth: trackOutlineWidth,
            thumbIcon: thumbIcon,
            materialTapTargetSize: materialTapTargetSize,
            dragStartBehavior: dragStartBehavior,
            mouseCursor: mouseCursor,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashRadius: splashRadius,
            focusNode: focusNode,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            padding: padding,
            applyCupertinoTheme: applyCupertinoTheme,
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

            _positionController = new AnimationController(duration: TimeSpan.FromMilliseconds(220), vsync: this)
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
            bool isCupertinoAdaptive = IsAdaptiveCupertino(theme);
            var config = ResolveConfig(theme.UseMaterial3, isCupertinoAdaptive);
            var sizeConfig = ResolveConfig(theme.UseMaterial3, isCupertinoAdaptive: false);
            bool enabled = CurrentWidget.OnChanged is not null;

            var effectivePadding = ResolvePadding(theme, switchTheme, isCupertinoAdaptive);
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? switchTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            double baseHeight = tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
                ? sizeConfig.BaseHeight
                : sizeConfig.CollapsedHeight;
            double totalWidth = sizeConfig.BaseWidth + effectivePadding.Left + effectivePadding.Right;
            double totalHeight = baseHeight + effectivePadding.Top + effectivePadding.Bottom;
            double splashRadius = ResolveSplashRadius(switchTheme, isCupertinoAdaptive);

            var activeStates = BuildVisualStates(enabled, selected: true);
            var inactiveStates = BuildVisualStates(enabled, selected: false);
            double position = CurrentPosition();
            var selectedStates = CurrentWidget.Value ? activeStates : inactiveStates;

            var activeThumbColor = ResolveThumbColor(theme, switchTheme, activeStates, isCupertinoAdaptive);
            var inactiveThumbColor = ResolveThumbColor(theme, switchTheme, inactiveStates, isCupertinoAdaptive);
            var thumbColor = AlphaBlend(
                LerpColor(inactiveThumbColor, activeThumbColor, position),
                theme.ColorScheme.Surface);

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

            double activeThumbDiameter = _isPressed
                ? config.PressedThumbDiameter
                : activeIcon is null
                    ? config.ActiveThumbDiameter
                    : config.ThumbDiameterWithIcon;
            double inactiveThumbDiameter = _isPressed
                ? config.PressedThumbDiameter
                : inactiveIcon is null && CurrentWidget.InactiveThumbImage is null
                    ? config.InactiveThumbDiameter
                    : config.ThumbDiameterWithIcon;
            Size thumbSize = ResolveThumbSize(config, inactiveThumbDiameter, activeThumbDiameter, position);
            double thumbHeight = thumbSize.Height;
            double thumbWidth = isCupertinoAdaptive
                ? ResolveCupertinoThumbWidth(thumbSize.Width, enabled)
                : thumbSize.Width;

            Widget thumbChild = new SizedBox(width: thumbWidth, height: thumbHeight);
            if (currentIcon is not null)
            {
                thumbChild = new SizedBox(
                    width: thumbWidth,
                    height: thumbHeight,
                    child: new Center(
                        child: new IconTheme(
                            data: new IconThemeData(
                                Color: iconColor,
                                Size: config.IconSize),
                            child: currentIcon)));
            }

            ImageProvider? thumbImage = position < 0.5
                ? CurrentWidget.InactiveThumbImage
                : CurrentWidget.ActiveThumbImage;
            ImageErrorListener? thumbImageError = position < 0.5
                ? CurrentWidget.OnInactiveThumbImageError
                : CurrentWidget.OnActiveThumbImageError;
            var thumb = new Container(
                width: thumbWidth,
                height: thumbHeight,
                decoration: new BoxDecoration(
                    Color: thumbColor,
                    BorderRadius: BorderRadius.Circular(thumbHeight / 2),
                    BoxShadows: isCupertinoAdaptive
                        ? CupertinoThumbShadows
                        : MaterialSurface.BuildBoxShadows(theme.ColorScheme.Shadow, config.ThumbElevation),
                    Image: thumbImage is null
                        ? null
                        : new DecorationImage(thumbImage, onError: thumbImageError)),
                child: thumbChild);

            Widget trackBody = new Align(
                alignment: new Alignment((position * 2) - 1, 0),
                child: thumb);

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
                    Border: trackOutline is { } outline ? Plumix.Rendering.Border.FromBorderSide(outline) : null,
                    BorderRadius: BorderRadius.Circular(config.TrackHeight / 2)),
                child: trackBody);

            Widget effectiveTrack = track;
            if (isCupertinoAdaptive && _hasFocus && overlayColor.HasValue && overlayColor.Value.A > 0)
            {
                effectiveTrack = new Container(
                    width: config.TrackWidth + 3.5,
                    height: config.TrackHeight + 3.5,
                    decoration: new BoxDecoration(
                        Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(overlayColor.Value, 3.5)),
                        BorderRadius: BorderRadius.Circular((config.TrackHeight + 3.5) / 2)),
                    child: new Center(child: track));
            }

            Widget child = new SizedBox(
                width: totalWidth,
                height: totalHeight,
                child: new Padding(
                    effectivePadding,
                    new Center(child: effectiveTrack)));

            if (isCupertinoAdaptive)
            {
                Widget adaptiveResult = new GestureDetector(
                    excludeFromSemantics: true,
                    behavior: HitTestBehavior.Opaque,
                    onTap: enabled ? HandleTap : null,
                    onHorizontalDragStart: enabled ? HandleAdaptiveDragStart : null,
                    onHorizontalDragUpdate: enabled ? HandleAdaptiveDragUpdate : null,
                    onHorizontalDragEnd: enabled ? HandleAdaptiveDragEnd : null,
                    dragStartBehavior: CurrentWidget.DragStartBehavior,
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

                adaptiveResult = new MouseRegion(
                    cursor: ResolveMouseCursor(switchTheme, selectedStates),
                    child: adaptiveResult);

                if (!enabled)
                {
                    adaptiveResult = new Opacity(CupertinoDisabledOpacity, adaptiveResult);
                }

                return new Semantics(
                    label: CurrentWidget.SemanticLabel,
                    flags: ResolveToggleSemanticsFlags(enabled),
                    onTap: enabled ? HandleTap : null,
                    child: adaptiveResult);
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
                Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                    BorderRadius.Circular(totalHeight / 2))),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(totalWidth, totalHeight)),
                FixedSize: MaterialStateProperty<Size?>.All(new Size(totalWidth, totalHeight)),
                MaximumSize: MaterialStateProperty<Size?>.All(new Size(totalWidth, totalHeight)),
                MouseCursor: MaterialStateProperty<MouseCursor?>.ResolveWith(
                    states => ResolveMouseCursor(switchTheme, states)),
                Alignment: Alignment.Center,
                TapTargetSize: tapTargetSize);

            var button = new MaterialButtonCore(
                child: child,
                onPressed: enabled ? HandleTap : null,
                style: style,
                onHoverChanged: HandleHoverChanged,
                focusNode: _focusNode,
                isSelected: CurrentWidget.Value,
                includeSemanticSelected: false,
                isSemanticButton: false,
                isSemanticChecked: CurrentWidget.Value,
                semanticLabel: CurrentWidget.SemanticLabel,
                splashRadius: splashRadius,
                autofocus: CurrentWidget.Autofocus);

            Widget materialChild = new Listener(
                behavior: HitTestBehavior.Opaque,
                onPointerDown: enabled ? HandlePointerDown : null,
                onPointerUp: enabled ? HandlePointerUp : null,
                onPointerCancel: enabled ? HandlePointerCancel : null,
                child: button);

            Widget result = new GestureDetector(
                excludeFromSemantics: true,
                behavior: HitTestBehavior.Opaque,
                onHorizontalDragStart: HandleMaterialDragStart,
                onHorizontalDragUpdate: HandleMaterialDragUpdate,
                onHorizontalDragEnd: HandleMaterialDragEnd,
                dragStartBehavior: CurrentWidget.DragStartBehavior,
                child: materialChild);

            return result;
        }

        private SemanticsFlags ResolveToggleSemanticsFlags(bool enabled)
        {
            var flags = SemanticsFlags.None;
            if (enabled)
            {
                flags |= SemanticsFlags.IsEnabled;
            }

            if (CurrentWidget.Value)
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

        private void HandleFocusChanged()
        {
            bool hasFocus = _focusNode?.HasFocus ?? false;
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
            double trackInnerLength = Math.Max(1.0, config.TrackWidth - config.TrackHeight);
            var direction = Directionality.Of(Context);
            int directionMultiplier = direction == TextDirection.Rtl ? -1 : 1;
            double next = _dragPosition.Value + ((details.PrimaryDelta / trackInnerLength) * directionMultiplier);
            SetState(() => _dragPosition = Math.Clamp(next, 0, 1));
        }

        private void HandleMaterialDragEnd(DragEndDetails details)
        {
            if (!(_dragPosition.HasValue && CurrentWidget.OnChanged is not null))
            {
                return;
            }

            double from = Math.Clamp(_dragPosition.Value, 0, 1);
            bool nextValue = from >= 0.5;
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
            int directionMultiplier = direction == TextDirection.Rtl ? -1 : 1;
            double initialDelta = 0.0;
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
            int directionMultiplier = direction == TextDirection.Rtl ? -1 : 1;
            _adaptiveDragDelta += (details.PrimaryDelta / config.TrackWidth) * directionMultiplier;

            bool valueChangedWhileDragging = CurrentWidget.Value != _adaptiveDragValue.Value;
            double threshold = valueChangedWhileDragging
                ? CupertinoDragReverseThreshold
                : CupertinoDragCommitThreshold;
            double effectiveThreshold = CurrentWidget.Value ? -threshold : threshold;
            bool newDragValue = _adaptiveDragDelta >= effectiveThreshold;

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

            bool nextValue = _adaptiveDragValue.Value;
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

            if (@event is KeyDownEvent)
            {
                HandleTap();
            }

            return KeyEventResult.Handled;
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

            return @event.LogicalKey.Equals(LogicalKeyboardKey.Enter)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.Enter)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.NumpadEnter)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.NumpadEnter)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.Space)
                   || @event.LogicalKey.Equals(LogicalKeyboardKey.Space);
        }

        private void AnimateTo(bool value, double? fromOverride = null)
        {
            double target = value ? 1.0 : 0.0;
            double from = Math.Clamp(fromOverride ?? CurrentPosition(), 0, 1);
            _fromPosition = from;
            _toPosition = target;

            if (Math.Abs(_toPosition - _fromPosition) <= 0.0001 || _positionController is null)
            {
                SetState(() => _animatedPosition = _toPosition);
                return;
            }

            var theme = Theme.Of(Context);
            bool isCupertinoAdaptive = IsAdaptiveCupertino(theme);
            var config = ResolveConfig(theme.UseMaterial3, isCupertinoAdaptive);
            _positionController.Duration = TimeSpan.FromMilliseconds(config.ToggleDuration);
            _positionController.Curve = isCupertinoAdaptive
                ? Curves.Linear
                : theme.UseMaterial3
                    ? Curves.EaseOutBack
                    : value
                        ? Curves.EaseIn
                        : Curves.EaseOut;
            _positionController.Forward(0);
        }

        private void HandlePositionTick()
        {
            if (_positionController is null || _dragPosition.HasValue)
            {
                return;
            }

            double t = _positionController.Evaluate();
            SetState(() => _animatedPosition = _fromPosition + ((_toPosition - _fromPosition) * t));
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
            return _dragPosition ?? _animatedPosition;
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

            var themedThumb = isCupertinoAdaptive ? null : switchTheme.ThumbColor?.Resolve(states);
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

            var themedTrack = isCupertinoAdaptive ? null : switchTheme.TrackColor?.Resolve(states);
            if (themedTrack.HasValue)
            {
                return themedTrack.Value;
            }

            if (states.HasFlag(MaterialState.Selected) && !isCupertinoAdaptive)
            {
                Color? thumbOverride = CurrentWidget.ActiveThumbColor ?? CurrentWidget.ActiveColor;
                if (thumbOverride.HasValue)
                {
                    return Color.FromArgb(
                        0x80,
                        thumbOverride.Value.R,
                        thumbOverride.Value.G,
                        thumbOverride.Value.B);
                }
            }

            return ResolveDefaultTrackColor(theme, states, isCupertinoAdaptive);
        }

        private BorderSide? ResolveTrackOutlineSide(ThemeData theme, SwitchThemeData switchTheme, MaterialState states, bool isCupertinoAdaptive)
        {
            var outlineColor = CurrentWidget.TrackOutlineColor?.Resolve(states)
                               ?? (isCupertinoAdaptive ? null : switchTheme.TrackOutlineColor?.Resolve(states))
                               ?? ResolveDefaultTrackOutlineColor(theme, states, isCupertinoAdaptive);
            double? outlineWidth = CurrentWidget.TrackOutlineWidth?.Resolve(states)
                                   ?? (isCupertinoAdaptive ? null : switchTheme.TrackOutlineWidth?.Resolve(states))
                                   ?? ResolveDefaultTrackOutlineWidth(theme, states, isCupertinoAdaptive);

            if (!outlineColor.HasValue || !outlineWidth.HasValue)
            {
                return null;
            }

            double width = NormalizeWidth(outlineWidth.Value);
            if (width <= 0)
            {
                return null;
            }

            return new BorderSide(outlineColor.Value, width);
        }

        private Icon? ResolveThumbIcon(SwitchThemeData switchTheme, MaterialState states)
        {
            return CurrentWidget.ThumbIcon?.Resolve(states)
                   ?? (IsAdaptiveCupertino(Theme.Of(Context)) ? null : switchTheme.ThumbIcon?.Resolve(states));
        }

        private Color ResolveThumbIconColor(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.38);
                }

                return theme.ColorScheme.OnPrimaryContainer;
            }

            if (!theme.UseMaterial3)
            {
                return Colors.Transparent;
            }

            if (states.HasFlag(MaterialState.Disabled))
            {
                return states.HasFlag(MaterialState.Selected)
                    ? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.38)
                    : MaterialButtonCore.ApplyOpacity(theme.ColorScheme.SurfaceContainerHighest, 0.38);
            }

            return states.HasFlag(MaterialState.Selected)
                ? theme.ColorScheme.OnPrimaryContainer
                : theme.ColorScheme.SurfaceContainerHighest;
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

            var themedOverlay = isCupertinoAdaptive ? null : switchTheme.OverlayColor?.Resolve(states);
            if (themedOverlay.HasValue)
            {
                return themedOverlay.Value;
            }

            return ResolveDefaultOverlayColor(theme, states, isCupertinoAdaptive);
        }

        private double ResolveSplashRadius(SwitchThemeData switchTheme, bool isCupertinoAdaptive)
        {
            double resolved = CurrentWidget.SplashRadius
                              ?? (isCupertinoAdaptive ? null : switchTheme.SplashRadius)
                              ?? (isCupertinoAdaptive ? 0.0 : DefaultSplashRadius);
            double fallback = isCupertinoAdaptive ? 0.0 : DefaultSplashRadius;
            return NormalizePositiveValue(resolved, fallback);
        }

        private Thickness ResolvePadding(ThemeData theme, SwitchThemeData switchTheme, bool isCupertinoAdaptive)
        {
            var fallback = theme.UseMaterial3
                ? new Thickness(4, 0, 4, 0)
                : default;
            var source = CurrentWidget.Padding
                         ?? (isCupertinoAdaptive ? null : switchTheme.Padding)
                         ?? fallback;
            return NormalizePadding(source);
        }

        private MouseCursor ResolveMouseCursor(SwitchThemeData switchTheme, MaterialState states)
        {
            MouseCursor? themedCursor = IsAdaptiveCupertino(Theme.Of(Context))
                ? null
                : switchTheme.MouseCursor?.Resolve(states);
            return CurrentWidget.MouseCursor
                   ?? themedCursor
                   ?? (states.HasFlag(MaterialState.Disabled)
                       ? SystemMouseCursors.Basic
                       : SystemMouseCursors.Click);
        }

        private static SwitchConfig ResolveConfig(bool useMaterial3, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                return new SwitchConfig(
                    BaseWidth: 60,
                    BaseHeight: 48,
                    CollapsedHeight: 40,
                    TrackWidth: 51,
                    TrackHeight: 31,
                    ActiveThumbDiameter: 28,
                    InactiveThumbDiameter: 28,
                    PressedThumbDiameter: 28,
                    ThumbDiameterWithIcon: 28,
                    TransitionalThumbSize: new Size(28, 28),
                    IconSize: 16,
                    ThumbElevation: 0,
                    ToggleDuration: 140);
            }

            return useMaterial3
                ? new SwitchConfig(
                    BaseWidth: 52,
                    BaseHeight: 48,
                    CollapsedHeight: 40,
                    TrackWidth: 52,
                    TrackHeight: 32,
                    ActiveThumbDiameter: 24,
                    InactiveThumbDiameter: 16,
                    PressedThumbDiameter: 28,
                    ThumbDiameterWithIcon: 24,
                    TransitionalThumbSize: new Size(34, 22),
                    IconSize: 16,
                    ThumbElevation: 0,
                    ToggleDuration: 300)
                : new SwitchConfig(
                    BaseWidth: 59,
                    BaseHeight: 48,
                    CollapsedHeight: 40,
                    TrackWidth: 33,
                    TrackHeight: 14,
                    ActiveThumbDiameter: 20,
                    InactiveThumbDiameter: 20,
                    PressedThumbDiameter: 20,
                    ThumbDiameterWithIcon: 20,
                    TransitionalThumbSize: new Size(20, 20),
                    IconSize: 14,
                    ThumbElevation: 1,
                    ToggleDuration: 200);
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

        private Size ResolveThumbSize(
            SwitchConfig config,
            double inactiveDiameter,
            double activeDiameter,
            double position)
        {
            var inactiveSize = new Size(inactiveDiameter, inactiveDiameter);
            var activeSize = new Size(activeDiameter, activeDiameter);
            if (!Theme.Of(Context).UseMaterial3 || _positionController?.IsAnimating != true)
            {
                return LerpSize(inactiveSize, activeSize, position);
            }

            double elapsed = _positionController.Value;
            Size begin = _toPosition >= 0.5 ? inactiveSize : activeSize;
            Size end = _toPosition >= 0.5 ? activeSize : inactiveSize;
            if (elapsed <= 0.11)
            {
                double segment = elapsed / 0.11;
                return LerpSize(
                    begin,
                    config.TransitionalThumbSize,
                    Curves.Cubic(0.31, 0.00, 0.56, 1.00)(segment));
            }

            if (elapsed <= 0.83)
            {
                double segment = (elapsed - 0.11) / 0.72;
                return LerpSize(
                    config.TransitionalThumbSize,
                    end,
                    Curves.Cubic(0.20, 0.00, 0.00, 1.00)(segment));
            }

            return end;
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
                        ? theme.ColorScheme.Surface
                        : MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.38);
                }

                if (states.HasFlag(MaterialState.Selected))
                {
                    return states.HasFlag(MaterialState.Pressed)
                           || states.HasFlag(MaterialState.Hovered)
                           || states.HasFlag(MaterialState.Focused)
                        ? theme.ColorScheme.PrimaryContainer
                        : theme.ColorScheme.OnPrimary;
                }

                return states.HasFlag(MaterialState.Pressed)
                       || states.HasFlag(MaterialState.Hovered)
                       || states.HasFlag(MaterialState.Focused)
                    ? theme.ColorScheme.OnSurfaceVariant
                    : theme.ColorScheme.Outline;
            }

            if (states.HasFlag(MaterialState.Disabled))
            {
                return theme.Brightness == Brightness.Dark
                    ? Color.FromRgb(0x42, 0x42, 0x42)
                    : Color.FromRgb(0xBD, 0xBD, 0xBD);
            }

            return states.HasFlag(MaterialState.Selected)
                ? theme.ColorScheme.Secondary
                : theme.Brightness == Brightness.Dark
                    ? Color.FromRgb(0xBD, 0xBD, 0xBD)
                    : Color.FromRgb(0xFA, 0xFA, 0xFA);
        }

        private Color ResolveDefaultTrackColor(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                return states.HasFlag(MaterialState.Selected)
                    ? CurrentWidget.ApplyCupertinoTheme == true
                        ? theme.ColorScheme.Primary
                        : ResolveCupertinoActiveTrackColor(theme)
                    : ResolveCupertinoInactiveTrackColor(theme);
            }

            if (theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return states.HasFlag(MaterialState.Selected)
                        ? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.12)
                        : MaterialButtonCore.ApplyOpacity(theme.ColorScheme.SurfaceContainerHighest, 0.12);
                }

                return states.HasFlag(MaterialState.Selected)
                    ? theme.ColorScheme.Primary
                    : theme.ColorScheme.SurfaceContainerHighest;
            }

            if (states.HasFlag(MaterialState.Disabled))
            {
                return theme.Brightness == Brightness.Dark
                    ? Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)
                    : Color.FromArgb(0x1F, 0x00, 0x00, 0x00);
            }

            return states.HasFlag(MaterialState.Selected)
                ? MaterialButtonCore.ApplyOpacity(theme.ColorScheme.Secondary, 0.50)
                : theme.Brightness == Brightness.Dark
                    ? Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)
                    : Color.FromArgb(0x52, 0x00, 0x00, 0x00);
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
                return MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.12);
            }

            return theme.ColorScheme.Outline;
        }

        private static double? ResolveDefaultTrackOutlineWidth(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                return 0.0;
            }

            return theme.UseMaterial3 ? 2.0 : 0.0;
        }

        private Color? ResolveDefaultOverlayColor(ThemeData theme, MaterialState states, bool isCupertinoAdaptive)
        {
            if (isCupertinoAdaptive)
            {
                if (!states.HasFlag(MaterialState.Focused))
                {
                    return Colors.Transparent;
                }

                Color primary = CurrentWidget.ApplyCupertinoTheme == true
                    ? theme.ColorScheme.Primary
                    : ResolveCupertinoActiveTrackColor(theme);
                return ResolveCupertinoFocusColor(primary);
            }

            if (theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Selected))
                {
                    if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
                    {
                        return MaterialButtonCore.ApplyOpacity(theme.ColorScheme.Primary, 0.10);
                    }

                    if (states.HasFlag(MaterialState.Hovered))
                    {
                        return MaterialButtonCore.ApplyOpacity(theme.ColorScheme.Primary, 0.08);
                    }

                    return null;
                }

                if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.10);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.ColorScheme.OnSurface, 0.08);
                }

                return null;
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                Color thumbColor = ResolveDefaultThumbColor(theme, states, isCupertinoAdaptive: false);
                return Color.FromArgb(0x1F, thumbColor.R, thumbColor.G, thumbColor.B);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return theme.HoverColor;
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return theme.FocusColor;
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
            double clamped = Math.Clamp(t, 0, 1);
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

        private static Color AlphaBlend(Color foreground, Color background)
        {
            double foregroundAlpha = foreground.A / 255.0;
            double backgroundAlpha = background.A / 255.0;
            double outputAlpha = foregroundAlpha + (backgroundAlpha * (1.0 - foregroundAlpha));
            if (outputAlpha <= 0.0)
            {
                return Colors.Transparent;
            }

            byte BlendChannel(byte foregroundChannel, byte backgroundChannel)
            {
                double numerator = (foregroundChannel * foregroundAlpha)
                                   + (backgroundChannel * backgroundAlpha * (1.0 - foregroundAlpha));
                return (byte)Math.Clamp((int)Math.Round(numerator / outputAlpha), 0, 255);
            }

            return Color.FromArgb(
                (byte)Math.Clamp((int)Math.Round(outputAlpha * 255.0), 0, 255),
                BlendChannel(foreground.R, background.R),
                BlendChannel(foreground.G, background.G),
                BlendChannel(foreground.B, background.B));
        }

        private static Color ResolveCupertinoFocusColor(Color primary)
        {
            double red = primary.R / 255.0;
            double green = primary.G / 255.0;
            double blue = primary.B / 255.0;
            double maximum = Math.Max(red, Math.Max(green, blue));
            double minimum = Math.Min(red, Math.Min(green, blue));
            double delta = maximum - minimum;
            double hue;
            if (delta == 0.0)
            {
                hue = 0.0;
            }
            else if (maximum == red)
            {
                hue = ((green - blue) / delta) % 6.0;
            }
            else if (maximum == green)
            {
                hue = ((blue - red) / delta) + 2.0;
            }
            else
            {
                hue = ((red - green) / delta) + 4.0;
            }

            hue /= 6.0;
            if (hue < 0.0)
            {
                hue += 1.0;
            }

            const double saturation = 0.835;
            const double lightness = 0.69;
            double chroma = (1.0 - Math.Abs((2.0 * lightness) - 1.0)) * saturation;
            double hueSection = hue * 6.0;
            double secondary = chroma * (1.0 - Math.Abs((hueSection % 2.0) - 1.0));
            (double redPrime, double greenPrime, double bluePrime) = hueSection switch
            {
                < 1.0 => (chroma, secondary, 0.0),
                < 2.0 => (secondary, chroma, 0.0),
                < 3.0 => (0.0, chroma, secondary),
                < 4.0 => (0.0, secondary, chroma),
                < 5.0 => (secondary, 0.0, chroma),
                _ => (chroma, 0.0, secondary)
            };
            double match = lightness - (chroma / 2.0);
            return Color.FromArgb(
                0xCC,
                ToColorChannel(redPrime + match),
                ToColorChannel(greenPrime + match),
                ToColorChannel(bluePrime + match));
        }

        private static Color ResolveCupertinoActiveTrackColor(ThemeData theme)
        {
            return theme.Brightness == Brightness.Dark
                ? CupertinoActiveTrackColorDark
                : CupertinoActiveTrackColor;
        }

        private static Color ResolveCupertinoInactiveTrackColor(ThemeData theme)
        {
            return theme.Brightness == Brightness.Dark
                ? CupertinoInactiveTrackColorDark
                : CupertinoInactiveTrackColor;
        }

        private static byte ToColorChannel(double value)
        {
            return (byte)Math.Clamp((int)Math.Round(value * 255.0), 0, 255);
        }

        private static BorderSide? LerpSide(BorderSide? from, BorderSide? to, double t)
        {
            double clamped = Math.Clamp(t, 0, 1);
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
            double width = fromSide.Width + ((toSide.Width - fromSide.Width) * clamped);
            var color = LerpColor(fromSide.Color, toSide.Color, clamped);
            return new BorderSide(color, width);
        }

        private static double LerpDouble(double from, double to, double t)
        {
            double clamped = Math.Clamp(t, 0, 1);
            return from + ((to - from) * clamped);
        }

        private static Size LerpSize(Size from, Size to, double t)
        {
            return new Size(
                LerpDouble(from.Width, to.Width, t),
                LerpDouble(from.Height, to.Height, t));
        }
    }

    private readonly record struct SwitchConfig(
        double BaseWidth,
        double BaseHeight,
        double CollapsedHeight,
        double TrackWidth,
        double TrackHeight,
        double ActiveThumbDiameter,
        double InactiveThumbDiameter,
        double PressedThumbDiameter,
        double ThumbDiameterWithIcon,
        Size TransitionalThumbSize,
        double IconSize,
        double ThumbElevation,
        int ToggleDuration);
}
