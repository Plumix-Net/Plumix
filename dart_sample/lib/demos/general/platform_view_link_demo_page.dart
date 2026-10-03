import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class PlatformViewLinkDemoPage extends StatefulWidget {
  const PlatformViewLinkDemoPage({super.key});

  @override
  State<PlatformViewLinkDemoPage> createState() =>
      _PlatformViewLinkDemoPageState();
}

class _PlatformViewLinkDemoPageState extends State<PlatformViewLinkDemoPage> {
  static const Map<String, Color> _viewColors = <String, Color>{
    'demo/blue': Color(0xFF9FC5E8),
    'demo/green': Color(0xFFB6D7A8),
  };

  final FocusNode _otherNode = FocusNode(
    debugLabel: 'outside the platform view',
  );
  final List<String> _log = <String>[];
  String _viewType = 'demo/blue';
  ValueChanged<bool>? _requestPlatformFocus;

  @override
  void dispose() {
    _otherNode.dispose();
    super.dispose();
  }

  void _addLog(String entry) {
    if (!mounted) {
      return;
    }
    setState(() {
      _log.insert(0, entry);
      if (_log.length > 6) {
        _log.removeLast();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'PlatformViewLink',
          style: TextStyle(fontSize: 20, color: Color(0xFF000000)),
        ),
        const Text(
          "The link lays out a placeholder, creates the controller with the "
          "placeholder's size and position, then shows the surface factory's "
          'widget. Focus moves both ways.',
          style: TextStyle(fontSize: 14, color: Color(0xFF696969)),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            _buildButton(
              'viewType: $_viewType',
              () => setState(
                () => _viewType = _viewType == 'demo/blue'
                    ? 'demo/green'
                    : 'demo/blue',
              ),
            ),
            _buildButton(
              'platform requests focus',
              () => _requestPlatformFocus?.call(true),
            ),
            _buildButton('focus outside', () => _otherNode.requestFocus()),
          ],
        ),
        SizedBox(
          height: 120,
          child: PlatformViewLink(
            viewType: _viewType,
            onCreatePlatformView: (PlatformViewCreationParams params) {
              _requestPlatformFocus = params.onFocusChanged;
              return _DemoPlatformViewController(params, _addLog);
            },
            surfaceFactory:
                (
                  BuildContext context,
                  PlatformViewController controller,
                ) => Container(
                  color: _viewColors[_viewType],
                  alignment: Alignment.center,
                  child: Builder(
                    builder: (BuildContext context) => Text(
                      'platform view #${controller.viewId}: '
                      '${Focus.of(context).hasFocus ? 'focused' : 'not focused'}',
                      style: const TextStyle(
                        fontSize: 16,
                        color: Color(0xFF000000),
                      ),
                    ),
                  ),
                ),
          ),
        ),
        Focus(
          focusNode: _otherNode,
          child: Builder(
            builder: (BuildContext context) => Container(
              height: 32,
              color: Focus.of(context).hasFocus
                  ? const Color(0xFFFFE082)
                  : const Color(0xFFDCE3ED),
              alignment: Alignment.center,
              child: const Text(
                'outside focus target',
                style: TextStyle(fontSize: 12, color: Color(0xFF000000)),
              ),
            ),
          ),
        ),
        for (final String entry in _log)
          Text(
            entry,
            style: const TextStyle(fontSize: 12, color: Color(0xFF2F4F4F)),
          ),
      ],
    );
  }

  Widget _buildButton(String label, VoidCallback onTap) {
    return CounterTapButton(
      label: label,
      onTap: onTap,
      background: const Color(0xFFDCE3ED),
      foreground: const Color(0xFF000000),
      fontSize: 12,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}

/// A controller whose "platform view" is created once the link reports a
/// non-empty size.
class _DemoPlatformViewController extends PlatformViewController {
  _DemoPlatformViewController(this._params, this._log);

  final PlatformViewCreationParams _params;
  final ValueChanged<String> _log;
  bool _created = false;

  @override
  int get viewId => _params.id;

  @override
  bool get awaitingCreation => !_created;

  @override
  Future<void> create({Size? size, Offset? position}) async {
    if (size == null || position == null) {
      return;
    }
    _created = true;
    _log(
      'create #$viewId (${_params.viewType}) '
      '${size.width.toStringAsFixed(0)}x${size.height.toStringAsFixed(0)} '
      'at (${position.dx.toStringAsFixed(0)}, '
      '${position.dy.toStringAsFixed(0)})',
    );
    _params.onPlatformViewCreated(viewId);
  }

  @override
  Future<void> dispatchPointerEvent(PointerEvent event) async {}

  @override
  Future<void> clearFocus() async {
    _log('clearFocus #$viewId');
  }

  @override
  Future<void> dispose() async {}
}
