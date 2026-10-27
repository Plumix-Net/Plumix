using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: engine/src/flutter/testing/dart/codec_test.dart
[Collection(SchedulerTestCollection.Name)]
public sealed class AnimatedImageCodecTests
{
    internal static string Fixture(string name) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "AnimatedImages", name);

    [Theory]
    [InlineData("hello_loop_2.gif")]
    [InlineData("hello_loop_2.webp")]
    public async Task AnimationMetadataDurationsWrappingAndOwnedFrames(string name)
    {
        Codec codec = await ImageDecoding.InstantiateImageCodec(File.ReadAllBytes(Fixture(name)));
        Assert.True(codec.FrameCount > 1);
        Assert.Equal(1, codec.RepetitionCount);
        FrameInfo first = await EventLoopPump.WaitFor(codec.GetNextFrame());
        Assert.True(first.Duration > TimeSpan.Zero);
        byte[] expected = RasterBackend.ReadPixels(first.Image);
        for (int index = 1; index < codec.FrameCount; index++)
        {
            FrameInfo frame = await EventLoopPump.WaitFor(codec.GetNextFrame());
            Assert.Equal(first.Image.PixelSize, frame.Image.PixelSize);
            frame.Image.Dispose();
        }

        FrameInfo wrapped = await EventLoopPump.WaitFor(codec.GetNextFrame());
        codec.Dispose();
        Assert.Equal(expected, RasterBackend.ReadPixels(wrapped.Image));
        Assert.Equal(expected, RasterBackend.ReadPixels(first.Image));
        first.Image.Dispose();
        wrapped.Image.Dispose();
        Assert.Throws<ObjectDisposedException>(() => { _ = codec.GetNextFrame(); });
    }

    [Theory]
    [InlineData("four_frame_with_reuse.gif", 4, "four_frame_with_reuse_end.png")]
    [InlineData("heart.webp", 69, "heart_end.png")]
    [InlineData("2_dispose_op_restore_previous.apng", 68, "2_dispose_op_restore_previous.apng.67.png")]
    [InlineData("2_dispose_op_restore_previous.apng", 69, "2_dispose_op_restore_previous.apng.68.png")]
    [InlineData("2_dispose_op_restore_previous.apng", 70, "2_dispose_op_restore_previous.apng.69.png")]
    public async Task DependentFramesMatchFlutterEngineGoldens(string source, int count, string golden)
    {
        using Codec codec = await ImageDecoding.InstantiateImageCodec(File.ReadAllBytes(Fixture(source)));
        FrameInfo? actual = null;
        try
        {
            for (int index = 0; index < count; index++)
            {
                actual?.Image.Dispose();
                actual = await EventLoopPump.WaitFor(codec.GetNextFrame());
            }

            using var expected = new Bitmap(Fixture(golden));
            Assert.Equal(expected.PixelSize, actual!.Image.PixelSize);
            Assert.Equal(RasterBackend.ReadPixels(expected), RasterBackend.ReadPixels(actual.Image));
        }
        finally
        {
            actual?.Image.Dispose();
        }
    }

    [Fact]
    public async Task ApngPreservesPremultipliedAlpha()
    {
        using Codec codec = await ImageDecoding.InstantiateImageCodec(
            File.ReadAllBytes(Fixture("alpha_animated.apng")));
        Assert.Equal(2, codec.FrameCount);
        FrameInfo first = await EventLoopPump.WaitFor(codec.GetNextFrame());
        Assert.Equal((51, 0, 0, 51), AsInts(RasterBackend.PixelAt(first.Image, 0, 0)));
        first.Image.Dispose();
        FrameInfo second = await EventLoopPump.WaitFor(codec.GetNextFrame());
        Assert.Equal((153, 0, 0, 153), AsInts(RasterBackend.PixelAt(second.Image, 0, 0)));
        second.Image.Dispose();
    }

    [Fact]
    public async Task ApngRestoresBackgroundToTransparent()
    {
        using Codec codec = await ImageDecoding.InstantiateImageCodec(
            File.ReadAllBytes(Fixture("dispose_op_background.apng")));
        for (int index = 0; index < 4; index++)
        {
            FrameInfo frame = await EventLoopPump.WaitFor(codec.GetNextFrame());
            using Bitmap image = frame.Image;
            if (index == 0) Assert.Equal((255, 0, 0, 255), AsInts(RasterBackend.PixelAt(image, 0, 0)));
            if (index == 2)
            {
                Assert.Equal((0, 0, 255, 255),
                    AsInts(RasterBackend.PixelAt(image, image.PixelSize.Width - 1, image.PixelSize.Height - 1)));
            }

            if (index == 3)
            {
                Assert.Equal((0, 0, 0, 0),
                    AsInts(RasterBackend.PixelAt(image, image.PixelSize.Width - 1, image.PixelSize.Height - 1)));
            }
        }
    }

    [Theory]
    [InlineData("out_of_bounds.apng")]
    [InlineData("out_of_bounds_wrapping.apng")]
    public async Task InvalidAnimatedFrameBoundsFailWithoutNativeCrash(string name)
    {
        using Codec codec = await ImageDecoding.InstantiateImageCodec(File.ReadAllBytes(Fixture(name)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => EventLoopPump.WaitFor(codec.GetNextFrame()));
    }

    [Fact]
    public async Task SizeCallbackResizesEveryAnimatedFrameAndDisposesEncodedBuffer()
    {
        ImmutableBuffer buffer = await ImmutableBuffer.FromUint8List(File.ReadAllBytes(Fixture("hello_loop_2.gif")));
        int calls = 0;
        using Codec codec = await ImageDecoding.InstantiateImageCodecWithSize(buffer, (_, _) =>
        {
            calls++;
            return new TargetImageSize(24, 16);
        });
        if (Foundation.Constants.KDebugMode) Assert.True(buffer.DebugDisposed);
        for (int index = 0; index < codec.FrameCount + 1; index++)
        {
            FrameInfo frame = await EventLoopPump.WaitFor(codec.GetNextFrame());
            Assert.Equal(new PixelSize(24, 16), frame.Image.PixelSize);
            frame.Image.Dispose();
        }

        Assert.Equal(1, calls);
    }

    private static (int, int, int, int) AsInts((byte R, byte G, byte B, byte A) color) =>
        (color.R, color.G, color.B, color.A);
}
