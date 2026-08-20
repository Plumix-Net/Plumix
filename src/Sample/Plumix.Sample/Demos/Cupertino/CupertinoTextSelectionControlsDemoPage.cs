using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_text_selection_controls_demo_page.dart
// (exact sample parity)

public sealed class CupertinoTextSelectionControlsDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Builder(builderContext => new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino text selection", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Line-height-aware iOS handles and handle-free macOS selection controls.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Container(
                    padding: new Thickness(12.0),
                    decoration: new BoxDecoration(
                        Color: Color.Parse("#FFF2F2F7"),
                        BorderRadius: BorderRadius.Circular(12.0)),
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.SpaceAround,
                        children:
                        [
                            BuildHandleProbe(builderContext, "14 px line", 14.0),
                            BuildHandleProbe(builderContext, "32 px line", 32.0),
                            new Column(
                                spacing: 6.0,
                                children:
                                [
                                    new Text("macOS", fontSize: 13.0, color: Colors.Black),
                                    new Text(
                                        "no handles",
                                        fontSize: 12.0,
                                        color: Color.Parse("#8A000000")),
                                ]),
                        ])),
                new Text(
                    "Selection toolbars use the existing Cupertino mobile and desktop surfaces; "
                    + "Material TextField and SelectableText now choose these handle controls on Apple platforms.",
                    fontSize: 13.0,
                    color: Color.Parse("#8A000000")),
            ]));
    }

    private static Widget BuildHandleProbe(BuildContext context, string label, double lineHeight)
    {
        return new Column(
            spacing: 6.0,
            children:
            [
                new Text(label, fontSize: 13.0, color: Colors.Black),
                new Row(
                    spacing: 12.0,
                    children:
                    [
                        CupertinoTextSelectionControls.Instance.BuildHandle(
                            context,
                            TextSelectionHandleType.Left,
                            lineHeight),
                        CupertinoTextSelectionControls.Instance.BuildHandle(
                            context,
                            TextSelectionHandleType.Right,
                            lineHeight),
                    ]),
            ]);
    }
}

