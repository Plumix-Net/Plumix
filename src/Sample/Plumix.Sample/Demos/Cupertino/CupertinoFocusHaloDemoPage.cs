using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_focus_halo_demo_page.dart
// (exact sample parity)

public sealed class CupertinoFocusHaloDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoFocusHaloDemoPageState();
}

internal sealed class CupertinoFocusHaloDemoPageState : State
{
    private readonly FocusNode _rectFocus = new();
    private readonly FocusNode _roundedRectFocus = new();
    private readonly FocusNode _superellipseFocus = new();

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino focus halo", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Press Tab or click a tile to move focus through the three halo shapes.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Wrap(
                    spacing: 16.0,
                    runSpacing: 16.0,
                    children:
                    [
                        CupertinoFocusHalo.WithRect(BuildFocusableTile("Rectangle", _rectFocus)),
                        CupertinoFocusHalo.WithRRect(
                            BuildFocusableTile("Rounded rectangle", _roundedRectFocus),
                            BorderRadius.Circular(12.0)),
                        CupertinoFocusHalo.WithRoundedSuperellipse(
                            BuildFocusableTile("Rounded superellipse", _superellipseFocus),
                            BorderRadius.Circular(12.0)),
                    ]),
            ]);
    }

    public override void Dispose()
    {
        _rectFocus.Dispose();
        _roundedRectFocus.Dispose();
        _superellipseFocus.Dispose();
        base.Dispose();
    }

    private static Widget BuildFocusableTile(string label, FocusNode focusNode)
    {
        return new Focus(
            focusNode: focusNode,
            child: new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: () => focusNode.RequestFocus(),
                child: new Container(
                    width: 176.0,
                    height: 72.0,
                    alignment: Alignment.Center,
                    decoration: new BoxDecoration(
                        Color: Color.Parse("#FFF2F2F7"),
                        BorderRadius: BorderRadius.Circular(12.0)),
                    child: new Text(label, fontSize: 14.0, color: Colors.Black, textAlign: TextAlign.Center))));
    }
}
