using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/list_view_reverse_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class ListViewReverseDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new ListViewReverseDemoPageState();
    }
}

internal sealed class ListViewReverseDemoPageState : State
{
    private readonly List<int> _messages = [..Enumerable.Range(1, 20)];
    private int _nextMessageId = 21;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ListView reverse=true", fontSize: 20, color: Colors.Black),
                new Text(
                    "Direction is inverted; drag/pointer behavior follows Flutter axisDirection mapping.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Push message",
                                onTap: () => SetState(() => _messages.Add(_nextMessageId++)),
                                background: Colors.SteelBlue,
                                foreground: Colors.White,
                                fontSize: 13)),
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Pop message",
                                onTap: _messages.Count == 0
                                    ? null
                                    : () => SetState(() => _messages.RemoveAt(_messages.Count - 1)),
                                background: Colors.IndianRed,
                                foreground: Colors.White,
                                fontSize: 13)),
                    ]),
                new Expanded(
                    child: ListView.Builder(
                        itemCount: _messages.Count,
                        reverse: true,
                        itemExtent: 44,
                        padding: new Thickness(12),
                        itemBuilder: (_, index) =>
                        {
                            var id = _messages[index];
                            return new Container(
                                color: id % 2 == 0 ? Color.Parse("#FFFFF3E0") : Colors.White,
                                padding: new Thickness(12, 8),
                                child: new Text($"message #{id}", fontSize: 13, color: Colors.Black));
                        },
                        addAutomaticKeepAlives: false)),
            ]);
    }
}
