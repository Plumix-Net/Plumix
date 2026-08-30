using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;
using Plumix.Painting;

namespace Plumix.Tests;

public sealed class ColoredBoxTests
{
    [Fact]
    public void Widget_UsesFlutterDefaultsAndUpdatesRenderObject()
    {
        var widget = new ColoredBox(Colors.CornflowerBlue);
        Assert.True(widget.IsAntiAlias);

        var renderObject = Assert.IsType<RenderColoredBox>(widget.CreateRenderObject(default));
        Assert.Equal(Colors.CornflowerBlue, renderObject.Color);
        Assert.True(renderObject.IsAntiAlias);
        Assert.Equal(HitTestBehavior.Opaque, renderObject.Behavior);

        var updated = new ColoredBox(Colors.OrangeRed, isAntiAlias: false);
        updated.UpdateRenderObject(default, renderObject);

        Assert.Equal(Colors.OrangeRed, renderObject.Color);
        Assert.False(renderObject.IsAntiAlias);
    }

    [DebugOnlyFact]
    public void Widget_DebugFillProperties_ReportsColorThenAntiAlias()
    {
        var widget = new ColoredBox(Colors.CornflowerBlue, isAntiAlias: false);
        var properties = new DiagnosticPropertiesBuilder();

        widget.DebugFillProperties(properties);

        var color = Assert.IsType<ColorProperty>(properties.Properties[0]);
        Assert.Equal(Colors.CornflowerBlue, color.TypedValue);
        var isAntiAlias = Assert.IsType<DiagnosticsProperty<bool>>(properties.Properties[1]);
        Assert.False(isAntiAlias.TypedValue);
        Assert.Equal(true, isAntiAlias.DefaultValue);
    }

    [Fact]
    public void RenderObject_ZeroSizeWithoutChild_DoesNotPaint()
    {
        var coloredBox = new RenderColoredBox(Colors.Transparent);

        OffsetLayer layer = Paint(coloredBox, new Size());

        Assert.Empty(layer.Children);
    }

    [Fact]
    public void RenderObject_ZeroSizeWithChild_StillPaintsChild()
    {
        var child = new PaintProbeRenderBox();
        var coloredBox = new RenderColoredBox(Colors.CornflowerBlue, child: child);

        OffsetLayer layer = Paint(coloredBox, new Size());

        Assert.Equal(1, child.PaintCount);
        Assert.Empty(layer.Children);
    }

    [Fact]
    public void RenderObject_NonEmptySizeWithoutChild_PaintsBackground()
    {
        var coloredBox = new RenderColoredBox(Colors.CornflowerBlue);
        var constrained = new RenderConstrainedBox(
            BoxConstraints.Tight(new Size(80.0, 60.0)),
            coloredBox);

        OffsetLayer layer = Paint(constrained, new Size(80.0, 60.0));

        var picture = Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.False(picture.IsEmpty);
    }

    [Fact]
    public void RenderObject_NonEmptySize_PaintsBackgroundBeforeChild()
    {
        var child = new PaintProbeRenderBox(new Size(80.0, 60.0));
        var coloredBox = new RenderColoredBox(
            Color.FromArgb(0, 0xAB, 0xCD, 0xEF),
            isAntiAlias: false,
            child: child);

        OffsetLayer layer = Paint(coloredBox, new Size(80.0, 60.0));

        Assert.Equal(1, child.PaintCount);
        var picture = Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.False(picture.IsEmpty);
        Assert.False(coloredBox.IsAntiAlias);
    }

    [Fact]
    public void RenderObject_OpaqueHitTestUsesHalfOpenBounds()
    {
        var coloredBox = new RenderColoredBox(Colors.CornflowerBlue);
        coloredBox.Layout(BoxConstraints.Tight(new Size(20.0, 10.0)));

        var inside = new BoxHitTestResult();
        Assert.True(coloredBox.HitTest(inside, new Point(0.0, 0.0)));
        Assert.Contains(inside.Path, entry => ReferenceEquals(entry.Target, coloredBox));
        Assert.True(coloredBox.HitTest(new BoxHitTestResult(), new Point(19.999, 9.999)));
        Assert.False(coloredBox.HitTest(new BoxHitTestResult(), new Point(20.0, 5.0)));
        Assert.False(coloredBox.HitTest(new BoxHitTestResult(), new Point(5.0, 10.0)));
        Assert.False(coloredBox.HitTest(new BoxHitTestResult(), new Point(-0.001, 5.0)));
    }

    private static OffsetLayer Paint(RenderBox root, Size size)
    {
        var renderView = new RenderView { Child = root };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(size);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        return pipeline.RootLayer;
    }

    private sealed class PaintProbeRenderBox : RenderBox
    {
        private readonly Size _desiredSize;

        public PaintProbeRenderBox(Size desiredSize = default)
        {
            _desiredSize = desiredSize;
        }

        public int PaintCount { get; private set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_desiredSize);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
            PaintCount += 1;
        }
    }
}
