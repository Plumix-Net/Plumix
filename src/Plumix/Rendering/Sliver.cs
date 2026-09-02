using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/sliver.dart (approximate)
// flutter/packages/flutter/lib/src/rendering/sliver_multi_box_adaptor.dart

namespace Plumix.Rendering;

public readonly record struct SliverConstraints(
    Axis Axis,
    double ScrollOffset,
    double RemainingPaintExtent,
    double CrossAxisExtent,
    double ViewportMainAxisExtent,
    double CacheOrigin = 0,
    double RemainingCacheExtent = 0,
    AxisDirection AxisDirection = AxisDirection.Down,
    GrowthDirection GrowthDirection = GrowthDirection.Forward,
    double Overlap = 0,
    double PrecedingScrollExtent = 0,
    ScrollDirection UserScrollDirection = ScrollDirection.Idle,
    AxisDirection CrossAxisDirection = AxisDirection.Right) : IConstraints
{
    public bool IsTight => false;

    /// <summary>
    /// The growth direction with respect to the axis direction rather than the scroll offset.
    /// </summary>
    /// <remarks>Flutter's <c>SliverConstraints.normalizedGrowthDirection</c>.</remarks>
    public GrowthDirection NormalizedGrowthDirection => AxisDirection switch
    {
        AxisDirection.Down or AxisDirection.Right => GrowthDirection,
        _ => GrowthDirection == GrowthDirection.Forward
            ? GrowthDirection.Reverse
            : GrowthDirection.Forward,
    };

    public bool IsNormalized => ScrollOffset >= 0.0
                                && CrossAxisExtent >= 0.0
                                && ViewportMainAxisExtent >= 0.0
                                && RemainingPaintExtent >= 0.0;

    public BoxConstraints AsBoxConstraints(
        double minExtent = 0.0,
        double maxExtent = double.PositiveInfinity,
        double? crossAxisExtent = null)
    {
        double effectiveCrossAxisExtent = crossAxisExtent ?? CrossAxisExtent;
        return Axis == Axis.Vertical
            ? new BoxConstraints(
                MinWidth: effectiveCrossAxisExtent,
                MaxWidth: effectiveCrossAxisExtent,
                MinHeight: minExtent,
                MaxHeight: maxExtent)
            : new BoxConstraints(
                MinWidth: minExtent,
                MaxWidth: maxExtent,
                MinHeight: effectiveCrossAxisExtent,
                MaxHeight: effectiveCrossAxisExtent);
    }
}

public readonly record struct SliverGeometry : IDiagnosticable
{
    public SliverGeometry(
        double ScrollExtent = 0.0,
        double PaintExtent = 0.0,
        double PaintOrigin = 0.0,
        double? LayoutExtent = null,
        double MaxPaintExtent = 0.0,
        double MaxScrollObstructionExtent = 0.0,
        double? CrossAxisExtent = null,
        double? HitTestExtent = null,
        bool? Visible = null,
        bool HasVisualOverflow = false,
        double? ScrollOffsetCorrection = null,
        double? CacheExtent = null)
    {
        if (Constants.KDebugMode && ScrollOffsetCorrection == 0.0)
        {
            throw new AssertionError();
        }

        this.ScrollExtent = ScrollExtent;
        this.PaintExtent = PaintExtent;
        this.PaintOrigin = PaintOrigin;
        this.LayoutExtent = LayoutExtent ?? PaintExtent;
        this.MaxPaintExtent = MaxPaintExtent;
        this.MaxScrollObstructionExtent = MaxScrollObstructionExtent;
        this.CrossAxisExtent = CrossAxisExtent;
        this.HitTestExtent = HitTestExtent ?? PaintExtent;
        this.Visible = Visible ?? PaintExtent > 0.0;
        this.HasVisualOverflow = HasVisualOverflow;
        this.ScrollOffsetCorrection = ScrollOffsetCorrection;
        this.CacheExtent = CacheExtent ?? this.LayoutExtent;
    }

    public static SliverGeometry Zero { get; } = new();

    public double ScrollExtent { get; init; }

    public double PaintExtent { get; init; }

    public double PaintOrigin { get; init; }

    public double LayoutExtent { get; init; }

    public double MaxPaintExtent { get; init; }

    public double MaxScrollObstructionExtent { get; init; }

    public double? CrossAxisExtent { get; init; }

    public double HitTestExtent { get; init; }

    public bool Visible { get; init; }

    public bool HasVisualOverflow { get; init; }

    public double? ScrollOffsetCorrection { get; init; }

    public double CacheExtent { get; init; }

    public SliverGeometry CopyWith(
        double? scrollExtent = null,
        double? paintExtent = null,
        double? paintOrigin = null,
        double? layoutExtent = null,
        double? maxPaintExtent = null,
        double? maxScrollObstructionExtent = null,
        double? crossAxisExtent = null,
        double? hitTestExtent = null,
        bool? visible = null,
        bool? hasVisualOverflow = null,
        double? cacheExtent = null)
    {
        return new SliverGeometry(
            ScrollExtent: scrollExtent ?? ScrollExtent,
            PaintExtent: paintExtent ?? PaintExtent,
            PaintOrigin: paintOrigin ?? PaintOrigin,
            LayoutExtent: layoutExtent ?? LayoutExtent,
            MaxPaintExtent: maxPaintExtent ?? MaxPaintExtent,
            MaxScrollObstructionExtent: maxScrollObstructionExtent ?? MaxScrollObstructionExtent,
            CrossAxisExtent: crossAxisExtent ?? CrossAxisExtent,
            HitTestExtent: hitTestExtent ?? HitTestExtent,
            Visible: visible ?? Visible,
            HasVisualOverflow: hasVisualOverflow ?? HasVisualOverflow,
            CacheExtent: cacheExtent ?? CacheExtent);
    }

    public bool DebugAssertIsValid(InformationCollector? informationCollector = null)
    {
        if (!Constants.KDebugMode)
        {
            return true;
        }

        if (ScrollExtent < 0.0)
        {
            ThrowInvalid(new ErrorSummary("The \"scrollExtent\" is negative."), informationCollector);
        }
        if (PaintExtent < 0.0)
        {
            ThrowInvalid(new ErrorSummary("The \"paintExtent\" is negative."), informationCollector);
        }
        if (LayoutExtent < 0.0)
        {
            ThrowInvalid(new ErrorSummary("The \"layoutExtent\" is negative."), informationCollector);
        }
        if (CacheExtent < 0.0)
        {
            ThrowInvalid(new ErrorSummary("The \"cacheExtent\" is negative."), informationCollector);
        }
        if (LayoutExtent > PaintExtent)
        {
            ThrowInvalid(
                new ErrorSummary("The \"layoutExtent\" exceeds the \"paintExtent\"."),
                informationCollector,
                CompareFloats("paintExtent", PaintExtent, "layoutExtent", LayoutExtent));
        }
        if (PaintExtent - MaxPaintExtent > Constants.PrecisionErrorTolerance)
        {
            ThrowInvalid(
                new ErrorSummary("The \"maxPaintExtent\" is less than the \"paintExtent\"."),
                informationCollector,
                CompareFloats("maxPaintExtent", MaxPaintExtent, "paintExtent", PaintExtent),
                new ErrorDescription(
                    "By definition, a sliver can't paint more than the maximum that it can paint!"));
        }
        if (HitTestExtent < 0.0)
        {
            ThrowInvalid(new ErrorSummary("The \"hitTestExtent\" is negative."), informationCollector);
        }
        if (ScrollOffsetCorrection == 0.0)
        {
            ThrowInvalid(new ErrorSummary("The \"scrollOffsetCorrection\" is zero."), informationCollector);
        }

        return true;
    }

    /// <inheritdoc />
    public string ToStringShort() => Diagnostics.ObjectRuntimeType(this, "SliverGeometry");

    /// <inheritdoc />
    public override string ToString() => ToString(DiagnosticLevel.Info);

    public string ToString(DiagnosticLevel minLevel) =>
        ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine).ToString(null, minLevel);

    /// <inheritdoc />
    public DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new DiagnosticableNode<IDiagnosticable>(name, this, style);

    /// <inheritdoc />
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DoubleProperty("scrollExtent", ScrollExtent));
        if (PaintExtent > 0.0)
        {
            properties.Add(new DoubleProperty("paintExtent", PaintExtent, unit: Visible ? null : " but not painting"));
        }
        else if (PaintExtent == 0.0)
        {
            if (Visible)
            {
                properties.Add(new DoubleProperty("paintExtent", PaintExtent));
            }

            properties.Add(new FlagProperty("visible", Visible, ifFalse: "hidden"));
        }
        else
        {
            // Negative paintExtent!
            properties.Add(new DoubleProperty("paintExtent", PaintExtent, tooltip: "!"));
        }

        properties.Add(new DoubleProperty("paintOrigin", PaintOrigin, defaultValue: 0.0));
        properties.Add(new DoubleProperty("layoutExtent", LayoutExtent, defaultValue: PaintExtent));
        properties.Add(new DoubleProperty("maxPaintExtent", MaxPaintExtent));
        properties.Add(new DoubleProperty("hitTestExtent", HitTestExtent, defaultValue: PaintExtent));
        properties.Add(new DiagnosticsProperty<bool>(
            "hasVisualOverflow",
            HasVisualOverflow,
            defaultValue: false));
        properties.Add(new DoubleProperty(
            "scrollOffsetCorrection",
            ScrollOffsetCorrection,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("cacheExtent", CacheExtent, defaultValue: 0.0));
    }

    private static ErrorDescription CompareFloats(
        string labelA,
        double valueA,
        string labelB,
        double valueB)
    {
        string roundedA = valueA.ToString("F1", CultureInfo.InvariantCulture);
        string roundedB = valueB.ToString("F1", CultureInfo.InvariantCulture);
        if (!string.Equals(roundedA, roundedB, StringComparison.Ordinal))
        {
            return new ErrorDescription(
                $"The {labelA} is {roundedA}, but the {labelB} is {roundedB}.");
        }

        return new ErrorDescription(
            $"The {labelA} is {valueA.ToString(CultureInfo.InvariantCulture)}, but the {labelB} is "
            + $"{valueB.ToString(CultureInfo.InvariantCulture)}. The values may have been affected by "
            + "floating point rounding errors.");
    }

    private static void ThrowInvalid(
        ErrorSummary summary,
        InformationCollector? informationCollector,
        params DiagnosticsNode[] details)
    {
        List<DiagnosticsNode> diagnostics =
        [
            new ErrorSummary($"SliverGeometry is not valid: {summary.MessageParts.Single()}"),
            .. details,
        ];
        if (informationCollector != null)
        {
            diagnostics.AddRange(informationCollector());
        }

        throw new FlutterError(diagnostics);
    }

}

/// <summary>
/// Maps a variable-extent sliver's child indexes to the current viewport geometry.
/// Implementations may derive item extents from the active scroll offset.
/// </summary>
public readonly record struct SliverGridGeometry(
    double ScrollOffset,
    double CrossAxisOffset,
    double MainAxisExtent,
    double CrossAxisExtent)
{
    public double TrailingScrollOffset => ScrollOffset + MainAxisExtent;

    public BoxConstraints GetBoxConstraints(SliverConstraints constraints)
    {
        if (constraints.Axis == Axis.Vertical)
        {
            return new BoxConstraints(
                MinWidth: CrossAxisExtent,
                MaxWidth: CrossAxisExtent,
                MinHeight: MainAxisExtent,
                MaxHeight: MainAxisExtent);
        }

        return new BoxConstraints(
            MinWidth: MainAxisExtent,
            MaxWidth: MainAxisExtent,
            MinHeight: CrossAxisExtent,
            MaxHeight: CrossAxisExtent);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string[] properties =
        [
            $"scrollOffset: {ScrollOffset}",
            $"crossAxisOffset: {CrossAxisOffset}",
            $"mainAxisExtent: {MainAxisExtent}",
            $"crossAxisExtent: {CrossAxisExtent}",
        ];
        return $"SliverGridGeometry({string.Join(", ", properties)})";
    }

}

public abstract class SliverGridLayout
{
    public abstract int GetMinChildIndexForScrollOffset(double scrollOffset);

    public abstract int GetMaxChildIndexForScrollOffset(double scrollOffset);

    public abstract SliverGridGeometry GetGeometryForChildIndex(int index);

    public abstract double ComputeMaxScrollOffset(int childCount);
}

