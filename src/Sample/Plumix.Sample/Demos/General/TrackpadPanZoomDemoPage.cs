using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/trackpad_pan_zoom_demo_page.dart (exact sample parity)

public sealed class TrackpadPanZoomDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new TrackpadPanZoomDemoPageState();
    }
}

internal sealed class TrackpadPanZoomDemoPageState : State
{
    private Point _pan;
    private double _scale = 1.0;
    private double _rotation;
    private int _updates;
    private string _phase = "idle";

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Trackpad pan / zoom", fontSize: 20, color: Colors.Black),
                new Text(
                    "Pinch or rotate on a trackpad over the panel. The platform reports the gesture "
                    + "as PointerPanZoom events, which Listener surfaces directly.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildProbe(),
                new Text(
                    $"phase {_phase} — pan {Format(_pan.X)}, {Format(_pan.Y)} — "
                    + $"scale {Format(_scale)} — rotation {Format(_rotation)} rad — {_updates} updates",
                    fontSize: 13,
                    color: Colors.DimGray),
            ]);
    }

    private Widget BuildProbe()
    {
        return new Listener(
            behavior: HitTestBehavior.Opaque,
            onPointerPanZoomStart: _ => SetState(() =>
            {
                _phase = "active";
                _pan = default;
                _scale = 1.0;
                _rotation = 0.0;
                _updates = 0;
            }),
            onPointerPanZoomUpdate: @event => SetState(() =>
            {
                _pan = @event.Pan;
                _scale = @event.Scale;
                _rotation = @event.Rotation;
                _updates++;
            }),
            onPointerPanZoomEnd: _ => SetState(() => _phase = "idle"),
            child: new Container(
                height: 220,
                alignment: Alignment.Center,
                decoration: new BoxDecoration(
                    Color: Color.Parse("#FFF1F3F4"),
                    Border: Rendering.Border.FromBorderSide(new BorderSide(
                        color: Color.Parse("#FF9AA0A6"),
                        width: 1)),
                    BorderRadius: BorderRadius.Circular(10)),
                child: BuildTarget()));
    }

    private Widget BuildTarget()
    {
        Matrix4 transform = Matrix4.TranslationValues(_pan.X, _pan.Y, 0.0);
        transform = transform.Multiplied(Matrix4.RotationZ(_rotation));
        transform = transform.Multiplied(Matrix4.Diagonal3Values(_scale, _scale, 1.0));

        return new Widgets.Transform(
            transform: transform,
            alignment: Alignment.Center,
            child: new Container(
                width: 96,
                height: 96,
                alignment: Alignment.Center,
                decoration: new BoxDecoration(
                    Color: Color.Parse("#FF00796B"),
                    BorderRadius: BorderRadius.Circular(12)),
                child: new Text("pinch me", fontSize: 13, color: Colors.White)));
    }

    private static string Format(double value)
    {
        return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
