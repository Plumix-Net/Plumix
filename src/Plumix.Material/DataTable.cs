using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/data_table.dart

public delegate void DataColumnSortCallback(int columnIndex, bool ascending);

public sealed record DataColumn
{
    public DataColumn(
        Widget label,
        TableColumnWidth? columnWidth = null,
        string? tooltip = null,
        bool numeric = false,
        DataColumnSortCallback? onSort = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        MainAxisAlignment? headingRowAlignment = null)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ColumnWidth = columnWidth;
        Tooltip = tooltip;
        Numeric = numeric;
        OnSort = onSort;
        MouseCursor = mouseCursor;
        HeadingRowAlignment = headingRowAlignment;
    }

    public Widget Label { get; }
    public TableColumnWidth? ColumnWidth { get; }
    public string? Tooltip { get; }
    public bool Numeric { get; }
    public DataColumnSortCallback? OnSort { get; }
    public MaterialStateProperty<MouseCursor?>? MouseCursor { get; }
    public MainAxisAlignment? HeadingRowAlignment { get; }
}

public sealed record DataRow
{
    public DataRow(
        IReadOnlyList<DataCell> cells,
        LocalKey? key = null,
        bool selected = false,
        Action<bool?>? onSelectChanged = null,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        MaterialStateProperty<Color?>? color = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null)
    {
        Cells = cells ?? throw new ArgumentNullException(nameof(cells));
        Key = key;
        Selected = selected;
        OnSelectChanged = onSelectChanged;
        OnLongPress = onLongPress;
        OnHover = onHover;
        Color = color;
        MouseCursor = mouseCursor;
    }

    public static DataRow ByIndex(
        IReadOnlyList<DataCell> cells,
        int? index = null,
        bool selected = false,
        Action<bool?>? onSelectChanged = null,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        MaterialStateProperty<Color?>? color = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null) => new(
            cells,
            key: new ValueKey<int?>(index),
            selected: selected,
            onSelectChanged: onSelectChanged,
            onLongPress: onLongPress,
            onHover: onHover,
            color: color,
            mouseCursor: mouseCursor);

    public LocalKey? Key { get; }
    public bool Selected { get; }
    public Action<bool?>? OnSelectChanged { get; }
    public Action? OnLongPress { get; }
    public Action<bool>? OnHover { get; }
    public IReadOnlyList<DataCell> Cells { get; }
    public MaterialStateProperty<Color?>? Color { get; }
    public MaterialStateProperty<MouseCursor?>? MouseCursor { get; }
}

public sealed record DataCell
{
    public DataCell(
        Widget child,
        bool placeholder = false,
        bool showEditIcon = false,
        Action? onTap = null,
        Action? onLongPress = null,
        Action<PointerDownEvent>? onTapDown = null,
        Action? onDoubleTap = null,
        Action? onTapCancel = null)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Placeholder = placeholder;
        ShowEditIcon = showEditIcon;
        OnTap = onTap;
        OnLongPress = onLongPress;
        OnTapDown = onTapDown;
        OnDoubleTap = onDoubleTap;
        OnTapCancel = onTapCancel;
    }

    public static DataCell Empty { get; } = new(new SizedBox());
    public Widget Child { get; }
    public bool Placeholder { get; }
    public bool ShowEditIcon { get; }
    public Action? OnTap { get; }
    public Action? OnDoubleTap { get; }
    public Action? OnLongPress { get; }
    public Action<PointerDownEvent>? OnTapDown { get; }
    public Action? OnTapCancel { get; }
    internal bool IsInteractive => OnTap is not null || OnDoubleTap is not null || OnLongPress is not null || OnTapDown is not null || OnTapCancel is not null;
}