public sealed class SliverGridRegularTileLayout : SliverGridLayout
{
    public SliverGridRegularTileLayout(
        int crossAxisCount,
        double mainAxisStride,
        double crossAxisStride,
        double childMainAxisExtent,
        double childCrossAxisExtent,
        bool reverseCrossAxis)
    {
        if (crossAxisCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisCount), "crossAxisCount must be greater than 0.");
        }

        if (mainAxisStride < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisStride), "mainAxisStride cannot be negative.");
        }

        if (crossAxisStride < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisStride), "crossAxisStride cannot be negative.");
        }

        if (childMainAxisExtent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childMainAxisExtent), "childMainAxisExtent cannot be negative.");
        }

        if (childCrossAxisExtent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childCrossAxisExtent), "childCrossAxisExtent cannot be negative.");
        }

        CrossAxisCount = crossAxisCount;
        MainAxisStride = mainAxisStride;
        CrossAxisStride = crossAxisStride;
        ChildMainAxisExtent = childMainAxisExtent;
        ChildCrossAxisExtent = childCrossAxisExtent;
        ReverseCrossAxis = reverseCrossAxis;
    }

    public int CrossAxisCount { get; }

    public double MainAxisStride { get; }

    public double CrossAxisStride { get; }

    public double ChildMainAxisExtent { get; }

    public double ChildCrossAxisExtent { get; }

    public bool ReverseCrossAxis { get; }

    public override int GetMinChildIndexForScrollOffset(double scrollOffset)
    {
        return MainAxisStride > 0.0001
            ? CrossAxisCount * (int)Math.Floor(scrollOffset / MainAxisStride)
            : 0;
    }

    public override int GetMaxChildIndexForScrollOffset(double scrollOffset)
    {
        if (MainAxisStride > 0)
        {
            int mainAxisCount = (int)Math.Ceiling(scrollOffset / MainAxisStride);
            return Math.Max(0, CrossAxisCount * mainAxisCount - 1);
        }

        return 0;
    }

    public override SliverGridGeometry GetGeometryForChildIndex(int index)
    {
        double crossAxisStart = (index % CrossAxisCount) * CrossAxisStride;
        return new SliverGridGeometry(
            ScrollOffset: (index / CrossAxisCount) * MainAxisStride,
            CrossAxisOffset: OffsetFromStartInCrossAxis(crossAxisStart),
            MainAxisExtent: ChildMainAxisExtent,
            CrossAxisExtent: ChildCrossAxisExtent);
    }

    public override double ComputeMaxScrollOffset(int childCount)
    {
        if (childCount == 0)
        {
            return 0;
        }

        int mainAxisCount = ((childCount - 1) / CrossAxisCount) + 1;
        double mainAxisSpacing = MainAxisStride - ChildMainAxisExtent;
        return MainAxisStride * mainAxisCount - mainAxisSpacing;
    }

    private double OffsetFromStartInCrossAxis(double crossAxisStart)
    {
        if (!ReverseCrossAxis)
        {
            return crossAxisStart;
        }

        return CrossAxisCount * CrossAxisStride
               - crossAxisStart
               - ChildCrossAxisExtent
               - (CrossAxisStride - ChildCrossAxisExtent);
    }
}

public abstract class SliverGridDelegate
{
    public abstract SliverGridLayout GetLayout(SliverConstraints constraints);

    public abstract bool ShouldRelayout(SliverGridDelegate oldDelegate);
}

public sealed class SliverGridDelegateWithFixedCrossAxisCount : SliverGridDelegate
{
    public SliverGridDelegateWithFixedCrossAxisCount(
        int crossAxisCount,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        double? mainAxisExtent = null)
    {
        if (crossAxisCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisCount), "crossAxisCount must be greater than 0.");
        }

        if (mainAxisSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisSpacing), "mainAxisSpacing cannot be negative.");
        }

        if (crossAxisSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisSpacing), "crossAxisSpacing cannot be negative.");
        }

        if (childAspectRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childAspectRatio), "childAspectRatio must be greater than 0.");
        }

        if (mainAxisExtent.HasValue && mainAxisExtent.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisExtent), "mainAxisExtent cannot be negative.");
        }

        CrossAxisCount = crossAxisCount;
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
        ChildAspectRatio = childAspectRatio;
        MainAxisExtent = mainAxisExtent;
    }

    public int CrossAxisCount { get; }

    public double MainAxisSpacing { get; }

    public double CrossAxisSpacing { get; }

    public double ChildAspectRatio { get; }

    public double? MainAxisExtent { get; }

    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        double usableCrossAxisExtent = Math.Max(
            0,
            constraints.CrossAxisExtent - CrossAxisSpacing * (CrossAxisCount - 1));
        double childCrossAxisExtent = usableCrossAxisExtent / CrossAxisCount;
        double childMainAxisExtent = MainAxisExtent ?? childCrossAxisExtent / ChildAspectRatio;
        return new SliverGridRegularTileLayout(
            crossAxisCount: CrossAxisCount,
            mainAxisStride: childMainAxisExtent + MainAxisSpacing,
            crossAxisStride: childCrossAxisExtent + CrossAxisSpacing,
            childMainAxisExtent: childMainAxisExtent,
            childCrossAxisExtent: childCrossAxisExtent,
            reverseCrossAxis: false);
    }

    public override bool ShouldRelayout(SliverGridDelegate oldDelegate)
    {
        if (oldDelegate is not SliverGridDelegateWithFixedCrossAxisCount old)
        {
            return true;
        }

        return old.CrossAxisCount != CrossAxisCount
               || Math.Abs(old.MainAxisSpacing - MainAxisSpacing) > 0.0001
               || Math.Abs(old.CrossAxisSpacing - CrossAxisSpacing) > 0.0001
               || Math.Abs(old.ChildAspectRatio - ChildAspectRatio) > 0.0001
               || NullableDoubleChanged(old.MainAxisExtent, MainAxisExtent);
    }

    private static bool NullableDoubleChanged(double? lhs, double? rhs)
    {
        if (!lhs.HasValue && !rhs.HasValue)
        {
            return false;
        }

        if (lhs.HasValue != rhs.HasValue)
        {
            return true;
        }

        return Math.Abs(lhs!.Value - rhs!.Value) > 0.0001;
    }
}

public sealed class SliverGridDelegateWithMaxCrossAxisExtent : SliverGridDelegate
{
    public SliverGridDelegateWithMaxCrossAxisExtent(
        double maxCrossAxisExtent,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        double? mainAxisExtent = null)
    {
        if (maxCrossAxisExtent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCrossAxisExtent), "maxCrossAxisExtent must be greater than 0.");
        }

        if (mainAxisSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisSpacing), "mainAxisSpacing cannot be negative.");
        }

        if (crossAxisSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossAxisSpacing), "crossAxisSpacing cannot be negative.");
        }

        if (childAspectRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childAspectRatio), "childAspectRatio must be greater than 0.");
        }

        if (mainAxisExtent.HasValue && mainAxisExtent.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainAxisExtent), "mainAxisExtent cannot be negative.");
        }

        MaxCrossAxisExtent = maxCrossAxisExtent;
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
        ChildAspectRatio = childAspectRatio;
        MainAxisExtent = mainAxisExtent;
    }

    public double MaxCrossAxisExtent { get; }

    public double MainAxisSpacing { get; }

    public double CrossAxisSpacing { get; }

    public double ChildAspectRatio { get; }

    public double? MainAxisExtent { get; }

    public override SliverGridLayout GetLayout(SliverConstraints constraints)
    {
        int crossAxisCount = (int)Math.Ceiling(
            constraints.CrossAxisExtent / (MaxCrossAxisExtent + CrossAxisSpacing));
        crossAxisCount = Math.Max(1, crossAxisCount);

        double usableCrossAxisExtent = Math.Max(
            0,
            constraints.CrossAxisExtent - CrossAxisSpacing * (crossAxisCount - 1));
        double childCrossAxisExtent = usableCrossAxisExtent / crossAxisCount;
        double childMainAxisExtent = MainAxisExtent ?? childCrossAxisExtent / ChildAspectRatio;
        return new SliverGridRegularTileLayout(
            crossAxisCount: crossAxisCount,
            mainAxisStride: childMainAxisExtent + MainAxisSpacing,
            crossAxisStride: childCrossAxisExtent + CrossAxisSpacing,
            childMainAxisExtent: childMainAxisExtent,
            childCrossAxisExtent: childCrossAxisExtent,
            reverseCrossAxis: false);
    }

    public override bool ShouldRelayout(SliverGridDelegate oldDelegate)
    {
        if (oldDelegate is not SliverGridDelegateWithMaxCrossAxisExtent old)
        {
            return true;
        }

        return Math.Abs(old.MaxCrossAxisExtent - MaxCrossAxisExtent) > 0.0001
               || Math.Abs(old.MainAxisSpacing - MainAxisSpacing) > 0.0001
               || Math.Abs(old.CrossAxisSpacing - CrossAxisSpacing) > 0.0001
               || Math.Abs(old.ChildAspectRatio - ChildAspectRatio) > 0.0001
               || NullableDoubleChanged(old.MainAxisExtent, MainAxisExtent);
    }

    private static bool NullableDoubleChanged(double? lhs, double? rhs)
    {
        if (!lhs.HasValue && !rhs.HasValue)
        {
            return false;
        }

        if (lhs.HasValue != rhs.HasValue)
        {
            return true;
        }

        return Math.Abs(lhs!.Value - rhs!.Value) > 0.0001;
    }
}

/// <remarks>Flutter's <c>RenderSliverBoxChildManager</c>.</remarks>
public interface IRenderSliverBoxChildManager
{
    /// <summary>
    /// A precise measure of the total number of children: one greater than the greatest index for
    /// which <see cref="CreateChild"/> will actually create a child.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>childCount</c>. Read when <see cref="CreateChild"/> could not add a child for a
    /// positive index, so it must be accurate; it is never read for an infinite child list.
    /// </remarks>
    int ChildCount { get; }

    /// <summary>The best available estimate of <see cref="ChildCount"/>, or null if none exists.</summary>
    /// <remarks>Flutter's <c>estimatedChildCount</c>, which defers to
    /// <see cref="SliverChildDelegate.EstimatedChildCount"/>.</remarks>
    int? EstimatedChildCount => null;

    void CreateChild(int index, RenderBox? after);

    void RemoveChild(RenderBox child);

    /// <summary>
    /// Estimates the total distance from the start of the child with the earliest possible index to
    /// the end of the child with the last possible index.
    /// </summary>
    /// <remarks>Flutter's <c>estimateMaxScrollOffset</c>.</remarks>
    double EstimateMaxScrollOffset(
        SliverConstraints constraints,
        int? firstIndex = null,
        int? lastIndex = null,
        double? leadingScrollOffset = null,
        double? trailingScrollOffset = null);

    void DidAdoptChild(RenderBox child);

    void SetDidUnderflow(bool value);

    /// <summary>Called at the beginning of layout to indicate that layout is about to occur.</summary>
    /// <remarks>Flutter's <c>didStartLayout</c>.</remarks>
    void DidStartLayout()
    {
    }

    /// <summary>Called at the end of layout to indicate that layout is now complete.</summary>
    /// <remarks>Flutter's <c>didFinishLayout</c>.</remarks>
    void DidFinishLayout()
    {
    }

    /// <summary>
    /// In debug mode, asserts that this manager is not expecting any modifications to the
    /// <see cref="RenderSliverMultiBoxAdaptor"/>'s child list. Always returns true.
    /// </summary>
    /// <remarks>Flutter's <c>debugAssertChildListLocked</c>.</remarks>
    bool DebugAssertChildListLocked() => true;
}

public sealed class SliverPhysicalParentData : ContainerBoxParentData<RenderSliver>
{
    public int? CrossAxisFlex { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"paintOffset={offset}";
}

/// <summary>
/// Parent data for slivers positioned by a scroll offset rather than by a paint offset.
/// </summary>
/// <remarks>Flutter's <c>SliverLogicalContainerParentData</c>.</remarks>
public sealed class SliverLogicalParentData : ContainerBoxParentData<RenderSliver>
{
    /// <summary>
    /// The position of the child relative to the zero scroll offset, along the main axis, or null
    /// before the child has been laid out.
    /// </summary>
    public double? LayoutOffset { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"layoutOffset={(LayoutOffset is null
            ? "None"
            : LayoutOffset.Value.ToString("F1", CultureInfo.InvariantCulture))}";
}

