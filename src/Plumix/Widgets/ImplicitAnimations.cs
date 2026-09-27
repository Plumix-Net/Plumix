using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/implicit_animations.dart

/// <summary>An interpolation between two <see cref="BoxConstraints"/>.</summary>
public class BoxConstraintsTween : Tween<BoxConstraints>
{
    /// <summary>
    /// Creates a <see cref="BoxConstraints"/> tween. A <c>null</c> end is unset, as in Dart.
    /// </summary>
    public BoxConstraintsTween(BoxConstraints? begin = null, BoxConstraints? end = null)
    {
        Begin = begin;
        End = end;
    }

    /// <inheritdoc cref="Tween{T}.Begin" />
    public new BoxConstraints? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set => SetOrClearBegin(value);
    }

    /// <inheritdoc cref="Tween{T}.End" />
    public new BoxConstraints? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set => SetOrClearEnd(value);
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override BoxConstraints Lerp(double t) => BoxConstraints.Lerp(Begin, End, t)!.Value;

    private void SetOrClearBegin(BoxConstraints? value)
    {
        if (value is { } constraints)
        {
            SetBeginValue(constraints);
        }
        else
        {
            ClearBeginValue();
        }
    }

    private void SetOrClearEnd(BoxConstraints? value)
    {
        if (value is { } constraints)
        {
            SetEndValue(constraints);
        }
        else
        {
            ClearEndValue();
        }
    }
}

/// <summary>An interpolation between two <see cref="Decoration"/>s.</summary>
public class DecorationTween : Tween<Decoration>
{
    /// <summary>Creates a decoration tween.</summary>
    public DecorationTween(Decoration? begin = null, Decoration? end = null)
        : base(begin, end)
    {
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override Decoration Lerp(double t)
    {
        // Dart's `Decoration.lerp(begin, end, t)!`: two null ends fail the null check.
        return Decoration.Lerp(Begin, End, t)
               ?? throw new InvalidOperationException("Null check operator used on a null value");
    }
}

/// <summary>An interpolation between two <see cref="EdgeInsets"/>s.</summary>
public class EdgeInsetsTween : Tween<EdgeInsets>
{
    /// <summary>Creates an <see cref="EdgeInsets"/> tween. A <c>null</c> end is unset, as in Dart.</summary>
    public EdgeInsetsTween(EdgeInsets? begin = null, EdgeInsets? end = null)
    {
        Begin = begin;
        End = end;
    }

    /// <inheritdoc cref="Tween{T}.Begin" />
    public new EdgeInsets? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set
        {
            if (value is { } insets)
            {
                SetBeginValue(insets);
            }
            else
            {
                ClearBeginValue();
            }
        }
    }

    /// <inheritdoc cref="Tween{T}.End" />
    public new EdgeInsets? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set
        {
            if (value is { } insets)
            {
                SetEndValue(insets);
            }
            else
            {
                ClearEndValue();
            }
        }
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override EdgeInsets Lerp(double t) => EdgeInsets.Lerp(Begin, End, t)!.Value;
}

/// <summary>An interpolation between two <see cref="EdgeInsetsGeometry"/>s.</summary>
public class EdgeInsetsGeometryTween : Tween<EdgeInsetsGeometry>
{
    /// <summary>
    /// Creates an <see cref="EdgeInsetsGeometry"/> tween. A <c>null</c> end is unset, as in Dart.
    /// </summary>
    public EdgeInsetsGeometryTween(EdgeInsetsGeometry? begin = null, EdgeInsetsGeometry? end = null)
    {
        Begin = begin;
        End = end;
    }

    /// <inheritdoc cref="Tween{T}.Begin" />
    public new EdgeInsetsGeometry? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set
        {
            if (value is { } insets)
            {
                SetBeginValue(insets);
            }
            else
            {
                ClearBeginValue();
            }
        }
    }

    /// <inheritdoc cref="Tween{T}.End" />
    public new EdgeInsetsGeometry? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set
        {
            if (value is { } insets)
            {
                SetEndValue(insets);
            }
            else
            {
                ClearEndValue();
            }
        }
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override EdgeInsetsGeometry Lerp(double t) => EdgeInsetsGeometry.Lerp(Begin, End, t)!.Value;
}

/// <summary>An interpolation between two <see cref="BorderRadius"/>s.</summary>
public class BorderRadiusTween : Tween<BorderRadius?>
{
    /// <summary>Creates a <see cref="BorderRadius"/> tween.</summary>
    public BorderRadiusTween(BorderRadius? begin = null, BorderRadius? end = null)
        : base(begin, end)
    {
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override BorderRadius? Lerp(double t) => BorderRadius.Lerp(Begin, End, t);
}

/// <summary>An interpolation between two <see cref="Border"/>s.</summary>
public class BorderTween : Tween<Border?>
{
    /// <summary>Creates a <see cref="Border"/> tween.</summary>
    public BorderTween(Border? begin = null, Border? end = null)
        : base(begin, end)
    {
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override Border? Lerp(double t) => Border.Lerp(Begin, End, t);
}

/// <summary>An interpolation between two <see cref="Matrix4"/>s.</summary>
/// <remarks>
/// Each end is decomposed into translation, rotation and scale, the three parts are interpolated
/// separately, and the result is recomposed. Rotation uses a normalized linear quaternion blend, so a
/// constant-speed rotation is approximated rather than exact.
/// </remarks>
public class Matrix4Tween : Tween<Matrix4>
{
    /// <summary>Creates a <see cref="Matrix4"/> tween.</summary>
    public Matrix4Tween(Matrix4? begin = null, Matrix4? end = null)
        : base(begin, end)
    {
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override Matrix4 Lerp(double t)
    {
        DebugAssertions.Assert(Begin is not null);
        DebugAssertions.Assert(End is not null);
        Vector3 beginTranslation = Vector3.Zero();
        Vector3 endTranslation = Vector3.Zero();
        Quaternion beginRotation = Quaternion.Identity();
        Quaternion endRotation = Quaternion.Identity();
        Vector3 beginScale = Vector3.Zero();
        Vector3 endScale = Vector3.Zero();
        Begin!.Decompose(beginTranslation, beginRotation, beginScale);
        End!.Decompose(endTranslation, endRotation, endScale);
        Vector3 lerpTranslation = (beginTranslation * (1.0 - t)) + (endTranslation * t);
        Quaternion lerpRotation = (beginRotation.Scaled(1.0 - t) + endRotation.Scaled(t)).Normalized();
        Vector3 lerpScale = (beginScale * (1.0 - t)) + (endScale * t);
        return Matrix4.Compose(lerpTranslation, lerpRotation, lerpScale);
    }
}

/// <summary>An interpolation between two <see cref="TextStyle"/>s.</summary>
/// <remarks>This will not work well if the styles don't set the same fields.</remarks>
public class TextStyleTween : Tween<TextStyle>
{
    /// <summary>Creates a text style tween.</summary>
    public TextStyleTween(TextStyle? begin = null, TextStyle? end = null)
        : base(begin, end)
    {
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override TextStyle Lerp(double t)
    {
        // Dart's `TextStyle.lerp(begin, end, t)!`.
        return TextStyle.Lerp(Begin, End, t)
               ?? throw new InvalidOperationException("Null check operator used on a null value");
    }
}

/// <summary>
/// An abstract class for building widgets that animate changes to their properties. Dart's
/// <c>ImplicitlyAnimatedWidget</c>.
/// </summary>
public abstract class ImplicitlyAnimatedWidget : StatefulWidget
{
    /// <summary>
    /// Initializes fields for subclasses. <paramref name="curve"/> defaults to <c>Curves.linear</c>.
    /// </summary>
    protected ImplicitlyAnimatedWidget(
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(key)
    {
        Curve = curve ?? Curves.Linear;
        Duration = duration;
        OnEnd = onEnd;
    }

    /// <summary>The curve to apply when animating the parameters of this container.</summary>
    public Curve Curve { get; }

    /// <summary>The duration over which to animate the parameters of this container.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Called every time an animation completes.</summary>
    public Action? OnEnd { get; }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new IntProperty("duration", (int)Duration.TotalMilliseconds, unit: "ms"));
    }
}

/// <summary>
/// Signature for a <see cref="Tween{T}"/> factory. Dart's <c>TweenConstructor</c>.
/// </summary>
public delegate Tween<T> TweenConstructor<T>(T targetValue);

/// <summary>
/// Dart's <c>TweenVisitor&lt;dynamic&gt;</c>: the function
/// <see cref="ImplicitlyAnimatedWidgetState{T}.ForEachTween"/> calls for every tween.
/// </summary>
/// <remarks>
/// C# cannot pass a generic lambda, so the visitor is an object with a generic method. Dart's target is
/// a nullable <c>T</c>; C#'s <c>T?</c> on an unconstrained type parameter cannot hold <c>null</c> for a
/// value type, so a value-type target goes through the <see cref="Nullable{T}"/> overload.
/// </remarks>
public abstract class TweenVisitor
{
    /// <summary>
    /// Visits one tween whose target is a reference type (or a <see cref="Nullable{T}"/> tween type):
    /// returns the (possibly newly constructed) tween to store, or <c>null</c> when
    /// <paramref name="targetValue"/> is <c>null</c>.
    /// </summary>
    public Tween<T>? Visit<T>(Tween<T>? tween, T? targetValue, TweenConstructor<T> constructor)
    {
        return VisitTween(tween, targetValue is not null, targetValue!, constructor);
    }

