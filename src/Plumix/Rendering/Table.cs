using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/table.dart

/// Parent data used by [RenderTable] for its children.
public sealed class TableCellParentData : BoxParentData
{
    /// Where this cell should be placed vertically.
    ///
    /// When using [TableCellVerticalAlignment.baseline], the text baseline must be
    /// configured on the [RenderTable] itself.
    public TableCellVerticalAlignment? VerticalAlignment { get; set; }

    /// The column that the child was in the last time it was laid out.
    public int? X { get; internal set; }

    /// The row that the child was in the last time it was laid out.
    public int? Y { get; internal set; }

    public override string ToString() =>
        $"{base.ToString()}; {(VerticalAlignment is null ? "default vertical alignment" : VerticalAlignment)}";
}

/// Base class to describe how wide a column in a [RenderTable] should be.
///
/// To size a column to a specific number of pixels, use a [FixedColumnWidth].
/// This is the cheapest way to size a column.
///
/// Other algorithms that are relatively cheap include [FlexColumnWidth], which
/// distributes the space equally among the flexible columns,
/// and [FractionColumnWidth], which sizes a column based on the size of the
/// table's container.
public abstract record TableColumnWidth
{
    /// The smallest width that the column can have.
    ///
    /// The `cells` argument is an iterable that provides all the cells
    /// in the table for this column. Walking the cells is by definition O(N), so
    /// algorithms that do that should be considered expensive.
    ///
    /// The `containerWidth` argument is the `maxWidth` of the incoming
    /// constraints of the table, and might be infinite.
    public abstract double MinIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth);

    /// The ideal width that the column should have. This must be equal
    /// to or greater than the [MinIntrinsicWidth]. The column might be
    /// bigger than this width, e.g. if the column is flexible or if the
    /// table's width ends up being forced to be bigger than the sum of
    /// all the maxIntrinsicWidth values.
    public abstract double MaxIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth);

    /// The flex factor to apply to the cell if there is any room left
    /// over when laying out the table. The remaining space is
    /// distributed to any columns with flex in proportion to their flex
    /// value (higher values get more space).
    ///
    /// The `cells` argument is an iterable that provides all the cells
    /// in the table for this column.
    ///
    /// Return null if the column should not be flexible.
    public virtual double? Flex(IReadOnlyList<RenderBox> cells) => null;
}

/// Sizes the column according to the intrinsic dimensions of all the
/// cells in that column.
///
/// This is a very expensive way to size a column.
///
/// A flex value can be provided. If specified (and non-null), the column will
/// participate in the distribution of remaining space once all the non-flexible
/// columns have been sized.
public sealed record IntrinsicColumnWidth : TableColumnWidth
{
    public IntrinsicColumnWidth(double? flex = null)
    {
        FlexFactor = flex;
    }

    /// The column's flex factor, if any.
    ///
    /// Named `FlexFactor` because `TableColumnWidth.Flex` is already a method on the base type;
    /// the constructor parameter keeps the source name `flex`.
    public double? FlexFactor { get; }

    public override double MinIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth)
    {
        double result = 0.0;
        foreach (RenderBox cell in cells)
        {
            result = Math.Max(result, cell.GetMinIntrinsicWidth(double.PositiveInfinity));
        }

        return result;
    }

    public override double MaxIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth)
    {
        double result = 0.0;
        foreach (RenderBox cell in cells)
        {
            result = Math.Max(result, cell.GetMaxIntrinsicWidth(double.PositiveInfinity));
        }

        return result;
    }

    public override double? Flex(IReadOnlyList<RenderBox> cells) => FlexFactor;
}

/// Sizes the column to a specific number of pixels.
///
/// This is the cheapest way to size a column.
public sealed record FixedColumnWidth(double Value) : TableColumnWidth
{
    public override double MinIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) => Value;

    public override double MaxIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) => Value;
}

/// Sizes the column to a fraction of the table's constraints' maxWidth.
///
/// This is a cheap way to size a column.
public sealed record FractionColumnWidth(double Value) : TableColumnWidth
{
    public override double MinIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) =>
        double.IsFinite(containerWidth) ? Value * containerWidth : 0.0;

    public override double MaxIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) =>
        double.IsFinite(containerWidth) ? Value * containerWidth : 0.0;
}

/// Sizes the column by taking a part of the remaining space once all
/// the other columns have been laid out.
///
/// For example, if two columns have a [FlexColumnWidth], then half the
/// space will go to one and half the space will go to the other.
///
/// This is a cheap way to size a column.
public sealed record FlexColumnWidth(double Value = 1.0) : TableColumnWidth
{
    public override double MinIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) => 0.0;

    public override double MaxIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) => 0.0;

    public override double? Flex(IReadOnlyList<RenderBox> cells) => Value;
}

