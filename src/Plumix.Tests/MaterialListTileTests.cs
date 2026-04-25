using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class MaterialListTileTests
{
    public MaterialListTileTests()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void ListTile_Throws_WhenIsThreeLineAndSubtitleIsNull()
    {
        Assert.Throws<ArgumentException>(() => new ListTile(
            title: new Text("Tile"),
            isThreeLine: true));
    }

    [Fact]
    public void ListTile_DefaultM3_OneLine_UsesMinHeight56()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("One line"),
                onTap: () => { })));

        harness.Pump(new Size(400, 200));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(300, material!.Size.Width, 3);
        Assert.Equal(56, material.Size.Height, 3);
    }

    [Fact]
    public void ListTile_DenseOneLine_UsesMinHeight48()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Dense"),
                dense: true,
                onTap: () => { })));

        harness.Pump(new Size(400, 200));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(48, material!.Size.Height, 3);
    }

    [Fact]
    public void ListTile_DefaultM3_TwoLine_UsesMinHeight72()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Title"),
                subtitle: new Text("Subtitle"),
                onTap: () => { })));

        harness.Pump(new Size(400, 220));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(72, material!.Size.Height, 3);
    }

    [Fact]
    public void ListTile_DefaultM3_ThreeLine_UsesMinHeight88()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Title"),
                subtitle: new Text("Subtitle"),
                isThreeLine: true,
                onTap: () => { })));

        harness.Pump(new Size(400, 240));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(88, material!.Size.Height, 3);
    }

    [Fact]
    public void ListTile_Selected_UsesSelectedColorForTitleAndLeadingIcon()
    {
        var theme = ThemeData.Light with
        {
            PrimaryColor = Colors.Coral
        };

        using var harness = new WidgetRenderHarness(
            BuildThemedTile(
                new ListTile(
                    title: new Text("Selected"),
                    leading: new Icon(Icons.StarOutline),
                    selected: true,
                    onTap: () => { }),
                theme));

        harness.Pump(new Size(400, 220));
        var renderRoot = harness.RenderView.Child;

        var titleParagraph = FindParagraphByText(renderRoot, "Selected");
        Assert.NotNull(titleParagraph);
        Assert.Equal(Colors.Coral, Assert.IsType<SolidColorBrush>(titleParagraph!.Foreground).Color);

        var iconParagraph = FindParagraphByText(renderRoot, char.ConvertFromUtf32(Icons.StarOutline.CodePoint));
        Assert.NotNull(iconParagraph);
        Assert.Equal(Colors.Coral, Assert.IsType<SolidColorBrush>(iconParagraph!.Foreground).Color);
    }

    [Fact]
    public void ListTile_SelectedTileColor_OverridesDefaultBackground()
    {
        var selectedTileColor = Color.Parse("#FFE7D6FF");
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Selected tile"),
                selected: true,
                selectedTileColor: selectedTileColor,
                onTap: () => { })));

        harness.Pump(new Size(400, 200));

        var material = FindDescendant<RenderDecoratedBox>(harness.RenderView);
        Assert.NotNull(material);
        Assert.Equal(selectedTileColor, material!.Decoration.Color);
    }

    [Fact]
    public void ListTile_ThemeColors_ApplyWhenWidgetOverridesMissing()
    {
        var themedText = Color.Parse("#FF0F5A4A");
        var themedIcon = Color.Parse("#FF904E1A");
        var themedTile = Color.Parse("#FFF3F8E8");
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(
                TextColor: themedText,
                IconColor: themedIcon,
                TileColor: themedTile)
        };

        using var harness = new WidgetRenderHarness(
            BuildThemedTile(
                new ListTile(
                    title: new Text("Themed tile"),
                    leading: new Icon(Icons.InfoOutline),
                    onTap: () => { }),
                theme));

        harness.Pump(new Size(420, 220));
        var renderRoot = harness.RenderView.Child;

        var titleParagraph = FindParagraphByText(renderRoot, "Themed tile");
        Assert.NotNull(titleParagraph);
        Assert.Equal(themedText, Assert.IsType<SolidColorBrush>(titleParagraph!.Foreground).Color);

        var iconParagraph = FindParagraphByText(renderRoot, char.ConvertFromUtf32(Icons.InfoOutline.CodePoint));
        Assert.NotNull(iconParagraph);
        Assert.Equal(themedIcon, Assert.IsType<SolidColorBrush>(iconParagraph!.Foreground).Color);

        var material = FindDescendant<RenderDecoratedBox>(renderRoot);
        Assert.NotNull(material);
        Assert.Equal(themedTile, material!.Decoration.Color);
    }

    [Fact]
    public void ListTile_OnTap_InvokesCallback()
    {
        var tapCount = 0;
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Tap target"),
                onTap: () => tapCount += 1)));

        harness.Pump(new Size(400, 200));

        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            binding.HandlePointerEvent(
                harness.RenderView,
                new PointerDownEvent(
                    pointer: 320,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(150, 28),
                    buttons: PointerButtons.Primary,
                    timestampUtc: DateTime.UtcNow));
            binding.HandlePointerEvent(
                harness.RenderView,
                new PointerUpEvent(
                    pointer: 320,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(150, 28),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow));
        }
        finally
        {
            binding.ResetForTests();
        }

        Assert.Equal(1, tapCount);
    }

    [Fact]
    public void ListTile_Disabled_SemanticsOmitEnabledAndTapAction()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Disabled"),
                enabled: false,
                onTap: () => { })));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(400, 200));
        Assert.NotNull(semanticsRoot);

        var buttonNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(buttonNode);
        Assert.False(buttonNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(buttonNode.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void ListTile_Selected_SemanticsIncludeSelectedFlag()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemedTile(new ListTile(
                title: new Text("Selected semantics"),
                selected: true,
                onTap: () => { })));

        var semanticsRoot = harness.PumpAndGetSemantics(new Size(400, 200));
        Assert.NotNull(semanticsRoot);

        var selectedNode = FindFirstSemanticsNode(
            semanticsRoot!,
            static node => node.Flags.HasFlag(SemanticsFlags.IsButton) && node.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.NotNull(selectedNode);
        Assert.True(selectedNode!.Flags.HasFlag(SemanticsFlags.IsEnabled));
    }

    [Fact]
    public void ListTile_DemoLikeState_DoesNotProduceFlexOverflow()
    {
        var theme = ThemeData.Light with
        {
            ListTileTheme = new ListTileThemeData(
                TextColor: Color.Parse("#FF27526B"),
                IconColor: Color.Parse("#FF7A4021"),
                TileColor: Color.Parse("#FFF5F9EE"),
                SelectedTileColor: Color.Parse("#FFE4EEFF"))
        };

        using var harness = new WidgetRenderHarness(
            new Theme(
                data: theme,
                child: new SizedBox(
                    width: 720,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            new ListTile(
                                title: new Text("One-line tile"),
                                leading: new Icon(Icons.Menu),
                                trailing: new Icon(Icons.InfoOutline),
                                enabled: false),
                            new ListTile(
                                title: new Text("Two-line tile"),
                                subtitle: new Text("Subtitle text demonstrates two-line default height."),
                                leading: new Icon(Icons.Add),
                                trailing: new Text("meta", fontSize: 12),
                                enabled: false),
                            new ListTile(
                                title: new Text("Three-line probe"),
                                subtitle: new Text("When 3-line is enabled this tile uses the taller baseline height for parity checks."),
                                leading: new Icon(Icons.StarOutline),
                                trailing: new Icon(Icons.Close),
                                enabled: false),
                        ]))));

        harness.Pump(new Size(760, 420));

        var flexes = FindDescendants<RenderFlex>(harness.RenderView).ToList();
        Assert.NotEmpty(flexes);
        var overflowCount = flexes.Count(static flex => flex._hasOverflow);
        Assert.True(
            overflowCount == 0,
            $"Expected no flex overflow in demo-like ListTile layout, but found {overflowCount} overflowing flex nodes.");
    }

    private static Widget BuildThemedTile(Widget tile, ThemeData? theme = null)
    {
        return new Theme(
            data: theme ?? ThemeData.Light,
            child: new SizedBox(
                width: 300,
                child: tile));
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root)
            .FirstOrDefault(paragraph => string.Equals(paragraph.Text, text, StringComparison.Ordinal));
    }

    private static IEnumerable<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var results = new List<T>();
        CollectDescendants(root, results);
        return results;
    }

    private static void CollectDescendants<T>(RenderObject? root, List<T> results) where T : RenderObject
    {
        if (root is null)
        {
            return;
        }

        if (root is T typed)
        {
            results.Add(typed);
        }

        root.VisitChildren(child => CollectDescendants(child, results));
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        return FindDescendants<T>(root).FirstOrDefault();
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