    /// <summary>Visits one tween whose target is a value type; see the other overload.</summary>
    public Tween<T>? Visit<T>(Tween<T>? tween, T? targetValue, TweenConstructor<T> constructor)
        where T : struct
    {
        return VisitTween(tween, targetValue.HasValue, targetValue.GetValueOrDefault(), constructor);
    }

    /// <summary>
    /// The visitor body: <paramref name="hasTargetValue"/> is <c>false</c> where Dart's target is
    /// <c>null</c>.
    /// </summary>
    protected abstract Tween<T>? VisitTween<T>(
        Tween<T>? tween,
        bool hasTargetValue,
        T targetValue,
        TweenConstructor<T> constructor);
}

/// <summary>
/// A base class for the <see cref="State"/> of widgets with implicit animations. Dart's
/// <c>ImplicitlyAnimatedWidgetState</c>.
/// </summary>
/// <remarks>
/// Dart mixes in <c>SingleTickerProviderStateMixin</c>; every Plumix <see cref="State"/> is already a
/// ticker provider (see DIVERGENCES.md).
/// </remarks>
public abstract class ImplicitlyAnimatedWidgetState<T> : State<T> where T : ImplicitlyAnimatedWidget
{
    private AnimationController? _controller;
    private CurvedAnimation? _animation;

    /// <summary>
    /// The animation controller driving this widget's implicit animations. Dart's <c>late final</c>
    /// field: created on first access.
    /// </summary>
    protected AnimationController Controller => _controller ??= new AnimationController(
        duration: Widget.Duration,
        debugLabel: Constants.KDebugMode ? Widget.ToStringShort() : null,
        vsync: this);

    /// <summary>The animation driving this widget's implicit animations.</summary>
    public Animation<double> Animation => CurrentAnimation;

    // Dart's `late CurvedAnimation _animation = _createCurve()`: created on first read.
    private CurvedAnimation CurrentAnimation => _animation ??= CreateCurve();

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        Controller.AddStatusListener(status =>
        {
            if (status.IsCompleted())
            {
                Widget.OnEnd?.Invoke();
            }
        });
        ConstructTweens();
        DidUpdateTweens();
    }

    /// <inheritdoc />
    public override void DidUpdateWidget(T oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (Widget.Curve != oldWidget.Curve)
        {
            CurrentAnimation.Dispose();
            _animation = CreateCurve();
        }

        Controller.Duration = Widget.Duration;
        if (ConstructTweens())
        {
            ForEachTween(new RetargetTweenVisitor(CurrentAnimation));
            Controller.Forward(from: 0.0);
            DidUpdateTweens();
        }
    }

    private CurvedAnimation CreateCurve()
    {
        return new CurvedAnimation(parent: Controller, curve: Widget.Curve);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        CurrentAnimation.Dispose();
        Controller.Dispose();
        base.Dispose();
    }

    private bool ConstructTweens()
    {
        var visitor = new ConstructTweenVisitor();
        ForEachTween(visitor);
        return visitor.ShouldStartAnimation;
    }

    /// <summary>
    /// Visits each tween controlled by this state with <paramref name="visitor"/>, storing the tween
    /// the visitor returns. Dart's <c>forEachTween</c>.
    /// </summary>
    /// <remarks>
    /// A tween is stored as <c>_x = (XTween?)visitor.Visit(_x, targetValue, value =&gt; new XTween(value))</c>.
    /// </remarks>
    protected abstract void ForEachTween(TweenVisitor visitor);

    /// <summary>
    /// Optional hook for subclasses that runs after all tweens have been updated (and therefore after
    /// the animation changed).
    /// </summary>
    protected virtual void DidUpdateTweens()
    {
    }

    // Dart's `tween.end ?? tween.begin`.
    private static object? EndOrBegin<TValue>(Tween<TValue> tween)
    {
        if (tween.HasEndValue)
        {
            return tween.GetEndValue();
        }

        return tween.HasBeginValue ? tween.GetBeginValue() : null;
    }

    private sealed class ConstructTweenVisitor : TweenVisitor
    {
        public bool ShouldStartAnimation { get; private set; }

