using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/widgets/basic.dart (StatefulBuilder)
// - flutter/packages/flutter/lib/src/widgets/lookup_boundary.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/view.dart

public sealed class StatefulBuilderLookupBoundaryTests
{
    [Fact]
    public void StatefulBuilder_StateSetterRebuildsOnlyItsLocalSubtree()
    {
        int parentBuilds = 0;
        int localBuilds = 0;
        int value = 1;
        StateSetter? stateSetter = null;
        var stableBuilder = new StatefulBuilder((context, setState) =>
        {
            stateSetter = setState;
            localBuilds += 1;
            return new SizedBox(width: value, height: value);
        });
        using var harness = new WidgetHarness(
            new Builder(context =>
            {
                parentBuilds += 1;
                return stableBuilder;
            }));

        Assert.Equal(1, parentBuilds);
        Assert.Equal(1, localBuilds);
        Assert.NotNull(stateSetter);

        stateSetter!(() => value = 2);
        harness.FlushBuild();

        Assert.Equal(1, parentBuilds);
        Assert.Equal(2, localBuilds);
        Assert.Equal(2, Assert.IsType<RenderConstrainedBox>(harness.RenderView.Child).Size.Width);
    }

    [Fact]
    public void StatefulBuilder_UpdateUsesReplacementBuilderAndRetainsStateSetter()
    {
        int firstBuilds = 0;
        int secondBuilds = 0;
        StateSetter? originalSetter = null;
        using var harness = new WidgetHarness(
            new StatefulBuilder((context, setState) =>
            {
                originalSetter = setState;
                firstBuilds += 1;
                return new SizedBox(width: 1, height: 1);
            }));

        harness.Update(
            new StatefulBuilder((context, setState) =>
            {
                secondBuilds += 1;
                return new SizedBox(width: 2, height: 2);
            }));

        Assert.Equal(1, firstBuilds);
        Assert.Equal(1, secondBuilds);

        originalSetter!(() => { });
        harness.FlushBuild();

        Assert.Equal(1, firstBuilds);
        Assert.Equal(2, secondBuilds);
    }

    [Fact]
    public void LookupBoundary_HidesOuterInheritedWidgetAndDependsOnVisibleCandidate()
    {
        var tracker = new LookupTracker();
        var stableProbe = new InheritedLookupProbe(tracker);
        var stableBoundary = new LookupBoundary(
            child: new IntScope(
                value: 2,
                child: stableProbe));
        using var harness = new WidgetHarness(
            new IntScope(
                value: 1,
                child: stableBoundary));

        Assert.Equal([2], tracker.BoundedValues);
        Assert.Equal([2], tracker.UnboundedValues);
        Assert.False(tracker.HiddenAtLastBuild);

        harness.Update(
            new IntScope(
                value: 9,
                child: stableBoundary));

        Assert.Equal([2], tracker.BoundedValues);
        Assert.Equal([2], tracker.UnboundedValues);

        harness.Update(
            new IntScope(
                value: 9,
                child: new LookupBoundary(
                    child: new IntScope(
                        value: 3,
                        child: stableProbe))));

        Assert.Equal([2, 3], tracker.BoundedValues);
        Assert.Equal([2, 3], tracker.UnboundedValues);
    }

    [Fact]
    public void LookupBoundary_ReturnsNullForHiddenInheritedWidgetAndReportsDebugState()
    {
        var tracker = new LookupTracker();
        using var harness = new WidgetHarness(
            new IntScope(
                value: 5,
                child: new LookupBoundary(
                    child: new HiddenInheritedLookupProbe(tracker))));

        Assert.Null(tracker.HiddenInheritedValue);
        Assert.True(tracker.HiddenAtLastBuild);
        Assert.Null(tracker.HiddenInheritedElement);
    }

