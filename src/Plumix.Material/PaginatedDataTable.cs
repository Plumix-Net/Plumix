using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/data_table_source.dart
// flutter/packages/flutter/lib/src/material/paginated_data_table.dart

public abstract class DataTableSource : ChangeNotifier
{
    public abstract DataRow? GetRow(int index);
    public abstract int RowCount { get; }
    public abstract bool IsRowCountApproximate { get; }
    public abstract int SelectedRowCount { get; }
}

public sealed class PaginatedDataTable : StatefulWidget
{
    public const int DefaultRowsPerPage = 10;
    private static readonly int[] DefaultAvailableRowsPerPage = [10, 20, 50, 100];

    public PaginatedDataTable(
        IReadOnlyList<DataColumn> columns,
        DataTableSource source,
        Widget? header = null,
        IReadOnlyList<Widget>? actions = null,
        int? sortColumnIndex = null,
        bool sortAscending = true,
        Action<bool?>? onSelectAll = null,
        double? dataRowHeight = null,
        double? dataRowMinHeight = null,
        double? dataRowMaxHeight = null,
        double headingRowHeight = 56.0,
        double horizontalMargin = 24.0,
        double columnSpacing = 56.0,
        bool showCheckboxColumn = true,
        bool showFirstLastButtons = false,
        int? initialFirstRowIndex = 0,
        Action<int>? onPageChanged = null,
        int rowsPerPage = DefaultRowsPerPage,
        IReadOnlyList<int>? availableRowsPerPage = null,
        Action<int?>? onRowsPerPageChanged = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        Color? arrowHeadColor = null,
        double? dividerThickness = null,
        double? checkboxHorizontalMargin = null,
        ScrollController? controller = null,
        bool? primary = null,
        MaterialStateProperty<Color?>? headingRowColor = null,
        bool showEmptyRows = true,
        Key? key = null) : base(key)
    {
        if (actions is not null && header is null) throw new ArgumentException("actions require a header.", nameof(actions));
        if (columns is null || columns.Count == 0) throw new ArgumentException("At least one column is required.", nameof(columns));
        ArgumentNullException.ThrowIfNull(source);
        if (sortColumnIndex.HasValue && (sortColumnIndex.Value < 0 || sortColumnIndex.Value >= columns.Count))
            throw new ArgumentOutOfRangeException(nameof(sortColumnIndex));
        if (dataRowHeight.HasValue && (dataRowMinHeight.HasValue || dataRowMaxHeight.HasValue))
            throw new ArgumentException("dataRowHeight cannot be combined with min/max row heights.");
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
        if (rowsPerPage <= 0) throw new ArgumentOutOfRangeException(nameof(rowsPerPage));
        var available = availableRowsPerPage ?? DefaultAvailableRowsPerPage;
        if (available.Any(value => value <= 0)) throw new ArgumentOutOfRangeException(nameof(availableRowsPerPage));
        if (onRowsPerPageChanged is not null && !available.Contains(rowsPerPage))
            throw new ArgumentException("availableRowsPerPage must contain rowsPerPage when the selector is enabled.", nameof(availableRowsPerPage));
        if (controller is not null && primary == true)
            throw new ArgumentException("An explicit controller cannot be combined with primary=true.", nameof(primary));

        Header = header;
        Actions = actions;
        Columns = columns;
        SortColumnIndex = sortColumnIndex;
        SortAscending = sortAscending;
        OnSelectAll = onSelectAll;
        DataRowMinHeight = dataRowMinHeight;
        DataRowMaxHeight = dataRowMaxHeight;
        HeadingRowHeight = headingRowHeight;
        HorizontalMargin = horizontalMargin;
        ColumnSpacing = columnSpacing;
        ShowCheckboxColumn = showCheckboxColumn;
        ShowFirstLastButtons = showFirstLastButtons;
        InitialFirstRowIndex = initialFirstRowIndex;
        OnPageChanged = onPageChanged;
        RowsPerPage = rowsPerPage;
        AvailableRowsPerPage = available;
        OnRowsPerPageChanged = onRowsPerPageChanged;
        DragStartBehavior = dragStartBehavior;
        ArrowHeadColor = arrowHeadColor;
        Source = source;
        DividerThickness = dividerThickness;
        CheckboxHorizontalMargin = checkboxHorizontalMargin;
        Controller = controller;
        Primary = primary;
        HeadingRowColor = headingRowColor;
        ShowEmptyRows = showEmptyRows;
    }

