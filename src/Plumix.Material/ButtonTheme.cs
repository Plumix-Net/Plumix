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
    public MaterialTapTargetSize? MaterialTapTargetSize { get; init; }

    public BoxConstraints Constraints => new(MinWidth: MinWidth, MinHeight: Height);

    public EdgeInsetsGeometry EffectivePadding => Padding;

    public BorderRadius EffectiveShape => Shape ?? BorderRadius.Circular(
        TextTheme == ButtonTextTheme.Primary ? 4 : 2);

    public ButtonTextTheme GetTextTheme(MaterialButton button) => button.TextTheme ?? TextTheme;

    public Color GetDisabledTextColor(MaterialButton button, ThemeData theme) =>
        button.TextColor ?? button.DisabledTextColor ?? ApplyOpacity(theme.OnSurfaceColor, 0.38);

    public Color GetDisabledFillColor(MaterialButton button, ThemeData theme) =>
        button.DisabledColor ?? DisabledColor ?? ApplyOpacity(theme.OnSurfaceColor, 0.38);

    public Color? GetFillColor(MaterialButton button, ThemeData theme)
    {
        var explicitFill = button.Enabled ? button.Color : button.DisabledColor;
        if (explicitFill.HasValue)
        {
            return explicitFill;
        }

        // Flutter keeps the exact base MaterialButton transparent unless a widget color is set.
        return null;
    }

    public Color GetTextColor(MaterialButton button, ThemeData theme)
    {
        if (!button.Enabled)
        {
            return GetDisabledTextColor(button, theme);
        }

        if (button.TextColor.HasValue)
        {
            return button.TextColor.Value;
        }

        return GetTextTheme(button) switch
        {
            ButtonTextTheme.Accent => theme.SecondaryColor,
            ButtonTextTheme.Primary => ResolvePrimaryTextColor(button, theme),
            _ => (button.ColorBrightness ?? theme.Brightness) == Brightness.Dark
                ? Colors.White
                : Color.FromArgb(0xDE, 0, 0, 0),
        };
    }

    public Color GetSplashColor(MaterialButton button, ThemeData theme)
    {
        if (button.SplashColor.HasValue)
        {
            return button.SplashColor.Value;
        }

        if (SplashColor.HasValue && GetTextTheme(button) != ButtonTextTheme.Primary)
        {
            return SplashColor.Value;
        }

        return ApplyOpacity(GetTextColor(button, theme), 0.12);
    }

    public Color GetFocusColor(MaterialButton button, ThemeData theme) =>
        button.FocusColor ?? FocusColor ?? ApplyOpacity(GetTextColor(button, theme), 0.12);

    public Color GetHoverColor(MaterialButton button, ThemeData theme) =>
        button.HoverColor ?? HoverColor ?? ApplyOpacity(GetTextColor(button, theme), 0.04);

    public Color GetHighlightColor(MaterialButton button, ThemeData theme)
    {
        if (button.HighlightColor.HasValue)
        {
            return button.HighlightColor.Value;
        }

        return GetTextTheme(button) == ButtonTextTheme.Primary
            ? Colors.Transparent
            : HighlightColor ?? ApplyOpacity(GetTextColor(button, theme), 0.16);
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

    private static bool IsDark(Color? color)
    {
        if (!color.HasValue)
        {
            return false;
        }

        var value = color.Value;
        double luminance = ((0.2126 * value.R) + (0.7152 * value.G) + (0.0722 * value.B)) / 255.0;
        return Math.Sqrt(luminance) + 0.15 < 0.5;
    }

    private Color ResolvePrimaryTextColor(MaterialButton button, ThemeData theme)
    {
        var fill = GetFillColor(button, theme);
        bool fillIsDark = fill.HasValue
            ? IsDark(fill)
            : (button.ColorBrightness ?? theme.Brightness) == Brightness.Dark;
        return fillIsDark ? Colors.White : Colors.Black;
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)(255 * opacity), 0, 255),
        color.R,
        color.G,
        color.B);

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
            MaterialTapTargetSize: materialTapTargetSize),
        child: child,
        key: key)
    {
    }

    public ButtonTheme(ButtonThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ButtonThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((ButtonTheme)oldWidget).Data, Data);

    public static ButtonThemeData Of(BuildContext context) =>
        context.DependOnInherited<ButtonTheme>()?.Data ?? Theme.Of(context).ButtonTheme;
}
