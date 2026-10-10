using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/platform_menu_bar_demo_page.dart (exact sample parity)
public sealed class PlatformMenuBarDemoPage : StatefulWidget
{
    public override State CreateState() => new PlatformMenuBarDemoPageState();
}

internal sealed class PlatformMenuBarDemoPageState : State<PlatformMenuBarDemoPage>
{
    private bool _enabled = true;
    private string _lastEvent = "none";

    public override Widget Build(BuildContext context)
    {
        var menus = new List<PlatformMenuItem>
        {
            new PlatformMenu(
                label: "Plumix demo",
                onOpen: () => Record("menu opened"),
                onClose: () => Record("menu closed"),
                menus:
                [
                    new PlatformMenuItemGroup(
                    [
                        new PlatformMenuItem(
                            label: "Say hello",
                            tooltip: "Update the message below",
                            shortcut: new SingleActivator(LogicalKeyboardKey.KeyH, control: true),
                            onSelected: _enabled ? () => Record("Hello from the system menu") : null),
                        new PlatformMenuItem(label: "Unavailable action"),
                    ]),
                    new PlatformMenu(
                        label: "Messages",
                        menus:
                        [
                            new PlatformMenuItem(label: "Welcome", onSelected: () => Record("Welcome")),
                            new PlatformMenuItem(label: "Goodbye", onSelected: () => Record("Goodbye")),
                        ]),
                ]),
        };
        if (PlatformProvidedMenuItem.HasMenu(PlatformProvidedMenuItemType.About))
        {
            menus.Add(new PlatformMenu("System",
            [
                new PlatformProvidedMenuItem(PlatformProvidedMenuItemType.About),
                new PlatformProvidedMenuItem(PlatformProvidedMenuItemType.ServicesSubmenu),
            ]));
        }

        return new PlatformMenuBar(
            menus: menus,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 16,
                children:
                [
                    new Text("PlatformMenuBar", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Open the system menu to try grouped actions, a Messages submenu and Ctrl+H. "
                        + "Native menus require a host with system-menu support.",
                        color: Colors.DimGray),
                    new Container(
                        padding: new Thickness(12),
                        color: new Color(0xFFF4F7FA),
                        child: new Text(
                            "Plumix demo\n  Say hello (Ctrl+H)\n  Unavailable action (disabled)\n"
                            + "  ──────────\n  Messages → Welcome / Goodbye",
                            color: Colors.Black)),
                    new Text($"Say hello: {(_enabled ? "enabled" : "disabled")}", color: Colors.Black),
                    new TextButton(
                        onPressed: () => SetState(() => _enabled = !_enabled),
                        child: new Text("Toggle Say hello")),
                    new Text($"Last event: {_lastEvent}", color: new Color(0xFF31506F)),
                ]));
    }

    private void Record(string message) => SetState(() => _lastEvent = message);
}
