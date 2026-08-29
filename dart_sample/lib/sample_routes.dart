import 'package:flutter/widgets.dart';

class SampleNavigationObservers {
  SampleNavigationObservers._();

  static final RouteObserver<PageRoute<dynamic>> pageRoutes =
      RouteObserver<PageRoute<dynamic>>();
}

class SampleRoutes {
  SampleRoutes._();

  static const String menu = '/';
  static const String counter = '/counter';
  static const String blocCounter = '/bloc-counter';
  static const String navigator = '/navigator';
  static const String hero = '/hero';
  static const String navigatorDetails = '/navigator/details';
  static const String navigatorPages = '/navigator-pages';
  static const String router = '/router';
  static const String listViewSeparated = '/list-separated';
  static const String listViewFixedExtent = '/list-fixed-extent';
  static const String scrollPhysics = '/scroll-physics';
  static const String ensureVisible = '/ensure-visible';
  static const String listViewReverse = '/list-reverse';
  static const String centerViewport = '/center-viewport';
  static const String pageView = '/page-view';
  static const String listWheelScrollView = '/list-wheel-scroll-view';
  static const String gridView = '/grid-view';
  static const String customSlivers = '/custom-slivers';
  static const String nestedScrollView = '/nested-scroll-view';
  static const String scrollbar = '/scrollbar';
  static const String draggableScrollableSheet = '/draggable-scrollable-sheet';
  static const String editableText = '/editable-text';
  static const String autofillGroup = '/autofill-group';
  static const String materialButtons = '/material-buttons';
  static const String animatedIcon = '/animated-icon';
  static const String tabs = '/tabs';
  static const String inkResponse = '/ink-response';
  static const String datePicker = '/date-picker';
  static const String actionButtons = '/action-buttons';
  static const String drawer = '/drawer';
  static const String scaffoldSlots = '/scaffold-slots';
  static const String drawerHeaders = '/drawer-headers';
  static const String divider = '/divider';
  static const String materialSwitch = '/material-switch';
  static const String badgeTooltip = '/badge-tooltip';
  static const String magnifier = '/magnifier';
  static const String selectionHandles = '/selection-handles';
  static const String circleAvatar = '/circle-avatar';
  static const String navigationSurfaces = '/navigation-surfaces';
  static const String navigationDrawer = '/navigation-drawer';
  static const String segmentedButtons = '/segmented-buttons';
  static const String banner = '/banner';
  static const String snackBar = '/snack-bar';
  static const String bottomSheet = '/bottom-sheet';
  static const String bottomNavigationBar = '/bottom-navigation-bar';
  static const String sliverAppBar = '/sliver-app-bar';
  static const String textField = '/text-field';
  static const String dialog = '/dialog';
  static const String popupMenu = '/popup-menu';
  static const String dropdown = '/dropdown';
  static const String search = '/search';
  static const String autocomplete = '/autocomplete';
  static const String selection = '/selection';
  static const String desktopTextSelectionToolbar =
      '/desktop-text-selection-toolbar';
  static const String chips = '/chips';
  static const String linearProgressIndicator = '/linear-progress-indicator';
  static const String circularProgressIndicator =
      '/circular-progress-indicator';
  static const String refreshIndicator = '/refresh-indicator';
  static const String barControls = '/bar-controls';
  static const String dataTable = '/data-table';
  static const String slider = '/slider';
  static const String rangeSlider = '/range-slider';
  static const String card = '/card';
  static const String carousel = '/carousel';
  static const String reorderableList = '/reorderable-list';
  static const String animatedList = '/animated-list';
  static const String animatedGrid = '/animated-grid';
  static const String gridTile = '/grid-tile';
  static const String listTile = '/list-tile';
  static const String listTileControls = '/list-tile-controls';
  static const String radioExpansionTile = '/radio-expansion-tile';
  static const String expansionPanel = '/expansion-panel';
  static const String stepper = '/stepper';
  static const String about = '/about';
  static const String floatingActionButton = '/floating-action-button';
  static const String checkbox = '/checkbox';
  static const String switchDemo = '/switch';
  static const String radio = '/radio';
  static const String appBarLeadingWidth = '/appbar-leading-width';
  static const String appBarActionsPadding = '/appbar-actions-padding';
  static const String appBarIconTheme = '/appbar-icon-theme';
  static const String appBarTextStyles = '/appbar-text-styles';
  static const String colorPalette = '/color-palette';
  static const String visualDensity = '/visual-density';
  static const String materialIcons = '/material-icons';
  static const String materialLocalizations = '/material-localizations';
  static const String cupertinoApp = '/cupertino-app';
  static const String cupertinoIcons = '/cupertino-icons';
  static const String cupertinoFocusHalo = '/cupertino-focus-halo';
  static const String cupertinoLocalizations = '/cupertino-localizations';
  static const String cupertinoTabBar = '/cupertino-tab-bar';
  static const String cupertinoListSection = '/cupertino-list-section';
  static const String cupertinoExpansionTile = '/cupertino-expansion-tile';
  static const String cupertinoTextSelectionControls =
      '/cupertino-text-selection-controls';
  static const String cupertinoTextField = '/cupertino-text-field';
  static const String cupertinoSearchTextField = '/cupertino-search-text-field';
  static const String cupertinoSegmentedControl =
      '/cupertino-segmented-control';
  static const String cupertinoContextMenu = '/cupertino-context-menu';
  static const String cupertinoMenuAnchor = '/cupertino-menu-anchor';
  static const String cupertinoTheme = '/cupertino-theme';
  static const String cupertinoPageScaffold = '/cupertino-page-scaffold';
  static const String cupertinoNavBar = '/cupertino-nav-bar';
  static const String cupertinoPicker = '/cupertino-picker';
  static const String cupertinoActivityIndicator =
      '/cupertino-activity-indicator';
  static const String cupertinoButton = '/cupertino-button';
  static const String cupertinoRadio = '/cupertino-radio';
  static const String cupertinoSlider = '/cupertino-slider';
  static const String cupertinoScrollbar = '/cupertino-scrollbar';
  static const String cupertinoRefresh = '/cupertino-refresh';
  static const String cupertinoRoute = '/cupertino-route';
  static const String cupertinoActionSheet = '/cupertino-action-sheet';
  static const String cupertinoMagnifier = '/cupertino-magnifier';
  static const String proxyWidgets = '/proxy-widgets';
  static const String align = '/align';
  static const String stack = '/stack';
  static const String decoratedBox = '/decorated-box';
  static const String gradients = '/gradients';
  static const String shapeBorders = '/shape-borders';
  static const String container = '/container';
  static const String aspectRatio = '/aspect-ratio';
  static const String flex = '/flex';
  static const String fractionallySizedBox = '/fractionally-sized-box';
  static const String fittedBox = '/fitted-box';
  static const String unconstrainedLimitedBox = '/unconstrained-limited-box';
  static const String overflowBox = '/overflow-box';
  static const String overflowIndicator = '/overflow-indicator';
  static const String offstage = '/offstage';
  static const String baseline = '/baseline';
  static const String richText = '/rich-text';
  static const String intrinsicWidgets = '/intrinsic-widgets';
  static const String layoutBuilder = '/layout-builder';
  static const String keyboardListener = '/keyboard-listener';

