namespace Plumix.Foundation;

public static class Constants
{
    /// The epsilon of tolerable double precision error.
    ///
    /// This is used in various places in the framework to allow for floating point
    /// precision loss in calculations. Differences below this threshold are safe to
    /// disregard.
    public const double PrecisionErrorTolerance = 1e-10;
}

// Dart parity source (reference): flutter/packages/flutter/lib/src/foundation/constants.dart (approximate)
