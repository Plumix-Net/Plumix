using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/button_theme.dart

public enum ButtonTextTheme
{
    Normal,
    Accent,
    Primary,
}

public enum ButtonBarLayoutBehavior
{
    Padded,
    Constrained,
}

public sealed record ButtonThemeData
{
    private EdgeInsetsGeometry? _padding;

    public ButtonThemeData(
        bool AlignedDropdown = false,
        ButtonTextTheme TextTheme = ButtonTextTheme.Normal,
        double MinWidth = 88.0,
        double Height = 36.0,
        EdgeInsetsGeometry? Padding = null,
        BorderRadius? Shape = null,
        ButtonBarLayoutBehavior LayoutBehavior = ButtonBarLayoutBehavior.Padded,
        Color? ButtonColor = null,
        Color? DisabledColor = null,
        Color? FocusColor = null,
        Color? HoverColor = null,
        Color? HighlightColor = null,
        Color? SplashColor = null,
        ColorScheme? ColorScheme = null,
        MaterialTapTargetSize? MaterialTapTargetSize = null)
    {
        ValidateExtent(nameof(MinWidth), MinWidth);
        ValidateExtent(nameof(Height), Height);
        this.AlignedDropdown = AlignedDropdown;
        this.TextTheme = TextTheme;
        this.MinWidth = MinWidth;
        this.Height = Height;
        _padding = Padding;
        this.Shape = Shape;
        this.LayoutBehavior = LayoutBehavior;
        this.ButtonColor = ButtonColor;
        this.DisabledColor = DisabledColor;
        this.FocusColor = FocusColor;
        this.HoverColor = HoverColor;
        this.HighlightColor = HighlightColor;
        this.SplashColor = SplashColor;
        this.ColorScheme = ColorScheme;
        this.MaterialTapTargetSize = MaterialTapTargetSize;
    }

    public bool AlignedDropdown { get; init; }
    public ButtonTextTheme TextTheme { get; init; }
    public double MinWidth { get; init; }
    public double Height { get; init; }
    public EdgeInsetsGeometry Padding
    {
        get => _padding ?? DefaultPadding(TextTheme);
        init => _padding = value;
    }
    public BorderRadius? Shape { get; init; }
    public ButtonBarLayoutBehavior LayoutBehavior { get; init; }
    public Color? ButtonColor { get; init; }
    public Color? DisabledColor { get; init; }
    public Color? FocusColor { get; init; }
    public Color? HoverColor { get; init; }
    public Color? HighlightColor { get; init; }
    public Color? SplashColor { get; init; }

    /// <summary>
    /// Defines a button's base colors, and the defaults for the button's minimum size, internal
    /// padding, and shape. <see cref="ButtonTheme.Of"/> fills it in from the ambient
    /// <see cref="ThemeData"/> when it is null; the button color getters read it.
    /// </summary>
    public ColorScheme? ColorScheme { get; init; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; init; }

    public BoxConstraints Constraints => new(MinWidth: MinWidth, MinHeight: Height);

    public EdgeInsetsGeometry EffectivePadding => Padding;

    public BorderRadius EffectiveShape => Shape ?? BorderRadius.Circular(
        TextTheme == ButtonTextTheme.Primary ? 4 : 2);

    /// <summary>The button's overall brightness: its <see cref="MaterialButton.ColorBrightness"/>
    /// if set, the <see cref="ColorScheme"/>'s brightness otherwise.</summary>
    public Brightness GetBrightness(MaterialButton button) =>
        button.ColorBrightness ?? ColorScheme!.Brightness;

    public ButtonTextTheme GetTextTheme(MaterialButton button) => button.TextTheme ?? TextTheme;

    public Color GetDisabledTextColor(MaterialButton button) =>
        button.TextColor ?? button.DisabledTextColor ?? ApplyOpacity(ColorScheme!.OnSurface, 0.38);

    public Color GetDisabledFillColor(MaterialButton button) =>
        button.DisabledColor ?? DisabledColor ?? ApplyOpacity(ColorScheme!.OnSurface, 0.38);

    public Color? GetFillColor(MaterialButton button)
    {
        Color? fillColor = button.Enabled ? button.Color : button.DisabledColor;
        if (fillColor != null)
        {
            return fillColor;
        }

        if (button.GetType() == typeof(MaterialButton))
        {
            return null;
        }

        if (button.Enabled && ButtonColor != null)
        {
            return ButtonColor;
        }

        return GetTextTheme(button) switch
        {
            ButtonTextTheme.Primary => button.Enabled
                ? ButtonColor ?? ColorScheme!.Primary
                : ApplyOpacity(ColorScheme!.OnSurface, 0.12),
            _ => button.Enabled ? ColorScheme!.Primary : GetDisabledFillColor(button),
        };
    }

    public Color GetTextColor(MaterialButton button)
    {
        if (!button.Enabled)
        {
            return GetDisabledTextColor(button);
        }

        if (button.TextColor != null)
        {
            return button.TextColor!;
        }

        switch (GetTextTheme(button))
        {
            case ButtonTextTheme.Normal:
                return GetBrightness(button) == Brightness.Dark
                    ? Colors.White
                    : Color.FromARGB(0xDE, 0, 0, 0);
            case ButtonTextTheme.Accent:
                return ColorScheme!.Secondary;
            default:
                Color? fill = GetFillColor(button);
                bool fillIsDark = fill != null
                    ? ThemeData.EstimateBrightnessForColor(fill) == Brightness.Dark
                    : GetBrightness(button) == Brightness.Dark;
                return fillIsDark ? Colors.White : Colors.Black;
        }
    }

