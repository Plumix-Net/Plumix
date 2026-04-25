using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/card_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CardDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new CardDemoPageState();
    }
}

internal sealed class CardDemoPageState : State
{
    private bool _useMaterial3 = true;
    private bool _useThemeOverrides;
    private bool _clip;
    private bool _dense;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var pageTheme = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            CardTheme = _useThemeOverrides
                ? new CardThemeData(
                    Color: Color.Parse("#FFF5F9EE"),
                    ShadowColor: Color.Parse("#FF455A64"),
                    SurfaceTintColor: Color.Parse("#FF6750A4"),
                    Elevation: 3,
                    Margin: new Thickness(8),
                    Shape: ShapeBorder.RoundedRectangle(18),
                    ClipBehavior: _clip ? Clip.AntiAlias : Clip.None)
                : new CardThemeData()
        };

        return new Theme(
            data: pageTheme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("Card baseline", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Elevated, filled, and outlined Material card variants with theme, mode, and clip probes.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 80,
                                background: Color.Parse("#FFE9F0FF")),
                            BuildControlButton(
                                label: _useThemeOverrides ? "Theme on" : "Theme off",
                                onTap: () => SetState(() => _useThemeOverrides = !_useThemeOverrides),
                                width: 112,
                                background: Color.Parse("#FFEAF6F7")),
                            BuildControlButton(
                                label: _clip ? "Clip on" : "Clip off",
                                onTap: () => SetState(() => _clip = !_clip),
                                width: 96,
                                background: Color.Parse("#FFF0E8FF")),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _dense ? "Dense" : "Regular",
                                onTap: () => SetState(() => _dense = !_dense),
                                width: 98,
                                background: Color.Parse("#FFF8EFE2")),
                            BuildControlButton(
                                label: "Reset",
                                onTap: ResetState,
                                width: 88,
                                background: Color.Parse("#FFF3E8D8")),
                        ]),
                    new Text(
                        $"useMaterial3={(_useMaterial3 ? "true" : "false")}, theme={(_useThemeOverrides ? "true" : "false")}, clip={(_clip ? "true" : "false")}, dense={(_dense ? "true" : "false")}",
                        fontSize: 12,
                        color: Color.Parse("#FF607D8B")),
                    new Expanded(
                        child: new Container(
                            color: Color.Parse("#FFF7F9FC"),
                            child: new SingleChildScrollView(
                                child: new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    children:
                                    [
                                        BuildElevatedCard(),
                                        BuildFilledCard(),
                                        BuildOutlinedCard(),
                                    ])))),
                ]));
    }

    private Widget BuildElevatedCard()
    {
        return new Card(
            clipBehavior: _clip ? Clip.AntiAlias : null,
            child: new ListTile(
                dense: _dense,
                leading: new Icon(Icons.StarOutline),
                title: new Text("Elevated card"),
                subtitle: new Text("Default variant keeps elevation and surfaceContainerLow color."),
                trailing: new Icon(Icons.InfoOutline)));
    }

    private Widget BuildFilledCard()
    {
        return Card.Filled(
            clipBehavior: _clip ? Clip.AntiAlias : null,
            child: BuildCardBody(
                title: "Filled card",
                body: "Filled cards use a quieter container color and zero default elevation in Material 3."));
    }

    private Widget BuildOutlinedCard()
    {
        return Card.Outlined(
            clipBehavior: _clip ? Clip.AntiAlias : null,
            child: BuildCardBody(
                title: "Outlined card",
                body: "Outlined cards add the default outlineVariant border while keeping elevation at zero."));
    }

    private Widget BuildCardBody(string title, string body)
    {
        return new Container(
            padding: _dense ? new Thickness(14, 10) : new Thickness(18, 14),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                spacing: 6,
                children:
                [
                    new Text(title, fontSize: 16, color: Colors.Black),
                    new Text(body, fontSize: 13, color: Color.Parse("#FF607D8B")),
                ]));
    }

    private Widget BuildControlButton(
        string label,
        Action onTap,
        double width,
        Color background)
    {
        return new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onTap,
                backgroundColor: background,
                foregroundColor: Colors.Black,
                minHeight: 36,
                padding: new Thickness(10, 8),
                borderRadius: BorderRadius.Circular(8),
                child: new Text(label, fontSize: 12)));
    }

    private void ResetState()
    {
        SetState(() =>
        {
            _useMaterial3 = true;
            _useThemeOverrides = false;
            _clip = false;
            _dense = false;
        });
    }
}
