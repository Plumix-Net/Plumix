using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/checkbox.dart

/// A macOS style checkbox.
///
/// The checkbox itself does not maintain any state. When the state of the checkbox changes, the
/// widget calls the <see cref="OnChanged"/> callback. The checkbox can optionally display three
/// values — true, false, and null — if <see cref="Tristate"/> is true; when <see cref="Value"/> is
/// null a dash is displayed.
public sealed class CupertinoCheckbox : StatefulWidget
{
    public CupertinoCheckbox(
        bool? value,
        Action<bool?>? onChanged,
        bool tristate = false,
        MouseCursor? mouseCursor = null,
        Color? activeColor = null,
        Color? inactiveColor = null,
        WidgetStateProperty<Color?>? fillColor = null,
        Color? checkColor = null,
        Color? focusColor = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        WidgetStateBorderSide? side = null,
        OutlinedBorder? shape = null,
        Size? tapTargetSize = null,
        string? semanticLabel = null,
        Key? key = null) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException(
                "Checkbox value cannot be null when tristate is false.",
                nameof(value));
        }

        Value = value;
        OnChanged = onChanged;
        Tristate = tristate;
        MouseCursor = mouseCursor;
        ActiveColor = activeColor;
        _inactiveColor = inactiveColor;
        FillColor = fillColor;
        CheckColor = checkColor;
        FocusColor = focusColor;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Side = side;
        Shape = shape;
        TapTargetSize = tapTargetSize;
        SemanticLabel = semanticLabel;
    }

    private readonly Color? _inactiveColor;

    /// Whether this checkbox is checked. When <see cref="Tristate"/> is true, a value of null
    /// corresponds to the mixed state.
    public bool? Value { get; }

    /// Called when the value of the checkbox should change. If null, the checkbox is displayed as
    /// disabled. When <see cref="Tristate"/> is true the callback cycles false => true => null.
    public Action<bool?>? OnChanged { get; }

    /// The cursor for a mouse pointer when it enters or is hovering over the widget. A
    /// <see cref="WidgetStateMouseCursor"/> resolves in the selected/focused/disabled states.
    public MouseCursor? MouseCursor { get; }

    /// The color to use when this checkbox is checked. Defaults to `CupertinoColors.activeBlue`.
    public Color? ActiveColor { get; }

    /// The color used to fill this checkbox, resolved in the selected/hovered/focused/disabled
    /// states. Takes precedence over <see cref="ActiveColor"/> when it resolves non-null.
    public WidgetStateProperty<Color?>? FillColor { get; }

    /// The color used if the checkbox is inactive. Currently unused: <see cref="FillColor"/>
    /// controls the background color in all states, including when unselected.
    [Obsolete("Use FillColor instead. FillColor now manages the background color in all states. "
              + "Mirrors Flutter's deprecation after v3.24.0-0.2.pre.")]
    public Color? InactiveColor => _inactiveColor;

    /// The color to use for the check icon when this checkbox is checked.
    public Color? CheckColor { get; }

    /// If true, the checkbox's <see cref="Value"/> can be true, false, or null.
    public bool Tristate { get; }

    /// The color for the checkbox's border shadow when it has the input focus. If null, a paler
    /// form of the <see cref="ActiveColor"/> is used.
    public Color? FocusColor { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    /// The color and width of the checkbox's border. A stateful side resolves in the
    /// pressed/selected/hovered/focused/disabled/error states; a plain side is only rendered when
    /// the checkbox's value is false, for backwards compatibility.
    public WidgetStateBorderSide? Side { get; }

    /// The shape of the checkbox. Defaults to a `RoundedRectangleBorder` with a circular corner
    /// radius of 4.0.
    public OutlinedBorder? Shape { get; }

    /// The tap target and layout size of the checkbox. If null, defaults to a square of
    /// <see cref="Width"/> pixels on desktop and `kMinInteractiveDimensionCupertino` on mobile.
    public Size? TapTargetSize { get; }

    /// The semantic label for the checkbox that is announced by screen readers.
    public string? SemanticLabel { get; }

    /// The width of a checkbox widget.
    public const double Width = 14.0;

    public override State CreateState() => new CupertinoCheckboxState();

    private sealed class CupertinoCheckboxState : ToggleableState
    {
        private CupertinoCheckboxPainter? _painter;
        private bool? _previousValue;

        private CupertinoCheckbox CurrentWidget => (CupertinoCheckbox)StateWidget;

        protected override bool IsInteractive => CurrentWidget.OnChanged is not null;

        protected override bool IsValueSelected => CurrentWidget.Value ?? true;

        public override void InitState()
        {
            base.InitState();
            _previousValue = CurrentWidget.Value;
            _painter = new CupertinoCheckboxPainter(
                Position,
                Reaction,
                ReactionHoverFade,
                ReactionFocusFade);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldCheckbox = (CupertinoCheckbox)oldWidget;
            base.DidUpdateWidget(oldWidget);
            if (oldCheckbox.Value != CurrentWidget.Value)
            {
                _previousValue = oldCheckbox.Value;
            }
        }

        public override void Dispose()
        {
            _painter?.Dispose();
            _painter = null;
            base.Dispose();
        }

        private WidgetStateProperty<Color> DefaultFillColor => WidgetStateProperty<Color>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Disabled))
            {
                return CupertinoCheckboxPainter.WithOpacity(CupertinoColors.White, 0.5);
            }
            if (states.Contains(WidgetState.Selected))
            {
                return CurrentWidget.ActiveColor
                       ?? CupertinoDynamicColor.Resolve(CupertinoCheckboxPainter.KDefaultFillColor, Context);
            }
            return CupertinoColors.White;
        });

        private WidgetStateProperty<Color> DefaultCheckColor => WidgetStateProperty<Color>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Disabled) && states.Contains(WidgetState.Selected))
            {
                return CurrentWidget.CheckColor
                       ?? CupertinoDynamicColor.Resolve(CupertinoCheckboxPainter.KDisabledCheckColor, Context);
            }
            if (states.Contains(WidgetState.Selected))
            {
                return CurrentWidget.CheckColor
                       ?? CupertinoDynamicColor.Resolve(CupertinoCheckboxPainter.KDefaultCheckColor, Context);
            }
            return CupertinoColors.White;
        });

        private WidgetStateProperty<BorderSide> DefaultSide => WidgetStateProperty<BorderSide>.ResolveWith(states =>
        {
            if ((states.Contains(WidgetState.Selected) || states.Contains(WidgetState.Focused))
                && !states.Contains(WidgetState.Disabled))
            {
                return new BorderSide(CupertinoColors.Transparent, 0.0);
            }
            if (states.Contains(WidgetState.Disabled))
            {
                return new BorderSide(
                    CupertinoDynamicColor.Resolve(CupertinoCheckboxPainter.KDisabledBorderColor, Context));
            }
            return new BorderSide(
                CupertinoDynamicColor.Resolve(CupertinoCheckboxPainter.KDefaultBorderColor, Context));
        });

        private static BorderSide? ResolveSide(WidgetStateBorderSide? side, IReadOnlySet<WidgetState> states)
        {
            // The wrapper carries Dart's `side is WidgetStateBorderSide` split: a stateful side
            // resolves with the states, a plain side only renders when not selected.
            return side?.Resolve(states);
        }

        public override Widget Build(BuildContext context)
        {
            // Colors need to be resolved in selected and non selected states separately.
            var activeStates = new HashSet<WidgetState>(CurrentWidgetStates) { WidgetState.Selected };
            var inactiveStates = new HashSet<WidgetState>(CurrentWidgetStates);
            inactiveStates.Remove(WidgetState.Selected);
            IReadOnlySet<WidgetState> currentStates = CurrentWidgetStates;

            Color effectiveActiveColor = CurrentWidget.FillColor?.Resolve(activeStates)
                                         ?? DefaultFillColor.Resolve(activeStates);

            Color effectiveInactiveColor = CurrentWidget.FillColor?.Resolve(inactiveStates)
                                           ?? DefaultFillColor.Resolve(inactiveStates);

            BorderSide effectiveBorderSide = ResolveSide(CurrentWidget.Side, currentStates)
                                             ?? DefaultSide.Resolve(currentStates);

            Color effectiveFocusOverlayColor = CurrentWidget.FocusColor
                ?? HSLColor.FromColor(CupertinoCheckboxPainter.WithOpacity(
                        effectiveActiveColor,
                        CupertinoConstants.CupertinoFocusColorOpacity))
                    .WithLightness(CupertinoConstants.CupertinoFocusColorBrightness)
                    .WithSaturation(CupertinoConstants.CupertinoFocusColorSaturation)
                    .ToColor();

            WidgetStateProperty<MouseCursor> effectiveMouseCursor =
                WidgetStateProperty<MouseCursor>.ResolveWith(states =>
                    (CurrentWidget.MouseCursor is WidgetStateMouseCursor stateCursor
                        ? stateCursor.Resolve(states)
                        : CurrentWidget.MouseCursor)
                    ?? (PlatformDefaults.IsWeb && !states.Contains(WidgetState.Disabled)
                        ? SystemMouseCursors.Click
                        : SystemMouseCursors.Basic));

            Size effectiveSize = CurrentWidget.TapTargetSize
                ?? PlatformDefaults.TargetPlatform switch
                {
                    TargetPlatform.IOS or TargetPlatform.Android or TargetPlatform.Fuchsia =>
                        new Size(
                            CupertinoConstants.MinInteractiveDimensionCupertino,
                            CupertinoConstants.MinInteractiveDimensionCupertino),
                    _ => new Size(CupertinoCheckbox.Width, CupertinoCheckbox.Width),
                };

            _painter!.Configure(
                focusColor: effectiveFocusOverlayColor,
                downPosition: DownPosition,
                isFocused: currentStates.Contains(WidgetState.Focused),
                isHovered: currentStates.Contains(WidgetState.Hovered),
                activeColor: effectiveActiveColor,
                inactiveColor: effectiveInactiveColor,
                checkColor: DefaultCheckColor.Resolve(currentStates),
                value: CurrentWidget.Value,
                previousValue: _previousValue,
                isActive: CurrentWidget.OnChanged is not null,
                shape: CurrentWidget.Shape
                       ?? new RoundedRectangleBorder(
                           borderRadius: Plumix.Rendering.BorderRadius.Circular(4.0)),
                side: effectiveBorderSide,
                brightness: CupertinoTheme.Of(context).Brightness);

            Widget toggleable = BuildToggleable(
                painter: _painter,
                size: effectiveSize,
                mouseCursor: effectiveMouseCursor,
                onTap: HandleTap,
                focusNode: CurrentWidget.FocusNode,
                onFocusChange: null,
                autofocus: CurrentWidget.Autofocus);

            return new Semantics(
                label: CurrentWidget.SemanticLabel,
                flags: IsInteractive ? SemanticsFlags.IsEnabled : SemanticsFlags.None,
                @checked: CurrentWidget.Value ?? false,
                mixed: CurrentWidget.Tristate ? CurrentWidget.Value is null : null,
                onTap: IsInteractive ? HandleTap : null,
                child: toggleable);
        }

        private void HandleTap()
        {
            CurrentWidget.OnChanged?.Invoke(NextValue());
            SemanticsService.SendEvent(
                new TapSemanticEvent(Context.FindRenderObject()?.SemanticsNodeId));
        }

        private bool? NextValue()
        {
            if (!CurrentWidget.Tristate)
            {
                return !(CurrentWidget.Value ?? false);
            }

            return CurrentWidget.Value switch
            {
                false => true,
                true => null,
                _ => false,
            };
        }
    }
}

