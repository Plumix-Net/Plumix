using Plumix.Foundation.Intl;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/global_cupertino_localizations.dart

/// <summary>
/// Implementation of localized strings for Cupertino widgets, using Plumix's <c>intl</c> subset for
/// date and time formatting.
/// </summary>
/// <remarks>
/// Further localization of strings beyond date time formatting is provided by language specific
/// subclasses of <see cref="GlobalCupertinoLocalizations"/>, generated into
/// <c>GlobalCupertinoLocalizations.g.cs</c>. The supported languages are
/// <see cref="CupertinoSupportedLanguages"/>.
/// </remarks>
public abstract partial class GlobalCupertinoLocalizations : CupertinoLocalizations
{
    private readonly string localeName;
    private readonly DateFormat fullYearFormat;
    private readonly DateFormat dayFormat;
    private readonly DateFormat weekdayFormat;
    private readonly DateFormat mediumDateFormat;
    private readonly DateFormat singleDigitHourFormat;
    private readonly DateFormat singleDigitMinuteFormat;
    private readonly DateFormat doubleDigitMinuteFormat;
    private readonly DateFormat singleDigitSecondFormat;
    private readonly NumberFormat decimalFormat;

    /// <summary>
    /// Initializes an object that defines the Cupertino widgets' localized strings for the given
    /// <paramref name="localeName"/>; the remaining arguments provide that locale's formats.
    /// </summary>
    protected GlobalCupertinoLocalizations(
        string localeName,
        DateFormat fullYearFormat,
        DateFormat dayFormat,
        DateFormat weekdayFormat,
        DateFormat mediumDateFormat,
        DateFormat singleDigitHourFormat,
        DateFormat singleDigitMinuteFormat,
        DateFormat doubleDigitMinuteFormat,
        DateFormat singleDigitSecondFormat,
        NumberFormat decimalFormat)
    {
        this.localeName = localeName;
        this.fullYearFormat = fullYearFormat;
        this.dayFormat = dayFormat;
        this.weekdayFormat = weekdayFormat;
        this.mediumDateFormat = mediumDateFormat;
        this.singleDigitHourFormat = singleDigitHourFormat;
        this.singleDigitMinuteFormat = singleDigitMinuteFormat;
        this.doubleDigitMinuteFormat = doubleDigitMinuteFormat;
        this.singleDigitSecondFormat = singleDigitSecondFormat;
        this.decimalFormat = decimalFormat;
    }

    /// <summary>A <see cref="LocalizationsDelegate{T}"/> for <see cref="CupertinoLocalizations"/>.</summary>
    public static LocalizationsDelegate<CupertinoLocalizations> Delegate { get; } =
        new GlobalCupertinoLocalizationsDelegate();

    /// <summary>
    /// A value for <see cref="CupertinoApp.LocalizationsDelegates"/> that's typically used by
    /// internationalized apps: this delegate plus the global widgets delegate.
    /// </summary>
    public static IReadOnlyList<LocalizationsDelegate> Delegates { get; } =
    [
        Delegate,
        GlobalWidgetsLocalizations.Delegate,
    ];

    public override string DatePickerYear(int yearIndex) =>
        fullYearFormat.Format(DartDateTime.Utc(yearIndex));

    public override string DatePickerMonth(int monthIndex)
    {
        // It doesn't actually have anything to do with fullYearFormat. It's just taking advantage of
        // the fact that fullYearFormat loaded the needed locale's symbols.
        return fullYearFormat.DateSymbols.Months[monthIndex - 1];
    }

    public override string DatePickerStandaloneMonth(int monthIndex)
    {
        // Because this will be used without specifying any day of month, in most cases it should be
        // capitalized (according to rules in specific language).
        string month = fullYearFormat.DateSymbols.StandaloneMonths[monthIndex - 1];
        return Intl.ToBeginningOfSentenceCase(month) ?? month;
    }

