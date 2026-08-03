using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/test/material/page_transitions_theme_test.dart
// flutter/packages/flutter/test/material/predictive_back_page_transitions_builder_test.dart
// flutter/packages/flutter/test/material/page_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialPageTransitionsTests : IDisposable
{
    public MaterialPageTransitionsTests()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    [Fact]
    public void PageTransitionsTheme_ExposesPinnedPlatformDefaultsAndFallbacks()
    {
        var theme = new PageTransitionsTheme();

        Assert.IsType<PredictiveBackPageTransitionsBuilder>(theme.Builders[TargetPlatform.Android]);
        Assert.IsType<CupertinoPageTransitionsBuilder>(theme.Builders[TargetPlatform.IOS]);
        Assert.IsType<CupertinoPageTransitionsBuilder>(theme.Builders[TargetPlatform.MacOS]);
        Assert.IsType<ZoomPageTransitionsBuilder>(theme.Builders[TargetPlatform.Linux]);
        Assert.IsType<ZoomPageTransitionsBuilder>(theme.Builders[TargetPlatform.Windows]);
        Assert.False(theme.Builders.ContainsKey(TargetPlatform.Fuchsia));

        var emptyTheme = new PageTransitionsTheme(
            new Dictionary<TargetPlatform, PageTransitionsBuilder>());
        Assert.IsType<CupertinoPageTransitionsBuilder>(emptyTheme.Resolve(TargetPlatform.IOS));
        Assert.IsType<ZoomPageTransitionsBuilder>(emptyTheme.Resolve(TargetPlatform.MacOS));
        Assert.NotNull(emptyTheme.DelegatedTransition(TargetPlatform.IOS));
    }

    [Fact]
    public void PageTransitionBuilders_ExposeExactDurationsSnapshotFlagsAndFallbackColors()
    {
        Color fallback = Colors.Black;
        var fade = new FadeForwardsPageTransitionsBuilder(fallback);
        var zoom = new ZoomPageTransitionsBuilder(
            allowSnapshotting: false,
            allowEnterRouteSnapshotting: false,
            backgroundColor: fallback);
        var predictive = new PredictiveBackPageTransitionsBuilder(fallback);
        var fullscreen = new PredictiveBackFullscreenPageTransitionsBuilder(fallback);

        Assert.Equal(TimeSpan.FromMilliseconds(450), fade.TransitionDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(300), zoom.TransitionDuration);
        Assert.False(zoom.AllowSnapshotting);
        Assert.False(zoom.AllowEnterRouteSnapshotting);
        Assert.Equal(fallback, zoom.BackgroundColor);
        Assert.Equal(TimeSpan.FromMilliseconds(450), predictive.TransitionDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(300), fullscreen.TransitionDuration);
        Assert.Null(predictive.DelegatedTransition);
        Assert.Null(fullscreen.DelegatedTransition);
    }

    [Fact]
    public void EmphasizedCurveAndTweenSequence_MatchFlutterControlPointsAndWeights()
    {
        Assert.Equal(0.0, Curves.EaseInOutCubicEmphasized(0.0), precision: 6);
        Assert.Equal(0.4, Curves.EaseInOutCubicEmphasized(0.166666), precision: 5);
        Assert.Equal(1.0, Curves.EaseInOutCubicEmphasized(1.0), precision: 6);

        var sequence = new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(new DoubleTween(0.0, 0.4), 0.166666),
            new TweenSequenceItem<double>(new DoubleTween(0.4, 1.0), 1.0 - 0.166666),
        ]);
        Assert.Equal(0.0, sequence.Transform(0.0), precision: 6);
        Assert.Equal(0.4, sequence.Transform(0.166666), precision: 5);
        Assert.Equal(1.0, sequence.Transform(1.0), precision: 6);
    }

    [Fact]
    public void PredictiveBackEvent_DecodesPlatformPayloadAndIdentifiesButtonEvents()
    {
        var gesture = PredictiveBackEvent.FromMap(
            new Dictionary<string, object?>
            {
                ["progress"] = 0.25,
                ["swipeEdge"] = 1,
                ["touchOffset"] = new object?[] { 12.0, 24.0 },
            });

        Assert.Equal(0.25, gesture.Progress);
        Assert.Equal(SwipeEdge.Right, gesture.SwipeEdge);
        Assert.Equal(new Point(12.0, 24.0), gesture.TouchOffset);
        Assert.False(gesture.IsButtonEvent);
        Assert.True(new PredictiveBackEvent(0.0, SwipeEdge.Left).IsButtonEvent);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PredictiveBackEvent(1.1, SwipeEdge.Left, new Point(1.0, 1.0)));
    }

    [Fact]
    public void WidgetsBinding_KeepsTheStartTimePredictiveObserverSetForTheWholeGesture()
    {
        var binding = new WidgetsBinding();
        var first = new RecordingBackObserver(accept: true);
        var late = new RecordingBackObserver(accept: true);
        var rejected = new RecordingBackObserver(accept: false);
        binding.AddObserver(first);
        binding.AddObserver(rejected);
        var start = new PredictiveBackEvent(0.0, SwipeEdge.Left, new Point(2.0, 3.0));

        Assert.True(binding.HandleStartBackGesture(start));
        binding.AddObserver(late);
        binding.HandleUpdateBackGestureProgress(
            new PredictiveBackEvent(0.5, SwipeEdge.Left, new Point(40.0, 3.0)));
        binding.HandleCommitBackGesture();

        Assert.Equal(1, first.StartCount);
        Assert.Equal(1, first.UpdateCount);
        Assert.Equal(1, first.CommitCount);
        Assert.Equal(1, rejected.StartCount);
        Assert.Equal(0, rejected.UpdateCount);
        Assert.Equal(0, late.StartCount);
        Assert.Equal(0, late.UpdateCount);
    }

    [Fact]
    public void MaterialPageRoute_PredictiveGestureUpdatesCancelsAndCommitsTheRoute()
    {
        NavigatorState? navigator = null;
        var initialRoute = new MaterialPageRoute(
            context =>
            {
                navigator ??= Navigator.Of(context);
                return new SizedBox(width: 20.0, height: 20.0);
            },
            settings: new RouteSettings(Name: "root"));
        var root = MountNavigator(initialRoute);
        var detailsRoute = new MaterialPageRoute(
            _ => new SizedBox(width: 20.0, height: 20.0),
            settings: new RouteSettings(Name: "details"));

        navigator!.Push(detailsRoute);
        Settle(root.TestOwner);
        Assert.True(detailsRoute.PopGestureEnabled);

        var start = new PredictiveBackEvent(0.0, SwipeEdge.Left, new Point(8.0, 300.0));
        Assert.True(WidgetsBinding.Instance.HandleStartBackGesture(start));
        WidgetsBinding.Instance.HandleUpdateBackGestureProgress(
            new PredictiveBackEvent(0.5, SwipeEdge.Left, new Point(80.0, 340.0)));
        root.TestOwner.FlushBuild();

        Assert.True(detailsRoute.PopGestureInProgress);
        Assert.Equal(0.5, detailsRoute.Animation.Value, precision: 6);
        Assert.Contains(FindWidgets<ClipRRect>(root), clip => clip.BorderRadius.Radius > 0.0);

        WidgetsBinding.Instance.HandleCancelBackGesture();
        Settle(root.TestOwner);
        Assert.Same(detailsRoute, navigator.CurrentRoute);
        Assert.False(detailsRoute.PopGestureInProgress);

        Assert.True(WidgetsBinding.Instance.HandleStartBackGesture(start));
        WidgetsBinding.Instance.HandleUpdateBackGestureProgress(
            new PredictiveBackEvent(0.6, SwipeEdge.Left, new Point(100.0, 360.0)));
        WidgetsBinding.Instance.HandleCommitBackGesture();
        root.TestOwner.FlushBuild();
        Assert.Same(initialRoute, navigator.CurrentRoute);
        Settle(root.TestOwner);
        Assert.False(navigator.UserGestureInProgress);

        root.Unmount();
    }

    [Fact]
    public void ModalRoute_UsesTheIncomingRoutesDelegatedTransition()
    {
        NavigatorState? navigator = null;
        var initialRoute = new TestPageRoute(
            context =>
            {
                navigator ??= Navigator.Of(context);
                return new SizedBox(width: 10.0, height: 10.0);
            });
        var root = MountNavigator(initialRoute);
        var delegatedRoute = new TestPageRoute(
            _ => new SizedBox(width: 10.0, height: 10.0),
            delegatedTransition: (_, _, _, _, child) => new Opacity(0.5, child));

        navigator!.Push(delegatedRoute);
        root.TestOwner.FlushBuild();

        Assert.NotNull(initialRoute.ReceivedTransition);
        Assert.Contains(FindWidgets<Opacity>(root), opacity => opacity.Value == 0.5);

        root.Unmount();
    }

    [Fact]
    public void SnapshotController_ControlsRetainedSnapshotRenderLayerState()
    {
        var controller = new SnapshotController();
        using var harness = new RenderHarness(
            new SnapshotWidget(
                controller,
                mode: SnapshotMode.Permissive,
                autoresize: true,
                pixelRatio: 2.0,
                child: new SizedBox(
                    width: 40.0,
                    height: 30.0,
                    child: new ColoredBox(Colors.Red))));

        harness.Pump(new Size(40.0, 30.0));
        RenderSnapshotWidget render = Assert.Single(FindRenderObjects<RenderSnapshotWidget>(harness.RenderView));
        var layer = Assert.IsType<SnapshotOffsetLayer>(render.EnsureCompositedLayer());
        Assert.False(layer.AllowSnapshotting);
        Assert.Equal(new Size(40.0, 30.0), layer.Size);

        controller.AllowSnapshotting = true;
        harness.Pump(new Size(40.0, 30.0));
        Assert.True(layer.AllowSnapshotting);
        int version = layer.ClearVersion;

        controller.Clear();
        harness.Pump(new Size(40.0, 30.0));
        Assert.True(layer.ClearVersion > version);
    }

    private static TestRootElement MountNavigator(Route initialRoute)
    {
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.Android,
        };
        Widget rootWidget = new MediaQuery(
            new MediaQueryData(
                Size: new Size(400.0, 800.0),
                DisplayCornerRadii: BorderRadius.Circular(33.3)),
            new Theme(
                theme,
                new Directionality(
                    TextDirection.Ltr,
                    new Navigator(initialRoute))));
        var root = new TestRootElement(rootWidget);
        root.Attach(root.TestOwner);
        root.Mount(parent: null, newSlot: null);
        root.TestOwner.FlushBuild();
        Settle(root.TestOwner);
        return root;
    }

    private static void Settle(BuildOwner owner)
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
    }

    private static IReadOnlyList<T> FindWidgets<T>(TestRootElement root) where T : Widget
    {
        var widgets = new List<T>();
        CollectWidgets(root, widgets);
        return widgets;
    }

    private static void CollectWidgets<T>(Element element, ICollection<T> widgets) where T : Widget
    {
        if (element.Widget is T match)
        {
            widgets.Add(match);
        }

        element.VisitChildren(child => CollectWidgets(child, widgets));
    }

    private static IReadOnlyList<T> FindRenderObjects<T>(RenderObject? root) where T : RenderObject
    {
        var results = new List<T>();
        if (root is null)
        {
            return results;
        }

        if (root is T match)
        {
            results.Add(match);
        }

        root.VisitChildren(child => results.AddRange(FindRenderObjects<T>(child)));
        return results;
    }

    private sealed class RecordingBackObserver : WidgetsBindingObserver
    {
        private readonly bool _accept;

        public RecordingBackObserver(bool accept)
        {
            _accept = accept;
        }

        public int StartCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int CommitCount { get; private set; }

        public bool HandleStartBackGesture(PredictiveBackEvent backEvent)
        {
            _ = backEvent;
            StartCount += 1;
            return _accept;
        }

        public void HandleUpdateBackGestureProgress(PredictiveBackEvent backEvent)
        {
            _ = backEvent;
            UpdateCount += 1;
        }

        public void HandleCommitBackGesture()
        {
            CommitCount += 1;
        }
    }

    private sealed class TestPageRoute : PageRoute
    {
        private readonly WidgetBuilder _builder;
        private readonly DelegatedTransitionBuilder? _delegatedTransition;

        public TestPageRoute(
            WidgetBuilder builder,
            DelegatedTransitionBuilder? delegatedTransition = null)
        {
            _builder = builder;
            _delegatedTransition = delegatedTransition;
        }

        public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(300);

        public override DelegatedTransitionBuilder? DelegatedTransition => _delegatedTransition;

        public override Widget BuildPage(BuildContext context)
        {
            return _builder(context);
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public BuildOwner TestOwner { get; } = new();

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
            if (_child is not null)
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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }
    }

    private sealed class RenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly RenderRootElement _root;

        public RenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RenderRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
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

        public void Dispose()
        {
            _root.Unmount();
        }

        private sealed class RenderRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public RenderRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
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

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
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

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