    public Color GetSplashColor(MaterialButton button)
    {
        if (button.SplashColor != null)
        {
            return button.SplashColor!;
        }

        if (SplashColor != null && GetTextTheme(button) != ButtonTextTheme.Primary)
        {
            return SplashColor!;
        }

        return ApplyOpacity(GetTextColor(button), 0.12);
    }

    public Color GetFocusColor(MaterialButton button) =>
        button.FocusColor ?? FocusColor ?? ApplyOpacity(GetTextColor(button), 0.12);

    public Color GetHoverColor(MaterialButton button) =>
        button.HoverColor ?? HoverColor ?? ApplyOpacity(GetTextColor(button), 0.04);

    public Color GetHighlightColor(MaterialButton button)
    {
        if (button.HighlightColor != null)
        {
            return button.HighlightColor!;
        }

        return GetTextTheme(button) == ButtonTextTheme.Primary
            ? Colors.Transparent
            : HighlightColor ?? ApplyOpacity(GetTextColor(button), 0.16);
    }

    public double GetElevation(MaterialButton button) => button.Elevation ?? 2;

    public double GetFocusElevation(MaterialButton button) => button.FocusElevation ?? 4;

    public double GetHoverElevation(MaterialButton button) => button.HoverElevation ?? 4;

    public double GetHighlightElevation(MaterialButton button) => button.HighlightElevation ?? 8;

    public double GetDisabledElevation(MaterialButton button) => button.DisabledElevation ?? 0;

    public EdgeInsetsGeometry GetPadding(MaterialButton button)
    {
        return button.Padding ?? _padding ?? DefaultPadding(button.TextTheme ?? TextTheme);
    }

    public BorderRadius GetShape(MaterialButton button) => button.Shape ?? EffectiveShape;

    public TimeSpan GetAnimationDuration(MaterialButton button) =>
        button.AnimationDuration ?? TimeSpan.FromMilliseconds(200);

    public BoxConstraints GetConstraints(MaterialButton button) => Constraints;

    public MaterialTapTargetSize GetMaterialTapTargetSize(MaterialButton button) =>
        button.MaterialTapTargetSize ?? MaterialTapTargetSize ?? global::Plumix.Material.MaterialTapTargetSize.Padded;

    private static Color ApplyOpacity(Color color, double opacity) => color.WithOpacity(opacity);

    private static void ValidateExtent(string name, double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private static EdgeInsetsGeometry DefaultPadding(ButtonTextTheme textTheme)
    {
        return textTheme == ButtonTextTheme.Primary
            ? EdgeInsetsGeometry.Symmetric(horizontal: 24)
            : EdgeInsetsGeometry.Symmetric(horizontal: 16);
    }
}

public sealed class ButtonTheme : InheritedWidget
{
    public ButtonTheme(
        Widget child,
        ButtonTextTheme textTheme = ButtonTextTheme.Normal,
        ButtonBarLayoutBehavior layoutBehavior = ButtonBarLayoutBehavior.Padded,
        double minWidth = 88,
        double height = 36,
        EdgeInsetsGeometry? padding = null,
        BorderRadius? shape = null,
        bool alignedDropdown = false,
        Color? buttonColor = null,
        Color? disabledColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        ColorScheme? colorScheme = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        Key? key = null) : this(
        data: new ButtonThemeData(
            AlignedDropdown: alignedDropdown,
            TextTheme: textTheme,
            MinWidth: minWidth,
            Height: height,
            Padding: padding,
            Shape: shape,
            LayoutBehavior: layoutBehavior,
            ButtonColor: buttonColor,
            DisabledColor: disabledColor,
            FocusColor: focusColor,
            HoverColor: hoverColor,
            HighlightColor: highlightColor,
            SplashColor: splashColor,
            ColorScheme: colorScheme,
            MaterialTapTargetSize: materialTapTargetSize),
        child: child,
        key: key)
    {
    }

    public ButtonTheme(ButtonThemeData data, Widget child, Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public ButtonThemeData Data { get; }
    public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((ButtonTheme)oldWidget).Data, Data);

    /// <summary>
    /// The closest <see cref="ButtonThemeData"/>, or the ambient <see cref="ThemeData.ButtonTheme"/>,
    /// with a null <see cref="ButtonThemeData.ColorScheme"/> filled in from the ambient theme.
    /// </summary>
    public static ButtonThemeData Of(BuildContext context)
    {
        ButtonThemeData? buttonTheme = context.DependOnInheritedWidgetOfExactType<ButtonTheme>()?.Data;
        if (buttonTheme?.ColorScheme is null)
        {
            ThemeData theme = Theme.Of(context);
            buttonTheme ??= theme.ButtonTheme;
            if (buttonTheme.ColorScheme is null)
            {
                buttonTheme = buttonTheme with
                {
                    ColorScheme = theme.ButtonTheme.ColorScheme ?? theme.ColorScheme,
                };
            }
        }

        return buttonTheme;
    }
}
