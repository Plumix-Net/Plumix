using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/switch.dart

public sealed class CupertinoSwitch : StatefulWidget
{
    public CupertinoSwitch(
        bool value,
        Action<bool>? onChanged,
        WidgetStateColor? activeColor = null,
        WidgetStateColor? trackColor = null,
        WidgetStateColor? activeTrackColor = null,
        WidgetStateColor? inactiveTrackColor = null,
        WidgetStateColor? thumbColor = null,
        WidgetStateColor? inactiveThumbColor = null,
        bool? applyTheme = null,
        CupertinoDynamicColor? focusColor = null,
        CupertinoDynamicColor? onLabelColor = null,
        CupertinoDynamicColor? offLabelColor = null,
        ImageProvider? activeThumbImage = null,
        ImageErrorListener? onActiveThumbImageError = null,
        ImageProvider? inactiveThumbImage = null,
        ImageErrorListener? onInactiveThumbImageError = null,
        WidgetStateProperty<Color?>? trackOutlineColor = null,
        WidgetStateProperty<double?>? trackOutlineWidth = null,
        WidgetStateProperty<Icon?>? thumbIcon = null,
        WidgetStateProperty<MouseCursor>? mouseCursor = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
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
        if (activeTrackColor is not null && activeColor is not null)
        {
            throw new ArgumentException("activeTrackColor and activeColor cannot both be supplied.");
        }
        if (inactiveTrackColor is not null && trackColor is not null)
        {
            throw new ArgumentException("inactiveTrackColor and trackColor cannot both be supplied.");
        }

        Value = value;
        OnChanged = onChanged;
        ActiveTrackColor = activeTrackColor ?? activeColor;
        InactiveTrackColor = inactiveTrackColor ?? trackColor;
        ThumbColor = thumbColor;
        InactiveThumbColor = inactiveThumbColor;
        ApplyTheme = applyTheme;
        FocusColor = focusColor;
        OnLabelColor = onLabelColor;
        OffLabelColor = offLabelColor;
        ActiveThumbImage = activeThumbImage;
        OnActiveThumbImageError = onActiveThumbImageError;
        InactiveThumbImage = inactiveThumbImage;
        OnInactiveThumbImageError = onInactiveThumbImageError;
        TrackOutlineColor = trackOutlineColor;
        TrackOutlineWidth = trackOutlineWidth;
        ThumbIcon = thumbIcon;
        MouseCursor = mouseCursor;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        DragStartBehavior = dragStartBehavior;
    }

    public bool Value { get; }

    public Action<bool>? OnChanged { get; }

    [Obsolete("Use ActiveTrackColor instead. Mirrors Flutter's deprecation after v3.24.0-0.2.pre.")]
    public WidgetStateColor? ActiveColor => ActiveTrackColor;

    [Obsolete("Use InactiveTrackColor instead. Mirrors Flutter's deprecation after v3.24.0-0.2.pre.")]
    public WidgetStateColor? TrackColor => InactiveTrackColor;

    public WidgetStateColor? ActiveTrackColor { get; }

    public WidgetStateColor? InactiveTrackColor { get; }

    public WidgetStateColor? ThumbColor { get; }

    public WidgetStateColor? InactiveThumbColor { get; }

    public bool? ApplyTheme { get; }

    public CupertinoDynamicColor? FocusColor { get; }

    public CupertinoDynamicColor? OnLabelColor { get; }

    public CupertinoDynamicColor? OffLabelColor { get; }

    public ImageProvider? ActiveThumbImage { get; }

    public ImageErrorListener? OnActiveThumbImageError { get; }

    public ImageProvider? InactiveThumbImage { get; }

    public ImageErrorListener? OnInactiveThumbImageError { get; }

    public WidgetStateProperty<Color?>? TrackOutlineColor { get; }

    public WidgetStateProperty<double?>? TrackOutlineWidth { get; }

    public WidgetStateProperty<Icon?>? ThumbIcon { get; }

