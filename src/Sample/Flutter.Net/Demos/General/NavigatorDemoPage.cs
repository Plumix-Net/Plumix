using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/navigator_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class NavigatorDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new Text("Navigator API demo", fontSize: 20, color: Colors.Black),
                new Text(
                    "Uses static Navigator methods and RouteData query parsing.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new NavigatorRouteAwareProbe(),
                BuildAction(
                    "PushNamed('/navigator/details?id=42&mode=string')",
                    () => Navigator.PushNamed(
                        context,
                        $"{SampleRoutes.NavigatorDetails}?id=42&mode=string",
                        arguments: "payload:string"),
                    Color.Parse("#FFE8F5E9")),
                BuildAction(
                    "PushNamed(RouteData.FromLocation(...))",
                    () => Navigator.PushNamed(
                        context,
                        RouteData.FromLocation(
                            $"{SampleRoutes.NavigatorDetails}?id=7&mode=route-data",
                            arguments: "payload:route-data")),
                    Color.Parse("#FFEAF4FF")),
                BuildAction(
                    "PushReplacementNamed(details)",
                    () => Navigator.PushReplacementNamed(
                        context,
                        $"{SampleRoutes.NavigatorDetails}?id=99&mode=replacement",
                        arguments: "payload:replacement"),
                    Color.Parse("#FFFFF3E0")),
                BuildAction(
                    "PushNamedAndRemoveUntil(details, menu)",
                    () => Navigator.PushNamedAndRemoveUntil(
                        context,
                        $"{SampleRoutes.NavigatorDetails}?id=5&mode=remove-until",
                        route => route.Settings.Name == SampleRoutes.Menu,
                        arguments: "payload:remove-until"),
                    Color.Parse("#FFF3E5F5")),
            ]);
    }

    private static Widget BuildAction(string label, Action onTap, Color background)
    {
        return new CounterTapButton(
            label: label,
            onTap: onTap,
            background: background,
            foreground: Colors.Black,
            fontSize: 12,
            padding: new Thickness(10, 8));
    }
}

internal sealed class NavigatorRouteAwareProbe : StatefulWidget
{
    public override State CreateState()
    {
        return new NavigatorRouteAwareProbeState();
    }
}

internal sealed class NavigatorRouteAwareProbeState : State, RouteAware
{
    private PageRoute? _route;
    private int _didPushCount;
    private int _didPopCount;
    private int _didPopNextCount;
    private int _didPushNextCount;

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();

        var modalRoute = ModalRoute.MaybeOf(Context);
        if (modalRoute is not PageRoute pageRoute)
        {
            return;
        }

        if (ReferenceEquals(_route, pageRoute))
        {
            return;
        }

        SampleNavigationObservers.PageRoutes.Unsubscribe(this);
        _route = pageRoute;
        SampleNavigationObservers.PageRoutes.Subscribe(this, pageRoute);
    }

    public override void Dispose()
    {
        SampleNavigationObservers.PageRoutes.Unsubscribe(this);
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var routeName = ModalRoute.MaybeOf(context)?.Settings.Name ?? "(null)";
        return new Container(
            color: Color.Parse("#FFF1F8FF"),
            padding: new Thickness(10, 8),
            child: new Text(
                $"RouteAware: name={routeName} didPush={_didPushCount} didPushNext={_didPushNextCount} didPopNext={_didPopNextCount} didPop={_didPopCount}",
                fontSize: 12,
                color: Colors.Black));
    }

    public void DidPush()
    {
        _didPushCount += 1;
    }

    public void DidPop()
    {
        _didPopCount += 1;
    }

    public void DidPopNext()
    {
        SetState(() => _didPopNextCount += 1);
    }

    public void DidPushNext()
    {
        _didPushNextCount += 1;
    }
}

internal sealed class NavigatorDetailsPage : StatelessWidget
{
    private readonly RouteData _routeData;

    public NavigatorDetailsPage(RouteData routeData)
    {
        _routeData = routeData;
    }

    public override Widget Build(BuildContext context)
    {
        var id = GetQuery("id");
        var mode = GetQuery("mode");
        var payload = _routeData.Arguments?.ToString() ?? "null";

        var nextId = 1;
        if (int.TryParse(id, out var parsedId))
        {
            nextId = parsedId + 1;
        }

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new Text($"route: {_routeData.Name}", fontSize: 14, color: Colors.Black),
                new Text($"query.id: {id}", fontSize: 14, color: Colors.Black),
                new Text($"query.mode: {mode}", fontSize: 14, color: Colors.Black),
                new Text($"arguments: {payload}", fontSize: 14, color: Colors.Black),
                new SizedBox(height: 6),
                new CounterTapButton(
                    label: $"Push next details (id={nextId})",
                    onTap: () => Navigator.PushNamed(
                        context,
                        RouteData.FromLocation(
                            $"{SampleRoutes.NavigatorDetails}?id={nextId}&mode=chain",
                            arguments: $"payload:chain:{nextId}")),
                    background: Color.Parse("#FFEAF4FF"),
                    foreground: Colors.Black,
                    fontSize: 12,
                    padding: new Thickness(10, 8)),
                new CounterTapButton(
                    label: "PopUntil(menu)",
                    onTap: () => Navigator.PopUntil(
                        context,
                        route => route.Settings.Name == SampleRoutes.Menu),
                    background: Color.Parse("#FFFFF3E0"),
                    foreground: Colors.Black,
                    fontSize: 12,
                    padding: new Thickness(10, 8)),
                new CounterTapButton(
                    label: "RemoveRouteBelow(current)",
                    onTap: () =>
                    {
                        if (Navigator.CanPop(context) && ModalRoute.MaybeOf(context) is { } currentRoute)
                        {
                            Navigator.RemoveRouteBelow(context, currentRoute);
                        }
                    },
                    background: Color.Parse("#FFF3E5F5"),
                    foreground: Colors.Black,
                    fontSize: 12,
                    padding: new Thickness(10, 8)),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Start gesture",
                                onTap: () => Navigator.StartUserGesture(context),
                                background: Color.Parse("#FFE0F7FA"),
                                foreground: Colors.Black,
                                fontSize: 12,
                                padding: new Thickness(10, 8))),
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Stop gesture",
                                onTap: () => Navigator.StopUserGesture(context),
                                background: Color.Parse("#FFFFF9C4"),
                                foreground: Colors.Black,
                                fontSize: 12,
                                padding: new Thickness(10, 8))),
                    ]),
                new CounterTapButton(
                    label: "MaybePopFromUserGesture",
                    onTap: () => Navigator.MaybePopFromUserGesture(context),
                    background: Color.Parse("#FFD7CCC8"),
                    foreground: Colors.Black,
                    fontSize: 12,
                    padding: new Thickness(10, 8)),
                new CounterTapButton(
                    label: "Back (MaybePop)",
                    onTap: () => Navigator.MaybePop(context),
                    background: Colors.SteelBlue,
                    foreground: Colors.White,
                    fontSize: 12,
                    padding: new Thickness(10, 8)),
            ]);
    }

    private string GetQuery(string key)
    {
        return _routeData.QueryParameters.TryGetValue(key, out var value)
            ? value
            : "-";
    }
}
