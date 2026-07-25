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

public sealed class UnconstrainedBox : SingleChildRenderObjectWidget
{
    public UnconstrainedBox(
        Widget? child = null,
        Alignment alignment = default,
        Axis? constrainedAxis = null,
        Key? key = null) : base(child, key)
    {
        Alignment = alignment;
        ConstrainedAxis = constrainedAxis;
    }

    public Alignment Alignment { get; }

    public Axis? ConstrainedAxis { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderUnconstrainedBox(
            alignment: Alignment,
            constrainedAxis: ConstrainedAxis);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var unconstrainedBox = (RenderUnconstrainedBox)renderObject;
        unconstrainedBox.Alignment = Alignment;
        unconstrainedBox.ConstrainedAxis = ConstrainedAxis;
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
    public Padding(Thickness insets, Widget? child = null, Key? key = null) : base(child, key)
    {
        Insets = insets;
    }

    public Thickness Insets { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderPadding(Insets);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderPadding)renderObject).Padding = Insets;
    }
}

public sealed class ColoredBox : SingleChildRenderObjectWidget
{
    public ColoredBox(Color color, Widget? child = null, Key? key = null) : base(child, key)
    {
        Color = color;
    }

    public Color Color { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderColoredBox(Color);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderColoredBox)renderObject).Color = Color;
    }
}

public sealed class DecoratedBox : SingleChildRenderObjectWidget
{
    public DecoratedBox(
        BoxDecoration decoration,
        Widget? child = null,
        Key? key = null,
        DecorationPosition position = DecorationPosition.Background) : base(child, key)
    {
        Decoration = decoration ?? new BoxDecoration();
        Position = position;
    }

    public BoxDecoration Decoration { get; }
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
        decoratedBox.Decoration = Decoration;
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
    public Transform(
        Matrix transform,
        Widget? child = null,
        Alignment? alignment = null,
        FilterQuality? filterQuality = null,
        Key? key = null) : base(child, key)
    {
        Matrix = transform;
        Alignment = alignment;
        FilterQuality = filterQuality;
    }

    public Matrix Matrix { get; }
    public Alignment? Alignment { get; }
    public FilterQuality? FilterQuality { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderTransform(Matrix, Alignment, child: null, FilterQuality);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var transform = (RenderTransform)renderObject;
        transform.Transform = Matrix;
        transform.Alignment = Alignment;
        transform.FilterQuality = FilterQuality;
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
        BoxDecoration? decoration = null,
        Alignment? alignment = null,
        Thickness? margin = null,
        BoxConstraints? constraints = null,
        Matrix? transform = null,
        Thickness? padding = null,
        double? width = null,
        double? height = null,
        Key? key = null,
        BoxDecoration? foregroundDecoration = null) : base(key)
    {
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
    }

    public Widget? Child { get; }

    public Color? Color { get; }

    public BoxDecoration? Decoration { get; }
    public BoxDecoration? ForegroundDecoration { get; }

    public Alignment? Alignment { get; }

    public Thickness? Margin { get; }

    public BoxConstraints? Constraints { get; }

    public Matrix? Transform { get; }

    public Thickness? Padding { get; }

    public double? Width { get; }

    public double? Height { get; }

    public override Widget Build(BuildContext context)
    {
        Widget current = Child ?? new SizedBox();

        if (Alignment.HasValue)
        {
            current = new Align(
                alignment: Alignment.Value,
                child: current);
        }

        if (Padding.HasValue)
        {
            current = new Padding(Padding.Value, current);
        }

        if (Decoration != null)
        {
            current = new DecoratedBox(Decoration, current);
        }
        else if (Color.HasValue)
        {
            current = new ColoredBox(Color.Value, current);
        }

        if (ForegroundDecoration != null)
        {
            current = new DecoratedBox(
                ForegroundDecoration,
                position: DecorationPosition.Foreground,
                child: current);
        }

        BoxConstraints? effectiveConstraints = Constraints;
        if (Width.HasValue || Height.HasValue)
        {
            effectiveConstraints = effectiveConstraints.HasValue
                ? effectiveConstraints.Value.Tighten(width: Width, height: Height)
                : BoxConstraints.TightFor(width: Width, height: Height);
        }

        if (effectiveConstraints.HasValue)
        {
            current = new ConstrainedBox(effectiveConstraints.Value, current);
        }

        if (Margin.HasValue)
        {
            current = new Padding(Margin.Value, current);
        }

        if (Transform.HasValue)
        {
            current = new Transform(Transform.Value, current);
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
        TextBaseline? textBaseline = null) : base(children, key)
    {
        Direction = direction;
        MainAxisSize = mainAxisSize;
        MainAxisAlignment = mainAxisAlignment;
        CrossAxisAlignment = crossAxisAlignment;
        Spacing = spacing;
        TextDirection = textDirection;
        TextBaseline = textBaseline;
    }

    public Axis Direction { get; }

    public MainAxisSize MainAxisSize { get; }

    public MainAxisAlignment MainAxisAlignment { get; }

    public CrossAxisAlignment CrossAxisAlignment { get; }

    public double Spacing { get; }

    public TextDirection? TextDirection { get; }

    public TextBaseline? TextBaseline { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFlex(
            children: null,
            direction: Direction,
            mainAxisSize: MainAxisSize,
            mainAxisAlignment: MainAxisAlignment,
            crossAxisAlignment: CrossAxisAlignment,
            textDirection: TextDirection,
            textBaseline: TextBaseline,
            spacing: Spacing);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var flex = (RenderFlex)renderObject;
        flex.Direction = Direction;
        flex.MainAxisSize = MainAxisSize;
        flex.MainAxisAlignment = MainAxisAlignment;
        flex.CrossAxisAlignment = CrossAxisAlignment;
        flex.TextDirection = TextDirection;
        flex.TextBaseline = TextBaseline;
        flex.Spacing = Spacing;
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
        TextBaseline? textBaseline = null) : base(
        direction: Axis.Horizontal,
        children: children,
        mainAxisSize: mainAxisSize,
        mainAxisAlignment: mainAxisAlignment,
        crossAxisAlignment: crossAxisAlignment,
        spacing: spacing,
        key: key,
        textDirection: textDirection,
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
        TextBaseline? textBaseline = null) : base(
        direction: Axis.Vertical,
        children: children,
        mainAxisSize: mainAxisSize,
        mainAxisAlignment: mainAxisAlignment,
        crossAxisAlignment: crossAxisAlignment,
        spacing: spacing,
        key: key,
        textDirection: textDirection,
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
        Alignment alignment = default,
        StackFit fit = StackFit.Loose,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(children, key)
    {
        Alignment = alignment;
        Fit = fit;
        ClipBehavior = clipBehavior;
    }

    public Alignment Alignment { get; }

    public StackFit Fit { get; }

    public Clip ClipBehavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderStack(
            alignment: Alignment,
            fit: Fit,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var stack = (RenderStack)renderObject;
        stack.Alignment = Alignment;
        stack.Fit = Fit;
        stack.ClipBehavior = ClipBehavior;
    }
}

public sealed class IndexedStack : MultiChildRenderObjectWidget
{
    public IndexedStack(
        IReadOnlyList<Widget>? children = null,
        int? index = 0,
        Alignment alignment = default,
        Key? key = null) : base(children, key)
    {
        if (index.HasValue && (index.Value < 0 || index.Value >= Children.Count))
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        Alignment = alignment;
    }

    public int? Index { get; }

    public Alignment Alignment { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderIndexedStack(Index, Alignment);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var stack = (RenderIndexedStack)renderObject;
        stack.Index = Index;
        stack.Alignment = Alignment;
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
