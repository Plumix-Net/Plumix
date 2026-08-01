import 'package:flutter/material.dart';

class RefreshIndicatorDemoPage extends StatefulWidget {
  const RefreshIndicatorDemoPage({super.key});

  @override
  State<RefreshIndicatorDemoPage> createState() =>
      _RefreshIndicatorDemoPageState();
}

class _RefreshIndicatorDemoPageState extends State<RefreshIndicatorDemoPage> {
  int _variant = 0;
  bool _useCupertinoPlatform = false;
  bool _useThemeOverrides = false;
  bool _useSchemeColor = false;
  int _refreshCount = 0;
  String _status = 'idle';

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData theme = baseTheme.copyWith(
      platform: _useCupertinoPlatform
          ? TargetPlatform.iOS
          : TargetPlatform.android,
      primaryColor: const Color(0xFFFF6F00),
      colorScheme: baseTheme.colorScheme.copyWith(
        primary: const Color(0xFF00897B),
      ),
      progressIndicatorTheme: _useThemeOverrides
          ? const ProgressIndicatorThemeData(
              color: Color(0xFF6A1B9A),
              refreshBackgroundColor: Color(0xFFFFF3E0),
              strokeAlign: -1,
              strokeCap: StrokeCap.round,
            )
          : const ProgressIndicatorThemeData(),
    );

    return Theme(
      data: theme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'RefreshIndicator + RefreshProgressIndicator',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const SizedBox(height: 8),
          const Text(
            'Pull the list down from its top edge. Cycle Material/adaptive/no-spinner paths and theme the refresh surface.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          const SizedBox(height: 10),
          Row(
            children: <Widget>[
              _buildButton(_variantLabel(), () {
                setState(() => _variant = (_variant + 1) % 3);
              }, 126),
              const SizedBox(width: 8),
              _buildButton(
                _useCupertinoPlatform ? 'platform=iOS' : 'platform=Android',
                () => setState(
                  () => _useCupertinoPlatform = !_useCupertinoPlatform,
                ),
                132,
              ),
              const SizedBox(width: 8),
              _buildButton(
                _useThemeOverrides ? 'theme=on' : 'theme=off',
                () => setState(() => _useThemeOverrides = !_useThemeOverrides),
                104,
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: <Widget>[
              _buildButton(
                _useSchemeColor ? 'color=scheme' : 'color=widget',
                () => setState(() => _useSchemeColor = !_useSchemeColor),
                126,
              ),
              const SizedBox(width: 8),
              const Text(
                'scheme teal; legacy primary orange',
                style: TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'status=$_status, refreshCount=$_refreshCount; '
            'drag past the armed threshold, then release',
            style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
          ),
          const SizedBox(height: 10),
          Expanded(child: _buildRefreshWrapper(_buildList())),
        ],
      ),
    );
  }

  Widget _buildRefreshWrapper(Widget child) {
    switch (_variant) {
      case 1:
        return RefreshIndicator.adaptive(
          onRefresh: _handleRefresh,
          color: _useSchemeColor ? null : const Color(0xFF1565C0),
          semanticsLabel: 'Refresh sample list',
          child: child,
        );
      case 2:
        return RefreshIndicator.noSpinner(
          onRefresh: _handleRefresh,
          onStatusChange: (RefreshIndicatorStatus? status) {
            setState(() => _status = status?.name ?? 'idle');
          },
          semanticsLabel: 'Refresh sample list',
          child: child,
        );
      default:
        return RefreshIndicator(
          onRefresh: _handleRefresh,
          color: _useSchemeColor ? null : const Color(0xFF1565C0),
          backgroundColor: _useThemeOverrides ? null : Colors.white,
          semanticsLabel: 'Refresh sample list',
          child: child,
        );
    }
  }

  Widget _buildList() {
    return ListView.builder(
      itemCount: 24,
      itemExtent: 54,
      padding: const EdgeInsets.all(8),
      itemBuilder: (BuildContext context, int index) => Container(
        color: index.isEven ? Colors.white : const Color(0xFFF5F7FA),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        child: Text(
          'refresh row #${index + 1}',
          style: const TextStyle(fontSize: 13, color: Colors.black),
        ),
      ),
    );
  }

  Future<void> _handleRefresh() async {
    if (mounted) setState(() => _status = 'refresh');
    await Future<void>.delayed(const Duration(milliseconds: 650));
    if (mounted) {
      setState(() {
        _refreshCount += 1;
        _status = 'done';
      });
    }
  }

  String _variantLabel() => switch (_variant) {
    1 => 'adaptive',
    2 => 'noSpinner',
    _ => 'material',
  };

  Widget _buildButton(String label, VoidCallback onTap, double width) {
    return SizedBox(
      width: width,
      child: TextButton(
        onPressed: onTap,
        style: TextButton.styleFrom(
          minimumSize: const Size(0, 36),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          backgroundColor: const Color(0xFFE9F0FF),
          foregroundColor: Colors.black,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }
}
