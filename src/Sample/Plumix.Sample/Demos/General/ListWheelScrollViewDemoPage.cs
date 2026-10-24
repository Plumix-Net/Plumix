using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/list_wheel_scroll_view_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ListWheelScrollViewDemoPage : StatefulWidget
{
    public override State CreateState() => new ListWheelScrollViewDemoPageState();

    private sealed class ListWheelScrollViewDemoPageState : State
    {
        private const int ItemCount = 24;

        private FixedExtentScrollController? _controller;
        private int _selectedItem = 6;
        private bool _useMagnifier = true;

        public override void InitState() => _controller = new FixedExtentScrollController(initialItem: _selectedItem);

        public override void Dispose()
        {
            _controller?.Dispose();
            _controller = null;

            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("ListWheelScrollView", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                    new Text(
                        "Children are laid out lazily on a cylinder; FixedExtentScrollPhysics snaps to whole items.",
                        style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                    new Expanded(
                        child: new Row(
                            children:
                            [
                                new Expanded(
                                    child: new ListWheelScrollView(
                                        controller: _controller,
                                        itemExtent: 48,
                                        physics: new FixedExtentScrollPhysics(),
                                        useMagnifier: _useMagnifier,
                                        magnification: 1.3,
                                        overAndUnderCenterOpacity: 0.6,
                                        onSelectedItemChanged: item => SetState(() => _selectedItem = item),
                                        children: Enumerable.Range(0, ItemCount)
                                            .Select(index => (Widget)new Center(
                                                child: new Text(
                                                    $"item #{index}",
                                                    style: new TextStyle(
                                                        FontSize: 18,
                                                        Color: Colors.Black))))
                                            .ToArray())),
                                new Expanded(
                                    child: new ListWheelScrollView(
                                        itemExtent: 40,
                                        diameterRatio: 1.2,
                                        offAxisFraction: -0.5,
                                        squeeze: 1.2,
                                        childDelegate: new ListWheelChildLoopingListDelegate(
                                            Enumerable.Range(0, 12)
                                                .Select(index => (Widget)new Center(
                                                    child: new Text(
                                                        $"loop {index}",
                                                        style: new TextStyle(
                                                            FontSize: 16,
                                                            Color: MaterialColors.Black54))))
                                                .ToArray()))),
                            ])),
                    new Row(
                        mainAxisAlignment: MainAxisAlignment.Center,
                        spacing: 12,
                        children:
                        [
                            new TextButton(
                                onPressed: () => _controller?.AnimateToItem(
                                    Math.Max(0, _selectedItem - 1),
                                    TimeSpan.FromMilliseconds(300),
                                    Curves.Ease),
                                child: new Text("Previous")),
                            new Text(
                                $"selected item {_selectedItem}",
                                style: new TextStyle(
                                    FontSize: 14,
                                    Color: Colors.Black)),
                            new TextButton(
                                onPressed: () => _controller?.AnimateToItem(
                                    Math.Min(ItemCount - 1, _selectedItem + 1),
                                    TimeSpan.FromMilliseconds(300),
                                    Curves.Ease),
                                child: new Text("Next")),
                            new TextButton(
                                onPressed: () => SetState(() => _useMagnifier = !_useMagnifier),
                                child: new Text(_useMagnifier ? "Magnifier on" : "Magnifier off")),
                        ]),
                ]);
        }
    }
}
