using Avalonia;
using Avalonia.Media;

namespace Plumix.UI;

// C#-only infrastructure: the engine side of `SceneBuilder.addPerformanceOverlay`. A port of the engine's
// `flow/stopwatch.{h,cc}` (`Stopwatch`, `FixedRefreshRateStopwatch`, `StopwatchVisualizer`) and
// `flow/stopwatch_dl.cc` (`DlStopwatchVisualizer`), drawn into an Avalonia drawing context instead of a
// display-list vertex buffer. No Dart source maps to this file.

/// <summary>Records the last <see cref="MaxSamples"/> lap times of a repeating task.</summary>
/// <remarks>
/// The engine's <c>flutter::Stopwatch</c> with a <c>FixedRefreshRateUpdater</c>: the frame budget is
/// fixed at construction (the engine's <c>fml::kDefaultFrameBudget</c>, 1000/60 ms, by default).
/// </remarks>
internal sealed class FrameStopwatch
{
    /// <summary>The number of laps kept: the engine's <c>Stopwatch::kMaxSamples</c>.</summary>
    public const int MaxSamples = 120;

    /// <summary>The engine's <c>fml::kDefaultFrameBudget</c>: one frame at 60 Hz, in milliseconds.</summary>
    public const double DefaultFrameBudgetMilliseconds = 1000.0 / 60.0;

    private readonly TimeSpan[] _laps = new TimeSpan[MaxSamples];
    private long _start = System.Diagnostics.Stopwatch.GetTimestamp();

    public FrameStopwatch(double frameBudgetMilliseconds = DefaultFrameBudgetMilliseconds)
    {
        FrameBudgetMilliseconds = frameBudgetMilliseconds;
    }

    /// <summary>The time one frame may take: <c>Stopwatch::GetFrameBudget</c>.</summary>
    public double FrameBudgetMilliseconds { get; }

    /// <summary>The index of the most recent lap: <c>Stopwatch::GetCurrentSample</c>.</summary>
    public int CurrentSample { get; private set; } = MaxSamples - 1;

    /// <summary>The number of laps kept: <c>Stopwatch::GetLapsCount</c>.</summary>
    public int LapsCount => _laps.Length;

    /// <summary>The most recent lap: <c>Stopwatch::LastLap</c>.</summary>
    public TimeSpan LastLap => _laps[CurrentSample];

    /// <summary>The lap stored at <paramref name="index"/>: <c>Stopwatch::GetLap</c>.</summary>
    public TimeSpan GetLap(int index) => _laps[index];

