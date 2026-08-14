using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/ensure_visible_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class EnsureVisibleDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new EnsureVisibleDemoPageState();
    }
}

internal sealed class EnsureVisibleDemoPageState : State
{
    private const int ItemCount = 40;
    private const double ItemExtent = 56.0;

    private readonly ScrollController _outerController = new();
    private readonly ScrollController _innerController = new();
    private readonly Dictionary<int, BuildContext> _itemContexts = [];
    private double _alignment;
    private ScrollPositionAlignmentPolicy _policy = ScrollPositionAlignmentPolicy.Explicit;
    private string _status = "Pick a row to reveal.";

    public override void Dispose()
    {
        _outerController.Dispose();
        _innerController.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Scrollable.EnsureVisible", fontSize: 20, color: Colors.Black),
                new Text(
                    "The inner list is nested in an outer scroller, so a reveal walks both viewports.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new CounterTapButton(
                                label: $"Alignment {_alignment:0.0}",
                                onTap: CycleAlignment,
                                background: Colors.SteelBlue,
                                foreground: Colors.White,
                                fontSize: 13)),
                        new Expanded(
                            child: new CounterTapButton(
                                label: PolicyLabel,
                                onTap: CyclePolicy,
                                background: Colors.SlateGray,
                                foreground: Colors.White,
                                fontSize: 13)),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Reveal row 8",
                                onTap: () => Reveal(8),
                                background: Colors.SeaGreen,
                                foreground: Colors.White,
                                fontSize: 13)),
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Reveal row 30",
                                onTap: () => Reveal(30),
                                background: Colors.DarkOrange,
                                foreground: Colors.White,
                                fontSize: 13)),
                    ]),
                new Text(_status, fontSize: 13, color: Colors.Black),
                new Expanded(
                    child: ListView.Builder(
                        controller: _outerController,
                        itemCount: 3,
                        itemBuilder: BuildOuterSection)),
            ]);
    }

    private string PolicyLabel => _policy switch
    {
        ScrollPositionAlignmentPolicy.KeepVisibleAtStart => "Keep at start",
        ScrollPositionAlignmentPolicy.KeepVisibleAtEnd => "Keep at end",
        _ => "Explicit",
    };

    private void CycleAlignment()
    {
        SetState(() =>
        {
            _alignment = _alignment switch
            {
                < 0.25 => 0.5,
                < 0.75 => 1.0,
                _ => 0.0,
            };
        });
    }

    private void CyclePolicy()
    {
        SetState(() =>
        {
            _policy = _policy switch
            {
                ScrollPositionAlignmentPolicy.Explicit => ScrollPositionAlignmentPolicy.KeepVisibleAtStart,
                ScrollPositionAlignmentPolicy.KeepVisibleAtStart => ScrollPositionAlignmentPolicy.KeepVisibleAtEnd,
                _ => ScrollPositionAlignmentPolicy.Explicit,
            };
        });
    }

    private void Reveal(int index)
    {
        if (!_itemContexts.TryGetValue(index, out BuildContext itemContext))
        {
            SetState(() => _status = $"Row {index} is not built yet; scroll closer to it first.");
            return;
        }

        _ = Scrollable.EnsureVisible(
            itemContext,
            alignment: _alignment,
            duration: System.TimeSpan.FromMilliseconds(400),
            alignmentPolicy: _policy);
        SetState(() => _status = $"Revealed row {index} at alignment {_alignment:0.0} ({PolicyLabel}).");
    }

    private Widget BuildOuterSection(BuildContext context, int index)
    {
        if (index != 1)
        {
            return new Container(
                height: 160,
                color: index == 0 ? Colors.WhiteSmoke : Colors.Gainsboro,
                alignment: Alignment.Center,
                child: new Text(
                    index == 0 ? "Outer header" : "Outer footer",
                    fontSize: 15,
                    color: Colors.DimGray));
        }

        return new Container(
            height: 240,
            color: Colors.White,
            child: ListView.Builder(
                controller: _innerController,
                itemCount: ItemCount,
                itemExtent: ItemExtent,
                itemBuilder: BuildRow));
    }

    private Widget BuildRow(BuildContext context, int index)
    {
        _itemContexts[index] = context;
        return new Container(
            color: index % 2 == 0 ? Colors.White : Colors.AliceBlue,
            alignment: Alignment.CenterLeft,
            padding: new Thickness(12, 0, 12, 0),
            child: new Text($"Row {index}", fontSize: 14, color: Colors.Black));
    }
}
