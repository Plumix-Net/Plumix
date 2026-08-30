using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using RelativeRect = Plumix.Rendering.RelativeRect;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

namespace Plumix.Widgets;

/// <summary>A widget that makes its child partially transparent.</summary>
public sealed class Opacity : SingleChildRenderObjectWidget
{
    public Opacity(double opacity, Widget? child = null, Key? key = null)
        : this(opacity, child, alwaysIncludeSemantics: false, key)
    {
    }

    public Opacity(
        double opacity,
        Widget? child,
        bool alwaysIncludeSemantics,
        Key? key = null) : base(child, key)
    {
        if (Constants.KDebugMode && !(opacity >= 0.0 && opacity <= 1.0))
        {
            throw new AssertionError("opacity must be between 0.0 and 1.0 inclusive.");
        }

        Value = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    /// <summary>The fraction to scale the child's alpha value.</summary>
    /// <remarks>Dart's `Opacity.opacity`; C# members may not repeat their declaring type's name.
    /// </remarks>
    public double Value { get; }

    /// <summary>Whether the semantics of the child are included even when it is fully transparent.
    /// </summary>
    public bool AlwaysIncludeSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderOpacity(Value, AlwaysIncludeSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var opacity = (RenderOpacity)renderObject;
        opacity.Opacity = Value;
        opacity.AlwaysIncludeSemantics = AlwaysIncludeSemantics;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("opacity", Value));
        properties.Add(new FlagProperty(
            "alwaysIncludeSemantics",
            value: AlwaysIncludeSemantics,
            ifTrue: "alwaysIncludeSemantics"));
    }
}

/// <summary>A widget that clips its child using a rectangle.</summary>
public sealed class ClipRect : SingleChildRenderObjectWidget
{
    public ClipRect(
        CustomClipper<Rect>? clipper = null,
        Clip clipBehavior = Clip.HardEdge,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Clipper = clipper;
        ClipBehavior = clipBehavior;
    }

    /// <summary>Supplies the rectangle to clip to; <see langword="null"/> clips to the layout rect.</summary>
    public CustomClipper<Rect>? Clipper { get; }

    /// <summary>How the clip is applied. Defaults to <see cref="Clip.HardEdge"/>.</summary>
    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderClipRect(clipper: Clipper, clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var clipRect = (RenderClipRect)renderObject;
        clipRect.Clipper = Clipper;
        clipRect.ClipBehavior = ClipBehavior;
    }

    internal override void DidUnmountRenderObject(RenderObject renderObject)
    {
        ((RenderClipRect)renderObject).Clipper = null;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<CustomClipper<Rect>?>("clipper", Clipper, defaultValue: null));
    }
}

/// <summary>A widget that clips its child using a rounded rectangle.</summary>
public sealed class ClipRRect : SingleChildRenderObjectWidget
{
    public ClipRRect(
        BorderRadiusGeometry? borderRadius = null,
        CustomClipper<RRect>? clipper = null,
        Clip clipBehavior = Clip.AntiAlias,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        BorderRadius = borderRadius ?? Rendering.BorderRadius.Zero;
        Clipper = clipper;
        ClipBehavior = clipBehavior;
    }

    /// <summary>The border radius of the rounded corners.</summary>
    public BorderRadiusGeometry BorderRadius { get; }

    /// <summary>Supplies the rounded rectangle to clip to; <see langword="null"/> uses
    /// <see cref="BorderRadius"/> over the layout rect.</summary>
    public CustomClipper<RRect>? Clipper { get; }

    /// <summary>How the clip is applied. Defaults to <see cref="Clip.AntiAlias"/>.</summary>
    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderClipRRect(
            borderRadius: BorderRadius,
            clipper: Clipper,
            clipBehavior: ClipBehavior,
            textDirection: Directionality.MaybeOf(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var clipRRect = (RenderClipRRect)renderObject;
        clipRRect.BorderRadius = BorderRadius;
        clipRRect.ClipBehavior = ClipBehavior;
        clipRRect.Clipper = Clipper;
        clipRRect.TextDirection = Directionality.MaybeOf(context);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<BorderRadiusGeometry>(
            "borderRadius",
            BorderRadius,
            showName: false,
            defaultValue: null));
        properties.Add(new DiagnosticsProperty<CustomClipper<RRect>?>("clipper", Clipper, defaultValue: null));
    }
}

/// <summary>A widget that applies a transformation before painting its child.</summary>
public sealed class Transform : SingleChildRenderObjectWidget
{
    public Transform(
        Matrix4 transform,
        Widget? child = null,
        Point? origin = null,
        AlignmentGeometry? alignment = null,
        bool transformHitTests = true,
        FilterQuality? filterQuality = null,
        Key? key = null) : base(child, key)
    {
        Matrix = transform;
        Origin = origin;
        Alignment = alignment;
        TransformHitTests = transformHitTests;
        FilterQuality = filterQuality;
    }

    /// <summary>Creates a widget that rotates its child by <paramref name="angle"/> clockwise radians.</summary>
    public static Transform Rotate(
        double angle,
        Widget? child = null,
        Point? origin = null,
        AlignmentGeometry? alignment = null,
        bool transformHitTests = true,
        FilterQuality? filterQuality = null,
        Key? key = null) =>
        new(
            ComputeRotation(angle),
            child,
            origin,
            alignment ?? Rendering.Alignment.Center,
            transformHitTests,
            filterQuality,
            key);

    /// <summary>Creates a widget that translates its child by <paramref name="offset"/>.</summary>
    public static Transform Translate(
        Point offset,
        Widget? child = null,
        bool transformHitTests = true,
        FilterQuality? filterQuality = null,
        Key? key = null) =>
        new(
            Matrix4.TranslationValues(offset.X, offset.Y, 0.0),
            child,
            origin: null,
            alignment: null,
            transformHitTests,
            filterQuality,
            key);

