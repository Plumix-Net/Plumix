using Avalonia;
using Avalonia.Media;
using Flutter.UI;
using Flutter.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/proxy_box.dart (approximate)

namespace Flutter.Rendering;

public abstract class RenderProxyBox : RenderBox, IRenderObjectSingleChildContainer
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

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_child != null)
        {
            var childParentData = (BoxParentData)_child.parentData!;
            visitor(_child, childParentData.offset, Matrix.Identity);
        }
    }

    protected override void PerformLayout()
    {
        if (_child != null)
        {
            _child.Layout(Constraints, parentUsesSize: true);
            Size = Constraints.Constrain(_child.Size);
            ((BoxParentData)_child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = Constraints.Constrain(new Size());
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_child != null)
        {
            var childParentData = (BoxParentData)_child.parentData!;
            ctx.PaintChild(_child, childParentData.offset + offset);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (_child == null)
        {
            return false;
        }

        var childParentData = (BoxParentData)_child.parentData!;
        return _child.HitTest(result, position - childParentData.offset);
    }
}

public sealed class RenderConstrainedBox : RenderProxyBox
{
    private BoxConstraints _additionalConstraints;

    public RenderConstrainedBox(BoxConstraints additionalConstraints, RenderBox? child = null)
    {
        _additionalConstraints = additionalConstraints;
        Child = child;
    }

    public BoxConstraints AdditionalConstraints
    {
        get => _additionalConstraints;
        set
        {
            if (_additionalConstraints.Equals(value))
            {
                return;
            }

            _additionalConstraints = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var enforced = _additionalConstraints.Enforce(Constraints);

        if (Child != null)
        {
            Child.Layout(enforced, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = enforced.Constrain(new Size());
        }
    }
}

public sealed class RenderUnconstrainedBox : RenderProxyBox
{
    private Alignment _alignment;
    private Axis? _constrainedAxis;

    public RenderUnconstrainedBox(
        Alignment alignment = default,
        Axis? constrainedAxis = null,
        RenderBox? child = null)
    {
        _alignment = alignment;
        _constrainedAxis = constrainedAxis;
        Child = child;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public Axis? ConstrainedAxis
    {
        get => _constrainedAxis;
        set
        {
            if (_constrainedAxis == value)
            {
                return;
            }

            _constrainedAxis = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child == null)
        {
            Size = Constraints.Constrain(new Size());
            return;
        }

        var childConstraints = _constrainedAxis switch
        {
            Axis.Horizontal => new BoxConstraints(
                MinWidth: Constraints.MinWidth,
                MaxWidth: Constraints.MaxWidth,
                MinHeight: 0,
                MaxHeight: double.PositiveInfinity),
            Axis.Vertical => new BoxConstraints(
                MinWidth: 0,
                MaxWidth: double.PositiveInfinity,
                MinHeight: Constraints.MinHeight,
                MaxHeight: Constraints.MaxHeight),
            null => new BoxConstraints(
                MinWidth: 0,
                MaxWidth: double.PositiveInfinity,
                MinHeight: 0,
                MaxHeight: double.PositiveInfinity),
            _ => throw new ArgumentOutOfRangeException()
        };

        Child.Layout(childConstraints, parentUsesSize: true);
        Size = Constraints.Constrain(Child.Size);
        ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
    }
}

public sealed class RenderLimitedBox : RenderProxyBox
{
    private double _maxWidth;
    private double _maxHeight;

    public RenderLimitedBox(
        double maxWidth = double.PositiveInfinity,
        double maxHeight = double.PositiveInfinity,
        RenderBox? child = null)
    {
        _maxWidth = ValidateMaxValue(maxWidth, nameof(maxWidth));
        _maxHeight = ValidateMaxValue(maxHeight, nameof(maxHeight));
        Child = child;
    }

    public double MaxWidth
    {
        get => _maxWidth;
        set
        {
            var normalized = ValidateMaxValue(value, nameof(value));
            if (Math.Abs(_maxWidth - normalized) < 0.0001)
            {
                return;
            }

            _maxWidth = normalized;
            MarkNeedsLayout();
        }
    }

    public double MaxHeight
    {
        get => _maxHeight;
        set
        {
            var normalized = ValidateMaxValue(value, nameof(value));
            if (Math.Abs(_maxHeight - normalized) < 0.0001)
            {
                return;
            }

            _maxHeight = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var limitedConstraints = new BoxConstraints(
            MinWidth: Constraints.MinWidth,
            MaxWidth: Constraints.HasBoundedWidth ? Constraints.MaxWidth : Constraints.ConstrainWidth(MaxWidth),
            MinHeight: Constraints.MinHeight,
            MaxHeight: Constraints.HasBoundedHeight ? Constraints.MaxHeight : Constraints.ConstrainHeight(MaxHeight));

        if (Child != null)
        {
            Child.Layout(limitedConstraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = limitedConstraints.Constrain(new Size());
        }
    }

    private static double ValidateMaxValue(double value, string parameterName)
    {
        if (double.IsNaN(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Max value must be non-negative.");
        }

        return value;
    }
}

public enum OverflowBoxFit
{
    Max,
    DeferToChild
}

public sealed class RenderConstrainedOverflowBox : RenderProxyBox
{
    private Alignment _alignment;
    private double? _minWidth;
    private double? _maxWidth;
    private double? _minHeight;
    private double? _maxHeight;
    private OverflowBoxFit _fit;

    public RenderConstrainedOverflowBox(
        Alignment alignment = default,
        double? minWidth = null,
        double? maxWidth = null,
        double? minHeight = null,
        double? maxHeight = null,
        OverflowBoxFit fit = OverflowBoxFit.Max,
        RenderBox? child = null)
    {
        _alignment = alignment;
        _minWidth = ValidateConstraint(minWidth, nameof(minWidth));
        _maxWidth = ValidateConstraint(maxWidth, nameof(maxWidth));
        _minHeight = ValidateConstraint(minHeight, nameof(minHeight));
        _maxHeight = ValidateConstraint(maxHeight, nameof(maxHeight));
        ValidateRanges(_minWidth, _maxWidth, nameof(minWidth), nameof(maxWidth));
        ValidateRanges(_minHeight, _maxHeight, nameof(minHeight), nameof(maxHeight));
        _fit = fit;
        Child = child;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public double? MinWidth
    {
        get => _minWidth;
        set
        {
            var normalized = ValidateConstraint(value, nameof(value));
            ValidateRanges(normalized, _maxWidth, nameof(value), nameof(MaxWidth));
            if (_minWidth == normalized)
            {
                return;
            }

            _minWidth = normalized;
            MarkNeedsLayout();
        }
    }

    public double? MaxWidth
    {
        get => _maxWidth;
        set
        {
            var normalized = ValidateConstraint(value, nameof(value));
            ValidateRanges(_minWidth, normalized, nameof(MinWidth), nameof(value));
            if (_maxWidth == normalized)
            {
                return;
            }

            _maxWidth = normalized;
            MarkNeedsLayout();
        }
    }

    public double? MinHeight
    {
        get => _minHeight;
        set
        {
            var normalized = ValidateConstraint(value, nameof(value));
            ValidateRanges(normalized, _maxHeight, nameof(value), nameof(MaxHeight));
            if (_minHeight == normalized)
            {
                return;
            }

            _minHeight = normalized;
            MarkNeedsLayout();
        }
    }

    public double? MaxHeight
    {
        get => _maxHeight;
        set
        {
            var normalized = ValidateConstraint(value, nameof(value));
            ValidateRanges(_minHeight, normalized, nameof(MinHeight), nameof(value));
            if (_maxHeight == normalized)
            {
                return;
            }

            _maxHeight = normalized;
            MarkNeedsLayout();
        }
    }

    public OverflowBoxFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            _fit = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child != null)
        {
            Child.Layout(GetInnerConstraints(Constraints), parentUsesSize: true);
            Size = _fit switch
            {
                OverflowBoxFit.Max => Constraints.Biggest,
                OverflowBoxFit.DeferToChild => Constraints.Constrain(Child.Size),
                _ => throw new ArgumentOutOfRangeException()
            };
            ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
            return;
        }

        Size = _fit switch
        {
            OverflowBoxFit.Max => Constraints.Biggest,
            OverflowBoxFit.DeferToChild => Constraints.Smallest,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private BoxConstraints GetInnerConstraints(BoxConstraints constraints)
    {
        return new BoxConstraints(
            MinWidth: _minWidth ?? constraints.MinWidth,
            MaxWidth: _maxWidth ?? constraints.MaxWidth,
            MinHeight: _minHeight ?? constraints.MinHeight,
            MaxHeight: _maxHeight ?? constraints.MaxHeight);
    }

    private static double? ValidateConstraint(double? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (double.IsNaN(value.Value) || value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Constraint value must be non-negative.");
        }

        return value.Value;
    }

    private static void ValidateRanges(
        double? minValue,
        double? maxValue,
        string minName,
        string maxName)
    {
        if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
        {
            throw new ArgumentOutOfRangeException(
                minName,
                $"{minName} cannot be greater than {maxName}.");
        }
    }
}

public sealed class RenderSizedOverflowBox : RenderProxyBox
{
    private Alignment _alignment;
    private Size _requestedSize;

    public RenderSizedOverflowBox(
        Size requestedSize,
        Alignment alignment = default,
        RenderBox? child = null)
    {
        _requestedSize = requestedSize;
        _alignment = alignment;
        Child = child;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public Size RequestedSize
    {
        get => _requestedSize;
        set
        {
            if (_requestedSize == value)
            {
                return;
            }

            _requestedSize = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(_requestedSize);
        if (Child == null)
        {
            return;
        }

        Child.Layout(Constraints, parentUsesSize: true);
        ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
    }
}

public sealed class RenderOffstage : RenderProxyBox
{
    private bool _offstage;

    public RenderOffstage(bool offstage = true, RenderBox? child = null)
    {
        _offstage = offstage;
        Child = child;
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

    protected override void PerformLayout()
    {
        if (_offstage)
        {
            if (Child != null)
            {
                Child.Layout(Constraints, parentUsesSize: true);
                ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
            }

            Size = Constraints.Smallest;
            return;
        }

        base.PerformLayout();
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        return !_offstage && base.HitTest(result, position);
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_offstage)
        {
            return;
        }

        base.Paint(ctx, offset);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (_offstage)
        {
            return;
        }

        base.VisitChildrenForSemantics(visitor);
    }
}

public sealed class RenderPadding : RenderProxyBox
{
    private Thickness _padding;

    public RenderPadding(Thickness padding, RenderBox? child = null)
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

    protected override void PerformLayout()
    {
        if (Child == null)
        {
            Size = Constraints.Constrain(new Size(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom));
            return;
        }

        var innerConstraints = Constraints.Deflate(Padding);
        Child.Layout(innerConstraints, parentUsesSize: true);

        var childSize = Child.Size;
        Size = Constraints.Constrain(
            new Size(childSize.Width + Padding.Left + Padding.Right, childSize.Height + Padding.Top + Padding.Bottom));

        ((BoxParentData)Child.parentData!).offset = new Point(Padding.Left, Padding.Top);
    }
}

public sealed class RenderAlign : RenderProxyBox
{
    private Alignment _alignment;
    private double? _widthFactor;
    private double? _heightFactor;

    public RenderAlign(
        Alignment alignment = default,
        double? widthFactor = null,
        double? heightFactor = null,
        RenderBox? child = null)
    {
        _alignment = alignment;
        _widthFactor = ValidateFactor(widthFactor, nameof(widthFactor));
        _heightFactor = ValidateFactor(heightFactor, nameof(heightFactor));
        Child = child;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public double? WidthFactor
    {
        get => _widthFactor;
        set
        {
            var normalized = ValidateFactor(value, nameof(value));
            if (_widthFactor == normalized)
            {
                return;
            }

            _widthFactor = normalized;
            MarkNeedsLayout();
        }
    }

    public double? HeightFactor
    {
        get => _heightFactor;
        set
        {
            var normalized = ValidateFactor(value, nameof(value));
            if (_heightFactor == normalized)
            {
                return;
            }

            _heightFactor = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var shrinkWrapWidth = _widthFactor.HasValue;
        var shrinkWrapHeight = _heightFactor.HasValue;

        if (Child == null)
        {
            var fallbackWidth = shrinkWrapWidth ? 0.0 : double.PositiveInfinity;
            var fallbackHeight = shrinkWrapHeight ? 0.0 : double.PositiveInfinity;
            Size = Constraints.Constrain(new Size(fallbackWidth, fallbackHeight));
            return;
        }

        Child.Layout(BoxConstraints.Loose(Constraints.Biggest), parentUsesSize: true);
        var childSize = Child.Size;
        var targetWidth = shrinkWrapWidth ? childSize.Width * _widthFactor!.Value : double.PositiveInfinity;
        var targetHeight = shrinkWrapHeight ? childSize.Height * _heightFactor!.Value : double.PositiveInfinity;
        Size = Constraints.Constrain(new Size(targetWidth, targetHeight));
        ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, childSize);
    }

    private static double? ValidateFactor(double? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Factor must be non-negative.");
        }

        return value.Value;
    }
}

public sealed class RenderAspectRatio : RenderProxyBox
{
    private double _aspectRatio;

    public RenderAspectRatio(double aspectRatio, RenderBox? child = null)
    {
        _aspectRatio = ValidateAspectRatio(aspectRatio, nameof(aspectRatio));
        Child = child;
    }

    public double AspectRatio
    {
        get => _aspectRatio;
        set
        {
            var normalized = ValidateAspectRatio(value, nameof(value));
            if (Math.Abs(_aspectRatio - normalized) < 0.0001)
            {
                return;
            }

            _aspectRatio = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        var computedSize = ComputeSizeForConstraints(Constraints);
        Size = computedSize;

        if (Child != null)
        {
            Child.Layout(BoxConstraints.Tight(computedSize));
            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }
    }

    private Size ComputeSizeForConstraints(BoxConstraints constraints)
    {
        if (constraints.IsTight)
        {
            return constraints.Smallest;
        }

        if (double.IsPositiveInfinity(constraints.MaxWidth) &&
            double.IsPositiveInfinity(constraints.MaxHeight))
        {
            throw new InvalidOperationException(
                "RenderAspectRatio requires at least one bounded axis.");
        }

        var width = constraints.MaxWidth;
        var height = width / _aspectRatio;

        if (double.IsPositiveInfinity(width))
        {
            height = constraints.MaxHeight;
            width = height * _aspectRatio;
        }

        if (width > constraints.MaxWidth)
        {
            width = constraints.MaxWidth;
            height = width / _aspectRatio;
        }

        if (height > constraints.MaxHeight)
        {
            height = constraints.MaxHeight;
            width = height * _aspectRatio;
        }

        if (width < constraints.MinWidth)
        {
            width = constraints.MinWidth;
            height = width / _aspectRatio;
        }

        if (height < constraints.MinHeight)
        {
            height = constraints.MinHeight;
            width = height * _aspectRatio;
        }

        return constraints.Constrain(new Size(width, height));
    }

    private static double ValidateAspectRatio(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Aspect ratio must be finite and positive.");
        }

        return value;
    }
}

public sealed class RenderFractionallySizedBox : RenderProxyBox
{
    private Alignment _alignment;
    private double? _widthFactor;
    private double? _heightFactor;

    public RenderFractionallySizedBox(
        Alignment alignment = default,
        double? widthFactor = null,
        double? heightFactor = null,
        RenderBox? child = null)
    {
        _alignment = alignment;
        _widthFactor = ValidateFactor(widthFactor, nameof(widthFactor));
        _heightFactor = ValidateFactor(heightFactor, nameof(heightFactor));
        Child = child;
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsLayout();
        }
    }

    public double? WidthFactor
    {
        get => _widthFactor;
        set
        {
            var normalized = ValidateFactor(value, nameof(value));
            if (_widthFactor == normalized)
            {
                return;
            }

            _widthFactor = normalized;
            MarkNeedsLayout();
        }
    }

    public double? HeightFactor
    {
        get => _heightFactor;
        set
        {
            var normalized = ValidateFactor(value, nameof(value));
            if (_heightFactor == normalized)
            {
                return;
            }

            _heightFactor = normalized;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child != null)
        {
            var innerConstraints = GetInnerConstraints(Constraints);
            Child.Layout(innerConstraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
            ((BoxParentData)Child.parentData!).offset = _alignment.AlongOffset(Size, Child.Size);
            return;
        }

        Size = Constraints.Constrain(GetInnerConstraints(Constraints).Constrain(new Size()));
    }

    private BoxConstraints GetInnerConstraints(BoxConstraints constraints)
    {
        var minWidth = constraints.MinWidth;
        var maxWidth = constraints.MaxWidth;

        if (_widthFactor.HasValue && double.IsFinite(maxWidth))
        {
            var width = maxWidth * _widthFactor.Value;
            minWidth = width;
            maxWidth = width;
        }

        var minHeight = constraints.MinHeight;
        var maxHeight = constraints.MaxHeight;

        if (_heightFactor.HasValue && double.IsFinite(maxHeight))
        {
            var height = maxHeight * _heightFactor.Value;
            minHeight = height;
            maxHeight = height;
        }

        return new BoxConstraints(
            MinWidth: minWidth,
            MaxWidth: maxWidth,
            MinHeight: minHeight,
            MaxHeight: maxHeight);
    }

    private static double? ValidateFactor(double? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (!double.IsFinite(value.Value) || value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Factor must be finite and non-negative.");
        }

        return value.Value;
    }
}

public sealed class RenderFittedBox : RenderProxyBox
{
    private BoxFit _fit;
    private Alignment _alignment;
    private Matrix _transform = Matrix.Identity;
    private bool _paintDataDirty = true;

    public RenderFittedBox(
        BoxFit fit = BoxFit.Contain,
        Alignment alignment = default,
        RenderBox? child = null)
    {
        _fit = fit;
        _alignment = alignment;
        Child = child;
    }

    public BoxFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            _fit = value;
            _paintDataDirty = true;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
    }

    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            _paintDataDirty = true;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void PerformLayout()
    {
        if (Child != null)
        {
            Child.Layout(
                new BoxConstraints(
                    MaxWidth: double.PositiveInfinity,
                    MaxHeight: double.PositiveInfinity),
                parentUsesSize: true);

            Size = _fit switch
            {
                BoxFit.ScaleDown => Constraints.Constrain(
                    Constraints.Loosen().ConstrainSizeAndAttemptToPreserveAspectRatio(Child.Size)),
                _ => Constraints.ConstrainSizeAndAttemptToPreserveAspectRatio(Child.Size)
            };

            ((BoxParentData)Child.parentData!).offset = new Point(0, 0);
        }
        else
        {
            Size = Constraints.Smallest;
        }

        _paintDataDirty = true;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child == null || Size.Width <= 0 || Size.Height <= 0 || Child.Size.Width <= 0 || Child.Size.Height <= 0)
        {
            return;
        }

        UpdatePaintData();
        ctx.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y), translatedContext =>
        {
            translatedContext.PushTransform(_transform, transformedContext =>
            {
                transformedContext.PaintChild(Child, new Point(0, 0));
            });
        });
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null || Size.Width <= 0 || Size.Height <= 0 || Child.Size.Width <= 0 || Child.Size.Height <= 0)
        {
            return false;
        }

        UpdatePaintData();
        if (!_transform.TryInvert(out var inverse))
        {
            return false;
        }

        var transformedPosition = inverse.Transform(position);
        return Child.HitTest(result, transformedPosition);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Child == null)
        {
            return;
        }

        UpdatePaintData();
        visitor(Child, new Point(0, 0), _transform);
    }

    private void UpdatePaintData()
    {
        if (!_paintDataDirty)
        {
            return;
        }

        _paintDataDirty = false;
        if (Child == null)
        {
            _transform = Matrix.Identity;
            return;
        }

        var childSize = Child.Size;
        var fittedSizes = BoxFitUtils.ApplyBoxFit(_fit, childSize, Size);
        var sourceSize = fittedSizes.Source;
        var destinationSize = fittedSizes.Destination;

        if (sourceSize.Width <= 0.0 || sourceSize.Height <= 0.0 ||
            destinationSize.Width <= 0.0 || destinationSize.Height <= 0.0)
        {
            _transform = Matrix.Identity;
            return;
        }

        var sourceOffset = _alignment.AlongOffset(childSize, sourceSize);
        var destinationOffset = _alignment.AlongOffset(Size, destinationSize);
        var scaleX = destinationSize.Width / sourceSize.Width;
        var scaleY = destinationSize.Height / sourceSize.Height;

        _transform =
            Matrix.CreateTranslation(destinationOffset.X, destinationOffset.Y)
            * new Matrix(scaleX, 0, 0, scaleY, 0, 0)
            * Matrix.CreateTranslation(-sourceOffset.X, -sourceOffset.Y);
    }
}

public sealed class RenderColoredBox : RenderProxyBox
{
    private Color _color;

    public RenderColoredBox(Color color, RenderBox? child = null)
    {
        _color = color;
        Child = child;
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        ctx.DrawRectangle(new SolidColorBrush(Color), null, new Rect(offset, Size));
        base.Paint(ctx, offset);
    }
}

public sealed class RenderDecoratedBox : RenderProxyBox
{
    private BoxDecoration _decoration;

    public RenderDecoratedBox(BoxDecoration decoration, RenderBox? child = null)
    {
        _decoration = decoration ?? new BoxDecoration();
        Child = child;
    }

    public BoxDecoration Decoration
    {
        get => _decoration;
        set
        {
            var next = value ?? new BoxDecoration();
            if (_decoration == next)
            {
                return;
            }

            _decoration = next;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        var rect = new Rect(offset, Size);
        var radius = _decoration.EffectiveBorderRadius.Radius;
        var boxShadows = _decoration.EffectiveBoxShadows;
        IBrush? fill = _decoration.Color.HasValue
            ? new SolidColorBrush(_decoration.Color.Value)
            : null;

        IPen? borderPen = null;
        if (_decoration.Border.HasValue)
        {
            var border = _decoration.Border.Value;
            if (border.Width > 0)
            {
                borderPen = new Pen(new SolidColorBrush(border.Color), border.Width);
            }
        }

        if (fill != null || borderPen != null || boxShadows.Count > 0)
        {
            ctx.DrawRectangle(fill ?? Brushes.Transparent, borderPen, rect, radius, radius, boxShadows);
        }

        base.Paint(ctx, offset);
    }
}

public sealed class RenderOpacity : RenderProxyBox
{
    private double _opacity;

    public RenderOpacity(double opacity = 1.0, RenderBox? child = null)
    {
        _opacity = Math.Clamp(opacity, 0.0, 1.0);
        Child = child;
    }

    public double Opacity
    {
        get => _opacity;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_opacity - clamped) < 0.0001)
            {
                return;
            }

            _opacity = clamped;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
            }
        }
    }

    public override bool IsRepaintBoundary => Child != null;
    protected override bool AlwaysNeedsCompositing => Child != null && Opacity < 1.0;

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as OpacityOffsetLayer ?? new OpacityOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is OpacityOffsetLayer opacityLayer)
        {
            opacityLayer.Opacity = Opacity;
        }
    }
}

