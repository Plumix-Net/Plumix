using Plumix.Widgets;
using Plumix.UI;
using System.Collections;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/time.dart

public enum DayPeriod
{
    Am,
    Pm,
}

public enum TimeOfDayFormat
{
    HHColonMm,
    HHDotMm,
    FrenchCanadian,
    HColonMm,
    HColonMmSpaceA,
    ASpaceHColonMm,
}

public enum HourFormat
{
    HH,
    H,
    H12,
}

public readonly record struct TimeOfDay : IComparable<TimeOfDay>
{
    public const int HoursPerDay = 24;
    public const int HoursPerPeriod = 12;
    public const int MinutesPerHour = 60;

    public TimeOfDay(int hour, int minute)
    {
        if (hour is < 0 or >= HoursPerDay) throw new ArgumentOutOfRangeException(nameof(hour));
        if (minute is < 0 or >= MinutesPerHour) throw new ArgumentOutOfRangeException(nameof(minute));
        Hour = hour;
        Minute = minute;
    }

    public int Hour { get; }
    public int Minute { get; }
    public DayPeriod Period => Hour < HoursPerPeriod ? DayPeriod.Am : DayPeriod.Pm;
    public int PeriodOffset => Period == DayPeriod.Am ? 0 : HoursPerPeriod;
    public int HourOfPeriod => Hour is 0 or 12 ? 12 : Hour - PeriodOffset;

    public static TimeOfDay Now() => FromDateTime(DateTime.Now);
    public static TimeOfDay FromDateTime(DateTime time) => new(time.Hour, time.Minute);

    public TimeOfDay Replacing(int? hour = null, int? minute = null) =>
        new(hour ?? Hour, minute ?? Minute);

    public string Format(BuildContext context) => MaterialLocalizations.Of(context).FormatTimeOfDay(
        this,
        MediaQuery.MaybeAlwaysUse24HourFormatOf(context) ?? false);

    public bool IsBefore(TimeOfDay other) => CompareTo(other) < 0;
    public bool IsAfter(TimeOfDay other) => CompareTo(other) > 0;
    public bool IsAtSameTimeAs(TimeOfDay other) => CompareTo(other) == 0;

    public int CompareTo(TimeOfDay other)
    {
        int hourComparison = Hour.CompareTo(other.Hour);
        return hourComparison == 0 ? Minute.CompareTo(other.Minute) : hourComparison;
    }

    public override string ToString() => $"TimeOfDay({Hour:00}:{Minute:00})";

    public static HourFormat HourFormatOf(TimeOfDayFormat format) => format switch
    {
        TimeOfDayFormat.HColonMmSpaceA or TimeOfDayFormat.ASpaceHColonMm => HourFormat.H12,
        TimeOfDayFormat.HColonMm => HourFormat.H,
        _ => HourFormat.HH,
    };
}

/// <summary>A restorable value that stores a time of day as minutes followed by hours.</summary>
public class RestorableTimeOfDay : RestorableValue<TimeOfDay>
{
    private readonly TimeOfDay _defaultValue;

    public RestorableTimeOfDay(TimeOfDay defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public override TimeOfDay CreateDefaultValue() => _defaultValue;

    protected override void DidUpdateValue(TimeOfDay oldValue)
    {
        RestorationSerialization.AssertSerializable(Value.Hour);
        RestorationSerialization.AssertSerializable(Value.Minute);
        NotifyListeners();
    }

    public override TimeOfDay FromPrimitives(object? data)
    {
        var timeData = (IList)data!;
        return new TimeOfDay(hour: (int)timeData[1]!, minute: (int)timeData[0]!);
    }

    public override object ToPrimitives() => new List<int> { Value.Minute, Value.Hour };
}
