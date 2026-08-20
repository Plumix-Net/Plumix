using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/bottom_tab_bar.dart

/// <summary>An iOS-styled bottom navigation tab bar.</summary>
public sealed class CupertinoTabBar : StatelessWidget, IPreferredSizeWidget
{
    private const double DefaultHeight = 50.0;
    private static readonly CupertinoDynamicColor DefaultTabBarBorderColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0x4D000000),
            Color.FromUInt32(0x29000000));
    private static readonly Border DefaultBorder = new(
        top: new BorderSide(
            Color.FromUInt32(0x4D000000),
            width: 0.0));

    public CupertinoTabBar(
        IReadOnlyList<BottomNavigationBarItem> items,
        Action<int>? onTap = null,
        int currentIndex = 0,
        CupertinoDynamicColor? backgroundColor = null,
        CupertinoDynamicColor? activeColor = null,
        CupertinoDynamicColor? inactiveColor = null,
        double iconSize = 30.0,
        double height = DefaultHeight,
        Key? key = null)
        : this(
            items,
            DefaultBorder,
            onTap,
            currentIndex,
            backgroundColor,
            activeColor,
            inactiveColor,
            iconSize,
            height,
            key)
    {
    }

    public CupertinoTabBar(
        IReadOnlyList<BottomNavigationBarItem> items,
        Border? border,
        Action<int>? onTap = null,
        int currentIndex = 0,
        CupertinoDynamicColor? backgroundColor = null,
        CupertinoDynamicColor? activeColor = null,
        CupertinoDynamicColor? inactiveColor = null,
        double iconSize = 30.0,
        double height = DefaultHeight,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count < 2)
        {
            throw new ArgumentException(
                "Tabs need at least 2 items to conform to Apple's HIG.",
                nameof(items));
        }

        if (currentIndex < 0 || currentIndex >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(currentIndex));
        }

        if (!double.IsFinite(height) || height < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Items = items;
        OnTap = onTap;
        CurrentIndex = currentIndex;
        BackgroundColor = backgroundColor;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor ?? CupertinoColors.InactiveGray;
        IconSize = iconSize;
        Height = height;
        Border = border;
    }

    public IReadOnlyList<BottomNavigationBarItem> Items { get; }

    public Action<int>? OnTap { get; }

    public int CurrentIndex { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public CupertinoDynamicColor? ActiveColor { get; }

    public CupertinoDynamicColor InactiveColor { get; }

    public double IconSize { get; }

    public double Height { get; }

    public Border? Border { get; }

    public Size PreferredSize => new(double.PositiveInfinity, Height);

    public bool Opaque(BuildContext context)
    {
        CupertinoDynamicColor backgroundColor = BackgroundColor ?? CupertinoTheme.Of(context).BarBackgroundColor;
        return CupertinoDynamicColor.Resolve(backgroundColor, context).A == byte.MaxValue;
    }

    public override Widget Build(BuildContext context)
    {
        double bottomPadding = MediaQuery.ViewPaddingOf(context).Bottom;
        Color backgroundColor = CupertinoDynamicColor.Resolve(
            BackgroundColor ?? CupertinoTheme.Of(context).BarBackgroundColor,
            context);
        Border? resolvedBorder = ResolveBorder(context);
        Color inactive = CupertinoDynamicColor.Resolve(InactiveColor, context);

        Widget result = new DecoratedBox(
            decoration: new BoxDecoration(
                Color: backgroundColor,
                Border: resolvedBorder),
            child: new SizedBox(
                height: Height + bottomPadding,
                child: IconTheme.Merge(
                    data: new IconThemeData(Color: inactive, Size: IconSize),
                    child: DefaultTextStyle.Merge(
                        style: CupertinoTheme.Of(context).TextTheme.TabLabelTextStyle.CopyWith(color: inactive),
                        child: new Padding(
                            new Thickness(0.0, 0.0, 0.0, bottomPadding),
                            child: new Semantics(
                                explicitChildNodes: true,
                                child: new Row(
                                    crossAxisAlignment: CrossAxisAlignment.End,
                                    children: BuildTabItems(context))))))));

        if (!Opaque(context))
        {
            result = new ClipRect(
                child: new BackdropFilter(
                    filter: new ImageFilter.Blur(sigmaX: 10.0, sigmaY: 10.0),
                    child: result));
        }

        return result;
    }

    public CupertinoTabBar CopyWith(
        Key? key = null,
        IReadOnlyList<BottomNavigationBarItem>? items = null,
        CupertinoDynamicColor? backgroundColor = null,
        CupertinoDynamicColor? activeColor = null,
        CupertinoDynamicColor? inactiveColor = null,
        double? iconSize = null,
        double? height = null,
        Border? border = null,
        int? currentIndex = null,
        Action<int>? onTap = null)
    {
        return new CupertinoTabBar(
            items ?? Items,
            border ?? Border,
            onTap ?? OnTap,
            currentIndex ?? CurrentIndex,
            backgroundColor ?? BackgroundColor,
            activeColor ?? ActiveColor,
            inactiveColor ?? InactiveColor,
            iconSize ?? IconSize,
            height ?? Height,
            key ?? Key);
    }

    private Border? ResolveBorder(BuildContext context)
    {
        if (ReferenceEquals(Border, DefaultBorder))
        {
            return new Border(
                top: DefaultBorder.Top.CopyWith(
                    color: CupertinoDynamicColor.Resolve(DefaultTabBarBorderColor, context)));
        }

        return Border;
    }

    private IReadOnlyList<Widget> BuildTabItems(BuildContext context)
    {
        var result = new List<Widget>(Items.Count);
        CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);

        for (int index = 0; index < Items.Count; index++)
        {
            int itemIndex = index;
            bool active = itemIndex == CurrentIndex;
            Widget item = new Expanded(
                child: new TextFieldTapRegion(
                    child: new Semantics(
                        selected: active,
                        hint: localizations.TabSemanticsLabel(itemIndex + 1, Items.Count),
                        child: new MouseRegion(
                            cursor: PlatformDefaults.IsWeb ? SystemMouseCursors.Click : MouseCursor.Defer,
                            child: new GestureDetector(
                                behavior: HitTestBehavior.Opaque,
                                onTap: OnTap is null ? null : () => OnTap(itemIndex),
                                child: new Padding(
                                    new Thickness(0.0, 0.0, 0.0, 4.0),
                                    child: new Column(
                                        mainAxisAlignment: MainAxisAlignment.End,
                                        children: BuildSingleTabItem(Items[itemIndex], active))))))));
            result.Add(WrapActiveItem(context, item, active));
        }

        return result;
    }

    private static IReadOnlyList<Widget> BuildSingleTabItem(BottomNavigationBarItem item, bool active)
    {
        var result = new List<Widget>
        {
            new Expanded(child: new Center(child: active ? item.ActiveIcon : item.Icon)),
        };
        if (item.Label is not null)
        {
            result.Add(new Text(item.Label, semanticsLabel: item.SemanticsLabel));
        }

        return result;
    }

    private Widget WrapActiveItem(BuildContext context, Widget item, bool active)
    {
        if (!active)
        {
            return item;
        }

        Color activeColor = CupertinoDynamicColor.Resolve(
            ActiveColor ?? CupertinoTheme.Of(context).PrimaryColor,
            context);
        return IconTheme.Merge(
            data: new IconThemeData(Color: activeColor),
            child: DefaultTextStyle.Merge(
                style: new TextStyle(Color: activeColor),
                child: item));
    }
}
