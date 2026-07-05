using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/sample_gallery_screen.dart; dart_sample/lib/sample_routes.dart (exact sample parity)

namespace Plumix;

internal static class SampleNavigationObservers
{
    public static RouteObserver<PageRoute> PageRoutes { get; } = new();
}

internal static class SampleRoutes
{
    public const string Menu = "/";
    public const string Counter = "/counter";
    public const string BlocCounter = "/bloc-counter";
    public const string Navigator = "/navigator";
    public const string NavigatorDetails = "/navigator/details";
    public const string ListViewSeparated = "/list-separated";
    public const string ListViewFixedExtent = "/list-fixed-extent";
    public const string ListViewReverse = "/list-reverse";
    public const string GridView = "/grid-view";
    public const string CustomSlivers = "/custom-slivers";
    public const string Scrollbar = "/scrollbar";
    public const string EditableText = "/editable-text";
    public const string MaterialButtons = "/material-buttons";
    public const string ActionButtons = "/action-buttons";
    public const string Drawer = "/drawer";
    public const string DrawerHeaders = "/drawer-headers";
    public const string Divider = "/divider";
    public const string BadgeTooltip = "/badge-tooltip";
    public const string CircleAvatar = "/circle-avatar";
    public const string NavigationSurfaces = "/navigation-surfaces";
    public const string NavigationDrawer = "/navigation-drawer";
    public const string SegmentedButtons = "/segmented-buttons";
    public const string Banner = "/banner";
    public const string SnackBar = "/snack-bar";
    public const string Dialog = "/dialog";
    public const string PopupMenu = "/popup-menu";
    public const string Dropdown = "/dropdown";
    public const string Chips = "/chips";
    public const string LinearProgressIndicator = "/linear-progress-indicator";
    public const string CircularProgressIndicator = "/circular-progress-indicator";
    public const string Slider = "/slider";
    public const string RangeSlider = "/range-slider";
    public const string Card = "/card";
    public const string GridTile = "/grid-tile";
    public const string ListTile = "/list-tile";
    public const string ListTileControls = "/list-tile-controls";
    public const string RadioExpansionTile = "/radio-expansion-tile";
    public const string ExpansionPanel = "/expansion-panel";
    public const string FloatingActionButton = "/floating-action-button";
    public const string Checkbox = "/checkbox";
    public const string Switch = "/switch";
    public const string Radio = "/radio";
    public const string AppBarLeadingWidth = "/appbar-leading-width";
    public const string AppBarActionsPadding = "/appbar-actions-padding";
    public const string AppBarIconTheme = "/appbar-icon-theme";
    public const string AppBarTextStyles = "/appbar-text-styles";
    public const string ProxyWidgets = "/proxy-widgets";
    public const string Align = "/align";
    public const string Stack = "/stack";
    public const string DecoratedBox = "/decorated-box";
    public const string Container = "/container";
    public const string AspectRatio = "/aspect-ratio";
    public const string FractionallySizedBox = "/fractionally-sized-box";
    public const string FittedBox = "/fitted-box";
    public const string UnconstrainedLimitedBox = "/unconstrained-limited-box";
    public const string OverflowBox = "/overflow-box";
    public const string OverflowIndicator = "/overflow-indicator";
    public const string Offstage = "/offstage";
}

internal readonly record struct SampleRouteDefinition(
    string RouteName,
    string Title,
    string Subtitle,
    Func<Widget> Builder);

internal readonly record struct SampleMenuTabDefinition(
    string Label,
    string Description,
    IconData Icon,
    IconData ActiveIcon,
    IReadOnlyList<SampleRouteDefinition> Pages);

