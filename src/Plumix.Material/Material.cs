using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/material.dart

public static class MaterialConstants
{
    public static readonly TimeSpan ThemeAnimationDuration = TimeSpan.FromMilliseconds(200);

    // Mirrors Flutter's `kTabScrollDuration` from `material/constants.dart`.
    /// <summary>The duration of a <see cref="TabController"/>'s index-change animation.</summary>
    public static readonly TimeSpan TabScrollDuration = TimeSpan.FromMilliseconds(300);

    // Mirrors Flutter's `kTabLabelPadding` from `material/constants.dart`.
    /// <summary>The horizontal padding included by default in each <see cref="Tab"/> label.</summary>
    public static readonly EdgeInsetsGeometry TabLabelPadding = EdgeInsetsGeometry.Symmetric(horizontal: 16.0);
}

/// <summary>The visual kind of a <see cref="Material"/> surface.</summary>
public enum MaterialType
{
    Canvas,
    Card,
    Circle,
    Button,
    Transparency,
}

/// <summary>Flutter's <c>kMaterialEdges</c> defaults for the supported shape model.</summary>
public static class MaterialEdges
{
    public static BorderRadius? ForType(MaterialType type)
    {
        return type switch
        {
            MaterialType.Card or MaterialType.Button => BorderRadius.Circular(2),
            _ => null,
        };
    }
}

/// <summary>Owns ink features painted by a descendant Material surface.</summary>
public abstract class MaterialInkController
{
    public abstract Color? Color { get; }

    public abstract ITickerProvider Vsync { get; }

    public abstract void AddInkFeature(InkFeature feature);

    protected internal abstract void RemoveInkFeature(InkFeature feature);

    public abstract void MarkNeedsPaint();

    internal void AddInkFeature(IMaterialInkFeature feature)
    {
        RequireRegistry().AddInkFeature(feature);
    }

    internal void RemoveInkFeature(IMaterialInkFeature feature)
    {
        RequireRegistry().RemoveInkFeature(feature);
    }

    private IMaterialInkRegistry RequireRegistry()
    {
        return this as IMaterialInkRegistry
               ?? throw new InvalidOperationException(
                   "This MaterialInkController does not own Plumix render-tree ink features.");
    }
}

/// <summary>A feature painted on the nearest ancestor <see cref="Material"/>.</summary>
public abstract class InkFeature : IDisposable, IMaterialInkFeature
{
    private bool _disposed;

    protected InkFeature(
        MaterialInkController controller,
        RenderBox referenceBox,
        Action? onRemoved = null)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        ReferenceBox = referenceBox ?? throw new ArgumentNullException(nameof(referenceBox));
        OnRemoved = onRemoved;
    }

    protected MaterialInkController Controller { get; }

    public RenderBox ReferenceBox { get; }

    protected Action? OnRemoved { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Controller.RemoveInkFeature(this);
        OnRemoved?.Invoke();
        GC.SuppressFinalize(this);
    }

    void IMaterialInkFeature.PaintFeature(PaintingContext context)
    {
        PaintFeature(context);
    }

    protected abstract void PaintFeature(PaintingContext context);
}

internal interface IMaterialInkFeature
{
    RenderBox ReferenceBox { get; }

    void PaintFeature(PaintingContext context);
}

internal interface IMaterialInkRegistry
{
    void AddInkFeature(IMaterialInkFeature feature);

    void RemoveInkFeature(IMaterialInkFeature feature);
}

/// <summary>
/// A Material Design surface that supplies its color, elevation, shape, clipping,
/// and default text style to the descendant subtree.
/// </summary>
public sealed class Material : StatefulWidget
{
    public Material(
        MaterialType type = MaterialType.Canvas,
        double elevation = 0,
        Color? color = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        TextStyle? textStyle = null,
        BorderRadius? borderRadius = null,
        ShapeBorder? shape = null,
        bool borderOnForeground = true,
        Clip clipBehavior = Clip.None,
        TimeSpan? animationDuration = null,
        Widget? child = null,
        bool animateColor = false,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(elevation) || elevation < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Material elevation must be finite and non-negative.");
        }

        if (shape is not null && borderRadius.HasValue)
        {
            throw new ArgumentException("shape and borderRadius cannot both be specified.", nameof(shape));
        }

        if (type == MaterialType.Circle && (shape is not null || borderRadius.HasValue))
        {
            throw new ArgumentException("Circle material cannot specify shape or borderRadius.", nameof(type));
        }

