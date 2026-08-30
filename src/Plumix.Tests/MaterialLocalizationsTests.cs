using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: material_ui/lib/src/material_localizations.dart
// (parity tests mirroring material_ui/test/localizations_test.dart)

namespace Plumix.Tests;

public sealed class MaterialLocalizationsTests
{
    [Fact]
    public void MaterialLocalizations_DeclaresEveryResourceAbstract()
    {
        // Dart declares the resources abstract on `MaterialLocalizations` and implements them on
        // `DefaultMaterialLocalizations`; a subclass that overrides nothing must not silently
        // inherit US English.
        Assert.True(typeof(MaterialLocalizations).IsAbstract);
        Assert.Contains(
            typeof(MaterialLocalizations).GetProperties(),
            property => property.Name == nameof(MaterialLocalizations.OkButtonLabel)
                        && property.GetMethod!.IsAbstract);
        Assert.Contains(
            typeof(MaterialLocalizations).GetMethods(),
            method => method.Name == nameof(MaterialLocalizations.AboutListTileTitle) && method.IsAbstract);
    }

    [Fact]
    public void DefaultMaterialLocalizations_CarriesTheUsEnglishValues()
    {
        var localizations = new DefaultMaterialLocalizations();

        Assert.Equal("OK", localizations.OkButtonLabel);
        Assert.Equal("Cancel", localizations.CancelButtonLabel);
        Assert.Equal("Open navigation menu", localizations.OpenAppDrawerTooltip);
        Assert.Equal("About Plumix", localizations.AboutListTileTitle("Plumix"));
        Assert.Equal("mm/dd/yyyy", localizations.DateHelpText);
        Assert.Equal(ScriptCategory.EnglishLike, localizations.ScriptCategory);
        Assert.Equal("1 license.", localizations.LicensesPackageDetailText(1));
        Assert.Equal(0, localizations.FirstDayOfWeekIndex);
    }

    [Fact]
    public void DefaultMaterialLocalizations_IsSubclassableSoATestCanOverrideOneResource()
    {
        MaterialLocalizations localizations = new FrenchishLocalizations();

        Assert.Equal("Retour", localizations.BackButtonTooltip);
        // Everything the subclass leaves alone keeps the US English value.
        Assert.Equal("OK", localizations.OkButtonLabel);
    }

    [Fact]
    public void DefaultMaterialLocalizations_ReportsEnglishLikeForEveryLocaleItSupports()
    {
        Assert.True(DefaultMaterialLocalizations.Delegate.IsSupported(new Locale("en", "GB")));
        Assert.False(DefaultMaterialLocalizations.Delegate.IsSupported(new Locale("fr")));
        Assert.Same(
            DefaultMaterialLocalizations.Instance,
            DefaultMaterialLocalizations.Delegate.LoadTyped(new Locale("en")));
        Assert.False(DefaultMaterialLocalizations.Delegate.ShouldReload(DefaultMaterialLocalizations.Delegate));
    }

    private sealed class FrenchishLocalizations : DefaultMaterialLocalizations
    {
        public override string BackButtonTooltip => "Retour";
    }
}
