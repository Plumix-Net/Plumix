using System.Globalization;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/localizations.dart

public enum DatePickerDateTimeOrder
{
    DateTimeDayPeriod,
    DateDayPeriodTime,
    TimeDayPeriodDate,
    DayPeriodTimeDate,
}

public enum DatePickerDateOrder
{
    Dmy,
    Mdy,
    Ymd,
    Ydm,
}

public abstract class CupertinoLocalizations
{
    public abstract string DatePickerYear(int yearIndex);

    public abstract string DatePickerMonth(int monthIndex);

    public abstract string DatePickerStandaloneMonth(int monthIndex);

    public abstract string DatePickerDayOfMonth(int dayIndex, int? weekDay = null);

    public abstract string DatePickerMediumDate(DateTime date);

    public abstract string DatePickerHour(int hour);

    public abstract string? DatePickerHourSemanticsLabel(int hour);

    public abstract string DatePickerMinute(int minute);

    public abstract string? DatePickerMinuteSemanticsLabel(int minute);

    public abstract DatePickerDateOrder DatePickerDateOrder { get; }

    public abstract DatePickerDateTimeOrder DatePickerDateTimeOrder { get; }

    public abstract string AnteMeridiemAbbreviation { get; }

    public abstract string PostMeridiemAbbreviation { get; }

    public abstract string TodayLabel { get; }

    public abstract string AlertDialogLabel { get; }

    public abstract string TabSemanticsLabel(int tabIndex, int tabCount);

    public abstract string TimerPickerHour(int hour);

    public abstract string TimerPickerMinute(int minute);

    public abstract string TimerPickerSecond(int second);

    public abstract string? TimerPickerHourLabel(int hour);

    public abstract IReadOnlyList<string> TimerPickerHourLabels { get; }

    public abstract string? TimerPickerMinuteLabel(int minute);

    public abstract IReadOnlyList<string> TimerPickerMinuteLabels { get; }

    public abstract string? TimerPickerSecondLabel(int second);

    public abstract IReadOnlyList<string> TimerPickerSecondLabels { get; }

    public abstract string CutButtonLabel { get; }

    public abstract string CopyButtonLabel { get; }

    public abstract string PasteButtonLabel { get; }

    public abstract string ClearButtonLabel { get; }

    public abstract string NoSpellCheckReplacementsLabel { get; }

    public abstract string SelectAllButtonLabel { get; }

    public abstract string LookUpButtonLabel { get; }

    public abstract string SearchWebButtonLabel { get; }

    public abstract string ShareButtonLabel { get; }

    public abstract string SearchTextFieldPlaceholderLabel { get; }

    public abstract string ModalBarrierDismissLabel { get; }

    public abstract string MenuDismissLabel { get; }

    public abstract string CancelButtonLabel { get; }

    public abstract string BackButtonLabel { get; }

    public virtual string ExpansionTileExpandedHint => "double tap to collapse";

    public virtual string ExpansionTileCollapsedHint => "double tap to expand";

    public virtual string ExpansionTileExpandedTapHint => "Collapse";

    public virtual string ExpansionTileCollapsedTapHint => "Expand for more details";

    public virtual string ExpandedHint => "Collapsed";

    public virtual string CollapsedHint => "Expanded";

    public static CupertinoLocalizations Of(BuildContext context)
    {
        return Localizations.MaybeOf<CupertinoLocalizations>(context)
               ?? throw new InvalidOperationException("No CupertinoLocalizations found.");
    }
}

