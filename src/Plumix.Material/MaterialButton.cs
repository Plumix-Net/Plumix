using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// material_ui/lib/src/material_button.dart
// material_ui/lib/src/button.dart

public sealed class MaterialButton : StatelessWidget
{
    public MaterialButton(
        Action? onPressed,
        Widget? child = null,
        Action? onLongPress = null,
        Action<bool>? onHighlightChanged = null,
        MouseCursor? mouseCursor = null,
        ButtonTextTheme? textTheme = null,
        Color? textColor = null,
        Color? disabledTextColor = null,
        Color? color = null,
        Color? disabledColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        Brightness? colorBrightness = null,
        double? elevation = null,
        double? focusElevation = null,
        double? hoverElevation = null,
        double? highlightElevation = null,
        double? disabledElevation = null,
        EdgeInsetsGeometry? padding = null,
        VisualDensity? visualDensity = null,
        BorderRadius? shape = null,
        Clip clipBehavior = Clip.None,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialTapTargetSize? materialTapTargetSize = null,
        TimeSpan? animationDuration = null,
        double? minWidth = null,
        double? height = null,
        bool enableFeedback = true,
        Key? key = null) : base(key)
    {
        ValidateElevation(nameof(elevation), elevation);
        ValidateElevation(nameof(focusElevation), focusElevation);
        ValidateElevation(nameof(hoverElevation), hoverElevation);
        ValidateElevation(nameof(highlightElevation), highlightElevation);
        ValidateElevation(nameof(disabledElevation), disabledElevation);
        ValidateExtent(nameof(minWidth), minWidth);
        ValidateExtent(nameof(height), height);

        OnPressed = onPressed;
        Child = child;
        OnLongPress = onLongPress;
        OnHighlightChanged = onHighlightChanged;
        MouseCursor = mouseCursor;
        TextTheme = textTheme;
        TextColor = textColor;
        DisabledTextColor = disabledTextColor;
        Color = color;
        DisabledColor = disabledColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        HighlightColor = highlightColor;
        SplashColor = splashColor;
        ColorBrightness = colorBrightness;
        Elevation = elevation;
        FocusElevation = focusElevation;
        HoverElevation = hoverElevation;
        HighlightElevation = highlightElevation;
        DisabledElevation = disabledElevation;
        Padding = padding;
        VisualDensity = visualDensity;
        Shape = shape;
        ClipBehavior = clipBehavior;
        FocusNode = focusNode;
        Autofocus = autofocus;
        MaterialTapTargetSize = materialTapTargetSize;
        AnimationDuration = animationDuration;
        MinWidth = minWidth;
        Height = height;
        EnableFeedback = enableFeedback;
    }

    public Action? OnPressed { get; }
    public Action? OnLongPress { get; }
    public Action<bool>? OnHighlightChanged { get; }
    public MouseCursor? MouseCursor { get; }
    public ButtonTextTheme? TextTheme { get; }
    public Color? TextColor { get; }
    public Color? DisabledTextColor { get; }
    public Color? Color { get; }
    public Color? DisabledColor { get; }
    public Color? FocusColor { get; }
    public Color? HoverColor { get; }
    public Color? HighlightColor { get; }
    public Color? SplashColor { get; }
    public Brightness? ColorBrightness { get; }
    public double? Elevation { get; }
    public double? FocusElevation { get; }
    public double? HoverElevation { get; }
    public double? HighlightElevation { get; }
    public double? DisabledElevation { get; }
    public EdgeInsetsGeometry? Padding { get; }
    public VisualDensity? VisualDensity { get; }
    public BorderRadius? Shape { get; }
    public Clip ClipBehavior { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public MaterialTapTargetSize? MaterialTapTargetSize { get; }
    public TimeSpan? AnimationDuration { get; }
    public double? MinWidth { get; }
    public double? Height { get; }
    public bool EnableFeedback { get; }
    public Widget? Child { get; }
    public bool Enabled => OnPressed is not null || OnLongPress is not null;

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var buttonTheme = ButtonTheme.Of(context);
        var constraints = buttonTheme.GetConstraints(this) with
        {
            MinWidth = MinWidth ?? buttonTheme.GetConstraints(this).MinWidth,
            MinHeight = Height ?? buttonTheme.GetConstraints(this).MinHeight,
        };

        return new RawMaterialButton(
            onPressed: OnPressed,
            child: Child,
            onLongPress: OnLongPress,
            onHighlightChanged: OnHighlightChanged,
            mouseCursor: MouseCursor,
            textStyle: theme.TextTheme.LabelLarge with
            {
                Color = buttonTheme.GetTextColor(this, theme)
            },
            fillColor: buttonTheme.GetFillColor(this, theme),
            focusColor: FocusColor ?? buttonTheme.GetFocusColor(this, theme),
            hoverColor: HoverColor ?? buttonTheme.GetHoverColor(this, theme),
            highlightColor: HighlightColor ?? theme.HighlightColor,
            splashColor: SplashColor ?? theme.SplashColor,
            elevation: buttonTheme.GetElevation(this),
            focusElevation: buttonTheme.GetFocusElevation(this),
            hoverElevation: buttonTheme.GetHoverElevation(this),
            highlightElevation: buttonTheme.GetHighlightElevation(this),
            disabledElevation: DisabledElevation ?? 0,
            padding: buttonTheme.GetPadding(this),
            visualDensity: VisualDensity ?? theme.VisualDensity,
            constraints: constraints,
            shape: new RoundedRectangleBorder(borderRadius: buttonTheme.GetShape(this)),
            animationDuration: buttonTheme.GetAnimationDuration(this),
            clipBehavior: ClipBehavior,
            focusNode: FocusNode,
            autofocus: Autofocus,
            materialTapTargetSize: MaterialTapTargetSize ?? theme.MaterialTapTargetSize,
            enableFeedback: EnableFeedback);
    }

