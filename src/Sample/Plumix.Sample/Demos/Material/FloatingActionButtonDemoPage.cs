using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/floating_action_button_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class FloatingActionButtonDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new FloatingActionButtonDemoPageState();
    }
}

internal sealed class FloatingActionButtonDemoPageState : State
{
    private bool _enabled = true;
    private bool _extendedOpen = true;
    private bool _useMaterial3 = true;
    private bool _dockedLocation;
    private int _regularTaps;
    private int _smallTaps;
    private int _largeTaps;
    private int _extendedTaps;
    private int _themedTaps;

    public override Widget Build(BuildContext context)
    {
        var ambientTheme = Theme.Of(context);
        var modeData = new ThemeData(
            platform: ambientTheme.Platform,
            colorScheme: ambientTheme.ColorScheme,
            textTheme: ambientTheme.TextTheme,
            useMaterial3: _useMaterial3,
            materialTapTargetSize: ambientTheme.MaterialTapTargetSize);
        var themedData = modeData with
        {
            FloatingActionButtonTheme = new FloatingActionButtonThemeData(
                ForegroundColor: Colors.White,
                BackgroundColor: Color.Parse("#FF00639B"),
                SizeConstraints: TightConstraints(64, 64),
                ExtendedSizeConstraints: new BoxConstraints(
                    MinWidth: 0,
                    MaxWidth: double.PositiveInfinity,
                    MinHeight: 60,
                    MaxHeight: 60)),
        };

        return new Theme(
            data: modeData,
            child: new SingleChildScrollView(
                child: new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("FloatingActionButton baseline", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Regular/small/large/extended FAB defaults, elevation states, and theme overrides.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _enabled ? "Enabled" : "Disabled",
                                onTap: ToggleEnabled,
                                width: 108,
                                background: Color.Parse("#FFE9F0FF")),
                            BuildControlButton(
                                label: _extendedOpen ? "Extended: open" : "Extended: icon",
                                onTap: ToggleExtended,
                                width: 146,
                                background: Color.Parse("#FFEAE4FF")),
                            BuildControlButton(
                                label: _useMaterial3 ? "Material 3" : "Material 2",
                                onTap: ToggleMaterialMode,
                                width: 104,
                                background: Color.Parse("#FFE4F3E8")),
                            BuildControlButton(
                                label: _dockedLocation ? "Location: centerDocked" : "Location: centerFloat",
                                onTap: ToggleLocation,
                                width: 190,
                                background: Color.Parse("#FFFFE7D6")),
                            BuildControlButton(
                                label: "Reset",
                                onTap: ResetCounters,
                                width: 88,
                                background: Color.Parse("#FFF3E8D8")),
                        ]),
                    new Text(
                        $"mode={(_useMaterial3 ? "M3" : "M2")}, enabled={(_enabled ? "true" : "false")}, "
                        + $"extended={(_extendedOpen ? "open" : "icon")}, regular={_regularTaps}, "
                        + $"small={_smallTaps}, large={_largeTaps}, extendedTaps={_extendedTaps}, "
                        + $"themed={_themedTaps}, "
                        + $"location={(_dockedLocation ? "centerDocked" : "centerFloat")}",
                        fontSize: 12,
                        color: Color.Parse("#FF607D8B")),
                    new SizedBox(
                        height: 160,
                        child: new Scaffold(
                            body: new Center(child: new Text("Scaffold: animated FAB location")),
                            bottomNavigationBar: new BottomAppBar(
                                shape: new CircularNotchedRectangle(),
                                notchMargin: 6),
                            floatingActionButtonLocation: _dockedLocation
                                ? FloatingActionButtonLocation.CenterDocked
                                : FloatingActionButtonLocation.CenterFloat,
                            floatingActionButton: new FloatingActionButton(
                                child: new Icon(Icons.Add),
                                tooltip: "Center float action",
                                onPressed: _enabled ? OnRegularTap : null))),
                    new Column(
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 8,
                        children:
                        [
                            BuildProbeCard(
                                title: "Regular",
                                subtitle: "56x56",
                                fab: new FloatingActionButton(
                                    child: new Icon(Icons.Add),
                                    onPressed: _enabled ? OnRegularTap : null,
                                    heroTag: null)),
                            BuildProbeCard(
                                title: "Small",
                                subtitle: "40x40",
                                fab: FloatingActionButton.Small(
                                    child: new Icon(Icons.Menu),
                                    onPressed: _enabled ? OnSmallTap : null,
                                    heroTag: null)),
                            BuildProbeCard(
                                title: "Large",
                                subtitle: "96x96",
                                fab: FloatingActionButton.Large(
                                    child: new Icon(Icons.Star),
                                    onPressed: _enabled ? OnLargeTap : null,
                                    heroTag: null)),
                        ]),
                    BuildProbeCard(
                        title: "Extended",
                        subtitle: "label + icon / collapsed icon",
                        fab: FloatingActionButton.Extended(
                            label: new Text("Create"),
                            icon: new Icon(Icons.Add),
                            isExtended: _extendedOpen,
                            onPressed: _enabled ? OnExtendedTap : null,
                            heroTag: null)),
                    new Theme(
                        data: themedData,
                        child: BuildProbeCard(
                            title: "Theme override",
                            subtitle: "FloatingActionButtonTheme colors + size",
                            fab: new FloatingActionButton(
                                child: new Icon(Icons.InfoOutline),
                                onPressed: _enabled ? OnThemedTap : null,
                                heroTag: null))),
                ])));
    }

    private Widget BuildProbeCard(string title, string subtitle, Widget fab)
    {
        return new Container(
            padding: new Thickness(10, 8),
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFF1F4F9"),
                BorderRadius: BorderRadius.Circular(10),
                Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#FFD6DEEA"), 1))),
            child: new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text(title, fontSize: 13, color: Colors.Black),
                    new Text(subtitle, fontSize: 12, color: Color.Parse("#8A000000")),
                    new SizedBox(
                        height: 112,
                        child: new Center(child: fab)),
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
                child: new Text(
                    label,
                    fontSize: 12),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(64, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
    }

    private static BoxConstraints TightConstraints(double width, double height)
    {
        return new BoxConstraints(
            MinWidth: width,
            MaxWidth: width,
            MinHeight: height,
            MaxHeight: height);
    }

    private void ToggleEnabled()
    {
        SetState(() => _enabled = !_enabled);
    }

    private void ToggleExtended()
    {
        SetState(() => _extendedOpen = !_extendedOpen);
    }

    private void ToggleMaterialMode()
    {
        SetState(() => _useMaterial3 = !_useMaterial3);
    }

    private void ToggleLocation()
    {
        SetState(() => _dockedLocation = !_dockedLocation);
    }

    private void ResetCounters()
    {
        SetState(() =>
        {
            _enabled = true;
            _extendedOpen = true;
            _useMaterial3 = true;
            _dockedLocation = false;
            _regularTaps = 0;
            _smallTaps = 0;
            _largeTaps = 0;
            _extendedTaps = 0;
            _themedTaps = 0;
        });
    }

    private void OnRegularTap()
    {
        SetState(() => _regularTaps += 1);
    }

    private void OnSmallTap()
    {
        SetState(() => _smallTaps += 1);
    }

    private void OnLargeTap()
    {
        SetState(() => _largeTaps += 1);
    }

    private void OnExtendedTap()
    {
        SetState(() => _extendedTaps += 1);
    }

    private void OnThemedTap()
    {
        SetState(() => _themedTaps += 1);
    }
}
