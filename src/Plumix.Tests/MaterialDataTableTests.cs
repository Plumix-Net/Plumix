using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDataTableTests : IDisposable
{
    public MaterialDataTableTests() => Scheduler.ResetForTests();
    public void Dispose() => Scheduler.ResetForTests();

    [Fact]
    public void DataTable_ModelsAndConstructorMatchFlutterContracts()
    {
        var column = new DataColumn(new Text("Name"));
        var cell = new DataCell(new Text("Ada"));
        var row = DataRow.ByIndex([cell], index: 4, selected: true);
        var table = new DataTable([column], [row]);

        Assert.False(column.Numeric);
        Assert.Null(column.OnSort);
        Assert.False(cell.Placeholder);
        Assert.False(cell.ShowEditIcon);
        Assert.Equal(new ValueKey<int?>(4), row.Key);
        Assert.True(row.Selected);
        Assert.True(table.SortAscending);
        Assert.True(table.ShowCheckboxColumn);
        Assert.False(table.ShowBottomBorder);
        Assert.Equal(Clip.None, table.ClipBehavior);

        Assert.Throws<ArgumentException>(() => new DataTable([], []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DataTable([column], [], sortColumnIndex: 1));
        Assert.Throws<ArgumentException>(() => new DataTable(
            [column],
            [new DataRow([cell, cell])]));
        Assert.Throws<ArgumentException>(() => new DataTable(
            [column],
            [],
            dataRowHeight: 48,
            dataRowMinHeight: 40));
        Assert.Throws<ArgumentException>(() => new DataTable(
            [column],
            [],
            dataRowMinHeight: 50,
            dataRowMaxHeight: 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DataTable([column], [], dividerThickness: -1));
    }

    [Fact]
    public void DataTable_UsesAlignedTableLayoutAndFlutterDefaultGeometry()
    {
        using var harness = new WidgetRenderHarness(Wrap(new DataTable(
            columns:
            [
                new DataColumn(new Text("Name")),
                new DataColumn(new Text("Score"), numeric: true),
            ],
            rows:
            [
                new DataRow([new DataCell(new Text("Ada Lovelace")), new DataCell(new Text("10"))]),
                new DataRow([new DataCell(new Text("Grace")), new DataCell(new Text("8"))]),
            ])));

        harness.Pump(new Size(480, 260));
        var table = Assert.Single(FindDescendants<RenderTable>(harness.RenderView));
        Assert.Equal(2, table.Columns);
        Assert.Equal(3, table.Rows);
        Assert.Equal([56.0, 48.0, 48.0], table.ResolvedRowHeights.Select(value => Math.Round(value, 3)).ToArray());
        Assert.Equal(480, table.Size.Width, 3);
        Assert.True(table.ResolvedColumnWidths[0] > table.ResolvedColumnWidths[1]);
        Assert.NotNull(FindParagraph(harness.RenderView, "Ada Lovelace"));
        Assert.NotNull(FindParagraph(harness.RenderView, "8"));
    }

    [Fact]
    public void DataTable_ThemeAndWidgetValuesFollowPrecedence()
    {
        var globalTheme = ThemeData.Light with
        {
            DataTableTheme = new DataTableThemeData(
                dataRowMinHeight: 44,
                dataRowMaxHeight: 52,
                headingRowHeight: 60,
                horizontalMargin: 12,
                columnSpacing: 20,
                headingRowColor: MaterialStateProperty<Color?>.All(Colors.Gold),
                dataRowColor: MaterialStateProperty<Color?>.All(Colors.MistyRose)),
        };
        using var themed = new WidgetRenderHarness(Wrap(
            new DataTableTheme(
                new DataTableThemeData(
                    dataRowMinHeight: 50,
                    dataRowMaxHeight: 50,
                    headingRowHeight: 64),
                SimpleTable()),
            globalTheme));
        themed.Pump(new Size(360, 220));
        var themeTable = Assert.Single(FindDescendants<RenderTable>(themed.RenderView));
        Assert.Equal(64, themeTable.ResolvedRowHeights[0], 3);
        Assert.Equal(50, themeTable.ResolvedRowHeights[1], 3);

        using var explicitHarness = new WidgetRenderHarness(Wrap(new DataTable(
            columns: [new DataColumn(new Text("Name"))],
            rows: [new DataRow([new DataCell(new Text("Ada"))])],
            headingRowHeight: 70,
            dataRowHeight: 58), globalTheme));
        explicitHarness.Pump(new Size(360, 220));
        var explicitTable = Assert.Single(FindDescendants<RenderTable>(explicitHarness.RenderView));
        Assert.Equal(70, explicitTable.ResolvedRowHeights[0], 3);
        Assert.Equal(58, explicitTable.ResolvedRowHeights[1], 3);
    }

    [Fact]
    public void DataTable_SortAndSelectAllCallbacksFollowSourceRules()
    {
        int? sortedColumn = null;
        bool? sortedAscending = null;
        var selected = new List<bool?>();
        var column = new DataColumn(
            new Text("Name"),
            onSort: (index, ascending) => { sortedColumn = index; sortedAscending = ascending; });
        var table = new DataTable(
            columns: [column],
            rows:
            [
                new DataRow([new DataCell(new Text("Ada"))], selected: true, onSelectChanged: value => selected.Add(value)),
                new DataRow([new DataCell(new Text("Grace"))], onSelectChanged: value => selected.Add(value)),
            ],
            sortColumnIndex: 0,
            sortAscending: true);
        using var harness = new WidgetRenderHarness(Wrap(table));
        var semantics = harness.PumpAndGetSemantics(new Size(360, 220));

        var tappable = FlattenSemantics(semantics)
            .Where(node => node.Actions.HasFlag(SemanticsActions.Tap))
            .ToArray();
        Assert.True(tappable.Length >= 4);

        // The sortable heading is the first enabled non-checkbox action in source order.
        var sortAction = tappable.First(node =>
            FlattenSemantics(node).Any(descendant => descendant.Label == "Name"));
        Assert.True(sortAction.PerformAction(SemanticsActions.Tap));
        Assert.Equal(0, sortedColumn);
        Assert.False(sortedAscending);

        var headerCheckbox = tappable[0];
        Assert.True(headerCheckbox.PerformAction(SemanticsActions.Tap));
        Assert.Equal([true], selected);
    }

    [Fact]
    public void PaginatedDataTable_ValidatesDefaultsAndPagesThroughLongLivedSource()
    {
        var source = new TestSource(12);
        Assert.Throws<ArgumentException>(() => new PaginatedDataTable([], source));
        Assert.Throws<ArgumentException>(() => new PaginatedDataTable(
            [new DataColumn(new Text("Name"))], source, actions: [new SizedBox()]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaginatedDataTable(
            [new DataColumn(new Text("Name"))], source, rowsPerPage: 0));
        Assert.Throws<ArgumentException>(() => new PaginatedDataTable(
            [new DataColumn(new Text("Name"))],
            source,
            rowsPerPage: 5,
            availableRowsPerPage: [10],
            onRowsPerPageChanged: _ => { }));

        var pageChanges = new List<int>();
        using var harness = new WidgetRenderHarness(Wrap(new PaginatedDataTable(
            columns: [new DataColumn(new Text("Name"))],
            source: source,
            header: new Text("People"),
            rowsPerPage: 5,
            availableRowsPerPage: [5, 10],
            showFirstLastButtons: true,
            onPageChanged: pageChanges.Add)));
        harness.Pump(new Size(640, 620));
        Assert.NotNull(FindParagraph(harness.RenderView, "Row 0"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Row 4"));
        Assert.Null(FindParagraph(harness.RenderView, "Row 5"));
        Assert.Equal(5, source.GetRowCalls);

        var state = Assert.IsType<PaginatedDataTableState>(harness.FindState<PaginatedDataTableState>());
        state.PageTo(5);
        harness.Pump(new Size(640, 620));
        Assert.Equal([5], pageChanges);
        Assert.NotNull(FindParagraph(harness.RenderView, "Row 5"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Row 9"));
        Assert.Null(FindParagraph(harness.RenderView, "Row 0"));
    }

    [Fact]
    public void PaginatedDataTable_RefreshesCacheAndSelectionHeaderWhenSourceNotifies()
    {
        var source = new TestSource(3);
        using var harness = new WidgetRenderHarness(Wrap(new PaginatedDataTable(
            columns: [new DataColumn(new Text("Name"))],
            source: source,
            header: new Text("People"),
            rowsPerPage: 3,
            availableRowsPerPage: [3])));
        harness.Pump(new Size(500, 420));
        Assert.NotNull(FindParagraph(harness.RenderView, "People"));
        int firstCalls = source.GetRowCalls;

        source.Selected = 2;
        source.Notify();
        harness.Pump(new Size(500, 420));
        Assert.NotNull(FindParagraph(harness.RenderView, "2 items selected"));
        Assert.True(source.GetRowCalls >= firstCalls + 3);
    }

    [Fact]
    public void DataTableDemoPage_PumpsFrameworkWidgetHostComposition()
    {
        using var harness = new WidgetRenderHarness(Wrap(new DataTableDemoPage()));
        harness.Pump(new Size(900, 900));
        Assert.NotNull(FindParagraph(harness.RenderView, "DataTable + PaginatedDataTable"));
        var tables = FindDescendants<RenderTable>(harness.RenderView).ToArray();
        Assert.True(tables.Length >= 2);
        Assert.All(tables, table =>
        {
            Assert.True(double.IsFinite(table.Size.Width));
            Assert.True(double.IsFinite(table.Size.Height));
            Assert.True(table.Size.Height > 0);
        });
        var viewports = FindDescendants<RenderSingleChildViewport>(harness.RenderView).ToArray();
        Assert.NotEmpty(viewports);
        Assert.All(viewports, viewport =>
        {
            Assert.True(double.IsFinite(viewport.Size.Width));
            Assert.True(double.IsFinite(viewport.Size.Height));
            Assert.True(viewport.Size.Height > 0);
        });
    }

    private static DataTable SimpleTable() => new(
        columns: [new DataColumn(new Text("Name"))],
        rows: [new DataRow([new DataCell(new Text("Ada"))])]);

    private static Widget Wrap(Widget child, ThemeData? theme = null) => new Directionality(
        TextDirection.Ltr,
        new MaterialLocalizationsScope(
            DefaultMaterialLocalizations.Instance,
            new Theme(theme ?? ThemeData.Light, child)));

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

    private static IEnumerable<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null) yield break;
        if (root is T typed) yield return typed;
        var children = new List<RenderObject>();
        root.VisitChildren(children.Add);
        foreach (var child in children)
        foreach (var descendant in FindDescendants<T>(child))
            yield return descendant;
    }

    private static IEnumerable<SemanticsNode> FlattenSemantics(SemanticsNode? root)
    {
        if (root is null) yield break;
        yield return root;
        foreach (var child in root.Children)
        foreach (var descendant in FlattenSemantics(child))
            yield return descendant;
    }

    private sealed class TestSource(int count) : DataTableSource
    {
        public int GetRowCalls { get; private set; }
        public int Selected { get; set; }
        public override int RowCount => count;
        public override bool IsRowCountApproximate => false;
        public override int SelectedRowCount => Selected;
        public override DataRow? GetRow(int index)
        {
            GetRowCalls++;
            return index >= count
                ? null
                : DataRow.ByIndex([new DataCell(new Text($"Row {index}"))], index);
        }
        public void Notify() => NotifyListeners();
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }
        public T? FindState<T>() where T : State => FindState<T>(_rootElement);
        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }
        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }
        public void Dispose() => _rootElement.Unmount();

        private static T? FindState<T>(Element element) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T match) return match;
            T? result = null;
            element.VisitChildren(child => result ??= FindState<T>(child));
            return result;
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;
            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
