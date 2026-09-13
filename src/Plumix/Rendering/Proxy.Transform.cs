using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

namespace Plumix.Rendering;

/// <summary>Applies a transformation before painting its child.</summary>
public class RenderTransform : RenderProxyBox
{
    private Point? _origin;
    private AlignmentGeometry? _alignment;
    private TextDirection? _textDirection;
    private Matrix4? _transform;
    private FilterQuality? _filterQuality;

    /// <summary>Creates a render object that transforms its child.</summary>
    public RenderTransform(
        Matrix4 transform,
        Point? origin = null,
        AlignmentGeometry? alignment = null,
        TextDirection? textDirection = null,
        bool transformHitTests = true,
        FilterQuality? filterQuality = null,
        RenderBox? child = null) : base(child)
    {
        TransformHitTests = transformHitTests;
        Transform = transform;
        Alignment = alignment;
        TextDirection = textDirection;
        FilterQuality = filterQuality;
        Origin = origin;
    }

    /// <summary>
    /// The origin of the coordinate system (relative to the upper left corner of this render object)
    /// in which to apply the matrix.
    /// </summary>
    public Point? Origin
    {
        get => _origin;
        set
        {
            if (_origin == value)
            {
                return;
            }

            _origin = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>The alignment of the origin, relative to the size of the box.</summary>
    public AlignmentGeometry? Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>The text direction with which to resolve <see cref="Alignment"/>.</summary>
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
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <inheritdoc />
    public override bool AlwaysNeedsCompositing => Child != null && _filterQuality != null;

    /// <summary>
    /// When set to true, hit tests are performed based on the position of the child as it is
    /// painted. When set to false, hit tests are performed as if the child was not transformed.
    /// </summary>
    public bool TransformHitTests { get; set; }

    /// <summary>The matrix to transform the child by during painting. The provided value is copied.</summary>
    /// <remarks>Dart has a setter only, so the live matrix never escapes.</remarks>
    public Matrix4 Transform
    {
        set
        {
            if (_transform == value)
            {
                return;
            }

            _transform = Matrix4.Copy(value);
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>
    /// The filter quality with which to apply the transform as a bitmap operation.
    /// </summary>
    public FilterQuality? FilterQuality
    {
        get => _filterQuality;
        set
        {
            if (_filterQuality == value)
            {
                return;
            }

            bool didNeedCompositing = AlwaysNeedsCompositing;
            _filterQuality = value;
            if (didNeedCompositing != AlwaysNeedsCompositing)
            {
                MarkNeedsCompositingBitsUpdate();
            }

            MarkNeedsPaint();
        }
    }

    /// <summary>Sets the transform to the identity matrix.</summary>
    public void SetIdentity()
    {
        _transform!.SetIdentity();
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Concatenates a rotation about the x axis into the transform.</summary>
    public void RotateX(double radians)
    {
        _transform!.RotateX(radians);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Concatenates a rotation about the y axis into the transform.</summary>
    public void RotateY(double radians)
    {
        _transform!.RotateY(radians);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Concatenates a rotation about the z axis into the transform.</summary>
    public void RotateZ(double radians)
    {
        _transform!.RotateZ(radians);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Concatenates a translation by (x, y, z) into the transform.</summary>
    public void Translate(double x, double y = 0.0, double z = 0.0)
    {
        _transform!.TranslateByDouble(x, y, z, 1);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    /// <summary>Concatenates a scale into the transform.</summary>
    public void Scale(double x, double? y = null, double? z = null)
    {
        _transform!.ScaleByDouble(x, y ?? x, z ?? x, 1);
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }

    private Matrix4? EffectiveTransform
    {
        get
        {
            Alignment? resolvedAlignment = Alignment?.Resolve(TextDirection);
            if (_origin == null && resolvedAlignment == null)
            {
                return _transform;
            }

            Matrix4 result = Matrix4.Identity();
            if (_origin != null)
            {
                result.TranslateByDouble(_origin.Value.X, _origin.Value.Y, 0, 1);
            }

            Point? translation = null;
            if (resolvedAlignment != null)
            {
                translation = resolvedAlignment.Value.AlongSize(Size);
                result.TranslateByDouble(translation.Value.X, translation.Value.Y, 0, 1);
            }

            result.Multiply(_transform!);
            if (resolvedAlignment != null)
            {
                result.TranslateByDouble(-translation!.Value.X, -translation.Value.Y, 0, 1);
            }

            if (_origin != null)
            {
                result.TranslateByDouble(-_origin.Value.X, -_origin.Value.Y, 0, 1);
            }

            return result;
        }
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        // RenderTransform objects don't check if they are themselves hit, because it's confusing to
        // think about how the untransformed size and the child's transformed position interact.
        return HitTestChildren(result, position);
    }

    /// <inheritdoc />
    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        Debug.Assert(!TransformHitTests || EffectiveTransform != null);
        return result.AddWithPaintTransform(
            TransformHitTests ? EffectiveTransform : null,
            position,
            base.HitTestChildren);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child != null)
        {
            Matrix4 transform = EffectiveTransform!;
            if (FilterQuality == null)
            {
                Point? childOffset = MatrixUtils.GetAsTranslation(transform);
                if (childOffset == null)
                {
                    // If the transform has a singular value, then no layer will be created and the
                    // child will not be painted.
                    double det = transform.Determinant();
                    if (det == 0 || !double.IsFinite(det))
                    {
                        Layer = null;
                        return;
                    }

                    Layer = context.PushTransform(
                        NeedsCompositing,
                        offset,
                        transform,
                        base.Paint,
                        oldLayer: Layer as TransformLayer);
                }
                else
                {
                    base.Paint(context, offset + childOffset.Value);
                    Layer = null;
                }
            }
            else
            {
                Matrix4 effectiveTransform = Matrix4.TranslationValues(offset.X, offset.Y, 0.0);
                effectiveTransform.Multiply(transform);
                effectiveTransform.TranslateByDouble(-offset.X, -offset.Y, 0, 1);
                var filter = new ImageFilter.Matrix(effectiveTransform.Storage, filterQuality: FilterQuality.Value);
                if (Layer is ImageFilterLayer filterLayer)
                {
                    filterLayer.ImageFilter = filter;
                }
                else
                {
                    Layer = new ImageFilterLayer { ImageFilter = filter };
                }

                var imageFilterLayer = (ImageFilterLayer)Layer;
                // Plumix-only: the CPU rasterizer needs the region the child paints into.
                imageFilterLayer.FilterBounds = new Rect(offset, Size);
                context.PushLayer(imageFilterLayer, base.Paint, offset);
                if (Constants.KDebugMode)
                {
                    Layer!.DebugCreator = DebugCreator;
                }
            }
        }
    }

    /// <inheritdoc />
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        transform.Multiply(EffectiveTransform!);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new TransformProperty("transform matrix", _transform));
        properties.Add(new DiagnosticsProperty<Point?>("origin", Origin));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
        properties.Add(new DiagnosticsProperty<bool>("transformHitTests", TransformHitTests));
    }
}

/// <summary>Scales and positions its child within itself according to <see cref="Fit"/>.</summary>
public class RenderFittedBox : RenderProxyBox
{
    private BoxFit _fit;
    private AlignmentGeometry _alignment;
    private TextDirection? _textDirection;
    private Alignment? _resolvedAlignment;
    private Clip _clipBehavior;
    private bool? _hasVisualOverflow;
    private Matrix4? _transform;

    /// <summary>Scales and positions its child within itself.</summary>
    public RenderFittedBox(
        BoxFit fit = BoxFit.Contain,
        AlignmentGeometry alignment = default,
        TextDirection? textDirection = null,
        RenderBox? child = null,
        Clip clipBehavior = Clip.None) : base(child)
    {
        _fit = fit;
        _alignment = alignment;
        _textDirection = textDirection;
        _clipBehavior = clipBehavior;
    }

    private Alignment Resolve() => _resolvedAlignment ??= _alignment.Resolve(_textDirection);

    private void MarkNeedResolution()
    {
        _resolvedAlignment = null;
        MarkNeedsPaint();
    }

    private static bool FitAffectsLayout(BoxFit fit)
    {
        switch (fit)
        {
            case BoxFit.ScaleDown:
                return true;
            case BoxFit.Contain:
            case BoxFit.Cover:
            case BoxFit.Fill:
            case BoxFit.FitHeight:
            case BoxFit.FitWidth:
            case BoxFit.None:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(fit), fit, null);
        }
    }

    /// <summary>How to inscribe the child into the space allocated during layout.</summary>
    public BoxFit Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            BoxFit lastFit = _fit;
            _fit = value;
            if (FitAffectsLayout(lastFit) || FitAffectsLayout(value))
            {
                MarkNeedsLayout();
            }
            else
            {
                ClearPaintData();
                MarkNeedsPaint();
            }
        }
    }

    /// <summary>How to align the child within its parent's bounds.</summary>
    public AlignmentGeometry Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            ClearPaintData();
            MarkNeedResolution();
        }
    }