internal sealed class CupertinoCheckboxPainter : ToggleablePainter
{
    // Eyeballed from a checkbox on a physical Macbook Pro running macOS version 14.5.
    internal static readonly CupertinoDynamicColor KDisabledCheckColor = CupertinoDynamicColor.WithBrightness(
        color: Color.FromArgb(64, 0, 0, 0),
        darkColor: Color.FromArgb(64, 255, 255, 255));
    internal static readonly CupertinoDynamicColor KDisabledBorderColor = CupertinoDynamicColor.WithBrightness(
        color: Color.FromArgb(13, 0, 0, 0),
        darkColor: Color.FromArgb(13, 0, 0, 0));
    internal static readonly CupertinoDynamicColor KDefaultBorderColor = CupertinoDynamicColor.WithBrightness(
        color: Color.FromArgb(255, 209, 209, 214),
        darkColor: Color.FromArgb(50, 128, 128, 128));
    internal static readonly CupertinoDynamicColor KDefaultFillColor = CupertinoDynamicColor.WithBrightness(
        color: CupertinoColors.ActiveBlue.Value,
        darkColor: Color.FromArgb(255, 50, 100, 215));
    internal static readonly CupertinoDynamicColor KDefaultCheckColor = CupertinoDynamicColor.WithBrightness(
        color: CupertinoColors.White,
        darkColor: Color.FromArgb(255, 222, 232, 248));
    internal const double KPressedOverlayOpacity = 0.15;
    // In dark mode, the fill color of a checkbox is an opacity gradient of the background color.
    internal static readonly IReadOnlyList<double> KDarkGradientOpacities = [0.14, 0.29];
    internal static readonly IReadOnlyList<double> KDisabledDarkGradientOpacities = [0.08, 0.14];

