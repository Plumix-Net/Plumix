// Dart parity source: material_ui/lib/src/colors.dart

using Avalonia.Media;
using Plumix.Painting;

namespace Plumix.Material;

/// <summary>
/// Defines a single color as well a color swatch with ten shades of the color.
/// </summary>
/// <remarks>
/// The color's shades are referred to by index. The greater the index, the darker the color.
/// There are 10 valid indices: 50, 100, 200, ..., 900. The value of this color should be the same
/// as the value of index 500 and <see cref="Shade500"/>.
/// </remarks>
public class MaterialColor : ColorSwatch<int>
{
    /// <summary>Creates a color swatch with a variety of shades.</summary>
    public MaterialColor(uint primary, IReadOnlyDictionary<int, Color> swatch)
        : base(primary, swatch)
    {
    }

    /// <summary>The lightest shade.</summary>
    public Color Shade50 => this[50]!.Value;

    /// <summary>The second lightest shade.</summary>
    public Color Shade100 => this[100]!.Value;

    /// <summary>The third lightest shade.</summary>
    public Color Shade200 => this[200]!.Value;

    /// <summary>The fourth lightest shade.</summary>
    public Color Shade300 => this[300]!.Value;

    /// <summary>The fifth lightest shade.</summary>
    public Color Shade400 => this[400]!.Value;

    /// <summary>The default shade.</summary>
    public Color Shade500 => this[500]!.Value;

    /// <summary>The fourth darkest shade.</summary>
    public Color Shade600 => this[600]!.Value;

    /// <summary>The third darkest shade.</summary>
    public Color Shade700 => this[700]!.Value;

    /// <summary>The second darkest shade.</summary>
    public Color Shade800 => this[800]!.Value;

    /// <summary>The darkest shade.</summary>
    public Color Shade900 => this[900]!.Value;
}

/// <summary>
/// Defines a single accent color as well a swatch of four shades of the accent color.
/// </summary>
/// <remarks>
/// The color's shades are referred to by index, the colors with smaller indices are lighter,
/// larger indices are darker. There are four valid indices: 100, 200, 400, and 700. The value of
/// this color should be the same as the value of index 200 and <see cref="Shade200"/>.
/// </remarks>
public class MaterialAccentColor : ColorSwatch<int>
{
    /// <summary>
    /// Creates a color swatch with a variety of shades appropriate for accent colors.
    /// </summary>
    public MaterialAccentColor(uint primary, IReadOnlyDictionary<int, Color> swatch)
        : base(primary, swatch)
    {
    }

    /// <summary>The lightest shade.</summary>
    public Color Shade100 => this[100]!.Value;

    /// <summary>The default shade.</summary>
    public Color Shade200 => this[200]!.Value;

    /// <summary>The second darkest shade.</summary>
    public Color Shade400 => this[400]!.Value;

    /// <summary>The darkest shade.</summary>
    public Color Shade700 => this[700]!.Value;
}
