using System.Globalization;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/material_localizations.dart (baseline subset)

public abstract class MaterialLocalizations
{
    public abstract string TabLabel(int tabIndex, int tabCount);

    public virtual string DeleteButtonTooltip => "Delete";

    public virtual string BackButtonTooltip => "Back";

    public virtual string CloseButtonTooltip => "Close";

    public virtual string OpenAppDrawerTooltip => "Open navigation menu";

    public virtual string SignedInLabel => "Signed in";

    public virtual string HideAccountsLabel => "Hide accounts";

    public virtual string ShowAccountsLabel => "Show accounts";

    public virtual string AlertDialogLabel => "Alert";

    public virtual string DialogLabel => "Dialog";

    public virtual string ModalBarrierDismissLabel => "Dismiss";

    public virtual string ShowMenuTooltip => "Show menu";

    public virtual string PopupMenuLabel => "Popup menu";

    public virtual string MenuDismissLabel => "Dismiss menu";

    public virtual string ExpandedIconTapHint => "Collapse";

    public virtual string CollapsedIconTapHint => "Expand";

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

    public virtual string HourLabel => "Hour";

    public virtual string MinuteLabel => "Minute";

    public virtual string AnteMeridiemAbbreviation => "AM";

    public virtual string PostMeridiemAbbreviation => "PM";

    public virtual TimeOfDayFormat TimeOfDayFormat(bool alwaysUse24HourFormat = false) =>
        alwaysUse24HourFormat ? Material.TimeOfDayFormat.HHColonMm : Material.TimeOfDayFormat.HColonMmSpaceA;

    public virtual string FormatHour(TimeOfDay timeOfDay, bool alwaysUse24HourFormat = false)
    {
        var format = TimeOfDayFormat(alwaysUse24HourFormat);
        var hour = TimeOfDay.HourFormatOf(format) switch
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
        var hour = FormatHour(timeOfDay, alwaysUse24HourFormat);
        var minute = FormatMinute(timeOfDay);
        return format switch
        {
            Material.TimeOfDayFormat.HHDotMm => $"{hour}.{minute}",
            Material.TimeOfDayFormat.FrenchCanadian => $"{hour} h {minute}",
            Material.TimeOfDayFormat.ASpaceHColonMm =>
                $"{(timeOfDay.Period == DayPeriod.Am ? AnteMeridiemAbbreviation : PostMeridiemAbbreviation)} {hour}:{minute}",
            Material.TimeOfDayFormat.HColonMmSpaceA =>
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
        var parts = input.Split('/');
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var day)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
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

    public virtual string LicensesPackageDetailText(int licenseCount) =>
        licenseCount == 1 ? "1 license" : $"{licenseCount} licenses";

    public static MaterialLocalizations Of(BuildContext context)
    {
        return MaterialLocalizationsScope.Of(context);
    }

    private static CultureInfo EnglishCulture { get; } = CultureInfo.GetCultureInfo("en-US");
}

public sealed class DefaultMaterialLocalizations : MaterialLocalizations
{
    private DefaultMaterialLocalizations()
    {
    }

    public static DefaultMaterialLocalizations Instance { get; } = new();

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

    public override string ShowMenuTooltip => "Show menu";

    public override string PopupMenuLabel => "Popup menu";

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
