using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/button_style_button.dart

/// <summary>Dart's `IconAlignment`: where a button's icon sits relative to its label.</summary>
public enum IconAlignment
{
    Start,
    End,
}

/// <summary>
/// Dart parity: `ButtonStyleButton`, the base of `TextButton`, `ElevatedButton`, `FilledButton` and
/// `OutlinedButton`. Subclasses supply `DefaultStyleOf` and `ThemeStyleOf`; this class resolves the
/// three style layers property by property and composes `Material` + `InkWell`.
/// </summary>
public abstract class ButtonStyleButton : StatefulWidget
{
    protected ButtonStyleButton(
        Action? onPressed,
        Action? onLongPress,
        Action<bool>? onHover,
        Action<bool>? onFocusChange,
        ButtonStyle? style,
        FocusNode? focusNode,
        bool autofocus,
        Clip? clipBehavior,
        Widget? child,
        MaterialStatesController? statesController = null,
        bool? isSemanticButton = true,
        string? tooltip = null,
        Key? key = null) : base(key)
    {
        OnPressed = onPressed;
        OnLongPress = onLongPress;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        Style = style;
        FocusNode = focusNode;
        Autofocus = autofocus;
        ClipBehavior = clipBehavior;
        StatesController = statesController;
        IsSemanticButton = isSemanticButton;
        Tooltip = tooltip;
        Child = child;
    }

    public Action? OnPressed { get; }

    public Action? OnLongPress { get; }

    public Action<bool>? OnHover { get; }

    public Action<bool>? OnFocusChange { get; }

    public ButtonStyle? Style { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public Clip? ClipBehavior { get; }

    public MaterialStatesController? StatesController { get; }

    public bool? IsSemanticButton { get; }

    public string? Tooltip { get; }

    public Widget? Child { get; }

    /// Dart's `ButtonStyleButton.enabled`.
    public bool Enabled => OnPressed is not null || OnLongPress is not null;

    /// Dart's `ButtonStyleButton.defaultStyleOf`: the lowest style layer, never null.
    protected internal abstract ButtonStyle DefaultStyleOf(BuildContext context);

    /// Dart's `ButtonStyleButton.themeStyleOf`: the component-theme layer, may be null.
    protected internal abstract ButtonStyle? ThemeStyleOf(BuildContext context);

    public override State CreateState() => new ButtonStyleState();

    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new FlagProperty("enabled", value: Enabled, ifFalse: "disabled"));
        properties.Add(new DiagnosticsProperty<ButtonStyle?>("style", Style, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<FocusNode?>("focusNode", FocusNode, defaultValue: nullDefault));
    }

    /// Dart's `ButtonStyleButton.allOrNull`.
    public static MaterialStateProperty<T?>? AllOrNull<T>(T? value)
        where T : class
    {
        return value is null ? null : MaterialStateProperty<T?>.All(value);
    }

    /// Dart's `ButtonStyleButton.allOrNull` for value types.
    public static MaterialStateProperty<T?>? AllOrNullValue<T>(T? value)
        where T : struct
    {
        return value is null ? null : MaterialStateProperty<T?>.All(value);
    }

