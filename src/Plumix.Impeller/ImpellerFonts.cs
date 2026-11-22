using System.Runtime.InteropServices;
using HarfBuzzSharp;
using NImpeller;
using Plumix.UI;
using HBBuffer = HarfBuzzSharp.Buffer;
using HBFont = HarfBuzzSharp.Font;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. The Impeller prototype's text engine (PRD decision 5): fonts
// are loaded once into both a HarfBuzz face (shaping, metrics) and an Impeller typeface (glyph runs), and
// HeadlessParagraph does the line layout over the shaped runs (UI/TextShaping.cs). Font matching is
// deliberately small: registered families, then the platform's default sans-serif, with a per-cluster
// fallback to the default face for code points the chosen face lacks. Weights a face lacks are synthesized
// (fake bold), as SkParagraph does; variable-font axes are not applied yet.

/// <summary>One loaded font face.</summary>
internal sealed class ImpellerFontFace
{
    private readonly HBFont _font;
    private readonly int _unitsPerEm;

    private ImpellerFontFace(string family, HBFont font, int unitsPerEm, ImpellerTypeface typeface,
        int weight, bool italic)
    {
        Family = family;
        _font = font;
        _unitsPerEm = unitsPerEm;
        Typeface = typeface;
        Weight = weight;
        Italic = italic;
        _font.TryGetHorizontalFontExtents(out FontExtents extents);
        Ascent = extents.Ascender / (double)unitsPerEm;
        Descent = -extents.Descender / (double)unitsPerEm;
    }

    public string Family { get; }

    public ImpellerTypeface Typeface { get; }

    public int Weight { get; }

    public bool Italic { get; }

    public double Ascent { get; }

    public double Descent { get; }

    /// <summary>Loads <paramref name="bytes"/>' first face, or null when neither engine can read it.</summary>
    public static ImpellerFontFace? Load(
        byte[] bytes, string family, int weight = 400, bool italic = false, int faceIndex = 0)
    {
        using var memory = new ImpellerUnmanagedMemory(bytes);
        if (ImpellerTypeface.New(memory, (uint)faceIndex) is not { } typeface)
        {
            return null;
        }

        var pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var blob = new Blob(pinned.AddrOfPinnedObject(), bytes.Length, MemoryMode.ReadOnly, () => pinned.Free());
        var face = new Face(blob, faceIndex);
        int unitsPerEm = face.UnitsPerEm > 0 ? face.UnitsPerEm : 1000;
        var font = new HBFont(face);
        font.SetFunctionsOpenType();
        font.SetScale(unitsPerEm, unitsPerEm);
        return new ImpellerFontFace(family, font, unitsPerEm, typeface, weight, italic);
    }

    /// <summary>Whether the face maps <paramref name="codePoint"/> to a glyph.</summary>
    public bool HasGlyph(int codePoint) => _font.TryGetGlyph((uint)codePoint, out uint glyph) && glyph != 0;

    /// <summary>Shapes <paramref name="text"/>[start, end) at <paramref name="fontSize"/>, in cluster order.</summary>
    public List<(int Cluster, int Glyph, double Advance, double OffsetX, double OffsetY)> Shape(
        string text, int start, int end, double fontSize, string? locale)
    {
        using var buffer = new HBBuffer();
        // HarfBuzz would shape control characters as .notdef; the paragraph gives them no width anyway.
        string shapingText = text.Replace('\t', ' ').Replace('\r', '‌').Replace('\n', '‌');
        buffer.AddUtf16(shapingText.AsSpan(), start, end - start);
        buffer.GuessSegmentProperties();
        if (locale is not null)
        {
            buffer.Language = new Language(locale);
        }

        _font.Shape(buffer);
        double scale = fontSize / _unitsPerEm;
        ReadOnlySpan<HarfBuzzSharp.GlyphInfo> infos = buffer.GetGlyphInfoSpan();
        ReadOnlySpan<GlyphPosition> positions = buffer.GetGlyphPositionSpan();
        var glyphs = new List<(int, int, double, double, double)>(infos.Length);
        for (int index = 0; index < infos.Length; index++)
        {
            glyphs.Add(((int)infos[index].Cluster, (int)infos[index].Codepoint, positions[index].XAdvance * scale,
                positions[index].XOffset * scale, positions[index].YOffset * scale));
        }

        if (buffer.Direction == Direction.RightToLeft)
        {
            glyphs.Reverse();
        }

        return glyphs;
    }
}

