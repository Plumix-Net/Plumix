// Dart parity source: flutter/engine/src/flutter/lib/ui/window.dart

namespace Plumix.UI;

/// <summary>Describes the contrast of a theme or color palette.</summary>
/// <remarks>
/// Declared light-first (Dart declares <c>dark</c> first) so that <c>default(Brightness)</c> stays
/// <see cref="Light"/>, as it was for the framework's former <c>PlatformBrightness</c> and
/// Material's former <c>Brightness</c>.
/// </remarks>
public enum Brightness
{
    /// <summary>The color is light and will require a dark text color to achieve readable contrast.</summary>
    Light,

    /// <summary>The color is dark and will require a light text color to achieve readable contrast.</summary>
    Dark,
}
