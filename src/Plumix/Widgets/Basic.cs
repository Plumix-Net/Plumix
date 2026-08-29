using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using RelativeRect = Plumix.Rendering.RelativeRect;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (approximate)

namespace Plumix.Widgets;

public sealed class SizedBox : SingleChildRenderObjectWidget
{
    public SizedBox(double? width = null, double? height = null, Widget? child = null, Key? key = null) : base(child, key)
    {
        Width = width;
        Height = height;
    }

    public double? Width { get; }

    public double? Height { get; }

    // Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (SizedBox.square).
    public static SizedBox Square(double? dimension = null, Widget? child = null, Key? key = null) =>
        new(width: dimension, height: dimension, child: child, key: key);

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderConstrainedBox(BoxConstraints.TightFor(width: Width, height: Height));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderConstrainedBox)renderObject).AdditionalConstraints = BoxConstraints.TightFor(width: Width, height: Height);
    }
}

public sealed class ConstrainedBox : SingleChildRenderObjectWidget
{
    public ConstrainedBox(BoxConstraints constraints, Widget? child = null, Key? key = null) : base(child, key)
    {
        Constraints = constraints;
    }

    public BoxConstraints Constraints { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderConstrainedBox(Constraints);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderConstrainedBox)renderObject).AdditionalConstraints = Constraints;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (UnconstrainedBox)
public sealed class UnconstrainedBox : StatelessWidget
{
    public UnconstrainedBox(
        Widget? child,
        Alignment alignment,
        Axis? constrainedAxis = null,
        Key? key = null) : this(
            child: child,
            alignment: (AlignmentGeometry)alignment,
            constrainedAxis: constrainedAxis,
            key: key)
    {
    }

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

    public Widget? Child { get; }

    public TextDirection? TextDirection { get; }

    public AlignmentGeometry Alignment { get; }

    public Axis? ConstrainedAxis { get; }

    public Clip ClipBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        BoxConstraintsTransform transform = ConstrainedAxis switch
        {
            Axis.Horizontal => ConstraintsTransformBox.HeightUnconstrained,
            Axis.Vertical => ConstraintsTransformBox.WidthUnconstrained,
            null => ConstraintsTransformBox.Unconstrained,
            _ => throw new ArgumentOutOfRangeException(),
        };

        return new ConstraintsTransformBox(
            constraintsTransform: transform,
            child: Child,
            textDirection: TextDirection,
            alignment: Alignment,
            clipBehavior: ClipBehavior);
    }
}

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

    public double MaxWidth { get; }

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

    private static double ValidateMax(double value, string parameterName)
    {
        if (double.IsNaN(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Max value must be non-negative.");
        }

        return value;
    }
}

public sealed class OverflowBox : SingleChildRenderObjectWidget
{
    public OverflowBox(
        Widget? child = null,
        Alignment alignment = default,
        double? minWidth = null,
        double? maxWidth = null,
        double? minHeight = null,
        double? maxHeight = null,
        OverflowBoxFit fit = OverflowBoxFit.Max,
        Key? key = null) : base(child, key)
    {
        MinWidth = ValidateConstraint(minWidth, nameof(minWidth));
        MaxWidth = ValidateConstraint(maxWidth, nameof(maxWidth));
        MinHeight = ValidateConstraint(minHeight, nameof(minHeight));
        MaxHeight = ValidateConstraint(maxHeight, nameof(maxHeight));
        ValidateRanges(MinWidth, MaxWidth, nameof(minWidth), nameof(maxWidth));
        ValidateRanges(MinHeight, MaxHeight, nameof(minHeight), nameof(maxHeight));
        Alignment = alignment;
        Fit = fit;
    }

    public Alignment Alignment { get; }

    public double? MinWidth { get; }

    public double? MaxWidth { get; }

    public double? MinHeight { get; }

    public double? MaxHeight { get; }

