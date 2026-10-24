using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: material_ui/test/list_tile_test.dart (ListTile background/ink visibility warning)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ListTileBackgroundCheckDartParityTests
{
    private static List<FlutterErrorDetails> PumpAndCollect(Widget wrapped)
    {
        var errors = new List<FlutterErrorDetails>();
        using var tester = new FrameworkDartTester();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = errors.Add;
        try
        {
            tester.PumpWidget(new MaterialApp(home: new Scaffold(body: new Center(child: wrapped))));
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        return errors;
    }

    private static ListTile Tile(string text) =>
        new(tileColor: MaterialColors.Red, title: new Text(text));

    [DebugOnlyFact]
    public void ContainerWithColor_ReportsTheHiddenBackground()
    {
        List<FlutterErrorDetails> errors = PumpAndCollect(
            new Container(color: MaterialColors.Amber, height: 200, child: Tile("ListTile")));

        string message = Assert.Single(errors).ToString();
        Assert.Contains("ListTile background color or ink splashes may be invisible", message);
        Assert.Contains("The ListTile is wrapped in a ColoredBox", message);
        Assert.Contains("ListTile:", message);
        Assert.Contains("ColoredBox:", message);
    }

    [DebugOnlyFact]
    public void ColoredBoxWithColor_ReportsTheHiddenBackground()
    {
        List<FlutterErrorDetails> errors = PumpAndCollect(
            new ColoredBox(color: MaterialColors.Amber, child: Tile("ListTile")));

        string message = Assert.Single(errors).ToString();
        Assert.Contains("ListTile background color or ink splashes may be invisible", message);
        Assert.Contains("The ListTile is wrapped in a ColoredBox", message);
    }

    [DebugOnlyFact]
    public void DecoratedBoxWithColor_ReportsTheHiddenBackground()
    {
        List<FlutterErrorDetails> errors = PumpAndCollect(new DecoratedBox(
            decoration: new BoxDecoration(Color: MaterialColors.Amber),
            child: Tile("ListTile")));

        string message = Assert.Single(errors).ToString();
        Assert.Contains("ListTile background color or ink splashes may be invisible", message);
        Assert.Contains("The ListTile is wrapped in a DecoratedBox", message);
        Assert.Contains("DecoratedBox:", message);
    }

    [Fact]
    public void TransparentParent_ReportsNothing()
    {
        List<FlutterErrorDetails> errors = PumpAndCollect(
            new ColoredBox(color: MaterialColors.Transparent, child: Tile("Visible ListTile")));

        Assert.Empty(errors);
    }
}