public class DefaultCupertinoLocalizations : CupertinoLocalizations
{
    private static readonly IReadOnlyList<string> ShortWeekdays =
        Array.AsReadOnly<string>(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]);

    private static readonly IReadOnlyList<string> ShortMonths =
        Array.AsReadOnly<string>(
            ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]);

    private static readonly IReadOnlyList<string> Months =
        Array.AsReadOnly<string>(
            [
                "January",
                "February",
                "March",
                "April",
                "May",
                "June",
                "July",
                "August",
                "September",
                "October",
                "November",
                "December",
            ]);

    private static readonly IReadOnlyList<string> HourLabels = Array.AsReadOnly<string>(["hour", "hours"]);
    private static readonly IReadOnlyList<string> MinuteLabels = Array.AsReadOnly<string>(["min."]);
    private static readonly IReadOnlyList<string> SecondLabels = Array.AsReadOnly<string>(["sec."]);

    public override string DatePickerYear(int yearIndex) => yearIndex.ToString(CultureInfo.InvariantCulture);

    public override string DatePickerMonth(int monthIndex) => Months[monthIndex - 1];

    public override string DatePickerStandaloneMonth(int monthIndex) => Months[monthIndex - 1];

    public override string DatePickerDayOfMonth(int dayIndex, int? weekDay = null)
    {
        return weekDay == null
            ? dayIndex.ToString(CultureInfo.InvariantCulture)
            : $" {ShortWeekdays[weekDay.Value - 1]} {dayIndex.ToString(CultureInfo.InvariantCulture)} ";
    }

    public override string DatePickerMediumDate(DateTime date)
    {
        int weekdayIndex = ((int)date.DayOfWeek + 6) % 7;
        string day = date.Day.ToString(CultureInfo.InvariantCulture).PadRight(2);
        return $"{ShortWeekdays[weekdayIndex]} {ShortMonths[date.Month - 1]} {day}";
    }

    public override string DatePickerHour(int hour) => hour.ToString(CultureInfo.InvariantCulture);

    public override string DatePickerHourSemanticsLabel(int hour) =>
        $"{hour.ToString(CultureInfo.InvariantCulture)} o'clock";

    public override string DatePickerMinute(int minute) => minute.ToString("00", CultureInfo.InvariantCulture);

    public override string DatePickerMinuteSemanticsLabel(int minute) =>
        minute == 1
            ? "1 minute"
            : $"{minute.ToString(CultureInfo.InvariantCulture)} minutes";

    public override DatePickerDateOrder DatePickerDateOrder => DatePickerDateOrder.Mdy;

    public override DatePickerDateTimeOrder DatePickerDateTimeOrder => DatePickerDateTimeOrder.DateTimeDayPeriod;

    public override string AnteMeridiemAbbreviation => "AM";

    public override string PostMeridiemAbbreviation => "PM";

    public override string TodayLabel => "Today";

    public override string AlertDialogLabel => "Alert";

    public override string TabSemanticsLabel(int tabIndex, int tabCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tabIndex, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabCount, 1);
        string index = tabIndex.ToString(CultureInfo.InvariantCulture);
        string count = tabCount.ToString(CultureInfo.InvariantCulture);
        return $"Tab {index} of {count}";
    }

    public override string TimerPickerHour(int hour) => hour.ToString(CultureInfo.InvariantCulture);

    public override string TimerPickerMinute(int minute) => minute.ToString(CultureInfo.InvariantCulture);

    public override string TimerPickerSecond(int second) => second.ToString(CultureInfo.InvariantCulture);

    public override string TimerPickerHourLabel(int hour) => hour == 1 ? "hour" : "hours";

    public override IReadOnlyList<string> TimerPickerHourLabels => HourLabels;

    public override string TimerPickerMinuteLabel(int minute) => "min.";

    public override IReadOnlyList<string> TimerPickerMinuteLabels => MinuteLabels;

    public override string TimerPickerSecondLabel(int second) => "sec.";

    public override IReadOnlyList<string> TimerPickerSecondLabels => SecondLabels;

    public override string CutButtonLabel => "Cut";

    public override string CopyButtonLabel => "Copy";

    public override string PasteButtonLabel => "Paste";

    public override string ClearButtonLabel => "Clear";

    public override string NoSpellCheckReplacementsLabel => "No Replacements Found";

    public override string SelectAllButtonLabel => "Select All";

    public override string LookUpButtonLabel => "Look Up";

    public override string SearchWebButtonLabel => "Search Web";

    public override string ShareButtonLabel => "Share...";

    public override string SearchTextFieldPlaceholderLabel => "Search";

    public override string ModalBarrierDismissLabel => "Dismiss";

    public override string MenuDismissLabel => "Dismiss menu";

    public override string CancelButtonLabel => "Cancel";

    public override string BackButtonLabel => "Back";

    public override string ExpansionTileExpandedHint => "double tap to collapse";

    public override string ExpansionTileCollapsedHint => "double tap to expand";

    public override string ExpansionTileExpandedTapHint => "Collapse";

    public override string ExpansionTileCollapsedTapHint => "Expand for more details";

    public override string ExpandedHint => "Collapsed";

    public override string CollapsedHint => "Expanded";

    public static CupertinoLocalizations Load(Locale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        return Instance;
    }

    public static DefaultCupertinoLocalizations Instance { get; } = new();

    public static LocalizationsDelegate<CupertinoLocalizations> Delegate { get; } =
        new DefaultCupertinoLocalizationsDelegate();

    private sealed class DefaultCupertinoLocalizationsDelegate : LocalizationsDelegate<CupertinoLocalizations>
    {
        public override bool IsSupported(Locale locale) => locale.LanguageCode == "en";

        public override CupertinoLocalizations LoadTyped(Locale locale) => DefaultCupertinoLocalizations.Load(locale);

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;

        public override string ToString() => "DefaultCupertinoLocalizations.delegate(en_US)";
    }
}