    public Widget? Header { get; }
    public IReadOnlyList<Widget>? Actions { get; }
    public IReadOnlyList<DataColumn> Columns { get; }
    public int? SortColumnIndex { get; }
    public bool SortAscending { get; }
    public Action<bool?>? OnSelectAll { get; }
    public double? DataRowMinHeight { get; }
    public double? DataRowMaxHeight { get; }
    public double? DataRowHeight => DataRowMinHeight == DataRowMaxHeight ? DataRowMinHeight : null;
    public double HeadingRowHeight { get; }
    public double HorizontalMargin { get; }
    public double ColumnSpacing { get; }
    public bool ShowCheckboxColumn { get; }
    public bool ShowFirstLastButtons { get; }
    public int? InitialFirstRowIndex { get; }
    public Action<int>? OnPageChanged { get; }
    public int RowsPerPage { get; }
    public IReadOnlyList<int> AvailableRowsPerPage { get; }
    public Action<int?>? OnRowsPerPageChanged { get; }
    public DataTableSource Source { get; }
    public DragStartBehavior DragStartBehavior { get; }
    public Color? ArrowHeadColor { get; }
    public double? DividerThickness { get; }
    public double? CheckboxHorizontalMargin { get; }
    public ScrollController? Controller { get; }
    public bool? Primary { get; }
    public MaterialStateProperty<Color?>? HeadingRowColor { get; }
    public bool ShowEmptyRows { get; }

    public override State CreateState() => new PaginatedDataTableState();

    private static void ValidateNonNegative(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
            throw new ArgumentOutOfRangeException(name);
    }
}

public sealed class PaginatedDataTableState : State
{
    private int _firstRowIndex;
    private int _rowCount;
    private bool _rowCountApproximate;
    private int _selectedRowCount;
    private readonly Dictionary<int, DataRow?> _rows = [];

    private PaginatedDataTable CurrentWidget => (PaginatedDataTable)StateWidget;
    public int FirstRowIndex => _firstRowIndex;

    public override void InitState()
    {
        _firstRowIndex = CurrentWidget.InitialFirstRowIndex ?? 0;
        CurrentWidget.Source.AddListener(HandleDataSourceChanged);
        UpdateCaches();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldTable = (PaginatedDataTable)oldWidget;
        if (!ReferenceEquals(oldTable.Source, CurrentWidget.Source))
        {
            oldTable.Source.RemoveListener(HandleDataSourceChanged);
            CurrentWidget.Source.AddListener(HandleDataSourceChanged);
            UpdateCaches();
        }
    }

    public override void Reassemble() => UpdateCaches();

    public override void Dispose() => CurrentWidget.Source.RemoveListener(HandleDataSourceChanged);

