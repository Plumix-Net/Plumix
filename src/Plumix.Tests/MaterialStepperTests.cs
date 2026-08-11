using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialStepperTests : IDisposable
{
    public MaterialStepperTests()
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
    public void Stepper_DefaultsAndValidation_MatchFlutterSurface()
    {
        var steps = BuildSteps();
        var stepper = new Stepper(steps);

        Assert.Equal(StepperType.Vertical, stepper.Type);
        Assert.Equal(0, stepper.CurrentStep);
        Assert.Equal(Clip.None, stepper.ClipBehavior);
        Assert.Null(stepper.ConnectorThickness);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stepper([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stepper(steps, currentStep: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stepper(steps, stepIconWidth: 23));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stepper(steps, stepIconHeight: 81));
        Assert.Throws<ArgumentException>(() => new Stepper(steps, stepIconWidth: 32, stepIconHeight: 40));
        Assert.Equal(-1, new Stepper(steps, connectorThickness: -1).ConnectorThickness);
        Assert.Equal(-1, new Stepper(steps, elevation: -1).Elevation);
    }

    [Fact]
    public void Stepper_NestedStepper_ThrowsLikeFlutter()
    {
        var nested = new Stepper(
            steps:
            [
                new Step(
                    new Text("Outer"),
                    new Stepper(steps: [new Step(new Text("Inner"), new Text("Inner content"))])),
            ]);

        using var harness = new WidgetRenderHarness(BuildThemed(nested));
        Assert.Throws<InvalidOperationException>(() => harness.Pump(new Size(420, 420)));
    }

    [Fact]
    public void Stepper_Vertical_RendersStateIconsContentAndDefaultControls()
    {
        using var harness = new WidgetRenderHarness(BuildThemed(new Stepper(
            currentStep: 1,
            onStepContinue: () => { },
            onStepCancel: () => { },
            steps:
            [
                new Step(new Text("Complete"), new Text("Old content"), state: StepState.Complete, isActive: true),
                new Step(new Text("Error"), new Text("Current content"), state: StepState.Error, isActive: true),
            ])));

        harness.Pump(new Size(420, 520));
        Assert.NotNull(FindParagraph(harness.RenderView, "Complete"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Error"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Current content"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Continue"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Cancel"));
        Assert.NotNull(FindParagraph(harness.RenderView, "!"));
        Assert.Contains(FindDescendants<RenderCustomPaint>(harness.RenderView), _ => true);
        Assert.Contains(FindDescendants<RenderAlign>(harness.RenderView), align => align.HeightFactor == 1);
    }

    [Fact]
    public void Stepper_DisabledHeaderDoesNotTap_EnabledHeaderDoes()
    {
        var taps = new List<int>();
        using var harness = new WidgetRenderHarness(BuildThemed(new Stepper(
            onStepTapped: taps.Add,
            steps:
            [
                new Step(new Text("Enabled"), new Text("One"), isActive: true),
                new Step(new Text("Disabled"), new Text("Two"), state: StepState.Disabled),
            ])));
        var semantics = harness.PumpAndGetSemantics(new Size(420, 420));
        var tappable = FindNodes(semantics!, node => node.Actions.HasFlag(SemanticsActions.Tap)).ToArray();
        Assert.Single(tappable);
        Assert.True(harness.PerformSemanticsAction(tappable[0].Id, SemanticsActions.Tap));
        Assert.Equal([0], taps);
    }

    [Fact]
    public void Stepper_VerticalHeaderTap_AnimatesStepIntoViewBeforeCallback()
    {
        var controller = new ScrollController();
        bool callbackSawDrivenScroll = false;
        using var harness = new WidgetRenderHarness(BuildThemed(new Stepper(
            controller: controller,
            onStepTapped: _ => callbackSawDrivenScroll = controller.PrimaryPosition?.Activity is DrivenScrollActivity,
            steps:
            [
                new Step(new Text("One"), new SizedBox(height: 80)),
                new Step(new Text("Two"), new SizedBox(height: 80)),
                new Step(new Text("Three"), new SizedBox(height: 80)),
            ])));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 300));
        var headers = FindNodes(semantics!, node => node.Actions.HasFlag(SemanticsActions.Tap)).ToArray();
        Assert.True(headers.Length >= 2);
        Assert.True(harness.PerformSemanticsAction(headers[1].Id, SemanticsActions.Tap));
        Assert.True(callbackSawDrivenScroll);
        Assert.Equal(0, controller.Offset);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.1));
        harness.Pump(new Size(320, 300));
        Assert.True(controller.Offset > 0);
        Assert.IsType<DrivenScrollActivity>(controller.PrimaryPosition!.Activity);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.2));
        harness.Pump(new Size(320, 300));
        Assert.IsType<IdleScrollActivity>(controller.PrimaryPosition.Activity);
    }

    [Fact]
    public void Stepper_UsesSharedImplicitAnimationAndVisibilityPrimitives()
    {
        using var vertical = new WidgetRenderHarness(BuildThemed(new Stepper(steps: BuildSteps())));
        vertical.Pump(new Size(420, 420));
        Assert.True(vertical.FindWidgets<AnimatedCrossFade>().Count >= 2);
        Assert.True(vertical.FindWidgets<AnimatedDefaultTextStyle>().Count >= 2);
        Assert.True(vertical.FindWidgets<AnimatedContainer>().Count >= 2);

        using var horizontal = new WidgetRenderHarness(BuildThemed(new Stepper(
            type: StepperType.Horizontal,
            steps: BuildSteps())));
        horizontal.Pump(new Size(640, 360));
        Assert.Contains(
            horizontal.FindWidgets<AnimatedSize>(),
            size => size.Duration == TimeSpan.FromMilliseconds(200) && size.Curve == Curves.FastOutSlowIn);
        Assert.Equal(2, horizontal.FindWidgets<Visibility>().Count);
        Assert.All(horizontal.FindWidgets<Visibility>(), visibility => Assert.True(visibility.MaintainState));
    }

    [Fact]
    public void Stepper_Horizontal_UsesCustomIconConnectorAndControlsDetails()
    {
        ControlsDetails? captured = null;
        using var harness = new WidgetRenderHarness(BuildThemed(new Stepper(
            type: StepperType.Horizontal,
            currentStep: 1,
            connectorColor: WidgetStateProperty<Color>.ResolveWith(states =>
                states.Contains(WidgetState.Selected) ? Colors.Green : Colors.Gray),
            connectorThickness: 3,
            stepIconWidth: 32,
            stepIconHeight: 32,
            stepIconBuilder: (index, _) => new Text($"I{index}"),
            controlsBuilder: (_, details) =>
            {
                captured = details;
                return new Text("Custom controls");
            },
            steps:
            [
                new Step(new Text("First"), new Text("Panel one"), isActive: true, label: new Text("A")),
                new Step(new Text("Second"), new Text("Panel two"), label: new Text("B")),
            ])));

        harness.Pump(new Size(640, 360));
        Assert.NotNull(FindParagraph(harness.RenderView, "I0"));
        Assert.NotNull(FindParagraph(harness.RenderView, "I1"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Panel one"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Panel two"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Custom controls"));
        Assert.Equal(new ControlsDetails(1, 1), captured);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Math.Abs(box.AdditionalConstraints.MinHeight - 3) < 0.001);
        Assert.Contains(FindDescendants<RenderOffstage>(harness.RenderView), offstage => offstage.Offstage);
    }

    [Fact]
    public void Stepper_PublicGeometryAndStepStyle_MatchFlutterSurface()
    {
        EdgeInsetsGeometry margin = EdgeInsetsGeometry.DirectionalOnly(start: 5, end: 7);
        EdgeInsetsGeometry headerPadding = EdgeInsetsGeometry.DirectionalOnly(start: 11, end: 13);
        EdgeInsetsGeometry contentPadding = EdgeInsetsGeometry.DirectionalOnly(start: 17, end: 19, bottom: 23);
        var iconMargin = new EdgeInsets(2, 3, 4, 5);
        var border = BoxBorder.All(new BorderSide(Colors.Red, 2));
        var gradient = new LinearGradient([Colors.Red, Colors.Blue]);
        var style = new StepStyle(
            Color: Colors.Green,
            ErrorColor: Colors.Yellow,
            ConnectorColor: Colors.Purple,
            ConnectorThickness: 3,
            Border: border,
            Gradient: gradient,
            IndexStyle: new TextStyle(FontSize: 14));
        var stepper = new Stepper(
            BuildSteps(),
            margin: margin,
            headerPadding: headerPadding,
            contentPadding: contentPadding,
            stepIconMargin: iconMargin);

        Assert.Equal(margin, stepper.Margin);
        Assert.Equal(headerPadding, stepper.HeaderPadding);
        Assert.Equal(contentPadding, stepper.ContentPadding);
        Assert.Equal(iconMargin, stepper.StepIconMargin);
        Assert.Same(gradient, style.Gradient);
        Assert.Same(border, style.Border);
        Assert.Equal(4, style.CopyWith(connectorThickness: 4).ConnectorThickness);
        Assert.Equal(Colors.Orange, style.Merge(new StepStyle(Color: Colors.Orange)).Color);

        using var harness = new WidgetRenderHarness(BuildThemed(new Stepper(
            steps:
            [
                new Step(new Text("Styled"), new Text("Body"), isActive: true, stepStyle: style),
            ])));
        harness.Pump(new Size(320, 240));
        BoxDecoration decoration = Assert.IsType<BoxDecoration>(
            Assert.Single(harness.FindWidgets<AnimatedContainer>()).Decoration);
        Assert.Same(gradient, decoration.Gradient);
        Assert.Same(border, decoration.BorderSides);
    }

    [Fact]
    public void Stepper_CircleAndConnectorColors_ReadColorSchemeAndWidgetStates()
    {
        Color lightPrimary = Color.Parse("#FF123456");
        Color lightOnSurface = Color.Parse("#FF654321");
        var lightTheme = ThemeData.Light with
        {
            PrimaryColor = Colors.Orange,
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Primary = lightPrimary,
                OnSurface = lightOnSurface,
            },
        };
        using var light = new WidgetRenderHarness(BuildThemed(new Stepper(
            steps:
            [
                new Step(new Text("Active"), new Text("A"), isActive: true),
                new Step(new Text("Inactive"), new Text("B")),
            ]), lightTheme));
        light.Pump(new Size(360, 360));
        BoxDecoration[] lightCircles = FindCircleDecorations(light);
        Assert.Contains(lightCircles, decoration => decoration.Color == lightPrimary);
        Assert.Contains(lightCircles, decoration => decoration.Color == ApplyOpacity(lightOnSurface, 0.38));

        Color darkSecondary = Color.Parse("#FF234567");
        Color darkBackground = Color.Parse("#FF765432");
        var darkTheme = ThemeData.Dark with
        {
            ColorScheme = ThemeData.Dark.ColorScheme with
            {
                Secondary = darkSecondary,
                Background = darkBackground,
            },
        };
        using var dark = new WidgetRenderHarness(BuildThemed(new Stepper(
            steps:
            [
                new Step(new Text("Active"), new Text("A"), isActive: true),
                new Step(new Text("Inactive"), new Text("B")),
            ]), darkTheme));
        dark.Pump(new Size(360, 360));
        BoxDecoration[] darkCircles = FindCircleDecorations(dark);
        Assert.Contains(darkCircles, decoration => decoration.Color == darkSecondary);
        Assert.Contains(darkCircles, decoration => decoration.Color == darkBackground);

        var resolvedStates = new List<IReadOnlySet<WidgetState>>();
        using var custom = new WidgetRenderHarness(BuildThemed(new Stepper(
            connectorColor: WidgetStateProperty<Color>.ResolveWith(states =>
            {
                resolvedStates.Add(states);
                return states.Contains(WidgetState.Selected) ? Colors.Green : Colors.Gray;
            }),
            steps:
            [
                new Step(new Text("Active"), new Text("A"), isActive: true),
                new Step(new Text("Inactive"), new Text("B")),
            ])));
        custom.Pump(new Size(360, 360));
        Assert.Contains(resolvedStates, states => states.SetEquals([WidgetState.Selected]));
        Assert.Contains(resolvedStates, states => states.SetEquals([WidgetState.Disabled]));
    }

    [Theory]
    [InlineData(false, false, "CONTINUE", "CANCEL")]
    [InlineData(true, false, "Continue", "Cancel")]
    [InlineData(true, true, "Continue", "Cancel")]
    public void Stepper_DefaultControlStyles_UseDirectColorSchemeRoles(
        bool useMaterial3,
        bool dark,
        string continueLabel,
        string cancelLabel)
    {
        ThemeData baseTheme = dark ? ThemeData.Dark : ThemeData.Light;
        Color primary = Color.Parse("#FF0A6B4F");
        Color onPrimary = Color.Parse("#FFF0FFF9");
        Color onSurface = Color.Parse("#FF102019");
        var theme = baseTheme with
        {
            UseMaterial3 = useMaterial3,
            PrimaryColor = Colors.Orange,
            OnPrimaryColor = Colors.Purple,
            OnSurfaceColor = Colors.Yellow,
            ColorScheme = baseTheme.ColorScheme with
            {
                Primary = primary,
                OnPrimary = onPrimary,
                OnSurface = onSurface,
            },
        };
        using var harness = new WidgetRenderHarness(BuildThemed(new Stepper(
            onStepContinue: () => { },
            onStepCancel: () => { },
            steps: [new Step(new Text("One"), new Text("Body"), isActive: true)]), theme));
        harness.Pump(new Size(360, 260));

        Assert.NotNull(FindParagraph(harness.RenderView, continueLabel));
        Assert.NotNull(FindParagraph(harness.RenderView, cancelLabel));
        TextButton continueButton = harness.FindWidgets<TextButton>()[0];
        Assert.Equal(dark ? onSurface : onPrimary, continueButton.Style!.ForegroundColor!.Resolve(MaterialState.None));
        Assert.Equal(dark ? null : primary, continueButton.Style.BackgroundColor!.Resolve(MaterialState.None));
        Assert.Null(continueButton.Style.ForegroundColor.Resolve(MaterialState.Disabled));
        Assert.Null(continueButton.Style.BackgroundColor.Resolve(MaterialState.Disabled));
        Assert.Equal(BorderRadius.Circular(2), continueButton.Style.Shape!.Resolve(MaterialState.None));
    }

    [Fact]
    public void Stepper_DirectionalPaddingAndIconMargin_ResolveLikeFlutter()
    {
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Rtl,
            new MaterialLocalizationsScope(
                DefaultMaterialLocalizations.Instance,
                new Theme(
                    ThemeData.Light,
                    new Stepper(
                        headerPadding: EdgeInsetsGeometry.DirectionalOnly(start: 10, end: 20),
                        stepIconMargin: new EdgeInsets(7, 8, 9, 10),
                        steps: [new Step(new Text("One"), new Text("Body"), isActive: true)])))));
        harness.Pump(new Size(360, 300));

        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(20, 0, 10, 0));
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(24, 0, 67, 24));
    }

    [Fact]
    public void Stepper_IconBuilderReceivesBothSidesOfErrorTransition()
    {
        var states = new List<StepState>();
        StepIconBuilder builder = (_, state) =>
        {
            states.Add(state);
            return state == StepState.Error ? new Text("Custom error") : null;
        };
        using var harness = new WidgetRenderHarness(BuildThemed(new Stepper(
            stepIconBuilder: builder,
            steps: [new Step(new Text("One"), new Text("Body"), state: StepState.Indexed)])));
        harness.Pump(new Size(320, 260));
        states.Clear();

        harness.Update(BuildThemed(new Stepper(
            stepIconBuilder: builder,
            steps: [new Step(new Text("One"), new Text("Body"), state: StepState.Error)])));
        harness.Pump(new Size(320, 260));

        Assert.Contains(StepState.Indexed, states);
        Assert.Contains(StepState.Error, states);
        Assert.NotNull(FindParagraph(harness.RenderView, "Custom error"));
    }

    private static IReadOnlyList<Step> BuildSteps() =>
    [
        new Step(new Text("One"), new Text("Content one")),
        new Step(new Text("Two"), new Text("Content two")),
    ];

    private static Widget BuildThemed(Widget child, ThemeData? theme = null) => new Directionality(
        TextDirection.Ltr,
        new MaterialLocalizationsScope(
            DefaultMaterialLocalizations.Instance,
            new Theme(theme ?? ThemeData.Light, child)));

    private static BoxDecoration[] FindCircleDecorations(WidgetRenderHarness harness) => harness
        .FindWidgets<AnimatedContainer>()
        .Select(container => container.Decoration)
        .OfType<BoxDecoration>()
        .Where(decoration => decoration.Shape == BoxShape.Circle)
        .ToArray();

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
        color.R,
        color.G,
        color.B);

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T typed) result.Add(typed);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindNode(SemanticsNode node, Func<SemanticsNode, bool> predicate) =>
        FindNodes(node, predicate).FirstOrDefault();

    private static IEnumerable<SemanticsNode> FindNodes(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node)) yield return node;
        foreach (var child in node.Children)
        foreach (var result in FindNodes(child, predicate))
            yield return result;
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

        public bool PerformSemanticsAction(int id, SemanticsActions action) =>
            _pipeline.SemanticsOwner.PerformAction(id, action);

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var result = new List<T>();
            Visit(_rootElement, result);
            return result;
        }

        public void Dispose() => _rootElement.Unmount();

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

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            public void UpdateRoot(Widget widget) => Update(widget);
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = Assert.IsAssignableFrom<RenderBox>(child);
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
