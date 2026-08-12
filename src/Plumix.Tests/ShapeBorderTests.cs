using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using Border = Plumix.Rendering.Border;
using Path = Plumix.UI.Path;

namespace Plumix.Tests;

public sealed class ShapeBorderTests
{
    private static readonly Color Green = Color.FromRgb(0, 0xFF, 0);
    private static readonly Color Blue = Color.FromRgb(0, 0, 0xFF);

    [Fact]
    public void BorderSide_DefaultsAndStrokeGeometryMatchFlutter()
    {
        var side = new BorderSide(Green);
        Assert.Equal(1.0, side.Width);
        Assert.Equal(BorderStyle.Solid, side.Style);
        Assert.Equal(BorderSide.StrokeAlignInside, side.StrokeAlign);

        Assert.Equal(0.0, BorderSide.None.Width);
        Assert.Equal(BorderStyle.None, BorderSide.None.Style);
        Assert.Equal(Color.FromRgb(0, 0, 0), BorderSide.None.Color);

        var inside = new BorderSide(Green, 10.0);
        var center = new BorderSide(Green, 10.0, BorderStyle.Solid, BorderSide.StrokeAlignCenter);
        var outside = new BorderSide(Green, 10.0, BorderStyle.Solid, BorderSide.StrokeAlignOutside);
        Assert.Equal(10.0, inside.StrokeInset);
        Assert.Equal(0.0, inside.StrokeOutset);
        Assert.Equal(-10.0, inside.StrokeOffset);
        Assert.Equal(5.0, center.StrokeInset);
        Assert.Equal(5.0, center.StrokeOutset);
        Assert.Equal(0.0, center.StrokeOffset);
        Assert.Equal(0.0, outside.StrokeInset);
        Assert.Equal(10.0, outside.StrokeOutset);
        Assert.Equal(10.0, outside.StrokeOffset);
    }

    [Fact]
    public void BorderSide_CanMergeAndMergeFollowTheSourceTable()
    {
        var green2 = new BorderSide(Green, 2.0);
        var green3 = new BorderSide(Green, 3.0);
        var blue2 = new BorderSide(Blue, 2.0);
        var none3 = new BorderSide(Green, 3.0, BorderStyle.None);

        Assert.True(BorderSide.CanMerge(BorderSide.None, BorderSide.None));
        Assert.True(BorderSide.CanMerge(BorderSide.None, green2));
        Assert.True(BorderSide.CanMerge(green2, green3));
        Assert.False(BorderSide.CanMerge(green2, blue2));
        Assert.False(BorderSide.CanMerge(none3, green2));

        Assert.Equal(BorderSide.None, BorderSide.Merge(BorderSide.None, BorderSide.None));
        Assert.Equal(green2, BorderSide.Merge(BorderSide.None, green2));
        Assert.Equal(new BorderSide(Green, 5.0), BorderSide.Merge(green2, green3));
        Assert.Throws<ArgumentException>(() => BorderSide.Merge(green2, blue2));
    }

    [Fact]
    public void BorderSide_ScaleDropsStrokeAlignAndNeverLerpsNegative()
    {
        var outside = new BorderSide(Green, 4.0, BorderStyle.Solid, BorderSide.StrokeAlignOutside);
        BorderSide scaled = outside.Scale(2.0);
        Assert.Equal(8.0, scaled.Width);
        Assert.Equal(BorderSide.StrokeAlignInside, scaled.StrokeAlign);
        Assert.Equal(BorderStyle.None, outside.Scale(0.0).Style);

        var side1 = new BorderSide(Green, 1.0);
        var side2 = new BorderSide(Green, 2.0);
        Assert.Equal(BorderSide.None, BorderSide.Lerp(side2, side1, 10.0));
        Assert.Equal(BorderSide.None, BorderSide.Lerp(side1, side2, -10.0));

        BorderSide alignLerp = BorderSide.Lerp(
            new BorderSide(Green, 2.0),
            new BorderSide(Green, 2.0, BorderStyle.Solid, BorderSide.StrokeAlignOutside),
            0.5);
        Assert.Equal(BorderSide.StrokeAlignCenter, alignLerp.StrokeAlign);
    }

