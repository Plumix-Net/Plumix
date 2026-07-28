import 'package:flutter/material.dart';

class DataTableDemoPage extends StatefulWidget {
  const DataTableDemoPage({super.key});

  @override
  State<DataTableDemoPage> createState() => _DataTableDemoPageState();
}

class _DataTableDemoPageState extends State<DataTableDemoPage> {
  final _PeopleDataSource _source = _PeopleDataSource();
  int? _sortColumnIndex;
  bool _sortAscending = true;
  bool _useThemeOverrides = false;
  bool _showCheckboxes = true;
  int _rowsPerPage = 5;

  @override
  void dispose() {
    _source.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData theme = baseTheme.copyWith(
      dataTableTheme: _useThemeOverrides
          ? const DataTableThemeData(
              headingRowColor: WidgetStatePropertyAll<Color>(Color(0xFFE8DEF8)),
              dataRowMinHeight: 44,
              dataRowMaxHeight: 52,
              horizontalMargin: 16,
              columnSpacing: 28,
              dividerThickness: 2,
            )
          : const DataTableThemeData(),
    );

    return Theme(
      data: theme,
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            const Text(
              'DataTable + PaginatedDataTable',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const SizedBox(height: 8),
            const Text(
              'Intrinsic columns, numeric alignment, row-wide TableRowInkWell '
              'selection, theme precedence, source caching, and page controls.',
              style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
            ),
            const SizedBox(height: 12),
            Row(
              children: <Widget>[
                _buildToggle(
                  _useThemeOverrides ? 'theme=on' : 'theme=off',
                  () => _useThemeOverrides = !_useThemeOverrides,
                ),
                const SizedBox(width: 8),
                _buildToggle(
                  _showCheckboxes ? 'checks=on' : 'checks=off',
                  () => _showCheckboxes = !_showCheckboxes,
                ),
              ],
            ),
            const SizedBox(height: 12),
            const Text(
              'Static DataTable',
              style: TextStyle(fontSize: 16, color: Colors.black),
            ),
            const SizedBox(height: 8),
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: _buildTable(_source.items.take(4).toList()),
            ),
            const SizedBox(height: 12),
            const Text(
              'Core TableCell vertical alignment',
              style: TextStyle(fontSize: 16, color: Colors.black),
            ),
            const SizedBox(height: 8),
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: _buildTableCellProbe(),
            ),
            const SizedBox(height: 12),
            const Text(
              'PaginatedDataTable',
              style: TextStyle(fontSize: 16, color: Colors.black),
            ),
            const SizedBox(height: 8),
            PaginatedDataTable(
              header: const Text('People'),
              actions: <Widget>[
                IconButton(
                  onPressed: _source.addPerson,
                  icon: const Icon(Icons.add),
                ),
              ],
              columns: _buildColumns(),
              source: _source,
              sortColumnIndex: _sortColumnIndex,
              sortAscending: _sortAscending,
              rowsPerPage: _rowsPerPage,
              availableRowsPerPage: const <int>[5, 10],
              onRowsPerPageChanged: (int? value) {
                setState(() => _rowsPerPage = value ?? 5);
              },
              showCheckboxColumn: _showCheckboxes,
              showFirstLastButtons: true,
              onSelectAll: (bool? value) => _source.selectAll(value ?? false),
            ),
          ],
        ),
      ),
    );
  }

  DataTable _buildTable(List<_PersonRow> people) {
    return DataTable(
      columns: _buildColumns(),
      rows: people
          .map(
            (_PersonRow person) =>
                _source.buildRow(person, onChanged: () => setState(() {})),
          )
          .toList(),
      sortColumnIndex: _sortColumnIndex,
      sortAscending: _sortAscending,
      showCheckboxColumn: _showCheckboxes,
      showBottomBorder: true,
      onSelectAll: (bool? value) => _source.selectAll(value ?? false),
    );
  }

  Table _buildTableCellProbe() {
    return Table(
      defaultColumnWidth: const FixedColumnWidth(100),
      border: TableBorder.all(color: const Color(0xFF94A3B8)),
      children: <TableRow>[
        TableRow(
          children: <Widget>[
            _buildProbeCell(
              'top',
              28,
              TableCellVerticalAlignment.top,
              const Color(0xFFE0F2FE),
            ),
            _buildProbeCell(
              'middle',
              52,
              TableCellVerticalAlignment.middle,
              const Color(0xFFDCFCE7),
            ),
            _buildProbeCell(
              'bottom',
              32,
              TableCellVerticalAlignment.bottom,
              const Color(0xFFFFEDD5),
            ),
          ],
        ),
      ],
    );
  }

  TableCell _buildProbeCell(
    String label,
    double height,
    TableCellVerticalAlignment alignment,
    Color color,
  ) {
    return TableCell(
      verticalAlignment: alignment,
      child: Container(
        height: height,
        color: color,
        alignment: Alignment.center,
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }

  List<DataColumn> _buildColumns() {
    return <DataColumn>[
      DataColumn(
        label: const Text('Name'),
        onSort: (int index, bool ascending) => _sort(index, ascending),
      ),
      const DataColumn(label: Text('Role')),
      DataColumn(
        label: const Text('Score'),
        numeric: true,
        onSort: (int index, bool ascending) => _sort(index, ascending),
      ),
    ];
  }

  void _sort(int columnIndex, bool ascending) {
    _source.sort(columnIndex, ascending);
    setState(() {
      _sortColumnIndex = columnIndex;
      _sortAscending = ascending;
    });
  }

  Widget _buildToggle(String label, VoidCallback update) {
    return SizedBox(
      width: 108,
      child: TextButton(
        onPressed: () => setState(update),
        style: TextButton.styleFrom(
          minimumSize: const Size(0, 36),
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
          backgroundColor: const Color(0xFFE9F0FF),
          foregroundColor: Colors.black,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 11)),
      ),
    );
  }
}