internal sealed class SampleGalleryScreen : StatelessWidget
{
    private static readonly IReadOnlyList<SampleRouteDefinition> MaterialDemoPages =
    [
        new(SampleRoutes.MaterialButtons, "Material buttons", "TextButton + ElevatedButton + OutlinedButton + FilledButton", () => new MaterialButtonsDemoPage()),
        new(SampleRoutes.ActionButtons, "Material action buttons", "back/close/drawer/end-drawer + ActionIconTheme", () => new ActionButtonsDemoPage()),
        new(SampleRoutes.Drawer, "Drawer", "scaffold drawer/endDrawer + theme/widget precedence probes", () => new DrawerDemoPage()),
        new(SampleRoutes.DrawerHeaders, "Drawer headers", "DrawerHeader + UserAccountsDrawerHeader", () => new DrawerHeadersDemoPage()),
        new(SampleRoutes.Divider, "Divider", "horizontal/vertical divider + theme/widget precedence probes", () => new DividerDemoPage()),
        new(SampleRoutes.BadgeTooltip, "Badge + Tooltip", "count/small badges + hover/long-press tooltip theming", () => new BadgeTooltipDemoPage()),
        new(SampleRoutes.CircleAvatar, "CircleAvatar", "initials + image layers + animated radius + fallback", () => new CircleAvatarDemoPage()),
        new(SampleRoutes.NavigationSurfaces, "NavigationBar + NavigationRail", "horizontal/vertical Material navigation + labels/themes", () => new NavigationSurfacesDemoPage()),
        new(SampleRoutes.NavigationDrawer, "NavigationDrawer", "destinations + custom children + selection/theme probes", () => new NavigationDrawerDemoPage()),
        new(SampleRoutes.SegmentedButtons, "ToggleButtons + SegmentedButton", "legacy/M3 segmented selection + themes/states", () => new SegmentedButtonsDemoPage()),
        new(SampleRoutes.Banner, "Banner + MaterialBanner", "diagonal ribbon + persistent actions/theme/overflow probes", () => new BannerDemoPage()),
        new(SampleRoutes.SnackBar, "SnackBar + SnackBarAction", "messenger queue + action/close/overflow probes", () => new SnackBarDemoPage()),
        new(SampleRoutes.Dialog, "Dialog family", "modal route + alert/simple/result/scrollable/theme probes", () => new DialogDemoPage()),
        new(SampleRoutes.PopupMenu, "PopupMenuButton + PopupMenuItem", "anchor + selection/cancel/keyboard/theme probes", () => new PopupMenuDemoPage()),
        new(SampleRoutes.Dropdown, "DropdownButton + DropdownMenuItem", "selection + hint/disabled/dense/expanded/route probes", () => new DropdownDemoPage()),
        new(SampleRoutes.Chips, "Material chips", "action/choice/filter/input + selection/deletion/theme probes", () => new ChipsDemoPage()),
        new(SampleRoutes.LinearProgressIndicator, "LinearProgressIndicator", "determinate/indeterminate + M2/M3 + theme/widget/RTL probes", () => new LinearProgressIndicatorDemoPage()),
        new(SampleRoutes.CircularProgressIndicator, "CircularProgressIndicator", "determinate/indeterminate + M2/M3 + theme/widget probes", () => new CircularProgressIndicatorDemoPage()),
        new(SampleRoutes.Slider, "Slider", "continuous/discrete + drag/tap/keyboard + theme/widget colors", () => new SliderDemoPage()),
        new(SampleRoutes.RangeSlider, "RangeSlider", "two-thumb range + continuous/discrete + theme/widget colors", () => new RangeSliderDemoPage()),
        new(SampleRoutes.Card, "Card", "elevated/filled/outlined variants + theme/clip probes", () => new CardDemoPage()),
        new(SampleRoutes.GridTile, "GridTile + GridTileBar", "header/footer overlays + one/two-line bars + RTL", () => new GridTileDemoPage()),
        new(SampleRoutes.ListTile, "ListTile", "leading/title/subtitle/trailing + selected/dense/theme probes", () => new ListTileDemoPage()),
        new(SampleRoutes.ListTileControls, "CheckboxListTile + SwitchListTile", "whole-row toggle + tristate + affinity + adaptive probes", () => new ListTileControlsDemoPage()),
        new(SampleRoutes.RadioExpansionTile, "RadioListTile + ExpansionTile", "RadioGroup + toggleable/adaptive + animated controller expansion", () => new RadioExpansionTileDemoPage()),
        new(SampleRoutes.ExpansionPanel, "ExpansionPanel + ExpansionPanelList", "controlled panels + radio accordion + animated material gaps", () => new ExpansionPanelDemoPage()),
        new(SampleRoutes.FloatingActionButton, "FloatingActionButton", "regular/small/large/extended + theme defaults", () => new FloatingActionButtonDemoPage()),
        new(SampleRoutes.AppBarLeadingWidth, "AppBar leadingWidth theme", "theme fallback + widget override runtime probe", () => new AppBarLeadingWidthDemoPage()),
        new(SampleRoutes.AppBarActionsPadding, "AppBar actionsPadding theme", "theme fallback + widget override runtime probe", () => new AppBarActionsPaddingDemoPage()),
        new(SampleRoutes.AppBarIconTheme, "AppBar icon themes", "iconTheme/actionsIconTheme precedence runtime probe", () => new AppBarIconThemeDemoPage()),
        new(SampleRoutes.AppBarTextStyles, "AppBar text styles", "title/toolbar text style precedence runtime probe", () => new AppBarTextStylesDemoPage()),
    ];

    private static readonly IReadOnlyList<SampleRouteDefinition> CupertinoDemoPages =
    [
        new(SampleRoutes.Checkbox, "Checkbox", "bool/bool? values + tristate + tap-target policy", () => new CheckboxDemoPage()),
        new(SampleRoutes.Switch, "Switch", "on/off value + track/thumb theming + drag", () => new SwitchDemoPage()),
        new(SampleRoutes.Radio, "Radio", "group selection + toggleable + tap-target policy", () => new RadioDemoPage()),
    ];

