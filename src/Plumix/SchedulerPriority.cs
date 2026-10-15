// Dart parity source: flutter/packages/flutter/lib/src/scheduler/priority.dart

namespace Plumix;

/// <summary>
/// A task priority, as passed to
/// <see cref="SchedulerBinding.ScheduleTask{T}(Func{T}, Priority, string?, Flow?)"/>.
/// </summary>
/// <remarks>
/// Dart's immutable <c>Priority</c>, which keeps identity equality: like Dart, this class does not
/// override equality, so compare <see cref="Value"/>s.
/// </remarks>
public sealed class Priority
{
    private readonly int _value;

    private Priority(int value) => _value = value;

    /// <summary>Integer that describes this Priority value.</summary>
    public int Value => _value;

    /// <summary>A task to run after all other tasks, when no animations are running.</summary>
    public static readonly Priority Idle = new(0);

    /// <summary>A task to run even when animations are running.</summary>
    public static readonly Priority Animation = new(100000);

    /// <summary>A task to run even when the user is interacting with the device.</summary>
    public static readonly Priority Touch = new(200000);

    /// <summary>Relative priorities are clamped by this offset.</summary>
    /// <remarks>It is still possible to have priorities that are offset by more than this amount by
    /// repeatedly taking relative offsets, but that's generally discouraged.</remarks>
    public const int KMaxOffset = 10000;

    /// <summary>Returns a priority relative to this priority.</summary>
    /// <remarks>
    /// A positive <paramref name="offset"/> indicates a higher priority. The parameter
    /// <paramref name="offset"/> is clamped to ±<see cref="KMaxOffset"/>.
    /// </remarks>
    public static Priority operator +(Priority priority, int offset)
    {
        ArgumentNullException.ThrowIfNull(priority);

        if (Math.Abs((long)offset) > KMaxOffset)
        {
            // Clamp the input offset.
            offset = KMaxOffset * Math.Sign(offset);
        }

        return new Priority(priority._value + offset);
    }

    /// <summary>Returns a priority relative to this priority.</summary>
    /// <remarks>A positive <paramref name="offset"/> indicates a lower priority. The parameter
    /// <paramref name="offset"/> is clamped to ±<see cref="KMaxOffset"/>.</remarks>
    public static Priority operator -(Priority priority, int offset) =>
        priority + (offset == int.MinValue ? int.MaxValue : -offset);
}
