using System.Globalization;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_metrics.dart

namespace Plumix.Widgets;

/// <summary>
/// A description of a <see cref="Scrollable"/>'s contents, useful for modeling the state of its
/// viewport.
/// </summary>
/// <remarks>
/// Flutter declares this as a mixin so that <c>ScrollPosition</c> can mix it into
/// <c>ViewportOffset</c>. C# has no mixins and <see cref="ScrollPosition"/> already extends
/// <see cref="ViewportOffset"/>, so the contract is an interface; the members Dart's mixin implements
/// are default implementations over <see cref="ScrollMetricsUtils"/>, which is also what the
/// implementing classes call so that redeclaring a member does not recurse through the interface.
/// </remarks>
public interface IScrollMetrics
{
    /// <summary>
    /// Creates a <see cref="IScrollMetrics"/> that has the same properties as this object.
    /// </summary>
    /// <remarks>
    /// The named arguments allow the values to be adjusted in the process, which is useful to
    /// examine hypothetical situations ("would applying this delta unmodified take the position
    /// <see cref="OutOfRange"/>?").
    /// </remarks>
    IScrollMetrics CopyWith(
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return ScrollMetricsUtils.Copy(
            this,
            minScrollExtent,
            maxScrollExtent,
            pixels,
            viewportDimension,
            axisDirection,
            devicePixelRatio);
    }

    /// <summary>The minimum in-range value for <see cref="Pixels"/>.</summary>
    double MinScrollExtent { get; }

    /// <summary>The maximum in-range value for <see cref="Pixels"/>.</summary>
    double MaxScrollExtent { get; }

    /// <summary>
    /// Whether the <see cref="MinScrollExtent"/> and <see cref="MaxScrollExtent"/> properties are
    /// available.
    /// </summary>
    bool HasContentDimensions { get; }

    /// <summary>The current scroll position, in logical pixels along the <see cref="AxisDirection"/>.</summary>
    double Pixels { get; }

    /// <summary>Whether the <see cref="Pixels"/> property is available.</summary>
    bool HasPixels { get; }

    /// <summary>The extent of the viewport along the <see cref="AxisDirection"/>.</summary>
    double ViewportDimension { get; }

    /// <summary>Whether the <see cref="ViewportDimension"/> property is available.</summary>
    bool HasViewportDimension { get; }

    /// <summary>The direction in which the scroll view scrolls.</summary>
    AxisDirection AxisDirection { get; }

    /// <summary>The axis in which the scroll view scrolls.</summary>
    Axis Axis => ScrollMetricsUtils.AxisOf(this);

    /// <summary>
    /// Whether the <see cref="Pixels"/> value is outside the <see cref="MinScrollExtent"/> and
    /// <see cref="MaxScrollExtent"/>.
    /// </summary>
    bool OutOfRange => ScrollMetricsUtils.OutOfRange(this);

    /// <summary>
    /// Whether the <see cref="Pixels"/> value is exactly at the <see cref="MinScrollExtent"/> or the
    /// <see cref="MaxScrollExtent"/>.
    /// </summary>
    bool AtEdge => ScrollMetricsUtils.AtEdge(this);

    /// <summary>The quantity of content conceptually "above" the viewport in the scrollable.</summary>
    double ExtentBefore => ScrollMetricsUtils.ExtentBefore(this);

    /// <summary>
    /// The quantity of content conceptually "inside" the viewport in the scrollable, including empty
    /// space when the total amount of content is less than the <see cref="ViewportDimension"/>.
    /// </summary>
    double ExtentInside => ScrollMetricsUtils.ExtentInside(this);

    /// <summary>The quantity of content conceptually "below" the viewport in the scrollable.</summary>
    double ExtentAfter => ScrollMetricsUtils.ExtentAfter(this);

    /// <summary>
    /// The total quantity of content available: the sum of <see cref="ExtentBefore"/>,
    /// <see cref="ExtentInside"/> and <see cref="ExtentAfter"/>, modulo any rounding errors.
    /// </summary>
    double ExtentTotal => ScrollMetricsUtils.ExtentTotal(this);

    /// <summary>
    /// The device pixel ratio of the view the <see cref="Scrollable"/> associated with these metrics
    /// is drawn into.
    /// </summary>
    double DevicePixelRatio { get; }
}

/// <summary>
/// The bodies Dart's <c>ScrollMetrics</c> mixin contributes to every class that mixes it in.
/// </summary>
/// <remarks>
/// C# resolves a class member that matches an interface member as that member's implementation, which
/// makes the default implementation unreachable and turns `((IScrollMetrics)this).X` inside such a
/// member into unbounded recursion. Both the default implementations and the classes therefore call
/// these once-written formulas instead of each other.
/// </remarks>
public static class ScrollMetricsUtils
{
    /// <summary>Dart parity: <c>ScrollMetrics.axis</c>.</summary>
    public static Axis AxisOf(IScrollMetrics metrics)
    {
        return ScrollDirectionUtils.AxisDirectionToAxis(metrics.AxisDirection);
    }

    /// <summary>Dart parity: <c>ScrollMetrics.outOfRange</c>.</summary>
    public static bool OutOfRange(IScrollMetrics metrics)
    {
        return metrics.Pixels < metrics.MinScrollExtent || metrics.Pixels > metrics.MaxScrollExtent;
    }

    /// <summary>Dart parity: <c>ScrollMetrics.atEdge</c>.</summary>
    public static bool AtEdge(IScrollMetrics metrics)
    {
        return metrics.Pixels == metrics.MinScrollExtent || metrics.Pixels == metrics.MaxScrollExtent;
    }

