using System.Globalization;
using System.Text;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/material_localizations.dart

/// <summary>
/// Defines the localized resource values used by the Material widgets.
/// </summary>
/// <remarks>
/// Dart declares most members abstract on <c>MaterialLocalizations</c> and implements them on
/// <c>DefaultMaterialLocalizations</c>; Plumix declares them <c>virtual</c> here carrying the very
/// same US English values, the way <see cref="WidgetsLocalizations"/> does (see
/// <c>docs/ai/DIVERGENCES.md</c>).
/// </remarks>
public abstract class MaterialLocalizations
{
    private static readonly IReadOnlyList<string> DefaultShortWeekdays =
        ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    private static readonly IReadOnlyList<string> DefaultWeekdays =
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    private static readonly IReadOnlyList<string> DefaultNarrowWeekdays =
        ["S", "M", "T", "W", "T", "F", "S"];

    private static readonly IReadOnlyList<string> DefaultShortMonths =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    private static readonly IReadOnlyList<string> DefaultMonths =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    public virtual string OpenAppDrawerTooltip => "Open navigation menu";

    public virtual string BackButtonTooltip => "Back";

    public virtual string ClearButtonTooltip => "Clear text";

    public virtual string CloseButtonTooltip => "Close";

    public virtual string DeleteButtonTooltip => "Delete";

    public virtual string MoreButtonTooltip => "More";

    public virtual string NextMonthTooltip => "Next month";

    public virtual string PreviousMonthTooltip => "Previous month";

    public virtual string FirstPageTooltip => "First page";

    public virtual string LastPageTooltip => "Last page";

    public virtual string NextPageTooltip => "Next page";

    public virtual string PreviousPageTooltip => "Previous page";

    public virtual string ShowMenuTooltip => "Show menu";

    public virtual string AboutListTileTitle(string applicationName) => $"About {applicationName}";

    public virtual string LicensesPageTitle => "Licenses";

    public virtual string LicensesPackageDetailText(int licenseCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(licenseCount);
        return licenseCount switch
        {
            0 => "No licenses.",
            1 => "1 license.",
            _ => $"{licenseCount} licenses.",
        };
    }

    public virtual string PageRowsInfoTitle(int firstRow, int lastRow, int rowCount, bool rowCountIsApproximate) =>
        rowCountIsApproximate
            ? $"{firstRow}–{lastRow} of about {rowCount}"
            : $"{firstRow}–{lastRow} of {rowCount}";

    public virtual string RowsPerPageTitle => "Rows per page:";

