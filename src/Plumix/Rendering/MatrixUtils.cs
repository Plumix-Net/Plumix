using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/painting/matrix_utils.dart

namespace Plumix.Rendering;

/// <summary>Utility methods for <see cref="Matrix4"/>.</summary>
public static class MatrixUtils
{
    [ThreadStatic]
    private static double[]? _minMax;

    /// <summary>
    /// The offset <paramref name="transform"/> applies, when it is a pure 2D translation.
    /// </summary>
    /// <returns><c>null</c> when the matrix does anything besides translate in x and y.</returns>
    public static Point? GetAsTranslation(Matrix4 transform)
    {
        double[] values = transform.Storage;
        if (values[0] == 1.0 && values[1] == 0.0 && values[2] == 0.0 && values[3] == 0.0
            && values[4] == 0.0 && values[5] == 1.0 && values[6] == 0.0 && values[7] == 0.0
            && values[8] == 0.0 && values[9] == 0.0 && values[10] == 1.0 && values[11] == 0.0
            && values[14] == 0.0 && values[15] == 1.0)
        {
            return new Point(values[12], values[13]);
        }

        return null;
    }

    /// <summary>
    /// The scale <paramref name="transform"/> applies, when it is a uniform 2D scale.
    /// </summary>
    /// <returns><c>null</c> when the matrix does anything besides scale x and y equally.</returns>
    public static double? GetAsScale(Matrix4 transform)
    {
        double[] values = transform.Storage;
        if (values[1] == 0.0 && values[2] == 0.0 && values[3] == 0.0
            && values[4] == 0.0 && values[6] == 0.0 && values[7] == 0.0
            && values[8] == 0.0 && values[9] == 0.0 && values[10] == 1.0 && values[11] == 0.0
            && values[12] == 0.0 && values[13] == 0.0 && values[14] == 0.0 && values[15] == 1.0
            && values[0] == values[5])
        {
            return values[0];
        }

        return null;
    }

    /// <summary>Stores <c>a * b</c> into <paramref name="b"/>, leaving <paramref name="a"/> untouched.</summary>
    /// <remarks>
    /// Flutter's <c>MatrixUtils.multiplyInPlace</c>; for right-multiply see
    /// <see cref="Matrix4.Multiply"/>.
    /// </remarks>
    public static void MultiplyInPlace(Matrix4 a, Matrix4 b)
    {
        Matrix4 result = a.Multiplied(b);
        b.SetFrom(result);
    }

    /// <summary>Whether two transforms are equal, treating <c>null</c> as the identity.</summary>
    public static bool MatrixEquals(Matrix4? a, Matrix4? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null)
        {
            return IsIdentity(b!);
        }

        if (b is null)
        {
            return IsIdentity(a);
        }