    public void PageTo(int rowIndex)
    {
        if (rowIndex < 0) throw new ArgumentOutOfRangeException(nameof(rowIndex));
        int oldIndex = _firstRowIndex;
        SetState(() => _firstRowIndex = (rowIndex / CurrentWidget.RowsPerPage) * CurrentWidget.RowsPerPage);
        if (oldIndex != _firstRowIndex) CurrentWidget.OnPageChanged?.Invoke(_firstRowIndex);
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        var children = new List<Widget>();

        var headerWidgets = new List<Widget>();
        if (widget.Header is not null)
        {
            headerWidgets.Add(new Expanded(_selectedRowCount == 0
                ? widget.Header
                : new Text(localizations.SelectedRowCountTitle(_selectedRowCount))));
        }
        if (widget.Actions is not null)
        {
            headerWidgets.AddRange(widget.Actions.Select(action => new Padding(new Thickness(8, 0, 0, 0), action)));
        }
        if (headerWidgets.Count > 0)
        {
            var headerStyle = (_selectedRowCount > 0 ? theme.TextTheme.TitleMedium : theme.TextTheme.TitleLarge)
                .CopyWith(color: _selectedRowCount > 0 ? theme.SecondaryColor : null);
            Widget header = new Container(
                height: 64,
                color: _selectedRowCount > 0 ? WithOpacity(theme.SecondaryColor, 0.12) : null,
                padding: new Thickness(24, 0, 14, 0),
                child: new DefaultTextStyle(
                    headerStyle,
                    new Row(mainAxisAlignment: MainAxisAlignment.End, children: headerWidgets)));
            children.Add(new Semantics(container: true, child: header));
        }

        children.Add(new SingleChildScrollView(
            scrollDirection: Axis.Horizontal,
            controller: widget.Controller,
            child: new DataTable(
                columns: widget.Columns,
                rows: GetRows(_firstRowIndex, widget.RowsPerPage),
                sortColumnIndex: widget.SortColumnIndex,
                sortAscending: widget.SortAscending,
                onSelectAll: widget.OnSelectAll,
                dataRowMinHeight: widget.DataRowMinHeight,
                dataRowMaxHeight: widget.DataRowMaxHeight,
                headingRowHeight: widget.HeadingRowHeight,
                horizontalMargin: widget.HorizontalMargin,
                checkboxHorizontalMargin: widget.CheckboxHorizontalMargin,
                columnSpacing: widget.ColumnSpacing,
                showCheckboxColumn: widget.ShowCheckboxColumn,
                showBottomBorder: true,
                dividerThickness: widget.DividerThickness,
                headingRowColor: widget.HeadingRowColor)));

        if (!widget.ShowEmptyRows)
        {
            int missingRows = Math.Clamp(widget.RowsPerPage - _rowCount + _firstRowIndex, 0, widget.RowsPerPage);
            if (missingRows > 0) children.Add(new SizedBox(height: (widget.DataRowMaxHeight ?? 48) * missingRows));
        }

        children.Add(new DefaultTextStyle(
            theme.TextTheme.BodySmall,
            new SizedBox(
                height: 56,
                child: new SingleChildScrollView(
                    scrollDirection: Axis.Horizontal,
                    reverse: true,
                    child: new Row(mainAxisSize: MainAxisSize.Min, children: BuildFooter(localizations))))));

        return new Card(
            semanticContainer: false,
            child: new Column(crossAxisAlignment: CrossAxisAlignment.Stretch, children: children));
    }