        protected override Tween<TValue>? VisitTween<TValue>(
            Tween<TValue>? tween,
            bool hasTargetValue,
            TValue targetValue,
            TweenConstructor<TValue> constructor)
        {
            if (hasTargetValue)
            {
                tween ??= constructor(targetValue);
                if (!Equals(targetValue, EndOrBegin(tween)))
                {
                    ShouldStartAnimation = true;
                }
                else if (!tween.HasEndValue && tween.HasBeginValue)
                {
                    // Dart's `tween.end ??= tween.begin`.
                    tween.SetEndValue(tween.GetBeginValue());
                }
            }
            else
            {
                tween = null;
            }

            return tween;
        }
    }

    // Dart's inline visitor in `didUpdateWidget`: `tween?..begin = tween.evaluate(_animation)..end = targetValue`.
    private sealed class RetargetTweenVisitor(Animation<double> animation) : TweenVisitor
    {
        protected override Tween<TValue>? VisitTween<TValue>(
            Tween<TValue>? tween,
            bool hasTargetValue,
            TValue targetValue,
            TweenConstructor<TValue> constructor)
        {
            if (tween is null)
            {
                return null;
            }

            TValue current = tween.Evaluate(animation);
            if (current is null)
            {
                tween.ClearBeginValue();
            }
            else
            {
                tween.SetBeginValue(current);
            }

            if (hasTargetValue && targetValue is not null)
            {
                tween.SetEndValue(targetValue);
            }
            else
            {
                tween.ClearEndValue();
            }

            return tween;
        }
    }
}

/// <summary>
/// A base class for widgets with implicit animations that need to rebuild their widget tree as the
/// animation runs. Dart's <c>AnimatedWidgetBaseState</c>.
/// </summary>
public abstract class AnimatedWidgetBaseState<T> : ImplicitlyAnimatedWidgetState<T>
    where T : ImplicitlyAnimatedWidget
{
    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        Controller.AddListener(HandleAnimationChanged);
    }

    private void HandleAnimationChanged()
    {
        SetState(() =>
        {
            // The animation ticked. Rebuild with new animation value.
        });
    }
}

/// <summary>
/// Animated version of <see cref="Container"/> that gradually changes its values over a period of time.
/// </summary>
public class AnimatedContainer : ImplicitlyAnimatedWidget
{
    /// <summary>
    /// Creates a container that animates its parameters implicitly. <paramref name="color"/>,
    /// <paramref name="width"/> and <paramref name="height"/> are shorthands folded into
    /// <see cref="Decoration"/> and <see cref="Constraints"/>.
    /// </summary>
    public AnimatedContainer(
        TimeSpan duration,
        AlignmentGeometry? alignment = null,
        EdgeInsetsGeometry? padding = null,
        Color? color = null,
        Decoration? decoration = null,
        Decoration? foregroundDecoration = null,
        double? width = null,
        double? height = null,
        BoxConstraints? constraints = null,
        EdgeInsetsGeometry? margin = null,
        Matrix4? transform = null,
        AlignmentGeometry? transformAlignment = null,
        Widget? child = null,
        Clip clipBehavior = Clip.None,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode)
        {
            if (margin is { } marginValue && !marginValue.IsNonNegative)
            {
                throw new AssertionError("margin == null || margin.isNonNegative");
            }

            if (padding is { } paddingValue && !paddingValue.IsNonNegative)
            {
                throw new AssertionError("padding == null || padding.isNonNegative");
            }

            constraints?.DebugAssertIsValid();
            if (color is not null && decoration is not null)
            {
                throw new AssertionError(
                    "Cannot provide both a color and a decoration\n"
                    + "The color argument is just a shorthand for \"decoration: BoxDecoration(color: color)\".");
            }
        }

        Alignment = alignment;
        Padding = padding;
        Decoration = decoration ?? (color is not null ? new BoxDecoration(Color: color) : null);
        ForegroundDecoration = foregroundDecoration;
        Constraints = width is not null || height is not null
            ? constraints?.Tighten(width: width, height: height)
              ?? BoxConstraints.TightFor(width: width, height: height)
            : constraints;
        Margin = margin;
        Transform = transform;
        TransformAlignment = transformAlignment;
        Child = child;
        ClipBehavior = clipBehavior;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>Align the child within the container.</summary>
    public AlignmentGeometry? Alignment { get; }

    /// <summary>Empty space to inscribe inside the decoration.</summary>
    public EdgeInsetsGeometry? Padding { get; }

    /// <summary>The decoration to paint behind the child.</summary>
    public Decoration? Decoration { get; }

    /// <summary>The decoration to paint in front of the child.</summary>
    public Decoration? ForegroundDecoration { get; }

    /// <summary>Additional constraints to apply to the child.</summary>
    public BoxConstraints? Constraints { get; }

    /// <summary>Empty space to surround the decoration and child.</summary>
    public EdgeInsetsGeometry? Margin { get; }

    /// <summary>The transformation matrix to apply before painting the container.</summary>
    public Matrix4? Transform { get; }

    /// <summary>The alignment of the origin, relative to the size of the container.</summary>
    public AlignmentGeometry? TransformAlignment { get; }