    public WidgetStateProperty<MouseCursor>? MouseCursor { get; }

    public FocusNode? FocusNode { get; }

    public Action<bool>? OnFocusChange { get; }

    public bool Autofocus { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public override State CreateState() => new CupertinoSwitchState();

    private sealed class CupertinoSwitchState : ToggleableState
    {
        private const double TrackInnerLength = 20.0;
        private const double DragCommitThreshold = 0.7;
        private const double DragReverseThreshold = 0.2;
        private static readonly Size SwitchSize = new(59.0, 39.0);
        private static readonly Color OffLabelColor = Color.FromUInt32(0xFFB3B3B3);
        private static readonly Color OffLabelHighContrastColor = Colors.White;

        private CupertinoSwitchPainter? _painter;
        private Point? _dragStartPosition;
        private double _dragDelta;
        private bool? _dragValue;
        private bool _needsPositionAnimation;

        private CupertinoSwitch CurrentWidget => (CupertinoSwitch)StateWidget;

        protected override bool IsInteractive => CurrentWidget.OnChanged is not null;

        protected override bool IsValueSelected => CurrentWidget.Value;

        public override void InitState()
        {
            base.InitState();
            PositionController.Duration = TimeSpan.FromMilliseconds(200.0);
            ReactionController.Duration = TimeSpan.FromMilliseconds(300.0);
            PositionAnimation.Curve = Curves.Ease;
            PositionAnimation.ReverseCurve = Curves.Flipped(Curves.Ease);
            _painter = new CupertinoSwitchPainter(
                Position,
                Reaction,
                ReactionHoverFade,
                ReactionFocusFade,
                PositionController);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldSwitch = (CupertinoSwitch)oldWidget;
            base.DidUpdateWidget(oldWidget);
            if (oldSwitch.Value != CurrentWidget.Value)
            {
                AnimateToValue(CurrentWidget.Value, tristate: false);
            }
        }

        public override Widget Build(BuildContext context)
        {
            CupertinoThemeData theme = CupertinoTheme.Of(context);
            IReadOnlySet<WidgetState> activeStates = States(selected: true);
            IReadOnlySet<WidgetState> inactiveStates = States(selected: false);
            Color resolvedActiveColor = ResolveActiveColor(context, theme);
            Color activeTrackColor = CurrentWidget.ActiveTrackColor?.DefaultValue ?? resolvedActiveColor;
            Color inactiveTrackColor = ResolveStateColor(
                                           CurrentWidget.InactiveTrackColor,
                                           inactiveStates,
                                           context)
                                       ?? CupertinoColors.SecondarySystemFill.ResolveFrom(context).Value;
            Color activeThumbColor = ResolveStateColor(CurrentWidget.ThumbColor, activeStates, context)
                                     ?? CupertinoColors.White;
            Color inactiveThumbColor = ResolveStateColor(CurrentWidget.InactiveThumbColor, inactiveStates, context)
                                         ?? activeThumbColor;
            Color activePressedThumbColor = ResolvePressedThumbColor(context, activeStates, active: true);
            Color inactivePressedThumbColor = ResolvePressedThumbColor(context, inactiveStates, active: false);
            Color focusColor = ResolveFocusColor(context, resolvedActiveColor);
            bool showLabels = MediaQuery.MaybeOnOffSwitchLabelsOf(context) ?? false;
            Color onLabelColor = CurrentWidget.OnLabelColor?.ResolveFrom(context).Value ?? CupertinoColors.White;
            Color offLabelColor = CurrentWidget.OffLabelColor?.ResolveFrom(context).Value
                                  ?? (MediaQuery.MaybeHighContrastOf(context) == true
                                      ? OffLabelHighContrastColor
                                      : OffLabelColor);

            _painter!.Configure(
                textDirection: Directionality.Of(context),
                focused: IsFocused,
                activeTrackColor: activeTrackColor,
                inactiveTrackColor: inactiveTrackColor,
                activeThumbColor: activeThumbColor,
                inactiveThumbColor: inactiveThumbColor,
                activePressedThumbColor: activePressedThumbColor,
                inactivePressedThumbColor: inactivePressedThumbColor,
                activeOutlineColor: CurrentWidget.TrackOutlineColor?.Resolve(activeStates),
                inactiveOutlineColor: CurrentWidget.TrackOutlineColor?.Resolve(inactiveStates),
                activeOutlineWidth: CurrentWidget.TrackOutlineWidth?.Resolve(activeStates),
                inactiveOutlineWidth: CurrentWidget.TrackOutlineWidth?.Resolve(inactiveStates),
                activeIcon: CurrentWidget.ThumbIcon?.Resolve(activeStates),
                inactiveIcon: CurrentWidget.ThumbIcon?.Resolve(inactiveStates),
                iconTheme: IconTheme.Of(context),
                focusColor: focusColor,
                showLabels: showLabels,
                onLabelColor: onLabelColor,
                offLabelColor: offLabelColor,
                activeThumbImage: CurrentWidget.ActiveThumbImage,
                onActiveThumbImageError: CurrentWidget.OnActiveThumbImageError,
                inactiveThumbImage: CurrentWidget.InactiveThumbImage,
                onInactiveThumbImageError: CurrentWidget.OnInactiveThumbImageError,
                backgroundColor: theme.ScaffoldBackgroundColor,
                imageConfiguration: ImageConfigurationUtils.CreateLocalImageConfiguration(context));

            if (_needsPositionAnimation)
            {
                _needsPositionAnimation = false;
                AnimateToValue(CurrentWidget.Value, tristate: false);
            }

            Widget result = BuildToggleable(
                painter: _painter,
                size: SwitchSize,
                mouseCursor: CurrentWidget.MouseCursor ?? DefaultMouseCursor(),
                onTap: HandleChanged,
                focusNode: CurrentWidget.FocusNode,
                onFocusChange: CurrentWidget.OnFocusChange,
                autofocus: CurrentWidget.Autofocus);
            result = new Opacity(IsInteractive ? 1.0 : 0.5, result);
            result = new GestureDetector(
                excludeFromSemantics: true,
                onTapDown: IsInteractive ? HandleTapDown : null,
                onHorizontalDragStart: IsInteractive ? HandleDragStart : null,
                onHorizontalDragUpdate: IsInteractive ? HandleDragUpdate : null,
                onHorizontalDragEnd: IsInteractive ? HandleDragEnd : null,
                onHorizontalDragCancel: IsInteractive ? HandleDragCancel : null,
                dragStartBehavior: CurrentWidget.DragStartBehavior,
                child: result);
            return new Semantics(
                toggled: CurrentWidget.Value,
                child: result);
        }

        public override void Dispose()
        {
            _painter?.Dispose();
            _painter = null;
            base.Dispose();
        }

        private IReadOnlySet<WidgetState> States(bool selected)
        {
            var states = new HashSet<WidgetState>(CurrentWidgetStates);
            states.Remove(WidgetState.Selected);
            if (selected)
            {
                states.Add(WidgetState.Selected);
            }
            return states;
        }

        private Color ResolveActiveColor(
            BuildContext context,
            CupertinoThemeData theme)
        {
            if (CurrentWidget.ActiveTrackColor is CupertinoDynamicWidgetStateColor dynamicColor)
            {
                return dynamicColor.DynamicColor.ResolveFrom(context).Value;
            }
            if (CurrentWidget.ActiveTrackColor is not null)
            {
                return CurrentWidget.ActiveTrackColor.DefaultValue;
            }
            if (CurrentWidget.ApplyTheme ?? theme.ApplyThemeToAll)
            {
                return theme.PrimaryColor;
            }
            return CupertinoColors.SystemGreen.ResolveFrom(context).Value;
        }

        private Color ResolvePressedThumbColor(
            BuildContext context,
            IReadOnlySet<WidgetState> states,
            bool active)
        {
            var pressedStates = new HashSet<WidgetState>(states) { WidgetState.Pressed };
            return ResolveStateColor(CurrentWidget.ThumbColor, pressedStates, context)
                   ?? (active
                       ? CupertinoColors.White
                       : CurrentWidget.InactiveThumbColor?.DefaultValue ?? CupertinoColors.White);
        }

        private Color ResolveFocusColor(BuildContext context, Color activeTrackColor)
        {
            if (CurrentWidget.FocusColor is { } explicitColor)
            {
                return explicitColor.ResolveFrom(context).Value;
            }

            byte alpha = (byte)Math.Clamp((int)Math.Round(activeTrackColor.A * 0.80), 0, byte.MaxValue);
            Color translucent = Color.FromArgb(
                alpha,
                activeTrackColor.R,
                activeTrackColor.G,
                activeTrackColor.B);
            Color color = HSLColor.FromColor(translucent)
                .WithLightness(0.69)
                .WithSaturation(0.835)
                .ToColor();
            return CupertinoDynamicColor.Resolve(color, context);
        }

        private static Color? ResolveStateColor(
            WidgetStateColor? color,
            IReadOnlySet<WidgetState> states,
            BuildContext context)
        {
            return color switch
            {
                null => null,
                CupertinoDynamicWidgetStateColor dynamicColor =>
                    dynamicColor.DynamicColor.ResolveFrom(context).Value,
                _ => color.Resolve(states),
            };
        }

        private static WidgetStateProperty<MouseCursor> DefaultMouseCursor()
        {
            return WidgetStateProperty<MouseCursor>.ResolveWith(states =>
            {
                if (states.Contains(WidgetState.Disabled))
                {
                    return Plumix.Widgets.MouseCursor.Defer;
                }
                return OperatingSystem.IsBrowser()
                    ? SystemMouseCursors.Click
                    : Plumix.Widgets.MouseCursor.Defer;
            });
        }

        private void HandleChanged()
        {
            CurrentWidget.OnChanged?.Invoke(!CurrentWidget.Value);
            EmitHapticFeedback();
        }

        private void HandleTapDown(TapDownDetails details)
        {
            _dragStartPosition = details.GlobalPosition;
        }

        private void HandleDragStart(DragStartDetails details)
        {
            ReactionController.Forward();
            _dragValue = CurrentWidget.Value;
            if (CurrentWidget.DragStartBehavior == DragStartBehavior.Start && _dragStartPosition.HasValue)
            {
                double delta = details.GlobalPosition.X - _dragStartPosition.Value.X;
                AddDragDelta(delta);
            }
        }

        private void HandleDragUpdate(DragUpdateDetails details)
        {
            AddDragDelta(details.PrimaryDelta ?? 0.0);
        }

        private void AddDragDelta(double delta)
        {
            double directedDelta = Directionality.Of(Context) == TextDirection.Ltr ? delta : -delta;
            _dragDelta += directedDelta / (TrackInnerLength + 31.0);
            double threshold = _dragValue == CurrentWidget.Value
                ? DragCommitThreshold
                : DragReverseThreshold;
            double effectiveThreshold = CurrentWidget.Value ? -threshold : threshold;
            bool newValue = _dragDelta >= effectiveThreshold;
            if (_dragValue == newValue)
            {
                return;
            }

            _dragValue = newValue;
            EmitHapticFeedback();
            if (newValue)
            {
                PositionController.Forward();
            }
            else
            {
                PositionController.Reverse();
            }
        }

        private void HandleDragEnd(DragEndDetails details)
        {
            _ = details;
            if (_dragValue.HasValue && _dragValue.Value != CurrentWidget.Value)
            {
                CurrentWidget.OnChanged?.Invoke(!CurrentWidget.Value);
            }
            _needsPositionAnimation = true;
            ResetDrag();
            ReactionController.Reverse();
        }

        private void HandleDragCancel()
        {
            _needsPositionAnimation = true;
            ResetDrag();
            ReactionController.Reverse();
        }

        private void ResetDrag()
        {
            _dragStartPosition = null;
            _dragDelta = 0.0;
            _dragValue = null;
        }

        private static void EmitHapticFeedback()
        {
            if (PlatformDefaults.TargetPlatform == TargetPlatform.IOS)
            {
                _ = HapticFeedback.LightImpact();
            }
        }
    }
}

internal sealed class CupertinoSwitchPainter : ToggleablePainter
{
    private const double TrackWidth = 51.0;
    private const double TrackHeight = 31.0;
    private const double TrackRadius = TrackHeight / 2.0;
    private const double ThumbDiameter = 28.0;
    private const double DefaultIconSize = 16.0;
    private static readonly Color ThumbBorderColor = Color.FromUInt32(0x0A000000);
    private static readonly IReadOnlyList<BoxShadow> ThumbShadows = CupertinoThumbPainter.SwitchThumb().Shadows;

