using System.Collections.Generic;
using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/scene_compositing_demo_page.dart (exact sample parity)

public sealed class SceneCompositingDemoPage : StatefulWidget
{
    public override State CreateState() => new SceneCompositingDemoPageState();
}

internal sealed class SceneCompositingDemoPageState : State
{
    private static readonly IReadOnlyList<Color> Tints =
        [new Color(0xFF9FC5E8), new Color(0xFFFFE082), new Color(0xFFB6D7A8)];

    private readonly LabeledGlobalKey<State> _boundaryKey = new("scene compositing boundary");
    private bool _showOverlay;
    private int _tint;
    private Bitmap? _capture;

    public override void Dispose()
    {
        _capture?.Dispose();
        base.Dispose();
    }

    private async void CaptureBoundary()
    {
        var boundary = (RenderRepaintBoundary)_boundaryKey.CurrentContext!.FindRenderObject()!;
        Bitmap image = await boundary.ToImage(pixelRatio: 2.0);
        if (!Mounted)
        {
            image.Dispose();
            return;
        }

        SetState(() =>
        {
            _capture?.Dispose();
            _capture = image;
        });
    }

    public override Widget Build(BuildContext context)
    {
        Widget page = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("dart:ui SceneBuilder + Scene", fontSize: 20, color: new Color(0xFF000000)),
                new Text(
                    "Every frame is built into a retained scene: clean layers are re-added with addRetained. "
                    + "RepaintBoundary.toImage rasterizes one boundary at 2x.",
                    fontSize: 14,
                    color: new Color(0xFF696969)),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildButton(
                            _showOverlay ? "performance overlay: on" : "performance overlay: off",
                            _showOverlay,
                            () => SetState(() => _showOverlay = !_showOverlay)),
                        BuildButton("recolor", false, () => SetState(() => _tint = (_tint + 1) % Tints.Count)),
                        BuildButton("capture toImage", false, CaptureBoundary),
                    ]),
                new Row(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    spacing: 16,
                    children:
                    [
                        new RepaintBoundary(
                            key: _boundaryKey,
                            child: new Container(
                                width: 160,
                                height: 96,
                                color: Tints[_tint],
                                alignment: Alignment.Center,
                                child: new Text($"boundary #{_tint}", fontSize: 16, color: new Color(0xFF000000)))),
                        _capture is { } capture
                            ? new Column(
                                crossAxisAlignment: CrossAxisAlignment.Start,
                                spacing: 4,
                                children:
                                [
                                    new RawImage(image: capture, width: 160, height: 96),
                                    new Text(
                                        $"{capture.PixelSize.Width} x {capture.PixelSize.Height} px",
                                        fontSize: 12,
                                        color: new Color(0xFF2F4F4F)),
                                ])
                            : new Text("no capture yet", fontSize: 12, color: new Color(0xFF2F4F4F)),
                    ]),
            ]);
        return new Stack(
            children:
            [
                page,
                .. _showOverlay
                    ? (Widget[])[new Positioned(top: 0, left: 0, right: 0, child: PerformanceOverlay.AllEnabled())]
                    : [],
            ]);
    }

    private static Widget BuildButton(string label, bool enabled, System.Action onTap)
    {
        return new CounterTapButton(
            label: label,
            onTap: onTap,
            background: enabled ? new Color(0xFF9FC5E8) : new Color(0xFFDCE3ED),
            foreground: new Color(0xFF000000),
            fontSize: 12,
            padding: new Thickness(10, 8));
    }
}
