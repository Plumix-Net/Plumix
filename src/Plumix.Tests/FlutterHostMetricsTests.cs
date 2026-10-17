using Avalonia;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only infrastructure: Avalonia publishes Bounds after the host's ArrangeOverride returns.

namespace Plumix.Tests;

public sealed class FlutterHostMetricsTests
{
    [Fact]
    public void ArrangePublishesTheIncomingSizeBeforeAvaloniaUpdatesBounds()
    {
        var host = new MetricsHost();
        Assert.Equal(default, host.Bounds.Size);

        host.ArrangeContent(new Size(350, 700));
        Assert.Equal(new Size(350, 700), host.RootFlutterView.PhysicalSize);
        Assert.Equal(new Size(350, 700), host.ReadMediaQuery().Size);

        // The old Bounds are still visible within ArrangeOverride during a resize, too.
        host.ArrangeContent(new Size(600, 400));
        Assert.Equal(new Size(600, 400), host.RootFlutterView.PhysicalSize);
        Assert.Equal(new Size(600, 400), host.ReadMediaQuery().Size);
    }

    [Fact]
    public void PointerDownAsksTheFrameworkForNativeViewsAtTheViewId()
    {
        var host = new MetricsHost();
        var requests = new List<HitTestRequest>();
        var answers = new List<(Point, bool)>();
        Func<HitTestRequest, HitTestResponse>? previous = PlatformDispatcher.Instance.OnHitTest;
        PlatformDispatcher.Instance.OnHitTest = request =>
        {
            requests.Add(request);
            return new HitTestResponse(true);
        };
        host.PlatformViewHitTested += (position, hit) => answers.Add((position, hit));

        try
        {
            host.QueryNativeHitTest(new Point(3, 4));

            Assert.Equal(new HitTestRequest(host.RootFlutterView.ViewId, new Point(3, 4)), Assert.Single(requests));
            Assert.Equal((new Point(3, 4), true), Assert.Single(answers));
        }
        finally
        {
            PlatformDispatcher.Instance.OnHitTest = previous;
        }
    }

    private sealed class MetricsHost : PlumixHost
    {
        public void ArrangeContent(Size size) => ArrangeOverride(size);

        public MediaQueryData ReadMediaQuery() => GetMediaQueryData();
    }
}
