using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/route_demo_page.dart (exact sample parity)

public sealed class CupertinoRouteDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoRouteDemoPageState();
}

internal sealed class CupertinoRouteDemoPageState : State
{
    private string _lastResult = "none";

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10.0,
            children:
            [
                new Text("Cupertino routes", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Normal and fullscreen page transitions, leading-edge back swipe, and modal popup route.",
                    fontSize: 14.0,
                    color: Colors.DimGray),
                new Text($"last result: {_lastResult}", fontSize: 12.0, color: Color.Parse("#FF607D8B")),
                BuildAction(
                    "Push standard CupertinoPageRoute",
                    () => PushPage(context, fullscreenDialog: false),
                    Color.Parse("#FFE9F0FF")),
                BuildAction(
                    "Push fullscreen CupertinoPageRoute",
                    () => PushPage(context, fullscreenDialog: true),
                    Color.Parse("#FFEAE4FF")),
                BuildAction(
                    "Show Cupertino modal popup",
                    () => ShowPopup(context),
                    Color.Parse("#FFE8F4E8")),
            ]);
    }

    private void PushPage(BuildContext context, bool fullscreenDialog)
    {
        string routeKind = fullscreenDialog ? "fullscreen" : "standard";
        var route = new CupertinoPageRoute<string>(
            builder: routeContext => new Center(
                child: new Container(
                    color: Colors.White,
                    padding: new Thickness(20.0),
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 12.0,
                        children:
                        [
                            new Text(
                                fullscreenDialog
                                    ? "Bottom-up fullscreen transition"
                                    : "Swipe from the leading edge to go back",
                                fontSize: 16.0,
                                color: Colors.Black),
                            BuildAction(
                                "Pop with result",
                                () => Complete(routeContext, $"{routeKind} page"),
                                Color.Parse("#FFFFF3E0")),
                        ]))),
            title: fullscreenDialog ? "Fullscreen" : "Details",
            fullscreenDialog: fullscreenDialog);
        Navigator.Of(context).Push(route);
    }

    private void ShowPopup(BuildContext context)
    {
        _ = CupertinoDialogs.ShowCupertinoModalPopup<string>(
            context,
            popupContext => new Container(
                color: Colors.White,
                padding: new Thickness(20.0),
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 12.0,
                    children:
                    [
                        new Text("Spring-driven bottom popup", fontSize: 16.0, color: Colors.Black),
                        BuildAction(
                            "Close popup",
                            () => Complete(popupContext, "modal popup"),
                            Color.Parse("#FFE0F2F1")),
                    ])));
    }

    private void Complete(BuildContext context, string result)
    {
        SetState(() => _lastResult = result);
        Navigator.Of(context).Pop(result);
    }

    private static Widget BuildAction(string label, Action onTap, Color background)
    {
        return new CounterTapButton(
            label: label,
            onTap: onTap,
            background: background,
            foreground: Colors.Black,
            fontSize: 12.0,
            padding: new Thickness(10.0, 8.0));
    }
}
