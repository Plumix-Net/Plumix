using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart; flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart (baseline fixed-type subset)

public enum BottomNavigationBarType
{
    Fixed,
    Shifting,
}

public sealed class BottomNavigationBarItem
{
    public BottomNavigationBarItem(
        Widget icon,
        string label,
        Widget? activeIcon = null,
        Color? backgroundColor = null,
        string? tooltip = null,
        Key? key = null)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ActiveIcon = activeIcon ?? icon;
        BackgroundColor = backgroundColor;
        Tooltip = tooltip;
        Key = key;
    }

    public Key? Key { get; }

    public Widget Icon { get; }

    public Widget ActiveIcon { get; }

    public string Label { get; }

    public Color? BackgroundColor { get; }

    public string? Tooltip { get; }
}

public sealed class BottomNavigationBar : StatelessWidget
{
    private const double DefaultHeight = 56.0;
    private const double DefaultIconSize = 24.0;

    public BottomNavigationBar(
        IReadOnlyList<BottomNavigationBarItem> items,
        Action<int>? onTap = null,
        int currentIndex = 0,
        BottomNavigationBarType? type = null,
        Color? backgroundColor = null,
        Color? selectedItemColor = null,
        Color? unselectedItemColor = null,
        IconThemeData? selectedIconTheme = null,
        IconThemeData? unselectedIconTheme = null,
        double? elevation = null,
        double iconSize = DefaultIconSize,
        double selectedFontSize = 14.0,
        double unselectedFontSize = 12.0,
        TextStyle? selectedLabelStyle = null,
        TextStyle? unselectedLabelStyle = null,
        bool? showSelectedLabels = null,
        bool? showUnselectedLabels = null,
        Key? key = null) : base(key)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items.Count < 2)
        {
            throw new ArgumentException("BottomNavigationBar requires at least two items.", nameof(items));
        }

        if (currentIndex < 0 || currentIndex >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(currentIndex), "Current index must be within item range.");
        }

        if (!double.IsFinite(iconSize) || iconSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iconSize), "Icon size must be finite and non-negative.");
        }

        if (!double.IsFinite(selectedFontSize) || selectedFontSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedFontSize), "Selected font size must be finite and non-negative.");
        }

        if (!double.IsFinite(unselectedFontSize) || unselectedFontSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unselectedFontSize), "Unselected font size must be finite and non-negative.");
        }

        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite and non-negative.");
        }

        if ((selectedIconTheme is null) != (unselectedIconTheme is null))
        {
            throw new ArgumentException("Both selectedIconTheme and unselectedIconTheme must be provided together.");
        }

        ValidateIconTheme(nameof(selectedIconTheme), selectedIconTheme);
        ValidateIconTheme(nameof(unselectedIconTheme), unselectedIconTheme);

        Items = items;
        OnTap = onTap;
        CurrentIndex = currentIndex;
        Type = type;
        BackgroundColor = backgroundColor;
        SelectedItemColor = selectedItemColor;
        UnselectedItemColor = unselectedItemColor;
        SelectedIconTheme = selectedIconTheme;
        UnselectedIconTheme = unselectedIconTheme;
        Elevation = elevation;
        IconSize = iconSize;
        SelectedFontSize = selectedFontSize;
        UnselectedFontSize = unselectedFontSize;
        SelectedLabelStyle = selectedLabelStyle;
        UnselectedLabelStyle = unselectedLabelStyle;
        ShowSelectedLabels = showSelectedLabels;
        ShowUnselectedLabels = showUnselectedLabels;
    }

    public IReadOnlyList<BottomNavigationBarItem> Items { get; }

    public Action<int>? OnTap { get; }

    public int CurrentIndex { get; }

    public BottomNavigationBarType? Type { get; }

    public Color? BackgroundColor { get; }

    public Color? SelectedItemColor { get; }

    public Color? UnselectedItemColor { get; }

    public IconThemeData? SelectedIconTheme { get; }

    public IconThemeData? UnselectedIconTheme { get; }

    public double? Elevation { get; }

    public double IconSize { get; }

    public double SelectedFontSize { get; }

    public double UnselectedFontSize { get; }

    public TextStyle? SelectedLabelStyle { get; }

    public TextStyle? UnselectedLabelStyle { get; }

    public bool? ShowSelectedLabels { get; }

    public bool? ShowUnselectedLabels { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var bottomTheme = BottomNavigationBarTheme.Of(context);
        var effectiveType = ResolveEffectiveType(bottomTheme);
        var effectiveBackground = ResolveBackgroundColor(theme, bottomTheme, effectiveType);
        var effectiveSelectedColor = SelectedItemColor
                                     ?? bottomTheme.SelectedItemColor
                                     ?? ResolveDefaultSelectedColor(theme, effectiveType);
        var effectiveUnselectedColor = UnselectedItemColor
                                       ?? bottomTheme.UnselectedItemColor
                                       ?? ResolveDefaultUnselectedColor(theme, effectiveType);
        var effectiveSelectedIconTheme = ResolveIconTheme(
            SelectedIconTheme ?? bottomTheme.SelectedIconTheme,
            effectiveSelectedColor,
            IconSize);
        var effectiveUnselectedIconTheme = ResolveIconTheme(
            UnselectedIconTheme ?? bottomTheme.UnselectedIconTheme,
            effectiveUnselectedColor,
            IconSize);
        var labelBaseStyle = theme.TextTheme.LabelLarge with
        {
            FontSize = SelectedFontSize
        };
        var effectiveSelectedLabelStyle = ResolveLabelStyle(
            labelBaseStyle,
            SelectedLabelStyle ?? bottomTheme.SelectedLabelStyle,
            SelectedFontSize,
            effectiveSelectedColor);
        var effectiveUnselectedLabelStyle = ResolveLabelStyle(
            labelBaseStyle,
            UnselectedLabelStyle ?? bottomTheme.UnselectedLabelStyle,
            UnselectedFontSize,
            effectiveUnselectedColor);
        var effectiveShowSelectedLabels = ShowSelectedLabels
                                          ?? bottomTheme.ShowSelectedLabels
                                          ?? true;
        var effectiveShowUnselectedLabels = ShowUnselectedLabels
                                            ?? bottomTheme.ShowUnselectedLabels
                                            ?? (effectiveType == BottomNavigationBarType.Fixed);
        var effectiveElevation = Elevation ?? bottomTheme.Elevation;

        var tiles = new List<Widget>(Items.Count);
        for (var index = 0; index < Items.Count; index++)
        {
            var itemIndex = index;
            var selected = index == CurrentIndex;
            var item = Items[index];
            var icon = selected ? item.ActiveIcon : item.Icon;
            var iconTheme = selected ? effectiveSelectedIconTheme : effectiveUnselectedIconTheme;
            var showLabel = selected ? effectiveShowSelectedLabels : effectiveShowUnselectedLabels;
            var labelStyle = selected ? effectiveSelectedLabelStyle : effectiveUnselectedLabelStyle;
            var tileFlex = effectiveType == BottomNavigationBarType.Shifting
                ? (selected ? 3 : 2)
                : 1;

            var tileChildren = new List<Widget>
            {
                new IconTheme(
                    data: iconTheme,
                    child: icon),
            };

            if (showLabel)
            {
                tileChildren.Add(CreateLabel(item.Label, labelStyle));
            }

            var tileContent = new SizedBox(
                height: DefaultHeight,
                child: new Center(
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Center,
                        spacing: 4,
                        children: tileChildren)));

            tiles.Add(new Expanded(
                flex: tileFlex,
                child: new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: OnTap is null ? null : () => OnTap(itemIndex),
                    child: tileContent)));
        }

        Widget bar = new Container(
            color: effectiveBackground,
            child: new SafeArea(
                top: false,
                child: new SizedBox(
                    height: DefaultHeight,
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                        spacing: 0,
                        children: tiles))));

        if (effectiveElevation.HasValue && effectiveElevation.Value > 0)
        {
            bar = new Container(
                decoration: new BoxDecoration(
                    Color: effectiveBackground,
                    BoxShadows: BuildBoxShadows(theme.ShadowColor, effectiveElevation.Value)),
                child: new SafeArea(
                    top: false,
                    child: new SizedBox(
                        height: DefaultHeight,
                        child: new Row(
                            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                            spacing: 0,
                            children: tiles))));
        }

        return bar;
    }

    private BottomNavigationBarType ResolveEffectiveType(BottomNavigationBarThemeData bottomTheme)
    {
        return Type
               ?? bottomTheme.Type
               ?? (Items.Count <= 3
                   ? BottomNavigationBarType.Fixed
                   : BottomNavigationBarType.Shifting);
    }

    private Color ResolveBackgroundColor(
        ThemeData theme,
        BottomNavigationBarThemeData bottomTheme,
        BottomNavigationBarType effectiveType)
    {
        if (effectiveType == BottomNavigationBarType.Shifting)
        {
            var shiftingBackground = Items[CurrentIndex].BackgroundColor;
            if (shiftingBackground.HasValue)
            {
                return shiftingBackground.Value;
            }
        }

        return BackgroundColor
               ?? bottomTheme.BackgroundColor
               ?? theme.CanvasColor;
    }

    private static TextStyle ResolveLabelStyle(
        TextStyle baseStyle,
        TextStyle? overrideStyle,
        double fallbackFontSize,
        Color fallbackColor)
    {
        overrideStyle ??= new TextStyle();
        return new TextStyle(
            FontFamily: overrideStyle.FontFamily ?? baseStyle.FontFamily,
            FontSize: overrideStyle.FontSize ?? fallbackFontSize,
            Color: overrideStyle.Color ?? fallbackColor,
            FontWeight: overrideStyle.FontWeight ?? baseStyle.FontWeight,
            FontStyle: overrideStyle.FontStyle ?? baseStyle.FontStyle,
            Height: overrideStyle.Height ?? baseStyle.Height,
            LetterSpacing: overrideStyle.LetterSpacing ?? baseStyle.LetterSpacing);
    }

    private static IconThemeData ResolveIconTheme(
        IconThemeData? iconTheme,
        Color fallbackColor,
        double fallbackSize)
    {
        return new IconThemeData(
            Color: iconTheme?.Color ?? fallbackColor,
            Size: iconTheme?.Size ?? fallbackSize);
    }

    private static void ValidateIconTheme(string paramName, IconThemeData? iconTheme)
    {
        if (iconTheme?.Size is not double size)
        {
            return;
        }

        if (!double.IsFinite(size) || size < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Icon theme size must be finite and non-negative.");
        }
    }

    private static Color ResolveDefaultSelectedColor(ThemeData theme, BottomNavigationBarType type)
    {
        if (type == BottomNavigationBarType.Shifting)
        {
            return Colors.White;
        }

        return theme.PrimaryColor;
    }

    private static Color ResolveDefaultUnselectedColor(ThemeData theme, BottomNavigationBarType type)
    {
        if (type == BottomNavigationBarType.Shifting)
        {
            return MaterialButtonCore.ApplyOpacity(Colors.White, 0.70);
        }

        return theme.UseMaterial3
            ? theme.OnSurfaceVariantColor
            : MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.60);
    }

    private static BoxShadows? BuildBoxShadows(Color shadowColor, double elevation)
    {
        if (elevation <= 0 || shadowColor.A == 0)
        {
            return null;
        }

        var keyShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(2, elevation * 2.4),
            Spread = 0,
            Color = MaterialButtonCore.ApplyOpacity(shadowColor, 0.20),
            IsInset = false,
        };

        var ambientShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(3, elevation * 3.2),
            Spread = 0,
            Color = MaterialButtonCore.ApplyOpacity(shadowColor, 0.14),
            IsInset = false,
        };

        return new BoxShadows(keyShadow, [ambientShadow]);
    }

    private static Widget CreateLabel(string label, TextStyle style)
    {
        return new Text(
            label,
            fontFamily: style.FontFamily,
            fontSize: style.FontSize,
            color: style.Color,
            fontWeight: style.FontWeight,
            fontStyle: style.FontStyle,
            height: style.Height,
            letterSpacing: style.LetterSpacing,
            softWrap: false,
            maxLines: 1,
            overflow: TextOverflow.Clip);
    }

}
