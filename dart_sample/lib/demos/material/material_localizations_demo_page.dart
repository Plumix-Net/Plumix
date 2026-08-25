import 'package:material_ui/material_ui.dart';

class MaterialLocalizationsDemoPage extends StatefulWidget {
  const MaterialLocalizationsDemoPage({super.key});

  @override
  State<MaterialLocalizationsDemoPage> createState() =>
      _MaterialLocalizationsDemoPageState();
}

class _MaterialLocalizationsDemoPageState
    extends State<MaterialLocalizationsDemoPage> {
  static const List<(String, Locale)> _locales = <(String, Locale)>[
    ('English', Locale('en')),
    ('Deutsch', Locale('de')),
    ('Español', Locale('es')),
    ('Русский', Locale('ru')),
    ('中文', Locale('zh')),
  ];

  static final DateTime _sampleDate = DateTime(2015, 7, 23);

  int _selected = 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Material localizations',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'GlobalMaterialLocalizations.delegates translates every Material '
          "string and formats dates, times and numbers with the locale's own "
          'CLDR data.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(spacing: 8, runSpacing: 8, children: _buildLocaleButtons()),
        Localizations(
          locale: _locales[_selected].$2,
          delegates: GlobalMaterialLocalizations.delegates,
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
                  ? const Color(0xFF6750A4)
                  : const Color(0xFFEDE7F6),
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
    final MaterialLocalizations localizations = MaterialLocalizations.of(
      context,
    );
    final WidgetsLocalizations widgets = WidgetsLocalizations.of(context);
    const TimeOfDay midMorning = TimeOfDay(hour: 9, minute: 32);
    const TimeOfDay evening = TimeOfDay(hour: 20, minute: 32);

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFEDE7F6),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 6,
        children: <Widget>[
          _buildRow('textDirection', widgets.textDirection.name),
          _buildRow('scriptCategory', localizations.scriptCategory.name),
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
          _buildRow('formatFullDate', localizations.formatFullDate(_sampleDate)),
          _buildRow(
            'formatMediumDate',
            localizations.formatMediumDate(_sampleDate),
          ),
          _buildRow(
            'formatMonthYear',
            localizations.formatMonthYear(_sampleDate),
          ),
          _buildRow(
            'formatCompactDate',
            localizations.formatCompactDate(_sampleDate),
          ),
          _buildRow('dateHelpText', localizations.dateHelpText),
          _buildRow(
            'narrowWeekdays / firstDayOfWeekIndex',
            '${localizations.narrowWeekdays.join(' ')} · '
                '${localizations.firstDayOfWeekIndex}',
          ),
          _buildRow('timeOfDayFormat', localizations.timeOfDayFormat().name),
          _buildRow(
            'formatTimeOfDay',
            '${localizations.formatTimeOfDay(midMorning)} · '
                '${localizations.formatTimeOfDay(evening)}',
          ),
          _buildRow(
            'formatDecimal',
            '${localizations.formatDecimal(123)} · '
                '${localizations.formatDecimal(10000)}',
          ),
          _buildRow(
            'selectedRowCountTitle',
            localizations.selectedRowCountTitle(2),
          ),
          _buildRow(
            'pageRowsInfoTitle',
            localizations.pageRowsInfoTitle(1, 10, 100, false),
          ),
          _buildRow(
            'tabLabel',
            localizations.tabLabel(tabIndex: 1, tabCount: 2),
          ),
          _buildRow(
            'aboutListTileTitle',
            localizations.aboutListTileTitle('Plumix'),
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
