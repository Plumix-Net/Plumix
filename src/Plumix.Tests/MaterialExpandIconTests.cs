using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialExpandIconTests : IDisposable
{
    private static readonly Color Black54 = Color.FromArgb(0x8A, 0x00, 0x00, 0x00);
    private static readonly Color Black38 = Color.FromArgb(0x61, 0x00, 0x00, 0x00);
    private static readonly Color White60 = Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF);
    private static readonly Color White38 = Color.FromArgb(0x61, 0xFF, 0xFF, 0xFF);

    public MaterialExpandIconTests()
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
    public void DefaultsAndPublicSurface_MatchFlutterContract()
    {
        Action<bool> callback = _ => { };
        var icon = new ExpandIcon(callback);

        Assert.False(icon.IsExpanded);
        Assert.Equal(24.0, icon.Size);
        Assert.Same(callback, icon.OnPressed);
        Assert.Equal(EdgeInsetsGeometry.All(8.0), icon.Padding);
        Assert.Null(icon.Color);
        Assert.Null(icon.DisabledColor);
        Assert.Null(icon.ExpandedColor);
        Assert.Null(icon.SplashColor);
        Assert.Null(icon.HighlightColor);
        Assert.Equal(0.0, new ExpandIcon(callback, size: 0.0).Size);
        Assert.Equal(48.0, new ExpandIcon(callback, size: 48.0).Size);
    }

    [Theory]
    [InlineData(Brightness.Light)]
    [InlineData(Brightness.Dark)]
    public void EnabledDefaultColor_FollowsBrightness(Brightness brightness)
    {
        var theme = new ThemeData(brightness: brightness);
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpandIcon(_ => { }), theme));

        harness.Pump(new Size(80, 80));

        Assert.Equal(
            brightness == Brightness.Light ? Black54 : White60,
            LastIconColor(harness));
    }

    [Theory]
    [InlineData(false, Brightness.Light)]
    [InlineData(false, Brightness.Dark)]
    [InlineData(true, Brightness.Light)]
    [InlineData(true, Brightness.Dark)]
    public void DisabledDefaultColor_FollowsIconButtonMaterialPolicy(bool useMaterial3, Brightness brightness)
    {
        Color onSurface = Color.Parse("#FF13579B");
        ColorScheme? colorScheme = useMaterial3
            ? ColorScheme.Light(brightness: brightness, onSurface: onSurface)
            : null;
        var theme = new ThemeData(
            brightness: brightness,
            colorScheme: colorScheme,
            useMaterial3: useMaterial3);
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpandIcon(onPressed: null), theme));

        harness.Pump(new Size(80, 80));

        Color expected = useMaterial3
            ? MaterialButtonCore.ApplyOpacity(onSurface, 0.38)
            : brightness == Brightness.Light ? Black38 : White38;
        Assert.Equal(expected, LastIconColor(harness));
    }

    [Fact]
    public void WidgetColors_RespectExpandedAndDisabledPrecedence()
    {
        Color collapsed = Colors.Indigo;
        Color expanded = Colors.Teal;
        Color disabled = Colors.Cyan;
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpandIcon(
            onPressed: _ => { },
            color: collapsed,
            expandedColor: expanded)));

        harness.Pump(new Size(80, 80));
        Assert.Equal(collapsed, LastIconColor(harness));

        harness.Update(BuildThemed(new ExpandIcon(
            onPressed: _ => { },
            isExpanded: true,
            color: collapsed,
            expandedColor: expanded)));
        harness.Pump(new Size(80, 80));
        Assert.Equal(expanded, LastIconColor(harness));

        harness.Update(BuildThemed(new ExpandIcon(
            onPressed: null,
            isExpanded: true,
            color: collapsed,
            expandedColor: expanded,
            disabledColor: disabled)));
        harness.Pump(new Size(80, 80));
        Assert.Equal(disabled, LastIconColor(harness));
    }

    [Fact]
    public void CallbackReceivesCurrentState_AndUpdatesDoNotInvokeIt()
    {
        var received = new List<bool>();
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpandIcon(received.Add)));
        SemanticsNode semantics = harness.PumpAndGetSemantics(new Size(80, 80))!;
        SemanticsNode button = Assert.Single(
            FindNodes(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));

        Assert.True(harness.PerformSemanticsAction(button.Id, SemanticsActions.Tap));
        Assert.Equal([false], received);

        harness.Update(BuildThemed(new ExpandIcon(received.Add, isExpanded: true)));
        semantics = harness.PumpAndGetSemantics(new Size(80, 80))!;
        Assert.Equal([false], received);
        button = Assert.Single(FindNodes(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));
        Assert.True(harness.PerformSemanticsAction(button.Id, SemanticsActions.Tap));
        Assert.Equal([false, true], received);
    }

    [Fact]
    public void RotationTransition_UsesHalfTurnFastOutSlowInAnimation()
    {
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpandIcon(_ => { })));
        harness.Pump(new Size(80, 80));
        RotationTransition rotation = Assert.Single(harness.FindWidgets<RotationTransition>());
        Assert.Equal(0.0, rotation.Turns.Value, precision: 6);

        harness.Update(BuildThemed(new ExpandIcon(_ => { }, isExpanded: true)));
        rotation = Assert.Single(harness.FindWidgets<RotationTransition>());
        Assert.Equal(0.0, rotation.Turns.Value, precision: 6);

        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.1));
        harness.Pump(new Size(80, 80));
        rotation = Assert.Single(harness.FindWidgets<RotationTransition>());
        Assert.InRange(rotation.Turns.Value, 0.0, 0.5);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.25));
        harness.Pump(new Size(80, 80));
        rotation = Assert.Single(harness.FindWidgets<RotationTransition>());
        Assert.Equal(0.5, rotation.Turns.Value, precision: 6);

        harness.Update(BuildThemed(new ExpandIcon(_ => { }, isExpanded: false)));
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.25));
        harness.Pump(new Size(80, 80));
        rotation = Assert.Single(harness.FindWidgets<RotationTransition>());
        Assert.Equal(0.0, rotation.Turns.Value, precision: 6);
    }

    [Fact]
    public void InitiallyExpanded_StartsAtHalfTurnWithoutAnimation()
    {
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpandIcon(_ => { }, isExpanded: true)));

        harness.Pump(new Size(80, 80));

        RotationTransition rotation = Assert.Single(harness.FindWidgets<RotationTransition>());
        Assert.Equal(0.5, rotation.Turns.Value, precision: 6);
        Assert.Equal(AnimationStatus.Completed, rotation.Turns.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_ExposeActionSpecificLocalizedHint(bool useMaterial3)
    {
        var theme = new ThemeData(useMaterial3: useMaterial3);
        using var harness = new WidgetRenderHarness(BuildThemed(new ExpandIcon(_ => { }), theme));
        SemanticsNode semantics = harness.PumpAndGetSemantics(new Size(80, 80))!;

        Assert.NotNull(FindNode(semantics, node => node.OnTapHint == "Expand"));
        Assert.Null(FindNode(semantics, node => node.Hint == "Expand"));
        Assert.NotNull(FindNode(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));

        harness.Update(BuildThemed(new ExpandIcon(_ => { }, isExpanded: true), theme));
        semantics = harness.PumpAndGetSemantics(new Size(80, 80))!;
        Assert.NotNull(FindNode(semantics, node => node.OnTapHint == "Collapse"));

        harness.Update(BuildThemed(new ExpandIcon(onPressed: null), theme));
        semantics = harness.PumpAndGetSemantics(new Size(80, 80))!;
        Assert.Null(FindNode(semantics, node => node.OnTapHint is not null));
        Assert.Null(FindNode(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public void DirectionalPadding_ResolvesThroughIconButton()
    {
        EdgeInsetsGeometry padding = EdgeInsetsGeometry.DirectionalOnly(
            start: 3.0,
            top: 2.0,
            end: 7.0,
            bottom: 4.0);
        var theme = new ThemeData(useMaterial3: false);
        using var harness = new WidgetRenderHarness(BuildThemed(
            new ExpandIcon(_ => { }, padding: padding),
            theme,
            TextDirection.Rtl));

        harness.Pump(new Size(80, 80));

        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            renderPadding => renderPadding.Padding == new Thickness(7.0, 2.0, 3.0, 4.0));
    }

    [Fact]
    public void ZeroAreaLayout_DoesNotCrash()
    {
        using var harness = new WidgetRenderHarness(BuildThemed(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new ExpandIcon(_ => { }))));

        harness.Pump(new Size(80, 80));

        Assert.Contains(
            FindDescendants<RenderTransform>(harness.RenderView),
            transform => transform.Size == default);
    }

    private static Color LastIconColor(WidgetRenderHarness harness)
    {
        IReadOnlyList<MaterialButtonCore> materialButtons = harness.FindWidgets<MaterialButtonCore>();
        if (materialButtons.Count > 0)
        {
            MaterialButtonCore button = materialButtons[^1];
            MaterialState states = button.OnPressed is null
                ? MaterialState.Disabled
                : MaterialState.None;
            return button.Style.ResolveIconColor(states)!.Value;
        }

        return harness.FindWidgets<IconTheme>()
            .Select(iconTheme => iconTheme.Data.Color)
            .Last(color => color.HasValue)!.Value;
    }

    private static Widget BuildThemed(
        Widget child,
        ThemeData? theme = null,
        TextDirection direction = TextDirection.Ltr)
    {
        return new Directionality(
            direction,
            new MaterialLocalizationsScope(
                DefaultMaterialLocalizations.Instance,
                new Theme(theme ?? ThemeData.Light, child)));
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T typed)
        {
            result.Add(typed);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindNode(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        return FindNodes(node, predicate).FirstOrDefault();
    }

    private static IEnumerable<SemanticsNode> FindNodes(
        SemanticsNode node,
        Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            yield return node;
        }

        foreach (SemanticsNode child in node.Children)
        foreach (SemanticsNode result in FindNodes(child, predicate))
        {
            yield return result;
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
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public string Dump() => _pipeline.SemanticsOwner.DebugDumpTree();

        public void Update(Widget widget)
        {
            _rootElement.UpdateRoot(widget);
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

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public bool PerformSemanticsAction(int id, SemanticsActions action)
        {
            return _pipeline.SemanticsOwner.PerformAction(id, action);
        }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var result = new List<T>();
            Visit(_rootElement, result);
            return result;
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private static void Visit<T>(Element element, List<T> result) where T : Widget
        {
            if (element.Widget is T widget)
            {
                result.Add(widget);
            }

            element.VisitChildren(child => Visit(child, result));
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

            public void UpdateRoot(Widget widget)
            {
                Update(widget);
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
                _renderView.Child = Assert.IsAssignableFrom<RenderBox>(child);
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
