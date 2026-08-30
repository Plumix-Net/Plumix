using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/autocomplete_demo_page.dart

public sealed class AutocompleteDemoPage : StatefulWidget
{
    public override State CreateState() => new AutocompleteDemoPageState();
}

internal sealed class AutocompleteDemoPageState : State
{
    private static readonly IReadOnlyList<string> FrameworkTerms =
    [
        "Widget",
        "Element",
        "RenderObject",
        "BuildContext",
        "StatefulWidget",
        "InheritedWidget",
        "Navigator",
        "Autocomplete",
        "RawAutocomplete",
    ];

    private readonly TextEditingController _materialController = new();
    private readonly FocusNode _materialFocusNode = new();
    private readonly TextEditingController _rawController = new();
    private readonly FocusNode _rawFocusNode = new();
    private OptionsViewOpenDirection _openDirection = OptionsViewOpenDirection.Down;
    private bool _useMaterial3 = true;
    private string _materialSelection = "none";
    private string _rawSelection = "none";

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("Autocomplete + RawAutocomplete", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Material defaults and a route-free raw options portal with shared filtering, keyboard " +
                        "highlighting, M2/M3 surfaces, inherited theme, and anchored direction probes.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Align(
                        alignment: Alignment.CenterLeft,
                        child: new Row(
                            mainAxisSize: MainAxisSize.Min,
                            children:
                            [
                                new TextButton(
                                    new Text($"Open: {FormatDirection(_openDirection)}"),
                                    () => SetState(() => _openDirection = NextDirection(_openDirection))),
                                new SizedBox(width: 8),
                                new TextButton(
                                    new Text(_useMaterial3 ? "Theme: M3" : "Theme: M2"),
                                    () => SetState(() => _useMaterial3 = !_useMaterial3)),
                            ])),
                    new Text("Material Autocomplete", fontSize: 18, color: Colors.Black),
                    new Theme(
                        new ThemeData(useMaterial3: _useMaterial3),
                        new Autocomplete<string>(
                            optionsBuilder: FilterTerms,
                            textEditingController: _materialController,
                            focusNode: _materialFocusNode,
                            optionsViewOpenDirection: _openDirection,
                            optionsMaxHeight: 160,
                            onSelected: value => SetState(() => _materialSelection = value))),
                    new Text($"Selected: {_materialSelection}", fontSize: 13),
                    new Divider(),
                    new Text("RawAutocomplete", fontSize: 18, color: Colors.Black),
                    new Theme(
                        Theme.Of(context) with { FocusColor = Color.Parse("#243F51B5") },
                        new RawAutocomplete<string>(
                            textEditingController: _rawController,
                            focusNode: _rawFocusNode,
                            optionsBuilder: FilterTerms,
                            optionsViewOpenDirection: _openDirection,
                            displayStringForOption: value => value,
                            fieldViewBuilder: (fieldContext, controller, focusNode, onSubmitted) => new TextField(
                                controller: controller,
                                focusNode: focusNode,
                                decoration: new InputDecoration(
                                    labelText: "Framework concept",
                                    hintText: "Type to filter"),
                                onSubmitted: value => onSubmitted()),
                            optionsViewBuilder: BuildRawOptions,
                            onSelected: value => SetState(() => _rawSelection = value))),
                    new Text($"Selected: {_rawSelection}", fontSize: 13),
                ]));
    }

    public override void Dispose()
    {
        _materialController.Dispose();
        _materialFocusNode.Dispose();
        _rawController.Dispose();
        _rawFocusNode.Dispose();
    }

    private static IEnumerable<string> FilterTerms(TextEditingValue value)
    {
        string query = value.Text.Trim();
        return FrameworkTerms.Where(term => query.Length == 0
            || term.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static Widget BuildRawOptions(
        BuildContext context,
        AutocompleteOnSelected<string> onSelected,
        IEnumerable<string> options)
    {
        int highlightedIndex = AutocompleteHighlightedOption.Of(context);
        string[] materialized = options.ToArray();
        var theme = Theme.Of(context);
        return new DecoratedBox(
            new BoxDecoration(
                Color: theme.CanvasColor,
                Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(theme.ColorScheme.OutlineVariant)),
                BorderRadius: BorderRadius.Circular(12)),
            child: new ClipRRect(
                BorderRadius.Circular(12),
                child: new ListView(
                    shrinkWrap: true,
                    children: materialized.Select((option, index) =>
                    {
                        Widget content = new Padding(new Thickness(16, 12), new Text(option));
                        if (index == highlightedIndex)
                        {
                            content = new ColoredBox(theme.FocusColor, child: content);
                        }

                        return (Widget)new InkWell(onTap: () => onSelected(option), child: content);
                    }).ToArray())));
    }

    private static OptionsViewOpenDirection NextDirection(OptionsViewOpenDirection value)
    {
        return value switch
        {
            OptionsViewOpenDirection.Down => OptionsViewOpenDirection.Up,
            OptionsViewOpenDirection.Up => OptionsViewOpenDirection.MostSpace,
            _ => OptionsViewOpenDirection.Down,
        };
    }

    private static string FormatDirection(OptionsViewOpenDirection value)
    {
        return value switch
        {
            OptionsViewOpenDirection.Up => "up",
            OptionsViewOpenDirection.MostSpace => "mostSpace",
            _ => "down",
        };
    }
}