  static const String trackpadPanZoom = '/trackpad-pan-zoom';
  static const String debugPainting = '/debug-painting';
  static const String flow = '/flow';
  static const String transform = '/transform';
  static const String compositedTransform = '/composited-transform';
  static const String image = '/image';
  static const String customMultiChildLayout = '/custom-multi-child-layout';
  static const String dismissibleSizeChangedLayout =
      '/dismissible-size-changed-layout';
  static const String stateStorage = '/state-storage';
  static const String navigationPop = '/navigation-pop';
  static const String asyncBuilders = '/async-builders';
  static const String statefulBuilderLookupBoundary =
      '/stateful-builder-lookup-boundary';
  static const String dragTarget = '/drag-target';
  static const String gestureRecognizers = '/gesture-recognizers';
  static const String lifecycleUtilities = '/lifecycle-utilities';
}

class SampleRouteDefinition {
  const SampleRouteDefinition({
    required this.routeName,
    required this.title,
    required this.subtitle,
    required this.builder,
  });

  final String routeName;
  final String title;
  final String subtitle;
  final Widget Function() builder;
}

class RouteData {
  const RouteData(
    this.name, {
    this.queryParameters = const <String, String>{},
    this.arguments,
  });

  factory RouteData.fromLocation(String location, {Object? arguments}) {
    final uri = Uri.parse(location);
    final routeName = uri.path.isEmpty ? SampleRoutes.menu : uri.path;
    return RouteData(
      routeName,
      queryParameters: Map<String, String>.unmodifiable(uri.queryParameters),
      arguments: arguments,
    );
  }

  final String name;
  final Map<String, String> queryParameters;
  final Object? arguments;

  String get location {
    if (queryParameters.isEmpty) {
      return name;
    }

    return Uri(path: name, queryParameters: queryParameters).toString();
  }
}
