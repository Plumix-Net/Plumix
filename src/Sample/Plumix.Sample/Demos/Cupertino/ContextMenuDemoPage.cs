using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/context_menu_demo_page.dart

public sealed class CupertinoContextMenuDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoContextMenuDemoPageState();
}

internal sealed class CupertinoContextMenuDemoPageState : State
{
    private string _lastAction = "none";

    public override Widget Build(BuildContext context)
    {
        Widget preview = new Container(
            width: 180.0,
            height: 120.0,
            decoration: new BoxDecoration(
                Color: Color.FromUInt32(0xFF5E5CE6),
                BorderRadius: BorderRadius.Circular(16.0)),
            alignment: Alignment.Center,
            child: new Text(
                "Press and hold",
                color: Colors.White,
                fontSize: 18.0));

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 18.0,
            children:
            [
                new Text("Cupertino context menu", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Hold the preview, choose an action, or drag the open preview down to dismiss.",
                    fontSize: 14.0,
                    color: Color.FromUInt32(0x8A000000)),
                new Center(
                    child: new CupertinoContextMenu(
                        actions:
                        [
                            BuildAction(context, "Copy", CupertinoIcons.DocOnDoc, "copy"),
                            BuildAction(context, "Share", CupertinoIcons.Share, "share"),
                            BuildAction(
                                context,
                                "Delete",
                                CupertinoIcons.Delete,
                                "delete",
                                destructive: true),
                        ],
                        child: preview,
                        enableHapticFeedback: true)),
                new Text(
                    $"last action: {_lastAction}",
                    fontSize: 13.0,
                    color: Color.FromUInt32(0xFF455A64)),
            ]);
    }

    private Widget BuildAction(
        BuildContext context,
        string label,
        IconData icon,
        string result,
        bool destructive = false)
    {
        return new CupertinoContextMenuAction(
            child: new Text(label),
            trailingIcon: icon,
            isDestructiveAction: destructive,
            onPressed: () =>
            {
                Navigator.Pop(context);
                SetState(() => _lastAction = result);
            });
    }
}
