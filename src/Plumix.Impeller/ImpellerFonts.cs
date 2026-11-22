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
// deliberately small: registered families, then the platform's named families (fonts.xml), then the default
// sans-serif, with a per-cluster fallback for code points the chosen face lacks. A face's weight and style
// come from its OS/2 table; a variable face is instanced at the requested weight (HarfBuzzInstancer), and weights
// a static family lacks are synthesized (fake bold), as SkParagraph does.

/// <summary>One loaded font face.</summary>
internal sealed class ImpellerFontFace
{
    private readonly HBFont _font;
    private readonly int _unitsPerEm;

    private readonly Face _face;
    private readonly Dictionary<int, ImpellerFontFace?> _instances = [];

    private ImpellerFontFace(string family, Face face, HBFont font, int unitsPerEm, ImpellerTypeface typeface,
        int weight, bool italic)
    {
        Family = family;
        _face = face;
        _font = font;
        _unitsPerEm = unitsPerEm;
        Typeface = typeface;
        Weight = weight;
        Italic = italic;
        _font.TryGetHorizontalFontExtents(out FontExtents extents);
        Ascent = extents.Ascender / (double)unitsPerEm;
        Descent = -extents.Descender / (double)unitsPerEm;
        WeightAxis = HarfBuzzInstancer.WeightAxis(face);
    }

    /// <summary>The variable font's weight axis (minimum, default, maximum); null for a static face.</summary>
    public (float Min, float Default, float Max)? WeightAxis { get; }

    /// <summary>
    /// This face at <paramref name="weight"/>: itself when static or already that weight, otherwise a static
    /// instance with the weight axis pinned (made once per weight; null when HarfBuzz cannot instance it).
    /// </summary>
    public ImpellerFontFace WithWeight(double weight)
    {
        if (WeightAxis is not { } axis)
        {
            return this;
        }

        int pinned = (int)Math.Round(Math.Clamp(weight, axis.Min, axis.Max));
        if (Math.Abs(pinned - axis.Default) < 0.5f)
        {
            return this;
        }

        lock (_instances)
        {
            if (!_instances.TryGetValue(pinned, out ImpellerFontFace? instance))
            {
                _instances[pinned] = instance = HarfBuzzInstancer.Instantiate(_face, pinned) is { } bytes
                    ? Load(bytes, Family, pinned, Italic)
                    : null;
            }

            return instance ?? this;
        }
    }

    public string Family { get; }

    public ImpellerTypeface Typeface { get; }

    public int Weight { get; }

    public bool Italic { get; }

    public double Ascent { get; }

    public double Descent { get; }

    /// <summary>
    /// Loads <paramref name="bytes"/>' face <paramref name="faceIndex"/>, or null when neither engine can read
    /// it. A weight or style not given is read from the face's OS/2 table, as Skia's typeface does.
    /// </summary>
    public static ImpellerFontFace? Load(
        byte[] bytes, string family, int? weight = null, bool? italic = null, int faceIndex = 0)
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
        (int os2Weight, bool os2Italic) = StyleOf(face);
        return new ImpellerFontFace(family, face, font, unitsPerEm, typeface, weight ?? os2Weight,
            italic ?? os2Italic);
    }

    // OS/2 usWeightClass (offset 4) and fsSelection's ITALIC bit (offset 62, bit 0); regular without the table.
    private static (int Weight, bool Italic) StyleOf(Face face)
    {
        using Blob table = face.ReferenceTable(Tag.Parse("OS/2"));
        ReadOnlySpan<byte> os2 = table.AsSpan();
        if (os2.Length < 64)
        {
            return (400, false);
        }

        int weight = (os2[4] << 8) | os2[5];
        bool italic = (os2[63] & 1) != 0;
        return (weight is >= 1 and <= 1000 ? weight : 400, italic);
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
    private readonly Dictionary<string, List<SystemFace>> _systemFamilies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string, int, int?, bool), Lazy<ImpellerFontFace?>> _systemFaceCache = [];
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

    /// <summary>
    /// Declares a named platform family's face (Android's named fonts.xml families: sans-serif, serif,
    /// monospace...), loaded the first time a style asks for that family. <paramref name="weight"/> is the
    /// weight the configuration declares; a variable instance (declared through axes the prototype cannot
    /// apply) passes <paramref name="actualWeight"/> null, so the file's own weight decides fake bold.
    /// </summary>
    public void AddSystemFace(string family, string path, int faceIndex, int weight, bool italic, int? actualWeight)
    {
        lock (_gate)
        {
            if (!_systemFamilies.TryGetValue(family, out List<SystemFace>? faces))
            {
                _systemFamilies[family] = faces = [];
            }

            // A variable font is listed once per instance: its instances share one loaded face.
            var key = (path, faceIndex, actualWeight, italic);
            if (!_systemFaceCache.TryGetValue(key, out Lazy<ImpellerFontFace?>? face))
            {
                _systemFaceCache[key] = face = new Lazy<ImpellerFontFace?>(() =>
                    File.Exists(path)
                        ? ImpellerFontFace.Load(File.ReadAllBytes(path), family, actualWeight, italic, faceIndex)
                        : null);
            }

            faces.Add(new SystemFace(weight, italic, face));
        }
    }

    /// <summary>Makes <paramref name="name"/> another name of the platform family <paramref name="family"/>.</summary>
    public void AddSystemAlias(string name, string family)
    {
        lock (_gate)
        {
            _aliases.TryAdd(name, family);
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

    // The matched face, at the requested weight when it is variable: a `wght` font variation wins over the weight,
    // as in SkParagraph.
    private ImpellerFontFace Resolve(ResolvedTextStyle style)
    {
        double weight = style.FontVariations?.LastOrDefault(variation => variation.Axis == "wght")?.Value
                        ?? style.FontWeight.Value;
        return Match(style).WithWeight(weight);
    }

    private ImpellerFontFace Match(ResolvedTextStyle style)
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

                if (SystemFaceOf(family, style) is { } systemFace)
                {
                    return systemFace;
                }
            }
        }

        return Default;
    }

    // A platform family (through its aliases): the declared face closest in style, loaded on first use.
    private ImpellerFontFace? SystemFaceOf(string family, ResolvedTextStyle style)
    {
        string name = _aliases.TryGetValue(family, out string? target) ? target : family;
        if (!_systemFamilies.TryGetValue(name, out List<SystemFace>? faces))
        {
            return null;
        }

        bool italic = style.FontStyle == FontStyle.Italic;
        int weight = style.FontWeight.Value;
        return faces.OrderBy(face => face.Italic == italic ? 0 : 1)
            .ThenBy(face => Math.Abs(face.Weight - weight))
            .Select(face => face.Face.Value)
            .FirstOrDefault(face => face is not null);
    }

    private sealed record SystemFace(int Weight, bool Italic, Lazy<ImpellerFontFace?> Face);

    // A package's family is registered and requested as `packages/<package>/<family>` (FontLoader,
    // TextStyle.package); the bare family is tried too, for fonts a package registered without the prefix.
    private static IEnumerable<string> Families(ResolvedTextStyle style)
    {
        IEnumerable<string> requested = style.FontFamily is { } family ? [family] : [];
        foreach (string name in requested.Concat(style.FontFamilyFallback ?? []))
        {
            yield return name;
            if (StripPackage(name) is { } bare && bare != name)
            {
                yield return bare;
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
