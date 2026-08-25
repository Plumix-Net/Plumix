using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter_localizations/lib/src/widgets_localizations.dart
//
// The per-locale bundles live in `GlobalWidgetsLocalizations.g.cs`.

/// <summary>
/// Localized values for widgets, for every language
/// <see cref="GlobalWidgetsLocalizations.WidgetsSupportedLanguages"/> lists.
/// </summary>
/// <remarks>
/// Besides localized strings, this class also maps a <see cref="Locale"/> to its
/// <see cref="TextDirection"/>: all locales are <see cref="TextDirection.Ltr"/> except Arabic
/// (<c>ar</c>), Farsi (<c>fa</c>), Hebrew (<c>he</c>), Pashto (<c>ps</c>), Sindhi (<c>sd</c>) and
/// Urdu (<c>ur</c>), which are <see cref="TextDirection.Rtl"/>.
/// </remarks>
public abstract partial class GlobalWidgetsLocalizations : WidgetsLocalizations
{
    /// <summary>
    /// Constructs an object that defines the localized values for the widgets library for the given
    /// <paramref name="textDirection"/>.
    /// </summary>
    protected GlobalWidgetsLocalizations(TextDirection textDirection)
    {
        TextDirection = textDirection;
    }

    public override TextDirection TextDirection { get; }

    /// <summary>A <see cref="LocalizationsDelegate{T}"/> for <see cref="WidgetsLocalizations"/>.</summary>
    public static LocalizationsDelegate<WidgetsLocalizations> Delegate { get; } =
        new WidgetsLocalizationsDelegate();

    private sealed class WidgetsLocalizationsDelegate : LocalizationsDelegate<WidgetsLocalizations>
    {
        private static readonly Dictionary<Locale, WidgetsLocalizations> LoadedTranslations = new();

        public override bool IsSupported(Locale locale) =>
            WidgetsSupportedLanguages.Contains(locale.LanguageCode);

        public override WidgetsLocalizations LoadTyped(Locale locale)
        {
            lock (LoadedTranslations)
            {
                if (LoadedTranslations.TryGetValue(locale, out WidgetsLocalizations? loaded))
                {
                    return loaded;
                }

                WidgetsLocalizations translation = GetWidgetsTranslation(locale)
                    ?? throw new InvalidOperationException(
                        $"GetWidgetsTranslation() called for unsupported locale \"{locale}\"");
                LoadedTranslations[locale] = translation;
                return translation;
            }
        }

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;

        public override string ToString() =>
            $"GlobalWidgetsLocalizations.delegate({WidgetsSupportedLanguages.Count} locales)";
    }
}
