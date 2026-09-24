using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/box.dart

namespace Plumix.Rendering;

/// <summary>
/// Transforms the constraints a render box passes to its child.
/// </summary>
public delegate BoxConstraints BoxConstraintsTransform(BoxConstraints constraints);

/// Parent data used by [RenderBox] and its subclasses.
///
/// {@tool dartpad}
/// Parent data is used to communicate to a render object about its
/// children. In this example, there are two render objects that perform
/// text layout. They use parent data to identify the kind of child they
/// are laying out, and space the children accordingly.
///
/// ** See code in examples/api/lib/rendering/box/parent_data.0.dart **
/// {@end-tool}
public class BoxParentData : ParentData
{
    /// The offset at which to paint the child in the parent's coordinate system.
    public Point offset = new Point();

    /// <inheritdoc />
    public override string ToString() => $"offset={DartFormat.Offset(offset)}";
}

/// Abstract [ParentData] subclass for [RenderBox] subclasses that want the
/// [ContainerRenderObjectMixin].
///
/// This is a convenience class that mixes in the relevant classes with
/// the relevant type arguments.
public abstract class ContainerBoxParentData<TChild> : BoxParentData, IContainerParentDataMixin<TChild>
    where TChild : RenderObject
{
    private readonly IContainerParentDataMixin<TChild> _mixin1;

    protected ContainerBoxParentData()
    {
        _mixin1 = new ContainerParentDataMixin<TChild>(this);
    }

    public TChild? previousSibling
    {
        get => _mixin1.previousSibling;
        set => _mixin1.previousSibling = value;
    }

    public TChild? nextSibling
    {
        get => _mixin1.nextSibling;
        set => _mixin1.nextSibling = value;
    }
}

/// <summary>
/// Marks a value a parent can attach to the <see cref="BoxConstraints"/> it lays a child out with, so the
/// child can read layout-time information its own constraints cannot express.
/// </summary>
/// <remarks>
/// Dart subclasses <c>BoxConstraints</c> for this (for example <c>_BodyBoxConstraints</c> in
/// <c>scaffold.dart</c>, which carries the scaffold's app-bar and bottom-widget heights to
/// <c>_BodyBuilder</c>). <see cref="BoxConstraints"/> is a value type here and cannot be subclassed, so the
/// metadata rides along as a field instead. Like the Dart subclasses, it takes part in equality — a child
/// laid out with different metadata is relaid out — and every derived constraint
/// (<see cref="BoxConstraints.Loosen"/>, <see cref="BoxConstraints.Tighten"/>, ...) drops it, exactly as
/// Dart's base-class methods return a plain <c>BoxConstraints</c>.
/// </remarks>
public interface IBoxConstraintsMetadata;