/// <summary>The Impeller prototype's fonts and HarfBuzz run shaping.</summary>
internal sealed class ImpellerTextShaper : ITextShaper
{
    private readonly object _gate = new();
    private readonly Dictionary<string, List<ImpellerFontFace>> _families = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(ImpellerFontFace, bool, bool), ShapingFace> _shapingFaces = [];
    private readonly List<(string Path, int Index, Lazy<ImpellerFontFace?> Face)> _fallbacks = [];
    private ImpellerFontFace? _default;

    /// <summary>The face used when no requested family is registered, and for missing code points.</summary>
    public ImpellerFontFace Default => _default ?? throw new InvalidOperationException("No default font loaded.");

    /// <summary>Loads the platform default font from the first of <paramref name="paths"/> that exists.</summary>
    public void LoadDefault(params string[] paths)
    {
        foreach (string path in paths)
        {
            if (File.Exists(path) && ImpellerFontFace.Load(File.ReadAllBytes(path), "sans-serif") is { } face)
            {
                _default = face;
                Add(face);
                return;
            }
        }

        throw new FileNotFoundException("None of the default font files exists.", paths.FirstOrDefault());
    }

    /// <summary>
    /// Appends a platform fallback font (Android's unnamed fonts.xml families), loaded the first time a
    /// cluster needs a code point no earlier face has.
    /// </summary>
    public void AddFallback(string path, int faceIndex)
    {
        lock (_gate)
        {
            if (_fallbacks.Any(fallback => fallback.Path == path && fallback.Index == faceIndex))
            {
                return;
            }

            _fallbacks.Add((path, faceIndex, new Lazy<ImpellerFontFace?>(() => File.Exists(path)
                ? ImpellerFontFace.Load(File.ReadAllBytes(path), string.Empty, faceIndex: faceIndex)
                : null)));
        }
    }

    public bool Register(ReadOnlyMemory<byte> bytes, string? family)
    {
        if (ImpellerFontFace.Load(bytes.ToArray(), family ?? string.Empty) is not { } face)
        {
            return false;
        }

        Add(face);
        return true;
    }

    public ShapingFace Face(ResolvedTextStyle style) => ShapingFaceOf(Resolve(style), style);

    public IReadOnlyList<ShapedGlyphInfo> Shape(string text, int start, int end, ResolvedTextStyle style)
    {
        ImpellerFontFace face = Resolve(style);
        ShapingFace shapingFace = ShapingFaceOf(face, style);
        var primary = face.Shape(text, start, end, style.FontSize, style.Locale);
        var glyphs = new List<ShapedGlyphInfo>(primary.Count);
        // Each fallback face shapes the whole run once, for context; its glyphs replace a missing cluster's.
        var fallbackRuns = new Dictionary<ImpellerFontFace,
            List<(int Cluster, int Glyph, double Advance, double OffsetX, double OffsetY)>>();
        int skipCluster = -1;
        foreach ((int cluster, int glyph, double advance, double offsetX, double offsetY) in primary)
        {
            if (cluster == skipCluster)
            {
                continue;
            }

            if (glyph == 0 && !char.IsWhiteSpace(text[cluster])
                && FallbackFor(CodePointAt(text, cluster), face) is { } fallback)
            {
                // SkParagraph's font fallback, per cluster: the first face that has the code point.
                if (!fallbackRuns.TryGetValue(fallback, out var run))
                {
                    fallbackRuns[fallback] = run = fallback.Shape(text, start, end, style.FontSize, style.Locale);
                }

                ShapingFace fallbackFace = ShapingFaceOf(fallback, style);
                foreach ((int fbCluster, int fbGlyph, double fbAdvance, double fbX, double fbY) in run)
                {
                    if (fbCluster == cluster)
                    {
                        glyphs.Add(new ShapedGlyphInfo(fbCluster, fbGlyph, fbAdvance, fbX, fbY, fallbackFace));
                    }
                }

                skipCluster = cluster;
                continue;
            }

            glyphs.Add(new ShapedGlyphInfo(cluster, glyph, advance, offsetX, offsetY, shapingFace));
        }

        return glyphs;
    }

