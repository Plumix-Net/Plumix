using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;

// C#-only infrastructure: the host's half of the semantics bridge. Semantics are produced by
// `RendererBinding.drawFrame` for the render-only view the host registers (rendering/binding.dart), and
// only while `SemanticsBinding.semanticsEnabled` holds, as for Dart's deprecated `pipelineOwner`.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PlumixHostSemanticsTests : IDisposable
{
    private SemanticsHandle? _semantics;
    private readonly List<PlumixHost> _hosts = [];

    public PlumixHostSemanticsTests()
    {
        SchedulerBinding.ResetForTests();
        _semantics = SemanticsBinding.Instance.EnsureSemantics();
    }

    public void Dispose()
    {
        foreach (PlumixHost host in _hosts)
        {
            host.SetRootChild(null);
        }

        _semantics?.Dispose();
        SchedulerBinding.ResetForTests();
    }

    private PlumixHost CreateHost()
    {
        var host = new PlumixHost();
        _hosts.Add(host);
        return host;
    }

    [Fact]
    public void PlumixHost_WithoutSemanticsEnabled_ProducesNoSemanticsTree()
    {
        // Dart's deprecated pipelineOwner only builds semantics while the binding's manifold reports
        // them enabled; nothing forces them on for a host.
        _semantics!.Dispose();
        _semantics = null;
        PlumixHost host = CreateHost();
        host.SetRootChild(new RenderButton(
            label: "Quiet",
            onPressed: static () => { },
            background: Colors.SteelBlue,
            foreground: Colors.White,
            fontSize: 14));
        host.PumpFrameForTests(new Size(320, 180));

        Assert.Null(host.SemanticsRoot);
        _semantics = SemanticsBinding.Instance.EnsureSemantics();
        host.PumpFrameForTests(new Size(320, 180));
        Assert.NotNull(host.SemanticsRoot);
    }

    [Fact]
    public void PlumixHost_SemanticsBridge_ExposesRootAndDispatchesAction()
    {
        bool tapped = false;
        PlumixHost host = CreateHost();
        host.SetRootChild(new RenderButton(
            label: "Tap me",
            onPressed: () => tapped = true,
            background: Colors.SteelBlue,
            foreground: Colors.White,
            fontSize: 14));

        host.PumpFrameForTests(new Size(320, 180));

        var root = host.SemanticsRoot;
        Assert.NotNull(root);
        var semanticsButton = FindFirstNode(root!, static node => node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(semanticsButton);
        Assert.Equal("Tap me", semanticsButton!.Label);
        Assert.True(semanticsButton.Actions.HasFlag(SemanticsActions.Tap));

        Assert.True(host.PerformSemanticsAction(semanticsButton.Id, SemanticsActions.Tap));
        Assert.True(tapped);
        Assert.False(host.PerformSemanticsAction(semanticsButton.Id, SemanticsActions.Dismiss));
    }

    [Fact]
    public void PlumixHost_SemanticsUpdated_EventRaisedOnSemanticsFlush()
    {
        PlumixHost host = CreateHost();
        var button = new RenderButton(
            label: "Initial",
            onPressed: null,
            background: Colors.Gray,
            foreground: Colors.White,
            fontSize: 14);
        int updateCount = 0;
        SemanticsNode? lastRoot = null;

        host.SemanticsUpdated += root =>
        {
            updateCount += 1;
            lastRoot = root;
        };

        host.SetRootChild(button);
        host.PumpFrameForTests(new Size(320, 180));

        Assert.Equal(1, updateCount);
        Assert.NotNull(lastRoot);
        var firstButtonNode = FindFirstNode(lastRoot!, static node => node.Label == "Initial");
        Assert.NotNull(firstButtonNode);

        button.Label = "Updated";
        host.PumpFrameForTests(new Size(320, 180));

        Assert.Equal(2, updateCount);
        Assert.NotNull(lastRoot);
        var updatedButtonNode = FindFirstNode(lastRoot!, static node => node.Label == "Updated");
        Assert.NotNull(updatedButtonNode);
    }

    [Fact]
    public void PlumixHost_GetRectOfSemanticsNodeInViewCoordinates_ResolvesTheNodeBox()
    {
        PlumixHost host = CreateHost();
        host.SetRootChild(new RenderButton(
            label: "Measure me",
            onPressed: static () => { },
            background: Colors.SteelBlue,
            foreground: Colors.White,
            fontSize: 14));
        host.PumpFrameForTests(new Size(320, 180));

        SemanticsNode? root = host.SemanticsRoot;
        Assert.NotNull(root);
        SemanticsNode? button = FindFirstNode(root!, static node => node.Label == "Measure me");
        Assert.NotNull(button);

        Rect? rect = host.GetRectOfSemanticsNodeInViewCoordinates(host.RootFlutterView.ViewId, button!.Id);
        Assert.NotNull(rect);
        Assert.Equal(button.GlobalRect, rect!.Value);

        // Unknown view and unknown node both resolve to null instead of throwing: the platform may
        // be acting on a tree the framework has already replaced.
        Assert.Null(host.GetRectOfSemanticsNodeInViewCoordinates(viewId: 999, nodeId: button.Id));
        Assert.Null(host.GetRectOfSemanticsNodeInViewCoordinates(viewId: 0, nodeId: -1));
    }

    private static SemanticsNode? FindFirstNode(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindFirstNode(child, predicate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