public sealed class RenderTransform : RenderProxyBox
{
    private Matrix _transform;

    public RenderTransform(Matrix transform, RenderBox? child = null)
    {
        _transform = transform;
        Child = child;
    }

    public Matrix Transform
    {
        get => _transform;
        set
        {
            if (_transform == value)
            {
                return;
            }

            _transform = value;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public override bool IsRepaintBoundary => Child != null;
    protected override bool AlwaysNeedsCompositing => Child != null;

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        if (Child != null)
        {
            var childParentData = (BoxParentData)Child.parentData!;
            visitor(Child, childParentData.offset, Transform);
        }
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as TransformOffsetLayer ?? new TransformOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is TransformOffsetLayer transformLayer)
        {
            transformLayer.Transform = Transform;
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null)
        {
            return false;
        }

        if (!Transform.TryInvert(out var inverse))
        {
            return false;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        var transformedPosition = inverse.Transform(position - childParentData.offset);
        return Child.HitTest(result, transformedPosition);
    }
}

public sealed class RenderClipRect : RenderProxyBox
{
    private Rect _clipRect;
    private bool _hasExplicitClipRect;

    public RenderClipRect(RenderBox? child = null)
    {
        Child = child;
    }

    public Rect ClipRect
    {
        get => _clipRect;
        set
        {
            if (_hasExplicitClipRect && _clipRect == value)
            {
                return;
            }

            _clipRect = value;
            _hasExplicitClipRect = true;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public override bool IsRepaintBoundary => Child != null;
    protected override bool AlwaysNeedsCompositing => Child != null;

    protected override void PerformLayout()
    {
        var hadSize = HasSize;
        var previousSize = hadSize ? Size : default;
        base.PerformLayout();

        if (_hasExplicitClipRect || Child == null)
        {
            return;
        }

        if (!hadSize || previousSize != Size)
        {
            MarkNeedsCompositedLayerUpdate();
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as ClipRectOffsetLayer ?? new ClipRectOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is ClipRectOffsetLayer clipLayer)
        {
            clipLayer.ClipRect = _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
        }
    }

    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return null;
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        var clip = _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
        if (!clip.Contains(position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }
}

public sealed class RenderClipRRect : RenderProxyBox
{
    private Rect _clipRect;
    private bool _hasExplicitClipRect;
    private BorderRadius _borderRadius;

    public RenderClipRRect(RenderBox? child = null)
    {
        Child = child;
    }

    public Rect ClipRect
    {
        get => _clipRect;
        set
        {
            if (_hasExplicitClipRect && _clipRect == value)
            {
                return;
            }

            _clipRect = value;
            _hasExplicitClipRect = true;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public BorderRadius BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            if (Child != null)
            {
                MarkNeedsCompositedLayerUpdate();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public override bool IsRepaintBoundary => Child != null;
    protected override bool AlwaysNeedsCompositing => Child != null;

    protected override void PerformLayout()
    {
        var hadSize = HasSize;
        var previousSize = hadSize ? Size : default;
        base.PerformLayout();

        if (_hasExplicitClipRect || Child == null)
        {
            return;
        }

        if (!hadSize || previousSize != Size)
        {
            MarkNeedsCompositedLayerUpdate();
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as ClipRRectOffsetLayer ?? new ClipRRectOffsetLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        if (layer is ClipRRectOffsetLayer clipLayer)
        {
            clipLayer.ClipRect = _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
            clipLayer.BorderRadius = _borderRadius;
        }
    }

    protected override Rect? DescribeSemanticsClip(RenderObject? child)
    {
        return null;
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        return _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        var clip = _hasExplicitClipRect ? _clipRect : new Rect(new Point(0, 0), Size);
        if (!clip.Contains(position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }
}

public sealed class RenderPointerListener : RenderProxyBox
{
    private HitTestBehavior _behavior;

    public RenderPointerListener(
        Action<PointerDownEvent>? onPointerDown = null,
        Action<PointerMoveEvent>? onPointerMove = null,
        Action<PointerEnterEvent>? onPointerEnter = null,
        Action<PointerExitEvent>? onPointerExit = null,
        Action<PointerHoverEvent>? onPointerHover = null,
        Action<PointerUpEvent>? onPointerUp = null,
        Action<PointerCancelEvent>? onPointerCancel = null,
        Action<PointerSignalEvent>? onPointerSignal = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        RenderBox? child = null)
    {
        OnPointerDown = onPointerDown;
        OnPointerMove = onPointerMove;
        OnPointerEnter = onPointerEnter;
        OnPointerExit = onPointerExit;
        OnPointerHover = onPointerHover;
        OnPointerUp = onPointerUp;
        OnPointerCancel = onPointerCancel;
        OnPointerSignal = onPointerSignal;
        _behavior = behavior;
        Child = child;
    }

    public Action<PointerDownEvent>? OnPointerDown { get; set; }

    public Action<PointerMoveEvent>? OnPointerMove { get; set; }

    public Action<PointerEnterEvent>? OnPointerEnter { get; set; }

    public Action<PointerExitEvent>? OnPointerExit { get; set; }

    public Action<PointerHoverEvent>? OnPointerHover { get; set; }

    public Action<PointerUpEvent>? OnPointerUp { get; set; }

    public Action<PointerCancelEvent>? OnPointerCancel { get; set; }

    public Action<PointerSignalEvent>? OnPointerSignal { get; set; }

    public HitTestBehavior Behavior
    {
        get => _behavior;
        set => _behavior = value;
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (position.X < 0 || position.Y < 0 || position.X > Size.Width || position.Y > Size.Height)
        {
            return false;
        }

        var hitTarget = HitTestChildren(result, position) || HitTestSelf(position);
        if (hitTarget || Behavior == HitTestBehavior.Translucent || Behavior == HitTestBehavior.Opaque)
        {
            result.Add(new BoxHitTestEntry(this, position));
        }

        return hitTarget || Behavior == HitTestBehavior.Opaque || Behavior == HitTestBehavior.Translucent;
    }

    protected override bool HitTestSelf(Point position)
    {
        return Behavior == HitTestBehavior.Opaque;
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        switch (@event)
        {
            case PointerDownEvent downEvent:
                OnPointerDown?.Invoke(downEvent);
                break;
            case PointerMoveEvent moveEvent:
                OnPointerMove?.Invoke(moveEvent);
                break;
            case PointerEnterEvent enterEvent:
                OnPointerEnter?.Invoke(enterEvent);
                break;
            case PointerExitEvent exitEvent:
                OnPointerExit?.Invoke(exitEvent);
                break;
            case PointerHoverEvent hoverEvent:
                OnPointerHover?.Invoke(hoverEvent);
                break;
            case PointerUpEvent upEvent:
                OnPointerUp?.Invoke(upEvent);
                break;
            case PointerCancelEvent cancelEvent:
                OnPointerCancel?.Invoke(cancelEvent);
                break;
            case PointerSignalEvent signalEvent:
                OnPointerSignal?.Invoke(signalEvent);
                break;
        }
    }
}

public sealed class RenderInkSplash : RenderProxyBox
{
    private Color? _splashColor;
    private Point _splashOrigin;
    private double _splashProgress;
    private bool _clipToBounds = true;

    public RenderInkSplash(
        Color? splashColor = null,
        Point splashOrigin = default,
        double splashProgress = 0,
        bool clipToBounds = true,
        RenderBox? child = null)
    {
        _splashColor = splashColor;
        _splashOrigin = splashOrigin;
        _splashProgress = NormalizeProgress(splashProgress);
        _clipToBounds = clipToBounds;
        Child = child;
    }

    public Color? SplashColor
    {
        get => _splashColor;
        set
        {
            if (_splashColor == value)
            {
                return;
            }

            _splashColor = value;
            MarkNeedsPaint();
        }
    }

    public Point SplashOrigin
    {
        get => _splashOrigin;
        set
        {
            if (_splashOrigin == value)
            {
                return;
            }

            _splashOrigin = value;
            MarkNeedsPaint();
        }
    }

    public double SplashProgress
    {
        get => _splashProgress;
        set
        {
            var normalized = NormalizeProgress(value);
            if (Math.Abs(_splashProgress - normalized) < 0.0001)
            {
                return;
            }

            _splashProgress = normalized;
            MarkNeedsPaint();
        }
    }

    public bool ClipToBounds
    {
        get => _clipToBounds;
        set
        {
            if (_clipToBounds == value)
            {
                return;
            }

            _clipToBounds = value;
            MarkNeedsPaint();
        }
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_clipToBounds)
        {
            var clipRect = new Rect(offset, Size);
            ctx.PushClipRect(clipRect, clippedContext =>
            {
                PaintSplash(clippedContext, offset);
                base.Paint(clippedContext, offset);
            });
            return;
        }

        PaintSplash(ctx, offset);
        base.Paint(ctx, offset);
    }

    private void PaintSplash(PaintingContext ctx, Point offset)
    {
        if (!_splashColor.HasValue || _splashProgress <= 0)
        {
            return;
        }

        var resolvedOrigin = ResolveOrigin(Size, _splashOrigin);
        var localMaxRadius = Math.Sqrt((Size.Width * Size.Width) + (Size.Height * Size.Height));
        var radius = localMaxRadius * _splashProgress;

        var brush = new SolidColorBrush(_splashColor.Value);
        ctx.DrawCircle(brush, pen: null, center: offset + resolvedOrigin, radius: radius);
    }

    private static double NormalizeProgress(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static Point ResolveOrigin(Size size, Point origin)
    {
        var center = new Point(size.Width / 2, size.Height / 2);

        var x = double.IsNaN(origin.X) || double.IsInfinity(origin.X)
            ? center.X
            : origin.X;
        var y = double.IsNaN(origin.Y) || double.IsInfinity(origin.Y)
            ? center.Y
            : origin.Y;

        return new Point(x, y);
    }
}
