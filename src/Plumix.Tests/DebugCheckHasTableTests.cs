using Avalonia;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// - flutter/packages/flutter/test/widgets/debug_test.dart ('debugCheckHasTable control test')
// - material_ui/lib/src/data_table.dart (TableRowInkWell.debugCheckContext)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class DebugCheckHasTableTests : IDisposable
{
    public DebugCheckHasTableTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    [DebugOnlyFact]
    public void DebugCheckHasTable_ControlTest()
    {
        FlutterError? error = null;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Builder(context =>
        {
            error = Assert.Throws<FlutterError>(() => WidgetsDebug.DebugCheckHasTable(context));
            return new Container();
        }));

        Assert.NotNull(error);
        Assert.Equal(4, error!.Diagnostics.Count);
        Assert.IsType<DiagnosticsProperty<Element>>(error.Diagnostics[2]);
        Assert.Equal("No Table widget found.", error.Diagnostics[0].ToDescription());
        Assert.Equal("Builder widgets require a Table widget ancestor.", error.Diagnostics[1].ToDescription());
        Assert.Contains("The specific widget that could not find a Table ancestor was", error.Message);
        Assert.Contains("The ownership chain for the affected widget is: \"Builder ←", error.Message);
    }

    [Fact]
    public void DebugCheckHasTable_AcceptsTheTableAndItsDescendants()
    {
        var results = new List<bool>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Table(children:
            [
                new TableRow(children:
                [
                    new Builder(context =>
                    {
                        results.Add(WidgetsDebug.DebugCheckHasTable(context));
                        return new SizedBox(height: 10.0);
                    }),
                ]),
            ])));
        Element table = tester.Element(Find.ByType<Table>());

        Assert.Equal([true], results);
        Assert.True(WidgetsDebug.DebugCheckHasTable(table));
    }

    [DebugOnlyFact]
    public void TableRowInkWell_OutsideATable_ReportsTheMissingTable()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Theme(
                data: ThemeData.Light,
                child: new Plumix.Material.Material(
                    child: new TableRowInkWell(onTap: () => { }, child: new SizedBox(width: 10.0, height: 10.0))))));

        var error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal("No Table widget found.", error.Diagnostics[0].ToDescription());
        // Dart runs debugCheckContext from _InkResponseState.build, so the checked context is the
        // ink response's own state widget, not the TableRowInkWell.
        Assert.Equal(
            "InkResponseStateWidget widgets require a Table widget ancestor.",
            error.Diagnostics[1].ToDescription());
    }
}
