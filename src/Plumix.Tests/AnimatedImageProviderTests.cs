using System.Net;
using System.Net.Sockets;
using System.Text;
using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources: flutter/packages/flutter/lib/src/painting/image_provider.dart,
// flutter/packages/flutter/test/widgets/image_test.dart (animation pause/first-frame/keep-alive cases).
[Collection(SchedulerTestCollection.Name)]
public sealed class AnimatedImageProviderTests : IDisposable
{
    internal static byte[] AnimationBytes() => Convert.FromBase64String(
        "R0lGODlhAQABAIAAAP88PChk/yH/C05FVFNDQVBFMi4wAwEAAAAh+QQEIwAAACwAAAAAAQABAAACAkQB" +
        "ACH5BAQjAAAALAAAAAABAAEAAAICTAEAOw==");

    public AnimatedImageProviderTests()
    {
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
        SchedulerBinding.ResetForTests();
    }

    public void Dispose()
    {
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
        SchedulerBinding.ResetForTests();
    }

    [Theory]
    [InlineData("memory", false)]
    [InlineData("memory", true)]
    [InlineData("file", true)]
    [InlineData("asset", true)]
    [InlineData("network", true)]
    public async Task EveryProviderPreservesAnimationAndResizeAcrossAllFrames(string kind, bool resize)
    {
        byte[] bytes = AnimationBytes();
        string temporary = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"plumix-{Guid.NewGuid()}.gif");
        TcpListener? server = null;
        Task? serving = null;
        try
        {
            ImageProvider provider;
            if (kind == "file")
            {
                await File.WriteAllBytesAsync(temporary, bytes);
                provider = new FileImage(temporary, scale: 2);
            }
            else if (kind == "asset")
            {
                provider = new ExactAssetImage("animation.gif", scale: 2, bundle: new ByteBundle(bytes));
            }
            else if (kind == "network")
            {
                server = new TcpListener(IPAddress.Loopback, 0);
                server.Start();
                int port = ((IPEndPoint)server.LocalEndpoint).Port;
                serving = ServeImage(server, bytes);
                provider = new NetworkImage($"http://127.0.0.1:{port}/animation.gif", scale: 2);
            }
            else
            {
                provider = new MemoryImage(bytes, scale: 2);
            }

            if (resize) provider = new ResizeImage(provider, width: 24, height: 16, allowUpscaling: true);
            ImageStream stream = provider.Resolve(ImageConfiguration.Empty);
            Assert.IsType<MultiFrameImageStreamCompleter>(stream.Completer);
            List<(byte R, byte G, byte B, byte A)> colors = [];
            List<long> progress = [];
            var listener = new ImageStreamListener((image, _) =>
            {
                Assert.Equal(2, image.Scale);
                var bitmap = Assert.IsAssignableFrom<Bitmap>(image.Image);
                Assert.Equal(resize ? new PixelSize(24, 16) : new PixelSize(1, 1), bitmap.PixelSize);
                colors.Add(RasterBackend.PixelAt(bitmap, 0, 0));
                image.Dispose();
            }, OnChunk: chunk => progress.Add(chunk.CumulativeBytesLoaded));
            stream.AddListener(listener);
            Assert.True(EventLoopPump.SpinUntil(() => SchedulerBinding.Instance.TransientCallbackCount > 0));
            for (int index = 0; index < 3; index++)
            {
                SchedulerBinding.PumpFrameForTests(TimeSpan.FromMilliseconds(index * 350));
                EventLoop.FlushMicrotasks();
            }

            Assert.Equal(3, colors.Count);
            Assert.NotEqual(colors[0], colors[1]);
            Assert.Equal(colors[0], colors[2]);
            if (kind == "network") Assert.Equal(bytes.Length, progress.Last());
            stream.RemoveListener(listener);
            if (serving is not null) await serving;
        }
        finally
        {
            server?.Stop();
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PausedImageLoadsFirstFrameAndResumesWithCacheDisabled(bool useMediaQuery, bool initiallyPaused)
    {
        ImageCache.Shared.MaximumSize = 0;
        var provider = new MemoryImage(AnimationBytes());
        int? frameNumber = null;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        Widget Build(bool paused) => new Directionality(TextDirection.Ltr,
            new MediaQuery(new MediaQueryData(DisableAnimations: useMediaQuery && paused),
                new TickerMode(enabled: useMediaQuery || !paused,
                    child: new Image(provider, width: 20, height: 20,
                        frameBuilder: (_, child, frame, _) => { frameNumber = frame; return child; }))));
        try
        {
            tester.PumpWidget(Build(initiallyPaused));
            Assert.True(EventLoopPump.SpinUntil(() => SchedulerBinding.Instance.TransientCallbackCount > 0));
            tester.Pump();
            Assert.Equal(0, frameNumber);
            tester.PumpWidget(Build(true));
            int? pausedFrame = frameNumber;
            tester.Pump(TimeSpan.FromSeconds(2));
            Assert.Equal(pausedFrame, frameNumber);
            tester.PumpWidget(Build(false));
            tester.Pump(TimeSpan.FromSeconds(2));
            tester.Pump();
            // With the cache disabled, a dependency change can resolve a new stream. Its first
            // frame is numbered zero again; verify that subsequent frames actually keep playing.
            tester.Pump(TimeSpan.FromMilliseconds(350));
            tester.Pump();
            Assert.True(frameNumber > pausedFrame);
            Assert.Null(tester.TakeException());
        }
        finally
        {
            ImageCache.Shared.MaximumSize = 1000;
        }
    }

    private sealed class ByteBundle(byte[] bytes) : AssetBundle
    {
        public override Task<Stream> LoadAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
    }

    private static async Task ServeImage(TcpListener server, byte[] bytes)
    {
        using TcpClient client = await server.AcceptTcpClientAsync();
        await using NetworkStream stream = client.GetStream();
        byte[] request = new byte[4096];
        await stream.ReadAsync(request);
        byte[] headers = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 200 OK\r\nContent-Length: {bytes.Length}\r\n" +
            "Content-Type: image/gif\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers);
        await stream.WriteAsync(bytes);
    }
}
