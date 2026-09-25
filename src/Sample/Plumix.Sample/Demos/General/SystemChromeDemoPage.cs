using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/system_chrome_demo_page.dart (exact sample parity)

public sealed class SystemChromeDemoPage : StatefulWidget
{
    public override State CreateState() => new SystemChromeDemoPageState();
}

internal sealed class SystemChromeDemoPageState : State
{
    private bool _alternateTitle;
    private bool _darkRegion;
    private string _lastRequest = "none";
    private string _overlaysVisible = "not reported";

    public override void InitState()
    {
        base.InitState();
        _ = SystemChrome.SetSystemUIChangeCallback(HandleSystemUIChange);
    }

    public override void Dispose()
    {
        _ = SystemChrome.SetSystemUIChangeCallback(null);
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 16,
                children:
                [
                    new Text("SystemChrome", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Requests go to the platform over flutter/platform; a platform that does not " +
                        "implement one ignores it. The window title follows the Title widget.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    BuildTitleSection(),
                    BuildRegionSection(),
                    BuildOrientationSection(),
                    BuildModeSection(),
                    new Text($"Last request: {_lastRequest}", color: new Color(0xFF31506F)),
                    new Text($"System overlays visible: {_overlaysVisible}", color: new Color(0xFF31506F)),
                ]));
    }

    private Widget BuildTitleSection()
    {
        string label = _alternateTitle ? "System chrome demo" : "Plumix gallery";
        return BuildSection(
            "Application switcher",
            [
                new Title(
                    title: label,
                    color: new Color(0xFF2A9D8F),
                    child: new Text($"Title: {label}", color: new Color(0xFF31506F))),
                BuildButton("Toggle title", () => SetState(() => _alternateTitle = !_alternateTitle)),
            ]);
    }

    private Widget BuildRegionSection()
    {
        SystemUiOverlayStyle style = _darkRegion ? SystemUiOverlayStyle.Light : SystemUiOverlayStyle.Dark;
        return BuildSection(
            "AnnotatedRegion",
            [
                new AnnotatedRegion<SystemUiOverlayStyle>(
                    value: style,
                    child: new Container(
                        height: 56,
                        color: _darkRegion ? new Color(0xFF264653) : new Color(0xFFE7EDF6),
                        alignment: Alignment.Center,
                        child: new Text(
                            _darkRegion
                                ? "SystemUiOverlayStyle.light (applies under the status bar)"
                                : "SystemUiOverlayStyle.dark (applies under the status bar)",
                            color: _darkRegion ? Colors.White : Colors.Black))),
                BuildButton("Toggle region style", () => SetState(() => _darkRegion = !_darkRegion)),
            ]);
    }

    private Widget BuildOrientationSection()
    {
        return BuildSection(
            "Preferred orientations",
            [
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildButton("Portrait", () => Request(
                            "setPreferredOrientations(portraitUp)",
                            SystemChrome.SetPreferredOrientations([DeviceOrientation.PortraitUp]))),
                        BuildButton("Landscape", () => Request(
                            "setPreferredOrientations(landscapeLeft, landscapeRight)",
                            SystemChrome.SetPreferredOrientations(
                                [DeviceOrientation.LandscapeLeft, DeviceOrientation.LandscapeRight]))),
                        BuildButton("Any", () => Request(
                            "setPreferredOrientations([])",
                            SystemChrome.SetPreferredOrientations([]))),
                    ]),
            ]);
    }

    private Widget BuildModeSection()
    {
        return BuildSection(
            "System UI mode",
            [
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildButton("Edge to edge", () => Request(
                            "setEnabledSystemUIMode(edgeToEdge)",
                            SystemChrome.SetEnabledSystemUIMode(SystemUiMode.EdgeToEdge))),
                        BuildButton("Immersive sticky", () => Request(
                            "setEnabledSystemUIMode(immersiveSticky)",
                            SystemChrome.SetEnabledSystemUIMode(SystemUiMode.ImmersiveSticky))),
                        BuildButton("Top bar only", () => Request(
                            "setEnabledSystemUIMode(manual, [top])",
                            SystemChrome.SetEnabledSystemUIMode(SystemUiMode.Manual, [SystemUiOverlay.Top]))),
                        BuildButton("Restore overlays", () => Request(
                            "restoreSystemUIOverlays()",
                            SystemChrome.RestoreSystemUIOverlays())),
                    ]),
            ]);
    }

    private static Widget BuildSection(string title, List<Widget> children)
    {
        return new Container(
            color: new Color(0xFFF4F7FA),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children: [new Text(title, fontSize: 16, color: Colors.Black), .. children]));
    }

    private static Widget BuildButton(string label, Action onPressed)
    {
        return new TextButton(
            onPressed: onPressed,
            child: new Text(label),
            style: TextButton.StyleFrom(
                backgroundColor: new Color(0xFFDCE3ED)));
    }

    private void Request(string description, Task request)
    {
        _ = request;
        SetState(() => _lastRequest = description);
    }

    private Task HandleSystemUIChange(bool systemOverlaysAreVisible)
    {
        if (Mounted)
        {
            SetState(() => _overlaysVisible = systemOverlaysAreVisible ? "yes" : "no");
        }

        return Task.CompletedTask;
    }
}
