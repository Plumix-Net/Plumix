using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/refresh_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CupertinoRefreshDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new CupertinoRefreshDemoPageState();
    }
}

internal sealed class CupertinoRefreshDemoPageState : State
{
    private int _refreshCount;
    private string _status = "Pull down from the top";

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10.0,
            children:
            [
                new Text("Cupertino sliver refresh", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Pull the list down past the indicator, then release. The sliver holds 60 px while refreshing.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Text(
                    $"{_status} · refreshCount={_refreshCount}",
                    fontSize: 12.0,
                    color: Color.Parse("#FF607D8B")),
                new Expanded(
                    child: new CustomScrollView(
                        physics: new BouncingScrollPhysics(parent: new AlwaysScrollableScrollPhysics()),
                        slivers:
                        [
                            new CupertinoSliverRefreshControl(onRefresh: HandleRefresh),
                            SliverFixedExtentList.Builder(
                                childCount: 24,
                                itemExtent: 54.0,
                                addAutomaticKeepAlives: false,
                                itemBuilder: (_, index) => new Container(
                                    color: index % 2 == 0 ? Colors.White : Color.Parse("#FFF5F7FA"),
                                    padding: new Thickness(12.0, 10.0),
                                    child: new Text(
                                        $"Cupertino refresh row #{index + 1}",
                                        fontSize: 13.0,
                                        color: Colors.Black))),
                        ])),
            ]);
    }

    private async Task HandleRefresh()
    {
        if (Mounted)
        {
            SetState(() => _status = "Refreshing");
        }

        await Task.Delay(650);
        if (Mounted)
        {
            SetState(() =>
            {
                _refreshCount++;
                _status = "Refresh complete";
            });
        }
    }
}
