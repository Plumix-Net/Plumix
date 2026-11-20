using Plumix.Rendering;
using Plumix.UI;

// C#-only test infrastructure; no Dart parity source.

namespace Plumix.Tests;

/// <summary>The fixture fonts, registered through <see cref="FontLoader"/> under their family names.</summary>
internal static class TestFonts
{
    private static readonly Lazy<string> AhemFamily = new(() => Load("Ahem", "ahem.ttf"));
    private static readonly Lazy<string> RobotoSlabFamily =
        new(() => Load("Roboto Slab", "RobotoSlab-VariableFont_wght.ttf"));

    public static string Ahem => AhemFamily.Value;

    public static string RobotoSlab => RobotoSlabFamily.Value;

    private static string Load(string family, string file)
    {
        Task load = new FontLoader(family)
            .AddFont(PlatformAssetBundle.Root.LoadBytesAsync($"packages/Plumix.Tests/Fixtures/Fonts/{file}"))
            .Load();
        // The bundle's bytes are in memory, so the family is registered before Load returns.
        if (!load.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException($"{file} did not load synchronously.");
        }

        return family;
    }
}
