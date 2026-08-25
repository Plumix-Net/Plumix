using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/action_sheet_demo_page.dart
// (exact sample parity)

public sealed class CupertinoActionSheetDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoActionSheetDemoPageState();
}

internal sealed class CupertinoActionSheetDemoPageState : State
{
    private string _lastResult = "none";

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10.0,
            children:
            [
                new Text("Cupertino action sheet", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Blurred bottom sheet with a title/message section, hairline-separated actions, "
                    + "a detached cancel button, and slide-to-select.",
                    fontSize: 14.0,
                    color: Colors.DimGray),
                new Text($"last result: {_lastResult}", fontSize: 12.0, color: Color.Parse("#FF607D8B")),
                BuildAction(
                    "Title + message + actions + cancel",
                    () => ShowFullSheet(context),
                    Color.Parse("#FFE9F0FF")),
                BuildAction(
                    "Actions only",
                    () => ShowActionsOnly(context),
                    Color.Parse("#FFEAE4FF")),
                BuildAction(
                    "Message + cancel only",
                    () => ShowMessageOnly(context),
                    Color.Parse("#FFE8F0FE")),
                BuildAction(
                    "Scrollable action list",
                    () => ShowScrollableSheet(context),
                    Color.Parse("#FFE8F4E8")),
            ]);
    }

    private void ShowFullSheet(BuildContext context)
    {
        Show(context, sheetContext => new CupertinoActionSheet(
            title: new Text("Move to trash"),
            message: new Text("This document and every revision of it will be deleted."),
            actions:
            [
                new CupertinoActionSheetAction(
                    new Text("Keep editing"),
                    () => Complete(sheetContext, "keep editing"),
                    isDefaultAction: true),
                new CupertinoActionSheetAction(
                    new Text("Duplicate first"),
                    () => Complete(sheetContext, "duplicate first")),
                new CupertinoActionSheetAction(
                    new Text("Delete"),
                    () => Complete(sheetContext, "delete"),
                    isDestructiveAction: true),
            ],
            cancelButton: new CupertinoActionSheetAction(
                new Text("Cancel"),
                () => Complete(sheetContext, "cancel"))));
    }

    private void ShowActionsOnly(BuildContext context)
    {
        Show(context, sheetContext => new CupertinoActionSheet(actions:
        [
            new CupertinoActionSheetAction(
                new Text("Copy link"),
                () => Complete(sheetContext, "copy link")),
            new CupertinoActionSheetAction(
                new Text("Share"),
                () => Complete(sheetContext, "share")),
        ]));
    }

    private void ShowMessageOnly(BuildContext context)
    {
        Show(context, sheetContext => new CupertinoActionSheet(
            message: new Text("Signing out removes every downloaded file from this device."),
            cancelButton: new CupertinoActionSheetAction(
                new Text("Not now"),
                () => Complete(sheetContext, "not now"))));
    }

    private void ShowScrollableSheet(BuildContext context)
    {
        Show(context, sheetContext => new CupertinoActionSheet(
            title: new Text("Pick a destination"),
            actions: Enumerable.Range(1, 12)
                .Select(index => (Widget)new CupertinoActionSheetAction(
                    new Text($"Folder {index}"),
                    () => Complete(sheetContext, $"folder {index}")))
                .ToList(),
            cancelButton: new CupertinoActionSheetAction(
                new Text("Cancel"),
                () => Complete(sheetContext, "cancel"))));
    }

    private static void Show(BuildContext context, WidgetBuilder builder)
    {
        _ = CupertinoDialogs.ShowCupertinoModalPopup<string>(context, builder);
    }

    private void Complete(BuildContext context, string result)
    {
        SetState(() => _lastResult = result);
        Navigator.Of(context).Pop(result);
    }

    private static Widget BuildAction(string label, Action onTap, Color background)
    {
        return new CounterTapButton(
            label: label,
            onTap: onTap,
            background: background,
            foreground: Colors.Black,
            fontSize: 12.0,
            padding: new Thickness(10.0, 8.0));
    }
}
