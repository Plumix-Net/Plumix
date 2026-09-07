using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/sliver.dart (parity tests)

namespace Plumix.Tests;

/// <summary>
/// Covers <see cref="SliverEnsureSemantics"/>: a sliver wrapped in it stays in the semantics tree
/// even when it is scrolled outside the viewport and its cache extent, while an unwrapped sibling
/// drops out.
/// </summary>
public sealed class SliverEnsureSemanticsTests
{
    private static readonly Size Surface = new(300, 200);

    [Fact]
    public void WrappedSliver_StaysInTheSemanticsTreeWhenScrolledOutOfTheCacheExtent()
    {
        using var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(Build(controller, ensureSemantics: true));
        harness.Pump(Surface);

        controller.JumpTo(4000);
        harness.Pump(Surface);

        Assert.NotNull(harness.FindSemanticsNode("ensured"));
    }

    [Fact]
    public void UnwrappedSliver_LeavesTheSemanticsTreeWhenScrolledOutOfTheCacheExtent()
    {
        using var controller = new ScrollController();
        var harness = new ScrollSemanticsHarness(Build(controller, ensureSemantics: false));
        harness.Pump(Surface);

        controller.JumpTo(4000);
        harness.Pump(Surface);

        Assert.Null(harness.FindSemanticsNode("ensured"));
    }

    [Fact]
    public void RenderObject_OptsIntoSemantics()
    {
        var sliver = new SliverEnsureSemantics(new SliverToBoxAdapter(new SizedBox()));
        RenderObject renderObject = sliver.CreateRenderObject(null!);
        Assert.True(Assert.IsAssignableFrom<RenderSliver>(renderObject).EnsureSemantics);
    }

    private static Widget Build(ScrollController controller, bool ensureSemantics)
    {
        Widget marker = new SliverToBoxAdapter(
            new SizedBox(height: 40, child: new Semantics(label: "ensured", container: true)));
        if (ensureSemantics)
        {
            marker = new SliverEnsureSemantics(marker);
        }

        return new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                controller: controller,
                slivers:
                [
                    marker,
                    SliverFixedExtentList.Builder(
                        static (_, index) => new Semantics(label: $"item {index}", container: true),
                        100.0,
                        80),
                ]));
    }
}
