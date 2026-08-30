using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/data_table.dart

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
        Action<TapDownDetails>? onTapDown = null,
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
    public Action<TapDownDetails>? OnTapDown { get; }
    public Action? OnTapCancel { get; }
    internal bool IsInteractive => OnTap is not null || OnDoubleTap is not null || OnLongPress is not null || OnTapDown is not null || OnTapCancel is not null;
}

public sealed class DataTable : StatelessWidget
{
    private static readonly LocalKey HeadingRowKey = new UniqueKey();

    public DataTable(
        IReadOnlyList<DataColumn> columns,
        IReadOnlyList<DataRow> rows,
        int? sortColumnIndex = null,
        bool sortAscending = true,
        Action<bool?>? onSelectAll = null,
        Decoration? decoration = null,
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
        if (dataRowMinHeight > dataRowMaxHeight)
        {
            throw new ArgumentException("Maximum row height must be at least minimum row height.");
        }
        if (dividerThickness < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(dividerThickness));
        }

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
    public Decoration? Decoration { get; }
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
        ThemeData theme = Theme.Of(context);
        DataTableThemeData dataTableTheme = DataTableTheme.Of(context);
        DataTableThemeData globalDataTableTheme = theme.DataTableTheme;
        TextDirection textDirection = Directionality.Of(context);
        double horizontalMargin = HorizontalMargin
                                  ?? dataTableTheme.HorizontalMargin
                                  ?? globalDataTableTheme.HorizontalMargin
                                  ?? 24.0;
        double columnSpacing = ColumnSpacing
                               ?? dataTableTheme.ColumnSpacing
                               ?? globalDataTableTheme.ColumnSpacing
                               ?? 56.0;
        double checkboxMarginStart = CheckboxHorizontalMargin
                                     ?? dataTableTheme.CheckboxHorizontalMargin
                                     ?? globalDataTableTheme.CheckboxHorizontalMargin
                                     ?? horizontalMargin;
        double checkboxMarginEnd = CheckboxHorizontalMargin
                                   ?? dataTableTheme.CheckboxHorizontalMargin
                                   ?? globalDataTableTheme.CheckboxHorizontalMargin
                                   ?? horizontalMargin / 2.0;
        double headingHeight = HeadingRowHeight
                               ?? dataTableTheme.HeadingRowHeight
                               ?? globalDataTableTheme.HeadingRowHeight
                               ?? 56.0;
        double dataMinHeight = DataRowMinHeight
                               ?? dataTableTheme.DataRowMinHeight
                               ?? globalDataTableTheme.DataRowMinHeight
                               ?? 48.0;
        double dataMaxHeight = DataRowMaxHeight
                               ?? dataTableTheme.DataRowMaxHeight
                               ?? globalDataTableTheme.DataRowMaxHeight
                               ?? 48.0;
        TextStyle headingStyle = HeadingTextStyle
                                 ?? dataTableTheme.HeadingTextStyle
                                 ?? globalDataTableTheme.HeadingTextStyle
                                 ?? theme.TextTheme.TitleSmall;
        TextStyle dataStyle = DataTextStyle
                              ?? dataTableTheme.DataTextStyle
                              ?? globalDataTableTheme.DataTextStyle
                              ?? theme.TextTheme.BodyMedium;
        MaterialStateProperty<Color?>? effectiveDataRowColor = DataRowColor
                                                                ?? dataTableTheme.DataRowColor
                                                                ?? globalDataTableTheme.DataRowColor;
        MaterialStateProperty<Color?>? effectiveHeadingRowColor = HeadingRowColor
                                                                   ?? dataTableTheme.HeadingRowColor
                                                                   ?? globalDataTableTheme.HeadingRowColor;
        bool anySelectable = Rows.Any(row => row.OnSelectChanged is not null);
        bool displayCheckbox = ShowCheckboxColumn && anySelectable;
        var selectableRows = Rows.Where(row => row.OnSelectChanged is not null).ToArray();
        int selectedRows = selectableRows.Count(row => row.Selected);
        bool allChecked = displayCheckbox && selectedRows == selectableRows.Length;
        bool someChecked = displayCheckbox && selectedRows > 0 && !allChecked;
        var textColumns = Columns
            .Select((column, index) => (column, index))
            .Where(pair => !pair.column.Numeric)
            .ToArray();
        int? onlyTextColumn = textColumns.Length == 1 ? textColumns[0].index : (int?)null;
        var tableRows = new List<TableRow>();
        double dividerThickness = DividerThickness
                                  ?? dataTableTheme.DividerThickness
                                  ?? globalDataTableTheme.DividerThickness
                                  ?? 1.0;
        BorderSide divider = Divider.CreateBorderSide(context, width: dividerThickness);

