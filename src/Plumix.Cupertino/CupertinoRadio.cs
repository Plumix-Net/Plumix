using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/radio.dart

/// A widget that builds a <see cref="RawRadio{T}"/> with a macOS-style UI.
///
/// Used to select between a number of mutually exclusive values. When one radio button in a group is
/// selected, the other radio buttons in the group are deselected. This widget typically has a
/// <see cref="RadioGroup{T}"/> ancestor, which takes in a group value, and the
/// <see cref="CupertinoRadio{T}"/> under it with a matching <see cref="Value"/> will be selected.
public sealed class CupertinoRadio<T> : StatefulWidget
{
    /// Creates a macOS-styled radio button.
    public CupertinoRadio(
        T value,
        T? groupValue = default,
        Action<T?>? onChanged = null,
        MouseCursor? mouseCursor = null,
        bool toggleable = false,
        Color? activeColor = null,
        Color? inactiveColor = null,
        Color? fillColor = null,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        bool useCheckmarkStyle = false,
        bool? enabled = null,
        RadioGroupRegistry<T>? groupRegistry = null,
        Key? key = null) : base(key)
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        MouseCursor = mouseCursor;
        Toggleable = toggleable;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        FillColor = fillColor;
        FocusColor = focusColor;
        FocusNode = focusNode;
        Autofocus = autofocus;
        UseCheckmarkStyle = useCheckmarkStyle;
        Enabled = enabled;
        GroupRegistry = groupRegistry;
    }

    /// The value represented by this radio button.
    public T Value { get; }

    /// The currently selected value for a group of radio buttons. This radio button is considered
    /// selected if its <see cref="Value"/> matches the group value.
    [Obsolete("Use a RadioGroup ancestor to manage group value instead. "
              + "Mirrors Flutter's deprecation after v3.32.0-0.0.pre.")]
    public T? GroupValue { get; }

    /// Called when the user selects this radio button. If null, the radio button is displayed as
    /// disabled. The callback is not invoked when this radio button is already selected and
    /// <see cref="Toggleable"/> is not set; with <see cref="Toggleable"/>, tapping an already
    /// selected radio invokes it with null.
    [Obsolete("Use RadioGroup to handle value change instead. "
              + "Mirrors Flutter's deprecation after v3.32.0-0.0.pre.")]
    public Action<T?>? OnChanged { get; }

    /// The cursor for a mouse pointer when it enters or is hovering over the widget. If null, then
    /// `SystemMouseCursors.basic` is used when this radio button is disabled; when enabled,
    /// `SystemMouseCursors.click` is used on Web and `SystemMouseCursors.basic` elsewhere.
    public MouseCursor? MouseCursor { get; }

    /// Whether tapping an already selected radio button deselects it, reporting null.
    public bool Toggleable { get; }

    /// Controls whether the radio displays in a checkbox style or the default iOS radio style.
    /// Defaults to false.
    public bool UseCheckmarkStyle { get; }

    /// The color to use when this radio button is selected. Defaults to `CupertinoColors.activeBlue`.
    public Color? ActiveColor { get; }

    /// The color to use when this radio button is not selected. Defaults to `CupertinoColors.white`.
    public Color? InactiveColor { get; }

    /// The color that fills the inner circle of the radio button when selected. Defaults to
    /// `CupertinoColors.white`.
    public Color? FillColor { get; }

    /// The color for the radio's border when it has the input focus. If null, then a paler form of
    /// the <see cref="ActiveColor"/> is used.
    public Color? FocusColor { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    /// The registry this radio button reports to. Unless provided, the <see cref="BuildContext"/> is
    /// used to look up the ancestor <see cref="RadioGroupRegistry{T}"/>.
    public RadioGroupRegistry<T>? GroupRegistry { get; }

    /// Whether this widget is interactive. If not provided, this widget is interactive when an
    /// <see cref="OnChanged"/> is provided, a <see cref="RadioGroup{T}"/> with the same type sits
    /// above it, or a <see cref="GroupRegistry"/> is provided.
    public bool? Enabled { get; }

    public override State CreateState() => new CupertinoRadioState();

    private sealed class CupertinoRadioState : State
    {
        private FocusNode? _internalFocusNode;
        private RadioRegistry? _internalRadioRegistry;

        internal CupertinoRadio<T> CurrentWidget => (CupertinoRadio<T>)StateWidget;

        private FocusNode EffectiveFocusNode =>
            CurrentWidget.FocusNode ?? (_internalFocusNode ??= new FocusNode());

        private bool IsEnabled => CurrentWidget.Enabled
                                  ?? (CurrentWidget.OnChanged is not null
                                      || CurrentWidget.GroupRegistry is not null
                                      || RadioGroup<T>.MaybeOf(Context) is not null);

        private RadioGroupRegistry<T> EffectiveRegistry
        {
            get
            {
                if (CurrentWidget.GroupRegistry is not null)
                {
                    return CurrentWidget.GroupRegistry;
                }

                RadioGroupRegistry<T>? inheritedRegistry = RadioGroup<T>.MaybeOf(Context);
                if (inheritedRegistry is not null)
                {
                    return inheritedRegistry;
                }

                // Handles deprecated API.
                return _internalRadioRegistry ??= new RadioRegistry(this);
            }
        }

        public override void Dispose()
        {
            _internalFocusNode?.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            if ((CurrentWidget.Enabled ?? false)
                && CurrentWidget.OnChanged is null
                && CurrentWidget.GroupRegistry is null
                && RadioGroup<T>.MaybeOf(context) is null)
            {
                throw new InvalidOperationException(
                    "Radio is enabled but has no CupertinoRadio.OnChanged, "
                    + "CupertinoRadio.GroupRegistry, or RadioGroup above");
            }

            WidgetStateProperty<MouseCursor> effectiveMouseCursor =
                WidgetStateProperty<MouseCursor>.ResolveWith(states =>
                    (CurrentWidget.MouseCursor is WidgetStateMouseCursor stateCursor
                        ? stateCursor.Resolve(states)
                        : CurrentWidget.MouseCursor)
                    ?? (!states.Contains(WidgetState.Disabled) && PlatformDefaults.IsWeb
                        ? SystemMouseCursors.Click
                        : SystemMouseCursors.Basic));

            return new RawRadio<T>(
                value: CurrentWidget.Value,
                groupRegistry: EffectiveRegistry,
                mouseCursor: effectiveMouseCursor,
                toggleable: CurrentWidget.Toggleable,
                focusNode: EffectiveFocusNode,
                autofocus: CurrentWidget.Autofocus,
                enabled: IsEnabled,
                builder: (_, state) => new CupertinoRadioPaint<T>(
                    activeColor: CurrentWidget.ActiveColor,
                    inactiveColor: CurrentWidget.InactiveColor,
                    fillColor: CurrentWidget.FillColor,
                    focusColor: CurrentWidget.FocusColor,
                    useCheckmarkStyle: CurrentWidget.UseCheckmarkStyle,
                    isActive: IsEnabled,
                    toggleableState: state,
                    focused: EffectiveFocusNode.HasFocus));
        }

        /// A registry for deprecated API.
        private sealed class RadioRegistry : RadioGroupRegistry<T>
        {
            private readonly CupertinoRadioState _state;

            public RadioRegistry(CupertinoRadioState state)
            {
                _state = state;
            }

            public override T? GroupValue => _state.CurrentWidget.GroupValue;

            public override Action<T?> OnChanged => _state.CurrentWidget.OnChanged!;

            public override void RegisterClient(RadioClient<T> radio)
            {
            }

            public override void UnregisterClient(RadioClient<T> radio)
            {
            }
        }
    }
}

