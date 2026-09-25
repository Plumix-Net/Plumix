using System;
using System.Collections.Generic;
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
                    color: new Color(0x8A000000)),
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
                        color: new Color(0xFFF7F2FA),
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
                                            color: new Color(0xFF1D192B)))),
                                new Positioned(
                                    left: _focusX - 50,
                                    top: 82,
                                    child: new RawMagnifier(
                                        size: new Size(100, 54),
                                        magnificationScale: 1.8,
                                        focalPointOffset: new Point(0, 74),
                                        decoration: new MagnifierDecoration(
                                            shape: new RoundedRectangleBorder(
                                                new BorderSide(new Color(0xFF006A6A), 2),
                                                Plumix.Rendering.BorderRadius.Circular(14)),
                                            shadows: BuildLensShadow()),
                                        clipBehavior: Clip.HardEdge,
                                        child: new ColoredBox(
                                            _showFilm
                                                ? Color.FromARGB(10, 0, 105, 105)
                                                : Colors.Transparent))),
                                new Positioned(
                                    left: _focusX - (MaterialMagnifier.DefaultMagnifierSize.Width / 2.0),
                                    top: 116,
                                    child: new MaterialMagnifier(
                                        filmColor: _showFilm
                                            ? Color.FromARGB(8, 158, 158, 158)
                                            : Colors.Transparent)),
                                new Positioned(
                                    left: 24,
                                    bottom: 18,
                                    child: new Text(
                                        $"focusX={_focusX:0}, raw scale=1.8, material scale=1.25",
                                        fontSize: 12,
                                        color: new Color(0xFF6750A4))),
                            ]))),
            ]);
    }

    private static Widget StripeRow()
    {
        var colors = new[]
        {
            new Color(0xFF6750A4),
            new Color(0xFFFFD8E4),
            new Color(0xFF006A6A),
            new Color(0xFFFFDDB3),
            new Color(0xFF386A20),
        };
        return new Row(
            children: colors
                .Select(color => (Widget)new Expanded(child: new ColoredBox(color)))
                .ToArray());
    }

    private static IReadOnlyList<Plumix.Rendering.BoxShadow> BuildLensShadow()
    {
        return
        [
            new Plumix.Rendering.BoxShadow(
                color: Color.FromARGB(25, 0, 0, 0),
                offset: new Point(0, 2),
                blurRadius: 1.5,
                spreadRadius: 0.75),
        ];
    }

    private static Widget ControlButton(string label, Action onPressed)
    {
        return new TextButton(
            onPressed: onPressed,
            child: new Text(label, fontSize: 12),
            style: TextButton.StyleFrom(
                foregroundColor: new Color(0xFF21005D),
                backgroundColor: new Color(0xFFEADDFF),
                minimumSize: new Size(64, 36)));
    }
}