    /// <summary>Creates a widget that scales its child uniformly or per axis.</summary>
    public static Transform Scale(
        double? scale = null,
        double? scaleX = null,
        double? scaleY = null,
        Widget? child = null,
        Point? origin = null,
        AlignmentGeometry? alignment = null,
        bool transformHitTests = true,
        FilterQuality? filterQuality = null,
        Key? key = null)
    {
        if (scale is null && scaleX is null && scaleY is null)
        {
            throw new ArgumentException(
                "At least one of 'scale', 'scaleX' and 'scaleY' is required to be non-null",
                nameof(scale));
        }

        if (scale is not null && (scaleX is not null || scaleY is not null))
        {
            throw new ArgumentException(
                "If 'scale' is non-null then 'scaleX' and 'scaleY' must be left null",
                nameof(scale));
        }

        return new Transform(
            Matrix4.Diagonal3Values(scale ?? scaleX ?? 1.0, scale ?? scaleY ?? 1.0, 1.0),
            child,
            origin,
            alignment ?? Rendering.Alignment.Center,
            transformHitTests,
            filterQuality,
            key);
    }

    /// <summary>Creates a widget that mirrors its child about its center.</summary>
    public static Transform Flip(
        bool flipX = false,
        bool flipY = false,
        Widget? child = null,
        Point? origin = null,
        bool transformHitTests = true,
        FilterQuality? filterQuality = null,
        Key? key = null) =>
        new(
            Matrix4.Diagonal3Values(flipX ? -1.0 : 1.0, flipY ? -1.0 : 1.0, 1.0),
            child,
            origin,
            Rendering.Alignment.Center,
            transformHitTests,
            filterQuality,
            key);

    /// <summary>The matrix to transform the child by during painting.</summary>
    /// <remarks>Dart's `Transform.transform`; C# members may not repeat their declaring type's name.
    /// </remarks>
    public Matrix4 Matrix { get; }

    /// <summary>The origin of the coordinate system in which to apply the matrix.</summary>
    public Point? Origin { get; }

    /// <summary>The alignment of the origin, relative to the size of the box.</summary>
    public AlignmentGeometry? Alignment { get; }

    /// <summary>Whether to apply the transformation when performing hit tests.</summary>
    public bool TransformHitTests { get; }

    /// <summary>The filter quality with which to apply the transform as a bitmap operation.</summary>
    public FilterQuality? FilterQuality { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderTransform(
            Matrix,
            Alignment,
            child: null,
            FilterQuality,
            Origin,
            TransformHitTests,
            Directionality.MaybeOf(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var transform = (RenderTransform)renderObject;
        transform.Transform = Matrix;
        transform.Origin = Origin;
        transform.Alignment = Alignment;
        transform.TextDirection = Directionality.MaybeOf(context);
        transform.TransformHitTests = TransformHitTests;
        transform.FilterQuality = FilterQuality;
    }

    /// <remarks>
    /// Flutter's <c>Transform._computeRotation</c>: quarter turns snap to exact matrices so a 90 degree
    /// rotation does not leave a 6e-17 skew behind.
    /// </remarks>
    private static Matrix4 ComputeRotation(double radians)
    {
        if (!double.IsFinite(radians))
        {
            throw new ArgumentException(
                $"Cannot compute the rotation matrix for a non-finite angle: {radians}",
                nameof(radians));
        }

        if (radians == 0.0)
        {
            return Matrix4.Identity();
        }

        double sine = Math.Sin(radians);
        if (sine == 1.0)
        {
            return CreateZRotation(1.0, 0.0);
        }

        if (sine == -1.0)
        {
            return CreateZRotation(-1.0, 0.0);
        }

        double cosine = Math.Cos(radians);
        if (cosine == -1.0)
        {
            return CreateZRotation(0.0, -1.0);
        }

        return CreateZRotation(sine, cosine);
    }

    private static Matrix4 CreateZRotation(double sine, double cosine)
    {
        Matrix4 result = Matrix4.Zero();
        result.Storage[0] = cosine;
        result.Storage[1] = sine;
        result.Storage[4] = -sine;
        result.Storage[5] = cosine;
        result.Storage[10] = 1.0;
        result.Storage[15] = 1.0;
        return result;
    }
}

/// <summary>Scales and positions its child within itself according to a <see cref="BoxFit"/>.</summary>
public sealed class FittedBox : SingleChildRenderObjectWidget
{
    public FittedBox(
        Widget? child = null,
        BoxFit fit = BoxFit.Contain,
        AlignmentGeometry alignment = default,
        Clip clipBehavior = Clip.None,
        Key? key = null) : base(child, key)
    {
        Fit = fit;
        Alignment = alignment;
        ClipBehavior = clipBehavior;
    }

    /// <summary>How to inscribe the child into the space allocated during layout.</summary>
    public BoxFit Fit { get; }

    /// <summary>How to align the child within its parent's bounds.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>How to clip the child when it overflows. Defaults to <see cref="Clip.None"/>.</summary>
    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFittedBox(
            fit: Fit,
            alignment: Alignment,
            textDirection: Directionality.MaybeOf(context),
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var fittedBox = (RenderFittedBox)renderObject;
        fittedBox.Fit = Fit;
        fittedBox.Alignment = Alignment;
        fittedBox.TextDirection = Directionality.MaybeOf(context);
        fittedBox.ClipBehavior = ClipBehavior;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<BoxFit>("fit", Fit));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
    }
}

/// <summary>Applies a translation expressed as a fraction of the box's own size before painting.</summary>
public sealed class FractionalTranslation : SingleChildRenderObjectWidget
{
    public FractionalTranslation(
        Vector translation,
        Widget? child = null,
        bool transformHitTests = true,
        Key? key = null) : base(child, key)
    {
        Translation = translation;
        TransformHitTests = transformHitTests;
    }

    /// <summary>The translation to apply, as a fraction of this box's size.</summary>
    public Vector Translation { get; }

    /// <summary>Whether to apply the translation when performing hit tests.</summary>
    public bool TransformHitTests { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFractionalTranslation(Translation, TransformHitTests);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var translation = (RenderFractionalTranslation)renderObject;
        translation.Translation = Translation;
        translation.TransformHitTests = TransformHitTests;
    }
}

/// <summary>Rotates its child by an integral number of quarter turns before layout.</summary>
public sealed class RotatedBox : SingleChildRenderObjectWidget
{
    public RotatedBox(
        int quarterTurns,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        QuarterTurns = quarterTurns;
    }

