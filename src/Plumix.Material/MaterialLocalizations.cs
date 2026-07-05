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
