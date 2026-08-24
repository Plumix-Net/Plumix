using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/activity_indicator.dart

/// <summary>An iOS-style activity indicator that spins clockwise.</summary>
public sealed class CupertinoActivityIndicator : StatefulWidget
{
    internal const double DefaultIndicatorRadius = 10.0;

    // Extracted from iOS 13.2 Beta.
    private static readonly CupertinoDynamicColor ActiveTickColor = CupertinoDynamicColor.WithBrightness(
        color: Avalonia.Media.Color.FromUInt32(0xFF3C3C44),
        darkColor: Avalonia.Media.Color.FromUInt32(0xFFEBEBF5));

    /// <summary>Creates an iOS-style activity indicator that spins clockwise.</summary>
    public CupertinoActivityIndicator(
        Color? color = null,
        bool animating = true,
        double radius = DefaultIndicatorRadius,
        Key? key = null)
        : this(color: color, animating: animating, radius: radius, progress: 1.0, key: key)
    {
    }

    private CupertinoActivityIndicator(
        Color? color,
        bool animating,
        double radius,
        double progress,
        Key? key)
        : base(key)
    {
        if (!(radius > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "radius must be greater than zero.");
        }

        if (!(progress >= 0.0) || !(progress <= 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(progress), "progress must be between 0.0 and 1.0.");
        }

        Color = color;
        Animating = animating;
        Radius = radius;
        Progress = progress;
    }

    /// <summary>
    /// Creates a non-animated iOS-style activity indicator that displays a partial count of ticks
    /// based on the value of <paramref name="progress"/>.
    /// </summary>
    public static CupertinoActivityIndicator PartiallyRevealed(
        Color? color = null,
        double radius = DefaultIndicatorRadius,
        double progress = 1.0,
        Key? key = null)
    {
        return new CupertinoActivityIndicator(
            color: color,
            animating: false,
            radius: radius,
            progress: progress,
            key: key);
    }

    /// <summary>Color of the activity indicator. Defaults to color extracted from native iOS.</summary>
    public Color? Color { get; }

    /// <summary>Whether the activity indicator is running its animation. Defaults to true.</summary>
    public bool Animating { get; }

    /// <summary>Radius of the spinner widget. Defaults to 10 pixels. Must be positive.</summary>
    public double Radius { get; }

    /// <summary>
    /// Determines the percentage of spinner ticks that will be shown. Typical usage would display
    /// all ticks, however, this allows for more fine-grained control such as during pull-to-refresh
    /// when the drag-down action shows one tick at a time as the user continues to drag down.
    /// Defaults to one. Must be between zero and one, inclusive.
    /// </summary>
    public double Progress { get; }

    public override State CreateState() => new CupertinoActivityIndicatorState();

    private sealed class CupertinoActivityIndicatorState : State
    {
        private AnimationController? _controller;

        private CupertinoActivityIndicator CurrentWidget => (CupertinoActivityIndicator)StateWidget;

        public override void InitState()
        {
            base.InitState();
            _controller = new AnimationController(duration: TimeSpan.FromSeconds(1), vsync: this);

            if (CurrentWidget.Animating)
            {
                _controller.Repeat();
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            if (CurrentWidget.Animating != ((CupertinoActivityIndicator)oldWidget).Animating)
            {
                if (CurrentWidget.Animating)
                {
                    _controller!.Repeat();
                }
                else
                {
                    _controller!.Stop();
                }
            }
        }

        public override void Dispose()
        {
            _controller!.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return SizedBox.Square(
                dimension: CurrentWidget.Radius * 2,
                child: new CustomPaint(
                    painter: new CupertinoActivityIndicatorPainter(
                        position: _controller!,
                        activeColor: CurrentWidget.Color
                                     ?? CupertinoDynamicColor.Resolve(ActiveTickColor, context),
                        radius: CurrentWidget.Radius,
                        progress: CurrentWidget.Progress)));
        }
    }
}

internal sealed class CupertinoActivityIndicatorPainter : CustomPainter
{
    private const double TwoPi = Math.PI * 2.0;

    /// <summary>
    /// Alpha values extracted from the native component (for both dark and light mode) to draw the
    /// spinning ticks.
    /// </summary>
    internal static readonly IReadOnlyList<byte> AlphaValues = [47, 47, 47, 47, 72, 97, 122, 147];

    /// <summary>The alpha value that is used to draw the partially revealed ticks.</summary>
    internal const byte PartiallyRevealedAlpha = 147;

    public CupertinoActivityIndicatorPainter(
        Animation<double> position,
        Color activeColor,
        double radius,
        double progress)
        : base(repaint: position)
    {
        Position = position;
        ActiveColor = activeColor;
        Radius = radius;
        Progress = progress;
        // Use a RRect instead of RSuperellipse since this shape is really small
        // and should make little visual difference.
        TickFundamentalShape = RRect.FromLTRBXY(
            -radius / CupertinoActivityIndicator.DefaultIndicatorRadius,
            -radius / 3.0,
            radius / CupertinoActivityIndicator.DefaultIndicatorRadius,
            -radius,
            radius / CupertinoActivityIndicator.DefaultIndicatorRadius,
            radius / CupertinoActivityIndicator.DefaultIndicatorRadius);
    }