    private readonly AnimationController _positionController;
    private BoxPainter? _thumbPainter;
    private TextPainter? _textPainter;
    private Color? _cachedThumbColor;
    private ImageProvider? _cachedThumbImage;
    private ImageErrorListener? _cachedThumbImageError;
    private bool _isPainting;
    private TextDirection _textDirection;
    private Color _activeTrackColor;
    private Color _inactiveTrackColor;
    private Color _activeThumbColor;
    private Color _inactiveThumbColor;
    private Color _activePressedThumbColor;
    private Color _inactivePressedThumbColor;
    private Color? _activeOutlineColor;
    private Color? _inactiveOutlineColor;
    private double? _activeOutlineWidth;
    private double? _inactiveOutlineWidth;
    private Icon? _activeIcon;
    private Icon? _inactiveIcon;
    private IconThemeData _iconTheme = IconThemeData.Fallback;
    private bool _showLabels;
    private Color _onLabelColor;
    private Color _offLabelColor;
    private ImageProvider? _activeThumbImage;
    private ImageErrorListener? _onActiveThumbImageError;
    private ImageProvider? _inactiveThumbImage;
    private ImageErrorListener? _onInactiveThumbImageError;
    private Color _backgroundColor;
    private ImageConfiguration _imageConfiguration = ImageConfiguration.Empty;

