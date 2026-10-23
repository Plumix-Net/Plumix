using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using HarfBuzzSharp;
using HBBuffer = HarfBuzzSharp.Buffer;
using HBFont = HarfBuzzSharp.Font;
using RunGlyph = Avalonia.Media.TextFormatting.GlyphInfo;

namespace Plumix.UI;

// C#-only infrastructure: HarfBuzz variable instances over Avalonia's resolved fallback face. The
// native draw bridge supplies outlines from the SAME instance that shapes advances. HarfBuzzSharp
// exposes shaping but not hb_font_set_variations/hb_font_draw_glyph; hosts already ship this ABI.
internal sealed class VariableFont : IDisposable
{
    private readonly Face _face;
    private readonly HBFont _font;
    private readonly double _scale;

    internal VariableFont(GlyphTypeface typeface, ResolvedTextStyle style)
    {
        _face = new Face((_, tag) => ReadTable(typeface, (uint)tag))
        {
            UnitsPerEm = typeface.Metrics.DesignEmHeight,
        };
        _font = new HBFont(_face);
        _font.SetFunctionsOpenType();
        _font.SetScale((int)_face.UnitsPerEm, (int)_face.UnitsPerEm);
        _scale = style.FontSize / _face.UnitsPerEm;
        var axes = new Dictionary<string, float> { ["wght"] = (int)style.FontWeight };
        if (style.FontVariations is { } variations)
        {
            foreach (FontVariation variation in variations)
            {
                axes[variation.Axis] = (float)variation.Value;
            }
        }

        NativeVariation[] values = axes.Select(axis => new NativeVariation(Tag(axis.Key), axis.Value)).ToArray();
        SetVariations(_font.Handle, values, (uint)values.Length);
    }

    internal static bool IsVariable(GlyphTypeface typeface) =>
        typeface.PlatformTypeface.TryGetTable(Tag("fvar"), out _);

    private static Blob? ReadTable(GlyphTypeface typeface, uint tag)
    {
        if (!typeface.PlatformTypeface.TryGetTable(tag, out ReadOnlyMemory<byte> table))
        {
            return null;
        }

        byte[] bytes = table.ToArray();
        var pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        return new Blob(pinned.AddrOfPinnedObject(), bytes.Length, MemoryMode.ReadOnly, () => pinned.Free());
    }

    internal ShapedBuffer Shape(
        string text,
        int start,
        int length,
        GlyphTypeface typeface,
        ResolvedTextStyle style,
        sbyte bidiLevel)
    {
        using var buffer = new HBBuffer();
        string shapingText = text.Replace('\t', ' ').Replace('\r', '\u200C').Replace('\n', '\u200C');
        buffer.AddUtf16(shapingText.AsSpan(), start, length);
        buffer.GuessSegmentProperties();
        buffer.Direction = (bidiLevel & 1) == 0 ? Direction.LeftToRight : Direction.RightToLeft;
        if (style.Locale is string locale)
        {
            buffer.Language = new Language(locale);
        }

        Feature[] features = style.FontFeatures?.Select(feature =>
            Feature.Parse($"{feature.Feature}={feature.Value}")).ToArray() ?? [];
        // Same buffer form as Avalonia's HarfBuzzTextShaper: HarfBuzz's visual glyph order and
        // clusters relative to the shaped slice.
        _font.Shape(buffer, features);
        var result = new ShapedBuffer(text.AsMemory(start, length), buffer.Length, typeface, style.FontSize, bidiLevel);
        ReadOnlySpan<HarfBuzzSharp.GlyphInfo> glyphs = buffer.GetGlyphInfoSpan();
        ReadOnlySpan<GlyphPosition> positions = buffer.GetGlyphPositionSpan();
        for (int index = 0; index < buffer.Length; index++)
        {
            result[index] = new RunGlyph(
                (ushort)glyphs[index].Codepoint,
                (int)glyphs[index].Cluster - start,
                positions[index].XAdvance * _scale,
                new Vector(positions[index].XOffset * _scale, -positions[index].YOffset * _scale));
        }

        return result;
    }

