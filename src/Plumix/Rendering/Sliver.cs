using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver.dart
// flutter/packages/flutter/lib/src/rendering/sliver_multi_box_adaptor.dart
// flutter/packages/flutter/lib/src/rendering/proxy_sliver.dart
// flutter/packages/flutter/lib/src/rendering/sliver_padding.dart

namespace Plumix.Rendering;

/// <summary>Immutable layout constraints for <see cref="RenderSliver"/> layout.</summary>
/// <remarks>
/// Flutter's <c>SliverConstraints</c>. Every field is required, as in Dart, and
/// <see cref="Axis"/> is derived from <see cref="AxisDirection"/>.
/// </remarks>
public readonly record struct SliverConstraints : IConstraints
{
    /// <summary>Creates sliver constraints with the given information.</summary>
    public SliverConstraints(
        AxisDirection AxisDirection,
        GrowthDirection GrowthDirection,
        ScrollDirection UserScrollDirection,
        double ScrollOffset,
        double PrecedingScrollExtent,
        double Overlap,
        double RemainingPaintExtent,
        double CrossAxisExtent,
        AxisDirection CrossAxisDirection,
        double ViewportMainAxisExtent,
        double RemainingCacheExtent,
        double CacheOrigin)
    {
        this.AxisDirection = AxisDirection;
        this.GrowthDirection = GrowthDirection;
        this.UserScrollDirection = UserScrollDirection;
        this.ScrollOffset = ScrollOffset;
        this.PrecedingScrollExtent = PrecedingScrollExtent;
        this.Overlap = Overlap;
        this.RemainingPaintExtent = RemainingPaintExtent;
        this.CrossAxisExtent = CrossAxisExtent;
        this.CrossAxisDirection = CrossAxisDirection;
        this.ViewportMainAxisExtent = ViewportMainAxisExtent;
        this.RemainingCacheExtent = RemainingCacheExtent;
        this.CacheOrigin = CacheOrigin;
    }

    /// <summary>
    /// The direction in which the <see cref="ScrollOffset"/> and <see cref="RemainingPaintExtent"/> increase.
    /// </summary>
    public AxisDirection AxisDirection { get; init; }

    /// <summary>
    /// The direction in which the contents of slivers are ordered, relative to the <see cref="AxisDirection"/>.
    /// </summary>
    public GrowthDirection GrowthDirection { get; init; }

    /// <summary>
    /// The direction in which the user is attempting to scroll, relative to the <see cref="AxisDirection"/>.
    /// </summary>
    public ScrollDirection UserScrollDirection { get; init; }

    /// <summary>
    /// The scroll offset, in this sliver's coordinate system, that corresponds to the earliest visible part of this
    /// sliver.
    /// </summary>
    public double ScrollOffset { get; init; }

    /// <summary>The scroll distance that has been consumed by all slivers that came before this sliver.</summary>
    public double PrecedingScrollExtent { get; init; }

    /// <summary>
    /// The number of pixels from where the pixels corresponding to the <see cref="ScrollOffset"/> will be painted up to
    /// the first pixel that has not yet been painted on by an earlier sliver.
    /// </summary>
    public double Overlap { get; init; }

    /// <summary>The number of pixels of content that the sliver should consider providing.</summary>
    public double RemainingPaintExtent { get; init; }

    /// <summary>The number of pixels in the cross-axis.</summary>
    public double CrossAxisExtent { get; init; }

    /// <summary>The direction in which children should be placed in the cross axis.</summary>
    public AxisDirection CrossAxisDirection { get; init; }

    /// <summary>The number of pixels the viewport can display in the main axis.</summary>
    public double ViewportMainAxisExtent { get; init; }

    /// <summary>Where the cache area starts relative to the <see cref="ScrollOffset"/>.</summary>
    public double CacheOrigin { get; init; }

    /// <summary>
    /// Describes how much content the sliver should provide starting from the <see cref="CacheOrigin"/>.
    /// </summary>
    public double RemainingCacheExtent { get; init; }

    /// <summary>
    /// The axis along which the <see cref="ScrollOffset"/> and <see cref="RemainingPaintExtent"/> are measured.
    /// </summary>
    public Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);

    /// <summary>
    /// Return what the <see cref="GrowthDirection"/> would be if the <see cref="AxisDirection"/> was
    /// either <see cref="AxisDirection.Down"/> or <see cref="AxisDirection.Right"/>.
    /// </summary>
    /// <remarks>Flutter's <c>SliverConstraints.normalizedGrowthDirection</c>.</remarks>
    public GrowthDirection NormalizedGrowthDirection =>
        ScrollDirectionUtils.AxisDirectionIsReversed(AxisDirection)
            ? GrowthDirection == GrowthDirection.Forward ? GrowthDirection.Reverse : GrowthDirection.Forward
            : GrowthDirection;

    /// <inheritdoc />
    public bool IsTight => false;

    /// <inheritdoc />
    public bool IsNormalized => ScrollOffset >= 0.0
                                && CrossAxisExtent >= 0.0
                                && ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection)
                                != ScrollDirectionUtils.AxisDirectionToAxis(CrossAxisDirection)
                                && ViewportMainAxisExtent >= 0.0
                                && RemainingPaintExtent >= 0.0;

    /// <summary>Creates a copy of this object but with the given fields replaced with the new values.</summary>
    /// <remarks>Flutter's <c>SliverConstraints.copyWith</c>; C#'s <c>with</c> expression does the same.</remarks>
    public SliverConstraints CopyWith(
        AxisDirection? axisDirection = null,
        GrowthDirection? growthDirection = null,
        ScrollDirection? userScrollDirection = null,
        double? scrollOffset = null,
        double? precedingScrollExtent = null,
        double? overlap = null,
        double? remainingPaintExtent = null,
        double? crossAxisExtent = null,
        AxisDirection? crossAxisDirection = null,
        double? viewportMainAxisExtent = null,
        double? remainingCacheExtent = null,
        double? cacheOrigin = null)
    {
        return new SliverConstraints(
            AxisDirection: axisDirection ?? AxisDirection,
            GrowthDirection: growthDirection ?? GrowthDirection,
            UserScrollDirection: userScrollDirection ?? UserScrollDirection,
            ScrollOffset: scrollOffset ?? ScrollOffset,
            PrecedingScrollExtent: precedingScrollExtent ?? PrecedingScrollExtent,
            Overlap: overlap ?? Overlap,
            RemainingPaintExtent: remainingPaintExtent ?? RemainingPaintExtent,
            CrossAxisExtent: crossAxisExtent ?? CrossAxisExtent,
            CrossAxisDirection: crossAxisDirection ?? CrossAxisDirection,
            ViewportMainAxisExtent: viewportMainAxisExtent ?? ViewportMainAxisExtent,
            RemainingCacheExtent: remainingCacheExtent ?? RemainingCacheExtent,
            CacheOrigin: cacheOrigin ?? CacheOrigin);
    }

    /// <summary>
    /// Returns <see cref="BoxConstraints"/> that reflects the sliver constraints.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SliverConstraints.asBoxConstraints</c>: the cross-axis extent is tight, the main
    /// axis extent ranges from <paramref name="minExtent"/> to <paramref name="maxExtent"/>.
    /// </remarks>
    public BoxConstraints AsBoxConstraints(
        double minExtent = 0.0,
        double maxExtent = double.PositiveInfinity,
        double? crossAxisExtent = null)
    {
        double effectiveCrossAxisExtent = crossAxisExtent ?? CrossAxisExtent;
        return Axis == Axis.Horizontal
            ? new BoxConstraints(
                MinWidth: minExtent,
                MaxWidth: maxExtent,
                MinHeight: effectiveCrossAxisExtent,
                MaxHeight: effectiveCrossAxisExtent)
            : new BoxConstraints(
                MinWidth: effectiveCrossAxisExtent,
                MaxWidth: effectiveCrossAxisExtent,
                MinHeight: minExtent,
                MaxHeight: maxExtent);
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>SliverConstraints.debugAssertIsValid</c>.</remarks>
    public bool DebugAssertIsValid(
        bool isAppliedConstraint = false,
        InformationCollector? informationCollector = null)
    {
        if (!Constants.KDebugMode)
        {
            return true;
        }

        bool hasErrors = false;
        var errorMessage = new StringBuilder("\n");
        void Verify(bool check, string message)
        {
            if (check)
            {
                return;
            }

            hasErrors = true;
            errorMessage.Append("  ").Append(message).Append('\n');
        }

        void VerifyDouble(double property, string name, bool mustBePositive = false, bool mustBeNegative = false)
        {
            if (double.IsNaN(property))
            {
                string additional = ".";
                if (mustBePositive)
                {
                    additional = ", expected greater than or equal to zero.";
                }
                else if (mustBeNegative)
                {
                    additional = ", expected less than or equal to zero.";
                }

                Verify(false, $"The \"{name}\" is NaN{additional}");
            }
            else if (mustBePositive)
            {
                Verify(property >= 0.0, $"The \"{name}\" is negative.");
            }
            else if (mustBeNegative)
            {
                Verify(property <= 0.0, $"The \"{name}\" is positive.");
            }
        }

        VerifyDouble(ScrollOffset, "scrollOffset");
        VerifyDouble(Overlap, "overlap");
        VerifyDouble(CrossAxisExtent, "crossAxisExtent");
        VerifyDouble(ScrollOffset, "scrollOffset", mustBePositive: true);
        Verify(
            ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection)
            != ScrollDirectionUtils.AxisDirectionToAxis(CrossAxisDirection),
            "The \"axisDirection\" and the \"crossAxisDirection\" are along the same axis.");
        VerifyDouble(ViewportMainAxisExtent, "viewportMainAxisExtent", mustBePositive: true);
        VerifyDouble(RemainingPaintExtent, "remainingPaintExtent", mustBePositive: true);
        VerifyDouble(RemainingCacheExtent, "remainingCacheExtent", mustBePositive: true);
        VerifyDouble(CacheOrigin, "cacheOrigin", mustBeNegative: true);
        VerifyDouble(PrecedingScrollExtent, "precedingScrollExtent", mustBePositive: true);
        // Should be redundant with the earlier checks.
        Verify(IsNormalized, "The constraints are not normalized.");
        if (hasErrors)
        {
            throw new FlutterError(
            [
                new ErrorSummary($"{nameof(SliverConstraints)} is not valid: {errorMessage}"),
                .. informationCollector?.Invoke() ?? [],
                new DiagnosticsProperty<SliverConstraints>(
                    "The offending constraints were",
                    this,
                    style: DiagnosticsTreeStyle.ErrorProperty),
            ]);
        }

        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>SliverConstraints.==</c>, which also asserts that the other constraints are valid.
    /// </remarks>
    public bool Equals(SliverConstraints other)
    {
        if (Constants.KDebugMode)
        {
            other.DebugAssertIsValid();
        }

        return other.AxisDirection == AxisDirection
               && other.GrowthDirection == GrowthDirection
               && other.UserScrollDirection == UserScrollDirection
               && other.ScrollOffset.Equals(ScrollOffset)
               && other.PrecedingScrollExtent.Equals(PrecedingScrollExtent)
               && other.Overlap.Equals(Overlap)
               && other.RemainingPaintExtent.Equals(RemainingPaintExtent)
               && other.CrossAxisExtent.Equals(CrossAxisExtent)
               && other.CrossAxisDirection == CrossAxisDirection
               && other.ViewportMainAxisExtent.Equals(ViewportMainAxisExtent)
               && other.RemainingCacheExtent.Equals(RemainingCacheExtent)
               && other.CacheOrigin.Equals(CacheOrigin);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AxisDirection);
        hash.Add(GrowthDirection);
        hash.Add(UserScrollDirection);
        hash.Add(ScrollOffset);
        hash.Add(PrecedingScrollExtent);
        hash.Add(Overlap);
        hash.Add(RemainingPaintExtent);
        hash.Add(CrossAxisExtent);
        hash.Add(CrossAxisDirection);
        hash.Add(ViewportMainAxisExtent);
        hash.Add(RemainingCacheExtent);
        hash.Add(CacheOrigin);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>SliverConstraints.toString</c>.</remarks>
    public override string ToString()
    {
        List<string> properties =
        [
            DartEnum(nameof(AxisDirection), AxisDirection.ToString()),
            DartEnum(nameof(GrowthDirection), GrowthDirection.ToString()),
            DartEnum(nameof(ScrollDirection), UserScrollDirection.ToString()),
            $"scrollOffset: {Fixed(ScrollOffset)}",
            $"precedingScrollExtent: {Fixed(PrecedingScrollExtent)}",
            $"remainingPaintExtent: {Fixed(RemainingPaintExtent)}",
        ];
        if (Overlap != 0.0)
        {
            properties.Add($"overlap: {Fixed(Overlap)}");
        }

        properties.Add($"crossAxisExtent: {Fixed(CrossAxisExtent)}");
        properties.Add($"crossAxisDirection: {DartEnum(nameof(AxisDirection), CrossAxisDirection.ToString())}");
        properties.Add($"viewportMainAxisExtent: {Fixed(ViewportMainAxisExtent)}");
        properties.Add($"remainingCacheExtent: {Fixed(RemainingCacheExtent)}");
        properties.Add($"cacheOrigin: {Fixed(CacheOrigin)}");
        return $"SliverConstraints({string.Join(", ", properties)})";
    }

    private static string Fixed(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    private static string DartEnum(string type, string value) =>
        $"{type}.{char.ToLowerInvariant(value[0])}{value[1..]}";
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
                DebugCompareFloats("paintExtent", PaintExtent, "layoutExtent", LayoutExtent));
        }
        if (PaintExtent - MaxPaintExtent > Constants.PrecisionErrorTolerance)
        {
            ThrowInvalid(
                new ErrorSummary("The \"maxPaintExtent\" is less than the \"paintExtent\"."),
                informationCollector,
                [
                    .. DebugCompareFloats("maxPaintExtent", MaxPaintExtent, "paintExtent", PaintExtent),
                    new ErrorDescription(
                        "By definition, a sliver can't paint more than the maximum that it can paint!"),
                ]);
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

    /// <remarks>Flutter's <c>_debugCompareFloats</c> (sliver.dart).</remarks>
    internal static DiagnosticsNode[] DebugCompareFloats(
        string labelA,
        double valueA,
        string labelB,
        double valueB)
    {
        string roundedA = valueA.ToString("F1", CultureInfo.InvariantCulture);
        string roundedB = valueB.ToString("F1", CultureInfo.InvariantCulture);
        if (!string.Equals(roundedA, roundedB, StringComparison.Ordinal))
        {
            return
            [
                new ErrorDescription($"The {labelA} is {roundedA}, but the {labelB} is {roundedB}."),
            ];
        }

        return
        [
            new ErrorDescription(
                $"The {labelA} is {DartDouble(valueA)}, but the {labelB} is {DartDouble(valueB)}."),
            new ErrorHint(
                "Maybe you have fallen prey to floating point rounding errors, and should explicitly "
                + $"apply the min() or max() functions, or the clamp() method, to the {labelB}?"),
        ];
    }

    /// <remarks>Dart's <c>double.toString</c>: integral values keep a <c>.0</c> suffix.</remarks>
    private static string DartDouble(double value)
    {
        string text = value.ToString("R", CultureInfo.InvariantCulture);
        return double.IsFinite(value) && value == Math.Floor(value) && !text.Contains('E')
            ? text + ".0"
            : text;
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

/// <summary>
/// Parent data that can keep its child alive after it scrolls out of view.
/// </summary>
/// <remarks>
/// Flutter's <c>KeepAliveParentDataMixin</c> (<c>rendering/sliver_multi_box_adaptor.dart</c>). C#
/// has no mixins, so the contract is an interface: it is what <see cref="Plumix.Widgets.KeepAlive"/>
/// writes into, and both the sliver adaptors and the two-dimensional viewport implement it.
/// </remarks>
public interface IKeepAliveParentData : IParentData
{
    /// <summary>Whether to keep the child alive even when it is no longer visible.</summary>
    bool KeepAlive { get; set; }

    /// <summary>
    /// Whether the child is currently being kept alive, i.e. has <see cref="KeepAlive"/> set and is
    /// offscreen.
    /// </summary>
    bool KeptAlive { get; }
}

/// <summary>
/// Parent data used by <see cref="RenderSliverMultiBoxAdaptor"/>.
/// </summary>
/// <remarks>
/// Flutter's <c>SliverMultiBoxAdaptorParentData</c>: a <c>SliverLogicalParentData</c> with the
/// container and keep-alive mixins. The child carries no paint offset — the adaptor derives one
/// from <see cref="RenderSliver.ChildMainAxisPosition"/> and
/// <see cref="RenderSliver.ChildCrossAxisPosition"/> each time it paints.
/// </remarks>
public class SliverMultiBoxAdaptorParentData : SliverLogicalParentData,
    IContainerParentDataMixin<RenderBox>,
    IKeepAliveParentData
{
    private readonly IContainerParentDataMixin<RenderBox> _containerMixin;

    public SliverMultiBoxAdaptorParentData()
    {
        _containerMixin = new ContainerParentDataMixin<RenderBox>(this);
    }

    /// <summary>
    /// The index of this child according to the <see cref="IRenderSliverBoxChildManager"/>, or null
    /// before the manager has adopted the child.
    /// </summary>
    public int? Index { get; set; }

    public bool KeepAlive { get; set; }

    public bool KeptAlive { get; set; }

    public RenderBox? previousSibling
    {
        get => _containerMixin.previousSibling;
        set => _containerMixin.previousSibling = value;
    }

    public RenderBox? nextSibling
    {
        get => _containerMixin.nextSibling;
        set => _containerMixin.nextSibling = value;
    }

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

/// <summary>Signature for a sliver hit test with main/cross-axis positions.</summary>
/// <remarks>Flutter's <c>SliverHitTest</c>.</remarks>
public delegate bool SliverHitTest(
    SliverHitTestResult result,
    double mainAxisPosition,
    double crossAxisPosition);

/// <summary>
/// The result of performing a hit test on <see cref="RenderSliver"/>s.
/// </summary>
/// <remarks>Flutter's <c>SliverHitTestResult</c>.</remarks>
public class SliverHitTestResult : HitTestResult
{
    /// <summary>Creates an empty hit test result for hit testing on <see cref="RenderSliver"/>.</summary>
    public SliverHitTestResult()
    {
    }

    /// <summary>
    /// Wraps <paramref name="result"/> to create a result that shares its path and transform stack.
    /// </summary>
    /// <remarks>Flutter's <c>SliverHitTestResult.wrap</c>.</remarks>
    public SliverHitTestResult(HitTestResult result) : base(result)
    {
    }

    /// <summary>Wraps <paramref name="result"/> so both share one path and transform stack.</summary>
    public static new SliverHitTestResult Wrap(HitTestResult result) => new(result);

    /// <summary>
    /// Transforms <paramref name="mainAxisPosition"/> and <paramref name="crossAxisPosition"/> to the
    /// local coordinate system of a child for hit-testing the child.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SliverHitTestResult.addWithAxisOffset</c>. The main/cross-axis offsets are
    /// subtracted from the positions before <paramref name="hitTest"/> runs; <paramref name="paintOffset"/>
    /// (the offset the child is painted at) only feeds the transform recorded on added entries.
    /// </remarks>
    public bool AddWithAxisOffset(
        Point? paintOffset,
        double mainAxisOffset,
        double crossAxisOffset,
        double mainAxisPosition,
        double crossAxisPosition,
        SliverHitTest hitTest)
    {
        ArgumentNullException.ThrowIfNull(hitTest);
        if (paintOffset is { } offset)
        {
            PushOffset(new Point(-offset.X, -offset.Y));
        }

        bool isHit = hitTest(
            this,
            mainAxisPosition - mainAxisOffset,
            crossAxisPosition - crossAxisOffset);
        if (paintOffset is not null)
        {
            PopTransform();
        }

        return isHit;
    }
}

/// <summary>
/// A hit test entry used by <see cref="RenderSliver"/>.
/// </summary>
/// <remarks>Flutter's <c>SliverHitTestEntry</c>.</remarks>
public class SliverHitTestEntry(RenderSliver target, double mainAxisPosition, double crossAxisPosition)
    : HitTestEntry(target)
{
    /// <summary>The <see cref="RenderSliver"/> that was hit.</summary>
    public new RenderSliver Target => (RenderSliver)base.Target;

    /// <summary>
    /// The distance in the <see cref="AxisDirection"/> from the edge of the sliver's painted
    /// boundary to the position of the hit.
    /// </summary>
    public double MainAxisPosition { get; } = mainAxisPosition;

    /// <summary>
    /// The distance to the hit in the cross axis direction for the sliver.
    /// </summary>
    public double CrossAxisPosition { get; } = crossAxisPosition;

    /// <inheritdoc />
    public override string ToString() =>
        $"{Target.GetType().Name}@(mainAxis: {DebugFormat(MainAxisPosition)}, "
        + $"crossAxis: {DebugFormat(CrossAxisPosition)})";

    /// <remarks>Dart's <c>double.toString</c>: integral values keep a <c>.0</c> suffix.</remarks>
    private static string DebugFormat(double value)
    {
        string text = value.ToString("R", CultureInfo.InvariantCulture);
        return double.IsFinite(value) && value == Math.Floor(value) && !text.Contains('E')
            ? text + ".0"
            : text;
    }
}

/// <summary>Base class for the render objects that implement scroll effects in viewports.</summary>
/// <remarks>
/// Flutter's <c>RenderSliver</c>: a <see cref="RenderObject"/> with its own layout protocol
/// (<see cref="SliverConstraints"/> in, <see cref="SliverGeometry"/> out) and its own hit-test
/// protocol (<see cref="SliverHitTestResult"/> with main/cross-axis positions).
/// </remarks>
public abstract class RenderSliver : RenderObject
{
    private SliverGeometry _geometry;
    private bool _hasGeometry;

    /// <summary>The layout constraints most recently supplied by the parent.</summary>
    /// <remarks>Flutter's <c>RenderSliver.constraints</c>.</remarks>
    public new SliverConstraints Constraints => (SliverConstraints)base.Constraints;

    /// <summary>
    /// Whether this sliver has been laid out at least once, so <see cref="Constraints"/> and
    /// <see cref="Geometry"/> are meaningful.
    /// </summary>
    /// <remarks>
    /// Stands in for Dart's <c>geometry != null</c>: <see cref="SliverGeometry"/> is a value type here,
    /// so <see cref="Geometry"/> reads as <see cref="SliverGeometry.Zero"/> before the first layout.
    /// </remarks>
    public bool HasSliverConstraints => _hasGeometry && HasConstraints;

    /// <summary>The amount of space this sliver occupies.</summary>
    /// <remarks>
    /// Flutter's <c>RenderSliver.geometry</c>. The setter may only be called by the sliver itself,
    /// from <see cref="RenderObject.PerformResize"/> when <see cref="RenderObject.SizedByParent"/> is
    /// true and from <see cref="RenderObject.PerformLayout"/> otherwise.
    /// </remarks>
    public SliverGeometry Geometry
    {
        get => _geometry;
        protected set
        {
            Debug.Assert(!(DebugDoingThisResize && DebugDoingThisLayout));
            Debug.Assert(SizedByParent || !DebugDoingThisResize);
            if (Constants.KDebugMode)
            {
                DebugCheckGeometrySetterPhase();
            }

            _geometry = value;
            _hasGeometry = true;
        }
    }

    /// <summary>Ports Dart's <c>RenderSliver.geometry</c> setter assertion.</summary>
    private void DebugCheckGeometrySetterPhase()
    {
        if ((SizedByParent && DebugDoingThisResize) || (!SizedByParent && DebugDoingThisLayout))
        {
            return;
        }

        Debug.Assert(!DebugDoingThisResize);
        DiagnosticsNode violation;
        DiagnosticsNode? hint = null;
        if (DebugDoingThisLayout)
        {
            Debug.Assert(SizedByParent);
            violation = new ErrorDescription(
                "It appears that the geometry setter was called from performLayout().");
        }
        else
        {
            violation = new ErrorDescription(
                "The geometry setter was called from outside layout (neither performResize() nor "
                + "performLayout() were being run for this object).");
            if (Owner is { DebugDoingLayout: true })
            {
                hint = new ErrorDescription(
                    "Only the object itself can set its geometry. It is a contract violation for other "
                    + "objects to set it.");
            }
        }

        DiagnosticsNode contract = SizedByParent
            ? new ErrorDescription(
                "Because this RenderSliver has sizedByParent set to true, it must set its geometry in "
                + "performResize().")
            : new ErrorDescription(
                "Because this RenderSliver has sizedByParent set to false, it must set its geometry in "
                + "performLayout().");
        throw new FlutterError(
        [
            new ErrorSummary("RenderSliver geometry setter called incorrectly."),
            violation,
            .. hint is null ? Array.Empty<DiagnosticsNode>() : [hint],
            contract,
            DescribeForError("The RenderSliver in question is"),
        ]);
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderSliver.semanticBounds</c>: the <see cref="PaintBounds"/>.</remarks>
    protected override Rect SemanticBounds => PaintBounds;

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliver.paintBounds</c>: the painted extent along the main axis by the cross
    /// axis extent, anchored at the origin.
    /// </remarks>
    public override Rect PaintBounds => Constraints.Axis switch
    {
        Axis.Horizontal => new Rect(0.0, 0.0, Geometry.PaintExtent, Constraints.CrossAxisExtent),
        _ => new Rect(0.0, 0.0, Constraints.CrossAxisExtent, Geometry.PaintExtent),
    };

    /// <inheritdoc />
    protected override void DebugResetSize()
    {
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderSliver.debugAssertDoesMeetConstraints</c>.</remarks>
    protected override void DebugAssertDoesMeetConstraints()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        Geometry.DebugAssertIsValid(() =>
        [
            DescribeForError("The RenderSliver that returned the offending geometry was"),
        ]);
        if (Geometry.PaintOrigin + Geometry.PaintExtent > Constraints.RemainingPaintExtent)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    "SliverGeometry has a paintOffset that exceeds the remainingPaintExtent from the "
                    + "constraints."),
                DescribeForError(
                    "The render object whose geometry violates the constraints is the following"),
                .. SliverGeometry.DebugCompareFloats(
                    "remainingPaintExtent",
                    Constraints.RemainingPaintExtent,
                    "paintOrigin + paintExtent",
                    Geometry.PaintOrigin + Geometry.PaintExtent),
                new ErrorDescription(
                    "The paintOrigin and paintExtent must cause the child sliver to paint within the "
                    + "viewport, and so cannot exceed the remainingPaintExtent."),
            ]);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliver.performResize</c>, which is <c>assert(false)</c>: slivers are never
    /// sized by their parent.
    /// </remarks>
    protected override void PerformResize()
    {
        Debug.Assert(false, "RenderSliver.PerformResize must not be called.");
    }

    /// <summary>
    /// Whether this sliver keeps contributing semantics even when it is scrolled entirely outside the
    /// viewport's paint and cache extents.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliver.ensureSemantics</c>. Defaults to <c>false</c>.</remarks>
    public virtual bool EnsureSemantics => false;

    /// <summary>
    /// For a center sliver, the distance before the absolute zero scroll offset that this sliver can
    /// cover.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliver.centerOffsetAdjustment</c>. Defaults to <c>0.0</c>.</remarks>
    public virtual double CenterOffsetAdjustment => 0.0;

    /// <summary>
    /// Determines the set of render objects located at the given position.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderSliver.hitTest</c>. Returns true if the given point is contained in this
    /// sliver or one of its descendants, and adds a <see cref="SliverHitTestEntry"/> for this sliver
    /// in that case. The main-axis position is measured from the edge of the sliver's painted boundary
    /// in the <see cref="SliverConstraints.AxisDirection"/>.
    /// </remarks>
    public virtual bool HitTest(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (mainAxisPosition >= 0.0
            && mainAxisPosition < Geometry.HitTestExtent
            && crossAxisPosition >= 0.0
            && crossAxisPosition < Constraints.CrossAxisExtent)
        {
            if (HitTestChildren(result, mainAxisPosition, crossAxisPosition)
                || HitTestSelf(mainAxisPosition, crossAxisPosition))
            {
                result.Add(new SliverHitTestEntry(this, mainAxisPosition, crossAxisPosition));
                return true;
            }
        }

        return false;
    }

    /// <summary>Override this method if this render object can be hit even if its children were not.</summary>
    /// <remarks>Flutter's <c>RenderSliver.hitTestSelf</c>.</remarks>
    protected virtual bool HitTestSelf(double mainAxisPosition, double crossAxisPosition) => false;

    /// <summary>Override this method to check whether any children are located at the given position.</summary>
    /// <remarks>Flutter's <c>RenderSliver.hitTestChildren</c>.</remarks>
    protected virtual bool HitTestChildren(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition) => false;

    /// <summary>
    /// Computes the portion of the region from <paramref name="from"/> to <paramref name="to"/> that
    /// is visible, assuming that only the region from <see cref="SliverConstraints.ScrollOffset"/>
    /// that is <see cref="SliverConstraints.RemainingPaintExtent"/> high is visible.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliver.calculatePaintOffset</c>.</remarks>
    public double CalculatePaintOffset(SliverConstraints constraints, double from, double to)
    {
        Debug.Assert(from <= to);
        double a = constraints.ScrollOffset;
        double b = constraints.ScrollOffset + constraints.RemainingPaintExtent;
        return Math.Clamp(
            Math.Clamp(to, a, b) - Math.Clamp(from, a, b),
            0.0,
            constraints.RemainingPaintExtent);
    }

    /// <summary>
    /// Computes the portion of the region from <paramref name="from"/> to <paramref name="to"/> that
    /// is within the cache extent of the viewport.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliver.calculateCacheOffset</c>.</remarks>
    public double CalculateCacheOffset(SliverConstraints constraints, double from, double to)
    {
        Debug.Assert(from <= to);
        double a = constraints.ScrollOffset + constraints.CacheOrigin;
        double b = constraints.ScrollOffset + constraints.RemainingCacheExtent;
        return Math.Clamp(
            Math.Clamp(to, a, b) - Math.Clamp(from, a, b),
            0.0,
            constraints.RemainingCacheExtent);
    }

    /// <summary>
    /// Returns the distance from the leading <em>visible</em> edge of the sliver to the side of the
    /// given child closest to that edge.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderSliver.childMainAxisPosition</c>. Slivers that have children must override
    /// it; the base implementation throws in debug builds the way Dart's assert does and returns
    /// <c>0.0</c> otherwise.
    /// </remarks>
    public virtual double ChildMainAxisPosition(RenderObject child)
    {
        if (Constants.KDebugMode)
        {
            throw new FlutterError(
                $"{Diagnostics.ObjectRuntimeType(this, "RenderSliver")} does not implement childPosition.");
        }

        return 0.0;
    }

    /// <summary>
    /// Returns the distance along the cross axis from the zero of the cross axis in this sliver's
    /// parent coordinate space to the corresponding zero in the child's coordinate space.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliver.childCrossAxisPosition</c>. Defaults to <c>0.0</c>.</remarks>
    public virtual double ChildCrossAxisPosition(RenderObject child) => 0.0;

    /// <summary>
    /// Returns the scroll offset for the leading edge of the given child.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderSliver.childScrollOffset</c>. Null when the child's position cannot be
    /// determined.
    /// </remarks>
    public virtual double? ChildScrollOffset(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        return 0.0;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliver.applyPaintTransform</c>, which asserts: a sliver with children must
    /// override it.
    /// </remarks>
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        if (Constants.KDebugMode)
        {
            throw new FlutterError(
                $"{Diagnostics.ObjectRuntimeType(this, "RenderSliver")} does not implement "
                + "applyPaintTransform.");
        }
    }

    /// <summary>
    /// This returns a <see cref="Size"/> with dimensions relative to the leading edge of the sliver,
    /// specifically the same offset that is given to the <see cref="RenderObject.Paint"/> method.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderSliver.getAbsoluteSizeRelativeToOrigin</c>. The width or height is negative
    /// when the sliver grows up or to the left.
    /// </remarks>
    protected internal Size GetAbsoluteSizeRelativeToOrigin()
    {
        Debug.Assert(HasSliverConstraints);
        Debug.Assert(!DebugNeedsLayout);
        return ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            Constraints.AxisDirection,
            Constraints.GrowthDirection) switch
        {
            AxisDirection.Up => new Size(Constraints.CrossAxisExtent, -Geometry.PaintExtent),
            AxisDirection.Right => new Size(Geometry.PaintExtent, Constraints.CrossAxisExtent),
            AxisDirection.Down => new Size(Constraints.CrossAxisExtent, Geometry.PaintExtent),
            _ => new Size(-Geometry.PaintExtent, Constraints.CrossAxisExtent),
        };
    }

    /// <summary>
    /// This returns the absolute <see cref="Size"/> of the sliver.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliver.getAbsoluteSize</c>.</remarks>
    protected internal Size GetAbsoluteSize()
    {
        Debug.Assert(HasSliverConstraints);
        Debug.Assert(!DebugNeedsLayout);
        return Constraints.AxisDirection switch
        {
            AxisDirection.Up or AxisDirection.Down => new Size(
                Constraints.CrossAxisExtent,
                Geometry.PaintExtent),
            _ => new Size(Geometry.PaintExtent, Constraints.CrossAxisExtent),
        };
    }

    /// <summary>Returns the <see cref="Rect"/> that covers the total paint extent of the sliver.</summary>
    /// <remarks>
    /// Flutter's <c>RenderSliver.getMaxPaintRect</c>. The rect is in this sliver's local coordinate
    /// system, whose origin is the <c>offset</c> passed to <see cref="RenderObject.Paint"/>.
    /// </remarks>
    protected Rect GetMaxPaintRect()
    {
        if (!HasSliverConstraints || Geometry == SliverGeometry.Zero)
        {
            return default;
        }

        SliverGeometry sliverGeometry = Geometry;
        SliverConstraints constraints = Constraints;
        double maxPaintExtent = sliverGeometry.MaxPaintExtent;
        if (double.IsInfinity(maxPaintExtent))
        {
            maxPaintExtent = constraints.ScrollOffset + sliverGeometry.CacheExtent + constraints.CacheOrigin;
        }

        double paintExtent = sliverGeometry.PaintExtent;
        // To ensure the computed rect remains visible when pinned, the leading offset is capped at the
        // sliver's `scrollExtent - maxScrollObstructionExtent`.
        double leadingOffset = Math.Clamp(
            constraints.ScrollOffset,
            0.0,
            sliverGeometry.ScrollExtent - sliverGeometry.MaxScrollObstructionExtent);
        double crossAxisExtent = sliverGeometry.CrossAxisExtent ?? constraints.CrossAxisExtent;
        Rect rect = constraints.Axis switch
        {
            Axis.Horizontal => new Rect(-leadingOffset, 0.0, maxPaintExtent, crossAxisExtent),
            _ => new Rect(0.0, -leadingOffset, crossAxisExtent, maxPaintExtent),
        };

        return ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            constraints.AxisDirection,
            constraints.GrowthDirection) switch
        {
            AxisDirection.Left => new Rect(
                new Point(paintExtent - rect.Right, rect.Top),
                new Point(paintExtent - rect.Left, rect.Bottom)),
            AxisDirection.Up => new Rect(
                new Point(rect.Left, paintExtent - rect.Bottom),
                new Point(rect.Right, paintExtent - rect.Top)),
            _ => rect,
        };
    }

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
        SliverConstraints constraints = Constraints;
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

    /// <summary>
    /// Override this method to handle pointer events that hit this render object.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderSliver.handleEvent</c>, whose entry is covariantly a
    /// <see cref="SliverHitTestEntry"/>.
    /// </remarks>
    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<SliverGeometry?>(
            "geometry",
            HasSliverConstraints ? Geometry : null));
    }
}

