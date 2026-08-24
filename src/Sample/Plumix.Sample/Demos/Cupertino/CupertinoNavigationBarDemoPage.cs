using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_navigation_bar_demo_page.dart (exact sample parity)

public sealed class CupertinoNavigationBarDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoNavigationBarDemoPageState();
}

internal sealed class CupertinoNavigationBarDemoPageState : State
{
    private bool _searchActive;

    public override Widget Build(BuildContext context)
    {
        return new CupertinoTheme(
            data: new CupertinoThemeData(),
            child: new CupertinoPageScaffold(
                child: new CustomScrollView(
                    slivers:
                    [
                        CupertinoSliverNavigationBar.Search(
                            searchField: new CupertinoSearchTextField(),
                            largeTitle: new Text("Nav bars"),
                            stretch: true,
                            onSearchableBottomTap: active => SetState(() => _searchActive = active)),
                        new SliverToBoxAdapter(new Builder(BuildContent)),
                    ])));
    }

    private Widget BuildContent(BuildContext context)
    {
        Color label = CupertinoDynamicColor.Resolve(CupertinoColors.Label, context);
        Color secondaryLabel = CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context);

        return new Padding(
            new Thickness(16.0),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10.0,
                children:
                [
                    new Text(
                        _searchActive
                            ? "Search is active — Cancel restores the collapsed bar."
                            : "Scroll to collapse the large title, overscroll to stretch it, "
                              + "or tap the field to search.",
                        fontSize: 14.0,
                        color: secondaryLabel),
                    BuildAction(
                        "Push detail page (auto middle + back label)",
                        () => PushDetail(context),
                        Color.Parse("#FFE9F0FF")),
                    BuildAction(
                        "Push page with a long title ('Back' fallback)",
                        () => PushLongTitle(context),
                        Color.Parse("#FFEAE4FF")),
                    .. Enumerable.Range(1, 20)
                        .Select(index => (Widget)new Text($"Row {index}", fontSize: 14.0, color: label)),
                ]));
    }

    private static void PushDetail(BuildContext context)
    {
        Navigator.Of(context).Push(new CupertinoPageRoute<object?>(
            title: "Details",
            builder: _ => new CupertinoPageScaffold(
                navigationBar: new CupertinoNavigationBar(),
                child: new Center(
                    child: new Builder(routeContext => new Text(
                        "The static bar implies its middle title and the back label "
                        + "from the route titles.",
                        fontSize: 14.0,
                        textAlign: TextAlign.Center,
                        color: CupertinoDynamicColor.Resolve(CupertinoColors.Label, routeContext)))))));
    }

    private static void PushLongTitle(BuildContext context)
    {
        Navigator.Of(context).Push(new CupertinoPageRoute<object?>(
            title: "Extended configuration options",
            builder: _ => new CupertinoPageScaffold(
                child: new CustomScrollView(
                    slivers:
                    [
                        new CupertinoSliverNavigationBar(),
                        new SliverToBoxAdapter(new Padding(
                            new Thickness(16.0),
                            child: new Builder(routeContext => new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 10.0,
                                children:
                                [
                                    new Text(
                                        "This title is over 12 characters, so the next page's "
                                        + "back label falls back to 'Back'.",
                                        fontSize: 14.0,
                                        color: CupertinoDynamicColor.Resolve(
                                            CupertinoColors.Label,
                                            routeContext)),
                                    BuildAction(
                                        "Push detail from here (shows 'Back')",
                                        () => PushDetail(routeContext),
                                        Color.Parse("#FFE8F4E8")),
                                ])))),
                    ]))));
    }

    private static Widget BuildAction(string label, Action onTap, Color background)
    {
        return new CounterTapButton(
            label: label,
            onTap: onTap,
            background: background,
            foreground: Colors.Black,
            fontSize: 12.0,
            padding: new Thickness(10.0, 8.0));
    }
}