    /// <summary>The clip behavior when <see cref="Decoration"/> is not null.</summary>
    public Clip ClipBehavior { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedContainerState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>(
            "alignment",
            Alignment,
            showName: false,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>(
            "padding",
            Padding,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<Decoration>(
            "bg",
            Decoration,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<Decoration>(
            "fg",
            ForegroundDecoration,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>(
            "constraints",
            Constraints,
            defaultValue: DiagnosticsDefaults.NullValue,
            showName: false));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>(
            "margin",
            Margin,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(ObjectFlagProperty<Matrix4>.Has("transform", Transform));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>(
            "transformAlignment",
            TransformAlignment,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<Clip>("clipBehavior", ClipBehavior));
    }

    private sealed class AnimatedContainerState : AnimatedWidgetBaseState<AnimatedContainer>
    {
        private AlignmentGeometryTween? _alignment;
        private EdgeInsetsGeometryTween? _padding;
        private DecorationTween? _decoration;
        private DecorationTween? _foregroundDecoration;
        private BoxConstraintsTween? _constraints;
        private EdgeInsetsGeometryTween? _margin;
        private Matrix4Tween? _transform;
        private AlignmentGeometryTween? _transformAlignment;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _alignment = (AlignmentGeometryTween?)visitor.Visit(
                _alignment,
                Widget.Alignment,
                value => new AlignmentGeometryTween(begin: value));
            _padding = (EdgeInsetsGeometryTween?)visitor.Visit(
                _padding,
                Widget.Padding,
                value => new EdgeInsetsGeometryTween(begin: value));
            _decoration = (DecorationTween?)visitor.Visit(
                _decoration,
                Widget.Decoration,
                value => new DecorationTween(begin: value));
            _foregroundDecoration = (DecorationTween?)visitor.Visit(
                _foregroundDecoration,
                Widget.ForegroundDecoration,
                value => new DecorationTween(begin: value));
            _constraints = (BoxConstraintsTween?)visitor.Visit(
                _constraints,
                Widget.Constraints,
                value => new BoxConstraintsTween(begin: value));
            _margin = (EdgeInsetsGeometryTween?)visitor.Visit(
                _margin,
                Widget.Margin,
                value => new EdgeInsetsGeometryTween(begin: value));
            _transform = (Matrix4Tween?)visitor.Visit(
                _transform,
                Widget.Transform,
                value => new Matrix4Tween(begin: value));
            _transformAlignment = (AlignmentGeometryTween?)visitor.Visit(
                _transformAlignment,
                Widget.TransformAlignment,
                value => new AlignmentGeometryTween(begin: value));
        }

        public override Widget Build(BuildContext context)
        {
            Animation<double> animation = Animation;
            return new Container(
                alignment: _alignment?.Evaluate(animation),
                padding: EvaluateInsets(_padding, animation),
                decoration: _decoration?.Evaluate(animation),
                foregroundDecoration: _foregroundDecoration?.Evaluate(animation),
                constraints: EvaluateConstraints(_constraints, animation),
                margin: EvaluateInsets(_margin, animation),
                transform: _transform?.Evaluate(animation),
                transformAlignment: _transformAlignment?.Evaluate(animation),
                clipBehavior: Widget.ClipBehavior,
                child: Widget.Child);
        }

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            properties.Add(new DiagnosticsProperty<AlignmentGeometryTween>(
                "alignment",
                _alignment,
                showName: false,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<EdgeInsetsGeometryTween>(
                "padding",
                _padding,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<DecorationTween>(
                "bg",
                _decoration,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<DecorationTween>(
                "fg",
                _foregroundDecoration,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<BoxConstraintsTween>(
                "constraints",
                _constraints,
                showName: false,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<EdgeInsetsGeometryTween>(
                "margin",
                _margin,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(ObjectFlagProperty<Matrix4Tween>.Has("transform", _transform));
            properties.Add(new DiagnosticsProperty<AlignmentGeometryTween>(
                "transformAlignment",
                _transformAlignment,
                defaultValue: DiagnosticsDefaults.NullValue));
        }

        // Dart's `tween?.evaluate(animation)` over a value-type tween.
        private static EdgeInsetsGeometry? EvaluateInsets(EdgeInsetsGeometryTween? tween, Animation<double> animation)
        {
            return tween is null ? null : tween.Evaluate(animation);
        }

        private static BoxConstraints? EvaluateConstraints(BoxConstraintsTween? tween, Animation<double> animation)
        {
            return tween is null ? null : tween.Evaluate(animation);
        }
    }
}

/// <summary>
/// Animated version of <see cref="Widgets.Padding"/> which automatically transitions the indentation
/// over a given duration whenever the given inset changes.
/// </summary>
public class AnimatedPadding : ImplicitlyAnimatedWidget
{
    /// <summary>Creates a widget that insets its child by a value that animates implicitly.</summary>
    public AnimatedPadding(
        EdgeInsetsGeometry padding,
        TimeSpan duration,
        Widget? child = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode && !padding.IsNonNegative)
        {
            throw new AssertionError("padding.isNonNegative");
        }

        Padding = padding;
        Child = child;
    }

    /// <summary>The amount of space by which to inset the child.</summary>
    public EdgeInsetsGeometry Padding { get; }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedPaddingState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry>("padding", Padding));
    }

    private sealed class AnimatedPaddingState : AnimatedWidgetBaseState<AnimatedPadding>
    {
        private EdgeInsetsGeometryTween? _padding;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _padding = (EdgeInsetsGeometryTween?)visitor.Visit(
                _padding,
                (EdgeInsetsGeometry?)Widget.Padding,
                value => new EdgeInsetsGeometryTween(begin: value));
        }

        public override Widget Build(BuildContext context)
        {
            return new Padding(
                _padding!.Evaluate(Animation).Clamp(EdgeInsets.Zero, EdgeInsetsGeometry.Infinity),
                Widget.Child);
        }

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            properties.Add(new DiagnosticsProperty<EdgeInsetsGeometryTween>(
                "padding",
                _padding,
                defaultValue: DiagnosticsDefaults.NullValue));
        }
    }
}

/// <summary>
/// Animated version of <see cref="Align"/> which automatically transitions the child's position over a
/// given duration whenever the given alignment changes.
/// </summary>
public class AnimatedAlign : ImplicitlyAnimatedWidget
{
    /// <summary>Creates a widget that positions its child by an alignment that animates implicitly.</summary>
    public AnimatedAlign(
        AlignmentGeometry alignment,
        TimeSpan duration,
        Widget? child = null,
        double? heightFactor = null,
        double? widthFactor = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode)
        {
            if (widthFactor is double width && !(width >= 0.0))
            {
                throw new AssertionError("widthFactor == null || widthFactor >= 0.0");
            }

            if (heightFactor is double height && !(height >= 0.0))
            {
                throw new AssertionError("heightFactor == null || heightFactor >= 0.0");
            }
        }

        Alignment = alignment;
        Child = child;
        HeightFactor = heightFactor;
        WidthFactor = widthFactor;
    }

    /// <summary>How to align the child.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>If non-null, sets its height to the child's height multiplied by this factor.</summary>
    public double? HeightFactor { get; }

    /// <summary>If non-null, sets its width to the child's width multiplied by this factor.</summary>
    public double? WidthFactor { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedAlignState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
    }

    private sealed class AnimatedAlignState : AnimatedWidgetBaseState<AnimatedAlign>
    {
        private AlignmentGeometryTween? _alignment;
        private DoubleTween? _heightFactorTween;
        private DoubleTween? _widthFactorTween;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _alignment = (AlignmentGeometryTween?)visitor.Visit(
                _alignment,
                (AlignmentGeometry?)Widget.Alignment,
                value => new AlignmentGeometryTween(begin: value));
            if (Widget.HeightFactor is not null)
            {
                _heightFactorTween = (DoubleTween?)visitor.Visit(
                    _heightFactorTween,
                    Widget.HeightFactor,
                    value => new DoubleTween(begin: value));
            }

            if (Widget.WidthFactor is not null)
            {
                _widthFactorTween = (DoubleTween?)visitor.Visit(
                    _widthFactorTween,
                    Widget.WidthFactor,
                    value => new DoubleTween(begin: value));
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new Align(
                alignment: _alignment!.Evaluate(Animation)!.Value,
                heightFactor: _heightFactorTween?.Evaluate(Animation),
                widthFactor: _widthFactorTween?.Evaluate(Animation),
                child: Widget.Child);
        }

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            properties.Add(new DiagnosticsProperty<AlignmentGeometryTween>(
                "alignment",
                _alignment,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<Tween<double>>(
                "widthFactor",
                _widthFactorTween,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<Tween<double>>(
                "heightFactor",
                _heightFactorTween,
                defaultValue: DiagnosticsDefaults.NullValue));
        }
    }
}

/// <summary>
/// Animated version of <see cref="Positioned"/> which automatically transitions the child's position
/// over a given duration whenever the given position changes.
/// </summary>
public class AnimatedPositioned : ImplicitlyAnimatedWidget
{
    /// <summary>
    /// Creates a widget that animates its position implicitly. Only two out of the three horizontal
    /// values (<paramref name="left"/>, <paramref name="right"/>, <paramref name="width"/>), and only two
    /// out of the three vertical values, can be set.
    /// </summary>
    public AnimatedPositioned(
        Widget child,
        TimeSpan duration,
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode)
        {
            if (left is not null && right is not null && width is not null)
            {
                throw new AssertionError("left == null || right == null || width == null");
            }

            if (top is not null && bottom is not null && height is not null)
            {
                throw new AssertionError("top == null || bottom == null || height == null");
            }
        }

        Child = child;
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Width = width;
        Height = height;
    }

    // Dart's `fromRect` initializer list, without the default constructor's asserts.
    private AnimatedPositioned(
        Widget child,
        Rect rect,
        TimeSpan duration,
        Curve? curve,
        Action? onEnd,
        Key? key) : base(duration, curve, onEnd, key)
    {
        Child = child;
        Left = rect.Left;
        Top = rect.Top;
        Width = rect.Width;
        Height = rect.Height;
    }

    /// <summary>Creates a widget that animates the rectangle it occupies implicitly. Dart's <c>fromRect</c>.</summary>
    public static AnimatedPositioned FromRect(
        Rect rect,
        Widget child,
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null)
    {
        return new AnimatedPositioned(child, rect, duration, curve, onEnd, key);
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>The offset of the child's left edge from the left of the stack.</summary>
    public double? Left { get; }

    /// <summary>The offset of the child's top edge from the top of the stack.</summary>
    public double? Top { get; }

    /// <summary>The offset of the child's right edge from the right of the stack.</summary>
    public double? Right { get; }

    /// <summary>The offset of the child's bottom edge from the bottom of the stack.</summary>
    public double? Bottom { get; }

    /// <summary>The child's width.</summary>
    public double? Width { get; }

    /// <summary>The child's height.</summary>
    public double? Height { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedPositionedState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("left", Left, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("top", Top, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("right", Right, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("bottom", Bottom, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("width", Width, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("height", Height, defaultValue: DiagnosticsDefaults.NullValue));
    }

    private sealed class AnimatedPositionedState : AnimatedWidgetBaseState<AnimatedPositioned>
    {
        private DoubleTween? _left;
        private DoubleTween? _top;
        private DoubleTween? _right;
        private DoubleTween? _bottom;
        private DoubleTween? _width;
        private DoubleTween? _height;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _left = VisitDouble(visitor, _left, Widget.Left);
            _top = VisitDouble(visitor, _top, Widget.Top);
            _right = VisitDouble(visitor, _right, Widget.Right);
            _bottom = VisitDouble(visitor, _bottom, Widget.Bottom);
            _width = VisitDouble(visitor, _width, Widget.Width);
            _height = VisitDouble(visitor, _height, Widget.Height);
        }

        public override Widget Build(BuildContext context)
        {
            return new Positioned(
                child: Widget.Child,
                left: _left?.Evaluate(Animation),
                top: _top?.Evaluate(Animation),
                right: _right?.Evaluate(Animation),
                bottom: _bottom?.Evaluate(Animation),
                width: _width?.Evaluate(Animation),
                height: _height?.Evaluate(Animation));
        }

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("left", _left));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("top", _top));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("right", _right));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("bottom", _bottom));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("width", _width));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("height", _height));
        }
    }

