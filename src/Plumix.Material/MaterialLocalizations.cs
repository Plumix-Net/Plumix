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
/// Every member is abstract, the way Dart declares them; the US English values live on
/// <see cref="DefaultMaterialLocalizations"/>.
/// </remarks>
public abstract class MaterialLocalizations
{
    public abstract string OpenAppDrawerTooltip { get; }

    public abstract string BackButtonTooltip { get; }

    public abstract string ClearButtonTooltip { get; }

    public abstract string CloseButtonTooltip { get; }

    public abstract string DeleteButtonTooltip { get; }

    public abstract string MoreButtonTooltip { get; }

    public abstract string NextMonthTooltip { get; }

    public abstract string PreviousMonthTooltip { get; }

    public abstract string FirstPageTooltip { get; }

    public abstract string LastPageTooltip { get; }

    public abstract string NextPageTooltip { get; }

    public abstract string PreviousPageTooltip { get; }

    public abstract string ShowMenuTooltip { get; }

    public abstract string AboutListTileTitle(string applicationName);

    public abstract string LicensesPageTitle { get; }

    public abstract string LicensesPackageDetailText(int licenseCount);

    public abstract string PageRowsInfoTitle(int firstRow, int lastRow, int rowCount, bool rowCountIsApproximate);

    public abstract string RowsPerPageTitle { get; }

    public abstract string TabLabel(int tabIndex, int tabCount);

    public abstract string SelectedRowCountTitle(int selectedRowCount);

    public abstract string CancelButtonLabel { get; }

    public abstract string CloseButtonLabel { get; }

    public abstract string ContinueButtonLabel { get; }

    public abstract string CopyButtonLabel { get; }

    public abstract string CutButtonLabel { get; }

    public abstract string ScanTextButtonLabel { get; }

    public abstract string OkButtonLabel { get; }

    public abstract string PasteButtonLabel { get; }

    public abstract string SelectAllButtonLabel { get; }

    public abstract string LookUpButtonLabel { get; }

    public abstract string SearchWebButtonLabel { get; }

    public abstract string ShareButtonLabel { get; }

    public abstract string ViewLicensesButtonLabel { get; }

    public abstract string AnteMeridiemAbbreviation { get; }

    public abstract string PostMeridiemAbbreviation { get; }

    public abstract string TimePickerHourModeAnnouncement { get; }

    public abstract string TimePickerMinuteModeAnnouncement { get; }

    public abstract string ModalBarrierDismissLabel { get; }

    public abstract string MenuDismissLabel { get; }

    public abstract string DrawerLabel { get; }

    public abstract string PopupMenuLabel { get; }

    public abstract string MenuBarMenuLabel { get; }

    public abstract string DialogLabel { get; }

    public abstract string AlertDialogLabel { get; }

    public abstract string SearchFieldLabel { get; }

    public abstract string CurrentDateLabel { get; }

    public abstract string SelectedDateLabel { get; }

    public abstract string ScrimLabel { get; }

    public abstract string BottomSheetLabel { get; }

    public abstract string ScrimOnTapHint(string modalRouteContentName);

    public abstract TimeOfDayFormat TimeOfDayFormat(bool alwaysUse24HourFormat = false);

    public abstract ScriptCategory ScriptCategory { get; }

    public abstract string FormatDecimal(int number);

    public abstract string FormatHour(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false);

    public abstract string FormatMinute(TimeOfDay timeOfDay);

    public abstract string FormatTimeOfDay(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false);

    public abstract string FormatYear(DateTime date);

    public abstract string FormatCompactDate(DateTime date);

    public abstract string FormatShortDate(DateTime date);

    public abstract string FormatMediumDate(DateTime date);

    public abstract string FormatFullDate(DateTime date);

    public abstract string FormatMonthYear(DateTime date);

    public abstract string FormatShortMonthDay(DateTime date);

    public abstract DateTime? ParseCompactDate(string? inputString);

    public abstract IReadOnlyList<string> NarrowWeekdays { get; }

    public abstract int FirstDayOfWeekIndex { get; }

    public abstract string DateSeparator { get; }

    public abstract string DateHelpText { get; }

    public abstract string SelectYearSemanticsLabel { get; }

    public abstract string UnspecifiedDate { get; }

    public abstract string UnspecifiedDateRange { get; }

    public abstract string DateInputLabel { get; }

    public abstract string DateRangeStartLabel { get; }

    public abstract string DateRangeEndLabel { get; }

    public abstract string DateRangeStartDateSemanticLabel(string formattedDate);

    public abstract string DateRangeEndDateSemanticLabel(string formattedDate);