public sealed class DataTable : StatelessWidget
{
    public DataTable(
        IReadOnlyList<DataColumn> columns,
        IReadOnlyList<DataRow> rows,
        int? sortColumnIndex = null,
        bool sortAscending = true,
        Action<bool?>? onSelectAll = null,
        BoxDecoration? decoration = null,
        MaterialStateProperty<Color?>? dataRowColor = null,
        double? dataRowHeight = null,
        double? dataRowMinHeight = null,
        double? dataRowMaxHeight = null,
        TextStyle? dataTextStyle = null,
        MaterialStateProperty<Color?>? headingRowColor = null,
        double? headingRowHeight = null,
        TextStyle? headingTextStyle = null,
        double? horizontalMargin = null,
        double? columnSpacing = null,
        bool showCheckboxColumn = true,
        bool showBottomBorder = false,
        double? dividerThickness = null,
        double? checkboxHorizontalMargin = null,
        TableBorder? border = null,
        Clip clipBehavior = Clip.None,
        Key? key = null) : base(key)
    {
        if (columns is null || columns.Count == 0) throw new ArgumentException("DataTable requires at least one column.", nameof(columns));
        ArgumentNullException.ThrowIfNull(rows);
        if (sortColumnIndex.HasValue && (sortColumnIndex.Value < 0 || sortColumnIndex.Value >= columns.Count))
            throw new ArgumentOutOfRangeException(nameof(sortColumnIndex));
        if (rows.Any(row => row.Cells.Count != columns.Count))
            throw new ArgumentException("All rows must have the same number of cells as columns.", nameof(rows));
        if (dataRowHeight.HasValue && (dataRowMinHeight.HasValue || dataRowMaxHeight.HasValue))
            throw new ArgumentException("dataRowHeight cannot be combined with dataRowMinHeight/dataRowMaxHeight.");
        dataRowMinHeight ??= dataRowHeight;
        dataRowMaxHeight ??= dataRowHeight;
        ValidateNonNegative(dataRowMinHeight, nameof(dataRowMinHeight));
        ValidateNonNegative(dataRowMaxHeight, nameof(dataRowMaxHeight));
        if (dataRowMinHeight > dataRowMaxHeight) throw new ArgumentException("Maximum row height must be at least minimum row height.");
        ValidateNonNegative(headingRowHeight, nameof(headingRowHeight));
        ValidateNonNegative(horizontalMargin, nameof(horizontalMargin));
        ValidateNonNegative(columnSpacing, nameof(columnSpacing));
        ValidateNonNegative(dividerThickness, nameof(dividerThickness));
        ValidateNonNegative(checkboxHorizontalMargin, nameof(checkboxHorizontalMargin));

        Columns = columns;
        Rows = rows;
        SortColumnIndex = sortColumnIndex;
        SortAscending = sortAscending;
        OnSelectAll = onSelectAll;
        Decoration = decoration;
        DataRowColor = dataRowColor;
        DataRowMinHeight = dataRowMinHeight;
        DataRowMaxHeight = dataRowMaxHeight;
        DataTextStyle = dataTextStyle;
        HeadingRowColor = headingRowColor;
        HeadingRowHeight = headingRowHeight;
        HeadingTextStyle = headingTextStyle;
        HorizontalMargin = horizontalMargin;
        ColumnSpacing = columnSpacing;
        ShowCheckboxColumn = showCheckboxColumn;
        ShowBottomBorder = showBottomBorder;
        DividerThickness = dividerThickness;
        CheckboxHorizontalMargin = checkboxHorizontalMargin;
        Border = border;
        ClipBehavior = clipBehavior;
    }

    public IReadOnlyList<DataColumn> Columns { get; }
    public IReadOnlyList<DataRow> Rows { get; }
    public int? SortColumnIndex { get; }
    public bool SortAscending { get; }
    public Action<bool?>? OnSelectAll { get; }
    public BoxDecoration? Decoration { get; }
    public MaterialStateProperty<Color?>? DataRowColor { get; }
    public double? DataRowMinHeight { get; }
    public double? DataRowMaxHeight { get; }
    public double? DataRowHeight => DataRowMinHeight == DataRowMaxHeight ? DataRowMinHeight : null;
    public TextStyle? DataTextStyle { get; }
    public MaterialStateProperty<Color?>? HeadingRowColor { get; }
    public double? HeadingRowHeight { get; }
    public TextStyle? HeadingTextStyle { get; }
    public double? HorizontalMargin { get; }
    public double? ColumnSpacing { get; }
    public bool ShowCheckboxColumn { get; }
    public bool ShowBottomBorder { get; }
    public double? DividerThickness { get; }
    public double? CheckboxHorizontalMargin { get; }
    public TableBorder? Border { get; }
    public Clip ClipBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var localTheme = DataTableTheme.Of(context);
        var textDirection = Directionality.Of(context);
        double horizontalMargin = HorizontalMargin ?? localTheme.HorizontalMargin ?? 24.0;
        double columnSpacing = ColumnSpacing ?? localTheme.ColumnSpacing ?? 56.0;
        double checkboxMargin = CheckboxHorizontalMargin ?? localTheme.CheckboxHorizontalMargin ?? horizontalMargin;
        double headingHeight = HeadingRowHeight ?? localTheme.HeadingRowHeight ?? 56.0;
        double dataMinHeight = DataRowMinHeight ?? localTheme.DataRowMinHeight ?? 48.0;
        double dataMaxHeight = DataRowMaxHeight ?? localTheme.DataRowMaxHeight ?? 48.0;
        var headingStyle = HeadingTextStyle ?? localTheme.HeadingTextStyle ?? theme.TextTheme.LabelLarge;
        var dataStyle = DataTextStyle ?? localTheme.DataTextStyle ?? theme.TextTheme.BodyMedium;
        var effectiveDataRowColor = DataRowColor ?? localTheme.DataRowColor;
        var effectiveHeadingRowColor = HeadingRowColor ?? localTheme.HeadingRowColor;
        bool anySelectable = Rows.Any(row => row.OnSelectChanged is not null);
        bool displayCheckbox = ShowCheckboxColumn && anySelectable;
        var selectableRows = Rows.Where(row => row.OnSelectChanged is not null).ToArray();
        int selectedRows = selectableRows.Count(row => row.Selected);
        bool allChecked = displayCheckbox && selectedRows == selectableRows.Length;
        bool someChecked = displayCheckbox && selectedRows > 0 && !allChecked;
        var textColumns = Columns.Select((column, index) => (column, index)).Where(pair => !pair.column.Numeric).ToArray();
        int? onlyTextColumn = textColumns.Length == 1 ? textColumns[0].index : (int?)null;
        var tableRows = new List<TableRow>();