/// <summary>
/// Helpers for slivers whose children are <see cref="RenderBox"/>es, which have to be converted
/// between the sliver coordinate system and the Cartesian one the box protocol uses.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderSliverHelpers</c>. C# has no mixins, so the bodies live in a static class that
/// takes the sliver as its first argument; every member it reads is public on
/// <see cref="RenderSliver"/>.
/// </remarks>
public static class RenderSliverHelpers
{
    /// <remarks>Flutter's <c>RenderSliverHelpers._getRightWayUp</c>.</remarks>
    private static bool GetRightWayUp(SliverConstraints constraints)
    {
        bool reversed = ScrollDirectionUtils.AxisDirectionIsReversed(constraints.AxisDirection);
        return constraints.GrowthDirection == GrowthDirection.Forward ? !reversed : reversed;
    }

    /// <summary>
    /// Hit tests a box child of <paramref name="sliver"/>, converting the sliver-space position into
    /// the child's box space. Calling this for a child that is not visible is not valid.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliverHelpers.hitTestBoxChild</c>.</remarks>
    public static bool HitTestBoxChild(
        this RenderSliver sliver,
        BoxHitTestResult result,
        RenderBox child,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        SliverConstraints constraints = sliver.Constraints;
        bool rightWayUp = GetRightWayUp(constraints);
        double delta = sliver.ChildMainAxisPosition(child);
        double crossAxisDelta = sliver.ChildCrossAxisPosition(child);
        double absolutePosition = mainAxisPosition - delta;
        double absoluteCrossAxisPosition = crossAxisPosition - crossAxisDelta;
        Point paintOffset;
        Point transformedPosition;
        if (constraints.Axis == Axis.Horizontal)
        {
            if (!rightWayUp)
            {
                absolutePosition = child.Size.Width - absolutePosition;
                delta = sliver.Geometry.PaintExtent - child.Size.Width - delta;
            }

            paintOffset = new Point(delta, crossAxisDelta);
            transformedPosition = new Point(absolutePosition, absoluteCrossAxisPosition);
        }
        else
        {
            if (!rightWayUp)
            {
                absolutePosition = child.Size.Height - absolutePosition;
                delta = sliver.Geometry.PaintExtent - child.Size.Height - delta;
            }

            paintOffset = new Point(crossAxisDelta, delta);
            transformedPosition = new Point(absoluteCrossAxisPosition, absolutePosition);
        }

        return result.AddWithOutOfBandPosition(
            hitResult => child.HitTest(hitResult, transformedPosition),
            paintOffset: paintOffset);
    }

