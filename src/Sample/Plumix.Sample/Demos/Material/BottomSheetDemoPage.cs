using System;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/bottom_sheet_demo_page.dart

public sealed class BottomSheetDemoPage : StatefulWidget
{
    public override State CreateState() => new BottomSheetDemoPageState();

    private sealed class BottomSheetDemoPageState : State
    {
        private bool _showDragHandle = true;
        private bool _scrollControlled;
        private bool _customTheme;
        private bool _anchorEnd;
        private string _lastResult = "none";

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("BottomSheet + ModalBottomSheet", fontSize: 20),
                    new Text(
                        "Persistent LocalHistory/controller flow, modal scrim/result, drag handle, 9/16 height cap, "
                        + "SafeArea, display-feature anchoring, and theme precedence.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(_showDragHandle ? "Handle on" : "Handle off", () => SetState(() => _showDragHandle = !_showDragHandle)),
                            ControlButton(_scrollControlled ? "Full height" : "9/16 cap", () => SetState(() => _scrollControlled = !_scrollControlled)),
                            ControlButton(_customTheme ? "Theme on" : "Theme off", () => SetState(() => _customTheme = !_customTheme)),
                            ControlButton(
                                _anchorEnd ? "Anchor end" : "Anchor start",
                                () => SetState(() => _anchorEnd = !_anchorEnd)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new ElevatedButton(new Text("SHOW PERSISTENT"), () => ShowPersistent(context)),
                            new FilledButton(new Text("SHOW MODAL"), () => ShowModal(context)),
                        ]),
                    new Text($"Last modal result: {_lastResult}", fontSize: 13),
                ]);
        }

        private void ShowPersistent(BuildContext context)
        {
            PersistentBottomSheetController? controller = null;
            controller = MaterialBottomSheets.ShowBottomSheet(
                context,
                _ => BuildSheetContent("Persistent sheet", () => controller?.Close()),
                backgroundColor: _customTheme ? Color.Parse("#FFFFF0F5") : null,
                shape: _customTheme ? ShapeBorder.RoundedRectangle(18) : null,
                showDragHandle: _showDragHandle);
        }

        private async void ShowModal(BuildContext context)
        {
            string? result = await MaterialBottomSheets.ShowModalBottomSheet<string>(
                context,
                sheetContext => BuildSheetContent("Modal sheet", () => Navigator.Pop(sheetContext, "accepted")),
                backgroundColor: _customTheme ? Color.Parse("#FFE8DEF8") : null,
                shape: _customTheme ? ShapeBorder.RoundedRectangle(18) : null,
                isScrollControlled: _scrollControlled,
                showDragHandle: _showDragHandle,
                useSafeArea: true,
                anchorPoint: _anchorEnd ? new Avalonia.Point(double.MaxValue, 0) : null);
            if (Mounted) SetState(() => _lastResult = result ?? "dismissed");
        }

        private static Widget BuildSheetContent(string title, Action close) => new Padding(
            new Avalonia.Thickness(24, 12, 24, 24),
            new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text(title, fontSize: 18),
                    new Text("Drag downward or use the action below.", color: Colors.DimGray),
                    new TextButton(new Text("CLOSE"), close),
                ]));

        private static Widget ControlButton(string label, Action onPressed) =>
            new TextButton(new Text(label, fontSize: 12), onPressed);
    }
}