    private Color _checkColor;
    private bool? _value;
    private bool? _previousValue;
    private OutlinedBorder _shape = new RoundedRectangleBorder(
        borderRadius: Plumix.Rendering.BorderRadius.Circular(4.0));
    private BorderSide _side;
    private PlatformBrightness? _brightness;

    public CupertinoCheckboxPainter(
        Animation<double> position,
        Animation<double> reaction,
        Animation<double> reactionHoverFade,
        Animation<double> reactionFocusFade)
        : base(position, reaction, reactionHoverFade, reactionFocusFade)
    {
    }

    internal Color CheckColor => _checkColor;

    internal bool? Value => _value;

    internal bool? PreviousValue => _previousValue;

    internal OutlinedBorder Shape => _shape;

    internal BorderSide Side => _side;

    internal PlatformBrightness? Brightness => _brightness;

    internal Color EffectiveFocusColor => FocusColor;

    internal void Configure(
        Color focusColor,
        Point? downPosition,
        bool isFocused,
        bool isHovered,
        Color activeColor,
        Color inactiveColor,
        Color checkColor,
        bool? value,
        bool? previousValue,
        bool isActive,
        OutlinedBorder shape,
        BorderSide side,
        PlatformBrightness? brightness)
    {
        FocusColor = focusColor;
        DownPosition = downPosition;
        IsFocused = isFocused;
        IsHovered = isHovered;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        _checkColor = checkColor;
        _value = value;
        _previousValue = previousValue;
        IsActive = isActive;
        _shape = shape;
        _side = side;
        _brightness = brightness;
        NotifyPainterChanged();
    }

