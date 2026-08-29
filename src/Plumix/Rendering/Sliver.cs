using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/sliver.dart (approximate)

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
    public string ToStringShort() => Diagnostics.ObjectRuntimeType(this);

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
public abstract class SliverVariableExtentLayout
{
    public abstract int GetMinChildIndexForScrollOffset(SliverConstraints constraints, double scrollOffset);

    public abstract int GetMaxChildIndexForScrollOffset(SliverConstraints constraints, double scrollOffset);

    public abstract double GetChildMainAxisExtent(SliverConstraints constraints, int index);

    public abstract double GetChildLayoutOffset(SliverConstraints constraints, int index);

    public abstract double ComputeMaxScrollOffset(SliverConstraints constraints, int? childCount);
}

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

public interface IRenderSliverBoxChildManager
{
    int? ChildCount { get; }
    bool CreateChild(int index, RenderBox? after);
    void RemoveChild(RenderBox child);
    void DidAdoptChild(RenderBox child);
    void SetDidUnderflow(bool value);
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
    public int Index { get; set; }
    public double LayoutOffset { get; set; }
    public bool KeepAlive { get; set; }
    public bool KeptAlive { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"index={Index}; {(KeepAlive ? "keepAlive; " : string.Empty)}{base.ToString()}";
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
        return oldLayer as OpacityOffsetLayer ?? new OpacityOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is OpacityOffsetLayer opacityLayer)
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
        return oldLayer as OpacityOffsetLayer ?? new OpacityOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is OpacityOffsetLayer opacityLayer)
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
    private IRenderSliverBoxChildManager? _childManager;

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

    public int ChildCount => _container.ChildCount;

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public void Insert(RenderBox child, RenderBox? after = null)
    {
        SetupParentData(child);
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        childParentData.KeptAlive = false;
        _container.Insert(child, after);
        _childManager?.DidAdoptChild(child);
    }

    public void Move(RenderBox child, RenderBox? after = null)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (!childParentData.KeptAlive)
        {
            _container.Move(child, after);
            _childManager?.DidAdoptChild(child);
            MarkNeedsLayout();
            return;
        }

        if (_keepAliveBucket.TryGetValue(childParentData.Index, out var cachedChild) && ReferenceEquals(cachedChild, child))
        {
            _keepAliveBucket.Remove(childParentData.Index);
        }

        _childManager?.DidAdoptChild(child);
        _keepAliveBucket[childParentData.Index] = child;
        MarkNeedsLayout();
    }

    public void Remove(RenderBox child)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (childParentData.KeptAlive)
        {
            if (_keepAliveBucket.TryGetValue(childParentData.Index, out var cachedChild) && ReferenceEquals(cachedChild, child))
            {
                _keepAliveBucket.Remove(childParentData.Index);
            }

            DropChild(child);
            childParentData.KeptAlive = false;
            return;
        }

        _container.Remove(child);
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

    public void RemoveAll() => _container.RemoveAll();

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

    protected int IndexOf(RenderBox child)
    {
        return ((SliverMultiBoxAdaptorParentData)child.parentData!).Index;
    }

    /// <summary>
    /// The typed sibling of <see cref="ChildScrollOffset(RenderObject)"/>. Every child of an adaptor
    /// sliver is laid out, so this never has to report a null offset.
    /// </summary>
    protected double ChildScrollOffset(RenderBox child)
    {
        return ((SliverMultiBoxAdaptorParentData)child.parentData!).LayoutOffset;
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

    protected bool AddInitialChild(int index = 0, double layoutOffset = 0)
    {
        if (FirstChild != null)
        {
            return true;
        }

        if (!CreateOrObtainChild(index, after: null) || FirstChild == null)
        {
            _childManager?.SetDidUnderflow(true);
            return false;
        }

        var firstChildParentData = (SliverMultiBoxAdaptorParentData)FirstChild.parentData!;
        firstChildParentData.LayoutOffset = layoutOffset;
        return true;
    }

    protected RenderBox? InsertAndLayoutLeadingChild(BoxConstraints childConstraints)
    {
        if (FirstChild == null)
        {
            return null;
        }

        int index = IndexOf(FirstChild) - 1;
        if (index < 0)
        {
            _childManager?.SetDidUnderflow(true);
            return null;
        }

        if (!CreateOrObtainChild(index, after: null) || FirstChild == null || IndexOf(FirstChild) != index)
        {
            _childManager?.SetDidUnderflow(true);
            return null;
        }

        FirstChild.Layout(childConstraints, parentUsesSize: true);
        return FirstChild;
    }

    protected RenderBox? InsertAndLayoutChild(BoxConstraints childConstraints, RenderBox after)
    {
        int index = IndexOf(after) + 1;
        if (!CreateOrObtainChild(index, after))
        {
            _childManager?.SetDidUnderflow(true);
            return null;
        }

        var child = ChildAfter(after);
        if (child == null || IndexOf(child) != index)
        {
            _childManager?.SetDidUnderflow(true);
            return null;
        }

        child.Layout(childConstraints, parentUsesSize: true);
        return child;
    }

    protected void CollectGarbage(int leadingGarbage, int trailingGarbage)
    {
        while (leadingGarbage > 0 && FirstChild != null)
        {
            DestroyOrCacheChild(FirstChild);
            leadingGarbage -= 1;
        }

        while (trailingGarbage > 0 && LastChild != null)
        {
            DestroyOrCacheChild(LastChild);
            trailingGarbage -= 1;
        }

        if (_childManager == null || _keepAliveBucket.Count == 0)
        {
            return;
        }

        foreach (var keepAliveChild in _keepAliveBucket.Values
                     .Where(child => !((SliverMultiBoxAdaptorParentData)child.parentData!).KeepAlive)
                     .ToArray())
        {
            _childManager.RemoveChild(keepAliveChild);
        }
    }

    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor._createOrObtainChild</c>: the whole body runs inside
    /// <c>invokeLayoutCallback</c>, because building or reviving a child dirties render objects while
    /// this sliver is laying itself out.
    /// </remarks>
    private bool CreateOrObtainChild(int index, RenderBox? after)
    {
        bool created = false;
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
                    created = true;
                    return;
                }

                created = _childManager?.CreateChild(index, after) ?? false;
            },
            ConstraintsForSliver);
        return created;
    }

    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor._destroyOrCacheChild</c>, likewise wrapped in
    /// <c>invokeLayoutCallback</c>.
    /// </remarks>
    private void DestroyOrCacheChild(RenderBox child)
    {
        InvokeLayoutCallback<SliverConstraints>(
            _ =>
            {
                var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
                if (childParentData.KeepAlive)
                {
                    Debug.Assert(!childParentData.KeptAlive);
                    Remove(child);
                    _keepAliveBucket[childParentData.Index] = child;

                    // `DropChild` clears the parent data, so Dart hands the saved instance back before
                    // re-adopting the child: the kept-alive child has to keep its index and flags.
                    child.parentData = childParentData;
                    AdoptChild(child);
                    childParentData.KeptAlive = true;
                    return;
                }

                _childManager?.RemoveChild(child);
            },
            ConstraintsForSliver);
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
        var childManager = ChildManager;
        if (childManager == null)
        {
            Geometry = default;
            return;
        }

        childManager.SetDidUnderflow(false);

        if (FirstChild == null && !AddInitialChild())
        {
            Geometry = default;
            return;
        }

        var firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            childManager.SetDidUnderflow(true);
            return;
        }

        var childConstraints = ChildConstraintsForSliver(constraints);
        double remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        double scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        double targetEndScrollOffset = scrollOffset + Math.Max(0, remainingCacheExtent);

        var earliestUsefulChild = firstChild;
        while (ChildScrollOffset(earliestUsefulChild) > scrollOffset)
        {
            var oldFirstChild = earliestUsefulChild;
            double oldFirstOffset = ChildScrollOffset(oldFirstChild);

            var newLeadingChild = InsertAndLayoutLeadingChild(childConstraints);
            if (newLeadingChild == null)
            {
                var anchorChild = FirstChild ?? earliestUsefulChild;
                if (IndexOf(anchorChild) == 0)
                {
                    double correction = -ChildScrollOffset(anchorChild);
                    if (Math.Abs(correction) > 0.0001)
                    {
                        Geometry = new SliverGeometry(ScrollOffsetCorrection: correction);
                        return;
                    }
                }

                break;
            }

            var newLeadingParentData = (SliverMultiBoxAdaptorParentData)newLeadingChild.parentData!;
            newLeadingParentData.LayoutOffset = oldFirstOffset - ChildMainAxisExtent(newLeadingChild, constraints.Axis);
            earliestUsefulChild = newLeadingChild;
        }

        earliestUsefulChild = FirstChild ?? earliestUsefulChild;
        earliestUsefulChild.Layout(childConstraints, parentUsesSize: true);
        var earliestUsefulParentData = (SliverMultiBoxAdaptorParentData)earliestUsefulChild.parentData!;
        earliestUsefulParentData.offset = constraints.Axis == Axis.Vertical
            ? new Point(0, earliestUsefulParentData.LayoutOffset - constraints.ScrollOffset)
            : new Point(earliestUsefulParentData.LayoutOffset - constraints.ScrollOffset, 0);

        int leadingGarbage = 0;
        int trailingGarbage = 0;
        bool reachedEnd = false;

        RenderBox? child = earliestUsefulChild;
        int index = IndexOf(child);
        double endScrollOffset = ChildScrollOffset(child) + ChildMainAxisExtent(child, constraints.Axis);

        bool Advance()
        {
            if (child == null)
            {
                return false;
            }

            var nextChild = ChildAfter(child);
            int nextIndex = index + 1;
            if (nextChild == null || IndexOf(nextChild) != nextIndex)
            {
                nextChild = InsertAndLayoutChild(childConstraints, child);
                if (nextChild == null)
                {
                    return false;
                }
            }
            else
            {
                nextChild.Layout(childConstraints, parentUsesSize: true);
            }

            var nextChildParentData = (SliverMultiBoxAdaptorParentData)nextChild.parentData!;
            nextChildParentData.Index = nextIndex;
            nextChildParentData.LayoutOffset = endScrollOffset;
            nextChildParentData.offset = constraints.Axis == Axis.Vertical
                ? new Point(0, nextChildParentData.LayoutOffset - constraints.ScrollOffset)
                : new Point(nextChildParentData.LayoutOffset - constraints.ScrollOffset, 0);

            child = nextChild;
            index = nextIndex;
            endScrollOffset = nextChildParentData.LayoutOffset + ChildMainAxisExtent(nextChild, constraints.Axis);
            return true;
        }

        while (endScrollOffset < scrollOffset)
        {
            leadingGarbage += 1;
            if (!Advance())
            {
                reachedEnd = true;
                if (leadingGarbage > 0)
                {
                    leadingGarbage -= 1;
                }

                break;
            }
        }

        if (!reachedEnd)
        {
            while (endScrollOffset < targetEndScrollOffset)
            {
                if (!Advance())
                {
                    reachedEnd = true;
                    break;
                }
            }
        }

        if (child != null)
        {
            for (var trailingChild = ChildAfter(child); trailingChild != null; trailingChild = ChildAfter(trailingChild))
            {
                trailingGarbage += 1;
            }
        }

        CollectGarbage(leadingGarbage, trailingGarbage);

        firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            return;
        }

        int firstIndex = IndexOf(firstChild);
        double leadingScrollOffset = ChildScrollOffset(firstChild);
        double estimatedMaxScrollOffset = reachedEnd
            ? endScrollOffset
            : EstimateMaxScrollOffset(
                firstIndex,
                index,
                leadingScrollOffset,
                endScrollOffset,
                childManager.ChildCount);

        double paintExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
            to: endScrollOffset,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        double layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
        double cacheExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
            to: endScrollOffset,
            scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
            remainingPaintExtent: remainingCacheExtent);
        double targetEndScrollOffsetForPaint = constraints.ScrollOffset + constraints.RemainingPaintExtent;

        Geometry = new SliverGeometry(
            ScrollExtent: estimatedMaxScrollOffset,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: estimatedMaxScrollOffset,
            CacheExtent: cacheExtent,
            HasVisualOverflow: endScrollOffset > targetEndScrollOffsetForPaint || constraints.ScrollOffset > 0);
    }

    private static double EstimateMaxScrollOffset(
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset,
        int? childCount)
    {
        if (!childCount.HasValue)
        {
            return double.PositiveInfinity;
        }

        if (lastIndex >= childCount.Value - 1)
        {
            return trailingScrollOffset;
        }

        int reifiedCount = Math.Max(1, lastIndex - firstIndex + 1);
        double averageExtent = (trailingScrollOffset - leadingScrollOffset) / reifiedCount;
        int remainingCount = Math.Max(0, childCount.Value - lastIndex - 1);
        return trailingScrollOffset + averageExtent * remainingCount;
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

        childManager.SetDidUnderflow(false);
        int? childCount = childManager.ChildCount;
        if (childCount == 0)
        {
            int activeChildCount = CountActiveChildren();
            if (activeChildCount > 0)
            {
                CollectGarbage(activeChildCount, 0);
            }

            Geometry = default;
            childManager.SetDidUnderflow(true);
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
            double max = childCount.HasValue
                ? layout.ComputeMaxScrollOffset(childCount.Value)
                : 0;
            Geometry = new SliverGeometry(
                ScrollExtent: max,
                MaxPaintExtent: max);
            childManager.SetDidUnderflow(true);
            return;
        }

        var firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            childManager.SetDidUnderflow(true);
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
            return;
        }

        for (var trailingChild = child; trailingChild != null; trailingChild = ChildAfter(trailingChild))
        {
            trailingGarbage += 1;
        }

        CollectGarbage(leadingGarbage, trailingGarbage);

        double estimatedMaxScrollOffset = childCount.HasValue
            ? layout.ComputeMaxScrollOffset(childCount.Value)
            : reachedEnd
                ? trailingScrollOffset
                : double.PositiveInfinity;

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

