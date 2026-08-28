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
/// Drives <see cref="HorizontalDragGestureRecognizer"/> and <see cref="LongPressGestureRecognizer"/>
/// through <see cref="RawGestureDetector"/> so the drag-start behavior, the accept threshold and the
/// per-button long-press callbacks are all visible.
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
    private DragStartBehavior _dragStartBehavior = DragStartBehavior.Start;
    private bool _onlyAcceptDragOnThreshold;
    private double _offset;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 16,
            children:
            [
                new Text("Drag and long-press recognizers", fontSize: 20, color: Colors.Black),
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
        var gestures = new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(LongPressGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                    () => new LongPressGestureRecognizer(),
                    instance =>
                    {
                        instance.OnLongPressDown = _ => Log(_longPressLog, "primary down");
                        instance.OnLongPressCancel = () => Log(_longPressLog, "primary cancel");
                        instance.OnLongPress = () => Log(_longPressLog, "primary long press");
                        instance.OnLongPressMoveUpdate = details => Log(
                            _longPressLog,
                            $"primary move {details.OffsetFromOrigin.X:F0}, {details.OffsetFromOrigin.Y:F0}");
                        instance.OnLongPressEnd = _ => Log(_longPressLog, "primary end");
                        instance.OnSecondaryLongPressDown = _ => Log(_longPressLog, "secondary down");
                        instance.OnSecondaryLongPress = () => Log(_longPressLog, "secondary long press");
                        instance.OnSecondaryLongPressEnd = _ => Log(_longPressLog, "secondary end");
                        instance.OnTertiaryLongPressDown = _ => Log(_longPressLog, "tertiary down");
                        instance.OnTertiaryLongPress = () => Log(_longPressLog, "tertiary long press");
                        instance.OnTertiaryLongPressEnd = _ => Log(_longPressLog, "tertiary end");
                    }),
        };

        return new RawGestureDetector(
            behavior: HitTestBehavior.Opaque,
            gestures: gestures,
            child: new Container(
                height: 96,
                color: Color.Parse("#FFE7EDF6"),
                child: new Center(
                    child: new Text(
                        "Press and hold with any mouse button",
                        fontSize: 14,
                        color: Color.Parse("#FF31506F")))));
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
