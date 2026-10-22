using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Plumix.UI;

// C#-only infrastructure: browser ICU word segmentation, matching Flutter's web text fragmenter.
internal static partial class BrowserTextSegmentation
{
    private const string Module = "plumix-text-segmentation";

    public static async Task InitializeAsync()
    {
        await JSHost.ImportAsync(Module, "./plumix-text-segmentation.js");
        TextSegmentation.WordBoundaryProvider = GetWordBoundaries;
    }

    [JSImport("getWordBoundaries", Module)]
    private static partial int[]? GetWordBoundaries(string text);
}
