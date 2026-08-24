using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using RelativeRect = Plumix.Rendering.RelativeRect;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/transitions.dart

public abstract class AnimatedWidget : StatefulWidget
{
    protected AnimatedWidget(IListenable listenable, Key? key = null) : base(key)
    {
        Listenable = listenable ?? throw new ArgumentNullException(nameof(listenable));
    }

    public IListenable Listenable { get; }

    public abstract Widget Build(BuildContext context);

    public sealed override State CreateState() => new AnimatedWidgetState();

    private sealed class AnimatedWidgetState : State
    {
        private AnimatedWidget CurrentWidget => (AnimatedWidget)StateWidget;

        public override void InitState()
        {
            CurrentWidget.Listenable.AddListener(HandleChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldAnimatedWidget = (AnimatedWidget)oldWidget;
            if (ReferenceEquals(oldAnimatedWidget.Listenable, CurrentWidget.Listenable))
            {
                return;
            }

            oldAnimatedWidget.Listenable.RemoveListener(HandleChanged);
            CurrentWidget.Listenable.AddListener(HandleChanged);
        }

        public override Widget Build(BuildContext context) => CurrentWidget.Build(context);

        public override void Dispose()
        {
            CurrentWidget.Listenable.RemoveListener(HandleChanged);
        }

        private void HandleChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }
    }
}

public delegate Widget TransitionBuilder(BuildContext context, Widget? child);

public class ListenableBuilder : AnimatedWidget
{
    public ListenableBuilder(
        IListenable listenable,
        TransitionBuilder builder,
        Widget? child = null,
        Key? key = null) : base(listenable, key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Child = child;
    }

    public TransitionBuilder Builder { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context) => Builder(context, Child);
}

public sealed class AnimatedBuilder : ListenableBuilder
{
    public AnimatedBuilder(
        IListenable animation,
        TransitionBuilder builder,
        Widget? child = null,
        Key? key = null) : base(
            listenable: animation,
            builder: builder,
            child: child,
            key: key)
    {
    }

    public IListenable Animation => Listenable;
}

public sealed class SlideTransition : AnimatedWidget
{
    public SlideTransition(
        Animation<Vector> position,
        bool transformHitTests = true,
        TextDirection? textDirection = null,
        Widget? child = null,
        Key? key = null) : base(position ?? throw new ArgumentNullException(nameof(position)), key)
    {
        TransformHitTests = transformHitTests;
        TextDirection = textDirection;
        Child = child;
    }

    public Animation<Vector> Position => (Animation<Vector>)Listenable;

    public bool TransformHitTests { get; }

    public TextDirection? TextDirection { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        Vector offset = Position.Value;
        if (TextDirection == Plumix.UI.TextDirection.Rtl)
        {
            offset = new Vector(-offset.X, offset.Y);
        }

        return new FractionalTranslation(
            translation: offset,
            transformHitTests: TransformHitTests,
            child: Child);
    }
}