    /// <summary>
    /// Turns the values <see cref="RenderSliver.ChildMainAxisPosition"/> and
    /// <see cref="RenderSliver.ChildCrossAxisPosition"/> return for a box child into a translation,
    /// and applies it to <paramref name="transform"/>. Calling this for a child that is not visible
    /// is not valid.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSliverHelpers.applyPaintTransformForBoxChild</c>.</remarks>
    public static void ApplyPaintTransformForBoxChild(
        this RenderSliver sliver,
        RenderBox child,
        Matrix4 transform)
    {
        SliverConstraints constraints = sliver.Constraints;
        bool rightWayUp = GetRightWayUp(constraints);
        double delta = sliver.ChildMainAxisPosition(child);
        double crossAxisDelta = sliver.ChildCrossAxisPosition(child);
        if (constraints.Axis == Axis.Horizontal)
        {
            if (!rightWayUp)
            {
                delta = sliver.Geometry.PaintExtent - child.Size.Width - delta;
            }

            transform.TranslateByDouble(delta, crossAxisDelta, 0, 1);
        }
        else
        {
            if (!rightWayUp)
            {
                delta = sliver.Geometry.PaintExtent - child.Size.Height - delta;
            }

            transform.TranslateByDouble(crossAxisDelta, delta, 0, 1);
        }
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

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderProxySliver.semanticBounds</c>: the child's, when there is one.</remarks>
    protected override Rect SemanticBounds => _child != null
        ? _child.SemanticBoundsForSemantics
        : base.SemanticBounds;

    public override double ChildMainAxisPosition(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child, _child));
        return 0.0;
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        ((SliverPhysicalParentData)child.parentData!).ApplyPaintTransform(transform);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null)
        {
            ctx.PaintChild(_child, offset);
        }
    }

    protected override bool HitTestChildren(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        return _child != null
               && _child.Geometry.HitTestExtent > 0
               && _child.HitTest(result, mainAxisPosition, crossAxisPosition);
    }

    protected override void PerformLayout()
    {
        DebugAssertions.Assert(_child != null);
        _child!.Layout(Constraints, parentUsesSize: true);
        Geometry = _child.Geometry;
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

/// <summary>
/// Adds the <see cref="SemanticsProperties"/> it is given to the semantics of its sliver child.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderSliverSemanticsAnnotations</c>. It shares every behavior with
/// <see cref="RenderSemanticsAnnotations"/> — in Dart through <c>SemanticsAnnotationsMixin</c>, here
/// through the same <see cref="SemanticsAnnotations"/> helper — and differs only in its base class.
/// </remarks>
public sealed class RenderSliverSemanticsAnnotations : RenderProxySliver
{
    private readonly SemanticsAnnotations _annotations;

    public RenderSliverSemanticsAnnotations(
        SemanticsProperties properties,
        bool container = false,
        bool explicitChildNodes = false,
        bool excludeSemantics = false,
        bool blockUserActions = false,
        TextDirection? textDirection = null,
        Locale? localeForSubtree = null,
        RenderSliver? child = null) : base(child)
    {
        _annotations = new SemanticsAnnotations(
            MarkNeedsSemanticsUpdate,
            properties,
            container: container,
            explicitChildNodes: explicitChildNodes,
            excludeSemantics: excludeSemantics,
            blockUserActions: blockUserActions,
            textDirection: textDirection,
            localeForSubtree: localeForSubtree);
    }

    /// <summary>All the annotations this render object contributes.</summary>
    public SemanticsProperties Properties
    {
        get => _annotations.Properties;
        set => _annotations.Properties = value;
    }

    /// <summary>Whether this annotation introduces a semantics node of its own.</summary>
    public bool Container
    {
        get => _annotations.Container;
        set => _annotations.Container = value;
    }

    /// <summary>Whether the descendants must each produce their own semantics node.</summary>
    public bool ExplicitChildNodes
    {
        get => _annotations.ExplicitChildNodes;
        set => _annotations.ExplicitChildNodes = value;
    }

    /// <summary>Whether to drop all of the child's semantics.</summary>
    public bool ExcludeSemantics
    {
        get => _annotations.ExcludeSemantics;
        set => _annotations.ExcludeSemantics = value;
    }

    /// <summary>Whether the user actions of this subtree are blocked.</summary>
    public bool BlockUserActions
    {
        get => _annotations.BlockUserActions;
        set => _annotations.BlockUserActions = value;
    }

    /// <summary>The reading direction for this subtree's semantic strings.</summary>
    public TextDirection? TextDirection
    {
        get => _annotations.TextDirection;
        set => _annotations.TextDirection = value;
    }

    /// <summary>The locale annotated onto this subtree's semantics nodes.</summary>
    public Locale? LocaleForSubtree
    {
        get => _annotations.LocaleForSubtree;
        set => _annotations.LocaleForSubtree = value;
    }

    /// <inheritdoc />
    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_annotations.ExcludeSemantics)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }

    /// <inheritdoc />
    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        _annotations.DescribeSemanticsConfiguration(configuration);
    }
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

    public override bool HitTest(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        return !_ignoring && base.HitTest(result, mainAxisPosition, crossAxisPosition);
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
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
            MarkNeedsLayoutForSizedByParentChange();
        }
    }

    protected override void PerformLayout()
    {
        DebugAssertions.Assert(Child != null);
        Child!.Layout(Constraints, parentUsesSize: true);
        Geometry = _offstage ? SliverGeometry.Zero : Child.Geometry;
    }

    public override bool HitTest(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        return !_offstage && base.HitTest(result, mainAxisPosition, crossAxisPosition);
    }

    protected override bool HitTestChildren(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        return !_offstage
               && Child != null
               && Child.Geometry.HitTestExtent > 0
               && Child.HitTest(result, mainAxisPosition, crossAxisPosition);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_offstage)
        {
            return;
        }

        ctx.PaintChild(Child!, offset);
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (!_offstage)
        {
            base.VisitChildrenForSemantics(visitor);
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

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
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

    public override bool AlwaysNeedsCompositing => Child != null && _opacity > 0.0;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child == null || !Child.Geometry.Visible || _opacity == 0.0)
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

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
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
    private readonly RenderAnimatedOpacityMixin _animatedOpacity;

    public RenderSliverAnimatedOpacity(
        Animation<double> opacity,
        bool alwaysIncludeSemantics = false,
        RenderSliver? sliver = null) : base(sliver)
    {
        _animatedOpacity = new RenderAnimatedOpacityMixin(this, () => Child != null);
        Opacity = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public Animation<double> Opacity
    {
        get => _animatedOpacity.Opacity;
        set => _animatedOpacity.Opacity = value;
    }

    public bool AlwaysIncludeSemantics
    {
        get => _animatedOpacity.AlwaysIncludeSemantics;
        set => _animatedOpacity.AlwaysIncludeSemantics = value;
    }

    public override bool IsRepaintBoundary => _animatedOpacity.IsRepaintBoundary;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_animatedOpacity.Alpha == 0)
        {
            return;
        }

        base.Paint(ctx, offset);
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer) =>
        _animatedOpacity.UpdateCompositedLayer(oldLayer);

    protected override void UpdateCompositedLayer(OffsetLayer layer) =>
        _animatedOpacity.UpdateCompositedLayer(layer);

    protected override void OnAttach()
    {
        base.OnAttach();
        _animatedOpacity.OnAttach();
    }

    protected override void OnDetach()
    {
        _animatedOpacity.OnDetach();
        base.OnDetach();
    }

    public override bool PaintsChild(RenderObject child) => _animatedOpacity.PaintsChild(child);

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (_animatedOpacity.IncludesChildInSemantics())
        {
            visitor(Child!);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        _animatedOpacity.DebugFillProperties(properties);
    }
}