    private static Rect OuterRectAt(Point origin)
    {
        return new Rect(origin.X, origin.Y, CupertinoCheckbox.Width, CupertinoCheckbox.Width);
    }

    // The checkbox's border color if value == false, or its fill color when value == true or null.
    private Color ColorAt(bool value)
    {
        return value && IsActive ? ActiveColor : InactiveColor;
    }

    // White stroke used to paint the check and dash.
    private IPen CreateStrokePen()
    {
        return new Pen(new SolidColorBrush(_checkColor), 2.0, lineCap: PenLineCap.Round);
    }

    // Draw a gradient from the top to the bottom of the checkbox.
    private void DrawFillGradient(PaintingContext context, Rect outer, Color topColor, Color bottomColor)
    {
        var fillGradient = new LinearGradient(
            colors: [topColor, bottomColor],
            begin: Alignment.TopCenter,
            end: Alignment.BottomCenter);
        DrawShape(context, outer, fillGradient.CreateShader(outer), pen: null);
    }

    private void DrawBox(
        PaintingContext context,
        Rect outer,
        Color paintColor,
        double? strokeWidth,
        BorderSide side,
        bool value)
    {
        // Draw a gradient in dark mode except when the checkbox is enabled and checked.
        if (_brightness == PlatformBrightness.Dark && !(IsActive && value))
        {
            DrawFillGradient(
                context,
                outer,
                WithOpacity(
                    paintColor,
                    IsActive ? KDarkGradientOpacities[0] : KDisabledDarkGradientOpacities[0]),
                WithOpacity(
                    paintColor,
                    IsActive ? KDarkGradientOpacities[1] : KDisabledDarkGradientOpacities[1]));
        }
        else if (strokeWidth is null)
        {
            DrawShape(context, outer, new SolidColorBrush(paintColor), pen: null);
        }
        else
        {
            DrawShape(context, outer, brush: null, new Pen(new SolidColorBrush(paintColor), strokeWidth.Value));
        }

        DrawSide(context, outer, side);
    }

