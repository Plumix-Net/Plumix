using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

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
        TextDirection? textDirection = null,
        TableCellVerticalAlignment defaultVerticalAlignment = TableCellVerticalAlignment.Top,
        TextBaseline? textBaseline = null,
        Key? key = null) : base(Flatten(children), key)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (defaultVerticalAlignment == TableCellVerticalAlignment.Baseline && !textBaseline.HasValue)
        {
            throw new ArgumentException(
                "textBaseline is required when defaultVerticalAlignment is baseline.",
                nameof(textBaseline));
        }

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
        TextDirection = textDirection;
        DefaultVerticalAlignment = defaultVerticalAlignment;
        TextBaseline = textBaseline;
    }

    public IReadOnlyList<TableRow> Rows { get; }
    public int ColumnCount { get; }
    public IReadOnlyDictionary<int, TableColumnWidth> ColumnWidths { get; }
    public TableColumnWidth DefaultColumnWidth { get; }
    public TableBorder? Border { get; }
    public TextDirection? TextDirection { get; }
    public TableCellVerticalAlignment DefaultVerticalAlignment { get; }
    public TextBaseline? TextBaseline { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderTable(
        columns: ColumnCount,
        rows: Rows.Count,
        columnWidths: ColumnWidths,
        defaultColumnWidth: DefaultColumnWidth,
        rowDecorations: Rows.Select(row => row.Decoration).ToArray(),
        border: Border,
        textDirection: TextDirection ?? Directionality.Of(context),
        defaultVerticalAlignment: DefaultVerticalAlignment,
        textBaseline: TextBaseline);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var table = (RenderTable)renderObject;
        table.Columns = ColumnCount;
        table.Rows = Rows.Count;
        table.ColumnWidths = ColumnWidths;
        table.DefaultColumnWidth = DefaultColumnWidth;
        table.RowDecorations = Rows.Select(row => row.Decoration).ToArray();
        table.Border = Border;
        table.TextDirection = TextDirection ?? Directionality.Of(context);
        if (TextBaseline.HasValue)
        {
            table.TextBaseline = TextBaseline;
            table.DefaultVerticalAlignment = DefaultVerticalAlignment;
        }
        else
        {
            table.DefaultVerticalAlignment = DefaultVerticalAlignment;
            table.TextBaseline = null;
        }
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

// Dart parity source: flutter/packages/flutter/lib/src/widgets/table.dart (TableCell)
public sealed class TableCell : StatelessWidget
{
    public TableCell(
        Widget child,
        TableCellVerticalAlignment? verticalAlignment = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        VerticalAlignment = verticalAlignment;
    }

    public TableCellVerticalAlignment? VerticalAlignment { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new TableCellParentDataWidget(
            verticalAlignment: VerticalAlignment,
            child: new Semantics(
                role: SemanticsRole.Cell,
                child: Child));
    }

    private sealed class TableCellParentDataWidget : ParentDataWidget<TableCellParentData>
    {
        public TableCellParentDataWidget(
            Widget child,
            TableCellVerticalAlignment? verticalAlignment) : base(child)
        {
            VerticalAlignment = verticalAlignment;
        }

        public TableCellVerticalAlignment? VerticalAlignment { get; }

        public override Type DebugTypicalAncestorWidgetType => typeof(Table);

        protected override void ApplyParentData(RenderObject renderObject)
        {
            var parentData = (TableCellParentData)renderObject.parentData!;
            if (parentData.VerticalAlignment == VerticalAlignment)
            {
                return;
            }

            parentData.VerticalAlignment = VerticalAlignment;
            renderObject.Parent?.MarkNeedsLayout();
        }
    }
}
