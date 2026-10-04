using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/widget_inspector_demo_page.dart (exact sample parity)

/// <summary>
/// The on-device widget inspector: select mode wraps the whole app in the root
/// <see cref="WidgetInspector"/>, and the page reads the selection back.
/// </summary>
public sealed class WidgetInspectorDemoPage : StatefulWidget
{
    public override State CreateState() => new WidgetInspectorDemoPageState();
}

internal sealed class WidgetInspectorDemoPageState : State
{
    private string _selection = "nothing selected";

    public override void InitState()
    {
        base.InitState();
        WidgetInspectorService.Instance.Selection.AddListener(HandleSelectionChanged);
        WidgetsBinding.Instance.DebugShowWidgetInspectorOverrideNotifier.AddListener(HandleSelectionChanged);
    }

    public override void Dispose()
    {
        WidgetInspectorService.Instance.Selection.RemoveListener(HandleSelectionChanged);
        WidgetsBinding.Instance.DebugShowWidgetInspectorOverrideNotifier.RemoveListener(HandleSelectionChanged);
        base.Dispose();
    }

    private void HandleSelectionChanged()
    {
        Element? element = WidgetInspectorService.Instance.Selection.CurrentElement;
        RenderObject? renderObject = WidgetInspectorService.Instance.Selection.Current;
        SetState(() =>
        {
            _selection = element is null
                ? "nothing selected"
                : $"{element.ToStringShort()} -> {renderObject?.GetType().Name ?? "no render object"}";
        });
    }

    public override Widget Build(BuildContext context)
    {
        bool selectMode = WidgetsBinding.Instance.DebugShowWidgetInspectorOverride;
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("WidgetInspector", fontSize: 20, color: Colors.Black),
                new Text(
                    "Select mode wraps the app in the root inspector: tap a widget to outline it, drag to "
                    + "browse candidates, and use the buttons at the bottom to exit, move them or let taps "
                    + "reach the app.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildButton(
                            selectMode ? "Exit select mode" : "Select widget",
                            () => WidgetsBinding.Instance.DebugShowWidgetInspectorOverride = !selectMode),
                        BuildButton(
                            "Clear selection",
                            () => WidgetInspectorService.Instance.Selection.Clear()),
                    ]),
                new Text("Selected: " + _selection, fontSize: 14, color: Colors.Black),
                new Expanded(
                    child: new Row(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 16,
                        children:
                        [
                            new Expanded(
                                child: BuildProbe("Blue card", new Color(0xFF90CAF9))),
                            new Expanded(
                                child: new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 16,
                                    children:
                                    [
                                        new Expanded(child: BuildProbe("Green card", new Color(0xFFA5D6A7))),
                                        new Expanded(
                                            child: new DisableWidgetInspectorScope(
                                                BuildProbe("Hidden from the tree", new Color(0xFFFFCC80)))),
                                    ])),
                        ])),
            ]);
    }

    private static Widget BuildProbe(string label, Color color)
    {
        return new ColoredBox(
            color: color,
            child: new Padding(
                new Thickness(12),
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    spacing: 8,
                    children:
                    [
                        new Text(label, fontSize: 16, color: Colors.Black),
                        new Container(width: 48, height: 24, color: new Color(0x99000000)),
                    ])));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 140,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: new Color(0xFFDCE3ED),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }
}
