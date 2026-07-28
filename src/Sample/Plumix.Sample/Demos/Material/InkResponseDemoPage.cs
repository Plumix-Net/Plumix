using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/ink_response_demo_page.dart

public sealed class InkResponseDemoPage : StatefulWidget
{
    public override State CreateState() => new InkResponseDemoPageState();

    private sealed class InkResponseDemoPageState : State
    {
        private bool _enabled = true;
        private bool _customOverlay = true;
        private int _splashMode;
        private int _responseTaps;
        private int _wellTaps;
        private int _secondaryTaps;
        private string _interaction = "Ready";

        private InteractiveInkFeatureFactory SplashFactory => _splashMode switch
        {
            0 => InkRipple.SplashFactory,
            1 => InkSparkle.ConstantTurbulenceSeedSplashFactory,
            2 => Plumix.Material.InkSplash.SplashFactory,
            _ => NoSplash.SplashFactory,
        };

        private string SplashName => _splashMode switch
        {
            0 => "InkRipple",
            1 => "InkSparkle",
            2 => "InkSplash",
            _ => "NoSplash",
        };

        public override Widget Build(BuildContext context)
        {
            var overlay = _customOverlay
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Pressed) ? Color.Parse("#556750A4")
                    : states.HasFlag(MaterialState.Hovered) ? Color.Parse("#336750A4")
                    : states.HasFlag(MaterialState.Focused) ? Color.Parse("#446750A4")
                    : null)
                : null;

            return new SingleChildScrollView(
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 14,
                    children:
                    [
                        new Text("InkResponse + InkWell", fontSize: 20, color: Colors.Black),
                        new Text(
                            "Circle/uncontained versus rectangle/contained ink, selectable ripple/sparkle/splash/"
                            + "no-splash factories, primary + secondary gestures, hover/focus, and overlay states.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Row(
                            mainAxisAlignment: MainAxisAlignment.SpaceAround,
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children:
                            [
                                BuildInkResponse(overlay),
                                BuildInkWell(overlay),
                            ]),
                        new Text(
                            $"InkResponse taps: {_responseTaps}  |  InkWell taps: {_wellTaps}  |  secondary: {_secondaryTaps}",
                            fontSize: 14,
                            color: Colors.Black),
                        new Text($"Interaction: {_interaction}", fontSize: 13, color: Colors.DimGray),
                        new OutlinedButton(
                            onPressed: () => SetState(() => _splashMode = (_splashMode + 1) % 4),
                            child: new Text($"Splash factory: {SplashName}")),
                        new Row(
                            spacing: 10,
                            children:
                            [
                                new Expanded(
                                    child: new FilledButton(
                                        onPressed: () => SetState(() => _enabled = !_enabled),
                                        child: new Text(_enabled ? "Disable ink" : "Enable ink"))),
                                new Expanded(
                                    child: new OutlinedButton(
                                        onPressed: () => SetState(() => _customOverlay = !_customOverlay),
                                        child: new Text(_customOverlay ? "Use theme colors" : "Use custom overlay"))),
                            ]),
                    ]));
        }

        private Widget BuildInkResponse(MaterialStateProperty<Color?>? overlay)
        {
            return new Column(
                spacing: 8,
                children:
                [
                    new Text("InkResponse", fontSize: 14, color: Colors.Black),
                    new Ink(
                        width: 112,
                        height: 112,
                        decoration: new BoxDecoration(
                            Color: Color.Parse("#FFEADDFF"),
                            Shape: BoxShape.Circle),
                        child: new InkResponse(
                            onTap: _enabled ? () => SetState(() => _responseTaps++) : null,
                            onSecondaryTap: _enabled ? HandleSecondaryTap : null,
                            onHover: value => SetState(() => _interaction = $"InkResponse hover: {value}"),
                            onHighlightChanged: value => SetState(() => _interaction = $"InkResponse pressed: {value}"),
                            overlayColor: overlay,
                            splashFactory: SplashFactory,
                            radius: 58,
                            child: new Center(child: new Icon(Icons.Star, size: 32, color: Color.Parse("#FF6750A4"))))),
                ]);
        }

        private Widget BuildInkWell(MaterialStateProperty<Color?>? overlay)
        {
            return new Column(
                spacing: 8,
                children:
                [
                    new Text("InkWell", fontSize: 14, color: Colors.Black),
                    new Ink(
                        width: 150,
                        height: 96,
                        decoration: new BoxDecoration(
                            Color: Color.Parse("#FFD7E3FF"),
                            BorderRadius: BorderRadius.Circular(18)),
                        child: new InkWell(
                            onTap: _enabled ? () => SetState(() => _wellTaps++) : null,
                            onLongPress: _enabled ? () => SetState(() => _interaction = "InkWell long press") : null,
                            onSecondaryTap: _enabled ? HandleSecondaryTap : null,
                            onHover: value => SetState(() => _interaction = $"InkWell hover: {value}"),
                            onHighlightChanged: value => SetState(() => _interaction = $"InkWell pressed: {value}"),
                            overlayColor: overlay,
                            splashFactory: SplashFactory,
                            borderRadius: BorderRadius.Circular(18),
                            child: new Center(child: new Text("Tap / hold", fontSize: 15, color: Colors.Black)))),
                ]);
        }

        private void HandleSecondaryTap()
        {
            SetState(() =>
            {
                _secondaryTaps++;
                _interaction = "Secondary tap";
            });
        }
    }
}
