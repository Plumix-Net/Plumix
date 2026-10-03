using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/interactive_viewer.dart

namespace Plumix.Widgets;

/// <summary>
/// A signature for widget builders that take a <see cref="Quad"/> of the current viewport.
/// </summary>
/// <remarks>Dart's <c>InteractiveViewerWidgetBuilder</c>.</remarks>
public delegate Widget InteractiveViewerWidgetBuilder(BuildContext context, Quad viewport);

/// <summary>
/// A widget that enables pan and zoom interactions with its child.
/// </summary>
/// <remarks>
/// The user can transform the child by dragging to pan or pinching to zoom. By default the child is
/// constrained to the viewport; set <see cref="Constrained"/> to <see langword="false"/> (or use
/// <see cref="Builder"/>) for children larger than the viewport. Dart's <c>InteractiveViewer</c>; its <c>InteractiveViewer.builder</c> named constructor
/// is the static <see cref="Builder"/> factory.
/// </remarks>
public sealed class InteractiveViewer : StatefulWidget
{
    // This value was eyeballed to give a feel similar to Google Photos.
    private const double KDrag = 0.0000135;

    /// <summary>Creates an interactive viewer around <paramref name="child"/>.</summary>
    public InteractiveViewer(
        Widget child,
        Clip clipBehavior = Clip.HardEdge,
        PanAxis panAxis = PanAxis.Free,
        EdgeInsets? boundaryMargin = null,
        bool constrained = true,
        // These default scale values were eyeballed as reasonable limits for common use cases.
        double maxScale = 2.5,
        double minScale = 0.8,
        double interactionEndFrictionCoefficient = KDrag,
        Action<ScaleEndDetails>? onInteractionEnd = null,
        Action<ScaleStartDetails>? onInteractionStart = null,
        Action<ScaleUpdateDetails>? onInteractionUpdate = null,
        bool panEnabled = true,
        bool scaleEnabled = true,
        double scaleFactor = ScaleGestureRecognizer.KDefaultMouseScrollToScaleFactor,
        TransformationController? transformationController = null,
        Alignment? alignment = null,
        bool trackpadScrollCausesScale = false,
        Key? key = null)
        : this(
            child ?? throw new ArgumentNullException(nameof(child)),
            builder: null,
            clipBehavior,
            panAxis,
            boundaryMargin ?? EdgeInsets.Zero,
            constrained,
            maxScale,
            minScale,
            interactionEndFrictionCoefficient,
            onInteractionEnd,
            onInteractionStart,
            onInteractionUpdate,
            panEnabled,
            scaleEnabled,
            scaleFactor,
            transformationController,
            alignment,
            trackpadScrollCausesScale,
            key)
    {
    }

    private InteractiveViewer(
        Widget? child,
        InteractiveViewerWidgetBuilder? builder,
        Clip clipBehavior,
        PanAxis panAxis,
        EdgeInsets boundaryMargin,
        bool constrained,
        double maxScale,
        double minScale,
        double interactionEndFrictionCoefficient,
        Action<ScaleEndDetails>? onInteractionEnd,
        Action<ScaleStartDetails>? onInteractionStart,
        Action<ScaleUpdateDetails>? onInteractionUpdate,
        bool panEnabled,
        bool scaleEnabled,
        double scaleFactor,
        TransformationController? transformationController,
        Alignment? alignment,
        bool trackpadScrollCausesScale,
        Key? key) : base(key)
    {
        DebugAssertions.Assert(minScale > 0);
        DebugAssertions.Assert(interactionEndFrictionCoefficient > 0);
        DebugAssertions.Assert(double.IsFinite(minScale));
        DebugAssertions.Assert(maxScale > 0);
        DebugAssertions.Assert(!double.IsNaN(maxScale));
        DebugAssertions.Assert(maxScale >= minScale);
        // Boundary margin must be either fully infinite or fully finite, but not a mix of both.
        DebugAssertions.Assert(
            (double.IsInfinity(boundaryMargin.Horizontal) && double.IsInfinity(boundaryMargin.Vertical))
            || (double.IsFinite(boundaryMargin.Top)
                && double.IsFinite(boundaryMargin.Right)
                && double.IsFinite(boundaryMargin.Bottom)
                && double.IsFinite(boundaryMargin.Left)));
        Child = child;
        Builder_ = builder;
        ClipBehavior = clipBehavior;
        PanAxis = panAxis;
        BoundaryMargin = boundaryMargin;
        Constrained = constrained;
        MaxScale = maxScale;
        MinScale = minScale;
        InteractionEndFrictionCoefficient = interactionEndFrictionCoefficient;
        OnInteractionEnd = onInteractionEnd;
        OnInteractionStart = onInteractionStart;
        OnInteractionUpdate = onInteractionUpdate;
        PanEnabled = panEnabled;
        ScaleEnabled = scaleEnabled;
        ScaleFactor = scaleFactor;
        TransformationController = transformationController;
        Alignment = alignment;
        TrackpadScrollCausesScale = trackpadScrollCausesScale;
    }

    /// <summary>
    /// Creates an interactive viewer whose child is rebuilt from the visible <see cref="Quad"/> of the
    /// viewport, so that only what is visible needs to be built. The child is never constrained.
    /// </summary>
    /// <remarks>Dart's <c>InteractiveViewer.builder</c>.</remarks>
    public static InteractiveViewer Builder(
        InteractiveViewerWidgetBuilder builder,
        Clip clipBehavior = Clip.HardEdge,
        PanAxis panAxis = PanAxis.Free,
        EdgeInsets? boundaryMargin = null,
        // These default scale values were eyeballed as reasonable limits for common use cases.
        double maxScale = 2.5,
        double minScale = 0.8,
        double? interactionEndFrictionCoefficient = null,
        Action<ScaleEndDetails>? onInteractionEnd = null,
        Action<ScaleStartDetails>? onInteractionStart = null,
        Action<ScaleUpdateDetails>? onInteractionUpdate = null,
        bool panEnabled = true,
        bool scaleEnabled = true,
        double scaleFactor = 200.0,
        TransformationController? transformationController = null,
        Alignment? alignment = null,
        bool trackpadScrollCausesScale = false,
        Key? key = null) =>
        new(
            child: null,
            builder ?? throw new ArgumentNullException(nameof(builder)),
            clipBehavior,
            panAxis,
            boundaryMargin ?? EdgeInsets.Zero,
            constrained: false,
            maxScale,
            minScale,
            interactionEndFrictionCoefficient ?? KDrag,
            onInteractionEnd,
            onInteractionStart,
            onInteractionUpdate,
            panEnabled,
            scaleEnabled,
            scaleFactor,
            transformationController,
            alignment,
            trackpadScrollCausesScale,
            key);

