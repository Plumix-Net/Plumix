using Avalonia;
using Avalonia.Media;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/table.dart

public enum TableCellVerticalAlignment
{
    Top,
    Middle,
    Bottom,
    Baseline,
    Fill,
    IntrinsicHeight,
}

public sealed class TableCellParentData : ContainerBoxParentData<RenderBox>
{
    public int X { get; internal set; }
    public int Y { get; internal set; }
    public TableCellVerticalAlignment? VerticalAlignment { get; set; }
}

public sealed class RenderTable : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, TableCellParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, TableCellParentData> _children;
    private int _columns;
    private int _rows;
    private IReadOnlyDictionary<int, TableColumnWidth> _columnWidths;
    private TableColumnWidth _defaultColumnWidth;
    private IReadOnlyList<BoxDecoration?> _rowDecorations;
    private TableBorder? _border;
    private TextDirection _textDirection;
    private TableCellVerticalAlignment _defaultVerticalAlignment;
    private TextBaseline? _textBaseline;
    private double[] _resolvedColumnWidths = [];
    private double[] _resolvedRowHeights = [];
    private double? _baselineDistance;

    public RenderTable(
        int columns,
        int rows,
        IReadOnlyDictionary<int, TableColumnWidth> columnWidths,
        TableColumnWidth defaultColumnWidth,
        IReadOnlyList<BoxDecoration?> rowDecorations,
        TableBorder? border,
        TextDirection textDirection = TextDirection.Ltr,
        TableCellVerticalAlignment defaultVerticalAlignment = TableCellVerticalAlignment.Top,
        TextBaseline? textBaseline = null)
    {
        ValidateBaseline(defaultVerticalAlignment, textBaseline);
        _columns = columns;
        _rows = rows;
        _columnWidths = columnWidths;
        _defaultColumnWidth = defaultColumnWidth;
        _rowDecorations = rowDecorations;
        _border = border;
        _textDirection = textDirection;
        _defaultVerticalAlignment = defaultVerticalAlignment;
        _textBaseline = textBaseline;
        _children = new RenderBoxContainerDefaultsMixin<RenderBox, TableCellParentData>(this);
    }

    public int Columns
    {
        get => _columns;
        set { if (_columns == value) return; _columns = value; MarkNeedsLayout(); }
    }

    public int Rows
    {
        get => _rows;
        set { if (_rows == value) return; _rows = value; MarkNeedsLayout(); }
    }

    public IReadOnlyDictionary<int, TableColumnWidth> ColumnWidths
    {
        get => _columnWidths;
        set { if (ReferenceEquals(_columnWidths, value)) return; _columnWidths = value; MarkNeedsLayout(); }
    }

    public TableColumnWidth DefaultColumnWidth
    {
        get => _defaultColumnWidth;
        set { if (Equals(_defaultColumnWidth, value)) return; _defaultColumnWidth = value; MarkNeedsLayout(); }
    }

    public IReadOnlyList<BoxDecoration?> RowDecorations
    {
        get => _rowDecorations;
        set { _rowDecorations = value; MarkNeedsPaint(); }
    }

    public TableBorder? Border
    {
        get => _border;
        set { if (Equals(_border, value)) return; _border = value; MarkNeedsPaint(); }
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

    public TableCellVerticalAlignment DefaultVerticalAlignment
    {
        get => _defaultVerticalAlignment;
        set
        {
            if (_defaultVerticalAlignment == value) return;
            ValidateBaseline(value, _textBaseline);
            _defaultVerticalAlignment = value;
            MarkNeedsLayout();
        }
    }

    public TextBaseline? TextBaseline
    {
        get => _textBaseline;
        set
        {
            if (_textBaseline == value) return;
            ValidateBaseline(_defaultVerticalAlignment, value);
            _textBaseline = value;
            MarkNeedsLayout();
        }
    }

    public IReadOnlyList<double> ResolvedColumnWidths => _resolvedColumnWidths;
    public IReadOnlyList<double> ResolvedRowHeights => _resolvedRowHeights;

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TableCellParentData) child.parentData = new TableCellParentData();
    }

    protected override void PerformLayout()
    {
        if (_columns == 0 || _rows == 0 || ChildCount == 0)
        {
            _resolvedColumnWidths = new double[Math.Max(0, _columns)];
            _resolvedRowHeights = new double[Math.Max(0, _rows)];
            _baselineDistance = null;
            Size = Constraints.Constrain(new Size());
            return;
        }

        _resolvedColumnWidths = new double[_columns];
        _resolvedRowHeights = new double[_rows];
        _baselineDistance = null;
        var children = EnumerateChildren().ToArray();

        // Flutter's intrinsic table layout first negotiates a width per column,
        // then lays every cell out again with the resolved width.
        for (int index = 0; index < children.Length; index++)
        {
            var child = children[index];
            int column = index % _columns;
            var width = ResolveWidth(column);
            if (width is FixedColumnWidth fixedWidth)
            {
                _resolvedColumnWidths[column] = fixedWidth.Value;
                continue;
            }

            child.Layout(new BoxConstraints(MaxWidth: double.PositiveInfinity), parentUsesSize: true);
            _resolvedColumnWidths[column] = Math.Max(_resolvedColumnWidths[column], child.Size.Width);
        }

        double intrinsicWidth = _resolvedColumnWidths.Sum();
        double targetWidth = Constraints.HasBoundedWidth
            ? Constraints.MaxWidth
            : Constraints.ConstrainWidth(intrinsicWidth);
        double remaining = Math.Max(0, targetWidth - intrinsicWidth);
        double flexTotal = 0.0;
        for (int column = 0; column < _columns; column++)
        {
            if (ResolveWidth(column) is IntrinsicColumnWidth { Flex: > 0 } flexible)
            {
                flexTotal += flexible.Flex!.Value;
            }
        }
        if (remaining > 0 && flexTotal > 0)
        {
            for (int column = 0; column < _columns; column++)
            {
                if (ResolveWidth(column) is IntrinsicColumnWidth { Flex: > 0 } flexible)
                {
                    _resolvedColumnWidths[column] += remaining * flexible.Flex!.Value / flexTotal;
                }
            }
        }

        double[] xOffsets = ResolveColumnOffsets(_resolvedColumnWidths, _textDirection);
        for (int row = 0; row < _rows; row++)
        {
            LayoutRow(children, row, xOffsets);
        }

        Size = Constraints.Constrain(new Size(_resolvedColumnWidths.Sum(), _resolvedRowHeights.Sum()));
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => _baselineDistance;

    public Rect GetRowBox(int row)
    {
        if (row < 0 || row >= _resolvedRowHeights.Length) throw new ArgumentOutOfRangeException(nameof(row));
        double y = _resolvedRowHeights.Take(row).Sum();
        return new Rect(0, y, Size.Width, _resolvedRowHeights[row]);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        for (int row = 0; row < _resolvedRowHeights.Length; row++)
        {
            var decoration = row < _rowDecorations.Count ? _rowDecorations[row] : null;
            if (decoration?.Color is { } color)
            {
                var box = GetRowBox(row);
                context.DrawRectangle(new SolidColorBrush(color), null, new Rect(box.Position + offset, box.Size));
            }
        }
        PaintBorder(context, offset);
        DefaultPaint(context, offset);
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        foreach (var child in EnumerateChildren()) visitor(child);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        foreach (var child in EnumerateChildren())
        {
            visitor(child, ((TableCellParentData)child.parentData!).offset, Matrix.Identity);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position) =>
        DefaultHitTestChildren(result, position);

    private TableColumnWidth ResolveWidth(int column) =>
        _columnWidths.TryGetValue(column, out var width) ? width : _defaultColumnWidth;

    private IEnumerable<RenderBox> EnumerateChildren()
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child)) yield return child;
    }

    private static double[] PrefixOffsets(IReadOnlyList<double> values)
    {
        double[] offsets = new double[values.Count];
        for (int index = 1; index < values.Count; index++) offsets[index] = offsets[index - 1] + values[index - 1];
        return offsets;
    }

    private void LayoutRow(IReadOnlyList<RenderBox> children, int row, IReadOnlyList<double> xOffsets)
    {
        double rowTop = _resolvedRowHeights.Take(row).Sum();
        double rowHeight = 0;
        bool haveBaseline = false;
        double beforeBaselineDistance = 0;
        double afterBaselineDistance = 0;
        double[] baselines = new double[_columns];

        for (int column = 0; column < _columns; column++)
        {
            RenderBox child = children[(row * _columns) + column];
            var parentData = (TableCellParentData)child.parentData!;
            parentData.X = column;
            parentData.Y = row;
            TableCellVerticalAlignment alignment = parentData.VerticalAlignment ?? _defaultVerticalAlignment;
            if (alignment == TableCellVerticalAlignment.Fill)
            {
                continue;
            }

            child.Layout(
                BoxConstraints.TightFor(width: _resolvedColumnWidths[column]),
                parentUsesSize: true);
            if (alignment != TableCellVerticalAlignment.Baseline)
            {
                rowHeight = Math.Max(rowHeight, child.Size.Height);
                continue;
            }

            if (!_textBaseline.HasValue)
            {
                throw new InvalidOperationException(
                    "An explicit textBaseline is required when using baseline alignment.");
            }

            double? childBaseline = child.GetDistanceToBaseline(_textBaseline.Value, onlyReal: true);
            if (childBaseline.HasValue)
            {
                beforeBaselineDistance = Math.Max(beforeBaselineDistance, childBaseline.Value);
                afterBaselineDistance = Math.Max(
                    afterBaselineDistance,
                    child.Size.Height - childBaseline.Value);
                baselines[column] = childBaseline.Value;
                haveBaseline = true;
            }
            else
            {
                rowHeight = Math.Max(rowHeight, child.Size.Height);
            }
        }

        if (haveBaseline)
        {
            if (row == 0)
            {
                _baselineDistance = beforeBaselineDistance;
            }

            rowHeight = Math.Max(rowHeight, beforeBaselineDistance + afterBaselineDistance);
        }

        _resolvedRowHeights[row] = rowHeight;
        for (int column = 0; column < _columns; column++)
        {
            RenderBox child = children[(row * _columns) + column];
            var parentData = (TableCellParentData)child.parentData!;
            TableCellVerticalAlignment alignment = parentData.VerticalAlignment ?? _defaultVerticalAlignment;
            double y = alignment switch
            {
                TableCellVerticalAlignment.Top => rowTop,
                TableCellVerticalAlignment.Middle => rowTop + ((rowHeight - child.Size.Height) / 2),
                TableCellVerticalAlignment.Bottom => rowTop + rowHeight - child.Size.Height,
                TableCellVerticalAlignment.Baseline => rowTop + beforeBaselineDistance - baselines[column],
                TableCellVerticalAlignment.Fill or TableCellVerticalAlignment.IntrinsicHeight => rowTop,
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (alignment is TableCellVerticalAlignment.Fill or TableCellVerticalAlignment.IntrinsicHeight)
            {
                child.Layout(
                    BoxConstraints.Tight(new Size(_resolvedColumnWidths[column], rowHeight)),
                    parentUsesSize: true);
            }

            parentData.offset = new Point(xOffsets[column], y);
        }
    }

    private static double[] ResolveColumnOffsets(
        IReadOnlyList<double> widths,
        TextDirection textDirection)
    {
        if (textDirection == TextDirection.Ltr)
        {
            return PrefixOffsets(widths);
        }

        double[] offsets = new double[widths.Count];
        for (int column = widths.Count - 2; column >= 0; column--)
        {
            offsets[column] = offsets[column + 1] + widths[column + 1];
        }

        return offsets;
    }

    private static void ValidateBaseline(
        TableCellVerticalAlignment defaultVerticalAlignment,
        TextBaseline? textBaseline)
    {
        if (defaultVerticalAlignment == TableCellVerticalAlignment.Baseline && !textBaseline.HasValue)
        {
            throw new ArgumentException(
                "textBaseline is required when defaultVerticalAlignment is baseline.",
                nameof(textBaseline));
        }
    }

    private void PaintBorder(PaintingContext context, Point offset)
    {
        if (_border is null) return;
        static Pen PenFor(BorderSide side) => new(new SolidColorBrush(side.Color), side.Width);
        double[] x = PrefixOffsets(_resolvedColumnWidths);
        double[] y = PrefixOffsets(_resolvedRowHeights);
        if (_border.Top is { } top) context.DrawLine(PenFor(top), offset, offset + new Vector(Size.Width, 0));
        if (_border.Bottom is { } bottom) context.DrawLine(PenFor(bottom), offset + new Vector(0, Size.Height), offset + new Vector(Size.Width, Size.Height));
        if (_border.Left is { } left) context.DrawLine(PenFor(left), offset, offset + new Vector(0, Size.Height));
        if (_border.Right is { } right) context.DrawLine(PenFor(right), offset + new Vector(Size.Width, 0), offset + new Vector(Size.Width, Size.Height));
        if (_border.VerticalInside is { } vertical)
        {
            for (int index = 1; index < x.Length; index++)
                context.DrawLine(PenFor(vertical), offset + new Vector(x[index], 0), offset + new Vector(x[index], Size.Height));
        }
        if (_border.HorizontalInside is { } horizontal)
        {
            for (int index = 1; index < y.Length; index++)
                context.DrawLine(PenFor(horizontal), offset + new Vector(0, y[index]), offset + new Vector(Size.Width, y[index]));
        }
    }

    public int ChildCount => _children.ChildCount;
    public RenderBox? FirstChild => _children.FirstChild;
    public RenderBox? LastChild => _children.LastChild;
    public void Insert(RenderBox child, RenderBox? after = null) => _children.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _children.Move(child, after);
    public void Remove(RenderBox child) => _children.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) => Insert((RenderBox)child, (RenderBox?)after);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) => Move((RenderBox)child, (RenderBox?)after);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
    public void AddAll(List<RenderBox> children) => _children.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _children.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _children.ChildAfter(child);
    public void DefaultPaint(PaintingContext context, Point offset) => _children.DefaultPaint(context, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) => _children.DefaultHitTestChildren(result, position);
}