    public abstract string InvalidDateFormatLabel { get; }

    public abstract string InvalidDateRangeLabel { get; }

    public abstract string DateOutOfRangeLabel { get; }

    public abstract string SaveButtonLabel { get; }

    public abstract string DatePickerHelpText { get; }

    public abstract string DateRangePickerHelpText { get; }

    public abstract string CalendarModeButtonLabel { get; }

    public abstract string InputDateModeButtonLabel { get; }

    public abstract string TimePickerDialHelpText { get; }

    public abstract string TimePickerInputHelpText { get; }

    public abstract string TimePickerHourLabel { get; }

    public abstract string TimePickerMinuteLabel { get; }

    public abstract string InvalidTimeLabel { get; }

    public abstract string DialModeButtonLabel { get; }

    public abstract string InputTimeModeButtonLabel { get; }

    public abstract string SignedInLabel { get; }

    public abstract string HideAccountsLabel { get; }

    public abstract string ShowAccountsLabel { get; }

    public abstract string ReorderItemToStart { get; }

    public abstract string ReorderItemToEnd { get; }

    public abstract string ReorderItemUp { get; }

    public abstract string ReorderItemDown { get; }

    public abstract string ReorderItemLeft { get; }

    public abstract string ReorderItemRight { get; }

    public abstract string ExpandedIconTapHint { get; }

    public abstract string CollapsedIconTapHint { get; }

    public abstract string ExpansionTileExpandedHint { get; }

    public abstract string ExpansionTileCollapsedHint { get; }

    public abstract string ExpansionTileExpandedTapHint { get; }

    public abstract string ExpansionTileCollapsedTapHint { get; }

    public abstract string ExpandedHint { get; }

    public abstract string CollapsedHint { get; }

    public abstract string RemainingTextFieldCharacterCount(int remaining);

    public abstract string RefreshIndicatorSemanticLabel { get; }

    // Flutter's `keyboardKey*` getters, used by the menu shortcut labeler.

    public abstract string KeyboardKeyAlt { get; }

    public abstract string KeyboardKeyAltGraph { get; }

    public abstract string KeyboardKeyBackspace { get; }

    public abstract string KeyboardKeyCapsLock { get; }

    public abstract string KeyboardKeyChannelDown { get; }

    public abstract string KeyboardKeyChannelUp { get; }

    public abstract string KeyboardKeyControl { get; }

    public abstract string KeyboardKeyDelete { get; }

    public abstract string KeyboardKeyEject { get; }

    public abstract string KeyboardKeyEnd { get; }

    public abstract string KeyboardKeyEscape { get; }

    public abstract string KeyboardKeyFn { get; }

    public abstract string KeyboardKeyHome { get; }

    public abstract string KeyboardKeyInsert { get; }

    public abstract string KeyboardKeyMeta { get; }

    public abstract string KeyboardKeyMetaMacOs { get; }

    public abstract string KeyboardKeyMetaWindows { get; }

    public abstract string KeyboardKeyNumLock { get; }

    public abstract string KeyboardKeyNumpad1 { get; }

    public abstract string KeyboardKeyNumpad2 { get; }

    public abstract string KeyboardKeyNumpad3 { get; }

    public abstract string KeyboardKeyNumpad4 { get; }

    public abstract string KeyboardKeyNumpad5 { get; }

    public abstract string KeyboardKeyNumpad6 { get; }

    public abstract string KeyboardKeyNumpad7 { get; }

    public abstract string KeyboardKeyNumpad8 { get; }

    public abstract string KeyboardKeyNumpad9 { get; }

    public abstract string KeyboardKeyNumpad0 { get; }

    public abstract string KeyboardKeyNumpadAdd { get; }

    public abstract string KeyboardKeyNumpadComma { get; }

    public abstract string KeyboardKeyNumpadDecimal { get; }

    public abstract string KeyboardKeyNumpadDivide { get; }

    public abstract string KeyboardKeyNumpadEnter { get; }

    public abstract string KeyboardKeyNumpadEqual { get; }

    public abstract string KeyboardKeyNumpadMultiply { get; }

    public abstract string KeyboardKeyNumpadParenLeft { get; }

    public abstract string KeyboardKeyNumpadParenRight { get; }

    public abstract string KeyboardKeyNumpadSubtract { get; }

    public abstract string KeyboardKeyPageDown { get; }

    public abstract string KeyboardKeyPageUp { get; }

    public abstract string KeyboardKeyPower { get; }

    public abstract string KeyboardKeyPowerOff { get; }

    public abstract string KeyboardKeyPrintScreen { get; }

