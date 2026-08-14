using System.Globalization;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): material_ui/lib/src/material_localizations.dart (baseline subset)

public abstract class MaterialLocalizations
{
    public virtual ScriptCategory ScriptCategory => ScriptCategory.EnglishLike;

    public abstract string TabLabel(int tabIndex, int tabCount);

    public virtual string DeleteButtonTooltip => "Delete";

    public virtual string CutButtonLabel => "Cut";

    public virtual string CopyButtonLabel => "Copy";

    public virtual string PasteButtonLabel => "Paste";

    public virtual string SelectAllButtonLabel => "Select all";

    public virtual string LookUpButtonLabel => "Look up";

    public virtual string SearchWebButtonLabel => "Search web";

    public virtual string ShareButtonLabel => "Share";

    public virtual string ScanTextButtonLabel => "Scan text";

    public virtual string BackButtonTooltip => "Back";

    public virtual string MoreButtonTooltip => "More";

    public virtual string CloseButtonTooltip => "Close";

    public virtual string OpenAppDrawerTooltip => "Open navigation menu";

    public virtual string SignedInLabel => "Signed in";

    public virtual string HideAccountsLabel => "Hide accounts";

    public virtual string ShowAccountsLabel => "Show accounts";

    public virtual string AlertDialogLabel => "Alert";

    public virtual string DialogLabel => "Dialog";

    public virtual string ModalBarrierDismissLabel => "Dismiss";

    public virtual string ScrimLabel => "Scrim";

    public virtual string BottomSheetLabel => "Bottom Sheet";

    public virtual string ScrimOnTapHint(string modalRouteContentName) => $"Close {modalRouteContentName}";

    public virtual string DrawerLabel => "Navigation menu";

    public virtual string ShowMenuTooltip => "Show menu";

    public virtual string PopupMenuLabel => "Popup menu";

    public virtual string MenuDismissLabel => "Dismiss menu";

    public virtual string SearchFieldLabel => "Search";

    public virtual string ClearButtonTooltip => "Clear";

    public virtual string ExpandedIconTapHint => "Collapse";

    public virtual string CollapsedIconTapHint => "Expand";

    public virtual string ExpansionTileExpandedHint => "double tap to collapse";

    public virtual string ExpansionTileCollapsedHint => "double tap to expand";

    public virtual string ExpansionTileExpandedTapHint => "Collapse";

    public virtual string ExpansionTileCollapsedTapHint => "Expand for more details";

    public virtual string ExpandedHint => "Collapsed";

    public virtual string CollapsedHint => "Expanded";

    public virtual string ContinueButtonLabel => "Continue";

    public virtual string CancelButtonLabel => "Cancel";

    public virtual string OkButtonLabel => "OK";

    public virtual string ViewLicensesButtonLabel => "View licenses";

    public virtual string CloseButtonLabel => "Close";

    public virtual string LicensesPageTitle => "Licenses";

    public virtual string RefreshIndicatorSemanticLabel => "Refresh";

    public virtual string RowsPerPageTitle => "Rows per page:";

    public virtual string FirstPageTooltip => "First page";

    public virtual string PreviousPageTooltip => "Previous page";

    public virtual string NextPageTooltip => "Next page";

    public virtual string LastPageTooltip => "Last page";

    public virtual IReadOnlyList<string> NarrowWeekdays { get; } = ["S", "M", "T", "W", "T", "F", "S"];

    public virtual int FirstDayOfWeekIndex => 0;

    public virtual string CurrentDateLabel => "Today";

    public virtual string SelectedDateLabel => "Selected";

    public virtual string SelectYearSemanticsLabel => "Select year";

    public virtual string PreviousMonthTooltip => "Previous month";

    public virtual string NextMonthTooltip => "Next month";

    public virtual string DateHelpText => "mm/dd/yyyy";

    public virtual string DateInputLabel => "Enter date";

    public virtual string InvalidDateFormatLabel => "Invalid format.";

    public virtual string DateOutOfRangeLabel => "Out of range.";

    public virtual string DatePickerHelpText => "Select date";

    public virtual string CalendarModeButtonLabel => "Switch to calendar";

    public virtual string InputDateModeButtonLabel => "Switch to input";

    public virtual string DateRangeStartLabel => "Start date";

    public virtual string DateRangeEndLabel => "End date";

    public virtual string DateRangePickerHelpText => "Select range";

    public virtual string InvalidDateRangeLabel => "Invalid range.";

    public virtual string SaveButtonLabel => "Save";

    public virtual string UnspecifiedDateRange => "Date range";

    public virtual string TimePickerDialHelpText => "Select time";

    public virtual string TimePickerInputHelpText => "Enter time";

    public virtual string InputTimeModeButtonLabel => "Switch to text input mode";

    public virtual string DialModeButtonLabel => "Switch to clock mode";

    public virtual string InvalidTimeLabel => "Enter a valid time";

    public virtual string TimePickerHourModeAnnouncement => "Select hours";

    public virtual string TimePickerMinuteModeAnnouncement => "Select minutes";

    public virtual string HourLabel => "Hour";

    public virtual string MinuteLabel => "Minute";

    public virtual string AnteMeridiemAbbreviation => "AM";

    public virtual string PostMeridiemAbbreviation => "PM";

