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
public sealed class MaterialChipTests : IDisposable
{
    public MaterialChipTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ChipConstructors_ValidateCallbacksAndElevation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ActionChip(new Text("action"), () => { }, elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ChoiceChip(new Text("choice"), false, _ => { }, pressElevation: double.NaN));
        Assert.Throws<ArgumentException>(() =>
            new RawChip(
                label: new Text("raw"),
                onPressed: () => { },
                onSelected: _ => { }));

        Assert.Equal(ChipVariant.Elevated, ActionChip.Elevated(new Text("a"), () => { }).Variant);
        Assert.Equal(ChipVariant.Elevated, ChoiceChip.Elevated(new Text("c"), false, _ => { }).Variant);
    }

    [Fact]
    public void ActionChip_M3FlatDefaultsMatchOutlineLabelAndGeometryTokens()
    {
        var theme = ThemeData.Light with
        {
            OutlineVariantColor = Colors.CadetBlue,
            OnSurfaceColor = Colors.DarkSlateBlue,
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new ActionChip(new Text("Action"), () => { })));

        harness.Pump(new Size(320, 120));

        var decoration = FindChipDecoration(harness.RenderView);
        Assert.Equal(Colors.Transparent, decoration.Decoration.Color);
        Assert.Equal(8, decoration.Decoration.BorderRadius!.Value.Radius);
        Assert.Equal(Colors.CadetBlue, decoration.Decoration.Border!.Value.Color);
        Assert.Equal(1, decoration.Decoration.Border.Value.Width);
        Assert.Equal(Colors.DarkSlateBlue, ForegroundColor(Paragraph(harness.RenderView, "Action")));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 32);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView),
            box => box.Padding.Left == 8 && box.Padding.Right == 8 && box.Padding.Top == 0);
    }

    [Fact]
    public void ActionChip_ElevatedUsesSurfaceContainerAndElevationDefaults()
    {
        var theme = ThemeData.Light with
        {
            SurfaceContainerLowColor = Colors.MediumPurple,
            ShadowColor = Colors.DarkGreen,
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            ActionChip.Elevated(new Text("Elevated"), () => { })));

        harness.Pump(new Size(320, 120));

        var decoration = FindChipDecoration(harness.RenderView);
        Assert.Equal(Colors.MediumPurple, decoration.Decoration.Color);
        Assert.Equal(Colors.Transparent, decoration.Decoration.Border!.Value.Color);
        Assert.True(decoration.Decoration.BoxShadows.HasValue);
        Assert.True(decoration.Decoration.BoxShadows.Value.Count > 0);
    }

    [Fact]
    public void ChoiceChip_SelectedUsesSecondaryContainerCheckmarkAndSelectedSemantics()
    {
        var theme = ThemeData.Light with
        {
            SecondaryContainerColor = Colors.DarkGreen,
            OnSecondaryContainerColor = Colors.Gold,
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new ChoiceChip(new Text("Selected"), true, _ => { })));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));

        Assert.Equal(Colors.DarkGreen, FindChipDecoration(harness.RenderView).Decoration.Color);
        Assert.Equal(Colors.Gold, ForegroundColor(Paragraph(harness.RenderView, "Selected")));
        Assert.True(FindDescendants<RenderParagraph>(harness.RenderView).Count >= 2);
        var selected = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.NotNull(selected);
        Assert.True(selected!.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(selected.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.True(selected.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void ChoiceChip_DisabledSelectedUsesDisabledSelectedTokenAndNoTapAction()
    {
        var theme = ThemeData.Light with { OnSurfaceColor = Colors.Crimson };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new ChoiceChip(new Text("Disabled"), true, onSelected: null)));

        var semantics = harness.PumpAndGetSemantics(new Size(320, 120));

        Assert.Equal(WithOpacity(Colors.Crimson, 0.12), FindChipDecoration(harness.RenderView).Decoration.Color);
        Assert.Equal(MaterialButtonCore.ApplyOpacity(Colors.Crimson, 0.38), ForegroundColor(Paragraph(harness.RenderView, "Disabled")));
        var selected = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.NotNull(selected);
        Assert.False(selected!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(selected.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void Chips_ResolveWidgetThenLocalThemeThenDefaults()
    {
        var themeData = new ChipThemeData(
            BackgroundColor: Colors.Purple,
            SelectedColor: Colors.DarkGreen,
            LabelStyle: new TextStyle(Color: Colors.Orange),
            Shape: ShapeBorder.RoundedRectangle(13));
        using var themedHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ChipTheme(
                themeData,
                new ActionChip(new Text("Themed"), () => { }))));
        themedHarness.Pump(new Size(320, 120));

        var themed = FindChipDecoration(themedHarness.RenderView);
        Assert.Equal(Colors.Purple, themed.Decoration.Color);
        Assert.Equal(13, themed.Decoration.BorderRadius!.Value.Radius);
        Assert.Equal(Colors.Orange, ForegroundColor(Paragraph(themedHarness.RenderView, "Themed")));

        using var widgetHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ChipTheme(
                themeData,
                new ChoiceChip(
                    new Text("Widget"),
                    selected: true,
                    onSelected: _ => { },
                    selectedColor: Colors.Gold,
                    labelStyle: new TextStyle(Color: Colors.Navy),
                    shape: ShapeBorder.RoundedRectangle(3)))));
        widgetHarness.Pump(new Size(320, 120));

        var widget = FindChipDecoration(widgetHarness.RenderView);
        Assert.Equal(Colors.Gold, widget.Decoration.Color);
        Assert.Equal(3, widget.Decoration.BorderRadius!.Value.Radius);
        Assert.Equal(Colors.Navy, ForegroundColor(Paragraph(widgetHarness.RenderView, "Widget")));
    }

    [Fact]
    public void WidgetStateColorOverridesLegacyColorsAndHandlesDisabledSelectedCombination()
    {
        var stateColor = MaterialStateProperty<Color?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled) && states.HasFlag(MaterialState.Selected)
                ? Colors.Crimson
                : states.HasFlag(MaterialState.Selected)
                    ? Colors.Gold
                    : Colors.CadetBlue);
        using var harness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ChoiceChip(
                new Text("State"),
                selected: true,
                onSelected: null,
                color: stateColor,
                selectedColor: Colors.DarkGreen,
                disabledColor: Colors.Gray)));

        harness.Pump(new Size(320, 120));

        Assert.Equal(Colors.Crimson, FindChipDecoration(harness.RenderView).Decoration.Color);
    }

    [Fact]
    public void RawChip_LegacySelectedColorAnimatesOverConfiguredSelectDuration()
    {
        var theme = ThemeData.Light with { SecondaryContainerColor = Colors.DarkGreen };
        var animation = new ChipAnimationStyle(SelectAnimation: TimeSpan.FromSeconds(10));
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new RawChip(
                label: new Text("Animated"),
                selected: false,
                onSelected: _ => { },
                selectedColor: Colors.DarkGreen,
                chipAnimationStyle: animation)));
        harness.Pump(new Size(320, 120));

        harness.Update(Root(
            theme,
            new RawChip(
                label: new Text("Animated"),
                selected: true,
                onSelected: _ => { },
                selectedColor: Colors.DarkGreen,
                chipAnimationStyle: animation)));
        harness.Pump(new Size(320, 120));
        var start = FindChipDecoration(harness.RenderView).Decoration.Color;

        var now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1));
        harness.Pump(new Size(320, 120));
        var middle = FindChipDecoration(harness.RenderView).Decoration.Color;
        Assert.NotEqual(start, middle);
        Assert.NotEqual(Colors.DarkGreen, middle);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 11));
        harness.Pump(new Size(320, 120));
        Assert.Equal(Colors.DarkGreen, FindChipDecoration(harness.RenderView).Decoration.Color);
    }

    [Fact]
    public void Chips_InvokeActionAndInverseSelectionCallbacks()
    {
        var actionCount = 0;
        bool? selected = null;
        using var actionHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ActionChip(new Text("Action"), () => actionCount++)));
        actionHarness.Pump(new Size(320, 120));
        Tap(actionHarness.RenderView, new Point(160, 60), 21);
        Assert.Equal(1, actionCount);

        using var choiceHarness = new WidgetRenderHarness(Root(
            ThemeData.Light,
            new ChoiceChip(new Text("Choice"), selected: true, onSelected: value => selected = value)));
        choiceHarness.Pump(new Size(320, 120));
        Tap(choiceHarness.RenderView, new Point(160, 60), 22);
        Assert.False(selected);
    }

    [Fact]
    public void Chips_M2DefaultsAndCompactTapTargetMatchFlutterPolicies()
    {
        var theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            VisualDensity = VisualDensity.Compact,
        };
        using var harness = new WidgetRenderHarness(Root(
            theme,
            new ChoiceChip(new Text("M2"), selected: true, onSelected: _ => { })));

        harness.Pump(new Size(320, 120));

        Assert.Equal(WithOpacity(theme.PrimaryColor, 0x3d / 255.0), FindChipDecoration(harness.RenderView).Decoration.Color);
        Assert.Equal(10_000, FindChipDecoration(harness.RenderView).Decoration.BorderRadius!.Value.Radius);
        Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView));
        Assert.Contains(FindDescendants<RenderButtonTapTargetPadding>(harness.RenderView),
            box => box.MinSize == new Size(40, 40));
    }

    private static Widget Root(ThemeData theme, Widget child)
    {
        return new MediaQuery(
            data: new MediaQueryData(Size: new Size(320, 120)),
            child: new Directionality(
                TextDirection.Ltr,
                new Theme(
                    theme,
                    new Align(alignment: Alignment.Center, child: child))));
    }

    private static RenderDecoratedBox FindChipDecoration(RenderObject root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .First(box => box.Decoration.BorderRadius.HasValue);
    }

    private static RenderParagraph Paragraph(RenderObject root, string text)
    {
        return FindDescendants<RenderParagraph>(root).Single(paragraph => paragraph.Text == text);
    }

    private static Color ForegroundColor(RenderParagraph paragraph)
    {
        return Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color;
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        return Color.FromArgb(
            (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255),
            color.R,
            color.G,
            color.B);
    }

    private static void Tap(RenderView view, Point point, int pointer)
    {
        var binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            var now = DateTime.UtcNow;
            binding.HandlePointerEvent(view, new PointerDownEvent(
                pointer, PointerDeviceKind.Mouse, point, PointerButtons.Primary, now));
            binding.HandlePointerEvent(view, new PointerUpEvent(
                pointer, PointerDeviceKind.Mouse, point, PointerButtons.None, now.AddMilliseconds(16)));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? root, Func<SemanticsNode, bool> predicate)
    {
        if (root is null) return null;
        if (predicate(root)) return root;
        foreach (var child in root.Children)
        {
            var found = FindSemantics(child, predicate);
            if (found is not null) return found;
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

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public HarnessRootElement(RenderView view, Widget widget) : base(widget) => _view = view;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            public void UpdateRoot(Widget widget) => Update(widget);
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget widget) { base.Update(widget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_view.Child, child)) _view.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
