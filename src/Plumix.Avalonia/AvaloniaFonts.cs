using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Plumix.UI;

// C#-only host infrastructure: no Dart parity source.
//
// The Avalonia engine side of `loadFontFromList` (ITextBackend.RegisterFont, through AvaloniaTextBackend): fonts
// registered at run time live in one Avalonia font collection (`fonts:Plumix`) under the family name they were loaded
// with, and the paragraph builders turn a style's family strings into an Avalonia `FontFamily` here. Registration can
// come before Avalonia's platform is set up (a `FontLoader` in `Main`), so fonts wait in a queue until the collection
// can be created. A process can also see more than one Avalonia font manager (headless test sessions each set up their
// own), so every loaded font is kept and registered again with a new manager.
internal static class AvaloniaFonts
{
    private static readonly Uri CollectionKey = new("fonts:Plumix");
    private static readonly object Gate = new();
    private static readonly List<(ReadOnlyMemory<byte> Bytes, string? Family)> Pending = [];
    private static readonly List<(ReadOnlyMemory<byte> Bytes, string? Family)> Loaded = [];
    private static readonly HashSet<string> Registered = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> RequestedMissing = new(StringComparer.OrdinalIgnoreCase);
    private static RuntimeFontCollection? s_collection;
    private static FontManager? s_fontManager;

    /// <summary>Adds a font file; false when the bytes are not a font Avalonia can read.</summary>
    public static bool Register(ReadOnlyMemory<byte> bytes, string? family)
    {
        bool notify;
        lock (Gate)
        {
            Pending.Add((bytes, family));
            if (!TryFlush(out bool valid))
            {
                // Avalonia is not set up yet: the font is checked when the collection is created.
                return true;
            }

            if (!valid)
            {
                return false;
            }

            notify = family is not null && RequestedMissing.Remove(family);
        }

        if (notify)
        {
            // Text laid out with a fallback before the family arrived lays out again, like the
            // engine's `fontsChange` message after `loadFontFromList`.
            EventLoop.PostTask(() => _ = HostSystemFonts.NotifySystemFontsChanged());
        }

        return true;
    }

    /// <summary>
    /// The Avalonia family for a style's <paramref name="family"/> followed by its
    /// <paramref name="fallback"/> families, in the engine's lookup order.
    /// </summary>
    public static FontFamily Resolve(string? family, IReadOnlyList<string>? fallback = null)
    {
        var names = new List<string>();
        if (!string.IsNullOrEmpty(family))
        {
            names.Add(family);
        }

        if (fallback is not null)
        {
            names.AddRange(fallback.Where(name => !string.IsNullOrEmpty(name)));
        }

        if (names.Count == 0)
        {
            return FontFamily.Default;
        }

        lock (Gate)
        {
            TryFlush(out _);
            for (int index = 0; index < names.Count; index++)
            {
                if (Registered.Contains(names[index]))
                {
                    names[index] = $"{CollectionKey}#{names[index]}";
                }
                else
                {
                    RequestedMissing.Add(names[index]);
                }
            }
        }

        return new FontFamily(string.Join(',', names));
    }

    /// <summary>Whether a family was registered through <see cref="PlatformFonts.LoadFontFromList"/>.</summary>
    public static bool IsRegistered(string family)
    {
        lock (Gate)
        {
            TryFlush(out _);
            return Registered.Contains(family);
        }
    }

    // Called with Gate held. False when Avalonia cannot host the collection yet.
    private static bool TryFlush(out bool allValid)
    {
        allValid = true;
        FontManager fontManager;
        try
        {
            fontManager = FontManager.Current;
        }
        catch (InvalidOperationException)
        {
            // No Avalonia font manager yet.
            return false;
        }

        if (s_collection is null || !ReferenceEquals(fontManager, s_fontManager))
        {
            var collection = new RuntimeFontCollection(CollectionKey);
            fontManager.AddFontCollection(collection);
            s_collection = collection;
            s_fontManager = fontManager;
            foreach ((ReadOnlyMemory<byte> bytes, string? family) in Loaded)
            {
                collection.Add(bytes, family);
            }
        }

        foreach ((ReadOnlyMemory<byte> bytes, string? family) in Pending)
        {
            if (s_collection.Add(bytes, family) is { } name)
            {
                Registered.Add(name);
                Loaded.Add((bytes, family));
            }
            else
            {
                allValid = false;
            }
        }

        Pending.Clear();
        return true;
    }

    private sealed class RuntimeFontCollection(Uri key) : FontCollectionBase
    {
        public override Uri Key => key;

        // Returns the family the font is now known by, or null for unreadable data. Loading the
        // same family and face twice keeps the first file, as the engine's collection does.
        public string? Add(ReadOnlyMemory<byte> bytes, string? family)
        {
            // Adding under the font's own names fails for a face that is already there; the
            // typeface still comes back and joins the requested family.
            var stream = new MemoryStream(bytes.ToArray(), writable: false);
            TryAddGlyphTypeface(stream, out GlyphTypeface? glyphTypeface);
            if (glyphTypeface is null)
            {
                return null;
            }

            if (family is null)
            {
                return glyphTypeface.FamilyName;
            }

            var collectionKey = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);
            TryAddGlyphTypeface(family, collectionKey, glyphTypeface);
            return family;
        }
    }
}