    /// <summary>Dart parity: <c>ScrollMetrics.extentBefore</c>.</summary>
    public static double ExtentBefore(IScrollMetrics metrics)
    {
        return Math.Max(metrics.Pixels - metrics.MinScrollExtent, 0.0);
    }

    /// <summary>Dart parity: <c>ScrollMetrics.extentInside</c>.</summary>
    public static double ExtentInside(IScrollMetrics metrics)
    {
        double viewportDimension = metrics.ViewportDimension;
        return viewportDimension
               // "above" overscroll value
               - Math.Clamp(metrics.MinScrollExtent - metrics.Pixels, 0.0, viewportDimension)
               // "below" overscroll value
               - Math.Clamp(metrics.Pixels - metrics.MaxScrollExtent, 0.0, viewportDimension);
    }

    /// <summary>Dart parity: <c>ScrollMetrics.extentAfter</c>.</summary>
    public static double ExtentAfter(IScrollMetrics metrics)
    {
        return Math.Max(metrics.MaxScrollExtent - metrics.Pixels, 0.0);
    }

    /// <summary>Dart parity: <c>ScrollMetrics.extentTotal</c>.</summary>
    public static double ExtentTotal(IScrollMetrics metrics)
    {
        return metrics.MaxScrollExtent - metrics.MinScrollExtent + metrics.ViewportDimension;
    }

    /// <summary>Dart parity: <c>ScrollMetrics.copyWith</c>.</summary>
    public static FixedScrollMetrics Copy(
        IScrollMetrics metrics,
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return new FixedScrollMetrics(
            minScrollExtent: minScrollExtent
                             ?? (metrics.HasContentDimensions ? metrics.MinScrollExtent : null),
            maxScrollExtent: maxScrollExtent
                             ?? (metrics.HasContentDimensions ? metrics.MaxScrollExtent : null),
            pixels: pixels ?? (metrics.HasPixels ? metrics.Pixels : null),
            viewportDimension: viewportDimension
                               ?? (metrics.HasViewportDimension ? metrics.ViewportDimension : null),
            axisDirection: axisDirection ?? metrics.AxisDirection,
            devicePixelRatio: devicePixelRatio ?? metrics.DevicePixelRatio);
    }
}

/// <summary>An immutable snapshot of values associated with a <see cref="Scrollable"/> viewport.</summary>
/// <remarks>For details, see <see cref="IScrollMetrics"/>, which defines this object's interfaces.</remarks>
public class FixedScrollMetrics : IScrollMetrics
{
    private readonly double? _minScrollExtent;
    private readonly double? _maxScrollExtent;
    private readonly double? _pixels;
    private readonly double? _viewportDimension;

    /// <summary>
    /// Creates an immutable snapshot of values associated with a <see cref="Scrollable"/> viewport.
    /// </summary>
    public FixedScrollMetrics(
        double? minScrollExtent,
        double? maxScrollExtent,
        double? pixels,
        double? viewportDimension,
        AxisDirection axisDirection,
        double devicePixelRatio)
    {
        _minScrollExtent = minScrollExtent;
        _maxScrollExtent = maxScrollExtent;
        _pixels = pixels;
        _viewportDimension = viewportDimension;
        AxisDirection = axisDirection;
        DevicePixelRatio = devicePixelRatio;
    }

    public double MinScrollExtent => _minScrollExtent ?? throw Unavailable(nameof(MinScrollExtent));

    public double MaxScrollExtent => _maxScrollExtent ?? throw Unavailable(nameof(MaxScrollExtent));

    public bool HasContentDimensions => _minScrollExtent != null && _maxScrollExtent != null;

    public double Pixels => _pixels ?? throw Unavailable(nameof(Pixels));

    public bool HasPixels => _pixels != null;

    public double ViewportDimension => _viewportDimension ?? throw Unavailable(nameof(ViewportDimension));

    public bool HasViewportDimension => _viewportDimension != null;

    public AxisDirection AxisDirection { get; }

    public double DevicePixelRatio { get; }

    public Axis Axis => ScrollMetricsUtils.AxisOf(this);

    public bool OutOfRange => ScrollMetricsUtils.OutOfRange(this);

    public bool AtEdge => ScrollMetricsUtils.AtEdge(this);

    public double ExtentBefore => ScrollMetricsUtils.ExtentBefore(this);

    public double ExtentInside => ScrollMetricsUtils.ExtentInside(this);

    public double ExtentAfter => ScrollMetricsUtils.ExtentAfter(this);

    public double ExtentTotal => ScrollMetricsUtils.ExtentTotal(this);

    public virtual FixedScrollMetrics CopyWith(
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return ScrollMetricsUtils.Copy(
            this,
            minScrollExtent,
            maxScrollExtent,
            pixels,
            viewportDimension,
            axisDirection,
            devicePixelRatio);
    }

    public override string ToString()
    {
        // Dart's `toStringAsFixed` is culture-invariant; `ToString("F1")` is not.
        return string.Create(
            CultureInfo.InvariantCulture,
            $"FixedScrollMetrics({ExtentBefore:F1}..[{ExtentInside:F1}]..{ExtentAfter:F1})");
    }

    /// <summary>
    /// The exception a missing value raises. Dart reads these through a null assertion, which throws
    /// on a snapshot whose corresponding <c>has*</c> flag is false.
    /// </summary>
    private protected static InvalidOperationException Unavailable(string property)
    {
        return new InvalidOperationException(
            $"{property} is not available on this scroll metrics snapshot.");
    }
}