    /// <summary>The alignment of the child's origin, relative to the size of the box.</summary>
    public Alignment? Alignment { get; }

    /// <summary>
    /// If set to <see cref="Clip.None"/>, the child may extend beyond the size of the
    /// InteractiveViewer, but it will not receive gestures in these areas. Defaults to
    /// <see cref="Clip.HardEdge"/>.
    /// </summary>
    public Clip ClipBehavior { get; }

    /// <summary>When set to <see cref="PanAxis.Aligned"/>, panning is only allowed along one axis.</summary>
    public PanAxis PanAxis { get; }

    /// <summary>
    /// A margin for the visible boundaries of the child. Any transformation that results in the
    /// viewport being able to view outside of the boundaries will be stopped at the boundary. Must be
    /// either fully finite or fully infinite (<c>EdgeInsets.All(double.PositiveInfinity)</c>).
    /// </summary>
    public EdgeInsets BoundaryMargin { get; }

    /// <summary>
    /// Builds the child of this widget from the visible viewport; set only by <see cref="Builder"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>builder</c> field. C# cannot give a property the name of the static
    /// <see cref="Builder"/> factory, so it carries a trailing underscore.
    /// </remarks>
    public InteractiveViewerWidgetBuilder? Builder_ { get; }

    /// <summary>The child widget that is transformed by the InteractiveViewer.</summary>
    public Widget? Child { get; }

    /// <summary>
    /// Whether the normal size constraints at this point in the widget tree are applied to the child.
    /// </summary>
    public bool Constrained { get; }

    /// <summary>If false, the user will be prevented from panning.</summary>
    public bool PanEnabled { get; }

    /// <summary>If false, the user will be prevented from scaling.</summary>
    public bool ScaleEnabled { get; }

    /// <summary>
    /// Whether scrolling up/down on a trackpad should cause scaling instead of panning.
    /// </summary>
    public bool TrackpadScrollCausesScale { get; }

    /// <summary>Determines the amount of scale to be performed per pointer scroll.</summary>
    public double ScaleFactor { get; }

    /// <summary>The maximum allowed scale. Must be greater than or equal to <see cref="MinScale"/>.</summary>
    public double MaxScale { get; }

    /// <summary>The minimum allowed scale. Must be a finite number greater than zero.</summary>
    public double MinScale { get; }

    /// <summary>
    /// Changes the deceleration behavior after a gesture. Defaults to 0.0000135.
    /// </summary>
    public double InteractionEndFrictionCoefficient { get; }

    /// <summary>Called when the user ends a pan or scale gesture on the widget.</summary>
    public Action<ScaleEndDetails>? OnInteractionEnd { get; }

    /// <summary>Called when the user begins a pan or scale gesture on the widget.</summary>
    public Action<ScaleStartDetails>? OnInteractionStart { get; }

    /// <summary>Called when the user updates a pan or scale gesture on the widget.</summary>
    public Action<ScaleUpdateDetails>? OnInteractionUpdate { get; }

    /// <summary>
    /// A <see cref="Widgets.TransformationController"/> for the transformation performed on the child.
    /// </summary>
    public TransformationController? TransformationController { get; }

    /// <summary>
    /// Returns the closest point to the given point on the given line segment.
    /// </summary>
    /// <remarks>Dart's <c>@visibleForTesting</c> static <c>getNearestPointOnLine</c>.</remarks>
    public static Vector3 GetNearestPointOnLine(Vector3 point, Vector3 l1, Vector3 l2)
    {
        double lengthSquared = Math.Pow(l2.X - l1.X, 2.0) + Math.Pow(l2.Y - l1.Y, 2.0);

        // In this case, l1 == l2.
        if (lengthSquared == 0)
        {
            return l1;
        }

        // Calculate how far down the line segment the closest point is and return the point.
        Vector3 l1P = point - l1;
        Vector3 l1L2 = l2 - l1;
        double fraction = Math.Clamp(l1P.Dot(l1L2) / lengthSquared, 0.0, 1.0);
        return l1 + (l1L2 * fraction);
    }

    /// <summary>Given a quad, returns its axis aligned bounding box.</summary>
    /// <remarks>Dart's <c>@visibleForTesting</c> static <c>getAxisAlignedBoundingBox</c>.</remarks>
    public static Quad GetAxisAlignedBoundingBox(Quad quad)
    {
        double minX = Math.Min(quad.Point0.X, Math.Min(quad.Point1.X, Math.Min(quad.Point2.X, quad.Point3.X)));
        double minY = Math.Min(quad.Point0.Y, Math.Min(quad.Point1.Y, Math.Min(quad.Point2.Y, quad.Point3.Y)));
        double maxX = Math.Max(quad.Point0.X, Math.Max(quad.Point1.X, Math.Max(quad.Point2.X, quad.Point3.X)));
        double maxY = Math.Max(quad.Point0.Y, Math.Max(quad.Point1.Y, Math.Max(quad.Point2.Y, quad.Point3.Y)));
        return Quad.Points(
            new Vector3(minX, minY, 0),
            new Vector3(maxX, minY, 0),
            new Vector3(maxX, maxY, 0),
            new Vector3(minX, maxY, 0));
    }

