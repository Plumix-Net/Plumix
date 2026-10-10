using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using Path = Plumix.UI.Path;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/rendering/table_border_test.dart
public sealed class TableBorderTests
{
    [Fact]
    public void Constructor_DefaultsDimensionsAndScalingMatchFlutter()
    {
        var green = new BorderSide(new Color(0xFF00FF00));
        var border = new TableBorder(left: new BorderSide(Colors.Black), right: green, verticalInside: new BorderSide(Colors.Black));
        Assert.Equal(BorderSide.None, border.Top);
        Assert.Equal(green, border.Right);
        Assert.Equal(BorderSide.None, border.Bottom);
        Assert.Equal(new BorderSide(Colors.Black), border.Left);
        Assert.Equal(BorderSide.None, border.HorizontalInside);
        Assert.Equal(new BorderSide(Colors.Black), border.VerticalInside);
        Assert.Equal(new Thickness(1, 0, 1, 0), border.Dimensions);
        Assert.False(border.IsUniform);
        Assert.Equal(new TableBorder(
            left: new BorderSide(Colors.Black, width: 2),
            right: new BorderSide(green.Color, 2),
            verticalInside: new BorderSide(Colors.Black, width: 2)), border.Scale(2));
    }

    [Fact]
    public void AllAndSymmetric_FactoriesAndScalingMatchFlutter()
    {
        var all = TableBorder.All(new Color(0xFF00FFFF), 2);
        Assert.All(Sides(all), side => Assert.Equal(new BorderSide(new Color(0xFF00FFFF), 2), side));
        Assert.Equal(new Thickness(2), all.Dimensions);
        Assert.True(all.IsUniform);
        Assert.Equal(TableBorder.All(new Color(0xFF00FFFF)), all.Scale(0.5));
        var inside = new BorderSide(Colors.Black, width: 3);
        var outside = new BorderSide(new Color(0xFFFF0000));
        var symmetric = TableBorder.Symmetric(inside, outside);
        Assert.All(Sides(symmetric).Take(4), side => Assert.Equal(outside, side));
        Assert.Equal(inside, symmetric.HorizontalInside);
        Assert.Equal(inside, symmetric.VerticalInside);
        Assert.Equal(new Thickness(1), symmetric.Dimensions);
        Assert.False(symmetric.IsUniform);
        Assert.Equal(TableBorder.Symmetric(outside: new BorderSide(outside.Color, 0, BorderStyle.None)),
            symmetric.Scale(0));
        Assert.Equal(BorderRadius.Circular(8), TableBorder.All(borderRadius: BorderRadius.Circular(8)).BorderRadius);
    }

    [Fact]
    public void Lerp_InterpolatesEverySideAndSupportsExtrapolation()
    {
        var a = new TableBorder(
            top: new BorderSide(new Color(1), 1),
            right: new BorderSide(new Color(2), 2),
            bottom: new BorderSide(new Color(3), 3),
            left: new BorderSide(new Color(4), 4),
            horizontalInside: new BorderSide(new Color(5), 5),
            verticalInside: new BorderSide(new Color(6), 6));
        TableBorder b = a.Scale(2);
        TableBorder c = a.Scale(3);
        Assert.False(a.IsUniform);
        Assert.Equal(new Thickness(4, 1, 2, 3), a.Dimensions);
        Assert.Equal(b, TableBorder.Lerp(a, c, 0.5));
        Assert.Equal(c, TableBorder.Lerp(a, b, 2));
        Assert.Equal(a, TableBorder.Lerp(b, c, -1));
        Assert.Equal(a.Dimensions.Left * 2.839, TableBorder.Lerp(a, c, 0.9195)!.Dimensions.Left, 8);
        Assert.Same(a, TableBorder.Lerp(a, a, 0.5));
        Assert.Null(TableBorder.Lerp(null, null, 0.5));
        Assert.Equal(TableBorder.All(), TableBorder.Lerp(null, TableBorder.All(width: 2), 0.5));
        Assert.Equal(TableBorder.All(), TableBorder.Lerp(TableBorder.All(width: 2), null, 0.5));
    }