    [Fact]
    public void LookupBoundary_BoundsWidgetStateRenderObjectAndAncestorVisitors()
    {
        var tracker = new LookupTracker();
        var outer = new OuterStatefulWidget(
            name: "outer",
            child: new SizedBox(
                child: new LookupBoundary(
                    child: new NamedStatefulWidget(
                        name: "inner",
                        child: new Padding(
                            insets: new Avalonia.Thickness(4),
                            child: new AncestorLookupProbe(tracker))))));
        using var harness = new WidgetHarness(outer);

        Assert.Equal("inner", tracker.NearestStateName);
        Assert.Equal("inner", tracker.RootStateName);
        Assert.Equal("outer", tracker.UnboundedRootStateName);
        Assert.True(tracker.FoundInnerPadding);
        Assert.False(tracker.FoundOuterSizedBox);
        Assert.True(tracker.HiddenOuterWidget);
        Assert.True(tracker.HiddenOuterState);
        Assert.True(tracker.HiddenOuterRenderObject);
        Assert.Equal([typeof(Padding), typeof(NamedStatefulWidget), typeof(LookupBoundary)], tracker.VisitedTypes);
    }

    [Fact]
    public void LookupBoundary_VisitChildElementsSkipsBoundaryChildren()
    {
        BuildContext? capturedContext = null;
        using var harness = new WidgetHarness(
            new Builder(context =>
            {
                capturedContext = context;
                return new LookupBoundary(child: new SizedBox(width: 1, height: 1));
            }));
        int visits = 0;

        LookupBoundary.VisitChildElements(capturedContext!.Value, element => visits += 1);

        Assert.Equal(0, visits);
    }

    [Fact]
    public void View_OfAndMaybeOfReturnTheExactVisibleView()
    {
        var view = new FlutterView(new Size(800, 600), devicePixelRatio: 2.0, viewId: 7);
        FlutterView? required = null;
        FlutterView? optional = null;
        using var harness = new WidgetHarness(
            new View(
                view,
                new Builder(context =>
                {
                    required = View.Of(context);
                    optional = View.MaybeOf(context);
                    return new SizedBox(width: 1, height: 1);
                })));

        Assert.Same(view, required);
        Assert.Same(view, optional);
    }