    /// <summary>
    /// Dart's `ButtonStyleButton.defaultColor`: a property that resolves <paramref name="disabled"/>
    /// for the disabled state and <paramref name="enabled"/> otherwise, or null when both are null.
    /// </summary>
    public static MaterialStateProperty<Color?>? DefaultColor(Color? enabled, Color? disabled)
    {
        if ((enabled ?? disabled) is null)
        {
            return null;
        }

        return MaterialStateProperty<Color?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled) ? disabled : enabled);
    }

    /// <summary>
    /// Dart's `ButtonStyleButton.scaledPadding`: picks or interpolates between the 1x, 2x and 3x
    /// insets by the font-size multiplier. A NaN multiplier falls through to the 3x geometry, as in
    /// Dart, where none of the relational patterns match.
    /// </summary>
    public static EdgeInsetsGeometry ScaledPadding(
        EdgeInsetsGeometry geometry1x,
        EdgeInsetsGeometry geometry2x,
        EdgeInsetsGeometry geometry3x,
        double fontSizeMultiplier)
    {
        if (fontSizeMultiplier <= 1)
        {
            return geometry1x;
        }

        if (fontSizeMultiplier < 2)
        {
            return EdgeInsetsGeometry.Lerp(geometry1x, geometry2x, fontSizeMultiplier - 1)!.Value;
        }

        if (fontSizeMultiplier < 3)
        {
            return EdgeInsetsGeometry.Lerp(geometry2x, geometry3x, fontSizeMultiplier - 2)!.Value;
        }

        return geometry3x;
    }

    /// <summary>
    /// Dart's per-button `effectiveTextScale`: the style font size run through the ambient
    /// `TextScaler`, divided by the literal 14.0 — not the raw text scale factor.
    /// </summary>
    internal static double EffectiveTextScale(BuildContext context, double? defaultFontSize)
    {
        double fontSize = defaultFontSize ?? 14.0;
        return MediaQuery.TextScalerOf(context).Scale(fontSize) / 14.0;
    }

    /// Dart's documented default for `ButtonStyle.iconAlignment` when nothing supplies one.
    internal const IconAlignment DefaultIconAlignment = IconAlignment.Start;

    /// Dart's `Size.infinite`.
    internal static Size InfiniteSize { get; } = new(double.PositiveInfinity, double.PositiveInfinity);

    /// <summary>
    /// Dart's `WidgetStateMouseCursor.adaptiveClickable`, adapted to the `[Flags] MaterialState`
    /// spelling `ButtonStyle.mouseCursor` uses (`docs/ai/DIVERGENCES.md`).
    /// </summary>
    internal static MaterialStateProperty<MouseCursor?> AdaptiveClickableCursor { get; } =
        MaterialStateProperty<MouseCursor?>.ResolveWith(states =>
            WidgetStateMouseCursor.AdaptiveClickable.Resolve(MaterialStateSet.Of(states)));

    /// Dart's `Color.withOpacity`.
    internal static Color WithOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * 255.0);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    /// <summary>
    /// The `styleFrom` background/icon colour switch shared by the four buttons: a single value
    /// applies in every state when no disabled colour is given, otherwise the two split on
    /// `WidgetState.disabled`.
    /// </summary>
    internal static MaterialStateProperty<Color?>? SingleValueOrDefaultColor(Color? enabled, Color? disabled)
    {
        if (enabled is not null && disabled is null)
        {
            return MaterialStateProperty<Color?>.All(enabled);
        }

        return DefaultColor(enabled, disabled);
    }

    /// <summary>
    /// The `styleFrom` overlay switch shared by the four buttons: null when neither colour is given,
    /// the overlay colour verbatim when it is fully transparent (which defeats every highlight), and
    /// otherwise pressed 0.1 / hovered 0.08 / focused 0.1 over `overlayColor ?? foregroundColor`.
    /// </summary>
    internal static MaterialStateProperty<Color?>? DefaultOverlayColor(Color? foregroundColor, Color? overlayColor)
    {
        if (foregroundColor is null && overlayColor is null)
        {
            return null;
        }

        if (overlayColor is { A: 0 })
        {
            return MaterialStateProperty<Color?>.All(overlayColor);
        }

        return StateOverlay(overlayColor ?? foregroundColor!.Value);
    }

    /// <summary>Dart's default overlay table: pressed 0.1, hovered 0.08, focused 0.1, otherwise null.</summary>
    internal static MaterialStateProperty<Color?> StateOverlay(Color color)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Pressed))
            {
                return WithOpacity(color, 0.1);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return WithOpacity(color, 0.08);
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return WithOpacity(color, 0.1);
            }

            return null;
        });
    }

    /// <summary>
    /// Dart's `styleFrom` cursor map: always a non-null property, which may resolve to null so the
    /// next style layer can supply the cursor.
    /// </summary>
    internal static MaterialStateProperty<MouseCursor?> StyleFromMouseCursor(
        MouseCursor? enabledMouseCursor,
        MouseCursor? disabledMouseCursor)
    {
        return MaterialStateProperty<MouseCursor?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled) ? disabledMouseCursor : enabledMouseCursor);
    }

    /// <summary>
    /// Dart's `_&lt;X&gt;ButtonWithIconChild.build`: a min-size `Row` whose gap shrinks from 8 to 4 as the
    /// text scale goes from 1x to 2x, with the icon leading or trailing per <paramref name="iconAlignment"/>.
    /// </summary>
    internal static Widget BuildIconChild(
        BuildContext context,
        ButtonStyle? buttonStyle,
        Widget icon,
        Widget label,
        IconAlignment iconAlignment)
    {
        double defaultFontSize = buttonStyle?.TextStyle?.Resolve(MaterialState.None)?.FontSize ?? 14.0;
        double scale = Math.Clamp(MediaQuery.TextScalerOf(context).Scale(defaultFontSize) / 14.0, 1.0, 2.0) - 1.0;
        return new Row(
            children: iconAlignment == IconAlignment.Start
                ? [icon, new Flexible(child: label)]
                : [new Flexible(child: label), icon],
            mainAxisSize: MainAxisSize.Min,
            spacing: 8.0 + ((4.0 - 8.0) * scale));
    }
}