    public CupertinoSwitchPainter(
        Animation<double> position,
        Animation<double> reaction,
        Animation<double> reactionHoverFade,
        Animation<double> reactionFocusFade,
        AnimationController positionController)
        : base(position, reaction, reactionHoverFade, reactionFocusFade)
    {
        _positionController = positionController;
    }

    internal static Size TrackSize => new(TrackWidth, TrackHeight);

    internal Color ActiveTrackColor => _activeTrackColor;

    internal Color InactiveTrackColor => _inactiveTrackColor;

    internal Color ActiveThumbColor => _activeThumbColor;

    internal Color EffectiveFocusColor => FocusColor;

    internal Color? ActiveOutlineColor => _activeOutlineColor;

    internal Color? InactiveOutlineColor => _inactiveOutlineColor;

    internal double? ActiveOutlineWidth => _activeOutlineWidth;

    internal double? InactiveOutlineWidth => _inactiveOutlineWidth;

    internal double PositionValue => Position.Value;

    internal bool ShowLabels => _showLabels;

    internal Color OnLabelColor => _onLabelColor;

    internal Color OffLabelColor => _offLabelColor;

    public void Configure(
        TextDirection textDirection,
        bool focused,
        Color activeTrackColor,
        Color inactiveTrackColor,
        Color activeThumbColor,
        Color inactiveThumbColor,
        Color activePressedThumbColor,
        Color inactivePressedThumbColor,
        Color? activeOutlineColor,
        Color? inactiveOutlineColor,
        double? activeOutlineWidth,
        double? inactiveOutlineWidth,
        Icon? activeIcon,
        Icon? inactiveIcon,
        IconThemeData iconTheme,
        Color focusColor,
        bool showLabels,
        Color onLabelColor,
        Color offLabelColor,
        ImageProvider? activeThumbImage,
        ImageErrorListener? onActiveThumbImageError,
        ImageProvider? inactiveThumbImage,
        ImageErrorListener? onInactiveThumbImageError,
        Color backgroundColor,
        ImageConfiguration imageConfiguration)
    {
        _textDirection = textDirection;
        IsFocused = focused;
        _activeTrackColor = activeTrackColor;
        _inactiveTrackColor = inactiveTrackColor;
        _activeThumbColor = activeThumbColor;
        _inactiveThumbColor = inactiveThumbColor;
        _activePressedThumbColor = activePressedThumbColor;
        _inactivePressedThumbColor = inactivePressedThumbColor;
        _activeOutlineColor = activeOutlineColor;
        _inactiveOutlineColor = inactiveOutlineColor;
        _activeOutlineWidth = activeOutlineWidth;
        _inactiveOutlineWidth = inactiveOutlineWidth;
        _activeIcon = activeIcon;
        _inactiveIcon = inactiveIcon;
        _iconTheme = iconTheme;
        FocusColor = focusColor;
        _showLabels = showLabels;
        _onLabelColor = onLabelColor;
        _offLabelColor = offLabelColor;
        _activeThumbImage = activeThumbImage;
        _onActiveThumbImageError = onActiveThumbImageError;
        _inactiveThumbImage = inactiveThumbImage;
        _onInactiveThumbImageError = onInactiveThumbImageError;
        _backgroundColor = backgroundColor;
        _imageConfiguration = imageConfiguration;
        NotifyPainterChanged();
    }

