using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using MaterialMagnifier = Plumix.Material.Magnifier;

// Dart parity source: dart_sample/lib/demos/material/magnifier_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class MagnifierDemoPage : StatefulWidget
{
    public override State CreateState() => new MagnifierDemoPageState();
}

internal sealed class MagnifierDemoPageState : State
{
    private double _focusX = 180;
    private bool _showFilm = true;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("RawMagnifier + Material Magnifier", fontSize: 20, color: Colors.Black),
                new Text(
                    "Move both lenses across high-contrast text and stripes to compare source geometry and styling.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ControlButton("Focus left", () => SetState(() => _focusX = Math.Max(90, _focusX - 36))),
                        ControlButton("Focus right", () => SetState(() => _focusX = Math.Min(310, _focusX + 36))),
                        ControlButton(
                            _showFilm ? "Film on" : "Film off",
                            () => SetState(() => _showFilm = !_showFilm)),
                    ]),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF7F2FA"),
                        child: new Stack(
                            clipBehavior: Clip.None,
                            children:
                            [
                                new Positioned(
                                    left: 24,
                                    top: 28,
                                    right: 24,
                                    height: 48,
                                    child: StripeRow()),
                                new Positioned(
                                    left: 24,
                                    top: 168,
                                    right: 24,
                                    child: new Center(
                                        child: new Text(
                                            "MAGNIFY 0123456789",
                                            fontSize: 24,
                                            color: Color.Parse("#FF1D192B")))),
                                new Positioned(
                                    left: _focusX - 50,
                                    top: 82,
                                    child: new RawMagnifier(
                                        size: new Size(100, 54),
                                        magnificationScale: 1.8,
                                        focalPointOffset: new Point(0, 74),
                                        decoration: new MagnifierDecoration(
                                            shape: new RoundedRectangleBorder(
                                                new BorderSide(Color.Parse("#FF006A6A"), 2),
                                                Plumix.Rendering.BorderRadius.Circular(14)),
                                            shadows: BuildLensShadow()),
                                        clipBehavior: Clip.HardEdge,
                                        child: new ColoredBox(
                                            _showFilm
                                                ? Color.FromArgb(10, 0, 105, 105)
                                                : Colors.Transparent))),
                                new Positioned(
                                    left: _focusX - (MaterialMagnifier.DefaultMagnifierSize.Width / 2.0),
                                    top: 116,
                                    child: new MaterialMagnifier(
                                        filmColor: _showFilm
                                            ? Color.FromArgb(8, 158, 158, 158)
                                            : Colors.Transparent)),
                                new Positioned(
                                    left: 24,
                                    bottom: 18,
                                    child: new Text(
                                        $"focusX={_focusX:0}, raw scale=1.8, material scale=1.25",
                                        fontSize: 12,
                                        color: Color.Parse("#FF6750A4"))),
                            ]))),
            ]);
    }

    private static Widget StripeRow()
    {
        var colors = new[]
        {
            Color.Parse("#FF6750A4"),
            Color.Parse("#FFFFD8E4"),
            Color.Parse("#FF006A6A"),
            Color.Parse("#FFFFDDB3"),
            Color.Parse("#FF386A20"),
        };
        return new Row(
            children: colors
                .Select(color => (Widget)new Expanded(child: new ColoredBox(color)))
                .ToArray());
    }

    private static BoxShadows BuildLensShadow()
    {
        return new BoxShadows(new BoxShadow
        {
            Blur = 1.5,
            OffsetY = 2,
            Spread = 0.75,
            Color = Color.FromArgb(25, 0, 0, 0),
        });
    }

    private static Widget ControlButton(string label, Action onPressed)
    {
        return new TextButton(
            onPressed: onPressed,
            backgroundColor: Color.Parse("#FFEADDFF"),
            foregroundColor: Color.Parse("#FF21005D"),
            minHeight: 36,
            child: new Text(label, fontSize: 12));
    }
}
