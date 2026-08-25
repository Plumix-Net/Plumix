using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_magnifier_demo_page.dart
// (exact sample parity)

public sealed class CupertinoMagnifierDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoMagnifierDemoPageState();
}

internal sealed class CupertinoMagnifierDemoPageState : State
{
    private static readonly double[] Scales = [1.0, 1.5, 2.0];

    private readonly MagnifierController _controller = new();
    private readonly ValueNotifier<MagnifierInfo> _magnifierInfo = new(MagnifierInfo.Empty);

    private BuildContext? _panelContext;
    private double _magnificationScale = 1.5;
    private Point _lastGesturePosition;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino magnifier", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Drag over the stripes: the text magnifier follows the gesture, stays inside the "
                    + "10pt screen padding and resists downward drag.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8.0,
                    children: Scales
                        .Select(scale => ScaleButton(scale))
                        .ToArray()),
                new Expanded(
                    child: new Builder(panelContext =>
                    {
                        _panelContext = panelContext;
                        return BuildPanel();
                    })),
            ]);
    }

    public override void Dispose()
    {
        _ = _controller.Hide();
        _magnifierInfo.Dispose();
        base.Dispose();
    }

    private Widget BuildPanel()
    {
        return new GestureDetector(
            behavior: HitTestBehavior.Opaque,
            onPanStart: details => ShowMagnifier(details.GlobalPosition),
            onPanUpdate: details => UpdateMagnifier(details.GlobalPosition),
            onPanEnd: _ => HideMagnifier(),
            onPanCancel: HideMagnifier,
            child: new Container(
                color: Color.Parse("#FFF2F2F7"),
                child: new Stack(
                    clipBehavior: Clip.None,
                    children:
                    [
                        new Positioned(left: 16.0, top: 24.0, right: 16.0, height: 44.0, child: StripeRow()),
                        new Positioned(
                            left: 16.0,
                            top: 96.0,
                            right: 16.0,
                            child: new Center(
                                child: new Text(
                                    "MAGNIFY 0123456789",
                                    fontSize: 24.0,
                                    color: Color.Parse("#FF1C1C1E")))),
                        new Positioned(
                            left: 16.0,
                            top: 148.0,
                            child: new CupertinoMagnifier(magnificationScale: _magnificationScale)),
                        new Positioned(
                            left: 16.0,
                            bottom: 16.0,
                            child: new Text(
                                $"gesture=({_lastGesturePosition.X:0}, {_lastGesturePosition.Y:0}), "
                                + $"scale={_magnificationScale:0.0}",
                                fontSize: 12.0,
                                color: Color.Parse("#FF007AFF"))),
                    ])));
    }

    private Widget ScaleButton(double scale)
    {
        bool selected = Math.Abs(_magnificationScale - scale) < 0.001;
        return new CupertinoButton(
            color: selected ? CupertinoColors.ActiveBlue : CupertinoColors.SystemGrey5,
            padding: EdgeInsets.Symmetric(horizontal: 16.0, vertical: 8.0),
            onPressed: () => SetState(() => _magnificationScale = scale),
            child: new Text(
                $"x{scale:0.0}",
                fontSize: 13.0,
                color: selected ? Colors.White : Color.Parse("#FF1C1C1E")));
    }

    private static Widget StripeRow()
    {
        var colors = new[]
        {
            Color.Parse("#FF007AFF"),
            Color.Parse("#FFFFCC00"),
            Color.Parse("#FF34C759"),
            Color.Parse("#FFFF3B30"),
            Color.Parse("#FF5856D6"),
        };
        return new Row(
            children: colors
                .Select(color => (Widget)new Expanded(child: new ColoredBox(color)))
                .ToArray());
    }

    private void ShowMagnifier(Point globalPosition)
    {
        UpdateMagnifierInfo(globalPosition);
        if (_controller.OverlayEntry is not null || _panelContext is not { } panelContext)
        {
            return;
        }

        _ = _controller.Show(
            panelContext,
            _ => new CupertinoTextMagnifier(_controller, _magnifierInfo));
    }

    private void UpdateMagnifier(Point globalPosition)
    {
        UpdateMagnifierInfo(globalPosition);
    }

    private void HideMagnifier()
    {
        _ = _controller.Hide();
    }

    private void UpdateMagnifierInfo(Point globalPosition)
    {
        Rect lineBounds = CurrentLineBounds(globalPosition);
        SetState(() => _lastGesturePosition = globalPosition);
        _magnifierInfo.Value = new MagnifierInfo(
            GlobalGesturePosition: globalPosition,
            CaretRect: lineBounds,
            FieldBounds: PanelBounds() ?? lineBounds,
            CurrentLineBoundaries: lineBounds);
    }

    /// <summary>The stripe band the magnifier treats as the "line" the lens must stay level with.</summary>
    private Rect CurrentLineBounds(Point globalPosition)
    {
        Rect? panel = PanelBounds();
        if (panel is not { } bounds)
        {
            return new Rect(globalPosition.X, globalPosition.Y, 1.0, 1.0);
        }

        return new Rect(bounds.Left + 16.0, bounds.Top + 24.0, Math.Max(0.0, bounds.Width - 32.0), 44.0);
    }

    private Rect? PanelBounds()
    {
        if (_panelContext?.FindRenderObject() is not RenderBox box || !box.HasSize)
        {
            return null;
        }

        return new Rect(box.LocalToGlobal(default), box.Size);
    }
}
