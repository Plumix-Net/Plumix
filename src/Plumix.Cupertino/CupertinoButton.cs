using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/button.dart

// Measured against iOS (17) Human Interface Guidelines.

/// <summary>
/// The size of a <see cref="CupertinoButton"/>, based on the iOS (17) Human Interface Guidelines.
/// </summary>
public enum CupertinoButtonSize
{
    /// <summary>
    /// A smaller button with round sides and smaller text (uses
    /// <see cref="CupertinoTextThemeData.ActionSmallTextStyle"/>).
    /// </summary>
    Small,

    /// <summary>A medium sized button with round sides and regular-sized text.</summary>
    Medium,

    /// <summary>A (classic) large button with rounded edges and regular-sized text.</summary>
    Large,
}

/// <summary>
/// The style of a <see cref="CupertinoButton"/> that changes the style of the button's background.
/// Ports Dart's private `_CupertinoButtonStyle`.
/// </summary>
internal enum CupertinoButtonStyle
{
    /// <summary>No background or border, primary foreground color.</summary>
    Plain,

    /// <summary>Translucent background, primary foreground color.</summary>
    Tinted,

    /// <summary>Solid background, contrasting foreground color.</summary>
    Filled,
}

/// <summary>
/// An iOS-style button.
///
/// Takes in a text or an icon that fades out and in on touch. May optionally have a background.
///
/// The <see cref="Padding"/> defaults to 16.0 pixels. When using a <see cref="CupertinoButton"/>
/// within a fixed height parent, like a <see cref="CupertinoNavigationBar"/>, a smaller, or even
/// zero, padding should be used to prevent clipping larger <see cref="Child"/> widgets.
///
/// Preserves any parent <see cref="IconThemeData"/> but overwrites its
/// <see cref="IconThemeData.Color"/> with the theme's primary color (or its primary contrasting
/// color if the button is filled).
/// </summary>
public sealed class CupertinoButton : StatefulWidget
{
    /// <summary>Creates an iOS-style button.</summary>
    public CupertinoButton(
        Widget child,
        Action? onPressed,
        CupertinoButtonSize sizeStyle = CupertinoButtonSize.Large,
        EdgeInsetsGeometry? padding = null,
        CupertinoDynamicColor? color = null,
        Color? foregroundColor = null,
        CupertinoDynamicColor? disabledColor = null,
        double? minSize = null,
        Size? minimumSize = null,
        double? pressedOpacity = 0.4,
        BorderRadius? borderRadius = null,
        AlignmentGeometry alignment = default,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        MouseCursor? mouseCursor = null,
        Action? onLongPress = null,
        Key? key = null)
        : this(
            CupertinoButtonStyle.Plain,
            child,
            onPressed,
            sizeStyle,
            padding,
            color,
            foregroundColor,
            disabledColor ?? CupertinoColors.QuaternarySystemFill,
            minSize,
            minimumSize,
            pressedOpacity,
            borderRadius,
            alignment,
            focusColor,
            focusNode,
            onFocusChange,
            autofocus,
            mouseCursor,
            onLongPress,
            key)
    {
    }

    private CupertinoButton(
        CupertinoButtonStyle style,
        Widget child,
        Action? onPressed,
        CupertinoButtonSize sizeStyle,
        EdgeInsetsGeometry? padding,
        CupertinoDynamicColor? color,
        Color? foregroundColor,
        CupertinoDynamicColor disabledColor,
        double? minSize,
        Size? minimumSize,
        double? pressedOpacity,
        BorderRadius? borderRadius,
        AlignmentGeometry alignment,
        Color? focusColor,
        FocusNode? focusNode,
        Action<bool>? onFocusChange,
        bool autofocus,
        MouseCursor? mouseCursor,
        Action? onLongPress,
        Key? key) : base(key)
    {
        // Dart asserts the opacity range on the default and `.filled` constructors only; `.tinted`
        // omits it. The check is shared here because an out-of-range opacity is invalid either way.
        if (pressedOpacity is { } opacity && (opacity < 0.0 || opacity > 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(pressedOpacity));
        }

        if (minimumSize is not null && minSize is not null)
        {
            throw new ArgumentException(
                "Only one of minimumSize and minSize may be specified.",
                nameof(minimumSize));
        }

        Style = style;
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnPressed = onPressed;
        SizeStyle = sizeStyle;
        Padding = padding;
        Color = color;
        ForegroundColor = foregroundColor;
        DisabledColor = disabledColor;
        MinSize = minSize;
        MinimumSize = minimumSize;
        PressedOpacity = pressedOpacity;
        BorderRadius = borderRadius;

        // `default(AlignmentGeometry)` is `Alignment.Center`, Dart's default for this argument.
        Alignment = alignment;
        FocusColor = focusColor;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        MouseCursor = mouseCursor;
        OnLongPress = onLongPress;
    }

