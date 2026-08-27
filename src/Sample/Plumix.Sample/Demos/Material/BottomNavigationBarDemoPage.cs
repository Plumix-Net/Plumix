using System;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/bottom_navigation_bar_demo_page.dart

public sealed class BottomNavigationBarDemoPage : StatefulWidget
{
    public override State CreateState() => new BottomNavigationBarDemoPageState();

    private sealed class BottomNavigationBarDemoPageState : State
    {
        private int _currentIndex;
        private BottomNavigationBarType? _type;
        private BottomNavigationBarLandscapeLayout _landscapeLayout =
            BottomNavigationBarLandscapeLayout.Spread;
        private bool _showSelectedLabels = true;
        private bool _showUnselectedLabels = true;
        private bool _customColors;
        private bool _customIconThemes;
        private bool _legacyColorScheme = true;
        private bool _enableFeedback = true;
        private bool _themed;
        private int _tapCount;

        public override Widget Build(BuildContext context)
        {
            Widget bar = BuildBar();
            if (_themed)
            {
                var bottomTheme = new BottomNavigationBarThemeData(
                    BackgroundColor: Color.Parse("#FFE8DEF8"),
                    Elevation: 12.0,
                    SelectedItemColor: Color.Parse("#FF6750A4"),
                    UnselectedItemColor: Color.Parse("#FF7A757F"),
                    SelectedLabelStyle: new TextStyle(FontSize: 15.0, FontWeight: FontWeight.Bold),
                    UnselectedLabelStyle: new TextStyle(FontSize: 12.0),
                    MouseCursor: WidgetStateProperty<MouseCursor?>.ResolveWith(states =>
                        states.Contains(WidgetState.Selected)
                            ? SystemMouseCursors.Grab
                            : SystemMouseCursors.Click));
                bar = new BottomNavigationBarTheme(data: bottomTheme, child: bar);
            }

            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("BottomNavigationBar", fontSize: 20),
                    new Text(
                        "Fixed/shifting types with the animated flex and radial background splash, landscape "
                        + "spread/centered/linear layouts, selected and unselected label visibility, item and "
                        + "label-style colors under both color schemes, theme precedence, feedback, and "
                        + "per-state mouse cursors.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(TypeLabel(), CycleType),
                            ControlButton($"Landscape: {_landscapeLayout}", CycleLandscapeLayout),
                            ControlButton(_themed ? "Theme on" : "Theme off", () => SetState(() => _themed = !_themed)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(
                                _showSelectedLabels ? "Selected labels on" : "Selected labels off",
                                () => SetState(() => _showSelectedLabels = !_showSelectedLabels)),
                            ControlButton(
                                _showUnselectedLabels ? "Unselected labels on" : "Unselected labels off",
                                () => SetState(() => _showUnselectedLabels = !_showUnselectedLabels)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(
                                _customColors ? "Item colors on" : "Item colors off",
                                () => SetState(() => _customColors = !_customColors)),
                            ControlButton(
                                _customIconThemes ? "Icon themes on" : "Icon themes off",
                                () => SetState(() => _customIconThemes = !_customIconThemes)),
                            ControlButton(
                                _legacyColorScheme ? "Legacy colors" : "Label-style colors",
                                () => SetState(() => _legacyColorScheme = !_legacyColorScheme)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            ControlButton(
                                _enableFeedback ? "Feedback on" : "Feedback off",
                                () => SetState(() => _enableFeedback = !_enableFeedback)),
                        ]),
                    new Text($"Selected index: {_currentIndex}   |   taps: {_tapCount}", fontSize: 13),
                    bar,
                ]);
        }

        private Widget BuildBar()
        {
            return new BottomNavigationBar(
                currentIndex: _currentIndex,
                onTap: index => SetState(() =>
                {
                    _currentIndex = index;
                    _tapCount++;
                }),
                type: _type,
                landscapeLayout: _landscapeLayout,
                showSelectedLabels: _showSelectedLabels,
                showUnselectedLabels: _showUnselectedLabels,
                useLegacyColorScheme: _legacyColorScheme,
                enableFeedback: _enableFeedback,
                selectedItemColor: _customColors ? Color.Parse("#FF1B5E20") : null,
                unselectedItemColor: _customColors ? Color.Parse("#FF8D6E63") : null,
                selectedLabelStyle: _customColors ? new TextStyle(Color: Color.Parse("#FFB3261E")) : null,
                unselectedLabelStyle: _customColors ? new TextStyle(Color: Color.Parse("#FF4A6572")) : null,
                selectedIconTheme: _customIconThemes
                    ? new IconThemeData(Color: Color.Parse("#FF0B57D0"), Size: 30.0)
                    : null,
                unselectedIconTheme: _customIconThemes
                    ? new IconThemeData(Color: Color.Parse("#FF9AA0A6"), Size: 20.0)
                    : null,
                items:
                [
                    new BottomNavigationBarItem(
                        icon: new Icon(Icons.StarOutline),
                        activeIcon: new Icon(Icons.Star),
                        label: "Favorites",
                        tooltip: "Saved items",
                        backgroundColor: Color.Parse("#FF1565C0")),
                    new BottomNavigationBarItem(
                        icon: new Icon(Icons.Menu),
                        label: "Browse",
                        backgroundColor: Color.Parse("#FF2E7D32")),
                    new BottomNavigationBarItem(
                        icon: new Icon(Icons.InfoOutline),
                        label: "About",
                        semanticsLabel: "About this sample",
                        backgroundColor: Color.Parse("#FF6A1B9A")),
                    new BottomNavigationBarItem(
                        icon: new Icon(Icons.Check),
                        label: "Done",
                        backgroundColor: Color.Parse("#FFAD1457")),
                ]);
        }

        private string TypeLabel()
        {
            return _type switch
            {
                null => "Type: automatic",
                BottomNavigationBarType.Fixed => "Type: fixed",
                _ => "Type: shifting",
            };
        }

        private void CycleType()
        {
            SetState(() =>
            {
                _type = _type switch
                {
                    null => BottomNavigationBarType.Fixed,
                    BottomNavigationBarType.Fixed => BottomNavigationBarType.Shifting,
                    _ => null,
                };
            });
        }

        private void CycleLandscapeLayout()
        {
            SetState(() =>
            {
                _landscapeLayout = _landscapeLayout switch
                {
                    BottomNavigationBarLandscapeLayout.Spread => BottomNavigationBarLandscapeLayout.Centered,
                    BottomNavigationBarLandscapeLayout.Centered => BottomNavigationBarLandscapeLayout.Linear,
                    _ => BottomNavigationBarLandscapeLayout.Spread,
                };
            });
        }

        private static Widget ControlButton(string label, Action onPressed)
        {
            return new OutlinedButton(
                onPressed: onPressed,
                child: new Text(label, fontSize: 12));
        }
    }
}
