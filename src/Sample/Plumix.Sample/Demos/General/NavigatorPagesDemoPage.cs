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
                        BuildAction("Add page", AddPage, new Color(0xFFE8F5E9)),
                        BuildAction("Remove top page", RemoveTopPage, new Color(0xFFFFF3E0)),
                    ]),
                new SizedBox(
                    height: 220,
                    child: new ColoredBox(
                        color: new Color(0xFFFAFAFA),
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
            color: _index % 2 == 0 ? new Color(0xFFE3F2FD) : new Color(0xFFF1F8E9),
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
                            // Qualified: `Route.Navigator` shadows the widget type inside a Route.
                            onTap: () => Plumix.Widgets.Navigator.Of(context).MaybePop(),
                            background: new Color(0xFFFFFFFF),
                            foreground: Colors.Black,
                            fontSize: 12,
                            padding: new Thickness(10, 6)),
                    ])));
    }
}