public class SliverMultiBoxAdaptorParentData : ContainerBoxParentData<RenderBox>
{
    /// <summary>
    /// The index of this child according to the <see cref="IRenderSliverBoxChildManager"/>, or null
    /// before the manager has adopted the child.
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// The position of the child relative to the zero scroll offset, along the main axis, or null
    /// when the child has not been laid out at this offset yet — which is how a child that the
    /// delegate reordered is marked for collection at the start of the next layout.
    /// </summary>
    public double? LayoutOffset { get; set; }

    public bool KeepAlive { get; set; }

    public bool KeptAlive { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"index={(Index is null ? "null" : Index.Value.ToString(CultureInfo.InvariantCulture))}; "
        + $"{(KeepAlive ? "keepAlive; " : string.Empty)}{base.ToString()}";
}

public sealed class SliverGridParentData : SliverMultiBoxAdaptorParentData
{
    public double CrossAxisOffset { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"crossAxisOffset={CrossAxisOffset}; {base.ToString()}";
}

public abstract class RenderSliver : RenderBox
{
    private SliverConstraints? _sliverConstraints;
    private SliverGeometry _geometry;

    public SliverConstraints ConstraintsForSliver =>
        _sliverConstraints ?? throw new InvalidOperationException("RenderSliver is not laid out.");

    /// <summary>
    /// Whether this sliver has been laid out at least once, so <see cref="ConstraintsForSliver"/> and
    /// <see cref="Geometry"/> are meaningful. Stands in for Flutter's <c>geometry != null</c> check.
    /// </summary>
    public bool HasSliverConstraints => _sliverConstraints.HasValue;

    public SliverGeometry Geometry
    {
        get => _geometry;
        protected set
        {
            if (Constants.KDebugMode)
            {
                DebugCheckGeometrySetterPhase();
            }

            value.DebugAssertIsValid(() =>
            [
                new DiagnosticsProperty<RenderSliver>(
                    "The RenderSliver that returned the offending geometry was",
                    this,
                    style: DiagnosticsTreeStyle.ErrorProperty),
            ]);
            _geometry = value;
        }
    }

    /// <summary>
    /// Ports Dart's <c>RenderSliver.geometry</c> setter assertions: the geometry may only be written
    /// by the sliver itself, from <see cref="PerformResize"/> when <see cref="SizedByParent"/> is
    /// <c>true</c>, and from <c>PerformLayout</c> when it is <c>false</c>.
    /// </summary>
    private void DebugCheckGeometrySetterPhase()
    {
        if (SizedByParent ? DebugDoingThisResize : DebugDoingThisLayout)
        {
            return;
        }

        string violation = DebugDoingThisLayout
            ? "It appears that the geometry setter was called from PerformLayout()."
            : "The geometry setter was called from outside layout (neither PerformResize() nor "
              + "PerformLayout() were being run for this object).";
        string contract = SizedByParent
            ? "Because this RenderSliver has SizedByParent set to true, it must set its geometry in "
              + "PerformResize()."
            : "Because this RenderSliver has SizedByParent set to false, it must set its geometry in "
              + "PerformLayout().";
        throw new AssertionError(
            $"RenderSliver geometry setter called incorrectly.\n{violation}\n{contract}\n"
            + $"The RenderSliver in question is: {GetType().Name}#{Diagnostics.ShortHash(this)}");
    }

    /// <summary>
    /// Whether this sliver keeps contributing semantics even when it is scrolled entirely outside the
    /// viewport's paint and cache extents.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliver.ensureSemantics</c>. Defaults to <c>false</c>.</remarks>
    public virtual bool EnsureSemantics => false;

    /// <summary>
    /// The amount by which the viewport's zero scroll offset is shifted when this sliver is the
    /// viewport's <c>center</c>.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliver.centerOffsetAdjustment</c>. Defaults to <c>0.0</c>.</remarks>
    public virtual double CenterOffsetAdjustment => 0.0;

    /// <summary>Lays this sliver out under sliver constraints.</summary>
    /// <remarks>
    /// Flutter's viewports call <c>child.layout(SliverConstraints(...), parentUsesSize: true)</c> —
    /// they always read the child's <c>geometry</c> afterwards — so a sliver is a relayout boundary
    /// only when it is <see cref="RenderObject.SizedByParent"/>, never merely by virtue of being laid
    /// out by a viewport.
    /// </remarks>
    public void LayoutWithSliverConstraints(SliverConstraints constraints, bool parentUsesSize = true)
    {
        if (_sliverConstraints != constraints)
        {
            MarkNeedsImmediateRelayout();
        }

        _sliverConstraints = constraints;
        double remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        double scrollAwareMainAxisExtent = constraints.ViewportMainAxisExtent
                                           + Math.Max(0, constraints.ScrollOffset)
                                           + Math.Max(0, remainingCacheExtent);

        BoxConstraints layoutConstraints;
        if (constraints.Axis == Axis.Vertical)
        {
            layoutConstraints = new BoxConstraints(
                MinWidth: constraints.CrossAxisExtent,
                MaxWidth: constraints.CrossAxisExtent,
                MinHeight: 0,
                MaxHeight: scrollAwareMainAxisExtent);
        }
        else
        {
            layoutConstraints = new BoxConstraints(
                MinWidth: 0,
                MaxWidth: scrollAwareMainAxisExtent,
                MinHeight: constraints.CrossAxisExtent,
                MaxHeight: constraints.CrossAxisExtent);
        }

        Layout(layoutConstraints, parentUsesSize: parentUsesSize);
    }

    protected override void PerformLayout()
    {
        var constraints = ConstraintsForSliver;
        PerformSliverLayout(constraints);

        double mainExtent = Math.Max(0, Geometry.PaintExtent);
        Size = constraints.Axis == Axis.Vertical
            ? new Size(constraints.CrossAxisExtent, mainExtent)
            : new Size(mainExtent, constraints.CrossAxisExtent);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (!HasSliverConstraints)
        {
            return false;
        }

        SliverConstraints constraints = ConstraintsForSliver;
        double mainAxisPosition = constraints.Axis == Axis.Vertical ? position.Y : position.X;
        double crossAxisPosition = constraints.Axis == Axis.Vertical ? position.X : position.Y;
        if (mainAxisPosition < 0.0
            || mainAxisPosition >= Geometry.HitTestExtent
            || crossAxisPosition < 0.0
            || crossAxisPosition >= constraints.CrossAxisExtent)
        {
            return false;
        }

        if (HitTestChildren(result, position) || HitTestSelf(position))
        {
            result.Add(new BoxHitTestEntry(this, position));
            return true;
        }

        return false;
    }

    protected abstract void PerformSliverLayout(SliverConstraints constraints);

    protected double CalculatePaintOffset(
        SliverConstraints constraints,
        double from,
        double to)
    {
        if (from > to)
        {
            throw new ArgumentOutOfRangeException(nameof(from), "from must be less than or equal to to.");
        }

        double leading = constraints.ScrollOffset;
        double trailing = constraints.ScrollOffset + constraints.RemainingPaintExtent;
        return Math.Clamp(
            Math.Clamp(to, leading, trailing) - Math.Clamp(from, leading, trailing),
            0.0,
            constraints.RemainingPaintExtent);
    }

    protected double CalculateCacheOffset(
        SliverConstraints constraints,
        double from,
        double to)
    {
        if (from > to)
        {
            throw new ArgumentOutOfRangeException(nameof(from), "from must be less than or equal to to.");
        }

        double leading = constraints.ScrollOffset + constraints.CacheOrigin;
        double trailing = constraints.ScrollOffset + constraints.RemainingCacheExtent;
        return Math.Clamp(
            Math.Clamp(to, leading, trailing) - Math.Clamp(from, leading, trailing),
            0.0,
            constraints.RemainingCacheExtent);
    }

    /// <summary>
    /// Returns the distance from the leading visible edge of this sliver to the leading edge of the
    /// given child, in the sliver's main axis.
    /// </summary>
    /// <remarks>
    /// Slivers that have children must override this; the base implementation throws the way
    /// Flutter's debug assert does.
    /// </remarks>
    public virtual double ChildMainAxisPosition(RenderObject child)
    {
        throw new InvalidOperationException(
            $"{GetType().Name} does not implement {nameof(ChildMainAxisPosition)}.");
    }

    /// <summary>
    /// Returns the distance from the leading edge of this sliver's cross axis to the leading edge of
    /// the given child.
    /// </summary>
    public virtual double ChildCrossAxisPosition(RenderObject child)
    {
        return 0.0;
    }

    /// <summary>Returns the scroll offset of the leading edge of the given child.</summary>
    /// <remarks>Null when the child's position cannot be determined (it has not been laid out yet).</remarks>
    public virtual double? ChildScrollOffset(RenderObject child)
    {
        if (!ReferenceEquals(child.Parent, this))
        {
            throw new ArgumentException("The child does not belong to this sliver.", nameof(child));
        }

        return 0.0;
    }

    protected Rect GetMaxPaintRect()
    {
        SliverGeometry geometry = Geometry;
        if (geometry == default)
        {
            return default;
        }

        SliverConstraints constraints = ConstraintsForSliver;
        double maxPaintExtent = geometry.MaxPaintExtent;
        if (double.IsPositiveInfinity(maxPaintExtent))
        {
            maxPaintExtent = constraints.ScrollOffset + geometry.CacheExtent + constraints.CacheOrigin;
        }

        double obstructionAdjustedScrollExtent = Math.Max(
            0.0,
            geometry.ScrollExtent - geometry.MaxScrollObstructionExtent);
        double leadingOffset = Math.Clamp(
            constraints.ScrollOffset,
            0.0,
            obstructionAdjustedScrollExtent);
        double crossAxisExtent = geometry.CrossAxisExtent ?? constraints.CrossAxisExtent;
        var rect = constraints.Axis == Axis.Horizontal
            ? new Rect(-leadingOffset, 0.0, maxPaintExtent, crossAxisExtent)
            : new Rect(0.0, -leadingOffset, crossAxisExtent, maxPaintExtent);

        AxisDirection effectiveAxisDirection = constraints.GrowthDirection == GrowthDirection.Forward
            ? constraints.AxisDirection
            : ReverseAxisDirection(constraints.AxisDirection);
        return effectiveAxisDirection switch
        {
            AxisDirection.Left => new Rect(
                geometry.PaintExtent - rect.Right,
                rect.Top,
                rect.Width,
                rect.Height),
            AxisDirection.Up => new Rect(
                rect.Left,
                geometry.PaintExtent - rect.Bottom,
                rect.Width,
                rect.Height),
            _ => rect,
        };
    }

    private static AxisDirection ReverseAxisDirection(AxisDirection direction) => direction switch
    {
        AxisDirection.Up => AxisDirection.Down,
        AxisDirection.Right => AxisDirection.Left,
        AxisDirection.Down => AxisDirection.Up,
        AxisDirection.Left => AxisDirection.Right,
        _ => direction,
    };

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliver.debugPaint</c>: a green arrow diagram showing this sliver's paint
    /// extent and growth direction. Dart strokes it through a
    /// <c>MaskFilter.blur(BlurStyle.solid, strokeWidth)</c>; Avalonia's drawing backend takes no
    /// mask filter, so the same stroke is drawn unblurred (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    protected override void DebugPaint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!RenderingDebug.PaintSizeEnabled || !HasSliverConstraints)
        {
            return;
        }

