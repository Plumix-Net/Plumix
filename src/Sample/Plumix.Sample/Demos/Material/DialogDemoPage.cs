using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/dialog_demo_page.dart

public sealed class DialogDemoPage : StatefulWidget
{
    public override State CreateState() => new DialogDemoPageState();

    private sealed class DialogDemoPageState : State
    {
        private bool _scrollable;
        private bool _barrierDismissible = true;
        private bool _useThemeOverrides;
        private string _lastResult = "none";

        public override Widget Build(BuildContext context)
        {
            var dialogTheme = _useThemeOverrides
                ? new DialogThemeData(
                    BackgroundColor: Color.Parse("#FFE0F2F1"),
                    IconColor: Color.Parse("#FF00695C"),
                    Shape: ShapeBorder.RoundedRectangle(14),
                    TitleTextStyle: Theme.Of(context).TextTheme.HeadlineSmall.CopyWith(color: Color.Parse("#FF004D40")),
                    BarrierColor: Color.FromArgb(0x99, 0x00, 0x4D, 0x40))
                : new DialogThemeData();
            return new DialogTheme(
                dialogTheme,
                new Builder(innerContext => BuildContent(innerContext)));
        }

        private Widget BuildContent(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 14,
                children:
                [
                    new Text("Dialog family", fontSize: 20),
                    new Text(
                        "Dialog, AlertDialog, SimpleDialog, typed results, intrinsic width, actions overflow, and scrollable choices.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(_scrollable ? "Scrollable" : "Static", () => SetState(() => _scrollable = !_scrollable)),
                            ControlButton(_barrierDismissible ? "Barrier closes" : "Barrier locked", () => SetState(() => _barrierDismissible = !_barrierDismissible)),
                            ControlButton(_useThemeOverrides ? "Theme on" : "Theme off", () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new ElevatedButton(new Text("SHOW ALERT"), () => ShowAlert(context)),
                            new OutlinedButton(new Text("SHOW DIALOG"), () => ShowPlainDialog(context)),
                            new FilledButton(new Text("SHOW SIMPLE"), () => ShowSimpleDialog(context)),
                        ]),
                    new Text($"Last result: {_lastResult}", fontSize: 13),
                ]);
        }

        private async void ShowAlert(BuildContext context)
        {
            string? result = await MaterialDialogs.ShowDialog<string>(
                context,
                routeContext => new AlertDialog(
                    icon: new Icon(Icons.InfoOutline),
                    title: new Text("Delete draft?"),
                    content: _scrollable
                        ? new Column(children:
                        [
                            new Text("This dialog keeps actions visible while the message scrolls."),
                            new SizedBox(height: 180),
                            new Text("End of the scrollable content."),
                        ])
                        : new Text("The draft can be restored later from history."),
                    scrollable: _scrollable,
                    actions:
                    [
                        new TextButton(new Text("CANCEL"), () => Navigator.Pop(routeContext, "cancel")),
                        new TextButton(new Text("DELETE"), () => Navigator.Pop(routeContext, "delete")),
                    ]),
                barrierDismissible: _barrierDismissible);
            if (Mounted) SetState(() => _lastResult = result ?? "dismissed");
        }

        private async void ShowPlainDialog(BuildContext context)
        {
            string? result = await MaterialDialogs.ShowDialog<string>(
                context,
                routeContext => new Dialog(
                    child: new Padding(
                        new Thickness(24),
                        new Column(
                            mainAxisSize: MainAxisSize.Min,
                            spacing: 16,
                            children:
                            [
                                new Text("Base Dialog", fontSize: 20),
                                new Text("This uses the same themed Material surface and route barrier."),
                                new TextButton(new Text("CLOSE"), () => Navigator.Pop(routeContext, "closed")),
                            ]))),
                barrierDismissible: _barrierDismissible);
            if (Mounted) SetState(() => _lastResult = result ?? "dismissed");
        }

        private async void ShowSimpleDialog(BuildContext context)
        {
            string? result = await MaterialDialogs.ShowDialog<string>(
                context,
                routeContext => new SimpleDialog(
                    title: new Text("Select workspace"),
                    children:
                    [
                        new SimpleDialogOption(
                            onPressed: () => Navigator.Pop(routeContext, "personal"),
                            child: new Text("Personal workspace")),
                        new SimpleDialogOption(
                            onPressed: () => Navigator.Pop(routeContext, "team"),
                            child: new Text("Team workspace")),
                        new SimpleDialogOption(
                            onPressed: () => Navigator.Pop(routeContext, "guest"),
                            child: new Text("Guest workspace")),
                    ]),
                barrierDismissible: _barrierDismissible);
            if (Mounted) SetState(() => _lastResult = result ?? "dismissed");
        }

        private static Widget ControlButton(string label, Action onPressed) =>
            new TextButton(new Text(label, fontSize: 12), onPressed);
    }
}