    /// <summary>The number of clockwise quarter turns the child should be rotated.</summary>
    public int QuarterTurns { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderRotatedBox(QuarterTurns);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderRotatedBox)renderObject).QuarterTurns = QuarterTurns;
    }
}

/// <summary>Insets its child by the given padding.</summary>
public sealed class Padding : SingleChildRenderObjectWidget
{
    public Padding(EdgeInsetsGeometry insets, Widget? child = null, Key? key = null) : base(child, key)
    {
        Insets = insets;
    }

    /// <summary>The amount of space by which to inset the child.</summary>
    /// <remarks>Dart's `Padding.padding`; C# members may not repeat their declaring type's name.
    /// </remarks>
    public EdgeInsetsGeometry Insets { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPadding(Insets, textDirection: Directionality.MaybeOf(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var padding = (RenderPadding)renderObject;
        padding.Padding = Insets;
        padding.TextDirection = Directionality.MaybeOf(context);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry>("padding", Insets));
    }
}

/// <summary>Aligns its child within itself and optionally sizes itself based on the child's size.
/// </summary>
public class Align : SingleChildRenderObjectWidget
{
    public Align(
        Widget? child = null,
        AlignmentGeometry alignment = default,
        double? widthFactor = null,
        double? heightFactor = null,
        Key? key = null) : base(child, key)
    {
        if (widthFactor.HasValue && (double.IsNaN(widthFactor.Value) || widthFactor.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(widthFactor), "Width factor must be non-negative.");
        }

        if (heightFactor.HasValue && (double.IsNaN(heightFactor.Value) || heightFactor.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(heightFactor), "Height factor must be non-negative.");
        }

        Alignment = alignment;
        WidthFactor = widthFactor;
        HeightFactor = heightFactor;
    }

    /// <summary>How to align the child.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>If non-null, sets its width to the child's width multiplied by this factor.</summary>
    public double? WidthFactor { get; }

    /// <summary>If non-null, sets its height to the child's height multiplied by this factor.</summary>
    public double? HeightFactor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPositionedBox(
            widthFactor: WidthFactor,
            heightFactor: HeightFactor,
            alignment: Alignment,
            textDirection: Directionality.MaybeOf(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var align = (RenderPositionedBox)renderObject;
        align.Alignment = Alignment;
        align.WidthFactor = WidthFactor;
        align.HeightFactor = HeightFactor;
        align.TextDirection = Directionality.MaybeOf(context);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new DoubleProperty("widthFactor", WidthFactor, defaultValue: null));
        properties.Add(new DoubleProperty("heightFactor", HeightFactor, defaultValue: null));
    }
}

/// <summary>Centers its child within itself.</summary>
public sealed class Center : Align
{
    public Center(
        Widget? child = null,
        double? widthFactor = null,
        double? heightFactor = null,
        Key? key = null) : base(
        child: child,
        alignment: Plumix.Rendering.Alignment.Center,
        widthFactor: widthFactor,
        heightFactor: heightFactor,
        key: key)
    {
    }
}

/// <summary>A box with a specified size.</summary>
public sealed class SizedBox : SingleChildRenderObjectWidget
{
    public SizedBox(double? width = null, double? height = null, Widget? child = null, Key? key = null)
        : base(child, key)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Dart's `SizedBox.expand`: a box that becomes as large as its parent allows.</summary>
    public static SizedBox Expand(Widget? child = null, Key? key = null) =>
        new(width: double.PositiveInfinity, height: double.PositiveInfinity, child: child, key: key);

    /// <summary>Dart's `SizedBox.shrink`: a box that becomes as small as its parent allows.</summary>
    public static SizedBox Shrink(Widget? child = null, Key? key = null) =>
        new(width: 0.0, height: 0.0, child: child, key: key);

    /// <summary>Dart's `SizedBox.fromSize`: a box with the given size.</summary>
    public static SizedBox FromSize(Size? size = null, Widget? child = null, Key? key = null) =>
        new(width: size?.Width, height: size?.Height, child: child, key: key);

    /// <summary>Dart's `SizedBox.square`: a box with the given dimension on both axes.</summary>
    public static SizedBox Square(double? dimension = null, Widget? child = null, Key? key = null) =>
        new(width: dimension, height: dimension, child: child, key: key);

    /// <summary>If non-null, requires the child to have exactly this width.</summary>
    public double? Width { get; }

    /// <summary>If non-null, requires the child to have exactly this height.</summary>
    public double? Height { get; }

    private BoxConstraints AdditionalConstraints =>
        BoxConstraints.TightFor(width: Width, height: Height);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderConstrainedBox(AdditionalConstraints);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderConstrainedBox)renderObject).AdditionalConstraints = AdditionalConstraints;
    }

    /// <inheritdoc />
    public override string ToStringShort()
    {
        string type = (Width, Height) switch
        {
            (double.PositiveInfinity, double.PositiveInfinity) => $"{nameof(SizedBox)}.Expand",
            (0.0, 0.0) => $"{nameof(SizedBox)}.Shrink",
            _ => nameof(SizedBox),
        };
        return Key is null ? type : $"{type}-{Key}";
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        DiagnosticLevel level =
            (Width == double.PositiveInfinity && Height == double.PositiveInfinity)
            || (Width == 0.0 && Height == 0.0)
                ? DiagnosticLevel.Hidden
                : DiagnosticLevel.Info;
        properties.Add(new DoubleProperty("width", Width, defaultValue: null, level: level));
        properties.Add(new DoubleProperty("height", Height, defaultValue: null, level: level));
    }
}

/// <summary>Imposes additional constraints on its child.</summary>
public sealed class ConstrainedBox : SingleChildRenderObjectWidget
{
    public ConstrainedBox(BoxConstraints constraints, Widget? child = null, Key? key = null) : base(child, key)
    {
        Constraints = constraints;
    }

    /// <summary>The additional constraints to impose on the child.</summary>
    public BoxConstraints Constraints { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderConstrainedBox(Constraints);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderConstrainedBox)renderObject).AdditionalConstraints = Constraints;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<BoxConstraints>("constraints", Constraints, showName: false));
    }
}

