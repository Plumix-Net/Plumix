using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/gesture_recognizer_demo_page.dart

/// <summary>
/// Drives <see cref="HorizontalDragGestureRecognizer"/>, <see cref="LongPressGestureRecognizer"/> and
/// <see cref="ScaleGestureRecognizer"/> so the drag-start behavior, the accept threshold, the
/// per-button long-press callbacks and pinch/zoom/rotate are all visible.
/// </summary>
public sealed class GestureRecognizerDemoPage : StatefulWidget
{
    public override State CreateState() => new GestureRecognizerDemoPageState();
}

public sealed class GestureRecognizerDemoPageState : State
{
    private const int MaxLogLines = 8;

    private readonly List<string> _dragLog = [];
    private readonly List<string> _longPressLog = [];
    private readonly List<string> _scaleLog = [];
    private DragStartBehavior _dragStartBehavior = DragStartBehavior.Start;
    private bool _onlyAcceptDragOnThreshold;
    private bool _trackpadScrollCausesScale;
    private double _offset;
    private double _scale = 1.0;
    private double _baseScale = 1.0;
    private double _rotation;
    private double _baseRotation;
    private Point _translation;
    private Point _baseTranslation;
    private Point _startFocalPoint;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 16,
            children:
            [
                new Text("Drag, long-press and scale recognizers", fontSize: 20, color: Colors.Black),
                new Text(
                    "A drag accepts once the pointer travels past the device hit slop (18 logical pixels for "
                    + "touch, 1 for a mouse). DragStartBehavior decides whether that travelled distance is "
                    + "reported from the down position or from where the gesture won the arena.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildSwitchRow(
                    "DragStartBehavior.Down (replay the pending offset)",
                    _dragStartBehavior == DragStartBehavior.Down,
                    value => SetState(() =>
                        _dragStartBehavior = value ? DragStartBehavior.Down : DragStartBehavior.Start)),
                BuildSwitchRow(
                    "OnlyAcceptDragOnThreshold (hold the drag back after winning)",
                    _onlyAcceptDragOnThreshold,
                    value => SetState(() => _onlyAcceptDragOnThreshold = value)),
                BuildDragSurface(),
                BuildLog("Drag events", _dragLog),
                new Text(
                    "The long press deadline is 500 ms. Each mouse button dispatches its own callback set; "
                    + "moving more than the touch slop before the deadline cancels it.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildLongPressSurface(),
                BuildLog("Long-press events", _longPressLog),
                new Text(
                    "The scale recognizer tracks every pointer at once: two fingers pinch and rotate, one "
                    + "finger pans, and a trackpad pan/zoom gesture counts as two pointers. It wins the arena "
                    + "once the span moves past the scale slop, the focal point past the pan slop, or the "
                    + "pan/zoom scale differs by more than 5%.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildSwitchRow(
                    "TrackpadScrollCausesScale (a trackpad scroll zooms instead of panning)",
                    _trackpadScrollCausesScale,
                    value => SetState(() => _trackpadScrollCausesScale = value)),
                BuildScaleSurface(),
                BuildLog("Scale events", _scaleLog),
            ]);
    }

    private Widget BuildSwitchRow(string label, bool value, Action<bool> onChanged)
    {
        return new Row(
            spacing: 12,
            children:
            [
                new Switch(value, onChanged),
                new Expanded(child: new Text(label, fontSize: 14, color: Colors.Black)),
            ]);
    }

    private Widget BuildDragSurface()
    {
        var gestures = new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(HorizontalDragGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<HorizontalDragGestureRecognizer>(
                    () => new HorizontalDragGestureRecognizer(),
                    instance =>
                    {
                        instance.DragStartBehavior = _dragStartBehavior;
                        instance.OnlyAcceptDragOnThreshold = _onlyAcceptDragOnThreshold;
                        instance.OnDown = _ => Log(_dragLog, "down");
                        instance.OnStart = details => Log(
                            _dragLog,
                            $"start at {details.GlobalPosition.X:F0}");
                        instance.OnUpdate = details => SetState(() =>
                        {
                            _offset = Math.Clamp(_offset + (details.PrimaryDelta ?? 0.0), 0.0, 220.0);
                            AppendLog(_dragLog, $"update {details.PrimaryDelta ?? 0.0:F0}");
                        });
                        instance.OnEnd = details => Log(
                            _dragLog,
                            $"end at {details.PrimaryVelocity ?? 0.0:F0} px/s");
                        instance.OnCancel = () => Log(_dragLog, "cancel");
                    }),
        };

        return new RawGestureDetector(
            behavior: HitTestBehavior.Opaque,
            gestures: gestures,
            child: new Container(
                height: 96,
                color: Color.Parse("#FFF4F7FA"),
                padding: new Thickness(8),
                child: new Align(
                    alignment: Alignment.CenterLeft,
                    child: new Padding(
                        insets: new Thickness(_offset, 0, 0, 0),
                        child: new Container(
                            width: 56,
                            height: 56,
                            color: Color.Parse("#FF31506F"))))));
    }

    private Widget BuildLongPressSurface()
    {
        // The long-press matrix straight off `GestureDetector`: it registers a single
        // `LongPressGestureRecognizer` and wires all 21 primary/secondary/tertiary callbacks.
        return new GestureDetector(
            behavior: HitTestBehavior.Opaque,
            onLongPressDown: _ => Log(_longPressLog, "primary down"),
            onLongPressCancel: () => Log(_longPressLog, "primary cancel"),
            onLongPress: () => Log(_longPressLog, "primary long press"),
            onLongPressMoveUpdate: details => Log(
                _longPressLog,
                $"primary move {details.OffsetFromOrigin.X:F0}, {details.OffsetFromOrigin.Y:F0}"),
            onLongPressEnd: _ => Log(_longPressLog, "primary end"),
            onSecondaryLongPressDown: _ => Log(_longPressLog, "secondary down"),
            onSecondaryLongPress: () => Log(_longPressLog, "secondary long press"),
            onSecondaryLongPressEnd: _ => Log(_longPressLog, "secondary end"),
            onTertiaryLongPressDown: _ => Log(_longPressLog, "tertiary down"),
            onTertiaryLongPress: () => Log(_longPressLog, "tertiary long press"),
            onTertiaryLongPressEnd: _ => Log(_longPressLog, "tertiary end"),
            child: new Container(
                height: 96,
                color: Color.Parse("#FFE7EDF6"),
                child: new Center(
                    child: new Text(
                        "Press and hold with any mouse button",
                        fontSize: 14,
                        color: Color.Parse("#FF31506F")))));
    }

    private Widget BuildScaleSurface()
    {
        return new GestureDetector(
            behavior: HitTestBehavior.Opaque,
            trackpadScrollCausesScale: _trackpadScrollCausesScale,
            onScaleStart: details =>
            {
                _baseScale = _scale;
                _baseRotation = _rotation;
                _baseTranslation = _translation;
                _startFocalPoint = details.FocalPoint;
                Log(_scaleLog, $"start with {details.PointerCount} pointer(s)");
            },
            onScaleUpdate: details => SetState(() =>
            {
                _scale = Math.Clamp(_baseScale * details.Scale, 0.5, 4.0);
                _rotation = _baseRotation + details.Rotation;
                Point moved = details.FocalPoint - _startFocalPoint;
                _translation = new Point(
                    Math.Clamp(_baseTranslation.X + moved.X, -96.0, 96.0),
                    Math.Clamp(_baseTranslation.Y + moved.Y, -40.0, 40.0));
                AppendLog(
                    _scaleLog,
                    $"scale {details.Scale:F2}, rotation {details.Rotation:F2}, {details.PointerCount} pointer(s)");
            }),
            onScaleEnd: details => Log(_scaleLog, $"end at {details.ScaleVelocity:F0} px/s"),
            child: new Container(
                height: 176,
                color: Color.Parse("#FFF4F7FA"),
                child: new Center(
                    child: Widgets.Transform.Translate(
                        offset: _translation,
                        child: Widgets.Transform.Rotate(
                            angle: _rotation,
                            child: Widgets.Transform.Scale(
                                scale: _scale,
                                child: new Container(
                                    width: 72,
                                    height: 72,
                                    color: Color.Parse("#FF31506F"))))))));
    }

    private static Widget BuildLog(string title, List<string> lines)
    {
        var children = new List<Widget> { new Text(title, fontSize: 14, color: Colors.Black) };
        if (lines.Count == 0)
        {
            children.Add(new Text("(no events yet)", fontSize: 13, color: Colors.DimGray));
        }
        else
        {
            foreach (string line in lines)
            {
                children.Add(new Text(line, fontSize: 13, color: Colors.DimGray));
            }
        }

        return new Container(
            color: Color.Parse("#FFF7F7F7"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                spacing: 4,
                children: children));
    }

    private void Log(List<string> log, string message)
    {
        SetState(() => AppendLog(log, message));
    }

    private static void AppendLog(List<string> log, string message)
    {
        log.Add(message);
        if (log.Count > MaxLogLines)
        {
            log.RemoveAt(0);
        }
    }
}