/// <summary>
/// An abstract class for <see cref="RenderSliver"/>s that contain a single <see cref="RenderBox"/>.
/// </summary>
/// <remarks>Flutter's <c>RenderSliverSingleBoxAdapter</c>.</remarks>
public abstract class RenderSliverSingleBoxAdapter : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderBox? _child;

    /// <summary>Creates a <see cref="RenderSliver"/> that wraps a <see cref="RenderBox"/>.</summary>
    protected RenderSliverSingleBoxAdapter(RenderBox? child = null)
    {
        Child = child;
    }

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

    /// <summary>Sets the <see cref="SliverPhysicalParentData.PaintOffset"/> of the child.</summary>
    /// <remarks>Flutter's <c>RenderSliverSingleBoxAdapter.setChildParentData</c>.</remarks>
    protected void SetChildParentData(
        RenderObject child,
        SliverConstraints constraints,
        SliverGeometry geometry)
    {
        var childParentData = (SliverPhysicalParentData)child.parentData!;
        childParentData.PaintOffset = ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            constraints.AxisDirection,
            constraints.GrowthDirection) switch
        {
            AxisDirection.Up => new Point(
                0.0,
                geometry.PaintExtent + constraints.ScrollOffset - geometry.ScrollExtent),
            AxisDirection.Left => new Point(
                geometry.PaintExtent + constraints.ScrollOffset - geometry.ScrollExtent,
                0.0),
            AxisDirection.Right => new Point(-constraints.ScrollOffset, 0.0),
            _ => new Point(0.0, -constraints.ScrollOffset),
        };
    }

    protected override bool HitTestChildren(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        Debug.Assert(Geometry.HitTestExtent > 0.0);
        if (Child != null)
        {
            return this.HitTestBoxChild(
                BoxHitTestResult.Wrap(result),
                Child,
                mainAxisPosition: mainAxisPosition,
                crossAxisPosition: crossAxisPosition);
        }

        return false;
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        return -Constraints.ScrollOffset;
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        Debug.Assert(ReferenceEquals(child, Child));
        ((SliverPhysicalParentData)child.parentData!).ApplyPaintTransform(transform);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child != null && Geometry.Visible)
        {
            var childParentData = (SliverPhysicalParentData)Child.parentData!;
            ctx.PaintChild(Child, offset + childParentData.PaintOffset);
        }
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

