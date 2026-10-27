using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/decorated_box_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class DecoratedBoxDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new DecoratedBoxDemoPageState();
    }
}

internal sealed class DecoratedBoxDemoPageState : State
{
    private double _radius = 10;
    private double _borderWidth = 2;
    private bool _accentFill = true;
    private BlurStyle _blurStyle = BlurStyle.Normal;
    private bool _multiply;
    private double _fade = 0.5;
    private bool _invert;
    private bool _outerProbe;
    private readonly MemoryImage _image = new(Convert.FromBase64String(ImageDemoPageState.DemoSamplePng));

    public override Widget Build(BuildContext context)
    {
        var fillColor = _accentFill ? new Color(0xFF9DC4FF) : new Color(0xFFF3F3F3);
        var borderColor = _accentFill ? new Color(0xFF1D3557) : new Color(0xFF555555);

        return new SingleChildScrollView(child: new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("DecoratedBox + Border + Radius", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                new Text(
                    "Adjust border width and corner radius; toggle fill theme, shadow blur style and blend mode.",
                    style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildButton("Radius -", () => ChangeRadius(-4), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("Radius +", () => ChangeRadius(4), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("Border -", () => ChangeBorder(-1), width: 88, colorHex: "#FFE9F5EC"),
                        BuildButton("Border +", () => ChangeBorder(1), width: 88, colorHex: "#FFE9F5EC"),
                    ]),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildButton(_accentFill ? "Fill: accent" : "Fill: neutral", ToggleFill, width: 128, colorHex: "#FFF3E8D8"),
                        BuildButton(
                            $"Shadow: {BlurStyleName(_blurStyle)}",
                            CycleBlurStyle,
                            width: 128,
                            colorHex: "#FFE6E0F5"),
                        BuildButton(
                            _multiply ? "Blend: multiply" : "Blend: none",
                            ToggleBlend,
                            width: 128,
                            colorHex: "#FFF5E0E6"),
                        BuildButton("Reset", Reset, width: 88, colorHex: "#FFE8EDF9"),
                    ]),
                new Text(
                    $"radius={_radius:0}, border={_borderWidth:0}, "
                    + $"fill={(_accentFill ? "accent" : "neutral")}, shadow={BlurStyleName(_blurStyle)}, "
                    + $"blend={(_multiply ? "multiply" : "none")}",
                    style: new TextStyle(FontSize: 12, Color: MaterialColors.BlueGrey)),
                new Container(
                    width: 220,
                    height: 140,
                    color: new Color(0xFFE7EDF6),
                    padding: EdgeInsets.All(8),
                    child: new Center(
                        child: new SizedBox(
                            width: 140,
                            height: 90,
                            child: new DecoratedBox(
                                decoration: new BoxDecoration(
                                    Color: fillColor,
                                    Border: Plumix.Rendering.Border.FromBorderSide(
                                        new BorderSide(borderColor, _borderWidth)),
                                    BorderRadius: BorderRadius.Circular(_radius),
                                    BoxShadows:
                                    [
                                        new Plumix.Rendering.BoxShadow(
                                            color: new Color(0x99000000),
                                            offset: new Point(0, 4),
                                            blurRadius: 12,
                                            spreadRadius: 1,
                                            blurStyle: _blurStyle),
                                    ],
                                    BackgroundBlendMode: _multiply ? BlendMode.Multiply : null),
                                child: new Center(
                                    child: new Text(
                                        "Decorated",
                                        style: new TextStyle(
                                            FontSize: 14,
                                            Color: new Color(0xFF14213D)))))))),
                new Text("Rounded corner hit testing", style: new TextStyle(FontSize: 16, Color: Colors.Black)),
                BuildButton(
                    _outerProbe ? "Point: outer" : "Point: inner",
                    () => SetState(() => _outerProbe = !_outerProbe),
                    width: 128,
                    colorHex: "#FFE0F0F5"),
                new Row(spacing: 16, children:
                [
                    BuildGeometryProbe(false),
                    BuildGeometryProbe(true),
                ]),
                new Text("DecorationImage cross-fade", style: new TextStyle(FontSize: 16, Color: Colors.Black)),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton($"Fade: {_fade * 100:0}%", CycleFade, width: 128, colorHex: "#FFE0F0F5"),
                        BuildButton(
                            _invert ? "Invert: on" : "Invert: off",
                            ToggleInvert,
                            width: 128,
                            colorHex: "#FFF0E8F5"),
                    ]),
                new Text(
                    $"fade={_fade:0.00}, invert={(_invert ? "on" : "off")}",
                    style: new TextStyle(FontSize: 12, Color: MaterialColors.BlueGrey)),
                new Container(
                    width: 220,
                    height: 140,
                    color: new Color(0xFFE7EDF6),
                    padding: EdgeInsets.All(8),
                    child: new Center(
                        child: new SizedBox(
                            width: 140,
                            height: 90,
                            child: new DecoratedBox(
                                decoration: new BoxDecoration(
                                    BorderRadius: BorderRadius.Circular(12),
                                    Image: DecorationImage.Lerp(
                                        new DecorationImage(_image, fit: BoxFit.Cover, invertColors: _invert),
                                        new DecorationImage(
                                            _image,
                                            repeat: ImageRepeat.Repeat,
                                            scale: 2,
                                            colorFilter: new ColorFilter.Mode(
                                                new Color(0xFF2A9D8F),
                                                BlendMode.Color)),
                                        _fade)))))),
            ]));
    }

    private Widget BuildGeometryProbe(bool continuous)
    {
        bool contains = GeometryProbePainter.Contains(continuous, _outerProbe);
        return new Column(children:
        [
            new Text($"{(continuous ? "Continuous" : "Circular")}: {(contains ? "inside" : "outside")}"),
            new CustomPaint(size: new Size(110, 110), painter: new GeometryProbePainter(continuous, _outerProbe)),
        ]);
    }

    private void CycleFade()
    {
        SetState(() => _fade = _fade >= 1.0 ? 0.0 : _fade + 0.25);
    }

    private void ToggleInvert()
    {
        SetState(() => _invert = !_invert);
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

    private void ChangeRadius(double delta)
    {
        SetState(() => _radius = Math.Clamp(_radius + delta, 0, 44));
    }

    private void ChangeBorder(double delta)
    {
        SetState(() => _borderWidth = Math.Clamp(_borderWidth + delta, 0, 12));
    }

    private void ToggleFill()
    {
        SetState(() => _accentFill = !_accentFill);
    }

    private void CycleBlurStyle()
    {
        SetState(() => _blurStyle = (BlurStyle)(((int)_blurStyle + 1) % Enum.GetValues<BlurStyle>().Length));
    }

    private void ToggleBlend()
    {
        SetState(() => _multiply = !_multiply);
    }

    private static string BlurStyleName(BlurStyle style) => style switch
    {
        BlurStyle.Solid => "solid",
        BlurStyle.Outer => "outer",
        BlurStyle.Inner => "inner",
        _ => "normal",
    };

    private void Reset()
    {
        SetState(() =>
        {
            _radius = 10;
            _borderWidth = 2;
            _accentFill = true;
            _blurStyle = BlurStyle.Normal;
            _multiply = false;
            _fade = 0.5;
            _invert = false;
            _outerProbe = false;
        });
    }
}

