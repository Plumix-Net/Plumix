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

        var unbounded = new DataTable(
            [column],
            [],
            dataRowMinHeight: 70.0,
            dataRowMaxHeight: double.PositiveInfinity);
        Assert.Equal(double.PositiveInfinity, unbounded.DataRowMaxHeight);
    }

    [Fact]
    public void DataTableThemeData_CopyWithAndLerpMatchFlutterContracts()
    {
        var decoration = new ShapeDecoration(
            new StadiumBorder(),
            Color: Colors.AliceBlue);
        var original = new DataTableThemeData(
            decoration: decoration,
            dataRowMinHeight: 40.0,
            dataRowMaxHeight: 50.0,
            headingRowAlignment: MainAxisAlignment.Start);

        DataTableThemeData copied = original.CopyWith(
            dataRowHeight: 44.0,
            headingRowAlignment: MainAxisAlignment.Center);

        Assert.Same(decoration, copied.Decoration);
        Assert.Equal(44.0, copied.DataRowHeight);
        Assert.Equal(MainAxisAlignment.Center, copied.HeadingRowAlignment);
        Assert.Same(original, DataTableThemeData.Lerp(original, original, 0.5));
        Assert.Throws<ArgumentException>(() => original.CopyWith(
            dataRowHeight: 44.0,
            dataRowMinHeight: 40.0));
    }

    [Fact]
    public void DataTable_SelectedRowsAndDividersUseDirectM2M3ThemeRoles()
    {
        Color primary = Colors.OrangeRed;
        Color outlineVariant = Colors.DodgerBlue;
        Color legacyDivider = Colors.ForestGreen;
        ThemeData m3Theme = ThemeData.Light with
        {
            UseMaterial3 = true,
            ColorScheme = ThemeData.Light.ColorScheme with
            {
                Primary = primary,
                OutlineVariant = outlineVariant,
            },
            DividerColor = legacyDivider,
        };
        using var m3 = new WidgetRenderHarness(Wrap(SelectedTable(), m3Theme));
        m3.Pump(new Size(360.0, 180.0));

        RenderTable m3Table = Assert.Single(FindDescendants<RenderTable>(m3.RenderView));
        BoxDecoration m3Row = Assert.IsType<BoxDecoration>(m3Table.RowDecorations![1]);
        Assert.Equal(Color.FromArgb(20, primary.R, primary.G, primary.B), m3Row.Color);
        var m3Border = Assert.IsType<Plumix.Rendering.Border>(m3Row.Border);
        Assert.Equal(outlineVariant, m3Border.Top.Color);
        Assert.Equal(1.0, m3Border.Top.Width);

        ThemeData m2Theme = m3Theme with { UseMaterial3 = false };
        using var m2 = new WidgetRenderHarness(Wrap(SelectedTable(), m2Theme));
        m2.Pump(new Size(360.0, 180.0));

        RenderTable m2Table = Assert.Single(FindDescendants<RenderTable>(m2.RenderView));
        BoxDecoration m2Row = Assert.IsType<BoxDecoration>(m2Table.RowDecorations![1]);
        var m2Border = Assert.IsType<Plumix.Rendering.Border>(m2Row.Border);
        Assert.Equal(legacyDivider, m2Border.Top.Color);
    }

    [Fact]
    public void DataTable_LocalThemeFallsBackToGlobalThemePerProperty()
    {
        ThemeData global = ThemeData.Light with
        {
            DataTableTheme = new DataTableThemeData(
                headingRowHeight: 63.0,
                dataRowMinHeight: 51.0,
                dataRowMaxHeight: 51.0,
                horizontalMargin: 17.0,
                columnSpacing: 29.0),
        };
        var local = new DataTableThemeData(
            headingRowColor: MaterialStateProperty<Color?>.All(Colors.Gold));
        using var harness = new WidgetRenderHarness(Wrap(
            new DataTableTheme(local, SimpleTable()),
            global));

        harness.Pump(new Size(360.0, 220.0));

        RenderTable table = Assert.Single(FindDescendants<RenderTable>(harness.RenderView));
        Assert.Equal([63.0, 51.0], table.ResolvedRowHeights.Select(value => Math.Round(value, 3)).ToArray());
        BoxDecoration heading = Assert.IsType<BoxDecoration>(table.RowDecorations![0]);
        Assert.Equal(Colors.Gold, heading.Color);
    }

    [Fact]
    public void DataTable_RowColorsResolveSelectedAndDisabledStates()
    {
        Color selected = Colors.ForestGreen;
        Color disabled = Colors.OrangeRed;
        MaterialStateProperty<Color?> rowColor = MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return disabled;
            }
            return states.HasFlag(MaterialState.Selected) ? selected : null;
        });
        using var harness = new WidgetRenderHarness(Wrap(new DataTable(
            columns: [new DataColumn(new Text("Name"))],
            rows:
            [
                new DataRow(
                    [new DataCell(new Text("Selected"))],
                    selected: true,
                    onSelectChanged: _ => { }),
                new DataRow([new DataCell(new Text("Disabled"))]),
            ],
            dataRowColor: rowColor)));

        harness.Pump(new Size(360.0, 220.0));

        RenderTable table = Assert.Single(FindDescendants<RenderTable>(harness.RenderView));
        BoxDecoration selectedRow = Assert.IsType<BoxDecoration>(table.RowDecorations![1]);
        BoxDecoration disabledRow = Assert.IsType<BoxDecoration>(table.RowDecorations[2]);
        Assert.Equal(selected, selectedRow.Color);
        Assert.Equal(disabled, disabledRow.Color);
    }

    [Fact]
    public void DataTable_ComposesColumnHeaderSemanticsAndTransparentClippedMaterial()
    {
        BorderRadius radius = BorderRadius.Circular(12.0);
        var decoration = new ShapeDecoration(
            new RoundedRectangleBorder(borderRadius: radius),
            Color: Colors.AliceBlue);
        var border = TableBorder.All(borderRadius: radius);
        using var harness = new WidgetRenderHarness(Wrap(new DataTable(
            columns: [new DataColumn(new Text("Name")), new DataColumn(new Text("Score"))],
            rows: [new DataRow([new DataCell(new Text("Ada")), new DataCell(new Text("10"))])],
            decoration: decoration,
            border: border,
            clipBehavior: Clip.HardEdge)));

        SemanticsNode? semanticsRoot = harness.PumpAndGetSemantics(new Size(420.0, 180.0));
        SemanticsNode tableNode = Assert.Single(
            FlattenSemantics(semanticsRoot),
            node => node.Role == SemanticsRole.Table);
        SemanticsNode headingRow = tableNode.Children[0];
        Assert.Equal(SemanticsRole.Row, headingRow.Role);
        Assert.Equal(
            [SemanticsRole.ColumnHeader, SemanticsRole.ColumnHeader],
            headingRow.Children.Select(node => node.Role).ToArray());

        Plumix.Material.Material material = Assert.Single(
            harness.FindWidgets<Plumix.Material.Material>(),
            widget => widget.Type == MaterialType.Transparency);
        Assert.Equal(Clip.HardEdge, material.ClipBehavior);
        Assert.Equal(radius, material.BorderRadius);
        Assert.Contains(
            harness.FindWidgets<Container>(),
            container => Equals(container.Decoration, decoration));
    }

    [Fact]
    public void DataTable_TextStylesMergeWithAmbientDefaultTextStyle()
    {
        Color ambientColor = Colors.ForestGreen;
        Color headingColor = Colors.Coral;
        Color dataColor = Colors.DodgerBlue;
        Widget table = new DefaultTextStyle(
            new TextStyle(FontSize: 31.0, Color: ambientColor),
            new DataTable(
                columns: [new DataColumn(new Text("Heading"))],
                rows: [new DataRow([new DataCell(new Text("Value"))])],
                headingTextStyle: new TextStyle(Color: headingColor),
                dataTextStyle: new TextStyle(Color: dataColor)));
        using var harness = new WidgetRenderHarness(Wrap(table));

        harness.Pump(new Size(360.0, 180.0));

        RenderParagraph heading = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Heading"));
        RenderParagraph data = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Value"));
        Assert.Equal(31.0, heading.FontSize);
        Assert.Equal(31.0, data.FontSize);
        Assert.Equal(headingColor, Assert.IsType<SolidColorBrush>(heading.Foreground).Color);
        Assert.Equal(dataColor, Assert.IsType<SolidColorBrush>(data.Foreground).Color);
        Assert.Single(harness.FindWidgets<AnimatedDefaultTextStyle>());
    }

    [Fact]
    public void DataTable_SortArrowAnimatesAndIgnoresUnrelatedRebuilds()
    {
        using var harness = new WidgetRenderHarness(Wrap(new SortTableHost()));
        harness.Pump(new Size(360.0, 180.0));

        SortTableHostState state = Assert.IsType<SortTableHostState>(harness.FindState<SortTableHostState>());
        RenderTransform initial = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.Equal(1.0, initial.Transform[0], precision: 6);

        double now = Scheduler.CurrentSeconds;
        state.SetAscending(false);
        harness.Pump(new Size(360.0, 180.0));
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.075));
        harness.Pump(new Size(360.0, 180.0));
        RenderTransform halfway = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.InRange(halfway.Transform[0], -0.999, 0.999);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.200));
        harness.Pump(new Size(360.0, 180.0));
        RenderTransform reversed = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.Equal(-1.0, reversed.Transform[0], precision: 6);

        state.RebuildWithoutSortChange();
        harness.Pump(new Size(360.0, 180.0));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.400));
        harness.Pump(new Size(360.0, 180.0));
        RenderTransform unchanged = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.Equal(-1.0, unchanged.Transform[0], precision: 6);
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
    public void TableRowInkWell_ResolvesTheEntireNearestTableRowInLocalCoordinates()
    {
        var rowInkWell = new TableRowInkWell(
            onTap: () => { },
            child: new SizedBox(width: 100.0, height: 48.0));
        Assert.True(rowInkWell.ContainedInkWell);
        Assert.Equal(BoxShape.Rectangle, rowInkWell.HighlightShape);

        using var harness = new WidgetRenderHarness(Wrap(new Table(
            columnWidths: new Dictionary<int, TableColumnWidth>
            {
                [0] = new FixedColumnWidth(100.0),
                [1] = new FixedColumnWidth(100.0),
            },
            children:
            [
                new TableRow(
                [
                    new TableCell(new SizedBox(width: 100.0, height: 48.0)),
                    new TableCell(rowInkWell),
                ]),
            ])));
        harness.Pump(new Size(240.0, 100.0));

        RenderInkResponsePaint paint = Assert.Single(
            FindDescendants<RenderInkResponsePaint>(harness.RenderView));
        Assert.Equal(new Rect(-100.0, 0.0, 200.0, 48.0), paint.ResolvedInkRect);

        var feature = new Plumix.Material.InkSplash(new InkFeatureConfiguration(
            Position: new Point(20.0, 24.0),
            Color: Colors.Blue,
            ContainedInkWell: true));
        InkFeatureFrame frame = feature.ResolveFrame(
            paint.ResolvedInkRect,
            progress: 1.0,
            confirmed: false,
            canceled: false);
        Assert.Equal(new Point(20.0, 24.0), frame.Center);
        Assert.Equal(Math.Sqrt((120.0 * 120.0) + (24.0 * 24.0)), frame.Radius, 3);
    }

    [Fact]
    public void DataTable_UsesRowInkUnlessTheCellOwnsItsGesture()
    {
        using var harness = new WidgetRenderHarness(Wrap(new DataTable(
            columns:
            [
                new DataColumn(new Text("Name")),
                new DataColumn(new Text("Score")),
            ],
            rows:
            [
                new DataRow(
                    cells:
                    [
                        new DataCell(new Text("Ada"), onTap: () => { }),
                        new DataCell(new Text("10")),
                    ],
                    onSelectChanged: _ => { }),
            ],
            showCheckboxColumn: false)));
        harness.Pump(new Size(320.0, 140.0));

        RenderTable table = Assert.Single(FindDescendants<RenderTable>(harness.RenderView));
        RenderInkResponsePaint[] paints = FindDescendants<RenderInkResponsePaint>(harness.RenderView).ToArray();
        Assert.Equal(4, paints.Length);
        Assert.Contains(paints, paint => Math.Abs(paint.ResolvedInkRect.Width - paint.Size.Width) < 0.001);
        RenderInkResponsePaint rowPaint = Assert.Single(
            paints,
            paint => paint.ResolvedInkRect.Width > paint.Size.Width + 0.001);
        Assert.Equal(table.Size.Width, rowPaint.ResolvedInkRect.Width, 3);
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

    private static DataTable SelectedTable() => new(
        columns: [new DataColumn(new Text("Name"))],
        rows:
        [
            new DataRow(
                [new DataCell(new Text("Ada"))],
                selected: true,
                onSelectChanged: _ => { }),
        ]);

    private static Widget Wrap(Widget child, ThemeData? theme = null) => new Directionality(
        TextDirection.Ltr,
        new MaterialLocalizationsScope(
            DefaultMaterialLocalizations.Instance,
            new Theme(theme ?? ThemeData.Light, child)));

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

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

    private sealed class SortTableHost : StatefulWidget
    {
        public override State CreateState() => new SortTableHostState();
    }

    private sealed class SortTableHostState : State
    {
        private bool _ascending = true;
        private int _revision;

        public void SetAscending(bool value) => SetState(() => _ascending = value);

        public void RebuildWithoutSortChange() => SetState(() => _revision++);

        public override Widget Build(BuildContext context)
        {
            return new DataTable(
                columns:
                [
                    new DataColumn(
                        new Text($"Name {_revision}"),
                        onSort: (_, _) => { }),
                ],
                rows: [new DataRow([new DataCell(new Text("Ada"))])],
                sortColumnIndex: 0,
                sortAscending: _ascending);
        }
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
        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            VisitWidgets(_rootElement, widgets);
            return widgets;
        }
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
            return _pipeline.SemanticsOwner!.RootNode;
        }
        public void Dispose() => _rootElement.Unmount();

        private static T? FindState<T>(Element element) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T match) return match;
            T? result = null;
            element.VisitChildren(child => result ??= FindState<T>(child));
            return result;
        }

        private static void VisitWidgets<T>(Element element, List<T> widgets) where T : Widget
        {
            if (element.Widget is T widget)
            {
                widgets.Add(widget);
            }
            element.VisitChildren(child => VisitWidgets(child, widgets));
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
