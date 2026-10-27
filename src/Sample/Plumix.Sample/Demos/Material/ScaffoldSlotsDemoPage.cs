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
    private bool _showKeyboard;
    private bool _resizeForKeyboard = true;

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(child: new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Scaffold slots", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                new Text(
                    "persistentFooterButtons, the extendBody padding restoration, and drawer paint order.",
                    style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _showFooter ? "Footer on" : "Footer off",
                            onTap: () => SetState(() => _showFooter = !_showFooter),
                            width: 96,
                            background: new Color(0xFFE9F0FF)),
                        BuildControlButton(
                            label: $"Align {FooterAlignmentLabels[_footerAlignmentIndex]}",
                            onTap: () => SetState(() =>
                                _footerAlignmentIndex = (_footerAlignmentIndex + 1) % FooterAlignments.Length),
                            width: 106,
                            background: new Color(0xFFEFE8F8)),
                        BuildControlButton(
                            label: _useFooterDecoration ? "Decoration" : "Divider",
                            onTap: () => SetState(() => _useFooterDecoration = !_useFooterDecoration),
                            width: 106,
                            background: new Color(0xFFF7E9E3)),
                    ]),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _extendBody ? "extendBody on" : "extendBody off",
                            onTap: () => SetState(() => _extendBody = !_extendBody),
                            width: 132,
                            background: new Color(0xFFE8F5E9)),
                        BuildControlButton(
                            label: _extendBodyBehindAppBar ? "behind bar on" : "behind bar off",
                            onTap: () => SetState(() => _extendBodyBehindAppBar = !_extendBodyBehindAppBar),
                            width: 132,
                            background: new Color(0xFFF3E8D8)),
                        BuildControlButton(
                            label: $"bottomSheet {BottomSheetLabels[_bottomSheetIndex]}",
                            onTap: () => SetState(() =>
                                _bottomSheetIndex = (_bottomSheetIndex + 1) % BottomSheetLabels.Length),
                            width: 132,
                            background: new Color(0xFFEDE7F6)),
                    ]),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _showKeyboard ? "Keyboard on" : "Keyboard off",
                            onTap: () => SetState(() => _showKeyboard = !_showKeyboard),
                            width: 132,
                            background: new Color(0xFFE8F5E9)),
                        BuildControlButton(
                            label: _resizeForKeyboard ? "Resize on" : "Resize off",
                            onTap: () => SetState(() => _resizeForKeyboard = !_resizeForKeyboard),
                            width: 132,
                            background: new Color(0xFFF3E8D8)),
                    ]),
                new Text(
                    $"footer={(_showFooter ? "true" : "false")}, "
                    + $"alignment={FooterAlignmentLabels[_footerAlignmentIndex]}, "
                    + $"decoration={(_useFooterDecoration ? "true" : "false")}, "
                    + $"extendBody={(_extendBody ? "true" : "false")}, "
                    + $"extendBodyBehindAppBar={(_extendBodyBehindAppBar ? "true" : "false")}, "
                    + $"bottomSheet={BottomSheetLabels[_bottomSheetIndex]}, "
                    + $"keyboard={(_showKeyboard ? "true" : "false")}, "
                    + $"resize={(_resizeForKeyboard ? "true" : "false")}",
                    style: new TextStyle(FontSize: 12, Color: new Color(0xFF607D8B))),
                new SizedBox(
                    height: 420,
                    child: new Container(
                        decoration: new BoxDecoration(
                            Color: new Color(0xFFFDFEFF),
                            BorderRadius: BorderRadius.Circular(10),
                            Border: Plumix.Rendering.Border.FromBorderSide(
                                new BorderSide(new Color(0xFFD6DEEA), 1))),
                        child: new ContextBuilder(previewContext => new MediaQuery(
                            data: MediaQuery.Of(previewContext).CopyWith(
                                viewInsets: EdgeInsets.FromLTRB(0, 0, 0, _showKeyboard ? 120 : 0)),
                            child: new Scaffold(
                                resizeToAvoidBottomInset: _resizeForKeyboard,
                                appBar: new AppBar(title: new Text("Slots preview")),
                                extendBody: _extendBody,
                                extendBodyBehindAppBar: _extendBodyBehindAppBar,
                                drawer: BuildDrawerPanel(isStartDrawer: true),
                                endDrawer: BuildDrawerPanel(isStartDrawer: false),
                                bottomSheet: BuildPersistentBottomSheet(),
                                persistentFooterAlignment: FooterAlignments[_footerAlignmentIndex],
                                persistentFooterDecoration: _useFooterDecoration
                                    ? new BoxDecoration(Color: new Color(0xFFEFF4FF))
                                    : null,
                                persistentFooterButtons: _showFooter
                                    ?
                                    [
                                        BuildFooterButton("Reset", Reset),
                                        BuildFooterButton("Save", () => { }),
                                    ]
                                    : null,
                                bottomNavigationBar: new Container(
                                    color: new Color(0xFFE3ECFB),
                                    height: 48,
                                    child: new Center(
                                        child: new Text(
                                            "bottomNavigationBar (48pt)",
                                            style: new TextStyle(FontSize: 12, Color: new Color(0xFF30404D))))),
                                body: new ContextBuilder(previewBodyContext =>
                                    new SingleChildScrollView(child: BuildPreviewBody(previewBodyContext)))))))),
            ]));
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
            color: _bottomSheetIndex == 1 ? new Color(0xFFE8DEF8) : new Color(0xFFD7E9DE),
            height: 56,
            child: new Center(
                child: new Text(
                    $"Scaffold.bottomSheet {BottomSheetLabels[_bottomSheetIndex]}",
                    style: new TextStyle(FontSize: 12, Color: new Color(0xFF30404D)))));
    }

    private Widget BuildPreviewBody(BuildContext context)
    {
        EdgeInsets padding = MediaQuery.Of(context).Padding;

        return new Container(
            color: new Color(0xFFF2F6FF),
            padding: EdgeInsets.All(14),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text(
                        "The body reports the padding _BodyBuilder restores for the slots it extends behind.",
                        style: new TextStyle(FontSize: 12, Color: new Color(0xFF30404D))),
                    new Text(
                        $"body MediaQuery padding: top={padding.Top:0.0}, bottom={padding.Bottom:0.0}",
                        style: new TextStyle(FontSize: 12, Color: new Color(0xFF0D47A1))),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: "Open start",
                                onTap: () => Scaffold.Of(context).OpenDrawer(),
                                width: 100,
                                background: new Color(0xFFE9EEF5)),
                            BuildControlButton(
                                label: "Open end",
                                onTap: () => Scaffold.Of(context).OpenEndDrawer(),
                                width: 100,
                                background: new Color(0xFFEFE8F8)),
                        ]),
                ]));
    }

    private Widget BuildDrawerPanel(bool isStartDrawer)
    {
        string title = isStartDrawer ? "Start drawer" : "End drawer";
        var accent = isStartDrawer ? new Color(0xFF0D47A1) : new Color(0xFF4A148C);

        return new Drawer(
            child: new ContextBuilder(
                context => new Container(
                    padding: EdgeInsets.All(14),
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text(title, style: new TextStyle(FontSize: 16, Color: accent)),
                            new Text(
                                "The opened end drawer is appended last, so it paints over the start drawer.",
                                style: new TextStyle(FontSize: 12, Color: new Color(0x8A000000))),
                            BuildControlButton(
                                label: "Close",
                                onTap: isStartDrawer
                                    ? () => Scaffold.Of(context).CloseDrawer()
                                    : () => Scaffold.Of(context).CloseEndDrawer(),
                                width: 84,
                                background: new Color(0xFFE9EEF5)),
                        ]))));
    }

    private Widget BuildFooterButton(string label, Action onTap)
    {
        return new TextButton(
            onPressed: onTap,
            child: new Text(label, style: new TextStyle(FontSize: 12)),
            style: TextButton.StyleFrom(
                foregroundColor: new Color(0xFF0D47A1),
                padding: EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                minimumSize: new Size(0, 36),
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
                child: new Text(label, style: new TextStyle(FontSize: 12)),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: EdgeInsets.Symmetric(horizontal: 10, vertical: 8),
                    minimumSize: new Size(0, 36),
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
            _showKeyboard = false;
            _resizeForKeyboard = true;
        });
    }
}