    private static readonly IReadOnlyList<SampleRouteDefinition> GeneralDemoPages =
    [
        new(SampleRoutes.Counter, "Counter", "existing sample", () => new CounterScreen()),
        new(SampleRoutes.BlocCounter, "Bloc counter", "BlocProvider + BlocBuilder + BlocListener + BlocSelector", () => new BlocCounterDemoPage()),
        new(SampleRoutes.Navigator, "Navigator", "named routes + RouteData + stack APIs", () => new NavigatorDemoPage()),
        new(SampleRoutes.ListViewSeparated, "ListView.Separated", "item + separator builder", () => new ListViewSeparatedDemoPage()),
        new(SampleRoutes.ListViewFixedExtent, "ListView fixed extent", "itemExtent + padding", () => new ListViewFixedExtentDemoPage()),
        new(SampleRoutes.ListViewReverse, "ListView reverse", "reverse=true behavior", () => new ListViewReverseDemoPage()),
        new(SampleRoutes.GridView, "GridView + SliverGrid", "delegate-based 2D layout", () => new GridViewDemoPage()),
        new(SampleRoutes.CustomSlivers, "Custom slivers", "SliverPadding + SliverFixedExtentList", () => new CustomSliversDemoPage()),
        new(SampleRoutes.Scrollbar, "Scrollbar", "controller + thumb", () => new ScrollbarDemoPage()),
        new(SampleRoutes.EditableText, "EditableText", "focus + IME + multiline caret", () => new EditableTextDemoPage()),
        new(SampleRoutes.ProxyWidgets, "Proxy widgets", "Opacity + Transform + ClipRect composition", () => new ProxyWidgetsDemoPage()),
        new(SampleRoutes.Align, "Align + Center", "single-child alignment and shrink factors", () => new AlignDemoPage()),
        new(SampleRoutes.Stack, "Stack + Positioned", "multi-child overlay layout", () => new StackDemoPage()),
        new(SampleRoutes.DecoratedBox, "DecoratedBox", "border + radius + fill decoration", () => new DecoratedBoxDemoPage()),
        new(SampleRoutes.Container, "Container", "alignment + margin + constraints + transform", () => new ContainerDemoPage()),
        new(SampleRoutes.AspectRatio, "AspectRatio + Spacer", "tight ratio layout + flex gap", () => new AspectRatioDemoPage()),
        new(SampleRoutes.FractionallySizedBox, "FractionallySizedBox", "fractional constraints + alignment", () => new FractionallySizedBoxDemoPage()),
        new(SampleRoutes.FittedBox, "FittedBox", "box-fit scaling + alignment", () => new FittedBoxDemoPage()),
        new(SampleRoutes.UnconstrainedLimitedBox, "UnconstrainedBox + LimitedBox", "axis unconstraint + unbounded max clamps", () => new UnconstrainedLimitedBoxDemoPage()),
        new(SampleRoutes.OverflowBox, "OverflowBox + SizedOverflowBox", "constraint override + fixed-size overflow", () => new OverflowBoxDemoPage()),
        new(SampleRoutes.OverflowIndicator, "Overflow indicator", "RenderFlex debug stripes + overflow label", () => new OverflowIndicatorDemoPage()),
        new(SampleRoutes.Offstage, "Offstage", "layout-without-paint and zero-space behavior", () => new OffstageDemoPage()),
    ];

    private static readonly IReadOnlyList<SampleMenuTabDefinition> DemoTabs =
    [
        new(
            Label: "Material",
            Description: "Material controls and theming demos.",
            Icon: Icons.StarOutline,
            ActiveIcon: Icons.Star,
            Pages: MaterialDemoPages),
        new(
            Label: "Cupertino",
            Description: "Adaptive Cupertino behavior probes for controls.",
            Icon: Icons.Check,
            ActiveIcon: Icons.Check,
            Pages: CupertinoDemoPages),
        new(
            Label: "General",
            Description: "Core widgets, layouts, navigation, and rendering demos.",
            Icon: Icons.Menu,
            ActiveIcon: Icons.InfoOutline,
            Pages: GeneralDemoPages),
    ];

    private static readonly IReadOnlyDictionary<string, SampleRouteDefinition> DemoPageByRoute =
        DemoTabs
            .SelectMany(tab => tab.Pages)
            .ToDictionary(page => page.RouteName, page => page);

    public override Widget Build(BuildContext context)
    {
        return new Scaffold(
            body: new Navigator(
                onGenerateRoute: BuildRoute,
                observers: [SampleNavigationObservers.PageRoutes],
                initialRouteName: SampleRoutes.Menu));
    }

