import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoLocalizationsDemoPage extends StatefulWidget {
  const CupertinoLocalizationsDemoPage({super.key});

  @override
  State<CupertinoLocalizationsDemoPage> createState() =>
      _CupertinoLocalizationsDemoPageState();
}

class _CupertinoLocalizationsDemoPageState
    extends State<CupertinoLocalizationsDemoPage> {
  static const List<(String, Locale)> _locales = <(String, Locale)>[
    ('English', Locale('en')),
    ('Français', Locale('fr')),
    ('Русский', Locale('ru')),
    ('中文', Locale('zh')),
    ('العربية', Locale('ar')),
  ];

  static final DateTime _sampleDate = DateTime(2019, 3, 25);

  int _selected = 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino localizations',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'GlobalCupertinoLocalizations.delegates translates every Cupertino '
          "string and formats dates with the locale's own calendar data.",
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(spacing: 8, runSpacing: 8, children: _buildLocaleButtons()),
        Localizations(
          locale: _locales[_selected].$2,
          delegates: GlobalCupertinoLocalizations.delegates,
          child: Builder(builder: _buildTranslations),
        ),
      ],
    );
  }

  List<Widget> _buildLocaleButtons() {
    return <Widget>[
      for (int index = 0; index < _locales.length; index++)
        GestureDetector(
          behavior: HitTestBehavior.opaque,
          onTap: () => setState(() => _selected = index),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
            decoration: BoxDecoration(
              color: index == _selected
                  ? const Color(0xFF007AFF)
                  : const Color(0xFFF2F2F7),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Text(
              _locales[index].$1,
              style: TextStyle(
                fontSize: 14,
                color: index == _selected ? Colors.white : Colors.black,
              ),
            ),
          ),
        ),
    ];
  }

  Widget _buildTranslations(BuildContext context) {
    final CupertinoLocalizations localizations = CupertinoLocalizations.of(
      context,
    );
    final WidgetsLocalizations widgets = WidgetsLocalizations.of(context);

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFF2F2F7),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 6,
        children: <Widget>[
          _buildRow('textDirection', widgets.textDirection.name),
          _buildRow('alertDialogLabel', localizations.alertDialogLabel),
          _buildRow(
            'toolbar',
            <String>[
              localizations.cutButtonLabel,
              localizations.copyButtonLabel,
              localizations.pasteButtonLabel,
              localizations.selectAllButtonLabel,
            ].join(' · '),
          ),
          _buildRow(
            'datePickerMediumDate',
            localizations.datePickerMediumDate(_sampleDate),
          ),
          _buildRow(
            'datePickerMonth / standalone',
            '${localizations.datePickerMonth(5)} / '
                '${localizations.datePickerStandaloneMonth(5)}',
          ),
          _buildRow(
            'datePickerDayOfMonth',
            '${localizations.datePickerDayOfMonth(1)} · '
                '${localizations.datePickerDayOfMonth(1, 2)}',
          ),
          _buildRow(
            'datePickerHourSemanticsLabel',
            '${localizations.datePickerHourSemanticsLabel(1)} · '
                '${localizations.datePickerHourSemanticsLabel(12)}',
          ),
          _buildRow(
            'timerPicker',
            '${localizations.timerPickerMinute(10)} '
                '${localizations.timerPickerMinuteLabel(10)} · '
                '${localizations.timerPickerHour(1)} '
                '${localizations.timerPickerHourLabel(1)}',
          ),
          _buildRow(
            'tabSemanticsLabel',
            localizations.tabSemanticsLabel(tabIndex: 1, tabCount: 2),
          ),
          _buildRow('reorderItemUp', widgets.reorderItemUp),
        ],
      ),
    );
  }

  Widget _buildRow(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      spacing: 12,
      children: <Widget>[
        SizedBox(
          width: 220,
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, color: Colors.black54),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(fontSize: 13, color: Colors.black),
          ),
        ),
      ],
    );
  }
}
