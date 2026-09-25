using Avalonia;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;

// Dart parity source (reference):
// flutter/bin/cache/pkg/sky_engine/lib/ui/window.dart (FlutterView; the host supplies the engine metrics)

namespace Plumix.Widgets;

/// <summary>
/// A view into which a Flutter application is rendered: the platform's window or surface, with
/// its metrics in physical pixels.
/// </summary>
/// <remarks>
/// dart:ui's <c>FlutterView</c>. Flutter's engine owns the metrics and writes them when the
/// platform reports a change; a Plumix host does the same through <see cref="UpdateMetrics"/>, and
/// a test supplies whatever metrics it needs the same way (Flutter's <c>TestFlutterView</c>).
/// Rendering into the view (<see cref="Render"/>) hands over the root layer of a composited frame:
/// a host that owns the view draws that layer from its own render pass, and a view no host backs
/// composites it headlessly. The semantics feed (<c>updateSemantics</c>) is published through
/// <see cref="SemanticsUpdated"/>.
/// </remarks>
public sealed class FlutterView
{
    private BoxConstraints? _physicalConstraints;

    /// <summary>Creates a view with the given metrics.</summary>
    public FlutterView(
        Size physicalSize,
        double devicePixelRatio = 1.0,
        int viewId = 0,
        BoxConstraints? physicalConstraints = null)
    {
        if (!double.IsFinite(devicePixelRatio) || devicePixelRatio <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(devicePixelRatio));
        }

        PhysicalSize = physicalSize;
        DevicePixelRatio = devicePixelRatio;
        ViewId = viewId;
        _physicalConstraints = physicalConstraints;
    }

    /// <summary>The dimensions of the view in physical pixels.</summary>
    public Size PhysicalSize { get; private set; }

    /// <summary>
    /// The constraints the view's content must satisfy, in physical pixels. Tight around
    /// <see cref="PhysicalSize"/> unless the platform allows the view to size itself.
    /// </summary>
    /// <remarks>dart:ui's <c>FlutterView.physicalConstraints</c>.</remarks>
    public BoxConstraints PhysicalConstraints => _physicalConstraints ?? BoxConstraints.Tight(PhysicalSize);

    /// <summary>The number of physical pixels per logical pixel.</summary>
    public double DevicePixelRatio { get; private set; }

    /// <summary>The platform identifier for this view.</summary>
    public int ViewId { get; private set; }

    /// <summary>
    /// The parts of the view obscured by system UI that content should avoid, in physical pixels.
    /// </summary>
    /// <remarks>dart:ui's <c>FlutterView.padding</c>.</remarks>
    public Thickness Padding { get; private set; }

    /// <summary>
    /// The parts of the view completely obscured by system UI, such as a keyboard, in physical pixels.
    /// </summary>
    /// <remarks>dart:ui's <c>FlutterView.viewInsets</c>.</remarks>
    public Thickness ViewInsets { get; private set; }

    /// <summary>
    /// The parts of the view obscured by system UI regardless of transient insets, in physical
    /// pixels.
    /// </summary>
    /// <remarks>dart:ui's <c>FlutterView.viewPadding</c>.</remarks>
    public Thickness ViewPadding { get; private set; }

    /// <summary>The areas the system reserves for its own gestures, in physical pixels.</summary>
    /// <remarks>dart:ui's <c>FlutterView.systemGestureInsets</c>.</remarks>
    public Thickness SystemGestureInsets { get; private set; }

    /// <summary>The platform's gesture settings, when the host reports them.</summary>
    /// <remarks>dart:ui's <c>FlutterView.gestureSettings</c>.</remarks>
    public DeviceGestureSettings? GestureSettings { get; private set; }

    /// <summary>The display features (folds, cutouts) that intersect the view.</summary>
    /// <remarks>dart:ui's <c>FlutterView.displayFeatures</c>.</remarks>
    public IReadOnlyList<DisplayFeature>? DisplayFeatures { get; private set; }

    /// <summary>The corner radii of the display the view is on, when the host reports them.</summary>
    /// <remarks>dart:ui's <c>FlutterView.displayCornerRadii</c>.</remarks>
    public BorderRadius? DisplayCornerRadii { get; private set; }

    /// <summary>
    /// Raised with every <see cref="SemanticsUpdate"/> the view's pipeline produces.
    /// </summary>
    /// <remarks>dart:ui's <c>FlutterView.updateSemantics</c>, which hands the update to the engine.</remarks>
    public event Action<SemanticsUpdate>? SemanticsUpdated;

    /// <summary>
    /// Writes new metrics, as the engine does when the platform reports a change. Arguments left
    /// <see langword="null"/> keep their current value.
    /// </summary>
    public void UpdateMetrics(
        Size? physicalSize = null,
        double? devicePixelRatio = null,
        int? viewId = null,
        BoxConstraints? physicalConstraints = null,
        Thickness? padding = null,
        Thickness? viewInsets = null,
        Thickness? viewPadding = null,
        Thickness? systemGestureInsets = null,
        DeviceGestureSettings? gestureSettings = null,
        IReadOnlyList<DisplayFeature>? displayFeatures = null,
        BorderRadius? displayCornerRadii = null)
    {
        if (devicePixelRatio is { } ratio && (!double.IsFinite(ratio) || ratio <= 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(devicePixelRatio));
        }

        PhysicalSize = physicalSize ?? PhysicalSize;
        DevicePixelRatio = devicePixelRatio ?? DevicePixelRatio;
        ViewId = viewId ?? ViewId;
        if (physicalConstraints is not null || physicalSize is not null)
        {
            _physicalConstraints = physicalConstraints;
        }

        Padding = padding ?? Padding;
        ViewInsets = viewInsets ?? ViewInsets;
        ViewPadding = viewPadding ?? ViewPadding;
        SystemGestureInsets = systemGestureInsets ?? SystemGestureInsets;
        GestureSettings = gestureSettings ?? GestureSettings;
        DisplayFeatures = displayFeatures ?? DisplayFeatures;
        DisplayCornerRadii = displayCornerRadii ?? DisplayCornerRadii;
    }

    /// <summary>Publishes a semantics update produced by this view's pipeline.</summary>
    /// <remarks>dart:ui's <c>FlutterView.updateSemantics</c>.</remarks>
    public void UpdateSemantics(SemanticsUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        SemanticsUpdated?.Invoke(update);
    }

    /// <summary>
    /// Raised by <see cref="Render"/> with the root layer of the frame to show. A host that renders
    /// the view subscribes and draws the layer from its next render pass.
    /// </summary>
    public event Action<OffsetLayer>? RenderRequested;

    /// <summary>Whether a host renders this view, i.e. <see cref="RenderRequested"/> has a subscriber.</summary>
    internal bool HasRenderer => RenderRequested is not null;

    /// <summary>Updates the view's rendering with the frame whose root layer is <paramref name="scene"/>.</summary>
    /// <remarks>
    /// dart:ui's <c>FlutterView.render</c>. Dart's scene is already built when it arrives; a Plumix
    /// layer builds its scene straight into the host's drawing context, so a view that no host renders
    /// runs the layer tree's composition work headlessly instead.
    /// </remarks>
    public void Render(OffsetLayer scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (RenderRequested is { } renderRequested)
        {
            renderRequested(scene);
            return;
        }

        scene.BuildScene(null);
    }

    /// <inheritdoc />
    public override string ToString() => $"FlutterView(id: {ViewId}, {PhysicalSize} at {DevicePixelRatio}x)";
}
