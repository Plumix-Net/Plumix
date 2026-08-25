using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_button_demo_page.dart
// (exact sample parity)

public sealed class CupertinoButtonDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoButtonDemoPageState();
}

internal sealed class CupertinoButtonDemoPageState : State
{
    private int _taps;
    private int _longPresses;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino buttons", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Plain, tinted and filled styles across the three size styles.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Wrap(
                    spacing: 12.0,
                    runSpacing: 12.0,
                    crossAxisAlignment: WrapCrossAlignment.Center,
                    children:
                    [
                        new CupertinoButton(
                            child: new Text("Plain"),
                            onPressed: () => SetState(() => _taps++)),
                        CupertinoButton.Tinted(
                            child: new Text("Tinted"),
                            onPressed: () => SetState(() => _taps++)),
                        CupertinoButton.Filled(
                            child: new Text("Filled"),
                            onPressed: () => SetState(() => _taps++)),
                        new CupertinoButton(child: new Text("Disabled"), onPressed: null),
                        CupertinoButton.Filled(child: new Text("Disabled filled"), onPressed: null),
                    ]),
                new Text("Size styles", fontSize: 14.0, color: Colors.Black),
                new Wrap(
                    spacing: 12.0,
                    runSpacing: 12.0,
                    crossAxisAlignment: WrapCrossAlignment.Center,
                    children:
                    [
                        CupertinoButton.Filled(
                            child: new Text("Small"),
                            sizeStyle: CupertinoButtonSize.Small,
                            onPressed: () => SetState(() => _taps++)),
                        CupertinoButton.Filled(
                            child: new Text("Medium"),
                            sizeStyle: CupertinoButtonSize.Medium,
                            onPressed: () => SetState(() => _taps++)),
                        CupertinoButton.Filled(
                            child: new Text("Large"),
                            onPressed: () => SetState(() => _taps++)),
                    ]),
                new Text("Customisation", fontSize: 14.0, color: Colors.Black),
                new Wrap(
                    spacing: 12.0,
                    runSpacing: 12.0,
                    crossAxisAlignment: WrapCrossAlignment.Center,
                    children:
                    [
                        CupertinoButton.Tinted(
                            child: new Text("Grey tint"),
                            color: CupertinoColors.SystemGrey,
                            onPressed: () => SetState(() => _taps++)),
                        CupertinoButton.Filled(
                            child: new Text("Custom radius"),
                            color: CupertinoColors.SystemRed,
                            borderRadius: BorderRadius.Circular(4.0),
                            onPressed: () => SetState(() => _taps++)),
                        new CupertinoButton(
                            child: new Icon(CupertinoIcons.Heart),
                            foregroundColor: CupertinoColors.SystemPink.Value,
                            onPressed: () => SetState(() => _taps++)),
                        new CupertinoButton(
                            child: new Text("Long press me"),
                            onPressed: null,
                            onLongPress: () => SetState(() => _longPresses++)),
                    ]),
                new Text($"Taps: {_taps}   Long presses: {_longPresses}", fontSize: 14.0, color: Colors.Black),
            ]);
    }
}