    /// <summary>
    /// Creates an iOS-style button with a tinted background.
    ///
    /// The background color is derived from the <see cref="CupertinoTheme"/>'s primary color plus
    /// transparency; the foreground color is that primary color. To specify a custom background
    /// color, use the <paramref name="color"/> argument; to match the iOS "grey" button style, set
    /// it to `CupertinoColors.SystemGrey`.
    /// </summary>
    public static CupertinoButton Tinted(
        Widget child,
        Action? onPressed,
        CupertinoButtonSize sizeStyle = CupertinoButtonSize.Large,
        EdgeInsetsGeometry? padding = null,
        CupertinoDynamicColor? color = null,
        Color? foregroundColor = null,
        CupertinoDynamicColor? disabledColor = null,
        double? minSize = null,
        Size? minimumSize = null,
        double? pressedOpacity = 0.4,
        BorderRadius? borderRadius = null,
        AlignmentGeometry alignment = default,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        MouseCursor? mouseCursor = null,
        Action? onLongPress = null,
        Key? key = null)
    {
        return new CupertinoButton(
            CupertinoButtonStyle.Tinted,
            child,
            onPressed,
            sizeStyle,
            padding,
            color,
            foregroundColor,
            disabledColor ?? CupertinoColors.TertiarySystemFill,
            minSize,
            minimumSize,
            pressedOpacity,
            borderRadius,
            alignment,
            focusColor,
            focusNode,
            onFocusChange,
            autofocus,
            mouseCursor,
            onLongPress,
            key);
    }

    /// <summary>
    /// Creates an iOS-style button with a filled background.
    ///
    /// The background color is derived from the <paramref name="color"/> argument; the foreground
    /// color is the <see cref="CupertinoTheme"/>'s primary contrasting color.
    /// </summary>
    public static CupertinoButton Filled(
        Widget child,
        Action? onPressed,
        CupertinoButtonSize sizeStyle = CupertinoButtonSize.Large,
        EdgeInsetsGeometry? padding = null,
        CupertinoDynamicColor? color = null,
        Color? foregroundColor = null,
        CupertinoDynamicColor? disabledColor = null,
        double? minSize = null,
        Size? minimumSize = null,
        double? pressedOpacity = 0.4,
        BorderRadius? borderRadius = null,
        AlignmentGeometry alignment = default,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        MouseCursor? mouseCursor = null,
        Action? onLongPress = null,
        Key? key = null)
    {
        return new CupertinoButton(
            CupertinoButtonStyle.Filled,
            child,
            onPressed,
            sizeStyle,
            padding,
            color,
            foregroundColor,
            disabledColor ?? CupertinoColors.TertiarySystemFill,
            minSize,
            minimumSize,
            pressedOpacity,
            borderRadius,
            alignment,
            focusColor,
            focusNode,
            onFocusChange,
            autofocus,
            mouseCursor,
            onLongPress,
            key);
    }

    /// <summary>The widget below this widget in the tree. Typically a <see cref="Text"/> widget.</summary>
    public Widget Child { get; }