/// Sizes the column such that it is the size that is the maximum of
/// two column width specifications.
///
/// For example, to have a column be 10 pixels wide, but at least as
/// wide as its widest cell, use `MaxColumnWidth(FixedColumnWidth(10.0),
/// IntrinsicColumnWidth())`.
///
/// Both specifications are evaluated, so if either specification is
/// expensive, so is this.
public sealed record MaxColumnWidth(TableColumnWidth A, TableColumnWidth B) : TableColumnWidth
{
    public override double MinIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) =>
        Math.Max(A.MinIntrinsicWidth(cells, containerWidth), B.MinIntrinsicWidth(cells, containerWidth));

    public override double MaxIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) =>
        Math.Max(A.MaxIntrinsicWidth(cells, containerWidth), B.MaxIntrinsicWidth(cells, containerWidth));

    public override double? Flex(IReadOnlyList<RenderBox> cells)
    {
        double? aFlex = A.Flex(cells);
        if (aFlex is null)
        {
            return B.Flex(cells);
        }

        double? bFlex = B.Flex(cells);
        return bFlex is null ? aFlex : Math.Max(aFlex.Value, bFlex.Value);
    }
}

/// Sizes the column such that it is the size that is the minimum of
/// two column width specifications.
///
/// For example, to have a column be 100 pixels wide, but at most as
/// wide as its widest cell, use `MinColumnWidth(FixedColumnWidth(100.0),
/// IntrinsicColumnWidth())`.
///
/// Both specifications are evaluated, so if either specification is
/// expensive, so is this.
public sealed record MinColumnWidth(TableColumnWidth A, TableColumnWidth B) : TableColumnWidth
{
    public override double MinIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) =>
        Math.Min(A.MinIntrinsicWidth(cells, containerWidth), B.MinIntrinsicWidth(cells, containerWidth));

    public override double MaxIntrinsicWidth(IReadOnlyList<RenderBox> cells, double containerWidth) =>
        Math.Min(A.MaxIntrinsicWidth(cells, containerWidth), B.MaxIntrinsicWidth(cells, containerWidth));

    public override double? Flex(IReadOnlyList<RenderBox> cells)
    {
        double? aFlex = A.Flex(cells);
        if (aFlex is null)
        {
            return B.Flex(cells);
        }

        double? bFlex = B.Flex(cells);
        return bFlex is null ? aFlex : Math.Min(aFlex.Value, bFlex.Value);
    }
}

/// Vertical alignment options for cells in [RenderTable] objects.
///
/// This is specified using [TableCellParentData] objects on the
/// [RenderObject.parentData] of the children of the [RenderTable].
public enum TableCellVerticalAlignment
{
    /// Cells with this alignment are placed with their top at the top of the row.
    Top,

    /// Cells with this alignment are vertically centered in the row.
    Middle,

    /// Cells with this alignment are placed with their bottom at the bottom of the row.
    Bottom,

    /// Cells with this alignment are aligned such that they all share the same
    /// baseline. Cells with no baseline are top-aligned instead. The baseline
    /// used is specified by [RenderTable.TextBaseline]. It is not valid to use
    /// the baseline value if a text baseline was not specified.
    Baseline,

    /// Cells with this alignment are sized to be as tall as the row, then made to fit the row.
    /// If all the cells have this alignment, then the row will have zero height.
    Fill,

    /// Cells with this alignment are sized to be the same height as the tallest cell in the row.
    IntrinsicHeight,
}

/// A table where the columns and rows are sized to fit the contents of the cells.
public sealed class RenderTable : RenderBox
{
    private List<RenderBox?> _children;
    private int _columns;
    private int _rows;
    private Dictionary<int, TableColumnWidth> _columnWidths;
    private TableColumnWidth _defaultColumnWidth;
    private IReadOnlyList<Decoration?>? _rowDecorations;
    private List<BoxPainter?>? _rowDecorationPainters;
    private TableBorder? _border;
    private ImageConfiguration _configuration;
    private TextDirection _textDirection;
    private TableCellVerticalAlignment _defaultVerticalAlignment;
    private TextBaseline? _textBaseline;

    private readonly List<double> _rowTops = [];
    private IReadOnlyList<double>? _columnLefts;
    private double _tableWidth;
    private double? _baselineDistance;

