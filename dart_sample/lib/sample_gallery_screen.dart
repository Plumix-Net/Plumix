import 'package:material_ui/material_ui.dart';

import 'demos/general/align_demo_page.dart';
import 'demos/general/async_builder_demo_page.dart';
import 'demos/general/animated_list_demo_page.dart';
import 'demos/general/animated_grid_demo_page.dart';
import 'demos/material/app_bar_actions_padding_demo_page.dart';
import 'demos/material/animated_icon_demo_page.dart';
import 'demos/material/app_bar_icon_theme_demo_page.dart';
import 'demos/material/app_bar_leading_width_demo_page.dart';
import 'demos/material/app_bar_text_styles_demo_page.dart';
import 'demos/material/action_buttons_demo_page.dart';
import 'demos/material/badge_tooltip_demo_page.dart';
import 'demos/material/magnifier_demo_page.dart';
import 'demos/material/selection_handles_demo_page.dart';
import 'demos/material/banner_demo_page.dart';
import 'demos/material/bar_controls_demo_page.dart';
import 'demos/material/data_table_demo_page.dart';
import 'demos/material/date_picker_demo_page.dart';
import 'demos/material/snack_bar_demo_page.dart';
import 'demos/material/bottom_navigation_bar_demo_page.dart';
import 'demos/material/bottom_sheet_demo_page.dart';
import 'demos/material/sliver_app_bar_demo_page.dart';
import 'demos/material/text_field_demo_page.dart';
import 'demos/material/dialog_demo_page.dart';
import 'demos/material/popup_menu_demo_page.dart';
import 'demos/material/dropdown_demo_page.dart';
import 'demos/material/scaffold_slots_demo_page.dart';
import 'demos/material/search_demo_page.dart';
import 'demos/material/autocomplete_demo_page.dart';
import 'demos/material/selection_demo_page.dart';
import 'demos/material/desktop_text_selection_toolbar_demo_page.dart';
import 'demos/general/aspect_ratio_demo_page.dart';
import 'demos/general/bloc_counter_demo_page.dart';
import 'demos/material/card_demo_page.dart';
import 'demos/material/carousel_demo_page.dart';
import 'demos/material/reorderable_list_demo_page.dart';
import 'demos/material/circular_progress_indicator_demo_page.dart';
import 'demos/material/circle_avatar_demo_page.dart';
import 'demos/material/color_palette_demo_page.dart';
import 'demos/material/material_icons_demo_page.dart';
import 'demos/material/material_localizations_demo_page.dart';
import 'demos/cupertino/checkbox_demo_page.dart';
import 'demos/cupertino/app_demo_page.dart';
import 'demos/cupertino/cupertino_icons_demo_page.dart';
import 'demos/cupertino/cupertino_focus_halo_demo_page.dart';
import 'demos/cupertino/cupertino_localizations_demo_page.dart';
import 'demos/cupertino/cupertino_magnifier_demo_page.dart';
import 'demos/cupertino/cupertino_tab_bar_demo_page.dart';
import 'demos/cupertino/cupertino_list_section_demo_page.dart';
import 'demos/cupertino/cupertino_expansion_tile_demo_page.dart';
import 'demos/cupertino/cupertino_text_selection_controls_demo_page.dart';
import 'demos/cupertino/cupertino_text_field_demo_page.dart';
import 'demos/cupertino/cupertino_search_text_field_demo_page.dart';
import 'demos/cupertino/segmented_control_demo_page.dart';
import 'demos/cupertino/context_menu_demo_page.dart';
import 'demos/cupertino/cupertino_menu_anchor_demo_page.dart';
import 'demos/cupertino/cupertino_navigation_bar_demo_page.dart';
import 'demos/cupertino/page_scaffold_demo_page.dart';
import 'demos/cupertino/picker_demo_page.dart';
import 'demos/cupertino/cupertino_activity_indicator_demo_page.dart';
import 'demos/cupertino/cupertino_button_demo_page.dart';
import 'demos/cupertino/cupertino_radio_demo_page.dart';
import 'demos/cupertino/cupertino_scrollbar_demo_page.dart';
import 'demos/cupertino/cupertino_slider_demo_page.dart';
import 'demos/cupertino/refresh_demo_page.dart';
import 'counter_screen.dart';
import 'demos/general/container_demo_page.dart';
import 'demos/general/custom_multi_child_layout_demo_page.dart';
import 'demos/general/dismissible_size_changed_layout_demo_page.dart';
import 'demos/general/custom_slivers_demo_page.dart';
import 'demos/general/nested_scroll_view_demo_page.dart';
import 'demos/general/decorated_box_demo_page.dart';
import 'demos/general/gradients_demo_page.dart';
import 'demos/general/shape_borders_demo_page.dart';
import 'demos/general/debug_painting_demo_page.dart';
import 'demos/general/drag_target_demo_page.dart';
import 'demos/material/drawer_demo_page.dart';
import 'demos/material/drawer_headers_demo_page.dart';
import 'demos/material/divider_demo_page.dart';
import 'demos/material/material_switch_demo_page.dart';
import 'demos/general/autofill_group_demo_page.dart';
import 'demos/general/editable_text_demo_page.dart';
import 'demos/general/fitted_box_demo_page.dart';
import 'demos/general/flow_demo_page.dart';
import 'demos/general/transform_demo_page.dart';
import 'demos/general/composited_transform_demo_page.dart';
import 'demos/general/image_demo_page.dart';
import 'demos/material/floating_action_button_demo_page.dart';
import 'demos/material/ink_response_demo_page.dart';
import 'demos/general/fractionally_sized_box_demo_page.dart';
import 'demos/general/grid_view_demo_page.dart';
import 'demos/material/grid_tile_demo_page.dart';
import 'demos/material/linear_progress_indicator_demo_page.dart';
import 'demos/general/list_view_fixed_extent_demo_page.dart';
import 'demos/general/list_wheel_scroll_view_demo_page.dart';
import 'demos/general/page_view_demo_page.dart';
import 'demos/general/ensure_visible_demo_page.dart';
import 'demos/general/center_viewport_demo_page.dart';
import 'demos/general/list_view_reverse_demo_page.dart';
import 'demos/general/scroll_physics_demo_page.dart';
import 'demos/general/list_view_separated_demo_page.dart';
import 'demos/material/list_tile_demo_page.dart';
import 'demos/material/list_tile_controls_demo_page.dart';
import 'demos/material/radio_expansion_tile_demo_page.dart';
import 'demos/material/expansion_panel_demo_page.dart';
import 'demos/material/stepper_demo_page.dart';
import 'demos/material/about_demo_page.dart';
import 'demos/material/material_buttons_demo_page.dart';
import 'demos/material/tabs_demo_page.dart';
import 'demos/material/navigation_surfaces_demo_page.dart';
import 'demos/material/navigation_drawer_demo_page.dart';
import 'demos/material/segmented_buttons_demo_page.dart';
import 'demos/material/chips_demo_page.dart';
import 'demos/general/hero_demo_page.dart';
import 'demos/general/navigator_demo_page.dart';
import 'demos/general/navigator_pages_demo_page.dart';
import 'demos/general/router_demo_page.dart';
import 'demos/general/offstage_demo_page.dart';
import 'demos/general/baseline_demo_page.dart';
import 'demos/general/rich_text_demo_page.dart';
import 'demos/general/intrinsic_widgets_demo_page.dart';
import 'demos/general/layout_builder_demo_page.dart';
import 'demos/general/keyboard_listener_demo_page.dart';
import 'demos/general/lifecycle_utilities_demo_page.dart';
import 'demos/general/overflow_box_demo_page.dart';
import 'demos/general/overflow_indicator_demo_page.dart';
import 'demos/general/proxy_widgets_demo_page.dart';
import 'demos/cupertino/radio_demo_page.dart';
import 'sample_routes.dart';
import 'demos/general/draggable_scrollable_sheet_demo_page.dart';
import 'demos/general/scrollbar_demo_page.dart';
import 'demos/general/stack_demo_page.dart';
import 'demos/general/state_storage_demo_page.dart';
import 'demos/general/stateful_builder_lookup_boundary_demo_page.dart';
import 'demos/general/navigation_pop_demo_page.dart';
import 'demos/cupertino/action_sheet_demo_page.dart';
import 'demos/cupertino/route_demo_page.dart';
import 'demos/cupertino/switch_demo_page.dart';
import 'demos/cupertino/theme_demo_page.dart';
import 'demos/material/range_slider_demo_page.dart';
import 'demos/material/refresh_indicator_demo_page.dart';
import 'demos/material/slider_demo_page.dart';
import 'demos/general/unconstrained_limited_box_demo_page.dart';