        double strokeWidth = Math.Min(4.0, Geometry.PaintExtent / 30.0);
        var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFF33CC33)), strokeWidth);
        double arrowExtent = Geometry.PaintExtent;
        double padding = Math.Max(2.0, strokeWidth);
        SliverConstraints constraints = ConstraintsForSliver;
        context.Canvas.DrawCircle(
            Brushes.Transparent,
            pen,
            new Point(offset.X + padding, offset.Y + padding),
            padding * 0.5);
        double cross = constraints.CrossAxisExtent;
        if (constraints.Axis == Axis.Vertical)
        {
            context.Canvas.DrawLine(pen, offset, new Point(offset.X + cross, offset.Y));
            DebugDrawArrow(
                context,
                pen,
                new Point(offset.X + (cross * 1.0 / 4.0), offset.Y + padding),
                new Point(offset.X + (cross * 1.0 / 4.0), offset.Y + arrowExtent - padding),
                constraints.NormalizedGrowthDirection);
            DebugDrawArrow(
                context,
                pen,
                new Point(offset.X + (cross * 3.0 / 4.0), offset.Y + padding),
                new Point(offset.X + (cross * 3.0 / 4.0), offset.Y + arrowExtent - padding),
                constraints.NormalizedGrowthDirection);
        }
        else
        {
            context.Canvas.DrawLine(pen, offset, new Point(offset.X, offset.Y + cross));
            DebugDrawArrow(
                context,
                pen,
                new Point(offset.X + padding, offset.Y + (cross * 1.0 / 4.0)),
                new Point(offset.X + arrowExtent - padding, offset.Y + (cross * 1.0 / 4.0)),
                constraints.NormalizedGrowthDirection);
            DebugDrawArrow(
                context,
                pen,
                new Point(offset.X + padding, offset.Y + (cross * 3.0 / 4.0)),
                new Point(offset.X + arrowExtent - padding, offset.Y + (cross * 3.0 / 4.0)),
                constraints.NormalizedGrowthDirection);
        }
    }

    /// <remarks>Flutter's <c>RenderSliver._debugDrawArrow</c>.</remarks>
    private static void DebugDrawArrow(
        PaintingContext context,
        IPen pen,
        Point p0,
        Point p1,
        GrowthDirection direction)
    {
        if (p0 == p1)
        {
            return;
        }

        Debug.Assert(p0.X == p1.X || p0.Y == p1.Y, "The arrow must be axis-aligned.");
        Point delta = p1 - p0;
        double d = Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y)) * 0.2;
        double dx1;
        double dx2;
        double dy1;
        double dy2;
        if (direction == GrowthDirection.Forward)
        {
            dx1 = dx2 = dy1 = dy2 = d;
        }
        else
        {
            (p0, p1) = (p1, p0);
            dx1 = dx2 = dy1 = dy2 = -d;
        }

        if (p0.X == p1.X)
        {
            dx2 = -dx2;
        }
        else
        {
            dy2 = -dy2;
        }

        var path = new Plumix.UI.Path();
        path.MoveTo(p0.X, p0.Y);
        path.LineTo(p1.X, p1.Y);
        path.MoveTo(p1.X - dx1, p1.Y - dy1);
        path.LineTo(p1.X, p1.Y);
        path.LineTo(p1.X - dx2, p1.Y - dy2);
        context.Canvas.DrawPath(path, brush: null, pen: pen);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<SliverGeometry>("geometry", Geometry));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_sliver.dart
public abstract class RenderProxySliver : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderSliver? _child;

    protected RenderProxySliver(RenderSliver? child = null)
    {
        Child = child;
    }

    public RenderSliver? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;
            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderSliver?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverPhysicalParentData)
        {
            child.parentData = new SliverPhysicalParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return 0.0;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child == null || !Geometry.Visible)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        ctx.PaintChild(_child, offset + childParentData.offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null || Geometry.HitTestExtent <= 0.0)
        {
            return false;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        RenderSliver child = _child;
        return result.AddWithPaintOffset(
            childParentData.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_child == null)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        visitor(_child);
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (_child == null)
        {
            Geometry = default;
            return;
        }

        _child.LayoutWithSliverConstraints(constraints);
        ((SliverPhysicalParentData)_child.parentData!).offset = new Point(0, 0);
        Geometry = _child.Geometry;
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

public sealed class RenderSliverIgnorePointer : RenderProxySliver
{
    private bool _ignoring;
    private bool? _ignoringSemantics;

    public RenderSliverIgnorePointer(
        bool ignoring = true,
        bool? ignoringSemantics = null,
        RenderSliver? sliver = null) : base(sliver)
    {
        _ignoring = ignoring;
        _ignoringSemantics = ignoringSemantics;
    }

    public bool Ignoring
    {
        get => _ignoring;
        set
        {
            if (_ignoring == value)
            {
                return;
            }

            _ignoring = value;
            if (_ignoringSemantics == null)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public bool? IgnoringSemantics
    {
        get => _ignoringSemantics;
        set
        {
            if (_ignoringSemantics == value)
            {
                return;
            }

            _ignoringSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return !_ignoring && base.HitTest(result, position);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_ignoringSemantics != true)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsBlockingUserActions = _ignoring && (_ignoringSemantics ?? true);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("ignoring", Ignoring));
        properties.Add(new DiagnosticsProperty<bool?>(
            "ignoringSemantics",
            IgnoringSemantics,
            description: IgnoringSemantics is null ? null : $"implicitly {IgnoringSemantics}"));
    }
}

public sealed class RenderSliverOffstage : RenderProxySliver
{
    private bool _offstage;

    public RenderSliverOffstage(bool offstage = true, RenderSliver? sliver = null) : base(sliver)
    {
        _offstage = offstage;
    }

    public bool Offstage
    {
        get => _offstage;
        set
        {
            if (_offstage == value)
            {
                return;
            }

            _offstage = value;
            MarkNeedsLayout();
        }
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return !_offstage && base.HitTest(result, position);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (!_offstage)
        {
            base.Paint(ctx, offset);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (!_offstage)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        base.PerformSliverLayout(constraints);
        if (_offstage)
        {
            Geometry = default;
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("offstage", Offstage));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        if (Child is null)
        {
            return [];
        }

        return
        [
            Child.ToDiagnosticsNode(
                name: "child",
                style: Offstage ? DiagnosticsTreeStyle.Offstage : DiagnosticsTreeStyle.Sparse),
        ];
    }
}

internal sealed class RenderSliverVisibility : RenderProxySliver
{
    private bool _visible;
    private bool _maintainSemantics;

    public RenderSliverVisibility(bool visible, bool maintainSemantics, RenderSliver? sliver = null) : base(sliver)
    {
        _visible = visible;
        _maintainSemantics = maintainSemantics;
    }

    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
            {
                return;
            }

            _visible = value;
            MarkNeedsPaint();
        }
    }

    public bool MaintainSemantics
    {
        get => _maintainSemantics;
        set
        {
            if (_maintainSemantics == value)
            {
                return;
            }

            _maintainSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_maintainSemantics || _visible)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_visible)
        {
            base.Paint(ctx, offset);
        }
    }
}

public sealed class RenderSliverOpacity : RenderProxySliver
{
    private double _opacity;
    private bool _alwaysIncludeSemantics;

    public RenderSliverOpacity(
        double opacity = 1.0,
        bool alwaysIncludeSemantics = false,
        RenderSliver? sliver = null) : base(sliver)
    {
        _opacity = ValidateOpacity(opacity, nameof(opacity));
        _alwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public double Opacity
    {
        get => _opacity;
        set
        {
            double normalized = ValidateOpacity(value, nameof(value));
            if (Math.Abs(_opacity - normalized) <= 0.000001)
            {
                return;
            }

            bool compositingChanged = (_opacity > 0.0) != (normalized > 0.0);
            bool semanticsVisibilityChanged = (_opacity == 0.0) != (normalized == 0.0);
            _opacity = normalized;
            if (compositingChanged)
            {
                MarkNeedsCompositingBitsUpdate();
            }

            MarkNeedsCompositedLayerUpdate();
            if (semanticsVisibilityChanged && !_alwaysIncludeSemantics)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public bool AlwaysIncludeSemantics
    {
        get => _alwaysIncludeSemantics;
        set
        {
            if (_alwaysIncludeSemantics == value)
            {
                return;
            }

            _alwaysIncludeSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool IsRepaintBoundary => Child != null && _opacity > 0.0;

    protected override bool AlwaysNeedsCompositing => Child != null && _opacity > 0.0;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_opacity == 0.0)
        {
            return;
        }

        base.Paint(ctx, offset);
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as OpacityLayer ?? new OpacityLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is OpacityLayer opacityLayer)
        {
            opacityLayer.Opacity = _opacity;
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_opacity > 0.0 || _alwaysIncludeSemantics)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    private static double ValidateOpacity(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Opacity must be between zero and one.");
        }

        return value;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("opacity", Opacity));
        properties.Add(new FlagProperty(
            "alwaysIncludeSemantics",
            AlwaysIncludeSemantics,
            ifTrue: "alwaysIncludeSemantics"));
    }
}

public sealed class RenderSliverAnimatedOpacity : RenderProxySliver
{
    private Animation<double> _opacity;
    private double _currentOpacity;
    private bool _alwaysIncludeSemantics;

    public RenderSliverAnimatedOpacity(
        Animation<double> opacity,
        bool alwaysIncludeSemantics = false,
        RenderSliver? sliver = null) : base(sliver)
    {
        _opacity = opacity ?? throw new ArgumentNullException(nameof(opacity));
        _currentOpacity = NormalizeOpacity(opacity.Value);
        _alwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public Animation<double> Opacity
    {
        get => _opacity;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_opacity, value))
            {
                return;
            }

            if (Attached)
            {
                _opacity.RemoveListener(HandleOpacityChanged);
                value.AddListener(HandleOpacityChanged);
            }

            _opacity = value;
            UpdateOpacity();
        }
    }

    public bool AlwaysIncludeSemantics
    {
        get => _alwaysIncludeSemantics;
        set
        {
            if (_alwaysIncludeSemantics == value)
            {
                return;
            }

            _alwaysIncludeSemantics = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public override bool IsRepaintBoundary => Child != null && _currentOpacity > 0.0;

    protected override bool AlwaysNeedsCompositing => Child != null && _currentOpacity > 0.0;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_currentOpacity == 0.0)
        {
            return;
        }

        base.Paint(ctx, offset);
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as OpacityLayer ?? new OpacityLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is OpacityLayer opacityLayer)
        {
            opacityLayer.Opacity = _currentOpacity;
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _opacity.AddListener(HandleOpacityChanged);
        UpdateOpacity();
    }

    protected override void OnDetach()
    {
        _opacity.RemoveListener(HandleOpacityChanged);
        base.OnDetach();
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_currentOpacity > 0.0 || _alwaysIncludeSemantics)
        {
            base.VisitChildrenForSemantics(visitor);
        }
    }

    private void HandleOpacityChanged()
    {
        UpdateOpacity();
    }

    private void UpdateOpacity()
    {
        double normalized = NormalizeOpacity(_opacity.Value);
        if (Math.Abs(_currentOpacity - normalized) <= 0.000001)
        {
            return;
        }

        bool compositingChanged = (_currentOpacity > 0.0) != (normalized > 0.0);
        bool semanticsVisibilityChanged = (_currentOpacity == 0.0) != (normalized == 0.0);
        _currentOpacity = normalized;
        if (compositingChanged)
        {
            MarkNeedsCompositingBitsUpdate();
        }

        MarkNeedsCompositedLayerUpdate();
        if (semanticsVisibilityChanged && !_alwaysIncludeSemantics)
        {
            MarkNeedsSemanticsUpdate();
        }
    }

    private static double NormalizeOpacity(double value)
    {
        return double.IsNaN(value) ? 0.0 : Math.Clamp(value, 0.0, 1.0);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Animation<double>>("opacity", Opacity));
        properties.Add(new FlagProperty(
            "alwaysIncludeSemantics",
            AlwaysIncludeSemantics,
            ifTrue: "alwaysIncludeSemantics"));
    }
}