    public override string DatePickerDayOfMonth(int dayIndex, int? weekDay = null)
    {
        if (weekDay != null)
        {
            string weekdayText = weekdayFormat.Format(DartDateTime.Utc(1, 1, weekDay.Value));
            return $"{weekdayText} {dayFormat.Format(DartDateTime.Utc(1, 1, dayIndex))}";
        }

        // Year and month doesn't matter since we just want the day formatted.
        return dayFormat.Format(DartDateTime.Utc(0, 0, dayIndex));
    }

    public override string DatePickerMediumDate(DateTime date) => mediumDateFormat.Format(date);

    public override string DatePickerHour(int hour) =>
        singleDigitHourFormat.Format(DartDateTime.Utc(0, 0, 0, hour));

    public override string DatePickerMinute(int minute) =>
        doubleDigitMinuteFormat.Format(DartDateTime.Utc(0, 0, 0, 0, minute));

    /// Subclasses should provide the optional zero pluralization of [DatePickerHourSemanticsLabel].
    protected virtual string? DatePickerHourSemanticsLabelZero => null;

    /// Subclasses should provide the optional one pluralization of [DatePickerHourSemanticsLabel].
    protected virtual string? DatePickerHourSemanticsLabelOne => null;

    /// Subclasses should provide the optional two pluralization of [DatePickerHourSemanticsLabel].
    protected virtual string? DatePickerHourSemanticsLabelTwo => null;

    /// Subclasses should provide the optional few pluralization of [DatePickerHourSemanticsLabel].
    protected virtual string? DatePickerHourSemanticsLabelFew => null;

    /// Subclasses should provide the optional many pluralization of [DatePickerHourSemanticsLabel].
    protected virtual string? DatePickerHourSemanticsLabelMany => null;

    /// Subclasses should provide the required other pluralization of [DatePickerHourSemanticsLabel].
    protected abstract string? DatePickerHourSemanticsLabelOther { get; }

    public override string? DatePickerHourSemanticsLabel(int hour) => ReplaceFirst(
        Intl.PluralLogic(
            hour,
            zero: DatePickerHourSemanticsLabelZero,
            one: DatePickerHourSemanticsLabelOne,
            two: DatePickerHourSemanticsLabelTwo,
            few: DatePickerHourSemanticsLabelFew,
            many: DatePickerHourSemanticsLabelMany,
            other: DatePickerHourSemanticsLabelOther,
            locale: localeName),
        "$hour",
        decimalFormat.Format(hour));

    /// Subclasses should provide the optional zero pluralization of [DatePickerMinuteSemanticsLabel].
    protected virtual string? DatePickerMinuteSemanticsLabelZero => null;

    /// Subclasses should provide the optional one pluralization of [DatePickerMinuteSemanticsLabel].
    protected virtual string? DatePickerMinuteSemanticsLabelOne => null;

    /// Subclasses should provide the optional two pluralization of [DatePickerMinuteSemanticsLabel].
    protected virtual string? DatePickerMinuteSemanticsLabelTwo => null;

    /// Subclasses should provide the optional few pluralization of [DatePickerMinuteSemanticsLabel].
    protected virtual string? DatePickerMinuteSemanticsLabelFew => null;

    /// Subclasses should provide the optional many pluralization of [DatePickerMinuteSemanticsLabel].
    protected virtual string? DatePickerMinuteSemanticsLabelMany => null;

    /// Subclasses should provide the required other pluralization of [DatePickerMinuteSemanticsLabel].
    protected abstract string? DatePickerMinuteSemanticsLabelOther { get; }

    public override string? DatePickerMinuteSemanticsLabel(int minute) => ReplaceFirst(
        Intl.PluralLogic(
            minute,
            zero: DatePickerMinuteSemanticsLabelZero,
            one: DatePickerMinuteSemanticsLabelOne,
            two: DatePickerMinuteSemanticsLabelTwo,
            few: DatePickerMinuteSemanticsLabelFew,
            many: DatePickerMinuteSemanticsLabelMany,
            other: DatePickerMinuteSemanticsLabelOther,
            locale: localeName),
        "$minute",
        decimalFormat.Format(minute));

