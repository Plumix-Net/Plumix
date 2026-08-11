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
public sealed class MaterialSegmentedButtonsTests
{
    [Fact]
    public void ToggleButtons_ValidatesParallelListsAndFocusNodesAtBuild()
    {
        Assert.Throws<ArgumentException>(() => new ToggleButtons(
            children: [new Text("One")],
            isSelected: []));
        Assert.Throws<ArgumentException>(() => new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ToggleButtons(
                children: [new Text("One")],
                isSelected: [false],
                focusNodes: []))));
    }

    [Fact]
    public void ToggleButtons_DefaultsResolveSelectedUnselectedAndDisabledStates()
    {
        var theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Primary = Colors.DarkGreen,
                OnSurface = Colors.DarkSlateBlue,
                Surface = Colors.Beige,
            },
        };
        using var enabled = new WidgetRenderHarness(Wrap(
            theme,
            new ToggleButtons(
                children: [new Text("One"), new Text("Two")],
                isSelected: [true, false],
                onPressed: _ => { })));
        enabled.Pump(new Size(320, 120));

        Assert.Equal(Colors.DarkGreen,
            Assert.IsType<SolidColorBrush>(FindParagraph(enabled.RenderView, "One")!.Foreground).Color);
        Assert.Equal(NavigationSurfaceUtilities.WithOpacity(Colors.DarkSlateBlue, 0.87),
            Assert.IsType<SolidColorBrush>(FindParagraph(enabled.RenderView, "Two")!.Foreground).Color);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(enabled.RenderView),
            box => box.Decoration.Color == NavigationSurfaceUtilities.WithOpacity(Colors.DarkGreen, 0.12));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(enabled.RenderView),
            box => box.AdditionalConstraints.MinWidth == 48
                   && box.AdditionalConstraints.MinHeight == 48);

        using var disabled = new WidgetRenderHarness(Wrap(
            theme,
            new ToggleButtons(
                children: [new Text("Disabled")],
                isSelected: [true])));
        disabled.Pump(new Size(320, 120));
        Assert.Equal(NavigationSurfaceUtilities.WithOpacity(Colors.DarkSlateBlue, 0.38),
            Assert.IsType<SolidColorBrush>(FindParagraph(disabled.RenderView, "Disabled")!.Foreground).Color);
    }

    [Fact]
    public void ToggleButtons_TapThemePrecedenceAndCheckedSemanticsAreWired()
    {
        int? pressed = null;
        var theme = ThemeData.Light with
        {
            ToggleButtonsTheme = new ToggleButtonsThemeData(
                Color: Colors.Purple,
                SelectedColor: Colors.Orange,
                FillColor: Colors.DarkGreen,
                BorderWidth: 3,
                BorderRadius: BorderRadius.Circular(12)),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new ToggleButtons(
                children: [new Text("One"), new Text("Two")],
                isSelected: [false, true],
                onPressed: index => pressed = index,
                selectedColor: Colors.Gold)));
        harness.Pump(new Size(320, 120));

        Tap(harness.RenderView, new Point(24, 24), 201);
        Assert.Equal(0, pressed);
        Assert.Equal(Colors.Purple,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "One")!.Foreground).Color);
        Assert.Equal(Colors.Gold,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Two")!.Foreground).Color);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));
        var checkedNode = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.NotNull(checkedNode);
        Assert.True(checkedNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(checkedNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void ToggleButtons_StatefulFillAndAdjacentSelectedBorderResolveByState()
    {
        var statefulFill = MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled)) return Colors.Orange;
            if (states.HasFlag(MaterialState.Selected)) return Colors.DarkGreen;
            return Colors.SteelBlue;
        });
        var theme = ThemeData.Light with
        {
            ToggleButtonsTheme = new ToggleButtonsThemeData(
                FillColor: statefulFill,
                BorderColor: Colors.Purple,
                SelectedBorderColor: Colors.Gold,
                DisabledBorderColor: Colors.Orange),
        };
        using var enabled = new WidgetRenderHarness(Wrap(
            theme,
            new ToggleButtons(
                children: [new Text("One"), new Text("Two"), new Text("Three")],
                isSelected: [true, false, false],
                onPressed: _ => { })));
        enabled.Pump(new Size(360, 120));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(enabled.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(enabled.RenderView),
            box => box.Decoration.Color == Colors.SteelBlue);
        var borders = FindDescendants<RenderBox>(enabled.RenderView)
            .Where(renderBox => renderBox.GetType().Name == "RenderSelectToggleButton")
            .ToList();
        Assert.Equal(3, borders.Count);
        Assert.Equal(Colors.Gold, Property<BorderSide>(borders[1], "LeadingBorderSide").Color);
        Assert.Equal(Colors.Purple, Property<BorderSide>(borders[1], "BorderSide").Color);
        Assert.Equal(BorderStyle.None, Property<BorderSide>(borders[1], "TrailingBorderSide").Style);

        using var disabled = new WidgetRenderHarness(Wrap(
            theme,
            new ToggleButtons(
                children: [new Text("Disabled")],
                isSelected: [true])));
        disabled.Pump(new Size(200, 80));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(disabled.RenderView),
            box => box.Decoration.Color == Colors.Orange);
    }

    [Fact]
    public void ToggleButtons_TapTargetPaddingIsCrossAxisOnlyAndBordersCanBeSuppressed()
    {
        var tight = BoxConstraints.Tight(new Size(20, 20));
        using var horizontal = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ToggleButtons(
                children: [new SizedBox()],
                isSelected: [false],
                onPressed: _ => { },
                constraints: tight,
                renderBorder: false,
                direction: Axis.Horizontal)));
        horizontal.Pump(new Size(120, 120));
        RenderBox horizontalPadding = Assert.Single(
            FindDescendants<RenderBox>(horizontal.RenderView),
            renderBox => renderBox.GetType().Name == "RenderToggleButtonInputPadding");
        Assert.Equal(new Size(20, 48), horizontalPadding.Size);
        RenderBox horizontalBorder = Assert.Single(
            FindDescendants<RenderBox>(horizontal.RenderView),
            renderBox => renderBox.GetType().Name == "RenderSelectToggleButton");
        Assert.Equal(BorderStyle.None, Property<BorderSide>(horizontalBorder, "LeadingBorderSide").Style);

        using var vertical = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ToggleButtons(
                children: [new SizedBox()],
                isSelected: [false],
                onPressed: _ => { },
                constraints: tight,
                renderBorder: false,
                direction: Axis.Vertical)));
        vertical.Pump(new Size(120, 120));
        RenderBox verticalPadding = Assert.Single(
            FindDescendants<RenderBox>(vertical.RenderView),
            renderBox => renderBox.GetType().Name == "RenderToggleButtonInputPadding");
        Assert.Equal(new Size(48, 20), verticalPadding.Size);
    }

    [Fact]
    public void ToggleButtons_PreservesEllipticalCornersAndTreatsAZeroAxisAsSquare()
    {
        var radius = new BorderRadius(
            Radius.Elliptical(12, 0),
            Radius.Elliptical(16, 8),
            Radius.Elliptical(10, 6),
            Radius.Elliptical(0, 14));
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ToggleButtons(
                children: [new Text("One")],
                isSelected: [true],
                onPressed: _ => { },
                borderRadius: radius)));
        harness.Pump(new Size(160, 80));

        RenderBox border = Assert.Single(
            FindDescendants<RenderBox>(harness.RenderView),
            renderBox => renderBox.GetType().Name == "RenderSelectToggleButton");
        var actual = Property<BorderRadius>(border, "BorderRadius");
        Assert.Equal(Radius.Elliptical(12, 0), actual.TopLeftRadius);
        Assert.Equal(Radius.Elliptical(16, 8), actual.TopRightRadius);
        Assert.Equal(Radius.Elliptical(10, 6), actual.BottomRightRadius);
        Assert.Equal(Radius.Elliptical(0, 14), actual.BottomLeftRadius);
    }

    [Fact]
    public void SegmentedButton_ValidatesSegmentsAndSelectionContract()
    {
        Assert.Throws<ArgumentException>(() => new ButtonSegment<int>(0));
        Assert.Throws<ArgumentException>(() => new SegmentedButton<int>(
            segments: [],
            selected: new HashSet<int> { 0 }));
        Assert.Throws<ArgumentException>(() => new SegmentedButton<int>(
            segments: Segments(),
            selected: new HashSet<int>()));
        Assert.Throws<ArgumentException>(() => new SegmentedButton<int>(
            segments: Segments(),
            selected: new HashSet<int> { 0, 1 }));
        Assert.Throws<ArgumentException>(() => new SegmentedButton<int>(
            segments:
            [
                new ButtonSegment<int>(0, label: new Text("A")),
                new ButtonSegment<int>(0, label: new Text("B")),
            ],
            selected: new HashSet<int> { 0 }));

        using var toggleZero = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ToggleButtons(
                children: [new Text("One")],
                isSelected: [false],
                onPressed: _ => { })));
        toggleZero.Pump(new Size(0, 0));

        using var segmentedZero = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SegmentedButton<int>(
                segments: [new ButtonSegment<int>(0, label: new Text("One"))],
                selected: new HashSet<int> { 0 },
                onSelectionChanged: _ => { })));
        segmentedZero.Pump(new Size(0, 0));
    }

    [Fact]
    public void SegmentedButton_DefaultsUseM3TokensSelectedIconAndDisabledState()
    {
        var theme = ThemeData.Light with
        {
            SecondaryContainerColor = Colors.DarkGreen,
            OnSecondaryContainerColor = Colors.Gold,
            OnSurfaceColor = Colors.DarkSlateBlue,
            OutlineColor = Colors.Orange,
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new SegmentedButton<int>(
                segments: Segments(disableSecond: true),
                selected: new HashSet<int> { 0 },
                onSelectionChanged: _ => { })));
        harness.Pump(new Size(360, 120));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == Colors.DarkGreen);
        Assert.Equal(Colors.Gold,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "One")!.Foreground).Color);
        Assert.Equal(NavigationSurfaceUtilities.WithOpacity(Colors.DarkSlateBlue, 0.38),
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Two")!.Foreground).Color);
        Assert.NotNull(FindParagraphByCodePoint(harness.RenderView, Icons.Check.CodePoint));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 40);

        var semantics = harness.PumpAndGetSemantics(new Size(360, 120));
        Assert.NotNull(FindSemantics(semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsSelected)
                    && node.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup)));
    }

    [Fact]
    public void SegmentedButton_ExclusiveMultiAndEmptySelectionRulesMatchFlutter()
    {
        IReadOnlySet<int>? update = null;
        using var exclusive = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SegmentedButton<int>(
                segments: Segments(),
                selected: new HashSet<int> { 0 },
                onSelectionChanged: value => update = value)));
        exclusive.Pump(new Size(360, 120));

        Tap(exclusive.RenderView, new Point(90, 24), 202);
        Assert.NotNull(update);
        Assert.True(update!.SetEquals([1]));
        update = null;
        Tap(exclusive.RenderView, new Point(24, 24), 203);
        Assert.Null(update);

        using var multi = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SegmentedButton<int>(
                segments: Segments(),
                selected: new HashSet<int> { 0 },
                multiSelectionEnabled: true,
                onSelectionChanged: value => update = value)));
        multi.Pump(new Size(360, 120));
        Tap(multi.RenderView, new Point(90, 24), 204);
        Assert.NotNull(update);
        Assert.True(update!.SetEquals([0, 1]));

        using var empty = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SegmentedButton<int>(
                segments: Segments(),
                selected: new HashSet<int> { 0 },
                emptySelectionAllowed: true,
                onSelectionChanged: value => update = value)));
        empty.Pump(new Size(360, 120));
        Tap(empty.RenderView, new Point(24, 24), 205);
        Assert.NotNull(update);
        Assert.Empty(update!);
    }

    [Fact]
    public void SegmentedButton_StyleThemeTooltipDirectionAndExpansionAreApplied()
    {
        var theme = ThemeData.Light with
        {
            SegmentedButtonTheme = new SegmentedButtonThemeData(
                Style: SegmentedButton<int>.StyleFrom(
                    foregroundColor: Colors.Purple,
                    selectedForegroundColor: Colors.Orange,
                    selectedBackgroundColor: Colors.DarkGreen,
                    minimumSize: new Size(70, 44)),
                SelectedIcon: new Text("theme-check")),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new SizedBox(
                width: 300,
                height: 180,
                child: new SegmentedButton<int>(
                    segments:
                    [
                        new ButtonSegment<int>(0, label: new Text("One"), tooltip: "First option"),
                        new ButtonSegment<int>(1, label: new Text("Two")),
                    ],
                    selected: new HashSet<int> { 0 },
                    onSelectionChanged: _ => { },
                    expandedInsets: new Thickness(8),
                    direction: Axis.Vertical,
                    style: SegmentedButton<int>.StyleFrom(selectedForegroundColor: Colors.Gold)))));
        harness.Pump(new Size(360, 220));

        Assert.Equal(Colors.Gold,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "One")!.Foreground).Color);
        Assert.Equal(Colors.Purple,
            Assert.IsType<SolidColorBrush>(FindParagraph(harness.RenderView, "Two")!.Foreground).Color);
        Assert.NotNull(FindParagraph(harness.RenderView, "theme-check"));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 44);

        var layout = Assert.Single(FindDescendants<RenderSegmentedControlLayout>(harness.RenderView));
        Assert.Equal(164, layout.Size.Height, precision: 3);
        Assert.Equal(layout.FirstChild!.Size.Width, layout.LastChild!.Size.Width, precision: 3);
    }

    [Fact]
    public void ToggleButtons_EqualizeCrossAxisExtentToTallestChild()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ToggleButtons(
                children:
                [
                    new SizedBox(width: 30, height: 72),
                    new SizedBox(width: 30, height: 20),
                ],
                isSelected: [false, false],
                onPressed: _ => { })));
        harness.Pump(new Size(320, 140));

        var buttons = FindDescendants<RenderBox>(harness.RenderView)
            .Where(renderBox => renderBox.GetType().Name == "RenderSelectToggleButton")
            .ToList();
        Assert.Equal(2, buttons.Count);
        Assert.Equal(buttons[0].Size.Height, buttons[1].Size.Height, precision: 3);
        Assert.True(buttons[0].Size.Height >= 72);
    }

    private static IReadOnlyList<ButtonSegment<int>> Segments(bool disableSecond = false) =>
    [
        new ButtonSegment<int>(0, icon: new Icon(Icons.StarOutline), label: new Text("One")),
        new ButtonSegment<int>(1, icon: new Icon(Icons.InfoOutline), label: new Text("Two"), enabled: !disableSecond),
    ];

    private static Widget Wrap(ThemeData theme, Widget child) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(360, 220)),
                new Theme(
                    theme,
                    new Align(alignment: Alignment.TopLeft, child: child))));

    private static void Tap(RenderView view, Point point, int pointer)
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            var now = DateTime.UtcNow;
            binding.HandlePointerEvent(view,
                new PointerDownEvent(pointer, PointerDeviceKind.Mouse, point, PointerButtons.Primary, now));
            binding.HandlePointerEvent(view,
                new PointerUpEvent(pointer, PointerDeviceKind.Mouse, point, PointerButtons.None, now.AddMilliseconds(16)));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

    private static RenderParagraph? FindParagraphByCodePoint(RenderObject? root, int codePoint) =>
        FindParagraph(root, char.ConvertFromUtf32(codePoint));

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T value) result.Add(value);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? root, Func<SemanticsNode, bool> predicate)
    {
        if (root is null) return null;
        if (predicate(root)) return root;
        foreach (var child in root.Children)
        {
            var match = FindSemantics(child, predicate);
            if (match is not null) return match;
        }
        return null;
    }

    private static T Property<T>(object instance, string name)
    {
        object? value = instance.GetType().GetProperty(name)?.GetValue(instance);
        return Assert.IsType<T>(value);
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
            return _pipeline.SemanticsOwner.RootNode;
        }

        public void Dispose() => _rootElement.Unmount();

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
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
