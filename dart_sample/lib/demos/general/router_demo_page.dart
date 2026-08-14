import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

/// C# parity source: src/Sample/Plumix.Sample/Demos/General/RouterDemoPage.cs
class RouterDemoPage extends StatefulWidget {
  const RouterDemoPage({super.key});

  @override
  State<RouterDemoPage> createState() => _RouterDemoPageState();
}

class _RouterDemoPageState extends State<RouterDemoPage> {
  final _DemoRouteInformationProvider _provider = _DemoRouteInformationProvider(
    RouteInformation(uri: Uri.parse('/home')),
  );
  final _DemoRouterDelegate _routerDelegate = _DemoRouterDelegate();
  final RootBackButtonDispatcher _backButtonDispatcher = RootBackButtonDispatcher();
  final List<String> _reports = <String>[];

  @override
  void initState() {
    super.initState();
    _provider.reported = (RouteInformation information) {
      setState(() => _reports.add(information.uri.toString()));
    };
  }

  @override
  void dispose() {
    _provider.dispose();
    _routerDelegate.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        const Text(
          'Router demo',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'The provider publishes a location, the parser turns it into a configuration and the '
          'delegate builds the page. Popping goes through the back-button dispatcher.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildAction('Home', () => _go('/home'), const Color(0xFFE8F5E9)),
            _buildAction('Details', () => _go('/details'), const Color(0xFFE3F2FD)),
            _buildAction('Settings', () => _go('/settings'), const Color(0xFFFFF3E0)),
            _buildAction('Back', _handleBack, const Color(0xFFFCE4EC)),
          ],
        ),
        Text(
          'reported: ${_reports.join(', ')}',
          style: const TextStyle(fontSize: 12),
        ),
        SizedBox(
          height: 180,
          child: ColoredBox(
            color: const Color(0xFFFAFAFA),
            child: Router<_DemoRouteConfiguration>(
              routerDelegate: _routerDelegate,
              routeInformationProvider: _provider,
              routeInformationParser: _DemoRouteInformationParser(),
              backButtonDispatcher: _backButtonDispatcher,
            ),
          ),
        ),
      ],
    );
  }

  void _go(String location) {
    _provider.setValue(RouteInformation(uri: Uri.parse(location)));
  }

  void _handleBack() {
    _backButtonDispatcher.invokeCallback(SynchronousFuture<bool>(false));
  }

  static Widget _buildAction(String label, VoidCallback onTap, Color background) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        color: background,
        child: Text(
          label,
          style: const TextStyle(fontSize: 12, color: Colors.black),
        ),
      ),
    );
  }
}

/// The parsed configuration the demo delegate renders.
@immutable
class _DemoRouteConfiguration {
  const _DemoRouteConfiguration(this.path, {required this.showDetail});

  final String path;
  final bool showDetail;
}

class _DemoRouteInformationParser extends RouteInformationParser<_DemoRouteConfiguration> {
  @override
  Future<_DemoRouteConfiguration> parseRouteInformation(RouteInformation routeInformation) {
    final String path = routeInformation.uri.toString();
    return SynchronousFuture<_DemoRouteConfiguration>(
      _DemoRouteConfiguration(path, showDetail: path == '/details'),
    );
  }

  @override
  RouteInformation? restoreRouteInformation(_DemoRouteConfiguration configuration) {
    return RouteInformation(uri: Uri.parse(configuration.path));
  }
}

class _DemoRouterDelegate extends RouterDelegate<_DemoRouteConfiguration> with ChangeNotifier {
  _DemoRouteConfiguration _configuration = const _DemoRouteConfiguration('/home', showDetail: false);

  @override
  _DemoRouteConfiguration? get currentConfiguration => _configuration;

  @override
  Future<void> setNewRoutePath(_DemoRouteConfiguration configuration) {
    _configuration = configuration;
    return SynchronousFuture<void>(null);
  }

  @override
  Future<bool> popRoute() {
    if (_configuration.path == '/home') {
      return SynchronousFuture<bool>(false);
    }

    _configuration = const _DemoRouteConfiguration('/home', showDetail: false);
    notifyListeners();
    return SynchronousFuture<bool>(true);
  }

  @override
  Widget build(BuildContext context) {
    return BackButtonListener(
      onBackButtonPressed: popRoute,
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          spacing: 8,
          children: <Widget>[
            Text(
              _configuration.path,
              style: const TextStyle(fontSize: 18, color: Colors.black),
            ),
            Text(
              _configuration.showDetail ? 'detail page' : 'top level page',
              style: const TextStyle(fontSize: 12, color: Colors.grey),
            ),
          ],
        ),
      ),
    );
  }
}

class _DemoRouteInformationProvider extends RouteInformationProvider with ChangeNotifier {
  _DemoRouteInformationProvider(this._value);

  RouteInformation _value;

  ValueChanged<RouteInformation>? reported;

  @override
  RouteInformation get value => _value;

  void setValue(RouteInformation value) {
    _value = value;
    notifyListeners();
  }

  @override
  void routerReportsNewRouteInformation(
    RouteInformation routeInformation, {
    RouteInformationReportingType type = RouteInformationReportingType.none,
  }) {
    _value = routeInformation;
    reported?.call(routeInformation);
  }
}