        var headingChildren = new List<Widget>();
        if (displayCheckbox)
        {
            headingChildren.Add(BuildCheckbox(
                context: context,
                value: someChecked ? null : allChecked,
                tristate: true,
                onRowTap: null,
                onChanged: value => HandleSelectAll(value, someChecked)));
        }
        for (int columnIndex = 0; columnIndex < Columns.Count; columnIndex++)
        {
            EdgeInsetsGeometry padding = ResolveCellPadding(
                columnIndex,
                displayCheckbox,
                horizontalMargin,
                columnSpacing);
            headingChildren.Add(BuildHeadingCell(
                context,
                Columns[columnIndex],
                columnIndex,
                padding,
                headingHeight,
                headingStyle,
                dataTableTheme,
                effectiveHeadingRowColor,
                textDirection));
        }
        Color? headingColor = effectiveHeadingRowColor?.Resolve(MaterialState.None);
        var headingBorder = ShowBottomBorder
            ? new Plumix.Rendering.Border(bottom: divider)
            : null;
        tableRows.Add(new TableRow(
            headingChildren,
            key: HeadingRowKey,
            decoration: new BoxDecoration(Color: headingColor, Border: headingBorder)));

        foreach (var row in Rows)
        {
            var children = new List<Widget>();
            MaterialState colorStates = (row.Selected ? MaterialState.Selected : MaterialState.None)
                                        | (anySelectable && row.OnSelectChanged is null
                                            ? MaterialState.Disabled
                                            : MaterialState.None);
            MaterialState cursorStates = row.Selected ? MaterialState.Selected : MaterialState.None;
            if (displayCheckbox)
            {
                children.Add(BuildCheckbox(
                    context: context,
                    value: row.Selected,
                    tristate: false,
                    onRowTap: row.OnSelectChanged is null
                        ? null
                        : () => row.OnSelectChanged(!row.Selected),
                    onChanged: row.OnSelectChanged,
                    overlayColor: row.Color ?? effectiveDataRowColor,
                    mouseCursor: row.MouseCursor?.Resolve(cursorStates)
                                 ?? dataTableTheme.DataRowCursor?.Resolve(cursorStates)));
            }
            for (int columnIndex = 0; columnIndex < Columns.Count; columnIndex++)
            {
                children.Add(BuildDataCell(
                    context,
                    Columns[columnIndex],
                    row,
                    row.Cells[columnIndex],
                    ResolveCellPadding(columnIndex, displayCheckbox, horizontalMargin, columnSpacing),
                    dataMinHeight,
                    dataMaxHeight,
                    dataStyle,
                    row.Color ?? effectiveDataRowColor,
                    row.MouseCursor?.Resolve(cursorStates)
                    ?? dataTableTheme.DataRowCursor?.Resolve(cursorStates),
                    textDirection));
            }
            Color? rowColor = (row.Color ?? effectiveDataRowColor)?.Resolve(colorStates)
                              ?? (row.Selected ? WithOpacity(theme.ColorScheme.Primary, 0.08) : null);
            var rowBorder = ShowBottomBorder
                ? new Plumix.Rendering.Border(bottom: divider)
                : new Plumix.Rendering.Border(top: divider);
            tableRows.Add(new TableRow(
                children,
                row.Key,
                new BoxDecoration(Color: rowColor, Border: rowBorder)));
        }

