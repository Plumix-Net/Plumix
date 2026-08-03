using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/material/expansion_panel_demo_page.dart
public sealed class ExpansionPanelDemoPage : StatefulWidget
{
    public override State CreateState() => new ExpansionPanelDemoPageState();
}

internal sealed class ExpansionPanelDemoPageState : State
{
    private bool _detailsExpanded = true;
    private bool _historyExpanded;
    private string _lastRadioEvent = "none";

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ExpansionPanel + ExpansionPanelList", fontSize: 20, color: Colors.Black),
                new Text(
                    "Controlled panels, header tap gating, animated material gaps, and mutually exclusive radio panels.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Text("Controlled list", fontSize: 14, color: Color.Parse("#FF6750A4")),
                BuildControlledList(),
                new Text("Radio list · last callback: " + _lastRadioEvent, fontSize: 14, color: Color.Parse("#FF006C4C")),
                BuildRadioList(),
            ]);
    }

    private Widget BuildControlledList()
    {
        return new ExpansionPanelList(
            materialGapSize: 12,
            dividerColor: Color.Parse("#FFE6E0E9"),
            expandIconColor: Color.Parse("#FF6750A4"),
            expansionCallback: (index, expanded) => SetState(() =>
            {
                if (index == 0)
                {
                    _detailsExpanded = expanded;
                }
                else
                {
                    _historyExpanded = expanded;
                }
            }),
            children:
            [
                new ExpansionPanel(
                    isExpanded: _detailsExpanded,
                    headerBuilder: (_, expanded) => BuildHeader("Account details", expanded),
                    body: BuildBody("Name and contact preferences are synchronized."),
                    backgroundColor: Color.Parse("#FFFFFBFE")),
                new ExpansionPanel(
                    isExpanded: _historyExpanded,
                    canTapOnHeader: true,
                    headerBuilder: (_, expanded) => BuildHeader("Recent activity", expanded),
                    body: BuildBody("Three successful synchronizations this week."),
                    backgroundColor: Color.Parse("#FFFFFBFE")),
            ]);
    }

    private Widget BuildRadioList()
    {
        return ExpansionPanelList.Radio(
            initialOpenPanelValue: "balanced",
            materialGapSize: 12,
            expansionCallback: (index, expanded) =>
                SetState(() => _lastRadioEvent = "$index:${expanded.ToString().ToLowerInvariant()}"),
            children:
            [
                new ExpansionPanelRadio(
                    value: "balanced",
                    canTapOnHeader: true,
                    headerBuilder: (_, expanded) => BuildHeader("Balanced sync", expanded),
                    body: BuildBody("Sync every hour while preserving battery.")),
                new ExpansionPanelRadio(
                    value: "instant",
                    canTapOnHeader: true,
                    headerBuilder: (_, expanded) => BuildHeader("Instant sync", expanded),
                    body: BuildBody("Sync immediately after every local change.")),
            ]);
    }

    private static Widget BuildHeader(string label, bool expanded)
    {
        return new Padding(
            new Avalonia.Thickness(16, 0),
            new Text($"{label} · {(expanded ? "open" : "closed")}", fontSize: 14, color: Colors.Black));
    }

    private static Widget BuildBody(string label)
    {
        return new Padding(
            new Avalonia.Thickness(16, 0, 16, 16),
            new Text(label, fontSize: 13, color: Color.Parse("#FF49454F")));
    }
}
