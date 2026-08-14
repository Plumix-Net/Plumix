using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/router_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class RouterDemoPage : StatefulWidget
{
    public override State CreateState() => new RouterDemoPageState();
}

internal sealed class RouterDemoPageState : State
{
    private readonly DemoRouteInformationProvider _provider =
        new(new RouteInformation(new Uri("/home", UriKind.Relative)));

    private readonly DemoRouterDelegate _routerDelegate = new();
    private readonly RootBackButtonDispatcher _backButtonDispatcher = new();
    private readonly List<string> _reports = [];

    public override void InitState()
    {
        base.InitState();
        _provider.Reported = information => SetState(() => _reports.Add(information.Uri.ToString()));
    }

    public override void Dispose()
    {
        _provider.Dispose();
        _routerDelegate.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new Text("Router demo", fontSize: 20, color: Colors.Black),
                new Text(
                    "The provider publishes a location, the parser turns it into a configuration and the "
                    + "delegate builds the page. Popping goes through the back-button dispatcher.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildAction("Home", () => Go("/home"), Color.Parse("#FFE8F5E9")),
                        BuildAction("Details", () => Go("/details"), Color.Parse("#FFE3F2FD")),
                        BuildAction("Settings", () => Go("/settings"), Color.Parse("#FFFFF3E0")),
                        BuildAction("Back", HandleBack, Color.Parse("#FFFCE4EC")),
                    ]),
                new Text($"reported: {string.Join(", ", _reports)}", fontSize: 12),
                new SizedBox(
                    height: 180,
                    child: new ColoredBox(
                        color: Color.Parse("#FFFAFAFA"),
                        child: new Router<DemoRouteConfiguration>(
                            routerDelegate: _routerDelegate,
                            routeInformationProvider: _provider,
                            routeInformationParser: new DemoRouteInformationParser(),
                            backButtonDispatcher: _backButtonDispatcher))),
            ]);
    }

    private void Go(string location)
    {
        _provider.SetValue(new RouteInformation(new Uri(location, UriKind.Relative)));
    }

    private void HandleBack()
    {
        _ = _backButtonDispatcher.InvokeCallback(Task.FromResult(false));
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

/// <summary>The parsed configuration the demo delegate renders.</summary>
internal sealed record DemoRouteConfiguration(string Path, bool ShowDetail);

internal sealed class DemoRouteInformationParser : RouteInformationParser<DemoRouteConfiguration>
{
    public override Task<DemoRouteConfiguration> ParseRouteInformation(RouteInformation routeInformation)
    {
        string path = routeInformation.Uri.ToString();
        return Task.FromResult(new DemoRouteConfiguration(path, path == "/details"));
    }

    public override RouteInformation? RestoreRouteInformation(DemoRouteConfiguration configuration)
    {
        return new RouteInformation(new Uri(configuration.Path, UriKind.Relative));
    }
}

internal sealed class DemoRouterDelegate : RouterDelegate<DemoRouteConfiguration>
{
    private DemoRouteConfiguration _configuration = new("/home", ShowDetail: false);

    public override DemoRouteConfiguration? CurrentConfiguration => _configuration;

    public override Task SetNewRoutePath(DemoRouteConfiguration configuration)
    {
        _configuration = configuration;
        return Task.CompletedTask;
    }

    public override Task<bool> PopRoute()
    {
        if (_configuration.Path == "/home")
        {
            return Task.FromResult(false);
        }

        _configuration = new DemoRouteConfiguration("/home", ShowDetail: false);
        NotifyListeners();
        return Task.FromResult(true);
    }

    public override Widget Build(BuildContext context)
    {
        return new BackButtonListener(
            onBackButtonPressed: PopRoute,
            child: new Center(
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 8,
                    children:
                    [
                        new Text(_configuration.Path, fontSize: 18, color: Colors.Black),
                        new Text(
                            _configuration.ShowDetail ? "detail page" : "top level page",
                            fontSize: 12,
                            color: Colors.DimGray),
                    ])));
    }
}

internal sealed class DemoRouteInformationProvider : RouteInformationProvider
{
    private RouteInformation _value;

    public DemoRouteInformationProvider(RouteInformation value)
    {
        _value = value;
    }

    public Action<RouteInformation>? Reported { get; set; }

    public override RouteInformation Value => _value;

    public void SetValue(RouteInformation value)
    {
        _value = value;
        NotifyListeners();
    }

    public override void RouterReportsNewRouteInformation(
        RouteInformation routeInformation,
        RouteInformationReportingType type = RouteInformationReportingType.None)
    {
        _value = routeInformation;
        Reported?.Invoke(routeInformation);
    }
}
