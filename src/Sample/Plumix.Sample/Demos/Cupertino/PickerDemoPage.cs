using System;
using System.Collections.Generic;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/picker_demo_page.dart
// (exact sample parity)

namespace Plumix;

public sealed class CupertinoPickerDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoPickerDemoPageState();
}

internal sealed class CupertinoPickerDemoPageState : State
{
    private static readonly IReadOnlyList<string> Fruits = ["Apple", "Banana", "Cherry", "Pear"];
    private static readonly IReadOnlyList<string> Sizes = ["Small", "Medium", "Large", "Extra large"];

    private readonly FixedExtentScrollController _fruitController = new(initialItem: 1);
    private readonly FixedExtentScrollController _sizeController = new(initialItem: 1);
    private int _fruitIndex = 1;
    private int _sizeIndex = 1;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino picker", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Scroll or tap a row. The left wheel loops; the right wheel uses the lazy builder API.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Text(
                    $"Fruit: {Fruits[_fruitIndex]} · Size: {Sizes[_sizeIndex]}",
                    fontSize: 13.0,
                    color: Color.Parse("#FF607D8B")),
                new Row(
                    spacing: 16.0,
                    children:
                    [
                        new Expanded(BuildWheelColumn(
                            "Looping list",
                            new CupertinoPicker(
                                itemExtent: 40.0,
                                onSelectedItemChanged: SelectFruit,
                                children: BuildFruitChildren(),
                                scrollController: _fruitController,
                                looping: true))),
                        new Expanded(BuildWheelColumn(
                            "Builder + magnifier",
                            CupertinoPicker.Builder(
                                itemExtent: 40.0,
                                onSelectedItemChanged: SelectSize,
                                itemBuilder: (_, index) => new Text(Sizes[index], textAlign: TextAlign.Center),
                                selectionOverlay: new CupertinoPickerDefaultSelectionOverlay(
                                    capStartEdge: false),
                                childCount: Sizes.Count,
                                useMagnifier: true,
                                magnification: 1.12,
                                scrollController: _sizeController))),
                    ]),
            ]);
    }

    public override void Dispose()
    {
        _fruitController.Dispose();
        _sizeController.Dispose();
        base.Dispose();
    }

    private static IReadOnlyList<Widget> BuildFruitChildren()
    {
        var children = new List<Widget>(Fruits.Count);
        foreach (string fruit in Fruits)
        {
            children.Add(new Text(fruit, textAlign: TextAlign.Center));
        }

        return children;
    }

    private static Widget BuildWheelColumn(string label, Widget picker)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 6.0,
            children:
            [
                new Text(label, fontSize: 13.0, color: Color.Parse("#FF37474F"), textAlign: TextAlign.Center),
                new SizedBox(height: 180.0, child: picker),
            ]);
    }

    private void SelectFruit(int index)
    {
        SetState(() => _fruitIndex = ((index % Fruits.Count) + Fruits.Count) % Fruits.Count);
    }

    private void SelectSize(int index)
    {
        SetState(() => _sizeIndex = index);
    }
}
