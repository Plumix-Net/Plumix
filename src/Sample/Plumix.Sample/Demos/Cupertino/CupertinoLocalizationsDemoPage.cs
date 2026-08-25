using System;
using System.Collections.Generic;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_localizations_demo_page.dart
// (exact sample parity)

public sealed class CupertinoLocalizationsDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoLocalizationsDemoPageState();
}

internal sealed class CupertinoLocalizationsDemoPageState : State
{
    private static readonly IReadOnlyList<(string Label, Locale Locale)> Locales =
    [
        ("English", new Locale("en")),
        ("Français", new Locale("fr")),
        ("Русский", new Locale("ru")),
        ("中文", new Locale("zh")),
        ("العربية", new Locale("ar")),
    ];

    private static readonly DateTime SampleDate = new(2019, 3, 25);

    private int _selected;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino localizations", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "GlobalCupertinoLocalizations.Delegates translates every Cupertino string and "
                    + "formats dates with the locale's own calendar data.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Wrap(spacing: 8.0, runSpacing: 8.0, children: BuildLocaleButtons()),
                new Localizations(
                    locale: Locales[_selected].Locale,
                    delegates: GlobalCupertinoLocalizations.Delegates,
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
                            Color: selected ? Color.Parse("#FF007AFF") : Color.Parse("#FFF2F2F7"),
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
        CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
        WidgetsLocalizations widgets = WidgetsLocalizations.Of(context);

        return new Container(
            padding: EdgeInsets.All(16.0),
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFF2F2F7"),
                BorderRadius: BorderRadius.Circular(12.0)),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 6.0,
                children:
                [
                    BuildRow("textDirection", widgets.TextDirection.ToString()),
                    BuildRow("alertDialogLabel", localizations.AlertDialogLabel),
                    BuildRow(
                        "toolbar",
                        string.Join(
                            " · ",
                            localizations.CutButtonLabel,
                            localizations.CopyButtonLabel,
                            localizations.PasteButtonLabel,
                            localizations.SelectAllButtonLabel)),
                    BuildRow("datePickerMediumDate", localizations.DatePickerMediumDate(SampleDate)),
                    BuildRow(
                        "datePickerMonth / standalone",
                        $"{localizations.DatePickerMonth(5)} / {localizations.DatePickerStandaloneMonth(5)}"),
                    BuildRow(
                        "datePickerDayOfMonth",
                        $"{localizations.DatePickerDayOfMonth(1)} · {localizations.DatePickerDayOfMonth(1, 2)}"),
                    BuildRow(
                        "datePickerHourSemanticsLabel",
                        $"{localizations.DatePickerHourSemanticsLabel(1)} · "
                        + $"{localizations.DatePickerHourSemanticsLabel(12)}"),
                    BuildRow(
                        "timerPicker",
                        $"{localizations.TimerPickerMinute(10)} "
                        + $"{localizations.TimerPickerMinuteLabel(10)} · "
                        + $"{localizations.TimerPickerHour(1)} {localizations.TimerPickerHourLabel(1)}"),
                    BuildRow("tabSemanticsLabel", localizations.TabSemanticsLabel(1, 2)),
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