        var headingChildren = new List<Widget>();
        if (displayCheckbox)
        {
            headingChildren.Add(BuildCheckbox(
                value: someChecked ? null : allChecked,
                tristate: true,
                horizontalStart: checkboxMargin,
                horizontalEnd: checkboxMargin / 2,
                onChanged: value => HandleSelectAll(value, someChecked)));
        }
        for (int columnIndex = 0; columnIndex < Columns.Count; columnIndex++)
        {
            var padding = ResolveCellPadding(columnIndex, displayCheckbox, horizontalMargin, columnSpacing, textDirection);
            headingChildren.Add(BuildHeadingCell(
                context,
                Columns[columnIndex],
                columnIndex,
                padding,
                headingHeight,
                headingStyle,
                localTheme));
        }
        tableRows.Add(new TableRow(
            headingChildren,
            decoration: new BoxDecoration(Color: effectiveHeadingRowColor?.Resolve(MaterialState.None))));

        foreach (var row in Rows)
        {
            var children = new List<Widget>();
            var rowStates = (row.Selected ? MaterialState.Selected : MaterialState.None)
                            | (anySelectable && row.OnSelectChanged is null ? MaterialState.Disabled : MaterialState.None);
            if (displayCheckbox)
            {
                children.Add(BuildCheckbox(
                    value: row.Selected,
                    tristate: false,
                    horizontalStart: checkboxMargin,
                    horizontalEnd: checkboxMargin / 2,
                    onChanged: row.OnSelectChanged));
            }
            for (int columnIndex = 0; columnIndex < Columns.Count; columnIndex++)
            {
                children.Add(BuildDataCell(
                    context,
                    Columns[columnIndex],
                    row,
                    row.Cells[columnIndex],
                    ResolveCellPadding(columnIndex, displayCheckbox, horizontalMargin, columnSpacing, textDirection),
                    dataMinHeight,
                    dataMaxHeight,
                    dataStyle,
                    row.MouseCursor?.Resolve(rowStates) ?? localTheme.DataRowCursor?.Resolve(rowStates)));
            }
            var rowColor = (row.Color ?? effectiveDataRowColor)?.Resolve(rowStates)
                           ?? (row.Selected ? WithOpacity(theme.PrimaryColor, 0.08) : null);
            tableRows.Add(new TableRow(children, row.Key, new BoxDecoration(Color: rowColor)));
        }

        var widths = new Dictionary<int, TableColumnWidth>();
        int displayIndex = 0;
        if (displayCheckbox)
        {
            widths[displayIndex++] = new FixedColumnWidth(checkboxMargin + Checkbox.Width + (checkboxMargin / 2));
        }
        for (int columnIndex = 0; columnIndex < Columns.Count; columnIndex++)
        {
            widths[displayIndex++] = Columns[columnIndex].ColumnWidth
                                     ?? (columnIndex == onlyTextColumn ? new IntrinsicColumnWidth(1) : new IntrinsicColumnWidth());
        }

