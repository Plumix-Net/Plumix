using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/material.dart

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
            if (CurrentWidget.BorderOnForeground && visual.Shape.Side is { Width: > 0 } foregroundBorder)
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
                                new BoxDecoration(
                                    Border: foregroundBorder,
                                    BorderRadius: visual.Shape.BorderRadius,
                                    Shape: visual.Shape.Shape),
                                new SizedBox()))
                    ]);
            }

            if (CurrentWidget.ClipBehavior != Clip.None)
            {
                // The complete Material shape/ink migration will adopt ClipOval for circles.
                // Until then, retain the existing rounded-rectangle composition.
                content = new ClipRRect(visual.Shape.BorderRadius, content);
            }

            var backgroundBorder = CurrentWidget.BorderOnForeground ? null : visual.Shape.Side;
            content = new DecoratedBox(
                new BoxDecoration(
                    Color: visual.Color,
                    Border: backgroundBorder,
                    BorderRadius: visual.Shape.BorderRadius,
                    BoxShadows: MaterialSurface.BuildBoxShadows(visual.ShadowColor, visual.Elevation),
                    Shape: visual.Shape.Shape),
                content);

            return new DefaultTextStyle(visual.TextStyle, content);
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
            _controller = new AnimationController(CurrentWidget.AnimationDuration)
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
                    ? ShapeBorder.Circle()
                    : new ShapeBorder(MaterialEdges.ForType(material.Type) ?? Plumix.Rendering.BorderRadius.Zero));
            if (material.BorderRadius.HasValue)
            {
                effectiveShape = new ShapeBorder(material.BorderRadius.Value, effectiveShape.Side)
                {
                    Shape = effectiveShape.Shape,
                };
            }

            Color defaultColor = material.Type switch
            {
                MaterialType.Canvas => theme.CanvasColor,
                MaterialType.Card => theme.CardColor,
                _ => Colors.Transparent,
            };
            Color baseColor = material.Color ?? defaultColor;
            Color tint = material.SurfaceTintColor ?? Colors.Transparent;
            Color effectiveColor = MaterialSurface.ApplySurfaceTint(baseColor, tint, material.Elevation);
            return new MaterialVisual(
                effectiveColor,
                material.ShadowColor ?? theme.ShadowColor,
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
            BorderRadius radius = new(
                begin.Shape.BorderRadius.Radius
                + ((end.Shape.BorderRadius.Radius - begin.Shape.BorderRadius.Radius) * clampedT));
            var shape = new ShapeBorder(radius, clampedT < 0.5 ? begin.Shape.Side : end.Shape.Side)
            {
                Shape = clampedT < 0.5 ? begin.Shape.Shape : end.Shape.Shape,
            };
            return new MaterialVisual(
                color,
                shadow,
                elevation,
                shape,
                Plumix.Widgets.TextStyle.Lerp(begin.TextStyle, end.TextStyle, clampedT));
        }
    }
}

internal static class MaterialSurface
{
    public static BoxShadows? BuildBoxShadows(Color shadowColor, double elevation)
    {
        if (elevation <= 0 || shadowColor.A == 0)
        {
            return null;
        }

        var keyShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation)),
            Blur = Math.Max(2, elevation * 2.4),
            Spread = 0,
            Color = ApplyOpacity(shadowColor, 0.20),
            IsInset = false,
        };
        var ambientShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(3, elevation * 3.2),
            Spread = 0,
            Color = ApplyOpacity(shadowColor, 0.14),
            IsInset = false,
        };
        return new BoxShadows(keyShadow, [ambientShadow]);
    }

    public static Color ApplySurfaceTint(Color color, Color surfaceTint, double elevation)
    {
        if (surfaceTint.A == 0 || elevation <= 0)
        {
            return color;
        }

        double opacity = ResolveSurfaceTintOpacityForElevation(elevation);
        if (opacity <= 0)
        {
            return color;
        }

        byte overlayAlpha = (byte)Math.Clamp((int)(opacity * 255), 0, 255);
        double quantizedOpacity = overlayAlpha / 255.0;
        static byte Blend(byte from, byte to, double amount)
        {
            return (byte)Math.Clamp((int)(from + ((to - from) * amount)), 0, 255);
        }

        return Color.FromArgb(
            color.A,
            Blend(color.R, surfaceTint.R, quantizedOpacity),
            Blend(color.G, surfaceTint.G, quantizedOpacity),
            Blend(color.B, surfaceTint.B, quantizedOpacity));
    }

    public static Color LerpColor(Color from, Color to, double t)
    {
        return new ColorTween().Evaluate(Math.Clamp(t, 0, 1), from, to);
    }

    private static double ResolveSurfaceTintOpacityForElevation(double elevation)
    {
        ReadOnlySpan<(double Elevation, double Opacity)> stops =
        [
            (0.0, 0.0), (1.0, 0.05), (3.0, 0.08), (6.0, 0.11),
            (8.0, 0.12), (12.0, 0.14),
        ];
        for (int index = 1; index < stops.Length; index++)
        {
            if (elevation <= stops[index].Elevation)
            {
                var lower = stops[index - 1];
                var upper = stops[index];
                double progress = (elevation - lower.Elevation) / (upper.Elevation - lower.Elevation);
                return lower.Opacity + ((upper.Opacity - lower.Opacity) * Math.Clamp(progress, 0, 1));
            }
        }

        return stops[^1].Opacity;
    }

    private static Color ApplyOpacity(Color color, double multiplier)
    {
        byte alpha = (byte)Math.Clamp((int)(color.A * multiplier), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