    private static Route? BuildRoute(RouteSettings settings)
    {
        if (settings.Name == SampleRoutes.Menu)
        {
            return new BuilderPageRoute(
                builder: _ => new SampleMenuPage(DemoTabs),
                settings: settings);
        }

        if (settings.Name == SampleRoutes.NavigatorDetails)
        {
            var routeData = settings.Arguments as RouteData
                ?? new RouteData(SampleRoutes.NavigatorDetails, arguments: settings.Arguments);
            return new BuilderPageRoute(
                builder: _ => new SampleDemoPage(
                    title: "Navigator details",
                    subtitle: "RouteData query/arguments + push/pop operations",
                    child: new NavigatorDetailsPage(routeData)),
                settings: settings);
        }

        if (settings.Name != null && DemoPageByRoute.TryGetValue(settings.Name, out var page))
        {
            return new BuilderPageRoute(
                builder: _ => new SampleDemoPage(page, page.Builder()),
                settings: settings);
        }

        return null;
    }
}

internal sealed class SampleMenuPage : StatefulWidget
{
    private readonly IReadOnlyList<SampleMenuTabDefinition> _tabs;

    public SampleMenuPage(IReadOnlyList<SampleMenuTabDefinition> tabs)
    {
        _tabs = tabs;
    }

    public override State CreateState() => new SampleMenuPageState();

    private sealed class SampleMenuPageState : State
    {
        private int _selectedTabIndex;

        private SampleMenuPage CurrentWidget => (SampleMenuPage)StateWidget;

        public override Widget Build(BuildContext context)
        {
            var tabs = CurrentWidget._tabs;
            var selectedTab = tabs[_selectedTabIndex];
            var pages = selectedTab.Pages;

            return new Scaffold(
                appBar: new AppBar(titleText: "Plumix.Sample widget pages"),
                body: new Container(
                    padding: new Thickness(16),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 10,
                        children:
                        [
                            new Text(
                                "Route-based sample menu. Open page and return via Back button or Esc.",
                                fontSize: 14,
                                color: Color.Parse("#8A000000")),
                            new Text(
                                selectedTab.Description,
                                fontSize: 12,
                                color: Color.Parse("#73000000")),
                            new Expanded(
                                child: ListView.Builder(
                                    itemCount: pages.Count,
                                    padding: new Thickness(0, 8, 0, 8),
                                    itemExtent: 56,
                                    itemBuilder: (_, index) => BuildPageButton(context, pages[index]),
                                    addAutomaticKeepAlives: false)),
                        ])),
                bottomNavigationBar: new BottomNavigationBar(
                    currentIndex: _selectedTabIndex,
                    onTap: HandleTabSelected,
                    items: BuildBottomNavigationItems(tabs)));
        }

        private void HandleTabSelected(int index)
        {
            if (index == _selectedTabIndex)
            {
                return;
            }

            SetState(() => _selectedTabIndex = index);
        }

        private static IReadOnlyList<BottomNavigationBarItem> BuildBottomNavigationItems(IReadOnlyList<SampleMenuTabDefinition> tabs)
        {
            var items = new List<BottomNavigationBarItem>(tabs.Count);
            foreach (var tab in tabs)
            {
                items.Add(new BottomNavigationBarItem(
                    icon: new Icon(tab.Icon),
                    activeIcon: new Icon(tab.ActiveIcon),
                    label: tab.Label));
            }

            return items;
        }

        private static Widget BuildPageButton(BuildContext context, SampleRouteDefinition page)
        {
            return new OutlinedButton(
                onPressed: () => Navigator.Of(context).PushNamed(page.RouteName),
                backgroundColor: Color.Parse("#FFDCE3ED"),
                borderColor: Color.Parse("#FFB8C4D4"),
                foregroundColor: Colors.Black,
                minHeight: 44,
                padding: new Thickness(10, 8),
                child: new Text($"{page.Title}  |  {page.Subtitle}", fontSize: 12));
        }
    }
}

internal sealed class SampleDemoPage : StatelessWidget
{
    private readonly string _title;
    private readonly string _subtitle;
    private readonly Widget _child;

    public SampleDemoPage(SampleRouteDefinition page, Widget child)
        : this(page.Title, page.Subtitle, child)
    {
    }

    public SampleDemoPage(string title, string subtitle, Widget child)
    {
        _title = title;
        _subtitle = subtitle;
        _child = child;
    }

    public override Widget Build(BuildContext context)
    {
        return new Scaffold(
            appBar: new AppBar(titleText: _title),
            body: new Container(
                padding: new Thickness(16),
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 10,
                    children:
                    [
                        new Text(_subtitle, fontSize: 14, color: Color.Parse("#8A000000")),
                        new Expanded(
                            child: new Container(
                                color: Color.Parse("#FFF7F9FC"),
                                padding: new Thickness(12),
                                child: _child)),
                    ])));
    }
}