    public virtual string TabLabel(int tabIndex, int tabCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tabIndex, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabCount, 1);
        return $"Tab {tabIndex} of {tabCount}";
    }

    public virtual string SelectedRowCountTitle(int selectedRowCount) => selectedRowCount switch
    {
        0 => "No items selected",
        1 => "1 item selected",
        _ => $"{selectedRowCount} items selected",
    };

    public virtual string CancelButtonLabel => "Cancel";

    public virtual string CloseButtonLabel => "Close";

    public virtual string ContinueButtonLabel => "Continue";

    public virtual string CopyButtonLabel => "Copy";

    public virtual string CutButtonLabel => "Cut";

    public virtual string ScanTextButtonLabel => "Scan text";

    public virtual string OkButtonLabel => "OK";

    public virtual string PasteButtonLabel => "Paste";

    public virtual string SelectAllButtonLabel => "Select all";

    public virtual string LookUpButtonLabel => "Look Up";

    public virtual string SearchWebButtonLabel => "Search Web";

    public virtual string ShareButtonLabel => "Share";

    public virtual string ViewLicensesButtonLabel => "View licenses";

    public virtual string AnteMeridiemAbbreviation => "AM";

    public virtual string PostMeridiemAbbreviation => "PM";

    public virtual string TimePickerHourModeAnnouncement => "Select hours";

    public virtual string TimePickerMinuteModeAnnouncement => "Select minutes";

    public virtual string ModalBarrierDismissLabel => "Dismiss";

    public virtual string MenuDismissLabel => "Dismiss menu";

    public virtual string DrawerLabel => "Navigation menu";

    public virtual string PopupMenuLabel => "Popup menu";

    public virtual string MenuBarMenuLabel => "Menu bar menu";

    public virtual string DialogLabel => "Dialog";

    public virtual string AlertDialogLabel => "Alert";

    public virtual string SearchFieldLabel => "Search";

    public virtual string CurrentDateLabel => "Today";

    public virtual string SelectedDateLabel => "Selected";

    public virtual string ScrimLabel => "Scrim";

    public virtual string BottomSheetLabel => "Bottom Sheet";

    public virtual string ScrimOnTapHint(string modalRouteContentName) => $"Close {modalRouteContentName}";

    public virtual TimeOfDayFormat TimeOfDayFormat(bool alwaysUse24HourFormat = false) =>
        alwaysUse24HourFormat
            ? global::Plumix.Material.TimeOfDayFormat.HHColonMm
            : global::Plumix.Material.TimeOfDayFormat.HColonMmSpaceA;

    public virtual ScriptCategory ScriptCategory => ScriptCategory.EnglishLike;

    public virtual string FormatDecimal(int number)
    {
        if (number is > -1000 and < 1000)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        string digits = Math.Abs((long)number).ToString(CultureInfo.InvariantCulture);
        var result = new StringBuilder(number < 0 ? "-" : string.Empty);
        int maxDigitIndex = digits.Length - 1;
        for (int i = 0; i <= maxDigitIndex; i++)
        {
            result.Append(digits[i]);
            if (i < maxDigitIndex && (maxDigitIndex - i) % 3 == 0)
            {
                result.Append(',');
            }
        }

        return result.ToString();
    }

    public virtual string FormatHour(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
    {
        TimeOfDayFormat format = TimeOfDayFormat(alwaysUse24HourFormat);
        return format switch
        {
            // `TimeOfDay.HourOfPeriod` already reports 12 where Dart reports 0.
            global::Plumix.Material.TimeOfDayFormat.HColonMmSpaceA => FormatDecimal(timeOfDay.HourOfPeriod),
            global::Plumix.Material.TimeOfDayFormat.HHColonMm => FormatTwoDigitZeroPad(timeOfDay.Hour),
            _ => throw new InvalidOperationException($"{GetType()} does not support {format}."),
        };
    }

    public virtual string FormatMinute(TimeOfDay timeOfDay) => FormatTwoDigitZeroPad(timeOfDay.Minute);

    public virtual string FormatTimeOfDay(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
    {
        // Not using DateFormat for two reasons:
        //
        // - DateFormat supports more formats than our material time picker does, and we want to be
        //   consistent across time picker format and the string formatting of the time of day.
        // - DateFormat operates on DateTime, which is sensitive to time eras and time zones, while
        //   here we want to format hour and minute within one day no matter what date the day falls
        //   on.
        var buffer = new StringBuilder();
        buffer.Append(FormatHour(timeOfDay, alwaysUse24HourFormat))
            .Append(':')
            .Append(FormatMinute(timeOfDay));
        if (alwaysUse24HourFormat)
        {
            // There's no AM/PM indicator in 24-hour format.
            return buffer.ToString();
        }

        return buffer.Append(' ').Append(FormatDayPeriod(timeOfDay)).ToString();
    }

    public virtual string FormatYear(DateTime date) => date.Year.ToString(CultureInfo.InvariantCulture);

    public virtual string FormatCompactDate(DateTime date)
    {
        // Assumes US mm/dd/yyyy format.
        string month = FormatTwoDigitZeroPad(date.Month);
        string day = FormatTwoDigitZeroPad(date.Day);
        string year = date.Year.ToString(CultureInfo.InvariantCulture).PadLeft(4, '0');
        return $"{month}/{day}/{year}";
    }

    public virtual string FormatShortDate(DateTime date) =>
        $"{DefaultShortMonths[date.Month - 1]} {date.Day}, {date.Year}";

    public virtual string FormatMediumDate(DateTime date) =>
        $"{DefaultShortWeekdays[DartWeekday(date) - 1]}, {DefaultShortMonths[date.Month - 1]} {date.Day}";

    public virtual string FormatFullDate(DateTime date) =>
        $"{DefaultWeekdays[DartWeekday(date) - 1]}, {DefaultMonths[date.Month - 1]} {date.Day}, {date.Year}";

    public virtual string FormatMonthYear(DateTime date) =>
        $"{DefaultMonths[date.Month - 1]} {FormatYear(date)}";

    public virtual string FormatShortMonthDay(DateTime date) =>
        $"{DefaultShortMonths[date.Month - 1]} {date.Day}";

    public virtual DateTime? ParseCompactDate(string? inputString)
    {
        if (inputString is null)
        {
            return null;
        }

        // Assumes US mm/dd/yyyy format.
        string[] inputParts = inputString.Split('/');
        if (inputParts.Length != 3)
        {
            return null;
        }

        if (!TryParseDecimal(inputParts[2], out int year) || year < 1 || year > 9999)
        {
            return null;
        }

        if (!TryParseDecimal(inputParts[0], out int month) || month is < 1 or > 12)
        {
            return null;
        }

        if (!TryParseDecimal(inputParts[1], out int day) || day < 1 || day > DaysInMonth(year, month))
        {
            return null;
        }

        return new DateTime(year, month, day);
    }

    public virtual IReadOnlyList<string> NarrowWeekdays => DefaultNarrowWeekdays;

    public virtual int FirstDayOfWeekIndex => 0; // NarrowWeekdays[0] is 'S' for Sunday.

    public virtual string DateSeparator => "/";

    public virtual string DateHelpText => "mm/dd/yyyy";

    public virtual string SelectYearSemanticsLabel => "Select year";

    public virtual string UnspecifiedDate => "Date";

    public virtual string UnspecifiedDateRange => "Date Range";

    public virtual string DateInputLabel => "Enter Date";

    public virtual string DateRangeStartLabel => "Start Date";

    public virtual string DateRangeEndLabel => "End Date";

    public virtual string DateRangeStartDateSemanticLabel(string formattedDate) => $"Start date {formattedDate}";

    public virtual string DateRangeEndDateSemanticLabel(string formattedDate) => $"End date {formattedDate}";

    public virtual string InvalidDateFormatLabel => "Invalid format.";

    public virtual string InvalidDateRangeLabel => "Invalid range.";

    public virtual string DateOutOfRangeLabel => "Out of range.";

    public virtual string SaveButtonLabel => "Save";

    public virtual string DatePickerHelpText => "Select date";

    public virtual string DateRangePickerHelpText => "Select range";

    public virtual string CalendarModeButtonLabel => "Switch to calendar";

    public virtual string InputDateModeButtonLabel => "Switch to input";

    public virtual string TimePickerDialHelpText => "Select time";

    public virtual string TimePickerInputHelpText => "Enter time";

    public virtual string TimePickerHourLabel => "Hour";

    public virtual string TimePickerMinuteLabel => "Minute";

    public virtual string InvalidTimeLabel => "Enter a valid time";

    public virtual string DialModeButtonLabel => "Switch to dial picker mode";

    public virtual string InputTimeModeButtonLabel => "Switch to text input mode";

    public virtual string SignedInLabel => "Signed in";

    public virtual string HideAccountsLabel => "Hide accounts";

    public virtual string ShowAccountsLabel => "Show accounts";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemToStart"/>.</summary>
    public virtual string ReorderItemToStart => "Move to the start";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemToEnd"/>.</summary>
    public virtual string ReorderItemToEnd => "Move to the end";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemUp"/>.</summary>
    public virtual string ReorderItemUp => "Move up";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemDown"/>.</summary>
    public virtual string ReorderItemDown => "Move down";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemLeft"/>.</summary>
    public virtual string ReorderItemLeft => "Move left";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemRight"/>.</summary>
    public virtual string ReorderItemRight => "Move right";

    public virtual string ExpandedIconTapHint => "Collapse";

    public virtual string CollapsedIconTapHint => "Expand";

    public virtual string ExpansionTileExpandedHint => "double tap to collapse";

    public virtual string ExpansionTileCollapsedHint => "double tap to expand";

    public virtual string ExpansionTileExpandedTapHint => "Collapse";

    public virtual string ExpansionTileCollapsedTapHint => "Expand for more details";

    public virtual string ExpandedHint => "Collapsed";

    public virtual string CollapsedHint => "Expanded";

    public virtual string RemainingTextFieldCharacterCount(int remaining) => remaining switch
    {
        0 => "No characters remaining",
        1 => "1 character remaining",
        _ => $"{remaining} characters remaining",
    };

    public virtual string RefreshIndicatorSemanticLabel => "Refresh";

    // Flutter's `keyboardKey*` getters, used by the menu shortcut labeler.

    public virtual string KeyboardKeyAlt => "Alt";

    public virtual string KeyboardKeyAltGraph => "AltGr";

    public virtual string KeyboardKeyBackspace => "Backspace";

    public virtual string KeyboardKeyCapsLock => "Caps Lock";

    public virtual string KeyboardKeyChannelDown => "Channel Down";

    public virtual string KeyboardKeyChannelUp => "Channel Up";

    public virtual string KeyboardKeyControl => "Ctrl";

    public virtual string KeyboardKeyDelete => "Del";

    public virtual string KeyboardKeyEject => "Eject";

    public virtual string KeyboardKeyEnd => "End";

    public virtual string KeyboardKeyEscape => "Esc";

    public virtual string KeyboardKeyFn => "Fn";

    public virtual string KeyboardKeyHome => "Home";

    public virtual string KeyboardKeyInsert => "Insert";

    public virtual string KeyboardKeyMeta => "Meta";

    public virtual string KeyboardKeyMetaMacOs => "Command";

    public virtual string KeyboardKeyMetaWindows => "Win";

    public virtual string KeyboardKeyNumLock => "Num Lock";

    public virtual string KeyboardKeyNumpad1 => "Num 1";

    public virtual string KeyboardKeyNumpad2 => "Num 2";

    public virtual string KeyboardKeyNumpad3 => "Num 3";

    public virtual string KeyboardKeyNumpad4 => "Num 4";

    public virtual string KeyboardKeyNumpad5 => "Num 5";

    public virtual string KeyboardKeyNumpad6 => "Num 6";

    public virtual string KeyboardKeyNumpad7 => "Num 7";

    public virtual string KeyboardKeyNumpad8 => "Num 8";

    public virtual string KeyboardKeyNumpad9 => "Num 9";

    public virtual string KeyboardKeyNumpad0 => "Num 0";

    public virtual string KeyboardKeyNumpadAdd => "Num +";

    public virtual string KeyboardKeyNumpadComma => "Num ,";

    public virtual string KeyboardKeyNumpadDecimal => "Num .";

    public virtual string KeyboardKeyNumpadDivide => "Num /";

    public virtual string KeyboardKeyNumpadEnter => "Num Enter";

    public virtual string KeyboardKeyNumpadEqual => "Num =";

    public virtual string KeyboardKeyNumpadMultiply => "Num *";

    public virtual string KeyboardKeyNumpadParenLeft => "Num (";

    public virtual string KeyboardKeyNumpadParenRight => "Num )";

    public virtual string KeyboardKeyNumpadSubtract => "Num -";

    public virtual string KeyboardKeyPageDown => "PgDown";

    public virtual string KeyboardKeyPageUp => "PgUp";

    public virtual string KeyboardKeyPower => "Power";

    public virtual string KeyboardKeyPowerOff => "Power Off";

    public virtual string KeyboardKeyPrintScreen => "Print Screen";

    public virtual string KeyboardKeyScrollLock => "Scroll Lock";

    public virtual string KeyboardKeySelect => "Select";

    public virtual string KeyboardKeyShift => "Shift";

    public virtual string KeyboardKeySpace => "Space";

    public static MaterialLocalizations Of(BuildContext context)
    {
        return Localizations.MaybeOf<MaterialLocalizations>(context)
               ?? MaterialLocalizationsScope.Of(context);
    }

    /// Dart's `DateTime.weekday`: Monday is 1, Sunday is 7.
    private protected static int DartWeekday(DateTime date) =>
        date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;

    private string FormatDayPeriod(TimeOfDay timeOfDay) =>
        timeOfDay.Period == DayPeriod.Am ? AnteMeridiemAbbreviation : PostMeridiemAbbreviation;

    private static string FormatTwoDigitZeroPad(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(number, 100);
        return number < 10 ? $"0{number}" : number.ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryParseDecimal(string text, out int value) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

    /// Dart's `DefaultMaterialLocalizations._getDaysInMonth`.
    private static int DaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            bool isLeapYear = (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
            return isLeapYear ? 29 : 28;
        }

        int[] daysInMonth = [31, -1, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        return daysInMonth[month - 1];
    }
}

/// <summary>US English strings for the Material widgets; Dart's <c>DefaultMaterialLocalizations</c>.</summary>
/// <remarks>
/// Dart's delegate supports only <c>en</c> and always reports
/// <see cref="Plumix.Material.ScriptCategory.EnglishLike"/>; Plumix's accepts every locale and picks
/// the script category from the language so that <see cref="Typography"/> stays usable without
/// <see cref="GlobalMaterialLocalizations"/> (see <c>docs/ai/DIVERGENCES.md</c>).
/// </remarks>
public sealed class DefaultMaterialLocalizations : MaterialLocalizations
{
    private static readonly IReadOnlySet<string> DenseLanguages = new HashSet<string>(StringComparer.Ordinal)
    {
        "bo", "hi", "ja", "km", "ko", "mr", "ta", "zh",
    };
    private static readonly IReadOnlySet<string> TallLanguages = new HashSet<string>(StringComparer.Ordinal)
    {
        "ar", "bn", "fa", "gu", "kn", "lo", "ml", "my", "ne", "or", "pa", "ps", "te", "th", "ug",
        "ur",
    };

    private DefaultMaterialLocalizations(ScriptCategory scriptCategory)
    {
        ScriptCategory = scriptCategory;
    }

    public static DefaultMaterialLocalizations Instance { get; } = new(ScriptCategory.EnglishLike);

    public static LocalizationsDelegate<MaterialLocalizations> Delegate { get; } =
        new DefaultMaterialLocalizationsDelegate();

    public override ScriptCategory ScriptCategory { get; }

    internal static DefaultMaterialLocalizations ForLocale(Locale locale)
    {
        ScriptCategory category = DenseLanguages.Contains(locale.LanguageCode)
            ? ScriptCategory.Dense
            : TallLanguages.Contains(locale.LanguageCode)
                ? ScriptCategory.Tall
                : ScriptCategory.EnglishLike;
        return category == ScriptCategory.EnglishLike
            ? Instance
            : new DefaultMaterialLocalizations(category);
    }
}

public sealed class DefaultMaterialLocalizationsDelegate : LocalizationsDelegate<MaterialLocalizations>
{
    public override bool IsSupported(Locale locale) => true;

    public override MaterialLocalizations LoadTyped(Locale locale)
    {
        return DefaultMaterialLocalizations.ForLocale(locale);
    }

    public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
}

public sealed class MaterialLocalizationsScope : InheritedWidget
{
    public MaterialLocalizationsScope(
        MaterialLocalizations localizations,
        Widget child,
        Key? key = null) : base(key)
    {
        Localizations = localizations ?? throw new ArgumentNullException(nameof(localizations));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public MaterialLocalizations Localizations { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((MaterialLocalizationsScope)oldWidget).Localizations, Localizations);
    }

    public static MaterialLocalizations Of(BuildContext context)
    {
        return context.DependOnInherited<MaterialLocalizationsScope>()?.Localizations
               ?? DefaultMaterialLocalizations.Instance;
    }
}
