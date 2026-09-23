using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/alignment.dart

public readonly record struct AlignmentDirectional(double Start, double Y)
{
    public static AlignmentDirectional TopStart => new(-1, -1);

    public static AlignmentDirectional TopCenter => new(0, -1);

    public static AlignmentDirectional TopEnd => new(1, -1);

    public static AlignmentDirectional CenterStart => new(-1, 0);

    public static AlignmentDirectional Center => new(0, 0);

    public static AlignmentDirectional CenterEnd => new(1, 0);

    public static AlignmentDirectional BottomStart => new(-1, 1);

    public static AlignmentDirectional BottomCenter => new(0, 1);

    public static AlignmentDirectional BottomEnd => new(1, 1);

    public static AlignmentDirectional operator +(AlignmentDirectional a, AlignmentDirectional b) =>
        new(a.Start + b.Start, a.Y + b.Y);

    public static AlignmentDirectional operator -(AlignmentDirectional a, AlignmentDirectional b) =>
        new(a.Start - b.Start, a.Y - b.Y);

    public static AlignmentDirectional operator -(AlignmentDirectional value) => new(-value.Start, -value.Y);

    public static AlignmentDirectional operator *(AlignmentDirectional value, double factor) =>
        new(value.Start * factor, value.Y * factor);

    public static AlignmentDirectional operator /(AlignmentDirectional value, double divisor) =>
        new(value.Start / divisor, value.Y / divisor);

    public static AlignmentDirectional operator %(AlignmentDirectional value, double divisor) =>
        new(AlignmentGeometry.Modulo(value.Start, divisor), AlignmentGeometry.Modulo(value.Y, divisor));

    public AlignmentDirectional TruncateDivide(double divisor) =>
        new(Math.Truncate(Start / divisor), Math.Truncate(Y / divisor));

    public AlignmentGeometry Add(AlignmentGeometry other) => (AlignmentGeometry)this + other;

    public static AlignmentDirectional? Lerp(AlignmentDirectional? a, AlignmentDirectional? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        AlignmentDirectional from = a ?? Center;
        AlignmentDirectional to = b ?? Center;
        return new AlignmentDirectional(
            from.Start + ((to.Start - from.Start) * t),
            from.Y + ((to.Y - from.Y) * t));
    }

    public Alignment Resolve(TextDirection direction)
    {
        double x = direction == TextDirection.Rtl ? -Start : Start;
        return new Alignment(x, Y);
    }

    public Alignment Resolve(TextDirection? direction)
    {
        if (direction is null)
        {
            throw new ArgumentNullException(nameof(direction));
        }

        return Resolve(direction.Value);
    }

    public override string ToString()
    {
        return (Start, Y) switch
        {
            (-1.0, -1.0) => "AlignmentDirectional.topStart",
            (0.0, -1.0) => "AlignmentDirectional.topCenter",
            (1.0, -1.0) => "AlignmentDirectional.topEnd",
            (-1.0, 0.0) => "AlignmentDirectional.centerStart",
            (0.0, 0.0) => "AlignmentDirectional.center",
            (1.0, 0.0) => "AlignmentDirectional.centerEnd",
            (-1.0, 1.0) => "AlignmentDirectional.bottomStart",
            (0.0, 1.0) => "AlignmentDirectional.bottomCenter",
            (1.0, 1.0) => "AlignmentDirectional.bottomEnd",
            _ => $"AlignmentDirectional({DartFormat.Fixed(Start)}, {DartFormat.Fixed(Y)})",
        };
    }
}