    internal (double Ascent, double Descent, double Gap)? Metrics =>
        _font.TryGetHorizontalFontExtents(out FontExtents metrics)
            ? (metrics.Ascender * _scale, -metrics.Descender * _scale, metrics.LineGap * _scale)
            : null;

    internal Geometry Outline(GlyphRun run)
    {
        var geometry = new StreamGeometry();
        using StreamGeometryContext context = geometry.Open();
        var target = new OutlineTarget(context, _scale);
        var handle = GCHandle.Alloc(target);
        try
        {
            double x = 0.0;
            foreach (RunGlyph glyph in run.GlyphInfos)
            {
                target.Origin = new Point(x + glyph.GlyphOffset.X, glyph.GlyphOffset.Y + run.BaselineOrigin.Y);
                DrawGlyph(_font.Handle, glyph.GlyphIndex, DrawFunctions.Value, GCHandle.ToIntPtr(handle));
                target.Close();
                x += glyph.GlyphAdvance;
            }
        }
        finally
        {
            handle.Free();
        }

        return geometry;
    }

    public void Dispose()
    {
        _font.Dispose();
        _face.Dispose();
    }

    private static uint Tag(string tag) =>
        ((uint)tag[0] << 24) | ((uint)tag[1] << 16) | ((uint)tag[2] << 8) | tag[3];

    private sealed class OutlineTarget(StreamGeometryContext context, double scale)
    {
        internal Point Origin;
        private bool _open;
        internal Point Point(float x, float y) => new(Origin.X + x * scale, Origin.Y - y * scale);
        internal void Move(float x, float y)
        {
            Close();
            context.BeginFigure(Point(x, y), true);
            _open = true;
        }

        internal void Line(float x, float y) => context.LineTo(Point(x, y));
        internal void Quad(float x, float y, float endX, float endY) =>
            context.QuadraticBezierTo(Point(x, y), Point(endX, endY));
        internal void Cubic(float x, float y, float x2, float y2, float endX, float endY) =>
            context.CubicBezierTo(Point(x, y), Point(x2, y2), Point(endX, endY));
        internal void Close()
        {
            if (_open)
            {
                context.EndFigure(true);
                _open = false;
            }
        }
    }

    private static OutlineTarget Target(IntPtr data) => (OutlineTarget)GCHandle.FromIntPtr(data).Target!;
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Move(IntPtr funcs, IntPtr data, IntPtr state, float x, float y, IntPtr user) =>
        Target(data).Move(x, y);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Line(IntPtr funcs, IntPtr data, IntPtr state, float x, float y, IntPtr user) =>
        Target(data).Line(x, y);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Quad(
        IntPtr funcs, IntPtr data, IntPtr state, float x, float y, float ex, float ey, IntPtr user) =>
        Target(data).Quad(x, y, ex, ey);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Cubic(
        IntPtr funcs, IntPtr data, IntPtr state, float x, float y, float x2, float y2,
        float ex, float ey, IntPtr user) => Target(data).Cubic(x, y, x2, y2, ex, ey);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Close(IntPtr funcs, IntPtr data, IntPtr state, IntPtr user) => Target(data).Close();

    private static readonly Lazy<IntPtr> DrawFunctions = new(CreateCallbacks);