/// <summary>Imposes no constraints on its child, allowing it to render at its natural size.</summary>
public sealed class UnconstrainedBox : StatelessWidget
{
    public UnconstrainedBox(
        Widget? child = null,
        TextDirection? textDirection = null,
        AlignmentGeometry alignment = default,
        Axis? constrainedAxis = null,
        Clip clipBehavior = Clip.None,
        Key? key = null) : base(key)
    {
        Child = child;
        TextDirection = textDirection;
        Alignment = alignment;
        ConstrainedAxis = constrainedAxis;
        ClipBehavior = clipBehavior;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>The text direction to use when resolving <see cref="Alignment"/>.</summary>
    public TextDirection? TextDirection { get; }

    /// <summary>The alignment to use when laying out the child.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>The axis to retain constraints on, if any.</summary>
    public Axis? ConstrainedAxis { get; }

    /// <summary>How to clip the child when it overflows. Defaults to <see cref="Clip.None"/>.</summary>
    public Clip ClipBehavior { get; }

    private static BoxConstraintsTransform AxisToTransform(Axis? constrainedAxis) => constrainedAxis switch
    {
        Axis.Horizontal => ConstraintsTransformBox.HeightUnconstrained,
        Axis.Vertical => ConstraintsTransformBox.WidthUnconstrained,
        null => ConstraintsTransformBox.Unconstrained,
        _ => throw new ArgumentOutOfRangeException(nameof(constrainedAxis)),
    };

    public override Widget Build(BuildContext context)
    {
        return new ConstraintsTransformBox(
            constraintsTransform: AxisToTransform(ConstrainedAxis),
            child: Child,
            textDirection: TextDirection,
            alignment: Alignment,
            clipBehavior: ClipBehavior);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new EnumProperty<Axis>("constrainedAxis", ConstrainedAxis, defaultValue: null));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
    }
}

/// <summary>Sizes its child to a fraction of the total available space.</summary>
public sealed class FractionallySizedBox : SingleChildRenderObjectWidget
{
    public FractionallySizedBox(
        Widget? child = null,
        AlignmentGeometry alignment = default,
        double? widthFactor = null,
        double? heightFactor = null,
        Key? key = null) : base(child, key)
    {
        Alignment = alignment;
        WidthFactor = ValidateFactor(widthFactor, nameof(widthFactor));
        HeightFactor = ValidateFactor(heightFactor, nameof(heightFactor));
    }

    /// <summary>How to align the child.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>If non-null, the fraction of the incoming width the child is given.</summary>
    public double? WidthFactor { get; }

    /// <summary>If non-null, the fraction of the incoming height the child is given.</summary>
    public double? HeightFactor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFractionallySizedOverflowBox(
            widthFactor: WidthFactor,
            heightFactor: HeightFactor,
            alignment: Alignment,
            textDirection: Directionality.MaybeOf(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var fractionallySizedBox = (RenderFractionallySizedOverflowBox)renderObject;
        fractionallySizedBox.Alignment = Alignment;
        fractionallySizedBox.WidthFactor = WidthFactor;
        fractionallySizedBox.HeightFactor = HeightFactor;
        fractionallySizedBox.TextDirection = Directionality.MaybeOf(context);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new DoubleProperty("widthFactor", WidthFactor, defaultValue: null));
        properties.Add(new DoubleProperty("heightFactor", HeightFactor, defaultValue: null));
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

/// <summary>A box that limits its size only when it is unconstrained.</summary>
public sealed class LimitedBox : SingleChildRenderObjectWidget
{
    public LimitedBox(
        Widget? child = null,
        double maxWidth = double.PositiveInfinity,
        double maxHeight = double.PositiveInfinity,
        Key? key = null) : base(child, key)
    {
        MaxWidth = ValidateMax(maxWidth, nameof(maxWidth));
        MaxHeight = ValidateMax(maxHeight, nameof(maxHeight));
    }

    /// <summary>The maximum width limit to apply in the absence of a bounded width constraint.</summary>
    public double MaxWidth { get; }

    /// <summary>The maximum height limit to apply in the absence of a bounded height constraint.</summary>
    public double MaxHeight { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderLimitedBox(
            maxWidth: MaxWidth,
            maxHeight: MaxHeight);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var limitedBox = (RenderLimitedBox)renderObject;
        limitedBox.MaxWidth = MaxWidth;
        limitedBox.MaxHeight = MaxHeight;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("maxWidth", MaxWidth, defaultValue: double.PositiveInfinity));
        properties.Add(new DoubleProperty("maxHeight", MaxHeight, defaultValue: double.PositiveInfinity));
    }

    private static double ValidateMax(double value, string parameterName)
    {
        if (double.IsNaN(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Max value must be non-negative.");
        }

        return value;
    }
}

/// <summary>Imposes different constraints on its child than it gets from its parent, possibly allowing
/// the child to overflow the parent.</summary>
public sealed class OverflowBox : SingleChildRenderObjectWidget
{
    public OverflowBox(
        Widget? child = null,
        AlignmentGeometry alignment = default,
        double? minWidth = null,
        double? maxWidth = null,
        double? minHeight = null,
        double? maxHeight = null,
        OverflowBoxFit fit = OverflowBoxFit.Max,
        Key? key = null) : base(child, key)
    {
        Alignment = alignment;
        MinWidth = minWidth;
        MaxWidth = maxWidth;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
        Fit = fit;
    }

    /// <summary>How to align the child.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>The minimum width constraint to give the child, if non-null.</summary>
    public double? MinWidth { get; }

    /// <summary>The maximum width constraint to give the child, if non-null.</summary>
    public double? MaxWidth { get; }

    /// <summary>The minimum height constraint to give the child, if non-null.</summary>
    public double? MinHeight { get; }

    /// <summary>The maximum height constraint to give the child, if non-null.</summary>
    public double? MaxHeight { get; }

    /// <summary>How much space this widget takes up.</summary>
    public OverflowBoxFit Fit { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderConstrainedOverflowBox(
            minWidth: MinWidth,
            maxWidth: MaxWidth,
            minHeight: MinHeight,
            maxHeight: MaxHeight,
            fit: Fit,
            alignment: Alignment,
            textDirection: Directionality.MaybeOf(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var overflowBox = (RenderConstrainedOverflowBox)renderObject;
        overflowBox.Alignment = Alignment;
        overflowBox.MinWidth = MinWidth;
        overflowBox.MaxWidth = MaxWidth;
        overflowBox.MinHeight = MinHeight;
        overflowBox.MaxHeight = MaxHeight;
        overflowBox.Fit = Fit;
        overflowBox.TextDirection = Directionality.MaybeOf(context);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new DoubleProperty("minWidth", MinWidth, defaultValue: null));
        properties.Add(new DoubleProperty("maxWidth", MaxWidth, defaultValue: null));
        properties.Add(new DoubleProperty("minHeight", MinHeight, defaultValue: null));
        properties.Add(new DoubleProperty("maxHeight", MaxHeight, defaultValue: null));
        properties.Add(new EnumProperty<OverflowBoxFit>("fit", Fit));
    }
}

/// <summary>A widget that is a specific size but passes its original constraints through to its child,
/// which it allows to overflow.</summary>
public sealed class SizedOverflowBox : SingleChildRenderObjectWidget
{
    public SizedOverflowBox(
        Size size,
        Widget? child = null,
        AlignmentGeometry alignment = default,
        Key? key = null) : base(child, key)
    {
        Size = size;
        Alignment = alignment;
    }

    /// <summary>The size this widget should attempt to be.</summary>
    public Size Size { get; }

    /// <summary>How to align the child.</summary>
    public AlignmentGeometry Alignment { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSizedOverflowBox(
            requestedSize: Size,
            alignment: Alignment,
            textDirection: Directionality.Of(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var sizedOverflowBox = (RenderSizedOverflowBox)renderObject;
        sizedOverflowBox.Alignment = Alignment;
        sizedOverflowBox.RequestedSize = Size;
        sizedOverflowBox.TextDirection = Directionality.Of(context);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new DiagnosticsProperty<Size>("size", Size, defaultValue: null));
    }
}

/// <summary>Lays the child out as if it was in the tree, but without painting anything, without making
/// the child available for hit testing, and without taking any room in the parent.</summary>
public sealed class Offstage : SingleChildRenderObjectWidget
{
    public Offstage(
        Widget? child = null,
        bool offstage = true,
        Key? key = null) : base(child, key)
    {
        IsOffstage = offstage;
    }

    /// <summary>Whether the child is hidden from the rest of the tree.</summary>
    /// <remarks>Dart's `Offstage.offstage`; C# members may not repeat their declaring type's name.
    /// </remarks>
    public bool IsOffstage { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderOffstage(offstage: IsOffstage);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderOffstage)renderObject).Offstage = IsOffstage;
    }

    internal override Element CreateElement() => new OffstageElement(this);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("offstage", IsOffstage));
    }
}