    internal static DoubleTween? VisitDouble(TweenVisitor visitor, DoubleTween? tween, double? targetValue)
    {
        return (DoubleTween?)visitor.Visit(tween, targetValue, value => new DoubleTween(begin: value));
    }
}

/// <summary>
/// Animated version of <see cref="Positioned"/> (which takes a specific <see cref="TextDirection"/>)
/// which automatically transitions the child's position over a given duration whenever the given
/// position changes.
/// </summary>
public class AnimatedPositionedDirectional : ImplicitlyAnimatedWidget
{
    /// <summary>
    /// Creates a widget that animates its position implicitly. Only two out of the three horizontal
    /// values (<paramref name="start"/>, <paramref name="end"/>, <paramref name="width"/>), and only two
    /// out of the three vertical values, can be set.
    /// </summary>
    public AnimatedPositionedDirectional(
        Widget child,
        TimeSpan duration,
        double? start = null,
        double? top = null,
        double? end = null,
        double? bottom = null,
        double? width = null,
        double? height = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode)
        {
            if (start is not null && end is not null && width is not null)
            {
                throw new AssertionError("start == null || end == null || width == null");
            }

            if (top is not null && bottom is not null && height is not null)
            {
                throw new AssertionError("top == null || bottom == null || height == null");
            }
        }

        Child = child;
        Start = start;
        Top = top;
        End = end;
        Bottom = bottom;
        Width = width;
        Height = height;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>The offset of the child's start edge from the start of the stack.</summary>
    public double? Start { get; }

    /// <summary>The offset of the child's top edge from the top of the stack.</summary>
    public double? Top { get; }

    /// <summary>The offset of the child's end edge from the end of the stack.</summary>
    public double? End { get; }

    /// <summary>The offset of the child's bottom edge from the bottom of the stack.</summary>
    public double? Bottom { get; }

    /// <summary>The child's width.</summary>
    public double? Width { get; }

    /// <summary>The child's height.</summary>
    public double? Height { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedPositionedDirectionalState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("start", Start, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("top", Top, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("end", End, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("bottom", Bottom, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("width", Width, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("height", Height, defaultValue: DiagnosticsDefaults.NullValue));
    }

    private sealed class AnimatedPositionedDirectionalState
        : AnimatedWidgetBaseState<AnimatedPositionedDirectional>
    {
        private DoubleTween? _start;
        private DoubleTween? _top;
        private DoubleTween? _end;
        private DoubleTween? _bottom;
        private DoubleTween? _width;
        private DoubleTween? _height;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _start = AnimatedPositioned.VisitDouble(visitor, _start, Widget.Start);
            _top = AnimatedPositioned.VisitDouble(visitor, _top, Widget.Top);
            _end = AnimatedPositioned.VisitDouble(visitor, _end, Widget.End);
            _bottom = AnimatedPositioned.VisitDouble(visitor, _bottom, Widget.Bottom);
            _width = AnimatedPositioned.VisitDouble(visitor, _width, Widget.Width);
            _height = AnimatedPositioned.VisitDouble(visitor, _height, Widget.Height);
        }

        public override Widget Build(BuildContext context)
        {
            DebugAssertions.Assert(WidgetsDebug.DebugCheckHasDirectionality(context));
            return Positioned.Directional(
                textDirection: Directionality.Of(context),
                start: _start?.Evaluate(Animation),
                top: _top?.Evaluate(Animation),
                end: _end?.Evaluate(Animation),
                bottom: _bottom?.Evaluate(Animation),
                width: _width?.Evaluate(Animation),
                height: _height?.Evaluate(Animation),
                child: Widget.Child);
        }

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("start", _start));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("top", _top));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("end", _end));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("bottom", _bottom));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("width", _width));
            properties.Add(ObjectFlagProperty<Tween<double>>.Has("height", _height));
        }
    }
}