    public RenderTable(
        int? columns = null,
        int? rows = null,
        IReadOnlyDictionary<int, TableColumnWidth>? columnWidths = null,
        TableColumnWidth? defaultColumnWidth = null,
        TextDirection textDirection = TextDirection.Ltr,
        TableBorder? border = null,
        IReadOnlyList<Decoration?>? rowDecorations = null,
        ImageConfiguration? configuration = null,
        TableCellVerticalAlignment defaultVerticalAlignment = TableCellVerticalAlignment.Top,
        TextBaseline? textBaseline = null,
        IReadOnlyList<IReadOnlyList<RenderBox?>>? children = null)
    {
        if (columns is < 0) throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows is < 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (rows is not null && children is not null)
        {
            throw new ArgumentException("Cannot supply both rows and children.", nameof(children));
        }

        _columns = columns ?? (children is { Count: > 0 } ? children[0].Count : 0);
        _rows = rows ?? 0;
        _columnWidths = columnWidths switch
        {
            null => [],
            Dictionary<int, TableColumnWidth> dictionary => dictionary,
            _ => new Dictionary<int, TableColumnWidth>(columnWidths),
        };
        _defaultColumnWidth = defaultColumnWidth ?? new FlexColumnWidth();
        _border = border;
        _configuration = configuration ?? ImageConfiguration.Empty;
        _textDirection = textDirection;
        _defaultVerticalAlignment = defaultVerticalAlignment;
        _textBaseline = textBaseline;
        _children = [.. Enumerable.Repeat<RenderBox?>(null, _columns * _rows)];
        RowDecorations = rowDecorations;
        if (children is not null)
        {
            foreach (IReadOnlyList<RenderBox?> row in children)
            {
                AddRow(row);
            }
        }
    }