    public OverflowBoxFit Fit { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderConstrainedOverflowBox(
            alignment: Alignment,
            minWidth: MinWidth,
            maxWidth: MaxWidth,
            minHeight: MinHeight,
            maxHeight: MaxHeight,
            fit: Fit);
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

public sealed class SizedOverflowBox : SingleChildRenderObjectWidget
{
    public SizedOverflowBox(
        Size size,
        Widget? child = null,
        Alignment alignment = default,
        Key? key = null) : base(child, key)
    {
        Size = size;
        Alignment = alignment;
    }

    public Size Size { get; }

    public Alignment Alignment { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSizedOverflowBox(
            requestedSize: Size,
            alignment: Alignment);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var sizedOverflowBox = (RenderSizedOverflowBox)renderObject;
        sizedOverflowBox.RequestedSize = Size;
        sizedOverflowBox.Alignment = Alignment;
    }
}

public sealed class Offstage : SingleChildRenderObjectWidget
{
    public Offstage(
        Widget? child = null,
        bool offstage = true,
        Key? key = null) : base(child, key)
    {
        IsOffstage = offstage;
    }

    public bool IsOffstage { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderOffstage(offstage: IsOffstage);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderOffstage)renderObject).Offstage = IsOffstage;
    }
}

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

    public bool Ignoring { get; }

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
}

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

    public bool Absorbing { get; }

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
}

public sealed class Padding : SingleChildRenderObjectWidget
{
    public Padding(Thickness insets, Widget? child = null, Key? key = null) : this(
        insets: (EdgeInsetsGeometry)insets,
        child: child,
        key: key)
    {
    }

    public Padding(EdgeInsetsGeometry insets, Widget? child = null, Key? key = null) : base(child, key)
    {
        InsetsGeometry = insets;
    }

    public Thickness Insets => InsetsGeometry.Resolve(TextDirection.Ltr);

    public EdgeInsetsGeometry InsetsGeometry { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPadding(ResolveInsets(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderPadding)renderObject).Padding = ResolveInsets(context);
    }

    private Thickness ResolveInsets(BuildContext context)
    {
        return InsetsGeometry.Resolve(Directionality.Of(context));
    }
}

public sealed class DecoratedBox : SingleChildRenderObjectWidget
{
    public DecoratedBox(
        Decoration decoration,
        Widget? child = null,
        Key? key = null,
        DecorationPosition position = DecorationPosition.Background) : base(child, key)
    {
        Decoration = decoration ?? throw new ArgumentNullException(nameof(decoration));
        Position = position;
    }

    public Decoration Decoration { get; }
    public DecorationPosition Position { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderDecoratedBox(
            Decoration,
            position: Position,
            configuration: CreateImageConfiguration(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var decoratedBox = (RenderDecoratedBox)renderObject;
        decoratedBox.DecorationValue = Decoration;
        decoratedBox.Position = Position;
        decoratedBox.Configuration = CreateImageConfiguration(context);
    }

    private static ImageConfiguration CreateImageConfiguration(BuildContext context)
    {
        return ImageConfigurationUtils.CreateLocalImageConfiguration(context);
    }
}

public sealed class InkSplash : SingleChildRenderObjectWidget
{
    public InkSplash(
        Widget? child = null,
        Color? splashColor = null,
        Point splashOrigin = default,
        double splashProgress = 0,
        double? splashRadius = null,
        bool clipToBounds = true,
        Key? key = null) : base(child, key)
    {
        SplashColor = splashColor;
        SplashOrigin = splashOrigin;
        SplashProgress = splashProgress;
        SplashRadius = splashRadius;
        ClipToBounds = clipToBounds;
    }

    public Color? SplashColor { get; }

    public Point SplashOrigin { get; }

    public double SplashProgress { get; }

    public double? SplashRadius { get; }

    public bool ClipToBounds { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderInkSplash(
            splashColor: SplashColor,
            splashOrigin: SplashOrigin,
            splashProgress: SplashProgress,
            splashRadius: SplashRadius,
            clipToBounds: ClipToBounds);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var inkSplash = (RenderInkSplash)renderObject;
        inkSplash.SplashColor = SplashColor;
        inkSplash.SplashOrigin = SplashOrigin;
        inkSplash.SplashProgress = SplashProgress;
        inkSplash.SplashRadius = SplashRadius;
        inkSplash.ClipToBounds = ClipToBounds;
    }
}

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
        Value = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public double Value { get; }

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
}

public sealed class Transform : SingleChildRenderObjectWidget
{
    private AlignmentGeometry? _geometryAlignment;

