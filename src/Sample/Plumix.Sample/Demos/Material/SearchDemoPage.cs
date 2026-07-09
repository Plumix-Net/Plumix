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
    private bool _enabled = true;
    private bool _useFullScreen;
    private bool _useThemeOverrides;
    private string _selected = "none";
    private string _status = "idle";

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var pageTheme = baseTheme with
        {
            SearchBarTheme = _useThemeOverrides
                ? new SearchBarThemeData(
                    BackgroundColor: MaterialStateProperty<Color?>.All(Color.Parse("#FFEAF6F7")),
                    Elevation: MaterialStateProperty<double?>.All(1),
                    Shape: MaterialStateProperty<ShapeBorder?>.All(ShapeBorder.RoundedRectangle(18)),
                    Side: MaterialStateProperty<BorderSide?>.All(new BorderSide(Color.Parse("#FF00695C"))),
                    Padding: MaterialStateProperty<Thickness?>.All(new Thickness(12, 0)),
                    Constraints: new BoxConstraints(MinWidth: 280, MaxWidth: 520, MinHeight: 52),
                    HintStyle: MaterialStateProperty<TextStyle?>.All(new TextStyle(Color: Color.Parse("#FF00695C"))))
                : new SearchBarThemeData(),
            SearchViewTheme = _useThemeOverrides
                ? new SearchViewThemeData(
                    BackgroundColor: Color.Parse("#FFF5FBFA"),
                    Elevation: 0,
                    Shape: ShapeBorder.RoundedRectangle(22, new BorderSide(Color.Parse("#FF80CBC4"))),
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
                        new Text("SearchBar + SearchAnchor", fontSize: 20, color: Colors.Black),
                        new Text(
                            "Controller-backed search view with suggestions, open/close callbacks, M3 defaults, and theme precedence probes.",
                            fontSize: 14,
                            color: Color.Parse("#8A000000")),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton(_enabled ? "Enabled" : "Disabled", () => SetState(() => _enabled = !_enabled)),
                                ControlButton(_useFullScreen ? "Full screen" : "Docked view", () => SetState(() => _useFullScreen = !_useFullScreen)),
                                ControlButton(_useThemeOverrides ? "Theme on" : "Theme off", () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
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
}