    /// <summary>
    /// The amount of space to surround the child inside the bounds of the button. Defaults to the
    /// <see cref="CupertinoConstants.CupertinoButtonPadding"/> entry for <see cref="SizeStyle"/>.
    /// </summary>
    public EdgeInsetsGeometry? Padding { get; }

    /// <summary>
    /// The color of the button's background. Defaults to null, which produces a button with no
    /// background or border; to the theme's primary color for <see cref="Filled"/>/<see cref="Tinted"/>.
    /// </summary>
    public CupertinoDynamicColor? Color { get; }

    /// <summary>
    /// The color of the button's background when the button is disabled. Ignored if the button does
    /// not also have a <see cref="Color"/>.
    /// </summary>
    public CupertinoDynamicColor DisabledColor { get; }

    /// <summary>The color of the button's text and icons.</summary>
    public Color? ForegroundColor { get; }

    /// <summary>
    /// The callback that is called when the button is tapped or otherwise activated. If both this
    /// and <see cref="OnLongPress"/> are null the button is disabled.
    /// </summary>
    public Action? OnPressed { get; }

    /// <summary>
    /// The callback that is called when the button is long-pressed. If both this and
    /// <see cref="OnPressed"/> are null the button is disabled.
    /// </summary>
    public Action? OnLongPress { get; }

    /// <summary>Minimum size of the button, applied to both dimensions.</summary>
    [Obsolete("Use MinimumSize instead. "
              + "Mirrors Flutter's deprecation after v3.28.0-3.0.pre.")]
    public double? MinSize { get; }

    /// <summary>
    /// The minimum size of the button. Defaults to a square of
    /// <see cref="CupertinoConstants.CupertinoButtonMinSize"/> for <see cref="SizeStyle"/>.
    /// </summary>
    public Size? MinimumSize { get; }

    /// <summary>
    /// The opacity that the button fades to when it is pressed; 1.0 when not pressed. Defaults to
    /// 0.4. If null, the opacity does not change on press.
    /// </summary>
    public double? PressedOpacity { get; }

    /// <summary>
    /// The radius of the button's corners when it has a background color. Defaults to the
    /// <see cref="CupertinoConstants.CupertinoButtonSizeBorderRadius"/> entry for <see cref="SizeStyle"/>.
    /// </summary>
    public BorderRadius? BorderRadius { get; }

    /// <summary>The size of the button. Defaults to <see cref="CupertinoButtonSize.Large"/>.</summary>
    public CupertinoButtonSize SizeStyle { get; }

    /// <summary>The alignment of the button's <see cref="Child"/>. Defaults to `Alignment.Center`.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>
    /// The color to use for the focus highlight for keyboard interactions. If null, a slightly
    /// transparent form of <see cref="Color"/> (or `CupertinoColors.ActiveBlue`) is used.
    /// </summary>
    public Color? FocusColor { get; }

    public FocusNode? FocusNode { get; }

    /// <summary>Called with true when this widget's node gains focus, and false when it loses it.</summary>
    public Action<bool>? OnFocusChange { get; }

    public bool Autofocus { get; }

    /// <summary>
    /// The cursor for a mouse pointer when it enters or is hovering over the widget. A
    /// <see cref="WidgetStateMouseCursor"/> is resolved for `Disabled`, `Pressed` and `Focused`.
    /// If null, `MouseCursor.Defer` is used when disabled; when enabled,
    /// `SystemMouseCursors.Click` is used on Web and `MouseCursor.Defer` elsewhere.
    /// </summary>
    public MouseCursor? MouseCursor { get; }

    internal CupertinoButtonStyle Style { get; }

    /// <summary>
    /// Whether the button is enabled. Buttons are disabled by default; set <see cref="OnPressed"/>
    /// or <see cref="OnLongPress"/> to a non-null value to enable one.
    /// </summary>
    public bool Enabled => OnPressed is not null || OnLongPress is not null;

