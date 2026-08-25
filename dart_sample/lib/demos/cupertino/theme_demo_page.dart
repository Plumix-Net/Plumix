import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoThemeDemoPage extends StatefulWidget {
  const CupertinoThemeDemoPage({super.key});

  @override
  State<CupertinoThemeDemoPage> createState() => _CupertinoThemeDemoPageState();
}

class _CupertinoThemeDemoPageState extends State<CupertinoThemeDemoPage> {
  static const List<(String, CupertinoDynamicColor)> _swatches =
      <(String, CupertinoDynamicColor)>[
        ('systemBlue', CupertinoColors.systemBlue),
        ('systemRed', CupertinoColors.systemRed),
        ('systemGreen', CupertinoColors.systemGreen),
        ('systemIndigo', CupertinoColors.systemIndigo),
        ('label', CupertinoColors.label),
        ('secondaryLabel', CupertinoColors.secondaryLabel),
        ('separator', CupertinoColors.separator),
        ('systemFill', CupertinoColors.systemFill),
        ('systemBackground', CupertinoColors.systemBackground),
        ('secondarySystemBackground', CupertinoColors.secondarySystemBackground),
      ];

  bool _dark = false;
  bool _highContrast = false;
  bool _elevated = false;
  bool _cupertinoOverride = false;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'CupertinoTheme + dynamic colors',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'CupertinoDynamicColor resolves against brightness, accessibility '
          'contrast and interface elevation.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              label: _dark ? 'Brightness: dark' : 'Brightness: light',
              onTap: _toggleBrightness,
              width: 168,
              background: const Color(0xFFE9F0FF),
            ),
            _buildControlButton(
              label: _highContrast ? 'Contrast: high' : 'Contrast: normal',
              onTap: _toggleHighContrast,
              width: 160,
              background: const Color(0xFFEAE4FF),
            ),
            _buildControlButton(
              label: _elevated ? 'Level: elevated' : 'Level: base',
              onTap: _toggleElevation,
              width: 148,
              background: const Color(0xFFE8F4E8),
            ),
          ],
        ),
        Text(
          'brightness=${_dark ? 'dark' : 'light'}, '
          'highContrast=${_highContrast ? 'true' : 'false'}, '
          'level=${_elevated ? 'elevated' : 'base'}',
          style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
        ),
        _buildProbe(context),
        const Text(
          'MaterialBasedCupertinoThemeData',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'A Material Theme installs a CupertinoTheme that defers to the '
          'Material ThemeData; ThemeData.cupertinoOverrideTheme preempts '
          'individual attributes.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        _buildControlButton(
          label: _cupertinoOverride
              ? 'Override: systemPink'
              : 'Override: none (cascaded)',
          onTap: _toggleCupertinoOverride,
          width: 232,
          background: const Color(0xFFFDE7F3),
        ),
        _buildBridgeProbe(),
      ],
    );
  }

  Widget _buildBridgeProbe() {
    return Row(
      spacing: 8,
      children: <Widget>[
        Expanded(
          child: _buildBridgeCard(
            'ThemeData.light()',
            ThemeData(
              colorScheme: const ColorScheme.light(primary: Color(0xFF2E7D32)),
              cupertinoOverrideTheme: _cupertinoOverride
                  ? const CupertinoThemeData(
                      primaryColor: CupertinoColors.systemPink,
                    )
                  : null,
            ),
          ),
        ),
        Expanded(
          child: _buildBridgeCard(
            'ThemeData.dark()',
            ThemeData(
              colorScheme: const ColorScheme.dark(primary: Color(0xFF80CBC4)),
              cupertinoOverrideTheme: _cupertinoOverride
                  ? const CupertinoThemeData(
                      primaryColor: CupertinoColors.systemPink,
                    )
                  : null,
            ),
          ),
        ),
      ],
    );
  }

  static Widget _buildBridgeCard(String label, ThemeData data) {
    return Theme(
      data: data,
      child: Builder(
        builder: (BuildContext context) {
          final CupertinoThemeData theme = CupertinoTheme.of(context);
          return Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: theme.scaffoldBackgroundColor,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: const Color(0x33000000)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 6,
              children: <Widget>[
                Text(
                  label,
                  style: theme.textTheme.navTitleTextStyle.copyWith(
                    fontSize: 14,
                  ),
                ),
                Text(
                  'brightness: ${CupertinoTheme.brightnessOf(context).name}',
                  style: theme.textTheme.textStyle.copyWith(fontSize: 12),
                ),
                Text(
                  'actionTextStyle follows primaryColor',
                  style: theme.textTheme.actionTextStyle.copyWith(fontSize: 12),
                ),
                CupertinoSwitch(value: true, onChanged: (bool _) {}),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildProbe(BuildContext context) {
    return MediaQuery(
      data: MediaQuery.of(context).copyWith(
        platformBrightness: _dark ? Brightness.dark : Brightness.light,
        highContrast: _highContrast,
      ),
      child: CupertinoUserInterfaceLevel(
        data: _elevated
            ? CupertinoUserInterfaceLevelData.elevated
            : CupertinoUserInterfaceLevelData.base,
        // No explicit brightness: the theme defers to the MediaQuery above.
        child: CupertinoTheme(
          data: const CupertinoThemeData(),
          child: Builder(builder: _buildResolvedTheme),
        ),
      ),
    );
  }

  static Widget _buildResolvedTheme(BuildContext context) {
    final CupertinoThemeData theme = CupertinoTheme.of(context);
    final Color labelColor = CupertinoDynamicColor.resolve(
      CupertinoColors.label,
      context,
    );
    final Color separator = CupertinoDynamicColor.resolve(
      CupertinoColors.separator,
      context,
    );

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: theme.scaffoldBackgroundColor,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: separator),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: <Widget>[
          Text('navTitleTextStyle', style: theme.textTheme.navTitleTextStyle),
          Text('textStyle — body copy', style: theme.textTheme.textStyle),
          Text(
            'actionTextStyle — primaryColor',
            style: theme.textTheme.actionTextStyle,
          ),
          Text('TABLABELTEXTSTYLE', style: theme.textTheme.tabLabelTextStyle),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              for (final (String name, CupertinoDynamicColor color) in _swatches)
                _buildSwatch(context, name, color, labelColor, separator),
            ],
          ),
        ],
      ),
    );
  }

  static Widget _buildSwatch(
    BuildContext context,
    String name,
    CupertinoDynamicColor color,
    Color labelColor,
    Color separator,
  ) {
    return SizedBox(
      width: 150,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 4,
        children: <Widget>[
          Container(
            height: 28,
            decoration: BoxDecoration(
              color: CupertinoDynamicColor.resolve(color, context),
              borderRadius: BorderRadius.circular(6),
              border: Border.all(color: separator),
            ),
          ),
          Text(name, style: TextStyle(fontSize: 11, color: labelColor)),
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
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }

  void _toggleBrightness() {
    setState(() => _dark = !_dark);
  }

  void _toggleHighContrast() {
    setState(() => _highContrast = !_highContrast);
  }

  void _toggleElevation() {
    setState(() => _elevated = !_elevated);
  }

  void _toggleCupertinoOverride() {
    setState(() => _cupertinoOverride = !_cupertinoOverride);
  }
}
