using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/drawer_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class DrawerDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new DrawerDemoPageState();
    }
}

internal sealed class DrawerDemoPageState : State
{
    private bool _useMaterial3 = true;
    private bool _useThemeOverrides;
    private bool _useWidgetOverrides;
    private bool _showEndDrawer = true;
    private int _startOpens;
    private int _endOpens;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var pageTheme = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            ColorScheme = baseTheme.ColorScheme.CopyWith(
                surfaceContainerLow: new Color(0xFFE9F1FF)),
            DrawerTheme = _useThemeOverrides
                ? new DrawerThemeData(
                    BackgroundColor: new Color(0xFFF3F7FC),
                    ScrimColor: Color.FromARGB(0x80, 0x12, 0x34, 0x56),
                    Elevation: 10,
                    ShadowColor: new Color(0xFF345E8B),
                    Width: 268)
                : new DrawerThemeData(),
        };

        return new Theme(
            data: pageTheme,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("Drawer baseline", fontSize: 20, color: Colors.Black),
                    new Text(
                        "M2/M3 defaults, direct surfaceContainerLow, inner-edge shape, and overrides.",
                        fontSize: 14,
                        color: new Color(0x8A000000)),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 80,
                                background: new Color(0xFFE9F0FF)),
                            BuildControlButton(
                                label: _useThemeOverrides ? "Theme on" : "Theme off",
                                onTap: () => SetState(() => _useThemeOverrides = !_useThemeOverrides),
                                width: 112,
                                background: new Color(0xFFEAF6F7)),
                            BuildControlButton(
                                label: _useWidgetOverrides ? "Widget on" : "Widget off",
                                onTap: () => SetState(() => _useWidgetOverrides = !_useWidgetOverrides),
                                width: 118,
                                background: new Color(0xFFF0E8FF)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _showEndDrawer ? "End drawer on" : "End drawer off",
                                onTap: () => SetState(() => _showEndDrawer = !_showEndDrawer),
                                width: 138,
                                background: new Color(0xFFEFF5E8)),
                            BuildControlButton(
                                label: "Reset",
                                onTap: Reset,
                                width: 88,
                                background: new Color(0xFFF3E8D8)),
                        ]),
                    new Text(
                        $"useMaterial3={(_useMaterial3 ? "true" : "false")}, theme={(_useThemeOverrides ? "true" : "false")}, widget={(_useWidgetOverrides ? "true" : "false")}, endDrawer={(_showEndDrawer ? "true" : "false")}, startOpens={_startOpens}, endOpens={_endOpens}",
                        fontSize: 12,
                        color: new Color(0xFF607D8B)),
                    new Expanded(
                        child: new Container(
                            decoration: new BoxDecoration(
                                Color: new Color(0xFFFDFEFF),
                                BorderRadius: BorderRadius.Circular(10),
                                Border: Plumix.Rendering.Border.FromBorderSide(
                                    new BorderSide(new Color(0xFFD6DEEA), 1))),
                            child: new Scaffold(
                                drawerScrimColor: _useWidgetOverrides ? Color.FromARGB(0x99, 0x33, 0x44, 0x55) : null,
                                drawer: BuildDrawerPanel(isStartDrawer: true),
                                endDrawer: _showEndDrawer ? BuildDrawerPanel(isStartDrawer: false) : null,
                                body: new ContextBuilder(BuildPreviewBody)))),
                ]));
    }

    private Widget BuildPreviewBody(BuildContext context)
    {
        return new Container(
            padding: new Thickness(14),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Container(
                        padding: new Thickness(10, 8),
                        decoration: new BoxDecoration(
                            Color: new Color(0xFFE8EEF7),
                            BorderRadius: BorderRadius.Circular(8)),
                        child: new Text(
                            "Use open/close controls to validate start/end drawer choreography and scrim behavior.",
                            fontSize: 12,
                            color: new Color(0xFF30404D))),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: "Open start",
                                onTap: () => OpenStartDrawer(context),
                                width: 104,
                                background: new Color(0xFFDDEBFF)),
                            BuildControlButton(
                                label: "Open end",
                                onTap: _showEndDrawer
                                    ? () => OpenEndDrawer(context)
                                    : null,
                                width: 98,
                                background: new Color(0xFFE6F2FF)),
                            BuildControlButton(
                                label: "Close all",
                                onTap: () => CloseAllDrawers(context),
                                width: 94,
                                background: new Color(0xFFF7E9E3)),
                        ]),
                    new Expanded(
                        child: new Center(
                            child: new Text(
                                "Drawer preview area",
                                fontSize: 13,
                                color: new Color(0x99000000)))),
                ]));
    }

    private Widget BuildDrawerPanel(bool isStartDrawer)
    {
        string title = isStartDrawer ? "Start drawer" : "End drawer";
        var accent = isStartDrawer ? new Color(0xFF0D47A1) : new Color(0xFF4A148C);

        return new Drawer(
            backgroundColor: _useWidgetOverrides
                ? (isStartDrawer ? new Color(0xFFEAF2FF) : new Color(0xFFF4ECFF))
                : null,
            elevation: _useWidgetOverrides ? (isStartDrawer ? 6 : 5) : null,
            shadowColor: _useWidgetOverrides ? (isStartDrawer ? new Color(0xFF305D8A) : new Color(0xFF5E3F86)) : null,
            width: _useWidgetOverrides ? (isStartDrawer ? 236 : 228) : null,
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
                                "Color, shape, elevation, and width show widget/theme/default precedence.",
                                fontSize: 12,
                                color: new Color(0x8A000000)),
                            new Text(
                                BuildControllerAlignmentLabel(context),
                                fontSize: 11,
                                color: new Color(0xFF607D8B)),
                            new Row(
                                spacing: 8,
                                children:
                                [
                                    BuildControlButton(
                                        label: "Close",
                                        onTap: isStartDrawer
                                            ? () => Scaffold.Of(context).CloseDrawer()
                                            : () => Scaffold.Of(context).CloseEndDrawer(),
                                        width: 84,
                                        background: new Color(0xFFE9EEF5)),
                                    BuildControlButton(
                                        label: isStartDrawer ? "Open end" : "Open start",
                                        onTap: isStartDrawer
                                            ? (_showEndDrawer ? () => OpenEndDrawer(context) : null)
                                            : (() => OpenStartDrawer(context)),
                                        width: 96,
                                        background: new Color(0xFFEFE8F8)),
                                ]),
                        ]))));
    }

    private static string BuildControllerAlignmentLabel(BuildContext context)
    {
        string alignment = DrawerController.Of(context).Alignment.ToString().ToLowerInvariant();
        return $"DrawerController alignment={alignment}";
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

    private void OpenStartDrawer(BuildContext context)
    {
        Scaffold.Of(context).OpenDrawer();
        SetState(() => _startOpens += 1);
    }

    private void OpenEndDrawer(BuildContext context)
    {
        Scaffold.Of(context).OpenEndDrawer();
        SetState(() => _endOpens += 1);
    }

    private static void CloseAllDrawers(BuildContext context)
    {
        var state = Scaffold.Of(context);
        state.CloseDrawer();
        state.CloseEndDrawer();
    }

    private void Reset()
    {
        SetState(() =>
        {
            _useMaterial3 = true;
            _useThemeOverrides = false;
            _useWidgetOverrides = false;
            _showEndDrawer = true;
            _startOpens = 0;
            _endOpens = 0;
        });
    }
}

internal sealed class ContextBuilder : StatelessWidget
{
    private readonly Func<BuildContext, Widget> _builder;

    public ContextBuilder(Func<BuildContext, Widget> builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public override Widget Build(BuildContext context)
    {
        return _builder(context);
    }
}