    private static int CodePointAt(string text, int index) =>
        char.IsSurrogatePair(text, index) ? char.ConvertToUtf32(text, index) : text[index];

    private ImpellerFontFace? FallbackFor(int codePoint, ImpellerFontFace primary)
    {
        if (!ReferenceEquals(primary, Default) && Default.HasGlyph(codePoint))
        {
            return Default;
        }

        (string Path, int Index, Lazy<ImpellerFontFace?> Face)[] fallbacks;
        lock (_gate)
        {
            fallbacks = [.. _fallbacks];
        }

        foreach ((_, _, Lazy<ImpellerFontFace?> lazy) in fallbacks)
        {
            if (lazy.Value is { } face && !ReferenceEquals(face, primary) && face.HasGlyph(codePoint))
            {
                return face;
            }
        }

        return null;
    }

    private void Add(ImpellerFontFace face)
    {
        lock (_gate)
        {
            if (!_families.TryGetValue(face.Family, out List<ImpellerFontFace>? faces))
            {
                _families[face.Family] = faces = [];
            }

            faces.Add(face);
        }
    }

    private ImpellerFontFace Resolve(ResolvedTextStyle style)
    {
        lock (_gate)
        {
            foreach (string family in Families(style))
            {
                if (_families.TryGetValue(family, out List<ImpellerFontFace>? faces) && faces.Count > 0)
                {
                    bool italic = style.FontStyle == FontStyle.Italic;
                    int weight = style.FontWeight.Value;
                    return faces.OrderBy(face => face.Italic == italic ? 0 : 1)
                        .ThenBy(face => Math.Abs(face.Weight - weight))
                        .First();
                }
            }
        }

        return Default;
    }

    private static IEnumerable<string> Families(ResolvedTextStyle style)
    {
        if (style.FontFamily is { } family)
        {
            yield return StripPackage(family);
        }

        if (style.FontFamilyFallback is { } fallbacks)
        {
            foreach (string fallback in fallbacks)
            {
                yield return StripPackage(fallback);
            }
        }
    }

    // `packages/<package>/<family>` names the family a package registered as `<family>`.
    private static string StripPackage(string family) =>
        family.StartsWith("packages/", StringComparison.Ordinal) && family.IndexOf('/', 9) is > 0 and int slash
            ? family[(slash + 1)..]
            : family;

    private ShapingFace ShapingFaceOf(ImpellerFontFace face, ResolvedTextStyle style)
    {
        int weight = style.FontWeight.Value;
        bool fakeBold = weight >= 600 && weight - face.Weight >= 200;
        bool fakeItalic = style.FontStyle == FontStyle.Italic && !face.Italic;
        lock (_gate)
        {
            if (!_shapingFaces.TryGetValue((face, fakeBold, fakeItalic), out ShapingFace? shapingFace))
            {
                shapingFace = new ShapingFace(face, face.Ascent, face.Descent, fakeBold, fakeItalic);
                _shapingFaces[(face, fakeBold, fakeItalic)] = shapingFace;
            }

            return shapingFace;
        }
    }
}

/// <summary>The Impeller prototype's text engine: HeadlessParagraph layout over HarfBuzz-shaped runs.</summary>
internal sealed class ImpellerTextBackend(ImpellerTextShaper shaper) : ITextBackend
{
    public Paragraph CreateParagraph(ParagraphContent content) => new HeadlessParagraph(content, shaper);

    public bool RegisterFont(ReadOnlyMemory<byte> bytes, string? family) => shaper.Register(bytes, family);
}
