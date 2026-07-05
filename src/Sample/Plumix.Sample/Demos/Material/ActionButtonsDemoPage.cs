using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/action_buttons_demo_page.dart

public sealed class ActionButtonsDemoPage : StatefulWidget
{
    public override State CreateState() => new ActionButtonsDemoPageState();

    private sealed class ActionButtonsDemoPageState : State
    {
        private bool _applePlatform;
        private bool _customIcons;
        private int _backCount;
        private int _closeCount;
        private int _drawerCount;
        private int _endDrawerCount;

        public override Widget Build(BuildContext context)
        {
            var localTheme = Theme.Of(context) with
            {
                Platform = _applePlatform ? TargetPlatform.IOS : TargetPlatform.Windows,
            };
            var actionIconTheme = _customIcons
                ? new ActionIconThemeData(
                    BackButtonIconBuilder: _ => new Icon(Icons.Star),
                    CloseButtonIconBuilder: _ => new Icon(Icons.Cancel),
                    DrawerButtonIconBuilder: _ => new Icon(Icons.InfoOutline),
                    EndDrawerButtonIconBuilder: _ => new Icon(Icons.StarOutline))
                : new ActionIconThemeData();

            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("Material action buttons", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Back/close/drawer/end-drawer icons, themes, callbacks, tooltips, and style precedence.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new TextButton(
                                child: new Text(_applePlatform ? "platform=iOS" : "platform=Windows"),
                                onPressed: () => SetState(() => _applePlatform = !_applePlatform)),
                            new TextButton(
                                child: new Text($"customIcons={_customIcons.ToString().ToLowerInvariant()}"),
                                onPressed: () => SetState(() => _customIcons = !_customIcons)),
                        ]),
                    new Theme(
                        data: localTheme,
                        child: new ActionIconTheme(
                            data: actionIconTheme,
                            child: new Row(
                                spacing: 16,
                                children:
                                [
                                    new BackButton(onPressed: () => SetState(() => _backCount++)),
                                    new CloseButton(onPressed: () => SetState(() => _closeCount++)),
                                    new DrawerButton(onPressed: () => SetState(() => _drawerCount++)),
                                    new EndDrawerButton(onPressed: () => SetState(() => _endDrawerCount++)),
                                    new Text("standalone:"),
                                    new BackButtonIcon(),
                                    new CloseButtonIcon(),
                                    new DrawerButtonIcon(),
                                    new EndDrawerButtonIcon(),
                                ]))),
                    new Text(
                        "back=$_backCount, close=$_closeCount, drawer=$_drawerCount, end=$_endDrawerCount",
                        color: Colors.Black),
                    new Text("style.iconColor overrides color", color: Colors.DimGray),
                    new Theme(
                        data: localTheme,
                        child: new BackButton(
                            color: Colors.Red,
                            style: new ButtonStyle(
                                IconColor: MaterialStateProperty<Color?>.All(Colors.Purple)),
                            onPressed: () => SetState(() => _backCount++))),
                ]);
        }
    }
}