internal sealed class CupertinoRadioPaint<T> : StatefulWidget
{
    public CupertinoRadioPaint(
        bool focused,
        RawRadioState<T> toggleableState,
        Color? activeColor,
        Color? inactiveColor,
        Color? fillColor,
        Color? focusColor,
        bool useCheckmarkStyle,
        bool isActive)
    {
        Focused = focused;
        ToggleableState = toggleableState;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        FillColor = fillColor;
        FocusColor = focusColor;
        UseCheckmarkStyle = useCheckmarkStyle;
        IsActive = isActive;
    }

    public RawRadioState<T> ToggleableState { get; }

    public Color? ActiveColor { get; }

    public Color? InactiveColor { get; }

    public Color? FillColor { get; }

    public Color? FocusColor { get; }

    public bool UseCheckmarkStyle { get; }

    public bool IsActive { get; }

    public bool Focused { get; }

    public override State CreateState() => new CupertinoRadioPaintState();

    private sealed class CupertinoRadioPaintState : State
    {
        private CupertinoRadioPainter? _painter;

        private CupertinoRadioPaint<T> CurrentWidget => (CupertinoRadioPaint<T>)StateWidget;

        public override void Dispose()
        {
            _painter?.Dispose();
            _painter = null;
            base.Dispose();
        }

