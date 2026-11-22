using NImpeller;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using Path = Plumix.UI.Path;

namespace Plumix.Impeller.Tests;

// C#-only tests of the Impeller backend; no Dart parity source.

public sealed class ImpellerBackendTests
{
    [Fact]
    public void Radii_OverflowingASide_ScaleDownTogether()
    {
        // A pill made with BorderRadius.circular(100): SkRRect scales every radius by 54 / 200.
        RRect pill = RRect.FromLTRBR(0, 0, 335.7, 54, Radius.Circular(100));

        ImpellerRoundingRadii radii = pill.Radii();

        Assert.Equal(27f, radii.Top_left.X, 3);
        Assert.Equal(27f, radii.Bottom_right.Y, 3);
    }

    [Fact]
    public void Radii_ThatFit_StayAsGiven()
    {
        RRect card = RRect.FromLTRBR(0, 0, 300, 120, Radius.Circular(12));

        ImpellerRoundingRadii radii = card.Radii();

        Assert.Equal(12f, radii.Top_right.X);
        Assert.Equal(12f, radii.Bottom_left.Y);
    }

    [Fact]
    public void InkSparkleProgram_MatchesTheEmbeddedBundle()
    {
        _ = ImpellerTestBackend.Shaper;
        using Stream stream = typeof(InkSparkle).Assembly.GetManifestResourceStream(
            "Plumix.Material.Assets.Shaders.ink_sparkle.sksl")!;

        RuntimeFragmentProgram program = RuntimeFragmentProgram.Compile(new StreamReader(stream).ReadToEnd());

        Assert.IsType<ImpellerFragmentProgram>(program.BackendState);
    }

    [Fact]
    public void UnknownProgram_HasNoBundle()
    {
        _ = ImpellerTestBackend.Shaper;

        RuntimeFragmentProgram program = RuntimeFragmentProgram.Compile(
            "uniform float u_unknown; half4 main(float2 p) { return half4(u_unknown); }");

        Assert.Null(program.BackendState);
    }

    [Fact]
    public void FontFace_ReadsWeightAndStyleFromOs2()
    {
        byte[] regular = File.ReadAllBytes(ImpellerTestBackend.Ahem);
        byte[] boldItalic = WithOs2(regular, weight: 700, italic: true);

        ImpellerFontFace? face = ImpellerFontFace.Load(regular, "Ahem");
        ImpellerFontFace? styled = ImpellerFontFace.Load(boldItalic, "Ahem");

        Assert.Equal((400, false), (face!.Weight, face.Italic));
        Assert.Equal((700, true), (styled!.Weight, styled.Italic));
    }

    [Fact]
    public void VariableFont_IsInstancedAtTheRequestedWeight()
    {
        ImpellerFontFace face = ImpellerFontFace.Load(File.ReadAllBytes(ImpellerTestBackend.RobotoSlab), "Slab")!;

        ImpellerFontFace bold = face.WithWeight(700);

        Assert.NotNull(face.WeightAxis);
        Assert.NotSame(face, bold);
        Assert.Equal(700, bold.Weight);
        Assert.Null(bold.WeightAxis);
        Assert.Same(bold, face.WithWeight(700));
        Assert.Same(face, face.WithWeight(face.WeightAxis!.Value.Default));
    }

    [Fact]
    public void Paragraph_DrawsAVariableFontsBoldInstance()
    {
        // RobotoSlab's bold glyphs are wider than its regular ones; a fake-bold regular face would keep the advances.
        Paragraph regular = ImpellerTestBackend.Paragraph("Weights", "RobotoSlab", fontSize: 40);
        Paragraph bold = ImpellerTestBackend.Paragraph("Weights", "RobotoSlab", fontSize: 40,
            fontWeight: FontWeight.Bold);

        Assert.True(bold.LongestLine > regular.LongestLine + 1,
            $"bold {bold.LongestLine} vs regular {regular.LongestLine}");
    }

    [Fact]
    public void Paragraph_ShapesARegisteredFamily()
    {
        // Ahem's glyphs are one em wide: five at 10 px are 50 px.
        Paragraph paragraph = ImpellerTestBackend.Paragraph("Hello", "Ahem");

        Assert.Equal(50, paragraph.LongestLine, 3);
        Assert.Equal(10, paragraph.Height, 3);
    }

