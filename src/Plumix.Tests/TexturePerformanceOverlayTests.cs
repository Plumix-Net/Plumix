using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Flutter's widgets/texture_test.dart, rendering/performance_overlay_test.dart and
// widgets/performance_overlay_test.dart, plus WidgetsApp's showPerformanceOverlay composition.
public sealed class TexturePerformanceOverlayTests
{
    [Fact]
    public void Texture_WithFreezeSetToTrue()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new Texture(textureId: 1, freeze: true)));

        var textureBox = (TextureBox)tester.ElementOfType<Texture>().FindRenderObject()!;
        Assert.Equal(1, textureBox.TextureId);
        Assert.True(textureBox.Freeze);

        TextureLayer textureLayer = PaintIntoLayer(textureBox);
        Assert.Equal(1, textureLayer.TextureId);
        Assert.True(textureLayer.Freeze);
    }

    [Fact]
    public void Texture_WithDefaultFilterQuality()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new Texture(textureId: 1)));

        var textureBox = (TextureBox)tester.ElementOfType<Texture>().FindRenderObject()!;
        Assert.Equal(FilterQuality.Low, textureBox.FilterQuality);
        Assert.Equal(FilterQuality.Low, PaintIntoLayer(textureBox).FilterQuality);
    }

    [Theory]
    [InlineData(FilterQuality.None)]
    [InlineData(FilterQuality.Low)]
    public void Texture_WithFilterQuality(FilterQuality filterQuality)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new Texture(textureId: 1, filterQuality: filterQuality)));

        var textureBox = (TextureBox)tester.ElementOfType<Texture>().FindRenderObject()!;
        Assert.Equal(filterQuality, textureBox.FilterQuality);
        Assert.Equal(filterQuality, PaintIntoLayer(textureBox).FilterQuality);
    }

    [Fact]
    public void TextureBox_FillsItsConstraintsAndHitTestsItself()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(300, 200));
        tester.PumpWidget(new Texture(textureId: 7));

        var textureBox = (TextureBox)tester.ElementOfType<Texture>().FindRenderObject()!;
        Assert.Equal(new Size(300, 200), textureBox.Size);
        Assert.True(textureBox.IsRepaintBoundary);
        Assert.True(textureBox.AlwaysNeedsCompositing);
        var result = new BoxHitTestResult();
        Assert.True(textureBox.HitTest(result, new Point(10, 10)));

        tester.PumpWidget(new Texture(textureId: 8, freeze: true, filterQuality: FilterQuality.High));
        Assert.Equal(8, textureBox.TextureId);
        Assert.True(textureBox.Freeze);
        Assert.Equal(FilterQuality.High, textureBox.FilterQuality);
    }

    [Fact]
    public void RenderPerformanceOverlay_IntrinsicHeightRespectsOptionsMask()
    {
        const double kGraph = 80.0;
        Assert.Equal(0.0, IntrinsicHeight(0));
        Assert.Equal(kGraph, IntrinsicHeight(Mask(PerformanceOverlayOption.DisplayEngineStatistics)));
        Assert.Equal(kGraph, IntrinsicHeight(Mask(PerformanceOverlayOption.VisualizeEngineStatistics)));
        Assert.Equal(kGraph, IntrinsicHeight(Mask(PerformanceOverlayOption.DisplayRasterizerStatistics)));
        Assert.Equal(kGraph, IntrinsicHeight(Mask(PerformanceOverlayOption.VisualizeRasterizerStatistics)));
        Assert.Equal(
            kGraph * 2,
            IntrinsicHeight(
                Mask(PerformanceOverlayOption.DisplayEngineStatistics)
                | Mask(PerformanceOverlayOption.DisplayRasterizerStatistics)));
        Assert.Equal(
            kGraph,
            IntrinsicHeight(
                Mask(PerformanceOverlayOption.DisplayEngineStatistics)
                | Mask(PerformanceOverlayOption.VisualizeEngineStatistics)));
        Assert.Equal(kGraph * 2, IntrinsicHeight(0b1111));
    }

    [Fact]
    public void RenderPerformanceOverlay_AddsAPerformanceOverlayLayerAtItsBounds()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(400, 300));
        tester.PumpWidget(new Column(children: [PerformanceOverlay.AllEnabled()]));

        var overlay = (RenderPerformanceOverlay)tester.ElementOfType<PerformanceOverlay>().FindRenderObject()!;
        Assert.Equal(new Size(400, 160), overlay.Size);
        Assert.Equal(0b1111, overlay.OptionsMask);
        Assert.True(overlay.NeedsCompositing);

        var layer = Assert.IsType<PerformanceOverlayLayer>(PaintIntoContainer(overlay).LastChild);
        Assert.Equal(new Rect(0, 0, 400, 160), layer.OverlayRect);
        Assert.Equal(0b1111, layer.OptionsMask);
    }

    [Fact]
    public void PerformanceOverlay_SmokeTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new PerformanceOverlay());
        Assert.Null(tester.TakeException());
        tester.PumpWidget(PerformanceOverlay.AllEnabled());
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void WidgetsApp_ShowPerformanceOverlayStacksAnAllEnabledOverlayAtTheTop()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(400, 300));
        tester.PumpWidget(new WidgetsApp(
            color: new Plumix.UI.Color(0xFF2196F3),
            builder: (_, _) => new SizedBox(),
            showPerformanceOverlay: true));

        Element overlayElement = tester.ElementOfType<PerformanceOverlay>();
        Assert.Equal(0b1111, ((PerformanceOverlay)overlayElement.Widget).OptionsMask);
        Assert.Equal(new Rect(0, 0, 400, 160), tester.GetRect(overlayElement));

        tester.PumpWidget(new WidgetsApp(
            color: new Plumix.UI.Color(0xFF2196F3),
            builder: (_, _) => new SizedBox()));
        Assert.Empty(tester.ElementsOfType<PerformanceOverlay>());

        WidgetsApp.ShowPerformanceOverlayOverride = true;
        try
        {
            tester.PumpWidget(new WidgetsApp(
                color: new Plumix.UI.Color(0xFF2196F3),
                builder: (_, _) => new SizedBox(width: 1)));
            Assert.Single(tester.ElementsOfType<PerformanceOverlay>());
        }
        finally
        {
            WidgetsApp.ShowPerformanceOverlayOverride = false;
        }
    }

    private static int Mask(PerformanceOverlayOption option) => 1 << (int)option;

    private static double IntrinsicHeight(int optionsMask)
    {
        var box = new RenderPerformanceOverlay(optionsMask: optionsMask);
        Assert.Equal(box.GetMinIntrinsicHeight(0.0), box.GetMaxIntrinsicHeight(0.0));
        return box.GetMinIntrinsicHeight(0.0);
    }

    private static TextureLayer PaintIntoLayer(TextureBox textureBox) =>
        Assert.IsType<TextureLayer>(PaintIntoContainer(textureBox).LastChild);

    private static ContainerLayer PaintIntoContainer(RenderBox box)
    {
        var containerLayer = new ContainerLayer();
        var context = new PaintingContext(containerLayer, default);
        box.Paint(context, default);
        return containerLayer;
    }
}