    public abstract string KeyboardKeyScrollLock { get; }

    public abstract string KeyboardKeySelect { get; }

    public abstract string KeyboardKeyShift { get; }

    public abstract string KeyboardKeySpace { get; }

    public static MaterialLocalizations Of(BuildContext context)
    {
        return Localizations.MaybeOf<MaterialLocalizations>(context)
               ?? MaterialLocalizationsScope.Of(context);
    }

    /// Dart's `DateTime.weekday`: Monday is 1, Sunday is 7.
    private protected static int DartWeekday(DateTime date) =>
        date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
}

/// <summary>US English strings for the Material widgets; Dart's <c>DefaultMaterialLocalizations</c>.</summary>
public class DefaultMaterialLocalizations : MaterialLocalizations
{
    /// <summary>Creates an object that provides US English resource values for the Material widgets.</summary>
    public DefaultMaterialLocalizations()
    {
    }

    /// <summary>The shared instance Dart's <c>DefaultMaterialLocalizations.load</c> returns.</summary>
    public static DefaultMaterialLocalizations Instance { get; } = new();

    /// <summary>
    /// A <see cref="LocalizationsDelegate{T}"/> that uses <see cref="DefaultMaterialLocalizations"/>
    /// for the <c>en</c> locale.
    /// </summary>
    public static LocalizationsDelegate<MaterialLocalizations> Delegate { get; } =
        new DefaultMaterialLocalizationsDelegate();

    /// <inheritdoc />
    public override ScriptCategory ScriptCategory => ScriptCategory.EnglishLike;

    public override string OpenAppDrawerTooltip => "Open navigation menu";

    public override string BackButtonTooltip => "Back";

    public override string ClearButtonTooltip => "Clear text";

    public override string CloseButtonTooltip => "Close";

    public override string DeleteButtonTooltip => "Delete";

    public override string MoreButtonTooltip => "More";

    public override string NextMonthTooltip => "Next month";

    public override string PreviousMonthTooltip => "Previous month";

    public override string FirstPageTooltip => "First page";

    public override string LastPageTooltip => "Last page";

    public override string NextPageTooltip => "Next page";

    public override string PreviousPageTooltip => "Previous page";

    public override string ShowMenuTooltip => "Show menu";

    public override string AboutListTileTitle(string applicationName) => $"About {applicationName}";

    public override string LicensesPageTitle => "Licenses";

    public override string LicensesPackageDetailText(int licenseCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(licenseCount);
        return licenseCount switch
        {
            0 => "No licenses.",
            1 => "1 license.",
            _ => $"{licenseCount} licenses.",
        };
    }

    public override string PageRowsInfoTitle(int firstRow, int lastRow, int rowCount, bool rowCountIsApproximate) =>
        rowCountIsApproximate
            ? $"{firstRow}–{lastRow} of about {rowCount}"
            : $"{firstRow}–{lastRow} of {rowCount}";

    public override string RowsPerPageTitle => "Rows per page:";

