using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/overflow_indicator_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class OverflowIndicatorDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new OverflowIndicatorDemoPageState();
    }
}

internal sealed class OverflowIndicatorDemoPageState : State
{
    private bool _horizontal;
    private int _itemCount = 6;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("RenderFlex overflow indicator", fontSize: 20, color: Colors.Black),
                new Text(
                    "Intentionally overflows a Flex container to show Plumix.Sample-like yellow/black debug stripes and overflow labels.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Vertical", () => SetHorizontal(false), 90, "#FFDCE3ED"),
                        BuildButton("Horizontal", () => SetHorizontal(true), 94, "#FFDCE3ED"),
                        BuildButton("- items", DecreaseItems, 84, "#FFE9F5EC"),
                        BuildButton("+ items", IncreaseItems, 84, "#FFE9F5EC"),
                    ]),
                new Text(
                    $"axis={(_horizontal ? "horizontal" : "vertical")}, itemCount={_itemCount} (overflow expected)",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 280,
                    height: 220,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new Center(
                        child: BuildProbe())),
            ]);
    }

    private Widget BuildProbe()
    {
        if (_horizontal)
        {
            return new Container(
                width: 240,
                height: 120,
                color: Colors.White,
                padding: new Thickness(8),
                child: new Row(
                    spacing: 8,
                    children:
                    [
                        ..Enumerable.Range(0, _itemCount).Select(BuildHorizontalTile),
                    ]));
        }

        return new Container(
            width: 240,
            height: 120,
            color: Colors.White,
            padding: new Thickness(8),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    ..Enumerable.Range(0, _itemCount).Select(BuildVerticalTile),
                ]));
    }

    private Widget BuildHorizontalTile(int index)
    {
        return new Container(
            width: 72,
            height: 86,
            color: index % 2 == 0 ? Color.Parse("#FFBBDEFB") : Color.Parse("#FFC8E6C9"),
            child: new Center(
                child: new Text($"tile {index}", fontSize: 12, color: Colors.Black)));
    }

    private Widget BuildVerticalTile(int index)
    {
        return new Container(
            height: 34,
            color: index % 2 == 0 ? Color.Parse("#FFBBDEFB") : Color.Parse("#FFC8E6C9"),
            child: new Center(
                child: new Text($"row {index}", fontSize: 12, color: Colors.Black)));
    }

    private Widget BuildButton(string label, Action onTap, double width, string colorHex)
    {
        return new SizedBox(
            width: width,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse(colorHex),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }

    private void SetHorizontal(bool value)
    {
        SetState(() => _horizontal = value);
    }

    private void IncreaseItems()
    {
        SetState(() =>
        {
            _itemCount = Math.Min(12, _itemCount + 1);
        });
    }

    private void DecreaseItems()
    {
        SetState(() =>
        {
            _itemCount = Math.Max(3, _itemCount - 1);
        });
    }
}
