using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/rendering/table.dart
// flutter/packages/flutter/lib/src/rendering/table_border.dart
// flutter/packages/flutter/lib/src/widgets/table.dart

public sealed class TableTests
{
    // ---------------------------------------------------------------- column widths

    [Fact]
    public void TableColumnWidth_DefaultsMatchFlutter()
    {
        IReadOnlyList<RenderBox> none = [];

        var flex = new FlexColumnWidth();
        Assert.Equal(1.0, flex.Value);
        Assert.Equal(0.0, flex.MinIntrinsicWidth(none, 100.0));
        Assert.Equal(0.0, flex.MaxIntrinsicWidth(none, 100.0));
        Assert.Equal(1.0, flex.Flex(none));

        var fixedWidth = new FixedColumnWidth(42.0);
        Assert.Equal(42.0, fixedWidth.MinIntrinsicWidth(none, 100.0));
        Assert.Equal(42.0, fixedWidth.MaxIntrinsicWidth(none, 100.0));
        Assert.Null(fixedWidth.Flex(none));

        var fraction = new FractionColumnWidth(0.25);
        Assert.Equal(25.0, fraction.MinIntrinsicWidth(none, 100.0));
        Assert.Equal(25.0, fraction.MaxIntrinsicWidth(none, 100.0));
        Assert.Equal(0.0, fraction.MinIntrinsicWidth(none, double.PositiveInfinity));
        Assert.Equal(0.0, fraction.MaxIntrinsicWidth(none, double.PositiveInfinity));
        Assert.Null(fraction.Flex(none));

        var intrinsic = new IntrinsicColumnWidth();
        Assert.Null(intrinsic.FlexFactor);
        Assert.Null(intrinsic.Flex(none));
        Assert.Equal(2.0, new IntrinsicColumnWidth(2.0).Flex(none));
    }

    [Fact]
    public void IntrinsicColumnWidth_UsesWidestCellIntrinsics()
    {
        IReadOnlyList<RenderBox> cells = [new SizingBox(new Size(30, 10)), new SizingBox(new Size(70, 10))];
        var intrinsic = new IntrinsicColumnWidth();

        Assert.Equal(70.0, intrinsic.MinIntrinsicWidth(cells, 100.0));
        Assert.Equal(70.0, intrinsic.MaxIntrinsicWidth(cells, 100.0));
    }

    [Fact]
    public void MaxAndMinColumnWidth_CombineWidthsAndSkipNullFlex()
    {
        IReadOnlyList<RenderBox> none = [];
        var fixedWidth = new FixedColumnWidth(100.0);
        var flex = new FlexColumnWidth();

        var max = new MaxColumnWidth(fixedWidth, flex);
        Assert.Equal(100.0, max.MinIntrinsicWidth(none, 400.0));
        Assert.Equal(100.0, max.MaxIntrinsicWidth(none, 400.0));
        Assert.Equal(1.0, max.Flex(none));
        Assert.Equal(1.0, new MaxColumnWidth(flex, fixedWidth).Flex(none));

        var min = new MinColumnWidth(fixedWidth, flex);
        Assert.Equal(0.0, min.MinIntrinsicWidth(none, 400.0));
        Assert.Equal(0.0, min.MaxIntrinsicWidth(none, 400.0));
        Assert.Equal(1.0, min.Flex(none));
        Assert.Equal(1.0, new MinColumnWidth(flex, fixedWidth).Flex(none));

        Assert.Equal(2.0, new MaxColumnWidth(new FlexColumnWidth(2.0), flex).Flex(none));
        Assert.Equal(1.0, new MinColumnWidth(new FlexColumnWidth(2.0), flex).Flex(none));
        Assert.Null(new MaxColumnWidth(fixedWidth, new FixedColumnWidth(1.0)).Flex(none));
    }

    // ---------------------------------------------------------------- RenderTable defaults / empty

    [Fact]
    public void RenderTable_DefaultsMatchFlutter()
    {
        var table = new RenderTable();

        Assert.Equal(0, table.Columns);
        Assert.Equal(0, table.Rows);
        Assert.Empty(table.ColumnWidths);
        Assert.Equal(new FlexColumnWidth(1.0), table.DefaultColumnWidth);
        Assert.Null(table.Border);
        Assert.Null(table.RowDecorations);
        Assert.Equal(ImageConfiguration.Empty, table.Configuration);
        Assert.Equal(TableCellVerticalAlignment.Top, table.DefaultVerticalAlignment);
        Assert.Null(table.TextBaseline);
        Assert.Equal(TextDirection.Ltr, table.TextDirection);
    }

    [Fact]
    public void RenderTable_EmptyTableConstrainsToTightSizeAndZeroUnderLooseConstraints()
    {
        var tight = new RenderTable(textDirection: TextDirection.Ltr);
        tight.Layout(BoxConstraints.Tight(new Size(800, 600)));
        Assert.Equal(new Size(800, 600), tight.Size);

        var loose = new RenderTable(textDirection: TextDirection.Ltr);
        loose.Layout(new BoxConstraints(MaxWidth: 800, MaxHeight: 600));
        Assert.Equal(new Size(0, 0), loose.Size);
    }

    [Fact]
    public void RenderTable_EmptyTableIntrinsicDimensionsDoNotCrash()
    {
        var table = new RenderTable(textDirection: TextDirection.Ltr);

        foreach (double extent in new[] { 100.0, double.PositiveInfinity })
        {
            Assert.Equal(0.0, table.GetMinIntrinsicWidth(extent));
            Assert.Equal(0.0, table.GetMaxIntrinsicWidth(extent));
            Assert.Equal(0.0, table.GetMinIntrinsicHeight(extent));
            Assert.Equal(0.0, table.GetMaxIntrinsicHeight(extent));
        }
    }

    // ---------------------------------------------------------------- layout