public readonly record struct AlignmentGeometry
{
    private AlignmentGeometry(double x, double start, double y, bool isDirectional, bool isMixed = false)
    {
        PhysicalX = x;
        Start = start;
        Y = y;
        IsDirectional = isDirectional;
        IsMixed = isMixed;
    }

    private double PhysicalX { get; }

    private double Start { get; }

    public double X => PhysicalX + Start;

    public double Y { get; }

    /// <summary>
    /// Whether this value came from an <see cref="AlignmentDirectional"/>, mirroring Flutter's
    /// `alignment is AlignmentDirectional` check. A value lerped between the two kinds is mixed,
    /// and reports <see langword="false"/> just as Dart's `_MixedAlignment` does.
    /// </summary>
    public bool IsDirectional { get; }

    private bool IsMixed { get; }

    /// <summary>
    /// Whether resolving this value needs a text direction, mirroring the assert Dart's
    /// `AlignmentDirectional.resolve` and `_MixedAlignment.resolve` share.
    /// </summary>
    public bool RequiresTextDirection => IsDirectional || IsMixed;

    public static AlignmentGeometry TopLeft => Alignment.TopLeft;
    public static AlignmentGeometry TopCenter => Alignment.TopCenter;
    public static AlignmentGeometry TopRight => Alignment.TopRight;
    public static AlignmentGeometry TopStart => AlignmentDirectional.TopStart;
    public static AlignmentGeometry TopEnd => AlignmentDirectional.TopEnd;
    public static AlignmentGeometry CenterLeft => Alignment.CenterLeft;
    public static AlignmentGeometry Center => Alignment.Center;
    public static AlignmentGeometry CenterRight => Alignment.CenterRight;
    public static AlignmentGeometry CenterStart => AlignmentDirectional.CenterStart;
    public static AlignmentGeometry CenterEnd => AlignmentDirectional.CenterEnd;
    public static AlignmentGeometry BottomLeft => Alignment.BottomLeft;
    public static AlignmentGeometry BottomCenter => Alignment.BottomCenter;
    public static AlignmentGeometry BottomRight => Alignment.BottomRight;
    public static AlignmentGeometry BottomStart => AlignmentDirectional.BottomStart;
    public static AlignmentGeometry BottomEnd => AlignmentDirectional.BottomEnd;

    public static AlignmentGeometry Xy(double x, double y) => new Alignment(x, y);

    public static AlignmentGeometry Directional(double start, double y) => new AlignmentDirectional(start, y);

    public static AlignmentGeometry operator +(AlignmentGeometry a, AlignmentGeometry b)
    {
        bool samePhysical = !a.IsDirectional && !a.IsMixed && !b.IsDirectional && !b.IsMixed;
        bool sameDirectional = a.IsDirectional && b.IsDirectional;
        return new AlignmentGeometry(
            a.PhysicalX + b.PhysicalX,
            a.Start + b.Start,
            a.Y + b.Y,
            sameDirectional,
            isMixed: !samePhysical && !sameDirectional);
    }

    public static AlignmentGeometry operator -(AlignmentGeometry value) =>
        new(-value.PhysicalX, -value.Start, -value.Y, value.IsDirectional, value.IsMixed);

    public static AlignmentGeometry operator *(AlignmentGeometry value, double factor) =>
        new(value.PhysicalX * factor, value.Start * factor, value.Y * factor,
            value.IsDirectional, value.IsMixed);

    public static AlignmentGeometry operator /(AlignmentGeometry value, double divisor) =>
        new(value.PhysicalX / divisor, value.Start / divisor, value.Y / divisor,
            value.IsDirectional, value.IsMixed);

    public static AlignmentGeometry operator %(AlignmentGeometry value, double divisor) =>
        new(Modulo(value.PhysicalX, divisor), Modulo(value.Start, divisor), Modulo(value.Y, divisor),
            value.IsDirectional, value.IsMixed);

    public AlignmentGeometry TruncateDivide(double divisor) =>
        new(Math.Truncate(PhysicalX / divisor), Math.Truncate(Start / divisor), Math.Truncate(Y / divisor),
            IsDirectional, IsMixed);

    public AlignmentGeometry Add(AlignmentGeometry other) => this + other;

    public Alignment Resolve(TextDirection direction)
    {
        double x = PhysicalX + (direction == TextDirection.Rtl ? -Start : Start);
        return new Alignment(x, Y);
    }

    /// <summary>
    /// Resolves against an optional direction: a purely physical alignment ignores it, while a
    /// directional (or mixed) one requires it.
    /// </summary>
    public Alignment Resolve(TextDirection? direction)
    {
        if (direction is { } value)
        {
            return Resolve(value);
        }

        if (RequiresTextDirection)
        {
            throw new ArgumentNullException(
                nameof(direction),
                "A directional alignment cannot be resolved without a text direction.");
        }

        return new Alignment(PhysicalX, Y);
    }

    public static AlignmentGeometry? Lerp(
        AlignmentGeometry? a,
        AlignmentGeometry? b,
        double t)
    {
        if (a is null && b is null)
        {
            return a;
        }

        if (a is null)
        {
            return b!.Value * t;
        }

        if (b is null)
        {
            return a.Value * (1.0 - t);
        }

        AlignmentGeometry from = a.Value;
        AlignmentGeometry to = b.Value;
        bool samePhysical = !from.IsDirectional && !from.IsMixed && !to.IsDirectional && !to.IsMixed;
        bool sameDirectional = from.IsDirectional && to.IsDirectional;
        return new AlignmentGeometry(
            x: LerpDouble(from.PhysicalX, to.PhysicalX, t),
            start: LerpDouble(from.Start, to.Start, t),
            y: LerpDouble(from.Y, to.Y, t),
            isDirectional: sameDirectional,
            isMixed: !samePhysical && !sameDirectional);
    }

    public static implicit operator AlignmentGeometry(Alignment alignment)
    {
        return new AlignmentGeometry(alignment.X, 0.0, alignment.Y, isDirectional: false);
    }

    public static implicit operator AlignmentGeometry(AlignmentDirectional alignment)
    {
        return new AlignmentGeometry(0.0, alignment.Start, alignment.Y, isDirectional: true);
    }

    internal static double Modulo(double value, double divisor)
    {
        double remainder = value % divisor;
        return remainder < 0.0 ? remainder + Math.Abs(divisor) : remainder;
    }

    public bool Equals(AlignmentGeometry other) =>
        PhysicalX == other.PhysicalX && Start == other.Start && Y == other.Y;

    public override int GetHashCode() => HashCode.Combine(PhysicalX, Start, Y);

    private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);

    public override string ToString()
    {
        if (IsDirectional)
        {
            return new AlignmentDirectional(Start, Y).ToString();
        }

        if (Start == 0.0)
        {
            return new Alignment(PhysicalX, Y).ToString();
        }

        if (PhysicalX == 0.0)
        {
            return new AlignmentDirectional(Start, Y).ToString();
        }

        // Dart's `_MixedAlignment` prints as the sum of its physical and directional halves.
        return $"{new Alignment(PhysicalX, Y)} + {new AlignmentDirectional(Start, 0.0)}";
    }
}
