namespace Plumix.Foundation;

// C#-only infrastructure: the Dart VM's seeded `dart:math` `Random` (sdk/lib/_internal/vm/lib/math_patch.dart).
// Flutter's `Curve2D.generateSamples` draws its subdivision points from `Random(samplingSeed)`, so
// reproducing Flutter's sample set (and `CatmullRomCurve`'s values) needs the same sequence bit for bit.

/// <summary>The Dart VM's seeded pseudo-random generator: a multiply-with-carry over 64-bit state.</summary>
internal sealed class DartRandom
{
    private const ulong Mask32 = 0xFFFFFFFF;
    private const double Pow2_27 = 1 << 27;
    private const double Pow2_53 = 1L << 53;

    private ulong _lo;
    private ulong _hi;

    /// <summary>Dart's <c>Random(seed)</c>: seed setup, then four state cranks.</summary>
    public DartRandom(long seed)
    {
        ulong state = (ulong)SetupSeed(seed);
        _lo = state & Mask32;
        _hi = state >> 32;
        NextState();
        NextState();
        NextState();
        NextState();
    }

    /// <summary>Dart's <c>nextInt(max)</c> for a power-of-two <paramref name="max"/>.</summary>
    public int NextIntPowerOfTwo(int max)
    {
        NextState();
        return (int)(_lo & (ulong)(max - 1));
    }

    /// <summary>Dart's <c>nextDouble()</c>: 53 random bits scaled into [0, 1).</summary>
    public double NextDouble()
    {
        return ((NextIntPowerOfTwo(1 << 26) * Pow2_27) + NextIntPowerOfTwo(1 << 27)) / Pow2_53;
    }

    // `_Random._nextState`.
    private void NextState()
    {
        const ulong a = 0xffffda61;
        ulong state = (a * _lo) + _hi;
        _lo = state & Mask32;
        _hi = state >> 32;
    }

    // `_Random._setupSeed`: 64-bit wrapping arithmetic with logical shifts.
    private static long SetupSeed(long seed)
    {
        unchecked
        {
            long n = seed;
            n = ~n + (n << 21);
            n ^= n >>> 24;
            n *= 265;
            n ^= n >>> 14;
            n *= 21;
            n ^= n >>> 28;
            n += n << 31;
            if (n == 0)
            {
                n = 0x5a17;
            }

            return n;
        }
    }
}
