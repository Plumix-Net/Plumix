using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/shadows.dart

/// <summary>
/// Dart's `kElevationToShadow`: the Material Design elevation-to-shadow table. Only the elevations bound
/// to one or more widgets are defined (1, 2, 3, 4, 6, 8, 9, 12, 16, 24); elevation 0 maps to an empty
/// list and every other elevation is absent, exactly as in the Dart map.
/// </summary>
public static class MaterialShadows
{
    private static readonly Color KeyUmbraOpacity = Color.FromArgb(0x33, 0x00, 0x00, 0x00);
    private static readonly Color KeyPenumbraOpacity = Color.FromArgb(0x24, 0x00, 0x00, 0x00);
    private static readonly Color AmbientShadowOpacity = Color.FromArgb(0x1F, 0x00, 0x00, 0x00);

    private static readonly Dictionary<int, IReadOnlyList<BoxShadow>> Table = new()
    {
        // The empty list depicts no elevation.
        [0] = [],
        [1] =
        [
            Umbra(2.0, 1.0, -1.0),
            Penumbra(1.0, 1.0, 0.0),
            Ambient(1.0, 3.0, 0.0),
        ],
        [2] =
        [
            Umbra(3.0, 1.0, -2.0),
            Penumbra(2.0, 2.0, 0.0),
            Ambient(1.0, 5.0, 0.0),
        ],
        [3] =
        [
            Umbra(3.0, 3.0, -2.0),
            Penumbra(3.0, 4.0, 0.0),
            Ambient(1.0, 8.0, 0.0),
        ],
        [4] =
        [
            Umbra(2.0, 4.0, -1.0),
            Penumbra(4.0, 5.0, 0.0),
            Ambient(1.0, 10.0, 0.0),
        ],
        [6] =
        [
            Umbra(3.0, 5.0, -1.0),
            Penumbra(6.0, 10.0, 0.0),
            Ambient(1.0, 18.0, 0.0),
        ],
        [8] =
        [
            Umbra(5.0, 5.0, -3.0),
            Penumbra(8.0, 10.0, 1.0),
            Ambient(3.0, 14.0, 2.0),
        ],
        [9] =
        [
            Umbra(5.0, 6.0, -3.0),
            Penumbra(9.0, 12.0, 1.0),
            Ambient(3.0, 16.0, 2.0),
        ],
        [12] =
        [
            Umbra(7.0, 8.0, -4.0),
            Penumbra(12.0, 17.0, 2.0),
            Ambient(5.0, 22.0, 4.0),
        ],
        [16] =
        [
            Umbra(8.0, 10.0, -5.0),
            Penumbra(16.0, 24.0, 2.0),
            Ambient(6.0, 30.0, 5.0),
        ],
        [24] =
        [
            Umbra(11.0, 15.0, -7.0),
            Penumbra(24.0, 38.0, 3.0),
            Ambient(9.0, 46.0, 8.0),
        ],
    };

    /// <summary>
    /// Dart's `kElevationToShadow`. Each defined entry has three shadows that must be combined to obtain
    /// the effect for that elevation.
    /// </summary>
    public static IReadOnlyDictionary<int, IReadOnlyList<BoxShadow>> ElevationToShadow => Table;

    /// <summary>
    /// The Dart map's index operator: the shadows for <paramref name="elevation"/>, or <see langword="null"/>
    /// when the elevation has no defined entry.
    /// </summary>
    public static IReadOnlyList<BoxShadow>? ForElevation(int elevation) =>
        Table.TryGetValue(elevation, out IReadOnlyList<BoxShadow>? shadows) ? shadows : null;

    private static BoxShadow Umbra(double dy, double blurRadius, double spreadRadius) =>
        new(KeyUmbraOpacity, new Point(0.0, dy), blurRadius, spreadRadius);

    private static BoxShadow Penumbra(double dy, double blurRadius, double spreadRadius) =>
        new(KeyPenumbraOpacity, new Point(0.0, dy), blurRadius, spreadRadius);

    private static BoxShadow Ambient(double dy, double blurRadius, double spreadRadius) =>
        new(AmbientShadowOpacity, new Point(0.0, dy), blurRadius, spreadRadius);
}
