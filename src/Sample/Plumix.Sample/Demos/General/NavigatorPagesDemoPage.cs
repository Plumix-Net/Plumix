using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/navigator_pages_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class NavigatorPagesDemoPage : StatefulWidget
{
    public override State CreateState() => new NavigatorPagesDemoPageState();
}

internal sealed class NavigatorPagesDemoPageState : State
{
    private readonly List<Page> _pages = [new SampleDeclarativePage("Home", 0)];
    private int _nextIndex = 1;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 8,
            children:
            [
                new Text("Navigator.Pages demo", fontSize: 20, color: Colors.Black),
                new Text(
                    "The page list owns the history: pushing appends a page, popping asks OnDidRemovePage "
                    + "to drop it.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Text($"pages: {string.Join(", ", _pages.Select(page => page.Name))}", fontSize: 12),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildAction("Add page", AddPage, Color.Parse("#FFE8F5E9")),
                        BuildAction("Remove top page", RemoveTopPage, Color.Parse("#FFFFF3E0")),
                    ]),
                new SizedBox(
                    height: 220,
                    child: new ColoredBox(
                        color: Color.Parse("#FFFAFAFA"),
                        child: new Navigator(
                            pages: [.. _pages],
                            onDidRemovePage: HandleDidRemovePage))),
            ]);
    }

    private void AddPage()
    {
        SetState(() =>
        {
            _pages.Add(new SampleDeclarativePage($"Page {_nextIndex}", _nextIndex));
            _nextIndex += 1;
        });
    }

    private void RemoveTopPage()
    {
        if (_pages.Count <= 1)
        {
            return;
        }

        SetState(() => _pages.RemoveAt(_pages.Count - 1));
    }

    private void HandleDidRemovePage(Page page)
    {
        SetState(() => _pages.Remove(page));
    }

    private static Widget BuildAction(string label, System.Action onTap, Color background)
    {
        return new CounterTapButton(
            label: label,
            onTap: onTap,
            background: background,
            foreground: Colors.Black,
            fontSize: 12,
            padding: new Thickness(10, 8));
    }
}

/// <summary>One entry of the declarative page list; its key keeps the same route across list updates.</summary>
internal sealed record SampleDeclarativePage : Page
{
    private readonly int _index;

    public SampleDeclarativePage(string label, int index)
        : base(key: new ValueKey<int>(index), name: label)
    {
        _index = index;
    }

    public override Route CreateRoute(BuildContext context) => new SampleDeclarativePageRoute(this, _index);
}

internal sealed class SampleDeclarativePageRoute : PageRoute
{
    private readonly int _index;

    public SampleDeclarativePageRoute(SampleDeclarativePage page, int index) : base(settings: page)
    {
        _index = index;
    }

    public override Widget BuildPage(BuildContext context)
    {
        return new ColoredBox(
            color: _index % 2 == 0 ? Color.Parse("#FFE3F2FD") : Color.Parse("#FFF1F8E9"),
            child: new Center(
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 8,
                    children:
                    [
                        new Text(Settings.Name ?? string.Empty, fontSize: 18, color: Colors.Black),
                        new Text($"canPop: {ModalRoute.CanPopOf(context)}", fontSize: 12, color: Colors.DimGray),
                        new CounterTapButton(
                            label: "Pop this page",
                            onTap: () => Navigator.Of(context).MaybePop(),
                            background: Color.Parse("#FFFFFFFF"),
                            foreground: Colors.Black,
                            fontSize: 12,
                            padding: new Thickness(10, 6)),
                    ])));
    }
}