    /// <summary>
    /// A string describing the <see cref="Cupertino.DatePickerDateOrder"/> value, from the ARB file.
    /// </summary>
    protected abstract string DatePickerDateOrderString { get; }

    public override DatePickerDateOrder DatePickerDateOrder => DatePickerDateOrderString switch
    {
        "dmy" => DatePickerDateOrder.Dmy,
        "mdy" => DatePickerDateOrder.Mdy,
        "ymd" => DatePickerDateOrder.Ymd,
        "ydm" => DatePickerDateOrder.Ydm,
        _ => throw new InvalidOperationException(
            $"Failed to load DatePickerDateOrder {DatePickerDateOrderString} for locale {localeName}."
            + $"\nNon conforming string for {localeName}'s .arb file"),
    };

    /// <summary>
    /// A string describing the <see cref="Cupertino.DatePickerDateTimeOrder"/> value, from the ARB file.
    /// </summary>
    protected abstract string DatePickerDateTimeOrderString { get; }

    public override DatePickerDateTimeOrder DatePickerDateTimeOrder => DatePickerDateTimeOrderString switch
    {
        "date_time_dayPeriod" => DatePickerDateTimeOrder.DateTimeDayPeriod,
        "date_dayPeriod_time" => DatePickerDateTimeOrder.DateDayPeriodTime,
        "time_dayPeriod_date" => DatePickerDateTimeOrder.TimeDayPeriodDate,
        "dayPeriod_time_date" => DatePickerDateTimeOrder.DayPeriodTimeDate,
        _ => throw new InvalidOperationException(
            $"Failed to load DatePickerDateTimeOrder {DatePickerDateTimeOrderString} for locale "
            + $"{localeName}.\nNon conforming string for {localeName}'s .arb file"),
    };

    /// <summary>
    /// The raw version of <see cref="TabSemanticsLabel"/>, with <c>$tabIndex</c> and <c>$tabCount</c>
    /// verbatim in the string.
    /// </summary>
    protected abstract string TabSemanticsLabelRaw { get; }