    [Fact]
    public void ScaleAndLerp_UseBorderSideRulesAndDiscardRadiusLikeDart()
    {
        var hairline = TableBorder.All(width: 0);
        Assert.Equal(BorderStyle.Solid, hairline.Scale(1).Top.Style);
        Assert.Equal(BorderStyle.None, hairline.Scale(-1).Top.Style);
        var a = TableBorder.Symmetric(outside: new BorderSide(Colors.Red, 4, strokeAlign: 1),
            borderRadius: BorderRadius.Circular(8));
        var b = TableBorder.Symmetric(outside: new BorderSide(Colors.Blue, 8, strokeAlign: -1));
        Assert.Equal(BorderSide.StrokeAlignInside, a.Scale(1).Top.StrokeAlign);
        Assert.Equal(BorderSide.Lerp(a.Top, b.Top, 0.25), TableBorder.Lerp(a, b, 0.25)!.Top);
        Assert.Equal(BorderRadius.Zero, a.Scale(1).BorderRadius);
        Assert.Equal(BorderRadius.Zero, TableBorder.Lerp(a, b, 0.25)!.BorderRadius);
    }

    [Fact]
    public void ObjectApi_EqualityHashAndStringMatchFlutter()
    {
        var empty = new TableBorder();
        Assert.False(empty.Equals(1.0));
        Assert.Equal(empty, new TableBorder());
        Assert.Equal(empty.GetHashCode(), new TableBorder().GetHashCode());
        Assert.NotEqual(empty.GetHashCode(), new TableBorder(top: new BorderSide(Colors.Black, width: 0)).GetHashCode());
        Assert.Equal($"TableBorder({BorderSide.None}, {BorderSide.None}, {BorderSide.None}, "
            + $"{BorderSide.None}, {BorderSide.None}, {BorderSide.None}, {BorderRadius.Zero})", empty.ToString());
        Assert.NotEqual(empty, new TableBorder(borderRadius: BorderRadius.Circular(8)));
    }

    [Fact]
    public void Uniformity_ComparesColorWidthAndStyleWithoutStrokeAlignment()
    {
        var inside = new BorderSide(Colors.Red, 2);
        var outside = new BorderSide(Colors.Red, 2, strokeAlign: 1);
        var border = new TableBorder(inside, outside, inside, outside, outside, inside,
            BorderRadius.Circular(8));
        Assert.True(border.IsUniform);
        var canvas = new CapturingCanvas();
        border.Paint(canvas, new Rect(10, 20, 100, 60), [], []);
        Ring ring = Assert.Single(canvas.Rings);
        Assert.Equal(new Rect(10, 20, 100, 60), ring.Outer.Rect);
        // Dart's uniform branch deflates by width, irrespective of each side's strokeAlign.
        Assert.Equal(new Rect(12, 22, 96, 56), ring.Inner.Rect);
    }

    [Fact]
    public void RoundedOuterBorder_RemainsUniformWithDifferentInteriorSides()
    {
        var border = TableBorder.Symmetric(new BorderSide(Colors.Blue), new BorderSide(Colors.Red, 2),
            new BorderRadius(Radius.Circular(8), Radius.Circular(12), Radius.Circular(16), Radius.Circular(4)));
        Assert.False(border.IsUniform);
        var canvas = new CapturingCanvas();
        border.Paint(canvas, new Rect(10, 20, 100, 60), [30], [50]);
        Ring ring = Assert.Single(canvas.Rings);
        Assert.Equal(Radius.Circular(8), ring.Outer.TopLeft);
        Assert.Equal(Radius.Circular(12), ring.Outer.TopRight);
        Assert.Equal(Radius.Circular(16), ring.Outer.BottomRight);
        Assert.Equal(Radius.Circular(4), ring.Outer.BottomLeft);
        Assert.Equal(Radius.Circular(10), ring.Inner.TopRight);
        Assert.Equal(Radius.Circular(14), ring.Inner.BottomRight);
        Assert.Equal(new[] { "path", "path", "ring" }, canvas.Order);
    }

    [Fact]
    public void NonUniformRoundedBorder_UsesIndividualStrokeInsetsOutsetsAndCornerRadii()
    {
        var border = new TableBorder(
            top: new BorderSide(Colors.Blue, 6, strokeAlign: 0),
            right: new BorderSide(Colors.Blue, 4, strokeAlign: 1),
            bottom: new BorderSide(Colors.Blue, 2),
            left: new BorderSide(Colors.Blue, 8),
            borderRadius: new BorderRadius(Radius.Circular(20), Radius.Circular(12),
                Radius.Circular(16), Radius.Circular(10)));
        var canvas = new CapturingCanvas();
        border.Paint(canvas, new Rect(20, 30, 100, 60), [], []);
        Ring ring = Assert.Single(canvas.Rings);
        Assert.Equal(new Rect(20, 27, 104, 63), ring.Outer.Rect);
        Assert.Equal(new Rect(28, 33, 92, 55), ring.Inner.Rect);
        Assert.Equal(Radius.Elliptical(12, 17), ring.Inner.TopLeft);
        Assert.Equal(Radius.Elliptical(12, 9), ring.Inner.TopRight);
        Assert.Equal(Radius.Elliptical(16, 14), ring.Inner.BottomRight);
        Assert.Equal(Radius.Elliptical(2, 8), ring.Inner.BottomLeft);
        Assert.Equal(Radius.Elliptical(16, 15), ring.Outer.TopRight);
        Assert.Equal(Colors.Blue, ring.Paint.Color);
    }

