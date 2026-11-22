using Avalonia;
using Plumix.UI;

// C#-only host infrastructure: no Dart parity source. Conversions between the framework's Matrix4 (the
// `vector_math` port, src/Plumix/UI/Matrix4.cs) and Avalonia's 3x3 projective matrix, for the backend's
// playback and the public AvaloniaInterop conversions.

namespace Plumix;

internal static class Matrix4Conversions
{
    extension(Matrix4 matrix)
    {
        /// <summary>
        /// This matrix as an Avalonia 3x3 projective matrix, dropping the z row and column.
        /// </summary>
        /// <remarks>
        /// Avalonia maps points as row vectors (`p * M`) while this matrix maps them as column vectors,
        /// so the 3x3 is the transpose of the (x, y, w) sub-matrix. Skia's `SkM44::asM33` drops the same
        /// row and column, which is why a perspective transform survives the conversion.
        /// </remarks>
        internal Matrix ToAvaloniaMatrix()
        {
            double[] storage = matrix.Storage;
            return new Matrix(
                storage[0], storage[1], storage[3],
                storage[4], storage[5], storage[7],
                storage[12], storage[13], storage[15]);
        }
    }

    extension(Matrix4)
    {
        /// <summary>The 4x4 matrix that agrees with the given Avalonia 3x3 on the z = 0 plane.</summary>
        internal static Matrix4 FromAvaloniaMatrix(Matrix matrix) =>
            new(
                matrix.M11, matrix.M12, 0.0, matrix.M13,
                matrix.M21, matrix.M22, 0.0, matrix.M23,
                0.0, 0.0, 1.0, 0.0,
                matrix.M31, matrix.M32, 0.0, matrix.M33);
    }
}
