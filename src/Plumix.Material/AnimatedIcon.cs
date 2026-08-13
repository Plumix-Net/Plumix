using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// material_ui/lib/src/animated_icons.dart
// material_ui/lib/src/animated_icons/animated_icons.dart
// material_ui/lib/src/animated_icons/animated_icons_data.dart

public abstract class AnimatedIconData
{
    protected AnimatedIconData()
    {
    }

    public abstract bool MatchTextDirection { get; }
}

public static partial class AnimatedIcons
{
}

public sealed class AnimatedIcon : StatelessWidget
{
    private const double DefaultIconSize = 24.0;

    public AnimatedIcon(
        AnimatedIconData icon,
        Animation<double> progress,
        Color? color = null,
        double? size = null,
        string? semanticLabel = null,
        TextDirection? textDirection = null,
        Key? key = null) : base(key)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        Color = color;
        Size = size;
        SemanticLabel = semanticLabel;
        TextDirection = textDirection;

        if (size.HasValue && (!double.IsFinite(size.Value) || size.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "AnimatedIcon size must be finite and non-negative.");
        }
    }

    public Animation<double> Progress { get; }

    public Color? Color { get; }

    public double? Size { get; }

    public AnimatedIconData Icon { get; }

    public string? SemanticLabel { get; }

    public TextDirection? TextDirection { get; }

    public override Widget Build(BuildContext context)
    {
        var iconData = (AnimatedIconDataImpl)Icon;
        var iconTheme = IconTheme.Of(context);
        double iconSize = Size ?? iconTheme.Size ?? DefaultIconSize;
        TextDirection textDirection = TextDirection ?? Directionality.Of(context);
        double iconOpacity = Math.Clamp(iconTheme.Opacity ?? 1.0, 0.0, 1.0);
        Color iconColor = Color ?? iconTheme.Color ?? Colors.Black;
        iconColor = ApplyOpacity(iconColor, iconOpacity);

        return new Semantics(
            label: SemanticLabel,
            child: new CustomPaint(
                size: new Size(iconSize, iconSize),
                painter: new AnimatedIconPainter(
                    iconData.Paths,
                    Progress,
                    iconColor,
                    iconSize / iconData.Size.Width,
                    textDirection == Plumix.UI.TextDirection.Rtl && iconData.MatchTextDirection)));
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(color.A * opacity), 0, 255);
        return Avalonia.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

internal sealed class AnimatedIconDataImpl : AnimatedIconData
{
    public AnimatedIconDataImpl(
        Size size,
        IReadOnlyList<PathFrames> paths,
        bool matchTextDirection = false)
    {
        Size = size;
        Paths = paths;
        MatchTextDirection = matchTextDirection;
    }

    internal Size Size { get; }

    internal IReadOnlyList<PathFrames> Paths { get; }

    public override bool MatchTextDirection { get; }
}

internal sealed class AnimatedIconPainter : CustomPainter
{
    public AnimatedIconPainter(
        IReadOnlyList<PathFrames> paths,
        Animation<double> progress,
        Color color,
        double scale,
        bool shouldMirror) : base(progress)
    {
        Paths = paths;
        Progress = progress;
        Color = color;
        Scale = scale;
        ShouldMirror = shouldMirror;
    }

    internal IReadOnlyList<PathFrames> Paths { get; }

    internal Animation<double> Progress { get; }

    internal Color Color { get; }

    internal double Scale { get; }

    internal bool ShouldMirror { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        Matrix transform = ShouldMirror
            ? new Matrix(-Scale, 0.0, 0.0, -Scale, size.Width, size.Height)
            : Matrix.CreateScale(Scale, Scale);
        double clampedProgress = Math.Clamp(Progress.Value, 0.0, 1.0);

        context.PushTransform(transform, transformedContext =>
        {
            foreach (PathFrames path in Paths)
            {
                double opacity = AnimatedIconInterpolation.Interpolate(path.Opacities, clampedProgress);
                byte alpha = (byte)Math.Clamp((int)Math.Round(Color.A * opacity), 0, 255);
                var brush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, Color.R, Color.G, Color.B));
                transformedContext.DrawGeometry(brush, null, path.BuildGeometry(clampedProgress));
            }
        });
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not AnimatedIconPainter oldPainter
               || oldPainter.Progress.Value != Progress.Value
               || oldPainter.Color != Color
               || !ReferenceEquals(oldPainter.Paths, Paths)
               || oldPainter.Scale != Scale;
    }

    public override bool? HitTest(Point position) => null;
}

