using Avalonia;
using Avalonia.Media;
using Flutter;
using Flutter.Gestures;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialBottomNavigationBarTests
{
    [Fact]
    public void BottomNavigationBar_RequiresAtLeastTwoItems()
    {
        var error = Assert.Throws<ArgumentException>(() => new BottomNavigationBar(
            items:
            [
                new BottomNavigationBarItem(
                    icon: new Icon(Icons.Menu),
                    label: "Only one"),
            ]));

        Assert.Contains("at least two", error.Message);
    }

    [Fact]
    public void BottomNavigationBar_InvalidCurrentIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomNavigationBar(
            items:
            [
                new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "One"),
                new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Two"),
            ],
            currentIndex: 2));
    }

    [Fact]
    public void BottomNavigationBar_DefaultColors_UseThemeCanvasPrimaryAndOnSurfaceVariant()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            CanvasColor = Colors.DarkSlateBlue,
            PrimaryColor = Colors.OrangeRed,
            OnSurfaceVariantColor = Colors.CadetBlue,
        };

        var root = new TestRootElement(
            WrapWithThemeAndMediaQuery(
                theme,
                new BottomNavigationBar(
                    currentIndex: 1,
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        Assert.Equal(Colors.DarkSlateBlue, ResolveBackgroundColor(renderRoot));

        var firstLabel = FindParagraphByText(renderRoot, "First");
        var secondLabel = FindParagraphByText(renderRoot, "Second");
        Assert.NotNull(firstLabel);
        Assert.NotNull(secondLabel);
        Assert.Equal(Colors.CadetBlue, Assert.IsType<SolidColorBrush>(firstLabel!.Foreground).Color);
        Assert.Equal(Colors.OrangeRed, Assert.IsType<SolidColorBrush>(secondLabel!.Foreground).Color);
    }

    [Fact]
    public void BottomNavigationBar_SelectedItem_UsesActiveIcon()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    currentIndex: 0,
                    items:
                    [
                        new BottomNavigationBarItem(
                            icon: new Text("icon-0"),
                            activeIcon: new Text("active-0"),
                            label: "First"),
                        new BottomNavigationBarItem(
                            icon: new Text("icon-1"),
                            label: "Second"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        Assert.NotNull(FindParagraphByText(renderRoot, "active-0"));
        Assert.Null(FindParagraphByText(renderRoot, "icon-0"));
        Assert.NotNull(FindParagraphByText(renderRoot, "icon-1"));
    }

    [Fact]
    public void BottomNavigationBar_OnTap_InvokesCallbackWithTappedIndex()
    {
        int? tappedIndex = null;

        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    currentIndex: 0,
                    onTap: index => tappedIndex = index,
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                    ])));

        harness.Pump(new Size(320, 120));

        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            binding.HandlePointerEvent(
                harness.RenderView,
                new PointerDownEvent(
                    pointer: 91,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(240, 30),
                    buttons: PointerButtons.Primary,
                    timestampUtc: DateTime.UtcNow));

            binding.HandlePointerEvent(
                harness.RenderView,
                new PointerUpEvent(
                    pointer: 91,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(240, 30),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow));

            Assert.Equal(1, tappedIndex);
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    [Fact]
    public void BottomNavigationBar_Semantics_ExposeButtonSelectionAndIndexLabel()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    currentIndex: 1,
                    onTap: _ => { },
                    showUnselectedLabels: false,
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.Check), label: "Third"),
                    ])));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(320, 120));
        Assert.NotNull(semanticsRoot);

        var firstIndexNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => string.Equals(node.Label, "Tab 1 of 3", StringComparison.Ordinal));
        Assert.NotNull(firstIndexNode);

        var secondIndexNode = FindFirstSemanticsNode(
            semanticsRoot,
            static node => string.Equals(node.Label, "Tab 2 of 3", StringComparison.Ordinal));
        Assert.NotNull(secondIndexNode);

        var firstNode = FindFirstSemanticsNode(
            semanticsRoot,
            static node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                           && node.Label is not null
                           && node.Label.Contains("First", StringComparison.Ordinal));
        Assert.NotNull(firstNode);
        Assert.True(firstNode.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(firstNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(firstNode.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(firstNode.Actions.HasFlag(SemanticsActions.Tap));

        var selectedNode = FindFirstSemanticsNode(
            semanticsRoot,
            static node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                           && node.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.NotNull(selectedNode);
        Assert.True(selectedNode.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(selectedNode.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(selectedNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(selectedNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void BottomNavigationBar_SemanticsIndexLabel_UsesMaterialLocalizationsOverride()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    currentIndex: 1,
                    onTap: _ => { },
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.Check), label: "Third"),
                    ]),
                localizations: new TestMaterialLocalizations()));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(320, 120));
        Assert.NotNull(semanticsRoot);

        var firstIndexNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => string.Equals(node.Label, "Section 1 / 3", StringComparison.Ordinal));
        Assert.NotNull(firstIndexNode);

        var secondIndexNode = FindFirstSemanticsNode(
            semanticsRoot,
            static node => string.Equals(node.Label, "Section 2 / 3", StringComparison.Ordinal));
        Assert.NotNull(secondIndexNode);
    }

    [Fact]
    public void BottomNavigationBar_DisabledSemantics_OmitEnabledFlagAndTapAction()
    {
        using var harness = new WidgetRenderHarness(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    currentIndex: 0,
                    onTap: null,
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                    ])));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(320, 120));
        Assert.NotNull(semanticsRoot);

        var selectedNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Flags.HasFlag(SemanticsFlags.IsButton)
                           && node.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.NotNull(selectedNode);
        Assert.True(selectedNode!.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(selectedNode.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.False(selectedNode.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(selectedNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void BottomNavigationBar_Tooltip_ShowsOnPointerEnter_AndHidesOnPointerExit()
    {
        Scheduler.ResetForTests();
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            var bar = new BottomNavigationBar(
                currentIndex: 0,
                items:
                [
                    new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First", tooltip: "First tooltip"),
                    new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                ]);

            using var harness = new WidgetRenderHarness(
                WrapWithThemeAndMediaQuery(ThemeData.Light, bar));

            harness.Pump(new Size(320, 120));
            var renderRoot = harness.RenderView.Child;
            Assert.Null(FindParagraphByText(renderRoot, "First tooltip"));

            binding.HandlePointerEvent(
                harness.RenderView,
                new PointerHoverEvent(
                    pointer: 501,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(40, 20),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.20));
            harness.Pump(new Size(320, 120));
            renderRoot = harness.RenderView.Child;
            Assert.NotNull(FindParagraphByText(renderRoot, "First tooltip"));

            binding.HandlePointerEvent(
                harness.RenderView,
                new PointerHoverEvent(
                    pointer: 501,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(1200, 20),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.50));
            harness.Pump(new Size(320, 120));
            renderRoot = harness.RenderView.Child;
            Assert.Null(FindParagraphByText(renderRoot, "First tooltip"));
        }
        finally
        {
            binding.ResetForTests();
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void BottomNavigationBar_ShiftingSelectionChange_AnimatesTileWidths()
    {
        Scheduler.ResetForTests();
        try
        {
            var items = (IReadOnlyList<BottomNavigationBarItem>)
            [
                new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First", backgroundColor: Colors.DarkRed),
                new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second", backgroundColor: Colors.DarkBlue),
                new BottomNavigationBarItem(icon: new Icon(Icons.Check), label: "Third", backgroundColor: Colors.DarkGreen),
                new BottomNavigationBarItem(icon: new Icon(Icons.Close), label: "Fourth", backgroundColor: Colors.DarkSlateGray),
            ];

            using var harness = new WidgetRenderHarness(
                WrapWithThemeAndMediaQuery(
                    ThemeData.Light,
                    new BottomNavigationBar(
                        currentIndex: 0,
                        items: items)));

            harness.Pump(new Size(360, 140));
            var initialWidths = GetBottomNavigationTileWidths(harness.RenderView.Child, items.Count);
            Assert.Equal(items.Count, initialWidths.Count);

            harness.UpdateRootWidget(
                WrapWithThemeAndMediaQuery(
                    ThemeData.Light,
                    new BottomNavigationBar(
                        currentIndex: 2,
                        items: items)));

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.15));
            harness.Pump(new Size(360, 140));
            var midWidths = GetBottomNavigationTileWidths(harness.RenderView.Child, items.Count);
            Assert.Equal(items.Count, midWidths.Count);

            Assert.True(midWidths[0] < initialWidths[0]);
            Assert.True(midWidths[2] > initialWidths[2]);

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.60));
            harness.Pump(new Size(360, 140));
            var finalWidths = GetBottomNavigationTileWidths(harness.RenderView.Child, items.Count);
            Assert.Equal(items.Count, finalWidths.Count);

            Assert.True(finalWidths[2] > finalWidths[0]);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void BottomNavigationBar_ThemeDataDefaults_AreUsed_WhenWidgetValuesAreNull()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            BottomNavigationBarTheme = new BottomNavigationBarThemeData(
                BackgroundColor: Colors.MidnightBlue,
                SelectedItemColor: Colors.Gold,
                UnselectedItemColor: Colors.DarkGray,
                ShowUnselectedLabels: false),
        };

        var root = new TestRootElement(
            WrapWithThemeAndMediaQuery(
                theme,
                new BottomNavigationBar(
                    currentIndex: 1,
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        Assert.Equal(Colors.MidnightBlue, ResolveBackgroundColor(renderRoot));
        Assert.Null(FindParagraphByText(renderRoot, "First"));

        var selectedLabel = FindParagraphByText(renderRoot, "Second");
        Assert.NotNull(selectedLabel);
        Assert.Equal(Colors.Gold, Assert.IsType<SolidColorBrush>(selectedLabel!.Foreground).Color);
    }

    [Fact]
    public void BottomNavigationBar_WidgetValues_OverrideThemeDefaults()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            BottomNavigationBarTheme = new BottomNavigationBarThemeData(
                BackgroundColor: Colors.MidnightBlue,
                SelectedItemColor: Colors.Gold,
                UnselectedItemColor: Colors.DarkGray,
                ShowUnselectedLabels: false),
        };

        var root = new TestRootElement(
            WrapWithThemeAndMediaQuery(
                theme,
                new BottomNavigationBar(
                    currentIndex: 1,
                    backgroundColor: Colors.Black,
                    selectedItemColor: Colors.HotPink,
                    unselectedItemColor: Colors.LightGreen,
                    showUnselectedLabels: true,
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        Assert.Equal(Colors.Black, ResolveBackgroundColor(renderRoot));

        var firstLabel = FindParagraphByText(renderRoot, "First");
        var secondLabel = FindParagraphByText(renderRoot, "Second");
        Assert.NotNull(firstLabel);
        Assert.NotNull(secondLabel);
        Assert.Equal(Colors.LightGreen, Assert.IsType<SolidColorBrush>(firstLabel!.Foreground).Color);
        Assert.Equal(Colors.HotPink, Assert.IsType<SolidColorBrush>(secondLabel!.Foreground).Color);
    }

    [Fact]
    public void BottomNavigationBar_ShiftingAutoType_UsesSelectedItemBackground_AndHidesUnselectedLabelsByDefault()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    currentIndex: 2,
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First", backgroundColor: Colors.DarkRed),
                        new BottomNavigationBarItem(icon: new Icon(Icons.Check), label: "Second", backgroundColor: Colors.DarkGreen),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Third", backgroundColor: Colors.DarkBlue),
                        new BottomNavigationBarItem(icon: new Icon(Icons.Close), label: "Fourth", backgroundColor: Colors.DarkSlateGray),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        Assert.Equal(Colors.DarkBlue, ResolveBackgroundColor(renderRoot));
        Assert.NotNull(FindParagraphByText(renderRoot, "Third"));
        Assert.Null(FindParagraphByText(renderRoot, "First"));
        Assert.Null(FindParagraphByText(renderRoot, "Second"));
        Assert.Null(FindParagraphByText(renderRoot, "Fourth"));
    }

    [Fact]
    public void BottomNavigationBar_LabelStyleColors_OverrideItemColors()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            WrapWithThemeAndMediaQuery(
                ThemeData.Light,
                new BottomNavigationBar(
                    currentIndex: 1,
                    selectedItemColor: Colors.OrangeRed,
                    unselectedItemColor: Colors.CadetBlue,
                    selectedLabelStyle: new TextStyle(Color: Colors.LawnGreen),
                    unselectedLabelStyle: new TextStyle(Color: Colors.IndianRed),
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First"),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second"),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var firstLabel = FindParagraphByText(renderRoot, "First");
        var secondLabel = FindParagraphByText(renderRoot, "Second");
        Assert.NotNull(firstLabel);
        Assert.NotNull(secondLabel);
        Assert.Equal(Colors.IndianRed, Assert.IsType<SolidColorBrush>(firstLabel!.Foreground).Color);
        Assert.Equal(Colors.LawnGreen, Assert.IsType<SolidColorBrush>(secondLabel!.Foreground).Color);
    }

    [Fact]
    public void BottomNavigationBar_ThemeTypeOverridesAutomaticType()
    {
        var owner = new BuildOwner();
        var theme = ThemeData.Light with
        {
            BottomNavigationBarTheme = new BottomNavigationBarThemeData(
                Type: BottomNavigationBarType.Shifting,
                ShowUnselectedLabels: false),
        };

        var root = new TestRootElement(
            WrapWithThemeAndMediaQuery(
                theme,
                new BottomNavigationBar(
                    currentIndex: 1,
                    items:
                    [
                        new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "First", backgroundColor: Colors.DarkRed),
                        new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Second", backgroundColor: Colors.DarkBlue),
                        new BottomNavigationBarItem(icon: new Icon(Icons.Check), label: "Third", backgroundColor: Colors.DarkGreen),
                    ])));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        Assert.Equal(Colors.DarkBlue, ResolveBackgroundColor(renderRoot));
        Assert.Null(FindParagraphByText(renderRoot, "First"));
        Assert.NotNull(FindParagraphByText(renderRoot, "Second"));
        Assert.Null(FindParagraphByText(renderRoot, "Third"));
    }

    [Fact]
    public void BottomNavigationBar_IconThemesMustBeProvidedTogether()
    {
        Assert.Throws<ArgumentException>(() => new BottomNavigationBar(
            items:
            [
                new BottomNavigationBarItem(icon: new Icon(Icons.Menu), label: "One"),
                new BottomNavigationBarItem(icon: new Icon(Icons.InfoOutline), label: "Two"),
            ],
            selectedIconTheme: new IconThemeData(
                Color: Colors.Red,
                Size: 20)));
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsAssignableFrom<T>(element.RenderObject);
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderParagraph paragraph && paragraph.Text == text)
        {
            return paragraph;
        }

        RenderParagraph? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindParagraphByText(child, text);
        });

        return result;
    }

    private static SemanticsNode? FindFirstSemanticsNode(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindFirstSemanticsNode(child, predicate);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static List<double> GetBottomNavigationTileWidths(RenderObject? root, int childCount)
    {
        var row = FindBottomNavigationRow(root, childCount)
                  ?? throw new InvalidOperationException("BottomNavigationBar row not found in render tree.");
        var widths = new List<double>(childCount);
        for (RenderBox? child = row.FirstChild; child != null; child = row.ChildAfter(child))
        {
            widths.Add(child.Size.Width);
        }

        return widths;
    }

    private static RenderFlex? FindBottomNavigationRow(RenderObject? root, int childCount)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderFlex flex
            && flex.Direction == Axis.Horizontal
            && flex.ChildCount == childCount)
        {
            return flex;
        }

        RenderFlex? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindBottomNavigationRow(child, childCount);
        });

        return result;
    }

    private static Color ResolveBackgroundColor(RenderObject renderObject)
    {
        return renderObject switch
        {
            RenderColoredBox coloredBox => coloredBox.Color,
            RenderDecoratedBox decoratedBox when decoratedBox.Decoration.Color.HasValue => decoratedBox.Decoration.Color.Value,
            _ => throw new InvalidOperationException($"Unable to resolve bottom navigation background from render object '{renderObject.GetType().Name}'."),
        };
    }

    private static Widget WrapWithThemeAndMediaQuery(
        ThemeData theme,
        Widget child,
        MaterialLocalizations? localizations = null)
    {
        Widget themedContent = new Theme(data: theme, child: child);
        if (localizations is not null)
        {
            themedContent = new MaterialLocalizationsScope(localizations: localizations, child: themedContent);
        }

        return new MediaQuery(
            data: new MediaQueryData(Size: new Size(390, 844)),
            child: themedContent);
    }

    private sealed class TestMaterialLocalizations : MaterialLocalizations
    {
        public override string TabLabel(int tabIndex, int tabCount)
        {
            return $"Section {tabIndex + 1} / {tabCount}";
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
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

        public void UpdateRootWidget(Widget rootWidget)
        {
            _rootElement.Update(rootWidget);
            _owner.FlushBuild();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public void Dispose()
        {
            _rootElement.Unmount();
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
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (child is not RenderBox renderBox)
                {
                    throw new InvalidOperationException("HarnessRootElement can host only RenderBox.");
                }

                _renderView.Child = renderBox;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
                if (!Equals(oldSlot, newSlot))
                {
                    throw new InvalidOperationException("HarnessRootElement does not support non-null slot moves.");
                }
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (slot != null)
                {
                    throw new InvalidOperationException("HarnessRootElement expects null slot.");
                }

                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
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
