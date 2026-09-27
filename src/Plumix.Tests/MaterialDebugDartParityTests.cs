// Dart parity source: material_ui/lib/src/debug.dart
// Mirrors material-ui-src/test/debug_test.dart.

using Plumix.Foundation;
using Plumix.Material;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDebugDartParityTests
{
    [DebugOnlyFact]
    public void DebugCheckHasMaterial_ReportsMissingAncestor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new Builder(context =>
        {
            MaterialDebug.DebugCheckHasMaterial(context);
            return new SizedBox();
        })));

        FlutterError error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal(5, error.Diagnostics.Count);
        Assert.IsType<ErrorHint>(error.Diagnostics[2]);
        Assert.IsType<DiagnosticsProperty<Element>>(error.Diagnostics[3]);
        Assert.IsType<DiagnosticsBlock>(error.Diagnostics[4]);
        Assert.StartsWith("No Material widget found.", error.Diagnostics[0].ToString());
    }

    [DebugOnlyFact]
    public void DebugCheckHasMaterialLocalizations_ReportsMissingAncestor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new Builder(context =>
        {
            MaterialDebug.DebugCheckHasMaterialLocalizations(context);
            return new SizedBox();
        })));

        FlutterError error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal(6, error.Diagnostics.Count);
        Assert.IsType<ErrorHint>(error.Diagnostics[3]);
        Assert.IsType<DiagnosticsProperty<Element>>(error.Diagnostics[4]);
        Assert.IsType<DiagnosticsBlock>(error.Diagnostics[5]);
        Assert.StartsWith("No MaterialLocalizations found.", error.Diagnostics[0].ToString());
    }

    [DebugOnlyFact]
    public void DebugCheckHasScaffold_ReportsMissingAncestorWithHintLast()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new Builder(context =>
        {
            MaterialDebug.DebugCheckHasScaffold(context);
            return new SizedBox();
        })));

        FlutterError error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal(5, error.Diagnostics.Count);
        Assert.IsType<DiagnosticsProperty<Element>>(error.Diagnostics[2]);
        Assert.IsType<DiagnosticsBlock>(error.Diagnostics[3]);
        Assert.IsType<ErrorHint>(error.Diagnostics[4]);
        Assert.StartsWith("No Scaffold widget found.", error.Diagnostics[0].ToString());
    }

    [DebugOnlyFact]
    public void DebugCheckHasScaffoldMessenger_ReportsMissingAncestorWithHintLast()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: new Builder(context =>
        {
            MaterialDebug.DebugCheckHasScaffoldMessenger(context);
            return new SizedBox();
        })));

        FlutterError error = Assert.IsType<FlutterError>(tester.TakeException());
        Assert.Equal(5, error.Diagnostics.Count);
        Assert.IsType<DiagnosticsProperty<Element>>(error.Diagnostics[2]);
        Assert.IsType<DiagnosticsBlock>(error.Diagnostics[3]);
        Assert.IsType<ErrorHint>(error.Diagnostics[4]);
        Assert.StartsWith("No ScaffoldMessenger widget found.", error.Diagnostics[0].ToString());
    }

    [DebugOnlyFact]
    public void ChecksReturnTrueWhenTheirRequiredAncestorsExist()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Localizations(
            locale: new Locale("en"),
            delegates: [DefaultWidgetsLocalizations.Delegate, DefaultMaterialLocalizations.Delegate],
            child: new ScaffoldMessenger(child: new Scaffold(body: new MaterialWidget(child: new Builder(context =>
            {
                Assert.True(MaterialDebug.DebugCheckHasMaterial(context));
                Assert.True(MaterialDebug.DebugCheckHasMaterialLocalizations(context));
                Assert.True(MaterialDebug.DebugCheckHasScaffold(context));
                Assert.True(MaterialDebug.DebugCheckHasScaffoldMessenger(context));
                return new SizedBox();
            }))))));

        Assert.Null(tester.TakeException());
        Element scaffold = Assert.Single(tester.AllElements(), element => element.Widget is Scaffold);
        Assert.True(MaterialDebug.DebugCheckHasScaffold(scaffold));
    }
}
