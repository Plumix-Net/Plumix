using Avalonia;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Parity coverage for `_ScaffoldLayout`, `ScaffoldGeometry`/`_ScaffoldGeometryNotifier` and
/// `_FloatingActionButtonTransition` (Dart parity source: `material_ui/lib/src/scaffold.dart`).
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialScaffoldGeometryTests
{
    private static readonly Size Viewport = new(800, 600);

    [Fact]
    public void ScaffoldGeometry_BottomNavigationBarTop_IsTheBarsTopEdge()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            bottomNavigationBar: new SizedBox(height: 100))));
        harness.Pump(Viewport);

        ScaffoldGeometry geometry = RequireGeometry(context);
        Assert.Equal(500.0, geometry.BottomNavigationBarTop);
    }

    [Fact]
    public void ScaffoldGeometry_WithoutBottomNavigationBar_LeavesBottomNavigationBarTopNull()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        Assert.Null(RequireGeometry(context).BottomNavigationBarTop);
    }

    [Fact]
    public void ScaffoldGeometry_FloatingActionButtonArea_MatchesTheMeasuredButtonRect()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        RenderBox slot = harness.RequireSlot(ScaffoldSlot.FloatingActionButton);
        Assert.Equal(new Size(56, 56), slot.Size);

        Rect? area = RequireGeometry(context).FloatingActionButtonArea;
        Assert.NotNull(area);

        // endFloat: x = width - kFloatingActionButtonMargin - fabWidth, y = contentBottom - fabHeight - margin.
        Assert.Equal(new Rect(new Point(728, 528), new Size(56, 56)), area!.Value);
        Assert.Equal(area.Value.Position, ((MultiChildLayoutParentData)slot.parentData!).offset);
    }

    [Fact]
    public void ScaffoldGeometry_WithoutFloatingActionButton_LeavesTheAreaNull()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        // The slot is always present, so the null area comes from scaling the stored rect by 0.0.
        Assert.Null(RequireGeometry(context).FloatingActionButtonArea);
    }

    [Fact]
    public void ScaffoldGeometry_ScalesTheAreaAboutItsCenterWhileTheButtonEnters()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        harness.Update(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.Tick(0.05);
        harness.Pump(Viewport);

        Rect? entering = RequireGeometry(context).FloatingActionButtonArea;
        Assert.NotNull(entering);
        Assert.True(entering!.Value.Width > 0.0);
        Assert.True(entering.Value.Width < 56.0);

        harness.SettleFloatingActionButton();
        Rect settled = RequireGeometry(context).FloatingActionButtonArea!.Value;
        Assert.Equal(56.0, settled.Width, 6);
        Assert.Equal(settled.Center.X, entering.Value.Center.X, 6);
        Assert.Equal(settled.Center.Y, entering.Value.Center.Y, 6);
    }

    [Fact]
    public void ScaffoldGeometry_NotifiesOnEveryAnimatingFrame()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        ScaffoldGeometryNotifier notifier = Scaffold.GeometryNotifierMaybeOf(context!.Value)!;
        int notifications = 0;
        notifier.AddListener(() => notifications++);

        int afterFirstFrame = notifications;
        harness.Update(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        Assert.True(notifications > afterFirstFrame);

        int afterAdding = notifications;
        harness.Tick(0.05);
        harness.Pump(Viewport);
        Assert.True(notifications > afterAdding);
    }

    [Fact]
    public void ScaffoldGeometryOf_WithoutAScaffoldAncestor_Throws()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(Capture(c => context = c)));
        harness.Pump(Viewport);

        Assert.Throws<InvalidOperationException>(() => Scaffold.GeometryOf(context!.Value));
    }

    [Fact]
    public void ScaffoldGeometryValue_OutsideThePaintPhase_Throws()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(body: Capture(c => context = c))));
        harness.Pump(Viewport);

        IValueListenable<ScaffoldGeometry> listenable = Scaffold.GeometryOf(context!.Value);
        Assert.Throws<InvalidOperationException>(() => _ = listenable.Value);
    }

    [Fact]
    public void ScaffoldLayout_MeasuresTheBottomSheetSoTheFloatingButtonAvoidsIt()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            bottomSheet: new SizedBox(height: 100),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        Assert.Equal(100.0, harness.RequireSlot(ScaffoldSlot.BottomSheet).Size.Height);

        // FabFloatOffsetY clamps to contentBottom - bottomSheetHeight - fabHeight / 2.
        Rect area = RequireGeometry(context).FloatingActionButtonArea!.Value;
        Assert.Equal(600.0 - 100.0 - 28.0, area.Top);
    }

    [Fact]
    public void ScaffoldLayout_MeasuresAFixedSnackBarBeforePositioningTheFloatingButton()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new ScaffoldMessenger(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { })))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();
        double withoutSnackBar = RequireGeometry(context).FloatingActionButtonArea!.Value.Top;

        harness.FindState<ScaffoldMessengerState>().ShowSnackBar(
            new SnackBar(content: new SizedBox(height: 40), behavior: SnackBarBehavior.Fixed));
        harness.Tick(0.4);
        harness.Pump(Viewport);

        double snackBarHeight = harness.RequireSlot(ScaffoldSlot.SnackBar).Size.Height;
        Assert.True(snackBarHeight > 0.0);

        // FabFloatOffsetY clamps to contentBottom - snackBarHeight - fabHeight - margin.
        Rect area = RequireGeometry(context).FloatingActionButtonArea!.Value;
        Assert.Equal(600.0 - snackBarHeight - 56.0 - 16.0, area.Top, 6);
        Assert.True(area.Top < withoutSnackBar);
    }

    [Fact]
    public void ScaffoldLayout_TopLocationIgnoresExtendBodyBehindAppBar()
    {
        BuildContext? plain = null;
        using var withAppBarBehind = new Harness(Wrap(new Scaffold(
            appBar: new AppBar(titleText: "Demo"),
            extendBodyBehindAppBar: true,
            body: Capture(c => plain = c),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndTop,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        withAppBarBehind.Pump(Viewport);
        withAppBarBehind.SettleFloatingActionButton();
        double extended = RequireGeometry(plain).FloatingActionButtonArea!.Value.Top;

        BuildContext? second = null;
        using var normal = new Harness(Wrap(new Scaffold(
            appBar: new AppBar(titleText: "Demo"),
            body: Capture(c => second = c),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndTop,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        normal.Pump(Viewport);
        normal.SettleFloatingActionButton();

        Assert.Equal(RequireGeometry(second).FloatingActionButtonArea!.Value.Top, extended);
    }

    [Fact]
    public void ScaffoldLayout_ExtendBodyBehindAppBarPlacesTheBodyUnderTheAppBar()
    {
        using var extended = new Harness(Wrap(new Scaffold(
            appBar: new AppBar(titleText: "Demo"),
            extendBodyBehindAppBar: true,
            body: new SizedBox())));
        extended.Pump(Viewport);
        Assert.Equal(
            0.0,
            ((MultiChildLayoutParentData)extended.RequireSlot(ScaffoldSlot.Body).parentData!).offset.Y);

        using var normal = new Harness(Wrap(new Scaffold(
            appBar: new AppBar(titleText: "Demo"),
            body: new SizedBox())));
        normal.Pump(Viewport);
        double appBarHeight = normal.RequireSlot(ScaffoldSlot.AppBar).Size.Height;
        Assert.True(appBarHeight > 0.0);
        Assert.Equal(
            appBarHeight,
            ((MultiChildLayoutParentData)normal.RequireSlot(ScaffoldSlot.Body).parentData!).offset.Y);
    }

    [Fact]
    public void ScaffoldLayout_ResizeToAvoidBottomInsetFalse_KeepsTheButtonAboveTheKeyboard()
    {
        BuildContext? context = null;
        var media = new MediaQueryData(Size: Viewport, ViewInsets: new Thickness(0, 0, 0, 300));
        using var harness = new Harness(Wrap(
            new Scaffold(
                body: Capture(c => context = c),
                resizeToAvoidBottomInset: false,
                floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { })),
            media));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        Assert.Equal(528.0, RequireGeometry(context).FloatingActionButtonArea!.Value.Top);
    }

    [Fact]
    public void ScaffoldLayout_ResizeToAvoidBottomInset_LiftsTheButtonAboveTheKeyboard()
    {
        BuildContext? context = null;
        var media = new MediaQueryData(Size: Viewport, ViewInsets: new Thickness(0, 0, 0, 300));
        using var harness = new Harness(Wrap(
            new Scaffold(
                body: Capture(c => context = c),
                floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { })),
            media));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        // contentBottom = 600 - 300, then the standard float margin.
        Assert.Equal(300.0 - 56.0 - 16.0, RequireGeometry(context).FloatingActionButtonArea!.Value.Top);
    }

    [Fact]
    public void ScaffoldState_LocationChange_AnimatesTheButtonThroughTheMotionAnimator()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();
        Assert.Equal(728.0, RequireGeometry(context).FloatingActionButtonArea!.Value.Left);

        harness.Update(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButtonLocation: FloatingActionButtonLocation.CenterFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);

        // The scaling animator holds the old offset for the first half of the 400ms segue.
        Assert.Equal(
            728.0,
            ((MultiChildLayoutParentData)harness.RequireSlot(ScaffoldSlot.FloatingActionButton).parentData!)
            .offset.X);

        harness.Tick(0.25);
        harness.Pump(Viewport);
        Assert.Equal(
            372.0,
            ((MultiChildLayoutParentData)harness.RequireSlot(ScaffoldSlot.FloatingActionButton).parentData!)
            .offset.X);
    }

    [Fact]
    public void ScaffoldState_InterruptedLocationChange_RestartsFromTheAnimatorRestartValue()
    {
        var scaffoldKey = new LabeledGlobalKey<ScaffoldState>("scaffold");
        using var harness = new Harness(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        harness.Update(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.CenterFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.Tick(0.1);
        harness.Pump(Viewport);

        var state = harness.FindState<ScaffoldState>();
        double interruptedAt = state.FloatingActionButtonMoveProgressForTests;
        Assert.InRange(interruptedAt, 0.2, 0.3);

        harness.Update(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.StartFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);

        // getAnimationRestart(previous) == min(1 - previous, previous) avoids a size jump.
        Assert.Equal(
            Math.Min(1.0 - interruptedAt, interruptedAt),
            state.FloatingActionButtonMoveProgressForTests,
            6);
    }

    [Fact]
    public void ScaffoldLayout_MotionRelayoutsWithoutRebuildingTheScaffold()
    {
        var scaffoldKey = new LabeledGlobalKey<ScaffoldState>("scaffold");
        using var harness = new Harness(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.EndFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);
        harness.SettleFloatingActionButton();

        harness.Update(Wrap(new Scaffold(
            key: scaffoldKey,
            body: new SizedBox(),
            floatingActionButtonLocation: FloatingActionButtonLocation.CenterFloat,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);

        var layout = harness.RequireLayout();
        Assert.False(layout.NeedsLayout);

        // The delegate listens to the move controller, so a tick dirties layout on its own.
        harness.Tick(0.05);
        Assert.True(layout.NeedsLayout);
    }

    [Fact]
    public void ScaffoldState_NoAnimationAnimator_PlacesTheButtonImmediately()
    {
        BuildContext? context = null;
        using var harness = new Harness(Wrap(new Scaffold(
            body: Capture(c => context = c),
            floatingActionButtonAnimator: FloatingActionButtonAnimator.NoAnimation,
            floatingActionButton: new FloatingActionButton(child: new SizedBox(), onPressed: () => { }))));
        harness.Pump(Viewport);

        Assert.Equal(
            new Rect(new Point(728, 528), new Size(56, 56)),
            RequireGeometry(context).FloatingActionButtonArea!.Value);
    }

    private static ScaffoldGeometry RequireGeometry(BuildContext? context)
    {
        Assert.True(context.HasValue);
        ScaffoldGeometryNotifier notifier = Scaffold.GeometryNotifierMaybeOf(context!.Value)!;
        Assert.NotNull(notifier);
        return notifier.ValueForLayout;
    }

    private static Widget Capture(Action<BuildContext> capture) => new Builder(context =>
    {
        capture(context);
        return new SizedBox();
    });

    private static Widget Wrap(Widget child, MediaQueryData? mediaQuery = null) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                mediaQuery ?? new MediaQueryData(Size: Viewport),
                new Theme(ThemeData.Light, child)));

    private sealed class Harness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public Harness(Widget rootWidget)
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

        public void Update(Widget widget)
        {
            _rootElement.Update(widget);
            _owner.FlushBuild();
        }

        public void Tick(double seconds)
        {
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + seconds));
        }

        /// <summary>Runs the 200ms entrance and the 400ms segue to completion.</summary>
        public void SettleFloatingActionButton()
        {
            Tick(0.05);
            Pump(Viewport);
            Tick(1.0);
            Pump(Viewport);
        }

        public RenderCustomMultiChildLayoutBox RequireLayout()
        {
            var found = new List<RenderCustomMultiChildLayoutBox>();
            Collect(RenderView, found);
            return found.First(layout => layout.Size == Viewport);
        }

        public RenderBox RequireSlot(ScaffoldSlot slot)
        {
            RenderCustomMultiChildLayoutBox layout = RequireLayout();
            for (RenderBox? child = layout.FirstChild; child is not null; child = layout.ChildAfter(child))
            {
                if (Equals(((MultiChildLayoutParentData)child.parentData!).Id, slot))
                {
                    return child;
                }
            }

            throw new InvalidOperationException($"Scaffold slot '{slot}' was not found.");
        }

        public T FindState<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return states.First();
        }

        public void Dispose() => _rootElement.Unmount();

        private static void Collect<T>(RenderObject? root, List<T> found) where T : RenderObject
        {
            if (root is null)
            {
                return;
            }

            if (root is T match)
            {
                found.Add(match);
            }

            root.VisitChildren(child => Collect(child, found));
        }

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state)
            {
                states.Add(state);
            }

            element.VisitChildren(child => CollectStates(child, states));
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) =>
                _renderView = renderView;

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

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;

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