        private WidgetStateProperty<Color> DefaultOuterColor => WidgetStateProperty<Color>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Disabled))
            {
                return CupertinoRadioPainter.KDisabledOuterColor;
            }
            if (states.Contains(WidgetState.Selected))
            {
                return CurrentWidget.ActiveColor
                       ?? CupertinoDynamicColor.Resolve(CupertinoRadioPainter.KDefaultOuterColor, Context);
            }
            return CurrentWidget.InactiveColor ?? CupertinoColors.White;
        });

        private WidgetStateProperty<Color> DefaultInnerColor => WidgetStateProperty<Color>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Disabled) && states.Contains(WidgetState.Selected))
            {
                return CurrentWidget.FillColor
                       ?? CupertinoDynamicColor.Resolve(CupertinoRadioPainter.KDisabledInnerColor, Context);
            }
            if (states.Contains(WidgetState.Selected))
            {
                return CurrentWidget.FillColor
                       ?? CupertinoDynamicColor.Resolve(CupertinoRadioPainter.KDefaultInnerColor, Context);
            }
            return CupertinoColors.White;
        });

        private WidgetStateProperty<Color> DefaultBorderColor => WidgetStateProperty<Color>.ResolveWith(states =>
        {
            if ((states.Contains(WidgetState.Selected) || states.Contains(WidgetState.Focused))
                && !states.Contains(WidgetState.Disabled))
            {
                return CupertinoColors.Transparent;
            }
            if (states.Contains(WidgetState.Disabled))
            {
                return CupertinoDynamicColor.Resolve(CupertinoRadioPainter.KDisabledBorderColor, Context);
            }
            return CupertinoDynamicColor.Resolve(CupertinoRadioPainter.KDefaultBorderColor, Context);
        });

        public override Widget Build(BuildContext context)
        {
            RawRadioState<T> toggleableState = CurrentWidget.ToggleableState;
            _painter ??= new CupertinoRadioPainter(
                toggleableState.PositionAnimation,
                toggleableState.ReactionAnimation,
                toggleableState.ReactionHoverFadeAnimation,
                toggleableState.ReactionFocusFadeAnimation);

            // Colors need to be resolved in selected and non selected states separately.
            var activeStates = new HashSet<WidgetState>(toggleableState.States) { WidgetState.Selected };
            var inactiveStates = new HashSet<WidgetState>(toggleableState.States);
            inactiveStates.Remove(WidgetState.Selected);

            // Since the states getter always makes a new set, make a copy to use throughout the
            // lifecycle of this build method.
            IReadOnlySet<WidgetState> currentStates = toggleableState.States;

            Color effectiveActiveColor = DefaultOuterColor.Resolve(activeStates);

            Color effectiveInactiveColor = DefaultOuterColor.Resolve(inactiveStates);

            Color effectiveFocusOverlayColor = CurrentWidget.FocusColor
                ?? HSLColor.FromColor(CupertinoRadioPainter.WithOpacity(
                        effectiveActiveColor,
                        CupertinoConstants.CupertinoFocusColorOpacity))
                    .WithLightness(CupertinoConstants.CupertinoFocusColorBrightness)
                    .WithSaturation(CupertinoConstants.CupertinoFocusColorSaturation)
                    .ToColor();

            Color effectiveFillColor = DefaultInnerColor.Resolve(currentStates);

            Color effectiveBorderColor = DefaultBorderColor.Resolve(currentStates);

            _painter.Configure(
                focusColor: effectiveFocusOverlayColor,
                downPosition: toggleableState.PressPosition,
                isFocused: CurrentWidget.Focused,
                activeColor: effectiveActiveColor,
                inactiveColor: effectiveInactiveColor,
                fillColor: effectiveFillColor,
                value: toggleableState.Selected,
                checkmarkStyle: CurrentWidget.UseCheckmarkStyle,
                isActive: CurrentWidget.IsActive,
                borderColor: effectiveBorderColor,
                brightness: CupertinoTheme.Of(context).Brightness);

            return new CustomPaint(painter: _painter, size: CupertinoRadioPainter.KSize);
        }
    }
}

