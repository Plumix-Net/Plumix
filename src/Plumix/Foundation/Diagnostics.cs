namespace Plumix.Foundation;

public static class Diagnostics
{
    /// Returns a 5 character long hexadecimal string generated from
    /// [Object.hashCode]'s 20 least-significant bits.
    public static string ShortHash(object? obj)
    {
        int hash = obj?.GetHashCode() ?? 0;

        int masked = hash & 0xFFFFF;

        return masked.ToString("x5");
    }
}

// Dart parity source (reference): flutter/packages/flutter/lib/src/foundation/diagnostics.dart (approximate)
