using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;

// Dart parity sources (reference, host semantics bridge regression tests):
// flutter/packages/flutter/lib/src/semantics/semantics.dart
// flutter/packages/flutter/lib/src/widgets/binding.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PlumixHostSemanticsTests
{
    [Fact]
    public void PlumixHost_SemanticsBridge_ExposesRootAndDispatchesAction()
    {
        bool tapped = false;
        var host = new PlumixHost();
        host.SetRootChild(new RenderButton(
            label: "Tap me",
            onPressed: () => tapped = true,
            background: Colors.SteelBlue,
            foreground: Colors.White,
            fontSize: 14));

        host.FlushPipelineForTests(new Size(320, 180));

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
        var host = new PlumixHost();
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
        host.FlushPipelineForTests(new Size(320, 180));

        Assert.Equal(1, updateCount);
        Assert.NotNull(lastRoot);
        var firstButtonNode = FindFirstNode(lastRoot!, static node => node.Label == "Initial");
        Assert.NotNull(firstButtonNode);

        button.Label = "Updated";
        host.FlushPipelineForTests(new Size(320, 180));

        Assert.Equal(2, updateCount);
        Assert.NotNull(lastRoot);
        var updatedButtonNode = FindFirstNode(lastRoot!, static node => node.Label == "Updated");
        Assert.NotNull(updatedButtonNode);
    }

    [Fact]
    public void PlumixHost_GetRectOfSemanticsNodeInViewCoordinates_ResolvesTheNodeBox()
    {
        var host = new PlumixHost();
        host.SetRootChild(new RenderButton(
            label: "Measure me",
            onPressed: static () => { },
            background: Colors.SteelBlue,
            foreground: Colors.White,
            fontSize: 14));
        host.FlushPipelineForTests(new Size(320, 180));

        SemanticsNode? root = host.SemanticsRoot;
        Assert.NotNull(root);
        SemanticsNode? button = FindFirstNode(root!, static node => node.Label == "Measure me");
        Assert.NotNull(button);

        Rect? rect = host.GetRectOfSemanticsNodeInViewCoordinates(viewId: 0, nodeId: button!.Id);
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