/// <summary>Dart parity: `_ButtonStyleState`.</summary>
public sealed class ButtonStyleState : State
{
    /// Dart's `kThemeChangeDuration`.
    internal static readonly TimeSpan ThemeChangeDuration = TimeSpan.FromMilliseconds(200);

    private AnimationController? _controller;
    private double? _elevation;
    private Color? _backgroundColor;
    private MaterialStatesController? _internalStatesController;

    private ButtonStyleButton CurrentWidget => (ButtonStyleButton)StateWidget;

    private MaterialStatesController StatesController =>
        CurrentWidget.StatesController ?? _internalStatesController!;

    public override void InitState()
    {
        base.InitState();
        InitStatesController();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (ButtonStyleButton)oldWidget;
        if (!ReferenceEquals(CurrentWidget.StatesController, previous.StatesController))
        {
            previous.StatesController?.RemoveListener(HandleStatesControllerChange);
            if (CurrentWidget.StatesController is not null)
            {
                _internalStatesController?.Dispose();
                _internalStatesController = null;
            }

            InitStatesController();
        }

        if (CurrentWidget.Enabled != previous.Enabled)
        {
            StatesController.Update(MaterialState.Disabled, !CurrentWidget.Enabled);
            if (!CurrentWidget.Enabled)
            {
                StatesController.Update(MaterialState.Pressed, false);
            }
        }
    }