    /// <summary>Returns true iff the point is inside the rectangle given by the Quad, inclusively.</summary>
    /// <remarks>
    /// Algorithm from https://math.stackexchange.com/a/190373. Dart's <c>@visibleForTesting</c> static
    /// <c>pointIsInside</c>.
    /// </remarks>
    public static bool PointIsInside(Vector3 point, Quad quad)
    {
        Vector3 aM = point - quad.Point0;
        Vector3 aB = quad.Point1 - quad.Point0;
        Vector3 aD = quad.Point3 - quad.Point0;

        double aMAB = aM.Dot(aB);
        double aBAB = aB.Dot(aB);
        double aMAD = aM.Dot(aD);
        double aDAD = aD.Dot(aD);

        return 0 <= aMAB && aMAB <= aBAB && 0 <= aMAD && aMAD <= aDAD;
    }

    /// <summary>
    /// Get the point inside (inclusively) the given Quad that is nearest to the given Vector3.
    /// </summary>
    /// <remarks>Dart's <c>@visibleForTesting</c> static <c>getNearestPointInside</c>.</remarks>
    public static Vector3 GetNearestPointInside(Vector3 point, Quad quad)
    {
        // If the point is inside the axis aligned bounding box, then it's ok where it is.
        if (PointIsInside(point, quad))
        {
            return point;
        }

        // Otherwise, return the nearest point on the quad.
        Vector3[] closestPoints =
        [
            GetNearestPointOnLine(point, quad.Point0, quad.Point1),
            GetNearestPointOnLine(point, quad.Point1, quad.Point2),
            GetNearestPointOnLine(point, quad.Point2, quad.Point3),
            GetNearestPointOnLine(point, quad.Point3, quad.Point0),
        ];
        double minDistance = double.PositiveInfinity;
        Vector3? closestOverall = null;
        foreach (Vector3 closePoint in closestPoints)
        {
            double distance = Math.Sqrt(Math.Pow(point.X - closePoint.X, 2) + Math.Pow(point.Y - closePoint.Y, 2));
            if (distance < minDistance)
            {
                minDistance = distance;
                closestOverall = closePoint;
            }
        }

        return closestOverall!;
    }

    public override State CreateState() => new InteractiveViewerState();

    // Dart's private top-level helpers, kept with the widget in source order.

    // Given a velocity and drag, calculate the time at which motion will come to a stop, within the
    // margin of effectivelyMotionless.
    private static double GetFinalTime(double velocity, double drag, double effectivelyMotionless = 10)
    {
        return Math.Log(effectivelyMotionless / velocity) / Math.Log(drag / 100);
    }

    // Return the translation from the given Matrix4 as an Offset.
    private static Point GetMatrixTranslation(Matrix4 matrix)
    {
        Vector3 nextTranslation = matrix.GetTranslation();
        return new Point(nextTranslation.X, nextTranslation.Y);
    }

    // Transform the four corners of the viewport by the inverse of the given matrix. This gives the
    // viewport after the child has been transformed by the given matrix. The viewport transforms as
    // the inverse of the child (i.e. moving the child left is equivalent to moving the viewport right).
    private static Quad TransformViewport(Matrix4 matrix, Rect viewport)
    {
        Matrix4 inverseMatrix = matrix.Clone();
        inverseMatrix.Invert();
        return Quad.Points(
            inverseMatrix.Transform3(new Vector3(viewport.TopLeft.X, viewport.TopLeft.Y, 0.0)),
            inverseMatrix.Transform3(new Vector3(viewport.TopRight.X, viewport.TopRight.Y, 0.0)),
            inverseMatrix.Transform3(new Vector3(viewport.BottomRight.X, viewport.BottomRight.Y, 0.0)),
            inverseMatrix.Transform3(new Vector3(viewport.BottomLeft.X, viewport.BottomLeft.Y, 0.0)));
    }

    // Find the axis aligned bounding box for the rect rotated about its center by the given amount.
    private static Quad GetAxisAlignedBoundingBoxWithRotation(LtrbRect rect, double rotation)
    {
        Matrix4 rotationMatrix = Matrix4.Identity();
        rotationMatrix.TranslateByDouble(rect.Width / 2, rect.Height / 2, 0, 1);
        rotationMatrix.RotateZ(rotation);
        rotationMatrix.TranslateByDouble(-rect.Width / 2, -rect.Height / 2, 0, 1);
        Quad boundariesRotated = Quad.Points(
            rotationMatrix.Transform3(new Vector3(rect.Left, rect.Top, 0.0)),
            rotationMatrix.Transform3(new Vector3(rect.Right, rect.Top, 0.0)),
            rotationMatrix.Transform3(new Vector3(rect.Right, rect.Bottom, 0.0)),
            rotationMatrix.Transform3(new Vector3(rect.Left, rect.Bottom, 0.0)));
        return GetAxisAlignedBoundingBox(boundariesRotated);
    }

    // Return the amount that viewport lies outside of boundary. If the viewport is completely
    // contained within the boundary (inclusively), then returns Offset.zero.
    private static Point ExceedsBy(Quad boundary, Quad viewport)
    {
        Vector3[] viewportPoints = [viewport.Point0, viewport.Point1, viewport.Point2, viewport.Point3];
        Point largestExcess = default;
        foreach (Vector3 point in viewportPoints)
        {
            Vector3 pointInside = GetNearestPointInside(point, boundary);
            var excess = new Point(pointInside.X - point.X, pointInside.Y - point.Y);
            if (Math.Abs(excess.X) > Math.Abs(largestExcess.X))
            {
                largestExcess = new Point(excess.X, largestExcess.Y);
            }

            if (Math.Abs(excess.Y) > Math.Abs(largestExcess.Y))
            {
                largestExcess = new Point(largestExcess.X, excess.Y);
            }
        }

        return Round(largestExcess);
    }

    // Round the output values. This works around a precision problem where values that should have
    // been zero were given as within 10^-10 of zero.
    private static Point Round(Point offset)
    {
        return new Point(
            double.Parse(Diagnostics.ToStringAsFixed(offset.X, 9), CultureInfo.InvariantCulture),
            double.Parse(Diagnostics.ToStringAsFixed(offset.Y, 9), CultureInfo.InvariantCulture));
    }

    // Align the given offset to the given axis by allowing movement only in the axis direction.
    private static Point AlignAxis(Point offset, Axis axis) => axis switch
    {
        Axis.Horizontal => new Point(offset.X, 0.0),
        Axis.Vertical => new Point(0.0, offset.Y),
        _ => throw new ArgumentOutOfRangeException(nameof(axis)),
    };

