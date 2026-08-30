using System.Text.RegularExpressions;
using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// Parity coverage for the diagnostics layer of `flutter/packages/flutter/lib/src/rendering/object.dart`
/// (`RenderObject` as a `DiagnosticableTree`) and the `debugFillProperties`/`debugDescribeChildren`
/// overrides across `rendering/`. Goldens are Flutter's own, from `test/rendering/box_test.dart`,
/// `limited_box_test.dart`, `flex_test.dart`, `image_test.dart` and `stack_test.dart`, compared after
/// normalizing identity hashes the way Flutter's `equalsIgnoringHashCodes` matcher does.
public sealed class RenderObjectDiagnosticsTests
{
    // ---------- RenderObject header ----------

    [DebugOnlyFact]
    public void ToStringShort_UnattachedBox_ReportsNeedsLayoutPaintAndDetached()
    {
        var box = new RenderConstrainedBox(new BoxConstraints(MaxWidth: 10, MaxHeight: 10));

        Assert.Equal(
            "RenderConstrainedBox#00000 NEEDS-LAYOUT NEEDS-PAINT DETACHED",
            Normalize(box.ToStringShort()));
    }

    [Fact]
    public void ToString_ReturnsToStringShort()
    {
        var box = new RenderConstrainedBox(new BoxConstraints(MaxWidth: 10, MaxHeight: 10));

        Assert.Equal(box.ToStringShort(), box.ToString());
    }

    [DebugOnlyFact]
    public void ToStringShort_LaidOutChild_ReportsRelayoutBoundaryDepth()
    {
        RenderBox child = new SizedBox(new Size(10, 10));
        var parent = new LooseParent { Child = child };
        Layout(parent, new BoxConstraints(MinWidth: 100, MaxWidth: 100, MinHeight: 100, MaxHeight: 100));

        Assert.Equal(
            "SizedBox#00000 relayoutBoundary=up1 NEEDS-PAINT",
            Normalize(child.ToStringShort()));
    }

    // ---------- RenderObject.debugFillProperties ----------

    [DebugOnlyFact]
    public void ToStringDeep_UnattachedBox_ReportsMissingParentDataConstraintsAndSize()
    {
        var box = new RenderConstraintsTransformBox(
            constraintsTransform: ConstraintsTransformBox.Unconstrained,
            alignment: Alignment.Center,
            textDirection: TextDirection.Ltr);

        Assert.Equal(
            """
            RenderConstraintsTransformBox#00000 NEEDS-LAYOUT NEEDS-PAINT DETACHED
               parentData: MISSING
               constraints: MISSING
               size: MISSING
               alignment: Alignment.center
               textDirection: ltr

            """.ReplaceLineEndings("\n"),
            Normalize(box.ToStringDeep(minLevel: DiagnosticLevel.Info)));
    }