        return a.Equals(b);
    }

    /// <summary>Whether <paramref name="a"/> is the identity matrix.</summary>
    public static bool IsIdentity(Matrix4 a) => a.IsIdentity();

    /// <summary>Applies <paramref name="transform"/> to <paramref name="point"/>, assuming z is zero.</summary>
    public static Point TransformPoint(Matrix4 transform, Point point)
    {
        double[] values = transform.Storage;
        double x = point.X;
        double y = point.Y;
        double rx = (values[0] * x) + (values[4] * y) + values[12];
        double ry = (values[1] * x) + (values[5] * y) + values[13];
        double rw = (values[3] * x) + (values[7] * y) + values[15];
        return rw == 1.0 ? new Point(rx, ry) : new Point(rx / rw, ry / rw);
    }

    /// <summary>The bounding rectangle of <paramref name="rect"/> after <paramref name="transform"/>.</summary>
    public static Rect TransformRect(Matrix4 transform, Rect rect)
    {
        double[] values = transform.Storage;
        double x = rect.Left;
        double y = rect.Top;
        double w = rect.Right - x;
        double h = rect.Bottom - y;
        if (!double.IsFinite(w) || !double.IsFinite(h))
        {
            return SafeTransformRect(transform, rect);
        }

        double wx = values[0] * w;
        double hx = values[4] * h;
        double rx = (values[0] * x) + (values[4] * y) + values[12];
        double wy = values[1] * w;
        double hy = values[5] * h;
        double ry = (values[1] * x) + (values[5] * y) + values[13];

        if (values[3] == 0.0 && values[7] == 0.0 && values[15] == 1.0)
        {
            double left = rx;
            double right = rx;
            if (wx < 0)
            {
                left += wx;
            }
            else
            {
                right += wx;
            }

            if (hx < 0)
            {
                left += hx;
            }
            else
            {
                right += hx;
            }

            double top = ry;
            double bottom = ry;
            if (wy < 0)
            {
                top += wy;
            }
            else
            {
                bottom += wy;
            }

            if (hy < 0)
            {
                top += hy;
            }
            else
            {
                bottom += hy;
            }

            return FromLtrb(left, top, right, bottom);
        }

        double ww = values[3] * w;
        double hw = values[7] * h;
        double rw = (values[3] * x) + (values[7] * y) + values[15];
        double ulx = rx / rw;
        double uly = ry / rw;
        double urx = (rx + wx) / (rw + ww);
        double ury = (ry + wy) / (rw + ww);
        double llx = (rx + hx) / (rw + hw);
        double lly = (ry + hy) / (rw + hw);
        double lrx = (rx + wx + hx) / (rw + ww + hw);
        double lry = (ry + wy + hy) / (rw + ww + hw);
        return FromLtrb(
            Min4(ulx, urx, llx, lrx),
            Min4(uly, ury, lly, lry),
            Max4(ulx, urx, llx, lrx),
            Max4(uly, ury, lly, lry));
    }

    private static Rect SafeTransformRect(Matrix4 transform, Rect rect)
    {
        double[] values = transform.Storage;
        bool isAffine = values[3] == 0.0 && values[7] == 0.0 && values[15] == 1.0;
        double[] minMax = _minMax ??= new double[4];
        Accumulate(values, minMax, rect.Left, rect.Top, first: true, isAffine);
        Accumulate(values, minMax, rect.Right, rect.Top, first: false, isAffine);
        Accumulate(values, minMax, rect.Left, rect.Bottom, first: false, isAffine);
        Accumulate(values, minMax, rect.Right, rect.Bottom, first: false, isAffine);
        return FromLtrb(minMax[0], minMax[1], minMax[2], minMax[3]);
    }

    private static void Accumulate(double[] m, double[] minMax, double x, double y, bool first, bool isAffine)
    {
        double w = isAffine ? 1.0 : 1.0 / ((m[3] * x) + (m[7] * y) + m[15]);
        double tx = ((m[0] * x) + (m[4] * y) + m[12]) * w;
        double ty = ((m[1] * x) + (m[5] * y) + m[13]) * w;
        if (first)
        {
            minMax[0] = minMax[2] = tx;
            minMax[1] = minMax[3] = ty;
            return;
        }

        if (tx < minMax[0])
        {
            minMax[0] = tx;
        }

        if (ty < minMax[1])
        {
            minMax[1] = ty;
        }

        if (tx > minMax[2])
        {
            minMax[2] = tx;
        }

        if (ty > minMax[3])
        {
            minMax[3] = ty;
        }
    }

    private static double Min4(double a, double b, double c, double d)
    {
        double e = a < b ? a : b;
        double f = c < d ? c : d;
        return e < f ? e : f;
    }

    private static double Max4(double a, double b, double c, double d)
    {
        double e = a > b ? a : b;
        double f = c > d ? c : d;
        return e > f ? e : f;
    }

    /// <summary>The bounding rectangle of <paramref name="rect"/> after the inverse transform.</summary>
    public static Rect InverseTransformRect(Matrix4 transform, Rect rect)
    {
        if (IsIdentity(transform))
        {
            return rect;
        }

        Matrix4 inverted = Matrix4.Copy(transform);
        inverted.Invert();
        return TransformRect(inverted, rect);
    }

    /// <summary>A transform that collapses everything onto the single point <paramref name="offset"/>.</summary>
    public static Matrix4 ForceToPoint(Point offset)
    {
        Matrix4 result = Matrix4.Zero();
        result.Storage[10] = 1;
        result.Storage[12] = offset.X;
        result.Storage[13] = offset.Y;
        result.Storage[15] = 1;
        return result;
    }

    /// <summary>
    /// A transform that rotates around a cylinder of the given radius, as seen through a camera with
    /// the given perspective.
    /// </summary>
    public static Matrix4 CreateCylindricalProjectionTransform(
        double radius,
        double angle,
        double perspective = 0.001,
        Axis orientation = Axis.Vertical)
    {
        if (perspective < 0 || perspective > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(perspective));
        }

        Matrix4 result = Matrix4.Identity();
        result.SetEntry(3, 2, -perspective);
        result.SetEntry(2, 3, -radius);
        result.SetEntry(3, 3, (perspective * radius) + 1.0);

        Matrix4 rotation = orientation switch
        {
            Axis.Horizontal => Matrix4.RotationY(angle),
            _ => Matrix4.RotationX(angle),
        };

        return result.Multiplied(rotation.Multiplied(Matrix4.TranslationValues(0.0, 0.0, radius)));
    }

    /// <summary>Flutter's <c>debugDescribeTransform</c>: one string per matrix row.</summary>
    public static IReadOnlyList<string> DebugDescribeTransform(Matrix4? transform)
    {
        if (transform is null)
        {
            return ["null"];
        }

        string[] rows = new string[4];
        for (int row = 0; row < 4; row++)
        {
            rows[row] = $"[{row}] {Format(transform.Entry(row, 0))},{Format(transform.Entry(row, 1))},"
                        + $"{Format(transform.Entry(row, 2))},{Format(transform.Entry(row, 3))}";
        }

        return rows;

        static string Format(double value) => value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Rect FromLtrb(double left, double top, double right, double bottom) =>
        new(new Point(left, top), new Point(right, bottom));
}

/// <summary>
/// Property which handles [Matrix4] that represent transforms.
/// </summary>
public sealed class TransformProperty : DiagnosticsProperty<Matrix4>
{
    /// Create a diagnostics property for [Matrix4] objects.
    public TransformProperty(
        string name,
        Matrix4? value,
        bool showName = true,
        object? defaultValue = null,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, value, showName: showName, defaultValue: defaultValue, level: level)
    {
    }

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        Matrix4? value = TypedValue;
        if (parentConfiguration is not null && !parentConfiguration.LineBreakProperties && value is not null)
        {
            // Format the value on a single line to be compatible with the parent's style.
            string[] values =
            [
                RowToString(value, 0),
                RowToString(value, 1),
                RowToString(value, 2),
                RowToString(value, 3),
            ];
            return $"[{string.Join("; ", values)}]";
        }

        return string.Join("\n", MatrixUtils.DebugDescribeTransform(value));
    }

    private static string RowToString(Matrix4 transform, int row)
    {
        return string.Join(
            ",",
            Enumerable.Range(0, 4).Select(col => DoubleProperty.FormatDouble(transform.Entry(row, col))));
    }
}
