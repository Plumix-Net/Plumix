using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Plumix.UI;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/selection_demo_page.dart

public sealed class SelectionDemoPage : StatefulWidget
{
    public override State CreateState() => new SelectionDemoPageState();
}

internal sealed class SelectionDemoPageState : State
{
    private readonly SelectionListenerNotifier _selectionNotifier = new();
    private readonly ContextMenuController _menuController = new();
    private int _menuValue;
    private int _pageClicks;
    private bool _interactive = true;
    private TargetPlatform _selectionPlatform = PlatformDefaults.TargetPlatform;
    private string _singleSelection = "none";
    private string _areaSelection = "none";
    private string _listenerDetails = "none";
    private string _scrollableSelection = "none";

    public override void InitState()
    {
        base.InitState();
        _selectionNotifier.AddListener(HandleSelectionDetailsChanged);
    }

    private void HandleSelectionDetailsChanged()
    {
        ISelectionDetails details = _selectionNotifier.Selection;
        SetState(() => _listenerDetails = details.Range is { } range
            ? $"{details.Status.ToString().ToLowerInvariant()}: {range.StartOffset}..{range.EndOffset}"
            : $"{details.Status.ToString().ToLowerInvariant()}: none");
    }

    public override void Dispose()
    {
        _menuController.Remove();
        _selectionNotifier.Dispose();
        base.Dispose();
    }

    private void ShowMenu()
    {
        _menuValue++;
        _menuController.Show(Context, BuildMenu);
    }