internal sealed class PathFrames
{
    public PathFrames(
        IReadOnlyList<PathCommand> commands,
        IReadOnlyList<double> opacities)
    {
        Commands = commands;
        Opacities = opacities;
    }

    internal IReadOnlyList<PathCommand> Commands { get; }

    internal IReadOnlyList<double> Opacities { get; }

    internal Geometry BuildGeometry(double progress)
    {
        var geometry = new StreamGeometry();
        using StreamGeometryContext path = geometry.Open();
        bool figureOpen = false;

        foreach (PathCommand command in Commands)
        {
            switch (command)
            {
                case PathMoveTo moveTo:
                    if (figureOpen)
                    {
                        path.EndFigure(isClosed: false);
                    }
                    path.BeginFigure(moveTo.Interpolate(progress), isFilled: true);
                    figureOpen = true;
                    break;
                case PathCubicTo cubicTo:
                    path.CubicBezierTo(
                        cubicTo.InterpolateControlPoint1(progress),
                        cubicTo.InterpolateControlPoint2(progress),
                        cubicTo.InterpolateTargetPoint(progress));
                    break;
                case PathClose:
                    if (figureOpen)
                    {
                        path.EndFigure(isClosed: true);
                        figureOpen = false;
                    }
                    break;
            }
        }

        if (figureOpen)
        {
            path.EndFigure(isClosed: false);
        }

        return geometry;
    }
}

internal abstract class PathCommand
{
}

internal sealed class PathMoveTo(IReadOnlyList<Point> points) : PathCommand
{
    internal IReadOnlyList<Point> Points { get; } = points;

    internal Point Interpolate(double progress) => AnimatedIconInterpolation.Interpolate(Points, progress);
}

internal sealed class PathCubicTo(
    IReadOnlyList<Point> controlPoints1,
    IReadOnlyList<Point> controlPoints2,
    IReadOnlyList<Point> targetPoints) : PathCommand
{
    internal IReadOnlyList<Point> ControlPoints1 { get; } = controlPoints1;

    internal IReadOnlyList<Point> ControlPoints2 { get; } = controlPoints2;

    internal IReadOnlyList<Point> TargetPoints { get; } = targetPoints;

    internal Point InterpolateControlPoint1(double progress) =>
        AnimatedIconInterpolation.Interpolate(ControlPoints1, progress);

    internal Point InterpolateControlPoint2(double progress) =>
        AnimatedIconInterpolation.Interpolate(ControlPoints2, progress);

    internal Point InterpolateTargetPoint(double progress) =>
        AnimatedIconInterpolation.Interpolate(TargetPoints, progress);
}

internal sealed class PathClose : PathCommand
{
}

internal static class AnimatedIconInterpolation
{
    internal static double Interpolate(IReadOnlyList<double> values, double progress)
    {
        if (values.Count == 1)
        {
            return values[0];
        }

        double targetIndex = (values.Count - 1) * progress;
        int lowIndex = (int)Math.Floor(targetIndex);
        int highIndex = (int)Math.Ceiling(targetIndex);
        double t = targetIndex - lowIndex;
        return values[lowIndex] + ((values[highIndex] - values[lowIndex]) * t);
    }

    internal static Point Interpolate(IReadOnlyList<Point> values, double progress)
    {
        if (values.Count == 1)
        {
            return values[0];
        }

        double targetIndex = (values.Count - 1) * progress;
        int lowIndex = (int)Math.Floor(targetIndex);
        int highIndex = (int)Math.Ceiling(targetIndex);
        double t = targetIndex - lowIndex;
        Point low = values[lowIndex];
        Point high = values[highIndex];
        return new Point(
            low.X + ((high.X - low.X) * t),
            low.Y + ((high.Y - low.Y) * t));
    }
}
