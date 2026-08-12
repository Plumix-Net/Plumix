using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/search_demo_page.dart

public sealed class SearchDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new SearchDemoPageState();
    }
}

internal sealed class SearchDemoPageState : State
{
    private static readonly IReadOnlyList<string> SearchTerms =
    [
        "Widget",
        "Element",
        "RenderObject",
        "SearchBar",
        "SearchAnchor",
        "Navigator",
        "Material",
        "TextField",
        "ThemeData",
        "Plumix",
    ];

    private readonly SearchController _controller = new();
    private readonly TextEditingController _standaloneController = new("Standalone");
    private SearchDelegate<string>? _legacyDelegate;
    private bool _enabled = true;
    private bool _useFullScreen;
    private bool _useThemeOverrides;
    private string _selected = "none";
    private string _status = "idle";

    public override void InitState()
    {
        _legacyDelegate = new TermSearchDelegate(SearchTerms, value => SetState(() =>
        {
            _selected = value;
            _status = $"legacy selected: {value}";
        }));
    }

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var pageTheme = baseTheme with
        {
            SearchBarTheme = _useThemeOverrides
                ? new SearchBarThemeData(
                    BackgroundColor: MaterialStateProperty<Color?>.All(Color.Parse("#FFEAF6F7")),
                    Elevation: MaterialStateProperty<double?>.All(1),
                    Shape: MaterialStateProperty<ShapeBorder?>.All(new RoundedRectangleBorder(borderRadius:
                        Plumix.Rendering.BorderRadius.Circular(18))),
                    Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Color.Parse("#FF00695C"))),
                    Padding: MaterialStateProperty<Thickness?>.All(new Thickness(12, 0)),
                    Constraints: new BoxConstraints(MinWidth: 280, MaxWidth: 520, MinHeight: 52),
                    HintStyle: MaterialStateProperty<TextStyle?>.All(new TextStyle(Color: Color.Parse("#FF00695C"))))
                : new SearchBarThemeData(),
            SearchViewTheme = _useThemeOverrides
                ? new SearchViewThemeData(
                    BackgroundColor: Color.Parse("#FFF5FBFA"),
                    Elevation: 0,
                    Shape: new RoundedRectangleBorder(
                        new BorderSide(Color.Parse("#FF80CBC4")), Plumix.Rendering.BorderRadius.Circular(22)),
                    HeaderHeight: 64,
                    BarPadding: new Thickness(12, 0),
                    DividerColor: Color.Parse("#FF80CBC4"),
                    Constraints: new BoxConstraints(MinWidth: 360, MinHeight: 260, MaxWidth: 560, MaxHeight: 420),
                    Padding: new Thickness(16))
                : new SearchViewThemeData()
        };

        return new Theme(
            data: pageTheme,
            child: new SingleChildScrollView(
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 12,
                    children:
                    [
                        new Text("SearchBar + SearchAnchor + SearchDelegate", fontSize: 20, color: Colors.Black),
                        new Text(
                            "Controller-backed search plus a legacy route with search-keyboard metadata "
                            + "and fade animation.",
                            fontSize: 14,
                            color: Color.Parse("#8A000000")),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton(_enabled ? "Enabled" : "Disabled", () => SetState(() => _enabled = !_enabled)),
                                ControlButton(_useFullScreen ? "Full screen" : "Docked view", () => SetState(() => _useFullScreen = !_useFullScreen)),
                                ControlButton(_useThemeOverrides ? "Theme on" : "Theme off", () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                                ControlButton("Legacy route", () => OpenLegacySearch(context)),
                            ]),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton("Open", () => _controller.OpenView()),
                                ControlButton("Clear", () => SetState(() =>
                                {
                                    _controller.Clear();
                                    _selected = "none";
                                    _status = "cleared";
                                })),
                            ]),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: SearchAnchor.Bar(
                                searchController: _controller,
                                barHintText: "Search framework terms",
                                viewHintText: "Type a Plumix concept",
                                isFullScreen: _useFullScreen,
                                enabled: _enabled,
                                shrinkWrap: true,
                                constraints: new BoxConstraints(MinWidth: 320, MaxWidth: 560, MinHeight: 56),
                                viewConstraints: new BoxConstraints(MinWidth: 360, MinHeight: 260, MaxWidth: 560, MaxHeight: 420),
                                barTrailing:
                                [
                                    new IconButton(
                                        icon: new Icon(Icons.Clear),
                                        onPressed: () => SetState(() =>
                                        {
                                            _controller.Clear();
                                            _status = "bar cleared";
                                        }))
                                ],
                                onOpen: () => SetState(() => _status = "opened"),
                                onClose: () => SetState(() => _status = "closed"),
                                onChanged: value => SetState(() => _status = $"changed: {FormatEmpty(value)}"),
                                onSubmitted: value => SetState(() => _status = $"submitted: {FormatEmpty(value)}"),
                                suggestionsBuilder: BuildSuggestions)),
                        new Text($"Selected: {_selected}", fontSize: 13),
                        new Text($"Controller text: {FormatEmpty(_controller.Text)}", fontSize: 13),
                        new Text($"Status: {_status}", fontSize: 13),
                        new Divider(),
                        new Text("Standalone SearchBar", fontSize: 18, color: Colors.Black),
                        new SearchBar(
                            controller: _standaloneController,
                            hintText: "Filter inside a page",
                            leading: new Icon(Icons.Search),
                            trailing:
                            [
                                new IconButton(
                                    icon: new Icon(Icons.Clear),
                                    onPressed: () => SetState(() => _standaloneController.Clear()))
                            ],
                            onTap: () => SetState(() => _status = "standalone tapped"),
                            onChanged: value => SetState(() => _status = $"standalone: {FormatEmpty(value)}"),
                            onSubmitted: value => SetState(() => _status = $"standalone submitted: {FormatEmpty(value)}")),
                    ])));
    }

    public override void Dispose()
    {
        _controller.Dispose();
        _standaloneController.Dispose();
        _legacyDelegate?.Dispose();
    }

    private IReadOnlyList<Widget> BuildSuggestions(BuildContext context, SearchController controller)
    {
        string query = controller.Text.Trim();
        var suggestions = new List<Widget>();
        foreach (string term in SearchTerms)
        {
            if (query.Length > 0 && !term.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            suggestions.Add(new ListTile(
                leading: new Icon(Icons.Search),
                title: new Text(term),
                subtitle: new Text($"Select {term}"),
                onTap: () => SetState(() =>
                {
                    _selected = term;
                    _status = $"selected: {term}";
                    controller.CloseView(term);
                })));
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add(new Padding(
                new Thickness(16),
                new Text($"No results for {query}", fontSize: 13, color: Colors.DimGray)));
        }

        return suggestions;
    }

    private static Widget ControlButton(string label, Action action)
    {
        return new TextButton(new Text(label, fontSize: 12), action);
    }

    private static string FormatEmpty(string value)
    {
        return string.IsNullOrEmpty(value) ? "empty" : value;
    }

    private async void OpenLegacySearch(BuildContext context)
    {
        var searchDelegate = _legacyDelegate;
        if (searchDelegate is null || searchDelegate.IsActive)
        {
            return;
        }

        _status = "legacy opened";
        string? result = await MaterialSearch.ShowSearch(context, searchDelegate, query: _controller.Text);
        if (result is not null && Mounted)
        {
            SetState(() => _status = $"legacy closed: {result}");
        }
    }

    private sealed class TermSearchDelegate : SearchDelegate<string>
    {
        private readonly IReadOnlyList<string> _terms;
        private readonly Action<string> _onSelected;

        public TermSearchDelegate(IReadOnlyList<string> terms, Action<string> onSelected) : base(
            searchFieldLabel: "Search framework terms",
            keyboardType: TextInputType.Text,
            textInputAction: TextInputAction.Search,
            autocorrect: false,
            enableSuggestions: true)
        {
            _terms = terms;
            _onSelected = onSelected;
        }

        public override Widget BuildSuggestions(BuildContext context) => BuildTerms(context, "Suggestions");

        public override Widget BuildResults(BuildContext context) => BuildTerms(context, "Results");

        public override Widget? BuildLeading(BuildContext context) => new FadeTransition(
            opacity: TransitionAnimation,
            child: new BackButton(onPressed: () => Navigator.MaybePop(context)));

        public override IReadOnlyList<Widget>? BuildActions(BuildContext context) =>
        [
            new Tooltip(
                message: MaterialLocalizations.Of(context).ClearButtonTooltip,
                child: new IconButton(
                    icon: new Icon(Icons.Clear),
                    onPressed: () =>
                    {
                        Query = string.Empty;
                        ShowSuggestions();
                    })),
        ];

        private Widget BuildTerms(BuildContext context, string label)
        {
            string query = Query.Trim();
            var children = new List<Widget> { new Text($"{label} for {FormatEmpty(query)}", fontSize: 13) };
            foreach (string term in _terms)
            {
                if (query.Length > 0 && !term.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                children.Add(new ListTile(
                    leading: new Icon(Icons.Search),
                    title: new Text(term),
                    onTap: () =>
                    {
                        if (label == "Suggestions")
                        {
                            Query = term;
                            ShowResults();
                        }
                        else
                        {
                            _onSelected(term);
                            Close(context, term);
                        }
                    }));
            }

            return new ListView(children: children);
        }
    }
}