/// <summary>Immutable layout constraints for <see cref="RenderBox"/> layout.</summary>
/// <remarks>
/// Flutter's <c>BoxConstraints</c>. A value type here, so the Dart <c>identical</c> short-cuts (in
/// <c>==</c> and <see cref="Lerp"/>) compare the fields instead, and the parameterless
/// <c>new BoxConstraints()</c> is not Dart's <c>const BoxConstraints()</c> — use
/// <see cref="Unbounded"/> for that.
/// </remarks>
/// <param name="MinWidth">The minimum width that satisfies the constraints.</param>
/// <param name="MaxWidth">The maximum width that satisfies the constraints. Might be infinite.</param>
/// <param name="MinHeight">The minimum height that satisfies the constraints.</param>
/// <param name="MaxHeight">The maximum height that satisfies the constraints. Might be infinite.</param>
/// <param name="Metadata">
/// Layout-time information attached by the parent; see <see cref="IBoxConstraintsMetadata"/>.
/// </param>
public readonly record struct BoxConstraints(
    double MinWidth = 0.0,
    double MaxWidth = double.PositiveInfinity,
    double MinHeight = 0.0,
    double MaxHeight = double.PositiveInfinity,
    IBoxConstraintsMetadata? Metadata = null)
    : IConstraints
{
    /// <summary>
    /// The constraints Dart writes as <c>const BoxConstraints()</c>: no minimum, unbounded maximum.
    /// </summary>
    /// <remarks>
    /// C#'s <c>new BoxConstraints()</c> bypasses the primary constructor's defaults and yields a
    /// tight 0x0 constraint instead, so every port of Dart's default constructor must use this.
    /// </remarks>
    public static BoxConstraints Unbounded => new(
        MinWidth: 0.0,
        MaxWidth: double.PositiveInfinity,
        MinHeight: 0.0,
        MaxHeight: double.PositiveInfinity);

    /// <summary>Creates box constraints that is respected only by the given size.</summary>
    public static BoxConstraints Tight(Size size) => new(size.Width, size.Width, size.Height, size.Height);

    /// <summary>Creates box constraints that require the given width or height.</summary>
    public static BoxConstraints TightFor(double? width = null, double? height = null) => new(
        width ?? 0.0,
        width ?? double.PositiveInfinity,
        height ?? 0.0,
        height ?? double.PositiveInfinity);

    /// <summary>
    /// Creates box constraints that require the given width or height, except if they are infinite.
    /// </summary>
    public static BoxConstraints TightForFinite(
        double width = double.PositiveInfinity,
        double height = double.PositiveInfinity) => new(
        width != double.PositiveInfinity ? width : 0.0,
        width != double.PositiveInfinity ? width : double.PositiveInfinity,
        height != double.PositiveInfinity ? height : 0.0,
        height != double.PositiveInfinity ? height : double.PositiveInfinity);

    /// <summary>Creates box constraints that forbid sizes larger than the given size.</summary>
    public static BoxConstraints Loose(Size size) => new(0.0, size.Width, 0.0, size.Height);

    /// <summary>
    /// Creates box constraints that expand to fill another box constraints.
    /// </summary>
    /// <remarks>
    /// If width or height is given, the constraints will require exactly the given value in the
    /// given dimension.
    /// </remarks>
    public static BoxConstraints Expand(double? width = null, double? height = null) => new(
        width ?? double.PositiveInfinity,
        width ?? double.PositiveInfinity,
        height ?? double.PositiveInfinity,
        height ?? double.PositiveInfinity);

    /// <summary>Creates box constraints that match the given view constraints.</summary>
    public static BoxConstraints FromViewConstraints(ViewConstraints constraints) => new(
        MinWidth: constraints.MinWidth,
        MaxWidth: constraints.MaxWidth,
        MinHeight: constraints.MinHeight,
        MaxHeight: constraints.MaxHeight);

    /// <summary>Creates a copy of this box constraints but with the given fields replaced.</summary>
    public BoxConstraints CopyWith(
        double? minWidth = null,
        double? maxWidth = null,
        double? minHeight = null,
        double? maxHeight = null) => new(
        MinWidth: minWidth ?? MinWidth,
        MaxWidth: maxWidth ?? MaxWidth,
        MinHeight: minHeight ?? MinHeight,
        MaxHeight: maxHeight ?? MaxHeight);

    /// <summary>Returns new box constraints that are smaller by the given edge dimensions.</summary>
    public BoxConstraints Deflate(EdgeInsetsGeometry edges)
    {
        DebugAssertValid();
        double horizontal = edges.Horizontal;
        double vertical = edges.Vertical;
        double deflatedMinWidth = Math.Max(0.0, MinWidth - horizontal);
        double deflatedMinHeight = Math.Max(0.0, MinHeight - vertical);
        return new BoxConstraints(
            MinWidth: deflatedMinWidth,
            MaxWidth: Math.Max(deflatedMinWidth, MaxWidth - horizontal),
            MinHeight: deflatedMinHeight,
            MaxHeight: Math.Max(deflatedMinHeight, MaxHeight - vertical));
    }

    /// <summary>Returns new box constraints that remove the minimum width and height requirements.</summary>
    public BoxConstraints Loosen()
    {
        DebugAssertValid();
        return new BoxConstraints(MaxWidth: MaxWidth, MaxHeight: MaxHeight);
    }

    /// <summary>
    /// Returns new box constraints that respect the given constraints while being as close as
    /// possible to the original constraints.
    /// </summary>
    public BoxConstraints Enforce(BoxConstraints constraints) => new(
        MinWidth: ClampDouble(MinWidth, constraints.MinWidth, constraints.MaxWidth),
        MaxWidth: ClampDouble(MaxWidth, constraints.MinWidth, constraints.MaxWidth),
        MinHeight: ClampDouble(MinHeight, constraints.MinHeight, constraints.MaxHeight),
        MaxHeight: ClampDouble(MaxHeight, constraints.MinHeight, constraints.MaxHeight));

    /// <summary>
    /// Returns new box constraints with a tight width and/or height as close to the given width and
    /// height as possible while still respecting the original box constraints.
    /// </summary>
    public BoxConstraints Tighten(double? width = null, double? height = null) => new(
        MinWidth: width is { } w ? ClampDouble(w, MinWidth, MaxWidth) : MinWidth,
        MaxWidth: width is { } w2 ? ClampDouble(w2, MinWidth, MaxWidth) : MaxWidth,
        MinHeight: height is { } h ? ClampDouble(h, MinHeight, MaxHeight) : MinHeight,
        MaxHeight: height is { } h2 ? ClampDouble(h2, MinHeight, MaxHeight) : MaxHeight);

    /// <summary>A box constraints with the width and height constraints flipped.</summary>
    public BoxConstraints Flipped => new(
        MinWidth: MinHeight,
        MaxWidth: MaxHeight,
        MinHeight: MinWidth,
        MaxHeight: MaxWidth);

    /// <summary>Returns box constraints with the same width constraints but with unconstrained height.</summary>
    public BoxConstraints WidthConstraints() => new(MinWidth: MinWidth, MaxWidth: MaxWidth);

    /// <summary>Returns box constraints with the same height constraints but with unconstrained width.</summary>
    public BoxConstraints HeightConstraints() => new(MinHeight: MinHeight, MaxHeight: MaxHeight);

    /// <summary>
    /// Returns the width that both satisfies the constraints and is as close as possible to the
    /// given width.
    /// </summary>
    public double ConstrainWidth(double width = double.PositiveInfinity)
    {
        DebugAssertValid();
        return ClampDouble(width, MinWidth, MaxWidth);
    }

    /// <summary>
    /// Returns the height that both satisfies the constraints and is as close as possible to the
    /// given height.
    /// </summary>
    public double ConstrainHeight(double height = double.PositiveInfinity)
    {
        DebugAssertValid();
        return ClampDouble(height, MinHeight, MaxHeight);
    }

    /// <summary>
    /// Returns the size that both satisfies the constraints and is as close as possible to the given
    /// size.
    /// </summary>
    /// <remarks>
    /// Dart's <c>_debugPropagateDebugSize</c> has no counterpart: Avalonia's <see cref="Size"/> is a
    /// sealed value type, so a size carries no owner (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public Size Constrain(Size size) => new(ConstrainWidth(size.Width), ConstrainHeight(size.Height));

    /// <summary>
    /// Returns the size that both satisfies the constraints and is as close as possible to the given
    /// width and height.
    /// </summary>
    public Size ConstrainDimensions(double width, double height) =>
        new(ConstrainWidth(width), ConstrainHeight(height));

    /// <summary>
    /// Returns a size that attempts to meet the conditions below, in order: the size must satisfy
    /// these constraints, the aspect ratio of the returned size matches the aspect ratio of the given
    /// size, and the returned size is as big as possible while still being equal to or smaller than
    /// the given size.
    /// </summary>
    public Size ConstrainSizeAndAttemptToPreserveAspectRatio(Size size)
    {
        if (IsTight)
        {
            return Smallest;
        }

        // Dart's `Size.isEmpty`.
        if (size.Width <= 0.0 || size.Height <= 0.0)
        {
            return Constrain(size);
        }

        double width = size.Width;
        double height = size.Height;
        double aspectRatio = width / height;

        if (width > MaxWidth)
        {
            width = MaxWidth;
            height = width / aspectRatio;
        }

        if (height > MaxHeight)
        {
            height = MaxHeight;
            width = height * aspectRatio;
        }

        if (width < MinWidth)
        {
            width = MinWidth;
            height = width / aspectRatio;
        }

        if (height < MinHeight)
        {
            height = MinHeight;
            width = height * aspectRatio;
        }

        return new Size(ConstrainWidth(width), ConstrainHeight(height));
    }

    /// <summary>The biggest size that satisfies the constraints.</summary>
    public Size Biggest => new(ConstrainWidth(), ConstrainHeight());

    /// <summary>The smallest size that satisfies the constraints.</summary>
    public Size Smallest => new(ConstrainWidth(0.0), ConstrainHeight(0.0));

    /// <summary>Whether there is exactly one width value that satisfies the constraints.</summary>
    public bool HasTightWidth => MinWidth >= MaxWidth;

    /// <summary>Whether there is exactly one height value that satisfies the constraints.</summary>
    public bool HasTightHeight => MinHeight >= MaxHeight;

    /// <inheritdoc />
    public bool IsTight => HasTightWidth && HasTightHeight;

    /// <summary>Whether there is an upper bound on the maximum width.</summary>
    public bool HasBoundedWidth => MaxWidth < double.PositiveInfinity;

    /// <summary>Whether there is an upper bound on the maximum height.</summary>
    public bool HasBoundedHeight => MaxHeight < double.PositiveInfinity;

    /// <summary>Whether the width constraint is infinite.</summary>
    public bool HasInfiniteWidth => MinWidth >= double.PositiveInfinity;

    /// <summary>Whether the height constraint is infinite.</summary>
    public bool HasInfiniteHeight => MinHeight >= double.PositiveInfinity;

    /// <summary>Whether the given size satisfies the constraints.</summary>
    public bool IsSatisfiedBy(Size size)
    {
        DebugAssertValid();
        return (MinWidth <= size.Width)
               && (size.Width <= MaxWidth)
               && (MinHeight <= size.Height)
               && (size.Height <= MaxHeight);
    }

    /// <summary>Scales each constraint parameter by the given factor.</summary>
    public static BoxConstraints operator *(BoxConstraints constraints, double factor) => new(
        MinWidth: constraints.MinWidth * factor,
        MaxWidth: constraints.MaxWidth * factor,
        MinHeight: constraints.MinHeight * factor,
        MaxHeight: constraints.MaxHeight * factor);

    /// <summary>Scales each constraint parameter by the inverse of the given factor.</summary>
    public static BoxConstraints operator /(BoxConstraints constraints, double factor) => new(
        MinWidth: constraints.MinWidth / factor,
        MaxWidth: constraints.MaxWidth / factor,
        MinHeight: constraints.MinHeight / factor,
        MaxHeight: constraints.MaxHeight / factor);

    /// <summary>
    /// Scales each constraint parameter by the inverse of the given factor, rounded to the nearest
    /// integer.
    /// </summary>
    /// <remarks>
    /// Dart's <c>operator ~/</c>, which C# cannot declare. Like Dart's <c>double ~/ double</c>, a
    /// non-finite quotient (any unbounded constraint) throws.
    /// </remarks>
    public BoxConstraints TruncatingDivide(double factor) => new(
        MinWidth: DartTruncatingDivide(MinWidth, factor),
        MaxWidth: DartTruncatingDivide(MaxWidth, factor),
        MinHeight: DartTruncatingDivide(MinHeight, factor),
        MaxHeight: DartTruncatingDivide(MaxHeight, factor));

    /// <summary>Computes the remainder of each constraint parameter by the given value.</summary>
    /// <remarks>Dart's <c>%</c> is Euclidean: the result is never negative.</remarks>
    public static BoxConstraints operator %(BoxConstraints constraints, double value) => new(
        MinWidth: DartModulo(constraints.MinWidth, value),
        MaxWidth: DartModulo(constraints.MaxWidth, value),
        MinHeight: DartModulo(constraints.MinHeight, value),
        MaxHeight: DartModulo(constraints.MaxHeight, value));

    /// <summary>Linearly interpolate between two BoxConstraints.</summary>
    /// <remarks>
    /// If either is null, this function interpolates from a <see cref="BoxConstraints"/> object whose
    /// fields are all set to 0.0. Interpolating between finite and unbounded constraints asserts.
    /// </remarks>
    public static BoxConstraints? Lerp(BoxConstraints? a, BoxConstraints? b, double t)
    {
        // Dart's `identical(a, b)`: a value type has no identity, so equal fields stand in for it.
        if (a is null && b is null)
        {
            return a;
        }

        if (a is { } same && b is { } other && same.FieldsEqual(other))
        {
            return a;
        }

        if (a is not { } from)
        {
            return b!.Value * t;
        }

        if (b is not { } to)
        {
            return from * (1.0 - t);
        }

        from.DebugAssertValid();
        to.DebugAssertValid();
        DebugAssertLerpable(from.MinWidth, to.MinWidth);
        DebugAssertLerpable(from.MaxWidth, to.MaxWidth);
        DebugAssertLerpable(from.MinHeight, to.MinHeight);
        DebugAssertLerpable(from.MaxHeight, to.MaxHeight);
        return new BoxConstraints(
            MinWidth: double.IsFinite(from.MinWidth)
                ? LerpDouble(from.MinWidth, to.MinWidth, t)
                : double.PositiveInfinity,
            MaxWidth: double.IsFinite(from.MaxWidth)
                ? LerpDouble(from.MaxWidth, to.MaxWidth, t)
                : double.PositiveInfinity,
            MinHeight: double.IsFinite(from.MinHeight)
                ? LerpDouble(from.MinHeight, to.MinHeight, t)
                : double.PositiveInfinity,
            MaxHeight: double.IsFinite(from.MaxHeight)
                ? LerpDouble(from.MaxHeight, to.MaxHeight, t)
                : double.PositiveInfinity);
    }

    /// <summary>
    /// Returns whether the object's constraints are normalized: the minimums are non-negative and not
    /// larger than the corresponding maximums.
    /// </summary>
    public bool IsNormalized =>
        MinWidth >= 0.0 && MinWidth <= MaxWidth && MinHeight >= 0.0 && MinHeight <= MaxHeight;

    /// <inheritdoc />
    /// <remarks>Flutter's <c>BoxConstraints.debugAssertIsValid</c>.</remarks>
    public bool DebugAssertIsValid(
        bool isAppliedConstraint = false,
        InformationCollector? informationCollector = null)
    {
        if (!Constants.KDebugMode)
        {
            return IsNormalized;
        }

        // Fast path: a normalized constraint can only fail the applied-constraint checks below.
        if (IsNormalized
            && (!isAppliedConstraint || (!double.IsInfinity(MinWidth) && !double.IsInfinity(MinHeight))))
        {
            return true;
        }

        BoxConstraints self = this;
        void ThrowError(DiagnosticsNode message)
        {
            throw new FlutterError(
            [
                message,
                .. informationCollector?.Invoke() ?? [],
                new DiagnosticsProperty<BoxConstraints>(
                    "The offending constraints were", self, style: DiagnosticsTreeStyle.ErrorProperty),
            ]);
        }

        if (double.IsNaN(MinWidth)
            || double.IsNaN(MaxWidth)
            || double.IsNaN(MinHeight)
            || double.IsNaN(MaxHeight))
        {
            var affectedFieldsList = new List<string>();
            if (double.IsNaN(MinWidth))
            {
                affectedFieldsList.Add("minWidth");
            }

            if (double.IsNaN(MaxWidth))
            {
                affectedFieldsList.Add("maxWidth");
            }

            if (double.IsNaN(MinHeight))
            {
                affectedFieldsList.Add("minHeight");
            }

            if (double.IsNaN(MaxHeight))
            {
                affectedFieldsList.Add("maxHeight");
            }

            if (affectedFieldsList.Count > 1)
            {
                affectedFieldsList[^1] = $"and {affectedFieldsList[^1]}";
            }

            string whichFields = affectedFieldsList.Count switch
            {
                1 => affectedFieldsList[0],
                2 => string.Join(" ", affectedFieldsList),
                _ => string.Join(", ", affectedFieldsList),
            };
            ThrowError(new ErrorSummary(
                $"BoxConstraints has {(affectedFieldsList.Count == 1 ? "a NaN value" : "NaN values")} "
                + $"in {whichFields}."));
        }

        if (MinWidth < 0.0 && MinHeight < 0.0)
        {
            ThrowError(new ErrorSummary(
                "BoxConstraints has both a negative minimum width and a negative minimum height."));
        }

        if (MinWidth < 0.0)
        {
            ThrowError(new ErrorSummary("BoxConstraints has a negative minimum width."));
        }

        if (MinHeight < 0.0)
        {
            ThrowError(new ErrorSummary("BoxConstraints has a negative minimum height."));
        }

        if (MaxWidth < MinWidth && MaxHeight < MinHeight)
        {
            ThrowError(new ErrorSummary("BoxConstraints has both width and height constraints non-normalized."));
        }

        if (MaxWidth < MinWidth)
        {
            ThrowError(new ErrorSummary("BoxConstraints has non-normalized width constraints."));
        }

        if (MaxHeight < MinHeight)
        {
            ThrowError(new ErrorSummary("BoxConstraints has non-normalized height constraints."));
        }

        if (isAppliedConstraint)
        {
            if (double.IsInfinity(MinWidth) && double.IsInfinity(MinHeight))
            {
                ThrowError(new ErrorSummary("BoxConstraints forces an infinite width and infinite height."));
            }

            if (double.IsInfinity(MinWidth))
            {
                ThrowError(new ErrorSummary("BoxConstraints forces an infinite width."));
            }

            if (double.IsInfinity(MinHeight))
            {
                ThrowError(new ErrorSummary("BoxConstraints forces an infinite height."));
            }
        }

        if (!IsNormalized)
        {
            throw new AssertionError();
        }

        return IsNormalized;
    }

    /// <summary>
    /// Returns a box constraints that <see cref="IsNormalized"/>.
    /// </summary>
    /// <remarks>
    /// The returned maxWidth is at least as large as the minWidth. Similarly, the returned maxHeight
    /// is at least as large as the minHeight.
    /// </remarks>
    public BoxConstraints Normalize()
    {
        if (IsNormalized)
        {
            return this;
        }

        double minWidth = MinWidth >= 0.0 ? MinWidth : 0.0;
        double minHeight = MinHeight >= 0.0 ? MinHeight : 0.0;
        return new BoxConstraints(
            MinWidth: minWidth,
            MaxWidth: minWidth > MaxWidth ? minWidth : MaxWidth,
            MinHeight: minHeight,
            MaxHeight: minHeight > MaxHeight ? minHeight : MaxHeight);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Dart's <c>operator ==</c>, which asserts that both operands are valid. The
    /// <see cref="Metadata"/> takes part, the way a Dart subclass's runtime type does.
    /// </remarks>
    public bool Equals(BoxConstraints other)
    {
        DebugAssertValid();
        if (Constants.KDebugMode)
        {
            other.DebugAssertIsValid();
        }

        return FieldsEqual(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        DebugAssertValid();
        return HashCode.Combine(MinWidth, MaxWidth, MinHeight, MaxHeight, Metadata);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string annotation = IsNormalized ? "" : "; NOT NORMALIZED";
        if (MinWidth == double.PositiveInfinity && MinHeight == double.PositiveInfinity)
        {
            return $"BoxConstraints(biggest{annotation})";
        }

        if (MinWidth == 0
            && MaxWidth == double.PositiveInfinity
            && MinHeight == 0
            && MaxHeight == double.PositiveInfinity)
        {
            return $"BoxConstraints(unconstrained{annotation})";
        }

        static string Describe(double min, double max, string dim)
        {
            if (min == max)
            {
                return $"{dim}={Fixed(min)}";
            }

            return $"{Fixed(min)}<={dim}<={Fixed(max)}";
        }

        string width = Describe(MinWidth, MaxWidth, "w");
        string height = Describe(MinHeight, MaxHeight, "h");
        return $"BoxConstraints({width}, {height}{annotation})";
    }

    private bool FieldsEqual(BoxConstraints other) =>
        MinWidth.Equals(other.MinWidth)
        && MaxWidth.Equals(other.MaxWidth)
        && MinHeight.Equals(other.MinHeight)
        && MaxHeight.Equals(other.MaxHeight)
        && Equals(Metadata, other.Metadata);

    /// <summary>Dart's <c>assert(debugAssertIsValid())</c>.</summary>
    private void DebugAssertValid()
    {
        if (Constants.KDebugMode)
        {
            DebugAssertIsValid();
        }
    }

    /// <summary>Dart's <c>toStringAsFixed(1)</c>, which spells infinity <c>Infinity</c>.</summary>
    private static string Fixed(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>
    /// dart:ui's <c>clampDouble</c>: unlike <see cref="Math.Clamp(double, double, double)"/>, a NaN
    /// value clamps to <paramref name="max"/>, and a reversed range only asserts.
    /// </summary>
    internal static double ClampDouble(double x, double min, double max)
    {
        if (Constants.KDebugMode && !(min <= max && !double.IsNaN(max) && !double.IsNaN(min)))
        {
            throw new AssertionError($"clampDouble requires min <= max, got min: {min}, max: {max}");
        }

        if (x < min)
        {
            return min;
        }

        if (x > max)
        {
            return max;
        }

        if (double.IsNaN(x))
        {
            return max;
        }

        return x;
    }

    private static double DartTruncatingDivide(double value, double factor)
    {
        double quotient = value / factor;
        if (!double.IsFinite(quotient))
        {
            throw new NotSupportedException("Infinity or NaN toInt");
        }

        return Math.Truncate(quotient);
    }

    private static double DartModulo(double value, double divisor)
    {
        // Dart's `double.remainder` is C#'s truncating `%`.
        double result = value % divisor;
        if (result == 0)
        {
            return 0.0;
        }

        if (result < 0)
        {
            return divisor < 0 ? result - divisor : result + divisor;
        }

        return result;
    }

    private static void DebugAssertLerpable(double a, double b)
    {
        if (Constants.KDebugMode
            && !((double.IsFinite(a) && double.IsFinite(b))
                 || (a == double.PositiveInfinity && b == double.PositiveInfinity)))
        {
            throw new AssertionError("Cannot interpolate between finite constraints and unbounded constraints.");
        }
    }

    /// <summary>dart:ui's <c>lerpDouble</c> for two finite values.</summary>
    private static double LerpDouble(double a, double b, double t)
    {
        if (a == b)
        {
            return a;
        }

        return (a * (1.0 - t)) + (b * t);
    }
}

/// <summary>
/// A wrapper that represents the baseline location of a <see cref="RenderBox"/>.
/// </summary>
/// <remarks>
/// Flutter's <c>BaselineOffset</c> extension type over <c>double?</c>: equality is the equality of the
/// wrapped offset.
/// </remarks>
/// <param name="Offset">
/// The baseline location as the distance from the top of the <see cref="RenderBox"/>, or null when
/// the render box has no baseline.
/// </param>
public readonly record struct BaselineOffset(double? Offset)
{
    /// <summary>A value that indicates that the associated render box does not have any baselines.</summary>
    public static readonly BaselineOffset NoBaseline = new(null);

    /// <summary>
    /// Returns a new baseline location that is <paramref name="offset"/> pixels further away from the
    /// origin than <paramref name="baseline"/>, or unchanged if it is <see cref="NoBaseline"/>.
    /// </summary>
    public static BaselineOffset operator +(BaselineOffset baseline, double offset) =>
        new(baseline.Offset is { } value ? value + offset : null);

    /// <summary>
    /// Compares this <see cref="BaselineOffset"/> and <paramref name="other"/>, and returns whichever
    /// is closer to the origin.
    /// </summary>
    /// <remarks>
    /// When both are <see cref="NoBaseline"/>, this returns <see cref="NoBaseline"/>. When one of them
    /// is <see cref="NoBaseline"/>, this returns the other operand.
    /// </remarks>
    public BaselineOffset MinOf(BaselineOffset other) => (Offset, other.Offset) switch
    {
        ({ } lhs, { } rhs) => lhs >= rhs ? other : this,
        ({ } lhs, null) => new BaselineOffset(lhs),
        (null, _) => other,
    };
}