/// <summary>Dart's private `_OffstageElement`: hides its child from the on-stage walk while offstage.
/// </summary>
internal sealed class OffstageElement : SingleChildRenderObjectElement
{
    public OffstageElement(Offstage widget) : base(widget)
    {
    }

    internal override void DebugVisitOnstageChildren(Action<Element> visitor)
    {
        if (!((Offstage)Widget).IsOffstage)
        {
            base.DebugVisitOnstageChildren(visitor);
        }
    }
}

/// <summary>Attempts to size the child to a specific aspect ratio.</summary>
public sealed class AspectRatio : SingleChildRenderObjectWidget
{
    public AspectRatio(double aspectRatio, Widget? child = null, Key? key = null) : base(child, key)
    {
        Ratio = ValidateRatio(aspectRatio, nameof(aspectRatio));
    }

    /// <summary>The aspect ratio to attempt to use, expressed as width divided by height.</summary>
    /// <remarks>Dart's `AspectRatio.aspectRatio`; C# members may not repeat their declaring type's
    /// name.</remarks>
    public double Ratio { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAspectRatio(Ratio);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderAspectRatio)renderObject).AspectRatio = Ratio;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("aspectRatio", Ratio));
    }

    private static double ValidateRatio(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Aspect ratio must be finite and positive.");
        }

        return value;
    }
}

/// <summary>Positions its children relative to the edges of its box.</summary>
public class Stack : MultiChildRenderObjectWidget
{
    public Stack(
        IReadOnlyList<Widget>? children = null,
        AlignmentGeometry? alignment = null,
        StackFit fit = StackFit.Loose,
        Clip clipBehavior = Clip.HardEdge,
        TextDirection? textDirection = null,
        Key? key = null) : base(children, key)
    {
        Alignment = alignment ?? AlignmentDirectional.TopStart;
        TextDirection = textDirection;
        Fit = fit;
        ClipBehavior = clipBehavior;
    }

    /// <summary>How to align the non-positioned and partially-positioned children.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>The text direction with which to resolve <see cref="Alignment"/>.</summary>
    public TextDirection? TextDirection { get; }

    /// <summary>How to size the non-positioned children.</summary>
    public StackFit Fit { get; }

    /// <summary>How to clip overflowing content. Defaults to <see cref="Clip.HardEdge"/>.</summary>
    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderStack(
            alignment: Alignment,
            fit: Fit,
            clipBehavior: ClipBehavior,
            textDirection: TextDirection ?? Directionality.MaybeOf(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var stack = (RenderStack)renderObject;
        stack.Alignment = Alignment;
        stack.TextDirection = TextDirection ?? Directionality.MaybeOf(context);
        stack.Fit = Fit;
        stack.ClipBehavior = ClipBehavior;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
        properties.Add(new EnumProperty<StackFit>("fit", Fit));
        properties.Add(new EnumProperty<Clip>("clipBehavior", ClipBehavior, defaultValue: Clip.HardEdge));
    }
}

/// <summary>Controls where a child of a <see cref="Stack"/> is positioned.</summary>
public sealed class Positioned : ParentDataWidget<StackParentData>
{
    public Positioned(
        Widget child,
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        Key? key = null) : base(child, key)
    {
        if (left.HasValue && right.HasValue && width.HasValue)
        {
            throw new ArgumentException("Cannot provide left, right, and width simultaneously.");
        }

        if (top.HasValue && bottom.HasValue && height.HasValue)
        {
            throw new ArgumentException("Cannot provide top, bottom, and height simultaneously.");
        }

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Width = width;
        Height = height;
    }