    // Given two points, return the axis where the distance between the points is greatest. If they
    // are equal, return null.
    private static Axis? GetPanAxis(Point point1, Point point2)
    {
        if (point1 == point2)
        {
            return null;
        }

        double x = point2.X - point1.X;
        double y = point2.Y - point1.Y;
        return Math.Abs(x) > Math.Abs(y) ? Axis.Horizontal : Axis.Vertical;
    }

    // A dart:ui Rect in Dart's left/top/right/bottom form. Avalonia's Rect stores x/y/width/height, so
    // the all-infinite boundary of an infinite boundaryMargin (LTRB -inf, -inf, +inf, +inf) would read
    // a NaN right/bottom edge; the boundary math needs Dart's edges, width and predicates.
    private readonly record struct LtrbRect(double Left, double Top, double Right, double Bottom)
    {
        public double Width => Right - Left;

        public double Height => Bottom - Top;

        // Dart's Rect.isEmpty.
        public bool IsEmpty => Left >= Right || Top >= Bottom;

        // Dart's Rect.isInfinite.
        public bool IsInfinite =>
            Left >= double.PositiveInfinity
            || Top >= double.PositiveInfinity
            || Right >= double.PositiveInfinity
            || Bottom >= double.PositiveInfinity;

        // Dart's Rect.isFinite.
        public bool IsFinite =>
            double.IsFinite(Left) && double.IsFinite(Top) && double.IsFinite(Right) && double.IsFinite(Bottom);
    }

    // The type of gesture the user is performing. Dart's private _GestureType.
    private enum GestureType
    {
        Pan,
        Scale,
        Rotate,
    }

    private sealed class InteractiveViewerState : State<InteractiveViewer>
    {
        private TransformationController? _transformerField;
        private readonly GlobalKey _childKey = new LabeledGlobalKey<State>(null);
        private readonly GlobalKey _parentKey = new LabeledGlobalKey<State>(null);
        private Animation<Point>? _animation;
        private Animation<double>? _scaleAnimation;
        private Point _scaleAnimationFocalPoint;
        private AnimationController _controller = null!;
        private AnimationController _scaleController = null!;
        private Axis? _currentAxis; // Used with panAxis.
        private Point? _referenceFocalPoint; // Point where the current gesture began.
        private double? _scaleStart; // Scale value at start of scaling gesture.
        private double? _rotationStart = 0.0; // Rotation at start of rotation gesture.
        private double _currentRotation; // Rotation of _transformationController.value.
        private GestureType? _gestureType;

        // TODO(justinmc): Add rotateEnabled parameter to the widget and remove this hardcoded value when
        // the rotation feature is implemented.
        // https://github.com/flutter/flutter/issues/57698
        private readonly bool _rotateEnabled = false;

        // Dart's `late TransformationController _transformer = widget.transformationController ??
        // TransformationController();`: initialized on first read.
        private TransformationController Transformer
        {
            get => _transformerField ??= Widget.TransformationController ?? new TransformationController();
            set => _transformerField = value;
        }

        // Used as the coefficient of friction in the inertial translation animation. This value was
        // eyeballed to give a feel similar to Google Photos.

        // The _boundaryRect is calculated by adding the boundaryMargin to the size of the child.
        private LtrbRect BoundaryRect
        {
            get
            {
                DebugAssertions.Assert(_childKey.CurrentContext is not null);
                DebugAssertions.Assert(!double.IsNaN(Widget.BoundaryMargin.Left));
                DebugAssertions.Assert(!double.IsNaN(Widget.BoundaryMargin.Right));
                DebugAssertions.Assert(!double.IsNaN(Widget.BoundaryMargin.Top));
                DebugAssertions.Assert(!double.IsNaN(Widget.BoundaryMargin.Bottom));

                var childRenderBox = (RenderBox)_childKey.CurrentContext!.FindRenderObject()!;
                Size childSize = childRenderBox.Size;
                // Dart's boundaryMargin.inflateRect(Offset.zero & childSize).
                var boundaryRect = new LtrbRect(
                    0.0 - Widget.BoundaryMargin.Left,
                    0.0 - Widget.BoundaryMargin.Top,
                    childSize.Width + Widget.BoundaryMargin.Right,
                    childSize.Height + Widget.BoundaryMargin.Bottom);
                DebugAssertions.Assert(
                    !boundaryRect.IsEmpty,
                    "InteractiveViewer's child must have nonzero dimensions.");
                // Boundaries that are partially infinite are not allowed because Matrix4's rotation
                // and translation methods don't handle infinites well.
                DebugAssertions.Assert(
                    boundaryRect.IsFinite
                    || (double.IsInfinity(boundaryRect.Left)
                        && double.IsInfinity(boundaryRect.Top)
                        && double.IsInfinity(boundaryRect.Right)
                        && double.IsInfinity(boundaryRect.Bottom)),
                    "boundaryRect must either be infinite in all directions or finite in all directions.");
                return boundaryRect;
            }
        }

        // The Rect representing the child's parent.
        private Rect Viewport
        {
            get
            {
                DebugAssertions.Assert(_parentKey.CurrentContext is not null);
                var parentRenderBox = (RenderBox)_parentKey.CurrentContext!.FindRenderObject()!;
                return new Rect(parentRenderBox.Size);
            }
        }

