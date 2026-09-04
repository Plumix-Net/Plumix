using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/table.dart

/// A horizontal group of cells in a [Table].
///
/// Every row in a table must have the same number of children.
///
/// The alignment of individual cells in a row can be controlled using a
/// [TableCell].
public sealed class TableRow
{
    public TableRow(
        IReadOnlyList<Widget>? children = null,
        Key? key = null,
        Decoration? decoration = null)
    {
        Children = children ?? [];
        Key = key;
        Decoration = decoration;
    }

    /// An identifier for this row.
    public Key? Key { get; }

    /// A decoration to paint behind this row.
    ///
    /// Row decorations fill the horizontal and vertical extent of each row in
    /// the table, unlike decorations for individual cells, which might not fill
    /// either.
    public Decoration? Decoration { get; }

    /// The widgets that comprise the cells in this row.
    ///
    /// Children may be wrapped in [TableCell] widgets to provide per-cell
    /// configuration to the [Table], but children are not required to be wrapped.
    public IReadOnlyList<Widget> Children { get; }

    public override string ToString()
    {
        string result = "TableRow(";
        if (Key is not null) result += $"{Key}, ";
        if (Decoration is not null) result += $"{Decoration}, ";
        result += Children.Count == 0 ? "no children" : $"[{string.Join(", ", Children)}]";
        return result + ")";
    }
}

/// A widget that uses the table layout algorithm for its children.
///
/// If you only have one row, the [Row] widget is more appropriate. If you only
/// have one column, the [SliverList] or [Column] widgets will be more
/// appropriate.
///
/// Rows size vertically based on their contents. To control the individual
/// column widths, use the [ColumnWidths] property to specify a
/// [TableColumnWidth] for each column. If [ColumnWidths] is null, or there is a
/// null entry for a given column in [ColumnWidths], the table uses the
/// [DefaultColumnWidth] instead.
public sealed class Table : RenderObjectWidget
{
    private static readonly IReadOnlyDictionary<int, TableColumnWidth> EmptyColumnWidths =
        new Dictionary<int, TableColumnWidth>();

    private readonly IReadOnlyList<Decoration?>? _rowDecorations;

    public Table(
        IReadOnlyList<TableRow>? children = null,
        IReadOnlyDictionary<int, TableColumnWidth>? columnWidths = null,
        TableColumnWidth? defaultColumnWidth = null,
        TextDirection? textDirection = null,
        TableBorder? border = null,
        TableCellVerticalAlignment defaultVerticalAlignment = TableCellVerticalAlignment.Top,
        TextBaseline? textBaseline = null,
        Key? key = null) : base(key)
    {
        Children = children ?? [];
        if (defaultVerticalAlignment == TableCellVerticalAlignment.Baseline && textBaseline is null)
        {
            throw new ArgumentException(
                "textBaseline is required if you specify the defaultVerticalAlignment with "
                + "TableCellVerticalAlignment.baseline",
                nameof(textBaseline));
        }

        if (Children.Any(row1 =>
                row1.Key is not null
                && Children.Any(row2 => !ReferenceEquals(row1, row2) && Equals(row1.Key, row2.Key))))
        {
            throw new ArgumentException(
                "Two or more TableRow children of this Table had the same key.\n"
                + "All the keyed TableRow children of a Table must have different Keys.",
                nameof(children));
        }

        if (Children.Count > 0)
        {
            int cellCount = Children[0].Children.Count;
            if (Children.Any(row => row.Children.Count != cellCount))
            {
                throw new ArgumentException(
                    "Table contains irregular row lengths.\n"
                    + "Every TableRow in a Table must have the same number of children, so that every cell is "
                    + "filled. Otherwise, the table will contain holes.",
                    nameof(children));
            }

            if (Children.Any(row => row.Children.Count == 0))
            {
                throw new ArgumentException(
                    "One or more TableRow have no children.\n"
                    + "Every TableRow in a Table must have at least one child, so there is no empty row.",
                    nameof(children));
            }
        }

        _rowDecorations = Children.Any(row => row.Decoration is not null)
            ? [.. Children.Select(row => row.Decoration)]
            : null;

        var flatChildren = Children.SelectMany(row => row.Children).ToArray();
        var seenKeys = new HashSet<Key>();
        foreach (Widget child in flatChildren)
        {
            if (child.Key is not null && !seenKeys.Add(child.Key))
            {
                throw new ArgumentException(
                    "Two or more cells in this Table contain widgets with the same key.\n"
                    + "Every widget child of every TableRow in a Table must have different keys. The cells of a "
                    + "Table are flattened out for processing, so separate cells cannot have duplicate keys even "
                    + "if they are in different rows.",
                    nameof(children));
            }
        }

        ColumnWidths = columnWidths;
        DefaultColumnWidth = defaultColumnWidth ?? new FlexColumnWidth();
        TextDirection = textDirection;
        Border = border;
        DefaultVerticalAlignment = defaultVerticalAlignment;
        TextBaseline = textBaseline;
    }