internal sealed class CupertinoRadioPainter : ToggleablePainter
{
    internal static readonly Size KSize = new(18.0, 18.0);
    internal const double KOuterRadius = 7.0;
    internal const double KInnerRadius = 2.975;

    // Eyeballed from a radio on a physical Macbook Pro running macOS version 14.5.
    internal static readonly Color KDisabledOuterColor = WithOpacity(CupertinoColors.White, 0.50);
    internal static readonly CupertinoDynamicColor KDisabledInnerColor = CupertinoDynamicColor.WithBrightness(
        color: Color.FromArgb(64, 0, 0, 0),
        darkColor: Color.FromArgb(64, 255, 255, 255));
    internal static readonly CupertinoDynamicColor KDisabledBorderColor = CupertinoDynamicColor.WithBrightness(
        color: Color.FromArgb(64, 0, 0, 0),
        darkColor: Color.FromArgb(64, 0, 0, 0));
    internal static readonly CupertinoDynamicColor KDefaultBorderColor = CupertinoDynamicColor.WithBrightness(
        color: Color.FromArgb(255, 209, 209, 214),
        darkColor: Color.FromArgb(64, 0, 0, 0));
    internal static readonly CupertinoDynamicColor KDefaultInnerColor = CupertinoDynamicColor.WithBrightness(
        color: CupertinoColors.White,
        darkColor: Color.FromArgb(255, 222, 232, 248));
    internal static readonly CupertinoDynamicColor KDefaultOuterColor = CupertinoDynamicColor.WithBrightness(
        color: CupertinoColors.ActiveBlue.Value,
        darkColor: Color.FromArgb(255, 50, 100, 215));
    internal const double KPressedOverlayOpacity = 0.15;
    internal const double KCheckmarkStrokeWidth = 2.0;
    internal const double KFocusOutlineStrokeWidth = 3.0;
    internal const double KBorderOutlineStrokeWidth = 0.3;
    // In dark mode, the outer color of a radio is an opacity gradient of the background color.
    internal static readonly IReadOnlyList<double> KDarkGradientOpacities = [0.14, 0.29];
    internal static readonly IReadOnlyList<double> KDisabledDarkGradientOpacities = [0.08, 0.14];

    private bool? _value;
    private Color _fillColor;
    private bool _checkmarkStyle;
    private PlatformBrightness? _brightness;
    private Color _borderColor;

    public CupertinoRadioPainter(
        Animation<double> position,
        Animation<double> reaction,
        Animation<double> reactionHoverFade,
        Animation<double> reactionFocusFade)
        : base(position, reaction, reactionHoverFade, reactionFocusFade)
    {
    }

    internal bool? Value => _value;

    internal Color FillColor => _fillColor;

    internal bool CheckmarkStyle => _checkmarkStyle;

    internal PlatformBrightness? Brightness => _brightness;

    internal Color BorderColor => _borderColor;

    internal Color EffectiveFocusColor => FocusColor;

    internal void Configure(
        Color focusColor,
        Point? downPosition,
        bool isFocused,
        Color activeColor,
        Color inactiveColor,
        Color fillColor,
        bool? value,
        bool checkmarkStyle,
        bool isActive,
        Color borderColor,
        PlatformBrightness? brightness)
    {
        FocusColor = focusColor;
        DownPosition = downPosition;
        IsFocused = isFocused;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        _fillColor = fillColor;
        _value = value;
        _checkmarkStyle = checkmarkStyle;
        IsActive = isActive;
        _borderColor = borderColor;
        _brightness = brightness;
        NotifyPainterChanged();
    }

    private void DrawPressedOverlay(PaintingContext context, Point center, double radius)
    {
        Color pressedColor = _brightness == PlatformBrightness.Light
            ? WithOpacity(CupertinoColors.Black, KPressedOverlayOpacity)
            : WithOpacity(CupertinoColors.White, KPressedOverlayOpacity);
        context.DrawCircle(new SolidColorBrush(pressedColor), pen: null, center, radius);
    }

    private static void DrawFillGradient(
        PaintingContext context,
        Point center,
        double radius,
        Color topColor,
        Color bottomColor)
    {
        var fillGradient = new LinearGradient(
            colors: [topColor, bottomColor],
            begin: Alignment.TopCenter,
            end: Alignment.BottomCenter);
        Rect circleRect = RectFromCircle(center, radius);
        // Dart fills `Path()..addOval(circleRect)`; an oval fill is the same geometry.
        context.DrawOval(circleRect, fillGradient.CreateShader(circleRect), pen: null);
    }

