using System;
using System.Collections.Generic;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/material_localizations_demo_page.dart
// (exact sample parity)

namespace Plumix;

public sealed class MaterialLocalizationsDemoPage : StatefulWidget
{
    public override State CreateState() => new MaterialLocalizationsDemoPageState();
}

internal sealed class MaterialLocalizationsDemoPageState : State
{
    private static readonly IReadOnlyList<(string Label, Locale Locale)> Locales =
    [
        ("English", new Locale("en")),
        ("Deutsch", new Locale("de")),
        ("Español", new Locale("es")),
        ("Русский", new Locale("ru")),
        ("中文", new Locale("zh")),
    ];

    private static readonly DateTime SampleDate = new(2015, 7, 23);

    private int _selected;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Material localizations", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "GlobalMaterialLocalizations.Delegates translates every Material string and "
                    + "formats dates, times and numbers with the locale's own CLDR data.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Wrap(spacing: 8.0, runSpacing: 8.0, children: BuildLocaleButtons()),
                new Localizations(
                    locale: Locales[_selected].Locale,
                    delegates: GlobalMaterialLocalizations.Delegates,
                    child: new Builder(BuildTranslations)),
            ]);
    }

    private IReadOnlyList<Widget> BuildLocaleButtons()
    {
        var buttons = new List<Widget>();
        for (int index = 0; index < Locales.Count; index++)
        {
            int target = index;
            bool selected = index == _selected;
            buttons.Add(
                new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: () => SetState(() => _selected = target),
                    child: new Container(
                        padding: EdgeInsets.Symmetric(horizontal: 14.0, vertical: 8.0),
                        decoration: new BoxDecoration(
                            Color: selected ? Color.Parse("#FF6750A4") : Color.Parse("#FFEDE7F6"),
                            BorderRadius: BorderRadius.Circular(10.0)),
                        child: new Text(
                            Locales[target].Label,
                            fontSize: 14.0,
                            color: selected ? Colors.White : Colors.Black))));
        }

        return buttons;
    }

    private static Widget BuildTranslations(BuildContext context)
    {
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        WidgetsLocalizations widgets = WidgetsLocalizations.Of(context);
        var midMorning = new TimeOfDay(9, 32);
        var evening = new TimeOfDay(20, 32);

        return new Container(
            padding: EdgeInsets.All(16.0),
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFEDE7F6"),
                BorderRadius: BorderRadius.Circular(12.0)),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 6.0,
                children:
                [
                    BuildRow("textDirection", widgets.TextDirection.ToString()),
                    BuildRow("scriptCategory", localizations.ScriptCategory.ToString()),
                    BuildRow("alertDialogLabel", localizations.AlertDialogLabel),
                    BuildRow(
                        "toolbar",
                        string.Join(
                            " · ",
                            localizations.CutButtonLabel,
                            localizations.CopyButtonLabel,
                            localizations.PasteButtonLabel,
                            localizations.SelectAllButtonLabel)),
                    BuildRow("formatFullDate", localizations.FormatFullDate(SampleDate)),
                    BuildRow("formatMediumDate", localizations.FormatMediumDate(SampleDate)),
                    BuildRow("formatMonthYear", localizations.FormatMonthYear(SampleDate)),
                    BuildRow("formatCompactDate", localizations.FormatCompactDate(SampleDate)),
                    BuildRow("dateHelpText", localizations.DateHelpText),
                    BuildRow(
                        "narrowWeekdays / firstDayOfWeekIndex",
                        $"{string.Join(' ', localizations.NarrowWeekdays)} · "
                        + $"{localizations.FirstDayOfWeekIndex}"),
                    BuildRow("timeOfDayFormat", localizations.TimeOfDayFormat().ToString()),
                    BuildRow(
                        "formatTimeOfDay",
                        $"{localizations.FormatTimeOfDay(midMorning)} · "
                        + $"{localizations.FormatTimeOfDay(evening)}"),
                    BuildRow(
                        "formatDecimal",
                        $"{localizations.FormatDecimal(123)} · {localizations.FormatDecimal(10000)}"),
                    BuildRow("selectedRowCountTitle", localizations.SelectedRowCountTitle(2)),
                    BuildRow("pageRowsInfoTitle", localizations.PageRowsInfoTitle(1, 10, 100, false)),
                    BuildRow("tabLabel", localizations.TabLabel(1, 2)),
                    BuildRow("aboutListTileTitle", localizations.AboutListTileTitle("Plumix")),
                    BuildRow("reorderItemUp", widgets.ReorderItemUp),
                ]));
    }

    private static Widget BuildRow(string label, string value)
    {
        return new Row(
            crossAxisAlignment: CrossAxisAlignment.Start,
            spacing: 12.0,
            children:
            [
                new SizedBox(
                    width: 220.0,
                    child: new Text(label, fontSize: 13.0, color: Color.Parse("#8A000000"))),
                new Expanded(child: new Text(value, fontSize: 13.0, color: Colors.Black)),
            ]);
    }
}
