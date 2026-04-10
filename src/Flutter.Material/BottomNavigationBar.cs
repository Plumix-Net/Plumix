using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart; flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart (baseline fixed-type subset)

public sealed class BottomNavigationBarItem
{
    public BottomNavigationBarItem(
        Widget icon,
        string label,
        Widget? activeIcon = null,
        Color? backgroundColor = null)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ActiveIcon = activeIcon ?? icon;
        BackgroundColor = backgroundColor;
    }

    public Widget Icon { get; }

    public Widget ActiveIcon { get; }

    public string Label { get; }

    public Color? BackgroundColor { get; }
}

public sealed class BottomNavigationBar : StatelessWidget
{
    private const double DefaultHeight = 56.0;
    private const double DefaultIconSize = 24.0;

    public BottomNavigationBar(
        IReadOnlyList<BottomNavigationBarItem> items,
        Action<int>? onTap = null,
        int currentIndex = 0,
        Color? backgroundColor = null,
        Color? selectedItemColor = null,
        Color? unselectedItemColor = null,
        double iconSize = DefaultIconSize,
        double selectedFontSize = 14.0,
        double unselectedFontSize = 12.0,
        bool showSelectedLabels = true,
        bool showUnselectedLabels = true,
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

        Items = items;
        OnTap = onTap;
        CurrentIndex = currentIndex;
        BackgroundColor = backgroundColor;
        SelectedItemColor = selectedItemColor;
        UnselectedItemColor = unselectedItemColor;
        IconSize = iconSize;
        SelectedFontSize = selectedFontSize;
        UnselectedFontSize = unselectedFontSize;
        ShowSelectedLabels = showSelectedLabels;
        ShowUnselectedLabels = showUnselectedLabels;
    }

    public IReadOnlyList<BottomNavigationBarItem> Items { get; }

    public Action<int>? OnTap { get; }

    public int CurrentIndex { get; }

    public Color? BackgroundColor { get; }

    public Color? SelectedItemColor { get; }

    public Color? UnselectedItemColor { get; }

    public double IconSize { get; }

    public double SelectedFontSize { get; }

    public double UnselectedFontSize { get; }

    public bool ShowSelectedLabels { get; }

    public bool ShowUnselectedLabels { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var effectiveBackground = BackgroundColor ?? theme.CanvasColor;
        var effectiveSelectedColor = SelectedItemColor ?? theme.PrimaryColor;
        var effectiveUnselectedColor = UnselectedItemColor ?? ResolveDefaultUnselectedColor(theme);
        var labelBaseStyle = theme.TextTheme.LabelLarge;

        var tiles = new List<Widget>(Items.Count);
        for (var index = 0; index < Items.Count; index++)
        {
            var itemIndex = index;
            var selected = index == CurrentIndex;
            var item = Items[index];
            var itemColor = selected ? effectiveSelectedColor : effectiveUnselectedColor;
            var icon = selected ? item.ActiveIcon : item.Icon;
            var showLabel = selected ? ShowSelectedLabels : ShowUnselectedLabels;
            var labelFontSize = selected ? SelectedFontSize : UnselectedFontSize;
            var labelStyle = labelBaseStyle with { Color = itemColor, FontSize = labelFontSize };

            var tileChildren = new List<Widget>
            {
                new IconTheme(
                    data: new IconThemeData(
                        Color: itemColor,
                        Size: IconSize),
                    child: icon),
            };

            if (showLabel)
            {
                tileChildren.Add(CreateLabel(item.Label, labelStyle));
            }

            tiles.Add(new Expanded(
                child: new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: OnTap is null ? null : () => OnTap(itemIndex),
                    child: new SizedBox(
                        height: DefaultHeight,
                        child: new Center(
                            child: new Column(
                                mainAxisSize: MainAxisSize.Min,
                                crossAxisAlignment: CrossAxisAlignment.Center,
                                spacing: 4,
                                children: tileChildren))))));
        }

        return new Container(
            color: effectiveBackground,
            child: new SafeArea(
                top: false,
                child: new SizedBox(
                    height: DefaultHeight,
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                        spacing: 0,
                        children: tiles))));
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

    private static Color ResolveDefaultUnselectedColor(ThemeData theme)
    {
        return theme.UseMaterial3
            ? theme.OnSurfaceVariantColor
            : MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.60);
    }
}
