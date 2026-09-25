using System;
using System.Collections.Generic;
using System.Linq;
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
                    "Page transitions, modal popups, and a CupertinoTabView with independent history.",
                    fontSize: 14.0,
                    color: Colors.DimGray),
                new Text($"last result: {_lastResult}", fontSize: 12.0, color: new Color(0xFF607D8B)),
                BuildAction(
                    "Push standard CupertinoPageRoute",
                    () => PushPage(context, fullscreenDialog: false),
                    new Color(0xFFE9F0FF)),
                BuildAction(
                    "Push fullscreen CupertinoPageRoute",
                    () => PushPage(context, fullscreenDialog: true),
                    new Color(0xFFEAE4FF)),
                BuildAction(
                    "Open independent CupertinoTabView",
                    () => PushTabView(context),
                    new Color(0xFFE8F0FE)),
                BuildAction(
                    "Show Cupertino modal popup",
                    () => ShowPopup(context),
                    new Color(0xFFE8F4E8)),
                BuildAction(
                    "Show draggable Cupertino sheet",
                    () => ShowSheet(context),
                    new Color(0xFFE5F4FF)),
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
                                new Color(0xFFFFF3E0)),
                        ]))),
            title: fullscreenDialog ? "Fullscreen" : "Details",
            fullscreenDialog: fullscreenDialog);
        Navigator.Of(context).Push(route);
    }

    private static void PushTabView(BuildContext context)
    {
        Navigator.Of(context).Push(new CupertinoPageRoute<object?>(
            title: "Tab history",
            builder: _ => new CupertinoTabView(
                defaultTitle: "Tab root",
                builder: tabContext => BuildTabPage(
                    "Independent tab root",
                    "Push a named route inside this tab",
                    () => Navigator.Of(tabContext).PushNamed("/details")),
                routes: new Dictionary<string, WidgetBuilder>
                {
                    ["/details"] = tabContext => BuildTabPage(
                        "Named tab route",
                        "Pop back to the tab root",
                        () => Navigator.Of(tabContext).Pop()),
                })));
    }

    private static Widget BuildTabPage(string title, string actionLabel, Action onTap)
    {
        return new Center(
            child: new Container(
                color: Colors.White,
                padding: new Thickness(20.0),
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 12.0,
                    children:
                    [
                        new Text(title, fontSize: 16.0, color: Colors.Black),
                        BuildAction(actionLabel, onTap, new Color(0xFFE8F0FE)),
                    ])));
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
                            new Color(0xFFE0F2F1)),
                    ])));
    }

    private static void ShowSheet(BuildContext context)
    {
        Navigator.Of(context).Push(new CupertinoSheetRoute<object?>(
            topGap: 0.12,
            showDragHandle: true,
            scrollableBuilder: (sheetContext, controller) => new Container(
                color: Colors.White,
                child: new SafeArea(
                    child: new ListView(
                        controller: controller,
                        itemExtent: 48.0,
                        padding: new Thickness(16.0, 20.0),
                        children:
                        [
                            new Text("Drag down to dismiss · drag up to stretch", fontSize: 15.0),
                            BuildAction(
                                "Close with CupertinoSheetRoute.PopSheet",
                                () => CupertinoSheetRoute<object?>.PopSheet(sheetContext),
                                new Color(0xFFFFF3E0)),
                            .. Enumerable.Range(1, 12)
                                .Select(index => (Widget)new Text($"Scrollable sheet row {index}", fontSize: 14.0)),
                        ])))));
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
