using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/popup_menu_demo_page.dart

public sealed class PopupMenuDemoPage : StatefulWidget
{
    public override State CreateState() => new PopupMenuDemoPageState();

    private sealed class PopupMenuDemoPageState : State
    {
        private bool _enabled = true;
        private bool _under;
        private bool _useTheme;
        private string _selected = "copy";
        private string _status = "idle";

        public override Widget Build(BuildContext context)
        {
            var popupTheme = _useTheme
                ? new PopupMenuThemeData(
                    Color: Color.Parse("#FFFFF3E0"),
                    Shape: ShapeBorder.RoundedRectangle(12),
                    MenuPadding: new Thickness(4),
                    IconColor: Color.Parse("#FFE65100"),
                    LabelTextStyle: MaterialStateProperty<TextStyle?>.ResolveWith(states =>
                        Theme.Of(context).TextTheme.LabelLarge.CopyWith(
                            color: states.HasFlag(MaterialState.Disabled)
                                ? Color.Parse("#619E9E9E")
                                : Color.Parse("#FFE65100"))))
                : new PopupMenuThemeData();
            return new PopupMenuTheme(
                popupTheme,
                new Builder(innerContext => BuildContent(innerContext)));
        }

        private Widget BuildContent(BuildContext context)
        {
            var position = _under ? PopupMenuPosition.Under : PopupMenuPosition.Over;
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 14,
                children:
                [
                    new Text("PopupMenuButton + PopupMenuItem", fontSize: 20),
                    new Text(
                        "Anchored menu route with selection, cancellation, disabled items, keyboard navigation, and theme precedence.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(_enabled ? "Enabled" : "Disabled", () => SetState(() => _enabled = !_enabled)),
                            ControlButton(_under ? "Under" : "Over", () => SetState(() => _under = !_under)),
                            ControlButton(_useTheme ? "Theme on" : "Theme off", () => SetState(() => _useTheme = !_useTheme)),
                        ]),
                    new Row(
                        spacing: 16,
                        children:
                        [
                            new PopupMenuButton<string>(
                                itemBuilder: BuildItems,
                                initialValue: _selected,
                                onOpened: () => SetState(() => _status = "opened"),
                                onSelected: value => SetState(() =>
                                {
                                    _selected = value;
                                    _status = $"selected: {value}";
                                }),
                                onCanceled: () => SetState(() => _status = "canceled"),
                                enabled: _enabled,
                                position: position,
                                child: new Padding(new Thickness(12, 8), new Text("CHILD MENU"))),
                            new PopupMenuButton<string>(
                                itemBuilder: BuildItems,
                                initialValue: _selected,
                                onSelected: value => SetState(() =>
                                {
                                    _selected = value;
                                    _status = $"icon selected: {value}";
                                }),
                                onCanceled: () => SetState(() => _status = "icon canceled"),
                                enabled: _enabled,
                                position: position,
                                icon: new Icon(Icons.MoreVert),
                                tooltip: "Show commands"),
                        ]),
                    new Text($"Selected: {_selected}", fontSize: 13),
                    new Text($"Status: {_status}", fontSize: 13),
                ]);
        }

        private static IReadOnlyList<PopupMenuEntry<string>> BuildItems(BuildContext context) =>
        [
            new PopupMenuItem<string>(new Text("Copy"), value: "copy"),
            new PopupMenuItem<string>(new Text("Rename"), value: "rename"),
            new PopupMenuItem<string>(new Text("Archive (disabled)"), value: "archive", enabled: false),
            new PopupMenuItem<string>(new Text("Delete"), value: "delete"),
        ];

        private static Widget ControlButton(string label, Action onPressed) =>
            new TextButton(new Text(label, fontSize: 12), onPressed);
    }
}
