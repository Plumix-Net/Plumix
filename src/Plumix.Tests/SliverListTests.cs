using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ported from flutter/packages/flutter/test/widgets/sliver_list_test.dart: the keyed reverse/replace
// cases and "recalculate inaccurate layout offset case 1". The other cases of that file live in
// SliverChildManagerTests.cs and ScrollDelegateTests.cs.

namespace Plumix.Tests;

public sealed class SliverListTests
{
    [Fact]
    public void SliverList_ReverseChildren_WithKeys()
    {
        using var tester = new FrameworkDartTester();
        List<int> items = [.. Enumerable.Range(0, 20)];
        const double itemHeight = 300.0;
        const double viewportHeight = 500.0;

        const double scrollPosition = 18 * itemHeight;
        var controller = new ScrollController(initialScrollOffset: scrollPosition);
        try
        {
            tester.PumpWidget(BuildSliverList(items, controller, itemHeight, viewportHeight));
            tester.PumpAndSettle();

            Assert.Equal(scrollPosition, controller.Offset);
            AssertTiles(tester, found: [18, 19], missing: [0, 1]);

            tester.PumpWidget(BuildSliverList(
                [.. Enumerable.Reverse(items)],
                controller,
                itemHeight,
                viewportHeight));
            int frames = tester.PumpAndSettle();
            Assert.Equal(1, frames); // ensures that there is no (animated) bouncing of the scrollable

            Assert.Equal(scrollPosition, controller.Offset);
            AssertTiles(tester, found: [1, 0], missing: [19, 18]);

            controller.JumpTo(0.0);
            tester.PumpAndSettle();

            Assert.Equal(0.0, controller.Offset);
            AssertTiles(tester, found: [19, 18], missing: [1, 0]);
        }
        finally
        {
            controller.Dispose();
        }
    }

    [Fact]
    public void SliverList_ReplaceChildren_WithKeys()
    {
        using var tester = new FrameworkDartTester();
        List<int> items = [.. Enumerable.Range(0, 20)];
        const double itemHeight = 300.0;
        const double viewportHeight = 500.0;

        const double scrollPosition = 18 * itemHeight;
        var controller = new ScrollController(initialScrollOffset: scrollPosition);
        try
        {
            tester.PumpWidget(BuildSliverList(items, controller, itemHeight, viewportHeight));
            tester.PumpAndSettle();

            Assert.Equal(scrollPosition, controller.Offset);
            AssertTiles(tester, found: [18, 19], missing: [0, 1]);

            tester.PumpWidget(BuildSliverList(
                [.. items.Select(i => i + 100)],
                controller,
                itemHeight,
                viewportHeight));
            int frames = tester.PumpAndSettle();
            Assert.Equal(1, frames); // ensures that there is no (animated) bouncing of the scrollable

            Assert.Equal(scrollPosition, controller.Offset);
            AssertTiles(tester, found: [118, 119], missing: [0, 1, 18, 19, 100, 101]);

            controller.JumpTo(0.0);
            tester.PumpAndSettle();

            Assert.Equal(0.0, controller.Offset);
            AssertTiles(tester, found: [100, 101], missing: [118, 119]);
        }
        finally
        {
            controller.Dispose();
        }
    }

    [Fact]
    public void SliverList_ReplaceWithShorterChildrenList_WithKeys()
    {
        using var tester = new FrameworkDartTester();
        List<int> items = [.. Enumerable.Range(0, 20)];
        const double itemHeight = 300.0;
        const double viewportHeight = 500.0;

        double scrollPosition = items.Count * itemHeight - viewportHeight;
        var controller = new ScrollController(initialScrollOffset: scrollPosition);
        try
        {
            tester.PumpWidget(BuildSliverList(items, controller, itemHeight, viewportHeight));
            tester.PumpAndSettle();

            Assert.Equal(scrollPosition, controller.Offset);
            AssertTiles(tester, found: [18, 19], missing: [0, 1, 17]);

            tester.PumpWidget(BuildSliverList(items[..^1], controller, itemHeight, viewportHeight));
            int frames = tester.PumpAndSettle();
            Assert.Equal(1, frames); // No animation when content shrinks suddenly.

            Assert.Equal(scrollPosition - itemHeight, controller.Offset);
            AssertTiles(tester, found: [17, 18], missing: [0, 1, 19]);
        }
        finally
        {
            controller.Dispose();
        }
    }

