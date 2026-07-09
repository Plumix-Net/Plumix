using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/table.dart
// flutter/packages/flutter/lib/src/rendering/table.dart

public abstract record TableColumnWidth;

public sealed record FixedColumnWidth(double Value) : TableColumnWidth
{
    public double Value { get; } = Validate(Value);

    private static double Validate(double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
}

public sealed record IntrinsicColumnWidth(double? Flex = null) : TableColumnWidth
{
    public double? Flex { get; } = Validate(Flex);

    private static double? Validate(double? value)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        return value;
    }
}

public sealed record TableBorder(
    BorderSide? Top = null,
    BorderSide? Right = null,
    BorderSide? Bottom = null,
    BorderSide? Left = null,
    BorderSide? HorizontalInside = null,
    BorderSide? VerticalInside = null,
    BorderRadius? BorderRadius = null)
{
    public static TableBorder All(BorderSide side) => new(
        Top: side,
        Right: side,
        Bottom: side,
        Left: side,
        HorizontalInside: side,
        VerticalInside: side);
}

public sealed class TableRow
{
    public TableRow(
        IReadOnlyList<Widget> children,
        Key? key = null,
        BoxDecoration? decoration = null)
    {
        Children = children ?? throw new ArgumentNullException(nameof(children));
        Key = key;
        Decoration = decoration;
    }

    public Key? Key { get; }
    public BoxDecoration? Decoration { get; }
    public IReadOnlyList<Widget> Children { get; }
}

public sealed class Table : MultiChildRenderObjectWidget
{
    public Table(
        IReadOnlyList<TableRow> children,
        IReadOnlyDictionary<int, TableColumnWidth>? columnWidths = null,
        TableColumnWidth? defaultColumnWidth = null,
        TableBorder? border = null,
        Key? key = null) : base(Flatten(children), key)
    {
        ArgumentNullException.ThrowIfNull(children);
        int columnCount = children.Count == 0 ? 0 : children[0].Children.Count;
        if (children.Any(row => row.Children.Count != columnCount))
        {
            throw new ArgumentException("Every TableRow must have the same number of children.", nameof(children));
        }

        Rows = children;
        ColumnCount = columnCount;
        ColumnWidths = columnWidths ?? new Dictionary<int, TableColumnWidth>();
        DefaultColumnWidth = defaultColumnWidth ?? new IntrinsicColumnWidth();
        Border = border;
    }

    public IReadOnlyList<TableRow> Rows { get; }
    public int ColumnCount { get; }
    public IReadOnlyDictionary<int, TableColumnWidth> ColumnWidths { get; }
    public TableColumnWidth DefaultColumnWidth { get; }
    public TableBorder? Border { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderTable(
        columns: ColumnCount,
        rows: Rows.Count,
        columnWidths: ColumnWidths,
        defaultColumnWidth: DefaultColumnWidth,
        rowDecorations: Rows.Select(row => row.Decoration).ToArray(),
        border: Border);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var table = (RenderTable)renderObject;
        table.Columns = ColumnCount;
        table.Rows = Rows.Count;
        table.ColumnWidths = ColumnWidths;
        table.DefaultColumnWidth = DefaultColumnWidth;
        table.RowDecorations = Rows.Select(row => row.Decoration).ToArray();
        table.Border = Border;
    }

    private static IReadOnlyList<Widget> Flatten(IReadOnlyList<TableRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows.SelectMany(row => row.Key is null
            ? row.Children
            : row.Children.Select((child, column) => (Widget)new KeyedSubtree(
                child,
                new ValueKey<(Key Row, int Column)>((row.Key, column))))).ToArray();
    }
}
