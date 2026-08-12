// Dart parity source: flutter/packages/flutter/lib/src/painting/text_scaler.dart

namespace Plumix.Painting;

/// A class that describes how textual contents should be scaled for better
/// readability.
///
/// The [Scale] function computes the scaled font size given the original
/// unscaled font size specified by app developers.
public abstract record TextScaler
{
    /// A [TextScaler] that doesn't scale the input font size.
    ///
    /// This is the default [TextScaler] everywhere.
    public static TextScaler NoScaling { get; } = new LinearTextScaler(1.0);

    /// Creates a proportional [TextScaler] that scales the incoming font size by
    /// multiplying it with the given `textScaleFactor`.
    public static TextScaler Linear(double textScaleFactor)
    {
        if (textScaleFactor < 0 || double.IsNaN(textScaleFactor) || double.IsInfinity(textScaleFactor))
        {
            throw new ArgumentOutOfRangeException(nameof(textScaleFactor));
        }

        return textScaleFactor == 1.0 ? NoScaling : new LinearTextScaler(textScaleFactor);
    }

    /// Computes the scaled font size (in logical pixels) with the given unscaled
    /// `fontSize` (in logical pixels).
    ///
    /// The input `fontSize` must be finite and non-negative.
    public abstract double Scale(double fontSize);

    /// The default font size multiplier this [TextScaler] applies, when it scales
    /// text proportionally.
    public abstract double TextScaleFactor { get; }

    /// Returns a [TextScaler] that restricts the scaled font size to within the
    /// range `[minScaleFactor * fontSize, maxScaleFactor * fontSize]`.
    public TextScaler Clamp(double minScaleFactor = 0, double maxScaleFactor = double.PositiveInfinity)
    {
        if (double.IsNaN(maxScaleFactor) || maxScaleFactor < minScaleFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(maxScaleFactor));
        }

        if (!double.IsFinite(minScaleFactor) || minScaleFactor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minScaleFactor));
        }

        return minScaleFactor == maxScaleFactor
            ? Linear(minScaleFactor)
            : new ClampedTextScaler(this, minScaleFactor, maxScaleFactor);
    }
}

/// A [TextScaler] that clamps the scaled font size of an inner scaler.
public sealed record ClampedTextScaler(TextScaler Inner, double MinScale, double MaxScale) : TextScaler
{
    public override double Scale(double fontSize) => Math.Clamp(
        Inner.Scale(fontSize),
        fontSize * MinScale,
        fontSize * MaxScale);

    public override double TextScaleFactor => Math.Clamp(Inner.TextScaleFactor, MinScale, MaxScale);
}

/// A [TextScaler] that scales the incoming font size by a constant factor.
public sealed record LinearTextScaler(double TextScaleFactorValue) : TextScaler
{
    public override double Scale(double fontSize)
    {
        if (double.IsNaN(fontSize) || double.IsInfinity(fontSize) || fontSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize));
        }

        return fontSize * TextScaleFactorValue;
    }

    public override double TextScaleFactor => TextScaleFactorValue;
}

/// Constants shared by the text stack.
public static class TextDefaults
{
    /// The default font size if none is specified.
    ///
    /// This should be kept in sync with the default values of the engine.
    public const double DefaultFontSize = 14.0;
}