    /// <summary>Dart's `Positioned.fromRect`: positions the child from a rect in the stack's
    /// coordinate space.</summary>
    public static Positioned FromRect(Rect rect, Widget child, Key? key = null)
    {
        return new Positioned(
            child: child,
            left: rect.Left,
            top: rect.Top,
            width: rect.Width,
            height: rect.Height,
            key: key);
    }

    /// <summary>Dart's `Positioned.fromRelativeRect`: positions the child from insets relative to the
    /// stack's edges.</summary>
    public static Positioned FromRelativeRect(
        RelativeRect rect,
        Widget child,
        Key? key = null)
    {
        return new Positioned(
            child: child,
            left: rect.Left,
            top: rect.Top,
            right: rect.Right,
            bottom: rect.Bottom,
            key: key);
    }

    /// <summary>Dart's `Positioned.fill`: positions the child to fill the stack, inset by the given
    /// distances.</summary>
    public static Positioned Fill(
        Widget child,
        double? left = 0.0,
        double? top = 0.0,
        double? right = 0.0,
        double? bottom = 0.0,
        Key? key = null)
    {
        return new Positioned(
            child: child,
            left: left,
            top: top,
            right: right,
            bottom: bottom,
            key: key);
    }

    /// <summary>Dart's `Positioned.directional`: positions the child using start/end resolved against
    /// the given text direction.</summary>
    public static Positioned Directional(
        TextDirection textDirection,
        Widget child,
        double? start = null,
        double? top = null,
        double? end = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        Key? key = null)
    {
        (double? left, double? right) = textDirection switch
        {
            TextDirection.Rtl => (end, start),
            TextDirection.Ltr => (start, end),
            _ => throw new ArgumentOutOfRangeException(nameof(textDirection)),
        };
        return new Positioned(
            child: child,
            left: left,
            top: top,
            right: right,
            bottom: bottom,
            width: width,
            height: height,
            key: key);
    }

    /// <summary>The distance from the left edge of the stack to the child's left edge.</summary>
    public double? Left { get; }

    /// <summary>The distance from the top edge of the stack to the child's top edge.</summary>
    public double? Top { get; }

    /// <summary>The distance from the right edge of the stack to the child's right edge.</summary>
    public double? Right { get; }

    /// <summary>The distance from the bottom edge of the stack to the child's bottom edge.</summary>
    public double? Bottom { get; }

    /// <summary>The child's width.</summary>
    public double? Width { get; }

    /// <summary>The child's height.</summary>
    public double? Height { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(Stack);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        ArgumentNullException.ThrowIfNull(renderObject);
        var parentData = (StackParentData)renderObject.parentData!;
        bool needsLayout = false;

        if (parentData.Left != Left)
        {
            parentData.Left = Left;
            needsLayout = true;
        }

        if (parentData.Top != Top)
        {
            parentData.Top = Top;
            needsLayout = true;
        }

        if (parentData.Right != Right)
        {
            parentData.Right = Right;
            needsLayout = true;
        }

        if (parentData.Bottom != Bottom)
        {
            parentData.Bottom = Bottom;
            needsLayout = true;
        }

        if (parentData.Width != Width)
        {
            parentData.Width = Width;
            needsLayout = true;
        }

        if (parentData.Height != Height)
        {
            parentData.Height = Height;
            needsLayout = true;
        }

        if (needsLayout)
        {
            renderObject.Parent?.MarkNeedsLayout();
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("left", Left, defaultValue: null));
        properties.Add(new DoubleProperty("top", Top, defaultValue: null));
        properties.Add(new DoubleProperty("right", Right, defaultValue: null));
        properties.Add(new DoubleProperty("bottom", Bottom, defaultValue: null));
        properties.Add(new DoubleProperty("width", Width, defaultValue: null));
        properties.Add(new DoubleProperty("height", Height, defaultValue: null));
    }
}

/// <summary>Controls where a child of a <see cref="Stack"/> is positioned, using start/end resolved
/// against the ambient <see cref="Directionality"/>.</summary>
public sealed class PositionedDirectional : StatelessWidget
{
    public PositionedDirectional(
        Widget child,
        double? start = null,
        double? top = null,
        double? end = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Start = start;
        Top = top;
        End = end;
        Bottom = bottom;
        Width = width;
        Height = height;
    }

    /// <summary>The distance from the leading edge of the stack to the child's leading edge.</summary>
    public double? Start { get; }

    /// <summary>The distance from the top edge of the stack to the child's top edge.</summary>
    public double? Top { get; }

    /// <summary>The distance from the trailing edge of the stack to the child's trailing edge.</summary>
    public double? End { get; }

    /// <summary>The distance from the bottom edge of the stack to the child's bottom edge.</summary>
    public double? Bottom { get; }

    /// <summary>The child's width.</summary>
    public double? Width { get; }

    /// <summary>The child's height.</summary>
    public double? Height { get; }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Positioned.Directional(
            textDirection: Directionality.Of(context),
            child: Child,
            start: Start,
            top: Top,
            end: End,
            bottom: Bottom,
            width: Width,
            height: Height);
    }
}

/// <summary>Displays its children in a one-dimensional array.</summary>
public class Flex : MultiChildRenderObjectWidget
{
    public Flex(
        Axis direction,
        IReadOnlyList<Widget>? children = null,
        MainAxisSize mainAxisSize = MainAxisSize.Max,
        MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Center,
        double spacing = 0,
        Key? key = null,
        TextDirection? textDirection = null,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        TextBaseline? textBaseline = null,
        Clip clipBehavior = Clip.None) : base(children, key)
    {
        if (Constants.KDebugMode
            && crossAxisAlignment == CrossAxisAlignment.Baseline
            && textBaseline == null)
        {
            throw new AssertionError(
                "textBaseline is required if you specify the crossAxisAlignment with "
                + "CrossAxisAlignment.Baseline");
        }

        Direction = direction;
        MainAxisSize = mainAxisSize;
        MainAxisAlignment = mainAxisAlignment;
        CrossAxisAlignment = crossAxisAlignment;
        Spacing = spacing;
        TextDirection = textDirection;
        VerticalDirection = verticalDirection;
        TextBaseline = textBaseline;
        ClipBehavior = clipBehavior;
    }

