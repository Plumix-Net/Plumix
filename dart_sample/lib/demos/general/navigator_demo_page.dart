import 'package:flutter/material.dart';

import '../../counter_widgets.dart';
import '../../sample_routes.dart';

class NavigatorDemoPage extends StatelessWidget {
  const NavigatorDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        const Text(
          'Navigator API demo',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Uses static Navigator methods and RouteData query parsing.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        const NavigatorRouteAwareProbe(),
        _buildAction(
          label: "PushNamed('/navigator/details?id=42&mode=string')",
          onTap: () => Navigator.of(context).pushNamed(
            '${SampleRoutes.navigatorDetails}?id=42&mode=string',
            arguments: 'payload:string',
          ),
          background: const Color(0xFFE8F5E9),
        ),
        _buildAction(
          label: 'PushNamed(RouteData.fromLocation(...))',
          onTap: () {
            final RouteData routeData = RouteData.fromLocation(
              '${SampleRoutes.navigatorDetails}?id=7&mode=route-data',
              arguments: 'payload:route-data',
            );
            Navigator.of(
              context,
            ).pushNamed(routeData.location, arguments: routeData);
          },
          background: const Color(0xFFEAF4FF),
        ),
        _buildAction(
          label: 'PushReplacementNamed(details)',
          onTap: () => Navigator.of(context).pushReplacementNamed(
            '${SampleRoutes.navigatorDetails}?id=99&mode=replacement',
            arguments: 'payload:replacement',
          ),
          background: const Color(0xFFFFF3E0),
        ),
        _buildAction(
          label: 'PushNamedAndRemoveUntil(details, menu)',
          onTap: () => Navigator.of(context).pushNamedAndRemoveUntil(
            '${SampleRoutes.navigatorDetails}?id=5&mode=remove-until',
            (Route<dynamic> route) => route.settings.name == SampleRoutes.menu,
            arguments: 'payload:remove-until',
          ),
          background: const Color(0xFFF3E5F5),
        ),
      ],
    );
  }

  static Widget _buildAction({
    required String label,
    required VoidCallback onTap,
    required Color background,
  }) {
    return CounterTapButton(
      label: label,
      onTap: onTap,
      background: background,
      foreground: Colors.black,
      fontSize: 12,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}

class NavigatorRouteAwareProbe extends StatefulWidget {
  const NavigatorRouteAwareProbe({super.key});

  @override
  State<NavigatorRouteAwareProbe> createState() =>
      _NavigatorRouteAwareProbeState();
}

class _NavigatorRouteAwareProbeState extends State<NavigatorRouteAwareProbe>
    with RouteAware {
  PageRoute<dynamic>? _route;
  int _didPushCount = 0;
  int _didPopCount = 0;
  int _didPopNextCount = 0;
  int _didPushNextCount = 0;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();

    final Route<dynamic>? modalRoute = ModalRoute.of(context);
    if (modalRoute is! PageRoute<dynamic>) {
      return;
    }

    if (identical(_route, modalRoute)) {
      return;
    }

    SampleNavigationObservers.pageRoutes.unsubscribe(this);
    _route = modalRoute;
    SampleNavigationObservers.pageRoutes.subscribe(this, modalRoute);
  }

  @override
  void dispose() {
    SampleNavigationObservers.pageRoutes.unsubscribe(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final String routeName = ModalRoute.of(context)?.settings.name ?? '(null)';
    return Container(
      color: const Color(0xFFF1F8FF),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      child: Text(
        'RouteAware: name=$routeName didPush=$_didPushCount didPushNext=$_didPushNextCount didPopNext=$_didPopNextCount didPop=$_didPopCount',
        style: const TextStyle(fontSize: 12, color: Colors.black),
      ),
    );
  }

  @override
  void didPush() {
    if (!mounted) {
      return;
    }

    setState(() => _didPushCount += 1);
  }

  @override
  void didPop() {
    if (!mounted) {
      return;
    }

    setState(() => _didPopCount += 1);
  }

  @override
  void didPopNext() {
    if (!mounted) {
      return;
    }

    setState(() => _didPopNextCount += 1);
  }

  @override
  void didPushNext() {
    if (!mounted) {
      return;
    }

    setState(() => _didPushNextCount += 1);
  }
}

class NavigatorDetailsPage extends StatefulWidget {
  const NavigatorDetailsPage({required this.routeData, super.key});

  final RouteData routeData;

  @override
  State<NavigatorDetailsPage> createState() => _NavigatorDetailsPageState();
}

class _NavigatorDetailsPageState extends State<NavigatorDetailsPage> {
  bool _userGestureInProgress = false;

  @override
  Widget build(BuildContext context) {
    final String id = _getQuery('id');
    final String mode = _getQuery('mode');
    final String payload = widget.routeData.arguments?.toString() ?? 'null';
    final int? parsedId = int.tryParse(id);
    final int nextId = parsedId == null ? 1 : parsedId + 1;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        Text(
          'route: ${widget.routeData.name}',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
        Text(
          'query.id: $id',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
        Text(
          'query.mode: $mode',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
        Text(
          'arguments: $payload',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
        const SizedBox(height: 6),
        CounterTapButton(
          label: 'Push next details (id=$nextId)',
          onTap: () {
            final RouteData routeData = RouteData.fromLocation(
              '${SampleRoutes.navigatorDetails}?id=$nextId&mode=chain',
              arguments: 'payload:chain:$nextId',
            );
            Navigator.of(
              context,
            ).pushNamed(routeData.location, arguments: routeData);
          },
          background: const Color(0xFFEAF4FF),
          foreground: Colors.black,
          fontSize: 12,
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        ),
        CounterTapButton(
          label: 'PopUntil(menu)',
          onTap: () => Navigator.of(context).popUntil(
            (Route<dynamic> route) => route.settings.name == SampleRoutes.menu,
          ),
          background: const Color(0xFFFFF3E0),
          foreground: Colors.black,
          fontSize: 12,
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        ),
        CounterTapButton(
          label: 'RemoveRouteBelow(current)',
          onTap: () {
            final NavigatorState navigator = Navigator.of(context);
            final Route<dynamic>? currentRoute = ModalRoute.of(context);
            if (navigator.canPop() && currentRoute != null) {
              navigator.removeRouteBelow(currentRoute);
            }
          },
          background: const Color(0xFFF3E5F5),
          foreground: Colors.black,
          fontSize: 12,
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: CounterTapButton(
                label: 'Start gesture',
                onTap: () => setState(() => _userGestureInProgress = true),
                background: const Color(0xFFE0F7FA),
                foreground: Colors.black,
                fontSize: 12,
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 8,
                ),
              ),
            ),
            Expanded(
              child: CounterTapButton(
                label: 'Stop gesture',
                onTap: () => setState(() => _userGestureInProgress = false),
                background: const Color(0xFFFFF9C4),
                foreground: Colors.black,
                fontSize: 12,
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 8,
                ),
              ),
            ),
          ],
        ),
        CounterTapButton(
          label: _userGestureInProgress
              ? 'MaybePopFromUserGesture'
              : 'MaybePopFromUserGesture (disabled)',
          onTap: _userGestureInProgress
              ? () => Navigator.maybePop(context)
              : null,
          background: const Color(0xFFD7CCC8),
          foreground: Colors.black,
          fontSize: 12,
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        ),
        CounterTapButton(
          label: 'Back (MaybePop)',
          onTap: () => Navigator.maybePop(context),
          background: Colors.blue,
          foreground: Colors.white,
          fontSize: 12,
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        ),
      ],
    );
  }

  String _getQuery(String key) {
    return widget.routeData.queryParameters[key] ?? '-';
  }
}