    private static unsafe IntPtr CreateCallbacks()
    {
        IntPtr functions = CreateDrawFunctions();
        SetMove(functions,
            (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, float, float, IntPtr, void>)&Move,
            IntPtr.Zero, IntPtr.Zero);
        SetLine(functions,
            (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, float, float, IntPtr, void>)&Line,
            IntPtr.Zero, IntPtr.Zero);
        SetQuad(functions,
            (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, float, float, float, float, IntPtr, void>)&Quad,
            IntPtr.Zero, IntPtr.Zero);
        SetCubic(functions,
            (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, float, float, float, float,
                float, float, IntPtr, void>)&Cubic, IntPtr.Zero, IntPtr.Zero);
        SetClose(functions, (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>)&Close,
            IntPtr.Zero, IntPtr.Zero);
        MakeImmutable(functions);
        return functions;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativeVariation(uint Tag, float Value);

    // Match HarfBuzzSharp's import names, including its packaged iOS/Mac Catalyst framework.
    private static bool AppleFramework => OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()
                                          || OperatingSystem.IsMacCatalyst();

    private static void SetVariations(IntPtr font, NativeVariation[] values, uint count)
    {
        if (AppleFramework) Apple.SetVariations(font, values, count);
        else Native.SetVariations(font, values, count);
    }

    private static void DrawGlyph(IntPtr font, uint glyph, IntPtr functions, IntPtr data)
    {
        if (AppleFramework) Apple.DrawGlyph(font, glyph, functions, data);
        else Native.DrawGlyph(font, glyph, functions, data);
    }

    private static IntPtr CreateDrawFunctions() =>
        AppleFramework ? Apple.CreateDrawFunctions() : Native.CreateDrawFunctions();

    private static void MakeImmutable(IntPtr functions)
    {
        if (AppleFramework) Apple.MakeImmutable(functions);
        else Native.MakeImmutable(functions);
    }

    private static void SetMove(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy)
    {
        if (AppleFramework) Apple.SetMove(functions, callback, user, destroy);
        else Native.SetMove(functions, callback, user, destroy);
    }

    private static void SetLine(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy)
    {
        if (AppleFramework) Apple.SetLine(functions, callback, user, destroy);
        else Native.SetLine(functions, callback, user, destroy);
    }

    private static void SetQuad(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy)
    {
        if (AppleFramework) Apple.SetQuad(functions, callback, user, destroy);
        else Native.SetQuad(functions, callback, user, destroy);
    }

    private static void SetCubic(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy)
    {
        if (AppleFramework) Apple.SetCubic(functions, callback, user, destroy);
        else Native.SetCubic(functions, callback, user, destroy);
    }

    private static void SetClose(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy)
    {
        if (AppleFramework) Apple.SetClose(functions, callback, user, destroy);
        else Native.SetClose(functions, callback, user, destroy);
    }

    private static class Native
    {
        private const string Library = "libHarfBuzzSharp";
        [DllImport(Library, EntryPoint = "hb_font_set_variations",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetVariations(IntPtr font, [In] NativeVariation[] values, uint count);
        [DllImport(Library, EntryPoint = "hb_font_draw_glyph",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DrawGlyph(IntPtr font, uint glyph, IntPtr functions, IntPtr data);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_create",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CreateDrawFunctions();
        [DllImport(Library, EntryPoint = "hb_draw_funcs_make_immutable",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void MakeImmutable(IntPtr functions);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_move_to_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetMove(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_line_to_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetLine(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_quadratic_to_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetQuad(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_cubic_to_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetCubic(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_close_path_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetClose(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
    }
    private static class Apple
    {
        private const string Library = "@rpath/libHarfBuzzSharp.framework/libHarfBuzzSharp";
        [DllImport(Library, EntryPoint = "hb_font_set_variations",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetVariations(IntPtr font, [In] NativeVariation[] values, uint count);
        [DllImport(Library, EntryPoint = "hb_font_draw_glyph",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DrawGlyph(IntPtr font, uint glyph, IntPtr functions, IntPtr data);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_create",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CreateDrawFunctions();
        [DllImport(Library, EntryPoint = "hb_draw_funcs_make_immutable",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void MakeImmutable(IntPtr functions);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_move_to_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetMove(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_line_to_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetLine(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_quadratic_to_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetQuad(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_cubic_to_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetCubic(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
        [DllImport(Library, EntryPoint = "hb_draw_funcs_set_close_path_func",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetClose(IntPtr functions, IntPtr callback, IntPtr user, IntPtr destroy);
    }
}