/// <summary>A <see cref="RenderSliver"/> that contains a single <see cref="RenderBox"/>.</summary>
/// <remarks>Flutter's <c>RenderSliverToBoxAdapter</c>.</remarks>
public class RenderSliverToBoxAdapter : RenderSliverSingleBoxAdapter
{
    /// <summary>Creates a <see cref="RenderSliver"/> that wraps a <see cref="RenderBox"/>.</summary>
    public RenderSliverToBoxAdapter(RenderBox? child = null) : base(child)
    {
    }

    protected override void PerformLayout()
    {
        if (Child == null)
        {
            Geometry = SliverGeometry.Zero;
            return;
        }

        SliverConstraints constraints = Constraints;
        Child.Layout(constraints.AsBoxConstraints(), parentUsesSize: true);
        double childExtent = constraints.Axis switch
        {
            Axis.Horizontal => Child.Size.Width,
            _ => Child.Size.Height,
        };
        double paintedChildSize = CalculatePaintOffset(constraints, from: 0.0, to: childExtent);
        double cacheExtent = CalculateCacheOffset(constraints, from: 0.0, to: childExtent);

        Debug.Assert(double.IsFinite(paintedChildSize));
        Debug.Assert(paintedChildSize >= 0.0);
        Geometry = new SliverGeometry(
            ScrollExtent: childExtent,
            PaintExtent: paintedChildSize,
            CacheExtent: cacheExtent,
            MaxPaintExtent: childExtent,
            HitTestExtent: paintedChildSize,
            HasVisualOverflow: childExtent > constraints.RemainingPaintExtent || constraints.ScrollOffset > 0.0);
        SetChildParentData(Child, constraints, Geometry);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_padding.dart
/// <summary>
/// Insets a <see cref="RenderSliver"/> by applying <see cref="ResolvedPadding"/> on each side.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderSliverEdgeInsetsPadding</c>. A subclass supplies <see cref="ResolvedPadding"/>,
/// the padding resolved against the text direction.
/// </remarks>
public abstract class RenderSliverEdgeInsetsPadding : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderSliver? _child;

    /// <summary>The sliver this padding insets.</summary>
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

    /// <summary>The amount to pad the child in each dimension.</summary>
    /// <remarks>Flutter's <c>RenderSliverEdgeInsetsPadding.resolvedPadding</c>; null until resolved.</remarks>
    public abstract Thickness? ResolvedPadding { get; }

    /// <summary>The padding in the scroll direction on the side nearest the 0.0 scroll direction.</summary>
    /// <remarks>Flutter's <c>RenderSliverEdgeInsetsPadding.beforePadding</c>.</remarks>
    protected double BeforePadding
    {
        get
        {
            Debug.Assert(ResolvedPadding != null);
            Thickness resolvedPadding = ResolvedPadding!.Value;
            return ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
                Constraints.AxisDirection,
                Constraints.GrowthDirection) switch
            {
                AxisDirection.Up => resolvedPadding.Bottom,
                AxisDirection.Right => resolvedPadding.Left,
                AxisDirection.Down => resolvedPadding.Top,
                _ => resolvedPadding.Right,
            };
        }
    }