    public override void Paint(PaintingContext context, Size size)
    {
        double currentValue = Position.Value;
        double visualPosition = _textDirection == TextDirection.Ltr ? currentValue : 1.0 - currentValue;
        double pressedExtension = Reaction.Value * CupertinoThumbPainter.Extension;
        var thumbSize = new Size(ThumbDiameter + pressedExtension, ThumbDiameter);
        double colorPosition = ColorAnimationValue();
        Color trackColor = ColorUtilities.Lerp(_inactiveTrackColor, _activeTrackColor, currentValue);
        Color thumbColor = ResolveThumbColor(colorPosition);
        thumbColor = ColorUtilities.AlphaBlend(thumbColor, _backgroundColor);
        Color? outlineColor = _inactiveOutlineColor.HasValue && _activeOutlineColor.HasValue
            ? ColorUtilities.Lerp(_inactiveOutlineColor.Value, _activeOutlineColor.Value, colorPosition)
            : null;
        double? outlineWidth = ColorUtilities.LerpDouble(
            _inactiveOutlineWidth,
            _activeOutlineWidth,
            colorPosition);
        Icon? icon = currentValue < 0.5 ? _inactiveIcon : _activeIcon;
        ImageProvider? image = currentValue < 0.5 ? _inactiveThumbImage : _activeThumbImage;
        ImageErrorListener? imageError = currentValue < 0.5
            ? _onInactiveThumbImageError
            : _onActiveThumbImageError;

        double trackX = (size.Width - TrackWidth) / 2.0;
        double trackY = (size.Height - TrackHeight) / 2.0;
        var trackRect = new Rect(trackX, trackY, TrackWidth, TrackHeight);
        var trackRRect = RRect.FromRectAndRadius(trackRect, TrackRadius);
        double horizontalProgress = visualPosition * (20.0 - pressedExtension);
        double thumbX = trackX + TrackRadius + (pressedExtension / 2.0)
                        - (thumbSize.Width / 2.0) + horizontalProgress;
        double thumbY = trackY - ((thumbSize.Height / 2.0) - TrackRadius);
        var thumbBounds = new Rect(thumbX, thumbY, thumbSize.Width, thumbSize.Height);

        context.Canvas.DrawRRect(trackRRect, new SolidColorBrush(trackColor), null);
        if (outlineColor.HasValue)
        {
            Rect outlineRect = trackRect.Deflate(1.0);
            context.Canvas.DrawRRect(
                RRect.FromRectAndRadius(outlineRect, TrackRadius),
                null,
                new Pen(new SolidColorBrush(outlineColor.Value), outlineWidth ?? 2.0));
        }
        if (IsFocused)
        {
            context.Canvas.DrawRRect(
                RRect.FromRectAndRadius(trackRect.Inflate(1.75), TrackRadius + 1.75),
                null,
                new Pen(new SolidColorBrush(FocusColor), 3.5));
        }

        context.PushClipRRect(
            false,
            new Point(0, 0),
            trackRect,
            RRect.FromRectAndRadius(trackRect, TrackRadius),
            (clippedContext, _) =>
        {
            if (_showLabels)
            {
                PaintLabels(clippedContext, trackRect, visualPosition);
            }
            PaintThumb(clippedContext, thumbBounds, thumbColor, image, imageError);
            if (icon?.IconData is not null)
            {
                PaintIcon(clippedContext, thumbBounds, icon, colorPosition);
            }
        });
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) => true;

