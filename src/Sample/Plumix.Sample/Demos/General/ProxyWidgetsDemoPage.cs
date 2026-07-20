using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;
using Path = Plumix.UI.Path;

// Dart parity source (reference): dart_sample/lib/proxy_widgets_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ProxyWidgetsDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new ProxyWidgetsDemoPageState();
    }
}

internal sealed class ProxyWidgetsDemoPageState : State
{
    private double _opacity = 0.8;
    private double _shiftX;
    private bool _tightClip = true;
    private double _fractionalShift;
    private int _quarterTurns;

    public override Widget Build(BuildContext context)
    {
        var clip = _tightClip
            ? new Rect(0, 0, 120, 80)
            : new Rect(0, 0, 190, 110);

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text(
                    "Proxy widgets: transforms + clips",
                    fontSize: 20,
                    color: Colors.Black),
                new Text(
                    "Use controls to fade a high-contrast black card over white canvas.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Opacity -", () => ChangeOpacity(-0.3), width: 96, colorHex: "#FFDCE3ED"),
                        BuildButton("Opacity +", () => ChangeOpacity(0.3), width: 96, colorHex: "#FFDCE3ED"),
                        BuildButton("Reset", Reset, width: 88, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Left", () => Move(-20), width: 88, colorHex: "#FFF3E8D8"),
                        BuildButton("Right", () => Move(20), width: 88, colorHex: "#FFF3E8D8"),
                        BuildButton(_tightClip ? "Clip: tight" : "Clip: wide", ToggleClip, width: 104, colorHex: "#FFE8EDF9"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Opacity 0", () => SetOpacity(0), width: 96, colorHex: "#FFF6E0E0"),
                        BuildButton("Opacity 1", () => SetOpacity(1), width: 96, colorHex: "#FFE0F0E7"),
                    ]),
                new Text(
                    $"opacity={_opacity:0.00}, shiftX={_shiftX:0}, clip={(_tightClip ? "tight" : "wide")}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 220,
                    height: 140,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(8),
                    child: new ClipRect(
                        clipRect: clip,
                        child: new Plumix.Widgets.Transform(
                            transform: Matrix.CreateTranslation(_shiftX, 10),
                            child: new Opacity(
                                opacity: _opacity,
                                child: new Container(
                                    width: 140,
                                    height: 90,
                                    color: Color.Parse("#FF111111"),
                                    padding: new Thickness(8),
                                    child: new Text("Layer", fontSize: 14, color: Colors.White)))))),
                new Text("FractionalTranslation + RotatedBox", fontSize: 14, color: Colors.Black),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Shift", CycleFractionalShift, width: 88, colorHex: "#FFE8EDF9"),
                        BuildButton("Rotate", RotateQuarterTurn, width: 88, colorHex: "#FFF3E8D8"),
                        new Text(
                            $"fraction={_fractionalShift:0.0}, turns={_quarterTurns}",
                            fontSize: 12,
                            color: Colors.DarkSlateGray),
                    ]),
                new Row(
                    spacing: 16,
                    children:
                    [
                        new Container(
                            width: 120,
                            height: 80,
                            color: Color.Parse("#FFE7EDF6"),
                            child: new Center(
                                new FractionalTranslation(
                                    translation: new Vector(_fractionalShift, 0),
                                    child: new Container(
                                        width: 56,
                                        height: 32,
                                        color: Color.Parse("#FF6750A4"),
                                        child: new Center(
                                            new Text("Shift", fontSize: 12, color: Colors.White)))))),
                        new Container(
                            width: 120,
                            height: 80,
                            color: Color.Parse("#FFE7EDF6"),
                            child: new Center(
                                new RotatedBox(
                                    quarterTurns: _quarterTurns,
                                    child: new Container(
                                        width: 64,
                                        height: 28,
                                        color: Color.Parse("#FF386A20"),
                                        child: new Center(
                                            new Text("Rotate", fontSize: 12, color: Colors.White)))))),
                    ]),
                new Text("ClipOval + ClipPath", fontSize: 14, color: Colors.Black),
                new Row(
                    spacing: 16,
                    children:
                    [
                        new SizedBox(
                            width: 96,
                            height: 72,
                            child: new ClipOval(
                                child: new ColoredBox(
                                    Color.Parse("#FF6750A4"),
                                    new Center(new Text("Oval", fontSize: 13, color: Colors.White))))),
                        new SizedBox(
                            width: 96,
                            height: 72,
                            child: new ClipPath(
                                clipper: new TrianglePathClipper(),
                                child: new ColoredBox(
                                    Color.Parse("#FF386A20"),
                                    new Center(new Text("Path", fontSize: 13, color: Colors.White))))),
                    ]),
            ]);
    }

    private Widget BuildButton(string label, Action onTap, double width, string colorHex)
    {
        return new SizedBox(
            width: width,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse(colorHex),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }

    private void ChangeOpacity(double delta)
    {
        SetState(() => _opacity = Math.Clamp(_opacity + delta, 0, 1));
    }

    private void SetOpacity(double value)
    {
        SetState(() => _opacity = Math.Clamp(value, 0, 1));
    }

    private void Move(double delta)
    {
        SetState(() => _shiftX = Math.Clamp(_shiftX + delta, -40, 80));
    }

    private void ToggleClip()
    {
        SetState(() => _tightClip = !_tightClip);
    }

    private void CycleFractionalShift()
    {
        SetState(() => _fractionalShift = _fractionalShift >= 0.5 ? -0.5 : _fractionalShift + 0.5);
    }

    private void RotateQuarterTurn()
    {
        SetState(() => _quarterTurns = (_quarterTurns + 1) % 4);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _opacity = 0.8;
            _shiftX = 0;
            _tightClip = true;
            _fractionalShift = 0;
            _quarterTurns = 0;
        });
    }

    private sealed class TrianglePathClipper : CustomClipper<Path>
    {
        public override Path GetClip(Size size)
        {
            var path = new Path();
            path.MoveTo(size.Width / 2.0, 0);
            path.LineTo(size.Width, size.Height);
            path.LineTo(0, size.Height);
            path.Close();
            return path;
        }

        public override bool ShouldReclip(CustomClipper<Path> oldClipper) => false;
    }
}
