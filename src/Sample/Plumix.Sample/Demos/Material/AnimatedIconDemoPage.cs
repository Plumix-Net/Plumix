using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/animated_icon_demo_page.dart (exact sample parity)

public sealed class AnimatedIconDemoPage : StatefulWidget
{
    public override State CreateState() => new AnimatedIconDemoPageState();
}

internal sealed class AnimatedIconDemoPageState : State
{
    private static readonly IReadOnlyList<(string Label, AnimatedIconData Icon)> Catalog =
    [
        ("add_event", AnimatedIcons.AddEvent),
        ("arrow_menu", AnimatedIcons.ArrowMenu),
        ("close_menu", AnimatedIcons.CloseMenu),
        ("ellipsis_search", AnimatedIcons.EllipsisSearch),
        ("event_add", AnimatedIcons.EventAdd),
        ("home_menu", AnimatedIcons.HomeMenu),
        ("list_view", AnimatedIcons.ListView),
        ("menu_arrow", AnimatedIcons.MenuArrow),
        ("menu_close", AnimatedIcons.MenuClose),
        ("menu_home", AnimatedIcons.MenuHome),
        ("pause_play", AnimatedIcons.PausePlay),
        ("play_pause", AnimatedIcons.PlayPause),
        ("search_ellipsis", AnimatedIcons.SearchEllipsis),
        ("view_list", AnimatedIcons.ViewList),
    ];

    private AnimationController _controller = null!;
    private bool _forward;
    private bool _rightToLeft;
    private bool _large;
    private bool _muted;

    public override void InitState()
    {
        _controller = new AnimationController(duration: TimeSpan.FromMilliseconds(700), vsync: this);
    }

    public override void Dispose()
    {
        _controller.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            padding: new Thickness(0, 0, 12, 12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("AnimatedIcon + AnimatedIcons", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Complete Flutter catalog with frame interpolation, IconTheme defaults, and RTL mirroring.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            new FilledButton(
                                child: new Text(_forward ? "Reverse" : "Forward"),
                                onPressed: ToggleDirection),
                            new OutlinedButton(
                                child: new Text(_rightToLeft ? "RTL: on" : "RTL: off"),
                                onPressed: ToggleTextDirection),
                            new OutlinedButton(
                                child: new Text(_large ? "Size: 48" : "Size: 36"),
                                onPressed: ToggleSize),
                            new TextButton(
                                child: new Text(_muted ? "Opacity: 0.45" : "Opacity: 1.0"),
                                onPressed: ToggleOpacity),
                        ]),
                    new Text(
                        $"direction={(_forward ? "forward" : "reverse")}, "
                        + $"textDirection={(_rightToLeft ? "rtl" : "ltr")}",
                        fontSize: 12,
                        color: Colors.DarkSlateGray),
                    new Directionality(
                        _rightToLeft ? TextDirection.Rtl : TextDirection.Ltr,
                        new IconTheme(
                            new IconThemeData(
                                Color: Color.Parse("#FF315A7D"),
                                Size: _large ? 48 : 36,
                                Opacity: _muted ? 0.45 : 1.0),
                            new Wrap(
                                spacing: 10,
                                runSpacing: 10,
                                children: Catalog.Select(BuildCatalogTile).ToList()))),
                ]));
    }

    private Widget BuildCatalogTile((string Label, AnimatedIconData Icon) entry)
    {
        return new Container(
            width: 132,
            height: 100,
            padding: new Thickness(8),
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFF1F4F8"),
                BorderRadius: new BorderRadius(12)),
            child: new Column(
                mainAxisAlignment: MainAxisAlignment.Center,
                spacing: 6,
                children:
                [
                    new AnimatedIcon(
                        entry.Icon,
                        _controller,
                        semanticLabel: entry.Label),
                    new Text(entry.Label, fontSize: 11, color: Colors.Black),
                ]));
    }

    private void ToggleDirection()
    {
        SetState(() => _forward = !_forward);
        if (_forward)
        {
            _controller.Forward();
        }
        else
        {
            _controller.Reverse();
        }
    }

    private void ToggleTextDirection() => SetState(() => _rightToLeft = !_rightToLeft);

    private void ToggleSize() => SetState(() => _large = !_large);

    private void ToggleOpacity() => SetState(() => _muted = !_muted);
}
