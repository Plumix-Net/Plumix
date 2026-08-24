using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_activity_indicator_demo_page.dart
// (exact sample parity)

public sealed class CupertinoActivityIndicatorDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoActivityIndicatorDemoPageState();
}

internal sealed class CupertinoActivityIndicatorDemoPageState : State
{
    private double _progress = 0.6;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino activity indicators", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Spinning ticks, partially revealed ticks and the linear progress bar.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Wrap(
                    spacing: 24.0,
                    runSpacing: 12.0,
                    crossAxisAlignment: WrapCrossAlignment.Center,
                    children:
                    [
                        BuildLabeled("Default", new CupertinoActivityIndicator()),
                        BuildLabeled("radius 20", new CupertinoActivityIndicator(radius: 20.0)),
                        BuildLabeled(
                            "Tinted",
                            new CupertinoActivityIndicator(
                                color: CupertinoColors.ActiveOrange.Value,
                                radius: 20.0)),
                        BuildLabeled(
                            "Paused",
                            new CupertinoActivityIndicator(animating: false, radius: 20.0)),
                        BuildLabeled(
                            "Partial",
                            CupertinoActivityIndicator.PartiallyRevealed(
                                progress: _progress,
                                radius: 20.0)),
                    ]),
                new Text("Progress for the partial spinner and the bars below:", fontSize: 14.0, color: Colors.Black),
                new CupertinoSlider(
                    value: _progress,
                    onChanged: value => SetState(() => _progress = value)),
                new CupertinoLinearActivityIndicator(progress: _progress),
                new CupertinoLinearActivityIndicator(
                    progress: _progress,
                    height: 10.0,
                    color: CupertinoColors.ActiveGreen.Value),
            ]);
    }

    private static Widget BuildLabeled(string label, Widget indicator)
    {
        return new Column(
            mainAxisSize: MainAxisSize.Min,
            spacing: 6.0,
            children:
            [
                new SizedBox(
                    width: 44.0,
                    height: 44.0,
                    child: new Center(child: indicator)),
                new Text(label, fontSize: 12.0, color: Colors.Black),
            ]);
    }
}
