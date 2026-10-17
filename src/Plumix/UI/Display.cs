using Avalonia;

// Dart parity source (reference): flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart
// (Display, adapted)

namespace Plumix.UI;

/// <summary>A configurable display that a <see cref="Widgets.FlutterView"/> renders on.</summary>
/// <remarks>
/// dart:ui's <c>Display</c>: the windowing API's <c>RegularWindowController.setFullscreen</c> names the
/// display to go fullscreen on. Plumix's engine role is played by its hosts, which report a display
/// for each screen they know about.
/// </remarks>
public sealed class Display
{
    /// <summary>Creates a display with the given metrics.</summary>
    public Display(int id, double devicePixelRatio, Size size, double refreshRate)
    {
        Id = id;
        DevicePixelRatio = devicePixelRatio;
        Size = size;
        RefreshRate = refreshRate;
    }

    /// <summary>A unique identifier for this display.</summary>
    public int Id { get; }

    /// <summary>The device pixel ratio of this display.</summary>
    public double DevicePixelRatio { get; }

    /// <summary>The physical size of this display.</summary>
    public Size Size { get; }

    /// <summary>The refresh rate in FPS of this display.</summary>
    public double RefreshRate { get; }

    public override string ToString() =>
        FormattableString.Invariant($"Display(id: {Id}, size: {Size.Width}x{Size.Height}, ")
        + FormattableString.Invariant($"devicePixelRatio: {DevicePixelRatio}, refreshRate: {RefreshRate})");
}
