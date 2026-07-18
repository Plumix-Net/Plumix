using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/animated_scroll_view.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class AnimatedListTests : IDisposable
{
    public AnimatedListTests()
    {
        Scheduler.ResetForTests();
        KeyedProbeState.Reset();
    }

    public void Dispose()
    {
        KeyedProbeState.Reset();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void AnimatedLists_ExposeFlutterDefaultsAndValidateContracts()
    {
        AnimatedItemBuilder builder = (_, _, _) => new SizedBox();
        var list = new AnimatedList(builder);
        var sliver = new SliverAnimatedList(builder);

        Assert.Same(builder, list.ItemBuilder);
        Assert.Equal(0, list.InitialItemCount);
        Assert.Equal(Axis.Vertical, list.ScrollDirection);
        Assert.False(list.Reverse);
        Assert.Null(list.Controller);
        Assert.Null(list.Primary);
        Assert.Null(list.Physics);
        Assert.False(list.ShrinkWrap);
        Assert.Null(list.Padding);
        Assert.Equal(Clip.HardEdge, list.ClipBehavior);
        Assert.Null(list.ScrollCacheExtent);

        Assert.Same(builder, sliver.ItemBuilder);
        Assert.Null(sliver.FindChildIndexCallback);
        Assert.Equal(0, sliver.InitialItemCount);

        Assert.Throws<ArgumentNullException>(() => new AnimatedList(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedList(builder, initialItemCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScrollCacheExtent.Pixels(-1));
        Assert.Equal(CacheExtentStyle.Viewport, ScrollCacheExtent.Viewport(1.5).Style);
        Assert.Throws<ArgumentNullException>(() => new SliverAnimatedList(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverAnimatedList(builder, initialItemCount: -1));
        Assert.Throws<ArgumentNullException>(() => AnimatedList.Separated(
            builder,
            separatorBuilder: null!,
            removedSeparatorBuilder: builder));
    }

    [Fact]
    public void SliverAnimatedList_InsertAndRemoveUseFlutterLogicalIndicesAndAnimations()
    {
        var key = new LabeledGlobalKey<SliverAnimatedListState>("sliver-animated-list");
        var itemBuilds = new List<(int Index, double Value, AnimationStatus Status)>();
        var removedBuilds = new List<(double Value, AnimationStatus Status)>();
        Widget list = new CustomScrollView(
            slivers:
            [
                new SliverAnimatedList(
                    itemBuilder: (_, index, animation) =>
                    {
                        itemBuilds.Add((index, animation.Value, animation.Status));
                        return new SizedBox(height: 30);
                    },
                    initialItemCount: 3,
                    key: key),
            ]);
        using WidgetRenderHarness harness = new(list);
        harness.Pump(new Size(200, 160));

        Assert.Equal(3, key.CurrentState!.ItemsCount);
        Assert.Contains(itemBuilds, build =>
            build.Index == 0 && build.Value == 1.0 && build.Status == AnimationStatus.Completed);
        Assert.Contains(itemBuilds, build =>
            build.Index == 2 && build.Value == 1.0 && build.Status == AnimationStatus.Completed);

        itemBuilds.Clear();
        key.CurrentState.InsertItem(1);
        harness.Pump(new Size(200, 160));

        Assert.Equal(4, key.CurrentState.ItemsCount);
        Assert.Contains(itemBuilds, build =>
            build.Index == 1 && build.Value == 0.0 && build.Status == AnimationStatus.Forward);
        Assert.Contains(itemBuilds, build => build.Index == 2 && build.Value == 1.0);

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.16));
        harness.Pump(new Size(200, 160));
        Assert.Contains(itemBuilds, build => build.Index == 1 && build.Value is > 0.0 and < 1.0);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.40));
        harness.Pump(new Size(200, 160));

        key.CurrentState.RemoveItem(
            1,
            (_, animation) =>
            {
                removedBuilds.Add((animation.Value, animation.Status));
                return new SizedBox(height: 30);
            });
        itemBuilds.Clear();
        harness.Pump(new Size(200, 160));

        Assert.Equal(4, key.CurrentState.ItemsCount);
        Assert.Contains(removedBuilds, build =>
            build.Value == 1.0 && build.Status == AnimationStatus.Reverse);
        Assert.Contains(itemBuilds, build => build.Index == 1);
        Assert.Contains(itemBuilds, build => build.Index == 2);

        double removeStart = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(removeStart + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(removeStart + 0.16));
        harness.Pump(new Size(200, 160));
        Assert.Contains(removedBuilds, build => build.Value is > 0.0 and < 1.0);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(removeStart + 0.35));
        harness.Pump(new Size(200, 160));

        Assert.Equal(3, key.CurrentState.ItemsCount);
    }

    [Fact]
    public void AnimatedList_SeparatedCoordinatesItemsAndCorrespondingSeparators()
    {
        var key = new LabeledGlobalKey<AnimatedListState>("animated-separated-list");
        var builtItems = new List<int>();
        var builtSeparators = new List<int>();
        var removedSeparators = new List<int>();
        var removedItems = new List<double>();
        Widget list = AnimatedList.Separated(
            itemBuilder: (_, index, _) =>
            {
                builtItems.Add(index);
                return new SizedBox(height: 24);
            },
            separatorBuilder: (_, index, _) =>
            {
                builtSeparators.Add(index);
                return new SizedBox(height: 4);
            },
            removedSeparatorBuilder: (_, index, _) =>
            {
                removedSeparators.Add(index);
                return new SizedBox(height: 4);
            },
            initialItemCount: 3,
            key: key);
        using WidgetRenderHarness harness = new(new Directionality(
            Plumix.UI.TextDirection.Ltr,
            list));
        harness.Pump(new Size(200, 160));

        Assert.Equal([0, 1, 2], builtItems.Distinct().Order().ToArray());
        Assert.Equal([0, 1], builtSeparators.Distinct().Order().ToArray());

        key.CurrentState!.RemoveItem(
            2,
            (_, animation) =>
            {
                removedItems.Add(animation.Value);
                return new SizedBox(height: 24);
            });
        harness.Pump(new Size(200, 160));

        Assert.Contains(1, removedSeparators);
        Assert.Contains(1.0, removedItems);

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        harness.Pump(new Size(200, 160));

        builtItems.Clear();
        builtSeparators.Clear();
        key.CurrentState.InsertItem(2);
        harness.Pump(new Size(200, 160));

        Assert.Contains(2, builtItems);
        Assert.Contains(1, builtSeparators);
    }

    [Fact]
    public void SliverAnimatedList_BulkOperationsAnimateEveryItemAndClearAfterReverseCompletes()
    {
        var key = new LabeledGlobalKey<SliverAnimatedListState>("bulk-sliver-animated-list");
        var removedValues = new List<double>();
        Widget list = new CustomScrollView(
            slivers:
            [
                new SliverAnimatedList(
                    itemBuilder: (_, _, _) => new SizedBox(height: 30),
                    initialItemCount: 1,
                    key: key),
            ]);
        using WidgetRenderHarness harness = new(list);
        harness.Pump(new Size(200, 160));

        key.CurrentState!.InsertAllItems(1, 2);
        harness.Pump(new Size(200, 160));
        Assert.Equal(3, key.CurrentState.ItemsCount);

        double insertStart = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(insertStart + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(insertStart + 0.35));
        harness.Pump(new Size(200, 160));

        key.CurrentState.RemoveAllItems(
            (_, animation) =>
            {
                removedValues.Add(animation.Value);
                return new SizedBox(height: 30);
            });
        harness.Pump(new Size(200, 160));

        Assert.Equal(3, key.CurrentState.ItemsCount);
        Assert.Equal(3, removedValues.Count(value => value == 1.0));

        double removeStart = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(removeStart + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(removeStart + 0.35));
        harness.Pump(new Size(200, 160));

        Assert.Equal(0, key.CurrentState.ItemsCount);
    }

    [Fact]
    public void AnimatedList_OfMaybeOfPaddingAndClipMatchSourceComposition()
    {
        var key = new LabeledGlobalKey<AnimatedListState>("animated-list");
        BuildContext? itemContext = null;
        Widget list = new MediaQuery(
            data: new MediaQueryData(Padding: new Thickness(10, 20, 30, 40)),
            child: new AnimatedList(
                itemBuilder: (context, _, _) =>
                {
                    itemContext = context;
                    return new SizedBox(height: 30);
                },
                initialItemCount: 1,
                clipBehavior: Clip.None,
                key: key));
        using WidgetRenderHarness harness = new(list);
        harness.Pump(new Size(200, 120));

        Assert.NotNull(itemContext);
        Assert.Same(key.CurrentState, AnimatedList.Of(itemContext!.Value));
        Assert.Same(key.CurrentState, AnimatedList.MaybeOf(itemContext.Value));
        Assert.NotNull(SliverAnimatedList.MaybeOf(itemContext.Value));

        RenderViewport viewport = Assert.Single(FindDescendants<RenderViewport>(harness.RenderView));
        Assert.Equal(Clip.None, viewport.ClipBehavior);
        RenderSliverPadding padding = Assert.Single(FindDescendants<RenderSliverPadding>(harness.RenderView));
        Assert.Equal(new Thickness(0, 20, 0, 40), padding.Padding);
    }

    [Fact]
    public void SliverAnimatedList_FindChildIndexPreservesKeyedStateAcrossInsertions()
    {
        var ids = new List<int> { 0, 1, 2 };
        var key = new LabeledGlobalKey<SliverAnimatedListState>("keyed-sliver-animated-list");
        Widget list = new CustomScrollView(
            slivers:
            [
                new SliverAnimatedList(
                    itemBuilder: (_, index, _) => new KeyedProbe(
                        ids[index],
                        new ValueKey<int>(ids[index])),
                    findChildIndexCallback: childKey => childKey is ValueKey<int> valueKey
                        ? ids.IndexOf(valueKey.Value)
                        : null,
                    initialItemCount: ids.Count,
                    key: key),
            ]);
        using WidgetRenderHarness harness = new(list);
        harness.Pump(new Size(200, 160));
        Dictionary<int, Guid> initialStates = KeyedProbeState.StateIds.ToDictionary();

        ids.Insert(1, 9);
        key.CurrentState!.InsertItem(1);
        harness.Pump(new Size(200, 160));

        Assert.Equal(initialStates[0], KeyedProbeState.StateIds[0]);
        Assert.Equal(initialStates[1], KeyedProbeState.StateIds[1]);
        Assert.Equal(initialStates[2], KeyedProbeState.StateIds[2]);
        Assert.Contains(9, KeyedProbeState.StateIds.Keys);
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

    private sealed class KeyedProbe : StatefulWidget
    {
        public KeyedProbe(int id, Key key) : base(key)
        {
            Id = id;
        }

        public int Id { get; }

        public override State CreateState() => new KeyedProbeState();
    }

    private sealed class KeyedProbeState : State
    {
        public static readonly Dictionary<int, Guid> StateIds = [];

        private readonly Guid _stateId = Guid.NewGuid();

        private KeyedProbe CurrentWidget => (KeyedProbe)StateWidget;

        public static void Reset() => StateIds.Clear();

        public override Widget Build(BuildContext context)
        {
            StateIds[CurrentWidget.Id] = _stateId;
            return new SizedBox(height: 30);
        }

        public override void Dispose()
        {
            if (StateIds.TryGetValue(CurrentWidget.Id, out Guid id) && id == _stateId)
            {
                StateIds.Remove(CurrentWidget.Id);
            }
        }
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
