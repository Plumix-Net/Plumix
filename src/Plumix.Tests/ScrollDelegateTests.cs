using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ported from flutter/packages/flutter/test/widgets/slivers_test.dart, sliver_list_test.dart,
// list_view_test.dart, scroll_view_test.dart and scrollable_selection_test.dart — the behaviours
// those files assert about `widgets/scroll_delegate.dart` and `SliverList.separated`.

namespace Plumix.Tests;

public sealed class ScrollDelegateTests
{
    /// <remarks>
    /// Dart's wrapper chain, outermost first:
    /// `KeyedSubtree > AutomaticKeepAlive > _SelectionKeepAlive > IndexedSemantics > RepaintBoundary`.
    /// </remarks>
    [Fact]
    public void BuilderDelegate_WrapsEveryChildInDartsOrder()
    {
        var childDelegate = new SliverChildBuilderDelegate(
            (_, index) => new SizedBox(height: 10, key: new ValueKey<int>(index)),
            childCount: 1);

        var keyed = Assert.IsType<KeyedSubtree>(childDelegate.Build(default, 0));
        Assert.Equal(new SliverChildKey(new ValueKey<int>(0)), keyed.Key);
        var keepAlive = Assert.IsType<AutomaticKeepAlive>(keyed.Child);
        var selectionKeepAlive = Assert.IsType<SelectionKeepAlive>(keepAlive.Child);
        var indexed = Assert.IsType<IndexedSemantics>(selectionKeepAlive.Child);
        Assert.Equal(0, indexed.Index);
        var boundary = Assert.IsType<RepaintBoundary>(indexed.Child);
        Assert.IsType<SizedBox>(boundary.Child);
    }

    /// <inheritdoc cref="BuilderDelegate_WrapsEveryChildInDartsOrder"/>
    [Fact]
    public void ListDelegate_WrapsEveryChildInDartsOrder()
    {
        var childDelegate = new SliverChildListDelegate([new SizedBox(height: 10)]);

        var keyed = Assert.IsType<KeyedSubtree>(childDelegate.Build(default, 0));
        Assert.Null(keyed.Key);
        var keepAlive = Assert.IsType<AutomaticKeepAlive>(keyed.Child);
        var selectionKeepAlive = Assert.IsType<SelectionKeepAlive>(keepAlive.Child);
        var indexed = Assert.IsType<IndexedSemantics>(selectionKeepAlive.Child);
        var boundary = Assert.IsType<RepaintBoundary>(indexed.Child);
        Assert.IsType<SizedBox>(boundary.Child);
    }

    /// <remarks>
    /// Flutter's `slivers_test.dart` "Can override ErrorWidget.build" builds the delegate with all
    /// three flags off and still casts the result to `KeyedSubtree`: Dart's `KeyedSubtree` wrapper is
    /// unconditional, even when the child has no key.
    /// </remarks>
    [Fact]
    public void EveryChildIsKeyedSubtreeWrappedEvenWithAllFlagsOff()
    {
        var builderDelegate = new SliverChildBuilderDelegate(
            (_, _) => new SizedBox(height: 10),
            childCount: 1,
            addAutomaticKeepAlives: false,
            addRepaintBoundaries: false,
            addSemanticIndexes: false);
        var listDelegate = new SliverChildListDelegate(
            [new SizedBox(height: 10)],
            addAutomaticKeepAlives: false,
            addRepaintBoundaries: false,
            addSemanticIndexes: false);

        var fromBuilder = Assert.IsType<KeyedSubtree>(builderDelegate.Build(default, 0));
        Assert.Null(fromBuilder.Key);
        Assert.IsType<SizedBox>(fromBuilder.Child);

        var fromList = Assert.IsType<KeyedSubtree>(listDelegate.Build(default, 0));
        Assert.IsType<SizedBox>(fromList.Child);
    }