    /// <summary>
    /// The distance a button needs to be moved after being pressed for its opacity to change.
    /// </summary>
    public static double TapMoveSlop()
    {
        return PlatformDefaults.TargetPlatform switch
        {
            TargetPlatform.IOS or TargetPlatform.Android or TargetPlatform.Fuchsia =>
                CupertinoConstants.CupertinoButtonTapMoveSlop,
            _ => 0.0,
        };
    }

    public override State CreateState() => new CupertinoButtonState();

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new FlagProperty("enabled", Enabled, ifFalse: "disabled"));
    }

    private sealed class CupertinoButtonState : State
    {
        // Eyeballed values. Feel free to tweak.
        private static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(120);
        private static readonly TimeSpan FadeInDuration = TimeSpan.FromMilliseconds(180);

        private static readonly WidgetStateMouseCursor DefaultCursor =
            WidgetStateMouseCursor.ResolveWith(states =>
                !states.Contains(WidgetState.Disabled) && PlatformDefaults.IsWeb
                    ? SystemMouseCursors.Click
                    : Widgets.MouseCursor.Defer);

        private readonly DoubleTween _opacityTween = new(begin: 1.0);
        private readonly Dictionary<Type, FlutterAction> _actionMap;

        private AnimationController _animationController = null!;
        private Animation<double> _opacityAnimation = null!;
        private bool _isFocused;
        private bool _buttonHeldDown;
        private bool _tapInProgress;

        public CupertinoButtonState()
        {
            _actionMap = new Dictionary<Type, FlutterAction>
            {
                [typeof(ActivateIntent)] = new CallbackAction<ActivateIntent>(_ =>
                {
                    HandleTap();
                    return null;
                }),
            };
        }

        private CupertinoButton Current => (CupertinoButton)StateWidget;

        public override void InitState()
        {
            base.InitState();
            _isFocused = false;
            _animationController = new AnimationController(
                duration: TimeSpan.FromMilliseconds(200),
                value: 0.0,
                vsync: this);
            _opacityAnimation = _animationController
                .Drive(new CurveTween(Curves.Decelerate))
                .Drive(_opacityTween);
            SetTween();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            SetTween();
        }

        public override void Dispose()
        {
            _animationController.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            bool enabled = Current.Enabled;
#pragma warning disable CS0618 // Dart still reads the deprecated minSize when minimumSize is null.
            Size? minimumSize = Current.MinimumSize
                                ?? (Current.MinSize is { } minSize ? new Size(minSize, minSize) : null);
#pragma warning restore CS0618
            CupertinoThemeData themeData = CupertinoTheme.Of(context);
            Color primaryColor = themeData.PrimaryColor;
            Color? unblendedColor = Current.Color is null
                ? Current.Style != CupertinoButtonStyle.Plain ? primaryColor : null
                : CupertinoDynamicColor.MaybeResolve(Current.Color, context);
            Color? backgroundColor = unblendedColor is { } blendable
                ? WithOpacity(
                    blendable,
                    Current.Style == CupertinoButtonStyle.Tinted
                        ? CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Light
                            ? CupertinoConstants.CupertinoButtonTintedOpacityLight
                            : CupertinoConstants.CupertinoButtonTintedOpacityDark
                        : Current.Color is { } widgetColor ? widgetColor.Value.A / 255.0 : 1.0)
                : null;

            Color effectiveForegroundColor =
                Current.ForegroundColor
                ?? (Current.Style, enabled) switch
                {
                    (CupertinoButtonStyle.Filled, _) => themeData.PrimaryContrastingColor,
                    (_, true) => primaryColor,
                    (_, false) => CupertinoDynamicColor.Resolve(CupertinoColors.TertiaryLabel, context),
                };

            Color effectiveFocusOutlineColor =
                Current.FocusColor
                ?? HSLColor.FromColor(
                        WithOpacity(
                            backgroundColor ?? CupertinoColors.ActiveBlue,
                            CupertinoConstants.CupertinoFocusColorOpacity))
                    .WithLightness(CupertinoConstants.CupertinoFocusColorBrightness)
                    .WithSaturation(CupertinoConstants.CupertinoFocusColorSaturation)
                    .ToColor();

            TextStyle textStyle =
                (Current.SizeStyle == CupertinoButtonSize.Small
                    ? themeData.TextTheme.ActionSmallTextStyle
                    : themeData.TextTheme.ActionTextStyle)
                .CopyWith(color: effectiveForegroundColor);
            IconThemeData iconTheme = IconTheme.Of(context).CopyWith(
                color: effectiveForegroundColor,
                size: textStyle.FontSize is { } fontSize
                    ? fontSize * 1.2
                    : CupertinoConstants.CupertinoButtonDefaultIconSize);

            DeviceGestureSettings? gestureSettings = MediaQuery.MaybeGestureSettingsOf(context);

            var states = new HashSet<WidgetState>();
            if (!enabled)
            {
                states.Add(WidgetState.Disabled);
            }

            if (_tapInProgress)
            {
                states.Add(WidgetState.Pressed);
            }

            if (_isFocused)
            {
                states.Add(WidgetState.Focused);
            }

            MouseCursor? resolvedCursor = Current.MouseCursor is WidgetStateMouseCursor stateCursor
                ? stateCursor.Resolve(states)
                : Current.MouseCursor;
            MouseCursor effectiveMouseCursor =
                resolvedCursor ?? DefaultCursor.Resolve(states) ?? Widgets.MouseCursor.Defer;

            var shapeDecoration = new ShapeDecoration(
                Shape: new RoundedSuperellipseBorder(
                    side: enabled && _isFocused
                        ? new BorderSide(
                            effectiveFocusOutlineColor,
                            width: 3.5,
                            strokeAlign: BorderSide.StrokeAlignOutside)
                        : BorderSide.None,
                    borderRadius: Current.BorderRadius
                                  ?? CupertinoConstants.CupertinoButtonSizeBorderRadius[Current.SizeStyle]),
                Color: backgroundColor is not null && !enabled
                    ? CupertinoDynamicColor.Resolve(Current.DisabledColor, context)
                    : backgroundColor);

            double fallbackMinSize = CupertinoConstants.CupertinoButtonMinSize
                .TryGetValue(Current.SizeStyle, out double sizeMinimum)
                ? sizeMinimum
                : CupertinoConstants.MinInteractiveDimensionCupertino;

            return new MouseRegion(
                cursor: effectiveMouseCursor,
                child: new FocusableActionDetector(
                    actions: _actionMap,
                    focusNode: Current.FocusNode,
                    autofocus: Current.Autofocus,
                    onFocusChange: Current.OnFocusChange,
                    onShowFocusHighlight: OnShowFocusHighlight,
                    enabled: enabled,
                    child: new RawGestureDetector(
                        behavior: HitTestBehavior.Opaque,
                        gestures: BuildGestures(enabled, gestureSettings),
                        child: new Semantics(
                            flags: SemanticsFlags.IsButton,
                            child: new ConstrainedBox(
                                new BoxConstraints(
                                    MinWidth: minimumSize?.Width ?? fallbackMinSize,
                                    MinHeight: minimumSize?.Height ?? fallbackMinSize),
                                new FadeTransition(
                                    opacity: _opacityAnimation,
                                    child: new DecoratedBox(
                                        decoration: shapeDecoration,
                                        child: new Padding(
                                            Current.Padding
                                            ?? CupertinoConstants.CupertinoButtonPadding[Current.SizeStyle],
                                            new Align(
                                                alignment: Current.Alignment,
                                                widthFactor: 1.0,
                                                heightFactor: 1.0,
                                                child: new DefaultTextStyle(
                                                    style: textStyle,
                                                    child: new IconTheme(
                                                        iconTheme,
                                                        Current.Child)))))))))));
        }

        private IReadOnlyDictionary<Type, IGestureRecognizerFactory> BuildGestures(
            bool enabled,
            DeviceGestureSettings? gestureSettings)
        {
            var gestures = new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TapGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                        () => new TapGestureRecognizer(postAcceptSlopTolerance: null),
                        instance =>
                        {
                            instance.OnTapDown = enabled ? HandleTapDown : null;
                            instance.OnTapUp = enabled ? HandleTapUp : null;
                            instance.OnTapCancel = enabled ? HandleTapCancel : null;
                            instance.OnTapMove = enabled ? HandleTapMove : null;
                            instance.GestureSettings = gestureSettings;
                        }),
            };

            if (Current.OnLongPress is not null)
            {
                gestures[typeof(LongPressGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                        () => new LongPressGestureRecognizer(),
                        instance =>
                        {
                            instance.OnLongPress = Current.OnLongPress;
                            instance.GestureSettings = gestureSettings;
                        });
            }

            return gestures;
        }

        private void SetTween()
        {
            _opacityTween.End = Current.PressedOpacity ?? 1.0;
        }

        private void HandleTapDown(PointerDownEvent @event)
        {
            SetState(() => _tapInProgress = true);
            if (!_buttonHeldDown)
            {
                _buttonHeldDown = true;
                Animate();
            }
        }

        private void HandleTapUp(PointerUpEvent @event)
        {
            SetState(() => _tapInProgress = false);
            if (_buttonHeldDown)
            {
                _buttonHeldDown = false;
                Animate();
            }

            if (Context.FindRenderObject() is not RenderBox renderObject)
            {
                return;
            }

            Point localPosition = renderObject.GlobalToLocal(@event.Position);
            if (Contains(renderObject.PaintBounds.Inflate(TapMoveSlop()), localPosition))
            {
                HandleTap();
            }
        }

        private void HandleTapCancel()
        {
            SetState(() => _tapInProgress = false);
            if (_buttonHeldDown)
            {
                _buttonHeldDown = false;
                Animate();
            }
        }

        private void HandleTapMove(TapMoveDetails details)
        {
            if (Context.FindRenderObject() is not RenderBox renderObject)
            {
                return;
            }

            Point localPosition = renderObject.GlobalToLocal(details.GlobalPosition);
            bool buttonShouldHeldDown =
                Contains(renderObject.PaintBounds.Inflate(TapMoveSlop()), localPosition);
            if (_tapInProgress && buttonShouldHeldDown != _buttonHeldDown)
            {
                _buttonHeldDown = buttonShouldHeldDown;
                Animate();
            }
        }

        private void HandleTap()
        {
            if (Current.OnPressed is not null)
            {
                Current.OnPressed();
                SemanticsService.SendEvent(
                    new TapSemanticEvent(Context.FindRenderObject()?.SemanticsNodeId));
            }
        }

        private void Animate()
        {
            if (_animationController.IsAnimating)
            {
                return;
            }

            bool wasHeldDown = _buttonHeldDown;
            TickerFuture ticker = _buttonHeldDown
                ? _animationController.AnimateTo(
                    1.0,
                    duration: FadeOutDuration,
                    curve: Curves.EaseInOutCubicEmphasized)
                : _animationController.AnimateTo(
                    0.0,
                    duration: FadeInDuration,
                    curve: Curves.EaseOutCubic);
            ticker.WhenComplete(() =>
            {
                if (Mounted && wasHeldDown != _buttonHeldDown)
                {
                    Animate();
                }
            });
        }

        private void OnShowFocusHighlight(bool showHighlight)
        {
            SetState(() => _isFocused = showHighlight);
        }

        // Dart's `Rect.contains`: the right and bottom edges are exclusive, unlike Avalonia's.
        private static bool Contains(Rect rect, Point point)
        {
            return point.X >= rect.Left
                   && point.X < rect.Right
                   && point.Y >= rect.Top
                   && point.Y < rect.Bottom;
        }

        // Dart's `Color.withOpacity`: replaces the alpha channel outright.
        private static Color WithOpacity(Color color, double opacity)
        {
            byte alpha = (byte)Math.Clamp(
                (int)Math.Round(byte.MaxValue * Math.Clamp(opacity, 0.0, 1.0)),
                0,
                byte.MaxValue);
            return Avalonia.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}
