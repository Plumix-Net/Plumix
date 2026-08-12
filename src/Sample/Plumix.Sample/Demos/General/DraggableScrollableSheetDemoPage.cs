using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/draggable_scrollable_sheet_demo_page.dart

public sealed class DraggableScrollableSheetDemoPage : StatefulWidget
{
    public override State CreateState() => new DraggableScrollableSheetDemoPageState();

    private sealed class DraggableScrollableSheetDemoPageState : State
    {
        private readonly DraggableScrollableController _controller = new();
        private bool _snap = true;
        private double _extent = 0.5;

        public override void InitState()
        {
            _controller.AddListener(HandleSizeChanged);
        }

        public override void Dispose()
        {
            _controller.RemoveListener(HandleSizeChanged);
            _controller.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("DraggableScrollableSheet", fontSize: 20),
                    new Text(
                        "Drag the sheet to resize it, keep dragging to scroll its list, and release to snap. "
                        + "The controller reports and drives the extent; the actuator resets it.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(_snap ? "Snap on" : "Snap off", () => SetState(() => _snap = !_snap)),
                            ControlButton("Jump to 0.4", () => _controller.JumpTo(0.4)),
                            ControlButton("Animate to 1.0", AnimateToTop),
                            ControlButton("Reset", () => DraggableScrollableActuator.Reset(Context)),
                        ]),
                    new Text($"extent: {_extent:0.00}", fontSize: 13),
                    new SizedBox(height: 320, child: BuildSheet()),
                ]);
        }

        private Widget BuildSheet()
        {
            var sheet = new DraggableScrollableSheet(
                initialChildSize: 0.5,
                minChildSize: 0.25,
                maxChildSize: 1.0,
                snap: _snap,
                snapSizes: [0.5],
                controller: _controller,
                builder: (_, scrollController) => new Container(
                    color: Colors.White,
                    child: new ListView(
                        controller: scrollController,
                        itemExtent: 44.0,
                        padding: new Thickness(12, 8),
                        children: BuildItems())));

            return new Container(
                color: Color.Parse("#FFE7EDF6"),
                child: new DraggableScrollableActuator(sheet));
        }

        private void HandleSizeChanged()
        {
            SetState(() => _extent = _controller.Size);
        }

        private void AnimateToTop()
        {
            _ = _controller.AnimateTo(1.0, TimeSpan.FromMilliseconds(300), Curves.EaseOut);
        }

        private static IReadOnlyList<Widget> BuildItems()
        {
            var items = new List<Widget>(24);
            for (int index = 0; index < 24; index++)
            {
                items.Add(new Align(
                    alignment: Alignment.CenterLeft,
                    child: new Text($"Item {index}", fontSize: 15)));
            }

            return items;
        }

        private static Widget ControlButton(string label, Action onPressed)
        {
            return new TextButton(new Text(label), onPressed);
        }
    }
}