        // Return a new matrix representing the given matrix after applying the given translation.
        private Matrix4 MatrixTranslate(Matrix4 matrix, Point translation)
        {
            if (translation == default)
            {
                return matrix.Clone();
            }

            Point alignedTranslation;

            if (_currentAxis is not null)
            {
                alignedTranslation = Widget.PanAxis switch
                {
                    PanAxis.Horizontal => AlignAxis(translation, Axis.Horizontal),
                    PanAxis.Vertical => AlignAxis(translation, Axis.Vertical),
                    PanAxis.Aligned => AlignAxis(translation, _currentAxis.Value),
                    PanAxis.Free => translation,
                    _ => throw new InvalidOperationException(),
                };
            }
            else
            {
                alignedTranslation = translation;
            }

            Matrix4 nextMatrix = matrix.Clone();
            nextMatrix.TranslateByDouble(alignedTranslation.X, alignedTranslation.Y, 0, 1);

            // Transform the viewport to determine where its four corners will be after the child has
            // been transformed.
            Quad nextViewport = TransformViewport(nextMatrix, Viewport);

            // If the boundaries are infinite, then no need to check if the translation fits within
            // them.
            if (BoundaryRect.IsInfinite)
            {
                return nextMatrix;
            }

            // Expand the boundaries with rotation. This prevents the problem where a mismatch in
            // orientation between the viewport and boundaries effectively limits translation. With
            // this approach, all points that are visible with no rotation are visible after rotation.
            Quad boundariesAabbQuad = GetAxisAlignedBoundingBoxWithRotation(BoundaryRect, _currentRotation);

            // If the given translation fits completely within the boundaries, allow it.
            Point offendingDistance = ExceedsBy(boundariesAabbQuad, nextViewport);
            if (offendingDistance == default)
            {
                return nextMatrix;
            }

            // Desired translation goes out of bounds, so translate to the nearest in-bounds point
            // instead.
            Point nextTotalTranslation = GetMatrixTranslation(nextMatrix);
            double currentScale = matrix.GetMaxScaleOnAxis();
            var correctedTotalTranslation = new Point(
                nextTotalTranslation.X - (offendingDistance.X * currentScale),
                nextTotalTranslation.Y - (offendingDistance.Y * currentScale));
            // TODO(justinmc): This needs some work to handle rotation properly. The translation
            // should align with the corrected rotation.
            Matrix4 correctedMatrix = matrix.Clone();
            correctedMatrix.SetTranslation(new Vector3(correctedTotalTranslation.X, correctedTotalTranslation.Y, 0.0));

            // Double check that the corrected translation fits.
            Quad correctedViewport = TransformViewport(correctedMatrix, Viewport);
            Point offendingCorrectedDistance = ExceedsBy(boundariesAabbQuad, correctedViewport);
            if (offendingCorrectedDistance == default)
            {
                return correctedMatrix;
            }

            // If the corrected translation doesn't fit in either direction, don't allow any
            // translation at all. This happens when the viewport is larger than the entire boundary.
            if (offendingCorrectedDistance.X != 0.0 && offendingCorrectedDistance.Y != 0.0)
            {
                return matrix.Clone();
            }

            // Otherwise, allow translation in only the direction that fits. This happens when the
            // viewport is larger than the boundary in one direction.
            var unidirectionalCorrectedTotalTranslation = new Point(
                offendingCorrectedDistance.X == 0.0 ? correctedTotalTranslation.X : 0.0,
                offendingCorrectedDistance.Y == 0.0 ? correctedTotalTranslation.Y : 0.0);
            Matrix4 unidirectionalMatrix = matrix.Clone();
            unidirectionalMatrix.SetTranslation(new Vector3(
                unidirectionalCorrectedTotalTranslation.X,
                unidirectionalCorrectedTotalTranslation.Y,
                0.0));
            return unidirectionalMatrix;
        }

        // Return a new matrix representing the given matrix after applying the given scale.
        private Matrix4 MatrixScale(Matrix4 matrix, double scale)
        {
            if (scale == 1.0)
            {
                return matrix.Clone();
            }

            DebugAssertions.Assert(scale != 0.0);

            // Don't allow a scale that results in an overall scale beyond min/max scale.
            double currentScale = Transformer.Value.GetMaxScaleOnAxis();
            double totalScale = Math.Max(
                currentScale * scale,
                // Ensure that the scale cannot make the child so big that it can't fit inside the
                // boundaries (in either direction).
                Math.Max(Viewport.Width / BoundaryRect.Width, Viewport.Height / BoundaryRect.Height));
            double clampedTotalScale = Math.Clamp(totalScale, Widget.MinScale, Widget.MaxScale);
            double clampedScale = clampedTotalScale / currentScale;
            Matrix4 result = matrix.Clone();
            result.ScaleByDouble(clampedScale, clampedScale, clampedScale, 1);
            return result;
        }

        // Return a new matrix representing the given matrix after applying the given rotation.
        private Matrix4 MatrixRotate(Matrix4 matrix, double rotation, Point focalPoint)
        {
            if (rotation == 0)
            {
                return matrix.Clone();
            }

            Point focalPointScene = Transformer.ToScene(focalPoint);
            Matrix4 result = matrix.Clone();
            result.TranslateByDouble(focalPointScene.X, focalPointScene.Y, 0, 1);
            result.RotateZ(-rotation);
            result.TranslateByDouble(-focalPointScene.X, -focalPointScene.Y, 0, 1);
            return result;
        }

        // Returns true iff the given _GestureType is enabled.
        private bool GestureIsSupported(GestureType? gestureType) => gestureType switch
        {
            GestureType.Rotate => _rotateEnabled,
            GestureType.Scale => Widget.ScaleEnabled,
            _ => Widget.PanEnabled,
        };

        // Decide which type of gesture this is by comparing the amount of scale and rotation in the
        // gesture, if any. Scale starts at 1 and rotation starts at 0. Pan will have no scale and no
        // rotation because it uses only one finger.
        private GestureType GetGestureType(ScaleUpdateDetails details)
        {
            double scale = !Widget.ScaleEnabled ? 1.0 : details.Scale;
            double rotation = !_rotateEnabled ? 0.0 : details.Rotation;
            if (Math.Abs(scale - 1) > Math.Abs(rotation))
            {
                return GestureType.Scale;
            }
            else if (rotation != 0.0)
            {
                return GestureType.Rotate;
            }
            else
            {
                return GestureType.Pan;
            }
        }

        // Handle the start of a gesture. All of pan, scale, and rotate are handled with
        // GestureDetector's scale gesture.
        private void OnScaleStart(ScaleStartDetails details)
        {
            Widget.OnInteractionStart?.Invoke(details);

            if (_controller.IsAnimating)
            {
                _controller.Stop();
                _controller.Reset();
                _animation?.RemoveListener(HandleInertiaAnimation);
                _animation = null;
            }

            if (_scaleController.IsAnimating)
            {
                _scaleController.Stop();
                _scaleController.Reset();
                _scaleAnimation?.RemoveListener(HandleScaleAnimation);
                _scaleAnimation = null;
            }

            _gestureType = null;
            _currentAxis = null;
            _scaleStart = Transformer.Value.GetMaxScaleOnAxis();
            _referenceFocalPoint = Transformer.ToScene(details.LocalFocalPoint);
            _rotationStart = _currentRotation;
        }