public class RenderSliverFixedExtentList : RenderSliverMultiBoxAdaptor
{
    private protected double _itemExtent;

    public RenderSliverFixedExtentList(double itemExtent, IRenderSliverBoxChildManager? childManager = null) : base(childManager)
    {
        _itemExtent = Math.Max(0, itemExtent);
    }

    public double ItemExtent
    {
        get => _itemExtent;
        set
        {
            double normalized = Math.Max(0, value);
            if (Math.Abs(_itemExtent - normalized) < 0.0001)
            {
                return;
            }

            _itemExtent = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        var childManager = ChildManager;
        double itemExtent = GetItemExtent(constraints);
        if (childManager == null || itemExtent <= 0)
        {
            Geometry = default;
            return;
        }

        childManager.SetDidUnderflow(false);

        int? childCount = childManager.ChildCount;
        if (childCount == 0)
        {
            int activeChildCount = CountActiveChildren();
            if (activeChildCount > 0)
            {
                CollectGarbage(activeChildCount, 0);
            }

            Geometry = default;
            childManager.SetDidUnderflow(true);
            return;
        }

        double remainingCacheExtent = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        double scrollOffset = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        double targetEndScrollOffset = scrollOffset + Math.Max(0, remainingCacheExtent);

        int firstIndex = GetMinChildIndexForScrollOffset(scrollOffset, itemExtent);
        int targetLastIndex = GetMaxChildIndexForScrollOffset(targetEndScrollOffset, itemExtent);
        if (childCount.HasValue)
        {
            int maxIndex = Math.Max(0, childCount.Value - 1);
            firstIndex = Math.Clamp(firstIndex, 0, maxIndex);
            targetLastIndex = Math.Clamp(targetLastIndex, 0, maxIndex);
            if (targetLastIndex < firstIndex)
            {
                targetLastIndex = firstIndex;
            }
        }

        var childConstraints = FixedExtentChildConstraints(constraints, itemExtent);
        if (FirstChild == null && !AddInitialChild(firstIndex, firstIndex * itemExtent))
        {
            Geometry = new SliverGeometry(
                ScrollExtent: childCount.HasValue ? childCount.Value * itemExtent : 0,
                MaxPaintExtent: childCount.HasValue ? childCount.Value * itemExtent : 0);
            childManager.SetDidUnderflow(true);
            return;
        }

        var firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            childManager.SetDidUnderflow(true);
            return;
        }

        while (IndexOf(firstChild) > firstIndex)
        {
            int targetIndex = IndexOf(firstChild) - 1;
            var newLeadingChild = InsertAndLayoutLeadingChild(childConstraints);
            if (newLeadingChild == null)
            {
                childManager.SetDidUnderflow(true);
                break;
            }

            var newLeadingParentData = (SliverMultiBoxAdaptorParentData)newLeadingChild.parentData!;
            newLeadingParentData.Index = targetIndex;
            newLeadingParentData.LayoutOffset = targetIndex * itemExtent;
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
                nextChild = InsertAndLayoutChild(childConstraints, child);
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
            targetLastIndex = Math.Max(targetLastIndex, firstIndex);
        }

        RenderBox? lastLaidOutChild = null;
        bool reachedEnd = false;

        while (child != null && index <= targetLastIndex)
        {
            var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            childParentData.Index = index;
            childParentData.LayoutOffset = index * itemExtent;
            child.Layout(childConstraints, parentUsesSize: true);
            childParentData.offset = constraints.Axis == Axis.Vertical
                ? new Point(0, childParentData.LayoutOffset - constraints.ScrollOffset)
                : new Point(childParentData.LayoutOffset - constraints.ScrollOffset, 0);

            lastLaidOutChild = child;

            if (index == targetLastIndex)
            {
                child = ChildAfter(child);
                break;
            }

            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != index + 1)
            {
                nextChild = InsertAndLayoutChild(childConstraints, child);
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
            return;
        }

        for (var trailingChild = child; trailingChild != null; trailingChild = ChildAfter(trailingChild))
        {
            trailingGarbage += 1;
        }

        CollectGarbage(leadingGarbage, trailingGarbage);

        double leadingScrollOffset = firstIndex * itemExtent;
        double trailingScrollOffset = (index + 1) * itemExtent;
        if (reachedEnd && childCount.HasValue)
        {
            trailingScrollOffset = Math.Min(trailingScrollOffset, childCount.Value * itemExtent);
        }

        double estimatedMaxScrollOffset = childCount.HasValue
            ? childCount.Value * itemExtent
            : reachedEnd
                ? trailingScrollOffset
                : double.PositiveInfinity;
        double paintExtent = CalculatePaintExtent(
            from: leadingScrollOffset,
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
            HasVisualOverflow: trailingScrollOffset > targetEndScrollOffsetForPaint || constraints.ScrollOffset > 0);
    }

