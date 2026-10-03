using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/interactive_viewer_demo_page.dart (exact sample parity)

public sealed class InteractiveViewerDemoPage : StatefulWidget
{
    public override State CreateState() => new InteractiveViewerDemoPageState();
}

internal sealed class InteractiveViewerDemoPageState : State
{
    private const int TileColumns = 6;
    private const int TileRows = 4;
    private const double TileSize = 100.0;
    private const int RowCount = 20;
    private const double RowHeight = 40.0;

    private static readonly PanAxis[] PanAxes = [PanAxis.Free, PanAxis.Aligned, PanAxis.Horizontal, PanAxis.Vertical];

    private static readonly Color[] TileColors =
    [
        new Color(0xFF1565C0),
        new Color(0xFF2E7D32),
        new Color(0xFFF57C00),
        new Color(0xFF6750A4),
    ];

    private readonly TransformationController _controller = new();
    private int _panAxisIndex;
    private bool _infiniteBoundary;
    private bool _builderMode;
    private string _lastEvent = "none";

    public override void InitState()
    {
        base.InitState();
        _controller.AddListener(HandleTransformation);
    }

    public override void Dispose()
    {
        _controller.RemoveListener(HandleTransformation);
        _controller.Dispose();
        base.Dispose();
    }

    private void HandleTransformation() => SetState(() => { });

    public override Widget Build(BuildContext context)
    {
        Matrix4 matrix = _controller.Value;
        Vector3 translation = matrix.GetTranslation();
        PanAxis panAxis = PanAxes[_panAxisIndex];

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("InteractiveViewer", fontSize: 20, color: Colors.Black),
                new Text(
                    "Drag to pan, pinch, scroll the mouse wheel or use a trackpad to zoom. Flings keep "
                    + "moving with inertia and slide along the boundary.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Reset", () => _controller.Value = Matrix4.Identity()),
                        BuildButton(
                            $"PanAxis: {PanAxisName(panAxis)}",
                            () => SetState(() => _panAxisIndex = (_panAxisIndex + 1) % PanAxes.Length)),
                        BuildButton(
                            _infiniteBoundary ? "Boundary: infinite" : "Boundary: 40",
                            () => SetState(() => _infiniteBoundary = !_infiniteBoundary)),
                        BuildButton(
                            _builderMode ? "Mode: builder" : "Mode: child",
                            () => SetState(() =>
                            {
                                _builderMode = !_builderMode;
                                _controller.Value = Matrix4.Identity();
                            })),
                    ]),
                new Text(
                    $"scale={matrix.GetMaxScaleOnAxis():0.00}, "
                    + $"translation=({translation.X:0.0}, {translation.Y:0.0}), last={_lastEvent}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Center(child: new Container(
                    width: 360,
                    height: 240,
                    color: new Color(0xFFF3F6FA),
                    child: _builderMode ? BuildBuilderViewer(panAxis) : BuildChildViewer(panAxis))),
            ]);
    }

    private Widget BuildChildViewer(PanAxis panAxis)
    {
        var rows = new List<Widget>();
        for (int row = 0; row < TileRows; row++)
        {
            var tiles = new List<Widget>();
            for (int column = 0; column < TileColumns; column++)
            {
                tiles.Add(new Container(
                    width: TileSize,
                    height: TileSize,
                    color: TileColors[(row + column) % TileColors.Length],
                    alignment: Alignment.Center,
                    child: new Text($"{row},{column}", fontSize: 14, color: Colors.White)));
            }

            rows.Add(new Row(mainAxisSize: MainAxisSize.Min, children: tiles));
        }

        return new InteractiveViewer(
            transformationController: _controller,
            constrained: false,
            panAxis: panAxis,
            boundaryMargin: _infiniteBoundary ? EdgeInsets.All(double.PositiveInfinity) : EdgeInsets.All(40.0),
            minScale: 0.5,
            maxScale: 4.0,
            onInteractionStart: _ => SetState(() => _lastEvent = "start"),
            onInteractionEnd: _ => SetState(() => _lastEvent = "end"),
            child: new Column(mainAxisSize: MainAxisSize.Min, children: rows));
    }

    private Widget BuildBuilderViewer(PanAxis panAxis)
    {
        return InteractiveViewer.Builder(
            transformationController: _controller,
            panAxis: panAxis,
            scaleEnabled: false,
            boundaryMargin: EdgeInsets.All(double.PositiveInfinity),
            onInteractionStart: _ => SetState(() => _lastEvent = "start"),
            onInteractionEnd: _ => SetState(() => _lastEvent = "end"),
            builder: (_, viewport) =>
            {
                double top = Math.Min(
                    viewport.Point0.Y,
                    Math.Min(viewport.Point1.Y, Math.Min(viewport.Point2.Y, viewport.Point3.Y)));
                double bottom = Math.Max(
                    viewport.Point0.Y,
                    Math.Max(viewport.Point1.Y, Math.Max(viewport.Point2.Y, viewport.Point3.Y)));
                var children = new List<Widget>();
                for (int i = 0; i < RowCount; i++)
                {
                    double rowTop = i * RowHeight;
                    double rowBottom = rowTop + RowHeight;
                    bool visible = rowBottom > top && rowTop < bottom;
                    children.Add(new Container(
                        width: 360,
                        height: RowHeight,
                        color: visible ? new Color(0xFF2E7D32) : new Color(0xFFC62828),
                        alignment: Alignment.Center,
                        child: new Text(
                            visible ? $"row {i} (built visible)" : $"row {i} (built hidden)",
                            fontSize: 12,
                            color: Colors.White)));
                }

                return new Column(mainAxisSize: MainAxisSize.Min, children: children);
            });
    }

    private static string PanAxisName(PanAxis panAxis) => panAxis switch
    {
        PanAxis.Free => "free",
        PanAxis.Aligned => "aligned",
        PanAxis.Horizontal => "horizontal",
        _ => "vertical",
    };

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 140,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: new Color(0xFFDCE3ED),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }
}