public sealed class FadeTransition : AnimatedWidget
{
    public FadeTransition(
        Animation<double> opacity,
        Widget? child = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(opacity ?? throw new ArgumentNullException(nameof(opacity)), key)
    {
        Child = child;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public Animation<double> Opacity => (Animation<double>)Listenable;

    public Widget? Child { get; }

    public bool AlwaysIncludeSemantics { get; }

    public override Widget Build(BuildContext context)
    {
        return new Opacity(
            opacity: Math.Clamp(Opacity.Value, 0.0, 1.0),
            child: Child,
            alwaysIncludeSemantics: AlwaysIncludeSemantics);
    }
}

public sealed class DecoratedBoxTransition : AnimatedWidget
{
    public DecoratedBoxTransition(
        Animation<Decoration> decoration,
        Widget child,
        DecorationPosition position = DecorationPosition.Background,
        Key? key = null) : base(
            decoration ?? throw new ArgumentNullException(nameof(decoration)),
            key)
    {
        Decoration = decoration;
        Position = position;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Animation<Decoration> Decoration { get; }

    public DecorationPosition Position { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new DecoratedBox(
            decoration: Decoration.Value,
            position: Position,
            child: Child);
    }
}

public delegate Matrix4 TransformCallback(double animationValue);

public class MatrixTransition : AnimatedWidget
{
    public MatrixTransition(
        Animation<double> animation,
        TransformCallback onTransform,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Widget? child = null,
        Key? key = null) : base(animation ?? throw new ArgumentNullException(nameof(animation)), key)
    {
        OnTransform = onTransform ?? throw new ArgumentNullException(nameof(onTransform));
        Alignment = alignment;
        FilterQuality = filterQuality;
        Child = child;
    }

    public TransformCallback OnTransform { get; }

    public Animation<double> Animation => (Animation<double>)Listenable;

    public Alignment Alignment { get; }

    public FilterQuality? FilterQuality { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new Transform(
            transform: OnTransform(Animation.Value),
            alignment: Alignment,
            filterQuality: Animation.Status.IsAnimating() ? FilterQuality : null,
            child: Child);
    }
}

public sealed class ScaleTransition : MatrixTransition
{
    public ScaleTransition(
        Animation<double> scale,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Widget? child = null,
        Key? key = null) : base(
            animation: scale,
            onTransform: HandleScaleMatrix,
            alignment: alignment,
            filterQuality: filterQuality,
            child: child,
            key: key)
    {
    }

    public Animation<double> Scale => Animation;

    private static Matrix4 HandleScaleMatrix(double value) => Matrix4.Diagonal3Values(value, value, 1.0);
}

public sealed class RotationTransition : MatrixTransition
{
    public RotationTransition(
        Animation<double> turns,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Widget? child = null,
        Key? key = null) : base(
            animation: turns,
            onTransform: HandleTurnsMatrix,
            alignment: alignment,
            filterQuality: filterQuality,
            child: child,
            key: key)
    {
    }

    public Animation<double> Turns => Animation;

    private static Matrix4 HandleTurnsMatrix(double value) => Matrix4.RotationZ(value * Math.PI * 2.0);
}

public sealed class SizeTransition : AnimatedWidget
{
    private readonly double? _axisAlignment;

    public SizeTransition(
        Animation<double> sizeFactor,
        Axis axis = Axis.Vertical,
        double? axisAlignment = null,
        AlignmentGeometry? alignment = null,
        double? fixedCrossAxisSizeFactor = null,
        Widget? child = null,
        Key? key = null) : base(sizeFactor ?? throw new ArgumentNullException(nameof(sizeFactor)), key)
    {
        if (axisAlignment.HasValue && alignment.HasValue)
        {
            throw new ArgumentException(
                "Cannot provide both axisAlignment and alignment because alignment supersedes axisAlignment.",
                nameof(alignment));
        }

        if (fixedCrossAxisSizeFactor.HasValue
            && (double.IsNaN(fixedCrossAxisSizeFactor.Value) || fixedCrossAxisSizeFactor.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fixedCrossAxisSizeFactor),
                "The fixed cross-axis size factor must be non-negative.");
        }

        Axis = axis;
        _axisAlignment = axisAlignment;
        Alignment = alignment;
        FixedCrossAxisSizeFactor = fixedCrossAxisSizeFactor;
        Child = child;
    }

    public Axis Axis { get; }

    public Animation<double> SizeFactor => (Animation<double>)Listenable;

    [Obsolete("Use Alignment instead. Alignment provides control over both axes.")]
    public double? AxisAlignment => _axisAlignment;

    public AlignmentGeometry? Alignment { get; }

    public double? FixedCrossAxisSizeFactor { get; }

    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        Alignment effectiveAlignment = ResolveAlignment(context);
        double factor = Math.Max(SizeFactor.Value, 0.0);

        return new ClipRect(
            child: new Align(
                alignment: effectiveAlignment,
                heightFactor: Axis == Axis.Vertical ? factor : FixedCrossAxisSizeFactor,
                widthFactor: Axis == Axis.Horizontal ? factor : FixedCrossAxisSizeFactor,
                child: Child));
    }

    private Alignment ResolveAlignment(BuildContext context)
    {
        if (!Alignment.HasValue)
        {
            return ResolveDirectionalAlignment(context);
        }

        AlignmentGeometry alignment = Alignment.Value;
        TextDirection direction = alignment.IsDirectional
            ? Directionality.Of(context)
            : Plumix.UI.TextDirection.Ltr;
        return alignment.Resolve(direction);
    }

    private Alignment ResolveDirectionalAlignment(BuildContext context)
    {
        bool rightToLeft = Directionality.Of(context) == Plumix.UI.TextDirection.Rtl;
        double logicalX = Axis == Axis.Horizontal ? _axisAlignment ?? 0.0 : -1.0;
        double resolvedX = rightToLeft ? -logicalX : logicalX;
        double y = Axis == Axis.Vertical ? _axisAlignment ?? 0.0 : -1.0;
        return new Alignment(resolvedX, y);
    }
}

public sealed class RelativeRectTween : Tween<RelativeRect>
{
    public RelativeRectTween(RelativeRect? begin = null, RelativeRect? end = null)
    {
        Begin = begin;
        End = end;
    }