    private void DrawShape(PaintingContext context, Rect rect, IBrush? brush, IPen? pen)
    {
        if (_shape is CircleBorder)
        {
            context.DrawOval(rect, brush, pen);
            return;
        }

        context.DrawRRect(
            Plumix.UI.RRect.FromRectAndCorners(rect, ShapeBorderGeometry.ResolveRadius(_shape)),
            brush,
            pen);
    }

    // Flutter's `shape.copyWith(side: side).paint(canvas, outer)`: the side strokes inward from the
    // shape's outline.
    private void DrawSide(PaintingContext context, Rect outer, BorderSide side)
    {
        if (side.Width <= 0.0 || side.Color.A == 0 || side.Style == BorderStyle.None)
        {
            return;
        }

        if (_shape is CircleBorder)
        {
            context.DrawOval(
                outer.Deflate(side.Width / 2.0),
                brush: null,
                new Pen(new SolidColorBrush(side.Color), side.Width));
            return;
        }

        Plumix.UI.RRect outerRRect = Plumix.UI.RRect.FromRectAndCorners(
            outer,
            ShapeBorderGeometry.ResolveRadius(_shape));
        context.DrawDRRect(outerRRect, outerRRect.Deflate(side.Width), new SolidColorBrush(side.Color));
    }

    private void DrawCheck(PaintingContext context, Point origin, IPen pen)
    {
        // The ratios for the offsets below were found from looking at the checkbox examples in the
        // HIG docs. The distance from the needed point to the edge was measured, then divided by
        // the total width.
        var start = new Point(origin.X + (CupertinoCheckbox.Width * 0.22), origin.Y + (CupertinoCheckbox.Width * 0.54));
        var mid = new Point(origin.X + (CupertinoCheckbox.Width * 0.40), origin.Y + (CupertinoCheckbox.Width * 0.75));
        var end = new Point(origin.X + (CupertinoCheckbox.Width * 0.78), origin.Y + (CupertinoCheckbox.Width * 0.25));
        context.DrawLine(pen, start, mid);
        context.DrawLine(pen, mid, end);
    }

    private void DrawDash(PaintingContext context, Point origin, IPen pen)
    {
        // From measuring the checkbox example in the HIG docs, the dash was found to be half the
        // total width, centered in the middle.
        var start = new Point(origin.X + (CupertinoCheckbox.Width * 0.25), origin.Y + (CupertinoCheckbox.Width * 0.5));
        var end = new Point(origin.X + (CupertinoCheckbox.Width * 0.75), origin.Y + (CupertinoCheckbox.Width * 0.5));
        context.DrawLine(pen, start, end);
    }

    public override void Paint(PaintingContext context, Size size)
    {
        IPen strokePen = CreateStrokePen();
        var origin = new Point(
            (size.Width / 2.0) - (CupertinoCheckbox.Width / 2.0),
            (size.Height / 2.0) - (CupertinoCheckbox.Width / 2.0));
        Rect outer = OuterRectAt(origin);
        Color paintColor = ColorAt(_value ?? true);

        switch (_value)
        {
            case false:
                DrawBox(context, outer, paintColor, strokeWidth: null, _side, _value ?? true);
                break;
            case true:
                DrawBox(context, outer, paintColor, strokeWidth: null, _side, _value ?? true);
                DrawCheck(context, origin, strokePen);
                break;
            case null:
                DrawBox(context, outer, paintColor, strokeWidth: null, _side, _value ?? true);
                DrawDash(context, origin, strokePen);
                break;
        }

        // The checkbox's opacity changes when pressed.
        if (DownPosition is not null)
        {
            Color pressedColor = _brightness == PlatformBrightness.Light
                ? WithOpacity(CupertinoColors.Black, KPressedOverlayOpacity)
                : WithOpacity(CupertinoColors.White, KPressedOverlayOpacity);
            DrawShape(context, outer, new SolidColorBrush(pressedColor), pen: null);
        }

        if (IsFocused)
        {
            Rect focusOuter = outer.Inflate(1);
            DrawBox(context, focusOuter, FocusColor, strokeWidth: 3.5, _side, _value ?? true);
        }
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return !ReferenceEquals(this, oldDelegate);
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
