namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/font_loader.dart

/// <summary>
/// A class that enables the dynamic loading of fonts at runtime. A font family registered here is
/// available to any text style that names it in <c>FontFamily</c>.
/// </summary>
/// <example>
/// <code>
/// var loader = new FontLoader("Inter");
/// loader.AddFont(PlatformAssetBundle.Root.LoadBytesAsync("fonts/Inter-Regular.ttf"));
/// await loader.Load();
/// </code>
/// </example>
public class FontLoader
{
    private readonly List<Task<ReadOnlyMemory<byte>>> _fontFutures = [];
    private bool _loaded;

    /// <summary>Creates a new loader for the font family named <paramref name="family"/>.</summary>
    public FontLoader(string family)
    {
        Family = family;
    }

    /// <summary>The font family being loaded. A style uses it by this name.</summary>
    public string Family { get; }

    /// <summary>
    /// Registers a future that holds the bytes of a font file in TrueType or OpenType format. Each
    /// font file describes one style and weight of the family. Returns this loader so calls chain.
    /// </summary>
    public FontLoader AddFont(Task<ReadOnlyMemory<byte>> bytes)
    {
        if (_loaded)
        {
            throw new InvalidOperationException("FontLoader is already loaded");
        }

        _fontFutures.Add(bytes);
        return this;
    }

    /// <summary>Registers the bytes of a font file that are already in memory.</summary>
    public FontLoader AddFont(ReadOnlyMemory<byte> bytes) => AddFont(Task.FromResult(bytes));

    /// <summary>
    /// Loads this font loader's font files and adds them to the platform font collection. When every
    /// font's bytes are already available the fonts are registered before this method returns.
    /// </summary>
    public async Task Load()
    {
        if (_loaded)
        {
            throw new InvalidOperationException("FontLoader is already loaded");
        }

        _loaded = true;
        foreach (Task<ReadOnlyMemory<byte>> fontFuture in _fontFutures)
        {
            await LoadFont(await fontFuture.ConfigureAwait(false), Family).ConfigureAwait(false);
        }
    }

    /// <summary>Hook called to load a font asset into the engine; tests override it.</summary>
    protected virtual Task LoadFont(ReadOnlyMemory<byte> list, string family) =>
        PlatformFonts.LoadFontFromList(list, fontFamily: family);
}

/// <summary>The engine's font collection (dart:ui <c>loadFontFromList</c>).</summary>
public static class PlatformFonts
{
    /// <summary>
    /// Loads a font from <paramref name="list"/> into the font collection under
    /// <paramref name="fontFamily"/>, or under the family name stored in the font when none is given.
    /// Text laid out before the family was available is laid out again.
    /// </summary>
    public static Task LoadFontFromList(ReadOnlyMemory<byte> list, string? fontFamily = null)
    {
        if (!AvaloniaFonts.Register(list, fontFamily))
        {
            throw new ArgumentException("Invalid font data", nameof(list));
        }

        return Task.CompletedTask;
    }
}