    /// <summary>The direction to use as the main axis.</summary>
    public Axis Direction { get; }

    /// <summary>How much space should be occupied in the main axis.</summary>
    public MainAxisSize MainAxisSize { get; }

    /// <summary>How the children should be placed along the main axis.</summary>
    public MainAxisAlignment MainAxisAlignment { get; }

    /// <summary>How the children should be placed along the cross axis.</summary>
    public CrossAxisAlignment CrossAxisAlignment { get; }

    /// <summary>How much space to place between children in the main axis.</summary>
    public double Spacing { get; }

    /// <summary>Determines the order to lay children out horizontally.</summary>
    public TextDirection? TextDirection { get; }

    /// <summary>Determines the order to lay children out vertically.</summary>
    public VerticalDirection VerticalDirection { get; }

    /// <summary>The baseline to align to when <see cref="CrossAxisAlignment.Baseline"/> is used.</summary>
    public TextBaseline? TextBaseline { get; }

    /// <summary>How to clip overflowing content. Defaults to <see cref="Clip.None"/>.</summary>
    public Clip ClipBehavior { get; }

    private bool NeedTextDirection => Direction switch
    {
        // Because it affects the layout order.
        Axis.Horizontal => true,
        Axis.Vertical => CrossAxisAlignment is CrossAxisAlignment.Start or CrossAxisAlignment.End,

        _ => throw new ArgumentOutOfRangeException()
    };

    /// The value to pass to [RenderFlex.TextDirection].
    ///
    /// This value is derived from the [TextDirection] property and the ambient
    /// [Directionality]. The value is null if there is no need to specify the
    /// text direction.
    protected TextDirection? GetEffectiveTextDirection(BuildContext context)
    {
        return TextDirection ?? (NeedTextDirection ? Directionality.MaybeOf(context) : null);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFlex(
            children: null,
            direction: Direction,
            mainAxisSize: MainAxisSize,
            mainAxisAlignment: MainAxisAlignment,
            crossAxisAlignment: CrossAxisAlignment,
            textDirection: GetEffectiveTextDirection(context),
            verticalDirection: VerticalDirection,
            textBaseline: TextBaseline,
            clipBehavior: ClipBehavior,
            spacing: Spacing);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var flex = (RenderFlex)renderObject;
        flex.Direction = Direction;
        flex.MainAxisAlignment = MainAxisAlignment;
        flex.MainAxisSize = MainAxisSize;
        flex.CrossAxisAlignment = CrossAxisAlignment;
        flex.TextDirection = GetEffectiveTextDirection(context);
        flex.VerticalDirection = VerticalDirection;
        flex.TextBaseline = TextBaseline;
        flex.ClipBehavior = ClipBehavior;
        flex.Spacing = Spacing;
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<Axis>("direction", Direction));
        properties.Add(new EnumProperty<MainAxisAlignment>("mainAxisAlignment", MainAxisAlignment));
        properties.Add(new EnumProperty<MainAxisSize>(
            "mainAxisSize",
            MainAxisSize,
            defaultValue: MainAxisSize.Max));
        properties.Add(new EnumProperty<CrossAxisAlignment>("crossAxisAlignment", CrossAxisAlignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
        properties.Add(new EnumProperty<VerticalDirection>(
            "verticalDirection",
            VerticalDirection,
            defaultValue: VerticalDirection.Down));
        properties.Add(new EnumProperty<TextBaseline>("textBaseline", TextBaseline, defaultValue: null));
        properties.Add(new EnumProperty<Clip>("clipBehavior", ClipBehavior, defaultValue: Clip.None));
        properties.Add(new DoubleProperty("spacing", Spacing, defaultValue: 0.0));
    }
}

/// <summary>Displays its children in a horizontal array.</summary>
public sealed class Row : Flex
{
    public Row(
        IReadOnlyList<Widget>? children = null,
        MainAxisSize mainAxisSize = MainAxisSize.Max,
        MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Center,
        double spacing = 0,
        Key? key = null,
        TextDirection? textDirection = null,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        TextBaseline? textBaseline = null) : base(
        direction: Axis.Horizontal,
        children: children,
        mainAxisSize: mainAxisSize,
        mainAxisAlignment: mainAxisAlignment,
        crossAxisAlignment: crossAxisAlignment,
        spacing: spacing,
        key: key,
        textDirection: textDirection,
        verticalDirection: verticalDirection,
        textBaseline: textBaseline)
    {
    }
}

/// <summary>Displays its children in a vertical array.</summary>
public sealed class Column : Flex
{
    public Column(
        IReadOnlyList<Widget>? children = null,
        MainAxisSize mainAxisSize = MainAxisSize.Max,
        MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Center,
        double spacing = 0,
        Key? key = null,
        TextDirection? textDirection = null,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        TextBaseline? textBaseline = null) : base(
        direction: Axis.Vertical,
        children: children,
        mainAxisSize: mainAxisSize,
        mainAxisAlignment: mainAxisAlignment,
        crossAxisAlignment: crossAxisAlignment,
        spacing: spacing,
        key: key,
        textDirection: textDirection,
        verticalDirection: verticalDirection,
        textBaseline: textBaseline)
    {
    }
}

/// <summary>Controls how a child of a <see cref="Flex"/> flexes.</summary>
public class Flexible : ParentDataWidget<FlexParentData>
{
    public Flexible(
        Widget child,
        int flex = 1,
        FlexFit fit = FlexFit.Loose,
        Key? key = null) : base(child, key)
    {
        Flex = flex;
        Fit = fit;
    }

    /// <summary>The flex factor to use for this child.</summary>
    public int Flex { get; }

    /// <summary>How a flexible child is inscribed into the available space.</summary>
    public FlexFit Fit { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(Flex);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        ArgumentNullException.ThrowIfNull(renderObject);
        var parentData = (FlexParentData)renderObject.parentData!;
        bool needsLayout = false;

        if (parentData.flex != Flex)
        {
            parentData.flex = Flex;
            needsLayout = true;
        }

        if (parentData.fit != Fit)
        {
            parentData.fit = Fit;
            needsLayout = true;
        }

        if (needsLayout)
        {
            renderObject.Parent?.MarkNeedsLayout();
        }
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new IntProperty("flex", Flex));
    }
}

