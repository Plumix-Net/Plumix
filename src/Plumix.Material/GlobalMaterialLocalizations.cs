using Plumix.Cupertino;
using Plumix.Foundation.Intl;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/global_material_localizations.dart

/// <summary>
/// Implementation of localized strings for the Material widgets, using Plumix's <c>intl</c> subset
/// for date, time and number formatting.
/// </summary>
/// <remarks>
/// Further localization of strings beyond date/time formatting is provided by language specific
/// subclasses of <see cref="GlobalMaterialLocalizations"/>, generated into
/// <c>GlobalMaterialLocalizations.g.cs</c>. The supported languages are
/// <see cref="MaterialSupportedLanguages"/>.
/// </remarks>
public abstract partial class GlobalMaterialLocalizations : MaterialLocalizations
{
    private readonly string localeName;
    private readonly DateFormat fullYearFormat;
    private readonly DateFormat compactDateFormat;
    private readonly DateFormat shortDateFormat;
    private readonly DateFormat mediumDateFormat;
    private readonly DateFormat longDateFormat;
    private readonly DateFormat yearMonthFormat;
    private readonly DateFormat shortMonthDayFormat;
    private readonly NumberFormat decimalFormat;
    private readonly NumberFormat twoDigitZeroPaddedFormat;

    /// <summary>
    /// Initializes an object that defines the Material widgets' localized strings for the given
    /// <paramref name="localeName"/>; the remaining arguments provide that locale's formats.
    /// </summary>
    protected GlobalMaterialLocalizations(
        string localeName,
        DateFormat fullYearFormat,
        DateFormat compactDateFormat,
        DateFormat shortDateFormat,
        DateFormat mediumDateFormat,
        DateFormat longDateFormat,
        DateFormat yearMonthFormat,
        DateFormat shortMonthDayFormat,
        NumberFormat decimalFormat,
        NumberFormat twoDigitZeroPaddedFormat)
    {
        this.localeName = localeName;
        this.fullYearFormat = fullYearFormat;
        this.compactDateFormat = compactDateFormat;
        this.shortDateFormat = shortDateFormat;
        this.mediumDateFormat = mediumDateFormat;
        this.longDateFormat = longDateFormat;
        this.yearMonthFormat = yearMonthFormat;
        this.shortMonthDayFormat = shortMonthDayFormat;
        this.decimalFormat = decimalFormat;
        this.twoDigitZeroPaddedFormat = twoDigitZeroPaddedFormat;
    }

    /// <summary>A <see cref="LocalizationsDelegate{T}"/> for <see cref="MaterialLocalizations"/>.</summary>
    public static LocalizationsDelegate<MaterialLocalizations> Delegate { get; } =
        new GlobalMaterialLocalizationsDelegate();

    /// <summary>
    /// A value for <see cref="MaterialApp.LocalizationsDelegates"/> that's typically used by
    /// internationalized apps: the Cupertino, Material and widgets delegates.
    /// </summary>
    public static IReadOnlyList<LocalizationsDelegate> Delegates { get; } =
    [
        GlobalCupertinoLocalizations.Delegate,
        Delegate,
        GlobalWidgetsLocalizations.Delegate,
    ];