    public Transform(
        Matrix4 transform,
        Widget? child = null,
        Point? origin = null,
        Alignment? alignment = null,
        bool transformHitTests = true,
        FilterQuality? filterQuality = null,
        Key? key = null) : base(child, key)
    {
        Matrix = transform;
        Origin = origin;
        Alignment = alignment;
        _geometryAlignment = alignment is { } value ? (AlignmentGeometry)value : null;
        TransformHitTests = transformHitTests;
        FilterQuality = filterQuality;
    }

    /// <summary>Creates a widget that rotates its child by <paramref name="angle"/> clockwise radians.</summary>
    public static Transform Rotate(
        double angle,
        Widget? child = null,
        Point? origin = null,
        Alignment? alignment = null,
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
        Alignment? alignment = null,
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

    /// <summary>
    /// Creates a uniformly scaled transform whose origin may be directional, matching Flutter's
    /// <c>Transform.scale</c> alignment surface.
    /// </summary>
    public static Transform Scale(
        double scale,
        Widget? child,
        AlignmentGeometry alignment,
        bool transformHitTests = true,
        FilterQuality? filterQuality = null,
        Key? key = null)
    {
        var result = Scale(
            scale: scale,
            child: child,
            alignment: null,
            transformHitTests: transformHitTests,
            filterQuality: filterQuality,
            key: key);
        result._geometryAlignment = alignment;
        return result;
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

    public Matrix4 Matrix { get; }
    public Point? Origin { get; }
    public Alignment? Alignment { get; }

    public AlignmentGeometry? GeometryAlignment => _geometryAlignment;
    public bool TransformHitTests { get; }
    public FilterQuality? FilterQuality { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderTransform(
            Matrix,
            ResolveAlignment(context),
            child: null,
            FilterQuality,
            Origin,
            TransformHitTests);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var transform = (RenderTransform)renderObject;
        transform.Transform = Matrix;
        transform.Origin = Origin;
        transform.Alignment = ResolveAlignment(context);
        transform.TransformHitTests = TransformHitTests;
        transform.FilterQuality = FilterQuality;
    }

    private Alignment? ResolveAlignment(BuildContext context)
    {
        if (_geometryAlignment is not { } alignment)
        {
            return null;
        }

        TextDirection direction = alignment.IsDirectional
            ? Directionality.Of(context)
            : TextDirection.Ltr;
        return alignment.Resolve(direction);
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

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart
// (FractionalTranslation, RotatedBox)
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

    public Vector Translation { get; }
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

public sealed class RotatedBox : SingleChildRenderObjectWidget
{
    public RotatedBox(
        int quarterTurns,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        QuarterTurns = quarterTurns;
    }

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

public sealed class ClipRect : SingleChildRenderObjectWidget
{
    public ClipRect(
        Rect? clipRect = null,
        CustomClipper<Rect>? clipper = null,
        Widget? child = null,
        Key? key = null,
        Clip clipBehavior = Plumix.UI.Clip.HardEdge) : base(child, key)
    {
        Clip = clipRect;
        Clipper = clipper;
        ClipBehavior = clipBehavior;
    }

    public Rect? Clip { get; }

    public CustomClipper<Rect>? Clipper { get; }

    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var renderObject = new RenderClipRect(
            clipper: Clipper,
            clipBehavior: ClipBehavior);
        if (Clip.HasValue)
        {
            renderObject.ClipRect = Clip.Value;
        }

        return renderObject;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var clipRect = (RenderClipRect)renderObject;
        clipRect.Clipper = Clipper;
        clipRect.ClipBehavior = ClipBehavior;
        if (Clip.HasValue)
        {
            clipRect.ClipRect = Clip.Value;
        }
        else
        {
            clipRect.ClearClipRect();
        }
    }
}

public sealed class ClipRRect : SingleChildRenderObjectWidget
{
    public ClipRRect(BorderRadius borderRadius, Widget? child = null, Key? key = null) : base(child, key)
    {
        BorderRadius = borderRadius;
    }

    public BorderRadius BorderRadius { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderClipRRect
        {
            BorderRadius = BorderRadius
        };
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderClipRRect)renderObject).BorderRadius = BorderRadius;
    }
}

public sealed class AspectRatio : SingleChildRenderObjectWidget
{
    public AspectRatio(double aspectRatio, Widget? child = null, Key? key = null) : base(child, key)
    {
        Ratio = ValidateRatio(aspectRatio, nameof(aspectRatio));
    }

    public double Ratio { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAspectRatio(Ratio);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderAspectRatio)renderObject).AspectRatio = Ratio;
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

public sealed class FractionallySizedBox : SingleChildRenderObjectWidget
{
    public FractionallySizedBox(
        Widget? child = null,
        Alignment alignment = default,
        double? widthFactor = null,
        double? heightFactor = null,
        Key? key = null) : base(child, key)
    {
        Alignment = alignment;
        WidthFactor = ValidateFactor(widthFactor, nameof(widthFactor));
        HeightFactor = ValidateFactor(heightFactor, nameof(heightFactor));
    }

    public Alignment Alignment { get; }

    public double? WidthFactor { get; }

    public double? HeightFactor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFractionallySizedBox(
            alignment: Alignment,
            widthFactor: WidthFactor,
            heightFactor: HeightFactor);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var fractionallySizedBox = (RenderFractionallySizedBox)renderObject;
        fractionallySizedBox.Alignment = Alignment;
        fractionallySizedBox.WidthFactor = WidthFactor;
        fractionallySizedBox.HeightFactor = HeightFactor;
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

public sealed class FittedBox : SingleChildRenderObjectWidget
{
    public FittedBox(
        Widget? child = null,
        BoxFit fit = BoxFit.Contain,
        Alignment alignment = default,
        Key? key = null) : base(child, key)
    {
        Fit = fit;
        Alignment = alignment;
    }

    public BoxFit Fit { get; }

    public Alignment Alignment { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFittedBox(
            fit: Fit,
            alignment: Alignment);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var fittedBox = (RenderFittedBox)renderObject;
        fittedBox.Fit = Fit;
        fittedBox.Alignment = Alignment;
    }
}

public sealed class Container : StatelessWidget
{
    public Container(
        Widget? child = null,
        Color? color = null,
        Decoration? decoration = null,
        Alignment? alignment = null,
        EdgeInsetsGeometry? margin = null,
        BoxConstraints? constraints = null,
        Matrix4? transform = null,
        EdgeInsetsGeometry? padding = null,
        double? width = null,
        double? height = null,
        Key? key = null,
        Decoration? foregroundDecoration = null,
        Clip clipBehavior = Clip.None) : base(key)
    {
        if (clipBehavior != Clip.None && decoration is null)
        {
            throw new ArgumentException(
                "Clipping a Container requires a decoration to derive the clip path from.",
                nameof(clipBehavior));
        }

        Child = child;
        Color = color;
        Decoration = decoration;
        ForegroundDecoration = foregroundDecoration;
        Alignment = alignment;
        Margin = margin;
        Constraints = constraints;
        Transform = transform;
        Padding = padding;
        Width = width;
        Height = height;
        ClipBehavior = clipBehavior;
    }

    public Widget? Child { get; }

    public Color? Color { get; }

    public Decoration? Decoration { get; }
    public Decoration? ForegroundDecoration { get; }

    public Alignment? Alignment { get; }

    public EdgeInsetsGeometry? Margin { get; }

    public BoxConstraints? Constraints { get; }

    public Matrix4? Transform { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public double? Width { get; }

    public double? Height { get; }

    /// <summary>The clip applied to the decoration's shape; requires <see cref="Decoration"/>.</summary>
    public Clip ClipBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        BoxConstraints? effectiveConstraints = Constraints;
        if (Width.HasValue || Height.HasValue)
        {
            effectiveConstraints = effectiveConstraints.HasValue
                ? effectiveConstraints.Value.Tighten(width: Width, height: Height)
                : BoxConstraints.TightFor(width: Width, height: Height);
        }

        bool expandsNullChild = Child is null
                                && (!effectiveConstraints.HasValue || !effectiveConstraints.Value.IsTight);
        Widget current;
        if (expandsNullChild)
        {
            current = new LimitedBox(
                maxWidth: 0.0,
                maxHeight: 0.0,
                child: new ConstrainedBox(BoxConstraints.Expand()));
        }
        else
        {
            current = Child ?? new SizedBox();
        }

        if (!expandsNullChild && Alignment.HasValue)
        {
            current = new Align(
                alignment: Alignment.Value,
                child: current);
        }

        if (Padding.HasValue)
        {
            current = new Padding(Padding.Value, current);
        }

        if (ClipBehavior != Clip.None)
        {
            current = new ClipPath(
                clipper: new DecorationClipper(Decoration!, Directionality.MaybeOf(context)),
                clipBehavior: ClipBehavior,
                child: current);
        }

        if (Decoration != null)
        {
            current = new DecoratedBox(Decoration, current);
        }
        else if (Color.HasValue)
        {
            current = new ColoredBox(Color.Value, child: current);
        }

        if (ForegroundDecoration != null)
        {
            current = new DecoratedBox(
                ForegroundDecoration,
                position: DecorationPosition.Foreground,
                child: current);
        }

        if (effectiveConstraints.HasValue)
        {
            current = new ConstrainedBox(effectiveConstraints.Value, current);
        }

        if (Margin.HasValue)
        {
            current = new Padding(Margin.Value, current);
        }

        if (Transform is { } containerTransform)
        {
            current = new Transform(containerTransform, current);
        }

        return current;
    }
}

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

    public AlignmentGeometry Alignment { get; }

    public double? WidthFactor { get; }

    public double? HeightFactor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAlign(
            alignment: ResolveAlignment(context),
            widthFactor: WidthFactor,
            heightFactor: HeightFactor);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var align = (RenderAlign)renderObject;
        align.Alignment = ResolveAlignment(context);
        align.WidthFactor = WidthFactor;
        align.HeightFactor = HeightFactor;
    }

    private Alignment ResolveAlignment(BuildContext context)
    {
        TextDirection direction = Alignment.IsDirectional
            ? Directionality.Of(context)
            : TextDirection.Ltr;
        return Alignment.Resolve(direction);
    }
}

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

