using System;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/snack_bar_demo_page.dart

public sealed class SnackBarDemoPage : StatefulWidget
{
    public override State CreateState() => new SnackBarDemoPageState();

    private sealed class SnackBarDemoPageState : State
    {
        private bool _floating;
        private bool _showAction = true;
        private bool _showCloseIcon;
        private bool _customColors;
        private bool _swipeUp;
        private bool _fixedWidth;
        private int _actionCount;

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 14,
                children:
                [
                    new Text("SnackBar + SnackBarAction", fontSize: 20),
                    new Text(
                        "ScaffoldMessenger queue, fixed/floating placement, one-shot action, close icon, overflow, and theme/widget precedence.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(_floating ? "Floating" : "Fixed", () => SetState(() => _floating = !_floating)),
                            ControlButton(_showAction ? "Action on" : "Action off", () => SetState(() => _showAction = !_showAction)),
                            ControlButton(_showCloseIcon ? "Close on" : "Close off", () => SetState(() => _showCloseIcon = !_showCloseIcon)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(
                                _customColors ? "Colors on" : "Colors off",
                                () => SetState(() => _customColors = !_customColors)),
                            ControlButton(
                                _swipeUp ? "Swipe up" : "Swipe down",
                                () => SetState(() => _swipeUp = !_swipeUp)),
                            ControlButton(
                                _fixedWidth ? "Width 320" : "Full width",
                                () => SetState(() => _fixedWidth = !_fixedWidth)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new ElevatedButton(new Text("SHOW SNACKBAR"), () => ShowSnackBar(context)),
                        ]),
                    new Text($"Action presses: {_actionCount}", fontSize: 13),
                    new Text(
                        "Narrow the window to move a wide action below the message; repeated SHOW calls "
                        + "show queue order. Drag the bar in the swipe direction to dismiss it; the width "
                        + "probe needs floating behavior.",
                        fontSize: 13,
                        color: Colors.DimGray),
                ]);
        }

        private void ShowSnackBar(BuildContext context)
        {
            ScaffoldMessenger.Of(context).ShowSnackBar(new SnackBar(
                content: new Text("A queued Material snackbar from Plumix."),
                behavior: _floating ? SnackBarBehavior.Floating : SnackBarBehavior.Fixed,
                backgroundColor: _customColors ? Color.Parse("#FF004D40") : null,
                closeIconColor: _customColors ? Color.Parse("#FFFFD54F") : null,
                showCloseIcon: _showCloseIcon,
                persist: _showAction || _showCloseIcon,
                dismissDirection: _swipeUp ? DismissDirection.Up : DismissDirection.Down,
                width: _floating && _fixedWidth ? 320 : null,
                action: _showAction
                    ? new SnackBarAction(
                        label: "UNDO LONG ACTION",
                        textColor: _customColors ? Color.Parse("#FFFFD54F") : null,
                        onPressed: () => SetState(() => _actionCount++))
                    : null));
        }

        private static Widget ControlButton(string label, Action onPressed) =>
            new TextButton(new Text(label, fontSize: 12), onPressed);
    }
}
