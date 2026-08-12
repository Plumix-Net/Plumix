using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/material_button.dart
// flutter/packages/flutter/lib/src/material/button.dart

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
        Thickness? padding = null,
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
    public Thickness? Padding { get; }
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
            shape: buttonTheme.GetShape(this),
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
        Thickness? padding = null,
        VisualDensity? visualDensity = null,
        BoxConstraints? constraints = null,
        BorderRadius? shape = null,
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
        Padding = padding ?? default;
        VisualDensity = visualDensity ?? global::Plumix.Material.VisualDensity.Standard;
        Constraints = constraints ?? new BoxConstraints(MinWidth: 88, MinHeight: 36);
        if (!Constraints.IsNormalized)
        {
            throw new ArgumentException("RawMaterialButton constraints must be normalized.", nameof(constraints));
        }

        Shape = shape ?? BorderRadius.Zero;
        AnimationDuration = animationDuration ?? TimeSpan.FromMilliseconds(200);
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
    public Thickness Padding { get; }
    public VisualDensity VisualDensity { get; }
    public BoxConstraints Constraints { get; }
    public BorderRadius Shape { get; }
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

    private sealed class RawMaterialButtonState : State
    {
        private RawMaterialButton CurrentWidget => (RawMaterialButton)StateWidget;

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var densityAdjustment = widget.VisualDensity.BaseSizeAdjustment;
            var padding = new Thickness(
                Math.Max(0, widget.Padding.Left + densityAdjustment.X),
                Math.Max(0, widget.Padding.Top + densityAdjustment.Y),
                Math.Max(0, widget.Padding.Right + densityAdjustment.X),
                Math.Max(0, widget.Padding.Bottom + densityAdjustment.Y));
            var foreground = widget.TextStyle?.Color;

            var style = new ButtonStyle(
                ForegroundColor: foreground.HasValue
                    ? MaterialStateProperty<Color?>.All(foreground.Value)
                    : null,
                BackgroundColor: widget.FillColor.HasValue
                    ? MaterialStateProperty<Color?>.All(widget.FillColor.Value)
                    : null,
                OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                {
                    if (states.HasFlag(MaterialState.Disabled)) return null;
                    if (states.HasFlag(MaterialState.Pressed)) return widget.HighlightColor;
                    if (states.HasFlag(MaterialState.Hovered)) return widget.HoverColor;
                    if (states.HasFlag(MaterialState.Focused)) return widget.FocusColor;
                    return null;
                }),
                SplashColor: widget.SplashColor.HasValue
                    ? MaterialButtonCore.CreateExplicitSplashResolver(widget.SplashColor.Value)
                    : null,
                Elevation: MaterialStateProperty<double?>.ResolveWith(states =>
                {
                    if (states.HasFlag(MaterialState.Disabled)) return widget.DisabledElevation;
                    if (states.HasFlag(MaterialState.Pressed)) return widget.HighlightElevation;
                    if (states.HasFlag(MaterialState.Hovered)) return widget.HoverElevation;
                    if (states.HasFlag(MaterialState.Focused)) return widget.FocusElevation;
                    return widget.Elevation;
                }),
                Padding: MaterialStateProperty<Thickness?>.All(padding),
                Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                    widget.Shape)),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(
                    widget.Constraints.MinWidth,
                    widget.Constraints.MinHeight)),
                MaximumSize: MaterialStateProperty<Size?>.All(new Size(
                    widget.Constraints.MaxWidth,
                    widget.Constraints.MaxHeight)),
                Alignment: Alignment.Center,
                TapTargetSize: widget.MaterialTapTargetSize,
                TextStyle: widget.TextStyle is null
                    ? null
                    : MaterialStateProperty<TextStyle?>.All(widget.TextStyle),
                VisualDensity: widget.VisualDensity,
                AnimationDuration: widget.AnimationDuration,
                EnableFeedback: widget.EnableFeedback);

            var tapTargetMinimum = widget.MaterialTapTargetSize == MaterialTapTargetSize.Padded
                ? new Size(
                    Math.Max(0, 48 + densityAdjustment.X),
                    Math.Max(0, 48 + densityAdjustment.Y))
                : new Size(0, 0);

            return new MaterialButtonCore(
                child: widget.Child ?? new SizedBox(),
                onPressed: widget.OnPressed,
                onLongPress: widget.OnLongPress,
                onHighlightChanged: widget.OnHighlightChanged,
                style: style,
                focusNode: widget.FocusNode,
                mouseCursor: widget.MouseCursor,
                clipBehavior: widget.ClipBehavior,
                enableFeedback: widget.EnableFeedback,
                autofocus: widget.Autofocus,
                tapTargetMinimumSize: tapTargetMinimum,
                enabled: widget.Enabled,
                semanticEnabled: widget.Enabled);
        }
    }
}
