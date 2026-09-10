using System.Globalization;

// Dart parity source: flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart
// (FramePhase, FrameTiming, DartPerformanceMode)

namespace Plumix.UI;

/// <summary>Various important time points in the lifetime of a frame.</summary>
/// <remarks>dart:ui's <c>FramePhase</c>. The order of the values is contractual:
/// <see cref="FrameTiming.TimestampInMicroseconds"/> indexes the raw data by it.</remarks>
public enum FramePhase
{
    /// <summary>The timestamp of the vsync signal given by the operating system.</summary>
    VsyncStart,

    /// <summary>When the UI thread starts building a frame.</summary>
    BuildStart,

    /// <summary>When the UI thread finishes building a frame.</summary>
    BuildFinish,

    /// <summary>When the raster thread starts rasterizing a frame.</summary>
    RasterStart,

    /// <summary>When the raster thread finishes rasterizing a frame.</summary>
    RasterFinish,

    /// <summary>When the raster thread finished rasterizing a frame, in wall time.</summary>
    RasterFinishWallTime,
}

/// <summary>Time-related performance metrics of a frame.</summary>
/// <remarks>
/// dart:ui's <c>FrameTiming</c>. A host reports these through
/// <see cref="PlatformDispatcher.ReportTimings"/>; the framework hands them to every callback
/// registered with <c>Scheduler.AddTimingsCallback</c>.
/// </remarks>
public sealed class FrameTiming
{
    private readonly long[] _data;

    /// <summary>Creates a frame timing from the raw engine timestamps, in microseconds.</summary>
    public FrameTiming(
        long vsyncStart,
        long buildStart,
        long buildFinish,
        long rasterStart,
        long rasterFinish,
        long rasterFinishWallTime,
        int layerCacheCount = 0,
        int layerCacheBytes = 0,
        int pictureCacheCount = 0,
        int pictureCacheBytes = 0,
        int frameNumber = -1)
    {
        _data =
        [
            vsyncStart,
            buildStart,
            buildFinish,
            rasterStart,
            rasterFinish,
            rasterFinishWallTime,
            layerCacheCount,
            layerCacheBytes,
            pictureCacheCount,
            pictureCacheBytes,
            frameNumber,
        ];
    }

    /// <summary>The duration to build the frame on the UI thread.</summary>
    public TimeSpan BuildDuration => RawDuration(FramePhase.BuildFinish) - RawDuration(FramePhase.BuildStart);

    /// <summary>The duration to rasterize the frame on the raster thread.</summary>
    public TimeSpan RasterDuration => RawDuration(FramePhase.RasterFinish) - RawDuration(FramePhase.RasterStart);

    /// <summary>The duration between receiving the vsync signal and starting to build the frame.</summary>
    public TimeSpan VsyncOverhead => RawDuration(FramePhase.BuildStart) - RawDuration(FramePhase.VsyncStart);

    /// <summary>The timespan between vsync start and raster finish.</summary>
    public TimeSpan TotalSpan => RawDuration(FramePhase.RasterFinish) - RawDuration(FramePhase.VsyncStart);

    /// <summary>The number of layers stored in the raster cache during the frame.</summary>
    public int LayerCacheCount => (int)_data[FramePhase.RasterFinishWallTime.Index() + 1];

    /// <summary>The number of bytes of image data used to cache layers during the frame.</summary>
    public int LayerCacheBytes => (int)_data[FramePhase.RasterFinishWallTime.Index() + 2];

    /// <summary><see cref="LayerCacheBytes"/> expressed in megabytes.</summary>
    public double LayerCacheMegabytes => LayerCacheBytes / 1024.0 / 1024.0;

    /// <summary>The number of pictures stored in the raster cache during the frame.</summary>
    public int PictureCacheCount => (int)_data[FramePhase.RasterFinishWallTime.Index() + 3];

    /// <summary>The number of bytes of image data used to cache pictures during the frame.</summary>
    public int PictureCacheBytes => (int)_data[FramePhase.RasterFinishWallTime.Index() + 4];

    /// <summary><see cref="PictureCacheBytes"/> expressed in megabytes.</summary>
    public double PictureCacheMegabytes => PictureCacheBytes / 1024.0 / 1024.0;

    /// <summary>The frame key associated with this frame measurement.</summary>
    public int FrameNumber => (int)_data[^1];

    /// <summary>The timestamp of the given phase, in microseconds.</summary>
    public long TimestampInMicroseconds(FramePhase phase) => _data[phase.Index()];

    public override string ToString()
    {
        return $"{nameof(FrameTiming)}(buildDuration: {FormatMilliseconds(BuildDuration)}, "
            + $"rasterDuration: {FormatMilliseconds(RasterDuration)}, "
            + $"vsyncOverhead: {FormatMilliseconds(VsyncOverhead)}, "
            + $"totalSpan: {FormatMilliseconds(TotalSpan)}, "
            + $"layerCacheCount: {LayerCacheCount}, "
            + $"layerCacheBytes: {LayerCacheBytes}, "
            + $"pictureCacheCount: {PictureCacheCount}, "
            + $"pictureCacheBytes: {PictureCacheBytes}, "
            + $"frameNumber: {FrameNumber})";
    }

    private static string FormatMilliseconds(TimeSpan duration)
    {
        double milliseconds = duration.Ticks / (double)TimeSpan.TicksPerMillisecond;
        return string.Create(CultureInfo.InvariantCulture, $"{milliseconds}ms");
    }

    private TimeSpan RawDuration(FramePhase phase)
    {
        return TimeSpan.FromTicks(_data[phase.Index()] * (TimeSpan.TicksPerMillisecond / 1000));
    }
}

/// <summary>Controls how the Dart runtime trades performance for resource use.</summary>
/// <remarks>dart:ui's <c>DartPerformanceMode</c>.</remarks>
public enum DartPerformanceMode
{
    /// <summary>The default balanced mode.</summary>
    Balanced,

    /// <summary>Optimize for low latency, delaying costly garbage collection.</summary>
    Latency,

    /// <summary>Optimize for high throughput.</summary>
    Throughput,

    /// <summary>Optimize for low memory use.</summary>
    Memory,
}

internal static class FramePhaseExtensions
{
    public static int Index(this FramePhase phase) => (int)phase;
}