    [Fact]
    public void RenderTable_ConstrainedFlexColumnsShareTheAvailableWidthEvenly()
    {
        var cells = Enumerable.Range(0, 6).Select(_ => new SizingBox(new Size(0, 10))).ToArray();
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        table.SetFlatChildren(6, cells);

        table.Layout(BoxConstraints.TightFor(width: 100.0));

        foreach (SizingBox cell in cells)
        {
            Assert.Equal(100.0 / 6.0, cell.Size.Width, 9);
        }
    }

    [Fact]
    public void RenderTable_IntrinsicColumnsLayOutCellsAtTheirColumnWidths()
    {
        var table = new RenderTable(
            columns: 5,
            rows: 5,
            defaultColumnWidth: new IntrinsicColumnWidth(),
            textDirection: TextDirection.Ltr,
            defaultVerticalAlignment: TableCellVerticalAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic);
        var loose = new BoxConstraints(MaxWidth: 800, MaxHeight: 600);

        table.Layout(loose);
        Assert.Equal(new Size(0, 0), table.Size);

        var tall = new SizingBox(new Size(100, 200));
        table.SetChild(2, 4, tall);
        table.Layout(loose);
        Assert.Equal(new Size(100, 200), table.Size);

        var a = new SizingBox(new Size(10, 30));
        var b = new SizingBox(new Size(20, 20));
        var c = new SizingBox(new Size(30, 10));
        table.SetChild(0, 0, a);
        table.SetChild(1, 0, b);
        table.SetChild(2, 0, c);
        table.Layout(loose);

        Assert.Equal(new Size(130, 230), table.Size);
        Assert.Equal([10.0, 20.0, 100.0, 0.0, 0.0], table.ResolvedColumnWidths);
        Assert.Equal([30.0, 0.0, 0.0, 0.0, 200.0], table.ResolvedRowHeights);
        Assert.Equal(new Point(0, 0), OffsetOf(a));
        Assert.Equal(new Point(10, 0), OffsetOf(b));
        Assert.Equal(new Point(30, 0), OffsetOf(c));
        Assert.Equal(new Point(30, 30), OffsetOf(tall));

        // The third column is widened by the tall cell below it, so `c` is stretched to 100.
        Assert.Equal(BoxConstraints.TightFor(width: 100.0), c.LastConstraints);
        Assert.Equal(new Size(100, 10), c.Size);
    }

    [Fact]
    public void RenderTable_PositionsColumnsRightToLeftUnderRtl()
    {
        var a = new SizingBox(new Size(10, 10));
        var b = new SizingBox(new Size(20, 10));
        var c = new SizingBox(new Size(30, 10));
        var table = new RenderTable(
            columnWidths: new Dictionary<int, TableColumnWidth>
            {
                [0] = new FixedColumnWidth(10),
                [1] = new FixedColumnWidth(20),
                [2] = new FixedColumnWidth(30),
            },
            textDirection: TextDirection.Rtl);
        table.SetFlatChildren(3, [a, b, c]);

        table.Layout(new BoxConstraints(MaxWidth: 60, MaxHeight: 40));

        Assert.Equal(50.0, OffsetOf(a).X);
        Assert.Equal(30.0, OffsetOf(b).X);
        Assert.Equal(0.0, OffsetOf(c).X);
    }

    [Fact]
    public void RenderTable_VerticalAlignmentsPlaceCellsWithinTheRow()
    {
        var top = new SizingBox(new Size(10, 10));
        var middle = new SizingBox(new Size(10, 10));
        var bottom = new SizingBox(new Size(10, 10));
        var fill = new SizingBox(new Size(10, 10));
        var intrinsic = new SizingBox(new Size(10, 10));
        var tall = new SizingBox(new Size(10, 40));
        SetAlignment(top, TableCellVerticalAlignment.Top);
        SetAlignment(middle, TableCellVerticalAlignment.Middle);
        SetAlignment(bottom, TableCellVerticalAlignment.Bottom);
        SetAlignment(fill, TableCellVerticalAlignment.Fill);
        SetAlignment(intrinsic, TableCellVerticalAlignment.IntrinsicHeight);

        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(10),
            textDirection: TextDirection.Ltr);
        table.SetFlatChildren(6, [top, middle, bottom, fill, intrinsic, tall]);
        table.Layout(new BoxConstraints(MaxWidth: 200, MaxHeight: 200));

