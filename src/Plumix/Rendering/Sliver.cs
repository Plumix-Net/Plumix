using Avalonia;
using Avalonia.Media;
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
    GrowthDirection GrowthDirection = GrowthDirection.Forward);

public readonly record struct SliverGeometry(
    double ScrollExtent = 0,
    double PaintExtent = 0,
    double LayoutExtent = 0,
    double MaxPaintExtent = 0,
    double CacheExtent = 0,
    double ScrollOffsetCorrection = 0,
    bool HasVisualOverflow = false);

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
}

public class SliverMultiBoxAdaptorParentData : ContainerBoxParentData<RenderBox>
{
    public int Index { get; set; }
    public double LayoutOffset { get; set; }
    public bool KeepAlive { get; set; }
    public bool KeptAlive { get; set; }
}

public sealed class SliverGridParentData : SliverMultiBoxAdaptorParentData
{
    public double CrossAxisOffset { get; set; }
}

public abstract class RenderSliver : RenderBox
{
    private SliverConstraints? _sliverConstraints;

    public SliverConstraints ConstraintsForSliver =>
        _sliverConstraints ?? throw new InvalidOperationException("RenderSliver is not laid out.");

    public SliverGeometry Geometry { get; protected set; }

    public void LayoutWithSliverConstraints(SliverConstraints constraints)
    {
        if (_sliverConstraints != constraints)
        {
            MarkNeedsLayout();
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

        Layout(layoutConstraints);
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

    protected abstract void PerformSliverLayout(SliverConstraints constraints);
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

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child == null || Geometry.PaintExtent <= 0)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        ctx.PaintChild(_child, offset + childParentData.offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null || Geometry.PaintExtent <= 0)
        {
            return false;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        return _child.HitTest(result, position - childParentData.offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_child == null)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        visitor(_child, childParentData.offset, Matrix.Identity);
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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
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

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child == null || Geometry.PaintExtent <= 0)
        {
            return;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        ctx.PaintChild(Child, offset + childParentData.offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null || Geometry.PaintExtent <= 0)
        {
            return false;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        return Child.HitTest(result, position - childParentData.offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Child == null)
        {
            return;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        visitor(Child, childParentData.offset, Matrix.Identity);
    }
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
        double layoutExtent = Math.Min(remaining, constraints.ViewportMainAxisExtent);
        double cacheStart = constraints.ScrollOffset + constraints.CacheOrigin;
        double cacheEnd = cacheStart + Math.Max(0, constraints.RemainingCacheExtent);
        double cacheExtent = Math.Max(0, Math.Min(childExtent, cacheEnd) - Math.Max(0, cacheStart));

        var childParentData = (BoxParentData)Child.parentData!;
        childParentData.offset = constraints.Axis == Axis.Vertical
            ? new Point(0, -effectiveScrollOffset)
            : new Point(-effectiveScrollOffset, 0);

        Geometry = new SliverGeometry(
            ScrollExtent: childExtent,
            PaintExtent: paintedExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: childExtent,
            CacheExtent: cacheExtent,
            HasVisualOverflow: remaining > constraints.RemainingPaintExtent);
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver_persistent_header.dart
public sealed class RenderSliverPersistentHeader : RenderSliverSingleBoxAdapter
{
    private double _minExtent;
    private double _maxExtent;
    private bool _pinned;
    private bool _floating;
    private double _lastActualScrollOffset;
    private double _effectiveScrollOffset;
    private bool _hasLayout;

    public RenderSliverPersistentHeader(
        double minExtent,
        double maxExtent,
        bool pinned,
        bool floating,
        Action<double, bool>? onLayout = null,
        RenderBox? child = null)
    {
        ValidateExtents(minExtent, maxExtent);
        _minExtent = minExtent;
        _maxExtent = maxExtent;
        _pinned = pinned;
        _floating = floating;
        OnLayout = onLayout;
        Child = child;
    }

    public double MinExtent { get => _minExtent; set { ValidateExtents(value, _maxExtent); if (Close(_minExtent, value)) return; _minExtent = value; MarkNeedsLayout(); } }
    public double MaxExtent { get => _maxExtent; set { ValidateExtents(_minExtent, value); if (Close(_maxExtent, value)) return; _maxExtent = value; MarkNeedsLayout(); } }
    public bool Pinned { get => _pinned; set { if (_pinned == value) return; _pinned = value; MarkNeedsLayout(); } }
    public bool Floating { get => _floating; set { if (_floating == value) return; _floating = value; _hasLayout = false; MarkNeedsLayout(); } }
    public Action<double, bool>? OnLayout { get; set; }
    public double LastShrinkOffset { get; private set; }
    public bool LastOverlapsContent { get; private set; }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        double maxShrinkExtent = Math.Max(0, MaxExtent - MinExtent);
        double actualScrollOffset = Math.Max(0, constraints.ScrollOffset);
        if (Floating)
        {
            if (_hasLayout && (actualScrollOffset < _lastActualScrollOffset || _effectiveScrollOffset < MaxExtent))
            {
                double delta = _lastActualScrollOffset - actualScrollOffset;
                if (delta > 0 && _effectiveScrollOffset > MaxExtent)
                    _effectiveScrollOffset = MaxExtent;
                _effectiveScrollOffset = Math.Clamp(_effectiveScrollOffset - delta, 0, actualScrollOffset);
            }
            else
            {
                _effectiveScrollOffset = actualScrollOffset;
            }
        }
        else
        {
            _effectiveScrollOffset = actualScrollOffset;
        }

        double shrinkOffset = Math.Clamp(_effectiveScrollOffset, 0, maxShrinkExtent);
        _lastActualScrollOffset = actualScrollOffset;
        _hasLayout = true;
        double currentExtent = Math.Max(MinExtent, MaxExtent - shrinkOffset);
        bool overlapsContent = Floating
            ? _effectiveScrollOffset < actualScrollOffset
            : actualScrollOffset > maxShrinkExtent + 0.0001;

        double unclampedPaintExtent = Floating
            ? MaxExtent - _effectiveScrollOffset
            : Pinned ? currentExtent : MaxExtent - actualScrollOffset;
        if (Pinned) unclampedPaintExtent = Math.Max(MinExtent, unclampedPaintExtent);
        double paintExtent = Math.Clamp(unclampedPaintExtent, 0, constraints.RemainingPaintExtent);

        if (Child is not null)
        {
            var childConstraints = constraints.Axis == Axis.Vertical
                ? new BoxConstraints(
                    MinWidth: constraints.CrossAxisExtent,
                    MaxWidth: constraints.CrossAxisExtent,
                    MinHeight: currentExtent,
                    MaxHeight: currentExtent)
                : new BoxConstraints(
                    MinWidth: currentExtent,
                    MaxWidth: currentExtent,
                    MinHeight: constraints.CrossAxisExtent,
                    MaxHeight: constraints.CrossAxisExtent);
            Child.Layout(childConstraints, parentUsesSize: true);
            double extraScroll = Pinned ? 0 : Math.Max(0, currentExtent - paintExtent);
            ((BoxParentData)Child.parentData!).offset = constraints.Axis == Axis.Vertical
                ? new Point(0, -extraScroll)
                : new Point(-extraScroll, 0);
        }

        double layoutExtent = Floating || Pinned
            ? Math.Clamp(MaxExtent - actualScrollOffset, 0, paintExtent)
            : paintExtent;
        Geometry = new SliverGeometry(
            ScrollExtent: MaxExtent,
            PaintExtent: paintExtent,
            LayoutExtent: layoutExtent,
            MaxPaintExtent: MaxExtent,
            CacheExtent: Math.Min(MaxExtent, Math.Max(0, constraints.RemainingCacheExtent)),
            HasVisualOverflow: currentExtent > paintExtent || overlapsContent);

        LastShrinkOffset = shrinkOffset;
        LastOverlapsContent = overlapsContent;
        OnLayout?.Invoke(shrinkOffset, overlapsContent);
    }

    private static void ValidateExtents(double minExtent, double maxExtent)
    {
        if (!double.IsFinite(minExtent) || minExtent < 0) throw new ArgumentOutOfRangeException(nameof(minExtent));
        if (!double.IsFinite(maxExtent) || maxExtent < minExtent) throw new ArgumentOutOfRangeException(nameof(maxExtent));
    }

    private static bool Close(double a, double b) => Math.Abs(a - b) <= 0.0001;
}

public sealed class RenderSliverPadding : RenderSliver, IRenderObjectSingleChildContainer
{
    private RenderSliver? _child;
    private Thickness _padding;

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
        if (_child == null || Geometry.PaintExtent <= 0)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        ctx.PaintChild(_child, offset + childParentData.offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null || Geometry.PaintExtent <= 0)
        {
            return false;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        return _child.HitTest(result, position - childParentData.offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_child == null)
        {
            return;
        }

        var childParentData = (SliverPhysicalParentData)_child.parentData!;
        visitor(_child, childParentData.offset, Matrix.Identity);
    }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        (double mainStartPadding, double mainEndPadding, double crossStartPadding, double crossEndPadding) = ResolvePadding(_padding, constraints);
        double mainAxisPadding = mainStartPadding + mainEndPadding;
        double crossAxisPadding = crossStartPadding + crossEndPadding;
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

        if (Math.Abs(_child.Geometry.ScrollOffsetCorrection) > 0.0001)
        {
            Geometry = new SliverGeometry(ScrollOffsetCorrection: _child.Geometry.ScrollOffsetCorrection);
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
        double paintExtent = CalculatePaintExtent(
            from: 0,
            to: totalScrollExtent,
            scrollOffset: constraints.ScrollOffset,
            remainingPaintExtent: constraints.RemainingPaintExtent);
        double layoutExtent = Math.Min(paintExtent, constraints.ViewportMainAxisExtent);
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
            CacheExtent: cacheExtent,
            HasVisualOverflow:
            _child.Geometry.HasVisualOverflow
            || totalScrollExtent > targetEndScrollOffsetForPaint
            || constraints.ScrollOffset > 0);
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

    public void AddAll(List<RenderBox> children)
    {
        _container.AddAll(children);
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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (var child = FirstChild; child != null; child = ChildAfter(child))
        {
            var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
            visitor(child, childParentData.offset, Matrix.Identity);
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

    protected double ChildScrollOffset(RenderBox child)
    {
        return ((SliverMultiBoxAdaptorParentData)child.parentData!).LayoutOffset;
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

    private bool CreateOrObtainChild(int index, RenderBox? after)
    {
        if (index < 0)
        {
            return false;
        }

        if (_keepAliveBucket.TryGetValue(index, out var keptAliveChild))
        {
            _keepAliveBucket.Remove(index);
            var parentData = (SliverMultiBoxAdaptorParentData)keptAliveChild.parentData!;
            parentData.KeptAlive = false;
            Insert(keptAliveChild, after);
            return true;
        }

        return _childManager?.CreateChild(index, after) ?? false;
    }

    private void DestroyOrCacheChild(RenderBox child)
    {
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        if (childParentData.KeepAlive)
        {
            Remove(child);
            _keepAliveBucket[childParentData.Index] = child;
            AdoptChild(child);
            childParentData.KeptAlive = true;
            return;
        }

        _childManager?.RemoveChild(child);
    }

    public void DefaultPaint(PaintingContext ctx, Point offset)
    {
        _container.DefaultPaint(ctx, offset);
    }

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
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
    private double _itemExtent;

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
        if (childManager == null || _itemExtent <= 0)
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

        int firstIndex = GetMinChildIndexForScrollOffset(scrollOffset, _itemExtent);
        int targetLastIndex = GetMaxChildIndexForScrollOffset(targetEndScrollOffset, _itemExtent);
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

        var childConstraints = FixedExtentChildConstraints(constraints, _itemExtent);
        if (FirstChild == null && !AddInitialChild(firstIndex, firstIndex * _itemExtent))
        {
            Geometry = new SliverGeometry(
                ScrollExtent: childCount.HasValue ? childCount.Value * _itemExtent : 0,
                MaxPaintExtent: childCount.HasValue ? childCount.Value * _itemExtent : 0);
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
            newLeadingParentData.LayoutOffset = targetIndex * _itemExtent;
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
            childParentData.LayoutOffset = index * _itemExtent;
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

        double leadingScrollOffset = firstIndex * _itemExtent;
        double trailingScrollOffset = (index + 1) * _itemExtent;
        if (reachedEnd && childCount.HasValue)
        {
            trailingScrollOffset = Math.Min(trailingScrollOffset, childCount.Value * _itemExtent);
        }

        double estimatedMaxScrollOffset = childCount.HasValue
            ? childCount.Value * _itemExtent
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

public sealed class RenderSliverVariableExtentList : RenderSliverMultiBoxAdaptor
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
        if (manager is null) { Geometry = default; return; }
        manager.SetDidUnderflow(false);
        int? count = manager.ChildCount;
        if (count == 0) { Geometry = default; manager.SetDidUnderflow(true); return; }

        double cache = constraints.RemainingCacheExtent > 0 ? constraints.RemainingCacheExtent : constraints.RemainingPaintExtent;
        double start = Math.Max(0, constraints.ScrollOffset + constraints.CacheOrigin);
        int first = Math.Max(0, ExtentLayout.GetMinChildIndexForScrollOffset(constraints, start));
        int last = Math.Max(first, ExtentLayout.GetMaxChildIndexForScrollOffset(constraints, start + cache));
        if (count.HasValue)
        {
            int max = Math.Max(0, count.Value - 1);
            first = Math.Min(first, max);
            last = Math.Min(last, max);
        }

        int active = 0;
        for (var existing = FirstChild; existing is not null; existing = ChildAfter(existing)) active++;
        if (active > 0) CollectGarbage(active, 0);
        if (!AddInitialChild(first, ExtentLayout.GetChildLayoutOffset(constraints, first)))
        {
            double max = ExtentLayout.ComputeMaxScrollOffset(constraints, count);
            Geometry = new SliverGeometry(ScrollExtent: max, MaxPaintExtent: max);
            manager.SetDidUnderflow(true);
            return;
        }

        RenderBox? child = FirstChild;
        int index = first;
        double trailing = 0;
        while (child is not null && index <= last)
        {
            double extent = Math.Max(0, ExtentLayout.GetChildMainAxisExtent(constraints, index));
            var childConstraints = constraints.Axis == Axis.Vertical
                ? new BoxConstraints(MinWidth: constraints.CrossAxisExtent, MaxWidth: constraints.CrossAxisExtent, MinHeight: extent, MaxHeight: extent)
                : new BoxConstraints(MinWidth: extent, MaxWidth: extent, MinHeight: constraints.CrossAxisExtent, MaxHeight: constraints.CrossAxisExtent);
            child.Layout(childConstraints, parentUsesSize: true);
            var data = (SliverMultiBoxAdaptorParentData)child.parentData!;
            data.Index = index;
            data.LayoutOffset = ExtentLayout.GetChildLayoutOffset(constraints, index);
            data.offset = constraints.Axis == Axis.Vertical
                ? new Point(0, data.LayoutOffset - constraints.ScrollOffset)
                : new Point(data.LayoutOffset - constraints.ScrollOffset, 0);
            trailing = data.LayoutOffset + extent;
            if (index == last) break;
            var next = InsertAndLayoutChild(childConstraints, child);
            if (next is null) { manager.SetDidUnderflow(true); break; }
            child = next;
            index++;
        }

        double maxExtent = ExtentLayout.ComputeMaxScrollOffset(constraints, count);
        double paint = Math.Max(0, Math.Min(trailing, constraints.ScrollOffset + constraints.RemainingPaintExtent) - Math.Max(ExtentLayout.GetChildLayoutOffset(constraints, first), constraints.ScrollOffset));
        Geometry = new SliverGeometry(ScrollExtent: maxExtent, PaintExtent: paint, LayoutExtent: Math.Min(paint, constraints.ViewportMainAxisExtent), MaxPaintExtent: maxExtent, CacheExtent: paint, HasVisualOverflow: trailing > constraints.ScrollOffset + constraints.RemainingPaintExtent || constraints.ScrollOffset > 0);
    }
}