    private void DrawOuterBorder(PaintingContext context, Point center)
    {
        var borderPen = new Pen(new SolidColorBrush(_borderColor), KBorderOutlineStrokeWidth);
        context.DrawOval(RectFromCircle(center, KOuterRadius), brush: null, borderPen);
    }

    public override void Paint(PaintingContext context, Size size)
    {
        var center = new Point(size.Width / 2.0, size.Height / 2.0);

        if (_checkmarkStyle)
        {
            if (_value ?? false)
            {
                var path = new Plumix.UI.Path();
                var checkPen = new Pen(
                    new SolidColorBrush(ActiveColor),
                    KCheckmarkStrokeWidth,
                    lineCap: PenLineCap.Round);
                double width = KSize.Width;
                var origin = new Point(center.X - (width / 2.0), center.Y - (width / 2.0));
                var start = new Point(width * 0.25, width * 0.52);
                var mid = new Point(width * 0.46, width * 0.75);
                var end = new Point(width * 0.85, width * 0.29);
                path.MoveTo(origin.X + start.X, origin.Y + start.Y);
                path.LineTo(origin.X + mid.X, origin.Y + mid.Y);
                context.DrawPath(path, brush: null, checkPen);
                path.MoveTo(origin.X + mid.X, origin.Y + mid.Y);
                path.LineTo(origin.X + end.X, origin.Y + end.Y);
                context.DrawPath(path, brush: null, checkPen);
            }
        }
        else if (_value ?? false)
        {
            Color outerColor = ActiveColor;
            // Draw a gradient in dark mode if the radio is disabled.
            if (_brightness == PlatformBrightness.Dark && !IsActive)
            {
                DrawFillGradient(
                    context,
                    center,
                    KOuterRadius,
                    WithOpacity(
                        outerColor,
                        IsActive ? KDarkGradientOpacities[0] : KDisabledDarkGradientOpacities[0]),
                    WithOpacity(
                        outerColor,
                        IsActive ? KDarkGradientOpacities[1] : KDisabledDarkGradientOpacities[1]));
            }
            else
            {
                context.DrawCircle(new SolidColorBrush(outerColor), pen: null, center, KOuterRadius);
            }
            // The outer circle's opacity changes when the radio is pressed.
            if (DownPosition is not null)
            {
                DrawPressedOverlay(context, center, KOuterRadius);
            }
            context.DrawCircle(new SolidColorBrush(_fillColor), pen: null, center, KInnerRadius);
            // Draw an outer border if the radio is disabled and selected.
            if (!IsActive)
            {
                DrawOuterBorder(context, center);
            }
        }
        else
        {
            Color paintColor = IsActive ? InactiveColor : KDisabledOuterColor;
            if (_brightness == PlatformBrightness.Dark)
            {
                DrawFillGradient(
                    context,
                    center,
                    KOuterRadius,
                    WithOpacity(
                        paintColor,
                        IsActive ? KDarkGradientOpacities[0] : KDisabledDarkGradientOpacities[0]),
                    WithOpacity(
                        paintColor,
                        IsActive ? KDarkGradientOpacities[1] : KDisabledDarkGradientOpacities[1]));
            }
            else
            {
                context.DrawCircle(new SolidColorBrush(paintColor), pen: null, center, KOuterRadius);
            }
            // The entire circle's opacity changes when the radio is pressed.
            if (DownPosition is not null)
            {
                DrawPressedOverlay(context, center, KOuterRadius);
            }
            DrawOuterBorder(context, center);
        }

        if (IsFocused)
        {
            var focusPen = new Pen(new SolidColorBrush(FocusColor), KFocusOutlineStrokeWidth);
            context.DrawOval(
                RectFromCircle(center, KOuterRadius + (KFocusOutlineStrokeWidth / 2.0)),
                brush: null,
                focusPen);
        }
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return !ReferenceEquals(this, oldDelegate);
    }

    private static Rect RectFromCircle(Point center, double radius)
    {
        return new Rect(center.X - radius, center.Y - radius, radius * 2.0, radius * 2.0);
    }

    // Dart's `Color.withOpacity`: replaces the alpha channel outright.
    internal static Color WithOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp(
            (int)Math.Round(byte.MaxValue * Math.Clamp(opacity, 0.0, 1.0)),
            0,
            byte.MaxValue);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