    [Fact]
    public void Paragraph_ResolvesPlatformFamiliesAndAliases()
    {
        Paragraph monospace = ImpellerTestBackend.Paragraph("abcd", "monospace");
        Paragraph alias = ImpellerTestBackend.Paragraph("abcd", "courier");

        Assert.Equal(40, monospace.LongestLine, 3);
        Assert.Equal(40, alias.LongestLine, 3);
    }

    [Fact]
    public void Paragraph_AtZeroWidth_BreaksEveryCharacter()
    {
        // Flutter's SkParagraph: a word wider than the line breaks by grapheme, so `Hello` is five lines.
        Paragraph paragraph = ImpellerTestBackend.Paragraph("Hello", "Ahem", width: 0);

        Assert.Equal(50, paragraph.Height, 3);
    }

    [Fact]
    public void Picture_PlaysIntoADisplayList()
    {
        _ = ImpellerTestBackend.Shaper;
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        canvas.Save();
        canvas.ClipRRect(RRect.FromLTRBR(0, 0, 100, 40, Radius.Circular(20)));
        canvas.DrawRect(Rect.FromLTRB(0, 0, 100, 40), new Paint { Color = new Color(0xFF2196F3) });
        canvas.Restore();
        var path = new Path();
        path.AddOval(Rect.FromLTRB(10, 50, 50, 90));
        canvas.DrawPath(path, new Paint { Color = new Color(0x80000000), Style = PaintingStyle.Stroke });
        canvas.DrawShadow(path, new Color(0xFF000000), 4, transparentOccluder: false);
        Picture picture = recorder.EndRecording();
        int unsupported = ImpellerPaints.ReportedUnsupported.Count;

        using ImpellerDisplayListBuilder builder = ImpellerDisplayListBuilder.New(
            new ImpellerRect { X = 0, Y = 0, Width = 100, Height = 100 })!;
        ImpellerCanvasPlayer.Play(picture, builder, Offset.Zero);
        using ImpellerDisplayList? displayList = builder.CreateDisplayListNew();

        Assert.NotNull(displayList);
        Assert.Equal(unsupported, ImpellerPaints.ReportedUnsupported.Count);
    }

    [Fact]
    public void LayerTree_BuildsADisplayList()
    {
        _ = ImpellerTestBackend.Shaper;
        var recorder = new PictureRecorder();
        new Canvas(recorder).DrawCircle(new Offset(20, 20), 10, new Paint { Color = new Color(0xFFFF0000) });
        var root = new ContainerFlowLayer();
        var transform = new TransformFlowLayer(Matrix4.TranslationValues(5, 5, 0));
        var opacity = new OpacityFlowLayer(128, Offset.Zero);
        var clip = new ClipRRectFlowLayer(RRect.FromLTRBR(0, 0, 40, 40, Radius.Circular(8)), Clip.AntiAlias);
        clip.Add(new DisplayListFlowLayer(Offset.Zero, recorder.EndRecording(), isComplex: false, willChange: false));
        opacity.Add(clip);
        transform.Add(opacity);
        root.Add(transform);

        using ImpellerDisplayList displayList = ImpellerLayerPainter.Build(root, 100, 100);

        Assert.NotNull(displayList);
    }

    // A copy of the font with OS/2 usWeightClass and the ITALIC bit of fsSelection replaced.
    private static byte[] WithOs2(byte[] font, int weight, bool italic)
    {
        byte[] copy = (byte[])font.Clone();
        int tables = (copy[4] << 8) | copy[5];
        for (int index = 0; index < tables; index++)
        {
            int record = 12 + (16 * index);
            if (copy[record] == 'O' && copy[record + 1] == 'S' && copy[record + 2] == '/' && copy[record + 3] == '2')
            {
                int offset = (copy[record + 8] << 24) | (copy[record + 9] << 16) | (copy[record + 10] << 8)
                             | copy[record + 11];
                copy[offset + 4] = (byte)(weight >> 8);
                copy[offset + 5] = (byte)weight;
                copy[offset + 63] = (byte)(italic ? copy[offset + 63] | 1 : copy[offset + 63] & ~1);
            }
        }

        return copy;
    }
}