    public Axis Direction { get; }

    public MainAxisSize MainAxisSize { get; }

    public MainAxisAlignment MainAxisAlignment { get; }

    public CrossAxisAlignment CrossAxisAlignment { get; }

    public double Spacing { get; }

    public TextDirection? TextDirection { get; }

    public VerticalDirection VerticalDirection { get; }

    public TextBaseline? TextBaseline { get; }

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

    public int Flex { get; }

    public FlexFit Fit { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(Flex);

    protected override void ApplyParentData(RenderObject renderObject)
    {
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
        base.DebugFillProperties(properties);
        properties.Add(new IntProperty("flex", Flex));
    }
}

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

public sealed class Spacer : StatelessWidget
{
    public Spacer(int flex = 1, Key? key = null) : base(key)
    {
        if (flex <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flex), "Flex must be greater than zero.");
        }

        Flex = flex;
    }

    public int Flex { get; }

    public override Widget Build(BuildContext context)
    {
        return new Expanded(
            flex: Flex,
            child: new SizedBox(width: 0, height: 0));
    }
}

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

    public Axis Direction { get; }
    public WrapAlignment Alignment { get; }
    public double Spacing { get; }
    public WrapAlignment RunAlignment { get; }
    public double RunSpacing { get; }
    public WrapCrossAlignment CrossAxisAlignment { get; }
    public TextDirection? TextDirection { get; }
    public VerticalDirection VerticalDirection { get; }
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
            textDirection: TextDirection ?? Directionality.Of(context),
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
        wrap.TextDirection = TextDirection ?? Directionality.Of(context);
        wrap.VerticalDirection = VerticalDirection;
        wrap.ClipBehavior = ClipBehavior;
    }
}