    [Fact]
    public void ShapeBorder_LerpReturnsIdenticalAndFallsBackAtHalf()
    {
        var circle = new CircleBorder();
        Assert.Null(ShapeBorder.Lerp(null, null, 0.0));
        Assert.Same(circle, ShapeBorder.Lerp(circle, circle, 0.5));

        var beveled = new BeveledRectangleBorder();
        var continuous = new ContinuousRectangleBorder();
        Assert.Equal(beveled, ShapeBorder.Lerp(beveled, continuous, 0.25));
        Assert.Equal(continuous, ShapeBorder.Lerp(beveled, continuous, 0.75));

        Assert.Null(OutlinedBorder.Lerp(null, null, 0.0));
        Assert.Same(circle, OutlinedBorder.Lerp(circle, circle, 0.5));
    }

    [Fact]
    public void CompoundBorder_MergesScalesLerpsAndPrintsInSourceOrder()
    {
        ShapeBorder green = Border.All(Green);
        ShapeBorder blue = Border.All(Blue);
        ShapeBorder compound = green + blue;

        Assert.IsType<CompoundBorder>(compound);
        Assert.Equal($"{green} + {blue}", compound.ToString());
        Assert.Equal(EdgeInsets.All(2.0), compound.Dimensions);
        Assert.Equal(EdgeInsets.All(3.0), (green + blue + green).Dimensions);

        // Adjacent compatible borders collapse into one.
        Assert.Equal(green + (blue + blue), (green + blue) + blue);

        ShapeBorder scaled = compound.Scale(3.0);
        var scaledCompound = Assert.IsType<CompoundBorder>(scaled);
        Assert.All(scaledCompound.Borders, border => Assert.Equal(3.0, ((Border)border).Top.Width));

        ShapeBorder lerped = ShapeBorder.Lerp(blue + green, green + blue, 0.0)!;
        Assert.Equal(blue + green, lerped);
    }

    [Fact]
    public void CompoundBorder_PreferPaintInteriorRequiresEveryMember()
    {
        ShapeBorder withInterior = new RoundedRectangleBorder();
        ShapeBorder withoutInterior = new BeveledRectangleBorder();
        Assert.True((withInterior + withInterior).PreferPaintInterior);
        Assert.False((withInterior + withoutInterior).PreferPaintInterior);
        Assert.False((withoutInterior + withInterior).PreferPaintInterior);
        Assert.False((withoutInterior + withoutInterior).PreferPaintInterior);
    }

    [Fact]
    public void Border_ConstructorsDimensionsAndUniformityMatchFlutter()
    {
        var side = new BorderSide(Green, 3.0);
        var uniform = Border.FromBorderSide(side);
        Assert.Equal(side, uniform.Top);
        Assert.Equal(side, uniform.Right);
        Assert.Equal(side, uniform.Bottom);
        Assert.Equal(side, uniform.Left);
        Assert.True(uniform.IsUniform);
        Assert.True(new Border().IsUniform);

        var symmetric = Border.Symmetric(vertical: new BorderSide(Green), horizontal: new BorderSide(Blue));
        Assert.Equal(new BorderSide(Blue), symmetric.Top);
        Assert.Equal(new BorderSide(Green), symmetric.Left);

        var mixed = new Border(
            top: new BorderSide(Green, 3.0),
            right: new BorderSide(Green, 7.0),
            bottom: new BorderSide(Green, 5.0),
            left: new BorderSide(Green, 2.0));
        Assert.Equal(EdgeInsetsGeometry.FromLTRB(2, 3, 7, 5), mixed.Dimensions);
        Assert.False(mixed.IsUniform);

        var strokeAligned = Border.All(Green, 10.0, BorderStyle.Solid, BorderSide.StrokeAlignCenter);
        Assert.Equal(EdgeInsets.All(5.0), strokeAligned.Dimensions);
        Assert.Equal(
            EdgeInsetsGeometry.Zero,
            Border.All(Green, 10.0, BorderStyle.Solid, BorderSide.StrokeAlignOutside).Dimensions);
    }

