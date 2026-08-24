using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/segmented_control_demo_page.dart

public sealed class CupertinoSegmentedControlDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoSegmentedControlDemoPageState();
}

internal sealed class CupertinoSegmentedControlDemoPageState : State
{
    private string? _selected = "day";
    private bool _disableWeek;
    private int _changes;

    public override Widget Build(BuildContext context)
    {
        IReadOnlyDictionary<string, Widget> segments = new Dictionary<string, Widget>
        {
            ["day"] = new Text("Day"),
            ["week"] = new Text("Week"),
            ["month"] = new Text("Month"),
        };
        IReadOnlySet<string> disabled = _disableWeek
            ? new HashSet<string> { "week" }
            : new HashSet<string>();

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 14.0,
            children:
            [
                new Text("Cupertino segmented control", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Controlled selection, sliding thumb, disabled segments, custom colors, and arrow-key focus.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new CupertinoSlidingSegmentedControl<string>(
                    children: segments,
                    groupValue: _selected,
                    disabledChildren: disabled,
                    proportionalWidth: true,
                    onValueChanged: value => Select(value)),
                new CupertinoSegmentedControl<string>(
                    children: segments,
                    groupValue: _selected,
                    disabledChildren: disabled,
                    onValueChanged: Select),
                new CupertinoSegmentedControl<string>(
                    children: segments,
                    groupValue: _selected,
                    disabledChildren: disabled,
                    selectedColor: Color.Parse("#FF00695C"),
                    unselectedColor: Color.Parse("#FFF4FBF8"),
                    borderColor: Color.Parse("#FF00695C"),
                    pressedColor: Color.Parse("#3300695C"),
                    disabledColor: Color.Parse("#FFCFD8D5"),
                    disabledTextColor: Color.Parse("#FF78908A"),
                    padding: EdgeInsetsGeometry.Zero,
                    onValueChanged: Select),
                new Row(
                    spacing: 10.0,
                    children:
                    [
                        BuildAction(
                            _disableWeek ? "Enable Week" : "Disable Week",
                            () => SetState(() => _disableWeek = !_disableWeek)),
                        BuildAction("Clear", () => SetState(() => _selected = null)),
                    ]),
                new Text(
                    $"selected={_selected ?? "none"}, changes={_changes}, weekDisabled={_disableWeek}",
                    fontSize: 13.0,
                    color: Color.Parse("#FF455A64")),
            ]);
    }

    private void Select(string? value)
    {
        SetState(() =>
        {
            _selected = value;
            _changes++;
        });
    }

    private static Widget BuildAction(string label, Action onTap)
    {
        return new GestureDetector(
            behavior: HitTestBehavior.Opaque,
            onTap: onTap,
            child: new Container(
                width: 126.0,
                padding: new Thickness(10.0, 8.0),
                decoration: new BoxDecoration(
                    Color: Color.Parse("#FFE8F4F1"),
                    BorderRadius: BorderRadius.Circular(8.0)),
                child: new Center(child: new Text(label, color: Color.Parse("#FF00695C")))));
    }
}