    /// The rows of the table.
    ///
    /// Every row in a table must have the same number of children, and all the
    /// children must be non-null.
    public IReadOnlyList<TableRow> Children { get; }

    /// How the horizontal extents of the columns of this table should be determined.
    public IReadOnlyDictionary<int, TableColumnWidth>? ColumnWidths { get; }

    /// How to determine with widths of columns that don't have an explicit sizing algorithm.
    ///
    /// Specifically, the [DefaultColumnWidth] is used for column `i` if
    /// `ColumnWidths[i]` is null. Defaults to [FlexColumnWidth], which will
    /// divide the remaining horizontal space up evenly between as many columns
    /// as there are.
    public TableColumnWidth DefaultColumnWidth { get; }

    /// The direction in which the columns are ordered.
    ///
    /// Defaults to the ambient [Directionality].
    public TextDirection? TextDirection { get; }

    /// The style to use when painting the boundary and interior divisions of the table.
    public TableBorder? Border { get; }

    /// How cells that do not explicitly specify a vertical alignment are aligned vertically.
    ///
    /// Cells may specify a vertical alignment by wrapping their contents in a [TableCell] widget.
    public TableCellVerticalAlignment DefaultVerticalAlignment { get; }

    /// The text baseline to use when aligning rows using [TableCellVerticalAlignment.Baseline].
    ///
    /// This must be set if using baseline alignment. There is no default because there is no
    /// way for the framework to know the correct baseline _a priori_.
    public TextBaseline? TextBaseline { get; }

    /// The number of columns the table has, derived from its first row.
    public int ColumnCount => Children.Count > 0 ? Children[0].Children.Count : 0;

    internal IReadOnlyList<Decoration?>? RowDecorations => _rowDecorations;

    public override Element CreateElement() => new TableElement(this);

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderTable(
        columns: ColumnCount,
        rows: Children.Count,
        columnWidths: ColumnWidths,
        defaultColumnWidth: DefaultColumnWidth,
        textDirection: TextDirection ?? Directionality.Of(context),
        border: Border,
        rowDecorations: _rowDecorations,
        configuration: ImageConfigurationUtils.CreateLocalImageConfiguration(context),
        defaultVerticalAlignment: DefaultVerticalAlignment,
        textBaseline: TextBaseline);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var table = (RenderTable)renderObject;
        table.ColumnWidths = ColumnWidths ?? EmptyColumnWidths;
        table.DefaultColumnWidth = DefaultColumnWidth;
        table.TextDirection = TextDirection ?? Directionality.Of(context);
        table.Border = Border;
        table.RowDecorations = _rowDecorations;
        table.Configuration = ImageConfigurationUtils.CreateLocalImageConfiguration(context);
        table.DefaultVerticalAlignment = DefaultVerticalAlignment;
        table.TextBaseline = TextBaseline;
    }
}

/// The element for a [Table], which reconciles its children one [TableRow] at a
/// time so that keyed rows keep their state when they move.
public sealed class TableElement : RenderObjectElement
{
    private IReadOnlyList<TableElementRow> _children = [];
    private bool _doingMountOrUpdate;
    private readonly HashSet<Element> _forgottenChildren = [];

    public TableElement(Table widget) : base(widget)
    {
    }

    private RenderTable Table => (RenderTable)RequireRenderObject();

    protected override void OnMount()
    {
        base.OnMount();
        _doingMountOrUpdate = true;
        int rowIndex = -1;
        var rows = new List<TableElementRow>();
        foreach (TableRow row in ((Table)Widget).Children)
        {
            var children = new List<Element>();
            rowIndex += 1;
            int columnIndex = 0;
            foreach (Widget child in row.Children)
            {
                children.Add(InflateWidget(child, new TableSlot(columnIndex, rowIndex)));
                columnIndex += 1;
            }

            rows.Add(new TableElementRow(row.Key, children));
        }

        _children = rows;
        UpdateRenderObjectChildren();
        _doingMountOrUpdate = false;
    }