    public Animation<double> Position { get; }

    public Color ActiveColor { get; }

    public double Radius { get; }

    public double Progress { get; }

    public RRect TickFundamentalShape { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        int tickCount = AlphaValues.Count;

        context.PushTransform(
            Matrix4.TranslationValues(size.Width / 2.0, size.Height / 2.0, 0.0),
            centeredContext =>
            {
                int activeTick = (int)Math.Floor(tickCount * Position.Value);

                for (int i = 0; i < tickCount * Progress; ++i)
                {
                    int t = FloorModulo(i - activeTick, tickCount);
                    byte alpha = Progress < 1 ? PartiallyRevealedAlpha : AlphaValues[t];
                    var tickColor = Avalonia.Media.Color.FromArgb(
                        alpha,
                        ActiveColor.R,
                        ActiveColor.G,
                        ActiveColor.B);
                    centeredContext.PushTransform(
                        Matrix4.RotationZ(i * TwoPi / tickCount),
                        rotatedContext => rotatedContext.DrawRRect(
                            TickFundamentalShape,
                            new SolidColorBrush(tickColor),
                            pen: null));
                }
            });
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not CupertinoActivityIndicatorPainter oldPainter
               || !ReferenceEquals(oldPainter.Position, Position)
               || oldPainter.ActiveColor != ActiveColor
               || oldPainter.Progress != Progress;
    }

    /// <summary>Dart's <c>%</c> operator, whose result is never negative.</summary>
    private static int FloorModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }
}

/// <summary>
/// An iOS-style linear activity indicator: a linear progress bar that displays a colored bar to
/// indicate the progress of an ongoing task.
/// </summary>
public sealed class CupertinoLinearActivityIndicator : StatelessWidget
{
    /// <summary>Creates a linear iOS-style activity indicator.</summary>
    public CupertinoLinearActivityIndicator(
        double progress,
        double height = 4.5,
        Color? color = null,
        Key? key = null)
        : base(key)
    {
        if (!(height > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(height), "height must be greater than zero.");
        }

        if (!(progress >= 0.0) || !(progress <= 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(progress), "progress must be between 0.0 and 1.0.");
        }

        Progress = progress;
        Height = height;
        Color = color;
    }

    /// <summary>
    /// The current progress of the linear activity indicator. This value must be between 0.0 and
    /// 1.0. A value of 0.0 means no progress and 1.0 means that progress is complete.
    /// </summary>
    public double Progress { get; }

    /// <summary>
    /// The height of the line used to draw the linear activity indicator.
    /// Defaults to 4.5 units. Must be positive.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// The color of the progress bar. This color represents the portion of the bar that indicates
    /// progress. Defaults to <see cref="CupertinoColors.ActiveBlue"/> if no color is specified.
    /// </summary>
    public Color? Color { get; }

    public override Widget Build(BuildContext context)
    {
        return new ConstrainedBox(
            constraints: new BoxConstraints(MinHeight: Height, MinWidth: double.PositiveInfinity),
            child: new CustomPaint(
                painter: new CupertinoLinearActivityIndicatorPainter(progress: Progress, color: Color)));
    }
}

// Dart names this private painter _CupertinoLinearActivityIndicator; C# cannot reuse the widget's
// name in the same namespace, so the painter carries the Painter suffix.
internal sealed class CupertinoLinearActivityIndicatorPainter : CustomPainter
{
    public CupertinoLinearActivityIndicatorPainter(double progress, Color? color = null)
    {
        Progress = progress;
        Color = color;
        BackgroundPaint = new SolidColorBrush(CupertinoColors.SystemFill.Value);
        ProgressPaint = new SolidColorBrush(color ?? CupertinoColors.ActiveBlue.Value);
    }

    public double Progress { get; }

    public Color? Color { get; }

    /// <summary>
    /// The background paint used to draw the full width of the progress bar. This paint object is
    /// created once and reused to fill the background with a system fill color.
    /// </summary>
    public SolidColorBrush BackgroundPaint { get; }

    /// <summary>
    /// The paint used to draw the progress portion of the progress bar. This paint object is
    /// created once and reused to fill the progress area.
    /// </summary>
    public SolidColorBrush ProgressPaint { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        // Draw the background of the progress bar.
        context.DrawRRect(
            RRect.FromRectAndRadius(new Rect(size), Radius.Circular(size.Height / 2)),
            BackgroundPaint,
            pen: null);

        // Draw the progress portion of the bar.
        if (Progress > 0)
        {
            context.DrawRRect(
                RRect.FromRectAndRadius(
                    new Rect(new Size(Math.Clamp(Progress, 0.0, 1.0) * size.Width, size.Height)),
                    Radius.Circular(size.Height / 2)),
                ProgressPaint,
                pen: null);
        }
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not CupertinoLinearActivityIndicatorPainter old
               || old.Progress != Progress
               || old.Color != Color;
    }
}
