using System;
using System.Collections.Generic;
using Avalonia;
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
    private int _demoIndex;
    private int _fruitIndex = 1;
    private int _sizeIndex = 1;
    private DateTime _selectedDateTime = new(2025, 6, 16, 10, 30, 0);
    private TimeSpan _selectedDuration = new(1, 20, 30);

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino picker", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Compare the base wheel, bounded date/time, and duration picker APIs.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8.0,
                    children:
                    [
                        new Expanded(BuildModeButton("Wheel", 0)),
                        new Expanded(BuildModeButton("Date + time", 1)),
                        new Expanded(BuildModeButton("Timer", 2)),
                    ]),
                new Text(BuildSummary(), fontSize: 13.0, color: Color.Parse("#FF607D8B")),
                BuildActivePicker(),
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

    private Widget BuildModeButton(string label, int index)
    {
        return new CupertinoButton(
            child: new Text(label, textAlign: TextAlign.Center),
            onPressed: () => SetState(() => _demoIndex = index),
            color: _demoIndex == index ? CupertinoColors.ActiveBlue : CupertinoColors.SystemGrey5,
            minSize: 36.0,
            padding: new Thickness(8.0, 6.0));
    }

    private Widget BuildActivePicker()
    {
        return _demoIndex switch
        {
            1 => new SizedBox(
                height: 216.0,
                child: new CupertinoDatePicker(
                    onDateTimeChanged: SelectDateTime,
                    initialDateTime: _selectedDateTime,
                    minimumDate: new DateTime(2025, 6, 13, 8, 0, 0),
                    maximumDate: new DateTime(2025, 6, 20, 18, 0, 0),
                    minuteInterval: 5,
                    showTimeSeparator: true,
                    selectableDayPredicate: date => date.DayOfWeek is not DayOfWeek.Saturday
                        and not DayOfWeek.Sunday)),
            2 => new SizedBox(
                height: 216.0,
                child: new CupertinoTimerPicker(
                    onTimerDurationChanged: SelectDuration,
                    initialTimerDuration: _selectedDuration,
                    minuteInterval: 5,
                    secondInterval: 10)),
            _ => new Row(
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
        };
    }

    private string BuildSummary()
    {
        return _demoIndex switch
        {
            1 => $"Selected: {_selectedDateTime:ddd, MMM d · HH:mm}",
            2 => $"Duration: {_selectedDuration:hh\\:mm\\:ss}",
            _ => $"Fruit: {Fruits[_fruitIndex]} · Size: {Sizes[_sizeIndex]}",
        };
    }

    private void SelectFruit(int index)
    {
        SetState(() => _fruitIndex = ((index % Fruits.Count) + Fruits.Count) % Fruits.Count);
    }

    private void SelectSize(int index)
    {
        SetState(() => _sizeIndex = index);
    }

    private void SelectDateTime(DateTime value)
    {
        SetState(() => _selectedDateTime = value);
    }

    private void SelectDuration(TimeSpan value)
    {
        SetState(() => _selectedDuration = value);
    }
}