/// <summary>
/// Animated version of <see cref="Transform.Scale"/> which automatically transitions the child's scale
/// over a given duration whenever the given scale changes.
/// </summary>
public class AnimatedScale : ImplicitlyAnimatedWidget
{
    /// <summary>
    /// Creates a widget that animates its scale implicitly. <paramref name="alignment"/> defaults to
    /// <c>Alignment.center</c>.
    /// </summary>
    public AnimatedScale(
        double scale,
        TimeSpan duration,
        Widget? child = null,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        Child = child;
        Scale = scale;
        Alignment = alignment;
        FilterQuality = filterQuality;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>The target scale.</summary>
    public double Scale { get; }

    /// <summary>The alignment of the origin of the coordinate system in which the scale takes place.</summary>
    public Alignment Alignment { get; }

    /// <summary>The filter quality with which to apply the transform as a bitmap operation.</summary>
    public FilterQuality? FilterQuality { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedScaleState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("scale", Scale));
        properties.Add(new DiagnosticsProperty<Alignment>(
            "alignment",
            Alignment,
            defaultValue: Rendering.Alignment.Center));
        properties.Add(new EnumProperty<FilterQuality>(
            "filterQuality",
            FilterQuality,
            defaultValue: DiagnosticsDefaults.NullValue));
    }

    private sealed class AnimatedScaleState : ImplicitlyAnimatedWidgetState<AnimatedScale>
    {
        private DoubleTween? _scale;
        private Animation<double> _scaleAnimation = null!;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _scale = (DoubleTween?)visitor.Visit(_scale, Widget.Scale, value => new DoubleTween(begin: value));
        }

        protected override void DidUpdateTweens()
        {
            _scaleAnimation = Animation.Drive(_scale!);
        }

        public override Widget Build(BuildContext context)
        {
            return new ScaleTransition(
                scale: _scaleAnimation,
                alignment: Widget.Alignment,
                filterQuality: Widget.FilterQuality,
                child: Widget.Child);
        }
    }
}

/// <summary>
/// Animated version of <see cref="Transform.Rotate"/> which automatically transitions the child's
/// rotation over a given duration whenever the given rotation changes.
/// </summary>
public class AnimatedRotation : ImplicitlyAnimatedWidget
{
    /// <summary>
    /// Creates a widget that animates its rotation implicitly. <paramref name="alignment"/> defaults to
    /// <c>Alignment.center</c>.
    /// </summary>
    public AnimatedRotation(
        double turns,
        TimeSpan duration,
        Widget? child = null,
        Alignment alignment = default,
        FilterQuality? filterQuality = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        Child = child;
        Turns = turns;
        Alignment = alignment;
        FilterQuality = filterQuality;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>The animation that controls the rotation of the child, in turns.</summary>
    public double Turns { get; }

    /// <summary>The alignment of the origin of the coordinate system in which the rotation takes place.</summary>
    public Alignment Alignment { get; }

    /// <summary>The filter quality with which to apply the transform as a bitmap operation.</summary>
    public FilterQuality? FilterQuality { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedRotationState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("turns", Turns));
        properties.Add(new DiagnosticsProperty<Alignment>(
            "alignment",
            Alignment,
            defaultValue: Rendering.Alignment.Center));
        properties.Add(new EnumProperty<FilterQuality>(
            "filterQuality",
            FilterQuality,
            defaultValue: DiagnosticsDefaults.NullValue));
    }

    private sealed class AnimatedRotationState : ImplicitlyAnimatedWidgetState<AnimatedRotation>
    {
        private DoubleTween? _turns;
        private Animation<double> _turnsAnimation = null!;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _turns = (DoubleTween?)visitor.Visit(_turns, Widget.Turns, value => new DoubleTween(begin: value));
        }

        protected override void DidUpdateTweens()
        {
            _turnsAnimation = Animation.Drive(_turns!);
        }

        public override Widget Build(BuildContext context)
        {
            return new RotationTransition(
                turns: _turnsAnimation,
                alignment: Widget.Alignment,
                filterQuality: Widget.FilterQuality,
                child: Widget.Child);
        }
    }
}

/// <summary>
/// Widget which automatically transitions the child's offset relative to its normal position whenever
/// the given offset changes.
/// </summary>
public class AnimatedSlide : ImplicitlyAnimatedWidget
{
    /// <summary>Creates a widget that animates its offset translation implicitly.</summary>
    public AnimatedSlide(
        Vector offset,
        TimeSpan duration,
        Widget? child = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        Child = child;
        Offset = offset;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>The target offset, a fraction of the child's size.</summary>
    public Vector Offset { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedSlideState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Vector>("offset", Offset));
    }

    private sealed class AnimatedSlideState : ImplicitlyAnimatedWidgetState<AnimatedSlide>
    {
        private VectorTween? _offset;
        private Animation<Vector> _offsetAnimation = null!;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _offset = (VectorTween?)visitor.Visit(_offset, Widget.Offset, value => new VectorTween(begin: value));
        }

        protected override void DidUpdateTweens()
        {
            _offsetAnimation = Animation.Drive(_offset!);
        }

        public override Widget Build(BuildContext context)
        {
            return new SlideTransition(position: _offsetAnimation, child: Widget.Child);
        }
    }
}

/// <summary>
/// Animated version of <see cref="Opacity"/> which automatically transitions the child's opacity over a
/// given duration whenever the given opacity changes.
/// </summary>
public class AnimatedOpacity : ImplicitlyAnimatedWidget
{
    /// <summary>Creates a widget that animates its opacity implicitly.</summary>
    public AnimatedOpacity(
        double opacity,
        TimeSpan duration,
        Widget? child = null,
        Curve? curve = null,
        Action? onEnd = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode && !(opacity >= 0.0 && opacity <= 1.0))
        {
            throw new AssertionError("opacity >= 0.0 && opacity <= 1.0");
        }

        Child = child;
        Opacity = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>The target opacity, between 0.0 and 1.0 (inclusive).</summary>
    public double Opacity { get; }

    /// <summary>Whether the semantic information of the children is always included.</summary>
    public bool AlwaysIncludeSemantics { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedOpacityState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("opacity", Opacity));
    }

    private sealed class AnimatedOpacityState : ImplicitlyAnimatedWidgetState<AnimatedOpacity>
    {
        private DoubleTween? _opacity;
        private Animation<double> _opacityAnimation = null!;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _opacity = (DoubleTween?)visitor.Visit(
                _opacity,
                Widget.Opacity,
                value => new DoubleTween(begin: value));
        }

        protected override void DidUpdateTweens()
        {
            _opacityAnimation = Animation.Drive(_opacity!);
        }

        public override Widget Build(BuildContext context)
        {
            return new FadeTransition(
                opacity: _opacityAnimation,
                alwaysIncludeSemantics: Widget.AlwaysIncludeSemantics,
                child: Widget.Child);
        }
    }
}

