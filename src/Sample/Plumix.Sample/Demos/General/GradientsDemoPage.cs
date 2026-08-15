using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;
using TileMode = Plumix.Rendering.TileMode;

// Dart parity source (reference): dart_sample/lib/demos/general/gradients_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class GradientsDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new GradientsDemoPageState();
    }
}

internal sealed class GradientsDemoPageState : State
{
    private static readonly TileMode[] TileModes =
    [
        TileMode.Clamp,
        TileMode.Repeated,
        TileMode.Mirror,
        TileMode.Decal,
    ];

    private double _rotation;
    private int _tileModeIndex;
    private bool _blended;

    private TileMode CurrentTileMode => TileModes[_tileModeIndex];

    private GradientTransform? CurrentTransform => _rotation == 0 ? null : new GradientRotation(_rotation);

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Gradients + BoxShadow lerp", fontSize: 20, color: Colors.Black),
                new Text(
                    "Linear, radial and sweep gradients share stops, tile modes and a rotation transform.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Rotate -", () => ChangeRotation(-Math.PI / 8), 96, "#FFDCE3ED"),
                        BuildButton("Rotate +", () => ChangeRotation(Math.PI / 8), 96, "#FFDCE3ED"),
                        BuildButton($"Tile: {CurrentTileMode}", CycleTileMode, 132, "#FFE9F5EC"),
                        BuildButton(_blended ? "Blend: B" : "Blend: A", ToggleBlend, 108, "#FFF3E8D8"),
                    ]),
                new Text(
                    $"rotation={_rotation:0.00} rad, tileMode={CurrentTileMode}, target={(_blended ? "B" : "A")}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Row(
                    spacing: 12,
                    children:
                    [
                        BuildSwatch("Linear", BuildLinearGradient()),
                        BuildSwatch("Radial", BuildRadialGradient()),
                        BuildSwatch("Sweep", BuildSweepGradient()),
                    ]),
                new Text(
                    "The card below animates its gradient colors and its shadow list at the same time.",
                    fontSize: 12,
                    color: Colors.DimGray),
                new Center(
                    child: new AnimatedContainer(
                        duration: TimeSpan.FromMilliseconds(450),
                        width: 240,
                        height: 96,
                        decoration: BuildAnimatedDecoration(),
                        child: new Center(
                            child: new Text("AnimatedContainer", fontSize: 14, color: Colors.White)))),
            ]);
    }

    private Widget BuildSwatch(string label, Gradient gradient)
    {
        return new Column(
            spacing: 6,
            children:
            [
                new SizedBox(
                    width: 96,
                    height: 96,
                    child: new DecoratedBox(decoration: new BoxDecoration(Gradient: gradient))),
                new Text(label, fontSize: 12, color: Colors.DarkSlateGray),
            ]);
    }

    private Gradient BuildLinearGradient()
    {
        return new LinearGradient(
            colors: [Color.Parse("#FF1D3557"), Color.Parse("#FF9DC4FF"), Color.Parse("#FFF3E8D8")],
            begin: Alignment.TopLeft,
            end: Alignment.BottomRight,
            stops: [0.0, 0.35, 0.7],
            tileMode: CurrentTileMode,
            transform: CurrentTransform);
    }

    private Gradient BuildRadialGradient()
    {
        return new RadialGradient(
            colors: [Color.Parse("#FFFFF1D0"), Color.Parse("#FFE76F51"), Color.Parse("#FF1D3557")],
            center: Alignment.Center,
            radius: 0.35,
            stops: [0.0, 0.55, 1.0],
            tileMode: CurrentTileMode,
            focal: Alignment.TopLeft,
            transform: CurrentTransform);
    }

    private Gradient BuildSweepGradient()
    {
        return new SweepGradient(
            colors: [Color.Parse("#FF2A9D8F"), Color.Parse("#FFE9C46A"), Color.Parse("#FF264653")],
            center: Alignment.Center,
            startAngle: 0.0,
            endAngle: Math.PI * 1.5,
            tileMode: CurrentTileMode,
            transform: CurrentTransform);
    }

    private BoxDecoration BuildAnimatedDecoration()
    {
        IReadOnlyList<BoxShadow> shadows = _blended
            ?
            [
                new BoxShadow(color: Color.FromArgb(90, 29, 53, 87), offset: new Point(0, 10), blurRadius: 18),
                new BoxShadow(color: Color.FromArgb(50, 29, 53, 87), offset: new Point(0, 2), blurRadius: 4),
            ]
            :
            [
                new BoxShadow(color: Color.FromArgb(40, 0, 0, 0), offset: new Point(0, 2), blurRadius: 6),
            ];

        return new BoxDecoration(
            Gradient: new LinearGradient(
                colors: _blended
                    ? [Color.Parse("#FF264653"), Color.Parse("#FF2A9D8F")]
                    : [Color.Parse("#FFE76F51"), Color.Parse("#FFF4A261")],
                begin: _blended ? Alignment.TopLeft : Alignment.BottomLeft,
                end: _blended ? Alignment.BottomRight : Alignment.TopRight),
            BorderRadius: BorderRadius.Circular(16),
            BoxShadows: shadows);
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

    private void ChangeRotation(double delta)
    {
        SetState(() => _rotation = Math.Clamp(_rotation + delta, -Math.PI, Math.PI));
    }

    private void CycleTileMode()
    {
        SetState(() => _tileModeIndex = (_tileModeIndex + 1) % TileModes.Length);
    }

    private void ToggleBlend()
    {
        SetState(() => _blended = !_blended);
    }
}