    public override string TabLabel(int tabIndex, int tabCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tabIndex, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabCount, 1);
        return $"Tab {tabIndex} of {tabCount}";
    }

    public override string SelectedRowCountTitle(int selectedRowCount) => selectedRowCount switch
    {
        0 => "No items selected",
        1 => "1 item selected",
        _ => $"{selectedRowCount} items selected",
    };

    public override string CancelButtonLabel => "Cancel";

    public override string CloseButtonLabel => "Close";

    public override string ContinueButtonLabel => "Continue";

    public override string CopyButtonLabel => "Copy";

    public override string CutButtonLabel => "Cut";

    public override string ScanTextButtonLabel => "Scan text";

    public override string OkButtonLabel => "OK";

    public override string PasteButtonLabel => "Paste";

    public override string SelectAllButtonLabel => "Select all";

    public override string LookUpButtonLabel => "Look Up";

    public override string SearchWebButtonLabel => "Search Web";

    public override string ShareButtonLabel => "Share";

    public override string ViewLicensesButtonLabel => "View licenses";

    public override string AnteMeridiemAbbreviation => "AM";

    public override string PostMeridiemAbbreviation => "PM";

    public override string TimePickerHourModeAnnouncement => "Select hours";

    public override string TimePickerMinuteModeAnnouncement => "Select minutes";

    public override string ModalBarrierDismissLabel => "Dismiss";

    public override string MenuDismissLabel => "Dismiss menu";

    public override string DrawerLabel => "Navigation menu";

    public override string PopupMenuLabel => "Popup menu";

    public override string MenuBarMenuLabel => "Menu bar menu";

    public override string DialogLabel => "Dialog";

    public override string AlertDialogLabel => "Alert";

    public override string SearchFieldLabel => "Search";

    public override string CurrentDateLabel => "Today";

    public override string SelectedDateLabel => "Selected";

    public override string ScrimLabel => "Scrim";

    public override string BottomSheetLabel => "Bottom Sheet";

    public override string ScrimOnTapHint(string modalRouteContentName) => $"Close {modalRouteContentName}";

    public override TimeOfDayFormat TimeOfDayFormat(bool alwaysUse24HourFormat = false) =>
        alwaysUse24HourFormat
            ? global::Plumix.Material.TimeOfDayFormat.HHColonMm
            : global::Plumix.Material.TimeOfDayFormat.HColonMmSpaceA;

    public override string FormatDecimal(int number)
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

    public override string FormatHour(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
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

    public override string FormatMinute(TimeOfDay timeOfDay) => FormatTwoDigitZeroPad(timeOfDay.Minute);

    public override string FormatTimeOfDay(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
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

    public override string FormatYear(DateTime date) => date.Year.ToString(CultureInfo.InvariantCulture);

    public override string FormatCompactDate(DateTime date)
    {
        // Assumes US mm/dd/yyyy format.
        string month = FormatTwoDigitZeroPad(date.Month);
        string day = FormatTwoDigitZeroPad(date.Day);
        string year = date.Year.ToString(CultureInfo.InvariantCulture).PadLeft(4, '0');
        return $"{month}/{day}/{year}";
    }

    public override string FormatShortDate(DateTime date) =>
        $"{DefaultShortMonths[date.Month - 1]} {date.Day}, {date.Year}";

    public override string FormatMediumDate(DateTime date) =>
        $"{DefaultShortWeekdays[DartWeekday(date) - 1]}, {DefaultShortMonths[date.Month - 1]} {date.Day}";

    public override string FormatFullDate(DateTime date) =>
        $"{DefaultWeekdays[DartWeekday(date) - 1]}, {DefaultMonths[date.Month - 1]} {date.Day}, {date.Year}";

    public override string FormatMonthYear(DateTime date) =>
        $"{DefaultMonths[date.Month - 1]} {FormatYear(date)}";

    public override string FormatShortMonthDay(DateTime date) =>
        $"{DefaultShortMonths[date.Month - 1]} {date.Day}";

    public override DateTime? ParseCompactDate(string? inputString)
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

    public override IReadOnlyList<string> NarrowWeekdays => DefaultNarrowWeekdays;

    public override int FirstDayOfWeekIndex => 0; // NarrowWeekdays[0] is 'S' for Sunday.

    public override string DateSeparator => "/";

    public override string DateHelpText => "mm/dd/yyyy";

    public override string SelectYearSemanticsLabel => "Select year";

    public override string UnspecifiedDate => "Date";

    public override string UnspecifiedDateRange => "Date Range";

    public override string DateInputLabel => "Enter Date";

    public override string DateRangeStartLabel => "Start Date";

    public override string DateRangeEndLabel => "End Date";

    public override string DateRangeStartDateSemanticLabel(string formattedDate) => $"Start date {formattedDate}";

    public override string DateRangeEndDateSemanticLabel(string formattedDate) => $"End date {formattedDate}";

    public override string InvalidDateFormatLabel => "Invalid format.";

    public override string InvalidDateRangeLabel => "Invalid range.";

    public override string DateOutOfRangeLabel => "Out of range.";

    public override string SaveButtonLabel => "Save";

    public override string DatePickerHelpText => "Select date";

    public override string DateRangePickerHelpText => "Select range";

    public override string CalendarModeButtonLabel => "Switch to calendar";

    public override string InputDateModeButtonLabel => "Switch to input";

    public override string TimePickerDialHelpText => "Select time";

    public override string TimePickerInputHelpText => "Enter time";

    public override string TimePickerHourLabel => "Hour";

    public override string TimePickerMinuteLabel => "Minute";

    public override string InvalidTimeLabel => "Enter a valid time";

    public override string DialModeButtonLabel => "Switch to dial picker mode";

    public override string InputTimeModeButtonLabel => "Switch to text input mode";

    public override string SignedInLabel => "Signed in";

    public override string HideAccountsLabel => "Hide accounts";

    public override string ShowAccountsLabel => "Show accounts";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemToStart"/>.</summary>
    public override string ReorderItemToStart => "Move to the start";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemToEnd"/>.</summary>
    public override string ReorderItemToEnd => "Move to the end";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemUp"/>.</summary>
    public override string ReorderItemUp => "Move up";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemDown"/>.</summary>
    public override string ReorderItemDown => "Move down";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemLeft"/>.</summary>
    public override string ReorderItemLeft => "Move left";

    /// <summary>Deprecated in Dart: use <see cref="WidgetsLocalizations.ReorderItemRight"/>.</summary>
    public override string ReorderItemRight => "Move right";

    public override string ExpandedIconTapHint => "Collapse";

    public override string CollapsedIconTapHint => "Expand";

    public override string ExpansionTileExpandedHint => "double tap to collapse";

    public override string ExpansionTileCollapsedHint => "double tap to expand";

    public override string ExpansionTileExpandedTapHint => "Collapse";

    public override string ExpansionTileCollapsedTapHint => "Expand for more details";

    public override string ExpandedHint => "Collapsed";

    public override string CollapsedHint => "Expanded";

    public override string RemainingTextFieldCharacterCount(int remaining) => remaining switch
    {
        0 => "No characters remaining",
        1 => "1 character remaining",
        _ => $"{remaining} characters remaining",
    };

    public override string RefreshIndicatorSemanticLabel => "Refresh";

    public override string KeyboardKeyAlt => "Alt";

    public override string KeyboardKeyAltGraph => "AltGr";

    public override string KeyboardKeyBackspace => "Backspace";

    public override string KeyboardKeyCapsLock => "Caps Lock";

    public override string KeyboardKeyChannelDown => "Channel Down";

    public override string KeyboardKeyChannelUp => "Channel Up";

    public override string KeyboardKeyControl => "Ctrl";

    public override string KeyboardKeyDelete => "Del";

    public override string KeyboardKeyEject => "Eject";

    public override string KeyboardKeyEnd => "End";

    public override string KeyboardKeyEscape => "Esc";

    public override string KeyboardKeyFn => "Fn";

    public override string KeyboardKeyHome => "Home";

    public override string KeyboardKeyInsert => "Insert";

    public override string KeyboardKeyMeta => "Meta";

    public override string KeyboardKeyMetaMacOs => "Command";

    public override string KeyboardKeyMetaWindows => "Win";

    public override string KeyboardKeyNumLock => "Num Lock";

    public override string KeyboardKeyNumpad1 => "Num 1";

    public override string KeyboardKeyNumpad2 => "Num 2";

    public override string KeyboardKeyNumpad3 => "Num 3";

    public override string KeyboardKeyNumpad4 => "Num 4";

    public override string KeyboardKeyNumpad5 => "Num 5";

    public override string KeyboardKeyNumpad6 => "Num 6";

    public override string KeyboardKeyNumpad7 => "Num 7";

    public override string KeyboardKeyNumpad8 => "Num 8";

    public override string KeyboardKeyNumpad9 => "Num 9";

    public override string KeyboardKeyNumpad0 => "Num 0";

    public override string KeyboardKeyNumpadAdd => "Num +";

    public override string KeyboardKeyNumpadComma => "Num ,";

    public override string KeyboardKeyNumpadDecimal => "Num .";

    public override string KeyboardKeyNumpadDivide => "Num /";

    public override string KeyboardKeyNumpadEnter => "Num Enter";

    public override string KeyboardKeyNumpadEqual => "Num =";

    public override string KeyboardKeyNumpadMultiply => "Num *";

    public override string KeyboardKeyNumpadParenLeft => "Num (";

    public override string KeyboardKeyNumpadParenRight => "Num )";

    public override string KeyboardKeyNumpadSubtract => "Num -";

    public override string KeyboardKeyPageDown => "PgDown";

    public override string KeyboardKeyPageUp => "PgUp";

    public override string KeyboardKeyPower => "Power";

    public override string KeyboardKeyPowerOff => "Power Off";

    public override string KeyboardKeyPrintScreen => "Print Screen";

    public override string KeyboardKeyScrollLock => "Scroll Lock";

    public override string KeyboardKeySelect => "Select";

    public override string KeyboardKeyShift => "Shift";

    public override string KeyboardKeySpace => "Space";

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

/// <summary>Flutter's <c>_MaterialLocalizationsDelegate</c>.</summary>
public sealed class DefaultMaterialLocalizationsDelegate : LocalizationsDelegate<MaterialLocalizations>
{
    /// <inheritdoc />
    public override bool IsSupported(Locale locale) => locale.LanguageCode == "en";

    /// <inheritdoc />
    public override MaterialLocalizations LoadTyped(Locale locale) => DefaultMaterialLocalizations.Instance;

    /// <inheritdoc />
    public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;

    /// <inheritdoc />
    public override string ToString() => "DefaultMaterialLocalizations.delegate(en_US)";
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