    public override void Dispose()
    {
        StatesController.RemoveListener(HandleStatesControllerChange);
        _internalStatesController?.Dispose();
        _controller?.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        ButtonStyleButton widget = CurrentWidget;
        ButtonStyle? widgetStyle = widget.Style;
        ButtonStyle? themeStyle = widget.ThemeStyleOf(context);
        ButtonStyle defaultStyle = widget.DefaultStyleOf(context);
        MaterialState states = StatesController.Value;
        ThemeData theme = Theme.Of(context);
        IconThemeData iconTheme = IconTheme.Of(context);

        TValue? EffectiveClass<TValue>(Func<ButtonStyle?, TValue?> getProperty)
            where TValue : class
        {
            return getProperty(widgetStyle) ?? getProperty(themeStyle) ?? getProperty(defaultStyle);
        }

        TValue? EffectiveStruct<TValue>(Func<ButtonStyle?, TValue?> getProperty)
            where TValue : struct
        {
            return getProperty(widgetStyle) ?? getProperty(themeStyle) ?? getProperty(defaultStyle);
        }

        TValue? ResolveClass<TValue>(Func<ButtonStyle?, MaterialStateProperty<TValue?>?> getProperty)
            where TValue : class
        {
            return EffectiveClass<TValue>(style => getProperty(style) is { } property
                ? property.Resolve(states)
                : null);
        }

        TValue? ResolveStruct<TValue>(Func<ButtonStyle?, MaterialStateProperty<TValue?>?> getProperty)
            where TValue : struct
        {
            return EffectiveStruct<TValue>(style => getProperty(style) is { } property
                ? property.Resolve(states)
                : null);
        }

        // Dart's `effectiveIconColor`: widget/theme foregroundColor outranks the default iconColor.
        Color? EffectiveIconColor()
        {
            return widgetStyle?.IconColor?.Resolve(states)
                   ?? themeStyle?.IconColor?.Resolve(states)
                   ?? widgetStyle?.ForegroundColor?.Resolve(states)
                   ?? themeStyle?.ForegroundColor?.Resolve(states)
                   ?? defaultStyle.IconColor?.Resolve(states)
                   ?? defaultStyle.ForegroundColor?.Resolve(states);
        }

        double resolvedElevation = ResolveStruct<double>(style => style?.Elevation) ?? 0.0;
        TextStyle? resolvedTextStyle = ResolveClass<TextStyle>(style => style?.TextStyle);
        Color? resolvedBackgroundColor = ResolveStruct<Color>(style => style?.BackgroundColor);
        Color? resolvedForegroundColor = ResolveStruct<Color>(style => style?.ForegroundColor);
        Color? resolvedShadowColor = ResolveStruct<Color>(style => style?.ShadowColor);
        Color? resolvedSurfaceTintColor = ResolveStruct<Color>(style => style?.SurfaceTintColor);
        EdgeInsetsGeometry resolvedPadding =
            ResolveStruct<EdgeInsetsGeometry>(style => style?.Padding) ?? EdgeInsetsGeometry.Zero;
        Size resolvedMinimumSize = ResolveStruct<Size>(style => style?.MinimumSize) ?? default;
        Size? resolvedFixedSize = ResolveStruct<Size>(style => style?.FixedSize);
        Size resolvedMaximumSize = ResolveStruct<Size>(style => style?.MaximumSize)
                                   ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
        Color? resolvedIconColor = EffectiveIconColor();
        double? resolvedIconSize = ResolveStruct<double>(style => style?.IconSize);
        BorderSide? resolvedSide = ResolveStruct<BorderSide>(style => style?.Side);
        OutlinedBorder resolvedShape =
            ResolveClass<OutlinedBorder>(style => style?.Shape) ?? new RoundedRectangleBorder();
        VisualDensity resolvedVisualDensity =
            EffectiveStruct<VisualDensity>(style => style?.VisualDensity) ?? theme.VisualDensity;
        MaterialTapTargetSize resolvedTapTargetSize =
            EffectiveStruct<MaterialTapTargetSize>(style => style?.TapTargetSize) ?? theme.MaterialTapTargetSize;
        TimeSpan resolvedAnimationDuration =
            EffectiveStruct<TimeSpan>(style => style?.AnimationDuration) ?? ThemeChangeDuration;
        bool resolvedEnableFeedback = EffectiveStruct<bool>(style => style?.EnableFeedback) ?? true;
        AlignmentGeometry resolvedAlignment =
            EffectiveStruct<AlignmentGeometry>(style => style?.Alignment) ?? Alignment.Center;
        InteractiveInkFeatureFactory? resolvedSplashFactory =
            EffectiveClass<InteractiveInkFeatureFactory>(style => style?.SplashFactory);
        ButtonLayerBuilder? resolvedBackgroundBuilder =
            EffectiveClass<ButtonLayerBuilder>(style => style?.BackgroundBuilder);
        ButtonLayerBuilder? resolvedForegroundBuilder =
            EffectiveClass<ButtonLayerBuilder>(style => style?.ForegroundBuilder);

        // Dart resolves these two lazily, against the states the consumer passes, not against the
        // build-time state set.
        MouseCursor mouseCursor = WidgetStateMouseCursor.ResolveWith(cursorStates =>
        {
            MaterialState flags = MaterialStateSet.Flags(cursorStates);
            return EffectiveClass<MouseCursor>(style => style?.MouseCursor?.Resolve(flags));
        });
        MaterialStateProperty<Color?> overlayColor = MaterialStateProperty<Color?>.ResolveWith(
            overlayStates => EffectiveStruct<Color>(style => style?.OverlayColor?.Resolve(overlayStates)));

        Vector densityAdjustment = resolvedVisualDensity.BaseSizeAdjustment;
        BoxConstraints effectiveConstraints = resolvedVisualDensity.EffectiveConstraints(new BoxConstraints(
            MinWidth: resolvedMinimumSize.Width,
            MaxWidth: resolvedMaximumSize.Width,
            MinHeight: resolvedMinimumSize.Height,
            MaxHeight: resolvedMaximumSize.Height));
        if (resolvedFixedSize is { } fixedSize)
        {
            Size constrained = effectiveConstraints.Constrain(fixedSize);
            if (double.IsFinite(constrained.Width))
            {
                effectiveConstraints = effectiveConstraints with
                {
                    MinWidth = constrained.Width,
                    MaxWidth = constrained.Width,
                };
            }

            if (double.IsFinite(constrained.Height))
            {
                effectiveConstraints = effectiveConstraints with
                {
                    MinHeight = constrained.Height,
                    MaxHeight = constrained.Height,
                };
            }
        }

        // Per the Material Design team: the visual-density adjustment must never reduce the
        // left/right padding, or `VisualDensity.compact` (the desktop/web default) would zero it.
        double verticalAdjustment = densityAdjustment.Y;
        double horizontalAdjustment = Math.Max(0.0, densityAdjustment.X);
        EdgeInsetsGeometry padding = resolvedPadding
            .Add(EdgeInsetsGeometry.FromLTRB(
                horizontalAdjustment,
                verticalAdjustment,
                horizontalAdjustment,
                verticalAdjustment))
            .Clamp(EdgeInsetsGeometry.Zero, EdgeInsetsGeometry.Infinity);

        Size minSize = resolvedTapTargetSize switch
        {
            MaterialTapTargetSize.Padded => new Size(
                WidgetConstants.MinInteractiveDimension + densityAdjustment.X,
                WidgetConstants.MinInteractiveDimension + densityAdjustment.Y),
            _ => new Size(0.0, 0.0),
        };

        resolvedBackgroundColor = ApplyElevationBeforeColorDeferral(
            resolvedAnimationDuration,
            resolvedElevation,
            resolvedBackgroundColor);
        _elevation = resolvedElevation;
        _backgroundColor = resolvedBackgroundColor;

        Clip effectiveClipBehavior = widget.ClipBehavior
                                     ?? ((resolvedBackgroundBuilder ?? resolvedForegroundBuilder) is not null
                                         ? Clip.AntiAlias
                                         : Clip.None);
        OutlinedBorder inkShape = resolvedShape.CopyWith(side: resolvedSide);

        Widget? content = resolvedForegroundBuilder is not null
            ? resolvedForegroundBuilder(context, states, widget.Child)
            : widget.Child;
        Widget result = new Align(
            alignment: resolvedAlignment.Resolve(Directionality.Of(context)),
            widthFactor: 1.0,
            heightFactor: 1.0,
            child: content);
        result = new Padding(padding, result);
        if (resolvedBackgroundBuilder is not null)
        {
            result = resolvedBackgroundBuilder(context, states, result);
        }

        result = new InkWell(
            onTap: widget.OnPressed,
            onLongPress: widget.OnLongPress,
            onHover: widget.OnHover,
            mouseCursor: mouseCursor,
            enableFeedback: resolvedEnableFeedback,
            focusNode: widget.FocusNode,
            canRequestFocus: widget.Enabled,
            onFocusChange: widget.OnFocusChange,
            autofocus: widget.Autofocus,
            splashFactory: resolvedSplashFactory,
            overlayColor: overlayColor,
            highlightColor: Colors.Transparent,
            customBorder: inkShape,
            statesController: StatesController,
            child: result);

        // Dart's `AnimatedTheme(data: theme.copyWith(iconTheme: ...))`: its only effect is animating
        // the icon theme, but a descendant reading `Theme.of(context)` sees the button's icon theme.
        result = new AnimatedTheme(
            data: theme with
            {
                IconTheme = iconTheme.Merge(
                    new IconThemeData(Color: resolvedIconColor, Size: resolvedIconSize)),
            },
            duration: resolvedAnimationDuration,
            child: result);

        if (widget.Tooltip is not null)
        {
            result = new Tooltip(message: widget.Tooltip, child: result);
        }

        result = new Material(
            elevation: resolvedElevation,
            textStyle: resolvedTextStyle is null
                ? null
                : resolvedTextStyle with { Color = resolvedForegroundColor },
            shape: inkShape,
            color: resolvedBackgroundColor,
            shadowColor: resolvedShadowColor,
            surfaceTintColor: resolvedSurfaceTintColor,
            type: resolvedBackgroundColor is null ? MaterialType.Transparency : MaterialType.Button,
            animationDuration: resolvedAnimationDuration,
            clipBehavior: effectiveClipBehavior,
            borderOnForeground: false,
            child: result);

        result = new ConstrainedBox(constraints: effectiveConstraints, child: result);
        result = new InputPadding(minSize: minSize, child: result);
        return new Semantics(
            container: true,
            flags: widget.IsSemanticButton == true ? SemanticsFlags.IsButton : SemanticsFlags.None,
            enabled: widget.Enabled,
            child: result);
    }