class _PeopleDataSource extends DataTableSource {
  int _nextId = 11;

  final List<_PersonRow> items = <_PersonRow>[
    _PersonRow(1, 'Ada', 'Engineer', 10),
    _PersonRow(2, 'Grace', 'Admiral', 9),
    _PersonRow(3, 'Katherine', 'Mathematician', 10),
    _PersonRow(4, 'Margaret', 'Engineer', 8),
    _PersonRow(5, 'Dorothy', 'Manager', 9),
    _PersonRow(6, 'Mary', 'Engineer', 8),
    _PersonRow(7, 'Annie', 'Astronomer', 9),
    _PersonRow(8, 'Hedy', 'Inventor', 8),
    _PersonRow(9, 'Radia', 'Engineer', 10),
    _PersonRow(10, 'Evelyn', 'Cryptanalyst', 9),
  ];

  @override
  int get rowCount => items.length;

  @override
  bool get isRowCountApproximate => false;

  @override
  int get selectedRowCount =>
      items.where((_PersonRow row) => row.selected).length;

  @override
  DataRow? getRow(int index) {
    return index >= 0 && index < items.length ? buildRow(items[index]) : null;
  }

  DataRow buildRow(_PersonRow person, {VoidCallback? onChanged}) {
    return DataRow(
      key: ValueKey<int>(person.id),
      selected: person.selected,
      onSelectChanged: (bool? value) {
        person.selected = value ?? false;
        notifyListeners();
        onChanged?.call();
      },
      cells: <DataCell>[
        DataCell(Text(person.name)),
        DataCell(Text(person.role)),
        DataCell(Text('${person.score}'), showEditIcon: person.score < 9),
      ],
    );
  }

  void sort(int columnIndex, bool ascending) {
    items.sort((_PersonRow left, _PersonRow right) {
      final int result = columnIndex == 2
          ? left.score.compareTo(right.score)
          : left.name.toLowerCase().compareTo(right.name.toLowerCase());
      return ascending ? result : -result;
    });
    notifyListeners();
  }

  void selectAll(bool selected) {
    for (final _PersonRow person in items) {
      person.selected = selected;
    }
    notifyListeners();
  }

  void addPerson() {
    items.add(_PersonRow(_nextId++, 'Person $_nextId', 'New row', 7));
    notifyListeners();
  }
}

class _PersonRow {
  _PersonRow(this.id, this.name, this.role, this.score);

  final int id;
  final String name;
  final String role;
  final int score;
  bool selected = false;
}