    public override string FormatHour(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
    {
        switch (TimeOfDay.HourFormatOf(TimeOfDayFormat(alwaysUse24HourFormat)))
        {
            case HourFormat.HH:
                return twoDigitZeroPaddedFormat.Format(timeOfDay.Hour);
            case HourFormat.H:
                return FormatDecimal(timeOfDay.Hour);
            default:
                // `TimeOfDay.HourOfPeriod` already reports 12 where Dart reports 0.
                return FormatDecimal(timeOfDay.HourOfPeriod);
        }
    }

    public override string FormatMinute(TimeOfDay timeOfDay) =>
        twoDigitZeroPaddedFormat.Format(timeOfDay.Minute);

    public override string FormatYear(DateTime date) => fullYearFormat.Format(date);

    public override string FormatCompactDate(DateTime date) => compactDateFormat.Format(date);

    public override string FormatShortDate(DateTime date) => shortDateFormat.Format(date);

    public override string FormatMediumDate(DateTime date) => mediumDateFormat.Format(date);

    public override string FormatFullDate(DateTime date) => longDateFormat.Format(date);

    public override string FormatMonthYear(DateTime date) => yearMonthFormat.Format(date);

    public override string FormatShortMonthDay(DateTime date) => shortMonthDayFormat.Format(date);

    public override DateTime? ParseCompactDate(string? inputString)
    {
        if (inputString == null)
        {
            return null;
        }

        try
        {
            return compactDateFormat.ParseStrict(inputString).ToDateTime();
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public override IReadOnlyList<string> NarrowWeekdays => longDateFormat.DateSymbols.NarrowWeekdays;

    public override int FirstDayOfWeekIndex => (longDateFormat.DateSymbols.FirstDayOfWeek + 1) % 7;

    public override string FormatDecimal(int number) => decimalFormat.Format(number);

    public override string FormatTimeOfDay(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
    {
        // Not using DateFormat for two reasons:
        //
        // - DateFormat supports more formats than our material time picker does, and we want to be
        //   consistent across time picker format and the string formatting of the time of day.
        // - DateFormat operates on DateTime, which is sensitive to time eras and time zones, while
        //   here we want to format hour and minute within one day no matter what date the day falls
        //   on.
        string hour = FormatHour(timeOfDay, alwaysUse24HourFormat);
        string minute = FormatMinute(timeOfDay);
        return TimeOfDayFormat(alwaysUse24HourFormat) switch
        {
            global::Plumix.Material.TimeOfDayFormat.HColonMmSpaceA =>
                $"{hour}:{minute} {FormatDayPeriod(timeOfDay)}",
            global::Plumix.Material.TimeOfDayFormat.HHDotMm => $"{hour}.{minute}",
            global::Plumix.Material.TimeOfDayFormat.ASpaceHColonMm =>
                $"{FormatDayPeriod(timeOfDay)} {hour}:{minute}",
            global::Plumix.Material.TimeOfDayFormat.FrenchCanadian => $"{hour} h {minute}",
            _ => $"{hour}:{minute}",
        };
    }

    /// <summary>
    /// The raw version of <see cref="DateRangeStartDateSemanticLabel"/>, with <c>$fullDate</c>
    /// verbatim in the string.
    /// </summary>
    protected abstract string DateRangeStartDateSemanticLabelRaw { get; }

    public override string DateRangeStartDateSemanticLabel(string formattedDate) =>
        ReplaceFirst(DateRangeStartDateSemanticLabelRaw, "$fullDate", formattedDate);

    /// <summary>
    /// The raw version of <see cref="DateRangeEndDateSemanticLabel"/>, with <c>$fullDate</c>
    /// verbatim in the string.
    /// </summary>
    protected abstract string DateRangeEndDateSemanticLabelRaw { get; }

    public override string DateRangeEndDateSemanticLabel(string formattedDate) =>
        ReplaceFirst(DateRangeEndDateSemanticLabelRaw, "$fullDate", formattedDate);

    /// <summary>
    /// The raw version of <see cref="ScrimOnTapHint"/>, with <c>$modalRouteContentName</c> verbatim
    /// in the string.
    /// </summary>
    protected abstract string ScrimOnTapHintRaw { get; }

    public override string ScrimOnTapHint(string modalRouteContentName) =>
        ReplaceFirst(ScrimOnTapHintRaw, "$modalRouteContentName", modalRouteContentName);

    /// <summary>
    /// The raw version of <see cref="AboutListTileTitle"/>, with <c>$applicationName</c> verbatim in
    /// the string.
    /// </summary>
    protected abstract string AboutListTileTitleRaw { get; }

    public override string AboutListTileTitle(string applicationName) =>
        ReplaceFirst(AboutListTileTitleRaw, "$applicationName", applicationName);

    /// <summary>
    /// The raw version of <see cref="PageRowsInfoTitle"/> for an approximate row count, with
    /// <c>$firstRow</c>, <c>$lastRow</c> and <c>$rowCount</c> verbatim in the string.
    /// </summary>
    protected abstract string PageRowsInfoTitleApproximateRaw { get; }

    /// <summary>
    /// The raw version of <see cref="PageRowsInfoTitle"/> for a precise row count, with
    /// <c>$firstRow</c>, <c>$lastRow</c> and <c>$rowCount</c> verbatim in the string.
    /// </summary>
    protected abstract string PageRowsInfoTitleRaw { get; }

    public override string PageRowsInfoTitle(
        int firstRow,
        int lastRow,
        int rowCount,
        bool rowCountIsApproximate)
    {
        string text = rowCountIsApproximate ? PageRowsInfoTitleApproximateRaw : PageRowsInfoTitleRaw;
        text = ReplaceFirst(text, "$firstRow", FormatDecimal(firstRow));
        text = ReplaceFirst(text, "$lastRow", FormatDecimal(lastRow));
        return ReplaceFirst(text, "$rowCount", FormatDecimal(rowCount));
    }

    /// <summary>
    /// The raw version of <see cref="TabLabel"/>, with <c>$tabIndex</c> and <c>$tabCount</c>
    /// verbatim in the string.
    /// </summary>
    protected abstract string TabLabelRaw { get; }

    public override string TabLabel(int tabIndex, int tabCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tabIndex, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabCount, 1);
        string template = ReplaceFirst(TabLabelRaw, "$tabIndex", FormatDecimal(tabIndex));
        return ReplaceFirst(template, "$tabCount", FormatDecimal(tabCount));
    }

    /// Subclasses should provide the optional zero pluralization of [SelectedRowCountTitle].
    protected virtual string? SelectedRowCountTitleZero => null;

    /// Subclasses should provide the optional one pluralization of [SelectedRowCountTitle].
    protected virtual string? SelectedRowCountTitleOne => null;

    /// Subclasses should provide the optional two pluralization of [SelectedRowCountTitle].
    protected virtual string? SelectedRowCountTitleTwo => null;

    /// Subclasses should provide the optional few pluralization of [SelectedRowCountTitle].
    protected virtual string? SelectedRowCountTitleFew => null;

    /// Subclasses should provide the optional many pluralization of [SelectedRowCountTitle].
    protected virtual string? SelectedRowCountTitleMany => null;

    /// Subclasses should provide the required other pluralization of [SelectedRowCountTitle].
    protected abstract string SelectedRowCountTitleOther { get; }

    public override string SelectedRowCountTitle(int selectedRowCount) => ReplaceFirst(
        Intl.PluralLogic(
            selectedRowCount,
            zero: SelectedRowCountTitleZero,
            one: SelectedRowCountTitleOne,
            two: SelectedRowCountTitleTwo,
            few: SelectedRowCountTitleFew,
            many: SelectedRowCountTitleMany,
            other: SelectedRowCountTitleOther,
            locale: localeName)!,
        "$selectedRowCount",
        FormatDecimal(selectedRowCount));

    /// <summary>The format to use for <see cref="TimeOfDayFormat"/>, from the ARB file.</summary>
    protected abstract global::Plumix.Material.TimeOfDayFormat TimeOfDayFormatRaw { get; }

    public override global::Plumix.Material.TimeOfDayFormat TimeOfDayFormat(
        bool alwaysUse24HourFormat = false) =>
        alwaysUse24HourFormat ? Get24HourVersionOf(TimeOfDayFormatRaw) : TimeOfDayFormatRaw;

    /// Subclasses should provide the optional zero pluralization of [LicensesPackageDetailText].
    protected virtual string? LicensesPackageDetailTextZero => null;

    /// Subclasses should provide the optional one pluralization of [LicensesPackageDetailText].
    protected virtual string? LicensesPackageDetailTextOne => null;

    /// Subclasses should provide the optional two pluralization of [LicensesPackageDetailText].
    protected virtual string? LicensesPackageDetailTextTwo => null;

    /// Subclasses should provide the optional many pluralization of [LicensesPackageDetailText].
    protected virtual string? LicensesPackageDetailTextMany => null;

    /// Subclasses should provide the optional few pluralization of [LicensesPackageDetailText].
    protected virtual string? LicensesPackageDetailTextFew => null;

    /// Subclasses should provide the required other pluralization of [LicensesPackageDetailText].
    protected abstract string LicensesPackageDetailTextOther { get; }

    public override string LicensesPackageDetailText(int licenseCount) => ReplaceFirst(
        Intl.PluralLogic(
            licenseCount,
            zero: LicensesPackageDetailTextZero,
            one: LicensesPackageDetailTextOne,
            two: LicensesPackageDetailTextTwo,
            few: LicensesPackageDetailTextFew,
            many: LicensesPackageDetailTextMany,
            other: LicensesPackageDetailTextOther,
            locale: localeName)!,
        "$licenseCount",
        FormatDecimal(licenseCount));

    /// Subclasses should provide the optional zero pluralization of [RemainingTextFieldCharacterCount].
    protected virtual string? RemainingTextFieldCharacterCountZero => null;

    /// Subclasses should provide the optional one pluralization of [RemainingTextFieldCharacterCount].
    protected virtual string? RemainingTextFieldCharacterCountOne => null;

    /// Subclasses should provide the optional two pluralization of [RemainingTextFieldCharacterCount].
    protected virtual string? RemainingTextFieldCharacterCountTwo => null;

    /// Subclasses should provide the optional many pluralization of [RemainingTextFieldCharacterCount].
    protected virtual string? RemainingTextFieldCharacterCountMany => null;

    /// Subclasses should provide the optional few pluralization of [RemainingTextFieldCharacterCount].
    protected virtual string? RemainingTextFieldCharacterCountFew => null;

    /// Subclasses should provide the required other pluralization of [RemainingTextFieldCharacterCount].
    protected abstract string RemainingTextFieldCharacterCountOther { get; }

    public override string RemainingTextFieldCharacterCount(int remaining) => ReplaceFirst(
        Intl.PluralLogic(
            remaining,
            zero: RemainingTextFieldCharacterCountZero,
            one: RemainingTextFieldCharacterCountOne,
            two: RemainingTextFieldCharacterCountTwo,
            few: RemainingTextFieldCharacterCountFew,
            many: RemainingTextFieldCharacterCountMany,
            other: RemainingTextFieldCharacterCountOther,
            locale: localeName)!,
        "$remainingCount",
        FormatDecimal(remaining));

    public abstract override ScriptCategory ScriptCategory { get; }

    private string FormatDayPeriod(TimeOfDay timeOfDay) =>
        timeOfDay.Period == DayPeriod.Am ? AnteMeridiemAbbreviation : PostMeridiemAbbreviation;

    /// Dart's `_get24HourVersionOf`.
    private static global::Plumix.Material.TimeOfDayFormat Get24HourVersionOf(
        global::Plumix.Material.TimeOfDayFormat original) => original switch
    {
        global::Plumix.Material.TimeOfDayFormat.HColonMmSpaceA
            or global::Plumix.Material.TimeOfDayFormat.ASpaceHColonMm =>
            global::Plumix.Material.TimeOfDayFormat.HHColonMm,
        _ => original,
    };

    /// Dart's `String.replaceFirst`, which has no direct C# counterpart.
    private static string ReplaceFirst(string source, string pattern, string replacement)
    {
        int index = source.IndexOf(pattern, StringComparison.Ordinal);
        return index < 0 ? source : source[..index] + replacement + source[(index + pattern.Length)..];
    }

    private sealed class GlobalMaterialLocalizationsDelegate : LocalizationsDelegate<MaterialLocalizations>
    {
        private static readonly Dictionary<Locale, MaterialLocalizations> LoadedTranslations = new();

        public override bool IsSupported(Locale locale) =>
            MaterialSupportedLanguages.Contains(locale.LanguageCode);

        public override MaterialLocalizations LoadTyped(Locale locale)
        {
            lock (LoadedTranslations)
            {
                if (LoadedTranslations.TryGetValue(locale, out MaterialLocalizations? loaded))
                {
                    return loaded;
                }

                string localeName = Intl.CanonicalizedLocale(locale.Name.Replace('-', '_'));

                string? datesLocale;
                if (DateFormat.LocaleExists(localeName))
                {
                    datesLocale = localeName;
                }
                else if (DateFormat.LocaleExists(locale.LanguageCode))
                {
                    datesLocale = locale.LanguageCode;
                }
                else
                {
                    datesLocale = null;
                }

                var fullYearFormat = new DateFormat("y", datesLocale);
                var compactDateFormat = new DateFormat("yMd", datesLocale);
                var shortDateFormat = new DateFormat("yMMMd", datesLocale);
                var mediumDateFormat = new DateFormat("MMMEd", datesLocale);
                var longDateFormat = new DateFormat("yMMMMEEEEd", datesLocale);
                var yearMonthFormat = new DateFormat("yMMMM", datesLocale);
                var shortMonthDayFormat = new DateFormat("MMMd", datesLocale);

                string? numbersLocale;
                if (NumberFormat.LocaleExists(localeName))
                {
                    numbersLocale = localeName;
                }
                else if (NumberFormat.LocaleExists(locale.LanguageCode))
                {
                    numbersLocale = locale.LanguageCode;
                }
                else
                {
                    numbersLocale = null;
                }

                NumberFormat decimalFormat = NumberFormat.DecimalPattern(numbersLocale);
                var twoDigitZeroPaddedFormat = new NumberFormat("00", numbersLocale);

                MaterialLocalizations translation = GetMaterialTranslation(
                    locale,
                    fullYearFormat,
                    compactDateFormat,
                    shortDateFormat,
                    mediumDateFormat,
                    longDateFormat,
                    yearMonthFormat,
                    shortMonthDayFormat,
                    decimalFormat,
                    twoDigitZeroPaddedFormat)
                    ?? throw new InvalidOperationException(
                        $"GetMaterialTranslation() called for unsupported locale \"{locale}\"");

                LoadedTranslations[locale] = translation;
                return translation;
            }
        }

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;

        public override string ToString() =>
            $"GlobalMaterialLocalizations.delegate({MaterialSupportedLanguages.Count} locales)";
    }
}
