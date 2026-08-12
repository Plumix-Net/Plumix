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
    public const string ScrollPhysics = "/scroll-physics";
    public const string ListViewReverse = "/list-reverse";
    public const string GridView = "/grid-view";
    public const string CustomSlivers = "/custom-slivers";
    public const string Scrollbar = "/scrollbar";
    public const string DraggableScrollableSheet = "/draggable-scrollable-sheet";
    public const string EditableText = "/editable-text";
    public const string MaterialButtons = "/material-buttons";
    public const string AnimatedIcon = "/animated-icon";
    public const string Tabs = "/tabs";
    public const string InkResponse = "/ink-response";
    public const string DatePicker = "/date-picker";
    public const string ActionButtons = "/action-buttons";
    public const string Drawer = "/drawer";
    public const string DrawerHeaders = "/drawer-headers";
    public const string Divider = "/divider";
    public const string BadgeTooltip = "/badge-tooltip";
    public const string Magnifier = "/magnifier";
    public const string SelectionHandles = "/selection-handles";
    public const string CircleAvatar = "/circle-avatar";
    public const string NavigationSurfaces = "/navigation-surfaces";
    public const string NavigationDrawer = "/navigation-drawer";
    public const string SegmentedButtons = "/segmented-buttons";
    public const string Banner = "/banner";
    public const string SnackBar = "/snack-bar";
    public const string BottomSheet = "/bottom-sheet";
    public const string SliverAppBar = "/sliver-app-bar";
    public const string TextField = "/text-field";
    public const string Dialog = "/dialog";
    public const string PopupMenu = "/popup-menu";
    public const string Dropdown = "/dropdown";
    public const string Search = "/search";
    public const string Autocomplete = "/autocomplete";
    public const string Selection = "/selection";
    public const string DesktopTextSelectionToolbar = "/desktop-text-selection-toolbar";
    public const string Chips = "/chips";
    public const string LinearProgressIndicator = "/linear-progress-indicator";
    public const string CircularProgressIndicator = "/circular-progress-indicator";
    public const string RefreshIndicator = "/refresh-indicator";
    public const string BarControls = "/bar-controls";
    public const string DataTable = "/data-table";
    public const string Slider = "/slider";
    public const string RangeSlider = "/range-slider";
    public const string Card = "/card";
    public const string Carousel = "/carousel";
    public const string ReorderableList = "/reorderable-list";
    public const string AnimatedList = "/animated-list";
    public const string AnimatedGrid = "/animated-grid";
    public const string GridTile = "/grid-tile";
    public const string ListTile = "/list-tile";
    public const string ListTileControls = "/list-tile-controls";
    public const string RadioExpansionTile = "/radio-expansion-tile";
    public const string ExpansionPanel = "/expansion-panel";
    public const string Stepper = "/stepper";
    public const string About = "/about";
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
    public const string ShapeBorders = "/shape-borders";
    public const string Container = "/container";
    public const string AspectRatio = "/aspect-ratio";
    public const string FractionallySizedBox = "/fractionally-sized-box";
    public const string FittedBox = "/fitted-box";
    public const string UnconstrainedLimitedBox = "/unconstrained-limited-box";
    public const string OverflowBox = "/overflow-box";
    public const string OverflowIndicator = "/overflow-indicator";
    public const string Offstage = "/offstage";
    public const string Baseline = "/baseline";
    public const string RichText = "/rich-text";
    public const string IntrinsicWidgets = "/intrinsic-widgets";
    public const string LayoutBuilder = "/layout-builder";
    public const string KeyboardListener = "/keyboard-listener";
    public const string DebugPainting = "/debug-painting";
    public const string Flow = "/flow";
    public const string CompositedTransform = "/composited-transform";
    public const string Image = "/image";
    public const string CustomMultiChildLayout = "/custom-multi-child-layout";
    public const string DismissibleSizeChangedLayout = "/dismissible-size-changed-layout";
    public const string StateStorage = "/state-storage";
    public const string NavigationPop = "/navigation-pop";
    public const string AsyncBuilders = "/async-builders";
    public const string StatefulBuilderLookupBoundary = "/stateful-builder-lookup-boundary";
    public const string DragTarget = "/drag-target";
    public const string LifecycleUtilities = "/lifecycle-utilities";
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
        new(
            SampleRoutes.AnimatedIcon,
            "AnimatedIcon + AnimatedIcons",
            "complete generated vector catalog + progress/RTL/theme probes",
            () => new AnimatedIconDemoPage()),
        new(SampleRoutes.MaterialButtons, "Material buttons", "TextButton + ElevatedButton + OutlinedButton + FilledButton", () => new MaterialButtonsDemoPage()),
        new(SampleRoutes.Tabs, "TabBar + TabBarView", "controller + indicator + scrollable tabs + swipe pages", () => new TabsDemoPage()),
        new(SampleRoutes.InkResponse, "InkResponse + InkWell", "circle/rectangle ink + gestures + overlay states", () => new InkResponseDemoPage()),
        new(SampleRoutes.DatePicker, "Date/time picker family", "day/year/time/range selection + input modes + M2/M3 themes", () => new DatePickerDemoPage()),
        new(SampleRoutes.ActionButtons, "Material action buttons", "back/close/drawer/end-drawer + ActionIconTheme", () => new ActionButtonsDemoPage()),
        new(SampleRoutes.Drawer, "Drawer", "scaffold drawer/endDrawer + theme/widget precedence probes", () => new DrawerDemoPage()),
        new(SampleRoutes.DrawerHeaders, "Drawer headers", "DrawerHeader + UserAccountsDrawerHeader", () => new DrawerHeadersDemoPage()),
        new(SampleRoutes.Divider, "Divider", "horizontal/vertical divider + theme/widget precedence probes", () => new DividerDemoPage()),
        new(SampleRoutes.BadgeTooltip, "Badge + Tooltip", "count/small badges + hover/long-press tooltip theming", () => new BadgeTooltipDemoPage()),
        new(
            SampleRoutes.Magnifier,
            "RawMagnifier + Magnifier",
            "backdrop zoom + focal offsets + Material lens styling",
            () => new MagnifierDemoPage()),
        new(
            SampleRoutes.SelectionHandles,
            "Selection handles",
            "SelectionOverlay handles + drag endpoints + collapsed handle",
            () => new SelectionHandlesDemoPage()),
        new(SampleRoutes.CircleAvatar, "CircleAvatar", "initials + image layers + animated radius + fallback", () => new CircleAvatarDemoPage()),
        new(SampleRoutes.NavigationSurfaces, "NavigationBar + NavigationRail", "horizontal/vertical Material navigation + labels/themes", () => new NavigationSurfacesDemoPage()),
        new(SampleRoutes.NavigationDrawer, "NavigationDrawer", "destinations + custom children + selection/theme probes", () => new NavigationDrawerDemoPage()),
        new(SampleRoutes.SegmentedButtons, "ToggleButtons + SegmentedButton", "legacy/M3 segmented selection + themes/states", () => new SegmentedButtonsDemoPage()),
        new(SampleRoutes.Banner, "Banner + MaterialBanner", "diagonal ribbon + persistent actions/theme/overflow probes", () => new BannerDemoPage()),
        new(SampleRoutes.SnackBar, "SnackBar + SnackBarAction", "messenger queue + action/close/overflow probes", () => new SnackBarDemoPage()),
        new(SampleRoutes.BottomSheet, "BottomSheet + ModalBottomSheet", "persistent controller + modal route/drag/theme probes", () => new BottomSheetDemoPage()),
        new(SampleRoutes.SliverAppBar, "SliverAppBar + FlexibleSpaceBar", "collapse/parallax + pinned/floating/snap + M3 variants", () => new SliverAppBarDemoPage()),
        new(SampleRoutes.TextField, "InputDecorator + TextField", "labels/borders/supporting text + editable states", () => new TextFieldDemoPage()),
        new(SampleRoutes.Dialog, "Dialog family", "modal route + alert/simple/result/scrollable/theme probes", () => new DialogDemoPage()),
        new(SampleRoutes.PopupMenu, "PopupMenuButton + PopupMenuItem", "anchor + selection/cancel/keyboard/theme probes", () => new PopupMenuDemoPage()),
        new(SampleRoutes.Dropdown, "Dropdown controls", "button/menu + form/filter/search/keyboard/route probes", () => new DropdownDemoPage()),
        new(SampleRoutes.Search, "SearchBar + SearchAnchor", "controller-backed search view + suggestions + theme probes", () => new SearchDemoPage()),
        new(SampleRoutes.Autocomplete, "Autocomplete + RawAutocomplete", "Material defaults + custom options + keyboard/open-direction probes", () => new AutocompleteDemoPage()),
        new(
            SampleRoutes.Selection,
            "SelectableText + SelectionArea",
            "single/subtree selection + keyboard/copy/theme probes",
            () => new SelectionDemoPage()),
        new(
            SampleRoutes.DesktopTextSelectionToolbar,
            "Text selection toolbars",
            "Android overflow paging + anchored desktop action styling",
            () => new DesktopTextSelectionToolbarDemoPage()),
        new(SampleRoutes.Chips, "Material chips", "action/choice/filter/input + selection/deletion/theme probes", () => new ChipsDemoPage()),
        new(SampleRoutes.LinearProgressIndicator, "LinearProgressIndicator", "determinate/indeterminate + M2/M3 + theme/widget/RTL probes", () => new LinearProgressIndicatorDemoPage()),
        new(SampleRoutes.CircularProgressIndicator, "CircularProgressIndicator", "determinate/indeterminate + M2/M3 + theme/widget probes", () => new CircularProgressIndicatorDemoPage()),
        new(SampleRoutes.RefreshIndicator, "RefreshIndicator + RefreshProgressIndicator", "pull-to-refresh lifecycle + adaptive/no-spinner/theme probes", () => new RefreshIndicatorDemoPage()),
        new(SampleRoutes.BarControls, "BottomAppBar + ButtonBar", "FAB notch + M2/M3 surface + legacy action overflow", () => new BarControlsDemoPage()),
        new(SampleRoutes.DataTable, "DataTable + PaginatedDataTable", "sorting + selection + themes + source-backed paging", () => new DataTableDemoPage()),
        new(SampleRoutes.Slider, "Slider", "continuous/discrete + drag/tap/keyboard + theme/widget colors", () => new SliderDemoPage()),
        new(SampleRoutes.RangeSlider, "RangeSlider", "two-thumb range + continuous/discrete + theme/widget colors", () => new RangeSliderDemoPage()),
        new(SampleRoutes.Card, "Card", "elevated/filled/outlined variants + theme/clip probes", () => new CardDemoPage()),
        new(SampleRoutes.Carousel, "CarouselView", "fixed/weighted item extents + snapping + theme", () => new CarouselDemoPage()),
        new(
            SampleRoutes.ReorderableList,
            "ReorderableListView",
            "desktop handles + custom handles + keyed reorder callbacks",
            () => new ReorderableListDemoPage()),
        new(SampleRoutes.GridTile, "GridTile + GridTileBar", "header/footer overlays + one/two-line bars + RTL", () => new GridTileDemoPage()),
        new(SampleRoutes.ListTile, "ListTile", "leading/title/subtitle/trailing + selected/dense/theme probes", () => new ListTileDemoPage()),
        new(SampleRoutes.ListTileControls, "CheckboxListTile + SwitchListTile", "whole-row toggle + tristate + affinity + adaptive probes", () => new ListTileControlsDemoPage()),
        new(SampleRoutes.RadioExpansionTile, "RadioListTile + ExpansionTile", "RadioGroup + toggleable/adaptive + animated controller expansion", () => new RadioExpansionTileDemoPage()),
        new(SampleRoutes.ExpansionPanel, "ExpansionPanel + ExpansionPanelList", "controlled panels + radio accordion + animated material gaps", () => new ExpansionPanelDemoPage()),
        new(SampleRoutes.Stepper, "ExpandIcon + Stepper", "disclosure animation + vertical/horizontal step progress", () => new StepperDemoPage()),
        new(SampleRoutes.About, "AboutDialog + LicensePage", "metadata dialog + registry/package license navigation", () => new AboutDemoPage()),
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
        new(
            SampleRoutes.ScrollPhysics,
            "Scroll physics",
            "bouncing overscroll + spring back vs clamping",
            () => new ScrollPhysicsDemoPage()),
        new(
            SampleRoutes.AnimatedList,
            "AnimatedList + SliverAnimatedList",
            "insert/remove animations + separated and sliver variants",
            () => new AnimatedListDemoPage()),
        new(
            SampleRoutes.AnimatedGrid,
            "AnimatedGrid + SliverAnimatedGrid",
            "insert/remove animations + grid delegate and keyed sliver variants",
            () => new AnimatedGridDemoPage()),
        new(SampleRoutes.GridView, "GridView + SliverGrid", "delegate-based 2D layout", () => new GridViewDemoPage()),
        new(
            SampleRoutes.CustomSlivers,
            "Custom slivers",
            "resizing/floating/fill/group/prototype/varied sliver adapters",
            () => new CustomSliversDemoPage()),
        new(SampleRoutes.Scrollbar, "Scrollbar", "controller + thumb", () => new ScrollbarDemoPage()),
        new(
            SampleRoutes.DraggableScrollableSheet,
            "DraggableScrollableSheet",
            "drag-to-resize + snap + controller + actuator reset",
            () => new DraggableScrollableSheetDemoPage()),
        new(SampleRoutes.EditableText, "EditableText", "focus + IME + multiline caret", () => new EditableTextDemoPage()),
        new(
            SampleRoutes.ProxyWidgets,
            "Proxy widgets",
            "Opacity + fractional/layout transforms + custom clips",
            () => new ProxyWidgetsDemoPage()),
        new(
            SampleRoutes.Align,
            "Animations + transitions",
            "implicit motion + explicit scale/rotation controllers + sliver fade",
            () => new AlignDemoPage()),
        new(SampleRoutes.Stack, "Stack + Positioned", "multi-child overlay layout", () => new StackDemoPage()),
        new(SampleRoutes.DecoratedBox, "DecoratedBox", "border + radius + fill decoration", () => new DecoratedBoxDemoPage()),
        new(
            SampleRoutes.ShapeBorders,
            "ShapeBorders",
            "outlined border shapes + lerp",
            () => new ShapeBordersDemoPage()),
        new(SampleRoutes.Container, "Container", "alignment + margin + constraints + transform", () => new ContainerDemoPage()),
        new(SampleRoutes.AspectRatio, "AspectRatio + Spacer", "tight ratio layout + flex gap", () => new AspectRatioDemoPage()),
        new(SampleRoutes.FractionallySizedBox, "FractionallySizedBox", "fractional constraints + alignment", () => new FractionallySizedBoxDemoPage()),
        new(SampleRoutes.FittedBox, "FittedBox", "box-fit scaling + alignment", () => new FittedBoxDemoPage()),
        new(
            SampleRoutes.UnconstrainedLimitedBox,
            "ConstraintsTransformBox + UnconstrainedBox",
            "arbitrary/axis constraint transforms + overflow clipping",
            () => new UnconstrainedLimitedBoxDemoPage()),
        new(SampleRoutes.OverflowBox, "OverflowBox + SizedOverflowBox", "constraint override + fixed-size overflow", () => new OverflowBoxDemoPage()),
        new(SampleRoutes.OverflowIndicator, "Overflow indicator", "RenderFlex debug stripes + overflow label", () => new OverflowIndicatorDemoPage()),
        new(
            SampleRoutes.Offstage,
            "Visibility + SliverVisibility + Offstage",
            "replacement + maintained size + sliver and offstage behavior",
            () => new OffstageDemoPage()),
        new(
            SampleRoutes.Baseline,
            "Baseline + IgnoreBaseline",
            "real text baselines + bottom fallback + Row exclusion",
            () => new BaselineDemoPage()),
        new(
            SampleRoutes.RichText,
            "RichText + TextSpan + WidgetSpan",
            "styled runs + span recognizers + inline widget alignment",
            () => new RichTextDemoPage()),
        new(
            SampleRoutes.IntrinsicWidgets,
            "IntrinsicWidth + IntrinsicHeight",
            "step-snapped width + tallest-child stretch height",
            () => new IntrinsicWidgetsDemoPage()),
        new(
            SampleRoutes.LayoutBuilder,
            "LayoutBuilder + OrientationBuilder",
            "layout-time constraints + landscape/portrait composition",
            () => new LayoutBuilderDemoPage()),
        new(
            SampleRoutes.KeyboardListener,
            "Keyboard listeners + actions",
            "focused key events + Actions/Shortcuts intent dispatch",
            () => new KeyboardListenerDemoPage()),
        new(
            SampleRoutes.DebugPainting,
            "Placeholder + GridPaper",
            "unbounded fallback sizing + foreground layout grid",
            () => new DebugPaintingDemoPage()),
        new(
            SampleRoutes.Flow,
            "Flow + RepaintBoundary",
            "paint-time transforms + isolated child display lists",
            () => new FlowDemoPage()),
        new(
            SampleRoutes.CompositedTransform,
            "Composited transforms",
            "linked target/follower anchors + unlinked visibility",
            () => new CompositedTransformDemoPage()),
        new(
            SampleRoutes.Image,
            "Image controls",
            "streams + decoded handles + fade + image icons",
            () => new ImageDemoPage()),
        new(
            SampleRoutes.CustomMultiChildLayout,
            "CustomMultiChildLayout + NavigationToolbar",
            "delegate slots + dependent sizing + centered/start LTR/RTL toolbar",
            () => new CustomMultiChildLayoutDemoPage()),
        new(
            SampleRoutes.DismissibleSizeChangedLayout,
            "Dismissible + size notifications",
            "directional swipe thresholds + collapse + layout-change notifications",
            () => new DismissibleSizeChangedLayoutDemoPage()),
        new(
            SampleRoutes.StateStorage,
            "PageStorage + SharedAppData",
            "scroll restoration + keyed inherited-model rebuilds",
            () => new StateStorageDemoPage()),
        new(
            SampleRoutes.NavigationPop,
            "PopScope + NavigatorPopHandler",
            "route veto/results + nested navigator Back handling",
            () => new NavigationPopDemoPage()),
        new(
            SampleRoutes.AsyncBuilders,
            "FutureBuilder + StreamBuilder",
            "waiting/data/error/done snapshots + source replacement",
            () => new AsyncBuilderDemoPage()),
        new(
            SampleRoutes.StatefulBuilderLookupBoundary,
            "StatefulBuilder + LookupBoundary",
            "local state rebuilds + bounded ancestor lookup",
            () => new StatefulBuilderLookupBoundaryDemoPage()),
        new(
            SampleRoutes.DragTarget,
            "Draggable + DragTarget",
            "overlay feedback + accepted/rejected target lifecycle",
            () => new DragTargetDemoPage()),
        new(
            SampleRoutes.LifecycleUtilities,
            "Lifecycle listener controls",
            "AppLifecycleListener + StatusTransitionWidget + safe BuildContext",
            () => new LifecycleUtilitiesDemoPage()),
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