    private static void ValidateElevation(string name, double? value)
    {
        if (value is < 0 || value is double.NaN)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private static void ValidateExtent(string name, double? value)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

public sealed class RawMaterialButton : StatefulWidget
{
    public RawMaterialButton(
        Action? onPressed,
        Widget? child = null,
        Action? onLongPress = null,
        Action<bool>? onHighlightChanged = null,
        MouseCursor? mouseCursor = null,
        TextStyle? textStyle = null,
        Color? fillColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        double elevation = 2,
        double focusElevation = 4,
        double hoverElevation = 4,
        double highlightElevation = 8,
        double disabledElevation = 0,
        EdgeInsetsGeometry padding = default,
        VisualDensity? visualDensity = null,
        BoxConstraints? constraints = null,
        ShapeBorder? shape = null,
        TimeSpan? animationDuration = null,
        Clip clipBehavior = Clip.None,
        FocusNode? focusNode = null,
        bool autofocus = false,
        MaterialTapTargetSize materialTapTargetSize = MaterialTapTargetSize.Padded,
        bool enableFeedback = true,
        Key? key = null) : base(key)
    {
        ValidateElevation(nameof(elevation), elevation);
        ValidateElevation(nameof(focusElevation), focusElevation);
        ValidateElevation(nameof(hoverElevation), hoverElevation);
        ValidateElevation(nameof(highlightElevation), highlightElevation);
        ValidateElevation(nameof(disabledElevation), disabledElevation);

        OnPressed = onPressed;
        Child = child;
        OnLongPress = onLongPress;
        OnHighlightChanged = onHighlightChanged;
        MouseCursor = mouseCursor;
        TextStyle = textStyle;
        FillColor = fillColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        HighlightColor = highlightColor;
        SplashColor = splashColor;
        Elevation = elevation;
        FocusElevation = focusElevation;
        HoverElevation = hoverElevation;
        HighlightElevation = highlightElevation;
        DisabledElevation = disabledElevation;
        Padding = padding;
        VisualDensity = visualDensity ?? global::Plumix.Material.VisualDensity.Standard;
        Constraints = constraints ?? new BoxConstraints(MinWidth: 88, MinHeight: 36);
        if (!Constraints.IsNormalized)
        {
            throw new ArgumentException("RawMaterialButton constraints must be normalized.", nameof(constraints));
        }

        Shape = shape ?? new RoundedRectangleBorder();
        AnimationDuration = animationDuration ?? ButtonStyleState.ThemeChangeDuration;
        ClipBehavior = clipBehavior;
        FocusNode = focusNode;
        Autofocus = autofocus;
        MaterialTapTargetSize = materialTapTargetSize;
        EnableFeedback = enableFeedback;
    }

    public Action? OnPressed { get; }
    public Action? OnLongPress { get; }
    public Action<bool>? OnHighlightChanged { get; }
    public MouseCursor? MouseCursor { get; }
    public TextStyle? TextStyle { get; }
    public Color? FillColor { get; }
    public Color? FocusColor { get; }
    public Color? HoverColor { get; }
    public Color? HighlightColor { get; }
    public Color? SplashColor { get; }
    public double Elevation { get; }
    public double FocusElevation { get; }
    public double HoverElevation { get; }
    public double HighlightElevation { get; }
    public double DisabledElevation { get; }
    public EdgeInsetsGeometry Padding { get; }
    public VisualDensity VisualDensity { get; }
    public BoxConstraints Constraints { get; }
    public ShapeBorder Shape { get; }
    public TimeSpan AnimationDuration { get; }
    public Widget? Child { get; }
    public bool Enabled => OnPressed is not null || OnLongPress is not null;
    public MaterialTapTargetSize MaterialTapTargetSize { get; }
    public FocusNode? FocusNode { get; }
    public bool Autofocus { get; }
    public Clip ClipBehavior { get; }
    public bool EnableFeedback { get; }

    public override State CreateState() => new RawMaterialButtonState();

    private static void ValidateElevation(string name, double value)
    {
        if (double.IsNaN(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private sealed class RawMaterialButtonState : MaterialStateMixin
    {
        private RawMaterialButton CurrentWidget => (RawMaterialButton)StateWidget;

        public override void InitState()
        {
            base.InitState();
            SetMaterialState(MaterialState.Disabled, !CurrentWidget.Enabled);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            SetMaterialState(MaterialState.Disabled, !CurrentWidget.Enabled);
            // If the button is disabled while a press gesture is currently ongoing, InkWell makes a
            // call to handleHighlightChanged. This causes an exception because it calls setState in
            // the middle of a build. To preempt this, manually clear pressed when that happens.
            if (IsDisabled && IsPressed)
            {
                RemoveMaterialState(MaterialState.Pressed);
            }
        }

        private double EffectiveElevation
        {
            get
            {
                // These conditionals are in order of precedence, so be careful about reorganizing them.
                RawMaterialButton widget = CurrentWidget;
                if (IsDisabled)
                {
                    return widget.DisabledElevation;
                }

                if (IsPressed)
                {
                    return widget.HighlightElevation;
                }

                if (IsHovered)
                {
                    return widget.HoverElevation;
                }

                if (IsFocused)
                {
                    return widget.FocusElevation;
                }

                return widget.Elevation;
            }
        }

        public override Widget Build(BuildContext context)
        {
            RawMaterialButton widget = CurrentWidget;
            ThemeData theme = Theme.Of(context);

            // `WidgetStateProperty.resolveAs` is a no-op for these two in C#: neither `Color` nor
            // `ShapeBorder` can subclass `WidgetStateProperty` (`docs/ai/DIVERGENCES.md`).
            Color? effectiveTextColor = widget.TextStyle?.Color;
            ShapeBorder effectiveShape = widget.Shape;
            Vector densityAdjustment = widget.VisualDensity.BaseSizeAdjustment;
            BoxConstraints effectiveConstraints = widget.VisualDensity.EffectiveConstraints(widget.Constraints);
            MouseCursor effectiveMouseCursor = ResolveMouseCursor(widget.MouseCursor);
            EdgeInsetsGeometry padding = widget.Padding
                .Add(EdgeInsetsGeometry.Only(
                    left: densityAdjustment.X,
                    top: densityAdjustment.Y,
                    right: densityAdjustment.X,
                    bottom: densityAdjustment.Y))
                .Clamp(EdgeInsetsGeometry.Zero, EdgeInsetsGeometry.Infinity);

            Widget result = new ConstrainedBox(
                constraints: effectiveConstraints,
                child: new global::Plumix.Material.Material(
                    elevation: EffectiveElevation,
                    textStyle: widget.TextStyle is null
                        ? null
                        : widget.TextStyle with { Color = effectiveTextColor },
                    shape: effectiveShape,
                    color: widget.FillColor,
                    // For compatibility during the M3 migration the default shadow needs to be passed.
                    shadowColor: theme.UseMaterial3 ? theme.ShadowColor : null,
                    type: widget.FillColor is null ? MaterialType.Transparency : MaterialType.Button,
                    animationDuration: widget.AnimationDuration,
                    clipBehavior: widget.ClipBehavior,
                    child: new InkWell(
                        focusNode: widget.FocusNode,
                        canRequestFocus: widget.Enabled,
                        onFocusChange: UpdateMaterialState(MaterialState.Focused),
                        autofocus: widget.Autofocus,
                        onHighlightChanged: UpdateMaterialState(
                            MaterialState.Pressed,
                            onChanged: widget.OnHighlightChanged),
                        splashColor: widget.SplashColor,
                        highlightColor: widget.HighlightColor,
                        focusColor: widget.FocusColor,
                        hoverColor: widget.HoverColor,
                        onHover: UpdateMaterialState(MaterialState.Hovered),
                        onTap: widget.OnPressed,
                        onLongPress: widget.OnLongPress,
                        enableFeedback: widget.EnableFeedback,
                        customBorder: effectiveShape,
                        mouseCursor: effectiveMouseCursor,
                        child: IconTheme.Merge(
                            data: new IconThemeData(Color: effectiveTextColor),
                            child: new Padding(
                                insets: padding,
                                child: new Center(
                                    widthFactor: 1.0,
                                    heightFactor: 1.0,
                                    child: widget.Child))))));

            Size minSize = widget.MaterialTapTargetSize switch
            {
                MaterialTapTargetSize.Padded => new Size(
                    WidgetConstants.MinInteractiveDimension + densityAdjustment.X,
                    WidgetConstants.MinInteractiveDimension + densityAdjustment.Y),
                _ => default
            };

            return new Semantics(
                container: true,
                flags: SemanticsFlags.IsButton,
                enabled: widget.Enabled,
                child: new InputPadding(minSize: minSize, child: result));
        }

        private MouseCursor ResolveMouseCursor(MouseCursor? cursor)
        {
            MouseCursor source = cursor ?? WidgetStateMouseCursor.AdaptiveClickable;
            return source is WidgetStateMouseCursor stateful
                ? stateful.Resolve(MaterialStateSet.Of(MaterialStates)) ?? SystemMouseCursors.Basic
                : source;
        }
    }
}