    /// <remarks>Regression test for https://github.com/flutter/flutter/issues/42142.</remarks>
    [Fact]
    public void SliverList_ShouldRecalculateInaccurateLayoutOffset_Case1()
    {
        using var tester = new FrameworkDartTester();
        List<int> items = [.. Enumerable.Range(0, 20)];
        var controller = new ScrollController();
        try
        {
            tester.PumpWidget(BuildSliverList([.. items], controller, itemHeight: 50, viewportHeight: 200));
            tester.PumpAndSettle();

            tester.Drag(FindTile(tester, 2).Single(), new Vector(0.0, -1000.0));
            tester.PumpAndSettle();

            // Viewport should be scrolled to the end of list.
            Assert.Equal(800.0, controller.Offset);
            AssertTiles(tester, found: [16, 17, 18, 19], missing: [15]);

            // Prepends item to the list.
            items.Insert(0, -1);
            tester.PumpWidget(BuildSliverList([.. items], controller, itemHeight: 50, viewportHeight: 200));
            tester.Pump();

            // We need second pump to ensure the scheduled animation gets run.
            tester.PumpAndSettle();

            // Scroll offset should stay the same, and the items in viewport should be shifted by one.
            Assert.Equal(800.0, controller.Offset);
            AssertTiles(tester, found: [15, 16, 17, 18], missing: [14, 19]);

            // Drags back to beginning and newly added item is visible.
            tester.Drag(FindTile(tester, 16).Single(), new Vector(0.0, 1000.0));
            tester.PumpAndSettle();
            Assert.Equal(0.0, controller.Offset);
            AssertTiles(tester, found: [-1, 0, 1, 2], missing: [3]);
        }
        finally
        {
            controller.Dispose();
        }
    }

    // sliver_list_test.dart: _buildSliverList.
    private static Widget BuildSliverList(
        List<int> items,
        ScrollController? controller = null,
        double itemHeight = 500.0,
        double viewportHeight = 300.0)
    {
        return new Directionality(
            TextDirection.Ltr,
            new Center(child: new SizedBox(
                height: viewportHeight,
                child: new CustomScrollView(
                    controller: controller,
                    slivers:
                    [
                        new SliverList(new SliverChildBuilderDelegate(
                            (_, i) => new SizedBox(
                                key: new ValueKey<int>(items[i]),
                                height: itemHeight,
                                child: new Text($"Tile {items[i]}")),
                            findChildIndexCallback: key =>
                            {
                                var valueKey = (ValueKey<int>)key;
                                int index = items.IndexOf(valueKey.Value);
                                return index == -1 ? null : index;
                            },
                            childCount: items.Count)),
                    ]))));
    }

    private static void AssertTiles(FrameworkDartTester tester, int[] found, int[] missing)
    {
        foreach (int tile in found)
        {
            Assert.True(FindTile(tester, tile).Count == 1, $"find.text('Tile {tile}') should find one widget");
        }

        foreach (int tile in missing)
        {
            Assert.True(FindTile(tester, tile).Count == 0, $"find.text('Tile {tile}') should find nothing");
        }
    }

    // flutter_test finders skip offstage elements (a sliver's cached children) by default.
    private static List<Element> FindTile(FrameworkDartTester tester, int tile)
    {
        string text = $"Tile {tile}";
        var result = new List<Element>();
        void Visit(Element element)
        {
            if (element.Widget is Text { Data: var data } && data == text)
            {
                result.Add(element);
            }

            element.DebugVisitOnstageChildren(Visit);
        }

        tester.Root.DebugVisitOnstageChildren(Visit);
        return result;
    }
}