    public override void Dispose()
    {
        _thumbPainter?.Dispose();
        _thumbPainter = null;
        _cachedThumbColor = null;
        _cachedThumbImage = null;
        _cachedThumbImageError = null;
        _textPainter?.Dispose();
        _textPainter = null;
        base.Dispose();
    }

    private double ColorAnimationValue()
    {
        double value = Math.Clamp(_positionController.Value, 0.0, 1.0);
        return _positionController.Status == AnimationStatus.Reverse
            ? Curves.EaseIn(value)
            : Curves.EaseOut(value);
    }

    private Color ResolveThumbColor(double currentValue)
    {
        if (Reaction.Status != AnimationStatus.Dismissed)
        {
            return ColorUtilities.Lerp(
                _inactivePressedThumbColor,
                _activePressedThumbColor,
                currentValue);
        }
        if (_positionController.Status == AnimationStatus.Forward)
        {
            return ColorUtilities.Lerp(_inactivePressedThumbColor, _activeThumbColor, currentValue);
        }
        if (_positionController.Status == AnimationStatus.Reverse)
        {
            return ColorUtilities.Lerp(_inactiveThumbColor, _activePressedThumbColor, currentValue);
        }
        return ColorUtilities.Lerp(_inactiveThumbColor, _activeThumbColor, currentValue);
    }