    /// <summary>The padding in the scroll direction on the side furthest from the 0.0 scroll offset.</summary>
    /// <remarks>Flutter's <c>RenderSliverEdgeInsetsPadding.afterPadding</c>.</remarks>
    protected double AfterPadding
    {
        get
        {
            Debug.Assert(ResolvedPadding != null);
            Thickness resolvedPadding = ResolvedPadding!.Value;
            return ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
                Constraints.AxisDirection,
                Constraints.GrowthDirection) switch
            {
                AxisDirection.Up => resolvedPadding.Top,
                AxisDirection.Right => resolvedPadding.Right,
                AxisDirection.Down => resolvedPadding.Bottom,
                _ => resolvedPadding.Left,
            };
        }
    }

    /// <summary>The total padding in the <see cref="SliverConstraints.AxisDirection"/>.</summary>
    /// <remarks>Flutter's <c>RenderSliverEdgeInsetsPadding.mainAxisPadding</c>.</remarks>
    protected double MainAxisPadding
    {
        get
        {
            Debug.Assert(ResolvedPadding != null);
            Thickness resolvedPadding = ResolvedPadding!.Value;
            return Constraints.Axis == Axis.Horizontal
                ? resolvedPadding.Left + resolvedPadding.Right
                : resolvedPadding.Top + resolvedPadding.Bottom;
        }
    }

    /// <summary>The total padding in the cross-axis direction.</summary>
    /// <remarks>Flutter's <c>RenderSliverEdgeInsetsPadding.crossAxisPadding</c>.</remarks>
    protected double CrossAxisPadding
    {
        get
        {
            Debug.Assert(ResolvedPadding != null);
            Thickness resolvedPadding = ResolvedPadding!.Value;
            return Constraints.Axis == Axis.Horizontal
                ? resolvedPadding.Top + resolvedPadding.Bottom
                : resolvedPadding.Left + resolvedPadding.Right;
        }
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

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
        double PaintOffset(double from, double to) => CalculatePaintOffset(constraints, from, to);
        double CacheOffset(double from, double to) => CalculateCacheOffset(constraints, from, to);

        Debug.Assert(ResolvedPadding != null);
        Thickness resolvedPadding = ResolvedPadding!.Value;
        double beforePadding = BeforePadding;
        double afterPadding = AfterPadding;
        double mainAxisPadding = MainAxisPadding;
        double crossAxisPadding = CrossAxisPadding;
        if (_child == null)
        {
            double emptyPaintExtent = PaintOffset(0.0, mainAxisPadding);
            double emptyCacheExtent = CacheOffset(0.0, mainAxisPadding);
            Geometry = new SliverGeometry(
                ScrollExtent: mainAxisPadding,
                PaintExtent: Math.Min(emptyPaintExtent, constraints.RemainingPaintExtent),
                MaxPaintExtent: mainAxisPadding,
                CacheExtent: emptyCacheExtent);
            return;
        }

        double beforePaddingPaintExtent = PaintOffset(0.0, beforePadding);
        double overlap = constraints.Overlap;
        if (overlap > 0)
        {
            overlap = Math.Max(0.0, constraints.Overlap - beforePaddingPaintExtent);
        }

        _child.Layout(
            constraints with
            {
                ScrollOffset = Math.Max(0.0, constraints.ScrollOffset - beforePadding),
                CacheOrigin = Math.Min(0.0, constraints.CacheOrigin + beforePadding),
                Overlap = overlap,
                RemainingPaintExtent = constraints.RemainingPaintExtent - PaintOffset(0.0, beforePadding),
                RemainingCacheExtent = constraints.RemainingCacheExtent - CacheOffset(0.0, beforePadding),
                CrossAxisExtent = Math.Max(0.0, constraints.CrossAxisExtent - crossAxisPadding),
                PrecedingScrollExtent = beforePadding + constraints.PrecedingScrollExtent,
            },
            parentUsesSize: true);
        SliverGeometry childLayoutGeometry = _child.Geometry;
        if (childLayoutGeometry.ScrollOffsetCorrection is double correction)
        {
            Geometry = new SliverGeometry(ScrollOffsetCorrection: correction);
            return;
        }

        double scrollExtent = childLayoutGeometry.ScrollExtent;
        double beforePaddingCacheExtent = CacheOffset(0.0, beforePadding);
        double afterPaddingCacheExtent = CacheOffset(
            beforePadding + scrollExtent,
            mainAxisPadding + scrollExtent);
        double afterPaddingPaintExtent = PaintOffset(
            beforePadding + scrollExtent,
            mainAxisPadding + scrollExtent);
        double mainAxisPaddingCacheExtent = beforePaddingCacheExtent + afterPaddingCacheExtent;
        double mainAxisPaddingPaintExtent = beforePaddingPaintExtent + afterPaddingPaintExtent;
        double paintExtent = Math.Min(
            beforePaddingPaintExtent
            + Math.Max(
                childLayoutGeometry.PaintExtent,
                childLayoutGeometry.LayoutExtent + afterPaddingPaintExtent),
            constraints.RemainingPaintExtent);
        Geometry = new SliverGeometry(
            PaintOrigin: childLayoutGeometry.PaintOrigin,
            ScrollExtent: mainAxisPadding + scrollExtent,
            PaintExtent: paintExtent,
            LayoutExtent: Math.Min(
                mainAxisPaddingPaintExtent + childLayoutGeometry.LayoutExtent,
                paintExtent),
            CacheExtent: Math.Min(
                mainAxisPaddingCacheExtent + childLayoutGeometry.CacheExtent,
                constraints.RemainingCacheExtent),
            MaxPaintExtent: mainAxisPadding + childLayoutGeometry.MaxPaintExtent,
            HitTestExtent: Math.Max(
                mainAxisPaddingPaintExtent + childLayoutGeometry.PaintExtent,
                beforePaddingPaintExtent + childLayoutGeometry.HitTestExtent),
            HasVisualOverflow: childLayoutGeometry.HasVisualOverflow);
        double calculatedOffset = ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            constraints.AxisDirection,
            constraints.GrowthDirection) switch
        {
            AxisDirection.Up => PaintOffset(
                resolvedPadding.Bottom + scrollExtent,
                resolvedPadding.Top + resolvedPadding.Bottom + scrollExtent),
            AxisDirection.Left => PaintOffset(
                resolvedPadding.Right + scrollExtent,
                resolvedPadding.Left + resolvedPadding.Right + scrollExtent),
            AxisDirection.Right => PaintOffset(0.0, resolvedPadding.Left),
            _ => PaintOffset(0.0, resolvedPadding.Top),
        };
        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        childParentData.PaintOffset = constraints.Axis switch
        {
            Axis.Horizontal => new Point(calculatedOffset, resolvedPadding.Top),
            _ => new Point(resolvedPadding.Left, calculatedOffset),
        };
        Debug.Assert(beforePadding == BeforePadding);
        Debug.Assert(afterPadding == AfterPadding);
        Debug.Assert(mainAxisPadding == MainAxisPadding);
        Debug.Assert(crossAxisPadding == CrossAxisPadding);
    }

    protected override bool HitTestChildren(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        if (_child != null && _child.Geometry.HitTestExtent > 0.0)
        {
            var childParentData = (SliverPhysicalParentData)_child.parentData!;
            return result.AddWithAxisOffset(
                mainAxisPosition: mainAxisPosition,
                crossAxisPosition: crossAxisPosition,
                mainAxisOffset: ChildMainAxisPosition(_child),
                crossAxisOffset: ChildCrossAxisPosition(_child),
                paintOffset: childParentData.PaintOffset,
                hitTest: _child.HitTest);
        }

        return false;
    }

    public override double ChildMainAxisPosition(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child, _child));
        return CalculatePaintOffset(Constraints, from: 0.0, to: BeforePadding);
    }

    public override double ChildCrossAxisPosition(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child, _child));
        Debug.Assert(ResolvedPadding != null);
        return Constraints.Axis == Axis.Horizontal ? ResolvedPadding!.Value.Top : ResolvedPadding!.Value.Left;
    }

    public override double? ChildScrollOffset(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        return BeforePadding;
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        Debug.Assert(ReferenceEquals(child, _child));
        ((SliverPhysicalParentData)child.parentData!).ApplyPaintTransform(transform);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null && _child.Geometry.Visible)
        {
            var childParentData = (SliverPhysicalParentData)_child.parentData!;
            ctx.PaintChild(_child, offset + childParentData.PaintOffset);
        }
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderSliverEdgeInsetsPadding.debugPaint</c>.</remarks>
    protected override void DebugPaint(PaintingContext context, Point offset)
    {
        ArgumentNullException.ThrowIfNull(context);
        base.DebugPaint(context, offset);
        if (!RenderingDebug.PaintSizeEnabled)
        {
            return;
        }

        var outerRect = new Rect(offset, GetAbsoluteSize());
        Rect? innerRect = null;
        if (_child is not null)
        {
            Size childSize = _child.GetAbsoluteSize();
            var childParentData = (SliverPhysicalParentData)_child.parentData!;
            Rect inner = new(offset + childParentData.PaintOffset, childSize);
            Debug.Assert(inner.Top >= outerRect.Top);
            Debug.Assert(inner.Left >= outerRect.Left);
            Debug.Assert(inner.Right <= outerRect.Right);
            Debug.Assert(inner.Bottom <= outerRect.Bottom);
            innerRect = inner;
        }

        RenderingDebug.PaintPadding(context, outerRect, innerRect);
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => DebugDescribeSingleChild(Child);
}