    private void HandleStatesControllerChange()
    {
        SetState(() => { });
    }

    private void InitStatesController()
    {
        if (CurrentWidget.StatesController is null)
        {
            _internalStatesController = new MaterialStatesController();
        }

        StatesController.Update(MaterialState.Disabled, !CurrentWidget.Enabled);
        StatesController.AddListener(HandleStatesControllerChange);
    }

    /// <summary>
    /// Dart's elevation-before-color deferral: when a button goes from an opaque background at a
    /// non-zero elevation to a translucent background at elevation zero, the old background is held
    /// for one animation so the shadow finishes fading before the color changes.
    /// </summary>
    private Color? ApplyElevationBeforeColorDeferral(
        TimeSpan animationDuration,
        double resolvedElevation,
        Color? resolvedBackgroundColor)
    {
        if (animationDuration <= TimeSpan.Zero
            || _elevation is null
            || _backgroundColor is null
            || _elevation == resolvedElevation
            || resolvedBackgroundColor is null
            || _backgroundColor.Value == resolvedBackgroundColor.Value
            || _backgroundColor.Value.A != byte.MaxValue
            || resolvedBackgroundColor.Value.A == byte.MaxValue
            || resolvedElevation != 0.0)
        {
            return resolvedBackgroundColor;
        }

        if (_controller?.Duration != animationDuration)
        {
            _controller?.Dispose();
            _controller = new AnimationController(duration: animationDuration, vsync: this);
            _controller.AddStatusListener(status =>
            {
                if (status == AnimationStatus.Completed)
                {
                    SetState(() => { });
                }
            });
        }

        _controller!.Forward(from: 0.0);
        return _backgroundColor;
    }
}

