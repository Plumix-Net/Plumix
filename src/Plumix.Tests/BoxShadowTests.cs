using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/box_painter_test.dart
public sealed class BoxShadowTests
{
    [Fact]
    public void BoxShadow_ControlTest()
    {
        var shadow = new BoxShadow(blurRadius: 4.0);

        Assert.Equal(new BoxShadow(blurRadius: 4.0), shadow);
        Assert.Equal(Color.FromArgb(0xFF, 0, 0, 0), shadow.Color);
        Assert.Equal(new Point(0, 0), shadow.Offset);
        Assert.Equal(0.0, shadow.SpreadRadius);
        Assert.Equal(BlurStyle.Normal, shadow.BlurStyle);

        BoxShadow? scaledDown = BoxShadow.Lerp(null, shadow, 0.25);
        BoxShadow? scaledUp = BoxShadow.Lerp(shadow, null, 0.25);

        Assert.Equal(1.0, scaledDown!.BlurRadius);
        Assert.Equal(3.0, scaledUp!.BlurRadius);
        Assert.Equal(2.0, BoxShadow.Lerp(scaledDown, scaledUp, 0.5)!.BlurRadius);
        Assert.NotEqual(shadow.GetHashCode(), scaledDown.GetHashCode());
    }

    [Fact]
    public void BoxShadow_LerpList_ScalesExcessEntries()
    {
        var keep = new BoxShadow(color: Colors.Green, blurRadius: 1.0);
        var first = new BoxShadow(color: Colors.Red, blurRadius: 2.0);
        var second = new BoxShadow(color: Colors.Red, blurRadius: 4.0);
        BoxShadow? middle = BoxShadow.Lerp(first, second, 0.5);

        Assert.NotNull(middle);
        Assert.Equal(
            [middle, keep.Scale(0.5)],
            BoxShadow.LerpList([first, keep], [second], 0.5)!);
        Assert.Equal(
            [middle, keep.Scale(0.5)],
            BoxShadow.LerpList([first], [second, keep], 0.5)!);
    }

    [Fact]
    public void BoxShadow_Lerp_IdenticalEndpoints()
    {
        Assert.Null(BoxShadow.Lerp(null, null, 0.0));
        Assert.Null(BoxShadow.LerpList(null, null, 0.0));

        var shadow = new BoxShadow(blurRadius: 4.0);
        Assert.Same(shadow, BoxShadow.Lerp(shadow, shadow, 0.5));

        BoxShadow[] shadows = [shadow];
        Assert.Same(shadows, BoxShadow.LerpList(shadows, shadows, 0.5));
    }

    [Fact]
    public void BoxShadow_BlurStyleTest()
    {
        var normal = new BoxShadow(blurRadius: 4.0);
        var outer = new BoxShadow(blurRadius: 4.0, blurStyle: BlurStyle.Outer);
        var solid = new BoxShadow(blurRadius: 4.0, blurStyle: BlurStyle.Solid);

        Assert.Equal(BlurStyle.Normal, normal.BlurStyle);
        Assert.Equal(BlurStyle.Normal, BoxShadow.Lerp(normal, null, 0.25)!.BlurStyle);
        Assert.Equal(BlurStyle.Normal, BoxShadow.Lerp(null, normal, 0.25)!.BlurStyle);
        Assert.Equal(BlurStyle.Outer, BoxShadow.Lerp(normal, outer, 0.25)!.BlurStyle);
        Assert.Equal(BlurStyle.Solid, BoxShadow.Lerp(solid, outer, 0.25)!.BlurStyle);

        IReadOnlyList<BoxShadow>? lerped = BoxShadow.LerpList([normal, solid], [outer], 0.25);
        Assert.NotNull(lerped);
        Assert.Equal(BlurStyle.Outer, lerped[0].BlurStyle);
        Assert.Equal(BlurStyle.Solid, lerped[1].BlurStyle);
    }

    [Fact]
    public void BoxShadow_ScaleKeepsColorAndStyle()
    {
        var shadow = new BoxShadow(
            color: Colors.Red,
            offset: new Point(2, 4),
            blurRadius: 6.0,
            spreadRadius: 8.0,
            blurStyle: BlurStyle.Inner);

        BoxShadow scaled = shadow.Scale(0.5);

        Assert.Equal(Colors.Red, scaled.Color);
        Assert.Equal(new Point(1, 2), scaled.Offset);
        Assert.Equal(3.0, scaled.BlurRadius);
        Assert.Equal(4.0, scaled.SpreadRadius);
        Assert.Equal(BlurStyle.Inner, scaled.BlurStyle);
    }