    private void PaintLabels(PaintingContext context, Rect trackRect, double visualPosition)
    {
        double reactionOpacity = 1.0 - Reaction.Value;
        double onOpacity = visualPosition * reactionOpacity;
        double offOpacity = (1.0 - visualPosition) * reactionOpacity;
        Point onCenter;
        Point offCenter;
        if (_textDirection == TextDirection.Ltr)
        {
            onCenter = new Point(trackRect.Left + 11.0, trackRect.Center.Y);
            offCenter = new Point(trackRect.Right - 12.0, trackRect.Center.Y);
        }
        else
        {
            onCenter = new Point(trackRect.Right - 11.0, trackRect.Center.Y);
            offCenter = new Point(trackRect.Left + 12.0, trackRect.Center.Y);
            (onOpacity, offOpacity) = (offOpacity, onOpacity);
        }

        Color onColor = WithOpacity(_onLabelColor, onOpacity);
        Color offColor = WithOpacity(_offLabelColor, offOpacity);
        context.Canvas.DrawRectangle(
            new SolidColorBrush(onColor),
            null,
            new Rect(onCenter.X - 0.5, onCenter.Y - 5.0, 1.0, 10.0));
        context.Canvas.DrawCircle(
            Brushes.Transparent,
            new Pen(new SolidColorBrush(offColor), 1.0),
            offCenter,
            5.0);
    }

