// Dart parity source: flutter/packages/flutter/lib/src/scheduler/priority.dart

namespace Plumix;

/// <summary>A priority for a task scheduled through <see cref="Scheduler.ScheduleTask{T}"/>.</summary>
public readonly record struct Priority
{
    private Priority(int value) => Value = value;

    /// <summary>Integer that describes this <see cref="Priority"/> value.</summary>
    public int Value { get; }

    /// <summary>A task to run after all other tasks, when no animations are running.</summary>
    public static Priority Idle => new(0);

    /// <summary>A task to run even when animations are running.</summary>
    public static Priority Animation => new(100000);

    /// <summary>A task to run even when the user is interacting with the device.</summary>
    public static Priority Touch => new(200000);

    /// <summary>Maximum offset by which to clamp relative priorities.</summary>
    public const int MaxOffset = 10000;

    /// <summary>Returns a priority relative to this one; a positive offset indicates a higher priority.</summary>
    public static Priority operator +(Priority priority, int offset)
    {
        if (Math.Abs(offset) > MaxOffset)
        {
            offset = MaxOffset * Math.Sign(offset);
        }

        return new Priority(priority.Value + offset);
    }

    /// <summary>Returns a priority relative to this one; a positive offset indicates a lower priority.</summary>
    public static Priority operator -(Priority priority, int offset)
    {
        if (Math.Abs(offset) > MaxOffset)
        {
            offset = MaxOffset * Math.Sign(offset);
        }

        return new Priority(priority.Value - offset);
    }
}

/// <summary>
/// Decides whether a task at a given priority should run now. Dart's `SchedulingStrategy` also takes the
/// binding instance; Plumix's scheduler is static, so the strategy only receives the priority.
/// </summary>
public delegate bool SchedulingStrategy(int priority);