        if (animationDuration.HasValue && animationDuration.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(animationDuration));
        }

        Type = type;
        Elevation = elevation;
        Color = color;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        TextStyle = textStyle;
        BorderRadius = borderRadius;
        Shape = shape;
        BorderOnForeground = borderOnForeground;
        ClipBehavior = clipBehavior;
        AnimationDuration = animationDuration ?? TimeSpan.FromMilliseconds(200);
        Child = child;
        AnimateColor = animateColor;
    }

    public MaterialType Type { get; }
    public double Elevation { get; }
    public Color? Color { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public TextStyle? TextStyle { get; }
    public BorderRadius? BorderRadius { get; }
    public ShapeBorder? Shape { get; }
    public bool BorderOnForeground { get; }
    public Clip ClipBehavior { get; }
    public TimeSpan AnimationDuration { get; }
    public Widget? Child { get; }
    public bool AnimateColor { get; }

    public static MaterialInkController? MaybeOf(BuildContext context)
    {
        return LookupBoundary.FindAncestorRenderObjectOfType<RenderMaterialInkFeatures>(context)?.Controller;
    }

    public static MaterialInkController Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "Material.of() called with a context that does not contain a Material ancestor.");
    }

    public override State CreateState() => new MaterialState();

    private sealed class MaterialState : State
    {
        private AnimationController? _controller;
        private MaterialVisual? _begin;
        private MaterialVisual? _end;

        private Material CurrentWidget => (Material)StateWidget;

        public override void InitState()
        {
            CreateController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldMaterial = (Material)oldWidget;
            if (oldMaterial.AnimationDuration != CurrentWidget.AnimationDuration)
            {
                DisposeController();
                CreateController();
            }
        }

        public override Widget Build(BuildContext context)
        {
            var target = MaterialVisual.Resolve(CurrentWidget, Theme.Of(context));
            if (_end is null)
            {
                _begin = target;
                _end = target;
            }
            else if (_end != target)
            {
                _begin = Evaluate();
                _end = target;
                _controller!.Forward(from: 0);
            }

            var visual = Evaluate();
            Widget content = CurrentWidget.Child ?? new SizedBox();
            if (CurrentWidget.BorderOnForeground && HasVisibleOutline(visual.Shape))
            {
                content = new Stack(
                    fit: StackFit.Passthrough,
                    children:
                    [
                        content,
                        new Positioned(
                            left: 0,
                            top: 0,
                            right: 0,
                            bottom: 0,
                            child: new DecoratedBox(
                                new ShapeDecoration(visual.Shape),
                                new SizedBox()))
                    ]);
            }

            content = new MaterialInkFeatures(
                color: visual.Color,
                vsync: this,
                absorbHitTest: CurrentWidget.Type != MaterialType.Transparency,
                child: content);

            if (CurrentWidget.ClipBehavior != Clip.None)
            {
                content = new ClipPath(
                    clipper: new ShapeBorderClipper(visual.Shape),
                    clipBehavior: CurrentWidget.ClipBehavior,
                    child: content);
            }

            ShapeBorder backgroundShape = CurrentWidget.BorderOnForeground
                ? StripOutline(visual.Shape)
                : visual.Shape;
            content = new DecoratedBox(
                new ShapeDecoration(
                    Shape: backgroundShape,
                    Color: visual.Color,
                    Shadows: MaterialSurface.BuildBoxShadows(visual.ShadowColor, visual.Elevation) ?? default),
                content);

            return new DefaultTextStyle(visual.TextStyle, content);
        }

        private static bool HasVisibleOutline(ShapeBorder shape)
        {
            return shape switch
            {
                OutlinedBorder outlined => outlined.Side is { Style: BorderStyle.Solid, Width: > 0 },
                BoxBorder box => box.Top.Style == BorderStyle.Solid
                                 || box.Bottom.Style == BorderStyle.Solid,
                _ => true,
            };
        }

        private static ShapeBorder StripOutline(ShapeBorder shape)
        {
            return shape switch
            {
                OutlinedBorder outlined => outlined.CopyWith(BorderSide.None),
                BoxBorder => new Plumix.Rendering.Border(),
                _ => shape,
            };
        }

        public override void Dispose()
        {
            DisposeController();
        }

        private MaterialVisual Evaluate()
        {
            if (_begin is null || _end is null)
            {
                throw new InvalidOperationException("Material visual state was not initialized.");
            }

            return MaterialVisual.Lerp(
                _begin,
                _end,
                _controller!.Evaluate(),
                CurrentWidget.AnimateColor);
        }

        private void CreateController()
        {
            _controller = new AnimationController(duration: CurrentWidget.AnimationDuration, vsync: this)
            {
                Curve = Curves.FastOutSlowIn,
            };
            _controller.Changed += HandleChanged;
        }

        private void DisposeController()
        {
            if (_controller is null)
            {
                return;
            }

            _controller.Changed -= HandleChanged;
            _controller.Dispose();
            _controller = null;
        }

        private void HandleChanged()
        {
            SetState(() => { });
        }
    }

    private sealed record MaterialVisual(
        Color Color,
        Color ShadowColor,
        double Elevation,
        ShapeBorder Shape,
        TextStyle TextStyle)
    {
        public static MaterialVisual Resolve(Material material, ThemeData theme)
        {
            ShapeBorder effectiveShape = material.Shape
                ?? (material.Type == MaterialType.Circle
                    ? new CircleBorder()
                    : new RoundedRectangleBorder(
                        borderRadius: MaterialEdges.ForType(material.Type) ?? Plumix.Rendering.BorderRadius.Zero));
            if (material.BorderRadius.HasValue)
            {
                BorderSide side = effectiveShape is OutlinedBorder outlined ? outlined.Side : BorderSide.None;
                effectiveShape = new RoundedRectangleBorder(side, material.BorderRadius.Value);
            }

            Color defaultColor = material.Type switch
            {
                MaterialType.Canvas => theme.CanvasColor,
                MaterialType.Card => theme.CardColor,
                _ => Colors.Transparent,
            };
            Color baseColor = material.Color ?? defaultColor;
            Color effectiveColor = material.Type == MaterialType.Transparency
                ? Colors.Transparent
                : theme.UseMaterial3
                    ? ElevationOverlay.ApplySurfaceTint(
                        baseColor,
                        material.SurfaceTintColor,
                        material.Elevation)
                    : ElevationOverlay.ApplyOverlay(theme, baseColor, material.Elevation);
            return new MaterialVisual(
                effectiveColor,
                material.ShadowColor
                ?? (theme.UseMaterial3 ? theme.ColorScheme.Shadow : theme.ShadowColor),
                material.Elevation,
                effectiveShape,
                material.TextStyle ?? theme.TextTheme.BodyMedium);
        }

        public static MaterialVisual Lerp(MaterialVisual begin, MaterialVisual end, double t, bool animateColor)
        {
            double clampedT = Math.Clamp(t, 0, 1);
            Color color = animateColor ? MaterialSurface.LerpColor(begin.Color, end.Color, clampedT) : end.Color;
            Color shadow = MaterialSurface.LerpColor(begin.ShadowColor, end.ShadowColor, clampedT);
            double elevation = begin.Elevation + ((end.Elevation - begin.Elevation) * clampedT);
            ShapeBorder shape = MaterialThemeLerp.Shape(begin.Shape, end.Shape, clampedT)!;
            return new MaterialVisual(
                color,
                shadow,
                elevation,
                shape,
                Plumix.Widgets.TextStyle.Lerp(begin.TextStyle, end.TextStyle, clampedT));
        }
    }
}