        var widths = new Dictionary<int, TableColumnWidth>();
        int displayIndex = 0;
        if (displayCheckbox)
        {
            widths[displayIndex++] = new FixedColumnWidth(
                checkboxMarginStart + Checkbox.Width + checkboxMarginEnd);
        }
        for (int columnIndex = 0; columnIndex < Columns.Count; columnIndex++)
        {
            widths[displayIndex++] = Columns[columnIndex].ColumnWidth
                                     ?? (columnIndex == onlyTextColumn
                                         ? new IntrinsicColumnWidth(1.0)
                                         : new IntrinsicColumnWidth());
        }

        Widget table = new Table(
            children: tableRows,
            columnWidths: widths,
            border: Border,
            defaultVerticalAlignment: TableCellVerticalAlignment.Middle);
        Widget material = new Material(
            type: MaterialType.Transparency,
            borderRadius: Border?.BorderRadius,
            clipBehavior: ClipBehavior,
            child: table);
        Decoration? decoration = Decoration
                                 ?? dataTableTheme.Decoration
                                 ?? globalDataTableTheme.Decoration;
        return new Container(decoration: decoration, child: material);
    }

    private Widget BuildHeadingCell(
        BuildContext context,
        DataColumn column,
        int columnIndex,
        EdgeInsetsGeometry padding,
        double height,
        TextStyle style,
        DataTableThemeData tableTheme,
        MaterialStateProperty<Color?>? overlayColor,
        TextDirection textDirection)
    {
        bool sorted = SortColumnIndex == columnIndex;
        MainAxisAlignment alignment = column.HeadingRowAlignment
                                      ?? tableTheme.HeadingRowAlignment
                                      ?? MainAxisAlignment.Start;
        var content = new List<Widget>();
        if (alignment == MainAxisAlignment.Center && column.OnSort is not null)
        {
            content.Add(new SizedBox(width: 18.0));
        }
        content.Add(column.Label);
        if (column.OnSort is not null)
        {
            content.Add(new SortArrow(
                visible: sorted,
                up: sorted ? SortAscending : null,
                duration: TimeSpan.FromMilliseconds(150)));
            content.Add(new SizedBox(width: 2.0));
        }
        Widget label = new Semantics(
            role: SemanticsRole.ColumnHeader,
            child: new Row(
                mainAxisAlignment: alignment,
                textDirection: column.Numeric ? TextDirection.Rtl : null,
                children: content));
        TextStyle effectiveStyle = DefaultTextStyle.Of(context).Merge(style);
        Alignment cellAlignment = column.Numeric
            ? Alignment.CenterRight
            : textDirection == TextDirection.Rtl
                ? Alignment.CenterRight
                : Alignment.CenterLeft;
        label = new Container(
            height: height,
            padding: padding,
            alignment: cellAlignment,
            child: new AnimatedDefaultTextStyle(
                child: label,
                style: effectiveStyle,
                softWrap: false,
                duration: TimeSpan.FromMilliseconds(150)));
        if (column.Tooltip is not null)
        {
            label = new Tooltip(column.Tooltip, child: label);
        }
        MaterialState states = column.OnSort is null ? MaterialState.Disabled : MaterialState.None;
        Widget inkWell = new InkWell(
            onTap: column.OnSort is null
                ? null
                : () => column.OnSort(columnIndex, SortColumnIndex != columnIndex || !SortAscending),
            overlayColor: overlayColor,
            mouseCursor: column.MouseCursor?.Resolve(states) ?? tableTheme.HeadingCellCursor?.Resolve(states),
            child: label);
        return inkWell;
    }

    private static Widget BuildDataCell(
        BuildContext context,
        DataColumn column,
        DataRow row,
        DataCell cell,
        EdgeInsetsGeometry padding,
        double minHeight,
        double maxHeight,
        TextStyle style,
        MaterialStateProperty<Color?>? overlayColor,
        MouseCursor? cursor,
        TextDirection textDirection)
    {
        Widget label = cell.Child;
        if (cell.ShowEditIcon)
        {
            label = new Row(
                textDirection: column.Numeric ? TextDirection.Rtl : null,
                children: [new Expanded(label), new Icon(Icons.Edit, size: 18.0)]);
        }
        TextStyle effectiveStyle = DefaultTextStyle.Of(context).Merge(style);
        if (cell.Placeholder && style.Color.HasValue)
        {
            effectiveStyle = effectiveStyle.CopyWith(color: WithOpacity(style.Color.Value, 0.60));
        }
        Alignment cellAlignment = column.Numeric
            ? Alignment.CenterRight
            : textDirection == TextDirection.Rtl
                ? Alignment.CenterRight
                : Alignment.CenterLeft;
        label = new Container(
            constraints: new BoxConstraints(MinHeight: minHeight, MaxHeight: maxHeight),
            padding: padding,
            alignment: cellAlignment,
            child: new DefaultTextStyle(effectiveStyle, new DropdownButtonHideUnderline(label)));

        if (cell.IsInteractive)
        {
            label = new InkWell(
                onTap: cell.OnTap,
                onDoubleTap: cell.OnDoubleTap,
                onTapDown: cell.OnTapDown,
                onTapCancel: cell.OnTapCancel,
                onLongPress: cell.OnLongPress,
                overlayColor: overlayColor,
                child: label);
        }
        else if (row.OnSelectChanged is not null
                 || row.OnLongPress is not null
                 || row.OnHover is not null)
        {
            label = new TableRowInkWell(
                onTap: row.OnSelectChanged is null ? null : () => row.OnSelectChanged(!row.Selected),
                onLongPress: row.OnLongPress,
                onHover: row.OnHover,
                overlayColor: overlayColor,
                mouseCursor: cursor,
                child: label);
        }

        return new TableCell(label);
    }

    private TableCell BuildCheckbox(
        BuildContext context,
        bool? value,
        bool tristate,
        Action? onRowTap,
        Action<bool?>? onChanged,
        MaterialStateProperty<Color?>? overlayColor = null,
        MouseCursor? mouseCursor = null)
    {
        ThemeData theme = Theme.Of(context);
        double horizontalMargin = HorizontalMargin
                                  ?? theme.DataTableTheme.HorizontalMargin
                                  ?? 24.0;
        double horizontalStart = CheckboxHorizontalMargin
                                 ?? theme.DataTableTheme.CheckboxHorizontalMargin
                                 ?? horizontalMargin;
        double horizontalEnd = CheckboxHorizontalMargin
                               ?? theme.DataTableTheme.CheckboxHorizontalMargin
                               ?? horizontalMargin / 2.0;
        Widget contents = new Semantics(
            container: true,
            child: new Padding(
                EdgeInsetsGeometry.DirectionalOnly(start: horizontalStart, end: horizontalEnd),
                new Center(child: new Checkbox(value, onChanged, tristate: tristate))));
        if (onRowTap is not null)
        {
            contents = new TableRowInkWell(
                onTap: onRowTap,
                overlayColor: overlayColor,
                mouseCursor: mouseCursor,
                child: contents);
        }

        return new TableCell(contents, TableCellVerticalAlignment.Fill);
    }

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

    private EdgeInsetsGeometry ResolveCellPadding(
        int index,
        bool hasCheckbox,
        double horizontalMargin,
        double columnSpacing)
    {
        double start = index switch
        {
            0 when hasCheckbox && CheckboxHorizontalMargin is null => horizontalMargin / 2,
            0 => horizontalMargin,
            _ => columnSpacing / 2,
        };
        double end = index == Columns.Count - 1 ? horizontalMargin : columnSpacing / 2;
        return EdgeInsetsGeometry.DirectionalOnly(start: start, end: end);
    }

    private static Color WithOpacity(Color color, double opacity) =>
        Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255), color.R, color.G, color.B);

    private sealed class SortArrow : StatefulWidget
    {
        public SortArrow(bool visible, bool? up, TimeSpan duration)
        {
            Visible = visible;
            Up = up;
            Duration = duration;
        }

        public bool Visible { get; }

        public bool? Up { get; }

        public TimeSpan Duration { get; }

        public override State CreateState() => new SortArrowState();

        private sealed class SortArrowState : State
        {
            private AnimationController? _opacityController;
            private AnimationController? _orientationController;
            private double _orientationOffset;
            private bool? _up;

            private SortArrow CurrentWidget => (SortArrow)StateWidget;

            public override void InitState()
            {
                _up = CurrentWidget.Up;
                _opacityController = new AnimationController(duration: CurrentWidget.Duration, vsync: this);
                _opacityController.SetValue(CurrentWidget.Visible ? 1.0 : 0.0);
                _opacityController.Changed += HandleChanged;
                _orientationController = new AnimationController(duration: CurrentWidget.Duration, vsync: this);
                _orientationController.Changed += HandleChanged;
                _orientationController.Completed += HandleOrientationCompleted;
                if (CurrentWidget.Visible)
                {
                    _orientationOffset = CurrentWidget.Up == true ? 0.0 : Math.PI;
                }
            }

            public override void DidUpdateWidget(StatefulWidget oldWidget)
            {
                var oldArrow = (SortArrow)oldWidget;
                _opacityController!.Duration = CurrentWidget.Duration;
                _orientationController!.Duration = CurrentWidget.Duration;
                bool skipArrow = false;
                bool? newUp = CurrentWidget.Up ?? _up;
                if (oldArrow.Visible != CurrentWidget.Visible)
                {
                    if (CurrentWidget.Visible
                        && _opacityController.Status == AnimationStatus.Dismissed)
                    {
                        _orientationController.Stop();
                        _orientationController.SetValue(0.0);
                        _orientationOffset = newUp == true ? 0.0 : Math.PI;
                        skipArrow = true;
                    }

                    if (CurrentWidget.Visible)
                    {
                        _opacityController.Forward();
                    }
                    else
                    {
                        _opacityController.Reverse();
                    }
                }

                if (_up != newUp && !skipArrow)
                {
                    if (_orientationController.Status == AnimationStatus.Dismissed)
                    {
                        _orientationController.Forward();
                    }
                    else
                    {
                        _orientationController.Reverse();
                    }
                }
                _up = newUp;
            }

            public override Widget Build(BuildContext context)
            {
                double opacity = Curves.FastOutSlowIn(_opacityController!.Value);
                double angle = _orientationOffset
                               + (Math.PI * Curves.EaseIn(_orientationController!.Value));
                double cosine = Math.Cos(angle);
                double sine = Math.Sin(angle);
                var transform = new Matrix4(
                    cosine, sine, 0.0, 0.0,
                    -sine, cosine, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, -1.5, 0.0, 1.0);
                return new Opacity(
                    opacity,
                    new Plumix.Widgets.Transform(
                        transform,
                        alignment: Alignment.Center,
                        child: new Icon(Icons.ArrowUpward, size: 16.0)));
            }

            public override void Dispose()
            {
                _opacityController!.Changed -= HandleChanged;
                _orientationController!.Changed -= HandleChanged;
                _orientationController.Completed -= HandleOrientationCompleted;
                _opacityController.Dispose();
                _orientationController.Dispose();
                _opacityController = null;
                _orientationController = null;
            }

            private void HandleChanged() => SetState(() => { });

            private void HandleOrientationCompleted()
            {
                _orientationOffset += Math.PI;
                _orientationController!.SetValue(0.0);
            }
        }
    }
}