    [DebugOnlyFact]
    public void DebugFillProperties_LaidOutChild_ReportsCanUseSizeTooltip()
    {
        RenderBox child = new SizedBox(new Size(10, 10));
        var parent = new LooseParent { Child = child };
        Layout(parent, new BoxConstraints(MinWidth: 100, MaxWidth: 100, MinHeight: 100, MaxHeight: 100));

        Assert.Contains("(can use size)", child.ToStringDeep(minLevel: DiagnosticLevel.Info), StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void DebugFillProperties_Creator_IsHiddenAtInfoLevelAndShownAtDebugLevel()
    {
        var box = new RenderConstrainedBox(new BoxConstraints(MaxWidth: 10, MaxHeight: 10))
        {
            DebugCreator = "the-creator",
        };

        Assert.DoesNotContain("creator", box.ToStringDeep(minLevel: DiagnosticLevel.Info), StringComparison.Ordinal);
        Assert.Contains(
            "creator: the-creator",
            box.ToStringDeep(minLevel: DiagnosticLevel.Debug),
            StringComparison.Ordinal);
    }

    // ---------- debugDescribeChildren ----------

    [Fact]
    public void DebugDescribeChildren_SingleChildBox_NamesTheChildChild()
    {
        RenderBox child = new SizedBox(new Size(10, 10));
        var parent = new RenderConstrainedBox(
            new BoxConstraints(MinWidth: 100, MaxWidth: 100, MinHeight: 100, MaxHeight: 100))
        {
            Child = child,
        };

        List<DiagnosticsNode> children = parent.DebugDescribeChildren();

        Assert.Equal("child", Assert.Single(children).Name);
    }

    [Fact]
    public void DebugDescribeChildren_MultiChildBox_NumbersTheChildrenFromOne()
    {
        var flex = new RenderFlex(
            children: [new SizedBox(new Size(10, 10)), new SizedBox(new Size(20, 20))],
            textDirection: TextDirection.Ltr);

        List<DiagnosticsNode> children = flex.DebugDescribeChildren();

        Assert.Equal(["child 1", "child 2"], children.Select(node => node.Name));
    }

    [DebugOnlyFact]
    public void DebugDescribeChildren_Offstage_UsesTheOffstageStyleForTheChild()
    {
        var offstage = new RenderOffstage(offstage: true) { Child = new SizedBox(new Size(10, 10)) };

        Assert.Equal(DiagnosticsTreeStyle.Offstage, Assert.Single(offstage.DebugDescribeChildren()).Style);

        offstage.Offstage = false;

        Assert.Equal(DiagnosticsTreeStyle.Sparse, Assert.Single(offstage.DebugDescribeChildren()).Style);
    }

    [DebugOnlyFact]
    public void DebugDescribeChildren_IndexedStack_MarksEveryChildButTheSelectedOneOffstage()
    {
        var stack = new RenderIndexedStack(index: 1);
        var first = new SizedBox(new Size(10, 10));
        stack.Insert(first);
        stack.Insert(new SizedBox(new Size(20, 20)), after: first);

        List<DiagnosticsNode> children = stack.DebugDescribeChildren();

        Assert.Equal(["child 1", "child 2"], children.Select(node => node.Name));
        Assert.Equal(DiagnosticsTreeStyle.Offstage, children[0].Style);
        Assert.Equal(DiagnosticsTreeStyle.Sparse, children[1].Style);
    }

    // ---------- Per-render-object properties ----------

    [DebugOnlyFact]
    public void RenderFlex_ToStringShort_AppendsOverflowingWhenTheChildrenOverflow()
    {
        var flex = new RenderFlex(
            children: [new SizedBox(new Size(500, 10))],
            textDirection: TextDirection.Ltr);
        Layout(flex, new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.EndsWith(" OVERFLOWING", flex.ToStringShort(), StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderFlex_DebugFillProperties_ReportsTheNineFlutterProperties()
    {
        var flex = new RenderFlex(
            direction: Axis.Vertical,
            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Start,
            textDirection: TextDirection.Ltr,
            verticalDirection: VerticalDirection.Up,
            textBaseline: TextBaseline.Alphabetic,
            spacing: 8.0);

        string dump = flex.ToStringDeep(minLevel: DiagnosticLevel.Info);

        Assert.Contains("direction: vertical", dump, StringComparison.Ordinal);
        Assert.Contains("mainAxisAlignment: spaceBetween", dump, StringComparison.Ordinal);
        Assert.Contains("mainAxisSize: min", dump, StringComparison.Ordinal);
        Assert.Contains("crossAxisAlignment: start", dump, StringComparison.Ordinal);
        Assert.Contains("textDirection: ltr", dump, StringComparison.Ordinal);
        Assert.Contains("verticalDirection: up", dump, StringComparison.Ordinal);
        Assert.Contains("textBaseline: alphabetic", dump, StringComparison.Ordinal);
        Assert.Contains("spacing: 8.0", dump, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderLimitedBox_DebugFillProperties_HidesTheInfiniteDefaults()
    {
        string unlimited = new RenderLimitedBox().ToStringDeep(minLevel: DiagnosticLevel.Info);
        string limited = new RenderLimitedBox(maxWidth: 100, maxHeight: 200)
            .ToStringDeep(minLevel: DiagnosticLevel.Info);

        Assert.DoesNotContain("maxWidth", unlimited, StringComparison.Ordinal);
        Assert.DoesNotContain("maxHeight", unlimited, StringComparison.Ordinal);
        Assert.Contains("maxWidth: 100.0", limited, StringComparison.Ordinal);
        Assert.Contains("maxHeight: 200.0", limited, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderConstrainedOverflowBox_DebugFillProperties_UsesTheIfNullDescriptions()
    {
        string dump = new RenderConstrainedOverflowBox().ToStringDeep(minLevel: DiagnosticLevel.Info);

        Assert.Contains("minWidth: use parent minWidth constraint", dump, StringComparison.Ordinal);
        Assert.Contains("maxWidth: use parent maxWidth constraint", dump, StringComparison.Ordinal);
        Assert.Contains("minHeight: use parent minHeight constraint", dump, StringComparison.Ordinal);
        Assert.Contains("maxHeight: use parent maxHeight constraint", dump, StringComparison.Ordinal);
        Assert.Contains("fit: max", dump, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderAlign_DebugFillProperties_DescribesNullFactorsAsExpand()
    {
        string dump = new RenderAlign().ToStringDeep(minLevel: DiagnosticLevel.Info);

        Assert.Contains("widthFactor: expand", dump, StringComparison.Ordinal);
        Assert.Contains("heightFactor: expand", dump, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderFractionallySizedBox_DebugFillProperties_DescribesNullFactorsAsPassThrough()
    {
        string dump = new RenderFractionallySizedBox().ToStringDeep(minLevel: DiagnosticLevel.Info);

        Assert.Contains("widthFactor: pass-through", dump, StringComparison.Ordinal);
        Assert.Contains("heightFactor: pass-through", dump, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderOpacity_DebugFillProperties_FlagsAlwaysIncludeSemanticsOnlyWhenSet()
    {
        Assert.DoesNotContain(
            "alwaysIncludeSemantics",
            new RenderOpacity().ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
        Assert.Contains(
            "alwaysIncludeSemantics",
            new RenderOpacity(opacity: 1.0, alwaysIncludeSemantics: true).ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderIgnorePointer_DebugFillProperties_DescribesAnImplicitIgnoringSemantics()
    {
        string dump = new RenderIgnorePointer(ignoring: true, ignoringSemantics: true)
            .ToStringDeep(minLevel: DiagnosticLevel.Info);

        Assert.Contains("ignoring: true", dump, StringComparison.Ordinal);
        Assert.Contains("ignoringSemantics: implicitly True", dump, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderPointerListener_DebugFillProperties_SummarizesTheAttachedListeners()
    {
        Assert.Contains(
            "listeners: <none>",
            new RenderPointerListener().ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);

        var listener = new RenderPointerListener { OnPointerDown = _ => { }, OnPointerUp = _ => { } };

        Assert.Contains(
            "listeners: down, up",
            listener.ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderSemanticsGestureHandler_DebugFillProperties_ListsTheHandledGestures()
    {
        Assert.Contains(
            "gestures: <none>",
            new RenderSemanticsGestureHandler().ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);

        var handler = new RenderSemanticsGestureHandler { OnTap = () => { }, OnLongPress = () => { } };

        Assert.Contains(
            "gestures: tap, long press",
            handler.ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderStack_DebugFillProperties_HidesTheHardEdgeClipDefault()
    {
        Assert.DoesNotContain(
            "clipBehavior",
            new RenderStack().ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
        Assert.Contains(
            "clipBehavior: none",
            new RenderStack { ClipBehavior = Clip.None }.ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
    }

    [Fact]
    public void RenderTable_DebugDescribeChildren_ReportsAnEmptyTableAndCellCoordinates()
    {
        Assert.Equal(
            "table is empty",
            Assert.Single(new RenderTable(columns: 0, rows: 0).DebugDescribeChildren()).ToDescription());

        var table = new RenderTable(columns: 2, rows: 1);
        table.SetChild(0, 0, new SizedBox(new Size(10, 10)));

        List<DiagnosticsNode> children = table.DebugDescribeChildren();

        Assert.Equal(["child (0, 0)", "child (1, 0)"], children.Select(node => node.Name));
        Assert.Equal("is null", children[1].ToDescription());
    }

    [DebugOnlyFact]
    public void RenderTable_DebugFillProperties_ReportsTheTableSize()
    {
        Assert.Contains(
            "table size: 2×1",
            new RenderTable(columns: 2, rows: 1).ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderSliver_DebugFillProperties_ReportsTheSliverGeometry()
    {
        var sliver = new RenderSliverToBoxAdapter { Child = new SizedBox(new Size(10, 10)) };

        Assert.Contains(
            "geometry: SliverGeometry",
            sliver.ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void SliverGeometry_DebugFillProperties_ReportsHiddenWhenNothingIsPainted()
    {
        var node = new SliverGeometry(ScrollExtent: 100, MaxPaintExtent: 100).ToDiagnosticsNode();
        string description = node.ToStringDeep();

        Assert.Contains("scrollExtent: 100.0", description, StringComparison.Ordinal);
        Assert.Contains("hidden", description, StringComparison.Ordinal);
        Assert.Contains("maxPaintExtent: 100.0", description, StringComparison.Ordinal);
    }

    // ---------- ParentData ----------

    [Fact]
    public void ParentData_ToString_MatchesFlutter()
    {
        Assert.Equal("<none>", new ParentData().ToString());
        Assert.Equal("offset=0, 0", new BoxParentData().ToString());
        Assert.Equal("not positioned; offset=0, 0", new StackParentData().ToString());
        Assert.Equal(
            "top=1.0; left=2.0; offset=0, 0",
            new StackParentData { Top = 1, Left = 2 }.ToString());
        Assert.Equal("paintOffset=0, 0", new SliverPhysicalParentData().ToString());
        Assert.Equal("layoutOffset=None", new SliverLogicalParentData().ToString());
        Assert.Equal("layoutOffset=12.5", new SliverLogicalParentData { LayoutOffset = 12.5 }.ToString());
        Assert.Equal("index=0; offset=0, 0", new SliverMultiBoxAdaptorParentData().ToString());
        Assert.Equal(
            "index=3; keepAlive; offset=0, 0",
            new SliverMultiBoxAdaptorParentData { Index = 3, KeepAlive = true }.ToString());
        Assert.Equal(
            "crossAxisOffset=4; index=0; offset=0, 0",
            new SliverGridParentData { CrossAxisOffset = 4 }.ToString());
        Assert.Equal("offset=0, 0; id=", new MultiChildLayoutParentData().ToString());
    }

    // ---------- Layer tree ----------

    [DebugOnlyFact]
    public void ContainerLayer_DebugDescribeChildren_NumbersTheChildrenFromOne()
    {
        var root = new ContainerLayer();
        root.Append(new OffsetLayer { Offset = new Point(1, 2) });
        root.Append(new OpacityLayer { Opacity = 0.5 });

        List<DiagnosticsNode> children = root.DebugDescribeChildren();

        Assert.Equal(["child 1", "child 2"], children.Select(node => node.Name));
        Assert.Contains(
            "offset: 1, 2",
            root.ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
        Assert.Contains(
            "opacity: 0.5",
            root.ToStringDeep(minLevel: DiagnosticLevel.Info),
            StringComparison.Ordinal);
    }

    // ---------- PipelineOwner ----------

    [DebugOnlyFact]
    public void PipelineOwner_DebugDumpRenderTree_DumpsFromTheRoot()
    {
        var root = new RenderView { Child = new SizedBox(new Size(10, 10)) };
        var owner = new PipelineOwner(root);
        owner.Attach(root);

        string dump = Normalize(owner.DebugDumpRenderTree());

        Assert.StartsWith("RenderView#00000", dump, StringComparison.Ordinal);
        Assert.Contains("child: SizedBox#00000", dump, StringComparison.Ordinal);
        Assert.Contains(
            "rootNode: RenderView#00000",
            Normalize(owner.ToStringDeep(minLevel: DiagnosticLevel.Info)),
            StringComparison.Ordinal);
    }

    // ---------- Painting-layer primitives this port had to land ----------

    [Fact]
    public void ColorProperty_SerializesTheChannelsTheWayFlutterDoes()
    {
        var property = new ColorProperty("color", Avalonia.Media.Color.FromArgb(0x12, 0x34, 0x56, 0x78));

        Dictionary<string, object?> json = property.ToJsonMap(DiagnosticsSerializationDelegate.Create());
        var channels = (Dictionary<string, object>)json["valueProperties"]!;

        Assert.Equal((byte)0x34, channels["red"]);
        Assert.Equal((byte)0x56, channels["green"]);
        Assert.Equal((byte)0x78, channels["blue"]);
        Assert.Equal((byte)0x12, channels["alpha"]);
    }

    [Fact]
    public void TransformProperty_RendersOneRowPerLineAndCollapsesInsideASingleLineParent()
    {
        var property = new TransformProperty("transform", Matrix4.Identity());

        Assert.Equal(
            "[0] 1.0,0.0,0.0,0.0\n[1] 0.0,1.0,0.0,0.0\n[2] 0.0,0.0,1.0,0.0\n[3] 0.0,0.0,0.0,1.0",
            property.ValueToString());
        Assert.Equal(
            "[1.0,0.0,0.0,0.0; 0.0,1.0,0.0,0.0; 0.0,0.0,1.0,0.0; 0.0,0.0,0.0,1.0]",
            property.ValueToString(TextTreeConfigurations.SingleLine));
    }

    [DebugOnlyFact]
    public void BoxDecoration_DebugFillProperties_UsesTheWhitespaceStyleAndEmptyBodyDescription()
    {
        string dump = new RenderDecoratedBox(new BoxDecoration())
            .ToStringDeep(minLevel: DiagnosticLevel.Info);

        Assert.Contains("decoration:", dump, StringComparison.Ordinal);
        Assert.Contains("<no decorations specified>", dump, StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void TextSpan_DebugDescribeChildren_ReportsTheChildSpans()
    {
        var span = new TextSpan(
            text: "outer",
            children: [new TextSpan(text: "inner")]);

        string dump = span.ToStringDeep();

        Assert.Contains("\"outer\"", dump, StringComparison.Ordinal);
        Assert.Contains("\"inner\"", dump, StringComparison.Ordinal);
        Assert.Contains("(empty)", new TextSpan().ToStringDeep(), StringComparison.Ordinal);
    }

    private static string Normalize(string value) => Regex.Replace(value, "#[0-9a-fA-F]{5}", "#00000");

    private static void Layout(RenderBox box, BoxConstraints constraints)
    {
        var root = new RenderView { Child = box };
        var owner = new PipelineOwner(root);
        owner.Attach(root);
        box.Layout(constraints, parentUsesSize: true);
    }

    /// Lays its child out with loose constraints and reads its size back, so the child is not a
    /// relayout boundary of its own.
    private sealed class LooseParent : RenderProxyBox
    {
        protected override void PerformLayout()
        {
            Child!.Layout(Constraints.Loosen(), parentUsesSize: true);
            Size = ((BoxConstraints)Constraints).Constrain(Child.Size);
        }
    }

    /// A fixed-size leaf box, standing in for Flutter's `RenderSizedBox` test double.
    private sealed class SizedBox(Size size) : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(size);

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(size);

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