/// <summary>
/// Insets a <see cref="RenderSliver"/>, applying padding on each side.
/// </summary>
/// <remarks>Flutter's <c>RenderSliverPadding</c>.</remarks>
public class RenderSliverPadding : RenderSliverEdgeInsetsPadding
{
    private Thickness? _resolvedPadding;
    private EdgeInsetsGeometry _padding;
    private TextDirection? _textDirection;

    /// <summary>Creates a render object that insets its child in a viewport.</summary>
    public RenderSliverPadding(
        EdgeInsetsGeometry padding,
        RenderSliver? child = null,
        TextDirection? textDirection = null)
    {
        Debug.Assert(padding.IsNonNegative);
        _padding = padding;
        _textDirection = textDirection;
        Child = child;
    }

    /// <inheritdoc />
    public override Thickness? ResolvedPadding => _resolvedPadding;

    private void Resolve()
    {
        if (ResolvedPadding != null)
        {
            return;
        }

        _resolvedPadding = _padding.Resolve(_textDirection);
        Debug.Assert(((EdgeInsetsGeometry)_resolvedPadding.Value).IsNonNegative);
    }

    private void MarkNeedsResolution()
    {
        _resolvedPadding = null;
        MarkNeedsLayout();
    }

    /// <summary>The amount to pad the child in each dimension.</summary>
    public EdgeInsetsGeometry Padding
    {
        get => _padding;
        set
        {
            Debug.Assert(value.IsNonNegative);
            if (_padding == value)
            {
                return;
            }

            _padding = value;
            MarkNeedsResolution();
        }
    }

