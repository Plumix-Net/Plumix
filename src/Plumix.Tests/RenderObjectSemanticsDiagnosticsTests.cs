using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart
// (_RenderObjectSemantics.debugFillProperties / debugDescribeChildren, debugDumpRenderObjectSemanticsTree)

namespace Plumix.Tests;

public sealed class RenderObjectSemanticsDiagnosticsTests
{
    [Fact]
    public void DebugDumpRenderObjectSemanticsTree_DescribesOwnersNodesAndBoundaries()
    {
        var leaf = new RenderSemanticsAnnotations(
            label: "diagnostics leaf",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var renderView = new RenderView { Child = leaf };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        string dump = pipeline.DebugDumpRenderObjectSemanticsTree();

        Assert.Contains("RenderObjectSemantics", dump, StringComparison.Ordinal);
        Assert.Contains("owner: \"RenderView#", dump, StringComparison.Ordinal);
        Assert.Contains("owner: \"RenderSemanticsAnnotations#", dump, StringComparison.Ordinal);
        // Both the root and the annotated box are semantic boundaries that formed a node.
        Assert.Equal(2, CountOccurrences(dump, "semantic boundary"));
        Assert.Equal(2, CountOccurrences(dump, "formedSemanticsNode: formed "));
        // Nothing is dirty or blocking after a clean flush.
        Assert.DoesNotContain("NO PARENT DATA", dump, StringComparison.Ordinal);
        Assert.DoesNotContain("NO GEOMETRY", dump, StringComparison.Ordinal);
        Assert.DoesNotContain("BLOCK PREVIOUS", dump, StringComparison.Ordinal);
        Assert.DoesNotContain("BLOCKS SEMANTICS", dump, StringComparison.Ordinal);
        Assert.DoesNotContain("Sibling group", dump, StringComparison.Ordinal);
    }

    [Fact]
    public void DebugDumpRenderObjectSemanticsTree_ReportsABlockingSibling()
    {
        // The wrapper is annotated but not a boundary, so `isBlockingPreviousSibling` recurses into
        // its subtree, finds the block, and reports it on the node the wrapper forms.
        var wrapper = new RenderSemanticsAnnotations(
            label: "diagnostics blocker",
            child: new RenderBlockSemantics(blocking: true)
            {
                Child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))),
            });
        var behind = new RenderSemanticsAnnotations(
            label: "diagnostics behind",
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var row = new RenderFlex(
            children: [behind, wrapper],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        var renderView = new RenderView { Child = row };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        string dump = pipeline.DebugDumpRenderObjectSemanticsTree();

        Assert.Contains("BLOCKS SEMANTICS", dump, StringComparison.Ordinal);
        Assert.Contains("diagnostics blocker", pipeline.SemanticsOwner.DebugDumpTree(), StringComparison.Ordinal);
        // Everything painted before the block is dropped from the semantics tree.
        Assert.DoesNotContain(
            "diagnostics behind",
            pipeline.SemanticsOwner.DebugDumpTree(),
            StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        for (int index = haystack.IndexOf(needle, StringComparison.Ordinal);
             index >= 0;
             index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal))
        {
            count += 1;
        }

        return count;
    }
}
