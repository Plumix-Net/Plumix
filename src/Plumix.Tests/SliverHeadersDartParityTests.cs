using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/widgets/pinned_header_sliver_test.dart,
// flutter/packages/flutter/test/widgets/sliver_resizing_header_test.dart and
// flutter/packages/flutter/test/widgets/sliver_floating_header_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SliverHeadersDartParityTests
{
    // ------------------------------------------------------------- pinned_header_sliver_test.dart

    private static Widget PinnedFrame(Axis axis, bool reverse)
    {
        return new TestWidgetsApp(
            home: new CustomScrollView(
                scrollDirection: axis,
                reverse: reverse,
                slivers:
                [
                    new PinnedHeaderSliver(child: new Text("PinnedHeaderSliver")),
                    SliverList.Builder(itemCount: 100, itemBuilder: (_, index) => new Text($"Item {index}")),
                ]));
    }

    // Flutter: 'PinnedHeaderSliver basics'
    [Fact]
    public void PinnedHeaderSliverBasics()
    {
        using var tester = new FrameworkDartTester();
        Rect GetHeaderRect() => tester.GetRect(Text(tester, "PinnedHeaderSliver"));
        Rect GetItemRect(int index) => tester.GetRect(Text(tester, $"Item {index}"));

        {
            tester.PumpWidget(PinnedFrame(axis: Axis.Vertical, reverse: false));
            tester.PumpAndSettle();
            ScrollPosition position = tester.State<ScrollableState>().Position;

            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(tester.GetSize(Text(tester, "PinnedHeaderSliver")).Height, GetHeaderRect().Height);

            double itemHeight = GetItemRect(0).Height;
            int visibleItemCount = (int)(600 / itemHeight) - 1; // less 1 for the header
            Assert.Single(tester.OnstageElementsWithText("Item 0"));
            Assert.Single(tester.OnstageElementsWithText($"Item {visibleItemCount - 1}"));

            _ = position.MoveTo(itemHeight * 5);
            tester.PumpAndSettle();
            Assert.Equal(0, GetHeaderRect().Top);
            Assert.Equal(800, GetHeaderRect().Width);
            _ = position.MoveTo(itemHeight * -5);
            Assert.Equal(0, GetHeaderRect().Top);
            Assert.Equal(800, GetHeaderRect().Width);
        }

        {
            tester.PumpWidget(PinnedFrame(axis: Axis.Horizontal, reverse: false));
            ScrollPosition position = tester.State<ScrollableState>().Position;
            tester.PumpAndSettle();

            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(600, GetHeaderRect().Height);
            Assert.Equal(tester.GetSize(Text(tester, "PinnedHeaderSliver")).Width, GetHeaderRect().Width);

            double itemWidth = GetItemRect(0).Width;
            int visibleItemCount = (int)((800 - GetHeaderRect().Width) / itemWidth);
            Assert.Single(tester.OnstageElementsWithText("Item 0"));
            Assert.Single(tester.OnstageElementsWithText($"Item {visibleItemCount - 1}"));

            _ = position.MoveTo(itemWidth * 5);
            tester.PumpAndSettle();
            Assert.Equal(0, GetHeaderRect().Left);
            Assert.Equal(600, GetHeaderRect().Height);
            _ = position.MoveTo(itemWidth * -5);
            Assert.Equal(0, GetHeaderRect().Left);
            Assert.Equal(600, GetHeaderRect().Height);
        }

        {
            tester.PumpWidget(PinnedFrame(axis: Axis.Vertical, reverse: true));
            tester.PumpAndSettle();
            ScrollPosition position = tester.State<ScrollableState>().Position;

            Assert.Equal(new Point(0, 600), GetHeaderRect().BottomLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(tester.GetSize(Text(tester, "PinnedHeaderSliver")).Height, GetHeaderRect().Height);

            double itemHeight = GetItemRect(0).Height;
            int visibleItemCount = (int)(600 / itemHeight) - 1; // less 1 for the header
            Assert.Single(tester.OnstageElementsWithText("Item 0"));
            Assert.Single(tester.OnstageElementsWithText($"Item {visibleItemCount - 1}"));

            _ = position.MoveTo(itemHeight * 5);
            tester.PumpAndSettle();
            Assert.Equal(new Point(0, 600), GetHeaderRect().BottomLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            _ = position.MoveTo(itemHeight * -5);
            Assert.Equal(new Point(0, 600), GetHeaderRect().BottomLeft);
            Assert.Equal(800, GetHeaderRect().Width);
        }

        {
            tester.PumpWidget(PinnedFrame(axis: Axis.Horizontal, reverse: true));
            ScrollPosition position = tester.State<ScrollableState>().Position;
            tester.PumpAndSettle();

            Assert.Equal(new Point(800, 0), GetHeaderRect().TopRight);
            Assert.Equal(600, GetHeaderRect().Height);
            Assert.Equal(tester.GetSize(Text(tester, "PinnedHeaderSliver")).Width, GetHeaderRect().Width);

            double itemWidth = GetItemRect(0).Width;
            int visibleItemCount = (int)((800 - GetHeaderRect().Width) / itemWidth);
            Assert.Single(tester.OnstageElementsWithText("Item 0"));
            Assert.Single(tester.OnstageElementsWithText($"Item {visibleItemCount - 1}"));

            _ = position.MoveTo(itemWidth * 5);
            tester.PumpAndSettle();
            Assert.Equal(new Point(800, 0), GetHeaderRect().TopRight);
            Assert.Equal(600, GetHeaderRect().Height);
            _ = position.MoveTo(itemWidth * -5);
            Assert.Equal(new Point(800, 0), GetHeaderRect().TopRight);
            Assert.Equal(600, GetHeaderRect().Height);
        }
    }

    // Flutter: 'PinnedHeaderSliver: multiple headers layout one after the other'
    [Fact]
    public void PinnedHeaderSliverMultipleHeadersLayoutOneAfterTheOther()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    new PinnedHeaderSliver(child: new Text("PinnedHeaderSliver 0")),
                    new PinnedHeaderSliver(child: new Text("PinnedHeaderSliver 1")),
                    new PinnedHeaderSliver(child: new Text("PinnedHeaderSliver 2")),
                    SliverList.Builder(itemCount: 100, itemBuilder: (_, index) => new Text($"Item {index}")),
                ])));

        Rect rect0 = tester.GetRect(Text(tester, "PinnedHeaderSliver 0"));
        Assert.Equal(0, rect0.Top);
        Assert.Equal(800, rect0.Width);

        Rect rect1 = tester.GetRect(Text(tester, "PinnedHeaderSliver 1"));
        Assert.Equal(rect0.Bottom, rect1.Top);
        Assert.Equal(800, rect1.Width);

        Rect rect2 = tester.GetRect(Text(tester, "PinnedHeaderSliver 2"));
        Assert.Equal(rect1.Bottom, rect2.Top);
        Assert.Equal(800, rect2.Width);
    }

    // Flutter: 'PinnedHeaderSliver: headers that do not start at the top'
    [Fact]
    public void PinnedHeaderSliverHeadersThatDoNotStartAtTheTop()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    SliverList.Builder(itemCount: 2, itemBuilder: (_, index) => new Text($"Item 0.{index}")),
                    new PinnedHeaderSliver(child: new Text("PinnedHeaderSliver 0")),
                    SliverList.Builder(itemCount: 2, itemBuilder: (_, index) => new Text($"Item 1.{index}")),
                    new PinnedHeaderSliver(child: new Text("PinnedHeaderSliver 1")),
                    SliverList.Builder(itemCount: 2, itemBuilder: (_, index) => new Text($"Item 2.{index}")),
                    new PinnedHeaderSliver(child: new Text("PinnedHeaderSliver 2")),
                    SliverList.Builder(itemCount: 100, itemBuilder: (_, index) => new Text($"Item {index}")),
                ])));

        double itemHeight = tester.GetSize(Text(tester, "Item 0.0")).Height;
        ScrollPosition position = tester.State<ScrollableState>().Position;

        _ = position.MoveTo(itemHeight * 2);
        tester.PumpAndSettle();

        Rect rect0 = tester.GetRect(Text(tester, "PinnedHeaderSliver 0"));
        Assert.Equal(0, rect0.Top);
        Assert.Equal(800, rect0.Width);

        _ = position.MoveTo(itemHeight * 4);
        tester.PumpAndSettle();

        Rect rect1 = tester.GetRect(Text(tester, "PinnedHeaderSliver 1"));
        Assert.Equal(rect0.Bottom, rect1.Top);
        Assert.Equal(800, rect1.Width);

        _ = position.MoveTo(itemHeight * 6);
        tester.PumpAndSettle();

        Rect rect2 = tester.GetRect(Text(tester, "PinnedHeaderSliver 2"));
        Assert.Equal(rect1.Bottom, rect2.Top);
        Assert.Equal(800, rect2.Width);

        _ = position.MoveTo(itemHeight * 10);
        tester.PumpAndSettle();
        Assert.Equal(rect0, tester.GetRect(Text(tester, "PinnedHeaderSliver 0")));
        Assert.Equal(rect1, tester.GetRect(Text(tester, "PinnedHeaderSliver 1")));
        Assert.Equal(rect2, tester.GetRect(Text(tester, "PinnedHeaderSliver 2")));
    }

    // Flutter: 'PinnedHeaderSliver: presence of RenderViewport.excludeFromScrolling tag when pinned'
    [Fact]
    public void PinnedHeaderSliverPresenceOfExcludeFromScrollingTagWhenPinned()
    {
        using var tester = new FrameworkDartTester();
        using SemanticsScope semantics = new(tester);

        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    new SliverToBoxAdapter(child: new SizedBox(height: 100, child: new Text("First child"))),
                    new PinnedHeaderSliver(child: new Text("PinnedHeaderSliver")),
                    SliverList.Builder(itemCount: 50, itemBuilder: (_, index) => new Text($"Item {index}")),
                ])));
        semantics.Flush();

        Rect GetHeaderRect() => tester.GetRect(Text(tester, "PinnedHeaderSliver"));
        Rect GetFirstChildRect() => tester.GetRect(Text(tester, "First child"));

        Rect firstChildRect = GetFirstChildRect();
        Assert.Equal(0.0, firstChildRect.Top);
        Assert.Equal(100.0, firstChildRect.Height);
        Assert.Equal(100.0, GetHeaderRect().Top);

        Assert.False(semantics.IncludesNodeWithTags(
            RenderViewport.ExcludeFromScrolling,
            RenderViewport.UseTwoPaneSemantics));

        tester.Drag(tester.ElementOfType<CustomScrollView>(), new Vector(0, -100));
        tester.PumpAndSettle();
        semantics.Flush();

        Assert.Equal(0.0, GetHeaderRect().Top);

        Assert.False(semantics.IncludesNodeWithTags(
            RenderViewport.ExcludeFromScrolling,
            RenderViewport.UseTwoPaneSemantics));

        tester.Drag(tester.ElementOfType<CustomScrollView>(), new Vector(0, -20));
        tester.PumpAndSettle();
        semantics.Flush();

        SemanticsNode? semanticNode = semantics.NodesWithLabel("PinnedHeaderSliver").FirstOrDefault();
        Assert.Equal(
            new HashSet<SemanticsTag> { RenderViewport.ExcludeFromScrolling, RenderViewport.UseTwoPaneSemantics },
            semanticNode?.Parent?.Tags?.ToHashSet());
    }

    // ------------------------------------------------------------ sliver_resizing_header_test.dart

    private static Widget ResizingFrame(Axis axis, bool reverse)
    {
        (Widget minPrototype, Widget maxPrototype) = axis switch
        {
            Axis.Vertical => ((Widget)new SizedBox(height: 100), (Widget)new SizedBox(height: 300)),
            _ => (new SizedBox(width: 100), new SizedBox(width: 300)),
        };
        return new TestWidgetsApp(
            home: new CustomScrollView(
                scrollDirection: axis,
                reverse: reverse,
                slivers:
                [
                    new SliverResizingHeader(
                        minExtentPrototype: minPrototype,
                        maxExtentPrototype: maxPrototype,
                        child: SizedBox.Expand(child: new Text("header"))),
                    SliverList.Builder(itemCount: 100, itemBuilder: (_, index) => new Text($"item {index}")),
                ]));
    }

    // Flutter: 'SliverResizingHeader basics'
    [Fact]
    public void SliverResizingHeaderBasics()
    {
        using var tester = new FrameworkDartTester();
        Rect GetHeaderRect() => tester.GetRect(Text(tester, "header"));
        Rect GetItemRect(int index) => tester.GetRect(Text(tester, $"item {index}"));

        {
            tester.PumpWidget(ResizingFrame(axis: Axis.Vertical, reverse: false));
            tester.PumpAndSettle();
            ScrollPosition position = tester.State<ScrollableState>().Position;

            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(300, GetHeaderRect().Height);

            double itemHeight = GetItemRect(0).Height;
            int visibleItemCount = (int)(300 / itemHeight); // 300 = viewport height - header height
            Assert.Single(tester.OnstageElementsWithText("item 0"));
            Assert.Single(tester.OnstageElementsWithText($"item {visibleItemCount - 1}"));

            _ = position.MoveTo(200);
            tester.PumpAndSettle();
            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(100, GetHeaderRect().Height);
            _ = position.MoveTo(0);
            tester.PumpAndSettle();
            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(300, GetHeaderRect().Height);
        }

        {
            tester.PumpWidget(ResizingFrame(axis: Axis.Horizontal, reverse: false));
            tester.PumpAndSettle();
            ScrollPosition position = tester.State<ScrollableState>().Position;

            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(300, GetHeaderRect().Width);
            Assert.Equal(600, GetHeaderRect().Height);

            double itemWidth = GetItemRect(0).Width;
            int visibleItemCount = (int)(500 / itemWidth); // 500 = viewport width - header width
            Assert.Single(tester.OnstageElementsWithText("item 0"));
            Assert.Single(tester.OnstageElementsWithText($"item {visibleItemCount - 1}"));

            _ = position.MoveTo(200);
            tester.PumpAndSettle();
            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(600, GetHeaderRect().Height);
            Assert.Equal(100, GetHeaderRect().Width);
            _ = position.MoveTo(0);
            tester.PumpAndSettle();
            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(600, GetHeaderRect().Height);
            Assert.Equal(300, GetHeaderRect().Width);
        }

        {
            tester.PumpWidget(ResizingFrame(axis: Axis.Vertical, reverse: true));
            tester.PumpAndSettle();
            ScrollPosition position = tester.State<ScrollableState>().Position;

            Assert.Equal(new Point(0, 600), GetHeaderRect().BottomLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(300, GetHeaderRect().Height);

            double itemHeight = GetItemRect(0).Height;
            int visibleItemCount = (int)(300 / itemHeight); // 300 = viewport height - header height
            Assert.Single(tester.OnstageElementsWithText("item 0"));
            Assert.Single(tester.OnstageElementsWithText($"item {visibleItemCount - 1}"));

            _ = position.MoveTo(200);
            tester.PumpAndSettle();
            Assert.Equal(new Point(0, 600), GetHeaderRect().BottomLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(100, GetHeaderRect().Height);
            _ = position.MoveTo(0);
            tester.PumpAndSettle();
            Assert.Equal(new Point(0, 600), GetHeaderRect().BottomLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(300, GetHeaderRect().Height);
        }

        {
            tester.PumpWidget(ResizingFrame(axis: Axis.Horizontal, reverse: true));
            tester.PumpAndSettle();
            ScrollPosition position = tester.State<ScrollableState>().Position;

            Assert.Equal(new Point(800, 0), GetHeaderRect().TopRight);
            Assert.Equal(300, GetHeaderRect().Width);
            Assert.Equal(600, GetHeaderRect().Height);

            double itemWidth = GetItemRect(0).Width;
            int visibleItemCount = (int)(500 / itemWidth); // 500 = viewport width - header width
            Assert.Single(tester.OnstageElementsWithText("item 0"));
            Assert.Single(tester.OnstageElementsWithText($"item {visibleItemCount - 1}"));

            _ = position.MoveTo(200);
            tester.PumpAndSettle();
            Assert.Equal(new Point(800, 0), GetHeaderRect().TopRight);
            Assert.Equal(600, GetHeaderRect().Height);
            Assert.Equal(100, GetHeaderRect().Width);
            _ = position.MoveTo(0);
            tester.PumpAndSettle();
            Assert.Equal(new Point(800, 0), GetHeaderRect().TopRight);
            Assert.Equal(600, GetHeaderRect().Height);
            Assert.Equal(300, GetHeaderRect().Width);
        }
    }

    // Flutter: 'SliverResizingHeader default minExtent is 0'
    [Fact]
    public void SliverResizingHeaderDefaultMinExtentIsZero()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    new SliverResizingHeader(
                        maxExtentPrototype: new SizedBox(height: 300),
                        child: SizedBox.Expand(child: new Text("header"))),
                    SliverList.Builder(itemCount: 100, itemBuilder: (_, index) => new Text($"item {index}")),
                ])));

        Assert.Equal(300, tester.GetSize(Text(tester, "header")).Height);

        ScrollPosition position = tester.State<ScrollableState>().Position;

        _ = position.MoveTo(299);
        tester.PumpAndSettle();
        Assert.Equal(1, tester.GetSize(Text(tester, "header")).Height);

        _ = position.MoveTo(300);
        tester.PumpAndSettle();
        Assert.Empty(tester.OnstageElementsWithText("header"));
    }

    // Flutter: 'SliverResizingHeader with identical min/max prototypes is effectively a pinned header'
    [Fact]
    public void SliverResizingHeaderWithIdenticalPrototypesIsEffectivelyAPinnedHeader()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    new SliverResizingHeader(
                        minExtentPrototype: new SizedBox(height: 100),
                        maxExtentPrototype: new SizedBox(height: 100),
                        child: SizedBox.Expand(child: new Text("header"))),
                    SliverList.Builder(itemCount: 100, itemBuilder: (_, index) => new Text($"item {index}")),
                ])));

        Assert.Equal(default, tester.GetTopLeft(Text(tester, "header")));
        Assert.Equal(new Size(800, 100), tester.GetSize(Text(tester, "header")));

        ScrollPosition position = tester.State<ScrollableState>().Position;

        _ = position.MoveTo(100);
        tester.PumpAndSettle();
        Assert.Equal(default, tester.GetTopLeft(Text(tester, "header")));
        Assert.Equal(new Size(800, 100), tester.GetSize(Text(tester, "header")));

        _ = position.MoveTo(0);
        tester.PumpAndSettle();
        Assert.Equal(default, tester.GetTopLeft(Text(tester, "header")));
        Assert.Equal(new Size(800, 100), tester.GetSize(Text(tester, "header")));
    }

    // Flutter: 'SliverResizingHeader default maxExtent matches the child'
    [Fact]
    public void SliverResizingHeaderDefaultMaxExtentMatchesTheChild()
    {
        using var tester = new FrameworkDartTester();
        Key headerKey = new UniqueKey();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    new SliverResizingHeader(child: new SizedBox(key: headerKey, height: 300)),
                    new SliverList(
                        @delegate: new SliverChildBuilderDelegate(
                            (_, index) => new Text($"item {index}"),
                            childCount: 100)),
                ])));

        Assert.Equal(300, tester.GetSize(tester.OnstageElementsWithKey(headerKey).Single()).Height);

        ScrollPosition position = tester.State<ScrollableState>().Position;

        _ = position.MoveTo(299);
        tester.PumpAndSettle();
        Assert.Equal(1, tester.GetSize(tester.OnstageElementsWithKey(headerKey).Single()).Height);

        _ = position.MoveTo(300);
        tester.PumpAndSettle();
        Assert.Empty(tester.OnstageElementsWithKey(headerKey));
    }

    // Flutter: 'SliverResizingHeader overrides initial out of bounds child size'
    [Fact]
    public void SliverResizingHeaderOverridesInitialOutOfBoundsChildSize()
    {
        using var tester = new FrameworkDartTester();
        Widget BuildFrame(double childHeight)
        {
            return new TestWidgetsApp(
                home: new CustomScrollView(
                    slivers:
                    [
                        new SliverResizingHeader(
                            minExtentPrototype: new SizedBox(height: 100),
                            maxExtentPrototype: new SizedBox(height: 300),
                            child: new SizedBox(height: childHeight, child: new Text("header"))),
                    ]));
        }

        tester.PumpWidget(BuildFrame(50));
        Assert.Equal(100, tester.GetSize(Text(tester, "header")).Height);

        tester.PumpWidget(BuildFrame(350));
        Assert.Equal(300, tester.GetSize(Text(tester, "header")).Height);
    }

    // Flutter: 'SliverResizingHeader update prototypes'
    [Fact]
    public void SliverResizingHeaderUpdatePrototypes()
    {
        using var tester = new FrameworkDartTester();
        Widget BuildFrame(double minHeight, double maxHeight)
        {
            return new TestWidgetsApp(
                home: new CustomScrollView(
                    slivers:
                    [
                        new SliverResizingHeader(
                            minExtentPrototype: new SizedBox(height: minHeight),
                            maxExtentPrototype: new SizedBox(height: maxHeight),
                            child: new SizedBox(height: 300, child: new Text("header"))),
                        new SliverList(
                            @delegate: new SliverChildBuilderDelegate(
                                (_, index) => new SizedBox(height: 50, child: new Text($"{index}")),
                                childCount: 100)),
                    ]));
        }

        double GetHeaderHeight() => tester.GetSize(Text(tester, "header")).Height;

        tester.PumpWidget(BuildFrame(100, 300));
        Assert.Equal(300, GetHeaderHeight());

        tester.Drag(tester.ElementOfType<CustomScrollView>(), new Vector(0, -300));
        tester.PumpAndSettle();
        Assert.Equal(100, GetHeaderHeight());

        tester.Drag(tester.ElementOfType<CustomScrollView>(), new Vector(0, 300));
        tester.PumpAndSettle();
        Assert.Equal(300, GetHeaderHeight());

        tester.PumpWidget(BuildFrame(150, 200));
        Assert.Equal(200, GetHeaderHeight());

        tester.Drag(tester.ElementOfType<CustomScrollView>(), new Vector(0, -100));
        tester.PumpAndSettle();
        Assert.Equal(150, GetHeaderHeight());

        tester.Drag(tester.ElementOfType<CustomScrollView>(), new Vector(0, 100));
        tester.PumpAndSettle();
        Assert.Equal(200, GetHeaderHeight());
    }

    // Flutter: 'SliverResizingHeader maxScrollObstructionExtent'
    [Fact]
    public void SliverResizingHeaderMaxScrollObstructionExtent()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new NestedScrollView(
                headerSliverBuilder: (context, _) =>
                [
                    new SliverOverlapAbsorber(
                        handle: NestedScrollView.SliverOverlapAbsorberHandleFor(context),
                        sliver: new SliverResizingHeader(
                            minExtentPrototype: new SizedBox(height: 100),
                            maxExtentPrototype: new SizedBox(height: 300),
                            child: SizedBox.Expand(child: new Text("header")))),
                ],
                body: new Builder(
                    builder: context => new CustomScrollView(
                        slivers:
                        [
                            new SliverOverlapInjector(
                                handle: NestedScrollView.SliverOverlapAbsorberHandleFor(context)),
                            new SliverList(
                                @delegate: new SliverChildBuilderDelegate(
                                    (_, index) => new SizedBox(height: 50, child: new Text($"{index}")),
                                    childCount: 100)),
                        ])))));

        double GetHeaderHeight() => tester.GetSize(Text(tester, "header")).Height;

        Assert.Equal(300, GetHeaderHeight());

        tester.Drag(tester.ElementOfType<NestedScrollView>(), new Vector(0, -150));
        tester.PumpAndSettle();
        Assert.Equal(150, GetHeaderHeight());

        tester.Drag(tester.ElementOfType<NestedScrollView>(), new Vector(0, -150));
        tester.PumpAndSettle();
        Assert.Equal(100, GetHeaderHeight());
    }

    // Flutter: 'SliverResizingHeader: presence of RenderViewport.excludeFromScrolling tag when pinned'
    [Fact]
    public void SliverResizingHeaderPresenceOfExcludeFromScrollingTagWhenPinned()
    {
        using var tester = new FrameworkDartTester();
        using SemanticsScope semantics = new(tester);

        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    new SliverToBoxAdapter(child: new SizedBox(height: 100, child: new Text("First child"))),
                    new SliverResizingHeader(
                        minExtentPrototype: new SizedBox(height: 300),
                        child: new SizedBox(height: 300, child: new Text("header"))),
                    SliverList.Builder(itemCount: 50, itemBuilder: (_, index) => new Text($"Item {index}")),
                ])));
        semantics.Flush();

        Assert.False(semantics.IncludesNodeWithTags(
            RenderViewport.ExcludeFromScrolling,
            RenderViewport.UseTwoPaneSemantics));
        Assert.Equal(new Rect(0, 100, 800, 300), tester.GetRect(Text(tester, "header")));

        tester.Drag(tester.ElementOfType<CustomScrollView>(), new Vector(0, -100));
        tester.PumpAndSettle();
        semantics.Flush();

        Assert.True(semantics.IncludesNodeWithTags(RenderViewport.UseTwoPaneSemantics));
        Assert.False(semantics.IncludesNodeWithTags(RenderViewport.ExcludeFromScrolling));
        Assert.Equal(new Rect(0, 0, 800, 300), tester.GetRect(Text(tester, "header")));

        tester.Drag(tester.ElementOfType<CustomScrollView>(), new Vector(0, -300));
        tester.PumpAndSettle();
        semantics.Flush();

        SemanticsNode? semanticNode = semantics.NodesWithLabel("header").FirstOrDefault();
        Assert.Equal(
            new HashSet<SemanticsTag> { RenderViewport.ExcludeFromScrolling, RenderViewport.UseTwoPaneSemantics },
            semanticNode?.Parent?.Tags?.ToHashSet());
        Assert.Equal(new Rect(0, 0, 800, 300), tester.GetRect(Text(tester, "header")));
    }

    // ------------------------------------------------------------ sliver_floating_header_test.dart

    private static Widget FloatingFrame(Axis axis, bool reverse)
    {
        return new TestWidgetsApp(
            home: new CustomScrollView(
                scrollDirection: axis,
                reverse: reverse,
                slivers:
                [
                    new SliverFloatingHeader(
                        child: axis switch
                        {
                            Axis.Vertical => new SizedBox(height: 200, child: new Text("header")),
                            _ => new SizedBox(width: 200, child: new Text("header")),
                        }),
                    SliverList.Builder(
                        itemCount: 100,
                        itemBuilder: (_, index) => axis switch
                        {
                            Axis.Vertical => new SizedBox(height: 100, child: new Text($"item {index}")),
                            _ => new SizedBox(width: 100, child: new Text($"item {index}")),
                        }),
                ]));
    }

    // Flutter: 'SliverFloatingHeader basics'
    [Fact]
    public void SliverFloatingHeaderBasics()
    {
        using var tester = new FrameworkDartTester();
        Rect GetHeaderRect() => tester.GetRect(Text(tester, "header"));

        int Scroll(Vector offset)
        {
            tester.TimedDrag(tester.ElementOfType<CustomScrollView>(), offset, TimeSpan.FromMilliseconds(500));
            return tester.PumpAndSettle();
        }

        {
            tester.PumpWidget(FloatingFrame(axis: Axis.Vertical, reverse: false));
            tester.PumpAndSettle();

            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(200, GetHeaderRect().Height);

            const int visibleItemCount = 4; // viewport height - header height = 400
            Assert.Single(tester.OnstageElementsWithText("item 0"));
            Assert.Single(tester.OnstageElementsWithText($"item {visibleItemCount - 1}"));

            Scroll(new Vector(0, -200));
            Assert.Empty(tester.OnstageElementsWithText("header"));

            Scroll(new Vector(0, 25));
            Assert.Equal(new Rect(0, 0, 800, 200), GetHeaderRect());

            Scroll(new Vector(0, 25));
            Assert.Equal(new Rect(0, 0, 800, 200), GetHeaderRect());

            Scroll(new Vector(0, -25));
            Assert.Empty(tester.OnstageElementsWithText("header"));
        }

        {
            tester.PumpWidget(FloatingFrame(axis: Axis.Horizontal, reverse: false));
            tester.PumpAndSettle();

            Assert.Equal(default, GetHeaderRect().TopLeft);
            Assert.Equal(200, GetHeaderRect().Width);
            Assert.Equal(600, GetHeaderRect().Height);

            const int visibleItemCount = 6; // 600 = viewport width - header width
            Assert.Single(tester.OnstageElementsWithText("item 0"));
            Assert.Single(tester.OnstageElementsWithText($"item {visibleItemCount - 1}"));

            Scroll(new Vector(-200, 0));
            Assert.Empty(tester.OnstageElementsWithText("header"));

            Scroll(new Vector(25, 0));
            Assert.Equal(new Rect(0, 0, 200, 600), GetHeaderRect());

            Scroll(new Vector(25, 0));
            Assert.Equal(new Rect(0, 0, 200, 600), GetHeaderRect());

            Scroll(new Vector(-25, 0));
            Assert.Empty(tester.OnstageElementsWithText("header"));
        }

        {
            tester.PumpWidget(FloatingFrame(axis: Axis.Vertical, reverse: true));
            tester.PumpAndSettle();

            Assert.Equal(new Point(0, 400), GetHeaderRect().TopLeft);
            Assert.Equal(800, GetHeaderRect().Width);
            Assert.Equal(200, GetHeaderRect().Height);

            const int visibleItemCount = 4; // viewport height - header height = 400
            Assert.Single(tester.OnstageElementsWithText("item 0"));
            Assert.Single(tester.OnstageElementsWithText($"item {visibleItemCount - 1}"));

            Scroll(new Vector(0, 200));
            Assert.Empty(tester.OnstageElementsWithText("header"));

            Scroll(new Vector(0, -25));
            Assert.Equal(new Rect(0, 400, 800, 200), GetHeaderRect());

            Scroll(new Vector(0, -25));
            Assert.Equal(new Rect(0, 400, 800, 200), GetHeaderRect());

            Scroll(new Vector(0, 25));
            Assert.Empty(tester.OnstageElementsWithText("header"));
        }

        {
            tester.PumpWidget(FloatingFrame(axis: Axis.Horizontal, reverse: true));
            tester.PumpAndSettle();

            Assert.Equal(new Point(600, 0), GetHeaderRect().TopLeft);
            Assert.Equal(200, GetHeaderRect().Width);
            Assert.Equal(600, GetHeaderRect().Height);

            const int visibleItemCount = 6; // 600 = viewport width - header width
            Assert.Single(tester.OnstageElementsWithText("item 0"));
            Assert.Single(tester.OnstageElementsWithText($"item {visibleItemCount - 1}"));

            Scroll(new Vector(200, 0));
            Assert.Empty(tester.OnstageElementsWithText("header"));

            Scroll(new Vector(-25, 0));
            Assert.Equal(new Rect(600, 0, 200, 600), GetHeaderRect());

            Scroll(new Vector(-25, 0));
            Assert.Equal(new Rect(600, 0, 200, 600), GetHeaderRect());

            Scroll(new Vector(25, 0));
            Assert.Empty(tester.OnstageElementsWithText("header"));
        }
    }

    // Flutter: 'SliverFloatingHeader override default AnimationStyle'
    [Fact]
    public void SliverFloatingHeaderOverrideDefaultAnimationStyle()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    new SliverFloatingHeader(
                        animationStyle: new AnimationStyle(
                            Curve: Curves.Linear,
                            ReverseCurve: Curves.Linear,
                            Duration: TimeSpan.FromSeconds(1),
                            ReverseDuration: TimeSpan.FromSeconds(1)),
                        child: new SizedBox(height: 200, child: new Text("header"))),
                    SliverList.Builder(
                        itemCount: 100,
                        itemBuilder: (_, index) => new SizedBox(height: 100, child: new Text($"item {index}"))),
                ])));

        Rect GetHeaderRect() => tester.GetRect(Text(tester, "header"));

        void Scroll(Vector offset) =>
            tester.TimedDrag(tester.ElementOfType<CustomScrollView>(), offset, TimeSpan.FromMilliseconds(500));

        Assert.Equal(default, GetHeaderRect().TopLeft);
        Assert.Equal(800, GetHeaderRect().Width);
        Assert.Equal(200, GetHeaderRect().Height);

        Scroll(new Vector(0, -200));
        tester.PumpAndSettle();
        Assert.Empty(tester.OnstageElementsWithText("header"));

        Scroll(new Vector(0, 25));

        Assert.Equal(new Rect(0, -175, 800, 200), GetHeaderRect());

        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new Rect(0, -175 / 2.0, 800, 200), GetHeaderRect());

        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new Rect(0, 0, 800, 200), GetHeaderRect());
    }

    // Flutter: 'SliverFloatingHeader snapMode parameter'
    [Fact]
    public void SliverFloatingHeaderSnapModeParameter()
    {
        using var tester = new FrameworkDartTester();
        Widget BuildFrame(FloatingHeaderSnapMode snapMode)
        {
            return new TestWidgetsApp(
                home: new CustomScrollView(
                    slivers:
                    [
                        new SliverFloatingHeader(
                            snapMode: snapMode,
                            child: new SizedBox(height: 200, child: new Text("header"))),
                        SliverList.Builder(
                            itemCount: 100,
                            itemBuilder: (_, index) => new SizedBox(height: 100, child: new Text($"item {index}"))),
                    ]));
        }

        Rect GetHeaderRect() => tester.GetRect(Text(tester, "header"));
        double GetItem0Y() => tester.GetRect(Text(tester, "item 0")).TopLeft.Y;

        void Scroll(Vector offset) =>
            tester.TimedDrag(tester.ElementOfType<CustomScrollView>(), offset, TimeSpan.FromMilliseconds(500));

        {
            tester.PumpWidget(BuildFrame(FloatingHeaderSnapMode.Overlay));
            tester.PumpAndSettle();
            Assert.Equal(new Rect(0, 0, 800, 200), GetHeaderRect());
            Assert.Equal(200, GetItem0Y());

            Scroll(new Vector(0, -200));
            tester.PumpAndSettle();
            Assert.Empty(tester.OnstageElementsWithText("header"));
            double item0StartY = GetItem0Y();
            Assert.True(item0StartY < 0, $"{item0StartY}");

            Scroll(new Vector(0, 25));
            tester.PumpAndSettle();

            // The snap animation overlaps the scrollable's content.
            Assert.Equal(item0StartY + 25, GetItem0Y());

            Scroll(new Vector(0, 200));
            tester.PumpAndSettle();
            Assert.Equal(new Rect(0, 0, 800, 200), GetHeaderRect());
            Assert.Equal(200, GetItem0Y());
        }

        {
            tester.PumpWidget(BuildFrame(FloatingHeaderSnapMode.Scroll));
            tester.PumpAndSettle();
            Assert.Equal(new Rect(0, 0, 800, 200), GetHeaderRect());
            Assert.Equal(200, GetItem0Y());

            Scroll(new Vector(0, -200));
            tester.PumpAndSettle();
            Assert.Empty(tester.OnstageElementsWithText("header"));
            double item0StartY = GetItem0Y();
            Assert.True(item0StartY < 0, $"{item0StartY}");

            Scroll(new Vector(0, 25));
            tester.PumpAndSettle();

            // The snap animation scrolls the scrollable's content.
            Assert.Equal(item0StartY + 200 + 25, GetItem0Y());
        }
    }

    // -------------------------------------------------------------------------------- helpers

    private static Element Text(FrameworkDartTester tester, string text) =>
        tester.OnstageElementsWithText(text).Single();

    /// <summary>semantics_tester.dart's <c>SemanticsTester</c>, reduced to what these tests use.</summary>
    private sealed class SemanticsScope : IDisposable
    {
        private readonly FrameworkDartTester _tester;
        private readonly SemanticsHandle _handle;

        public SemanticsScope(FrameworkDartTester tester)
        {
            _tester = tester;
            PipelineOwner owner = tester.RenderView.Owner!;
            bool createsOwner = owner.SemanticsOwner is null;
            _handle = owner.EnsureSemantics();
            if (createsOwner)
            {
                // flutter_test's reusable view clears its semantics cache when semantics turn on.
                tester.RenderView.ClearSemantics();
                tester.RenderView.ScheduleInitialSemantics();
            }
        }

        // flutter_test's frame runs `flushSemantics` after compositing; the C# tester's frame stops
        // at composite.
        public void Flush() => _tester.RenderView.Owner!.FlushSemantics();

        private IEnumerable<SemanticsNode> AllNodes()
        {
            var nodes = new List<SemanticsNode>();
            void Visit(SemanticsNode node)
            {
                nodes.Add(node);
                foreach (SemanticsNode child in node.Children)
                {
                    Visit(child);
                }
            }

            Visit(_tester.RenderView.Owner!.SemanticsOwner!.RootNode!);
            return nodes;
        }

        public List<SemanticsNode> NodesWithLabel(string label) =>
            AllNodes().Where(node => node.Label == label).ToList();

        /// <summary>Dart's <c>includesNodeWith(tags: ...)</c>: <c>setEquals(data.tags, tags)</c>.</summary>
        public bool IncludesNodeWithTags(params SemanticsTag[] tags)
        {
            var expected = new HashSet<SemanticsTag>(tags);
            return AllNodes().Any(node =>
                node.GetSemanticsData().Tags is { } actual && expected.SetEquals(actual));
        }

        public void Dispose() => _handle.Dispose();
    }
}
