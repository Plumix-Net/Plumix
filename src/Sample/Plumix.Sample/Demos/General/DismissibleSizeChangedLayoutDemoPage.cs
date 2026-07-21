using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/dismissible_size_changed_layout_demo_page.dart

public sealed class DismissibleSizeChangedLayoutDemoPage : StatefulWidget
{
    public override State CreateState() => new DismissibleSizeChangedLayoutDemoPageState();
}

internal sealed class DismissibleSizeChangedLayoutDemoPageState : State
{
    private readonly List<int> _items = [1, 2, 3];
    private int _sizeNotifications;
    private bool _expanded;
    private bool _rightToLeft;

    public override Widget Build(BuildContext context)
    {
        TextDirection textDirection = _rightToLeft ? TextDirection.Rtl : TextDirection.Ltr;
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Dismissible + SizeChangedLayoutNotifier", fontSize: 20, color: Colors.Black),
                new Text(
                    "Swipe rows in either direction. The resize probe reports notifications only after its " +
                    "established layout size changes.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _rightToLeft ? "Direction: RTL" : "Direction: LTR",
                            () => SetState(() => _rightToLeft = !_rightToLeft)),
                        BuildButton(
                            _expanded ? "Shrink probe" : "Grow probe",
                            () => SetState(() => _expanded = !_expanded)),
                        BuildButton(
                            "Reset rows",
                            () => SetState(() =>
                            {
                                _items.Clear();
                                _items.AddRange([1, 2, 3]);
                            })),
                    ]),
                new NotificationListener<SizeChangedLayoutNotification>(
                    onNotification: _ =>
                    {
                        Scheduler.AddPostFrameCallback(_ =>
                        {
                            if (Mounted)
                            {
                                SetState(() => _sizeNotifications++);
                            }
                        });
                        return false;
                    },
                    child: new Align(
                        alignment: Alignment.CenterLeft,
                        child: new SizeChangedLayoutNotifier(
                            child: new Container(
                                width: _expanded ? 320 : 190,
                                height: _expanded ? 64 : 44,
                                color: Color.Parse("#FFDCEAF7"),
                                alignment: Alignment.Center,
                                child: new Text(
                                    $"layout notifications: {_sizeNotifications}",
                                    color: Color.Parse("#FF174A72")))))),
                new Expanded(
                    child: new Directionality(
                        textDirection,
                        new ListView(
                            children: _items
                                .Select(BuildDismissibleRow)
                                .ToArray()))),
            ]);
    }

    private Widget BuildDismissibleRow(int item)
    {
        return new Padding(
            insets: new Thickness(0, 0, 0, 8),
            child: new Dismissible(
                key: new ValueKey<int>(item),
                child: BuildRowSurface($"Swipe row {item}", Color.Parse("#FFF4F6F8")),
                background: BuildRowBackground("START →", Alignment.CenterLeft, Color.Parse("#FF2E7D32")),
                secondaryBackground: BuildRowBackground(
                    "← END",
                    Alignment.CenterRight,
                    Color.Parse("#FFC62828")),
                crossAxisEndOffset: 0.08,
                onDismissed: _ => SetState(() => _items.Remove(item))));
    }

    private static Widget BuildRowSurface(string label, Color color)
    {
        return new Container(
            height: 58,
            color: color,
            padding: new Thickness(16, 0),
            alignment: Alignment.CenterLeft,
            child: new Text(label, color: Colors.Black));
    }

    private static Widget BuildRowBackground(string label, Alignment alignment, Color color)
    {
        return new Container(
            height: 58,
            color: color,
            padding: new Thickness(16, 0),
            alignment: alignment,
            child: new Text(label, color: Colors.White));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 130,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse("#FFDCE3ED"),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(8, 7)));
    }
}
