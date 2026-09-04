using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/sliver_app_bar_demo_page.dart

public sealed class SliverAppBarDemoPage : StatefulWidget
{
    public override State CreateState() => new SliverAppBarDemoPageState();

    private sealed class SliverAppBarDemoPageState : State
    {
        private int _variant;
        private bool _pinned = true;
        private bool _floating;
        private bool _snap;
        private bool _stretch;

        public override Widget Build(BuildContext context) => new CustomScrollView(
            slivers:
            [
                BuildAppBar(),
                new SliverToBoxAdapter(
                    new Padding(
                        new Thickness(12),
                        new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 8,
                            children:
                            [
                                new Text("SliverAppBar + FlexibleSpaceBar", fontSize: 20),
                                new Text(
                                    "Collapse, parallax, floating reveal, pinned extent, snap contract, and Material 3 medium/large variants.",
                                    fontSize: 14,
                                    color: Colors.DimGray),
                                new Row(
                                    spacing: 6,
                                    children:
                                    [
                                        ToggleButton(VariantLabel(), () => SetState(() => _variant = (_variant + 1) % 3)),
                                        ToggleButton(_pinned ? "Pinned on" : "Pinned off", () => SetState(() => _pinned = !_pinned)),
                                        ToggleButton(_floating ? "Floating on" : "Floating off", ToggleFloating),
                                    ]),
                                new Row(
                                    spacing: 6,
                                    children:
                                    [
                                        ToggleButton(_snap ? "Snap on" : "Snap off", ToggleSnap),
                                        ToggleButton(_stretch ? "Stretch on" : "Stretch off", () => SetState(() => _stretch = !_stretch)),
                                    ]),
                            ]))),
                new SliverPadding(
                    padding: new Thickness(12, 0, 12, 20),
                    sliver: SliverList.Builder(
                        itemCount: 24,
                        itemBuilder: (_, index) => new Container(
                            color: index % 2 == 0 ? Color.Parse("#FFF3EDF7") : Color.Parse("#FFE8DEF8"),
                            margin: new Thickness(0, 0, 0, 6),
                            padding: new Thickness(14),
                            child: new Text($"Scrollable row #{index + 1}")),
                        addAutomaticKeepAlives: false)),
            ]);

        private Widget BuildAppBar()
        {
            var title = new Text(_variant == 0 ? "Flexible space" : _variant == 1 ? "Medium app bar" : "Large app bar");
            var flexible = new FlexibleSpaceBar(
                title: title,
                background: new DecoratedBox(
                    new BoxDecoration(Color: Color.Parse("#FF6750A4")),
                    child: new Align(
                        alignment: Alignment.Center,
                        child: new Text("PARALLAX", fontSize: 30, color: Colors.White))),
                stretchModes:
                [
                    StretchMode.ZoomBackground,
                    StretchMode.BlurBackground,
                    StretchMode.FadeTitle,
                ]);

            return _variant switch
            {
                1 => SliverAppBar.Medium(
                    title: title,
                    pinned: _pinned,
                    floating: _floating,
                    snap: _snap,
                    stretch: _stretch),
                2 => SliverAppBar.Large(
                    title: title,
                    pinned: _pinned,
                    floating: _floating,
                    snap: _snap,
                    stretch: _stretch),
                _ => new SliverAppBar(
                    pinned: _pinned,
                    floating: _floating,
                    snap: _snap,
                    stretch: _stretch,
                    expandedHeight: 220,
                    flexibleSpace: flexible),
            };
        }

        private string VariantLabel() => _variant switch
        {
            1 => "Medium",
            2 => "Large",
            _ => "Regular",
        };

        private void ToggleFloating() => SetState(() =>
        {
            _floating = !_floating;
            if (!_floating) _snap = false;
        });

        private void ToggleSnap() => SetState(() =>
        {
            if (!_floating) _floating = true;
            _snap = !_snap;
        });

        private static Widget ToggleButton(string label, Action onPressed) =>
            new TextButton(new Text(label, fontSize: 12), onPressed);
    }
}
