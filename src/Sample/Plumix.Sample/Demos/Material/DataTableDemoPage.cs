using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/data_table_demo_page.dart

public sealed class DataTableDemoPage : StatefulWidget
{
    public override State CreateState() => new DataTableDemoPageState();
}

internal sealed class DataTableDemoPageState : State
{
    private readonly PeopleDataSource _source = new();
    private int? _sortColumnIndex;
    private bool _sortAscending = true;
    private bool _useThemeOverrides;
    private bool _showCheckboxes = true;
    private int _rowsPerPage = 5;

    public override void Dispose() => _source.Dispose();

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var theme = baseTheme with
        {
            DataTableTheme = _useThemeOverrides
                ? new DataTableThemeData(
                    headingRowColor: MaterialStateProperty<Color?>.All(Color.Parse("#FFE8DEF8")),
                    dataRowMinHeight: 44,
                    dataRowMaxHeight: 52,
                    horizontalMargin: 16,
                    columnSpacing: 28,
                    dividerThickness: 2)
                : new DataTableThemeData(),
        };

        return new Theme(
            theme,
            new SingleChildScrollView(
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 12,
                    children:
                    [
                        new Text("DataTable + PaginatedDataTable", fontSize: 20, color: Colors.Black),
                        new Text(
                            "Intrinsic columns, numeric alignment, row-wide TableRowInkWell selection, theme "
                            + "precedence, source caching, and page controls.",
                            fontSize: 14,
                            color: Color.Parse("#8A000000")),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                BuildToggle(_useThemeOverrides ? "theme=on" : "theme=off", () => _useThemeOverrides = !_useThemeOverrides),
                                BuildToggle(_showCheckboxes ? "checks=on" : "checks=off", () => _showCheckboxes = !_showCheckboxes),
                            ]),
                        new Text("Static DataTable", fontSize: 16, color: Colors.Black),
                        new SingleChildScrollView(
                            scrollDirection: Axis.Horizontal,
                            child: BuildTable(_source.Items.Take(4).ToArray())),
                        new Text("Core TableCell vertical alignment", fontSize: 16, color: Colors.Black),
                        new SingleChildScrollView(
                            scrollDirection: Axis.Horizontal,
                            child: BuildTableCellProbe()),
                        new Text("PaginatedDataTable", fontSize: 16, color: Colors.Black),
                        new PaginatedDataTable(
                            header: new Text("People"),
                            actions:
                            [
                                new IconButton(new Icon(Icons.Add), () => _source.AddPerson()),
                            ],
                            columns: BuildColumns(),
                            source: _source,
                            sortColumnIndex: _sortColumnIndex,
                            sortAscending: _sortAscending,
                            rowsPerPage: _rowsPerPage,
                            availableRowsPerPage: [5, 10],
                            onRowsPerPageChanged: value => SetState(() => _rowsPerPage = value ?? 5),
                            showCheckboxColumn: _showCheckboxes,
                            showFirstLastButtons: true,
                            onSelectAll: value => _source.SelectAll(value ?? false)),
                    ])));
    }

    private DataTable BuildTable(IReadOnlyList<PersonRow> people) => new(
        columns: BuildColumns(),
        rows: people.Select(person => _source.BuildRow(person, () => SetState(static () => { }))).ToArray(),
        sortColumnIndex: _sortColumnIndex,
        sortAscending: _sortAscending,
        showCheckboxColumn: _showCheckboxes,
        showBottomBorder: true,
        onSelectAll: value => _source.SelectAll(value ?? false));

    private static Table BuildTableCellProbe() => new(
        defaultColumnWidth: new FixedColumnWidth(100),
        border: TableBorder.All(new BorderSide(Color.Parse("#FF94A3B8"))),
        children:
        [
            new TableRow(
            [
                BuildProbeCell("top", 28, TableCellVerticalAlignment.Top, "#FFE0F2FE"),
                BuildProbeCell("middle", 52, TableCellVerticalAlignment.Middle, "#FFDCFCE7"),
                BuildProbeCell("bottom", 32, TableCellVerticalAlignment.Bottom, "#FFFFEDD5"),
            ]),
        ]);

    private static TableCell BuildProbeCell(
        string label,
        double height,
        TableCellVerticalAlignment alignment,
        string colorHex) => new(
        verticalAlignment: alignment,
        child: new Container(
            height: height,
            color: Color.Parse(colorHex),
            child: new Center(
                child: new Text(label, fontSize: 12, color: Colors.Black))));

    private IReadOnlyList<DataColumn> BuildColumns() =>
    [
        new DataColumn(new Text("Name"), onSort: (index, ascending) => Sort(index, ascending)),
        new DataColumn(new Text("Role")),
        new DataColumn(new Text("Score"), numeric: true, onSort: (index, ascending) => Sort(index, ascending)),
    ];

    private void Sort(int columnIndex, bool ascending)
    {
        _source.Sort(columnIndex, ascending);
        SetState(() =>
        {
            _sortColumnIndex = columnIndex;
            _sortAscending = ascending;
        });
    }

    private Widget BuildToggle(string label, Action update) => new SizedBox(
        width: 108,
        child: new TextButton(
            onPressed: () => SetState(update),
            minHeight: 36,
            padding: new Thickness(8, 6),
            backgroundColor: Color.Parse("#FFE9F0FF"),
            foregroundColor: Colors.Black,
            borderRadius: BorderRadius.Circular(8),
            child: new Text(label, fontSize: 11)));
}

internal sealed class PeopleDataSource : DataTableSource
{
    private int _nextId = 11;
    public List<PersonRow> Items { get; } =
    [
        new(1, "Ada", "Engineer", 10),
        new(2, "Grace", "Admiral", 9),
        new(3, "Katherine", "Mathematician", 10),
        new(4, "Margaret", "Engineer", 8),
        new(5, "Dorothy", "Manager", 9),
        new(6, "Mary", "Engineer", 8),
        new(7, "Annie", "Astronomer", 9),
        new(8, "Hedy", "Inventor", 8),
        new(9, "Radia", "Engineer", 10),
        new(10, "Evelyn", "Cryptanalyst", 9),
    ];

    public override int RowCount => Items.Count;
    public override bool IsRowCountApproximate => false;
    public override int SelectedRowCount => Items.Count(person => person.Selected);
    public override DataRow? GetRow(int index) => index >= 0 && index < Items.Count ? BuildRow(Items[index]) : null;

    public DataRow BuildRow(PersonRow person, Action? onChanged = null) => new(
        key: new ValueKey<int>(person.Id),
        selected: person.Selected,
        onSelectChanged: value =>
        {
            person.Selected = value ?? false;
            NotifyListeners();
            onChanged?.Invoke();
        },
        cells:
        [
            new DataCell(new Text(person.Name)),
            new DataCell(new Text(person.Role)),
            new DataCell(new Text(person.Score.ToString()), showEditIcon: person.Score < 9),
        ]);

    public void Sort(int columnIndex, bool ascending)
    {
        Items.Sort((left, right) =>
        {
            int result = columnIndex == 2
                ? left.Score.CompareTo(right.Score)
                : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            return ascending ? result : -result;
        });
        NotifyListeners();
    }

    public void SelectAll(bool selected)
    {
        foreach (var person in Items) person.Selected = selected;
        NotifyListeners();
    }

    public void AddPerson()
    {
        Items.Add(new PersonRow(_nextId++, $"Person {_nextId}", "New row", 7));
        NotifyListeners();
    }
}

internal sealed record PersonRow(int Id, string Name, string Role, int Score)
{
    public bool Selected { get; set; }
}
