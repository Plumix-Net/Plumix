using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources (reference):
// - flutter/packages/flutter/lib/src/widgets/page_storage.dart
// - flutter/packages/flutter/lib/src/widgets/shared_app_data.dart
// - flutter/packages/flutter/lib/src/widgets/scroll_position.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class StateStorageWidgetsTests
{
    [Fact]
    public void PageStorageBucket_UsesCompletePageStorageKeyChain()
    {
        var bucket = new PageStorageBucket();
        object? restored = null;

        using var harness = new WidgetRenderHarness(
            BuildStorageProbe(bucket, outerKey: "outer-a", innerKey: "inner", context =>
            {
                bucket.WriteState(context, 42);
            }));
        harness.Pump(new Size(40, 40));

        harness.Update(BuildStorageProbe(bucket, outerKey: "outer-a", innerKey: "inner", context =>
        {
            restored = bucket.ReadState(context);
        }));
        Assert.Equal(42, restored);

        harness.Update(BuildStorageProbe(bucket, outerKey: "outer-b", innerKey: "inner", context =>
        {
            restored = bucket.ReadState(context);
        }));
        Assert.Null(restored);
    }

    [Fact]
    public void PageStorageBucket_ExplicitIdentifier_DoesNotRequirePageStorageKey()
    {
        var bucket = new PageStorageBucket();
        object? restored = null;

        using var harness = new WidgetRenderHarness(
            new PageStorage(
                bucket,
                new ContextProbe(context =>
                {
                    bucket.WriteState(context, "saved", identifier: "explicit");
                    restored = bucket.ReadState(context, identifier: "explicit");
                })));
        harness.Pump(new Size(40, 40));

        Assert.Equal("saved", restored);
    }

    [Fact]
    public void PageStorage_OfAndMaybeOf_ResolveNearestBucket()
    {
        var outer = new PageStorageBucket();
        var inner = new PageStorageBucket();
        PageStorageBucket? resolved = null;

        using var harness = new WidgetRenderHarness(
            new PageStorage(
                outer,
                new PageStorage(
                    inner,
                    new ContextProbe(context => resolved = PageStorage.Of(context)))));
        harness.Pump(new Size(40, 40));

        Assert.Same(inner, resolved);
    }

    [Fact]
    public void ScrollController_RestoresOffsetAfterScrollableIsRecreated()
    {
        var bucket = new PageStorageBucket();
        var firstController = new ScrollController(initialScrollOffset: 12);
        using var harness = new WidgetRenderHarness(BuildScrollable(bucket, firstController));
        harness.Pump(new Size(100, 100));

        firstController.JumpTo(180);
        harness.Pump(new Size(100, 100));
        Assert.Equal(180, firstController.Offset);

        harness.Update(new PageStorage(bucket, new SizedBox(width: 100, height: 100)));
        harness.Pump(new Size(100, 100));

        var secondController = new ScrollController(initialScrollOffset: 24);
        harness.Update(BuildScrollable(bucket, secondController));
        harness.Pump(new Size(100, 100));

        Assert.Equal(180, secondController.Offset);
    }

    [Fact]
    public void ScrollController_KeepScrollOffsetFalse_DoesNotWriteOrRestorePageStorage()
    {
        var bucket = new PageStorageBucket();
        var firstController = new ScrollController(keepScrollOffset: false);
        using var harness = new WidgetRenderHarness(BuildScrollable(bucket, firstController));
        harness.Pump(new Size(100, 100));

        firstController.JumpTo(180);
        harness.Pump(new Size(100, 100));
        harness.Update(new PageStorage(bucket, new SizedBox(width: 100, height: 100)));
        harness.Pump(new Size(100, 100));

        var secondController = new ScrollController(initialScrollOffset: 24);
        harness.Update(BuildScrollable(bucket, secondController));
        harness.Pump(new Size(100, 100));

        Assert.Equal(24, secondController.Offset);
    }

    [Fact]
    public void SharedAppData_LazilyInitializesValueOnce()
    {
        int initCount = 0;
        int? observed = null;
        var reader = new SharedValueReader(
            "counter",
            () =>
            {
                initCount += 1;
                return 7;
            },
            value => observed = value);

        using var harness = new WidgetRenderHarness(new SharedAppData(reader));
        harness.Pump(new Size(40, 40));
        harness.Update(new SharedAppData(reader));
        harness.Pump(new Size(40, 40));

        Assert.Equal(1, initCount);
        Assert.Equal(7, observed);
    }

    [Fact]
    public void SharedAppData_RebuildsOnlyDependentsOfChangedKey()
    {
        BuildContext? context = null;
        int keyABuilds = 0;
        int keyBBuilds = 0;
        int observedA = 0;
        int observedB = 0;
        var child = new Row(
            children:
            [
                new SharedValueReader(
                    "a",
                    () => 1,
                    value =>
                    {
                        keyABuilds += 1;
                        observedA = value;
                    },
                    captured => context = captured),
                new SharedValueReader(
                    "b",
                    () => 10,
                    value =>
                    {
                        keyBBuilds += 1;
                        observedB = value;
                    }),
            ]);

        using var harness = new WidgetRenderHarness(new SharedAppData(child));
        harness.Pump(new Size(80, 40));
        Assert.Equal((1, 1), (keyABuilds, keyBBuilds));

        SharedAppData.SetValue(context!.Value, "a", 2);
        harness.Pump(new Size(80, 40));
        Assert.Equal((2, 1), (keyABuilds, keyBBuilds));
        Assert.Equal((2, 10), (observedA, observedB));

        SharedAppData.SetValue(context.Value, "a", 2);
        harness.Pump(new Size(80, 40));
        Assert.Equal((2, 1), (keyABuilds, keyBBuilds));

        SharedAppData.SetValue(context.Value, "b", 20);
        harness.Pump(new Size(80, 40));
        Assert.Equal((2, 2), (keyABuilds, keyBBuilds));
        Assert.Equal((2, 20), (observedA, observedB));
    }

    [Fact]
    public void SharedAppData_GetValueWithoutAncestor_Throws()
    {
        Exception? exception = null;
        using var harness = new WidgetRenderHarness(
            new ContextProbe(context =>
            {
                exception = Record.Exception(() => SharedAppData.GetValue(context, "key", () => 1));
            }));
        harness.Pump(new Size(40, 40));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("SharedAppData.GetValue", invalidOperation.Message);
    }

    private static Widget BuildStorageProbe(
        PageStorageBucket bucket,
        string outerKey,
        string innerKey,
        Action<BuildContext> callback)
    {
        return new PageStorage(
            bucket,
            new KeyedSubtree(
                key: new PageStorageKey<string>(outerKey),
                child: new ContextProbe(callback, new PageStorageKey<string>(innerKey))));
    }

    private static Widget BuildScrollable(PageStorageBucket bucket, ScrollController controller)
    {
        return new PageStorage(
            bucket,
            new SingleChildScrollView(
                key: new PageStorageKey<string>("restorable-scroll"),
                controller: controller,
                child: new SizedBox(width: 100, height: 500)));
    }

    private sealed class ContextProbe : StatelessWidget
    {
        private readonly Action<BuildContext> _callback;

        public ContextProbe(Action<BuildContext> callback, Key? key = null) : base(key)
        {
            _callback = callback;
        }

        public override Widget Build(BuildContext context)
        {
            _callback(context);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class SharedValueReader : StatelessWidget
    {
        private readonly string _key;
        private readonly Func<int> _init;
        private readonly Action<int> _onValue;
        private readonly Action<BuildContext>? _onContext;

        public SharedValueReader(
            string key,
            Func<int> init,
            Action<int> onValue,
            Action<BuildContext>? onContext = null)
        {
            _key = key;
            _init = init;
            _onValue = onValue;
            _onContext = onContext;
        }

        public override Widget Build(BuildContext context)
        {
            _onContext?.Invoke(context);
            int value = SharedAppData.GetValue(context, _key, _init);
            _onValue(value);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            var renderView = new RenderView();
            _pipeline = new PipelineOwner(renderView);
            _pipeline.Attach(renderView);
            _root = new HarnessRootElement(
                renderView,
                new Directionality(TextDirection.Ltr, child: widget));
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public void Update(Widget widget)
        {
            _root.UpdateWidget(new Directionality(TextDirection.Ltr, child: widget));
            _owner.FlushBuild();
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose()
        {
            _root.Unmount();
            Scheduler.PumpFrameForTests();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

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

            public void UpdateWidget(Widget widget)
            {
                Update(widget);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(child, _child))
                {
                    _child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child != null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = child as RenderBox
                                    ?? throw new InvalidOperationException("Root child must be a RenderBox.");
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }
        }
    }
}
