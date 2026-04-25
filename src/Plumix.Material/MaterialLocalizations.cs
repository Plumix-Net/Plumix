using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/material_localizations.dart (baseline subset)

public abstract class MaterialLocalizations
{
    public abstract string TabLabel(int tabIndex, int tabCount);

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

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((MaterialLocalizationsScope)oldWidget).Localizations, Localizations);
    }

    public static MaterialLocalizations Of(BuildContext context)
    {
        return context.DependOnInherited<MaterialLocalizationsScope>()?.Localizations
               ?? DefaultMaterialLocalizations.Instance;
    }
}
