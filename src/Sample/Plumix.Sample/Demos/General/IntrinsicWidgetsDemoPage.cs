using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/intrinsic_widgets_demo_page.dart (exact sample parity)

public sealed class IntrinsicWidgetsDemoPage : StatefulWidget
{
    public override State CreateState() => new IntrinsicWidgetsDemoPageState();
}

internal sealed class IntrinsicWidgetsDemoPageState : State
{
    private bool _snapWidth = true;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("IntrinsicWidth + IntrinsicHeight", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "IntrinsicWidth snaps the content width to an optional step. IntrinsicHeight gives the Row " +
                    "the tallest child's height before stretch layout.",
                    fontSize: 14.0,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8.0,
                    children:
                    [
                        BuildButton("stepWidth: 0", false),
                        BuildButton("stepWidth: 56", true),
                    ]),
                new Container(
                    height: 86.0,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(12.0),
                    child: new Align(
                        alignment: Alignment.CenterLeft,
                        child: new IntrinsicWidth(
                            stepWidth: _snapWidth ? 56.0 : 0.0,
                            child: new Container(
                                width: 70.0,
                                color: Color.Parse("#FFCCE3FF"),
                                padding: new Thickness(10.0, 8.0),
                                child: new Text(
                                    _snapWidth ? "70 → 112" : "70 px",
                                    fontSize: 13.0,
                                    color: Color.Parse("#FF1D3557")))))),
                new Text(
                    "All three tiles below receive the tallest tile's 64 px height.",
                    fontSize: 14.0,
                    color: Colors.DimGray),
                new Container(
                    color: Color.Parse("#FFF1F5F9"),
                    padding: new Thickness(12.0),
                    child: new IntrinsicHeight(
                        child: new Row(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 10.0,
                            children:
                            [
                                BuildTile("32", 32.0, "#FF457B9D"),
                                BuildTile("64", 64.0, "#FF2A9D8F"),
                                BuildTile("44", 44.0, "#FFE76F51"),
                            ]))),
            ]);
    }

    private Widget BuildButton(string label, bool enabled)
    {
        return new SizedBox(
            width: 128.0,
            child: new CounterTapButton(
                label: label,
                onTap: () => SetState(() => _snapWidth = enabled),
                background: _snapWidth == enabled
                    ? Color.Parse("#FF1D3557")
                    : Color.Parse("#FFDCE3ED"),
                foreground: _snapWidth == enabled ? Colors.White : Colors.Black,
                fontSize: 12.0,
                padding: new Thickness(10.0, 8.0)));
    }

    private static Widget BuildTile(string label, double height, string colorHex)
    {
        return new Container(
            width: 70.0,
            height: height,
            color: Color.Parse(colorHex),
            child: new Center(child: new Text(label, fontSize: 13.0, color: Colors.White)));
    }
}
