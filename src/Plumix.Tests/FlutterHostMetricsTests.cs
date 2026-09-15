using Avalonia;
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

    private sealed class MetricsHost : PlumixHost
    {
        public void ArrangeContent(Size size) => ArrangeOverride(size);

        public MediaQueryData ReadMediaQuery() => GetMediaQueryData();
    }
}