    public override void Update(Widget newWidget)
    {
        _doingMountOrUpdate = true;
        var oldKeyedRows = new Dictionary<Key, List<Element>>();
        foreach (TableElementRow row in _children)
        {
            if (row.Key is not null) oldKeyedRows[row.Key] = row.Children;
        }

        var oldUnkeyedRows = _children.Where(row => row.Key is null).GetEnumerator();
        var newChildren = new List<TableElementRow>();
        var taken = new HashSet<List<Element>>();
        var newWidgetRows = ((Table)newWidget).Children;

        for (int rowIndex = 0; rowIndex < newWidgetRows.Count; rowIndex++)
        {
            TableRow row = newWidgetRows[rowIndex];
            List<Element> oldChildren;
            if (row.Key is not null && oldKeyedRows.TryGetValue(row.Key, out List<Element>? keyed))
            {
                oldChildren = keyed;
                taken.Add(keyed);
            }
            else if (row.Key is null && oldUnkeyedRows.MoveNext())
            {
                oldChildren = oldUnkeyedRows.Current.Children;
            }
            else
            {
                oldChildren = [];
            }

            var slots = new List<object?>(row.Children.Count);
            for (int columnIndex = 0; columnIndex < row.Children.Count; columnIndex++)
            {
                slots.Add(new TableSlot(columnIndex, rowIndex));
            }

            newChildren.Add(new TableElementRow(
                row.Key,
                UpdateChildren(oldChildren, row.Children, _forgottenChildren, slots)));
        }

        while (oldUnkeyedRows.MoveNext())
        {
            UpdateChildren(oldUnkeyedRows.Current.Children, [], _forgottenChildren);
        }

        oldUnkeyedRows.Dispose();

        foreach (List<Element> oldChildren in oldKeyedRows.Values.Where(children => !taken.Contains(children)))
        {
            UpdateChildren(oldChildren, [], _forgottenChildren);
        }

        _children = newChildren;
        UpdateRenderObjectChildren();
        _forgottenChildren.Clear();
        base.Update(newWidget);
        _doingMountOrUpdate = false;
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        Table.SetupParentData(child);

        // Once [Update] or [OnMount] has run, the whole grid is written at once by
        // [UpdateRenderObjectChildren]; only out-of-band insertions land here.
        if (!_doingMountOrUpdate)
        {
            var tableSlot = (TableSlot)slot!;
            Table.SetChild(tableSlot.Column, tableSlot.Row, (RenderBox)child);
        }
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        // Moves are handled by [UpdateRenderObjectChildren], which rewrites the grid.
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        var tableSlot = (TableSlot)slot!;
        Table.SetChild(tableSlot.Column, tableSlot.Row, null);
    }

    public override void ForgetChild(Element child)
    {
        _forgottenChildren.Add(child);
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        foreach (TableElementRow row in _children)
        {
            foreach (Element child in row.Children)
            {
                if (!_forgottenChildren.Contains(child)) visitor(child);
            }
        }
    }

    private void UpdateRenderObjectChildren()
    {
        var cells = new List<RenderBox?>();
        foreach (TableElementRow row in _children)
        {
            foreach (Element child in row.Children)
            {
                cells.Add((RenderBox?)child.RenderObject);
            }
        }

        Table.SetFlatChildren(_children.Count > 0 ? _children[0].Children.Count : 0, cells);
    }
}

/// One row of elements owned by a [TableElement].
public sealed record TableElementRow(Key? Key, List<Element> Children);

/// The slot a [Table] cell occupies, identified by its column and row.
public sealed record TableSlot(int Column, int Row);

/// A widget that controls how a child of a [Table] is aligned.
///
/// A [TableCell] widget must be a descendant of a [Table], and the path from
/// the [TableCell] widget to its enclosing [Table] must contain only
/// [TableRow]s, [StatelessWidget]s, or [StatefulWidget]s (not
/// other kinds of widgets, like [RenderObjectWidget]s).
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

    /// How this cell is aligned vertically.
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
