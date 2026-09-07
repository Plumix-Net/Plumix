using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/mouse_region_demo_page.dart (exact sample parity)

public sealed class MouseRegionDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new MouseRegionDemoPageState();
    }
}

internal sealed class MouseRegionDemoPageState : State
{
    private readonly List<string> _log = [];
    private bool _opaque = true;
    private Point? _hoverPosition;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("MouseRegion + MouseTracker", fontSize: 20, color: Colors.Black),
                new Text(
                    "Enter and exit come from the mouse tracker's per-device annotation diff, so "
                    + "they also fire when a region appears, moves or disappears under a still "
                    + "pointer. The cursor is the first non-deferring region in hit-test order.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildCursorRow(),
                BuildStackedRegions(),
                BuildToggle(),
                new Text(
                    _hoverPosition is null
                        ? "hover — outside"
                        : $"hover — {Format(_hoverPosition.Value.X)}, {Format(_hoverPosition.Value.Y)}",
                    fontSize: 13,
                    color: Colors.DimGray),
                new Text(
                    _log.Count == 0 ? "no enter/exit yet" : string.Join("  ", _log),
                    fontSize: 13,
                    color: Colors.DimGray),
            ]);
    }

    private Widget BuildCursorRow()
    {
        return new Row(
            spacing: 12,
            children:
            [
                BuildCursorSwatch("text", SystemMouseCursors.Text, Color.Parse("#FF00796B")),
                BuildCursorSwatch("click", SystemMouseCursors.Click, Color.Parse("#FF1A73E8")),
                BuildCursorSwatch("grab", SystemMouseCursors.Grab, Color.Parse("#FF8E24AA")),
                BuildCursorSwatch("forbidden", SystemMouseCursors.Forbidden, Color.Parse("#FFD93025")),
            ]);
    }

    private Widget BuildCursorSwatch(string label, MouseCursor cursor, Color color)
    {
        return new Expanded(
            child: new MouseRegion(
                cursor: cursor,
                onEnter: _ => Record($"enter {label}"),
                onExit: _ => Record($"exit {label}"),
                child: new Container(
                    height: 64,
                    alignment: Alignment.Center,
                    decoration: new BoxDecoration(
                        Color: color,
                        BorderRadius: BorderRadius.Circular(10)),
                    child: new Text(label, fontSize: 13, color: Colors.White))));
    }

    private Widget BuildStackedRegions()
    {
        return new Container(
            height: 180,
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFF1F3F4"),
                Border: Rendering.Border.FromBorderSide(new BorderSide(
                    color: Color.Parse("#FF9AA0A6"),
                    width: 1)),
                BorderRadius: BorderRadius.Circular(10)),
            child: new Stack(
                children:
                [
                    // The outer region always sees the pointer; the inner one sits on top of it and
                    // only lets it through when `opaque` is false.
                    Positioned.Fill(
                        child: new MouseRegion(
                            onEnter: _ => Record("enter outer"),
                            onExit: _ => Record("exit outer"),
                            onHover: @event => SetState(() => _hoverPosition = @event.LocalPosition),
                            child: new SizedBox())),
                    new Positioned(
                        left: 40,
                        top: 40,
                        width: 160,
                        height: 100,
                        child: new MouseRegion(
                            opaque: _opaque,
                            cursor: SystemMouseCursors.Precise,
                            onEnter: _ => Record("enter inner"),
                            onExit: _ => Record("exit inner"),
                            child: new Container(
                                alignment: Alignment.Center,
                                decoration: new BoxDecoration(
                                    Color: Color.Parse("#FFFFCC80"),
                                    BorderRadius: BorderRadius.Circular(8)),
                                child: new Text(
                                    _opaque ? "opaque: true" : "opaque: false",
                                    fontSize: 13,
                                    color: Colors.Black)))),
                ]));
    }

    private Widget BuildToggle()
    {
        return new MouseRegion(
            cursor: SystemMouseCursors.Click,
            child: new GestureDetector(
                onTap: () => SetState(() => _opaque = !_opaque),
                behavior: HitTestBehavior.Opaque,
                child: new Container(
                    height: 40,
                    alignment: Alignment.Center,
                    decoration: new BoxDecoration(
                        Color: Color.Parse("#FF3C4043"),
                        BorderRadius: BorderRadius.Circular(8)),
                    child: new Text(
                        "toggle the inner region's opaque flag",
                        fontSize: 13,
                        color: Colors.White))));
    }

    private void Record(string entry)
    {
        SetState(() =>
        {
            _log.Add(entry);
            if (_log.Count > 6)
            {
                _log.RemoveAt(0);
            }
        });
    }

    private static string Format(double value)
    {
        return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
