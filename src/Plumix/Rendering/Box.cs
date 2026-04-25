using Avalonia;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/box.dart (approximate)

namespace Plumix.Rendering;

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
/// Immutable layout constraints for [RenderBox] layout.
/// </summary>
/// <param name="MinWidth">The minimum width that satisfies the constraints.</param>
/// <param name="MaxWidth">The maximum width that satisfies the constraints. Might be [double.PositiveInfinity].</param>
/// <param name="MinHeight">The minimum height that satisfies the constraints.</param>
/// <param name="MaxHeight">The maximum height that satisfies the constraints. Might be [double.PositiveInfinity].</param>
public readonly record struct BoxConstraints(
    double MinWidth = 0.0,
    double MaxWidth = double.PositiveInfinity,
    double MinHeight = 0.0,
    double MaxHeight = double.PositiveInfinity)
    : IConstraints
{
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
    public static BoxConstraints Tight(Size s) => new BoxConstraints(s.Width, s.Width, s.Height, s.Height);

    /// Creates box constraints that require the given width or height.
    public static BoxConstraints TightFor(double? width = null, double? height = null)
        => new BoxConstraints(
            width ?? 0.0,
            width ?? double.PositiveInfinity,
            height ?? 0.0,
            height ?? double.PositiveInfinity);

    public static BoxConstraints Loose(Size s) => new BoxConstraints(0, s.Width, 0, s.Height);

    public BoxConstraints Loosen() => new BoxConstraints(MaxWidth: MaxWidth, MaxHeight: MaxHeight);

    public BoxConstraints Tighten(double? width = null, double? height = null)
    {
        var tightenedWidth = width.HasValue
            ? Math.Clamp(width.Value, MinWidth, MaxWidth)
            : (double?)null;
        var tightenedHeight = height.HasValue
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
        var horizontal = edges.Left + edges.Right;
        var vertical = edges.Top + edges.Bottom;

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

        var width = size.Width;
        var height = size.Height;
        var aspectRatio = width / height;

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