class SampleGalleryScreen extends StatelessWidget {
  const SampleGalleryScreen({super.key});

  static final List<SampleRouteDefinition>
  _materialDemoPages = <SampleRouteDefinition>[
    SampleRouteDefinition(
      routeName: SampleRoutes.animatedIcon,
      title: 'AnimatedIcon + AnimatedIcons',
      subtitle: 'complete generated vector catalog + progress/RTL/theme probes',
      builder: () => const AnimatedIconDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.actionButtons,
      title: 'Material action buttons',
      subtitle: 'back/close/drawer/end-drawer + ActionIconTheme',
      builder: () => const ActionButtonsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.materialButtons,
      title: 'Material buttons',
      subtitle: 'TextButton + ElevatedButton + OutlinedButton + FilledButton',
      builder: () => const MaterialButtonsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.tabs,
      title: 'TabBar + TabBarView',
      subtitle: 'controller + indicator + scrollable tabs + swipe pages',
      builder: () => const TabsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.inkResponse,
      title: 'InkResponse + InkWell',
      subtitle: 'circle/rectangle ink + gestures + overlay states',
      builder: () => const InkResponseDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.datePicker,
      title: 'Date/time picker family',
      subtitle: 'day/year/time/range selection + input modes + M2/M3 themes',
      builder: () => const DatePickerDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.drawer,
      title: 'Drawer',
      subtitle: 'scaffold drawer/endDrawer + theme/widget precedence probes',
      builder: () => const DrawerDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.scaffoldSlots,
      title: 'Scaffold slots',
      subtitle: 'persistent footer + extendBody padding + drawer paint order',
      builder: () => const ScaffoldSlotsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.drawerHeaders,
      title: 'Drawer headers',
      subtitle: 'DrawerHeader + UserAccountsDrawerHeader',
      builder: () => const DrawerHeadersDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.divider,
      title: 'Divider',
      subtitle: 'horizontal/vertical divider + theme/widget precedence probes',
      builder: () => const DividerDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.materialSwitch,
      title: 'Switch',
      subtitle: 'M2/M3 tokens + thumb icons + track outline + Switch.adaptive',
      builder: () => const MaterialSwitchDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.badgeTooltip,
      title: 'Badge + Tooltip',
      subtitle: 'count/small badges + hover/long-press tooltip theming',
      builder: () => const BadgeTooltipDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.magnifier,
      title: 'RawMagnifier + Magnifier',
      subtitle: 'backdrop zoom + focal offsets + Material lens styling',
      builder: () => const MagnifierDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.selectionHandles,
      title: 'Selection handles',
      subtitle: 'SelectionOverlay handles + drag endpoints + collapsed handle',
      builder: () => const SelectionHandlesDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.circleAvatar,
      title: 'CircleAvatar',
      subtitle: 'initials + image layers + animated radius + fallback',
      builder: () => const CircleAvatarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.navigationSurfaces,
      title: 'NavigationBar + NavigationRail',
      subtitle: 'horizontal/vertical Material navigation + labels/themes',
      builder: () => const NavigationSurfacesDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.navigationDrawer,
      title: 'NavigationDrawer',
      subtitle: 'destinations + custom children + selection/theme probes',
      builder: () => const NavigationDrawerDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.segmentedButtons,
      title: 'ToggleButtons + SegmentedButton',
      subtitle: 'legacy/M3 segmented selection + themes/states',
      builder: () => const SegmentedButtonsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.banner,
      title: 'Banner + MaterialBanner',
      subtitle: 'diagonal ribbon + persistent actions/theme/overflow probes',
      builder: () => const BannerDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.snackBar,
      title: 'SnackBar + SnackBarAction',
      subtitle: 'messenger queue + action/close/overflow probes',
      builder: () => const SnackBarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.bottomSheet,
      title: 'BottomSheet + ModalBottomSheet',
      subtitle: 'persistent controller + modal route/drag/theme probes',
      builder: () => const BottomSheetDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.bottomNavigationBar,
      title: 'BottomNavigationBar',
      subtitle: 'fixed/shifting + landscape layouts + label/color/theme probes',
      builder: () => const BottomNavigationBarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.sliverAppBar,
      title: 'SliverAppBar + FlexibleSpaceBar',
      subtitle: 'collapse/parallax + pinned/floating/snap + M3 variants',
      builder: () => const SliverAppBarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.textField,
      title: 'InputDecorator + TextField',
      subtitle: 'labels/borders/supporting text + editable states',
      builder: () => const TextFieldDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.dialog,
      title: 'Dialog family',
      subtitle: 'modal route + alert/simple/result/scrollable/theme probes',
      builder: () => const DialogDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.popupMenu,
      title: 'PopupMenuButton + PopupMenuItem',
      subtitle: 'anchor + selection/cancel/keyboard/theme probes',
      builder: () => const PopupMenuDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.dropdown,
      title: 'Dropdown controls',
      subtitle: 'button/menu + form/filter/search/keyboard/route probes',
      builder: () => const DropdownDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.search,
      title: 'SearchBar + SearchAnchor',
      subtitle: 'controller-backed search view + suggestions + theme probes',
      builder: () => const SearchDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.autocomplete,
      title: 'Autocomplete + RawAutocomplete',
      subtitle:
          'Material defaults + custom options + keyboard/open-direction probes',
      builder: () => const AutocompleteDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.selection,
      title: 'SelectableText + SelectionArea',
      subtitle: 'single/subtree selection + keyboard/copy/theme probes',
      builder: () => const SelectionDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.desktopTextSelectionToolbar,
      title: 'Text selection toolbars',
      subtitle: 'Android overflow paging + anchored desktop action styling',
      builder: () => const DesktopTextSelectionToolbarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.chips,
      title: 'Material chips',
      subtitle: 'action/choice/filter/input + selection/deletion/theme probes',
      builder: () => const ChipsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.linearProgressIndicator,
      title: 'LinearProgressIndicator',
      subtitle: 'determinate/indeterminate + M2/M3 + theme/widget/RTL probes',
      builder: () => const LinearProgressIndicatorDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.circularProgressIndicator,
      title: 'CircularProgressIndicator',
      subtitle: 'determinate/indeterminate + M2/M3 + theme/widget probes',
      builder: () => const CircularProgressIndicatorDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.refreshIndicator,
      title: 'RefreshIndicator + RefreshProgressIndicator',
      subtitle: 'pull-to-refresh lifecycle + adaptive/no-spinner/theme probes',
      builder: () => const RefreshIndicatorDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.barControls,
      title: 'BottomAppBar + ButtonBar',
      subtitle: 'FAB notch + M2/M3 surface + legacy action overflow',
      builder: () => const BarControlsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.dataTable,
      title: 'DataTable + PaginatedDataTable',
      subtitle: 'sorting + selection + themes + source-backed paging',
      builder: () => const DataTableDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.slider,
      title: 'Slider',
      subtitle: 'continuous/discrete + drag/tap/keyboard + theme/widget colors',
      builder: () => const SliderDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.rangeSlider,
      title: 'RangeSlider',
      subtitle: 'two-thumb range + continuous/discrete + theme/widget colors',
      builder: () => const RangeSliderDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.card,
      title: 'Card',
      subtitle: 'elevated/filled/outlined variants + theme/clip probes',
      builder: () => const CardDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.carousel,
      title: 'CarouselView',
      subtitle: 'fixed/weighted item extents + snapping + theme',
      builder: () => const CarouselDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.reorderableList,
      title: 'ReorderableListView',
      subtitle: 'desktop handles + custom handles + keyed reorder callbacks',
      builder: () => const ReorderableListDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.gridTile,
      title: 'GridTile + GridTileBar',
      subtitle: 'header/footer overlays + one/two-line bars + RTL',
      builder: () => const GridTileDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.listTile,
      title: 'ListTile',
      subtitle: 'leading/title/subtitle/trailing + selected/dense/theme probes',
      builder: () => const ListTileDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.listTileControls,
      title: 'CheckboxListTile + SwitchListTile',
      subtitle: 'whole-row toggle + tristate + affinity + adaptive probes',
      builder: () => const ListTileControlsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.radioExpansionTile,
      title: 'RadioListTile + ExpansionTile',
      subtitle:
          'RadioGroup + toggleable/adaptive + animated controller expansion',
      builder: () => const RadioExpansionTileDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.expansionPanel,
      title: 'ExpansionPanel + ExpansionPanelList',
      subtitle: 'controlled panels + radio accordion + animated material gaps',
      builder: () => const ExpansionPanelDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.stepper,
      title: 'ExpandIcon + Stepper',
      subtitle: 'disclosure animation + vertical/horizontal step progress',
      builder: () => const StepperDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.about,
      title: 'AboutDialog + LicensePage',
      subtitle: 'metadata dialog + registry/package license navigation',
      builder: () => const AboutDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.floatingActionButton,
      title: 'FloatingActionButton',
      subtitle: 'regular/small/large/extended + theme defaults',
      builder: () => const FloatingActionButtonDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.appBarLeadingWidth,
      title: 'AppBar leadingWidth theme',
      subtitle: 'theme fallback + widget override runtime probe',
      builder: () => const AppBarLeadingWidthDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.appBarActionsPadding,
      title: 'AppBar actionsPadding theme',
      subtitle: 'theme fallback + widget override runtime probe',
      builder: () => const AppBarActionsPaddingDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.appBarIconTheme,
      title: 'AppBar icon themes',
      subtitle: 'iconTheme/actionsIconTheme precedence runtime probe',
      builder: () => const AppBarIconThemeDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.appBarTextStyles,
      title: 'AppBar text styles',
      subtitle: 'title/toolbar text style precedence runtime probe',
      builder: () => const AppBarTextStylesDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.materialIcons,
      title: 'Icons',
      subtitle:
          'full catalog: base/outlined/rounded/sharp variants + adaptive + '
          'directional glyphs',
      builder: () => const MaterialIconsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.colorPalette,
      title: 'Colors + primarySwatch',
      subtitle:
          'MaterialColor shades + fromSwatch M2 scheme + swatch-derived theme colors',
      builder: () => const ColorPaletteDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.materialLocalizations,
      title: 'Localizations',
      subtitle:
          'global delegates: translated strings + locale date/time/number formats',
      builder: () => const MaterialLocalizationsDemoPage(),
    ),
  ];