/// <summary>
/// Animated version of <see cref="SliverOpacity"/> which automatically transitions the sliver child's
/// opacity over a given duration whenever the given opacity changes.
/// </summary>
public class SliverAnimatedOpacity : ImplicitlyAnimatedWidget
{
    /// <summary>Creates a widget that animates its opacity implicitly.</summary>
    public SliverAnimatedOpacity(
        double opacity,
        TimeSpan duration,
        Widget? sliver = null,
        Curve? curve = null,
        Action? onEnd = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode && !(opacity >= 0.0 && opacity <= 1.0))
        {
            throw new AssertionError("opacity >= 0.0 && opacity <= 1.0");
        }

        Sliver = sliver;
        Opacity = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    /// <summary>The sliver below this widget in the tree.</summary>
    public Widget? Sliver { get; }

    /// <summary>The target opacity, between 0.0 and 1.0 (inclusive).</summary>
    public double Opacity { get; }

    /// <summary>Whether the semantic information of the children is always included.</summary>
    public bool AlwaysIncludeSemantics { get; }

    /// <inheritdoc />
    public override State CreateState() => new SliverAnimatedOpacityState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DoubleProperty("opacity", Opacity));
    }

    private sealed class SliverAnimatedOpacityState : ImplicitlyAnimatedWidgetState<SliverAnimatedOpacity>
    {
        private DoubleTween? _opacity;
        private Animation<double> _opacityAnimation = null!;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _opacity = (DoubleTween?)visitor.Visit(
                _opacity,
                Widget.Opacity,
                value => new DoubleTween(begin: value));
        }

        protected override void DidUpdateTweens()
        {
            _opacityAnimation = Animation.Drive(_opacity!);
        }

        public override Widget Build(BuildContext context)
        {
            return new SliverFadeTransition(
                opacity: _opacityAnimation,
                sliver: Widget.Sliver,
                alwaysIncludeSemantics: Widget.AlwaysIncludeSemantics);
        }
    }
}

/// <summary>
/// Animated version of <see cref="DefaultTextStyle"/> which automatically transitions the default text
/// style (the text style to apply to descendant <see cref="Text"/> widgets without explicit style) over
/// a given duration whenever the given style changes.
/// </summary>
/// <remarks>
/// The <see cref="TextAlign"/>, <see cref="SoftWrap"/>, <see cref="Overflow"/>, <see cref="MaxLines"/>,
/// <see cref="TextWidthBasis"/> and <see cref="TextHeightBehavior"/> properties are not animated and
/// take effect immediately when changed.
/// </remarks>
public class AnimatedDefaultTextStyle : ImplicitlyAnimatedWidget
{
    /// <summary>Creates a widget that animates the default text style implicitly.</summary>
    public AnimatedDefaultTextStyle(
        Widget child,
        TextStyle style,
        TimeSpan duration,
        TextAlign? textAlign = null,
        bool softWrap = true,
        TextOverflow overflow = TextOverflow.Clip,
        int? maxLines = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        TextHeightBehavior? textHeightBehavior = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode && maxLines is not null && !(maxLines > 0))
        {
            throw new AssertionError("maxLines == null || maxLines > 0");
        }

        Child = child;
        Style = style;
        TextAlign = textAlign;
        SoftWrap = softWrap;
        Overflow = overflow;
        MaxLines = maxLines;
        TextWidthBasis = textWidthBasis;
        TextHeightBehavior = textHeightBehavior;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>The target text style.</summary>
    public TextStyle Style { get; }

    /// <summary>How the text should be aligned horizontally.</summary>
    public TextAlign? TextAlign { get; }

    /// <summary>Whether the text should break at soft line breaks.</summary>
    public bool SoftWrap { get; }

    /// <summary>How visual overflow should be handled.</summary>
    public TextOverflow Overflow { get; }

    /// <summary>An optional maximum number of lines for the text to span, wrapping if necessary.</summary>
    public int? MaxLines { get; }

    /// <summary>The strategy to use when calculating the width of the text.</summary>
    public TextWidthBasis TextWidthBasis { get; }

