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

    public virtual string ViewLicensesButtonLabel => "View licenses";

    public virtual string CloseButtonLabel => "Close";

    public virtual string LicensesPageTitle => "Licenses";

    public virtual string RefreshIndicatorSemanticLabel => "Refresh";

    public virtual string RowsPerPageTitle => "Rows per page:";

    public virtual string FirstPageTooltip => "First page";

    public virtual string PreviousPageTooltip => "Previous page";

    public virtual string NextPageTooltip => "Next page";

    public virtual string LastPageTooltip => "Last page";

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