public sealed class Stack : MultiChildRenderObjectWidget
{
    public Stack(
        IReadOnlyList<Widget>? children = null,
        AlignmentGeometry alignment = default,
        StackFit fit = StackFit.Loose,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(children, key)
    {
        Alignment = alignment;
        Fit = fit;
        ClipBehavior = clipBehavior;
    }

    public AlignmentGeometry Alignment { get; }

    public StackFit Fit { get; }

    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderStack(
            alignment: ResolveAlignment(context),
            fit: Fit,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var stack = (RenderStack)renderObject;
        stack.Alignment = ResolveAlignment(context);
        stack.Fit = Fit;
        stack.ClipBehavior = ClipBehavior;
    }

    private Alignment ResolveAlignment(BuildContext context)
    {
        TextDirection direction = Alignment.IsDirectional
            ? Directionality.Of(context)
            : TextDirection.Ltr;
        return Alignment.Resolve(direction);
    }
}

public sealed class IndexedStack : StatelessWidget
{
    public IndexedStack(
        IReadOnlyList<Widget>? children = null,
        int? index = 0,
        AlignmentGeometry alignment = default,
        Key? key = null) : base(key)
    {
        Children = children ?? [];
        if (index.HasValue && (index.Value < 0 || index.Value >= Children.Count))
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        Alignment = alignment;
    }

