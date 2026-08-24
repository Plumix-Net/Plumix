using System;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_menu_anchor_demo_page.dart

public sealed class CupertinoMenuAnchorDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoMenuAnchorDemoPageState();
}

internal sealed class CupertinoMenuAnchorDemoPageState : State
{
    private string _lastAction = "No action selected";

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Start,
            spacing: 16.0,
            children:
            [
                new Text("Cupertino menu anchor", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Open the anchored menu by button, keyboard, long press, or swipe. " +
                    "Items demonstrate leading, subtitle, trailing, disabled, and destructive states.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new CupertinoMenuAnchor(
                    enableLongPressToOpen: true,
                    menuChildren:
                    [
                        new CupertinoMenuItem(
                            child: new Text("Rename"),
                            subtitle: new Text("Keep this document in place"),
                            leading: new Icon(CupertinoIcons.Pencil),
                            trailing: new Text("⌘R"),
                            onPressed: () => Select("Rename")),
                        new CupertinoMenuItem(
                            child: new Text("Share"),
                            leading: new Icon(CupertinoIcons.Share),
                            onPressed: () => Select("Share")),
                        new CupertinoMenuDivider(),
                        new CupertinoMenuItem(
                            child: new Text("Unavailable action"),
                            leading: new Icon(CupertinoIcons.Folder),
                            onPressed: null),
                        new CupertinoMenuItem(
                            child: new Text("Delete"),
                            leading: new Icon(CupertinoIcons.Trash),
                            isDestructiveAction: true,
                            onPressed: () => Select("Delete")),
                    ],
                    builder: (_, controller, child) => new CupertinoButton(
                        child: child ?? new Row(
                            mainAxisSize: MainAxisSize.Min,
                            spacing: 8.0,
                            children:
                            [
                                new Text("Document actions", color: Colors.White),
                                new Icon(CupertinoIcons.ChevronDown, color: Colors.White, size: 18.0),
                            ]),
                        color: Color.Parse("#FF007AFF"),
                        onPressed: () =>
                        {
                            if (controller.IsOpen)
                            {
                                controller.Close();
                            }
                            else
                            {
                                controller.Open();
                            }
                        })),
                new Text(_lastAction, fontSize: 13.0, color: Color.Parse("#FF455A64")),
            ]);
    }

    private void Select(string action)
    {
        SetState(() => _lastAction = $"Last action: {action}");
    }
}
