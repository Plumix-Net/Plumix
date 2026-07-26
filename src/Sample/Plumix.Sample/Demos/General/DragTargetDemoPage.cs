using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/drag_target_demo_page.dart (exact sample parity)

public sealed class DragTargetDemoPage : StatefulWidget
{
    public override State CreateState() => new DragTargetDemoPageState();
}

internal sealed class DragTargetDemoPageState : State
{
    private OverlayEntry? _entry;

    public override void InitState()
    {
        base.InitState();
        _entry = new OverlayEntry(_ => new DragTargetDemoContent());
    }

    public override Widget Build(BuildContext context)
    {
        return new Overlay(initialEntries: [_entry!]);
    }

    public override void Dispose()
    {
        _entry!.Dispose();
        _entry = null;
        base.Dispose();
    }
}

internal sealed class DragTargetDemoContent : StatefulWidget
{
    public override State CreateState() => new DragTargetDemoContentState();
}

internal sealed class DragTargetDemoContentState : State
{
    private int _acceptedCount;
    private string _status = "Drag either item onto the target.";

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 16,
                children:
                [
                    new Text("Draggable + DragTarget", fontSize: 20, color: Colors.Black),
                    new Text(
                        "The plum is accepted; the stone exercises rejectedData and onLeave.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Wrap(
                        spacing: 16,
                        runSpacing: 16,
                        children:
                        [
                            BuildDraggable("plum", "#FF6750A4"),
                            BuildDraggable("stone", "#FF5F6368"),
                            BuildTarget(),
                        ]),
                    new Container(
                        color: Color.Parse("#FFF4F0FA"),
                        padding: new Thickness(12),
                        child: new Text(
                            $"accepted={_acceptedCount}; {_status}",
                            fontSize: 13,
                            color: Color.Parse("#FF332D41"))),
                    new Align(
                        alignment: Alignment.CenterLeft,
                        child: new TextButton(
                            onPressed: Reset,
                            child: new Text("Reset"))),
                ]));
    }

    private Widget BuildDraggable(string data, string colorHex)
    {
        Widget tile = BuildTile(data, colorHex, opacity: 1.0);
        return new Draggable<string>(
            data: data,
            child: tile,
            childWhenDragging: BuildTile(data, colorHex, opacity: 0.35),
            feedback: BuildTile(data, colorHex, opacity: 0.9),
            hitTestBehavior: HitTestBehavior.Opaque,
            onDragStarted: () => SetStatus($"dragging {data}"),
            onDragCompleted: () => SetStatus($"{data} accepted"),
            onDraggableCanceled: (_, _) => SetStatus($"{data} not accepted"));
    }

    private Widget BuildTarget()
    {
        return new DragTarget<string>(
            onWillAcceptWithDetails: details => details.Data == "plum",
            onAcceptWithDetails: details =>
            {
                SetState(() =>
                {
                    _acceptedCount += 1;
                    _status = $"{details.Data} dropped at "
                              + $"({details.Offset.X:0}, {details.Offset.Y:0})";
                });
            },
            onLeave: data => SetStatus($"{data ?? "item"} left target"),
            builder: (_, candidates, rejected) =>
            {
                string colorHex = candidates.Count > 0
                    ? "#FFD8F5D0"
                    : rejected.Count > 0
                        ? "#FFFFDAD6"
                        : "#FFE7E0EC";
                string label = candidates.Count > 0
                    ? "Release to accept"
                    : rejected.Count > 0
                        ? "Rejected"
                        : "Drop target";
                return new Container(
                    width: 190,
                    height: 96,
                    color: Color.Parse(colorHex),
                    alignment: Alignment.Center,
                    child: new Text(label, fontSize: 14, color: Colors.Black));
            });
    }

    private static Widget BuildTile(
        string label,
        string colorHex,
        double opacity)
    {
        return new Opacity(
            opacity,
            child: new Container(
                width: 96,
                height: 64,
                color: Color.Parse(colorHex),
                alignment: Alignment.Center,
                child: new Text(label, fontSize: 14, color: Colors.White)));
    }

    private void SetStatus(string status)
    {
        if (Mounted)
        {
            SetState(() => _status = status);
        }
    }

    private void Reset()
    {
        SetState(() =>
        {
            _acceptedCount = 0;
            _status = "Drag either item onto the target.";
        });
    }
}
