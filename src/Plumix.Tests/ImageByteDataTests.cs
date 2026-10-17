using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (`Image.toByteData`,
// `ImageByteFormat`) and flutter/packages/flutter/lib/src/semantics/semantics.dart
// (`SemanticsNode.visitChildren`): the primitives the accessibility evaluations read pixels and
// semantics children through.

namespace Plumix.Tests;

public sealed class ImageByteDataTests
{
    // One opaque blue pixel and one half-transparent red pixel, premultiplied BGRA.
    private static WriteableBitmap TwoPixels()
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(2, 1),
            new Vector(96.0, 96.0),
            PixelFormats.Bgra8888,
            AlphaFormat.Premul);
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        Marshal.Copy(new byte[] { 255, 0, 0, 255, 0, 0, 128, 128 }, 0, framebuffer.Address, 8);
        return bitmap;
    }

    [Fact]
    public async Task RawRgba_IsPremultipliedRgbaRowByRow()
    {
        using WriteableBitmap bitmap = TwoPixels();

        ByteData data = (await bitmap.ToByteData())!;

        Assert.Equal(8, data.LengthInBytes);
        Assert.Equal(0x0000FFFFu, data.GetUint32(0));
        Assert.Equal(0x80000080u, data.GetUint32(4));
    }

    [Fact]
    public async Task RawStraightRgba_UnpremultipliesAlpha()
    {
        using WriteableBitmap bitmap = TwoPixels();

        ByteData data = (await bitmap.ToByteData(ImageByteFormat.RawStraightRgba))!;

        Assert.Equal(0x0000FFFFu, data.GetUint32(0));
        Assert.InRange(data.GetUint8(4), (byte)254, (byte)255);
        Assert.Equal(0, data.GetUint8(5));
        Assert.Equal(0, data.GetUint8(6));
        Assert.Equal(128, data.GetUint8(7));
    }

    [Fact]
    public async Task RawExtendedRgba128_WritesStraightFloatsPerChannel()
    {
        using WriteableBitmap bitmap = TwoPixels();

        ByteData data = (await bitmap.ToByteData(ImageByteFormat.RawExtendedRgba128))!;

        Assert.Equal(32, data.LengthInBytes);
        float[] floats = MemoryMarshal.Cast<byte, float>(data.Buffer.AsSpan(0, 32)).ToArray();
        Assert.Equal([0.0f, 0.0f, 1.0f, 1.0f], floats[..4]);
        Assert.Equal(128 / 255.0f, floats[7]);
    }

    [Fact]
    public async Task Png_EncodesThePngSignature()
    {
        using WriteableBitmap bitmap = TwoPixels();

        ByteData data = (await bitmap.ToByteData(ImageByteFormat.Png))!;

        Assert.Equal(0x89504E47u, data.GetUint32(0));
    }

    [Fact]
    public void SemanticsNodeVisitChildren_VisitsInPaintOrderUntilTheVisitorStops()
    {
        using var tester = new FrameworkDartTester();
        SemanticsHandle handle = tester.EnsureSemantics();
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Column(children:
        [
            new Semantics(container: true, label: "a", child: new SizedBox(width: 10, height: 10)),
            new Semantics(container: true, label: "b", child: new SizedBox(width: 10, height: 10)),
            new Semantics(container: true, label: "c", child: new SizedBox(width: 10, height: 10)),
        ])));
        tester.Pump();
        SemanticsNode root = tester.RenderView.Owner!.SemanticsOwner!.RootNode!;

        var all = new List<string>();
        root.VisitChildren(child =>
        {
            all.Add(child.GetSemanticsData().Label);
            return true;
        });
        var first = new List<string>();
        root.VisitChildren(child =>
        {
            first.Add(child.GetSemanticsData().Label);
            return false;
        });

        Assert.Equal(["a", "b", "c"], all);
        Assert.Equal(["a"], first);
        handle.Dispose();
    }
}