    [Fact]
    public void Border_AddScaleAndLerpMatchFlutter()
    {
        var green3 = Border.All(Green, 3.0);
        var green6 = Border.All(Green, 6.0);
        Assert.Equal(green6, green3 + green3);
        Assert.IsType<CompoundBorder>(new Border(left: new BorderSide(Green, 3.0))
                                      + new Border(left: new BorderSide(Blue, 2.0)));
        Assert.Equal(green6, green3.Scale(2.0));
        Assert.Equal(green3, green6.Scale(0.5));
        Assert.Equal(BorderStyle.None, ((Border)Border.All(Green, 2.0).Scale(0.0)).Top.Style);

        Assert.Null(Border.Lerp(null, null, 0.5));
        Assert.Equal(Border.All(Green, 20.0), Border.Lerp(Border.All(Green, 10.0), null, -1.0));
        Assert.Equal(Border.All(Green, 7.5), Border.Lerp(Border.All(Green, 10.0), null, 0.25));
        Assert.Equal(new Border(), Border.Lerp(null, Border.All(width: 10.0), -1.0));
        Assert.Equal(
            Border.All(Green, 15.0),
            Border.Lerp(Border.All(Green, 10.0), Border.All(Green, 20.0), 0.5));
    }

    [Fact]
    public void Border_PaintRejectsNonUniformRadiusAndStrokeAlign()
    {
        var harness = new PaintProbe();
        var mixedAlign = new Border(
            top: new BorderSide(Green, 2.0),
            right: new BorderSide(Green, 2.0, BorderStyle.Solid, BorderSide.StrokeAlignCenter),
            bottom: new BorderSide(Green, 2.0),
            left: new BorderSide(Green, 2.0));
        Assert.Throws<InvalidOperationException>(() => mixedAlign.Paint(
            harness.Context,
            new Rect(0, 0, 40, 40),
            TextDirection.Ltr,
            BoxShape.Rectangle,
            null));

        var mixedColors = new Border(top: new BorderSide(Green, 2.0), bottom: new BorderSide(Blue, 2.0));
        Assert.Throws<InvalidOperationException>(() => mixedColors.Paint(
            harness.Context,
            new Rect(0, 0, 40, 40),
            TextDirection.Ltr,
            BoxShape.Rectangle,
            BorderRadius.Circular(20)));
    }

    [Fact]
    public void BorderDirectional_DimensionsAddAndPathsResolveTextDirection()
    {
        var directional = new BorderDirectional(
            top: new BorderSide(Green, 3.0),
            start: new BorderSide(Green, 2.0),
            end: new BorderSide(Green, 7.0),
            bottom: new BorderSide(Green, 5.0));
        Assert.Equal(EdgeInsetsGeometry.DirectionalOnly(2, 3, 7, 5), directional.Dimensions);

        var rect = new Rect(50, 60, 60, 130);
        Assert.Equal(rect, directional.GetOuterPath(rect, TextDirection.Rtl).GetBounds());
        Assert.Equal(
            new Rect(52, 63, 51, 122),
            directional.GetInnerPath(rect, TextDirection.Ltr).GetBounds());
        Assert.Equal(
            new Rect(57, 63, 51, 122),
            directional.GetInnerPath(rect, TextDirection.Rtl).GetBounds());

        // A visual border with no lateral sides merges into a directional one.
        ShapeBorder merged = new BorderDirectional(start: new BorderSide(Green))
                             + new Border(top: new BorderSide(Blue));
        Assert.IsType<BorderDirectional>(merged);

        // A directional border with no lateral sides merges into a visual one.
        ShapeBorder visual = new BorderDirectional(top: new BorderSide(Blue))
                             + new Border(left: new BorderSide(Green));
        Assert.IsType<Border>(visual);
    }

    [Fact]
    public void BoxBorder_LerpDispatchesAcrossVisualAndDirectionalBorders()
    {
        var visual = Border.All(Green, 10.0);
        var directional = new BorderDirectional(top: new BorderSide(Green, 10.0));

        Assert.Null(BoxBorder.Lerp(null, null, 0.5));
        Assert.IsType<Border>(BoxBorder.Lerp(visual, Border.All(Green, 20.0), 0.5));
        Assert.IsType<BorderDirectional>(BoxBorder.Lerp(directional, directional, 0.5));

        // A directional border with no lateral sides lerps into a visual border.
        BoxBorder? mixed = BoxBorder.Lerp(visual, directional, 0.25);
        Assert.IsType<Border>(mixed);

        // The swap-and-invert branch turns (directional, visual) into a visual result.
        Assert.IsType<Border>(BoxBorder.Lerp(directional, visual, 0.25));
    }

