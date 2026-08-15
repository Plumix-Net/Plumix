using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialMergeableMaterialTests
{
    private static readonly Size ViewSize = new(320, 600);
    private static readonly Color FirstColor = Color.Parse("#FFE8F5E9");
    private static readonly Color SecondColor = Color.Parse("#FFE3F2FD");
    private static readonly Color ThirdColor = Color.Parse("#FFFFF3E0");

    [Fact]
    public void ConstructorAndItems_PreserveFlutterDefaultsAndDiagnostics()
    {
        var first = Slice("A", 40, FirstColor);
        var gap = Gap("x");
        var widget = new MergeableMaterial(children: [first, gap, Slice("B", 40, SecondColor)]);

        Assert.Equal(Axis.Vertical, widget.MainAxis);
        Assert.Equal(2, widget.Elevation);
        Assert.False(widget.HasDividers);
        Assert.Null(widget.DividerColor);
        Assert.Same(first, widget.Children[0]);
        Assert.Equal(16, gap.Size);
        Assert.Contains("MergeableSlice", first.ToString());
        Assert.Contains("MaterialGap", gap.ToString());

        var empty = new MergeableMaterial();
        Assert.Empty(empty.Children);
    }

    [Fact]
    public void InitialLayout_UsesFullGapsRoundedSlicesAndRenderOwnedShadows()
    {
        using var harness = new WidgetRenderHarness(Build(
            [Slice("A", 100, FirstColor), Gap("x"), Slice("B", 100, SecondColor)],
            elevation: 2.5));
        harness.Pump(ViewSize);

        RenderMergeableMaterialListBody body = Body(harness);
        Assert.Equal(new Size(240, 216), body.Size);
        Assert.Equal(3, body.ChildCount);
        Assert.Equal(2.5, body.Elevation);
        Assert.All(SliceDecorations(harness), decoration => Assert.Equal(2, decoration.BorderRadius!.Value.Radius));
        Assert.DoesNotContain(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.BoxShadows is not null);

        harness.Update(Build([Slice("A", 100, FirstColor)], elevation: 0));
        harness.Pump(ViewSize);
        Assert.Equal(0, Body(harness).Elevation);
        Assert.Equal(100, Body(harness).Size.Height);
    }

    [Fact]
    public void UpdatingAndSwappingSlices_AreImmediateAndKeepJoinedCorners()
    {
        using var harness = new WidgetRenderHarness(Build(
            [Slice("A", 100, FirstColor), Slice("B", 100, SecondColor)]));
        harness.Pump(ViewSize);

        harness.Update(Build([Slice("A", 200, FirstColor), Slice("B", 100, SecondColor)]));
        harness.Pump(ViewSize);
        Assert.Equal(300, Body(harness).Size.Height);
        AssertJoinedCorners(harness);

        harness.Update(Build([Slice("B", 100, SecondColor), Slice("A", 200, FirstColor)]));
        harness.Pump(ViewSize);
        Assert.Equal(300, Body(harness).Size.Height);
        AssertJoinedCorners(harness);
    }

    [Fact]
    public void GapMergeAndSeparation_AnimateExtentAndAdjacentCorners()
    {
        IReadOnlyList<MergeableMaterialItem> separated =
            [Slice("A", 100, FirstColor), Gap("x"), Slice("B", 100, SecondColor)];
        IReadOnlyList<MergeableMaterialItem> joined =
            [Slice("A", 100, FirstColor), Slice("B", 100, SecondColor)];
        using var harness = new WidgetRenderHarness(Build(separated));
        harness.Pump(ViewSize);

        Assert.Equal(216, Body(harness).Size.Height);
        AssertSeparatedCorners(harness);

        UpdateAndPumpAnimation(harness, joined, TimeSpan.FromMilliseconds(100));
        Assert.InRange(Body(harness).Size.Height, 200.001, 215.999);
        AssertShiftingInnerCorners(harness);

        PumpAnimation(harness, TimeSpan.FromMilliseconds(250));
        Assert.Equal(200, Body(harness).Size.Height);
        AssertJoinedCorners(harness);

        UpdateAndPumpAnimation(harness, separated, TimeSpan.FromMilliseconds(100));
        Assert.InRange(Body(harness).Size.Height, 200.001, 215.999);
        AssertShiftingInnerCorners(harness);

        PumpAnimation(harness, TimeSpan.FromMilliseconds(250));
        Assert.Equal(216, Body(harness).Size.Height);
        AssertSeparatedCorners(harness);
    }

    [Fact]
    public void InsertingAndRemovingSlices_UpdatesLayoutWithoutGapAnimation()
    {
        IReadOnlyList<MergeableMaterialItem> two =
            [Slice("A", 100, FirstColor), Slice("C", 100, ThirdColor)];
        IReadOnlyList<MergeableMaterialItem> three =
            [Slice("A", 100, FirstColor), Slice("B", 100, SecondColor), Slice("C", 100, ThirdColor)];
        using var harness = new WidgetRenderHarness(Build(two));
        harness.Pump(ViewSize);

        harness.Update(Build(three));
        harness.Pump(ViewSize);
        Assert.Equal(300, Body(harness).Size.Height);
        AssertJoinedCorners(harness, expectedCount: 3);

        harness.Update(Build(two));
        harness.Pump(ViewSize);
        Assert.Equal(200, Body(harness).Size.Height);
        AssertJoinedCorners(harness);
    }

    [Fact]
    public void InsertingAndRemovingChunks_InterpolatesAllReplacementGaps()
    {
        IReadOnlyList<MergeableMaterialItem> joined =
            [Slice("A", 100, FirstColor), Slice("C", 100, ThirdColor)];
        IReadOnlyList<MergeableMaterialItem> chunk =
        [
            Slice("A", 100, FirstColor),
            Gap("x"),
            Slice("B", 100, SecondColor),
            Gap("y"),
            Slice("C", 100, ThirdColor),
        ];
        using var harness = new WidgetRenderHarness(Build(joined));
        harness.Pump(ViewSize);

        UpdateAndPumpAnimation(harness, chunk, TimeSpan.FromMilliseconds(100));
        Assert.InRange(Body(harness).Size.Height, 300.001, 331.999);
        AssertShiftingInnerCorners(harness, expectedCount: 3);

        PumpAnimation(harness, TimeSpan.FromMilliseconds(250));
        Assert.Equal(332, Body(harness).Size.Height);
        AssertSeparatedCorners(harness, expectedCount: 3);

        UpdateAndPumpAnimation(harness, joined, TimeSpan.FromMilliseconds(100));
        Assert.InRange(Body(harness).Size.Height, 200.001, 331.999);
        AssertShiftingInnerCorners(harness);

        PumpAnimation(harness, TimeSpan.FromMilliseconds(250));
        Assert.Equal(200, Body(harness).Size.Height);
        AssertJoinedCorners(harness);
    }

    [Fact]
    public void GapAndChunkReplacement_PreserveCombinedGapExtentDuringAnimation()
    {
        IReadOnlyList<MergeableMaterialItem> oneGap =
            [Slice("A", 100, FirstColor), Gap("x"), Slice("C", 100, ThirdColor)];
        IReadOnlyList<MergeableMaterialItem> chunk =
        [
            Slice("A", 100, FirstColor),
            Gap("y"),
            Slice("B", 100, SecondColor),
            Gap("z"),
            Slice("C", 100, ThirdColor),
        ];
        using var harness = new WidgetRenderHarness(Build(oneGap));
        harness.Pump(ViewSize);

        UpdateAndPumpAnimation(harness, chunk, TimeSpan.FromMilliseconds(100));
        Assert.InRange(Body(harness).Size.Height, 300.001, 331.999);
        PumpAnimation(harness, TimeSpan.FromMilliseconds(250));
        Assert.Equal(332, Body(harness).Size.Height);

        IReadOnlyList<MergeableMaterialItem> replacement =
            [Slice("A", 100, FirstColor), Gap("q"), Slice("C", 100, ThirdColor)];
        UpdateAndPumpAnimation(harness, replacement, TimeSpan.FromMilliseconds(100));
        Assert.InRange(Body(harness).Size.Height, 216.001, 331.999);
        PumpAnimation(harness, TimeSpan.FromMilliseconds(250));
        Assert.Equal(216, Body(harness).Size.Height);
        AssertSeparatedCorners(harness);
    }

    [Fact]
    public void DividersAndSliceColors_UseWidgetAndThemePrecedence()
    {
        Color dividerColor = Colors.Red;
        using var harness = new WidgetRenderHarness(Build(
            [Slice("A", 40, FirstColor), Slice("B", 40, SecondColor), Slice("C", 40, null)],
            hasDividers: true,
            dividerColor: dividerColor));
        harness.Pump(ViewSize);

        BoxDecoration[] sliceDecorations = SliceDecorations(harness);
        Assert.Contains(sliceDecorations, decoration => decoration.Color == FirstColor);
        Assert.Contains(sliceDecorations, decoration => decoration.Color == SecondColor);
        Assert.Contains(sliceDecorations, decoration => decoration.Color == ThemeData.Light.CardColor);

        BoxBorder[] borders = FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Select(box => box.Decoration.Border)
            .OfType<BoxBorder>()
            .ToArray();
        Assert.Equal(3, borders.Length);
        Assert.Equal(BorderStyle.None, borders[0].Top.Style);
        Assert.Equal(dividerColor, borders[0].Bottom.Color);
        Assert.Equal(dividerColor, borders[1].Top.Color);
        Assert.Equal(dividerColor, borders[1].Bottom.Color);
        Assert.Equal(dividerColor, borders[2].Top.Color);
        Assert.Equal(BorderStyle.None, borders[2].Bottom.Style);
    }

    [Fact]
    public void HorizontalRtl_UsesLeftAxisDirectionAndGapWidth()
    {
        using var harness = new WidgetRenderHarness(Build(
            [Slice("A", 40, FirstColor, width: 100), Gap("x"), Slice("B", 40, SecondColor, width: 100)],
            axis: Axis.Horizontal,
            textDirection: TextDirection.Rtl));
        harness.Pump(ViewSize);

        RenderMergeableMaterialListBody body = Body(harness);
        Assert.Equal(AxisDirection.Left, body.AxisDirection);
        Assert.Equal(new Size(216, 80), body.Size);
        Assert.Equal(16, Assert.IsType<RenderConstrainedBox>(body.ChildAfter(body.FirstChild!)!).Size.Width);
    }

    private static Widget Build(
        IReadOnlyList<MergeableMaterialItem> children,
        Axis axis = Axis.Vertical,
        double elevation = 2.0,
        bool hasDividers = false,
        Color? dividerColor = null,
        TextDirection textDirection = TextDirection.Ltr)
    {
        return new Directionality(
            textDirection,
            new Theme(
                ThemeData.Light,
                new SizedBox(
                    width: axis == Axis.Vertical ? 240 : null,
                    height: axis == Axis.Horizontal ? 80 : null,
                    child: new MergeableMaterial(
                        mainAxis: axis,
                        elevation: elevation,
                        hasDividers: hasDividers,
                        children: children,
                        dividerColor: dividerColor))));
    }

    private static MaterialSlice Slice(
        string key,
        double height,
        Color? color,
        double width = 100.0)
    {
        return new MaterialSlice(
            new ValueKey<string>(key),
            new SizedBox(width: width, height: height),
            color);
    }

    private static MaterialGap Gap(string key, double size = 16.0) =>
        new(new ValueKey<string>(key), size);

    private static RenderMergeableMaterialListBody Body(WidgetRenderHarness harness) =>
        Assert.Single(FindDescendants<RenderMergeableMaterialListBody>(harness.RenderView));

    private static BoxDecoration[] SliceDecorations(WidgetRenderHarness harness)
    {
        return FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Select(box => box.Decoration)
            .Where(decoration => decoration.Color is { } color && color != MaterialColors.Transparent)
            .ToArray();
    }

    private static void AssertJoinedCorners(WidgetRenderHarness harness, int expectedCount = 2)
    {
        BoxDecoration[] decorations = SliceDecorations(harness);
        Assert.Equal(expectedCount, decorations.Length);
        Assert.Equal(2, decorations[0].BorderRadius!.Value.TopLeft);
        Assert.Equal(0, decorations[0].BorderRadius!.Value.BottomLeft);
        Assert.Equal(0, decorations[^1].BorderRadius!.Value.TopLeft);
        Assert.Equal(2, decorations[^1].BorderRadius!.Value.BottomLeft);
        if (expectedCount == 3)
        {
            Assert.Equal(BorderRadius.Zero, decorations[1].BorderRadius);
        }
    }

    private static void AssertSeparatedCorners(WidgetRenderHarness harness, int expectedCount = 2)
    {
        BoxDecoration[] decorations = SliceDecorations(harness);
        Assert.Equal(expectedCount, decorations.Length);
        Assert.All(decorations, decoration => Assert.Equal(2, decoration.BorderRadius!.Value.Radius));
    }

    private static void AssertShiftingInnerCorners(WidgetRenderHarness harness, int expectedCount = 2)
    {
        BoxDecoration[] decorations = SliceDecorations(harness);
        Assert.Equal(expectedCount, decorations.Length);
        Assert.InRange(decorations[0].BorderRadius!.Value.BottomLeft, 0.001, 1.999);
        Assert.InRange(decorations[^1].BorderRadius!.Value.TopLeft, 0.001, 1.999);
        if (expectedCount == 3)
        {
            Assert.InRange(decorations[1].BorderRadius!.Value.TopLeft, 0.001, 1.999);
            Assert.InRange(decorations[1].BorderRadius!.Value.BottomLeft, 0.001, 1.999);
        }
    }

    private static void UpdateAndPumpAnimation(
        WidgetRenderHarness harness,
        IReadOnlyList<MergeableMaterialItem> children,
        TimeSpan elapsed)
    {
        harness.Update(Build(children));
        harness.Pump(ViewSize);
        PumpAnimation(harness, elapsed);
    }

    private static void PumpAnimation(WidgetRenderHarness harness, TimeSpan elapsed)
    {
        AnimationPump.Advance(elapsed.TotalSeconds);
        harness.Pump(ViewSize);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T target)
        {
            result.Add(target);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
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

        public void Dispose() => _rootElement.Unmount();

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
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
            }
            public void UpdateRoot(Widget widget) => Update(widget);
            internal override void Unmount()
            {
                if (_child is not null) { UnmountChild(_child); _child = null; }
                base.Unmount();
            }
        }
    }
}