public abstract class RenderSliverSingleBoxAdapter : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;

    public RenderBox? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;
            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderBox?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    protected static double ChildExtentForAxis(Size size, Axis axis)
    {
        return axis == Axis.Vertical ? size.Height : size.Width;
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return -ConstraintsForSliver.ScrollOffset;
    }

    protected static void SetChildParentData(
        RenderBox child,
        SliverConstraints constraints,
        SliverGeometry geometry)
    {
        var childParentData = (BoxParentData)child.parentData!;
        childParentData.offset = ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            constraints.AxisDirection,
            constraints.GrowthDirection) switch
        {
            AxisDirection.Up => new Point(
                0.0,
                -(geometry.ScrollExtent - (geometry.PaintExtent + constraints.ScrollOffset))),
            AxisDirection.Right => new Point(-constraints.ScrollOffset, 0.0),
            AxisDirection.Down => new Point(0.0, -constraints.ScrollOffset),
            _ => new Point(
                -(geometry.ScrollExtent - (geometry.PaintExtent + constraints.ScrollOffset)),
                0.0),
        };
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child == null || !Geometry.Visible)
        {
            return;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        ctx.PaintChild(Child, offset + childParentData.offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null || Geometry.HitTestExtent <= 0.0)
        {
            return false;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        RenderBox child = Child;
        return result.AddWithPaintOffset(
            childParentData.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Child == null)
        {
            return;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        visitor(Child);
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

public class RenderSliverToBoxAdapter : RenderSliverSingleBoxAdapter
{
    public RenderSliverToBoxAdapter(RenderBox? child = null)
    {
        Child = child;
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (Child == null)
        {
            Geometry = default;
            return;
        }

        BoxConstraints childConstraints;
        if (constraints.Axis == Axis.Vertical)
        {
            childConstraints = new BoxConstraints(
                MinWidth: constraints.CrossAxisExtent,
                MaxWidth: constraints.CrossAxisExtent,
                MinHeight: 0,
                MaxHeight: double.PositiveInfinity);
        }
        else
        {
            childConstraints = new BoxConstraints(
                MinWidth: 0,
                MaxWidth: double.PositiveInfinity,
                MinHeight: constraints.CrossAxisExtent,
                MaxHeight: constraints.CrossAxisExtent);
        }

        Child.Layout(childConstraints, parentUsesSize: true);

        double childExtent = ChildExtentForAxis(Child.Size, constraints.Axis);
        double effectiveScrollOffset = Math.Clamp(constraints.ScrollOffset, 0, childExtent);
        double remaining = Math.Max(0, childExtent - effectiveScrollOffset);

        double paintedExtent = Math.Min(remaining, constraints.RemainingPaintExtent);
        double layoutExtent = paintedExtent;
        double cacheStart = constraints.ScrollOffset + constraints.CacheOrigin;
        double cacheEnd = cacheStart + Math.Max(0, constraints.RemainingCacheExtent);
        double cacheExtent = Math.Max(0, Math.Min(childExtent, cacheEnd) - Math.Max(0, cacheStart));

        Geometry = new SliverGeometry(
            ScrollExtent: childExtent,
            PaintExtent: paintedExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: childExtent,
            HitTestExtent: paintedExtent,
            CacheExtent: cacheExtent,
            HasVisualOverflow: remaining > constraints.RemainingPaintExtent);
        SetChildParentData(Child, constraints with { ScrollOffset = effectiveScrollOffset }, Geometry);
    }
}

public class RenderSliverPadding : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderSliver? _child;
    private Thickness _padding;
    private double _beforePadding;
    private double _crossStartPadding;

    public RenderSliverPadding(Thickness padding, RenderSliver? child = null)
    {
        _padding = padding;
        Child = child;
    }

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding.Equals(value))
            {
                return;
            }

            _padding = value;
            MarkNeedsLayout();
        }
    }

    public RenderSliver? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child != null)
            {
                DropChild(_child);
            }

            _child = value;
            if (_child != null)
            {
                AdoptChild(_child);
            }

            MarkNeedsLayout();
        }
    }

    RenderObject? IRenderObjectSingleChildContainer.Child
    {
        get => Child;
        set => Child = (RenderSliver?)value;
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverPhysicalParentData)
        {
            child.parentData = new SliverPhysicalParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child == null || !_child.Geometry.Visible)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        ctx.PaintChild(_child, offset + childParentData.offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null || _child.Geometry.HitTestExtent <= 0.0)
        {
            return false;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        RenderSliver child = _child;
        return result.AddWithPaintOffset(
            childParentData.offset,
            position,
            (hitResult, transformed) => child.HitTest(hitResult, transformed));
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_child == null)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        visitor(_child);
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        Thickness resolvedPadding = ResolvePaddingForConstraints(constraints);
        (double mainStartPadding, double mainEndPadding, double crossStartPadding, double crossEndPadding) =
            ResolvePadding(resolvedPadding, constraints);
        double mainAxisPadding = mainStartPadding + mainEndPadding;
        double crossAxisPadding = crossStartPadding + crossEndPadding;
        _beforePadding = mainStartPadding;
        _crossStartPadding = crossStartPadding;
        double remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;

        if (_child == null)
        {
            double paddedPaintExtent = CalculatePaintExtent(
                from: 0,
                to: mainAxisPadding,
                scrollOffset: constraints.ScrollOffset,
                remainingPaintExtent: constraints.RemainingPaintExtent);
            double paddedLayoutExtent = Math.Min(paddedPaintExtent, constraints.ViewportMainAxisExtent);
            double paddedCacheExtent = CalculatePaintExtent(
                from: 0,
                to: mainAxisPadding,
                scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
                remainingPaintExtent: remainingCacheExtent);
            double paddedTargetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

            Geometry = new SliverGeometry(
                ScrollExtent: mainAxisPadding,
                PaintExtent: paddedPaintExtent,
                LayoutExtent: paddedLayoutExtent,
                MaxPaintExtent: mainAxisPadding,
                CacheExtent: paddedCacheExtent,
                HasVisualOverflow: mainAxisPadding > paddedTargetEndScrollOffsetForPaint || constraints.ScrollOffset > 0);
            return;
        }

        double cacheStart = constraints.ScrollOffset + constraints.CacheOrigin;
        double cacheEnd = cacheStart + Math.Max(0, remainingCacheExtent);
        double childScrollOffset = Math.Max(0, constraints.ScrollOffset - mainStartPadding);
        double childCacheStart = Math.Max(0, cacheStart - mainStartPadding);
        double childCacheEnd = Math.Max(childCacheStart, cacheEnd - mainStartPadding);
        double childRemainingCacheExtent = Math.Max(0, childCacheEnd - childCacheStart);
        double childCacheOrigin = childCacheStart - childScrollOffset;
        double beforePaddingPaintExtent = CalculatePaintExtent(
            from: 0,
            to: mainStartPadding,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        double childRemainingPaintExtent = Math.Max(0, constraints.RemainingPaintExtent - beforePaddingPaintExtent);
        double childCrossAxisExtent = Math.Max(0, constraints.CrossAxisExtent - crossAxisPadding);

        _child.LayoutWithSliverConstraints(new SliverConstraints(
            constraints.Axis,
            childScrollOffset,
            childRemainingPaintExtent,
            childCrossAxisExtent,
            constraints.ViewportMainAxisExtent,
            CacheOrigin: childCacheOrigin,
            RemainingCacheExtent: childRemainingCacheExtent,
            AxisDirection: constraints.AxisDirection,
            GrowthDirection: constraints.GrowthDirection));

        if (_child.Geometry.ScrollOffsetCorrection is double correction)
        {
            Geometry = new SliverGeometry(ScrollOffsetCorrection: correction);
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        // Child paint origin is the visible portion of leading padding; the child sliver
        // applies its own scroll offset internally and must not be shifted by full scroll offset again.
        double childMainAxisOffset = beforePaddingPaintExtent;
        childParentData.offset = constraints.Axis == Axis.Vertical
            ? new Point(crossStartPadding, childMainAxisOffset)
            : new Point(childMainAxisOffset, crossStartPadding);

        double totalScrollExtent = mainStartPadding + _child.Geometry.ScrollExtent + mainEndPadding;
        double maxPaintExtent = mainStartPadding + _child.Geometry.MaxPaintExtent + mainEndPadding;
        double afterPaddingPaintExtent = CalculatePaintExtent(
            from: mainStartPadding + _child.Geometry.ScrollExtent,
            to: totalScrollExtent,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        double mainAxisPaddingPaintExtent = beforePaddingPaintExtent + afterPaddingPaintExtent;
        double paintExtent = CalculatePaintExtent(
            from: 0,
            to: totalScrollExtent,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        double layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
        double hitTestExtent = Math.Max(
            mainAxisPaddingPaintExtent + _child.Geometry.PaintExtent,
            beforePaddingPaintExtent + _child.Geometry.HitTestExtent);
        double cacheExtent = CalculatePaintExtent(
            from: 0,
            to: totalScrollExtent,
            scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
            remainingPaintExtent: remainingCacheExtent);
        double targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        Geometry = new SliverGeometry(
            ScrollExtent: totalScrollExtent,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: maxPaintExtent,
            HitTestExtent: hitTestExtent,
            CacheExtent: cacheExtent,
            HasVisualOverflow:
            _child.Geometry.HasVisualOverflow
            || totalScrollExtent > targetEndScrollOffsetForPaint
            || constraints.ScrollOffset > 0);
    }

    /// <summary>The main-axis padding before the child, in scroll-offset units.</summary>
    protected double BeforePadding => _beforePadding;

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return CalculatePaintOffset(ConstraintsForSliver, from: 0.0, to: _beforePadding);
    }

    public override double ChildCrossAxisPosition(RenderObject child)
    {
        return _crossStartPadding;
    }

    public override double? ChildScrollOffset(RenderObject child)
    {
        if (!ReferenceEquals(child.Parent, this))
        {
            throw new ArgumentException("The child does not belong to this sliver.", nameof(child));
        }

        return _beforePadding;
    }

    protected virtual Thickness ResolvePaddingForConstraints(SliverConstraints constraints)
    {
        return _padding;
    }

    private static (double mainStart, double mainEnd, double crossStart, double crossEnd) ResolvePadding(
        Thickness padding,
        SliverConstraints constraints)
    {
        double mainStart;
        double mainEnd;
        double crossStart;
        double crossEnd;

        if (constraints.Axis == Axis.Vertical)
        {
            mainStart = constraints.AxisDirection == AxisDirection.Up ? padding.Bottom : padding.Top;
            mainEnd = constraints.AxisDirection == AxisDirection.Up ? padding.Top : padding.Bottom;
            crossStart = padding.Left;
            crossEnd = padding.Right;
        }
        else
        {
            mainStart = constraints.AxisDirection == AxisDirection.Left ? padding.Right : padding.Left;
            mainEnd = constraints.AxisDirection == AxisDirection.Left ? padding.Left : padding.Right;
            crossStart = padding.Top;
            crossEnd = padding.Bottom;
        }

        if (constraints.GrowthDirection == GrowthDirection.Reverse)
        {
            (mainStart, mainEnd) = (mainEnd, mainStart);
        }

        return (mainStart, mainEnd, crossStart, crossEnd);
    }

    private static double CalculatePaintExtent(
        double from,
        double to,
        double scrollOffset,
        double remainingPaintExtent)
    {
        double visibleStart = Math.Max(from, scrollOffset);
        double visibleEnd = Math.Min(to, scrollOffset + remainingPaintExtent);
        return Math.Max(0, visibleEnd - visibleStart);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliverEdgeInsetsPadding.debugPaint</c>. Dart's <c>getAbsoluteSize()</c> is
    /// Plumix's <see cref="RenderBox.Size"/>, which <c>RenderSliver.PerformLayout</c> already sets
    /// from the paint extent and the cross axis extent.
    /// </remarks>
    protected override void DebugPaint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        base.DebugPaint(context, offset);
        if (!RenderingDebug.PaintSizeEnabled)
        {
            return;
        }

        var outerRect = new Rect(offset, Size);
        Rect? innerRect = null;
        if (_child is not null)
        {
            var childParentData = (SliverPhysicalParentData)_child.parentData!;
            innerRect = new Rect(offset + childParentData.offset, _child.Size);
        }

        RenderingDebug.PaintPadding(context, outerRect, innerRect);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Thickness>("padding", Padding));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

public abstract class RenderSliverMultiBoxAdaptor : RenderSliver,
    IRenderBoxContainerDefaultsMixin<RenderBox, SliverMultiBoxAdaptorParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, SliverMultiBoxAdaptorParentData> _container;
    private readonly Dictionary<int, RenderBox> _keepAliveBucket = [];
    private readonly List<RenderBox> _debugDanglingKeepAlives = [];
    private IRenderSliverBoxChildManager? _childManager;
    private bool _debugChildIntegrityEnabled = true;

    protected RenderSliverMultiBoxAdaptor(IRenderSliverBoxChildManager? childManager = null)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, SliverMultiBoxAdaptorParentData>(this);
        _childManager = childManager;
    }

    public IRenderSliverBoxChildManager? ChildManager
    {
        get => _childManager;
        set
        {
            if (ReferenceEquals(_childManager, value))
            {
                return;
            }

            _childManager = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>
    /// Whether the child-integrity check is enabled. Setting it immediately performs the check.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor.debugChildIntegrityEnabled</c>. The check verifies
    /// that the child indices in the child list are in ascending order, and that <see cref="Move"/>
    /// left no dangling kept-alive child behind. It has no effect in release builds.
    /// </remarks>
    public bool DebugChildIntegrityEnabled
    {
        get => _debugChildIntegrityEnabled;
        set
        {
            if (!Constants.KDebugMode)
            {
                return;
            }

            _debugChildIntegrityEnabled = value;
            Debug.Assert(DebugVerifyChildOrder());
            Debug.Assert(!_debugChildIntegrityEnabled || _debugDanglingKeepAlives.Count == 0);
        }
    }

    public int ChildCount => _container.ChildCount;

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor.adoptChild</c>: a child that is being revived out of
    /// the keep-alive bucket keeps the index it was cached under, so the child manager is not told
    /// about it again.
    /// </remarks>
    public override void AdoptChild(RenderObject child)
    {
        base.AdoptChild(child);
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (!childParentData.KeptAlive)
        {
            _childManager?.DidAdoptChild((RenderBox)child);
        }
    }

    public void Insert(RenderBox child, RenderBox? after = null)
    {
        Debug.Assert(!_keepAliveBucket.ContainsValue(child));
        SetupParentData(child);
        _container.Insert(child, after);
        Debug.Assert(FirstChild is not null);
        Debug.Assert(DebugVerifyChildOrder());
    }

    public void Move(RenderBox child, RenderBox? after = null)
    {
        // Two scenarios. A child that is not kept alive still sits in the container's child list, so
        // the move relinks it and the manager updates the slot. A kept-alive child is no longer in
        // that list but may sit in the keep-alive bucket, whose key has to move with it.
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (!childParentData.KeptAlive)
        {
            _container.Move(child, after);
            _childManager?.DidAdoptChild(child);

            // The slot may change even when the position does not, so the layout still has to re-run.
            MarkNeedsLayout();
            return;
        }

        // If the child in the bucket is not this child, someone has already moved and replaced it,
        // and this child must not be removed.
        if (_keepAliveBucket.TryGetValue(childParentData.Index!.Value, out RenderBox? cachedChild)
            && ReferenceEquals(cachedChild, child))
        {
            _keepAliveBucket.Remove(childParentData.Index!.Value);
        }

        if (Constants.KDebugMode)
        {
            _debugDanglingKeepAlives.Remove(child);
        }

        _childManager?.DidAdoptChild(child);
        if (Constants.KDebugMode
            && _keepAliveBucket.TryGetValue(childParentData.Index!.Value, out RenderBox? displaced))
        {
            _debugDanglingKeepAlives.Add(displaced);
        }

        _keepAliveBucket[childParentData.Index!.Value] = child;
        MarkNeedsLayout();
    }

    public void Remove(RenderBox child)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (!childParentData.KeptAlive)
        {
            _container.Remove(child);
            return;
        }

        Debug.Assert(_keepAliveBucket[childParentData.Index!.Value] == child);
        if (Constants.KDebugMode)
        {
            _debugDanglingKeepAlives.Remove(child);
        }

        _keepAliveBucket.Remove(childParentData.Index!.Value);
        DropChild(child);
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, (RenderBox?)after);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, (RenderBox?)after);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderBox)child);
    }

    public RenderBox? ChildAfter(RenderBox child)
    {
        return _container.ChildAfter(child);
    }

    public RenderBox? ChildBefore(RenderBox child)
    {
        return _container.ChildBefore(child);
    }

    public void AddAll(List<RenderBox>? children)
    {
        _container.AddAll(children);
    }

    public void RemoveAll()
    {
        _container.RemoveAll();
        foreach (RenderBox child in _keepAliveBucket.Values)
        {
            DropChild(child);
        }

        _keepAliveBucket.Clear();
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverMultiBoxAdaptorParentData)
        {
            child.parentData = new SliverMultiBoxAdaptorParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }

        foreach (var child in _keepAliveBucket.Values)
        {
            visitor(child);
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        _container.DefaultPaint(ctx, offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            visitor(child);
        }
    }

    protected BoxConstraints ChildConstraintsForSliver(SliverConstraints constraints)
    {
        if (constraints.Axis == Axis.Vertical)
        {
            return new BoxConstraints(
                MinWidth: constraints.CrossAxisExtent,
                MaxWidth: constraints.CrossAxisExtent,
                MinHeight: 0,
                MaxHeight: double.PositiveInfinity);
        }

        return new BoxConstraints(
            MinWidth: 0,
            MaxWidth: double.PositiveInfinity,
            MinHeight: constraints.CrossAxisExtent,
            MaxHeight: constraints.CrossAxisExtent);
    }

    protected static double ChildMainAxisExtent(RenderBox child, Axis axis)
    {
        return axis == Axis.Vertical ? child.Size.Height : child.Size.Width;
    }

    /// <summary>
    /// The index of the given child, as given by <see cref="SliverMultiBoxAdaptorParentData.Index"/>.
    /// </summary>
    public int IndexOf(RenderBox child)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        Debug.Assert(childParentData.Index is not null);
        return childParentData.Index!.Value;
    }

    public override double? ChildScrollOffset(RenderObject child)
    {
        if (!ReferenceEquals(child.Parent, this))
        {
            throw new ArgumentException("The child does not belong to this sliver.", nameof(child));
        }

        return ((SliverMultiBoxAdaptorParentData)child.parentData!).LayoutOffset;
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return ChildScrollOffset(child)!.Value - ConstraintsForSliver.ScrollOffset;
    }

    /// <remarks>Flutter's <c>RenderSliverMultiBoxAdaptor._debugAssertChildListLocked</c>.</remarks>
    private bool DebugAssertChildListLocked() => _childManager?.DebugAssertChildListLocked() ?? true;

    /// <summary>Verifies that the child-list indices are in strictly increasing order.</summary>
    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor._debugVerifyChildOrder</c>; always returns true and
    /// has no effect in release builds.
    /// </remarks>
    private bool DebugVerifyChildOrder()
    {
        if (!_debugChildIntegrityEnabled)
        {
            return true;
        }

        RenderBox? child = FirstChild;
        while (child is not null)
        {
            int index = IndexOf(child);
            child = ChildAfter(child);
            Debug.Assert(child is null || IndexOf(child) > index);
        }

        return true;
    }

    /// <summary>
    /// Asserts that the reified child list is not empty and has a contiguous sequence of indices.
    /// </summary>
    /// <remarks>Flutter's <c>debugAssertChildListIsNonEmptyAndContiguous</c>; always returns true.</remarks>
    public bool DebugAssertChildListIsNonEmptyAndContiguous()
    {
        if (!Constants.KDebugMode)
        {
            return true;
        }

        Debug.Assert(FirstChild is not null);
        int index = IndexOf(FirstChild!);
        RenderBox? child = ChildAfter(FirstChild!);
        while (child is not null)
        {
            index += 1;
            Debug.Assert(IndexOf(child) == index);
            child = ChildAfter(child);
        }

        return true;
    }

    protected bool AddInitialChild(int index = 0, double layoutOffset = 0)
    {
        Debug.Assert(DebugAssertChildListLocked());
        Debug.Assert(FirstChild is null);
        CreateOrObtainChild(index, after: null);
        if (FirstChild is not null)
        {
            Debug.Assert(ReferenceEquals(FirstChild, LastChild));
            Debug.Assert(IndexOf(FirstChild) == index);
            var firstChildParentData = (SliverMultiBoxAdaptorParentData)FirstChild.parentData!;
            firstChildParentData.LayoutOffset = layoutOffset;
            return true;
        }

        _childManager?.SetDidUnderflow(true);
        return false;
    }

    protected RenderBox? InsertAndLayoutLeadingChild(BoxConstraints childConstraints, bool parentUsesSize = false)
    {
        Debug.Assert(DebugAssertChildListLocked());
        int index = IndexOf(FirstChild!) - 1;
        CreateOrObtainChild(index, after: null);
        if (FirstChild is not null && IndexOf(FirstChild) == index)
        {
            FirstChild.Layout(childConstraints, parentUsesSize: parentUsesSize);
            return FirstChild;
        }

        _childManager?.SetDidUnderflow(true);
        return null;
    }

    protected RenderBox? InsertAndLayoutChild(
        BoxConstraints childConstraints,
        RenderBox? after,
        bool parentUsesSize = false)
    {
        Debug.Assert(DebugAssertChildListLocked());
        Debug.Assert(after is not null);
        int index = IndexOf(after!) + 1;
        CreateOrObtainChild(index, after);
        RenderBox? child = ChildAfter(after!);
        if (child is not null && IndexOf(child) == index)
        {
            child.Layout(childConstraints, parentUsesSize: parentUsesSize);
            return child;
        }

        _childManager?.SetDidUnderflow(true);
        return null;
    }

    /// <summary>
    /// The number of children ahead of <paramref name="firstIndex"/> that can be garbage collected.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliverMultiBoxAdaptor.calculateLeadingGarbage</c>.</remarks>
    public int CalculateLeadingGarbage(int firstIndex)
    {
        RenderBox? walker = FirstChild;
        int leadingGarbage = 0;
        while (walker is not null && IndexOf(walker) < firstIndex)
        {
            leadingGarbage += 1;
            walker = ChildAfter(walker);
        }

        return leadingGarbage;
    }

    /// <summary>
    /// The number of children following <paramref name="lastIndex"/> that can be garbage collected.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliverMultiBoxAdaptor.calculateTrailingGarbage</c>.</remarks>
    public int CalculateTrailingGarbage(int lastIndex)
    {
        RenderBox? walker = LastChild;
        int trailingGarbage = 0;
        while (walker is not null && IndexOf(walker) > lastIndex)
        {
            trailingGarbage += 1;
            walker = ChildBefore(walker);
        }

        return trailingGarbage;
    }

    /// <summary>The main-axis extent the given child occupies once it has been laid out.</summary>
    /// <remarks>Flutter's <c>RenderSliverMultiBoxAdaptor.paintExtentOf</c>.</remarks>
    public virtual double PaintExtentOf(RenderBox child)
    {
        return ConstraintsForSliver.Axis == Axis.Horizontal ? child.Size.Width : child.Size.Height;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor.paintsChild</c>: a child that has been moved into
    /// the keep-alive bucket is still adopted by this sliver but is no longer painted.
    /// </remarks>
    public override bool PaintsChild(RenderObject child)
    {
        if (child.parentData is not SliverMultiBoxAdaptorParentData childParentData)
        {
            return false;
        }

        return childParentData.Index is not { } index || !_keepAliveBucket.ContainsKey(index);
    }

    protected void CollectGarbage(int leadingGarbage, int trailingGarbage)
    {
        Debug.Assert(DebugAssertChildListLocked());
        Debug.Assert(ChildCount >= leadingGarbage + trailingGarbage);
        Action<SliverConstraints> body =
            _ =>
            {
                while (leadingGarbage > 0)
                {
                    DestroyOrCacheChild(FirstChild!);
                    leadingGarbage -= 1;
                }

                while (trailingGarbage > 0)
                {
                    DestroyOrCacheChild(LastChild!);
                    trailingGarbage -= 1;
                }

                // Ask the child manager to remove the children that are no longer being kept alive.
                // This mutates the bucket, so the list has to be prepared ahead of time.
                foreach (RenderBox keepAliveChild in _keepAliveBucket.Values
                             .Where(static child =>
                                 !((SliverMultiBoxAdaptorParentData)child.parentData!).KeepAlive)
                             .ToArray())
                {
                    _childManager?.RemoveChild(keepAliveChild);
                }

                Debug.Assert(_keepAliveBucket.Values.All(static child =>
                    ((SliverMultiBoxAdaptorParentData)child.parentData!).KeepAlive));
            };
        body(ConstraintsForSliver);
    }

    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor._createOrObtainChild</c>: the whole body runs inside
    /// <c>invokeLayoutCallback</c>, because building or reviving a child dirties render objects while
    /// this sliver is laying itself out.
    /// </remarks>
    private void CreateOrObtainChild(int index, RenderBox? after)
    {
        InvokeLayoutCallback<SliverConstraints>(
            _ =>
            {
                if (index < 0)
                {
                    return;
                }

                if (_keepAliveBucket.TryGetValue(index, out RenderBox? keptAliveChild))
                {
                    _keepAliveBucket.Remove(index);
                    var parentData = (SliverMultiBoxAdaptorParentData)keptAliveChild.parentData!;
                    Debug.Assert(parentData.KeptAlive);

                    // A kept-alive child is still adopted by this sliver, so it has to be dropped
                    // before it can be inserted back into the child list; `DropChild` clears the
                    // parent data, which Dart hands straight back.
                    DropChild(keptAliveChild);
                    keptAliveChild.parentData = parentData;
                    Insert(keptAliveChild, after);
                    parentData.KeptAlive = false;
                    return;
                }

                _childManager?.CreateChild(index, after);
            },
            ConstraintsForSliver);
    }

    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor._destroyOrCacheChild</c>, likewise wrapped in
    /// <c>invokeLayoutCallback</c>.
    /// </remarks>
    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor._destroyOrCacheChild</c>. Its caller
    /// (<see cref="CollectGarbage"/>) already runs inside <c>invokeLayoutCallback</c>, so this does
    /// not open a second one.
    /// </remarks>
    private void DestroyOrCacheChild(RenderBox child)
    {
        InvokeLayoutCallback<SliverConstraints>(_ => DestroyOrCacheChildInner(child), ConstraintsForSliver);
    }

    private void DestroyOrCacheChildInner(RenderBox child)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (childParentData.KeepAlive)
        {
            Debug.Assert(!childParentData.KeptAlive);
            Remove(child);
            _keepAliveBucket[childParentData.Index!.Value] = child;

            // `DropChild` clears the parent data, so Dart hands the saved instance back before
            // re-adopting the child: the kept-alive child has to keep its index and flags.
            child.parentData = childParentData;
            base.AdoptChild(child);
            childParentData.KeptAlive = true;
            return;
        }

        Debug.Assert(ReferenceEquals(child.Parent, this));
        _childManager?.RemoveChild(child);
        Debug.Assert(child.Parent is null);
    }

    public void DefaultPaint(PaintingContext ctx, Point offset)
    {
        _container.DefaultPaint(ctx, offset);
    }

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(DiagnosticsNode.Message(
            FirstChild is not null
                ? $"currently live children: {IndexOf(FirstChild)} to {IndexOf(LastChild!)}"
                : "no children current live"));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        var children = new List<DiagnosticsNode>();
        if (FirstChild is not null)
        {
            RenderBox? child = FirstChild;
            while (true)
            {
                var childParentData = (SliverMultiBoxAdaptorParentData)child!.parentData!;
                children.Add(child.ToDiagnosticsNode(name: $"child with index {childParentData.Index}"));
                if (ReferenceEquals(child, LastChild))
                {
                    break;
                }

                child = childParentData.nextSibling;
            }
        }

        if (_keepAliveBucket.Count > 0)
        {
            List<int> indices = [.. _keepAliveBucket.Keys];
            indices.Sort();
            foreach (int index in indices)
            {
                children.Add(_keepAliveBucket[index].ToDiagnosticsNode(
                    name: $"child with index {index} (kept alive but not laid out)",
                    style: DiagnosticsTreeStyle.Offstage));
            }
        }

        return children;
    }
}