        // Handle an update to an ongoing gesture. All of pan, scale, and rotate are handled with
        // GestureDetector's scale gesture.
        private void OnScaleUpdate(ScaleUpdateDetails details)
        {
            double scale = Transformer.Value.GetMaxScaleOnAxis();
            _scaleAnimationFocalPoint = details.LocalFocalPoint;
            Point focalPointScene = Transformer.ToScene(details.LocalFocalPoint);

            if (_gestureType == GestureType.Pan)
            {
                // When a gesture first starts, it sometimes has no change in scale and rotation
                // despite being a two-finger gesture. Here the gesture is allowed to be reinterpreted
                // as its correct type after originally being marked as a pan.
                _gestureType = GetGestureType(details);
            }
            else
            {
                _gestureType ??= GetGestureType(details);
            }

            if (!GestureIsSupported(_gestureType))
            {
                Widget.OnInteractionUpdate?.Invoke(details);
                return;
            }

            switch (_gestureType!.Value)
            {
                case GestureType.Scale:
                {
                    DebugAssertions.Assert(_scaleStart is not null);
                    // details.scale gives us the amount to change the scale as of the start of this
                    // gesture, so calculate the amount to scale as of the previous update call.
                    double desiredScale = _scaleStart!.Value * details.Scale;
                    double scaleChange = desiredScale / scale;
                    Transformer.Value = MatrixScale(Transformer.Value, scaleChange);

                    // While scaling, translate such that the user's two fingers stay on the same
                    // places in the scene. That means that the focal point of the scale should be on
                    // the same place in the scene before and after the scale.
                    Point focalPointSceneScaled = Transformer.ToScene(details.LocalFocalPoint);
                    Transformer.Value = MatrixTranslate(
                        Transformer.Value,
                        focalPointSceneScaled - _referenceFocalPoint!.Value);

                    // details.localFocalPoint should now be at the same location as the original
                    // _referenceFocalPoint point. If it's not, that's because the translate came in
                    // contact with a boundary. In that case, update _referenceFocalPoint so
                    // subsequent updates happen in relation to the new effective focal point.
                    Point focalPointSceneCheck = Transformer.ToScene(details.LocalFocalPoint);
                    if (Round(_referenceFocalPoint.Value) != Round(focalPointSceneCheck))
                    {
                        _referenceFocalPoint = focalPointSceneCheck;
                    }

                    break;
                }

                case GestureType.Rotate:
                {
                    if (details.Rotation == 0.0)
                    {
                        Widget.OnInteractionUpdate?.Invoke(details);
                        return;
                    }

                    double desiredRotation = _rotationStart!.Value + details.Rotation;
                    Transformer.Value = MatrixRotate(
                        Transformer.Value,
                        _currentRotation - desiredRotation,
                        details.LocalFocalPoint);
                    _currentRotation = desiredRotation;
                    break;
                }

                case GestureType.Pan:
                {
                    DebugAssertions.Assert(_referenceFocalPoint is not null);
                    // details may have a change in scale here when scaleEnabled is false. In an
                    // effort to keep the behavior similar whether or not scaleEnabled is true, these
                    // gestures are thrown away.
                    if (details.Scale != 1.0)
                    {
                        Widget.OnInteractionUpdate?.Invoke(details);
                        return;
                    }

                    _currentAxis ??= GetPanAxis(_referenceFocalPoint!.Value, focalPointScene);
                    // Translate so that the same point in the scene is underneath the focal point
                    // before and after the movement.
                    Point translationChange = focalPointScene - _referenceFocalPoint!.Value;
                    Transformer.Value = MatrixTranslate(Transformer.Value, translationChange);
                    _referenceFocalPoint = Transformer.ToScene(details.LocalFocalPoint);
                    break;
                }
            }

            Widget.OnInteractionUpdate?.Invoke(details);
        }

        // Handle the end of a gesture of _GestureType. All of pan, scale, and rotate are handled with
        // GestureDetector's scale gesture.
        private void OnScaleEnd(ScaleEndDetails details)
        {
            Widget.OnInteractionEnd?.Invoke(details);
            _scaleStart = null;
            _rotationStart = null;
            _referenceFocalPoint = null;

            _animation?.RemoveListener(HandleInertiaAnimation);
            _scaleAnimation?.RemoveListener(HandleScaleAnimation);
            _controller.Reset();
            _scaleController.Reset();

            if (!GestureIsSupported(_gestureType))
            {
                _currentAxis = null;
                return;
            }

            switch (_gestureType)
            {
                case GestureType.Pan:
                {
                    if (details.Velocity.PixelsPerSecond.Length < GestureConstants.MinFlingVelocity)
                    {
                        _currentAxis = null;
                        return;
                    }

                    Vector3 translationVector = Transformer.Value.GetTranslation();
                    var translation = new Point(translationVector.X, translationVector.Y);
                    var frictionSimulationX = new FrictionSimulation(
                        Widget.InteractionEndFrictionCoefficient,
                        translation.X,
                        details.Velocity.PixelsPerSecond.X);
                    var frictionSimulationY = new FrictionSimulation(
                        Widget.InteractionEndFrictionCoefficient,
                        translation.Y,
                        details.Velocity.PixelsPerSecond.Y);
                    double tFinal = GetFinalTime(
                        details.Velocity.PixelsPerSecond.Length,
                        Widget.InteractionEndFrictionCoefficient);
                    _animation = new Tween<Point>(
                            begin: translation,
                            end: new Point(frictionSimulationX.FinalX, frictionSimulationY.FinalX))
                        .Chain(new CurveTween(curve: Curves.Decelerate))
                        .Animate(_controller);
                    _controller.Duration = TimeSpan.FromMilliseconds(Math.Round(tFinal * 1000));
                    _animation.AddListener(HandleInertiaAnimation);
                    _controller.Forward();
                    break;
                }

                case GestureType.Scale:
                {
                    if (Math.Abs(details.ScaleVelocity) < 0.1)
                    {
                        _currentAxis = null;
                        return;
                    }

                    double scale = Transformer.Value.GetMaxScaleOnAxis();
                    var frictionSimulation = new FrictionSimulation(
                        Widget.InteractionEndFrictionCoefficient * Widget.ScaleFactor,
                        scale,
                        details.ScaleVelocity / 10);
                    double tFinal = GetFinalTime(
                        Math.Abs(details.ScaleVelocity),
                        Widget.InteractionEndFrictionCoefficient,
                        effectivelyMotionless: 0.1);
                    _scaleAnimation = new Tween<double>(begin: scale, end: frictionSimulation.X(tFinal))
                        .Chain(new CurveTween(curve: Curves.Decelerate))
                        .Animate(_scaleController);
                    _scaleController.Duration = TimeSpan.FromMilliseconds(Math.Round(tFinal * 1000));
                    _scaleAnimation.AddListener(HandleScaleAnimation);
                    _scaleController.Forward();
                    break;
                }

                case GestureType.Rotate:
                case null:
                    break;
            }
        }

