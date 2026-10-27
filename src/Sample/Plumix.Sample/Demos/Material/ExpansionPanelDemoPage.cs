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
                new Text(
                    "ExpansionPanel + ExpansionPanelList",
                    style: new TextStyle(
                        FontSize: 20,
                        Color: Colors.Black)),
                new Text(
                    "Controlled panels, header tap gating, animated material gaps, and mutually exclusive radio panels.",
                    style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                new Text("Controlled list", style: new TextStyle(FontSize: 14, Color: new Color(0xFF6750A4))),
                BuildControlledList(),
                new Text(
                    "Radio list · last callback: " + _lastRadioEvent,
                    style: new TextStyle(
                        FontSize: 14,
                        Color: new Color(0xFF006C4C))),
                BuildRadioList(),
            ]);
    }

    private Widget BuildControlledList()
    {
        return new ExpansionPanelList(
            materialGapSize: 12,
            dividerColor: new Color(0xFFE6E0E9),
            expandIconColor: new Color(0xFF6750A4),
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
                    backgroundColor: new Color(0xFFFFFBFE)),
                new ExpansionPanel(
                    isExpanded: _historyExpanded,
                    canTapOnHeader: true,
                    headerBuilder: (_, expanded) => BuildHeader("Recent activity", expanded),
                    body: BuildBody("Three successful synchronizations this week."),
                    backgroundColor: new Color(0xFFFFFBFE)),
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
            EdgeInsets.Symmetric(horizontal: 16),
            new Text(
                $"{label} · {(expanded ? "open" : "closed")}",
                style: new TextStyle(
                    FontSize: 14,
                    Color: Colors.Black)));
    }

    private static Widget BuildBody(string label)
    {
        return new Padding(
            EdgeInsets.FromLTRB(16, 0, 16, 16),
            new Text(label, style: new TextStyle(FontSize: 13, Color: new Color(0xFF49454F))));
    }
}
