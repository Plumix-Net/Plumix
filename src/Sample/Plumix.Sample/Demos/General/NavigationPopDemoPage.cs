using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/navigation_pop_demo_page.dart (exact sample parity)

public sealed class NavigationPopDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new NavigationPopDemoPageState();
    }
}

internal sealed class NavigationPopDemoPageState : State
{
    private NavigatorState? _nestedNavigator;
    private bool _canLeave = true;
    private int _nestedPage = 1;
    private string _status = "No pop attempted";

    public override Widget Build(BuildContext context)
    {
        return new PopScope<object?>(
            canPop: _canLeave,
            onPopInvokedWithResult: (didPop, result) =>
            {
                if (Mounted)
                {
                    SetState(() => _status = didPop ? "Route popped" : "Pop handled or blocked");
                }
            },
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("PopScope + NavigatorPopHandler", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Push a nested page, then simulate a parent Back. The handler consumes it in the nested " +
                        "navigator. Disable route pop to probe PopScope veto behavior.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new TextButton(
                                onPressed: ToggleCanLeave,
                                child: new Text(_canLeave ? "Disable route pop" : "Enable route pop")),
                            new TextButton(
                                onPressed: PushNestedPage,
                                child: new Text("Push nested page")),
                            new TextButton(
                                onPressed: () => Navigator.Of(Context).MaybePop("demo-result"),
                                child: new Text("Simulate parent Back")),
                        ]),
                    new Text($"Status: {_status}", color: Color.Parse("#FF31506F")),
                    new Expanded(
                        child: new NavigatorPopHandler<object?>(
                            onPopWithResult: result =>
                            {
                                _nestedNavigator?.MaybePop(result);
                                if (Mounted)
                                {
                                    SetState(() => _status = "NavigatorPopHandler popped the nested route");
                                }
                            },
                            child: new Navigator(
                                initialRoute: new BuilderPageRoute(
                                    builder: nestedContext =>
                                    {
                                        _nestedNavigator = Navigator.Of(nestedContext);
                                        return BuildNestedPage(0);
                                    },
                                    settings: new RouteSettings(Name: "nested-root"))))),
                ]));
    }

    private void ToggleCanLeave()
    {
        SetState(() =>
        {
            _canLeave = !_canLeave;
            _status = _canLeave ? "Route pop enabled" : "Route pop disabled";
        });
    }

    private void PushNestedPage()
    {
        int page = _nestedPage++;
        _nestedNavigator?.Push(new BuilderPageRoute(
            builder: _ => BuildNestedPage(page),
            settings: new RouteSettings(Name: $"nested-{page}")));
        SetState(() => _status = $"Nested page {page} pushed");
    }

    private static Widget BuildNestedPage(int page)
    {
        return new Container(
            color: page == 0 ? Color.Parse("#FFE8F0FE") : Color.Parse("#FFE6F4EA"),
            padding: new Thickness(16),
            alignment: Alignment.Center,
            child: new Text(
                page == 0 ? "Nested root" : $"Nested page {page}",
                fontSize: 18,
                color: Colors.Black));
    }
}