public sealed class RenderSliverList : RenderSliverMultiBoxAdaptor
{
    public RenderSliverList(IRenderSliverBoxChildManager? childManager = null) : base(childManager)
    {
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        IRenderSliverBoxChildManager? childManager = ChildManager;
        if (childManager is null)
        {
            Geometry = SliverGeometry.Zero;
            return;
        }

        childManager.DidStartLayout();
        childManager.SetDidUnderflow(false);

        double scrollOffset = constraints.ScrollOffset + constraints.CacheOrigin;
        Debug.Assert(scrollOffset >= 0.0);
        double remainingExtent = constraints.RemainingCacheExtent;
        Debug.Assert(remainingExtent >= 0.0);
        double targetEndScrollOffset = scrollOffset + remainingExtent;
        BoxConstraints childConstraints = ChildConstraintsForSliver(constraints);
        int leadingGarbage = 0;
        int trailingGarbage = 0;
        bool reachedEnd = false;

        // This algorithm in principle is straight-forward: find the first child that overlaps the
        // given scrollOffset, creating more children at the top of the list if necessary, then walk
        // down the list updating and laying out each child and adding more at the end if necessary
        // until we have enough children to cover the entire viewport.
        //
        // It is complicated by one minor issue, which is that any time you update or create a child,
        // it's possible that some of the children that haven't yet been laid out will be removed,
        // leaving the list in an inconsistent state, and requiring that missing nodes be recreated.
        //
        // To keep this mess tractable, this algorithm starts from what is currently the first child,
        // if any, and then walks up and/or down from there, so that the nodes that might get removed
        // are always at the edges of what has already been laid out.

        // Make sure we have at least one child to start from.
        if (FirstChild is null && !AddInitialChild())
        {
            // There are no children.
            Geometry = SliverGeometry.Zero;
            childManager.DidFinishLayout();
            return;
        }

        // We have at least one child.

        // These variables track the range of children that we have laid out. Within this range, the
        // children have consecutive indices. Outside this range, it's possible for a child to get
        // removed without notice.
        RenderBox? leadingChildWithLayout = null;
        RenderBox? trailingChildWithLayout = null;

        RenderBox? earliestUsefulChild = FirstChild;

        // A firstChild with null layout offset is likely a result of children reordering.
        //
        // We rely on firstChild to have an accurate layout offset. In the case of a null layout
        // offset, we have to find the first child that has a valid one.
        if (ChildScrollOffset(FirstChild!) is null)
        {
            int leadingChildrenWithoutLayoutOffset = 0;
            while (earliestUsefulChild is not null && ChildScrollOffset(earliestUsefulChild) is null)
            {
                earliestUsefulChild = ChildAfter(earliestUsefulChild);
                leadingChildrenWithoutLayoutOffset += 1;
            }

            // We should be able to destroy children with a null layout offset safely, because they
            // are likely outside of the viewport.
            CollectGarbage(leadingChildrenWithoutLayoutOffset, 0);

            // If we cannot find a valid layout offset, start from the initial child.
            if (FirstChild is null && !AddInitialChild())
            {
                // There are no children.
                Geometry = SliverGeometry.Zero;
                childManager.DidFinishLayout();
                return;
            }
        }

        // Find the last child that is at or before the scrollOffset.
        earliestUsefulChild = FirstChild;
        for (double earliestScrollOffset = ChildScrollOffset(earliestUsefulChild!)!.Value;
             earliestScrollOffset > scrollOffset;
             earliestScrollOffset = ChildScrollOffset(earliestUsefulChild!)!.Value)
        {
            // We have to add children before the earliestUsefulChild.
            earliestUsefulChild = InsertAndLayoutLeadingChild(childConstraints, parentUsesSize: true);
            if (earliestUsefulChild is null)
            {
                var firstChildParentData = (SliverMultiBoxAdaptorParentData)FirstChild!.parentData!;
                firstChildParentData.LayoutOffset = 0.0;

                if (scrollOffset == 0.0)
                {
                    // InsertAndLayoutLeadingChild only lays out the children before firstChild. In
                    // this case, nothing has been laid out, so firstChild is laid out by hand.
                    FirstChild.Layout(childConstraints, parentUsesSize: true);
                    earliestUsefulChild = FirstChild;
                    leadingChildWithLayout = earliestUsefulChild;
                    trailingChildWithLayout ??= earliestUsefulChild;
                    break;
                }

                // We ran out of children before reaching the scroll offset. We must inform our
                // parent that this sliver cannot fulfill its contract and that we need a scroll
                // offset correction.
                Geometry = new SliverGeometry(ScrollOffsetCorrection: -scrollOffset);
                return;
            }

            double firstChildScrollOffset = earliestScrollOffset - PaintExtentOf(FirstChild!);

            // firstChildScrollOffset may contain a double precision error.
            if (firstChildScrollOffset < -Constants.PrecisionErrorTolerance)
            {
                // Let's assume there is no child before the first child. We will correct it on the
                // next layout if it is not.
                Geometry = new SliverGeometry(ScrollOffsetCorrection: -firstChildScrollOffset);
                var firstChildParentData = (SliverMultiBoxAdaptorParentData)FirstChild!.parentData!;
                firstChildParentData.LayoutOffset = 0.0;
                return;
            }

            var childParentData = (SliverMultiBoxAdaptorParentData)earliestUsefulChild.parentData!;
            childParentData.LayoutOffset = firstChildScrollOffset;
            Debug.Assert(ReferenceEquals(earliestUsefulChild, FirstChild));
            leadingChildWithLayout = earliestUsefulChild;
            trailingChildWithLayout ??= earliestUsefulChild;
        }

        Debug.Assert(ChildScrollOffset(FirstChild!)!.Value > -Constants.PrecisionErrorTolerance);

        // If the scroll offset is at zero, we should make sure we are actually at the beginning of
        // the list.
        if (scrollOffset < Constants.PrecisionErrorTolerance)
        {
            // We iterate from the firstChild in case the leading child has a 0 paint extent.
            while (IndexOf(FirstChild!) > 0)
            {
                double earliestScrollOffset = ChildScrollOffset(FirstChild!)!.Value;

                // We correct one child at a time. If there are more children before the
                // earliestUsefulChild, we will correct it once the scroll offset reaches zero again.
                earliestUsefulChild = InsertAndLayoutLeadingChild(childConstraints, parentUsesSize: true);
                Debug.Assert(earliestUsefulChild is not null);
                double firstChildScrollOffset = earliestScrollOffset - PaintExtentOf(FirstChild!);
                var childParentData = (SliverMultiBoxAdaptorParentData)FirstChild!.parentData!;
                childParentData.LayoutOffset = 0.0;

                // We only need to correct if the leading child actually has a paint extent.
                if (firstChildScrollOffset < -Constants.PrecisionErrorTolerance)
                {
                    Geometry = new SliverGeometry(ScrollOffsetCorrection: -firstChildScrollOffset);
                    return;
                }
            }
        }

        // At this point, earliestUsefulChild is the first child, and is a child whose scrollOffset is
        // at or before the scrollOffset, and leadingChildWithLayout and trailingChildWithLayout are
        // either null or cover a range of render boxes that we have laid out with the first being the
        // same as earliestUsefulChild and the last being either at or after the scroll offset.
        Debug.Assert(ReferenceEquals(earliestUsefulChild, FirstChild));
        Debug.Assert(ChildScrollOffset(earliestUsefulChild!)!.Value <= scrollOffset);

        // Make sure we've laid out at least one child.
        if (leadingChildWithLayout is null)
        {
            earliestUsefulChild!.Layout(childConstraints, parentUsesSize: true);
            leadingChildWithLayout = earliestUsefulChild;
            trailingChildWithLayout = earliestUsefulChild;
        }

        // Here, earliestUsefulChild is still the first child, it's got a scrollOffset that is at or
        // before our actual scrollOffset, and it has been laid out, and is in fact our
        // leadingChildWithLayout. It's possible that some children beyond that one have also been
        // laid out.
        bool inLayoutRange = true;
        RenderBox? child = earliestUsefulChild;
        int index = IndexOf(child!);
        double endScrollOffset = ChildScrollOffset(child!)!.Value + PaintExtentOf(child!);

        // Returns true if we advanced, false if we have no more children. Used in two different
        // places below, to avoid code duplication.
        bool Advance()
        {
            Debug.Assert(child is not null);
            if (ReferenceEquals(child, trailingChildWithLayout))
            {
                inLayoutRange = false;
            }

            child = ChildAfter(child!);
            if (child is null)
            {
                inLayoutRange = false;
            }

            index += 1;
            if (!inLayoutRange)
            {
                if (child is null || IndexOf(child) != index)
                {
                    // We are missing a child. Insert it (and lay it out) if possible.
                    child = InsertAndLayoutChild(
                        childConstraints,
                        after: trailingChildWithLayout,
                        parentUsesSize: true);
                    if (child is null)
                    {
                        // We have run out of children.
                        return false;
                    }
                }
                else
                {
                    // Lay out the child.
                    child.Layout(childConstraints, parentUsesSize: true);
                }

                trailingChildWithLayout = child;
            }

            Debug.Assert(child is not null);
            var childParentData = (SliverMultiBoxAdaptorParentData)child!.parentData!;
            childParentData.LayoutOffset = endScrollOffset;
            Debug.Assert(childParentData.Index == index);
            endScrollOffset = ChildScrollOffset(child)!.Value + PaintExtentOf(child);
            return true;
        }

        // Find the first child that ends after the scroll offset.
        while (endScrollOffset < scrollOffset)
        {
            leadingGarbage += 1;
            if (!Advance())
            {
                Debug.Assert(leadingGarbage == ChildCount);
                Debug.Assert(child is null);

                // We want to make sure we keep the last child around so we know the end scroll offset.
                CollectGarbage(leadingGarbage - 1, 0);
                Debug.Assert(ReferenceEquals(FirstChild, LastChild));
                double lastExtent = ChildScrollOffset(LastChild!)!.Value + PaintExtentOf(LastChild!);
                PlaceChildren(constraints);
                Geometry = new SliverGeometry(ScrollExtent: lastExtent, MaxPaintExtent: lastExtent);
                return;
            }
        }

        // Now find the first child that ends after our end.
        while (endScrollOffset < targetEndScrollOffset)
        {
            if (!Advance())
            {
                reachedEnd = true;
                break;
            }
        }

        // Finally count up all the remaining children and label them as garbage.
        if (child is not null)
        {
            child = ChildAfter(child);
            while (child is not null)
            {
                trailingGarbage += 1;
                child = ChildAfter(child);
            }
        }

        // At this point everything should be good to go, we just have to clean up the garbage and
        // report the geometry.
        CollectGarbage(leadingGarbage, trailingGarbage);

        Debug.Assert(DebugAssertChildListIsNonEmptyAndContiguous());
        double estimatedMaxScrollOffset;
        if (reachedEnd)
        {
            estimatedMaxScrollOffset = endScrollOffset;
        }
        else
        {
            estimatedMaxScrollOffset = childManager.EstimateMaxScrollOffset(
                constraints,
                firstIndex: IndexOf(FirstChild!),
                lastIndex: IndexOf(LastChild!),
                leadingScrollOffset: ChildScrollOffset(FirstChild!),
                trailingScrollOffset: endScrollOffset);
            Debug.Assert(estimatedMaxScrollOffset
                >= endScrollOffset - ChildScrollOffset(FirstChild!)!.Value);
        }

        double paintExtent = CalculatePaintOffset(
            constraints,
            from: ChildScrollOffset(FirstChild!)!.Value,
            to: endScrollOffset);
        double cacheExtent = CalculateCacheOffset(
            constraints,
            from: ChildScrollOffset(FirstChild!)!.Value,
            to: endScrollOffset);
        double targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        PlaceChildren(constraints);
        Geometry = new SliverGeometry(
            ScrollExtent: estimatedMaxScrollOffset,
            PaintExtent: paintExtent,
            MaxPaintExtent: estimatedMaxScrollOffset,
            CacheExtent: cacheExtent,

            // Conservative to avoid flickering away the clip during scroll.
            HasVisualOverflow: endScrollOffset > targetEndScrollOffsetForPaint || constraints.ScrollOffset > 0.0);

        // We may have started the layout while scrolled to the end, which would not expose a new
        // child.
        if (estimatedMaxScrollOffset == endScrollOffset)
        {
            childManager.SetDidUnderflow(true);
        }

        childManager.DidFinishLayout();
    }