internal sealed class GeometryProbePainter(bool continuous, bool outer) : CustomPainter
{
    private static Point Probe(bool outer) => outer ? new Point(95.74, 99.89) : new Point(95.72, 99.87);

    internal static bool Contains(bool continuous, bool outer) => continuous
        ? Plumix.UI.RSuperellipse.FromLTRBXY(0, 0, 100, 100, 5, 5).Contains(Probe(outer))
        : Plumix.UI.RRect.FromLTRBXY(0, 0, 100, 100, 5, 5).Contains(Probe(outer));

    public override void Paint(PaintingContext context, Size size)
    {
        var paint = new Plumix.UI.Paint { Color = new Color(0xFF9DC4FF) };
        if (continuous)
        {
            context.Canvas.DrawRSuperellipse(Plumix.UI.RSuperellipse.FromLTRBXY(5, 5, 105, 105, 5, 5), paint);
        }
        else
        {
            context.Canvas.DrawRRect(Plumix.UI.RRect.FromLTRBXY(5, 5, 105, 105, 5, 5), paint);
        }
        context.Canvas.DrawCircle(Probe(outer) + new Point(5, 5), 2,
            new Plumix.UI.Paint
            {
                Color = Contains(continuous, outer) ? new Color(0xFF1B5E20) : new Color(0xFFB71C1C),
            });
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) => true;
}