    /// <summary>The text direction with which to resolve <see cref="Alignment"/>.</summary>
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
            ClearPaintData();
            MarkNeedResolution();
        }
    }

    /// <inheritdoc />
    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child != null)
        {
            Size childSize = Child.GetDryLayout(BoxConstraints.Unbounded);

            switch (_fit)
            {
                case BoxFit.ScaleDown:
                    BoxConstraints sizeConstraints = constraints.Loosen();
                    Size unconstrainedSize = sizeConstraints.ConstrainSizeAndAttemptToPreserveAspectRatio(childSize);
                    return constraints.Constrain(unconstrainedSize);
                default:
                    return constraints.ConstrainSizeAndAttemptToPreserveAspectRatio(childSize);
            }
        }

        return constraints.Smallest;
    }

    /// <inheritdoc />
    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        return Child?.GetDryBaseline(BoxConstraints.Unbounded, baseline);
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        if (Child != null)
        {
            Child.Layout(BoxConstraints.Unbounded, parentUsesSize: true);
            switch (_fit)
            {
                case BoxFit.ScaleDown:
                    BoxConstraints sizeConstraints = Constraints.Loosen();
                    Size unconstrainedSize =
                        sizeConstraints.ConstrainSizeAndAttemptToPreserveAspectRatio(Child.Size);
                    Size = Constraints.Constrain(unconstrainedSize);
                    break;
                default:
                    Size = Constraints.ConstrainSizeAndAttemptToPreserveAspectRatio(Child.Size);
                    break;
            }

            ClearPaintData();
        }
        else
        {
            Size = Constraints.Smallest;
        }
    }

    /// <summary>
    /// Defaults to <see cref="Clip.None"/>.
    /// </summary>
    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (value != _clipBehavior)
            {
                _clipBehavior = value;
                MarkNeedsPaint();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    private void ClearPaintData()
    {
        _hasVisualOverflow = null;
        _transform = null;
    }

    private void UpdatePaintData()
    {
        if (_transform != null)
        {
            return;
        }

        if (Child == null)
        {
            _hasVisualOverflow = false;
            _transform = Matrix4.Identity();
        }
        else
        {
            Alignment resolvedAlignment = Resolve();
            Size childSize = Child.Size;
            FittedSizes sizes = BoxFitUtils.ApplyBoxFit(_fit, childSize, Size);
            double scaleX = sizes.Destination.Width / sizes.Source.Width;
            double scaleY = sizes.Destination.Height / sizes.Source.Height;
            Rect sourceRect = resolvedAlignment.Inscribe(sizes.Source, new Rect(new Point(0, 0), childSize));
            Rect destinationRect = resolvedAlignment.Inscribe(sizes.Destination, new Rect(new Point(0, 0), Size));
            _hasVisualOverflow = sourceRect.Width < childSize.Width || sourceRect.Height < childSize.Height;
            Debug.Assert(double.IsFinite(scaleX) && double.IsFinite(scaleY));
            _transform = Matrix4.TranslationValues(destinationRect.Left, destinationRect.Top, 0.0);
            _transform.ScaleByDouble(scaleX, scaleY, 1.0, 1);
            _transform.TranslateByDouble(-sourceRect.Left, -sourceRect.Top, 0, 1);
            Debug.Assert(_transform.Storage.All(double.IsFinite));
        }
    }

    private TransformLayer? PaintChildWithTransform(PaintingContext context, Point offset)
    {
        Point? childOffset = MatrixUtils.GetAsTranslation(_transform!);
        if (childOffset == null)
        {
            return context.PushTransform(
                NeedsCompositing,
                offset,
                _transform!,
                base.Paint,
                oldLayer: Layer as TransformLayer);
        }

        base.Paint(context, offset + childOffset.Value);
        return null;
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child == null || Size.IsEmpty || Child.Size.IsEmpty)
        {
            return;
        }

        UpdatePaintData();
        Debug.Assert(Child != null);
        if (_hasVisualOverflow!.Value && ClipBehavior != Clip.None)
        {
            Layer = context.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(new Point(0, 0), Size),
                (clipContext, clipOffset) => PaintChildWithTransform(clipContext, clipOffset),
                clipBehavior: ClipBehavior,
                oldLayer: Layer as ClipRectLayer);
        }
        else
        {
            Layer = PaintChildWithTransform(context, offset);
        }
    }

    /// <inheritdoc />
    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Size.IsEmpty || (Child?.Size.IsEmpty ?? false))
        {
            return false;
        }

        UpdatePaintData();
        return result.AddWithPaintTransform(_transform, position, base.HitTestChildren);
    }

    /// <inheritdoc />
    /// <remarks>Dart's parameter is <c>covariant RenderBox child</c>.</remarks>
    public override bool PaintsChild(RenderObject child)
    {
        Debug.Assert(ReferenceEquals(child.Parent, this));
        return !Size.IsEmpty && !((RenderBox)child).Size.IsEmpty;
    }

    /// <inheritdoc />
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        if (!PaintsChild(child))
        {
            transform.SetZero();
        }
        else
        {
            UpdatePaintData();
            transform.Multiply(_transform!);
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<BoxFit>("fit", Fit));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
    }
}