internal sealed class MaterialInkFeatures : SingleChildRenderObjectWidget
{
    public MaterialInkFeatures(
        Color color,
        ITickerProvider vsync,
        bool absorbHitTest,
        Widget child) : base(child)
    {
        Color = color;
        Vsync = vsync ?? throw new ArgumentNullException(nameof(vsync));
        AbsorbHitTest = absorbHitTest;
    }

    public Color Color { get; }

    public ITickerProvider Vsync { get; }

    public bool AbsorbHitTest { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderMaterialInkFeatures(Color, Vsync, AbsorbHitTest);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var inkFeatures = (RenderMaterialInkFeatures)renderObject;
        inkFeatures.Color = Color;
        inkFeatures.Vsync = Vsync;
        inkFeatures.AbsorbHitTest = AbsorbHitTest;
    }
}

internal sealed class RenderMaterialInkFeatures : RenderProxyBoxWithHitTestBehavior
{
    private readonly List<IMaterialInkFeature> _inkFeatures = [];
    private readonly ControllerAdapter _controller;
    private Color _color;
    private ITickerProvider _vsync;

    public RenderMaterialInkFeatures(Color color, ITickerProvider vsync, bool absorbHitTest)
        : base(absorbHitTest ? HitTestBehavior.Opaque : HitTestBehavior.DeferToChild)
    {
        _color = color;
        _vsync = vsync;
        _controller = new ControllerAdapter(this);
    }

    public MaterialInkController Controller => _controller;

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

