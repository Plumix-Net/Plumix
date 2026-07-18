using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/reorderable_list.dart
// flutter/packages/flutter/lib/src/material/reorderable_list.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialReorderableListTests
{
    [Fact]
    public void ReorderableLists_ValidateCallbacksKeysAndExtentContracts()
    {
        IndexedWidgetBuilder builder = (_, index) => new SizedBox(key: new ValueKey<int>(index));
        ReorderCallback callback = (_, _) => { };

        Assert.Throws<ArgumentException>(() => new ReorderableList(builder, 1));
        Assert.Throws<ArgumentException>(() => new ReorderableList(
            builder,
            1,
            onReorder: callback,
            onReorderItem: callback));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReorderableList(
            builder,
            -1,
            onReorderItem: callback));
        Assert.Throws<ArgumentException>(() => new ReorderableList(
            builder,
            1,
            onReorderItem: callback,
            itemExtent: 40,
            itemExtentBuilder: (_, _) => 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReorderableList(
            builder,
            1,
            onReorderItem: callback,
            autoScrollerVelocityScalar: 0));
        Assert.Throws<ArgumentException>(() => new ReorderableListView(
            [new SizedBox()],
            onReorderItem: callback));

        ReorderableListView list = ReorderableListView.Builder(
            builder,
            3,
            onReorderItem: callback);
        Assert.Equal(Axis.Vertical, list.ScrollDirection);
        Assert.True(list.BuildDefaultDragHandles);
        Assert.False(list.Reverse);
        Assert.False(list.ShrinkWrap);
        Assert.Equal(50, list.AutoScrollerVelocityScalar);
    }

    [Fact]
    public void CoreReorderableList_ImmediateDragReportsAdjustedItemIndexAndLifecycle()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<int> starts = [];
        List<int> ends = [];
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
            Widget list = new ReorderableList(
                itemBuilder: (_, index) => new ReorderableDragStartListener(
                    child: new SizedBox(height: 50),
                    index: index,
                    key: new ValueKey<int>(index)),
                itemCount: 3,
                onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                onReorderStart: starts.Add,
                onReorderEnd: ends.Add,
                itemExtent: 50);
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(200, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 71, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 71, new Point(100, 115), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 71, new Point(100, 115), start.AddMilliseconds(200));

            Assert.Equal([0], starts);
            Assert.Equal([3], ends);
            Assert.Equal([(0, 2)], reorders);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void CoreReorderableList_DeprecatedCallbackRetainsInsertionIndex()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
#pragma warning disable CS0618
            Widget list = new ReorderableList(
                itemBuilder: (_, index) => new ReorderableDragStartListener(
                    child: new SizedBox(height: 50),
                    index: index,
                    key: new ValueKey<int>(index)),
                itemCount: 3,
                onReorder: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                itemExtent: 50);
#pragma warning restore CS0618
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(200, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 72, new Point(100, 25), start);
            DispatchMove(binding, harness.RenderView, 72, new Point(100, 115), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 72, new Point(100, 115), start.AddMilliseconds(200));

            Assert.Equal([(0, 3)], reorders);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void CoreReorderableList_HorizontalDragUsesMainAxisGeometry()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
            Widget list = new ReorderableList(
                itemBuilder: (_, index) => new ReorderableDragStartListener(
                    child: new SizedBox(width: 50),
                    index: index,
                    key: new ValueKey<int>(index)),
                itemCount: 3,
                onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                itemExtent: 50,
                scrollDirection: Axis.Horizontal);
            using WidgetRenderHarness harness = new(Wrap(list));
            harness.Pump(new Size(150, 100));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 74, new Point(25, 50), start);
            DispatchMove(binding, harness.RenderView, 74, new Point(115, 50), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 74, new Point(115, 50), start.AddMilliseconds(200));

            Assert.Equal([(0, 2)], reorders);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ReorderableListView_BuildsDesktopHandlesAndMobileLongPressItems()
    {
        IReadOnlyList<Widget> children =
        [
            new SizedBox(height: 48, key: new ValueKey<int>(0)),
            new SizedBox(height: 48, key: new ValueKey<int>(1)),
            new SizedBox(height: 48, key: new ValueKey<int>(2)),
        ];

        ThemeData desktopTheme = ThemeData.Light with { Platform = TargetPlatform.Windows };
        using WidgetRenderHarness desktop = new(Wrap(
            new ReorderableListView(children, onReorderItem: (_, _) => { }, itemExtent: 48),
            desktopTheme));
        desktop.Pump(new Size(240, 144));

        string dragGlyph = char.ConvertFromUtf32(Icons.DragHandle.CodePoint);
        Assert.Equal(3, FindDescendants<RenderParagraph>(desktop.RenderView)
            .Count(paragraph => paragraph.Text == dragGlyph));

        ThemeData mobileTheme = ThemeData.Light with { Platform = TargetPlatform.Android };
        using WidgetRenderHarness mobile = new(Wrap(
            new ReorderableListView(children, onReorderItem: (_, _) => { }, itemExtent: 48),
            mobileTheme));
        mobile.Pump(new Size(240, 144));

        Assert.DoesNotContain(
            FindDescendants<RenderParagraph>(mobile.RenderView),
            paragraph => paragraph.Text == dragGlyph);
    }

    [Fact]
    public void ReorderableListView_DesktopHandleOwnsDragAndReportsAdjustedIndex()
    {
        Scheduler.ResetForTests();
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        List<(int OldIndex, int NewIndex)> reorders = [];

        try
        {
            IReadOnlyList<Widget> children =
            [
                new SizedBox(height: 50, key: new ValueKey<int>(0)),
                new SizedBox(height: 50, key: new ValueKey<int>(1)),
                new SizedBox(height: 50, key: new ValueKey<int>(2)),
            ];
            ThemeData theme = ThemeData.Light with { Platform = TargetPlatform.Windows };
            using WidgetRenderHarness harness = new(Wrap(
                new ReorderableListView(
                    children,
                    onReorderItem: (oldIndex, newIndex) => reorders.Add((oldIndex, newIndex)),
                    itemExtent: 50),
                theme));
            harness.Pump(new Size(240, 150));

            DateTime start = DateTime.UtcNow;
            DispatchDown(binding, harness.RenderView, 73, new Point(220, 25), start);
            DispatchMove(binding, harness.RenderView, 73, new Point(220, 115), start.AddMilliseconds(100));
            DispatchUp(binding, harness.RenderView, 73, new Point(220, 115), start.AddMilliseconds(200));

            Assert.Equal([(0, 2)], reorders);
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void ReorderableListView_HeaderFooterSplitPaddingLikeFlutter()
    {
        Widget list = new ReorderableListView(
            [new SizedBox(height: 40, key: new ValueKey<int>(0))],
            onReorderItem: (_, _) => { },
            buildDefaultDragHandles: false,
            padding: new Thickness(10),
            header: new SizedBox(height: 20),
            footer: new SizedBox(height: 30),
            itemExtent: 40);
        using WidgetRenderHarness harness = new(Wrap(list));
        harness.Pump(new Size(200, 120));

        List<RenderSliverPadding> paddings = FindDescendants<RenderSliverPadding>(harness.RenderView);
        Assert.Equal(3, paddings.Count);
        Assert.Contains(paddings, value => value.Padding == new Thickness(10, 0, 10, 10));
        Assert.Contains(paddings, value => value.Padding == new Thickness(10, 0, 10, 0));
        Assert.Contains(paddings, value => value.Padding == new Thickness(10, 10, 10, 0));
    }

    [Fact]
    public void SliverReorderableList_ItemExtentBuilderControlsEachChildExtent()
    {
        Widget list = new CustomScrollView(
            slivers:
            [
                new SliverReorderableList(
                    itemBuilder: (_, index) => new SizedBox(key: new ValueKey<int>(index)),
                    itemCount: 3,
                    onReorderItem: (_, _) => { },
                    itemExtentBuilder: (index, dimensions) => 30 + (index * 20)),
            ]);
        using WidgetRenderHarness harness = new(Wrap(list));
        harness.Pump(new Size(200, 150));

        RenderSliverVariableExtentList sliver = Assert.Single(
            FindDescendants<RenderSliverVariableExtentList>(harness.RenderView));
        List<double> heights = [];
        for (RenderBox? child = sliver.FirstChild; child is not null; child = sliver.ChildAfter(child))
        {
            heights.Add(child.Size.Height);
        }

        Assert.Equal([30, 50, 70], heights);
        Assert.Equal(150, sliver.Geometry.ScrollExtent);
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(new Size(360, 640)),
                new Theme(theme ?? ThemeData.Light, child)));
    }

    private static void DispatchDown(
        GestureBinding binding,
        RenderView view,
        int pointer,
        Point position,
        DateTime timestamp)
    {
        binding.HandlePointerEvent(
            view,
            new PointerDownEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, timestamp));
    }

    private static void DispatchMove(
        GestureBinding binding,
        RenderView view,
        int pointer,
        Point position,
        DateTime timestamp)
    {
        binding.HandlePointerEvent(
            view,
            new PointerMoveEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                down: true,
                timestamp));
    }

    private static void DispatchUp(
        GestureBinding binding,
        RenderView view,
        int pointer,
        Point position,
        DateTime timestamp)
    {
        binding.HandlePointerEvent(
            view,
            new PointerUpEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, timestamp));
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        List<T> result = [];
        if (root is null)
        {
            return result;
        }

        if (root is T value)
        {
            result.Add(value);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _root.Unmount();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public RootElement(RenderView view, Widget widget) : base(widget)
            {
                _view = view;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _view.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_view.Child, child))
                {
                    _view.Child = null;
                }
            }
        }
    }
}
