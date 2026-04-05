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
  static const String navigator = '/navigator';
  static const String navigatorDetails = '/navigator/details';
  static const String listViewSeparated = '/list-separated';
  static const String listViewFixedExtent = '/list-fixed-extent';
  static const String listViewReverse = '/list-reverse';
  static const String gridView = '/grid-view';
  static const String customSlivers = '/custom-slivers';
  static const String scrollbar = '/scrollbar';
  static const String editableText = '/editable-text';
  static const String materialButtons = '/material-buttons';
  static const String checkbox = '/checkbox';
  static const String appBarLeadingWidth = '/appbar-leading-width';
  static const String appBarActionsPadding = '/appbar-actions-padding';
  static const String appBarIconTheme = '/appbar-icon-theme';
  static const String appBarTextStyles = '/appbar-text-styles';
  static const String proxyWidgets = '/proxy-widgets';
  static const String align = '/align';
  static const String stack = '/stack';
  static const String decoratedBox = '/decorated-box';
  static const String container = '/container';
  static const String aspectRatio = '/aspect-ratio';
  static const String fractionallySizedBox = '/fractionally-sized-box';
  static const String fittedBox = '/fitted-box';
  static const String unconstrainedLimitedBox = '/unconstrained-limited-box';
  static const String overflowBox = '/overflow-box';
  static const String overflowIndicator = '/overflow-indicator';
  static const String offstage = '/offstage';
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
