using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: dart_sample/lib/demos/material/selection_handles_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class SelectionHandlesDemoPage : StatefulWidget
{
    public override State CreateState() => new SelectionHandlesDemoPageState();
}

internal sealed class SelectionHandlesDemoPageState : State
{
    private const double LineTop = 96;
    private const double LineHeight = 24;

    private readonly LayerLink _startHandleLink = new();
    private readonly LayerLink _endHandleLink = new();
    private readonly LayerLink _toolbarLink = new();
    private readonly TextEditingController _fieldController = new(
        "Long press this real text field, then drag either selection handle.");

    private SelectionOverlay? _overlay;
    private double _startX = 48;
    private double _endX = 232;
    private bool _handlesVisible;
    private bool _collapsed;

    public override void Dispose()
    {
        _overlay?.Dispose();
        _overlay = null;
        _fieldController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("SelectionOverlay + Material handles", fontSize: 20, color: Colors.Black),
                new Text(
                    "Drag either handle to move its endpoint. Collapsed mode keeps a single upward handle.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new TextField(
                    controller: _fieldController,
                    maxLines: 2,
                    decoration: new InputDecoration(
                        labelText: "RenderEditable-backed handles",
                        border: new OutlineInputBorder())),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ControlButton(_handlesVisible ? "Hide handles" : "Show handles", ToggleHandles),
                        ControlButton(_collapsed ? "Ranged" : "Collapsed", ToggleCollapsed),
                        ControlButton("Reset", ResetEndpoints),
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
                                    top: LineTop - LineHeight,
                                    right: 24,
                                    child: new Text(
                                        "Drag the handles across this line",
                                        fontSize: 18,
                                        color: Color.Parse("#FF1D192B"))),
                                new Positioned(
                                    left: EffectiveStartX,
                                    top: LineTop,
                                    child: new CompositedTransformTarget(
                                        link: _startHandleLink,
                                        child: new SizedBox(width: 0, height: 0))),
                                new Positioned(
                                    left: _endX,
                                    top: LineTop,
                                    child: new CompositedTransformTarget(
                                        link: _endHandleLink,
                                        child: new SizedBox(width: 0, height: 0))),
                                new Positioned(
                                    left: 24,
                                    top: LineTop + 24,
                                    child: new CompositedTransformTarget(
                                        link: _toolbarLink,
                                        child: new SizedBox(width: 0, height: 0))),
                                new Positioned(
                                    left: 24,
                                    bottom: 18,
                                    child: new Text(
                                        $"startX={EffectiveStartX:0}, endX={_endX:0}, "
                                        + $"handles={(_handlesVisible ? "on" : "off")}",
                                        fontSize: 12,
                                        color: Color.Parse("#FF6750A4"))),
                            ]))),
            ]);
    }

    private double EffectiveStartX => _collapsed ? _endX : _startX;

    private void ToggleHandles()
    {
        SelectionOverlay overlay = EnsureOverlay();
        if (_handlesVisible)
        {
            overlay.HideHandles();
        }
        else
        {
            overlay.ShowHandles();
        }

        SetState(() => _handlesVisible = !_handlesVisible);
    }

    private void ToggleCollapsed()
    {
        SetState(() => _collapsed = !_collapsed);
        SyncOverlay();
    }

    private void ResetEndpoints()
    {
        SetState(() =>
        {
            _startX = 48;
            _endX = 232;
        });
        SyncOverlay();
    }

    private SelectionOverlay EnsureOverlay()
    {
        if (_overlay is not null)
        {
            return _overlay;
        }

        _overlay = new SelectionOverlay(
            context: Context,
            startHandleType: TextSelectionHandleType.Left,
            lineHeightAtStart: LineHeight,
            endHandleType: TextSelectionHandleType.Right,
            lineHeightAtEnd: LineHeight,
            selectionEndpoints: BuildEndpoints(),
            selectionControls: MaterialTextSelectionHandleControls.Instance,
            selectionDelegate: null,
            clipboardStatus: null,
            startHandleLayerLink: _startHandleLink,
            endHandleLayerLink: _endHandleLink,
            toolbarLayerLink: _toolbarLink,
            onStartHandleDragUpdate: details => MoveStart(details.Delta.X),
            onEndHandleDragUpdate: details => MoveEnd(details.Delta.X));
        SyncOverlay();
        return _overlay;
    }

    private void MoveStart(double delta)
    {
        SetState(() => _startX = Math.Clamp(_startX + delta, 24, _endX));
        SyncOverlay();
    }

    private void MoveEnd(double delta)
    {
        SetState(() => _endX = Math.Clamp(_endX + delta, EffectiveStartX, 320));
        SyncOverlay();
    }

    private void SyncOverlay()
    {
        if (_overlay is null)
        {
            return;
        }

        _overlay.StartHandleType = _collapsed
            ? TextSelectionHandleType.Collapsed
            : TextSelectionHandleType.Left;
        _overlay.EndHandleType = _collapsed
            ? TextSelectionHandleType.Collapsed
            : TextSelectionHandleType.Right;
        _overlay.SelectionEndpoints = BuildEndpoints();
        _overlay.MarkNeedsBuild();
    }

    private TextSelectionPoint[] BuildEndpoints()
    {
        return
        [
            new TextSelectionPoint(new Point(EffectiveStartX, LineTop), TextDirection.Ltr),
            new TextSelectionPoint(new Point(_endX, LineTop), TextDirection.Ltr),
        ];
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