    public virtual TimeOfDayFormat TimeOfDayFormat(bool alwaysUse24HourFormat = false) =>
        alwaysUse24HourFormat
            ? global::Plumix.Material.TimeOfDayFormat.HHColonMm
            : global::Plumix.Material.TimeOfDayFormat.HColonMmSpaceA;

    public virtual string FormatHour(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
    {
        var format = TimeOfDayFormat(alwaysUse24HourFormat);
        int hour = TimeOfDay.HourFormatOf(format) switch
        {
            HourFormat.H12 => timeOfDay.HourOfPeriod,
            _ => timeOfDay.Hour,
        };
        return TimeOfDay.HourFormatOf(format) == HourFormat.HH
            ? hour.ToString("00", CultureInfo.InvariantCulture)
            : hour.ToString(CultureInfo.InvariantCulture);
    }

    public virtual string FormatMinute(TimeOfDay timeOfDay) =>
        timeOfDay.Minute.ToString("00", CultureInfo.InvariantCulture);

    public virtual string FormatTimeOfDay(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
    {
        var format = TimeOfDayFormat(alwaysUse24HourFormat);
        string hour = FormatHour(timeOfDay, alwaysUse24HourFormat);
        string minute = FormatMinute(timeOfDay);
        return format switch
        {
            global::Plumix.Material.TimeOfDayFormat.HHDotMm => $"{hour}.{minute}",
            global::Plumix.Material.TimeOfDayFormat.FrenchCanadian => $"{hour} h {minute}",
            global::Plumix.Material.TimeOfDayFormat.ASpaceHColonMm =>
                $"{(timeOfDay.Period == DayPeriod.Am ? AnteMeridiemAbbreviation : PostMeridiemAbbreviation)} {hour}:{minute}",
            global::Plumix.Material.TimeOfDayFormat.HColonMmSpaceA =>
                $"{hour}:{minute} {(timeOfDay.Period == DayPeriod.Am ? AnteMeridiemAbbreviation : PostMeridiemAbbreviation)}",
            _ => $"{hour}:{minute}",
        };
    }

    public virtual string FormatDecimal(int number) => number.ToString(CultureInfo.InvariantCulture);

    public virtual string FormatYear(DateTime date) => date.Year.ToString(CultureInfo.InvariantCulture);

    public virtual string FormatMonthYear(DateTime date) => date.ToString("MMMM yyyy", EnglishCulture);

    public virtual string FormatMediumDate(DateTime date) => date.ToString("ddd, MMM d", EnglishCulture);

    public virtual string FormatShortMonthDay(DateTime date) => date.ToString("MMM d", EnglishCulture);

    public virtual string FormatShortDate(DateTime date) => date.ToString("MMM d, yyyy", EnglishCulture);

    public virtual string FormatFullDate(DateTime date) => date.ToString("dddd, MMMM d, yyyy", EnglishCulture);

    public virtual string FormatCompactDate(DateTime date) => date.ToString("MM/dd/yyyy", EnglishCulture);

    public virtual DateTime? ParseCompactDate(string? input)
    {
        if (input is null) return null;
        string[] parts = input.Split('/');
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int month)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int day)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int year)
            || year < 1 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return null;
        }
        return new DateTime(year, month, day);
    }

    public virtual string SelectedRowCountTitle(int selectedRowCount) =>
        selectedRowCount == 1 ? "1 item selected" : $"{selectedRowCount} items selected";

    public virtual string PageRowsInfoTitle(int firstRow, int lastRow, int rowCount, bool rowCountIsApproximate) =>
        $"{firstRow}–{lastRow} of {(rowCountIsApproximate ? "about " : string.Empty)}{rowCount}";

    public virtual string AboutListTileTitle(string applicationName) => $"About {applicationName}";

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

    // Flutter's `keyboardKey*` getters, used by the menu shortcut labeler. Declared here with the
    // `DefaultMaterialLocalizations` English strings, matching how the rest of this class carries
    // its defaults.

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

    private static CultureInfo EnglishCulture { get; } = CultureInfo.GetCultureInfo("en-US");
}

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

    public override string DeleteButtonTooltip => "Delete";

    public override string BackButtonTooltip => "Back";

    public override string CloseButtonTooltip => "Close";

    public override string OpenAppDrawerTooltip => "Open navigation menu";

    public override string SignedInLabel => "Signed in";

    public override string HideAccountsLabel => "Hide accounts";

    public override string ShowAccountsLabel => "Show accounts";

    public override string AlertDialogLabel => "Alert";

    public override string DialogLabel => "Dialog";

    public override string ModalBarrierDismissLabel => "Dismiss";

    public override string ScrimLabel => "Scrim";

    public override string BottomSheetLabel => "Bottom Sheet";

    public override string ScrimOnTapHint(string modalRouteContentName) => $"Close {modalRouteContentName}";

    public override string ShowMenuTooltip => "Show menu";

    public override string PopupMenuLabel => "Popup menu";

    public override string SearchFieldLabel => "Search";

    public override string ClearButtonTooltip => "Clear";

    public override string MenuDismissLabel => "Dismiss menu";

    public override string TabLabel(int tabIndex, int tabCount)
    {
        if (tabCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tabCount), "Tab count must be greater than zero.");
        }

        if (tabIndex < 0 || tabIndex >= tabCount)
        {
            throw new ArgumentOutOfRangeException(nameof(tabIndex), "Tab index must be within tab count bounds.");
        }

        return $"Tab {tabIndex + 1} of {tabCount}";
    }

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