    public IReadOnlyList<Widget> Children { get; }

    public int? Index { get; }

    public AlignmentGeometry Alignment { get; }

    public override Widget Build(BuildContext context)
    {
        // Each child is wrapped with VisibilityScope (so Visibility.Of reports the child as hidden
        // when it is not the selected index) and with ExcludeFocus (so non-selected children cannot
        // receive focus). Neither introduces a RenderObject between the child and the enclosing
        // RenderIndexedStack, so ParentDataWidgets such as Positioned still apply their
        // StackParentData. Painting, hit-testing and semantics for non-selected children are
        // already handled by RenderIndexedStack.
        List<Widget> wrappedChildren = new(Children.Count);
        for (int i = 0; i < Children.Count; i++)
        {
            bool isSelected = i == Index;
            wrappedChildren.Add(new VisibilityScope(
                isSelected,
                new ExcludeFocus(Children[i], excluding: !isSelected)));
        }

        return new RawIndexedStack(wrappedChildren, Index, Alignment);
    }
}

/// The render object widget that backs <see cref="IndexedStack"/>. Dart's private
/// `_RawIndexedStack`.
internal sealed class RawIndexedStack : MultiChildRenderObjectWidget
{
    public RawIndexedStack(
        IReadOnlyList<Widget>? children = null,
        int? index = 0,
        AlignmentGeometry alignment = default,
        Key? key = null) : base(children, key)
    {
        Index = index;
        Alignment = alignment;
    }

    public int? Index { get; }

    public AlignmentGeometry Alignment { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderIndexedStack(Index, ResolveAlignment(context));

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var stack = (RenderIndexedStack)renderObject;
        stack.Index = Index;
        stack.Alignment = ResolveAlignment(context);
    }

    private Alignment ResolveAlignment(BuildContext context)
    {
        TextDirection direction = Alignment.IsDirectional
            ? Directionality.Of(context)
            : TextDirection.Ltr;
        return Alignment.Resolve(direction);
    }
}

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
        double? left = textDirection == TextDirection.Ltr ? start : end;
        double? right = textDirection == TextDirection.Ltr ? end : start;
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

    public double? Left { get; }

    public double? Top { get; }

    public double? Right { get; }

    public double? Bottom { get; }

    public double? Width { get; }

    public double? Height { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(Stack);

    protected override void ApplyParentData(RenderObject renderObject)
    {
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
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (PositionedDirectional)
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

    public double? Start { get; }

    public double? Top { get; }

    public double? End { get; }

    public double? Bottom { get; }

    public double? Width { get; }

    public double? Height { get; }

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