    [Fact]
    public void HiddenSides_DoNotContributeInsetsOrColorsToNonUniformRing()
    {
        var border = new TableBorder(top: new BorderSide(Colors.Red, 4),
            right: new BorderSide(Colors.Blue, 20, BorderStyle.None, 1),
            bottom: new BorderSide(Colors.Red, 2), borderRadius: BorderRadius.Circular(12));
        var canvas = new CapturingCanvas();
        border.Paint(canvas, new Rect(20, 30, 100, 60), [], []);
        Ring ring = Assert.Single(canvas.Rings);
        Assert.Equal(new Rect(20, 30, 100, 60), ring.Outer.Rect);
        Assert.Equal(new Rect(20, 34, 100, 54), ring.Inner.Rect);
        Assert.Equal(Radius.Elliptical(12, 8), ring.Inner.TopRight);
    }

    [Fact]
    public void MixedColors_FallBackToSidePathsInTopRightBottomLeftOrder()
    {
        var border = new TableBorder(top: new BorderSide(Colors.Red, 3),
            right: new BorderSide(Colors.Green, 0), bottom: new BorderSide(Colors.Blue, 2),
            left: new BorderSide(Colors.Black, 4), borderRadius: BorderRadius.Circular(12));
        var canvas = new CapturingCanvas();
        border.Paint(canvas, new Rect(10, 20, 100, 60), [], []);
        Assert.Empty(canvas.Rings);
        Assert.Equal(new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Black },
            canvas.Paths.Select(draw => draw.Paint.Color));
        Assert.Equal(PaintingStyle.Stroke, canvas.Paths[1].Paint.Style);
        Assert.Equal(0, canvas.Paths[1].Paint.StrokeWidth);
        Assert.Equal(new Rect(110, 20, 0, 60), canvas.Paths[1].Path.GetBounds());
        Assert.Equal(PaintingStyle.Fill, canvas.Paths[0].Paint.Style);
        Assert.True(canvas.Paths[0].Path.Contains(new Point(50, 21)));
        Assert.False(canvas.Paths[0].Path.Contains(new Point(50, 25)));
    }

    [Fact]
    public void InteriorPaths_AreBatchedVerticalThenHorizontalAtRectOffset()
    {
        var border = TableBorder.Symmetric(new BorderSide(Colors.Blue, 3));
        var canvas = new CapturingCanvas();
        border.Paint(canvas, new Rect(10, 20, 100, 60), [20, 40], [25, 75]);
        Assert.Equal(2, canvas.Paths.Count);
        Assert.Equal(new Rect(35, 20, 50, 60), canvas.Paths[0].Path.GetBounds());
        Assert.Equal(new Rect(10, 40, 100, 20), canvas.Paths[1].Path.GetBounds());
        Assert.All(canvas.Paths, draw =>
        {
            Assert.Equal(PaintingStyle.Stroke, draw.Paint.Style);
            Assert.Equal(3, draw.Paint.StrokeWidth);
            Assert.Equal(Colors.Blue, draw.Paint.Color);
        });
        var empty = new CapturingCanvas();
        border.Paint(empty, new Rect(0, 0, 100, 60), [], []);
        Assert.Empty(empty.Order);
    }

    [DebugOnlyFact]
    public void Paint_ValidatesRowAndColumnExtentsLikeFlutter()
    {
        var border = new TableBorder();
        var canvas = new CapturingCanvas();
        var rect = new Rect(0, 0, 100, 60);
        Assert.Throws<AssertionError>(() => border.Paint(canvas, rect, [-1], []));
        Assert.Throws<AssertionError>(() => border.Paint(canvas, rect, [61], []));
        Assert.Throws<AssertionError>(() => border.Paint(canvas, rect, [], [-1]));
        Assert.Throws<AssertionError>(() => border.Paint(canvas, rect, [], [101]));
        border.Paint(canvas, rect, [0, 60], [0, 100]);
    }

    [Fact]
    public void RoundedBorder_RasterPreservesDifferentCornerRadiiAndStrokeOutset()
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        var border = new TableBorder(top: new BorderSide(Colors.Red, 6),
            right: new BorderSide(Colors.Red, 4, strokeAlign: 1),
            bottom: new BorderSide(Colors.Red, 2), left: new BorderSide(Colors.Red, 8),
            borderRadius: new BorderRadius(Radius.Circular(20), Radius.Circular(4),
                Radius.Circular(12), Radius.Circular(4)));
        border.Paint(canvas, new Rect(20, 20, 100, 60), [], []);
        Picture picture = recorder.EndRecording();
        var builder = new SceneBuilder();
        builder.AddPicture(default, picture);
        using Scene scene = builder.Build();
        using Bitmap image = scene.ToImageSync(140, 100);
        Assert.Equal((byte)0, RasterBackend.PixelAt(image, 21, 21).A);
        Assert.Equal((byte)255, RasterBackend.PixelAt(image, 117, 21).A);
        Assert.Equal((byte)255, RasterBackend.PixelAt(image, 122, 45).A);
        Assert.Equal((byte)0, RasterBackend.PixelAt(image, 125, 45).A);
        Assert.Equal((byte)255, RasterBackend.PixelAt(image, 24, 45).A);
        Assert.Equal((byte)0, RasterBackend.PixelAt(image, 29, 45).A);
        Assert.Equal((byte)255, RasterBackend.PixelAt(image, 60, 22).A);
        Assert.Equal((byte)0, RasterBackend.PixelAt(image, 60, 27).A);
    }

    [Fact]
    public void RecordedPaths_RasterizeEachDivisionAndSideAfterSharedPathIsReset()
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        var border = new TableBorder(top: new BorderSide(Colors.Red, 4),
            right: new BorderSide(Colors.Green, 4), bottom: new BorderSide(Colors.Blue, 4),
            left: new BorderSide(Colors.Black, 4), horizontalInside: new BorderSide(Colors.Blue, 4),
            verticalInside: new BorderSide(Colors.Red, 4));
        border.Paint(canvas, new Rect(10, 10, 80, 60), [30], [20, 60]);
        Picture picture = recorder.EndRecording();
        var builder = new SceneBuilder();
        builder.AddPicture(default, picture);
        using Scene scene = builder.Build();
        using Bitmap image = scene.ToImageSync(100, 80);
        Assert.Equal(((byte)255, (byte)0, (byte)0, (byte)255), RasterBackend.PixelAt(image, 30, 25));
        Assert.Equal(((byte)255, (byte)0, (byte)0, (byte)255), RasterBackend.PixelAt(image, 70, 25));
        Assert.Equal(((byte)0, (byte)0, (byte)255, (byte)255), RasterBackend.PixelAt(image, 30, 40));
        Assert.Equal(((byte)255, (byte)0, (byte)0, (byte)255), RasterBackend.PixelAt(image, 50, 11));
        Assert.Equal(((byte)0, (byte)0, (byte)255, (byte)255), RasterBackend.PixelAt(image, 50, 68));
        Assert.Equal(((byte)0, (byte)0, (byte)0, (byte)255), RasterBackend.PixelAt(image, 11, 25));
        Assert.Equal((byte)0, RasterBackend.PixelAt(image, 50, 25).A);
    }

    private static IEnumerable<BorderSide> Sides(TableBorder border) =>
        [border.Top, border.Right, border.Bottom, border.Left, border.HorizontalInside, border.VerticalInside];

    private sealed record Ring(RRect Outer, RRect Inner, Paint Paint);

    private sealed class CapturingCanvas() : Canvas(new PictureRecorder())
    {
        public List<Ring> Rings { get; } = [];
        public List<(Path Path, Paint Paint)> Paths { get; } = [];
        public List<string> Order { get; } = [];

        public override void DrawDRRect(RRect outer, RRect inner, Paint paint)
        {
            Rings.Add(new Ring(outer, inner, new Paint(paint)));
            Order.Add("ring");
        }

        public override void DrawPath(Path path, Paint paint)
        {
            Paths.Add((new Path(path), new Paint(paint)));
            Order.Add("path");
        }
    }
}