    /// <summary>How the paragraph's line heights apply to its first and last lines.</summary>
    public TextHeightBehavior? TextHeightBehavior { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedDefaultTextStyleState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        Style.DebugFillProperties(properties);
        properties.Add(new EnumProperty<TextAlign>(
            "textAlign",
            TextAlign,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new FlagProperty(
            "softWrap",
            value: SoftWrap,
            ifTrue: "wrapping at box width",
            ifFalse: "no wrapping except at line break characters",
            showName: true));
        properties.Add(new EnumProperty<TextOverflow>(
            "overflow",
            Overflow,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty("maxLines", MaxLines, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<TextWidthBasis>(
            "textWidthBasis",
            TextWidthBasis,
            defaultValue: TextWidthBasis.Parent));
        properties.Add(new DiagnosticsProperty<TextHeightBehavior?>(
            "textHeightBehavior",
            TextHeightBehavior,
            defaultValue: DiagnosticsDefaults.NullValue));
    }

    private sealed class AnimatedDefaultTextStyleState : AnimatedWidgetBaseState<AnimatedDefaultTextStyle>
    {
        private TextStyleTween? _style;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _style = (TextStyleTween?)visitor.Visit(
                _style,
                Widget.Style,
                value => new TextStyleTween(begin: value));
        }

        public override Widget Build(BuildContext context)
        {
            return new DefaultTextStyle(
                style: _style!.Evaluate(Animation),
                textAlign: Widget.TextAlign,
                softWrap: Widget.SoftWrap,
                overflow: Widget.Overflow,
                maxLines: Widget.MaxLines,
                textWidthBasis: Widget.TextWidthBasis,
                textHeightBehavior: Widget.TextHeightBehavior,
                child: Widget.Child);
        }
    }
}

/// <summary>
/// Animated version of <see cref="PhysicalModel"/>. The <see cref="BorderRadius"/> and
/// <see cref="Elevation"/> are animated; <see cref="Color"/> and <see cref="ShadowColor"/> are animated
/// when <see cref="AnimateColor"/> and <see cref="AnimateShadowColor"/> are set; the
/// <see cref="Shape"/> is not animated.
/// </summary>
public class AnimatedPhysicalModel : ImplicitlyAnimatedWidget
{
    /// <summary>Creates a widget that animates the properties of a <see cref="PhysicalModel"/>.</summary>
    public AnimatedPhysicalModel(
        Widget child,
        Color color,
        Color shadowColor,
        TimeSpan duration,
        BoxShape shape = BoxShape.Rectangle,
        Clip clipBehavior = Clip.None,
        BorderRadius? borderRadius = null,
        double elevation = 0.0,
        bool animateColor = true,
        bool animateShadowColor = true,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode && !(elevation >= 0.0))
        {
            throw new AssertionError("elevation >= 0.0");
        }

        Child = child;
        Shape = shape;
        ClipBehavior = clipBehavior;
        BorderRadius = borderRadius;
        Elevation = elevation;
        Color = color;
        AnimateColor = animateColor;
        ShadowColor = shadowColor;
        AnimateShadowColor = animateShadowColor;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>The type of shape. This property is not animated.</summary>
    public BoxShape Shape { get; }

    /// <summary>The content will be clipped (or not) according to this option.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>The target border radius of the rounded corners for a rectangle shape.</summary>
    public BorderRadius? BorderRadius { get; }

    /// <summary>The target z-coordinate relative to the parent at which to place this physical object.</summary>
    public double Elevation { get; }

    /// <summary>The target background color.</summary>
    public Color Color { get; }

    /// <summary>Whether the color should be animated.</summary>
    public bool AnimateColor { get; }

    /// <summary>The target shadow color.</summary>
    public Color ShadowColor { get; }

    /// <summary>Whether the shadow color should be animated.</summary>
    public bool AnimateShadowColor { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedPhysicalModelState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<BoxShape>("shape", Shape));
        properties.Add(new DiagnosticsProperty<BorderRadius?>("borderRadius", BorderRadius));
        properties.Add(new DoubleProperty("elevation", Elevation));
        properties.Add(new ColorProperty("color", Color));
        properties.Add(new DiagnosticsProperty<bool>("animateColor", AnimateColor));
        properties.Add(new ColorProperty("shadowColor", ShadowColor));
        properties.Add(new DiagnosticsProperty<bool>("animateShadowColor", AnimateShadowColor));
    }

    private sealed class AnimatedPhysicalModelState : AnimatedWidgetBaseState<AnimatedPhysicalModel>
    {
        private BorderRadiusTween? _borderRadius;
        private DoubleTween? _elevation;
        private ColorTween? _color;
        private ColorTween? _shadowColor;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _borderRadius = (BorderRadiusTween?)visitor.Visit(
                _borderRadius,
                Widget.BorderRadius ?? Rendering.BorderRadius.Zero,
                value => new BorderRadiusTween(begin: value));
            _elevation = (DoubleTween?)visitor.Visit(
                _elevation,
                Widget.Elevation,
                value => new DoubleTween(begin: value));
            _color = (ColorTween?)visitor.Visit(
                _color,
                Widget.Color,
                value => new ColorTween(begin: value));
            _shadowColor = (ColorTween?)visitor.Visit(
                _shadowColor,
                Widget.ShadowColor,
                value => new ColorTween(begin: value));
        }

        public override Widget Build(BuildContext context)
        {
            return new PhysicalModel(
                shape: Widget.Shape,
                clipBehavior: Widget.ClipBehavior,
                borderRadius: _borderRadius!.Evaluate(Animation),
                elevation: _elevation!.Evaluate(Animation),
                color: Widget.AnimateColor ? _color!.Evaluate(Animation)! : Widget.Color,
                shadowColor: Widget.AnimateShadowColor
                    ? _shadowColor!.Evaluate(Animation)!
                    : Widget.ShadowColor,
                child: Widget.Child);
        }
    }
}

/// <summary>
/// Animated version of <see cref="FractionallySizedBox"/> which automatically transitions the child's
/// size over a given duration whenever the given <see cref="WidthFactor"/> or
/// <see cref="HeightFactor"/> changes, as well as the position whenever the given
/// <see cref="Alignment"/> changes.
/// </summary>
public class AnimatedFractionallySizedBox : ImplicitlyAnimatedWidget
{
    /// <summary>
    /// Creates a widget that sizes its child to a fraction of the total available space that animates
    /// implicitly. <paramref name="alignment"/> defaults to <c>Alignment.center</c>.
    /// </summary>
    public AnimatedFractionallySizedBox(
        TimeSpan duration,
        Widget? child = null,
        AlignmentGeometry alignment = default,
        double? heightFactor = null,
        double? widthFactor = null,
        Curve? curve = null,
        Action? onEnd = null,
        Key? key = null) : base(duration, curve, onEnd, key)
    {
        if (Constants.KDebugMode)
        {
            if (widthFactor is double width && !(width >= 0.0))
            {
                throw new AssertionError("widthFactor == null || widthFactor >= 0.0");
            }

            if (heightFactor is double height && !(height >= 0.0))
            {
                throw new AssertionError("heightFactor == null || heightFactor >= 0.0");
            }
        }

        Alignment = alignment;
        Child = child;
        HeightFactor = heightFactor;
        WidthFactor = widthFactor;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>
    /// If non-null, the fraction of the incoming height given to the child; otherwise the child keeps
    /// the incoming height constraints.
    /// </summary>
    public double? HeightFactor { get; }

    /// <summary>
    /// If non-null, the fraction of the incoming width given to the child; otherwise the child keeps
    /// the incoming width constraints.
    /// </summary>
    public double? WidthFactor { get; }

    /// <summary>How to align the child.</summary>
    public AlignmentGeometry Alignment { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedFractionallySizedBoxState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<AlignmentGeometry>("alignment", Alignment));
        properties.Add(new DiagnosticsProperty<double?>("widthFactor", WidthFactor));
        properties.Add(new DiagnosticsProperty<double?>("heightFactor", HeightFactor));
    }

    private sealed class AnimatedFractionallySizedBoxState : AnimatedWidgetBaseState<AnimatedFractionallySizedBox>
    {
        private AlignmentGeometryTween? _alignment;
        private DoubleTween? _heightFactorTween;
        private DoubleTween? _widthFactorTween;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            _alignment = (AlignmentGeometryTween?)visitor.Visit(
                _alignment,
                (AlignmentGeometry?)Widget.Alignment,
                value => new AlignmentGeometryTween(begin: value));
            if (Widget.HeightFactor is not null)
            {
                _heightFactorTween = (DoubleTween?)visitor.Visit(
                    _heightFactorTween,
                    Widget.HeightFactor,
                    value => new DoubleTween(begin: value));
            }

            if (Widget.WidthFactor is not null)
            {
                _widthFactorTween = (DoubleTween?)visitor.Visit(
                    _widthFactorTween,
                    Widget.WidthFactor,
                    value => new DoubleTween(begin: value));
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new FractionallySizedBox(
                alignment: _alignment!.Evaluate(Animation)!.Value,
                heightFactor: _heightFactorTween?.Evaluate(Animation),
                widthFactor: _widthFactorTween?.Evaluate(Animation),
                child: Widget.Child);
        }

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            properties.Add(new DiagnosticsProperty<AlignmentGeometryTween>(
                "alignment",
                _alignment,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<Tween<double>>(
                "widthFactor",
                _widthFactorTween,
                defaultValue: DiagnosticsDefaults.NullValue));
            properties.Add(new DiagnosticsProperty<Tween<double>>(
                "heightFactor",
                _heightFactorTween,
                defaultValue: DiagnosticsDefaults.NullValue));
        }
    }
}