        // Handle mousewheel and web trackpad scroll events.
        private void ReceivedPointerSignal(PointerSignalEvent @event)
        {
            Point local = @event.LocalPosition;
            Point global = @event.Position;
            double scaleChange;
            if (@event is PointerScrollEvent scrollEvent)
            {
                if (@event.Kind == PointerDeviceKind.Trackpad && !Widget.TrackpadScrollCausesScale)
                {
                    // Trackpad scroll, so treat it as a pan.
                    Widget.OnInteractionStart?.Invoke(
                        new ScaleStartDetails(focalPoint: global, localFocalPoint: local));

                    Point localDelta = PointerEvent.TransformDeltaViaPositions(
                        untransformedEndPosition: global + scrollEvent.ScrollDelta,
                        untransformedDelta: scrollEvent.ScrollDelta,
                        transform: @event.Transform);

                    if (!GestureIsSupported(GestureType.Pan))
                    {
                        Widget.OnInteractionUpdate?.Invoke(new ScaleUpdateDetails(
                            focalPoint: global - scrollEvent.ScrollDelta,
                            localFocalPoint: local - scrollEvent.ScrollDelta,
                            focalPointDelta: -localDelta));
                        Widget.OnInteractionEnd?.Invoke(new ScaleEndDetails());
                        return;
                    }

                    Point focalPointScene = Transformer.ToScene(local);
                    Point newFocalPointScene = Transformer.ToScene(local - localDelta);

                    Transformer.Value = MatrixTranslate(Transformer.Value, newFocalPointScene - focalPointScene);

                    Widget.OnInteractionUpdate?.Invoke(new ScaleUpdateDetails(
                        focalPoint: global - scrollEvent.ScrollDelta,
                        localFocalPoint: local - localDelta,
                        focalPointDelta: -localDelta));
                    Widget.OnInteractionEnd?.Invoke(new ScaleEndDetails());
                    return;
                }

                // Ignore left and right mouse wheel scroll.
                if (scrollEvent.ScrollDelta.Y == 0.0)
                {
                    return;
                }

                scaleChange = Math.Exp(-scrollEvent.ScrollDelta.Y / Widget.ScaleFactor);
            }
            else if (@event is PointerScaleEvent scaleEvent)
            {
                scaleChange = scaleEvent.Scale;
            }
            else
            {
                return;
            }

            Widget.OnInteractionStart?.Invoke(new ScaleStartDetails(focalPoint: global, localFocalPoint: local));

            if (!GestureIsSupported(GestureType.Scale))
            {
                Widget.OnInteractionUpdate?.Invoke(new ScaleUpdateDetails(
                    focalPoint: global,
                    localFocalPoint: local,
                    scale: scaleChange));
                Widget.OnInteractionEnd?.Invoke(new ScaleEndDetails());
                return;
            }

            Point focalPointSceneBefore = Transformer.ToScene(local);

            Transformer.Value = MatrixScale(Transformer.Value, scaleChange);

            // After scaling, translate such that the event's position is at the same scene point
            // before and after the scale.
            Point focalPointSceneScaled = Transformer.ToScene(local);
            Transformer.Value = MatrixTranslate(Transformer.Value, focalPointSceneScaled - focalPointSceneBefore);

            Widget.OnInteractionUpdate?.Invoke(new ScaleUpdateDetails(
                focalPoint: global,
                localFocalPoint: local,
                scale: scaleChange));
            Widget.OnInteractionEnd?.Invoke(new ScaleEndDetails());
        }

        // Handle inertia drag animation.
        private void HandleInertiaAnimation()
        {
            if (!_controller.IsAnimating)
            {
                _currentAxis = null;
                _animation?.RemoveListener(HandleInertiaAnimation);
                _animation = null;
                _controller.Reset();
                return;
            }

            // Translate such that the resulting translation is _animation.value.
            Vector3 translationVector = Transformer.Value.GetTranslation();
            var translation = new Point(translationVector.X, translationVector.Y);
            Transformer.Value = MatrixTranslate(
                Transformer.Value,
                Transformer.ToScene(_animation!.Value) - Transformer.ToScene(translation));
        }

        // Handle inertia scale animation.
        private void HandleScaleAnimation()
        {
            if (!_scaleController.IsAnimating)
            {
                _currentAxis = null;
                _scaleAnimation?.RemoveListener(HandleScaleAnimation);
                _scaleAnimation = null;
                _scaleController.Reset();
                return;
            }

            double desiredScale = _scaleAnimation!.Value;
            double scaleChange = desiredScale / Transformer.Value.GetMaxScaleOnAxis();
            Point referenceFocalPoint = Transformer.ToScene(_scaleAnimationFocalPoint);
            Transformer.Value = MatrixScale(Transformer.Value, scaleChange);

            // While scaling, translate such that the user's two fingers stay on the same places in
            // the scene. That means that the focal point of the scale should be on the same place in
            // the scene before and after the scale.
            Point focalPointSceneScaled = Transformer.ToScene(_scaleAnimationFocalPoint);
            Transformer.Value = MatrixTranslate(Transformer.Value, focalPointSceneScaled - referenceFocalPoint);
        }

