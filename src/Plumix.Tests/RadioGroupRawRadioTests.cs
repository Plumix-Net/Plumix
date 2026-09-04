using Avalonia;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/focus_traversal.dart
namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RadioGroupRawRadioTests : IDisposable
{
    public RadioGroupRawRadioTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void RawRadio_Constructor_RequiresRegistryWhenEnabled()
    {
        Assert.Throws<ArgumentException>(() => new RawRadio<string>(
            value: "a",
            mouseCursor: WidgetStateProperty<MouseCursor>.All(SystemMouseCursors.Click),
            toggleable: false,
            focusNode: new FocusNode(),
            autofocus: false,
            groupRegistry: null,
            enabled: true,
            builder: (_, _) => new SizedBox(width: 20, height: 20)));
    }

    [Fact]
    public void RawRadio_RegistersAnimatesAndUnregisters_WithSourceStateSurface()
    {
        var registry = new TestRegistry<string>("a");
        RawRadioState<string>? capturedState = null;
        using (var harness = new WidgetRenderHarness(
                   new RawRadio<string>(
                       value: "a",
                       mouseCursor: WidgetStateProperty<MouseCursor>.ResolveWith(states =>
                           states.Contains(WidgetState.Disabled)
                               ? SystemMouseCursors.Basic
                               : SystemMouseCursors.Click),
                       toggleable: true,
                       focusNode: new FocusNode(),
                       autofocus: false,
                       groupRegistry: registry,
                       enabled: true,
                       builder: (_, state) =>
                       {
                           capturedState = state;
                           return new SizedBox(width: 20, height: 20);
                       })))
        {
            harness.Pump(new Size(80, 40));

            RawRadioState<string> state = Assert.IsType<RawRadioState<string>>(capturedState);
            Assert.Single(registry.Clients);
            Assert.True(state.Selected);
            Assert.Equal(1.0, state.Position);
            Assert.Contains(WidgetState.Selected, state.States);

            registry.SetGroupValue("b");
            state.AnimateToValue();
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(
                TimeSpan.FromSeconds(Scheduler.CurrentSeconds) + TimeSpan.FromMilliseconds(250));
            harness.Pump(new Size(80, 40));

            Assert.False(state.Selected);
            Assert.Equal(0.0, state.Position);
        }

        Assert.Empty(registry.Clients);
    }

    [Fact]
    public void RadioGroup_ArrowKeysSelectNextEnabledAndWrap()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new RadioGroupProbe(disableMiddle: true))));
        harness.Pump(new Size(240, 120));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        state.FirstFocus.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        harness.Pump(new Size(240, 120));

        Assert.Equal("third", state.GroupValue);
        Assert.True(state.ThirdFocus.HasFocus);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        harness.Pump(new Size(240, 120));

        Assert.Equal("first", state.GroupValue);
        Assert.True(state.FirstFocus.HasFocus);
    }

    [Fact]
    public void RadioGroup_ArrowKeysFollowGeometryReadingOrder()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new RadioGroupProbe(geometryOrder: true))));
        harness.Pump(new Size(240, 120));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        state.FirstFocus.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        harness.Pump(new Size(240, 120));

        Assert.Equal("third", state.GroupValue);
        Assert.True(state.ThirdFocus.HasFocus);
    }

    [Fact]
    public void RadioGroup_ArrowKeysHonorRtlReadingOrder()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Rtl,
                    child: new RadioGroupProbe(rtlOrder: true))));
        harness.Pump(new Size(240, 120));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        state.FirstFocus.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        harness.Pump(new Size(240, 120));

        Assert.Equal("third", state.GroupValue);
        Assert.True(state.ThirdFocus.HasFocus);
    }

    [Fact]
    public void RadioGroup_SpaceTogglesOnlyToggleableSelectedRadio()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new RadioGroupProbe(toggleable: true))));
        harness.Pump(new Size(240, 120));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        state.FirstFocus.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space)));
        harness.Pump(new Size(240, 120));

        Assert.Null(state.GroupValue);
    }

    [Fact]
    public void RadioGroup_ShortcutsFallThroughWhenNonRadioDescendantHasFocus()
    {
        int outerShortcutCount = 0;
        string? groupValue = "first";
        var radioFocus = new FocusNode();
        var otherFocus = new FocusNode();
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Shortcuts(
                        shortcuts: new Dictionary<ShortcutActivator, Intent>
                        {
                            [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] = new VoidCallbackIntent(
                                () => outerShortcutCount += 1),
                        },
                        child: new RadioGroup<string>(
                            groupValue: groupValue,
                            onChanged: value => groupValue = value,
                            child: new Column(
                                children:
                                [
                                    new Radio<string>(value: "first", focusNode: radioFocus),
                                    new Focus(
                                        focusNode: otherFocus,
                                        child: new SizedBox(width: 20, height: 20)),
                                ]))))));
        harness.Pump(new Size(240, 120));

        otherFocus.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.Equal(1, outerShortcutCount);

        radioFocus.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.Equal(1, outerShortcutCount);
        Assert.Equal("first", groupValue);
    }

    [Fact]
    public void RadioGroup_TabTraversalExposesOnlySelectedRadio()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Column(
                        children:
                        [
                            new RadioGroupProbe(),
                            new Focus(
                                focusNode: new FocusNode(),
                                child: new SizedBox(width: 20, height: 20)),
                        ]))));
        harness.Pump(new Size(240, 160));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.True(state.FirstFocus.HasFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.False(state.SecondFocus.HasFocus);
        Assert.False(state.ThirdFocus.HasFocus);
    }

    [Fact]
    public void RadioGroup_TabTraversalUsesFirstRadioInReadingOrderWhenUnselected()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new RadioGroupProbe(
                        geometryOrder: true,
                        initialGroupValue: null))));
        harness.Pump(new Size(240, 120));
        RadioGroupProbeState state = harness.FindState<RadioGroupProbeState>();

        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));

        Assert.True(state.ThirdFocus.HasFocus);
        Assert.False(state.FirstFocus.HasFocus);
        Assert.False(state.SecondFocus.HasFocus);
    }

    [Fact]
    public void FocusTraversalGroup_NestedPoliciesSortThenFlattenTheirMembers()
    {
        var first = new FocusNode();
        var nestedFirst = new FocusNode();
        var nestedSecond = new FocusNode();
        var last = new FocusNode();
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new FocusTraversalGroup(
                    policy: new WidgetOrderTraversalPolicy(),
                    child: new Column(
                        children:
                        [
                            new Focus(
                                focusNode: first,
                                child: new SizedBox(width: 20, height: 20)),
                            new FocusTraversalGroup(
                                policy: new ReverseTraversalPolicy(),
                                child: new Column(
                                    children:
                                    [
                                        new Focus(
                                            focusNode: nestedFirst,
                                            child: new SizedBox(width: 20, height: 20)),
                                        new Focus(
                                            focusNode: nestedSecond,
                                            child: new SizedBox(width: 20, height: 20)),
                                    ])),
                            new Focus(
                                focusNode: last,
                                child: new SizedBox(width: 20, height: 20)),
                        ]))));
        harness.Pump(new Size(240, 120));

        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.True(first.HasFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.True(nestedSecond.HasFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.True(nestedFirst.HasFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.True(last.HasFocus);
    }

    [Fact]
    public void RadioGroupAndRawRadio_ExposeGroupAndCheckedSemantics()
    {
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new RadioGroup<string>(
                    groupValue: "a",
                    onChanged: _ => { },
                    child: new RawRadioFromGroup(value: "a"))));

        SemanticsNode root = Assert.IsType<SemanticsNode>(
            harness.PumpAndGetSemantics(new Size(80, 40)));
        SemanticsNode group = Assert.Single(FindNodes(root, node => node.Role == SemanticsRole.RadioGroup));
        SemanticsNode radio = Assert.Single(FindNodes(group, node =>
            node.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup)));

        Assert.True(radio.Flags.HasFlag(SemanticsFlags.HasCheckedState));
        Assert.True(radio.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.True(radio.Flags.HasFlag(SemanticsFlags.IsEnabled));
    }

    [Fact]
    public void RadioGroup_RejectsMultipleClientsWithSelectedValue()
    {
        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(
            new Theme(
                data: ThemeData.Light,
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new RadioGroup<string>(
                        groupValue: "same",
                        onChanged: _ => { },
                        child: new Column(
                            children:
                            [
                                new Radio<string>(value: "same"),
                                new Radio<string>(value: "same"),
                            ]))))));
    }

    private static bool PumpFocus(Func<bool> action)
    {
        bool result = action();
        Scheduler.FlushMicrotasks();
        return result;
    }

    private static void PumpFocus(Action action)
    {
        action();
        Scheduler.FlushMicrotasks();
    }

    private static List<SemanticsNode> FindNodes(
        SemanticsNode root,
        Func<SemanticsNode, bool> predicate)
    {
        var result = new List<SemanticsNode>();
        if (predicate(root))
        {
            result.Add(root);
        }

        foreach (SemanticsNode child in root.Children)
        {
            result.AddRange(FindNodes(child, predicate));
        }

        return result;
    }

    private sealed class TestRegistry<T> : RadioGroupRegistry<T>
    {
        private T? _groupValue;

        public TestRegistry(T? groupValue)
        {
            _groupValue = groupValue;
        }

        public List<RadioClient<T>> Clients { get; } = [];

        public override T? GroupValue => _groupValue;

        public override Action<T?> OnChanged => value => _groupValue = value;

        public void SetGroupValue(T? value)
        {
            _groupValue = value;
        }

        public override void RegisterClient(RadioClient<T> radio)
        {
            Clients.Add(radio);
        }

        public override void UnregisterClient(RadioClient<T> radio)
        {
            Clients.Remove(radio);
        }
    }

    private sealed class ReverseTraversalPolicy : DirectionalFocusTraversalPolicy
    {
        public override IEnumerable<FocusNode> SortDescendants(
            IEnumerable<FocusNode> descendants,
            FocusNode currentNode)
        {
            return descendants.Reverse().ToList();
        }
    }

    private sealed class RawRadioFromGroup : StatelessWidget
    {
        public RawRadioFromGroup(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override Widget Build(BuildContext context)
        {
            RadioGroupRegistry<string> registry = RadioGroup<string>.MaybeOf(context)
                                                  ?? throw new InvalidOperationException();
            return new RawRadio<string>(
                value: Value,
                mouseCursor: WidgetStateProperty<MouseCursor>.All(SystemMouseCursors.Click),
                toggleable: false,
                focusNode: new FocusNode(),
                autofocus: false,
                groupRegistry: registry,
                enabled: true,
                builder: (_, _) => new SizedBox(width: 20, height: 20));
        }
    }

    private sealed class RadioGroupProbe : StatefulWidget
    {
        public RadioGroupProbe(
            bool disableMiddle = false,
            bool toggleable = false,
            bool geometryOrder = false,
            bool rtlOrder = false,
            string? initialGroupValue = "first")
        {
            DisableMiddle = disableMiddle;
            Toggleable = toggleable;
            GeometryOrder = geometryOrder;
            RtlOrder = rtlOrder;
            InitialGroupValue = initialGroupValue;
        }

        public bool DisableMiddle { get; }

        public bool Toggleable { get; }

        public bool GeometryOrder { get; }

        public bool RtlOrder { get; }

        public string? InitialGroupValue { get; }

        public override State CreateState()
        {
            return new RadioGroupProbeState();
        }
    }

    private sealed class RadioGroupProbeState : State
    {
        public string? GroupValue { get; private set; }

        public FocusNode FirstFocus { get; } = new();

        public FocusNode SecondFocus { get; } = new();

        public FocusNode ThirdFocus { get; } = new();

        private RadioGroupProbe CurrentWidget => (RadioGroupProbe)StateWidget;

        public override void InitState()
        {
            GroupValue = CurrentWidget.InitialGroupValue;
        }

        public override Widget Build(BuildContext context)
        {
            if (CurrentWidget.GeometryOrder)
            {
                FirstFocus.TraversalRect = new Rect(0, 80, 20, 20);
                SecondFocus.TraversalRect = new Rect(0, 40, 20, 20);
                ThirdFocus.TraversalRect = new Rect(0, 0, 20, 20);
            }
            else if (CurrentWidget.RtlOrder)
            {
                FirstFocus.TraversalRect = new Rect(0, 0, 20, 20);
                SecondFocus.TraversalRect = new Rect(40, 0, 20, 20);
                ThirdFocus.TraversalRect = new Rect(80, 0, 20, 20);
            }

            return new RadioGroup<string>(
                groupValue: GroupValue,
                onChanged: value => SetState(() => GroupValue = value),
                child: new Column(
                    children:
                    [
                        new Radio<string>(
                            value: "first",
                            toggleable: CurrentWidget.Toggleable,
                            focusNode: FirstFocus),
                        new Radio<string>(
                            value: "second",
                            enabled: !CurrentWidget.DisableMiddle,
                            focusNode: SecondFocus),
                        new Radio<string>(
                            value: "third",
                            focusNode: ThirdFocus),
                    ]));
        }

        public override void Dispose()
        {
            FirstFocus.Dispose();
            SecondFocus.Dispose();
            ThirdFocus.Dispose();
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            Widget appRoot = new Actions(
                actions: new Dictionary<Type, FlutterAction>
                {
                    [typeof(VoidCallbackIntent)] = new VoidCallbackAction(),
                },
                child: rootWidget);
            _rootElement = new HarnessRootElement(RenderView, appRoot);
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

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public T FindState<T>() where T : State
        {
            return FindState<T>(_rootElement)
                   ?? throw new InvalidOperationException($"State {typeof(T).Name} not found.");
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private static T? FindState<T>(Element element) where T : State
        {
            if (element is StatefulElement { State: T state })
            {
                return state;
            }

            T? result = null;
            element.VisitChildren(child => result ??= FindState<T>(child));
            return result;
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            public override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            public override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
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