    /// <summary>
    /// Writes each laid-out child's paint offset from its layout offset.
    /// </summary>
    /// <remarks>
    /// Dart has no counterpart: its <c>RenderSliverMultiBoxAdaptor.paint</c> derives the paint offset
    /// from <c>childMainAxisPosition</c> instead of reading <c>parentData.offset</c>. Plumix paints
    /// adaptor children through the container defaults, so the offset is materialized here (see the
    /// adaptor-paint row in <c>docs/ai/BACKLOG.md</c>).
    /// </remarks>
    private void PlaceChildren(SliverConstraints constraints)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            if (childParentData.LayoutOffset is not { } layoutOffset)
            {
                continue;
            }

            childParentData.offset = constraints.Axis == Axis.Vertical
                ? new Point(0, layoutOffset - constraints.ScrollOffset)
                : new Point(layoutOffset - constraints.ScrollOffset, 0);
        }
    }
}

public sealed class RenderSliverGrid : RenderSliverMultiBoxAdaptor
{
    private SliverGridDelegate _gridDelegate;

    public RenderSliverGrid(SliverGridDelegate gridDelegate, IRenderSliverBoxChildManager? childManager = null) : base(childManager)
    {
        _gridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
    }

    public SliverGridDelegate GridDelegate
    {
        get => _gridDelegate;
        set
        {
            if (ReferenceEquals(_gridDelegate, value))
            {
                return;
            }

            bool shouldRelayout = value.GetType() != _gridDelegate.GetType() || value.ShouldRelayout(_gridDelegate);
            _gridDelegate = value;
            if (shouldRelayout)
            {
                MarkNeedsLayout();
            }
        }
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not SliverGridParentData)
        {
            child.parentData = new SliverGridParentData();
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        var childManager = ChildManager;
        if (childManager == null)
        {
            Geometry = default;
            return;
        }

        childManager.DidStartLayout();
        childManager.SetDidUnderflow(false);
        int? childCount = childManager.EstimatedChildCount;
        if (childCount == 0)
        {
            int activeChildCount = CountActiveChildren();
            if (activeChildCount > 0)
            {
                CollectGarbage(activeChildCount, 0);
            }

            Geometry = default;
            childManager.SetDidUnderflow(true);
            childManager.DidFinishLayout();
            return;
        }

        double remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        double scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        double targetEndScrollOffset = scrollOffset + Math.Max(0, remainingCacheExtent);
        var layout = _gridDelegate.GetLayout(constraints);

        int firstIndex = layout.GetMinChildIndexForScrollOffset(scrollOffset);
        bool hasFiniteTarget = !double.IsInfinity(targetEndScrollOffset);
        int targetLastIndex = hasFiniteTarget
            ? layout.GetMaxChildIndexForScrollOffset(targetEndScrollOffset)
            : int.MaxValue;

        if (childCount.HasValue)
        {
            if (childCount.Value <= 0)
            {
                Geometry = default;
                childManager.SetDidUnderflow(true);
                childManager.DidFinishLayout();
                return;
            }

            int maxIndex = childCount.Value - 1;
            firstIndex = Math.Clamp(firstIndex, 0, maxIndex);
            if (hasFiniteTarget)
            {
                targetLastIndex = Math.Clamp(targetLastIndex, 0, maxIndex);
                if (targetLastIndex < firstIndex)
                {
                    targetLastIndex = firstIndex;
                }
            }
        }

        var firstChildGeometry = layout.GetGeometryForChildIndex(firstIndex);
        if (FirstChild == null && !AddInitialChild(firstIndex, firstChildGeometry.ScrollOffset))
        {
            // There are either no children, or we are past the end of all our children.
            double max = layout.ComputeMaxScrollOffset(childManager.ChildCount);
            Geometry = new SliverGeometry(
                ScrollExtent: max,
                MaxPaintExtent: max);
            childManager.SetDidUnderflow(true);
            childManager.DidFinishLayout();
            return;
        }

        var firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            childManager.SetDidUnderflow(true);
            childManager.DidFinishLayout();
            return;
        }