    public new RelativeRect? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set
        {
            if (value.HasValue)
            {
                SetBeginValue(value.Value);
            }
            else
            {
                ClearBeginValue();
            }
        }
    }

    public new RelativeRect? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set
        {
            if (value.HasValue)
            {
                SetEndValue(value.Value);
            }
            else
            {
                ClearEndValue();
            }
        }
    }

    public override RelativeRect Evaluate(double t)
    {
        return RelativeRect.Lerp(Begin, End, t);
    }

    public override RelativeRect Lerp(RelativeRect a, RelativeRect b, double t)
    {
        return RelativeRect.Lerp(a, b, t);
    }
}

public sealed class PositionedTransition : AnimatedWidget
{
    public PositionedTransition(
        Animation<RelativeRect> rect,
        Widget child,
        Key? key = null) : base(rect ?? throw new ArgumentNullException(nameof(rect)), key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Animation<RelativeRect> Rect => (Animation<RelativeRect>)Listenable;

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Positioned.FromRelativeRect(
            rect: Rect.Value,
            child: Child);
    }
}

public sealed class RelativePositionedTransition : AnimatedWidget
{
    public RelativePositionedTransition(
        Animation<Rect?> rect,
        Size size,
        Widget child,
        Key? key = null) : base(rect ?? throw new ArgumentNullException(nameof(rect)), key)
    {
        Size = size;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Animation<Rect?> Rect => (Animation<Rect?>)Listenable;

    public Size Size { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        RelativeRect offsets = RelativeRect.FromSize(Rect.Value ?? default, Size);
        return new Positioned(
            child: Child,
            left: offsets.Left,
            top: offsets.Top,
            right: offsets.Right,
            bottom: offsets.Bottom);
    }
}

public sealed class AlignTransition : AnimatedWidget
{
    public AlignTransition(
        Animation<AlignmentGeometry> alignment,
        Widget child,
        double? widthFactor = null,
        double? heightFactor = null,
        Key? key = null) : base(alignment ?? throw new ArgumentNullException(nameof(alignment)), key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        WidthFactor = widthFactor;
        HeightFactor = heightFactor;
    }

    public Animation<AlignmentGeometry> Alignment => (Animation<AlignmentGeometry>)Listenable;

    public double? WidthFactor { get; }

    public double? HeightFactor { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new Align(
            alignment: Alignment.Value,
            widthFactor: WidthFactor,
            heightFactor: HeightFactor,
            child: Child);
    }
}

public sealed class DefaultTextStyleTransition : AnimatedWidget
{
    public DefaultTextStyleTransition(
        Animation<TextStyle> style,
        Widget child,
        TextAlign? textAlign = null,
        bool softWrap = true,
        TextOverflow overflow = TextOverflow.Clip,
        int? maxLines = null,
        Key? key = null) : base(style ?? throw new ArgumentNullException(nameof(style)), key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        TextAlign = textAlign;
        SoftWrap = softWrap;
        Overflow = overflow;
        MaxLines = maxLines;
    }

    public Animation<TextStyle> Style => (Animation<TextStyle>)Listenable;

    public TextAlign? TextAlign { get; }

    public bool SoftWrap { get; }

    public TextOverflow Overflow { get; }

    public int? MaxLines { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new DefaultTextStyle(
            style: Style.Value,
            child: Child,
            textAlign: TextAlign,
            softWrap: SoftWrap,
            overflow: Overflow,
            maxLines: MaxLines);
    }
}

/// <summary>An interpolation between two <see cref="TextStyle"/>s.</summary>
public sealed class TextStyleTween : Plumix.Tween<TextStyle>
{
    public TextStyleTween(TextStyle? begin = null, TextStyle? end = null)
    {
        Begin = begin;
        End = end;
    }

    public override TextStyle Lerp(TextStyle a, TextStyle b, double t) => TextStyle.Lerp(a, b, t);
}

public sealed class SliverFadeTransition : SingleChildRenderObjectWidget
{
    public SliverFadeTransition(
        Animation<double> opacity,
        Widget? sliver = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(sliver, key)
    {
        Opacity = opacity ?? throw new ArgumentNullException(nameof(opacity));
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public Animation<double> Opacity { get; }

    public bool AlwaysIncludeSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverAnimatedOpacity(
            opacity: Opacity,
            alwaysIncludeSemantics: AlwaysIncludeSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var opacity = (RenderSliverAnimatedOpacity)renderObject;
        opacity.Opacity = Opacity;
        opacity.AlwaysIncludeSemantics = AlwaysIncludeSemantics;
    }
}
