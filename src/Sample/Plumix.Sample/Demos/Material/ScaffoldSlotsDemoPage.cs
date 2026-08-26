using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/scaffold_slots_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ScaffoldSlotsDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new ScaffoldSlotsDemoPageState();
    }
}

internal sealed class ScaffoldSlotsDemoPageState : State
{
    private static readonly AlignmentDirectional[] FooterAlignments =
    [
        AlignmentDirectional.CenterStart,
        AlignmentDirectional.Center,
        AlignmentDirectional.CenterEnd,
    ];

    private static readonly string[] FooterAlignmentLabels = ["start", "center", "end"];

    private static readonly string[] BottomSheetLabels = ["off", "A", "B"];

    private bool _showFooter = true;
    private int _footerAlignmentIndex = 2;
    private bool _useFooterDecoration;
    private bool _extendBody;
    private bool _extendBodyBehindAppBar;
    private int _bottomSheetIndex;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Scaffold slots", fontSize: 20, color: Colors.Black),
                new Text(
                    "persistentFooterButtons, the extendBody padding restoration, and drawer paint order.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _showFooter ? "Footer on" : "Footer off",
                            onTap: () => SetState(() => _showFooter = !_showFooter),
                            width: 96,
                            background: Color.Parse("#FFE9F0FF")),
                        BuildControlButton(
                            label: $"Align {FooterAlignmentLabels[_footerAlignmentIndex]}",
                            onTap: () => SetState(() =>
                                _footerAlignmentIndex = (_footerAlignmentIndex + 1) % FooterAlignments.Length),
                            width: 106,
                            background: Color.Parse("#FFEFE8F8")),
                        BuildControlButton(
                            label: _useFooterDecoration ? "Decoration" : "Divider",
                            onTap: () => SetState(() => _useFooterDecoration = !_useFooterDecoration),
                            width: 106,
                            background: Color.Parse("#FFF7E9E3")),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _extendBody ? "extendBody on" : "extendBody off",
                            onTap: () => SetState(() => _extendBody = !_extendBody),
                            width: 132,
                            background: Color.Parse("#FFE8F5E9")),
                        BuildControlButton(
                            label: _extendBodyBehindAppBar ? "behind bar on" : "behind bar off",
                            onTap: () => SetState(() => _extendBodyBehindAppBar = !_extendBodyBehindAppBar),
                            width: 132,
                            background: Color.Parse("#FFF3E8D8")),
                        BuildControlButton(
                            label: $"bottomSheet {BottomSheetLabels[_bottomSheetIndex]}",
                            onTap: () => SetState(() =>
                                _bottomSheetIndex = (_bottomSheetIndex + 1) % BottomSheetLabels.Length),
                            width: 132,
                            background: Color.Parse("#FFEDE7F6")),
                    ]),
                new Text(
                    $"footer={(_showFooter ? "true" : "false")}, "
                    + $"alignment={FooterAlignmentLabels[_footerAlignmentIndex]}, "
                    + $"decoration={(_useFooterDecoration ? "true" : "false")}, "
                    + $"extendBody={(_extendBody ? "true" : "false")}, "
                    + $"extendBodyBehindAppBar={(_extendBodyBehindAppBar ? "true" : "false")}, "
                    + $"bottomSheet={BottomSheetLabels[_bottomSheetIndex]}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Expanded(
                    child: new Container(
                        decoration: new BoxDecoration(
                            Color: Color.Parse("#FFFDFEFF"),
                            BorderRadius: BorderRadius.Circular(10),
                            Border: Plumix.Rendering.Border.FromBorderSide(
                                new BorderSide(Color.Parse("#FFD6DEEA"), 1))),
                        child: new Scaffold(
                            appBar: new AppBar(title: new Text("Slots preview")),
                            extendBody: _extendBody,
                            extendBodyBehindAppBar: _extendBodyBehindAppBar,
                            drawer: BuildDrawerPanel(isStartDrawer: true),
                            endDrawer: BuildDrawerPanel(isStartDrawer: false),
                            bottomSheet: BuildPersistentBottomSheet(),
                            persistentFooterAlignment: FooterAlignments[_footerAlignmentIndex],
                            persistentFooterDecoration: _useFooterDecoration
                                ? new BoxDecoration(Color: Color.Parse("#FFEFF4FF"))
                                : null,
                            persistentFooterButtons: _showFooter
                                ?
                                [
                                    BuildFooterButton("Reset", Reset),
                                    BuildFooterButton("Save", () => { }),
                                ]
                                : null,
                            bottomNavigationBar: new Container(
                                color: Color.Parse("#FFE3ECFB"),
                                height: 48,
                                child: new Center(
                                    child: new Text(
                                        "bottomNavigationBar (48pt)",
                                        fontSize: 12,
                                        color: Color.Parse("#FF30404D")))),
                            body: new ContextBuilder(BuildPreviewBody)))),
            ]);
    }

    /// <summary>
    /// Cycles `Scaffold.bottomSheet` off -> A -> B: swapping A for B updates the mounted sheet in place,
    /// while switching back to "off" keeps the sheet on screen until its exit animation finishes.
    /// </summary>
    private Widget? BuildPersistentBottomSheet()
    {
        if (_bottomSheetIndex == 0)
        {
            return null;
        }

        return new Container(
            color: _bottomSheetIndex == 1 ? Color.Parse("#FFE8DEF8") : Color.Parse("#FFD7E9DE"),
            height: 56,
            child: new Center(
                child: new Text(
                    $"Scaffold.bottomSheet {BottomSheetLabels[_bottomSheetIndex]}",
                    fontSize: 12,
                    color: Color.Parse("#FF30404D"))));
    }

    private Widget BuildPreviewBody(BuildContext context)
    {
        Thickness padding = MediaQuery.Of(context).Padding;

        return new Container(
            color: Color.Parse("#FFF2F6FF"),
            padding: new Thickness(14),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text(
                        "The body reports the padding _BodyBuilder restores for the slots it extends behind.",
                        fontSize: 12,
                        color: Color.Parse("#FF30404D")),
                    new Text(
                        $"body MediaQuery padding: top={padding.Top:0.#}, bottom={padding.Bottom:0.#}",
                        fontSize: 12,
                        color: Color.Parse("#FF0D47A1")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: "Open start",
                                onTap: () => Scaffold.Of(context).OpenDrawer(),
                                width: 100,
                                background: Color.Parse("#FFE9EEF5")),
                            BuildControlButton(
                                label: "Open end",
                                onTap: () => Scaffold.Of(context).OpenEndDrawer(),
                                width: 100,
                                background: Color.Parse("#FFEFE8F8")),
                        ]),
                ]));
    }

    private Widget BuildDrawerPanel(bool isStartDrawer)
    {
        string title = isStartDrawer ? "Start drawer" : "End drawer";
        var accent = isStartDrawer ? Color.Parse("#FF0D47A1") : Color.Parse("#FF4A148C");

        return new Drawer(
            child: new ContextBuilder(
                context => new Container(
                    padding: new Thickness(14),
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text(title, fontSize: 16, color: accent),
                            new Text(
                                "The opened end drawer is appended last, so it paints over the start drawer.",
                                fontSize: 12,
                                color: Color.Parse("#8A000000")),
                            BuildControlButton(
                                label: "Close",
                                onTap: isStartDrawer
                                    ? () => Scaffold.Of(context).CloseDrawer()
                                    : () => Scaffold.Of(context).CloseEndDrawer(),
                                width: 84,
                                background: Color.Parse("#FFE9EEF5")),
                        ]))));
    }

    private Widget BuildFooterButton(string label, Action onTap)
    {
        return new TextButton(
            onPressed: onTap,
            child: new Text(label, fontSize: 12),
            style: TextButton.StyleFrom(
                foregroundColor: Color.Parse("#FF0D47A1"),
                padding: new Thickness(12, 8),
                minimumSize: new Size(64, 36),
                shape: new RoundedRectangleBorder(
                    borderRadius: BorderRadius.Circular(8))));
    }

    private Widget BuildControlButton(
        string label,
        Action? onTap,
        double width,
        Color background)
    {
        return new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onTap,
                child: new Text(label, fontSize: 12),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(64, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
    }

    private void Reset()
    {
        SetState(() =>
        {
            _showFooter = true;
            _footerAlignmentIndex = 2;
            _useFooterDecoration = false;
            _extendBody = false;
            _extendBodyBehindAppBar = false;
            _bottomSheetIndex = 0;
        });
    }
}