        while (IndexOf(firstChild) > firstIndex)
        {
            int targetIndex = IndexOf(firstChild) - 1;
            var gridGeometry = layout.GetGeometryForChildIndex(targetIndex);
            var newLeadingChild = InsertAndLayoutLeadingChild(gridGeometry.GetBoxConstraints(constraints));
            if (newLeadingChild == null)
            {
                childManager.SetDidUnderflow(true);
                break;
            }

            var newLeadingParentData = (SliverGridParentData)newLeadingChild.parentData!;
            newLeadingParentData.Index = targetIndex;
            ApplyChildGeometry(newLeadingParentData, gridGeometry, constraints);
            firstChild = newLeadingChild;
        }

        int leadingGarbage = 0;
        int trailingGarbage = 0;
        var child = firstChild;
        int index = IndexOf(child);

        while (index < firstIndex)
        {
            leadingGarbage += 1;
            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != index + 1)
            {
                var nextGeometry = layout.GetGeometryForChildIndex(index + 1);
                nextChild = InsertAndLayoutChild(nextGeometry.GetBoxConstraints(constraints), child);
                if (nextChild == null)
                {
                    childManager.SetDidUnderflow(true);
                    break;
                }
            }

            child = nextChild;
            index += 1;
        }

        if (index != firstIndex)
        {
            firstIndex = index;
            if (hasFiniteTarget && targetLastIndex < firstIndex)
            {
                targetLastIndex = firstIndex;
            }
        }

        RenderBox? lastLaidOutChild = null;
        bool reachedEnd = false;
        double leadingScrollOffset = layout.GetGeometryForChildIndex(firstIndex).ScrollOffset;
        double trailingScrollOffset = leadingScrollOffset;

        while (child != null && (!hasFiniteTarget || index <= targetLastIndex))
        {
            var gridGeometry = layout.GetGeometryForChildIndex(index);
            child.Layout(gridGeometry.GetBoxConstraints(constraints), parentUsesSize: true);
            var childParentData = (SliverGridParentData)child.parentData!;
            childParentData.Index = index;
            ApplyChildGeometry(childParentData, gridGeometry, constraints);
            lastLaidOutChild = child;
            trailingScrollOffset = Math.Max(trailingScrollOffset, gridGeometry.TrailingScrollOffset);

            if (hasFiniteTarget && index == targetLastIndex)
            {
                child = ChildAfter(child);
                break;
            }

            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != index + 1)
            {
                var nextGeometry = layout.GetGeometryForChildIndex(index + 1);
                nextChild = InsertAndLayoutChild(nextGeometry.GetBoxConstraints(constraints), child);
                if (nextChild == null)
                {
                    reachedEnd = true;
                    childManager.SetDidUnderflow(true);
                    child = null;
                    break;
                }
            }

            child = nextChild;
            index += 1;
        }

        if (lastLaidOutChild == null)
        {
            Geometry = default;
            childManager.DidFinishLayout();
            return;
        }

        for (var trailingChild = child; trailingChild != null; trailingChild = ChildAfter(trailingChild))
        {
            trailingGarbage += 1;
        }

        CollectGarbage(leadingGarbage, trailingGarbage);

        int lastIndex = IndexOf(LastChild!);
        double estimatedMaxScrollOffset = reachedEnd
            ? trailingScrollOffset
            : childManager.EstimateMaxScrollOffset(
                constraints,
                firstIndex: firstIndex,
                lastIndex: lastIndex,
                leadingScrollOffset: leadingScrollOffset,
                trailingScrollOffset: trailingScrollOffset);

        double paintExtent = CalculatePaintExtent(
            from: Math.Min(constraints.ScrollOffset, leadingScrollOffset),
            to: trailingScrollOffset,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        double layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
        double cacheExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
            to: trailingScrollOffset,
            scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
            remainingPaintExtent: remainingCacheExtent);
        double targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        Geometry = new SliverGeometry(
            ScrollExtent: estimatedMaxScrollOffset,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: estimatedMaxScrollOffset,
            CacheExtent: cacheExtent,
            HasVisualOverflow: estimatedMaxScrollOffset > targetEndScrollOffsetForPaint || constraints.ScrollOffset > 0);

        if (Math.Abs(estimatedMaxScrollOffset - trailingScrollOffset) < 0.0001)
        {
            childManager.SetDidUnderflow(true);
        }

        childManager.DidFinishLayout();
    }

    public override double ChildCrossAxisPosition(RenderObject child)
    {
        return ((SliverGridParentData)child.parentData!).CrossAxisOffset;
    }

    private static void ApplyChildGeometry(
        SliverGridParentData parentData,
        SliverGridGeometry geometry,
        SliverConstraints constraints)
    {
        parentData.LayoutOffset = geometry.ScrollOffset;
        parentData.CrossAxisOffset = geometry.CrossAxisOffset;
        parentData.offset = constraints.Axis == Axis.Vertical
            ? new Point(geometry.CrossAxisOffset, geometry.ScrollOffset - constraints.ScrollOffset)
            : new Point(geometry.ScrollOffset - constraints.ScrollOffset, geometry.CrossAxisOffset);
    }

    private int CountActiveChildren()
    {
        int count = 0;
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            count += 1;
        }

        return count;
    }

    private static double CalculatePaintExtent(
        double from,
        double to,
        double scrollOffset,
        double remainingPaintExtent)
    {
        double visibleStart = Math.Max(from, scrollOffset);
        double visibleEnd = Math.Min(to, scrollOffset + remainingPaintExtent);
        return Math.Max(0, visibleEnd - visibleStart);
    }
}