  static final List<SampleRouteDefinition>
  _cupertinoDemoPages = <SampleRouteDefinition>[
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoApp,
      title: 'Application shell',
      subtitle:
          'theme + localization + selection + scroll + '
          'CupertinoPageRoute defaults',
      builder: () => const CupertinoAppDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoIcons,
      title: 'Icons',
      subtitle: 'legacy + SF Symbols catalog, aliases, and directional glyphs',
      builder: () => const CupertinoIconsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoFocusHalo,
      title: 'Focus halo',
      subtitle:
          'rectangular, rounded-rectangle, and rounded-superellipse '
          'descendant focus outlines',
      builder: () => const CupertinoFocusHaloDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoLocalizations,
      title: 'Localizations',
      subtitle:
          'global delegates: translated strings + locale date formats + '
          'text direction',
      builder: () => const CupertinoLocalizationsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoTabBar,
      title: 'Tab scaffold + bar',
      subtitle:
          'lazy tab bodies + retained state + active icons + safe-area blur',
      builder: () => const CupertinoTabBarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoListSection,
      title: 'List + form sections',
      subtitle: 'edge-to-edge/inset groups + split rows + helper/error content',
      builder: () => const CupertinoListSectionDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoExpansionTile,
      title: 'List + expansion tiles',
      subtitle: 'base/notched rows + async activation + fade/scroll expansion',
      builder: () => const CupertinoExpansionTileDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoTextSelectionControls,
      title: 'Text selection controls',
      subtitle: 'line-height handles + macOS handle-free defaults',
      builder: () => const CupertinoTextSelectionControlsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoTextField,
      title: 'Text field',
      subtitle:
          'rounded/borderless + validated form row + clear and disabled states',
      builder: () => const CupertinoTextFieldDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoSearchTextField,
      title: 'Search text field',
      subtitle:
          'localized placeholder + clear action + custom icons + disabled state',
      builder: () => const CupertinoSearchTextFieldDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoSegmentedControl,
      title: 'Segmented control',
      subtitle:
          'controlled selection + press animation + disabled/custom states',
      builder: () => const CupertinoSegmentedControlDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoContextMenu,
      title: 'Context menu',
      subtitle: 'hold preview + action sheet + drag/fling dismissal',
      builder: () => const CupertinoContextMenuDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoMenuAnchor,
      title: 'Menu anchor',
      subtitle:
          'anchored overlay + leading/subtitle/trailing + swipe and '
          'long-press opening',
      builder: () => const CupertinoMenuAnchorDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.checkbox,
      title: 'Checkbox',
      subtitle: 'bool/bool? values + tristate + tap-target policy',
      builder: () => const CheckboxDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.switchDemo,
      title: 'Switch',
      subtitle: 'on/off value + track/thumb theming + drag',
      builder: () => const SwitchDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.radio,
      title: 'Radio',
      subtitle: 'group selection + toggleable + tap-target policy',
      builder: () => const RadioDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoRadio,
      title: 'Radio (Cupertino)',
      subtitle:
          'RadioGroup selection + toggleable + checkmark style + '
          'dark-mode painting',
      builder: () => const CupertinoRadioDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoSlider,
      title: 'Slider (Cupertino)',
      subtitle:
          'continuous/discrete values + min-max ranges + '
          'active/thumb colors + LTR/RTL drag',
      builder: () => const CupertinoSliderDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoScrollbar,
      title: 'Scrollbar (Cupertino)',
      subtitle:
          'fading thumb + press-and-hold resize + left rail orientation + '
          'dynamic thumb color',
      builder: () => const CupertinoScrollbarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoTheme,
      title: 'Theme + dynamic colors',
      subtitle:
          'brightness/contrast/elevation resolution + '
          'CupertinoTextThemeData styles',
      builder: () => const CupertinoThemeDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoPageScaffold,
      title: 'Page scaffold',
      subtitle: 'opaque/translucent bars + keyboard inset consumption',
      builder: () => const CupertinoPageScaffoldDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoNavBar,
      title: 'Navigation bars',
      subtitle:
          'large-title sliver + search + auto back labels + hero transitions',
      builder: () => const CupertinoNavigationBarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoPicker,
      title: 'Picker family',
      subtitle: 'wheel + bounded date/time + duration columns and overlays',
      builder: () => const CupertinoPickerDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoActivityIndicator,
      title: 'Activity indicators',
      subtitle: 'spinning + partially revealed ticks + linear progress bar',
      builder: () => const CupertinoActivityIndicatorDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoButton,
      title: 'Buttons',
      subtitle: 'plain/tinted/filled styles + size styles + long press',
      builder: () => const CupertinoButtonDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoRefresh,
      title: 'Sliver refresh',
      subtitle: 'pull threshold + held refresh extent + native progress states',
      builder: () => const CupertinoRefreshDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoRoute,
      title: 'Routes + tab view',
      subtitle: 'page transitions + independent tab history + popup',
      builder: () => const CupertinoRouteDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoActionSheet,
      title: 'Action sheet',
      subtitle:
          'title/message + hairline actions + detached cancel + slide-to-select',
      builder: () => const CupertinoActionSheetDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.cupertinoMagnifier,
      title: 'Magnifier',
      subtitle:
          'elliptical rim + theme border color + drag-following text magnifier',
      builder: () => const CupertinoMagnifierDemoPage(),
    ),
  ];

  static final List<SampleRouteDefinition>
  _generalDemoPages = <SampleRouteDefinition>[
    SampleRouteDefinition(
      routeName: SampleRoutes.counter,
      title: 'Counter',
      subtitle: 'existing sample',
      builder: () => const CounterScreen(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.blocCounter,
      title: 'Bloc counter',
      subtitle: 'BlocProvider + BlocBuilder + BlocListener + BlocSelector',
      builder: () => const BlocCounterDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.navigator,
      title: 'Navigator',
      subtitle: 'named routes + RouteData + stack APIs',
      builder: () => const NavigatorDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.hero,
      title: 'Hero',
      subtitle: 'shared-element flight + shuttle/placeholder builders + HeroMode',
      builder: () => const HeroDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.navigatorPages,
      title: 'Navigator.pages',
      subtitle: 'declarative pages + transition delegate',
      builder: () => const NavigatorPagesDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.router,
      title: 'Router',
      subtitle: 'delegate + parser + provider + back-button dispatcher',
      builder: () => const RouterDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.listViewSeparated,
      title: 'ListView.Separated',
      subtitle: 'item + separator builder',
      builder: () => const ListViewSeparatedDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.listViewFixedExtent,
      title: 'ListView fixed extent',
      subtitle: 'itemExtent + padding',
      builder: () => const ListViewFixedExtentDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.listViewReverse,
      title: 'ListView reverse',
      subtitle: 'reverse=true behavior',
      builder: () => const ListViewReverseDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.centerViewport,
      title: 'CustomScrollView center',
      subtitle: 'center key + negative scroll offsets',
      builder: () => const CenterViewportDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.ensureVisible,
      title: 'Ensure visible',
      subtitle: 'nested reveal + alignment policies',
      builder: () => const EnsureVisibleDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.pageView,
      title: 'PageView.builder',
      subtitle: 'lazy pages + viewportFraction',
      builder: () => const PageViewDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.listWheelScrollView,
      title: 'ListWheelScrollView',
      subtitle: 'cylindrical wheel + FixedExtentScrollController',
      builder: () => const ListWheelScrollViewDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.scrollPhysics,
      title: 'Scroll physics',
      subtitle: 'bouncing overscroll + spring back vs clamping',
      builder: () => const ScrollPhysicsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.animatedList,
      title: 'AnimatedList + SliverAnimatedList',
      subtitle: 'insert/remove animations + separated and sliver variants',
      builder: () => const AnimatedListDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.animatedGrid,
      title: 'AnimatedGrid + SliverAnimatedGrid',
      subtitle:
          'insert/remove animations + grid delegate and keyed sliver variants',
      builder: () => const AnimatedGridDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.gridView,
      title: 'GridView + SliverGrid',
      subtitle: 'delegate-based 2D layout',
      builder: () => const GridViewDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.customSlivers,
      title: 'Custom slivers',
      subtitle: 'resizing/floating/fill/group/prototype/varied sliver adapters',
      builder: () => const CustomSliversDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.nestedScrollView,
      title: 'NestedScrollView',
      subtitle: 'header/body coordination + overlap absorber and injector',
      builder: () => const NestedScrollViewDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.scrollbar,
      title: 'Scrollbar',
      subtitle: 'controller + thumb',
      builder: () => const ScrollbarDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.draggableScrollableSheet,
      title: 'DraggableScrollableSheet',
      subtitle: 'drag-to-resize + snap + controller + actuator reset',
      builder: () => const DraggableScrollableSheetDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.editableText,
      title: 'EditableText',
      subtitle: 'focus + IME + multiline caret',
      builder: () => const EditableTextDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.autofillGroup,
      title: 'AutofillGroup',
      subtitle: 'autofill scope + hints + finishAutofillContext',
      builder: () => const AutofillGroupDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.proxyWidgets,
      title: 'Proxy widgets',
      subtitle: 'Opacity + transforms + clips + ColoredBox edge modes',
      builder: () => const ProxyWidgetsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.align,
      title: 'Animations + transitions',
      subtitle:
          'implicit motion + explicit scale/rotation controllers + sliver fade',
      builder: () => const AlignDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.stack,
      title: 'Stack + Positioned',
      subtitle: 'multi-child overlay layout',
      builder: () => const StackDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.decoratedBox,
      title: 'DecoratedBox',
      subtitle: 'border + radius + fill decoration',
      builder: () => const DecoratedBoxDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.gradients,
      title: 'Gradients',
      subtitle: 'linear/radial/sweep gradients + shadow lerp',
      builder: () => const GradientsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.shapeBorders,
      title: 'ShapeBorders',
      subtitle: 'outlined border shapes + lerp',
      builder: () => const ShapeBordersDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.container,
      title: 'Container',
      subtitle: 'alignment + margin + constraints + transform',
      builder: () => const ContainerDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.aspectRatio,
      title: 'AspectRatio + Spacer',
      subtitle: 'tight ratio layout + flex gap',
      builder: () => const AspectRatioDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.fractionallySizedBox,
      title: 'FractionallySizedBox',
      subtitle: 'fractional constraints + alignment',
      builder: () => const FractionallySizedBoxDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.fittedBox,
      title: 'FittedBox',
      subtitle: 'box-fit scaling + alignment',
      builder: () => const FittedBoxDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.unconstrainedLimitedBox,
      title: 'ConstraintsTransformBox + UnconstrainedBox',
      subtitle: 'arbitrary/axis constraint transforms + overflow clipping',
      builder: () => const UnconstrainedLimitedBoxDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.overflowBox,
      title: 'OverflowBox + SizedOverflowBox',
      subtitle: 'constraint override + fixed-size overflow',
      builder: () => const OverflowBoxDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.overflowIndicator,
      title: 'Overflow indicator',
      subtitle: 'RenderFlex debug stripes + overflow label',
      builder: () => const OverflowIndicatorDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.offstage,
      title: 'Visibility + SliverVisibility + Offstage',
      subtitle: 'replacement + maintained size + sliver and offstage behavior',
      builder: () => const OffstageDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.baseline,
      title: 'Baseline + IgnoreBaseline',
      subtitle: 'real text baselines + bottom fallback + Row exclusion',
      builder: () => const BaselineDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.richText,
      title: 'RichText + TextSpan + WidgetSpan',
      subtitle: 'styled runs + span recognizers + inline widget alignment',
      builder: () => const RichTextDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.intrinsicWidgets,
      title: 'IntrinsicWidth + IntrinsicHeight',
      subtitle: 'step-snapped width + tallest-child stretch height',
      builder: () => const IntrinsicWidgetsDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.layoutBuilder,
      title: 'LayoutBuilder + OrientationBuilder',
      subtitle: 'layout-time constraints + landscape/portrait composition',
      builder: () => const LayoutBuilderDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.keyboardListener,
      title: 'Keyboard listeners + actions',
      subtitle: 'focused key events + Actions/Shortcuts intent dispatch',
      builder: () => const KeyboardListenerDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.debugPainting,
      title: 'Placeholder + GridPaper',
      subtitle: 'unbounded fallback sizing + foreground layout grid',
      builder: () => const DebugPaintingDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.flow,
      title: 'Flow + RepaintBoundary',
      subtitle: 'paint-time transforms + isolated child display lists',
      builder: () => const FlowDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.transform,
      title: 'Transform + Matrix4',
      subtitle: 'rotate/scale/flip/translate + perspective 3D rotation',
      builder: () => const TransformDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.compositedTransform,
      title: 'Composited transforms',
      subtitle: 'linked target/follower anchors + unlinked visibility',
      builder: () => const CompositedTransformDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.image,
      title: 'Image controls',
      subtitle: 'streams + decoded handles + fade + image icons',
      builder: () => const ImageDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.customMultiChildLayout,
      title: 'CustomMultiChildLayout + NavigationToolbar',
      subtitle:
          'delegate slots + dependent sizing + centered/start LTR/RTL toolbar',
      builder: () => const CustomMultiChildLayoutDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.dismissibleSizeChangedLayout,
      title: 'Dismissible + size notifications',
      subtitle:
          'directional swipe thresholds + collapse + layout-change notifications',
      builder: () => const DismissibleSizeChangedLayoutDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.stateStorage,
      title: 'PageStorage + SharedAppData',
      subtitle: 'scroll restoration + keyed inherited-model rebuilds',
      builder: () => const StateStorageDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.navigationPop,
      title: 'PopScope + NavigatorPopHandler',
      subtitle: 'route veto/results + nested navigator Back handling',
      builder: () => const NavigationPopDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.asyncBuilders,
      title: 'FutureBuilder + StreamBuilder',
      subtitle: 'waiting/data/error/done snapshots + source replacement',
      builder: () => const AsyncBuilderDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.statefulBuilderLookupBoundary,
      title: 'StatefulBuilder + LookupBoundary',
      subtitle: 'local state rebuilds + bounded ancestor lookup',
      builder: () => const StatefulBuilderLookupBoundaryDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.dragTarget,
      title: 'Draggable + DragTarget',
      subtitle: 'overlay feedback + accepted/rejected target lifecycle',
      builder: () => const DragTargetDemoPage(),
    ),
    SampleRouteDefinition(
      routeName: SampleRoutes.lifecycleUtilities,
      title: 'Lifecycle listener controls',
      subtitle:
          'AppLifecycleListener + StatusTransitionWidget + safe BuildContext',
      builder: () => const LifecycleUtilitiesDemoPage(),
    ),
  ];

  static final List<SampleMenuTabDefinition> _demoTabs =
      <SampleMenuTabDefinition>[
        SampleMenuTabDefinition(
          label: 'Material',
          description: 'Material controls and theming demos.',
          icon: Icons.star_outline,
          activeIcon: Icons.star,
          pages: _materialDemoPages,
        ),
        SampleMenuTabDefinition(
          label: 'Cupertino',
          description: 'Adaptive Cupertino behavior probes for controls.',
          icon: Icons.check,
          activeIcon: Icons.check,
          pages: _cupertinoDemoPages,
        ),
        SampleMenuTabDefinition(
          label: 'General',
          description:
              'Core widgets, layouts, navigation, and rendering demos.',
          icon: Icons.menu,
          activeIcon: Icons.info_outline,
          pages: _generalDemoPages,
        ),
      ];

  static final Map<String, SampleRouteDefinition> _demoPageByRoute =
      <String, SampleRouteDefinition>{
        for (final SampleMenuTabDefinition tab in _demoTabs)
          for (final SampleRouteDefinition page in tab.pages)
            page.routeName: page,
      };

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Navigator(
        onGenerateRoute: _buildRoute,
        observers: <NavigatorObserver>[SampleNavigationObservers.pageRoutes],
        initialRoute: SampleRoutes.menu,
      ),
    );
  }

  static Route<dynamic> _buildRoute(RouteSettings settings) {
    final RouteData routeData = _routeDataFromSettings(settings);

    if (routeData.name == SampleRoutes.menu) {
      return MaterialPageRoute<void>(
        builder: (BuildContext context) => SampleMenuPage(tabs: _demoTabs),
        settings: settings,
      );
    }

    if (routeData.name == SampleRoutes.navigatorDetails) {
      return MaterialPageRoute<void>(
        builder: (BuildContext context) => SampleDemoPage(
          title: 'Navigator details',
          subtitle: 'RouteData query/arguments + push/pop operations',
          child: NavigatorDetailsPage(routeData: routeData),
        ),
        settings: settings,
      );
    }

    final SampleRouteDefinition? page = _demoPageByRoute[routeData.name];
    if (page != null) {
      return MaterialPageRoute<void>(
        builder: (BuildContext context) =>
            SampleDemoPage.fromDefinition(page: page, child: page.builder()),
        settings: settings,
      );
    }

    return MaterialPageRoute<void>(
      builder: (BuildContext context) =>
          SampleUnknownRoutePage(routeName: settings.name ?? '(null)'),
      settings: settings,
    );
  }

  static RouteData _routeDataFromSettings(RouteSettings settings) {
    if (settings.arguments is RouteData) {
      final RouteData routeData = settings.arguments! as RouteData;
      return RouteData.fromLocation(
        settings.name ?? routeData.location,
        arguments: routeData.arguments,
      );
    }

    return RouteData.fromLocation(
      settings.name ?? SampleRoutes.menu,
      arguments: settings.arguments,
    );
  }
}

