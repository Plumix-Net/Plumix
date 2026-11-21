using Avalonia.Platform;
using Plumix.UI;

// C#-only host infrastructure; no Dart parity source. Avalonia's embedded resources (AvaloniaResource
// items) as the framework's asset source (IAssetSource, src/Plumix/UI/ImageBackend.cs).

namespace Plumix;

/// <summary>Opens bundled assets through Avalonia's asset loader.</summary>
internal sealed class AvaloniaAssetSource : IAssetSource
{
    internal static readonly AvaloniaAssetSource Instance = new();

    private AvaloniaAssetSource()
    {
    }

    public Stream Open(Uri uri, Uri? baseUri) => AssetLoader.Open(uri, baseUri);

    public IEnumerable<Uri> GetAssets(Uri directory, Uri? baseUri) => AssetLoader.GetAssets(directory, baseUri);
}