    [Fact]
    public void BoxShadow_CopyWithReplacesOnlyGivenFields()
    {
        var shadow = new BoxShadow(color: Colors.Red, offset: new Point(1, 2), blurRadius: 3.0);

        BoxShadow copy = shadow.CopyWith(spreadRadius: 5.0);

        Assert.Equal(Colors.Red, copy.Color);
        Assert.Equal(new Point(1, 2), copy.Offset);
        Assert.Equal(3.0, copy.BlurRadius);
        Assert.Equal(5.0, copy.SpreadRadius);
    }

    [Fact]
    public void BoxShadow_ToStringTest()
    {
        Assert.Equal(
            "BoxShadow(Color(0xff000000), Offset(0.0, 0.0), 4.0, 0.0, BlurStyle.normal)",
            new BoxShadow(blurRadius: 4.0).ToString());
        Assert.Contains("BlurStyle.solid", new BoxShadow(blurRadius: 4.0, blurStyle: BlurStyle.Solid).ToString());
    }

    [Fact]
    public void Shadow_ConvertsRadiusToSigmaAndRejectsNegativeBlur()
    {
        Assert.Equal(0.0, Shadow.ConvertRadiusToSigma(0.0));
        Assert.Equal((4.0 * 0.57735) + 0.5, Shadow.ConvertRadiusToSigma(4.0));
        Assert.Equal((4.0 * 0.57735) + 0.5, new BoxShadow(blurRadius: 4.0).BlurSigma);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoxShadow(blurRadius: -1.0));
    }

    [Fact]
    public void Shadow_LerpListPadsShorterList()
    {
        var first = new Shadow(color: Colors.Red, blurRadius: 2.0);
        var second = new Shadow(color: Colors.Red, blurRadius: 4.0);

        Assert.Null(Shadow.LerpList(null, null, 0.5));

        IReadOnlyList<Shadow>? lerped = Shadow.LerpList([first, first], [second], 0.5);

        Assert.NotNull(lerped);
        Assert.Equal(2, lerped.Count);
        Assert.Equal(3.0, lerped[0].BlurRadius);
        Assert.Equal(1.0, lerped[1].BlurRadius);
    }

    [Fact]
    public void BoxShadow_MapsOntoTheBackendShadowValue()
    {
        var shadow = new BoxShadow(
            color: Colors.Red,
            offset: new Point(1, 2),
            blurRadius: 3.0,
            spreadRadius: 4.0);

        Avalonia.Media.BoxShadow converted = shadow.ToAvalonia();

        Assert.Equal(1.0, converted.OffsetX);
        Assert.Equal(2.0, converted.OffsetY);
        Assert.Equal(3.0, converted.Blur);
        Assert.Equal(4.0, converted.Spread);
        Assert.Equal(Colors.Red, converted.Color);
        Assert.False(converted.IsInset);

        Avalonia.Media.BoxShadows empty = ((IReadOnlyList<BoxShadow>?)null).ToAvalonia();
        Assert.Equal(0, empty.Count);
        Assert.Equal(2, new[] { shadow, shadow }.ToAvalonia().Count);
    }

    [Fact]
    public void BoxDecoration_LerpsShadowListsInsteadOfSnapping()
    {
        var from = new BoxDecoration(BoxShadows: [new BoxShadow(color: Colors.Red, blurRadius: 2.0)]);
        var to = new BoxDecoration(BoxShadows: [new BoxShadow(color: Colors.Red, blurRadius: 6.0)]);

        var middle = Assert.IsType<BoxDecoration>(BoxDecoration.Lerp(from, to, 0.5));

        Assert.NotNull(middle.BoxShadows);
        Assert.Equal(4.0, middle.BoxShadows[0].BlurRadius);
    }

    [Fact]
    public void ShapeDecoration_LerpsShadowListsInsteadOfSnapping()
    {
        var from = new ShapeDecoration(
            Shape: new CircleBorder(),
            Shadows: [new BoxShadow(color: Colors.Red, blurRadius: 2.0)]);
        var to = new ShapeDecoration(
            Shape: new CircleBorder(),
            Shadows: [new BoxShadow(color: Colors.Red, blurRadius: 6.0)]);

        var middle = Assert.IsType<ShapeDecoration>(ShapeDecoration.Lerp(from, to, 0.5));

        Assert.NotNull(middle.Shadows);
        Assert.Equal(4.0, middle.Shadows[0].BlurRadius);
    }

    [Fact]
    public void BoxDecoration_ComparesShadowListsStructurally()
    {
        var first = new BoxDecoration(BoxShadows: [new BoxShadow(color: Colors.Red, blurRadius: 2.0)]);
        var second = new BoxDecoration(BoxShadows: [new BoxShadow(color: Colors.Red, blurRadius: 2.0)]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(
            first,
            new BoxDecoration(BoxShadows: [new BoxShadow(color: Colors.Red, blurRadius: 3.0)]));
    }
}