    /// <remarks>
    /// Dart hands `semanticIndexCallback` the already-repaint-boundary-wrapped child, not the raw
    /// builder result, and skips `IndexedSemantics` entirely when the callback returns null.
    /// </remarks>
    [Fact]
    public void SemanticIndexCallback_SeesTheWrappedChildAndCanSuppressTheIndex()
    {
        var seen = new List<Widget>();
        var childDelegate = new SliverChildBuilderDelegate(
            (_, _) => new SizedBox(height: 10),
            childCount: 2,
            addAutomaticKeepAlives: false,
            semanticIndexCallback: (widget, localIndex) =>
            {
                seen.Add(widget);
                return localIndex == 0 ? localIndex : null;
            });

        var first = Assert.IsType<KeyedSubtree>(childDelegate.Build(default, 0));
        var second = Assert.IsType<KeyedSubtree>(childDelegate.Build(default, 1));

        Assert.All(seen, widget => Assert.IsType<RepaintBoundary>(widget));
        Assert.Equal(0, Assert.IsType<IndexedSemantics>(first.Child).Index);
        Assert.IsType<RepaintBoundary>(second.Child);
    }

    /// <remarks>
    /// Flutter's `slivers_test.dart` "SliverFixedExtentList.builder should respect
    /// semanticIndexOffset", "SliverGrid.builder respects semanticIndexOffset" and
    /// `sliver_list_test.dart` "SliverList.builder respects semanticIndexOffset".
    /// </remarks>
    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(10)]
    public void SemanticIndexOffset_ShiftsEveryIndex(int offset)
    {
        NullableIndexedWidgetBuilder itemBuilder = (_, _) => new SizedBox(height: 10);
        SliverChildDelegate[] delegates =
        [
            SliverList.Builder(itemBuilder, itemCount: 3, semanticIndexOffset: offset).Delegate,
            SliverFixedExtentList
                .Builder(itemBuilder, itemExtent: 10, itemCount: 3, semanticIndexOffset: offset).Delegate,
            SliverGrid.Builder(
                itemBuilder,
                new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2),
                itemCount: 3,
                semanticIndexOffset: offset).Delegate,
        ];

        foreach (SliverChildDelegate childDelegate in delegates)
        {
            for (int index = 0; index < 3; index++)
            {
                Assert.Equal(offset + index, SemanticIndexOf(childDelegate.Build(default, index)));
            }
        }
    }

    /// <remarks>
    /// Flutter's `slivers_test.dart` "Can override ErrorWidget.build": a throwing item builder is
    /// reported through `FlutterError.reportError` and replaced by `ErrorWidget.builder`'s result.
    /// </remarks>
    [Fact]
    public void ThrowingBuilder_ReportsTheErrorAndBuildsTheErrorWidget()
    {
        FlutterExceptionHandler? previousOnError = FlutterError.OnError;
        ErrorWidgetBuilder previousBuilder = ErrorWidget.Builder;
        var reported = new List<FlutterErrorDetails>();
        try
        {
            FlutterError.OnError = reported.Add;
            var childDelegate = new SliverChildBuilderDelegate(
                (_, _) => throw new InvalidOperationException("builder"),
                childCount: 1,
                addAutomaticKeepAlives: false,
                addRepaintBoundaries: false,
                addSemanticIndexes: false);

            var keyed = Assert.IsType<KeyedSubtree>(childDelegate.Build(default, 0));
            var errorWidget = Assert.IsType<ErrorWidget>(keyed.Child);

            FlutterErrorDetails details = Assert.Single(reported);
            Assert.Equal("widgets library", details.Library);
            Assert.IsType<InvalidOperationException>(details.Exception);
            Assert.Contains("builder", errorWidget.Message);

            // The builder is overridable, and the override sees the same details object.
            reported.Clear();
            FlutterErrorDetails? overridden = null;
            ErrorWidget.Builder = errorDetails =>
            {
                overridden = errorDetails;
                return new SizedBox(height: 1);
            };
            var replaced = Assert.IsType<KeyedSubtree>(childDelegate.Build(default, 0));
            Assert.IsType<SizedBox>(replaced.Child);
            Assert.Same(Assert.Single(reported), overridden);
        }
        finally
        {
            ErrorWidget.Builder = previousBuilder;
            FlutterError.OnError = previousOnError;
        }
    }

    /// <remarks>
    /// Flutter's `SliverChildDelegate.toString` is `describeIdentity(this)(estimated child count: n)`;
    /// a null estimate contributes nothing and a throwing estimate becomes `EXCEPTION (<type>)`.
    /// </remarks>
    [Fact]
    public void ToString_ReportsTheEstimatedChildCount()
    {
        Assert.Contains(
            "(estimated child count: 2)",
            new SliverChildListDelegate([new SizedBox(), new SizedBox()]).ToString());
        Assert.EndsWith("()", new SliverChildBuilderDelegate((_, _) => new SizedBox()).ToString());
        Assert.Contains(
            "(estimated child count: EXCEPTION (InvalidOperationException))",
            new ThrowingEstimateDelegate().ToString());
    }

    /// <remarks>
    /// Flutter's `slivers_test.dart` "SliverList.separated has correct number of children": the
    /// delegate holds `max(0, itemCount * 2 - 1)` children.
    /// </remarks>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(5, 9)]
    public void Separated_HoldsTwoChildrenPerItemMinusTheTrailingSeparator(int itemCount, int expected)
    {
        SliverList sliver = SliverList.Separated(
            (_, index) => new SizedBox(height: 10, key: new ValueKey<int>(index)),
            (_, _) => new SizedBox(height: 1),
            itemCount: itemCount);

        Assert.Equal(expected, sliver.Delegate.EstimatedChildCount);
    }

    /// <remarks>
    /// Flutter's `slivers_test.dart` "SliverList.separated can build children": an even child index
    /// is an item and an odd one is the separator after it, and separators get no semantic index.
    /// </remarks>
    [Fact]
    public void Separated_AlternatesItemsAndSeparatorsAndOnlyIndexesItems()
    {
        var items = new List<int>();
        var separators = new List<int>();
        SliverList sliver = SliverList.Separated(
            (_, index) =>
            {
                items.Add(index);
                return new SizedBox(height: 10);
            },
            (_, index) =>
            {
                separators.Add(index);
                return new SizedBox(height: 1);
            },
            itemCount: 3,
            addAutomaticKeepAlives: false);

        for (int index = 0; index < 5; index++)
        {
            sliver.Delegate.Build(default, index);
        }

        Assert.Equal([0, 1, 2], items);
        Assert.Equal([0, 1], separators);
        Assert.Equal(0, SemanticIndexOf(sliver.Delegate.Build(default, 0)));
        Assert.Null(SemanticIndexOf(sliver.Delegate.Build(default, 1)));
        Assert.Equal(1, SemanticIndexOf(sliver.Delegate.Build(default, 2)));
        Assert.Null(SemanticIndexOf(sliver.Delegate.Build(default, 3)));
        Assert.Equal(2, SemanticIndexOf(sliver.Delegate.Build(default, 4)));
        Assert.Null(sliver.Delegate.Build(default, 5));
    }

    /// <remarks>
    /// `findItemIndexCallback` returns an item index, which the delegate doubles into a child index;
    /// the deprecated `findChildIndexCallback` already returns a child index and is passed through.
    /// Providing both is an error.
    /// </remarks>
    [Fact]
    public void Separated_DoublesTheItemIndexCallbackButNotTheChildIndexCallback()
    {
        NullableIndexedWidgetBuilder itemBuilder = (_, _) => new SizedBox(height: 10);
        NullableIndexedWidgetBuilder separatorBuilder = (_, _) => new SizedBox(height: 1);

        SliverList byItem = SliverList.Separated(
            itemBuilder,
            separatorBuilder,
            itemCount: 4,
            findItemIndexCallback: _ => 3);
        Assert.Equal(6, byItem.Delegate.FindIndexByKey(new ValueKey<int>(0)));

        SliverList byChild = SliverList.Separated(
            itemBuilder,
            separatorBuilder,
            itemCount: 4,
            findChildIndexCallback: _ => 3);
        Assert.Equal(3, byChild.Delegate.FindIndexByKey(new ValueKey<int>(0)));

        Assert.Throws<ArgumentException>(() => SliverList.Separated(
            itemBuilder,
            separatorBuilder,
            itemCount: 4,
            findItemIndexCallback: _ => 0,
            findChildIndexCallback: _ => 0));
    }

    /// <remarks>
    /// Dart asserts `separatorBuilder cannot return null.` inside `SliverList.separated`'s builder,
    /// which `SliverChildBuilderDelegate.build` then catches and routes through `_createErrorWidget`.
    /// </remarks>
    [Fact]
    public void Separated_RejectsANullSeparator()
    {
        FlutterExceptionHandler? previousOnError = FlutterError.OnError;
        var reported = new List<FlutterErrorDetails>();
        try
        {
            FlutterError.OnError = reported.Add;
            SliverList sliver = SliverList.Separated(
                (_, _) => new SizedBox(height: 10),
                (_, _) => null,
                itemCount: 2,
                addAutomaticKeepAlives: false,
                addRepaintBoundaries: false,
                addSemanticIndexes: false);

            var keyed = Assert.IsType<KeyedSubtree>(sliver.Delegate.Build(default, 1));
            Assert.IsType<ErrorWidget>(keyed.Child);
            var error = Assert.IsType<FlutterError>(Assert.Single(reported).Exception);
            Assert.Contains("separatorBuilder cannot return null.", error.Message);
        }
        finally
        {
            FlutterError.OnError = previousOnError;
        }
    }

    /// <remarks>
    /// `ListView.separated` reports `itemCount` (not the doubled child count) as its
    /// `semanticChildCount`, exactly as Dart's `super(semanticChildCount: itemCount)` does.
    /// </remarks>
    [Fact]
    public void ListViewSeparated_ReportsTheItemCountAsItsSemanticChildCount()
    {
        ListView separated = ListView.Separated(
            itemCount: 4,
            itemBuilder: (_, _) => new SizedBox(height: 10),
            separatorBuilder: (_, _) => new SizedBox(height: 1));

        Assert.Equal(4, separated.SemanticChildCount);
    }

    /// <remarks>
    /// Dart's `ListView.builder(itemCount: null)` is unbounded: the delegate reports no estimate and
    /// the list ends where the builder first returns null.
    /// </remarks>
    [Fact]
    public void ListViewBuilder_AcceptsANullItemCount()
    {
        ListView listView = ListView.Builder((_, index) => index >= 3 ? null : new SizedBox(height: 100));

        Assert.Null(listView.SemanticChildCount);

        var harness = new WidgetRenderHarness(listView);
        harness.Pump(new Size(800, 600));

        var sliver = Assert.IsType<RenderSliverList>(FindRenderObject<RenderSliverList>(harness.RenderView));
        Assert.Equal(300.0, sliver.Geometry.ScrollExtent);
    }

    /// <remarks>
    /// Dart's `_SelectionKeepAlive` interposes itself between the descendants' selectables and the
    /// ancestor registrar, and asks to be kept alive exactly while one of them holds a selection.
    /// </remarks>
    [Fact]
    public void SelectionKeepAlive_ForwardsSelectablesAndKeepsAliveWhileSelected()
    {
        var registrar = new RecordingRegistrar();
        var selectable = new FakeSelectable();
        var host = new SelectableHost(selectable);
        var handles = new List<KeepAliveHandle>();
        int releases = 0;

        var harness = new WidgetRenderHarness(new SelectionRegistrarScope(
            registrar,
            new NotificationListener<KeepAliveNotification>(
                new SelectionKeepAlive(host),
                notification =>
                {
                    if (!handles.Contains(notification.Handle))
                    {
                        handles.Add(notification.Handle);
                        notification.Handle.AddListener(() => releases++);
                    }

                    return true;
                })));
        harness.Pump(new Size(200, 200));

        Assert.Same(selectable, Assert.Single(registrar.Selectables));
        Assert.Empty(handles);

        // The keep-alive interposes itself: the subtree registers with it, not with the outer scope.
        Assert.NotNull(host.SeenRegistrar);
        Assert.NotSame(registrar, host.SeenRegistrar);

        selectable.SetHasSelection(true);
        Assert.Single(handles);
        Assert.Equal(0, releases);

        selectable.SetHasSelection(false);
        Assert.Equal(1, releases);
    }

    /// <remarks>With no ancestor registrar the widget is a pass-through and registers nothing.</remarks>
    [Fact]
    public void SelectionKeepAlive_WithoutARegistrarPassesTheChildThrough()
    {
        var selectable = new FakeSelectable();
        var host = new SelectableHost(selectable);
        var harness = new WidgetRenderHarness(new SelectionKeepAlive(host));
        harness.Pump(new Size(200, 200));

        // No SelectionRegistrarScope was inserted, so the subtree sees no registrar at all and the
        // keep-alive never has anything to keep alive.
        Assert.Null(host.SeenRegistrar);
        selectable.SetHasSelection(true);
        Assert.Null(host.SeenRegistrar);
    }

    private static int? SemanticIndexOf(Widget? widget)
    {
        while (widget is not null)
        {
            switch (widget)
            {
                case IndexedSemantics indexed:
                    return indexed.Index;
                case KeyedSubtree keyed:
                    widget = keyed.Child;
                    break;
                case AutomaticKeepAlive keepAlive:
                    widget = keepAlive.Child;
                    break;
                case SelectionKeepAlive selectionKeepAlive:
                    widget = selectionKeepAlive.Child;
                    break;
                default:
                    return null;
            }
        }

        return null;
    }

    private static T? FindRenderObject<T>(RenderObject? node) where T : RenderObject
    {
        if (node is T match)
        {
            return match;
        }

        T? found = null;
        node?.VisitChildren(child =>
        {
            found ??= FindRenderObject<T>(child);
        });
        return found;
    }

    private sealed class ThrowingEstimateDelegate : SliverChildDelegate
    {
        public override int? EstimatedChildCount => throw new InvalidOperationException("nope");

        public override Widget? Build(BuildContext context, int index) => null;

        public override bool ShouldRebuild(SliverChildDelegate oldDelegate) => true;
    }

    private sealed class RecordingRegistrar : ISelectionRegistrar
    {
        public List<ISelectable> Selectables { get; } = [];

        public void Add(ISelectable selectable) => Selectables.Add(selectable);

        public void Remove(ISelectable selectable) => Selectables.Remove(selectable);
    }

    /// <summary>Registers one selectable with whatever registrar the context provides.</summary>
    private sealed class SelectableHost : StatefulWidget
    {
        public SelectableHost(FakeSelectable selectable)
        {
            Selectable = selectable;
        }

        public FakeSelectable Selectable { get; }

        /// <summary>The registrar the subtree saw, or null when there was none.</summary>
        public ISelectionRegistrar? SeenRegistrar { get; private set; }

        public override State CreateState() => new SelectableHostState();

        private sealed class SelectableHostState : State
        {
            private ISelectionRegistrar? _registrar;

            public override void DidChangeDependencies()
            {
                base.DidChangeDependencies();
                ISelectionRegistrar? newRegistrar = SelectionContainer.MaybeOf(Context);
                if (ReferenceEquals(_registrar, newRegistrar))
                {
                    return;
                }

                var host = (SelectableHost)StateWidget;
                _registrar?.Remove(host.Selectable);
                _registrar = newRegistrar;
                host.SeenRegistrar = newRegistrar;
                _registrar?.Add(host.Selectable);
            }

            public override Widget Build(BuildContext context) => new SizedBox(height: 10);
        }
    }

    private sealed class FakeSelectable : ISelectable
    {
        private readonly List<Action> _listeners = [];

        public SelectionGeometry Value { get; private set; } =
            new(SelectionStatus.None, hasContent: true);

        public int ContentLength => 1;

        public Size Size => new(10, 10);

        public IReadOnlyList<Rect> BoundingBoxes => [];

        public void SetHasSelection(bool hasSelection)
        {
            Value = new SelectionGeometry(
                hasSelection ? SelectionStatus.Uncollapsed : SelectionStatus.None,
                hasContent: true);
            foreach (Action listener in _listeners.ToArray())
            {
                listener();
            }
        }

        public void AddListener(Action listener) => _listeners.Add(listener);

        public void RemoveListener(Action listener) => _listeners.Remove(listener);

        public void PushHandleLayers(LayerLink? startHandle, LayerLink? endHandle)
        {
        }

        public SelectedContent? GetSelectedContent() => null;

        public SelectedContentRange? GetSelection() => null;

        public SelectionResult DispatchSelectionEvent(SelectionEvent @event) => SelectionResult.None;

        public Matrix4 GetTransformTo(RenderObject? ancestor) => Matrix4.Identity();

        public void Dispose()
        {
        }
    }
    private sealed class WidgetRenderHarness
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);

            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
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

        private sealed class HarnessRootElement(RenderView renderView, Widget widget) : Element(widget), IRenderObjectHost
        {
            private Element? _child;

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
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is RenderBox renderBox && ReferenceEquals(renderView.Child, renderBox))
                {
                    renderView.Child = null;
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
        }
    }
}