    protected virtual double GetItemExtent(SliverConstraints constraints)
    {
        return _itemExtent;
    }

    protected static int GetMinChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        if (scrollOffset <= 0)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Floor(scrollOffset / itemExtent));
    }

    protected static int GetMaxChildIndexForScrollOffset(double scrollOffset, double itemExtent)
    {
        if (scrollOffset <= 0)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Ceiling(scrollOffset / itemExtent) - 1);
    }

    protected static BoxConstraints FixedExtentChildConstraints(SliverConstraints constraints, double itemExtent)
    {
        if (constraints.Axis == Axis.Vertical)
        {
            return new BoxConstraints(
                MinWidth: constraints.CrossAxisExtent,
                MaxWidth: constraints.CrossAxisExtent,
                MinHeight: itemExtent,
                MaxHeight: itemExtent);
        }

        return new BoxConstraints(
            MinWidth: itemExtent,
            MaxWidth: itemExtent,
            MinHeight: constraints.CrossAxisExtent,
            MaxHeight: constraints.CrossAxisExtent);
    }

    protected int CountActiveChildren()
    {
        int count = 0;
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            count += 1;
        }

        return count;
    }

    protected static double CalculatePaintExtent(
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

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_fixed_extent_list.dart
public class RenderSliverVariableExtentList : RenderSliverMultiBoxAdaptor
{
    private SliverVariableExtentLayout _layout;

    public RenderSliverVariableExtentList(SliverVariableExtentLayout layout, IRenderSliverBoxChildManager? childManager = null) : base(childManager)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public SliverVariableExtentLayout ExtentLayout
    {
        get => _layout;
        set
        {
            if (ReferenceEquals(_layout, value)) return;
            _layout = value ?? throw new ArgumentNullException(nameof(value));
            MarkNeedsLayout();
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        var manager = ChildManager;
        if (manager is null)
        {
            Geometry = default;
            return;
        }

        manager.SetDidUnderflow(false);
        int? count = manager.ChildCount;
        if (count == 0)
        {
            int activeChildCount = CountActiveChildren();
            if (activeChildCount > 0)
            {
                CollectGarbage(activeChildCount, 0);
            }

            Geometry = default;
            manager.SetDidUnderflow(true);
            return;
        }

        double cache = constraints.RemainingCacheExtent > 0
            ? constraints.RemainingCacheExtent
            : constraints.RemainingPaintExtent;
        double start = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        int first = Math.Max(0, ExtentLayout.GetMinChildIndexForScrollOffset(constraints, start));
        int last = Math.Max(first, ExtentLayout.GetMaxChildIndexForScrollOffset(constraints, start + cache));
        if (count.HasValue)
        {
            int max = Math.Max(0, count.Value - 1);
            first = Math.Min(first, max);
            last = Math.Min(last, max);
        }

        if (!AddInitialChild(first, ExtentLayout.GetChildLayoutOffset(constraints, first)))
        {
            double max = ExtentLayout.ComputeMaxScrollOffset(constraints, count);
            Geometry = new SliverGeometry(ScrollExtent: max, MaxPaintExtent: max);
            manager.SetDidUnderflow(true);
            return;
        }

        var firstChild = FirstChild;
        if (firstChild == null)
        {
            Geometry = default;
            manager.SetDidUnderflow(true);
            return;
        }

        while (IndexOf(firstChild) > first)
        {
            int targetIndex = IndexOf(firstChild) - 1;
            var newLeadingChild = InsertAndLayoutLeadingChild(ChildConstraints(constraints, targetIndex));
            if (newLeadingChild == null)
            {
                manager.SetDidUnderflow(true);
                break;
            }

            var data = (SliverMultiBoxAdaptorParentData)newLeadingChild.parentData!;
            data.Index = targetIndex;
            ApplyChildGeometry(data, constraints, targetIndex);
            firstChild = newLeadingChild;
        }

        int leadingGarbage = 0;
        int trailingGarbage = 0;
        var child = firstChild;
        int index = IndexOf(child);

        while (index < first)
        {
            leadingGarbage += 1;
            int nextIndex = index + 1;
            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != nextIndex)
            {
                nextChild = InsertAndLayoutChild(ChildConstraints(constraints, nextIndex), child);
                if (nextChild == null)
                {
                    manager.SetDidUnderflow(true);
                    break;
                }
            }

            child = nextChild;
            index = nextIndex;
        }

        if (index != first)
        {
            first = index;
            last = Math.Max(last, first);
        }

        RenderBox? lastLaidOutChild = null;
        bool reachedEnd = false;
        double leading = ExtentLayout.GetChildLayoutOffset(constraints, first);
        double trailing = leading;
        while (child is not null && index <= last)
        {
            child.Layout(ChildConstraints(constraints, index), parentUsesSize: true);
            var data = (SliverMultiBoxAdaptorParentData)child.parentData!;
            data.Index = index;
            ApplyChildGeometry(data, constraints, index);
            lastLaidOutChild = child;
            trailing = data.LayoutOffset + ExtentLayout.GetChildMainAxisExtent(constraints, index);

            if (index == last)
            {
                child = ChildAfter(child);
                break;
            }

            int nextIndex = index + 1;
            var nextChild = ChildAfter(child);
            if (nextChild == null || IndexOf(nextChild) != nextIndex)
            {
                nextChild = InsertAndLayoutChild(ChildConstraints(constraints, nextIndex), child);
                if (nextChild == null)
                {
                    reachedEnd = true;
                    manager.SetDidUnderflow(true);
                    child = null;
                    break;
                }
            }

            child = nextChild;
            index = nextIndex;
        }

        if (lastLaidOutChild == null)
        {
            Geometry = default;
            return;
        }

        for (var trailingChild = child; trailingChild != null; trailingChild = ChildAfter(trailingChild))
        {
            trailingGarbage += 1;
        }

        CollectGarbage(leadingGarbage, trailingGarbage);

        double maxExtent = ExtentLayout.ComputeMaxScrollOffset(constraints, count);
        if (double.IsPositiveInfinity(maxExtent) && reachedEnd)
        {
            maxExtent = trailing;
        }

        double paint = CalculatePaintExtent(
            from: leading,
            to: trailing,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        double cacheExtent = CalculatePaintExtent(
            from: leading,
            to: trailing,
            scrollOffset: constraints.ScrollOffset + constraints.CacheOrigin,
            remainingPaintExtent: cache);
        Geometry = new SliverGeometry(
            ScrollExtent: maxExtent,
            PaintExtent: paint,
            LayoutExtent: Math.Min(paint, constraints.ViewportMainAxisExtent),
            MaxPaintExtent: maxExtent,
            CacheExtent: cacheExtent,
            HasVisualOverflow: trailing > constraints.ScrollOffset + constraints.RemainingPaintExtent
                || constraints.ScrollOffset > 0);

        if (Math.Abs(maxExtent - trailing) < 0.0001)
        {
            manager.SetDidUnderflow(true);
        }
    }

    private BoxConstraints ChildConstraints(SliverConstraints constraints, int index)
    {
        double extent = ExtentLayout.GetChildMainAxisExtent(constraints, index);
        if (!double.IsFinite(extent) || extent < 0)
        {
            throw new InvalidOperationException("A variable sliver item extent must be finite and non-negative.");
        }

        return constraints.AsBoxConstraints(minExtent: extent, maxExtent: extent);
    }

    private void ApplyChildGeometry(
        SliverMultiBoxAdaptorParentData data,
        SliverConstraints constraints,
        int index)
    {
        data.LayoutOffset = ExtentLayout.GetChildLayoutOffset(constraints, index);
        data.offset = constraints.Axis == Axis.Vertical
            ? new Point(0, data.LayoutOffset - constraints.ScrollOffset)
            : new Point(data.LayoutOffset - constraints.ScrollOffset, 0);
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

/// <summary>Flutter-shaped varied-extent sliver render object.</summary>
public sealed class RenderSliverVariedExtentList : RenderSliverVariableExtentList
{
    private ItemExtentBuilder _itemExtentBuilder;

    public RenderSliverVariedExtentList(
        ItemExtentBuilder itemExtentBuilder,
        IRenderSliverBoxChildManager? childManager = null)
        : base(new ItemExtentBuilderSliverLayout(itemExtentBuilder), childManager)
    {
        _itemExtentBuilder = itemExtentBuilder ?? throw new ArgumentNullException(nameof(itemExtentBuilder));
    }

    public ItemExtentBuilder ItemExtentBuilder
    {
        get => _itemExtentBuilder;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_itemExtentBuilder, value))
            {
                return;
            }

            _itemExtentBuilder = value;
            ExtentLayout = new ItemExtentBuilderSliverLayout(value);
        }
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        ((ItemExtentBuilderSliverLayout)ExtentLayout).ChildCount = ChildManager?.ChildCount;
        base.PerformSliverLayout(constraints);
    }
}

internal sealed class ItemExtentBuilderSliverLayout : SliverVariableExtentLayout
{
    private readonly ItemExtentBuilder _builder;
    private readonly List<double?> _cachedExtents = [];
    private readonly List<double> _cachedOffsets = [0];
    private SliverLayoutDimensions? _cachedDimensions;

    public ItemExtentBuilderSliverLayout(ItemExtentBuilder builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public int? ChildCount { get; set; }

    public override int GetMinChildIndexForScrollOffset(SliverConstraints constraints, double scrollOffset)
    {
        double position = 0;
        int index = 0;
        while (position < scrollOffset)
        {
            if (ChildCount.HasValue && index >= ChildCount.Value)
            {
                break;
            }

            double? extent = ExtentAt(constraints, index);
            if (!extent.HasValue)
            {
                break;
            }

            if (position + extent.Value > scrollOffset)
            {
                break;
            }

            position += extent.Value;
            index += 1;
        }

        return index;
    }

    public override int GetMaxChildIndexForScrollOffset(SliverConstraints constraints, double scrollOffset)
    {
        double position = 0;
        int index = 0;
        while (position < scrollOffset)
        {
            if (ChildCount.HasValue && index >= ChildCount.Value)
            {
                return Math.Max(0, index - 1);
            }

            double? extent = ExtentAt(constraints, index);
            if (!extent.HasValue)
            {
                return Math.Max(0, index - 1);
            }

            position += extent.Value;
            index += 1;
        }

        return Math.Max(0, index - 1);
    }

    public override double GetChildMainAxisExtent(SliverConstraints constraints, int index)
    {
        return ExtentAt(constraints, index)
            ?? throw new InvalidOperationException($"itemExtentBuilder returned null for child index {index}.");
    }

    public override double GetChildLayoutOffset(SliverConstraints constraints, int index)
    {
        PrepareCache(constraints);
        int cappedIndex = ChildCount.HasValue ? Math.Min(index, ChildCount.Value) : index;
        for (int childIndex = _cachedExtents.Count; childIndex < cappedIndex; childIndex++)
        {
            double? extent = ExtentAt(constraints, childIndex);
            if (!extent.HasValue)
            {
                break;
            }
        }

        return _cachedOffsets[Math.Min(cappedIndex, _cachedOffsets.Count - 1)];
    }

    public override double ComputeMaxScrollOffset(SliverConstraints constraints, int? childCount)
    {
        if (!childCount.HasValue)
        {
            return double.PositiveInfinity;
        }

        return GetChildLayoutOffset(constraints, childCount.Value);
    }

    private double? ExtentAt(SliverConstraints constraints, int index)
    {
        PrepareCache(constraints);
        if (index < _cachedExtents.Count)
        {
            return _cachedExtents[index];
        }

        if (_cachedExtents.Count > 0 && !_cachedExtents[^1].HasValue)
        {
            return null;
        }

        while (_cachedExtents.Count <= index)
        {
            int nextIndex = _cachedExtents.Count;
            double? extent = _builder(nextIndex, _cachedDimensions!.Value);
            if (extent.HasValue && (!double.IsFinite(extent.Value) || extent.Value < 0))
            {
                throw new InvalidOperationException(
                    "itemExtentBuilder must return null or a finite non-negative value.");
            }

            _cachedExtents.Add(extent);
            _cachedOffsets.Add(_cachedOffsets[^1] + (extent ?? 0));
            if (!extent.HasValue)
            {
                break;
            }
        }

        return index < _cachedExtents.Count ? _cachedExtents[index] : null;
    }

    private void PrepareCache(SliverConstraints constraints)
    {
        var dimensions = new SliverLayoutDimensions(
            ScrollOffset: constraints.ScrollOffset,
            PrecedingScrollExtent: constraints.PrecedingScrollExtent,
            ViewportMainAxisExtent: constraints.ViewportMainAxisExtent,
            CrossAxisExtent: constraints.CrossAxisExtent);
        if (_cachedDimensions == dimensions)
        {
            return;
        }

        _cachedDimensions = dimensions;
        _cachedExtents.Clear();
        _cachedOffsets.Clear();
        _cachedOffsets.Add(0);
    }
}

/// <summary>Measures an offstage prototype and uses its extent for all list children.</summary>
public sealed class RenderSliverPrototypeExtentList : RenderSliverFixedExtentList
{
    private RenderBox? _prototypeChild;

    public RenderSliverPrototypeExtentList(
        RenderBox? prototypeChild = null,
        IRenderSliverBoxChildManager? childManager = null) : base(0, childManager)
    {
        PrototypeChild = prototypeChild;
    }

    public RenderBox? PrototypeChild
    {
        get => _prototypeChild;
        set
        {
            if (ReferenceEquals(_prototypeChild, value))
            {
                return;
            }

            if (_prototypeChild != null)
            {
                DropChild(_prototypeChild);
            }

            _prototypeChild = value;
            if (_prototypeChild != null)
            {
                AdoptChild(_prototypeChild);
            }

            MarkNeedsLayout();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_prototypeChild != null)
        {
            visitor(_prototypeChild);
        }

        base.VisitChildren(visitor);
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (_prototypeChild == null)
        {
            Geometry = default;
            return;
        }

        _prototypeChild.Layout(constraints.AsBoxConstraints(), parentUsesSize: true);
        base.PerformSliverLayout(constraints);
    }

    protected override double GetItemExtent(SliverConstraints constraints)
    {
        if (_prototypeChild == null)
        {
            return 0;
        }

        return constraints.Axis == Axis.Vertical
            ? _prototypeChild.Size.Height
            : _prototypeChild.Size.Width;
    }
}
