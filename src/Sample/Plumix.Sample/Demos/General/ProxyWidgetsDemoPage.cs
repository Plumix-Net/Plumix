using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Path = Plumix.UI.Path;

// Dart parity source (reference): dart_sample/lib/demos/general/proxy_widgets_demo_page.dart (exact sample parity)

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
    private int _pathOperation;
    private int _pathStep = 1;

    public override Widget Build(BuildContext context)
    {
        var clip = _tightClip
            ? new Rect(0, 0, 120, 80)
            : new Rect(0, 0, 190, 110);

        return new SingleChildScrollView(child: new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text(
                    "Proxy widgets: transforms + clips",
                    style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                new Text(
                    "Use controls to fade a high-contrast black card over white canvas.",
                    style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
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
                    style: new TextStyle(FontSize: 12, Color: MaterialColors.BlueGrey)),
                new Container(
                    width: 220,
                    height: 140,
                    color: new Color(0xFFE7EDF6),
                    padding: EdgeInsets.All(8),
                    child: new ClipRect(
                        clipper: new FixedRectClipper(clip),
                        child: new Plumix.Widgets.Transform(
                            transform: Matrix4.TranslationValues(_shiftX, 10.0, 0.0),
                            child: new Opacity(
                                opacity: _opacity,
                                child: new Container(
                                    width: 140,
                                    height: 90,
                                    color: new Color(0xFF111111),
                                    padding: EdgeInsets.All(8),
                                    child: new Text(
                                        "Layer",
                                        style: new TextStyle(
                                            FontSize: 14,
                                            Color: Colors.White))))))),
                new Text("FractionalTranslation + RotatedBox", style: new TextStyle(FontSize: 14, Color: Colors.Black)),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Shift", CycleFractionalShift, width: 88, colorHex: "#FFE8EDF9"),
                        BuildButton("Rotate", RotateQuarterTurn, width: 88, colorHex: "#FFF3E8D8"),
                        new Text(
                            $"fraction={_fractionalShift:0.0}, turns={_quarterTurns}",
                            style: new TextStyle(FontSize: 12, Color: MaterialColors.BlueGrey)),
                    ]),
                new Row(
                    spacing: 16,
                    children:
                    [
                        new Container(
                            width: 120,
                            height: 80,
                            color: new Color(0xFFE7EDF6),
                            child: new Center(
                                new FractionalTranslation(
                                    translation: new Point(_fractionalShift, 0),
                                    child: new Container(
                                        width: 56,
                                        height: 32,
                                        color: new Color(0xFF6750A4),
                                        child: new Center(
                                            new Text(
                                                "Shift",
                                                style: new TextStyle(
                                                    FontSize: 12,
                                                    Color: Colors.White))))))),
                        new Container(
                            width: 120,
                            height: 80,
                            color: new Color(0xFFE7EDF6),
                            child: new Center(
                                new RotatedBox(
                                    quarterTurns: _quarterTurns,
                                    child: new Container(
                                        width: 64,
                                        height: 28,
                                        color: new Color(0xFF386A20),
                                        child: new Center(
                                            new Text(
                                                "Rotate",
                                                style: new TextStyle(
                                                    FontSize: 12,
                                                    Color: Colors.White))))))),
                    ]),
                new Text(
                    "ClipOval + ClipPath + PhysicalShape",
                    style: new TextStyle(
                        FontSize: 14,
                        Color: Colors.Black)),
                new Row(
                    spacing: 16,
                    children:
                    [
                        new SizedBox(
                            width: 96,
                            height: 72,
                            child: new ClipOval(
                                child: new ColoredBox(
                                    new Color(0xFF6750A4),
                                    child: new Center(new Text(
                                        "Oval",
                                        style: new TextStyle(
                                            FontSize: 13,
                                            Color: Colors.White)))))),
                        new SizedBox(
                            width: 96,
                            height: 72,
                                child: new ClipPath(
                                    clipper: new TrianglePathClipper(),
                                    child: new ColoredBox(
                                        new Color(0xFF386A20),
                                        child: new Center(new Text(
                                            "Path",
                                            style: new TextStyle(
                                                FontSize: 13,
                                                Color: Colors.White)))))),
                        new SizedBox(
                            width: 96,
                            height: 72,
                            child: new PhysicalShape(
                                clipper: new TrianglePathClipper(),
                                color: new Color(0xFFB3261E),
                                clipBehavior: Clip.AntiAlias,
                                elevation: 6,
                                shadowColor: Colors.Black,
                                child: new Center(
                                    new Text("Physical", style: new TextStyle(FontSize: 13, Color: Colors.White))))),
                    ]),
                new Text("ColoredBox edge anti-aliasing", style: new TextStyle(FontSize: 14, Color: Colors.Black)),
                new Row(
                    spacing: 24,
                    children:
                    [
                        BuildColoredBoxEdgeProbe("smooth", isAntiAlias: true),
                        BuildColoredBoxEdgeProbe("crisp", isAntiAlias: false),
                    ]),
                new Text("Path operations + measured contours", style: new TextStyle(FontSize: 14)),
                new Row(spacing: 8, children:
                [
                    BuildButton(((PathOperation)_pathOperation).ToString(),
                        () => SetState(() => _pathOperation = (_pathOperation + 1) % 5),
                        width: 144, colorHex: "#FFE8EDF9"),
                    BuildButton($"Trace {_pathStep}/4",
                        () => SetState(() => _pathStep = (_pathStep + 1) % 5),
                        width: 104, colorHex: "#FFE9F5EC"),
                ]),
                new SizedBox(width: 240, height: 120,
                    child: new CustomPaint(painter: new PathProbePainter((PathOperation)_pathOperation, _pathStep))),
            ]));
    }

    private static Widget BuildColoredBoxEdgeProbe(string label, bool isAntiAlias)
    {
        return new SizedBox(
            width: 88,
            height: 88,
            child: new Center(
                Plumix.Widgets.Transform.Rotate(
                    angle: 0.12,
                    child: new SizedBox(
                        width: 62,
                        height: 62,
                        child: new ColoredBox(
                            color: new Color(0xFF6750A4),
                            isAntiAlias: isAntiAlias,
                            child: new Center(
                                new Text(label, style: new TextStyle(FontSize: 12, Color: Colors.White))))))));
    }

    private Widget BuildButton(string label, Action onTap, double width, string colorHex)
    {
        return new SizedBox(
            width: width,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: (Color)Avalonia.Media.Color.Parse(colorHex),
                foreground: Colors.Black,
                fontSize: 12,
                padding: EdgeInsets.Symmetric(horizontal: 10, vertical: 8)));
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
            _pathOperation = 0;
            _pathStep = 1;
        });
    }

    private sealed class FixedRectClipper : CustomClipper<Rect>
    {
        private readonly Rect _rect;

        public FixedRectClipper(Rect rect)
        {
            _rect = rect;
        }

        public override Rect GetClip(Size size) => _rect;

        public override bool ShouldReclip(CustomClipper<Rect> oldClipper) =>
            oldClipper is not FixedRectClipper other || other._rect != _rect;
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

    private sealed class PathProbePainter(PathOperation operation, int step) : CustomPainter
    {
        public override void Paint(PaintingContext context, Size size)
        {
            var left = new Path();
            left.MoveTo(10, 90);
            left.CubicTo(10, -15, 140, -15, 140, 90);
            left.Close();
            var right = new Path();
            right.AddOval(new Rect(65, 15, 110, 90));
            Path result = Path.Combine(operation, left, right);
            // A boolean result can be extended and measured like any other path.
            result.AddRect(new Rect(195, 15, 20, 20));
            context.Canvas.DrawPath(result, new Paint { Color = new Color(0xFF6750A4) });
            PathMetric metric = result.ComputeMetrics().First();
            double distance = metric.Length * step / 4.0;
            context.Canvas.DrawPath(metric.ExtractPath(0, distance), new Paint
            {
                Color = new Color(0xFFFFB300), Style = PaintingStyle.Stroke, StrokeWidth = 3,
            });
            Tangent? tangent = metric.GetTangentForOffset(distance);
            if (tangent is not null)
            {
                context.Canvas.DrawCircle(tangent.Position, 4, new Paint { Color = new Color(0xFFB3261E) });
            }
        }

        public override bool ShouldRepaint(CustomPainter oldDelegate) =>
            oldDelegate is not PathProbePainter other || other.Operation != operation || other.Step != step;
        private PathOperation Operation => operation;
        private int Step => step;
    }
}