/// <summary>
/// Dart parity: `_InputPadding`. Grows the button's box to the minimum tap target without moving the
/// visual material, and redirects taps that land in the resulting ring to the child's centre.
/// </summary>
internal sealed class InputPadding : SingleChildRenderObjectWidget
{
    public InputPadding(Size minSize, Widget child, Key? key = null) : base(child, key)
    {
        MinSize = minSize;
    }

    public Size MinSize { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderInputPadding(MinSize);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderInputPadding)renderObject).MinSize = MinSize;
    }
}

/// <summary>Dart parity: `_RenderInputPadding`.</summary>
internal sealed class RenderInputPadding : RenderProxyBox
{
    private Size _minSize;

    public RenderInputPadding(Size minSize, RenderBox? child = null)
    {
        _minSize = minSize;
        Child = child;
    }

    public Size MinSize
    {
        get => _minSize;
        set
        {
            if (_minSize == value)
            {
                return;
            }

            _minSize = value;
            MarkNeedsLayout();
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return Child is null ? 0.0 : Math.Max(Child.GetMinIntrinsicWidth(height), _minSize.Width);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return Child is null ? 0.0 : Math.Max(Child.GetMaxIntrinsicWidth(height), _minSize.Width);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return Child is null ? 0.0 : Math.Max(Child.GetMinIntrinsicHeight(width), _minSize.Height);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return Child is null ? 0.0 : Math.Max(Child.GetMaxIntrinsicHeight(width), _minSize.Height);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return ComputeSize(constraints, dry: true);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is null)
        {
            return null;
        }

        double? childBaseline = Child.GetDryBaseline(constraints, baseline);
        if (childBaseline is null)
        {
            return null;
        }

        Size childSize = Child.GetDryLayout(constraints);
        Size dryLayout = GetDryLayout(constraints);
        return childBaseline + ((dryLayout.Height - childSize.Height) / 2.0);
    }

    protected override void PerformLayout()
    {
        Size = ComputeSize(Constraints, dry: false);
        if (Child is null)
        {
            return;
        }

        ((BoxParentData)Child.parentData!).offset = new Point(
            (Size.Width - Child.Size.Width) / 2.0,
            (Size.Height - Child.Size.Height) / 2.0);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (base.HitTest(result, position))
        {
            return true;
        }

        if (Child is null)
        {
            return false;
        }

        var center = new Point(Child.Size.Width / 2.0, Child.Size.Height / 2.0);
        return result.AddWithRawTransform(
            MatrixUtils.ForceToPoint(center),
            center,
            (BoxHitTestResult nested, Point nestedPosition) => Child.HitTest(nested, nestedPosition));
    }

    private Size ComputeSize(BoxConstraints constraints, bool dry)
    {
        if (Child is null)
        {
            return default;
        }

        Size childSize;
        if (dry)
        {
            childSize = Child.GetDryLayout(constraints);
        }
        else
        {
            Child.Layout(constraints, parentUsesSize: true);
            childSize = Child.Size;
        }

        return constraints.Constrain(new Size(
            Math.Max(childSize.Width, _minSize.Width),
            Math.Max(childSize.Height, _minSize.Height)));
    }
}
