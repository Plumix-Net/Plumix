using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: dart_sample/lib/demos/material/badge_tooltip_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class BadgeTooltipDemoPage : StatefulWidget
{
    public override State CreateState() => new BadgeTooltipDemoPageState();
}

internal sealed class BadgeTooltipDemoPageState : State
{
    private int _count = 7;
    private bool _isLabelVisible = true;
    private bool _useThemeOverrides;
    private bool _tooltipsVisible = true;
    private bool _useRtl;

    public override Widget Build(BuildContext context)
    {
        var content = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 14,
            children:
            [
                new Text("Badge + Tooltip", fontSize: 20, color: Colors.Black),
                new Text(
                    "Badge geometry plus overlay tooltips with hover, edge-aware placement, and custom positioning.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ControlButton("Count +1", () => SetState(() => _count++)),
                        ControlButton(
                            _isLabelVisible ? "Label on" : "Label off",
                            () => SetState(() => _isLabelVisible = !_isLabelVisible)),
                        ControlButton(
                            _useThemeOverrides ? "Theme on" : "Theme off",
                            () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                        ControlButton(
                            _tooltipsVisible ? "Tooltips on" : "Tooltips off",
                            () => SetState(() => _tooltipsVisible = !_tooltipsVisible)),
                    ]),
                new Row(
                    children:
                    [
                        ControlButton(
                            _useRtl ? "Direction RTL" : "Direction LTR",
                            () => SetState(() => _useRtl = !_useRtl)),
                    ]),
                new Container(
                    color: Color.Parse("#FFF7F2FA"),
                    padding: new Thickness(20),
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.SpaceAround,
                        children:
                        [
                            Probe("Count", Badge.Count(
                                count: _count,
                                maxCount: 99,
                                isLabelVisible: _isLabelVisible,
                                child: new Icon(Icons.InfoOutline, size: 32))),
                            Probe("Small", new Badge(
                                isLabelVisible: _isLabelVisible,
                                child: new Icon(Icons.StarOutline, size: 32))),
                            Probe(
                                "Scheme tokens",
                                new Theme(
                                    Theme.Of(context) with
                                    {
                                        ColorScheme = Theme.Of(context).ColorScheme with
                                        {
                                            Error = Color.Parse("#FF00639B"),
                                            OnError = Colors.White,
                                        },
                                    },
                                    new Badge(
                                        label: new Text("M3"),
                                        isLabelVisible: _isLabelVisible,
                                        child: new Icon(Icons.InfoOutline, size: 32)))),
                            Probe("Widget override", new Badge(
                                backgroundColor: Color.Parse("#FF00695C"),
                                textColor: Colors.White,
                                largeSize: 20,
                                offset: new Vector(7, -7),
                                label: new Text("NEW"),
                                isLabelVisible: _isLabelVisible,
                                child: new Icon(Icons.Check, size: 32))),
                            Probe(
                                _useRtl ? "Top end RTL" : "Top end LTR",
                                new Directionality(
                                    _useRtl ? TextDirection.Rtl : TextDirection.Ltr,
                                    new Badge(
                                        alignment: AlignmentDirectional.TopEnd,
                                        label: new Text("END"),
                                        isLabelVisible: _isLabelVisible,
                                        child: new Icon(Icons.InfoOutline, size: 32)))),
                        ])),
                new Text("Hover or long-press these controls:", fontSize: 14, color: Colors.Black),
                new TooltipVisibility(
                    visible: _tooltipsVisible,
                    child: new Row(
                        spacing: 12,
                        children:
                        [
                            new Tooltip(
                                message: "Default tooltip",
                                child: new OutlinedButton(
                                    onPressed: () => { },
                                    child: new Text("Default"))),
                            new Tooltip(
                                message: "Widget override tooltip",
                                preferBelow: false,
                                verticalOffset: 28,
                                decoration: new BoxDecoration(
                                    Color: Color.Parse("#FF4527A0"),
                                    BorderRadius: BorderRadius.Circular(8)),
                                textStyle: new TextStyle(Color: Colors.White, FontSize: 13),
                                waitDuration: TimeSpan.FromMilliseconds(250),
                                child: new OutlinedButton(
                                    onPressed: () => { },
                                    child: new Text("Above + delay"))),
                            new Tooltip(
                                message: "Custom right tooltip",
                                positionDelegate: position => new Point(
                                    position.Target.X + (position.TargetSize.Width / 2.0) + 8,
                                    position.Target.Y - (position.TooltipSize.Height / 2.0)),
                                child: new OutlinedButton(
                                    onPressed: () => { },
                                    child: new Text("Custom right"))),
                        ])),
            ]);

        if (!_useThemeOverrides)
        {
            return content;
        }

        return new BadgeTheme(
            data: new BadgeThemeData(
                BackgroundColor: Color.Parse("#FFB3261E"),
                TextColor: Colors.White,
                LargeSize: 18,
                SmallSize: 8,
                Padding: new Thickness(5, 0),
                Alignment: AlignmentDirectional.BottomEnd),
            child: new TooltipTheme(
                data: new TooltipThemeData(
                    Decoration: new BoxDecoration(
                        Color: Color.Parse("#FF00695C"),
                        BorderRadius: BorderRadius.Circular(6)),
                    TextStyle: new TextStyle(Color: Colors.White, FontSize: 12),
                    WaitDuration: TimeSpan.FromMilliseconds(150),
                    ExitDuration: TimeSpan.FromMilliseconds(200)),
                child: content));
    }

    private static Widget Probe(string label, Widget child)
    {
        return new Column(
            mainAxisSize: MainAxisSize.Min,
            spacing: 8,
            children: [child, new Text(label, fontSize: 12, color: Colors.Black)]);
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