    [Fact]
    public void RoundedRectangleBorder_DefaultsScaleLerpAndPaths()
    {
        var border = new RoundedRectangleBorder();
        Assert.Equal(BorderSide.None, border.Side);
        Assert.True(border.BorderRadius.IsZero);
        Assert.Equal(border, border.CopyWith(null, null));

        var c10 = new RoundedRectangleBorder(new BorderSide(Green, 10.0), BorderRadius.Circular(100));
        var c20 = new RoundedRectangleBorder(new BorderSide(Green, 20.0), BorderRadius.Circular(200));
        Assert.Equal(EdgeInsets.All(10.0), c10.Dimensions);
        Assert.Equal(c20, c10.Scale(2.0));
        Assert.Equal(c10, c20.Scale(0.5));
        Assert.Equal(
            new RoundedRectangleBorder(new BorderSide(Green, 15.0), BorderRadius.Circular(150)),
            ShapeBorder.Lerp(c10, c20, 0.5));

        var rect = new Rect(10, 20, 70, 170);
        Assert.Equal(rect, c10.GetOuterPath(rect).GetBounds());
        Assert.Equal(new Rect(20, 30, 50, 150), c10.GetInnerPath(rect).GetBounds());

        // A directional radius resolves per text direction.
        var directional = new RoundedRectangleBorder(
            borderRadius: BorderRadiusDirectional.Only(topStart: 20.0));
        Assert.True(directional.GetOuterPath(new Rect(0, 0, 100, 100), TextDirection.Ltr).Contains(new Point(99, 1)));
        Assert.False(directional.GetOuterPath(new Rect(0, 0, 100, 100), TextDirection.Ltr).Contains(new Point(1, 1)));
    }

    [Fact]
    public void RoundedRectangleBorder_LerpsTowardsCircleWithSourceToString()
    {
        var rounded = new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(10));
        var circle = new CircleBorder();
        var rect = new Rect(0, 0, 100, 20);

        ShapeBorder tenth = ShapeBorder.Lerp(rounded, circle, 0.1)!;
        ShapeBorder ninth = ShapeBorder.Lerp(rounded, circle, 0.9)!;
        Assert.True(tenth.GetOuterPath(rect).Contains(new Point(30, 10)));
        Assert.False(ninth.GetOuterPath(rect).Contains(new Point(30, 10)));
        Assert.True(ninth.GetOuterPath(rect).Contains(new Point(50, 10)));