    private void PaintThumb(
        PaintingContext context,
        Rect thumbBounds,
        Color thumbColor,
        ImageProvider? image,
        ImageErrorListener? imageError)
    {
        double radius = thumbBounds.Height / 2.0;
        context.Canvas.DrawRectangle(
            Brushes.Transparent,
            null,
            thumbBounds,
            BorderRadius.Circular(radius),
            ThumbShadows.ToAvalonia());
        context.Canvas.DrawRRect(
            RRect.FromRectAndRadius(thumbBounds.Inflate(0.5), radius + 0.5),
            new SolidColorBrush(ThumbBorderColor),
            null);

        try
        {
            _isPainting = true;
            if (_thumbPainter is null
                || _cachedThumbColor != thumbColor
                || !ReferenceEquals(_cachedThumbImage, image)
                || !ReferenceEquals(_cachedThumbImageError, imageError))
            {
                _thumbPainter?.Dispose();
                _cachedThumbColor = thumbColor;
                _cachedThumbImage = image;
                _cachedThumbImageError = imageError;
                var decoration = new ShapeDecoration(
                    Shape: new StadiumBorder(),
                    Color: thumbColor,
                    Image: image is null ? null : new DecorationImage(image, onError: imageError));
                _thumbPainter = decoration.CreateBoxPainter(HandleDecorationChanged);
            }

            _thumbPainter.Paint(
                context,
                thumbBounds.Position,
                _imageConfiguration.CopyWith(size: thumbBounds.Size));
        }
        finally
        {
            _isPainting = false;
        }
    }

    private void PaintIcon(PaintingContext context, Rect thumbBounds, Icon icon, double currentValue)
    {
        Color inactiveColor = _inactiveIcon?.Color ?? CupertinoColors.Black;
        Color activeColor = _activeIcon?.Color ?? CupertinoColors.Black;
        Color iconColor = ColorUtilities.Lerp(inactiveColor, activeColor, currentValue);
        double iconSize = icon.Size ?? DefaultIconSize;
        FontWeight weight = icon.FontWeight ?? ResolveFontWeight(icon.Weight ?? _iconTheme.Weight);
        var style = new TextStyle(
            FontFamily: Icon.ResolveFontFamily(icon.IconData!),
            FontSize: iconSize,
            Color: iconColor,
            FontWeight: weight,
            Height: 1.0,
            LetterSpacing: 0.0);
        _textPainter ??= new TextPainter(textDirection: _textDirection, maxLines: 1);
        _textPainter.TextDirection = _textDirection;
        _textPainter.Text = new TextSpan(char.ConvertFromUtf32(icon.IconData!.CodePoint), style: style);
        _textPainter.Layout();
        Point offset = new(
            thumbBounds.Left + ((thumbBounds.Width - iconSize) / 2.0),
            thumbBounds.Top + ((thumbBounds.Height - iconSize) / 2.0));
        _textPainter.Paint(context, offset);
    }

    private void HandleDecorationChanged()
    {
        if (!_isPainting)
        {
            NotifyPainterChanged();
        }
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(color.A * Math.Clamp(opacity, 0.0, 1.0)), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static FontWeight ResolveFontWeight(double? weight)
    {
        double value = weight ?? 400.0;
        if (value < 150.0) return FontWeight.Thin;
        if (value < 250.0) return FontWeight.ExtraLight;
        if (value < 350.0) return FontWeight.Light;
        if (value < 450.0) return FontWeight.Normal;
        if (value < 550.0) return FontWeight.Medium;
        if (value < 650.0) return FontWeight.SemiBold;
        if (value < 750.0) return FontWeight.Bold;
        if (value < 850.0) return FontWeight.ExtraBold;
        return FontWeight.Black;
    }
}