    public override string TabSemanticsLabel(int tabIndex, int tabCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tabIndex, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabCount, 1);
        string template = ReplaceFirst(TabSemanticsLabelRaw, "$tabIndex", decimalFormat.Format(tabIndex))!;
        return ReplaceFirst(template, "$tabCount", decimalFormat.Format(tabCount))!;
    }

    public override string TimerPickerHour(int hour) =>
        singleDigitHourFormat.Format(DartDateTime.Utc(0, 0, 0, hour));

    public override string TimerPickerMinute(int minute) =>
        singleDigitMinuteFormat.Format(DartDateTime.Utc(0, 0, 0, 0, minute));

    public override string TimerPickerSecond(int second) =>
        singleDigitSecondFormat.Format(DartDateTime.Utc(0, 0, 0, 0, 0, second));

    /// Subclasses should provide the optional zero pluralization of [TimerPickerHourLabel].
    protected virtual string? TimerPickerHourLabelZero => null;

    /// Subclasses should provide the optional one pluralization of [TimerPickerHourLabel].
    protected virtual string? TimerPickerHourLabelOne => null;

    /// Subclasses should provide the optional two pluralization of [TimerPickerHourLabel].
    protected virtual string? TimerPickerHourLabelTwo => null;

    /// Subclasses should provide the optional few pluralization of [TimerPickerHourLabel].
    protected virtual string? TimerPickerHourLabelFew => null;

    /// Subclasses should provide the optional many pluralization of [TimerPickerHourLabel].
    protected virtual string? TimerPickerHourLabelMany => null;

    /// Subclasses should provide the required other pluralization of [TimerPickerHourLabel].
    protected abstract string? TimerPickerHourLabelOther { get; }

    public override string? TimerPickerHourLabel(int hour) => ReplaceFirst(
        Intl.PluralLogic(
            hour,
            zero: TimerPickerHourLabelZero,
            one: TimerPickerHourLabelOne,
            two: TimerPickerHourLabelTwo,
            few: TimerPickerHourLabelFew,
            many: TimerPickerHourLabelMany,
            other: TimerPickerHourLabelOther,
            locale: localeName),
        "$hour",
        decimalFormat.Format(hour));

    public override IReadOnlyList<string> TimerPickerHourLabels => Labels(
        TimerPickerHourLabelZero,
        TimerPickerHourLabelOne,
        TimerPickerHourLabelTwo,
        TimerPickerHourLabelFew,
        TimerPickerHourLabelMany,
        TimerPickerHourLabelOther);

    /// Subclasses should provide the optional zero pluralization of [TimerPickerMinuteLabel].
    protected virtual string? TimerPickerMinuteLabelZero => null;

    /// Subclasses should provide the optional one pluralization of [TimerPickerMinuteLabel].
    protected virtual string? TimerPickerMinuteLabelOne => null;

    /// Subclasses should provide the optional two pluralization of [TimerPickerMinuteLabel].
    protected virtual string? TimerPickerMinuteLabelTwo => null;

    /// Subclasses should provide the optional few pluralization of [TimerPickerMinuteLabel].
    protected virtual string? TimerPickerMinuteLabelFew => null;

    /// Subclasses should provide the optional many pluralization of [TimerPickerMinuteLabel].
    protected virtual string? TimerPickerMinuteLabelMany => null;

    /// Subclasses should provide the required other pluralization of [TimerPickerMinuteLabel].
    protected abstract string? TimerPickerMinuteLabelOther { get; }

    public override string? TimerPickerMinuteLabel(int minute) => ReplaceFirst(
        Intl.PluralLogic(
            minute,
            zero: TimerPickerMinuteLabelZero,
            one: TimerPickerMinuteLabelOne,
            two: TimerPickerMinuteLabelTwo,
            few: TimerPickerMinuteLabelFew,
            many: TimerPickerMinuteLabelMany,
            other: TimerPickerMinuteLabelOther,
            locale: localeName),
        "$minute",
        decimalFormat.Format(minute));

    public override IReadOnlyList<string> TimerPickerMinuteLabels => Labels(
        TimerPickerMinuteLabelZero,
        TimerPickerMinuteLabelOne,
        TimerPickerMinuteLabelTwo,
        TimerPickerMinuteLabelFew,
        TimerPickerMinuteLabelMany,
        TimerPickerMinuteLabelOther);

    /// Subclasses should provide the optional zero pluralization of [TimerPickerSecondLabel].
    protected virtual string? TimerPickerSecondLabelZero => null;

    /// Subclasses should provide the optional one pluralization of [TimerPickerSecondLabel].
    protected virtual string? TimerPickerSecondLabelOne => null;

    /// Subclasses should provide the optional two pluralization of [TimerPickerSecondLabel].
    protected virtual string? TimerPickerSecondLabelTwo => null;

    /// Subclasses should provide the optional few pluralization of [TimerPickerSecondLabel].
    protected virtual string? TimerPickerSecondLabelFew => null;

    /// Subclasses should provide the optional many pluralization of [TimerPickerSecondLabel].
    protected virtual string? TimerPickerSecondLabelMany => null;

    /// Subclasses should provide the required other pluralization of [TimerPickerSecondLabel].
    protected abstract string? TimerPickerSecondLabelOther { get; }

    public override string? TimerPickerSecondLabel(int second) => ReplaceFirst(
        Intl.PluralLogic(
            second,
            zero: TimerPickerSecondLabelZero,
            one: TimerPickerSecondLabelOne,
            two: TimerPickerSecondLabelTwo,
            few: TimerPickerSecondLabelFew,
            many: TimerPickerSecondLabelMany,
            other: TimerPickerSecondLabelOther,
            locale: localeName),
        "$second",
        decimalFormat.Format(second));

    public override IReadOnlyList<string> TimerPickerSecondLabels => Labels(
        TimerPickerSecondLabelZero,
        TimerPickerSecondLabelOne,
        TimerPickerSecondLabelTwo,
        TimerPickerSecondLabelFew,
        TimerPickerSecondLabelMany,
        TimerPickerSecondLabelOther);

    /// Dart's `String.replaceFirst`, which has no direct C# counterpart.
    private static string? ReplaceFirst(string? source, string pattern, string replacement)
    {
        if (source == null)
        {
            return null;
        }

        int index = source.IndexOf(pattern, StringComparison.Ordinal);
        return index < 0 ? source : source[..index] + replacement + source[(index + pattern.Length)..];
    }

    private static IReadOnlyList<string> Labels(params string?[] candidates) =>
        candidates.Where(label => label != null).Select(label => label!).ToArray();

    private sealed class GlobalCupertinoLocalizationsDelegate : LocalizationsDelegate<CupertinoLocalizations>
    {
        private static readonly Dictionary<Locale, CupertinoLocalizations> LoadedTranslations = new();

        public override bool IsSupported(Locale locale) =>
            CupertinoSupportedLanguages.Contains(locale.LanguageCode);

        public override CupertinoLocalizations LoadTyped(Locale locale)
        {
            lock (LoadedTranslations)
            {
                if (LoadedTranslations.TryGetValue(locale, out CupertinoLocalizations? loaded))
                {
                    return loaded;
                }

                string localeName = Intl.CanonicalizedLocale(locale.Name.Replace('-', '_'));

                DateFormat fullYearFormat;
                DateFormat dayFormat;
                DateFormat weekdayFormat;
                DateFormat mediumDateFormat;

                // We don't want any additional decoration here. The am/pm is handled in the date
                // picker. We just want an hour number localized.
                DateFormat singleDigitHourFormat;
                DateFormat singleDigitMinuteFormat;
                DateFormat doubleDigitMinuteFormat;
                DateFormat singleDigitSecondFormat;
                NumberFormat decimalFormat;

                string? formatsLocale;
                if (DateFormat.LocaleExists(localeName))
                {
                    formatsLocale = localeName;
                }
                else if (DateFormat.LocaleExists(locale.LanguageCode))
                {
                    formatsLocale = locale.LanguageCode;
                }
                else
                {
                    formatsLocale = null;
                }

                fullYearFormat = new DateFormat("y", formatsLocale);
                dayFormat = new DateFormat("d", formatsLocale);
                weekdayFormat = new DateFormat("E", formatsLocale);
                mediumDateFormat = new DateFormat("MMMEd", formatsLocale);
                singleDigitHourFormat = new DateFormat("HH", formatsLocale);
                singleDigitMinuteFormat = new DateFormat("m", formatsLocale);
                doubleDigitMinuteFormat = new DateFormat("mm", formatsLocale);
                singleDigitSecondFormat = new DateFormat("s", formatsLocale);
                decimalFormat = NumberFormat.DecimalPattern(formatsLocale);

                CupertinoLocalizations translation = GetCupertinoTranslation(
                    locale,
                    fullYearFormat,
                    dayFormat,
                    weekdayFormat,
                    mediumDateFormat,
                    singleDigitHourFormat,
                    singleDigitMinuteFormat,
                    doubleDigitMinuteFormat,
                    singleDigitSecondFormat,
                    decimalFormat)
                    ?? throw new InvalidOperationException(
                        $"GetCupertinoTranslation() called for unsupported locale \"{locale}\"");

                LoadedTranslations[locale] = translation;
                return translation;
            }
        }

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;

        public override string ToString() =>
            $"GlobalCupertinoLocalizations.delegate({CupertinoSupportedLanguages.Count} locales)";
    }
}
