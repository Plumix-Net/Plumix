using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;

// C#-only test infrastructure; no Dart parity source. Flutter's widget tests that build Material
// widgets without a MaterialApp give them `Localizations(locale: Locale('en', 'US'), delegates:
// [DefaultMaterialLocalizations.delegate, DefaultWidgetsLocalizations.delegate])`.

namespace Plumix.Tests;

internal static class MaterialLocalizationsTesting
{
    /// <summary>
    /// Wraps <paramref name="child"/> in the <see cref="Localizations"/> ancestor Material widgets
    /// need: <paramref name="localizations"/> (US English by default) and the default widgets ones.
    /// </summary>
    /// <remarks>
    /// <see cref="Localizations"/> inserts a left-to-right <see cref="Directionality"/>, so a test of
    /// another direction puts its own <see cref="Directionality"/> below this one.
    /// </remarks>
    public static Widget Localized(Widget child, MaterialLocalizations? localizations = null) => new Localizations(
        locale: new Locale("en", "US"),
        delegates:
        [
            localizations is null
                ? DefaultMaterialLocalizations.Delegate
                : new InstanceDelegate(localizations),
            DefaultWidgetsLocalizations.Delegate,
        ],
        child: child);

    private sealed class InstanceDelegate(MaterialLocalizations localizations)
        : LocalizationsDelegate<MaterialLocalizations>
    {
        public override bool IsSupported(Locale locale) => true;

        public override MaterialLocalizations LoadTyped(Locale locale) => localizations;

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) =>
            oldDelegate is not InstanceDelegate old || !ReferenceEquals(old.Instance, localizations);

        private MaterialLocalizations Instance => localizations;
    }
}
