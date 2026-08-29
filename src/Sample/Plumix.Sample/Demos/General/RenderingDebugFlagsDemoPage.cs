using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/rendering_debug_flags_demo_page.dart (exact sample parity)

public sealed class RenderingDebugFlagsDemoPage : StatefulWidget
{
    public override State CreateState() => new RenderingDebugFlagsDemoPageState();
}

internal sealed class RenderingDebugFlagsDemoPageState : State
{
    private int _repaintToken;

    public override void Dispose()
    {
        RenderingDebug.PaintSizeEnabled = false;
        RenderingDebug.PaintBaselinesEnabled = false;
        RenderingDebug.PaintPointersEnabled = false;
        RenderingDebug.PaintLayerBordersEnabled = false;
        RenderingDebug.DisableClipLayers = false;
        RenderingDebug.DisableOpacityLayers = false;
        RenderingDebug.DisablePhysicalShapeLayers = false;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("rendering/debug.dart flags", fontSize: 20, color: Colors.Black),
                new Text(
                    "Each toggle flips the matching library-level debug variable and rebuilds the probe below.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        BuildToggle(
                            "paint size",
                            RenderingDebug.PaintSizeEnabled,
                            () => RenderingDebug.PaintSizeEnabled = !RenderingDebug.PaintSizeEnabled),
                        BuildToggle(
                            "baselines",
                            RenderingDebug.PaintBaselinesEnabled,
                            () => RenderingDebug.PaintBaselinesEnabled = !RenderingDebug.PaintBaselinesEnabled),
                        BuildToggle(
                            "pointers",
                            RenderingDebug.PaintPointersEnabled,
                            () => RenderingDebug.PaintPointersEnabled = !RenderingDebug.PaintPointersEnabled),
                        BuildToggle(
                            "layer borders",
                            RenderingDebug.PaintLayerBordersEnabled,
                            () => RenderingDebug.PaintLayerBordersEnabled = !RenderingDebug.PaintLayerBordersEnabled),
                        BuildToggle(
                            "no clips",
                            RenderingDebug.DisableClipLayers,
                            () => RenderingDebug.DisableClipLayers = !RenderingDebug.DisableClipLayers),
                        BuildToggle(
                            "no opacity",
                            RenderingDebug.DisableOpacityLayers,
                            () => RenderingDebug.DisableOpacityLayers = !RenderingDebug.DisableOpacityLayers),
                        BuildToggle(
                            "no shadows",
                            RenderingDebug.DisablePhysicalShapeLayers,
                            () => RenderingDebug.DisablePhysicalShapeLayers =
                                !RenderingDebug.DisablePhysicalShapeLayers),
                    ]),
                new Expanded(child: BuildProbe()),
            ]);
    }

    private Widget BuildProbe()
    {
        return new KeyedSubtree(
            key: new ValueKey<int>(_repaintToken),
            child: new Container(
                color: Colors.White,
                padding: new Thickness(16),
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    spacing: 16,
                    children:
                    [
                        new Text(
                            "Padding draws its construction lines, clips get a scissors marker.",
                            fontSize: 12,
                            color: Colors.DarkSlateGray),
                        new Padding(
                            insets: new Thickness(20, 12),
                            child: new ClipRRect(
                                borderRadius: BorderRadius.Circular(12),
                                child: new Container(
                                    width: 200,
                                    height: 64,
                                    color: Color.Parse("#FFB3E5FC"),
                                    alignment: Alignment.Center,
                                    child: new Text("clipped", fontSize: 16, color: Colors.Black)))),
                        new Opacity(
                            opacity: 0.45,
                            child: new PhysicalModel(
                                color: Color.Parse("#FFFFE082"),
                                elevation: 8,
                                borderRadius: BorderRadius.Circular(8),
                                child: new SizedBox(
                                    width: 200,
                                    height: 56,
                                    child: new Center(
                                        child: new Text(
                                            "elevated + 45% opacity",
                                            fontSize: 14,
                                            color: Colors.Black))))),
                        new Listener(
                            behavior: HitTestBehavior.Opaque,
                            child: new Container(
                                width: 200,
                                height: 44,
                                color: Color.Parse("#FFDCE3ED"),
                                alignment: Alignment.Center,
                                child: new Text("press and hold me", fontSize: 14, color: Colors.Black))),
                    ])));
    }

    private Widget BuildToggle(string label, bool enabled, Action toggle)
    {
        return new CounterTapButton(
            label: enabled ? $"{label}: on" : $"{label}: off",
            onTap: () => SetState(() =>
            {
                toggle();
                _repaintToken += 1;
            }),
            background: enabled ? Color.Parse("#FF9FC5E8") : Color.Parse("#FFDCE3ED"),
            foreground: Colors.Black,
            fontSize: 12,
            padding: new Thickness(10, 8));
    }
}
