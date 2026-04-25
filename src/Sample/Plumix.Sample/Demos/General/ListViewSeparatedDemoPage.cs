using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/list_view_separated_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ListViewSeparatedDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ListView.Separated", fontSize: 20, color: Colors.Black),
                new Text(
                    "Uses separate builders for rows and separators.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Expanded(
                    child: ListView.Separated(
                        itemCount: 30,
                        padding: new Thickness(12),
                        itemBuilder: (_, index) => new SeparatedListItem(index),
                        separatorBuilder: (_, index) => new Container(
                            height: 4,
                            color: index % 2 == 0
                                ? Color.Parse("#FFDCE3ED")
                                : Color.Parse("#FFE9EEF5")),
                        addAutomaticKeepAlives: false)),
            ]);
    }
}

internal sealed class SeparatedListItem : StatefulWidget
{
    public SeparatedListItem(int index, Key? key = null) : base(key)
    {
        Index = index;
    }

    public int Index { get; }

    public override State CreateState()
    {
        return new SeparatedListItemState(Index);
    }
}

internal sealed class SeparatedListItemState : State
{
    private readonly int _index;
    private int _taps;

    public SeparatedListItemState(int index)
    {
        _index = index;
    }

    public override Widget Build(BuildContext context)
    {
        return new CounterTapButton(
            label: $"item #{_index} taps={_taps}",
            onTap: () => SetState(() => _taps += 1),
            background: _index % 2 == 0 ? Color.Parse("#FFEFF5FF") : Colors.White,
            foreground: Colors.Black,
            fontSize: 13,
            padding: new Thickness(10, 8));
    }
}