/// <summary>Applies a translation transformation before painting its child.</summary>
/// <remarks>
/// The translation is expressed as a <see cref="Point"/> (Dart's <c>Offset</c>) scaled to the child's
/// size.
/// </remarks>
public class RenderFractionalTranslation : RenderProxyBox
{
    private Point _translation;

    /// <summary>Creates a render object that translates its child's painting.</summary>
    public RenderFractionalTranslation(
        Point translation,
        bool transformHitTests = true,
        RenderBox? child = null) : base(child)
    {
        TransformHitTests = transformHitTests;
        _translation = translation;
    }

    /// <summary>The translation to apply to the child, scaled to the child's size.</summary>
    public Point Translation
    {
        get => _translation;
        set
        {
            if (_translation == value)
            {
                return;
            }

            _translation = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <inheritdoc />
    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        // RenderFractionalTranslation objects don't check if they are themselves hit, because it's
        // confusing to think about how the untransformed size and the child's transformed position
        // interact.
        return HitTestChildren(result, position);
    }

    /// <summary>
    /// When set to true, hit tests are performed based on the position of the child as it is
    /// painted. When set to false, hit tests are performed as if the child was not transformed.
    /// </summary>
    public bool TransformHitTests { get; set; }

    /// <inheritdoc />
    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        Debug.Assert(!DebugNeedsLayout);
        return result.AddWithPaintOffset(
            TransformHitTests
                ? new Point(Translation.X * Size.Width, Translation.Y * Size.Height)
                : null,
            position,
            base.HitTestChildren);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        Debug.Assert(!DebugNeedsLayout);
        if (Child != null)
        {
            base.Paint(
                context,
                new Point(
                    offset.X + Translation.X * Size.Width,
                    offset.Y + Translation.Y * Size.Height));
        }
    }

    /// <inheritdoc />
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        transform.TranslateByDouble(Translation.X * Size.Width, Translation.Y * Size.Height, 0, 1);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Point>("translation", Translation));
        properties.Add(new DiagnosticsProperty<bool>("transformHitTests", TransformHitTests));
    }
}