        Assert.Equal(40.0, table.ResolvedRowHeights[0]);
        Assert.Equal(0.0, OffsetOf(top).Y);
        Assert.Equal(15.0, OffsetOf(middle).Y);
        Assert.Equal(30.0, OffsetOf(bottom).Y);
        Assert.Equal(0.0, OffsetOf(fill).Y);
        Assert.Equal(0.0, OffsetOf(intrinsic).Y);
        Assert.Equal(40.0, fill.Size.Height);
        Assert.Equal(40.0, intrinsic.Size.Height);
    }

    [Fact]
    public void RenderTable_RowOfOnlyFillCellsHasZeroHeight()
    {
        var first = new SizingBox(new Size(10, 30));
        var second = new SizingBox(new Size(10, 40));
        SetAlignment(first, TableCellVerticalAlignment.Fill);
        SetAlignment(second, TableCellVerticalAlignment.Fill);
        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(10),
            textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [first, second]);

        table.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(0.0, table.ResolvedRowHeights[0]);
        Assert.Equal(0.0, table.Size.Height);
        Assert.Equal(0.0, first.Size.Height);
    }

    [Fact]
    public void RenderTable_IntrinsicHeightMatchesTheTallestCellPerRow()
    {
        var a = new SizingBox(new Size(10, 100));
        var b = new SizingBox(new Size(10, 200));
        var c = new SizingBox(new Size(10, 200));
        var d = new SizingBox(new Size(10, 300));
        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(10),
            textDirection: TextDirection.Ltr,
            defaultVerticalAlignment: TableCellVerticalAlignment.IntrinsicHeight);
        table.SetFlatChildren(2, [a, b, c, d]);

        table.Layout(new BoxConstraints(MaxWidth: 400, MaxHeight: 1000));

        Assert.Equal(a.Size.Height, b.Size.Height);
        Assert.Equal(c.Size.Height, d.Size.Height);
        Assert.Equal(200.0, a.Size.Height);
        Assert.Equal(300.0, d.Size.Height);
    }

    [Fact]
    public void RenderTable_BaselineAlignmentSharesTheRowBaselineAndExposesRowZero()
    {
        var shallow = new SizingBox(new Size(10, 30), alphabeticBaseline: 10);
        var deep = new SizingBox(new Size(10, 30), alphabeticBaseline: 25);
        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(10),
            textDirection: TextDirection.Ltr,
            defaultVerticalAlignment: TableCellVerticalAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic);
        table.SetFlatChildren(2, [shallow, deep]);

        table.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(15.0, OffsetOf(shallow).Y);
        Assert.Equal(0.0, OffsetOf(deep).Y);
        // beforeBaseline = max(10, 25) = 25, afterBaseline = max(30 - 10, 30 - 25) = 20.
        Assert.Equal(45.0, table.ResolvedRowHeights[0]);
        Assert.Equal(25.0, table.GetDistanceToBaseline(TextBaseline.Alphabetic));
    }

    [Fact]
    public void RenderTable_BaselineAlignmentWithoutTextBaselineThrows()
    {
        var cell = new SizingBox(new Size(10, 10), alphabeticBaseline: 5);
        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(10),
            textDirection: TextDirection.Ltr,
            defaultVerticalAlignment: TableCellVerticalAlignment.Baseline);
        table.SetFlatChildren(1, [cell]);

        var error = Assert.Throws<InvalidOperationException>(
            () => table.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100)));
        Assert.Contains("textBaseline", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderTable_FlexColumnsShrinkToTheirMinimumBeforeOverflowing()
    {
        var narrow = new SizingBox(new Size(40, 10));
        var wide = new SizingBox(new Size(40, 10));
        var table = new RenderTable(
            columnWidths: new Dictionary<int, TableColumnWidth>
            {
                [0] = new MaxColumnWidth(new FlexColumnWidth(), new FixedColumnWidth(80)),
                [1] = new FlexColumnWidth(),
            },
            textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [narrow, wide]);

        table.Layout(BoxConstraints.TightFor(width: 100.0));

        // Column 0 cannot go below its 80px minimum, so the deficit is drained from column 1.
        Assert.Equal(80.0, table.ResolvedColumnWidths[0], 9);
        Assert.Equal(20.0, table.ResolvedColumnWidths[1], 9);
    }

    [Fact]
    public void RenderTable_TinyFlexDeficitsTerminate()
    {
        var cells = Enumerable.Range(0, 12).Select(_ => new SizingBox(new Size(16, 16))).ToArray();
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        table.SetFlatChildren(6, cells);
        table.Layout(BoxConstraints.Tight(new Size(96, 32)));
        Assert.Equal(new Size(96, 32), table.Size);

        var widths = new Dictionary<int, TableColumnWidth> { [0] = new FlexColumnWidth() };
        for (int column = 1; column < 7; column++)
        {
            widths[column] = new FlexColumnWidth(0.123);
        }

        var flexCells = Enumerable.Range(0, 7).Select(_ => new SizingBox(new Size(1, 1))).ToArray();
        var flexTable = new RenderTable(columnWidths: widths, textDirection: TextDirection.Ltr);
        flexTable.SetFlatChildren(7, flexCells);
        flexTable.Layout(BoxConstraints.Tight(new Size(600, 800)));
        Assert.Equal(new Size(600, 800), flexTable.Size);
    }

    [Fact]
    public void RenderTable_FractionColumnWidthUsesTheIncomingMaxWidth()
    {
        var a = new SizingBox(new Size(10, 10));
        var b = new SizingBox(new Size(10, 10));
        var table = new RenderTable(
            columnWidths: new Dictionary<int, TableColumnWidth>
            {
                [0] = new FractionColumnWidth(0.25),
                [1] = new FractionColumnWidth(0.5),
            },
            textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [a, b]);

        table.Layout(new BoxConstraints(MaxWidth: 400, MaxHeight: 100));

        Assert.Equal(100.0, table.ResolvedColumnWidths[0]);
        Assert.Equal(200.0, table.ResolvedColumnWidths[1]);
        Assert.Equal(300.0, table.Size.Width);
    }

    [Fact]
    public void RenderTable_IntrinsicDimensionsSumColumnsAndRows()
    {
        var a = new SizingBox(new Size(10, 30));
        var b = new SizingBox(new Size(20, 40));
        var table = new RenderTable(
            defaultColumnWidth: new IntrinsicColumnWidth(),
            textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [a, b]);

        Assert.Equal(30.0, table.GetMinIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(30.0, table.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(40.0, table.GetMinIntrinsicHeight(100.0));
        Assert.Equal(40.0, table.GetMaxIntrinsicHeight(100.0));
    }

    [Fact]
    public void RenderTable_DryLayoutMatchesLayoutAndRefusesBaselineRows()
    {
        var a = new SizingBox(new Size(10, 30));
        var b = new SizingBox(new Size(20, 40));
        var table = new RenderTable(
            defaultColumnWidth: new IntrinsicColumnWidth(),
            textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [a, b]);
        var constraints = new BoxConstraints(MaxWidth: 400, MaxHeight: 400);

        Assert.Equal(new Size(30, 40), table.GetDryLayout(constraints));

        table.DefaultVerticalAlignment = TableCellVerticalAlignment.Baseline;
        table.TextBaseline = TextBaseline.Alphabetic;
        Assert.Throws<InvalidOperationException>(() => table.GetDryLayout(constraints));
    }

    // ---------------------------------------------------------------- child mutation

    [Fact]
    public void RenderTable_SetFlatChildrenDerivesRowCountAndKeepsMovedChildren()
    {
        var cells = Enumerable.Range(0, 6).Select(_ => new SizingBox(new Size(10, 10))).ToArray();
        var table = new RenderTable(textDirection: TextDirection.Ltr);

        table.SetFlatChildren(3, cells);
        Assert.Equal(3, table.Columns);
        Assert.Equal(2, table.Rows);

        RenderBox?[] shifted = [null, .. cells.Take(5)];
        table.SetFlatChildren(3, shifted);
        Assert.Equal(3, table.Columns);
        Assert.Equal(2, table.Rows);
        Assert.Same(cells[0], table.Row(0)[0]);

        RenderBox?[] shiftedAgain = [null, null, .. cells.Take(4)];
        table.SetFlatChildren(3, shiftedAgain);
        Assert.Equal(3, table.Columns);
        Assert.Equal(2, table.Rows);
        foreach (SizingBox cell in cells.Take(4))
        {
            Assert.Same(table, cell.Parent);
        }

        // Cells pushed out of the grid are dropped.
        Assert.Null(cells[5].Parent);
    }

    [Fact]
    public void RenderTable_ShrinkingRowsAndColumnsDropsTheRemovedCells()
    {
        var cells = Enumerable.Range(0, 25).Select(_ => new SizingBox(new Size(10, 10))).ToArray();
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        table.SetFlatChildren(5, cells);

        RenderBox lastCell = cells[24];
        Assert.Same(table, lastCell.Parent);
        table.Rows = 4;
        Assert.Equal(4, table.Rows);
        Assert.Null(lastCell.Parent);

        RenderBox lastColumnCell = cells[4];
        Assert.Same(table, lastColumnCell.Parent);
        table.Columns = 4;
        Assert.Equal(4, table.Columns);
        Assert.Null(lastColumnCell.Parent);
        Assert.Same(cells[0], table.Row(0)[0]);
        Assert.Equal(4, table.Row(0).Count);
    }

    [Fact]
    public void RenderTable_AddRowAndSetChildMaintainTheGrid()
    {
        var table = new RenderTable(columns: 2, textDirection: TextDirection.Ltr);
        var a = new SizingBox(new Size(10, 10));
        var b = new SizingBox(new Size(10, 10));
        table.AddRow([a, b]);

        Assert.Equal(1, table.Rows);
        Assert.Equal([a, b], table.Row(0));
        Assert.Equal([a], table.Column(0));

        var replacement = new SizingBox(new Size(10, 10));
        table.SetChild(0, 0, replacement);
        Assert.Null(a.Parent);
        Assert.Same(replacement, table.Row(0)[0]);

        table.SetChild(0, 0, null);
        Assert.Single(table.Row(0));
        Assert.Throws<ArgumentException>(() => table.AddRow([a]));
    }

    [Fact]
    public void RenderTable_ColumnWidthSettersSkipRedundantWork()
    {
        var cell = new SizingBox(new Size(10, 10));
        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(10),
            textDirection: TextDirection.Ltr);
        table.SetFlatChildren(1, [cell]);
        var constraints = new BoxConstraints(MaxWidth: 50, MaxHeight: 50);
        table.Layout(constraints);
        IReadOnlyList<double> widths = table.ResolvedColumnWidths;

        // A fresh but equal column-width configuration must not dirty the layout; every
        // PerformLayout pass publishes a new width array, so identity is the probe.
        table.ColumnWidths = new Dictionary<int, TableColumnWidth>();
        table.Layout(constraints);
        Assert.Same(widths, table.ResolvedColumnWidths);

        table.DefaultColumnWidth = new FixedColumnWidth(10);
        table.Layout(constraints);
        Assert.Same(widths, table.ResolvedColumnWidths);

        table.SetColumnWidth(0, new FixedColumnWidth(20));
        table.Layout(constraints);
        Assert.NotSame(widths, table.ResolvedColumnWidths);
        Assert.Equal(20.0, table.ResolvedColumnWidths[0]);
    }

    // ---------------------------------------------------------------- paint

    [Fact]
    public void RenderTable_PaintsRowDecorationsAcrossEachRowBox()
    {
        var decoration = new RecordingDecoration();
        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(40),
            textDirection: TextDirection.Ltr,
            rowDecorations: [decoration, null]);
        table.SetFlatChildren(1, [new SizingBox(new Size(10, 20)), new SizingBox(new Size(10, 30))]);

        PaintThroughPipeline(table, new Size(100, 100));

        (Point offset, ImageConfiguration configuration) = Assert.Single(decoration.Painted);
        Assert.Equal(new Point(0, 0), offset);
        Assert.Equal(new Size(table.Size.Width, 20), configuration.Size);
    }

    [Fact]
    public void RenderTable_RowDecorationPaintersAreDisposedWhenReplaced()
    {
        var decoration = new RecordingDecoration();
        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(10),
            textDirection: TextDirection.Ltr,
            rowDecorations: [decoration]);
        table.SetFlatChildren(1, [new SizingBox(new Size(10, 10))]);
        PaintThroughPipeline(table, new Size(100, 100));
        Assert.Single(decoration.Painted);
        Assert.Equal(0, decoration.DisposedPainters);

        table.RowDecorations = null;

        Assert.Equal(1, decoration.DisposedPainters);
        Assert.Null(table.RowDecorations);
    }

    [Fact]
    public void RenderTable_HitTestsCellsInReversePaintOrder()
    {
        var first = new HitBox();
        var second = new HitBox();
        var table = new RenderTable(
            defaultColumnWidth: new FixedColumnWidth(20),
            textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [first, second]);
        table.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        var result = new BoxHitTestResult();
        Assert.True(table.HitTest(result, new Point(25, 5)));
        Assert.Equal(new Point(5, 5), second.LastPosition);
    }

    [Fact]
    public void RenderTable_DeclaresTheTableSemanticsRoleAsAnExplicitBoundary()
    {
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        var configuration = new SemanticsConfiguration();

        table.InvokeDescribeSemanticsConfiguration(configuration);

        Assert.Equal(SemanticsRole.Table, configuration.Role);
        Assert.True(configuration.IsSemanticBoundary);
        Assert.True(configuration.ExplicitChildNodes);
    }

    [Fact]
    public void RenderTable_SynthesizesOneRowNodePerRowAndKeepsCellRoledChildren()
    {
        RenderSemanticsAnnotations header = SemanticsCell("Header", SemanticsRole.ColumnHeader);
        RenderSemanticsAnnotations trailingHeader = SemanticsCell("Trailing", SemanticsRole.ColumnHeader);
        RenderSemanticsAnnotations first = SemanticsCell("First", SemanticsRole.Cell);
        RenderSemanticsAnnotations second = SemanticsCell("Second", SemanticsRole.Cell);
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [header, trailingHeader, first, second]);

        SemanticsNode tableNode = FlushTableSemantics(table, new Size(100, 100)).TableNode;

        Assert.Equal(SemanticsRole.Table, tableNode.Role);
        Assert.Collection(
            tableNode.Children,
            row =>
            {
                Assert.Equal(SemanticsRole.Row, row.Role);
                Assert.Equal(0, row.IndexInParent);
                Assert.Equal(new Rect(0, 0, 100, 10), row.GlobalRect);
                Assert.Collection(
                    row.Children,
                    cell =>
                    {
                        // A ColumnHeader/Cell child is used as-is: no wrapper node is inserted.
                        Assert.Same(FindNodeWithLabel(tableNode, "Header"), cell);
                        Assert.Equal(SemanticsRole.ColumnHeader, cell.Role);
                        Assert.Equal(0, cell.IndexInParent);
                    },
                    cell =>
                    {
                        Assert.Equal("Trailing", cell.Label);
                        Assert.Equal(1, cell.IndexInParent);
                    });
            },
            row =>
            {
                Assert.Equal(SemanticsRole.Row, row.Role);
                Assert.Equal(1, row.IndexInParent);
                Assert.Equal(new Rect(0, 10, 100, 10), row.GlobalRect);
                Assert.Collection(
                    row.Children,
                    cell =>
                    {
                        Assert.Equal("First", cell.Label);
                        Assert.Equal(SemanticsRole.Cell, cell.Role);
                        Assert.Equal(0, cell.IndexInParent);
                    },
                    cell =>
                    {
                        Assert.Equal("Second", cell.Label);
                        Assert.Equal(1, cell.IndexInParent);
                    });
            });
    }

    [Fact]
    public void RenderTable_WrapsChildrenThatDoNotAlreadyCarryACellRole()
    {
        RenderSemanticsAnnotations plain = SemanticsCell("Plain", SemanticsRole.None);
        RenderSemanticsAnnotations celled = SemanticsCell("Celled", SemanticsRole.Cell);
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [plain, celled]);

        SemanticsNode tableNode = FlushTableSemantics(table, new Size(100, 100)).TableNode;

        SemanticsNode row = Assert.Single(tableNode.Children);
        SemanticsNode wrapper = row.Children[0];
        Assert.Equal(SemanticsRole.Cell, wrapper.Role);
        Assert.Null(wrapper.Label);
        Assert.Equal(0, wrapper.IndexInParent);
        // The wrapper is clipped to its column, not to the whole row.
        Assert.Equal(new Rect(0, 0, 50, 10), wrapper.GlobalRect);
        Assert.Equal("Plain", Assert.Single(wrapper.Children).Label);
        Assert.Same(FindNodeWithLabel(tableNode, "Celled"), row.Children[1]);
    }

    [Fact]
    public void RenderTable_WrapsCellsThatProduceMoreThanOneSemanticsNode()
    {
        RenderSemanticsAnnotations left = SemanticsCell("Left", SemanticsRole.Cell, new Size(20, 10));
        RenderSemanticsAnnotations right = SemanticsCell("Right", SemanticsRole.Cell, new Size(20, 10));
        var pair = new RenderFlex(
            children: [left, right],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        table.SetFlatChildren(2, [pair, SemanticsCell("Other", SemanticsRole.Cell)]);

        SemanticsNode tableNode = FlushTableSemantics(table, new Size(100, 100)).TableNode;

        SemanticsNode row = Assert.Single(tableNode.Children);
        SemanticsNode wrapper = row.Children[0];
        Assert.Equal(SemanticsRole.Cell, wrapper.Role);
        Assert.Collection(
            wrapper.Children,
            child => Assert.Equal("Left", child.Label),
            child => Assert.Equal("Right", child.Label));
    }

    [Fact]
    public void RenderTable_ReusesSynthesizedRowAndCellNodesAcrossSemanticsPasses()
    {
        RenderSemanticsAnnotations plain = SemanticsCell("Plain", SemanticsRole.None);
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        table.SetFlatChildren(1, [plain]);

        (PipelineOwner pipeline, SemanticsNode tableNode) = FlushTableSemantics(table, new Size(100, 100));
        SemanticsNode row = Assert.Single(tableNode.Children);
        SemanticsNode wrapper = row.Children[0];

        plain.Label = "Renamed";
        pipeline.FlushSemantics();

        SemanticsNode rebuiltRow = Assert.Single(FindNodeWithRole(pipeline, SemanticsRole.Table).Children);
        Assert.Same(row, rebuiltRow);
        Assert.Same(wrapper, rebuiltRow.Children[0]);
        Assert.Equal("Renamed", Assert.Single(rebuiltRow.Children[0].Children).Label);
    }

    [Fact]
    public void RenderTable_SkipsEmptyRowsAndZeroWidthCells()
    {
        RenderSemanticsAnnotations visible = SemanticsCell("Visible", SemanticsRole.Cell);
        RenderSemanticsAnnotations collapsed = SemanticsCell("Collapsed", SemanticsRole.Cell, new Size(0, 10));
        var table = new RenderTable(
            columnWidths: new Dictionary<int, TableColumnWidth> { [1] = new FixedColumnWidth(0) },
            defaultColumnWidth: new FixedColumnWidth(50),
            textDirection: TextDirection.Ltr);
        // Row 1 has no children at all, so it collapses to zero height.
        table.SetFlatChildren(2, [visible, collapsed, null, null]);

        SemanticsNode tableNode = FlushTableSemantics(table, new Size(100, 100)).TableNode;

        SemanticsNode row = Assert.Single(tableNode.Children);
        Assert.Equal(0, row.IndexInParent);
        Assert.Equal("Visible", Assert.Single(row.Children).Label);
    }

    [Fact]
    public void RenderTable_ClearSemanticsReleasesTheSynthesizedNodes()
    {
        RenderSemanticsAnnotations plain = SemanticsCell("Plain", SemanticsRole.None);
        var table = new RenderTable(textDirection: TextDirection.Ltr);
        table.SetFlatChildren(1, [plain]);
        var renderView = new RenderView { Child = table };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(100, 100));
        pipeline.FlushSemantics();
        SemanticsNode wrapper = FindNodeWithRole(pipeline, SemanticsRole.Table).Children[0].Children[0];

        renderView.ClearSemantics();
        Assert.Null(table.SemanticsNodeId);

        // Re-hosting the table must not resurrect nodes whose ids came from the previous owner.
        var rehostedView = new RenderView { Child = table };
        var rehostedPipeline = new PipelineOwner(rehostedView);
        rehostedPipeline.Attach(rehostedView);
        rehostedPipeline.FlushLayout(new Size(100, 100));
        rehostedPipeline.FlushSemantics();

        SemanticsNode rebuiltWrapper = FindNodeWithRole(rehostedPipeline, SemanticsRole.Table).Children[0].Children[0];
        Assert.NotSame(wrapper, rebuiltWrapper);
        Assert.Equal(SemanticsRole.Cell, rebuiltWrapper.Role);
    }

    private static RenderSemanticsAnnotations SemanticsCell(string label, SemanticsRole role, Size? size = null)
    {
        return new RenderSemanticsAnnotations(
            label: label,
            role: role,
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(size ?? new Size(20, 10))));
    }

    private static (PipelineOwner Pipeline, SemanticsNode TableNode) FlushTableSemantics(
        RenderTable table,
        Size viewSize)
    {
        var renderView = new RenderView { Child = table };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(viewSize);
        pipeline.FlushSemantics();
        return (pipeline, FindNodeWithRole(pipeline, SemanticsRole.Table));
    }

    private static SemanticsNode FindNodeWithRole(PipelineOwner pipeline, SemanticsRole role)
    {
        SemanticsNode? found = FindNode(pipeline.SemanticsOwner.RootNode, node => node.Role == role);
        Assert.NotNull(found);
        return found;
    }

    private static SemanticsNode FindNodeWithLabel(SemanticsNode root, string label)
    {
        SemanticsNode? found = FindNode(root, node => node.Label == label);
        Assert.NotNull(found);
        return found;
    }

    private static SemanticsNode? FindNode(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null) return null;
        if (predicate(node)) return node;
        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? found = FindNode(child, predicate);
            if (found is not null) return found;
        }

        return null;
    }

    // ---------------------------------------------------------------- TableBorder

    [Fact]
    public void TableBorder_DefaultsAndFactoriesMatchFlutter()
    {
        var empty = new TableBorder();
        Assert.Equal(BorderSide.None, empty.Top);
        Assert.Equal(BorderSide.None, empty.VerticalInside);
        Assert.Equal(BorderRadius.Zero, empty.BorderRadius);
        Assert.True(empty.IsUniform);

        var all = TableBorder.All();
        Assert.Equal(Avalonia.Media.Color.FromUInt32(0xFF000000), all.Top.Color);
        Assert.Equal(1.0, all.Top.Width);
        Assert.Equal(BorderStyle.Solid, all.HorizontalInside.Style);
        Assert.True(all.IsUniform);
        Assert.Equal(new Thickness(1, 1, 1, 1), all.Dimensions);

        var side = new BorderSide(Avalonia.Media.Colors.Red, 2.0);
        var symmetric = TableBorder.Symmetric(inside: side);
        Assert.Equal(side, symmetric.HorizontalInside);
        Assert.Equal(side, symmetric.VerticalInside);
        Assert.Equal(BorderSide.None, symmetric.Top);
        Assert.False(symmetric.IsUniform);
    }

    [Fact]
    public void TableBorder_LerpAndScaleInterpolateEverySide()
    {
        var a = TableBorder.All(color: Avalonia.Media.Colors.Black, width: 4.0);
        var b = TableBorder.All(color: Avalonia.Media.Colors.Black, width: 8.0);

        TableBorder? mid = TableBorder.Lerp(a, b, 0.5);
        Assert.Equal(6.0, mid!.Top.Width);
        Assert.Equal(6.0, mid.HorizontalInside.Width);

        Assert.Equal(2.0, a.Scale(0.5).Top.Width);
        Assert.Equal(BorderStyle.None, a.Scale(0.0).Top.Style);
        Assert.Equal(2.0, TableBorder.Lerp(null, a, 0.5)!.Top.Width);
        Assert.Equal(2.0, TableBorder.Lerp(a, null, 0.5)!.Top.Width);
    }

    // ---------------------------------------------------------------- Table widget

    [Fact]
    public void Table_DefaultsMatchFlutter()
    {
        var table = new Table();

        Assert.Empty(table.Children);
        Assert.Null(table.ColumnWidths);
        Assert.Equal(new FlexColumnWidth(1.0), table.DefaultColumnWidth);
        Assert.Null(table.TextDirection);
        Assert.Null(table.Border);
        Assert.Equal(TableCellVerticalAlignment.Top, table.DefaultVerticalAlignment);
        Assert.Null(table.TextBaseline);
        Assert.Equal(0, table.ColumnCount);
    }

    [Fact]
    public void TableRow_DefaultsMatchFlutter()
    {
        var row = new TableRow();

        Assert.Empty(row.Children);
        Assert.Null(row.Key);
        Assert.Null(row.Decoration);
        Assert.Equal("TableRow(no children)", row.ToString());
    }

    [Fact]
    public void Table_RejectsIrregularEmptyAndDuplicatelyKeyedRows()
    {
        var irregular = Assert.Throws<ArgumentException>(() => new Table(children:
        [
            new TableRow([new SizedBox()]),
            new TableRow(),
        ]));
        Assert.Contains("Table contains irregular row lengths.", irregular.Message, StringComparison.Ordinal);

        var empty = Assert.Throws<ArgumentException>(() => new Table(children: [new TableRow()]));
        Assert.Contains("One or more TableRow have no children.", empty.Message, StringComparison.Ordinal);

        var duplicateRowKey = Assert.Throws<ArgumentException>(() => new Table(children:
        [
            new TableRow([new SizedBox()], key: new ValueKey<int>(1)),
            new TableRow([new SizedBox()], key: new ValueKey<int>(1)),
        ]));
        Assert.Contains("had the same key", duplicateRowKey.Message, StringComparison.Ordinal);

        var duplicateCellKey = Assert.Throws<ArgumentException>(() => new Table(children:
        [
            new TableRow([new SizedBox(key: new ValueKey<int>(7))]),
            new TableRow([new SizedBox(key: new ValueKey<int>(7))]),
        ]));
        Assert.Contains("same key", duplicateCellKey.Message, StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() => new Table(
            children: [],
            defaultVerticalAlignment: TableCellVerticalAlignment.Baseline));
    }

    [Fact]
    public void Table_EmptyTableBuilds()
    {
        var owner = new BuildOwner();
        var root = Mount(owner, new Directionality(TextDirection.Ltr, new Table()));

        var table = FindRenderObject<RenderTable>(root);
        Assert.Equal(0, table.Columns);
        Assert.Equal(0, table.Rows);
    }

    [Theory]
    [InlineData(TextDirection.Ltr)]
    [InlineData(TextDirection.Rtl)]
    public void Table_DefaultFlexColumnsGiveEveryCellTheSameWidth(TextDirection direction)
    {
        var owner = new BuildOwner();
        var root = Mount(owner, new Directionality(direction, BuildGrid(3, 3)));
        var table = FindRenderObject<RenderTable>(root);

        table.Layout(BoxConstraints.Tight(new Size(300, 300)));

        Assert.Equal([100.0, 100.0, 100.0], table.ResolvedColumnWidths);
    }

    [Theory]
    [InlineData(TextDirection.Ltr)]
    [InlineData(TextDirection.Rtl)]
    public void Table_ColumnOffsetsFollowTheAmbientDirection(TextDirection direction)
    {
        var widths = new Dictionary<int, TableColumnWidth>
        {
            [0] = new FixedColumnWidth(100),
            [1] = new FixedColumnWidth(110),
            [2] = new FixedColumnWidth(125),
        };
        var owner = new BuildOwner();
        var root = Mount(owner, new Directionality(direction, BuildGrid(3, 3, widths)));
        var table = FindRenderObject<RenderTable>(root);

        table.Layout(new BoxConstraints(MaxWidth: 800, MaxHeight: 600));

        Assert.Equal(335.0, table.Size.Width);
        IReadOnlyList<RenderBox> row = table.Row(0);
        double[] expected = direction == TextDirection.Ltr
            ? [0.0, 100.0, 210.0]
            : [235.0, 125.0, 0.0];
        Assert.Equal(expected, row.Select(cell => ((BoxParentData)cell.parentData!).offset.X));
    }

    [Fact]
    public void Table_ChangingDimensionsReusesSurvivingCells()
    {
        var owner = new BuildOwner();
        var root = Mount(owner, new Directionality(TextDirection.Ltr, BuildGrid(3, 3)));
        var table = FindRenderObject<RenderTable>(root);
        RenderBox firstCellBefore = table.Row(0)[0];
        RenderBox lastRowCellBefore = table.Row(2)[0];

        root.Update(new Directionality(TextDirection.Ltr, BuildGrid(2, 4)));
        owner.FlushBuild();

        Assert.Same(table, FindRenderObject<RenderTable>(root));
        Assert.Equal(4, table.Columns);
        Assert.Equal(2, table.Rows);
        Assert.Same(firstCellBefore, table.Row(0)[0]);
        Assert.Null(lastRowCellBefore.Parent);
    }

    [Fact]
    public void Table_MovingAKeyedRowPreservesItsCellElements()
    {
        var owner = new BuildOwner();
        var root = Mount(owner, new Directionality(TextDirection.Ltr, BuildKeyedRows([1, 2])));
        var table = FindRenderObject<RenderTable>(root);
        RenderBox keyedCellBefore = table.Row(0)[0];

        root.Update(new Directionality(TextDirection.Ltr, BuildKeyedRows([2, 1])));
        owner.FlushBuild();

        Assert.Same(keyedCellBefore, table.Row(1)[0]);
    }

    [Fact]
    public void Table_RemovingAKeyedRowUnmountsOnlyThatRow()
    {
        var owner = new BuildOwner();
        var root = Mount(owner, new Directionality(TextDirection.Ltr, BuildKeyedRows([1, 2])));
        var table = FindRenderObject<RenderTable>(root);
        RenderBox removed = table.Row(0)[0];
        RenderBox kept = table.Row(1)[0];

        root.Update(new Directionality(TextDirection.Ltr, BuildKeyedRows([2])));
        owner.FlushBuild();

        Assert.Equal(1, table.Rows);
        Assert.Null(removed.Parent);
        Assert.Same(kept, table.Row(0)[0]);
    }

    [Fact]
    public void Table_SwitchingDefaultColumnWidthRelayouts()
    {
        var owner = new BuildOwner();
        var root = Mount(owner, new Directionality(TextDirection.Ltr, BuildGrid(1, 2)));
        var table = FindRenderObject<RenderTable>(root);
        var constraints = BoxConstraints.Tight(new Size(300, 40));
        table.Layout(constraints);
        Assert.Equal([150.0, 150.0], table.ResolvedColumnWidths);

        root.Update(new Directionality(
            TextDirection.Ltr,
            BuildGrid(1, 2, defaultColumnWidth: new IntrinsicColumnWidth())));
        owner.FlushBuild();

        // Same constraints: the widths only change if the new column width dirtied the layout.
        table.Layout(constraints);
        Assert.Equal([140.0, 160.0], table.ResolvedColumnWidths);
    }

    [Fact]
    public void Table_RowDecorationsAreForwardedOnlyWhenPresent()
    {
        var owner = new BuildOwner();
        var root = Mount(owner, new Directionality(TextDirection.Ltr, BuildGrid(1, 1)));
        var table = FindRenderObject<RenderTable>(root);
        Assert.Null(table.RowDecorations);

        var decoration = new BoxDecoration(Color: Avalonia.Media.Colors.Red);
        root.Update(new Directionality(TextDirection.Ltr, BuildGrid(1, 1, rowDecoration: decoration)));
        owner.FlushBuild();

        Assert.Equal([decoration], table.RowDecorations);
    }

    // ---------------------------------------------------------------- helpers

    private static Point OffsetOf(RenderBox box) => ((BoxParentData)box.parentData!).offset;

    private static void PaintThroughPipeline(RenderTable table, Size viewSize)
    {
        var renderView = new RenderView { Child = table };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(viewSize);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
    }

    private static void SetAlignment(RenderBox box, TableCellVerticalAlignment alignment)
    {
        box.parentData = new TableCellParentData { VerticalAlignment = alignment };
    }

    private static Widget BuildGrid(
        int rows,
        int columns,
        IReadOnlyDictionary<int, TableColumnWidth>? columnWidths = null,
        TableColumnWidth? defaultColumnWidth = null,
        Decoration? rowDecoration = null)
    {
        var tableRows = new List<TableRow>();
        for (int row = 0; row < rows; row++)
        {
            var cells = new List<Widget>();
            for (int column = 0; column < columns; column++)
            {
                cells.Add(new SizedBox(width: 20.0 * (column + 1), height: 10.0 * (row + 1)));
            }

            tableRows.Add(new TableRow(cells, decoration: rowDecoration));
        }

        return new Table(
            children: tableRows,
            columnWidths: columnWidths,
            defaultColumnWidth: defaultColumnWidth);
    }

    private static Widget BuildKeyedRows(IReadOnlyList<int> keys)
    {
        return new Table(children:
        [
            .. keys.Select(key => new TableRow(
                [new SizedBox(width: 10, height: 10, key: new ValueKey<int>(key * 100))],
                key: new ValueKey<int>(key))),
        ]);
    }

    private static TestRootElement Mount(BuildOwner owner, Widget widget)
    {
        var root = new TestRootElement(widget);
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        return root;
    }

    private static T FindRenderObject<T>(Element element) where T : RenderObject
    {
        return FindRenderObjectOrDefault<T>(element)
               ?? throw new Xunit.Sdk.XunitException($"Render object {typeof(T).Name} was not found.");
    }

    private static T? FindRenderObjectOrDefault<T>(Element element) where T : RenderObject
    {
        if (element.RenderObject is T renderObject)
        {
            return renderObject;
        }

        T? result = null;
        element.VisitChildren(child => result ??= FindRenderObjectOrDefault<T>(child));
        return result;
    }

    private sealed class SizingBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly double? _alphabeticBaseline;

        public SizingBox(Size desiredSize, double? alphabeticBaseline = null)
        {
            _desiredSize = desiredSize;
            _alphabeticBaseline = alphabeticBaseline;
        }

        public BoxConstraints LastConstraints { get; private set; }

        protected override double ComputeMinIntrinsicWidth(double height) => _desiredSize.Width;

        protected override double ComputeMaxIntrinsicWidth(double height) => _desiredSize.Width;

        protected override double ComputeMinIntrinsicHeight(double width) => _desiredSize.Height;

        protected override double ComputeMaxIntrinsicHeight(double width) => _desiredSize.Height;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_desiredSize);

        protected override void PerformLayout()
        {
            LastConstraints = Constraints;
            Size = Constraints.Constrain(_desiredSize);
        }

        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) =>
            baseline == TextBaseline.Alphabetic ? _alphabeticBaseline : null;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class HitBox : RenderBox
    {
        public Point LastPosition { get; private set; }

        protected override void PerformLayout() => Size = Constraints.Constrain(new Size(20, 10));

        protected override bool HitTestSelf(Point position)
        {
            LastPosition = position;
            return true;
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed record RecordingDecoration : Decoration
    {
        public List<(Point Offset, ImageConfiguration Configuration)> Painted { get; } = [];

        public int DisposedPainters { get; private set; }

        public override BoxPainter CreateBoxPainter(Action? onChanged = null) => new Painter(this, onChanged);

        private sealed class Painter(RecordingDecoration owner, Action? onChanged) : BoxPainter(onChanged)
        {
            public override void Paint(PaintingContext context, Point offset, ImageConfiguration configuration)
            {
                owner.Painted.Add((offset, configuration));
            }

            public override void Dispose()
            {
                owner.DisposedPainters += 1;
                base.Dispose();
            }
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
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
