using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_scrollbar_demo_page.dart
// (exact sample parity)

public sealed class CupertinoScrollbarDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoScrollbarDemoPageState();
}

internal sealed class CupertinoScrollbarDemoPageState : State
{
    private ScrollController _fadingController = null!;
    private ScrollController _alwaysVisibleController = null!;
    private ScrollController _leftController = null!;
    private bool _dark;
    private bool _rightToLeft;

    public override void InitState()
    {
        _fadingController = new ScrollController();
        _alwaysVisibleController = new ScrollController();
        _leftController = new ScrollController();
    }

    public override void Dispose()
    {
        _fadingController.Dispose();
        _alwaysVisibleController.Dispose();
        _leftController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new CupertinoTheme(
            new CupertinoThemeData(brightness: _dark ? PlatformBrightness.Dark : PlatformBrightness.Light),
            new Directionality(
                _rightToLeft ? TextDirection.Rtl : TextDirection.Ltr,
                new Container(
                    color: _dark ? Color.Parse("#FF1C1C1E") : CupertinoColors.White,
                    padding: new Thickness(12.0),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 12.0,
                        children:
                        [
                            new Text("CupertinoScrollbar", fontSize: 20.0, color: TitleColor),
                            new Text(
                                "Touching the thumb grows it from 3 to 8 logical pixels over 100 ms and "
                                + "starts the drag. Tapping the track never pages on iOS.",
                                fontSize: 14.0,
                                color: SubtitleColor),
                            new Wrap(
                                spacing: 8.0,
                                runSpacing: 8.0,
                                children:
                                [
                                    BuildControl(_dark ? "Dark" : "Light", () => _dark = !_dark),
                                    BuildControl(
                                        _rightToLeft ? "RTL" : "LTR",
                                        () => _rightToLeft = !_rightToLeft),
                                ]),
                            new Expanded(
                                child: new Row(
                                    spacing: 12.0,
                                    children:
                                    [
                                        new Expanded(
                                            child: BuildPane(
                                                "Fading",
                                                "default thumbVisibility: fades in while scrolling",
                                                new CupertinoScrollbar(
                                                    controller: _fadingController,
                                                    child: BuildList(_fadingController)))),
                                        new Expanded(
                                            child: BuildPane(
                                                "Always visible",
                                                "thumbVisibility: true, thicker while dragging",
                                                new CupertinoScrollbar(
                                                    controller: _alwaysVisibleController,
                                                    thumbVisibility: true,
                                                    thickness: 6.0,
                                                    thicknessWhileDragging: 14.0,
                                                    radius: 3.0,
                                                    radiusWhileDragging: 7.0,
                                                    child: BuildList(_alwaysVisibleController)))),
                                        new Expanded(
                                            child: BuildPane(
                                                "Left rail",
                                                "scrollbarOrientation: Left, mainAxisMargin: 12",
                                                new CupertinoScrollbar(
                                                    controller: _leftController,
                                                    thumbVisibility: true,
                                                    scrollbarOrientation: ScrollbarOrientation.Left,
                                                    mainAxisMargin: 12.0,
                                                    child: BuildList(_leftController)))),
                                    ])),
                        ]))));
    }

    private Color TitleColor => _dark ? CupertinoColors.White : CupertinoColors.Black;

    private Color SubtitleColor => _dark ? Color.Parse("#99FFFFFF") : Color.Parse("#8A000000");

    private Widget BuildPane(string title, string subtitle, Widget scrollbar)
    {
        return new Container(
            padding: new Thickness(10.0, 8.0),
            decoration: new BoxDecoration(
                Color: _dark ? Color.Parse("#FF2C2C2E") : Color.Parse("#FFF1F4F9"),
                BorderRadius: BorderRadius.Circular(10.0),
                Border: Border.FromBorderSide(
                    new BorderSide(_dark ? Color.Parse("#FF3A3A3C") : Color.Parse("#FFD6DEEA"), 1.0))),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 6.0,
                children:
                [
                    new Text(title, fontSize: 13.0, color: TitleColor),
                    new Text(subtitle, fontSize: 12.0, color: SubtitleColor),
                    new Expanded(child: scrollbar),
                ]));
    }

    private Widget BuildList(ScrollController controller)
    {
        return ListView.Builder(
            controller: controller,
            itemCount: 40,
            itemExtent: 34.0,
            itemBuilder: (_, index) => new Align(
                alignment: Alignment.CenterLeft,
                child: new Text($"row {index}", fontSize: 13.0, color: TitleColor)));
    }

    private Widget BuildControl(string label, Action onPressed)
    {
        return new CupertinoButton(
            onPressed: () => SetState(onPressed),
            padding: new Thickness(12.0, 6.0),
            child: new Text(label, fontSize: 12.0, color: CupertinoColors.ActiveBlue.Value));
    }
}
