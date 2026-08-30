using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/box.dart (approximate)

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

    public override string ToString() => $"offset={offset}";
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

/// <summary>
/// Immutable layout constraints for [RenderBox] layout.
/// </summary>
/// <param name="MinWidth">The minimum width that satisfies the constraints.</param>
/// <param name="MaxWidth">The maximum width that satisfies the constraints. Might be [double.PositiveInfinity].</param>
/// <param name="MinHeight">The minimum height that satisfies the constraints.</param>
/// <param name="MaxHeight">The maximum height that satisfies the constraints. Might be [double.PositiveInfinity].</param>
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
    public override string ToString()
    {
        return $"BoxConstraints({MinWidth:0.###}≤w≤{MaxWidth:0.###}, {MinHeight:0.###}≤h≤{MaxHeight:0.###})";
    }

    /// The biggest size that satisfies the constraints.
    public Size Biggest => new Size(ConstrainWidth(), ConstrainHeight());

    /// The smallest size that satisfies the constraints.
    public Size Smallest => new Size(ConstrainWidth(0.0), ConstrainHeight(0.0));


    /// Whether there is exactly one width value that satisfies the constraints.
    public bool HasTightWidth => MinWidth >= MaxWidth;

    /// Whether there is exactly one height value that satisfies the constraints.
    public bool HasTightHeight => MinHeight >= MaxHeight;

    public bool IsTight => HasTightWidth && HasTightHeight;

    public bool IsNormalized => MinWidth >= 0.0 && MinWidth <= MaxWidth && MinHeight >= 0.0 && MinHeight <= MaxHeight;

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

        BoxConstraints self = this;
        void Throw(string message)
        {
            List<DiagnosticsNode> information =
            [
                new ErrorSummary($"BoxConstraints has {message}."),
                .. informationCollector?.Invoke() ?? [],
                new DiagnosticsProperty<BoxConstraints>(
                    "The offending constraints were", self, style: DiagnosticsTreeStyle.ErrorProperty),
            ];
            throw new FlutterError(information);
        }

        var nanFields = new List<string>();
        if (double.IsNaN(MinWidth))
        {
            nanFields.Add("MinWidth");
        }

        if (double.IsNaN(MaxWidth))
        {
            nanFields.Add("MaxWidth");
        }

        if (double.IsNaN(MinHeight))
        {
            nanFields.Add("MinHeight");
        }

        if (double.IsNaN(MaxHeight))
        {
            nanFields.Add("MaxHeight");
        }

        if (nanFields.Count > 0)
        {
            Throw($"NaN values in {string.Join(", ", nanFields)}");
        }

        if (MinWidth < 0.0 && MinHeight < 0.0)
        {
            Throw("both a negative minimum width and a negative minimum height");
        }

        if (MinWidth < 0.0)
        {
            Throw("a negative minimum width");
        }

        if (MinHeight < 0.0)
        {
            Throw("a negative minimum height");
        }

        if (MaxWidth < MinWidth && MaxHeight < MinHeight)
        {
            Throw("both width and height constraints non-normalized");
        }

        if (MaxWidth < MinWidth)
        {
            Throw("non-normalized width constraints");
        }

        if (MaxHeight < MinHeight)
        {
            Throw("non-normalized height constraints");
        }

        if (isAppliedConstraint)
        {
            if (double.IsInfinity(MinWidth) && double.IsInfinity(MinHeight))
            {
                Throw("infinite minimum constraints");
            }

            if (double.IsInfinity(MinWidth))
            {
                Throw("an infinite minimum width constraint");
            }

            if (double.IsInfinity(MinHeight))
            {
                Throw("an infinite minimum height constraint");
            }
        }

        Debug.Assert(IsNormalized);
        return IsNormalized;
    }

    public bool HasBoundedWidth => double.IsFinite(MaxWidth);

    public bool HasBoundedHeight => double.IsFinite(MaxHeight);

    /// A box constraints with the width and height constraints flipped.
    public BoxConstraints Flipped => new(
        MinWidth: MinHeight,
        MaxWidth: MaxHeight,
        MinHeight: MinWidth,
        MaxHeight: MaxWidth
    );


    public Size Constrain(Size size)
    {
        double w = Math.Clamp(size.Width, MinWidth, MaxWidth);
        double h = Math.Clamp(size.Height, MinHeight, MaxHeight);
        return new Size(w, h);
    }

    /// <summary>
    /// Creates box constraints that is respected only by the given size.
    /// </summary>
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

    public static BoxConstraints Tight(Size s) => new BoxConstraints(s.Width, s.Width, s.Height, s.Height);

    /// Creates box constraints that require the given width or height.
    public static BoxConstraints TightFor(double? width = null, double? height = null)
        => new BoxConstraints(
            width ?? 0.0,
            width ?? double.PositiveInfinity,
            height ?? 0.0,
            height ?? double.PositiveInfinity);

    public static BoxConstraints TightForFinite(
        double width = double.PositiveInfinity,
        double height = double.PositiveInfinity)
    {
        return TightFor(
            width: double.IsFinite(width) ? width : null,
            height: double.IsFinite(height) ? height : null);
    }

    /// <summary>
    /// Creates constraints that expand to fill the maximum size permitted by the parent.
    /// </summary>
    public static BoxConstraints Expand(double? width = null, double? height = null)
        => new BoxConstraints(
            width ?? double.PositiveInfinity,
            width ?? double.PositiveInfinity,
            height ?? double.PositiveInfinity,
            height ?? double.PositiveInfinity);

    public static BoxConstraints Loose(Size s) => new BoxConstraints(0, s.Width, 0, s.Height);

    public BoxConstraints Loosen() => new BoxConstraints(MaxWidth: MaxWidth, MaxHeight: MaxHeight);

    /// <summary>
    /// Dart's <c>BoxConstraints.operator *</c>: scales every constraint by <paramref name="factor"/>.
    /// </summary>
    public static BoxConstraints operator *(BoxConstraints constraints, double factor)
        => new BoxConstraints(
            MinWidth: constraints.MinWidth * factor,
            MaxWidth: constraints.MaxWidth * factor,
            MinHeight: constraints.MinHeight * factor,
            MaxHeight: constraints.MaxHeight * factor);

    /// <summary>
    /// Dart's <c>BoxConstraints.operator /</c>: divides every constraint by <paramref name="factor"/>.
    /// </summary>
    public static BoxConstraints operator /(BoxConstraints constraints, double factor)
        => new BoxConstraints(
            MinWidth: constraints.MinWidth / factor,
            MaxWidth: constraints.MaxWidth / factor,
            MinHeight: constraints.MinHeight / factor,
            MaxHeight: constraints.MaxHeight / factor);

    /// <summary>Dart's <c>BoxConstraints.operator %</c>: the remainder of every constraint.</summary>
    public static BoxConstraints operator %(BoxConstraints constraints, double value)
        => new BoxConstraints(
            MinWidth: constraints.MinWidth % value,
            MaxWidth: constraints.MaxWidth % value,
            MinHeight: constraints.MinHeight % value,
            MaxHeight: constraints.MaxHeight % value);

    /// <summary>Dart's <c>BoxConstraints.operator ~/</c>: truncating division of every constraint.</summary>
    public BoxConstraints TruncatingDivide(double factor)
        => new BoxConstraints(
            MinWidth: Math.Truncate(MinWidth / factor),
            MaxWidth: Math.Truncate(MaxWidth / factor),
            MinHeight: Math.Truncate(MinHeight / factor),
            MaxHeight: Math.Truncate(MaxHeight / factor));

    /// <summary>Dart's `BoxConstraints.widthConstraints`: only the width constraints.</summary>
    public BoxConstraints WidthConstraints() => new BoxConstraints(MinWidth: MinWidth, MaxWidth: MaxWidth);

    /// <summary>Dart's `BoxConstraints.heightConstraints`: only the height constraints.</summary>
    public BoxConstraints HeightConstraints() => new BoxConstraints(MinHeight: MinHeight, MaxHeight: MaxHeight);

    public BoxConstraints Tighten(double? width = null, double? height = null)
    {
        double? tightenedWidth = width.HasValue
            ? Math.Clamp(width.Value, MinWidth, MaxWidth)
            : (double?)null;
        double? tightenedHeight = height.HasValue
            ? Math.Clamp(height.Value, MinHeight, MaxHeight)
            : (double?)null;

        return new BoxConstraints(
            MinWidth: tightenedWidth ?? MinWidth,
            MaxWidth: tightenedWidth ?? MaxWidth,
            MinHeight: tightenedHeight ?? MinHeight,
            MaxHeight: tightenedHeight ?? MaxHeight);
    }

    public BoxConstraints Enforce(BoxConstraints constraints) =>
        new(
            MinWidth: Math.Clamp(MinWidth, constraints.MinWidth, constraints.MaxWidth),
            MaxWidth: Math.Clamp(MaxWidth, constraints.MinWidth, constraints.MaxWidth),
            MinHeight: Math.Clamp(MinHeight, constraints.MinHeight, constraints.MaxHeight),
            MaxHeight: Math.Clamp(MaxHeight, constraints.MinHeight, constraints.MaxHeight)
        );

    public BoxConstraints Deflate(Thickness edges)
    {
        double horizontal = edges.Left + edges.Right;
        double vertical = edges.Top + edges.Bottom;

        double deflatedMinWidth = Math.Max(0, MinWidth - horizontal);
        double deflatedMaxWidth = double.IsPositiveInfinity(MaxWidth)
            ? double.PositiveInfinity
            : Math.Max(deflatedMinWidth, MaxWidth - horizontal);

        double deflatedMinHeight = Math.Max(0, MinHeight - vertical);
        double deflatedMaxHeight = double.IsPositiveInfinity(MaxHeight)
            ? double.PositiveInfinity
            : Math.Max(deflatedMinHeight, MaxHeight - vertical);

        return new BoxConstraints(
            MinWidth: deflatedMinWidth,
            MaxWidth: deflatedMaxWidth,
            MinHeight: deflatedMinHeight,
            MaxHeight: deflatedMaxHeight);
    }

    /// Returns the width that both satisfies the constraints and is as close as
    /// possible to the given width.
    public double ConstrainWidth(double width = double.PositiveInfinity)
    {
        //assert(debugAssertIsValid());
        return Math.Clamp(width, MinWidth, MaxWidth);
    }

    /// Returns the height that both satisfies the constraints and is as close as
    /// possible to the given height.
    public double ConstrainHeight(double height = double.PositiveInfinity)
    {
        //assert(debugAssertIsValid());
        return Math.Clamp(height, MinHeight, MaxHeight);
    }

    public Size ConstrainSizeAndAttemptToPreserveAspectRatio(Size size)
    {
        if (IsTight)
        {
            return Smallest;
        }

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

        return new Size(
            ConstrainWidth(width),
            ConstrainHeight(height));
    }
}