        Assert.Contains("10.0% of the way to being a CircleBorder", tenth.ToString());
        Assert.Contains("20.0% of the way to being a CircleBorder",
            ShapeBorder.Lerp(rounded, circle, 0.2)!.ToString());
        Assert.Contains("10.0% of the way to being a CircleBorder",
            ShapeBorder.Lerp(circle, rounded, 0.9)!.ToString());
        Assert.Equal(
            ShapeBorder.Lerp(rounded, circle, 0.5),
            ShapeBorder.Lerp(ShapeBorder.Lerp(circle, rounded, 0.1), ShapeBorder.Lerp(circle, rounded, 0.9), 0.5));
    }

    [Fact]
    public void StadiumBorder_DefaultsPathsAndLerps()
    {
        var stadium = new StadiumBorder();
        Assert.Equal(BorderSide.None, stadium.Side);
        Assert.Equal(stadium, stadium.CopyWith());

        var c10 = new StadiumBorder(new BorderSide(Green, 10.0));
        Assert.Equal(EdgeInsets.All(10.0), c10.Dimensions);
        Assert.Equal(new StadiumBorder(new BorderSide(Green, 20.0)), c10.Scale(2.0));

        var rect = new Rect(0, 0, 100, 20);
        Assert.True(stadium.GetOuterPath(rect).Contains(new Point(50, 10)));
        Assert.False(stadium.GetOuterPath(rect).Contains(new Point(1, 1)));

        Assert.Contains("10.0% of the way to being a CircleBorder",
            ShapeBorder.Lerp(stadium, new CircleBorder(), 0.1)!.ToString());
        Assert.Contains("10.0% of the way to being a RoundedRectangleBorder",
            ShapeBorder.Lerp(stadium, new RoundedRectangleBorder(), 0.1)!.ToString());
    }

    [Fact]
    public void CircleAndOvalBorder_AdjustRectByEccentricity()
    {
        var rect = new Rect(0, 0, 200, 100);
        Assert.Equal(new Rect(50, 0, 100, 100), new CircleBorder().GetOuterPath(rect).GetBounds());
        Assert.Equal(rect, new CircleBorder(eccentricity: 1.0).GetOuterPath(rect).GetBounds());
        Assert.Equal(rect, new OvalBorder().GetOuterPath(rect).GetBounds());

        var thick = new CircleBorder(new BorderSide(Green, 10.0), eccentricity: 1.0);
        Assert.Equal(new Rect(10, 10, 180, 80), thick.GetInnerPath(rect).GetBounds());
        Assert.Equal(EdgeInsets.All(10.0), thick.Dimensions);

        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleBorder(eccentricity: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircleBorder(eccentricity: 1.1));

        // OvalBorder only lerps with itself; a plain circle falls back to CircleBorder.
        Assert.IsType<OvalBorder>(ShapeBorder.Lerp(new OvalBorder(), new OvalBorder(), 0.5));
        Assert.NotEqual<ShapeBorder>(new OvalBorder(), new CircleBorder(eccentricity: 1.0));
    }

    [Fact]
    public void BeveledAndContinuousRectangleBorder_DefaultsAndGeometry()
    {
        var beveled = new BeveledRectangleBorder();
        Assert.Equal(BorderSide.None, beveled.Side);
        Assert.True(beveled.BorderRadius.IsZero);

        var c10 = new BeveledRectangleBorder(new BorderSide(Green, 10.0), BorderRadius.Circular(100));
        Assert.Equal(EdgeInsets.All(10.0), c10.Dimensions);
        Assert.Equal(
            new BeveledRectangleBorder(new BorderSide(Green, 20.0), BorderRadius.Circular(200)),
            c10.Scale(2.0));

        var rect = new Rect(10, 20, 20, 20);
        var bevel = new BeveledRectangleBorder(borderRadius: BorderRadius.Circular(5.0));
        Assert.True(bevel.GetOuterPath(rect).Contains(new Point(15, 25)));
        Assert.False(bevel.GetOuterPath(rect).Contains(new Point(10, 20)));

        // ContinuousRectangleBorder measures its dimensions from the raw width.
        var continuous = new ContinuousRectangleBorder(
            new BorderSide(Green, 10.0, BorderStyle.Solid, BorderSide.StrokeAlignOutside));
        Assert.Equal(EdgeInsets.All(10.0), continuous.Dimensions);
        Assert.True(new ContinuousRectangleBorder(borderRadius: BorderRadius.Circular(5.0))
            .GetOuterPath(rect)
            .Contains(new Point(20, 30)));
    }

    [Fact]
    public void StarBorder_DefaultsEqualityAndDegenerateRect()
    {
        var star = new StarBorder();
        Assert.Equal(BorderSide.None, star.Side);
        Assert.Equal(5, star.Points);
        Assert.Equal(0.4, star.InnerRadiusRatio);
        Assert.Equal(0, star.PointRounding);
        Assert.Equal(0, star.ValleyRounding);
        Assert.Equal(0, star.Rotation);
        Assert.Equal(0, star.Squash);

        StarBorder polygon = StarBorder.Polygon();
        Assert.Equal(5, polygon.Points);
        Assert.Equal(Math.Cos(Math.PI / 5), polygon.InnerRadiusRatio);
        Assert.NotEqual(polygon, new StarBorder(innerRadiusRatio: polygon.InnerRadiusRatio));

        Assert.Equal(star, star.CopyWith(null, null, null, null, null, null, null));
        Assert.NotEqual(star, star.CopyWith(null, 10, null, null, null, null, null));
        Assert.NotEqual(star, star.CopyWith(null, null, null, null, null, 10, null));

        Assert.Throws<ArgumentException>(() => new StarBorder(pointRounding: 0.6, valleyRounding: 0.6));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StarBorder(points: 1));

        // A degenerate rect must not produce an infinite scale matrix.
        Path degenerate = star.GetOuterPath(new Rect(100, 100, 0, 0));
        Assert.NotNull(degenerate);

        Assert.True(star.GetOuterPath(new Rect(0, 0, 100, 100)).Contains(new Point(50, 50)));
    }

    [Fact]
    public void LinearBorder_DefaultsLerpAndToString()
    {
        Assert.Equal(1.0, new LinearBorderEdge().Size);
        Assert.Equal(0.0, new LinearBorderEdge().Alignment);
        Assert.Equal("LinearBorderEdge(size: 0.5, alignment: -0.5)",
            new LinearBorderEdge(0.5, -0.5).ToString());

        LinearBorder none = LinearBorder.None;
        Assert.Equal(BorderSide.None, none.Side);
        Assert.Equal(EdgeInsetsGeometry.Zero, none.Dimensions);
        Assert.False(none.PreferPaintInterior);
        Assert.Null(none.Start);
        Assert.Null(none.End);
        Assert.Null(none.Top);
        Assert.Null(none.Bottom);
        Assert.Equal("LinearBorder.none", none.ToString());

        LinearBorder start = LinearBorder.StartEdge();
        Assert.Equal(new LinearBorderEdge(), start.Start);
        Assert.Null(start.End);

        var edge = new LinearBorderEdge();
        Assert.Null(LinearBorderEdge.Lerp(null, null, 0.0));
        Assert.Same(edge, LinearBorderEdge.Lerp(edge, edge, 0.5));

        // scale() drops every edge, keeping only the side.
        var full = new LinearBorder(new BorderSide(Green, 4.0), start: edge, end: edge, top: edge, bottom: edge);
        var scaled = (LinearBorder)full.Scale(0.5);
        Assert.Null(scaled.Start);
        Assert.Equal(2.0, scaled.Side.Width);

        var directionalInsets = new LinearBorder(new BorderSide(Green, 4.0), start: edge);
        Assert.Equal(new Thickness(4, 0, 0, 0), directionalInsets.Dimensions.Resolve(TextDirection.Ltr));
        Assert.Equal(new Thickness(0, 0, 4, 0), directionalInsets.Dimensions.Resolve(TextDirection.Rtl));
    }

    [Fact]
    public void ShapeDecoration_FromBoxDecorationPaddingHitTestAndClipPath()
    {
        Assert.Equal(
            new CircleBorder(),
            ShapeDecoration.FromBoxDecoration(new BoxDecoration(Shape: BoxShape.Circle)).Shape);
        Assert.Equal(
            new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(100)),
            ShapeDecoration.FromBoxDecoration(
                new BoxDecoration(BorderRadius: BorderRadius.Circular(100))).Shape);
        Assert.Equal(
            new CircleBorder(new BorderSide(Green)),
            ShapeDecoration.FromBoxDecoration(
                new BoxDecoration(Shape: BoxShape.Circle, Border: Border.All(Green))).Shape);
        Assert.Equal(
            Border.All(Blue),
            ShapeDecoration.FromBoxDecoration(new BoxDecoration(Border: Border.All(Blue))).Shape);

        var decoration = new ShapeDecoration(new RoundedRectangleBorder(new BorderSide(Green, 4.0)));
        Assert.Equal(EdgeInsets.All(4.0), decoration.Padding);

        var circleDecoration = new ShapeDecoration(new CircleBorder());
        Assert.True(circleDecoration.HitTest(new Size(100, 20), new Point(50, 10)));
        Assert.False(circleDecoration.HitTest(new Size(100, 20), new Point(1, 1)));
        Assert.False(circleDecoration.HitTest(new Size(100, 20), new Point(30, 10)));

        Path clip = circleDecoration.GetClipPath(new Rect(0, 0, 100, 20), TextDirection.Ltr);
        Assert.True(clip.Contains(new Point(50, 10)));
        Assert.False(clip.Contains(new Point(99, 19)));

        Assert.Null(ShapeDecoration.Lerp(null, null, 0.0));
        Assert.Same(decoration, ShapeDecoration.Lerp(decoration, decoration, 0.5));
    }

    private sealed class PaintProbe
    {
        public PaintProbe()
        {
            Context = new PaintingContext(new OffsetLayer());
        }

        public PaintingContext Context { get; }
    }
}