    /// The number of vertical alignment lines in this table.
    ///
    /// Changing the number of columns will remove any children that no longer fit
    /// in the table.
    public int Columns
    {
        get => _columns;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == _columns) return;
            int oldColumns = _columns;
            List<RenderBox?> oldChildren = _children;
            _columns = value;
            _children = [.. Enumerable.Repeat<RenderBox?>(null, _columns * _rows)];
            int columnsToCopy = Math.Min(oldColumns, _columns);
            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < columnsToCopy; x++)
                {
                    _children[(y * _columns) + x] = oldChildren[(y * oldColumns) + x];
                }
            }

            if (oldColumns > _columns)
            {
                for (int y = 0; y < _rows; y++)
                {
                    for (int x = _columns; x < oldColumns; x++)
                    {
                        RenderBox? child = oldChildren[(y * oldColumns) + x];
                        if (child is not null) DropChild(child);
                    }
                }
            }

            MarkNeedsLayout();
        }
    }

    /// The number of horizontal alignment lines in this table.
    ///
    /// Changing the number of rows will remove any children that no longer fit
    /// in the table.
    public int Rows
    {
        get => _rows;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == _rows) return;
            if (_rows > value)
            {
                for (int index = _columns * value; index < _children.Count; index++)
                {
                    RenderBox? child = _children[index];
                    if (child is not null) DropChild(child);
                }
            }

            _rows = value;
            ResizeChildren(_columns * _rows);
            MarkNeedsLayout();
        }
    }

    /// How the horizontal extents of the columns of this table should be determined.
    ///
    /// If the [Dictionary] has a null entry for a given column, the table uses the
    /// [DefaultColumnWidth] instead.
    public IReadOnlyDictionary<int, TableColumnWidth> ColumnWidths
    {
        get => _columnWidths;
        set
        {
            if (ReferenceEquals(_columnWidths, value)) return;
            if (_columnWidths.Count == 0 && value.Count == 0) return;
            _columnWidths = value as Dictionary<int, TableColumnWidth>
                            ?? new Dictionary<int, TableColumnWidth>(value);
            MarkNeedsLayout();
        }
    }

    /// Determines how the width of the column with the given index is determined.
    public void SetColumnWidth(int column, TableColumnWidth value)
    {
        if (_columnWidths.TryGetValue(column, out TableColumnWidth? existing) && Equals(existing, value))
        {
            return;
        }

        _columnWidths[column] = value;
        MarkNeedsLayout();
    }

    /// How to determine with widths of columns that don't have an explicit sizing algorithm.
    public TableColumnWidth DefaultColumnWidth
    {
        get => _defaultColumnWidth;
        set
        {
            if (Equals(_defaultColumnWidth, value)) return;
            _defaultColumnWidth = value;
            MarkNeedsLayout();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value) return;
            _textDirection = value;
            MarkNeedsLayout();
        }
    }

    /// The style to use when painting the boundary and interior divisions of the table.
    public TableBorder? Border
    {
        get => _border;
        set
        {
            if (Equals(_border, value)) return;
            _border = value;
            MarkNeedsPaint();
        }
    }

    /// The decorations to use for each row of the table.
    ///
    /// Row decorations fill the horizontal and vertical extent of each row in
    /// the table, unlike decorations for individual cells, which might not fill
    /// either.
    public IReadOnlyList<Decoration?>? RowDecorations
    {
        get => _rowDecorations;
        set
        {
            if (ReferenceEquals(_rowDecorations, value)) return;
            _rowDecorations = value;
            DisposeRowDecorationPainters();
            _rowDecorationPainters = _rowDecorations is null
                ? null
                : [.. Enumerable.Repeat<BoxPainter?>(null, _rowDecorations.Count)];
        }
    }

    /// The settings to pass to the [RowDecorations] when painting, so that they
    /// can resolve images appropriately.
    public ImageConfiguration Configuration
    {
        get => _configuration;
        set
        {
            if (Equals(_configuration, value)) return;
            _configuration = value;
            MarkNeedsPaint();
        }
    }

    /// How cells that do not explicitly specify a vertical alignment are aligned vertically.
    public TableCellVerticalAlignment DefaultVerticalAlignment
    {
        get => _defaultVerticalAlignment;
        set
        {
            if (_defaultVerticalAlignment == value) return;
            _defaultVerticalAlignment = value;
            MarkNeedsLayout();
        }
    }

    /// The text baseline to use when aligning rows using [TableCellVerticalAlignment.Baseline].
    public TextBaseline? TextBaseline
    {
        get => _textBaseline;
        set
        {
            if (_textBaseline == value) return;
            _textBaseline = value;
            MarkNeedsLayout();
        }
    }

    /// The resolved widths of the columns after the most recent layout.
    public IReadOnlyList<double> ResolvedColumnWidths { get; private set; } = [];

    /// The resolved heights of the rows after the most recent layout.
    public IReadOnlyList<double> ResolvedRowHeights { get; private set; } = [];

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TableCellParentData) child.parentData = new TableCellParentData();
    }

    /// Replaces the children of this table with the given cells.
    ///
    /// The cells are given in row-major order.
    public void SetFlatChildren(int columns, IReadOnlyList<RenderBox?> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (ReferenceEquals(cells, _children) && columns == _columns) return;
        if (columns < 0) throw new ArgumentOutOfRangeException(nameof(columns));

        if (columns == 0 || cells.Count == 0)
        {
            if (cells.Count != 0) throw new ArgumentException("Cells must be empty.", nameof(cells));
            _columns = columns;
            if (_children.Count == 0)
            {
                if (_rows != 0) throw new InvalidOperationException("Table row count is out of sync with its cells.");
                return;
            }

            foreach (RenderBox? oldChild in _children)
            {
                if (oldChild is not null) DropChild(oldChild);
            }

            _rows = 0;
            _children.Clear();
            MarkNeedsLayout();
            return;
        }

        if (cells.Count % columns != 0)
        {
            throw new ArgumentException("Cell count must be a multiple of the column count.", nameof(cells));
        }

        // Remove cells that are no longer in the table, but keep those that only moved:
        // re-adopting a moved child would reset its parent data.
        var lostChildren = new HashSet<RenderBox>();
        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                int xyOld = x + (y * _columns);
                int xyNew = x + (y * columns);
                RenderBox? oldChild = _children[xyOld];
                if (oldChild is not null
                    && (x >= columns || xyNew >= cells.Count || !ReferenceEquals(oldChild, cells[xyNew])))
                {
                    lostChildren.Add(oldChild);
                }
            }
        }

        int rowIndex = 0;
        while (rowIndex * columns < cells.Count)
        {
            for (int x = 0; x < columns; x++)
            {
                int xyNew = x + (rowIndex * columns);
                int xyOld = x + (rowIndex * _columns);
                RenderBox? newChild = cells[xyNew];
                if (newChild is not null
                    && (x >= _columns || rowIndex >= _rows || !ReferenceEquals(_children[xyOld], newChild)))
                {
                    if (!lostChildren.Remove(newChild)) AdoptChild(newChild);
                }
            }

            rowIndex += 1;
        }

        foreach (RenderBox lostChild in lostChildren)
        {
            DropChild(lostChild);
        }

        _columns = columns;
        _rows = cells.Count / columns;
        _children = [.. cells];
        MarkNeedsLayout();
    }

    /// Replaces the children of this table with the given cells.
    public void SetChildren(IReadOnlyList<IReadOnlyList<RenderBox?>>? cells)
    {
        if (cells is null)
        {
            SetFlatChildren(0, []);
            return;
        }

        foreach (RenderBox? oldChild in _children)
        {
            if (oldChild is not null) DropChild(oldChild);
        }

        _children.Clear();
        _columns = cells.Count > 0 ? cells[0].Count : 0;
        _rows = 0;
        foreach (IReadOnlyList<RenderBox?> row in cells)
        {
            AddRow(row);
        }
    }

    /// Adds a row to the end of the table.
    ///
    /// The newly added children must not already have parents.
    public void AddRow(IReadOnlyList<RenderBox?> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Count != _columns)
        {
            throw new ArgumentException("A table row must have exactly one cell per column.", nameof(cells));
        }

        _rows += 1;
        _children.AddRange(cells);
        foreach (RenderBox? child in cells)
        {
            if (child is not null) AdoptChild(child);
        }

        MarkNeedsLayout();
    }

    /// Replaces the child at the given position with the given child.
    ///
    /// If the given child is already located at the given position, this function
    /// does not modify the table.
    public void SetChild(int x, int y, RenderBox? value)
    {
        if (x < 0 || x >= _columns) throw new ArgumentOutOfRangeException(nameof(x));
        if (y < 0 || y >= _rows) throw new ArgumentOutOfRangeException(nameof(y));
        int xy = x + (y * _columns);
        RenderBox? oldChild = _children[xy];
        if (ReferenceEquals(oldChild, value)) return;
        if (oldChild is not null) DropChild(oldChild);
        _children[xy] = value;
        if (value is not null) AdoptChild(value);
    }

    /// Returns the children of the given column, in row order, skipping empty cells.
    public IReadOnlyList<RenderBox> Column(int x)
    {
        var result = new List<RenderBox>(_rows);
        for (int y = 0; y < _rows; y++)
        {
            RenderBox? child = _children[x + (y * _columns)];
            if (child is not null) result.Add(child);
        }

        return result;
    }

    /// Returns the children of the given row, in column order, skipping empty cells.
    public IReadOnlyList<RenderBox> Row(int y)
    {
        var result = new List<RenderBox>(_columns);
        int start = y * _columns;
        for (int xy = start; xy < start + _columns; xy++)
        {
            RenderBox? child = _children[xy];
            if (child is not null) result.Add(child);
        }

        return result;
    }

    /// Returns the position and dimensions of the box that the given row covers, in this render object's coordinates.
    public Rect GetRowBox(int row)
    {
        if (row < 0 || row >= _rows) throw new ArgumentOutOfRangeException(nameof(row));
        return new Rect(0.0, _rowTops[row], Size.Width, _rowTops[row + 1] - _rowTops[row]);
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        foreach (RenderBox? child in _children)
        {
            child?.Attach(Owner!);
        }
    }

    protected override void OnDetach()
    {
        base.OnDetach();
        if (_rowDecorationPainters is not null)
        {
            DisposeRowDecorationPainters();
            _rowDecorationPainters = [.. Enumerable.Repeat<BoxPainter?>(null, _rowDecorations!.Count)];
        }

        foreach (RenderBox? child in _children)
        {
            child?.Detach();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        foreach (RenderBox? child in _children)
        {
            if (child is not null) visitor(child);
        }
    }

    protected override void RedepthChildren() => VisitChildren(child => RedepthChild(child));

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.Role = SemanticsRole.Table;
        configuration.IsSemanticBoundary = true;
        configuration.ExplicitChildNodes = true;
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        foreach (RenderBox? child in _children)
        {
            if (child is null) continue;
            visitor(child, ((TableCellParentData)child.parentData!).offset, Matrix.Identity);
        }
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        if (_rows * _columns == 0) return 0.0;
        double totalMinWidth = 0.0;
        for (int x = 0; x < _columns; x++)
        {
            TableColumnWidth columnWidth = ResolveColumnWidth(x);
            totalMinWidth += columnWidth.MinIntrinsicWidth(Column(x), double.PositiveInfinity);
        }

        return totalMinWidth;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        if (_rows * _columns == 0) return 0.0;
        double totalMaxWidth = 0.0;
        for (int x = 0; x < _columns; x++)
        {
            TableColumnWidth columnWidth = ResolveColumnWidth(x);
            totalMaxWidth += columnWidth.MaxIntrinsicWidth(Column(x), double.PositiveInfinity);
        }

        return totalMaxWidth;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        // Winner of the "biggest lie" award.
        if (_rows * _columns == 0) return 0.0;
        IReadOnlyList<double> widths = ComputeColumnWidths(BoxConstraints.TightForFinite(width: width));
        double rowTop = 0.0;
        for (int y = 0; y < _rows; y++)
        {
            double rowHeight = 0.0;
            for (int x = 0; x < _columns; x++)
            {
                RenderBox? child = _children[(y * _columns) + x];
                if (child is not null)
                {
                    rowHeight = Math.Max(rowHeight, child.GetMaxIntrinsicHeight(widths[x]));
                }
            }

            rowTop += rowHeight;
        }

        return rowTop;
    }

    protected override double ComputeMaxIntrinsicHeight(double width) => GetMinIntrinsicHeight(width);

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => _baselineDistance;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (_rows * _columns == 0)
        {
            return constraints.Constrain(new Size(0.0, 0.0));
        }

        IReadOnlyList<double> widths = ComputeColumnWidths(constraints);
        double tableWidth = widths.Sum();
        double rowTop = 0.0;
        for (int y = 0; y < _rows; y++)
        {
            double rowHeight = 0.0;
            for (int x = 0; x < _columns; x++)
            {
                RenderBox? child = _children[(y * _columns) + x];
                if (child is null) continue;
                var parentData = (TableCellParentData)child.parentData!;
                switch (parentData.VerticalAlignment ?? _defaultVerticalAlignment)
                {
                    case TableCellVerticalAlignment.Baseline:
                        DebugCannotComputeDryLayout(
                            "TableCellVerticalAlignment.Baseline requires a full layout for baseline metrics "
                            + "to be available.");
                        return new Size(0.0, 0.0);
                    case TableCellVerticalAlignment.Top:
                    case TableCellVerticalAlignment.Middle:
                    case TableCellVerticalAlignment.Bottom:
                    case TableCellVerticalAlignment.IntrinsicHeight:
                        rowHeight = Math.Max(
                            rowHeight,
                            child.GetDryLayout(BoxConstraints.TightFor(width: widths[x])).Height);
                        break;
                    case TableCellVerticalAlignment.Fill:
                        break;
                }
            }

            rowTop += rowHeight;
        }

        return constraints.Constrain(new Size(tableWidth, rowTop));
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (_rows * _columns == 0) return null;
        IReadOnlyList<double> widths = ComputeColumnWidths(constraints);
        double? baselineOffset = null;
        for (int col = 0; col < _columns; col++)
        {
            RenderBox? child = _children[col];
            if (child is null) continue;
            var parentData = (TableCellParentData)child.parentData!;
            if ((parentData.VerticalAlignment ?? _defaultVerticalAlignment) != TableCellVerticalAlignment.Baseline)
            {
                continue;
            }

            var childConstraints = BoxConstraints.TightFor(width: widths[col]);
            double? childBaseline = child.GetDryBaseline(childConstraints, baseline);
            if (childBaseline is not null && (baselineOffset is null || baselineOffset < childBaseline))
            {
                baselineOffset = childBaseline;
            }
        }

        return baselineOffset;
    }

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        if (_rows * _columns == 0)
        {
            // TODO(ianh): if columns is zero, this should be zero width
            // TODO(ianh): if columns is not zero, this should be based on the column width specifications
            _tableWidth = 0.0;
            ResolvedColumnWidths = [];
            ResolvedRowHeights = [];
            _baselineDistance = null;
            Size = constraints.Constrain(new Size(0.0, 0.0));
            return;
        }

        IReadOnlyList<double> widths = ComputeColumnWidths(constraints);
        double[] positions = new double[_columns];
        switch (_textDirection)
        {
            case TextDirection.Rtl:
                positions[_columns - 1] = 0.0;
                for (int x = _columns - 2; x >= 0; x--)
                {
                    positions[x] = positions[x + 1] + widths[x + 1];
                }

                _columnLefts = [.. positions.Reverse()];
                _tableWidth = positions[0] + widths[0];
                break;
            case TextDirection.Ltr:
                positions[0] = 0.0;
                for (int x = 1; x < _columns; x++)
                {
                    positions[x] = positions[x - 1] + widths[x - 1];
                }

                _columnLefts = positions;
                _tableWidth = positions[^1] + widths[^1];
                break;
        }

        _rowTops.Clear();
        _baselineDistance = null;
        double[] rowHeights = new double[_rows];
        double rowTop = 0.0;
        for (int y = 0; y < _rows; y++)
        {
            _rowTops.Add(rowTop);
            double rowHeight = 0.0;
            bool haveBaseline = false;
            double beforeBaselineDistance = 0.0;
            double afterBaselineDistance = 0.0;
            double[] baselines = new double[_columns];
            for (int x = 0; x < _columns; x++)
            {
                int xy = (y * _columns) + x;
                RenderBox? child = _children[xy];
                if (child is null) continue;
                var parentData = (TableCellParentData)child.parentData!;
                parentData.X = x;
                parentData.Y = y;
                switch (parentData.VerticalAlignment ?? _defaultVerticalAlignment)
                {
                    case TableCellVerticalAlignment.Baseline:
                        if (_textBaseline is null)
                        {
                            throw new InvalidOperationException(
                                "An explicit textBaseline is required when using baseline alignment.");
                        }

                        child.Layout(BoxConstraints.TightFor(width: widths[x]), parentUsesSize: true);
                        double? childBaseline = child.GetDistanceToBaseline(_textBaseline.Value, onlyReal: true);
                        if (childBaseline is not null)
                        {
                            beforeBaselineDistance = Math.Max(beforeBaselineDistance, childBaseline.Value);
                            afterBaselineDistance = Math.Max(
                                afterBaselineDistance,
                                child.Size.Height - childBaseline.Value);
                            baselines[x] = childBaseline.Value;
                            haveBaseline = true;
                        }
                        else
                        {
                            rowHeight = Math.Max(rowHeight, child.Size.Height);
                            parentData.offset = new Point(positions[x], rowTop);
                        }

                        break;
                    case TableCellVerticalAlignment.Top:
                    case TableCellVerticalAlignment.Middle:
                    case TableCellVerticalAlignment.Bottom:
                    case TableCellVerticalAlignment.IntrinsicHeight:
                        child.Layout(BoxConstraints.TightFor(width: widths[x]), parentUsesSize: true);
                        rowHeight = Math.Max(rowHeight, child.Size.Height);
                        break;
                    case TableCellVerticalAlignment.Fill:
                        break;
                }
            }

            if (haveBaseline)
            {
                if (y == 0) _baselineDistance = beforeBaselineDistance;
                rowHeight = Math.Max(rowHeight, beforeBaselineDistance + afterBaselineDistance);
            }

            for (int x = 0; x < _columns; x++)
            {
                int xy = (y * _columns) + x;
                RenderBox? child = _children[xy];
                if (child is null) continue;
                var parentData = (TableCellParentData)child.parentData!;
                switch (parentData.VerticalAlignment ?? _defaultVerticalAlignment)
                {
                    case TableCellVerticalAlignment.Baseline:
                        parentData.offset = new Point(
                            positions[x],
                            rowTop + beforeBaselineDistance - baselines[x]);
                        break;
                    case TableCellVerticalAlignment.Top:
                        parentData.offset = new Point(positions[x], rowTop);
                        break;
                    case TableCellVerticalAlignment.Middle:
                        parentData.offset = new Point(
                            positions[x],
                            rowTop + ((rowHeight - child.Size.Height) / 2.0));
                        break;
                    case TableCellVerticalAlignment.Bottom:
                        parentData.offset = new Point(positions[x], rowTop + rowHeight - child.Size.Height);
                        break;
                    case TableCellVerticalAlignment.Fill:
                    case TableCellVerticalAlignment.IntrinsicHeight:
                        child.Layout(BoxConstraints.TightFor(width: widths[x], height: rowHeight));
                        parentData.offset = new Point(positions[x], rowTop);
                        break;
                }
            }

            rowHeights[y] = rowHeight;
            rowTop += rowHeight;
        }

        _rowTops.Add(rowTop);
        ResolvedColumnWidths = widths;
        ResolvedRowHeights = rowHeights;
        Size = constraints.Constrain(new Size(_tableWidth, rowTop));
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (int index = _children.Count - 1; index >= 0; index--)
        {
            RenderBox? child = _children[index];
            if (child is null) continue;
            var parentData = (BoxParentData)child.parentData!;
            if (child.HitTest(result, position - parentData.offset)) return true;
        }

        return false;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (_rows * _columns == 0)
        {
            if (_border is not null)
            {
                var emptyRect = new Rect(offset.X, offset.Y, _tableWidth, 0.0);
                _border.Paint(context, emptyRect, rows: [], columns: []);
            }

            return;
        }

        if (_rowDecorations is not null)
        {
            for (int y = 0; y < _rows; y++)
            {
                if (_rowDecorations.Count <= y) break;
                Decoration? decoration = _rowDecorations[y];
                if (decoration is null) continue;
                BoxPainter painter = _rowDecorationPainters![y] ??= decoration.CreateBoxPainter(MarkNeedsPaint);
                _rowDecorationPainters[y] = painter;
                painter.Paint(
                    context,
                    new Point(offset.X, offset.Y + _rowTops[y]),
                    _configuration.CopyWith(size: new Size(Size.Width, _rowTops[y + 1] - _rowTops[y])));
            }
        }

        foreach (RenderBox? child in _children)
        {
            if (child is null) continue;
            var parentData = (BoxParentData)child.parentData!;
            context.PaintChild(child, parentData.offset + offset);
        }

        if (_border is not null)
        {
            var borderRect = new Rect(offset.X, offset.Y, _tableWidth, _rowTops[^1]);
            IReadOnlyList<double> rows = [.. _rowTops.GetRange(1, _rowTops.Count - 2)];
            IReadOnlyList<double> columns = [.. _columnLefts!.Skip(1)];
            _border.Paint(context, borderRect, rows: rows, columns: columns);
        }
    }

    private TableColumnWidth ResolveColumnWidth(int x) =>
        _columnWidths.TryGetValue(x, out TableColumnWidth? width) ? width : _defaultColumnWidth;

    private void ResizeChildren(int length)
    {
        while (_children.Count > length) _children.RemoveAt(_children.Count - 1);
        while (_children.Count < length) _children.Add(null);
    }

    private void DisposeRowDecorationPainters()
    {
        if (_rowDecorationPainters is null) return;
        foreach (BoxPainter? painter in _rowDecorationPainters)
        {
            painter?.Dispose();
        }
    }

    private IReadOnlyList<double> ComputeColumnWidths(BoxConstraints constraints)
    {
        // Each column is sized by its own algorithm first, then the flexible columns
        // grow into the free space, and finally any deficit is taken back out of the
        // columns that still sit above their minimum width.
        double[] widths = new double[_columns];
        double[] minWidths = new double[_columns];
        double?[] flexes = new double?[_columns];
        double tableWidth = 0.0;
        double unflexedTableWidth = 0.0;
        double totalFlex = 0.0;

        for (int x = 0; x < _columns; x++)
        {
            TableColumnWidth columnWidth = ResolveColumnWidth(x);
            IReadOnlyList<RenderBox> columnCells = Column(x);

            double maxIntrinsicWidth = columnWidth.MaxIntrinsicWidth(columnCells, constraints.MaxWidth);
            widths[x] = maxIntrinsicWidth;
            tableWidth += maxIntrinsicWidth;

            double minIntrinsicWidth = columnWidth.MinIntrinsicWidth(columnCells, constraints.MaxWidth);
            minWidths[x] = minIntrinsicWidth;

            double? flex = columnWidth.Flex(columnCells);
            if (flex is not null)
            {
                flexes[x] = flex;
                totalFlex += flex.Value;
            }
            else
            {
                unflexedTableWidth += maxIntrinsicWidth;
            }
        }

        double maxWidthConstraint = constraints.MaxWidth;
        double minWidthConstraint = constraints.MinWidth;

        if (totalFlex > 0.0)
        {
            double targetWidth = double.IsFinite(maxWidthConstraint) ? maxWidthConstraint : minWidthConstraint;
            if (tableWidth < targetWidth)
            {
                double remainingWidth = targetWidth - unflexedTableWidth;
                for (int x = 0; x < _columns; x++)
                {
                    if (flexes[x] is not { } flex) continue;
                    double flexedWidth = remainingWidth * flex / totalFlex;
                    if (widths[x] < flexedWidth)
                    {
                        tableWidth += flexedWidth - widths[x];
                        widths[x] = flexedWidth;
                    }
                }
            }
        }
        else if (tableWidth < minWidthConstraint)
        {
            double delta = (minWidthConstraint - tableWidth) / _columns;
            for (int x = 0; x < _columns; x++)
            {
                widths[x] += delta;
            }

            tableWidth = minWidthConstraint;
        }

        if (tableWidth > maxWidthConstraint)
        {
            double deficit = tableWidth - maxWidthConstraint;
            int availableColumns = _columns;
            while (deficit > Constants.PrecisionErrorTolerance && totalFlex > Constants.PrecisionErrorTolerance)
            {
                double newTotalFlex = 0.0;
                for (int x = 0; x < _columns; x++)
                {
                    if (flexes[x] is not { } flex) continue;
                    double newWidth = widths[x] - (deficit * flex / totalFlex);
                    if (newWidth <= minWidths[x])
                    {
                        deficit -= widths[x] - minWidths[x];
                        widths[x] = minWidths[x];
                        flexes[x] = null;
                        availableColumns -= 1;
                    }
                    else
                    {
                        deficit -= widths[x] - newWidth;
                        widths[x] = newWidth;
                        newTotalFlex += flex;
                    }
                }

                totalFlex = newTotalFlex;
            }

            while (deficit > Constants.PrecisionErrorTolerance && availableColumns > 0)
            {
                double delta = deficit / availableColumns;
                int newAvailableColumns = 0;
                for (int x = 0; x < _columns; x++)
                {
                    double availableDelta = widths[x] - minWidths[x];
                    if (availableDelta <= 0.0) continue;
                    if (availableDelta <= delta)
                    {
                        deficit -= widths[x] - minWidths[x];
                        widths[x] = minWidths[x];
                    }
                    else
                    {
                        deficit -= delta;
                        widths[x] -= delta;
                        newAvailableColumns += 1;
                    }
                }

                availableColumns = newAvailableColumns;
            }
        }

        return widths;
    }
}