    private Widget BuildMenu(BuildContext context)
    {
        return new Positioned(
            right: 16,
            top: 80,
            width: 260,
            child: new Plumix.Material.Material(
                elevation: 8,
                borderRadius: BorderRadius.Circular(12),
                child: new Padding(
                    insets: new Thickness(16),
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        spacing: 8,
                        children:
                        [
                            new Text($"Menu value: {_menuValue}", style: new TextStyle(FontSize: 18)),
                            new Text("The page stays interactive while this menu is open."),
                            new TextButton(new Text("Close menu"), _menuController.Remove),
                        ]))));
    }

    private void ToggleBrowserMenu()
    {
        EventLoop.RunAsync(async () =>
        {
            if (BrowserContextMenu.Enabled)
            {
                await BrowserContextMenu.DisableContextMenu();
            }
            else
            {
                await BrowserContextMenu.EnableContextMenu();
            }

            if (Mounted) SetState(() => { });
        });
    }

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("SelectableText + SelectionArea", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                    new Text(
                        "Drag across text, then right-click or long-press for the adaptive context menu. "
                        + "Double-tap selects a word and triple-tap a paragraph; long press raises the "
                        + "drag handles and the magnifier. Ctrl/Cmd+A and Ctrl/Cmd+C also work. "
                        + "The second probe spans several Text widgets.",
                        style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                    new Align(
                        alignment: Alignment.CenterLeft,
                        child: new TextButton(
                            new Text($"Interactive: {_interactive}"),
                            () => SetState(() => _interactive = !_interactive))),
                    new Text("ContextMenuController", style: new TextStyle(FontSize: 18, Color: Colors.Black)),
                    new Text("Show a menu, update its value, and click the page while it stays open."),
                    new Wrap(
                        spacing: 8,
                        children:
                        [
                            new TextButton(new Text("Show / replace builder"), ShowMenu),
                            new TextButton(new Text("Rebuild menu"), () =>
                            {
                                if (!_menuController.IsShown) return;
                                _menuValue++;
                                _menuController.MarkNeedsBuild();
                            }),
                            new TextButton(new Text($"Page clicks: {_pageClicks}"),
                                () => SetState(() => _pageClicks++)),
                        ]),
                    new Text("Single selectable run", style: new TextStyle(FontSize: 18, Color: Colors.Black)),
                    new DecoratedBox(
                        decoration: new BoxDecoration(
                            Color: new Color(0xFFF7F2FA),
                            Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(new Color(0xFFCAC4D0))),
                            BorderRadius: BorderRadius.Circular(12)),
                        child: new Padding(
                            insets: new Thickness(16),
                            child: new SelectableText(
                                "Plumix keeps selectable text in the framework render pipeline.",
                                style: new TextStyle(FontSize: 17, Height: 1.35),
                                showCursor: true,
                                enableInteractiveSelection: _interactive,
                                onSelectionChanged: (selection, cause) => SetState(() =>
                                    _singleSelection = selection.IsCollapsed
                                        ? "none"
                                        : $"{selection.Start}..{selection.End} ({cause})")))),
                    new Text($"Single selection: {_singleSelection}", style: new TextStyle(FontSize: 13)),
                    new Divider(),
                    new Text("SelectionArea subtree", style: new TextStyle(FontSize: 18, Color: Colors.Black)),
                    new TextSelectionTheme(
                        data: new TextSelectionThemeData(
                            CursorColor: Colors.DarkGreen,
                            SelectionColor: Color.FromARGB(0x66, 0x00, 0x80, 0x80)),
                        child: new SelectionArea(
                            onSelectionChanged: content => SetState(() =>
                                _areaSelection = content?.PlainText ?? "none"),
                            child: new SelectionListener(
                                _selectionNotifier,
                                new DecoratedBox(
                                decoration: new BoxDecoration(
                                    Color: new Color(0xFFF4FBF8),
                                    Border: Plumix.Rendering.Border.FromBorderSide(
                                        new BorderSide(new Color(0xFF80CBC4))),
                                    BorderRadius: BorderRadius.Circular(12)),
                                child: new Padding(
                                    insets: new Thickness(16),
                                    child: new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Start,
                                        spacing: 6,
                                        children:
                                        [
                                            new Text("SelectionArea coordinates selection across"),
                                            new Text("multiple Text widgets in one subtree."),
                                            new Row(
                                                spacing: 8,
                                                children:
                                                [
                                                    new Text("It also works"),
                                                    new Text("across a Row."),
                                                ]),
                                        ])))))),
                    new Text($"Area selection: {_areaSelection}", style: new TextStyle(FontSize: 13), maxLines: 3),
                    new Text($"SelectionListener: {_listenerDetails}", style: new TextStyle(FontSize: 13)),
                    new Divider(),
                    new Text("Selection inside a scrollable", style: new TextStyle(FontSize: 18, Color: Colors.Black)),
                    new Text(
                        "Drag a selection past the top or bottom edge of the list: the scrollable auto "
                        + "scrolls while the drag continues, keeps the selection anchored to the content, "
                        + "and restores it for rows that scroll back into view.",
                        style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                    new SelectionArea(
                        onSelectionChanged: content => SetState(() =>
                            _scrollableSelection = content?.PlainText ?? "none"),
                        child: new DecoratedBox(
                            decoration: new BoxDecoration(
                                Color: new Color(0xFFFFFBF2),
                                Border: Plumix.Rendering.Border.FromBorderSide(
                                    new BorderSide(new Color(0xFFE0C08A))),
                                BorderRadius: BorderRadius.Circular(12)),
                            child: new SizedBox(
                                height: 160,
                                child: ListView.Builder(
                                    itemCount: 40,
                                    itemExtent: 28,
                                    itemBuilder: (context, index) => new Padding(
                                        insets: new Thickness(12, 4),
                                        child: new Text(
                                            $"Row {index} of the scrollable selection probe")))))),
                    new Text(
                        $"Scrollable selection: {_scrollableSelection}",
                        style: new TextStyle(
                            FontSize: 13),
                        maxLines: 3),
                    new Divider(),
                    new Text("Platform selection controls", style: new TextStyle(FontSize: 18)),
                    new Text(
                        "Switch the handle style, then long-press the text. Select all inside the field "
                        + "to check that it keeps its own keyboard actions."),
                    new Wrap(spacing: 8, children:
                    [
                        new TextButton(new Text($"Platform: {_selectionPlatform}"), () => SetState(() =>
                            _selectionPlatform = (TargetPlatform)(((int)_selectionPlatform + 1) % 6))),
                        .. PlatformDefaults.IsWeb ? new Widget[]
                        {
                            new TextButton(
                                new Text($"Browser menu: {BrowserContextMenu.Enabled}"), ToggleBrowserMenu),
                        } : System.Array.Empty<Widget>(),
                    ]),
                    new Theme(Theme.Of(context) with { Platform = _selectionPlatform },
                        new SelectionArea(new Column(crossAxisAlignment: CrossAxisAlignment.Start, children:
                        [
                            new Text("Select this paragraph with the platform's handles."),
                            new TextField(decoration: new InputDecoration(labelText: "A field inside SelectionArea")),
                        ]))),
                    new Divider(),
                    new Text("DefaultSelectionStyle scope", style: new TextStyle(FontSize: 18, Color: Colors.Black)),
                    new DefaultSelectionStyle(
                        cursorColor: new Color(0xFFFF5722),
                        selectionColor: Color.FromARGB(0x66, 0xFF, 0x57, 0x22),
                        mouseCursor: SystemMouseCursors.Click,
                        child: new SelectableText(
                            "Cursor, selection, and mouse cursor inherit from the core selection style.")),
                    new Divider(),
                    new Text(
                        "TextSelectionTheme on TextField",
                        style: new TextStyle(
                            FontSize: 18,
                            Color: Colors.Black)),
                    new TextSelectionTheme(
                        data: new TextSelectionThemeData(
                            CursorColor: new Color(0xFF7B1FA2),
                            SelectionColor: Color.FromARGB(0x66, 0x7B, 0x1F, 0xA2),
                            SelectionHandleColor: new Color(0xFF7B1FA2)),
                        child: new TextField(
                            decoration: new InputDecoration(
                                labelText: "Themed cursor, selection, and handles"))),
                    new TextField(
                        cursorColor: new Color(0xFF1565C0),
                        cursorErrorColor: new Color(0xFFB3261E),
                        decoration: new InputDecoration(
                            labelText: "Explicit cursorColor",
                            errorText: "An errored field paints cursorErrorColor")),
                ]));
    }
}