    /// <summary>Starts a lap: <c>Stopwatch::Start</c>.</summary>
    public void Start()
    {
        _start = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    /// <summary>Ends the lap started by <see cref="Start"/>: <c>Stopwatch::Stop</c>.</summary>
    public void Stop()
    {
        SetLapTime(System.Diagnostics.Stopwatch.GetElapsedTime(_start));
    }

    /// <summary>Records <paramref name="delta"/> as the next lap: <c>Stopwatch::SetLapTime</c>.</summary>
    public void SetLapTime(TimeSpan delta)
    {
        CurrentSample = (CurrentSample + 1) % MaxSamples;
        _laps[CurrentSample] = delta;
    }

    /// <summary>The longest kept lap: <c>Stopwatch::MaxDelta</c>.</summary>
    public TimeSpan MaxDelta()
    {
        TimeSpan maxDelta = TimeSpan.Zero;
        for (int i = 0; i < MaxSamples; i++)
        {
            if (_laps[i] > maxDelta)
            {
                maxDelta = _laps[i];
            }
        }

        return maxDelta;
    }

    /// <summary>The mean of every kept lap, unrecorded ones counting as zero: <c>AverageDelta</c>.</summary>
    public TimeSpan AverageDelta()
    {
        TimeSpan sum = TimeSpan.Zero;
        for (int i = 0; i < MaxSamples; i++)
        {
            sum += _laps[i];
        }

        return sum / MaxSamples;
    }

    /// <summary>
    /// <c>StopwatchVisualizer::UnitFrameInterval</c>: <paramref name="timeMilliseconds"/> in frame budgets.
    /// </summary>
    internal double UnitFrameInterval(double timeMilliseconds) => timeMilliseconds / FrameBudgetMilliseconds;

    /// <summary><c>StopwatchVisualizer::UnitHeight</c>: the bar height as a fraction, at most 1.</summary>
    internal double UnitHeight(double timeMilliseconds, double maxUnitInterval)
    {
        double unitHeight = UnitFrameInterval(timeMilliseconds) / maxUnitInterval;
        if (unitHeight > 1.0)
        {
            unitHeight = 1.0;
        }

        return unitHeight;
    }

    /// <summary>
    /// Draws this stopwatch's lap graph into <paramref name="rect"/>: <c>DlStopwatchVisualizer::Visualize</c>.
    /// </summary>
    public void Visualize(DrawingContext context, Rect rect)
    {
        const int maxFrameMarkers = 8;

        // Establish the graph position.
        double x = rect.X;
        double y = rect.Y;
        double width = rect.Width;
        double height = rect.Height;
        double bottom = rect.Bottom;

        // Scale the graph to show time frames up to those that are 3x the frame time.
        double oneFrameMilliseconds = FrameBudgetMilliseconds;
        double maxInterval = oneFrameMilliseconds * 3.0;
        double maxUnitInterval = UnitFrameInterval(maxInterval);
        double sampleUnitWidth = width / MaxSamples;

        // Provide a semi-transparent background for the graph.
        FillRect(context, rect, 0x99FFFFFF);

        // Prepare a path for the data; we start at the height of the last point so it looks like we
        // wrap around.
        double barLeft = x;
        for (int i = 0; i < LapsCount; i++)
        {
            double timeMilliseconds = GetLap(i).TotalMilliseconds;
            double sampleUnitHeight = height * UnitHeight(timeMilliseconds, maxUnitInterval);
            double barTop = bottom - sampleUnitHeight;
            double barRight = x + ((i + 1) * sampleUnitWidth);
            FillRect(context, new Rect(new Point(barLeft, barTop), new Point(barRight, bottom)), 0xAA0000FF);
            barLeft = barRight;
        }

        // Draw horizontal frame markers.
        if (maxInterval > oneFrameMilliseconds)
        {
            // Paint the horizontal markers.
            int count = (int)(maxInterval / oneFrameMilliseconds);
            // Limit the number of markers to a reasonable amount.
            if (count > maxFrameMarkers)
            {
                count = 1;
            }

            for (int i = 0; i < count; i++)
            {
                double frameHeight =
                    height * (1.0 - (UnitFrameInterval(i + 1) * oneFrameMilliseconds / maxUnitInterval));
                // Draw a skinny rectangle (i.e. a line).
                FillRect(
                    context,
                    new Rect(new Point(x, y + frameHeight), new Point(x + width, y + frameHeight + 1)),
                    0xCC000000);
            }
        }

        // Paint the vertical marker for the current frame.
        uint color = 0xFF00FF00;
        if (UnitFrameInterval(LastLap.TotalMilliseconds) > 1.0)
        {
            // Budget exceeded.
            color = 0xFFFF0000;
        }

        int sample = (CurrentSample + 1) % LapsCount;
        double left = x + (sample * sampleUnitWidth);
        FillRect(context, new Rect(new Point(left, y), new Point(left + sampleUnitWidth, rect.Bottom)), color);
    }

    private static void FillRect(DrawingContext context, Rect rect, uint argb)
    {
        context.DrawRectangle(new SolidColorBrush(Avalonia.Media.Color.FromUInt32(argb)), null, rect);
    }
}

/// <summary>The per-surface frame statistics the performance overlay draws.</summary>
/// <remarks>
/// The engine's <c>flutter::CompositorContext</c> stopwatches: <see cref="RasterTime"/> laps the
/// rasterization of each frame, <see cref="UiTime"/> the build that produced it.
/// </remarks>
internal sealed class CompositorContext
{
    /// <summary><c>CompositorContext::raster_time</c>.</summary>
    public FrameStopwatch RasterTime { get; } = new();

    /// <summary><c>CompositorContext::ui_time</c>.</summary>
    public FrameStopwatch UiTime { get; } = new();
}