/// <summary>A <see cref="Flexible"/> that forces its child to fill the available space.</summary>
public sealed class Expanded : Flexible
{
    public Expanded(Widget child, int flex = 1, Key? key = null) : base(
        child: child,
        flex: flex,
        fit: FlexFit.Tight,
        key: key)
    {
    }
}

/// <summary>Displays its children in multiple horizontal or vertical runs.</summary>
public sealed class Wrap : MultiChildRenderObjectWidget
{
    public Wrap(
        IReadOnlyList<Widget>? children = null,
        Axis direction = Axis.Horizontal,
        WrapAlignment alignment = WrapAlignment.Start,
        double spacing = 0,
        WrapAlignment runAlignment = WrapAlignment.Start,
        double runSpacing = 0,
        WrapCrossAlignment crossAxisAlignment = WrapCrossAlignment.Start,
        TextDirection? textDirection = null,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        Clip clipBehavior = Clip.None,
        Key? key = null) : base(children, key)
    {
        Direction = direction;
        Alignment = alignment;
        Spacing = spacing;
        RunAlignment = runAlignment;
        RunSpacing = runSpacing;
        CrossAxisAlignment = crossAxisAlignment;
        TextDirection = textDirection;
        VerticalDirection = verticalDirection;
        ClipBehavior = clipBehavior;
    }

    /// <summary>The direction to use as the main axis.</summary>
    public Axis Direction { get; }

    /// <summary>How the children within a run should be placed in the main axis.</summary>
    public WrapAlignment Alignment { get; }

    /// <summary>How much space to place between children in a run in the main axis.</summary>
    public double Spacing { get; }

    /// <summary>How the runs themselves should be placed in the cross axis.</summary>
    public WrapAlignment RunAlignment { get; }

    /// <summary>How much space to place between the runs themselves in the cross axis.</summary>
    public double RunSpacing { get; }

    /// <summary>How the children within a run should be aligned relative to each other.</summary>
    public WrapCrossAlignment CrossAxisAlignment { get; }

    /// <summary>Determines the order to lay children out horizontally.</summary>
    public TextDirection? TextDirection { get; }

    /// <summary>Determines the order to lay children out vertically.</summary>
    public VerticalDirection VerticalDirection { get; }

    /// <summary>How to clip overflowing content. Defaults to <see cref="Clip.None"/>.</summary>
    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderWrap(
            direction: Direction,
            alignment: Alignment,
            spacing: Spacing,
            runAlignment: RunAlignment,
            runSpacing: RunSpacing,
            crossAxisAlignment: CrossAxisAlignment,
            textDirection: TextDirection ?? Directionality.MaybeOf(context),
            verticalDirection: VerticalDirection,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var wrap = (RenderWrap)renderObject;
        wrap.Direction = Direction;
        wrap.Alignment = Alignment;
        wrap.Spacing = Spacing;
        wrap.RunAlignment = RunAlignment;
        wrap.RunSpacing = RunSpacing;
        wrap.CrossAxisAlignment = CrossAxisAlignment;
        wrap.TextDirection = TextDirection ?? Directionality.MaybeOf(context);
        wrap.VerticalDirection = VerticalDirection;
        wrap.ClipBehavior = ClipBehavior;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<Axis>("direction", Direction));
        properties.Add(new EnumProperty<WrapAlignment>("alignment", Alignment));
        properties.Add(new DoubleProperty("spacing", Spacing));
        properties.Add(new EnumProperty<WrapAlignment>("runAlignment", RunAlignment));
        properties.Add(new DoubleProperty("runSpacing", RunSpacing));
        properties.Add(new EnumProperty<WrapCrossAlignment>("crossAxisAlignment", CrossAxisAlignment));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
        properties.Add(new EnumProperty<VerticalDirection>(
            "verticalDirection",
            VerticalDirection,
            defaultValue: VerticalDirection.Down));
    }
}

/// <summary>A widget that is invisible during hit testing.</summary>
public sealed class IgnorePointer : SingleChildRenderObjectWidget
{
    public IgnorePointer(
        Widget? child = null,
        bool ignoring = true,
        bool? ignoringSemantics = null,
        Key? key = null) : base(child, key)
    {
        Ignoring = ignoring;
        IgnoringSemantics = ignoringSemantics;
    }

    /// <summary>Whether this widget is ignored during hit testing.</summary>
    public bool Ignoring { get; }

    /// <summary>Deprecated in Flutter: whether the semantics of this widget are ignored.</summary>
    public bool? IgnoringSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderIgnorePointer(
            ignoring: Ignoring,
            ignoringSemantics: IgnoringSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var ignorePointer = (RenderIgnorePointer)renderObject;
        ignorePointer.Ignoring = Ignoring;
        ignorePointer.IgnoringSemantics = IgnoringSemantics;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("ignoring", Ignoring));
        properties.Add(new DiagnosticsProperty<bool?>(
            "ignoringSemantics",
            IgnoringSemantics,
            defaultValue: null));
    }
}

/// <summary>A widget that absorbs pointers during hit testing.</summary>
public sealed class AbsorbPointer : SingleChildRenderObjectWidget
{
    public AbsorbPointer(
        Widget? child = null,
        bool absorbing = true,
        bool? ignoringSemantics = null,
        Key? key = null) : base(child, key)
    {
        Absorbing = absorbing;
        IgnoringSemantics = ignoringSemantics;
    }

    /// <summary>Whether this widget absorbs pointers during hit testing.</summary>
    public bool Absorbing { get; }

    /// <summary>Deprecated in Flutter: whether the semantics of this widget are ignored.</summary>
    public bool? IgnoringSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAbsorbPointer(
            absorbing: Absorbing,
            ignoringSemantics: IgnoringSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var absorbPointer = (RenderAbsorbPointer)renderObject;
        absorbPointer.Absorbing = Absorbing;
        absorbPointer.IgnoringSemantics = IgnoringSemantics;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("absorbing", Absorbing));
        properties.Add(new DiagnosticsProperty<bool?>(
            "ignoringSemantics",
            IgnoringSemantics,
            defaultValue: null));
    }
}