class SampleMenuPage extends StatefulWidget {
  const SampleMenuPage({required this.tabs, super.key});

  final List<SampleMenuTabDefinition> tabs;

  @override
  State<SampleMenuPage> createState() => _SampleMenuPageState();
}

class _SampleMenuPageState extends State<SampleMenuPage> {
  int _selectedTabIndex = 0;

  @override
  Widget build(BuildContext context) {
    final SampleMenuTabDefinition selectedTab = widget.tabs[_selectedTabIndex];
    final List<SampleRouteDefinition> pages = selectedTab.pages;

    return Scaffold(
      appBar: AppBar(title: const Text('Flutter.Net widget pages')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 10,
          children: <Widget>[
            const Text(
              'Route-based sample menu. Open page and return via Back button or Esc.',
              style: TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Text(
              selectedTab.description,
              style: const TextStyle(fontSize: 12, color: Color(0x73000000)),
            ),
            Expanded(
              child: ListView.builder(
                itemCount: pages.length,
                itemExtent: 56,
                padding: const EdgeInsets.fromLTRB(0, 8, 0, 8),
                itemBuilder: (BuildContext itemContext, int index) {
                  return _buildPageButton(context, pages[index]);
                },
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedTabIndex,
        onTap: (int index) {
          if (index == _selectedTabIndex) {
            return;
          }

          setState(() => _selectedTabIndex = index);
        },
        items: <BottomNavigationBarItem>[
          for (final SampleMenuTabDefinition tab in widget.tabs)
            BottomNavigationBarItem(
              icon: Icon(tab.icon),
              activeIcon: Icon(tab.activeIcon),
              label: tab.label,
            ),
        ],
      ),
    );
  }

  static Widget _buildPageButton(
    BuildContext context,
    SampleRouteDefinition page,
  ) {
    return OutlinedButton(
      onPressed: () => Navigator.of(context).pushNamed(page.routeName),
      style: OutlinedButton.styleFrom(
        backgroundColor: const Color(0xFFDCE3ED),
        foregroundColor: Colors.black,
        side: const BorderSide(color: Color(0xFFB8C4D4), width: 1),
        minimumSize: const Size(0, 44),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
      child: Text(
        '${page.title}  |  ${page.subtitle}',
        style: const TextStyle(fontSize: 12),
      ),
    );
  }
}

class SampleMenuTabDefinition {
  const SampleMenuTabDefinition({
    required this.label,
    required this.description,
    required this.icon,
    required this.activeIcon,
    required this.pages,
  });

  final String label;
  final String description;
  final IconData icon;
  final IconData activeIcon;
  final List<SampleRouteDefinition> pages;
}

class SampleDemoPage extends StatelessWidget {
  const SampleDemoPage({
    required this.title,
    required this.subtitle,
    required this.child,
    super.key,
  });

  SampleDemoPage.fromDefinition({
    required SampleRouteDefinition page,
    required this.child,
    super.key,
  }) : title = page.title,
       subtitle = page.subtitle;

  final String title;
  final String subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 10,
          children: <Widget>[
            Text(
              subtitle,
              style: const TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Expanded(
              child: Container(
                color: const Color(0xFFF7F9FC),
                padding: const EdgeInsets.all(12),
                child: child,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class SampleUnknownRoutePage extends StatelessWidget {
  const SampleUnknownRoutePage({required this.routeName, super.key});

  final String routeName;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      alignment: Alignment.center,
      child: Text(
        'Unknown route: $routeName',
        style: const TextStyle(fontSize: 16, color: Colors.black),
      ),
    );
  }
}