    public ITickerProvider Vsync
    {
        get => _vsync;
        set => _vsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    public bool AbsorbHitTest
    {
        get => Behavior == HitTestBehavior.Opaque;
        set => Behavior = value ? HitTestBehavior.Opaque : HitTestBehavior.DeferToChild;
    }

    internal int FeatureCount => _inkFeatures.Count;

    public override void Paint(PaintingContext context, Point offset)
    {
        if (_inkFeatures.Count > 0)
        {
            context.PushClipRect(new Rect(offset, Size), clippedContext =>
            {
                foreach (IMaterialInkFeature feature in _inkFeatures.ToArray())
                {
                    if (!InkFeatureTransform.TryResolve(feature.ReferenceBox, this, out Matrix4 transform))
                    {
                        continue;
                    }

                    clippedContext.PushTransform(
                        Matrix4.TranslationValues(offset.X, offset.Y, 0.0),
                        translatedContext => translatedContext.PushTransform(transform, feature.PaintFeature));
                }
            });
        }

        base.Paint(context, offset);
    }

    private void AddFeature(IMaterialInkFeature feature)
    {
        if (_inkFeatures.Contains(feature))
        {
            return;
        }

        _inkFeatures.Add(feature);
        MarkNeedsPaint();
    }

    private void RemoveFeature(IMaterialInkFeature feature)
    {
        if (_inkFeatures.Remove(feature))
        {
            MarkNeedsPaint();
        }
    }

    private sealed class ControllerAdapter : MaterialInkController, IMaterialInkRegistry
    {
        private readonly RenderMaterialInkFeatures _owner;

        public ControllerAdapter(RenderMaterialInkFeatures owner)
        {
            _owner = owner;
        }

        public override Color? Color => _owner.Color;

        public override ITickerProvider Vsync => _owner.Vsync;

        public override void AddInkFeature(InkFeature feature)
        {
            ArgumentNullException.ThrowIfNull(feature);
            _owner.AddFeature(feature);
        }

        protected internal override void RemoveInkFeature(InkFeature feature)
        {
            ArgumentNullException.ThrowIfNull(feature);
            _owner.RemoveFeature(feature);
        }

        public override void MarkNeedsPaint()
        {
            _owner.MarkNeedsPaint();
        }

        void IMaterialInkRegistry.AddInkFeature(IMaterialInkFeature feature)
        {
            _owner.AddFeature(feature);
        }

        void IMaterialInkRegistry.RemoveInkFeature(IMaterialInkFeature feature)
        {
            _owner.RemoveFeature(feature);
        }
    }
}

internal static class InkFeatureTransform
{
    public static bool TryResolve(RenderBox referenceBox, RenderObject controller, out Matrix4 transform)
    {
        transform = Matrix4.Identity();
        RenderObject current = referenceBox;
        while (!ReferenceEquals(current, controller))
        {
            RenderObject? parent = current.Parent;
            if (parent is null)
            {
                transform = Matrix4.Identity();
                return false;
            }

            Point childOffset = current.parentData is BoxParentData data ? data.offset : default;
            // The level's own step maps `current` into `parent`: the render transform runs first and
            // the parent data offset places the result, and the walk is leaf-first so each new level
            // left-multiplies what has been accumulated so far.
            Matrix4 step = Matrix4.TranslationValues(childOffset.X, childOffset.Y, 0.0);
            if (parent is RenderTransform renderTransform)
            {
                step.Multiply(renderTransform.Transform);
            }

            MatrixUtils.MultiplyInPlace(step, transform);
            current = parent;
        }

        return Matrix4.TryInvert(transform) is not null;
    }
}

internal static class MaterialSurface
{
    public static IReadOnlyList<BoxShadow>? BuildBoxShadows(Color shadowColor, double elevation)
    {
        if (elevation <= 0 || shadowColor.A == 0)
        {
            return null;
        }

        var keyShadow = new BoxShadow(
            color: ApplyOpacity(shadowColor, 0.20),
            offset: new Point(0, Math.Max(1, Math.Round(elevation))),
            blurRadius: Math.Max(2, elevation * 2.4));
        var ambientShadow = new BoxShadow(
            color: ApplyOpacity(shadowColor, 0.14),
            offset: new Point(0, Math.Max(1, Math.Round(elevation * 0.5))),
            blurRadius: Math.Max(3, elevation * 3.2));
        return [keyShadow, ambientShadow];
    }

    public static Color ApplySurfaceTint(Color color, Color surfaceTint, double elevation)
    {
        return ElevationOverlay.ApplySurfaceTint(color, surfaceTint, elevation);
    }

    public static Color LerpColor(Color from, Color to, double t)
    {
        return new ColorTween().Evaluate(Math.Clamp(t, 0, 1), from, to);
    }

    private static Color ApplyOpacity(Color color, double multiplier)
    {
        byte alpha = (byte)Math.Clamp((int)(color.A * multiplier), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