    [Fact]
    public void View_LookupBoundaryHidesTheOuterView()
    {
        FlutterView? optional = null;
        InvalidOperationException? error = null;
        using var harness = new WidgetHarness(
            new View(
                new FlutterView(new Size(800, 600)),
                new LookupBoundary(
                    child: new Builder(context =>
                    {
                        optional = View.MaybeOf(context);
                        error = Assert.Throws<InvalidOperationException>(() => View.Of(context));
                        return new SizedBox(width: 1, height: 1);
                    }))));

        Assert.Null(optional);
        Assert.Contains("hidden by a LookupBoundary", error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void View_NotifiesDependentsOnlyWhenTheViewIdentityChanges()
    {
        int builds = 0;
        var first = new FlutterView(new Size(800, 600));
        var second = new FlutterView(new Size(1024, 768));
        var probe = new Builder(context =>
        {
            _ = View.Of(context);
            builds += 1;
            return new SizedBox(width: 1, height: 1);
        });
        using var harness = new WidgetHarness(new View(first, probe));

        first.UpdateMetrics(new Size(900, 700), devicePixelRatio: 1.0, viewId: 0);
        harness.Update(new View(first, probe));
        Assert.Equal(1, builds);

        harness.Update(new View(second, probe));
        Assert.Equal(2, builds);
    }

    private sealed class IntScope : InheritedWidget
    {
        public IntScope(int value, Widget child)
        {
            Value = value;
            Child = child;
        }

        public int Value { get; }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return Value != ((IntScope)oldWidget).Value;
        }
    }

    private sealed class InheritedLookupProbe(LookupTracker tracker) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            IntScope bounded = LookupBoundary.DependOnInheritedWidgetOfExactType<IntScope>(context)
                ?? throw new InvalidOperationException("Expected the inner IntScope.");
            IntScope unbounded = context.DependOnInherited<IntScope>()
                ?? throw new InvalidOperationException("Expected the nearest IntScope.");
            tracker.BoundedValues.Add(bounded.Value);
            tracker.UnboundedValues.Add(unbounded.Value);
            tracker.HiddenAtLastBuild = LookupBoundary.DebugIsHidingAncestorWidgetOfExactType<IntScope>(context);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class HiddenInheritedLookupProbe(LookupTracker tracker) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            tracker.HiddenInheritedValue = LookupBoundary.GetInheritedWidgetOfExactType<IntScope>(context)?.Value;
            tracker.HiddenInheritedElement =
                LookupBoundary.GetElementForInheritedWidgetOfExactType<IntScope>(context);
            tracker.HiddenAtLastBuild = LookupBoundary.DebugIsHidingAncestorWidgetOfExactType<IntScope>(context);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class NamedStatefulWidget : StatefulWidget
    {
        public NamedStatefulWidget(string name, Widget child)
        {
            Name = name;
            Child = child;
        }

        public string Name { get; }

        public Widget Child { get; }

        public override State CreateState() => new NamedState();
    }

    private abstract class NamedStateBase : State
    {
        public abstract string Name { get; }
    }

    private sealed class NamedState : NamedStateBase
    {
        public override string Name => ((NamedStatefulWidget)StateWidget).Name;

        public override Widget Build(BuildContext context) => ((NamedStatefulWidget)StateWidget).Child;
    }

    private sealed class OuterStatefulWidget : StatefulWidget
    {
        public OuterStatefulWidget(string name, Widget child)
        {
            Name = name;
            Child = child;
        }

        public string Name { get; }

        public Widget Child { get; }

        public override State CreateState() => new OuterNamedState();
    }

    private sealed class OuterNamedState : NamedStateBase
    {
        public override string Name => ((OuterStatefulWidget)StateWidget).Name;

        public override Widget Build(BuildContext context) => ((OuterStatefulWidget)StateWidget).Child;
    }

    private sealed class AncestorLookupProbe(LookupTracker tracker) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            tracker.NearestStateName = LookupBoundary.FindAncestorStateOfType<NamedStateBase>(context)?.Name;
            tracker.RootStateName = LookupBoundary.FindRootAncestorStateOfType<NamedStateBase>(context)?.Name;
            tracker.UnboundedRootStateName = context.FindRootAncestorStateOfType<NamedStateBase>()?.Name;
            tracker.FoundInnerPadding =
                LookupBoundary.FindAncestorWidgetOfExactType<Padding>(context) != null;
            tracker.FoundOuterSizedBox =
                LookupBoundary.FindAncestorWidgetOfExactType<SizedBox>(context) != null;
            tracker.HiddenOuterWidget =
                LookupBoundary.DebugIsHidingAncestorWidgetOfExactType<SizedBox>(context);
            tracker.HiddenOuterState =
                LookupBoundary.DebugIsHidingAncestorStateOfType<OuterNamedState>(context);
            tracker.HiddenOuterRenderObject =
                LookupBoundary.DebugIsHidingAncestorRenderObjectOfType<RenderConstrainedBox>(context);
            tracker.VisitedTypes.Clear();
            LookupBoundary.VisitAncestorElements(context, element =>
            {
                tracker.VisitedTypes.Add(element.Widget.GetType());
                return true;
            });
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class LookupTracker
    {
        public List<int> BoundedValues { get; } = [];
        public List<int> UnboundedValues { get; } = [];
        public List<Type> VisitedTypes { get; } = [];
        public int? HiddenInheritedValue { get; set; }
        public InheritedElement? HiddenInheritedElement { get; set; }
        public bool HiddenAtLastBuild { get; set; }
        public string? NearestStateName { get; set; }
        public string? RootStateName { get; set; }
        public string? UnboundedRootStateName { get; set; }
        public bool FoundInnerPadding { get; set; }
        public bool FoundOuterSizedBox { get; set; }
        public bool HiddenOuterWidget { get; set; }
        public bool HiddenOuterState { get; set; }
        public bool HiddenOuterRenderObject { get; set; }
    }

    private sealed class WidgetHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly RootElement _root;

        public WidgetHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Update(Widget widget)
        {
            _root.Update(widget);
            FlushBuild();
        }

        public void FlushBuild()
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(new Avalonia.Size(100, 100));
        }

        public void Dispose()
        {
            _root.Unmount();
        }

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public RootElement(RenderView renderView, Widget widget) : base(widget)
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
                if (ReferenceEquals(_child, child))
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
                _renderView.Child = child as RenderBox;
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