    /// <summary>The text direction with which to resolve <see cref="Padding"/>.</summary>
    public TextDirection? TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsResolution();
        }
    }

    protected override void PerformLayout()
    {
        Resolve();
        base.PerformLayout();
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry>("padding", Padding));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
    }
}

public abstract class RenderSliverMultiBoxAdaptor : RenderSliver,
    IContainerRenderObjectMixin<RenderBox, SliverMultiBoxAdaptorParentData>,
    IRenderObjectContainer
{
    private readonly ContainerRenderObjectMixin<RenderBox, SliverMultiBoxAdaptorParentData> _container;
    private readonly Dictionary<int, RenderBox> _keepAliveBucket = [];
    private readonly List<RenderBox> _debugDanglingKeepAlives = [];
    private IRenderSliverBoxChildManager? _childManager;
    private bool _debugChildIntegrityEnabled = true;

    protected RenderSliverMultiBoxAdaptor(IRenderSliverBoxChildManager? childManager = null)
    {
        _container = new ContainerRenderObjectMixin<RenderBox, SliverMultiBoxAdaptorParentData>(this);
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

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor.paint</c>: the child's paint offset is derived from
    /// <see cref="RenderSliver.ChildMainAxisPosition"/> and
    /// <see cref="RenderSliver.ChildCrossAxisPosition"/> against the resolved axis direction, so a
    /// reversed sliver paints its children from the trailing edge back.
    /// </remarks>
    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (FirstChild is null)
        {
            return;
        }

        // offset is to the top-left corner, regardless of our axis direction.
        // originOffset gives us the delta from the real origin to the origin in the axis direction.
        SliverConstraints constraints = Constraints;
        Point mainAxisUnit;
        Point crossAxisUnit;
        Point originOffset;
        bool addExtent;
        switch (ScrollDirectionUtils.ApplyGrowthDirectionToAxisDirection(
            constraints.AxisDirection,
            constraints.GrowthDirection))
        {
            case AxisDirection.Up:
                mainAxisUnit = new Point(0.0, -1.0);
                crossAxisUnit = new Point(1.0, 0.0);
                originOffset = offset + new Point(0.0, Geometry.PaintExtent);
                addExtent = true;
                break;
            case AxisDirection.Right:
                mainAxisUnit = new Point(1.0, 0.0);
                crossAxisUnit = new Point(0.0, 1.0);
                originOffset = offset;
                addExtent = false;
                break;
            case AxisDirection.Down:
                mainAxisUnit = new Point(0.0, 1.0);
                crossAxisUnit = new Point(1.0, 0.0);
                originOffset = offset;
                addExtent = false;
                break;
            default:
                mainAxisUnit = new Point(-1.0, 0.0);
                crossAxisUnit = new Point(0.0, 1.0);
                originOffset = offset + new Point(Geometry.PaintExtent, 0.0);
                addExtent = true;
                break;
        }

        RenderBox? child = FirstChild;
        while (child is not null)
        {
            double mainAxisDelta = ChildMainAxisPosition(child);
            double crossAxisDelta = ChildCrossAxisPosition(child);
            var childOffset = new Point(
                originOffset.X + (mainAxisUnit.X * mainAxisDelta) + (crossAxisUnit.X * crossAxisDelta),
                originOffset.Y + (mainAxisUnit.Y * mainAxisDelta) + (crossAxisUnit.Y * crossAxisDelta));
            double childExtent = PaintExtentOf(child);
            if (addExtent)
            {
                childOffset += new Point(mainAxisUnit.X * childExtent, mainAxisUnit.Y * childExtent);
            }

            // If the child's visible interval (mainAxisDelta, mainAxisDelta + paintExtentOf(child))
            // does not intersect the paint extent interval (0, constraints.remainingPaintExtent),
            // it's hidden.
            if (mainAxisDelta < constraints.RemainingPaintExtent && mainAxisDelta + childExtent > 0)
            {
                ctx.PaintChild(child, childOffset);
            }

            child = ChildAfter(child);
        }
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>RenderSliverMultiBoxAdaptor.hitTestChildren</c>.</remarks>
    protected override bool HitTestChildren(
        SliverHitTestResult result,
        double mainAxisPosition,
        double crossAxisPosition)
    {
        RenderBox? child = LastChild;
        var boxResult = BoxHitTestResult.Wrap(result);
        while (child is not null)
        {
            if (this.HitTestBoxChild(boxResult, child, mainAxisPosition, crossAxisPosition))
            {
                return true;
            }

            child = ChildBefore(child);
        }

        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>RenderSliverMultiBoxAdaptor.applyPaintTransform</c>: a child that is not painted
    /// has no meaningful transform, and asking for one is valid, so the matrix is zeroed instead of
    /// running the box-child conversion, which would throw.
    /// </remarks>
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        if (!PaintsChild(child))
        {
            transform.SetZero();
            return;
        }

        this.ApplyPaintTransformForBoxChild((RenderBox)child, transform);
    }

    public override void VisitChildrenForSemantics(Action<RenderObject> visitor)
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
        return ChildScrollOffset(child)!.Value - Constraints.ScrollOffset;
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
        return Constraints.Axis == Axis.Horizontal ? child.Size.Width : child.Size.Height;
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

        return childParentData.Index is { } index && !_keepAliveBucket.ContainsKey(index);
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
        body(Constraints);
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
            Constraints);
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
        InvokeLayoutCallback<SliverConstraints>(_ => DestroyOrCacheChildInner(child), Constraints);
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

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
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

    protected override void PerformLayout()
    {
        SliverConstraints constraints = Constraints;
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
            ApplyChildGeometry(newLeadingParentData, gridGeometry);
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
            ApplyChildGeometry(childParentData, gridGeometry);
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

    private static void ApplyChildGeometry(SliverGridParentData parentData, SliverGridGeometry geometry)
    {
        parentData.LayoutOffset = geometry.ScrollOffset;
        parentData.CrossAxisOffset = geometry.CrossAxisOffset;
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