        var divider = new BorderSide(theme.DividerColor, DividerThickness ?? localTheme.DividerThickness ?? 1.0);
        var effectiveBorder = Border ?? new TableBorder(
            Bottom: ShowBottomBorder ? divider : null,
            HorizontalInside: divider);
        Widget result = new Table(
            tableRows,
            widths,
            border: effectiveBorder,
            defaultVerticalAlignment: TableCellVerticalAlignment.Middle);
        var decoration = Decoration ?? localTheme.Decoration;
        if (decoration is not null) result = new DecoratedBox(decoration, result);
        if (ClipBehavior != Clip.None)
        {
            result = decoration?.BorderRadius is { } radius
                ? new ClipRRect(radius, result)
                : new ClipRect(child: result);
        }
        return result;
    }

    private Widget BuildHeadingCell(
        BuildContext context,
        DataColumn column,
        int columnIndex,
        Thickness padding,
        double height,
        TextStyle style,
        DataTableThemeData tableTheme)
    {
        bool sorted = SortColumnIndex == columnIndex;
        var alignment = column.HeadingRowAlignment ?? tableTheme.HeadingRowAlignment ?? MainAxisAlignment.Start;
        var content = new List<Widget>();
        if (alignment == MainAxisAlignment.Center && column.OnSort is not null) content.Add(new SizedBox(width: 18));
        content.Add(column.Label);
        if (column.OnSort is not null)
        {
            content.Add(new SizedBox(width: 2));
            content.Add(new Opacity(sorted ? 1 : 0, new Icon(
                SortAscending ? Icons.ArrowUpward : Icons.ArrowDownward,
                size: 16,
                color: style.Color)));
        }
        Widget label = new Container(
            height: height,
            padding: padding,
            alignment: column.Numeric ? Alignment.CenterRight : Alignment.CenterLeft,
            child: new DefaultTextStyle(
                style,
                new Row(
                    mainAxisSize: MainAxisSize.Min,
                    mainAxisAlignment: alignment,
                    textDirection: column.Numeric ? TextDirection.Rtl : null,
                    children: content),
                softWrap: false));
        if (column.Tooltip is not null) label = new Tooltip(column.Tooltip, label);
        var states = column.OnSort is null ? MaterialState.Disabled : MaterialState.None;
        return new InkWell(
            onTap: column.OnSort is null
                ? null
                : () => column.OnSort(columnIndex, SortColumnIndex != columnIndex || !SortAscending),
            mouseCursor: column.MouseCursor?.Resolve(states) ?? tableTheme.HeadingCellCursor?.Resolve(states),
            child: label);
    }

    private static Widget BuildDataCell(
        BuildContext context,
        DataColumn column,
        DataRow row,
        DataCell cell,
        Thickness padding,
        double minHeight,
        double maxHeight,
        TextStyle style,
        MouseCursor? cursor)
    {
        var effectiveStyle = cell.Placeholder && style.Color.HasValue
            ? style.CopyWith(color: WithOpacity(style.Color.Value, 0.60))
            : style;
        Widget label = cell.Child;
        if (cell.ShowEditIcon)
        {
            label = new Row(
                mainAxisSize: MainAxisSize.Min,
                textDirection: column.Numeric ? TextDirection.Rtl : null,
                children: [new Flexible(label), new SizedBox(width: 8), new Icon(Icons.Edit, size: 18)]);
        }
        label = new Container(
            constraints: new BoxConstraints(MinHeight: minHeight, MaxHeight: maxHeight),
            padding: padding,
            alignment: column.Numeric ? Alignment.CenterRight : Alignment.CenterLeft,
            child: new DefaultTextStyle(effectiveStyle, new DropdownButtonHideUnderline(label)));

        var tap = cell.OnTap ?? (row.OnSelectChanged is null ? null : () => row.OnSelectChanged(!row.Selected));
        var longPress = cell.OnLongPress ?? row.OnLongPress;
        if (cell.IsInteractive || row.OnSelectChanged is not null || row.OnLongPress is not null || row.OnHover is not null)
        {
            label = new InkWell(
                onTap: tap,
                onDoubleTap: cell.OnDoubleTap,
                onTapDown: cell.OnTapDown,
                onTapCancel: cell.OnTapCancel,
                onLongPress: longPress,
                onHover: row.OnHover,
                mouseCursor: cursor,
                child: label);
        }
        return label;
    }

    private static Widget BuildCheckbox(
        bool? value,
        bool tristate,
        double horizontalStart,
        double horizontalEnd,
        Action<bool?>? onChanged) => new Padding(
            new Thickness(horizontalStart, 0, horizontalEnd, 0),
            new Center(child: new Checkbox(value, onChanged, tristate: tristate)));

    private void HandleSelectAll(bool? value, bool someChecked)
    {
        bool effective = someChecked || (value ?? false);
        if (OnSelectAll is not null)
        {
            OnSelectAll(effective);
            return;
        }
        foreach (var row in Rows)
        {
            if (row.OnSelectChanged is not null && row.Selected != effective) row.OnSelectChanged(effective);
        }
    }

    private Thickness ResolveCellPadding(
        int index,
        bool hasCheckbox,
        double horizontalMargin,
        double columnSpacing,
        TextDirection textDirection)
    {
        double start = index switch
        {
            0 when hasCheckbox && CheckboxHorizontalMargin is null => horizontalMargin / 2,
            0 => horizontalMargin,
            _ => columnSpacing / 2,
        };
        double end = index == Columns.Count - 1 ? horizontalMargin : columnSpacing / 2;
        return textDirection == TextDirection.Rtl
            ? new Thickness(end, 0, start, 0)
            : new Thickness(start, 0, end, 0);
    }

    private static Color WithOpacity(Color color, double opacity) =>
        Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255), color.R, color.G, color.B);

    private static void ValidateNonNegative(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
            throw new ArgumentOutOfRangeException(name);
    }
}
