import 'package:flutter/material.dart';

class CardDemoPage extends StatefulWidget {
  const CardDemoPage({super.key});

  @override
  State<CardDemoPage> createState() => _CardDemoPageState();
}

class _CardDemoPageState extends State<CardDemoPage> {
  bool _useMaterial3 = true;
  bool _useThemeOverrides = false;
  bool _clip = false;
  bool _dense = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData pageTheme = ThemeData(
      useMaterial3: _useMaterial3,
      colorScheme: baseTheme.colorScheme,
      textTheme: baseTheme.textTheme,
      scaffoldBackgroundColor: baseTheme.scaffoldBackgroundColor,
      cardTheme: _useThemeOverrides
          ? CardThemeData(
              color: const Color(0xFFF5F9EE),
              shadowColor: const Color(0xFF455A64),
              surfaceTintColor: const Color(0xFF6750A4),
              elevation: 3,
              margin: const EdgeInsets.all(8),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(18),
              ),
              clipBehavior: _clip ? Clip.antiAlias : Clip.none,
            )
          : const CardThemeData(),
    );

    return Theme(
      data: pageTheme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'Card baseline',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Elevated, filled, and outlined Material card variants with theme, mode, and clip probes.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildControlButton(
                label: _useMaterial3 ? 'M3' : 'M2',
                onTap: () => setState(() => _useMaterial3 = !_useMaterial3),
                width: 80,
                background: const Color(0xFFE9F0FF),
              ),
              _buildControlButton(
                label: _useThemeOverrides ? 'Theme on' : 'Theme off',
                onTap: () =>
                    setState(() => _useThemeOverrides = !_useThemeOverrides),
                width: 112,
                background: const Color(0xFFEAF6F7),
              ),
              _buildControlButton(
                label: _clip ? 'Clip on' : 'Clip off',
                onTap: () => setState(() => _clip = !_clip),
                width: 96,
                background: const Color(0xFFF0E8FF),
              ),
            ],
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildControlButton(
                label: _dense ? 'Dense' : 'Regular',
                onTap: () => setState(() => _dense = !_dense),
                width: 98,
                background: const Color(0xFFF8EFE2),
              ),
              _buildControlButton(
                label: 'Reset',
                onTap: _resetState,
                width: 88,
                background: const Color(0xFFF3E8D8),
              ),
            ],
          ),
          Text(
            'useMaterial3=$_useMaterial3, theme=$_useThemeOverrides, clip=$_clip, dense=$_dense',
            style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
          Expanded(
            child: ColoredBox(
              color: const Color(0xFFF7F9FC),
              child: SingleChildScrollView(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: <Widget>[
                    _buildElevatedCard(),
                    _buildFilledCard(),
                    _buildOutlinedCard(),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildElevatedCard() {
    return Card(
      clipBehavior: _clip ? Clip.antiAlias : null,
      child: ListTile(
        dense: _dense,
        leading: const Icon(Icons.star_outline),
        title: const Text('Elevated card'),
        subtitle: const Text(
          'Default variant keeps elevation and surfaceContainerLow color.',
        ),
        trailing: const Icon(Icons.info_outline),
      ),
    );
  }

  Widget _buildFilledCard() {
    return Card.filled(
      clipBehavior: _clip ? Clip.antiAlias : null,
      child: _buildCardBody(
        title: 'Filled card',
        body:
            'Filled cards use a quieter container color and zero default elevation in Material 3.',
      ),
    );
  }

  Widget _buildOutlinedCard() {
    return Card.outlined(
      clipBehavior: _clip ? Clip.antiAlias : null,
      child: _buildCardBody(
        title: 'Outlined card',
        body:
            'Outlined cards add the default outlineVariant border while keeping elevation at zero.',
      ),
    );
  }

  Widget _buildCardBody({required String title, required String body}) {
    return Padding(
      padding: _dense
          ? const EdgeInsets.symmetric(horizontal: 14, vertical: 10)
          : const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        spacing: 6,
        children: <Widget>[
          Text(
            title,
            style: const TextStyle(fontSize: 16, color: Colors.black),
          ),
          Text(
            body,
            style: const TextStyle(fontSize: 13, color: Colors.blueGrey),
          ),
        ],
      ),
    );
  }

  Widget _buildControlButton({
    required String label,
    required VoidCallback onTap,
    required double width,
    required Color background,
  }) {
    return SizedBox(
      width: width,
      child: TextButton(
        onPressed: onTap,
        style: TextButton.styleFrom(
          backgroundColor: background,
          foregroundColor: Colors.black,
          minimumSize: const Size(0, 36),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }

  void _resetState() {
    setState(() {
      _useMaterial3 = true;
      _useThemeOverrides = false;
      _clip = false;
      _dense = false;
    });
  }
}