    private IReadOnlyList<Widget> BuildFooter(MaterialLocalizations localizations)
    {
        var widget = CurrentWidget;
        var footer = new List<Widget>();
        if (widget.OnRowsPerPageChanged is not null)
        {
            var items = widget.AvailableRowsPerPage
                .Where(value => value <= _rowCount || value == widget.RowsPerPage)
                .Select(value => new DropdownMenuItem<int>(new Text(value.ToString()), value))
                .ToArray();
            footer.Add(new SizedBox(width: 14));
            footer.Add(new Text(localizations.RowsPerPageTitle));
            footer.Add(new ConstrainedBox(
                new BoxConstraints(MinWidth: 64),
                new Align(
                    alignment: Alignment.CenterRight,
                    child: new DropdownButtonHideUnderline(new DropdownButton<int>(
                        items,
                        value => widget.OnRowsPerPageChanged?.Invoke(value),
                        value: widget.RowsPerPage)))));
        }
        int lastRow = Math.Min(_firstRowIndex + widget.RowsPerPage, _rowCount);
        footer.Add(new SizedBox(width: 32));
        footer.Add(new Text(localizations.PageRowsInfoTitle(
            Math.Min(_firstRowIndex + 1, Math.Max(_rowCount, 1)),
            lastRow,
            _rowCount,
            _rowCountApproximate)));
        footer.Add(new SizedBox(width: 32));
        if (widget.ShowFirstLastButtons)
            footer.Add(PageButton(Icons.FirstPage, localizations.FirstPageTooltip, _firstRowIndex <= 0 ? null : () => PageTo(0)));
        footer.Add(PageButton(Icons.ChevronLeft, localizations.PreviousPageTooltip, _firstRowIndex <= 0 ? null : HandlePrevious));
        footer.Add(new SizedBox(width: 24));
        footer.Add(PageButton(Icons.ChevronRight, localizations.NextPageTooltip, IsNextUnavailable() ? null : HandleNext));
        if (widget.ShowFirstLastButtons)
            footer.Add(PageButton(Icons.LastPage, localizations.LastPageTooltip, IsNextUnavailable() ? null : HandleLast));
        footer.Add(new SizedBox(width: 14));
        return footer;
    }

    private Widget PageButton(IconData icon, string tooltip, Action? onPressed) => new Tooltip(
        tooltip,
        new IconButton(
            new Icon(icon),
            onPressed,
            color: CurrentWidget.ArrowHeadColor,
            padding: new Thickness(0)));

    private IReadOnlyList<DataRow> GetRows(int firstRowIndex, int rowsPerPage)
    {
        var result = new List<DataRow>();
        bool haveProgress = false;
        for (int index = firstRowIndex; index < firstRowIndex + rowsPerPage; index++)
        {
            DataRow? row = null;
            if (index < _rowCount || _rowCountApproximate)
            {
                if (!_rows.TryGetValue(index, out row))
                {
                    row = CurrentWidget.Source.GetRow(index);
                    _rows[index] = row;
                }
                if (row is null && !haveProgress)
                {
                    row = ProgressRow(index);
                    haveProgress = true;
                }
            }
            if (CurrentWidget.ShowEmptyRows) row ??= BlankRow(index);
            if (row is not null) result.Add(row);
        }
        return result;
    }

    private DataRow BlankRow(int index) => DataRow.ByIndex(CurrentWidget.Columns.Select(_ => DataCell.Empty).ToArray(), index);

    private DataRow ProgressRow(int index)
    {
        bool inserted = false;
        var cells = CurrentWidget.Columns.Select(column =>
        {
            if (!inserted && !column.Numeric)
            {
                inserted = true;
                return new DataCell(new CircularProgressIndicator());
            }
            return DataCell.Empty;
        }).ToArray();
        if (!inserted) cells[0] = new DataCell(new CircularProgressIndicator());
        return DataRow.ByIndex(cells, index);
    }

    private void HandlePrevious() => PageTo(Math.Max(_firstRowIndex - CurrentWidget.RowsPerPage, 0));
    private void HandleNext() => PageTo(_firstRowIndex + CurrentWidget.RowsPerPage);
    private void HandleLast() => PageTo(Math.Max(0, ((_rowCount - 1) / CurrentWidget.RowsPerPage) * CurrentWidget.RowsPerPage));
    private bool IsNextUnavailable() => !_rowCountApproximate && _firstRowIndex + CurrentWidget.RowsPerPage >= _rowCount;

    private void HandleDataSourceChanged() => SetState(UpdateCaches);

    private void UpdateCaches()
    {
        _rowCount = CurrentWidget.Source.RowCount;
        _rowCountApproximate = CurrentWidget.Source.IsRowCountApproximate;
        _selectedRowCount = CurrentWidget.Source.SelectedRowCount;
        _rows.Clear();
    }

    private static Color WithOpacity(Color color, double opacity) =>
        Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255), color.R, color.G, color.B);
}