        private void HandleTransformation()
        {
            // A change to the TransformationController's value is a change to the state.
            SetState(() => { });
        }

        public override void InitState()
        {
            base.InitState();
            _controller = new AnimationController(vsync: this);
            _scaleController = new AnimationController(vsync: this);

            Transformer.AddListener(HandleTransformation);
        }

        public override void DidUpdateWidget(InteractiveViewer oldWidget)
        {
            base.DidUpdateWidget(oldWidget);

            TransformationController? newController = Widget.TransformationController;
            if (ReferenceEquals(newController, oldWidget.TransformationController))
            {
                return;
            }

            Transformer.RemoveListener(HandleTransformation);
            if (oldWidget.TransformationController is null)
            {
                Transformer.Dispose();
            }

            Transformer = newController ?? new TransformationController();
            Transformer.AddListener(HandleTransformation);
        }

        public override void Dispose()
        {
            _controller.Dispose();
            _scaleController.Dispose();
            Transformer.RemoveListener(HandleTransformation);
            if (Widget.TransformationController is null)
            {
                Transformer.Dispose();
            }

            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            Widget child;
            if (Widget.Child is not null)
            {
                child = new InteractiveViewerBuilt(
                    childKey: _childKey,
                    clipBehavior: Widget.ClipBehavior,
                    constrained: Widget.Constrained,
                    matrix: Transformer.Value,
                    alignment: Widget.Alignment,
                    child: Widget.Child);
            }
            else
            {
                // When using InteractiveViewer.builder, then constrained is false and the viewport is
                // the size of the constraints.
                DebugAssertions.Assert(Widget.Builder_ is not null);
                DebugAssertions.Assert(!Widget.Constrained);
                child = new LayoutBuilder(
                    builder: (builderContext, constraints) =>
                    {
                        Matrix4 matrix = Transformer.Value;
                        return new InteractiveViewerBuilt(
                            childKey: _childKey,
                            clipBehavior: Widget.ClipBehavior,
                            constrained: Widget.Constrained,
                            alignment: Widget.Alignment,
                            matrix: matrix,
                            child: Widget.Builder_!(
                                builderContext,
                                TransformViewport(matrix, new Rect(constraints.Biggest))));
                    });
            }

            return new Listener(
                key: _parentKey,
                onPointerSignal: ReceivedPointerSignal,
                child: new GestureDetector(
                    // Necessary when panning off screen.
                    behavior: HitTestBehavior.Opaque,
                    onScaleEnd: OnScaleEnd,
                    onScaleStart: OnScaleStart,
                    onScaleUpdate: OnScaleUpdate,
                    trackpadScrollCausesScale: Widget.TrackpadScrollCausesScale,
                    trackpadScrollToScaleFactor: new Point(0, -1 / Widget.ScaleFactor),
                    child: child));
        }
    }

    // This class is private to InteractiveViewer in Dart (_InteractiveViewerBuilt).
    private sealed class InteractiveViewerBuilt : StatelessWidget
    {
        public InteractiveViewerBuilt(
            Widget child,
            GlobalKey childKey,
            Clip clipBehavior,
            bool constrained,
            Matrix4 matrix,
            Alignment? alignment)
        {
            Child = child;
            ChildKey = childKey;
            ClipBehavior = clipBehavior;
            Constrained = constrained;
            Matrix = matrix;
            Alignment = alignment;
        }

        public Widget Child { get; }

        public GlobalKey ChildKey { get; }

        public Clip ClipBehavior { get; }

        public bool Constrained { get; }

        public Matrix4 Matrix { get; }

        public Alignment? Alignment { get; }

        public override Widget Build(BuildContext context)
        {
            Widget child = new Transform(
                transform: Matrix,
                alignment: Alignment,
                child: new KeyedSubtree(key: ChildKey, child: Child));

            if (!Constrained)
            {
                child = new OverflowBox(
                    alignment: Rendering.Alignment.TopLeft,
                    minWidth: 0.0,
                    minHeight: 0.0,
                    maxWidth: double.PositiveInfinity,
                    maxHeight: double.PositiveInfinity,
                    child: child);
            }

            return new ClipRect(clipBehavior: ClipBehavior, child: child);
        }
    }
}

/// <summary>
/// A thin wrapper on <see cref="ValueNotifier{T}"/> whose value is a <see cref="Matrix4"/>
/// representing a transformation.
/// </summary>
/// <remarks>Dart's <c>TransformationController</c>.</remarks>
public class TransformationController : ValueNotifier<Matrix4>
{
    /// <summary>Create an instance of TransformationController; defaults to the identity.</summary>
    public TransformationController(Matrix4? value = null) : base(value ?? Matrix4.Identity())
    {
    }

    /// <summary>
    /// Return the scene point at the given viewport point: the inverse of the current transformation
    /// applied to it.
    /// </summary>
    public Point ToScene(Point viewportPoint)
    {
        // On viewportPoint, perform the inverse transformation of the scene to get where the point
        // would be in the scene before the transformation.
        Matrix4 inverseMatrix = Matrix4.Inverted(Value);
        Vector3 untransformed = inverseMatrix.Transform3(new Vector3(viewportPoint.X, viewportPoint.Y, 0));
        return new Point(untransformed.X, untransformed.Y);
    }
}

/// <summary>
/// This enum is used to specify the behavior of the <see cref="InteractiveViewer"/> when the user
/// drags the viewport.
/// </summary>
public enum PanAxis
{
    /// <summary>The user can only pan the viewport along the horizontal axis.</summary>
    Horizontal,

    /// <summary>The user can only pan the viewport along the vertical axis.</summary>
    Vertical,

    /// <summary>
    /// The user can pan the viewport along the horizontal or vertical axis, but not diagonally.
    /// </summary>
    Aligned,

    /// <summary>The user can pan the viewport freely in any direction.</summary>
    Free,
}
