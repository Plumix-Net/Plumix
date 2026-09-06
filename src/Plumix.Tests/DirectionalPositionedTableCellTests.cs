using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/basic.dart (PositionedDirectional)
// flutter/packages/flutter/lib/src/widgets/table.dart (TableCell)
// flutter/packages/flutter/lib/src/rendering/table.dart (RenderTable cell alignment)

public sealed class DirectionalPositionedTableCellTests
{
    [Fact]
    public void DirectionalPositionedAndTableCell_DefaultsMatchFlutterContracts()
    {
        var child = new SizedBox();
        var positioned = new PositionedDirectional(child);
        var cell = new TableCell(child);
        var table = new Table(children: []);

        Assert.Same(child, positioned.Child);
        Assert.Null(positioned.Start);
        Assert.Null(positioned.Top);
        Assert.Null(positioned.End);
        Assert.Null(positioned.Bottom);
        Assert.Null(positioned.Width);
        Assert.Null(positioned.Height);
        Assert.Same(child, cell.Child);
        Assert.Null(cell.VerticalAlignment);
        Assert.Equal(TableCellVerticalAlignment.Top, table.DefaultVerticalAlignment);
        Assert.Null(table.TextBaseline);
        Assert.Null(table.TextDirection);
    }

    [Fact]
    public void PositionedDirectional_UsesAmbientDirectionAndUpdatesPhysicalInsets()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildDirectionalStack(TextDirection.Ltr));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var stack = FindRenderObject<RenderStack>(root);
        var child = Assert.IsAssignableFrom<RenderBox>(stack.FirstChild);
        var parentData = Assert.IsType<StackParentData>(child.parentData);
        Assert.Equal(6, parentData.Left);
        Assert.Equal(14, parentData.Right);
        Assert.Equal(4, parentData.Top);

        root.Update(BuildDirectionalStack(TextDirection.Rtl));
        owner.FlushBuild();

        var updatedStack = FindRenderObject<RenderStack>(root);
        Assert.Same(stack, updatedStack);
        var updatedChild = Assert.IsAssignableFrom<RenderBox>(updatedStack.FirstChild);
        var updatedParentData = Assert.IsType<StackParentData>(updatedChild.parentData);
        Assert.Equal(14, updatedParentData.Left);
        Assert.Equal(6, updatedParentData.Right);
        Assert.Equal(4, updatedParentData.Top);
    }

    [Fact]
    public void PositionedDirectional_RejectsOverSpecifiedResolvedInsets()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(
            TextDirection.Ltr,
            new Stack(children:
            [
                new PositionedDirectional(
                    start: 1,
                    end: 2,
                    width: 3,
                    child: new SizedBox()),
            ])));

        root.Attach(owner);
        Assert.Throws<ArgumentException>(() => root.Mount(parent: null, newSlot: null));
    }

    [Fact]
    public void TableCellWidget_AppliesAndUpdatesParentDataAndCellSemantics()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildTableCell(TableCellVerticalAlignment.Top));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var table = FindRenderObject<RenderTable>(root);
        var cell = Assert.IsType<RenderSemanticsAnnotations>(table.Row(0)[0]);
        var parentData = Assert.IsType<TableCellParentData>(cell.parentData);
        Assert.Equal(TableCellVerticalAlignment.Top, parentData.VerticalAlignment);
        Assert.Equal(SemanticsRole.Cell, cell.Role);

        root.Update(BuildTableCell(TableCellVerticalAlignment.Bottom));
        owner.FlushBuild();

        var updatedTable = FindRenderObject<RenderTable>(root);
        var updatedCell = Assert.IsType<RenderSemanticsAnnotations>(updatedTable.Row(0)[0]);
        Assert.Same(table, updatedTable);
        Assert.Same(cell, updatedCell);
        Assert.Equal(
            TableCellVerticalAlignment.Bottom,
            Assert.IsType<TableCellParentData>(updatedCell.parentData).VerticalAlignment);
    }

    [Fact]
    public void RenderTable_AlignsTopMiddleBottomAndFillCells()
    {
        var top = new FixedBaselineBox(new Size(10, 10));
        var middle = new FixedBaselineBox(new Size(10, 20));
        var bottom = new FixedBaselineBox(new Size(10, 10));
        var fill = new FixedBaselineBox(new Size(10, 4));
        var table = CreateTable(
            [top, middle, bottom, fill],
            columns: 4,
            textDirection: TextDirection.Ltr);

        ((TableCellParentData)top.parentData!).VerticalAlignment = TableCellVerticalAlignment.Top;
        ((TableCellParentData)middle.parentData!).VerticalAlignment = TableCellVerticalAlignment.Middle;
        ((TableCellParentData)bottom.parentData!).VerticalAlignment = TableCellVerticalAlignment.Bottom;
        ((TableCellParentData)fill.parentData!).VerticalAlignment = TableCellVerticalAlignment.Fill;

        table.Layout(new BoxConstraints(MaxWidth: 80, MaxHeight: 80));

        Assert.Equal(20, Assert.Single(table.ResolvedRowHeights));
        Assert.Equal(0, ((TableCellParentData)top.parentData!).offset.Y);
        Assert.Equal(0, ((TableCellParentData)middle.parentData!).offset.Y);
        Assert.Equal(10, ((TableCellParentData)bottom.parentData!).offset.Y);
        Assert.Equal(new Size(20, 20), fill.Size);
        Assert.Equal(0, ((TableCellParentData)fill.parentData!).offset.Y);
    }

    [Fact]
    public void RenderTable_AlignsBaselinesAndReportsFirstRowBaseline()
    {
        var first = new FixedBaselineBox(new Size(20, 10), alphabeticBaseline: 8);
        var second = new FixedBaselineBox(new Size(20, 14), alphabeticBaseline: 5);
        var table = CreateTable(
            [first, second],
            columns: 2,
            textDirection: TextDirection.Ltr,
            defaultVerticalAlignment: TableCellVerticalAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic);

        table.Layout(new BoxConstraints(MaxWidth: 40, MaxHeight: 40));

        Assert.Equal(17, Assert.Single(table.ResolvedRowHeights));
        Assert.Equal(0, ((TableCellParentData)first.parentData!).offset.Y);
        Assert.Equal(3, ((TableCellParentData)second.parentData!).offset.Y);
        Assert.Equal(8, table.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
    }

    [Fact]
    public void RenderTable_OrdersColumnsFromTheRightInRtl()
    {
        var first = new FixedBaselineBox(new Size(10, 10));
        var second = new FixedBaselineBox(new Size(20, 10));
        var third = new FixedBaselineBox(new Size(30, 10));
        var table = new RenderTable(
            columns: 3,
            rows: 1,
            columnWidths: new Dictionary<int, TableColumnWidth>
            {
                [0] = new FixedColumnWidth(10),
                [1] = new FixedColumnWidth(20),
                [2] = new FixedColumnWidth(30),
            },
            defaultColumnWidth: new IntrinsicColumnWidth(),
            rowDecorations: [null],
            border: null,
            textDirection: TextDirection.Rtl);
        table.SetFlatChildren(3, [first, second, third]);

        table.Layout(new BoxConstraints(MaxWidth: 60, MaxHeight: 40));

        Assert.Equal(50, ((TableCellParentData)first.parentData!).offset.X);
        Assert.Equal(30, ((TableCellParentData)second.parentData!).offset.X);
        Assert.Equal(0, ((TableCellParentData)third.parentData!).offset.X);
    }

    [Fact]
    public void Table_RequiresTextBaselineForDefaultBaselineAlignment()
    {
        Assert.Throws<ArgumentException>(() => new Table(
            children: [],
            defaultVerticalAlignment: TableCellVerticalAlignment.Baseline));
    }

    private static Widget BuildDirectionalStack(TextDirection direction)
    {
        return new Directionality(
            direction,
            new Stack(children:
            [
                new PositionedDirectional(
                    start: 6,
                    end: 14,
                    top: 4,
                    child: new SizedBox(width: 10, height: 10)),
            ]));
    }

    private static Widget BuildTableCell(TableCellVerticalAlignment alignment)
    {
        return new Directionality(
            TextDirection.Ltr,
            new Table(
                children:
                [
                    new TableRow(
                    [
                        new TableCell(
                            verticalAlignment: alignment,
                            child: new SizedBox(width: 10, height: 10)),
                    ]),
                ],
                columnWidths: new Dictionary<int, TableColumnWidth>
                {
                    [0] = new FixedColumnWidth(20),
                }));
    }

    private static RenderTable CreateTable(
        IReadOnlyList<RenderBox> children,
        int columns,
        TextDirection textDirection,
        TableCellVerticalAlignment defaultVerticalAlignment = TableCellVerticalAlignment.Top,
        TextBaseline? textBaseline = null)
    {
        var widths = Enumerable.Range(0, columns)
            .ToDictionary(index => index, _ => (TableColumnWidth)new FixedColumnWidth(20));
        var table = new RenderTable(
            columns: columns,
            rows: children.Count / columns,
            columnWidths: widths,
            defaultColumnWidth: new IntrinsicColumnWidth(),
            rowDecorations: Enumerable.Repeat<Decoration?>(null, children.Count / columns).ToArray(),
            border: null,
            textDirection: textDirection,
            defaultVerticalAlignment: defaultVerticalAlignment,
            textBaseline: textBaseline);
        table.SetFlatChildren(columns, [.. children]);
        return table;
    }

    private static T FindRenderObject<T>(Element element) where T : RenderObject
    {
        if (element.RenderObject is T renderObject)
        {
            return renderObject;
        }

        T? result = null;
        element.VisitChildren(child =>
        {
            if (result is null)
            {
                result = FindRenderObjectOrDefault<T>(child);
            }
        });
        return result ?? throw new Xunit.Sdk.XunitException($"Render object {typeof(T).Name} was not found.");
    }

    private static T? FindRenderObjectOrDefault<T>(Element element) where T : RenderObject
    {
        if (element.RenderObject is T renderObject)
        {
            return renderObject;
        }

        T? result = null;
        element.VisitChildren(child =>
        {
            if (result is null)
            {
                result = FindRenderObjectOrDefault<T>(child);
            }
        });
        return result;
    }

    private sealed class FixedBaselineBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly double? _alphabeticBaseline;

        public FixedBaselineBox(Size desiredSize, double? alphabeticBaseline = null)
        {
            _desiredSize = desiredSize;
            _alphabeticBaseline = alphabeticBaseline;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_desiredSize);
        }

        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
        {
            return baseline == TextBaseline.Alphabetic ? _alphabeticBaseline : null;
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
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
}
