using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;

// C#-only infrastructure: no Dart parity source.

namespace Plumix.Hosting;

/// <summary>
/// Conversions between Plumix value types and Avalonia's, for applications that mix Plumix widgets
/// with Avalonia controls. Each conversion copies the values; nothing allocates.
/// </summary>
public static class AvaloniaInterop
{
    /// <summary>The 8-bit sRGB Avalonia color, quantised like <see cref="Plumix.UI.Color.ToARGB32"/>.</summary>
    public static global::Avalonia.Media.Color ToAvalonia(this Plumix.UI.Color color)
    {
        ArgumentNullException.ThrowIfNull(color);
        return global::Avalonia.Media.Color.FromUInt32(color.ToARGB32());
    }

    /// <summary>The Plumix color of an 8-bit sRGB Avalonia color.</summary>
    public static Plumix.UI.Color ToPlumix(this global::Avalonia.Media.Color color) => new(color.ToUInt32());

    /// <summary>The insets as an Avalonia <see cref="global::Avalonia.Thickness"/>.</summary>
    public static global::Avalonia.Thickness ToAvalonia(this EdgeInsets insets) =>
        new(insets.Left, insets.Top, insets.Right, insets.Bottom);

    /// <summary>The Plumix insets of an Avalonia <see cref="global::Avalonia.Thickness"/>.</summary>
    public static EdgeInsets ToPlumix(this global::Avalonia.Thickness thickness) =>
        new(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

    /// <summary>The 2D affine part of the matrix as an Avalonia <see cref="global::Avalonia.Matrix"/>.</summary>
    public static global::Avalonia.Matrix ToAvalonia(this Matrix4 matrix) => matrix.ToAvaloniaMatrix();

    /// <summary>A Plumix matrix from an Avalonia <see cref="global::Avalonia.Matrix"/>.</summary>
    public static Matrix4 ToPlumix(this global::Avalonia.Matrix matrix) => Matrix4.FromAvaloniaMatrix(matrix);

    public static global::Avalonia.Point ToAvalonia(this Offset offset) => new(offset.Dx, offset.Dy);

    public static Offset ToPlumix(this global::Avalonia.Point point) => new(point.X, point.Y);

    public static Offset ToPlumix(this global::Avalonia.Vector vector) => new(vector.X, vector.Y);

    public static global::Avalonia.Size ToAvalonia(this Plumix.UI.Size size) => new(size.Width, size.Height);

    public static Plumix.UI.Size ToPlumix(this global::Avalonia.Size size) => new(size.Width, size.Height);

    /// <summary>
    /// The rectangle as an Avalonia <see cref="global::Avalonia.Rect"/>, which stores position and
    /// size: an edge at infinity comes back as NaN.
    /// </summary>
    public static global::Avalonia.Rect ToAvalonia(this Plumix.UI.Rect rect) =>
        new(rect.Left, rect.Top, rect.Width, rect.Height);

    public static Plumix.UI.Rect ToPlumix(this global::Avalonia.Rect rect) =>
        Plumix.UI.Rect.FromLTWH(rect.X, rect.Y, rect.Width, rect.Height);

    /// <summary>Draws a recorded <see cref="Picture"/> into an Avalonia drawing context.</summary>
    public static void Playback(this Picture picture, DrawingContext context, Offset offset = default)
    {
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(context);
        CanvasPlayback.Play(picture, context, offset);
    }
}
