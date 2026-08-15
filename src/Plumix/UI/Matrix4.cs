using Avalonia;

// Port of the `vector_math` pub package (version 2.4.2), the matrix library Flutter itself depends on:
// vector_math/lib/src/vector_math_64/{vector3,vector4,matrix4}.dart. It is not part of the Flutter
// repository, so it carries no `Dart parity source:` marker; the members ported here are the subset
// Flutter, `material_ui` and `cupertino_ui` actually call.
//
// Storage is column-major exactly as in Dart: `Storage[col * 4 + row]`, so `Storage[12..14]` is the
// translation column and `Storage[3], [7], [11], [15]` is the perspective row. Points are column
// vectors (`p' = M * p`), which means `a.Multiply(b)` applies `b` first and `a` second.

namespace Plumix.UI;

/// <summary>A 3-component double vector.</summary>
public sealed class Vector3
{
    private readonly double[] _storage;

    private Vector3(double[] storage)
    {
        _storage = storage;
    }

    public Vector3(double x, double y, double z) : this(new double[3])
    {
        SetValues(x, y, z);
    }

    public static Vector3 Zero() => new(new double[3]);

    public static Vector3 All(double value) => new(value, value, value);

    public static Vector3 Copy(Vector3 other) => new(other.X, other.Y, other.Z);

    public double[] Storage => _storage;

    public double X
    {
        get => _storage[0];
        set => _storage[0] = value;
    }

    public double Y
    {
        get => _storage[1];
        set => _storage[1] = value;
    }

    public double Z
    {
        get => _storage[2];
        set => _storage[2] = value;
    }

    public double this[int index]
    {
        get => _storage[index];
        set => _storage[index] = value;
    }

    public void SetValues(double x, double y, double z)
    {
        _storage[0] = x;
        _storage[1] = y;
        _storage[2] = z;
    }

    public void SetFrom(Vector3 other)
    {
        _storage[0] = other._storage[0];
        _storage[1] = other._storage[1];
        _storage[2] = other._storage[2];
    }

    /// <remarks>Dart accumulates `z*z`, then `y*y`, then `x*x`, in that order.</remarks>
    public double Length2
    {
        get
        {
            double sum = Z * Z;
            sum += Y * Y;
            sum += X * X;
            return sum;
        }
    }

    public double Length => Math.Sqrt(Length2);

    public double Normalize()
    {
        double length = Length;
        if (length == 0.0)
        {
            return 0.0;
        }

        double inverse = 1.0 / length;
        Z *= inverse;
        Y *= inverse;
        X *= inverse;
        return length;
    }

    public Vector3 Clone() => Copy(this);

    public static Vector3 operator +(Vector3 left, Vector3 right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3 operator -(Vector3 left, Vector3 right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Vector3 operator -(Vector3 value) => new(-value.X, -value.Y, -value.Z);

    public static Vector3 operator *(Vector3 value, double scale) =>
        new(value.X * scale, value.Y * scale, value.Z * scale);

    public override string ToString() => $"[{_storage[0]},{_storage[1]},{_storage[2]}]";

    public override bool Equals(object? obj) =>
        obj is Vector3 other && Z == other.Z && Y == other.Y && X == other.X;

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}

/// <summary>A 4-component double vector.</summary>
public sealed class Vector4
{
    private readonly double[] _storage;

    private Vector4(double[] storage)
    {
        _storage = storage;
    }

    public Vector4(double x, double y, double z, double w) : this(new double[4])
    {
        SetValues(x, y, z, w);
    }

    public static Vector4 Zero() => new(new double[4]);

    public static Vector4 Identity() => new(0.0, 0.0, 0.0, 1.0);

    public static Vector4 Copy(Vector4 other) => new(other.X, other.Y, other.Z, other.W);

    public double[] Storage => _storage;

    public double X
    {
        get => _storage[0];
        set => _storage[0] = value;
    }

    public double Y
    {
        get => _storage[1];
        set => _storage[1] = value;
    }

    public double Z
    {
        get => _storage[2];
        set => _storage[2] = value;
    }

    public double W
    {
        get => _storage[3];
        set => _storage[3] = value;
    }

    public double this[int index]
    {
        get => _storage[index];
        set => _storage[index] = value;
    }

    public void SetValues(double x, double y, double z, double w)
    {
        _storage[0] = x;
        _storage[1] = y;
        _storage[2] = z;
        _storage[3] = w;
    }

    public override string ToString() =>
        $"[{_storage[0]},{_storage[1]},{_storage[2]},{_storage[3]}]";

    public override bool Equals(object? obj) =>
        obj is Vector4 other && W == other.W && Z == other.Z && Y == other.Y && X == other.X;

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
}

/// <summary>A 3x3 double matrix, stored column-major.</summary>
/// <remarks>Only the members `Matrix4.Decompose` and `Quaternion.SetFromRotation` need are ported.</remarks>
public sealed class Matrix3
{
    private readonly double[] _storage = new double[9];

    public static Matrix3 Zero() => new();

    public double[] Storage => _storage;

    public double this[int index]
    {
        get => _storage[index];
        set => _storage[index] = value;
    }

    public double Entry(int row, int col) => _storage[(col * 3) + row];

    public double Trace() => _storage[0] + _storage[4] + _storage[8];
}

/// <summary>A unit quaternion, stored as <c>(x, y, z, w)</c>.</summary>
public sealed class Quaternion
{
    private readonly double[] _storage = new double[4];

    private Quaternion()
    {
    }

    public static Quaternion Identity()
    {
        var result = new Quaternion();
        result._storage[3] = 1.0;
        return result;
    }

    public double[] Storage => _storage;

    public double X => _storage[0];

    public double Y => _storage[1];

    public double Z => _storage[2];

    public double W => _storage[3];

    public double Length => Math.Sqrt(
        (_storage[0] * _storage[0]) + (_storage[1] * _storage[1])
        + (_storage[2] * _storage[2]) + (_storage[3] * _storage[3]));

    /// <remarks>`vector_math`'s <c>Quaternion.setFromRotation</c>.</remarks>
    public void SetFromRotation(Matrix3 rotation)
    {
        double trace = rotation.Trace();
        if (trace > 0.0)
        {
            double s = Math.Sqrt(trace + 1.0);
            _storage[3] = s * 0.5;
            s = 0.5 / s;
            _storage[0] = (rotation[5] - rotation[7]) * s;
            _storage[1] = (rotation[6] - rotation[2]) * s;
            _storage[2] = (rotation[1] - rotation[3]) * s;
            return;
        }

        int i = rotation[0] < rotation[4]
            ? (rotation[4] < rotation[8] ? 2 : 1)
            : (rotation[0] < rotation[8] ? 2 : 0);
        int j = (i + 1) % 3;
        int k = (i + 2) % 3;
        double side = Math.Sqrt(rotation.Entry(i, i) - rotation.Entry(j, j) - rotation.Entry(k, k) + 1.0);
        _storage[i] = side * 0.5;
        side = 0.5 / side;
        _storage[3] = (rotation.Entry(k, j) - rotation.Entry(j, k)) * side;
        _storage[j] = (rotation.Entry(j, i) + rotation.Entry(i, j)) * side;
        _storage[k] = (rotation.Entry(k, i) + rotation.Entry(i, k)) * side;
    }

    public Quaternion Scaled(double scale)
    {
        var result = new Quaternion();
        for (int index = 0; index < 4; index++)
        {
            result._storage[index] = _storage[index] * scale;
        }

        return result;
    }

    public static Quaternion operator +(Quaternion left, Quaternion right)
    {
        var result = new Quaternion();
        for (int index = 0; index < 4; index++)
        {
            result._storage[index] = left._storage[index] + right._storage[index];
        }

        return result;
    }

    public Quaternion Normalized()
    {
        var result = new Quaternion();
        Array.Copy(_storage, result._storage, 4);
        double length = result.Length;
        if (length == 0.0)
        {
            return result;
        }

        double inverse = 1.0 / length;
        for (int index = 0; index < 4; index++)
        {
            result._storage[index] *= inverse;
        }

        return result;
    }
}

/// <summary>A 4x4 double matrix, stored column-major exactly as `vector_math`'s `Matrix4`.</summary>
/// <remarks>
/// Mutable by design: Flutter's whole rendering layer passes one matrix down
/// <c>RenderObject.ApplyPaintTransform</c> and lets each render object post-multiply its own step
/// into it. A value type would break that contract.
/// </remarks>
public sealed class Matrix4
{
    private readonly double[] _storage;

    private Matrix4(double[] storage)
    {
        _storage = storage;
    }

    public Matrix4(
        double arg0,
        double arg1,
        double arg2,
        double arg3,
        double arg4,
        double arg5,
        double arg6,
        double arg7,
        double arg8,
        double arg9,
        double arg10,
        double arg11,
        double arg12,
        double arg13,
        double arg14,
        double arg15) : this(new double[16])
    {
        SetValues(
            arg0, arg1, arg2, arg3,
            arg4, arg5, arg6, arg7,
            arg8, arg9, arg10, arg11,
            arg12, arg13, arg14, arg15);
    }

    /// <summary>The all-zero matrix.</summary>
    public static Matrix4 Zero() => new(new double[16]);

    /// <summary>The identity matrix.</summary>
    public static Matrix4 Identity()
    {
        var result = new Matrix4(new double[16]);
        result._storage[0] = 1.0;
        result._storage[5] = 1.0;
        result._storage[10] = 1.0;
        result._storage[15] = 1.0;
        return result;
    }

    public static Matrix4 FromList(IReadOnlyList<double> values)
    {
        var result = new Matrix4(new double[16]);
        for (int index = 0; index < 16; index++)
        {
            result._storage[index] = values[index];
        }

        return result;
    }

    public static Matrix4 Copy(Matrix4 other)
    {
        var result = new Matrix4(new double[16]);
        result.SetFrom(other);
        return result;
    }

    /// <summary>The inverse of <paramref name="other"/>.</summary>
    /// <exception cref="ArgumentException">The matrix is not invertible.</exception>
    public static Matrix4 Inverted(Matrix4 other)
    {
        var result = new Matrix4(new double[16]);
        double determinant = result.CopyInverse(other);
        if (determinant == 0.0)
        {
            throw new ArgumentException("Matrix cannot be inverted", nameof(other));
        }

        return result;
    }

    /// <summary>The inverse of <paramref name="other"/>, or <c>null</c> when it is singular.</summary>
    public static Matrix4? TryInvert(Matrix4 other)
    {
        var result = new Matrix4(new double[16]);
        double determinant = result.CopyInverse(other);
        return determinant == 0.0 ? null : result;
    }

    public static Matrix4 RotationX(double radians)
    {
        var result = new Matrix4(new double[16]);
        result._storage[15] = 1.0;
        result.SetRotationX(radians);
        return result;
    }

    public static Matrix4 RotationY(double radians)
    {
        var result = new Matrix4(new double[16]);
        result._storage[15] = 1.0;
        result.SetRotationY(radians);
        return result;
    }

    public static Matrix4 RotationZ(double radians)
    {
        var result = new Matrix4(new double[16]);
        result._storage[15] = 1.0;
        result.SetRotationZ(radians);
        return result;
    }

    public static Matrix4 Translation(Vector3 translation) =>
        TranslationValues(translation.X, translation.Y, translation.Z);

    public static Matrix4 TranslationValues(double x, double y, double z)
    {
        Matrix4 result = Identity();
        result.SetTranslationRaw(x, y, z);
        return result;
    }

    public static Matrix4 Diagonal3(Vector3 scale) => Diagonal3Values(scale.X, scale.Y, scale.Z);

    public static Matrix4 Diagonal3Values(double x, double y, double z)
    {
        var result = new Matrix4(new double[16]);
        result._storage[15] = 1.0;
        result._storage[10] = z;
        result._storage[5] = y;
        result._storage[0] = x;
        return result;
    }

    public static Matrix4 SkewX(double alpha)
    {
        Matrix4 result = Identity();
        result._storage[4] = Math.Tan(alpha);
        return result;
    }

    public static Matrix4 SkewY(double beta)
    {
        Matrix4 result = Identity();
        result._storage[1] = Math.Tan(beta);
        return result;
    }

    public static Matrix4 Skew(double alpha, double beta)
    {
        Matrix4 result = Identity();
        result._storage[1] = Math.Tan(beta);
        result._storage[4] = Math.Tan(alpha);
        return result;
    }

    /// <summary>The live 16-element column-major backing store.</summary>
    public double[] Storage => _storage;

    public int Dimension => 4;

    public double this[int index]
    {
        get => _storage[index];
        set => _storage[index] = value;
    }

    public double Entry(int row, int col) => _storage[(col * 4) + row];

    public void SetEntry(int row, int col, double value) => _storage[(col * 4) + row] = value;

    public void SetValues(
        double arg0,
        double arg1,
        double arg2,
        double arg3,
        double arg4,
        double arg5,
        double arg6,
        double arg7,
        double arg8,
        double arg9,
        double arg10,
        double arg11,
        double arg12,
        double arg13,
        double arg14,
        double arg15)
    {
        _storage[0] = arg0;
        _storage[1] = arg1;
        _storage[2] = arg2;
        _storage[3] = arg3;
        _storage[4] = arg4;
        _storage[5] = arg5;
        _storage[6] = arg6;
        _storage[7] = arg7;
        _storage[8] = arg8;
        _storage[9] = arg9;
        _storage[10] = arg10;
        _storage[11] = arg11;
        _storage[12] = arg12;
        _storage[13] = arg13;
        _storage[14] = arg14;
        _storage[15] = arg15;
    }

    public void SetFrom(Matrix4 other)
    {
        for (int index = 15; index >= 0; index--)
        {
            _storage[index] = other._storage[index];
        }
    }

    public void SetIdentity()
    {
        Array.Clear(_storage);
        _storage[0] = 1.0;
        _storage[5] = 1.0;
        _storage[10] = 1.0;
        _storage[15] = 1.0;
    }

    public void SetZero() => Array.Clear(_storage);

    public Vector4 GetColumn(int column)
    {
        int entry = column * 4;
        return new Vector4(_storage[entry], _storage[entry + 1], _storage[entry + 2], _storage[entry + 3]);
    }

    public void SetColumn(int column, Vector4 value)
    {
        int entry = column * 4;
        _storage[entry + 3] = value[3];
        _storage[entry + 2] = value[2];
        _storage[entry + 1] = value[1];
        _storage[entry] = value[0];
    }

    public Vector4 GetRow(int row) =>
        new(_storage[row], _storage[4 + row], _storage[8 + row], _storage[12 + row]);

    public void SetRow(int row, Vector4 value)
    {
        _storage[row] = value[0];
        _storage[4 + row] = value[1];
        _storage[8 + row] = value[2];
        _storage[12 + row] = value[3];
    }

    public void SetTranslationRaw(double x, double y, double z)
    {
        _storage[14] = z;
        _storage[13] = y;
        _storage[12] = x;
    }

    public void SetTranslation(Vector3 translation) =>
        SetTranslationRaw(translation.X, translation.Y, translation.Z);

    public Vector3 GetTranslation() => new(_storage[12], _storage[13], _storage[14]);

    public void SetRotationX(double radians)
    {
        double cosine = Math.Cos(radians);
        double sine = Math.Sin(radians);
        _storage[0] = 1.0;
        _storage[1] = 0.0;
        _storage[2] = 0.0;
        _storage[4] = 0.0;
        _storage[5] = cosine;
        _storage[6] = sine;
        _storage[8] = 0.0;
        _storage[9] = -sine;
        _storage[10] = cosine;
        _storage[3] = 0.0;
        _storage[7] = 0.0;
        _storage[11] = 0.0;
    }

    public void SetRotationY(double radians)
    {
        double cosine = Math.Cos(radians);
        double sine = Math.Sin(radians);
        _storage[0] = cosine;
        _storage[1] = 0.0;
        _storage[2] = -sine;
        _storage[4] = 0.0;
        _storage[5] = 1.0;
        _storage[6] = 0.0;
        _storage[8] = sine;
        _storage[9] = 0.0;
        _storage[10] = cosine;
        _storage[3] = 0.0;
        _storage[7] = 0.0;
        _storage[11] = 0.0;
    }

    public void SetRotationZ(double radians)
    {
        double cosine = Math.Cos(radians);
        double sine = Math.Sin(radians);
        _storage[0] = cosine;
        _storage[1] = sine;
        _storage[2] = 0.0;
        _storage[4] = -sine;
        _storage[5] = cosine;
        _storage[6] = 0.0;
        _storage[8] = 0.0;
        _storage[9] = 0.0;
        _storage[10] = 1.0;
        _storage[3] = 0.0;
        _storage[7] = 0.0;
        _storage[11] = 0.0;
    }

    /// <summary>
    /// Post-multiplies this matrix by <paramref name="arg"/> in place (<c>this = this * arg</c>).
    /// </summary>
    public void Multiply(Matrix4 arg)
    {
        double m00 = _storage[0];
        double m01 = _storage[4];
        double m02 = _storage[8];
        double m03 = _storage[12];
        double m10 = _storage[1];
        double m11 = _storage[5];
        double m12 = _storage[9];
        double m13 = _storage[13];
        double m20 = _storage[2];
        double m21 = _storage[6];
        double m22 = _storage[10];
        double m23 = _storage[14];
        double m30 = _storage[3];
        double m31 = _storage[7];
        double m32 = _storage[11];
        double m33 = _storage[15];
        double[] arg2 = arg._storage;
        double n00 = arg2[0];
        double n01 = arg2[4];
        double n02 = arg2[8];
        double n03 = arg2[12];
        double n10 = arg2[1];
        double n11 = arg2[5];
        double n12 = arg2[9];
        double n13 = arg2[13];
        double n20 = arg2[2];
        double n21 = arg2[6];
        double n22 = arg2[10];
        double n23 = arg2[14];
        double n30 = arg2[3];
        double n31 = arg2[7];
        double n32 = arg2[11];
        double n33 = arg2[15];
        _storage[0] = (m00 * n00) + (m01 * n10) + (m02 * n20) + (m03 * n30);
        _storage[4] = (m00 * n01) + (m01 * n11) + (m02 * n21) + (m03 * n31);
        _storage[8] = (m00 * n02) + (m01 * n12) + (m02 * n22) + (m03 * n32);
        _storage[12] = (m00 * n03) + (m01 * n13) + (m02 * n23) + (m03 * n33);
        _storage[1] = (m10 * n00) + (m11 * n10) + (m12 * n20) + (m13 * n30);
        _storage[5] = (m10 * n01) + (m11 * n11) + (m12 * n21) + (m13 * n31);
        _storage[9] = (m10 * n02) + (m11 * n12) + (m12 * n22) + (m13 * n32);
        _storage[13] = (m10 * n03) + (m11 * n13) + (m12 * n23) + (m13 * n33);
        _storage[2] = (m20 * n00) + (m21 * n10) + (m22 * n20) + (m23 * n30);
        _storage[6] = (m20 * n01) + (m21 * n11) + (m22 * n21) + (m23 * n31);
        _storage[10] = (m20 * n02) + (m21 * n12) + (m22 * n22) + (m23 * n32);
        _storage[14] = (m20 * n03) + (m21 * n13) + (m22 * n23) + (m23 * n33);
        _storage[3] = (m30 * n00) + (m31 * n10) + (m32 * n20) + (m33 * n30);
        _storage[7] = (m30 * n01) + (m31 * n11) + (m32 * n21) + (m33 * n31);
        _storage[11] = (m30 * n02) + (m31 * n12) + (m32 * n22) + (m33 * n32);
        _storage[15] = (m30 * n03) + (m31 * n13) + (m32 * n23) + (m33 * n33);
    }

    public Matrix4 Multiplied(Matrix4 arg)
    {
        Matrix4 result = Clone();
        result.Multiply(arg);
        return result;
    }

    /// <summary>Pre-multiplies this matrix by <paramref name="arg"/> in place (<c>this = arg * this</c>).</summary>
    public void LeftMultiply(Matrix4 arg)
    {
        Matrix4 result = Copy(arg);
        result.Multiply(this);
        SetFrom(result);
    }

    /// <summary>Post-multiplies by the translation <c>T(tx, ty, tz; tw)</c>.</summary>
    public void TranslateByDouble(double tx, double ty, double tz, double tw)
    {
        double t1 = (_storage[0] * tx) + (_storage[4] * ty) + (_storage[8] * tz) + (_storage[12] * tw);
        double t2 = (_storage[1] * tx) + (_storage[5] * ty) + (_storage[9] * tz) + (_storage[13] * tw);
        double t3 = (_storage[2] * tx) + (_storage[6] * ty) + (_storage[10] * tz) + (_storage[14] * tw);
        double t4 = (_storage[3] * tx) + (_storage[7] * ty) + (_storage[11] * tz) + (_storage[15] * tw);
        _storage[12] = t1;
        _storage[13] = t2;
        _storage[14] = t3;
        _storage[15] = t4;
    }

    public void TranslateByVector3(Vector3 translation) =>
        TranslateByDouble(translation.X, translation.Y, translation.Z, 1.0);

    /// <summary>Pre-multiplies by the translation <c>T(tx, ty, tz; tw)</c>.</summary>
    public void LeftTranslateByDouble(double tx, double ty, double tz, double tw)
    {
        double r1 = _storage[3];
        _storage[0] += tx * r1;
        _storage[1] += ty * r1;
        _storage[2] += tz * r1;
        _storage[3] = tw * r1;
        double r2 = _storage[7];
        _storage[4] += tx * r2;
        _storage[5] += ty * r2;
        _storage[6] += tz * r2;
        _storage[7] = tw * r2;
        double r3 = _storage[11];
        _storage[8] += tx * r3;
        _storage[9] += ty * r3;
        _storage[10] += tz * r3;
        _storage[11] = tw * r3;
        double r4 = _storage[15];
        _storage[12] += tx * r4;
        _storage[13] += ty * r4;
        _storage[14] += tz * r4;
        _storage[15] = tw * r4;
    }

    /// <summary>
    /// Post-multiplies by a rotation of <paramref name="angle"/> radians about <paramref name="axis"/>.
    /// </summary>
    public void Rotate(Vector3 axis, double angle)
    {
        double length = axis.Length;
        double x = axis[0] / length;
        double y = axis[1] / length;
        double z = axis[2] / length;
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        double complement = 1.0 - cosine;
        double m11 = (x * x * complement) + cosine;
        double m12 = (x * y * complement) - (z * sine);
        double m13 = (x * z * complement) + (y * sine);
        double m21 = (y * x * complement) + (z * sine);
        double m22 = (y * y * complement) + cosine;
        double m23 = (y * z * complement) - (x * sine);
        double m31 = (z * x * complement) - (y * sine);
        double m32 = (z * y * complement) + (x * sine);
        double m33 = (z * z * complement) + cosine;
        double t1 = (_storage[0] * m11) + (_storage[4] * m21) + (_storage[8] * m31);
        double t2 = (_storage[1] * m11) + (_storage[5] * m21) + (_storage[9] * m31);
        double t3 = (_storage[2] * m11) + (_storage[6] * m21) + (_storage[10] * m31);
        double t4 = (_storage[3] * m11) + (_storage[7] * m21) + (_storage[11] * m31);
        double t5 = (_storage[0] * m12) + (_storage[4] * m22) + (_storage[8] * m32);
        double t6 = (_storage[1] * m12) + (_storage[5] * m22) + (_storage[9] * m32);
        double t7 = (_storage[2] * m12) + (_storage[6] * m22) + (_storage[10] * m32);
        double t8 = (_storage[3] * m12) + (_storage[7] * m22) + (_storage[11] * m32);
        double t9 = (_storage[0] * m13) + (_storage[4] * m23) + (_storage[8] * m33);
        double t10 = (_storage[1] * m13) + (_storage[5] * m23) + (_storage[9] * m33);
        double t11 = (_storage[2] * m13) + (_storage[6] * m23) + (_storage[10] * m33);
        double t12 = (_storage[3] * m13) + (_storage[7] * m23) + (_storage[11] * m33);
        _storage[0] = t1;
        _storage[1] = t2;
        _storage[2] = t3;
        _storage[3] = t4;
        _storage[4] = t5;
        _storage[5] = t6;
        _storage[6] = t7;
        _storage[7] = t8;
        _storage[8] = t9;
        _storage[9] = t10;
        _storage[10] = t11;
        _storage[11] = t12;
    }

    public void RotateX(double angle)
    {
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        double t1 = (_storage[4] * cosine) + (_storage[8] * sine);
        double t2 = (_storage[5] * cosine) + (_storage[9] * sine);
        double t3 = (_storage[6] * cosine) + (_storage[10] * sine);
        double t4 = (_storage[7] * cosine) + (_storage[11] * sine);
        double t5 = (_storage[4] * -sine) + (_storage[8] * cosine);
        double t6 = (_storage[5] * -sine) + (_storage[9] * cosine);
        double t7 = (_storage[6] * -sine) + (_storage[10] * cosine);
        double t8 = (_storage[7] * -sine) + (_storage[11] * cosine);
        _storage[4] = t1;
        _storage[5] = t2;
        _storage[6] = t3;
        _storage[7] = t4;
        _storage[8] = t5;
        _storage[9] = t6;
        _storage[10] = t7;
        _storage[11] = t8;
    }

    public void RotateY(double angle)
    {
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        double t1 = (_storage[0] * cosine) + (_storage[8] * -sine);
        double t2 = (_storage[1] * cosine) + (_storage[9] * -sine);
        double t3 = (_storage[2] * cosine) + (_storage[10] * -sine);
        double t4 = (_storage[3] * cosine) + (_storage[11] * -sine);
        double t5 = (_storage[0] * sine) + (_storage[8] * cosine);
        double t6 = (_storage[1] * sine) + (_storage[9] * cosine);
        double t7 = (_storage[2] * sine) + (_storage[10] * cosine);
        double t8 = (_storage[3] * sine) + (_storage[11] * cosine);
        _storage[0] = t1;
        _storage[1] = t2;
        _storage[2] = t3;
        _storage[3] = t4;
        _storage[8] = t5;
        _storage[9] = t6;
        _storage[10] = t7;
        _storage[11] = t8;
    }

    public void RotateZ(double angle)
    {
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        double t1 = (_storage[0] * cosine) + (_storage[4] * sine);
        double t2 = (_storage[1] * cosine) + (_storage[5] * sine);
        double t3 = (_storage[2] * cosine) + (_storage[6] * sine);
        double t4 = (_storage[3] * cosine) + (_storage[7] * sine);
        double t5 = (_storage[0] * -sine) + (_storage[4] * cosine);
        double t6 = (_storage[1] * -sine) + (_storage[5] * cosine);
        double t7 = (_storage[2] * -sine) + (_storage[6] * cosine);
        double t8 = (_storage[3] * -sine) + (_storage[7] * cosine);
        _storage[0] = t1;
        _storage[1] = t2;
        _storage[2] = t3;
        _storage[3] = t4;
        _storage[4] = t5;
        _storage[5] = t6;
        _storage[6] = t7;
        _storage[7] = t8;
    }

    /// <summary>Post-multiplies by <c>diag(sx, sy, sz, sw)</c>.</summary>
    public void ScaleByDouble(double sx, double sy, double sz, double sw)
    {
        _storage[0] *= sx;
        _storage[1] *= sx;
        _storage[2] *= sx;
        _storage[3] *= sx;
        _storage[4] *= sy;
        _storage[5] *= sy;
        _storage[6] *= sy;
        _storage[7] *= sy;
        _storage[8] *= sz;
        _storage[9] *= sz;
        _storage[10] *= sz;
        _storage[11] *= sz;
        _storage[12] *= sw;
        _storage[13] *= sw;
        _storage[14] *= sw;
        _storage[15] *= sw;
    }

    public void ScaleByVector3(Vector3 scale) => ScaleByDouble(scale.X, scale.Y, scale.Z, 1.0);

    public Matrix4 ScaledByDouble(double sx, double sy, double sz, double sw)
    {
        Matrix4 result = Clone();
        result.ScaleByDouble(sx, sy, sz, sw);
        return result;
    }

    public double Determinant()
    {
        double det2Zero1Zero1 = (_storage[0] * _storage[5]) - (_storage[1] * _storage[4]);
        double det2Zero1Zero2 = (_storage[0] * _storage[6]) - (_storage[2] * _storage[4]);
        double det2Zero1Zero3 = (_storage[0] * _storage[7]) - (_storage[3] * _storage[4]);
        double det2Zero1One2 = (_storage[1] * _storage[6]) - (_storage[2] * _storage[5]);
        double det2Zero1One3 = (_storage[1] * _storage[7]) - (_storage[3] * _storage[5]);
        double det2Zero1Two3 = (_storage[2] * _storage[7]) - (_storage[3] * _storage[6]);
        double det3Zero12 =
            (_storage[8] * det2Zero1One2) - (_storage[9] * det2Zero1Zero2) + (_storage[10] * det2Zero1Zero1);
        double det3Zero13 =
            (_storage[8] * det2Zero1One3) - (_storage[9] * det2Zero1Zero3) + (_storage[11] * det2Zero1Zero1);
        double det3Zero23 =
            (_storage[8] * det2Zero1Two3) - (_storage[10] * det2Zero1Zero3) + (_storage[11] * det2Zero1Zero2);
        double det3One23 =
            (_storage[9] * det2Zero1Two3) - (_storage[10] * det2Zero1One3) + (_storage[11] * det2Zero1One2);
        return (-det3One23 * _storage[12])
               + (det3Zero23 * _storage[13])
               - (det3Zero13 * _storage[14])
               + (det3Zero12 * _storage[15]);
    }

    /// <summary>Inverts this matrix in place, returning its determinant (0 leaves it unchanged).</summary>
    public double Invert() => CopyInverse(this);

    /// <summary>
    /// Sets this matrix to the inverse of <paramref name="arg"/>, returning <c>arg</c>'s determinant.
    /// </summary>
    public double CopyInverse(Matrix4 arg)
    {
        double a00 = arg._storage[0];
        double a01 = arg._storage[1];
        double a02 = arg._storage[2];
        double a03 = arg._storage[3];
        double a10 = arg._storage[4];
        double a11 = arg._storage[5];
        double a12 = arg._storage[6];
        double a13 = arg._storage[7];
        double a20 = arg._storage[8];
        double a21 = arg._storage[9];
        double a22 = arg._storage[10];
        double a23 = arg._storage[11];
        double a30 = arg._storage[12];
        double a31 = arg._storage[13];
        double a32 = arg._storage[14];
        double a33 = arg._storage[15];
        double b00 = (a00 * a11) - (a01 * a10);
        double b01 = (a00 * a12) - (a02 * a10);
        double b02 = (a00 * a13) - (a03 * a10);
        double b03 = (a01 * a12) - (a02 * a11);
        double b04 = (a01 * a13) - (a03 * a11);
        double b05 = (a02 * a13) - (a03 * a12);
        double b06 = (a20 * a31) - (a21 * a30);
        double b07 = (a20 * a32) - (a22 * a30);
        double b08 = (a20 * a33) - (a23 * a30);
        double b09 = (a21 * a32) - (a22 * a31);
        double b10 = (a21 * a33) - (a23 * a31);
        double b11 = (a22 * a33) - (a23 * a32);
        double det = (b00 * b11) - (b01 * b10) + (b02 * b09) + (b03 * b08) - (b04 * b07) + (b05 * b06);
        if (det == 0.0)
        {
            SetFrom(arg);
            return 0.0;
        }

        double invDet = 1.0 / det;
        _storage[0] = ((a11 * b11) - (a12 * b10) + (a13 * b09)) * invDet;
        _storage[1] = ((-a01 * b11) + (a02 * b10) - (a03 * b09)) * invDet;
        _storage[2] = ((a31 * b05) - (a32 * b04) + (a33 * b03)) * invDet;
        _storage[3] = ((-a21 * b05) + (a22 * b04) - (a23 * b03)) * invDet;
        _storage[4] = ((-a10 * b11) + (a12 * b08) - (a13 * b07)) * invDet;
        _storage[5] = ((a00 * b11) - (a02 * b08) + (a03 * b07)) * invDet;
        _storage[6] = ((-a30 * b05) + (a32 * b02) - (a33 * b01)) * invDet;
        _storage[7] = ((a20 * b05) - (a22 * b02) + (a23 * b01)) * invDet;
        _storage[8] = ((a10 * b10) - (a11 * b08) + (a13 * b06)) * invDet;
        _storage[9] = ((-a00 * b10) + (a01 * b08) - (a03 * b06)) * invDet;
        _storage[10] = ((a30 * b04) - (a31 * b02) + (a33 * b00)) * invDet;
        _storage[11] = ((-a20 * b04) + (a21 * b02) - (a23 * b00)) * invDet;
        _storage[12] = ((-a10 * b09) + (a11 * b07) - (a12 * b06)) * invDet;
        _storage[13] = ((a00 * b09) - (a01 * b07) + (a02 * b06)) * invDet;
        _storage[14] = ((-a30 * b03) + (a31 * b01) - (a32 * b00)) * invDet;
        _storage[15] = ((a20 * b03) - (a21 * b01) + (a22 * b00)) * invDet;
        return det;
    }

    public void Transpose()
    {
        (_storage[4], _storage[1]) = (_storage[1], _storage[4]);
        (_storage[8], _storage[2]) = (_storage[2], _storage[8]);
        (_storage[12], _storage[3]) = (_storage[3], _storage[12]);
        (_storage[9], _storage[6]) = (_storage[6], _storage[9]);
        (_storage[13], _storage[7]) = (_storage[7], _storage[13]);
        (_storage[14], _storage[11]) = (_storage[11], _storage[14]);
    }

    public Matrix4 Transposed()
    {
        Matrix4 result = Clone();
        result.Transpose();
        return result;
    }

    public bool IsIdentity() =>
        _storage[0] == 1.0 && _storage[1] == 0.0 && _storage[2] == 0.0 && _storage[3] == 0.0
        && _storage[4] == 0.0 && _storage[5] == 1.0 && _storage[6] == 0.0 && _storage[7] == 0.0
        && _storage[8] == 0.0 && _storage[9] == 0.0 && _storage[10] == 1.0 && _storage[11] == 0.0
        && _storage[12] == 0.0 && _storage[13] == 0.0 && _storage[14] == 0.0 && _storage[15] == 1.0;

    public bool IsZero()
    {
        for (int index = 0; index < 16; index++)
        {
            if (_storage[index] != 0.0)
            {
                return false;
            }
        }

        return true;
    }

    public Matrix4 Clone() => Copy(this);

    public Matrix4 CopyInto(Matrix4 arg)
    {
        arg.SetFrom(this);
        return arg;
    }

    public double GetMaxScaleOnAxis()
    {
        double scaleXSquared = (_storage[0] * _storage[0])
                               + (_storage[1] * _storage[1])
                               + (_storage[2] * _storage[2]);
        double scaleYSquared = (_storage[4] * _storage[4])
                               + (_storage[5] * _storage[5])
                               + (_storage[6] * _storage[6]);
        double scaleZSquared = (_storage[8] * _storage[8])
                               + (_storage[9] * _storage[9])
                               + (_storage[10] * _storage[10]);
        return Math.Sqrt(Math.Max(scaleXSquared, Math.Max(scaleYSquared, scaleZSquared)));
    }

    /// <summary>
    /// Transforms <paramref name="arg"/> in place, assuming the perspective row is (0, 0, 0, 1).
    /// </summary>
    public Vector3 Transform3(Vector3 arg)
    {
        double a0 = arg[0];
        double a1 = arg[1];
        double a2 = arg[2];
        double x = (_storage[0] * a0) + (_storage[4] * a1) + (_storage[8] * a2) + _storage[12];
        double y = (_storage[1] * a0) + (_storage[5] * a1) + (_storage[9] * a2) + _storage[13];
        double z = (_storage[2] * a0) + (_storage[6] * a1) + (_storage[10] * a2) + _storage[14];
        arg[0] = x;
        arg[1] = y;
        arg[2] = z;
        return arg;
    }

    public Vector3 Transformed3(Vector3 arg) => Transform3(Vector3.Copy(arg));

    /// <summary>Transforms <paramref name="arg"/> in place through the full 4x4.</summary>
    public Vector4 Transform(Vector4 arg)
    {
        double a0 = arg[0];
        double a1 = arg[1];
        double a2 = arg[2];
        double a3 = arg[3];
        double x = (_storage[0] * a0) + (_storage[4] * a1) + (_storage[8] * a2) + (_storage[12] * a3);
        double y = (_storage[1] * a0) + (_storage[5] * a1) + (_storage[9] * a2) + (_storage[13] * a3);
        double z = (_storage[2] * a0) + (_storage[6] * a1) + (_storage[10] * a2) + (_storage[14] * a3);
        double w = (_storage[3] * a0) + (_storage[7] * a1) + (_storage[11] * a2) + (_storage[15] * a3);
        arg[0] = x;
        arg[1] = y;
        arg[2] = z;
        arg[3] = w;
        return arg;
    }

    /// <summary>
    /// Transforms <paramref name="arg"/> in place and divides through by the resulting w.
    /// </summary>
    public Vector3 PerspectiveTransform(Vector3 arg)
    {
        double a0 = arg[0];
        double a1 = arg[1];
        double a2 = arg[2];
        double x = (_storage[0] * a0) + (_storage[4] * a1) + (_storage[8] * a2) + _storage[12];
        double y = (_storage[1] * a0) + (_storage[5] * a1) + (_storage[9] * a2) + _storage[13];
        double z = (_storage[2] * a0) + (_storage[6] * a1) + (_storage[10] * a2) + _storage[14];
        double w = 1.0 / ((_storage[3] * a0) + (_storage[7] * a1) + (_storage[11] * a2) + _storage[15]);
        arg[0] = x * w;
        arg[1] = y * w;
        arg[2] = z * w;
        return arg;
    }

    /// <summary>Copies the upper 3x3 rotation part into <paramref name="rotation"/>.</summary>
    public void CopyRotation(Matrix3 rotation)
    {
        rotation[0] = _storage[0];
        rotation[1] = _storage[1];
        rotation[2] = _storage[2];
        rotation[3] = _storage[4];
        rotation[4] = _storage[5];
        rotation[5] = _storage[6];
        rotation[6] = _storage[8];
        rotation[7] = _storage[9];
        rotation[8] = _storage[10];
    }

    /// <summary>Splits this matrix into a translation, a rotation and a scale.</summary>
    /// <remarks>`vector_math`'s <c>Matrix4.decompose</c>.</remarks>
    public void Decompose(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        double scaleX = new Vector3(_storage[0], _storage[1], _storage[2]).Length;
        double scaleY = new Vector3(_storage[4], _storage[5], _storage[6]).Length;
        double scaleZ = new Vector3(_storage[8], _storage[9], _storage[10]).Length;
        if (Determinant() < 0)
        {
            scaleX = -scaleX;
        }

        translation.SetValues(_storage[12], _storage[13], _storage[14]);

        double inverseX = 1.0 / scaleX;
        double inverseY = 1.0 / scaleY;
        double inverseZ = 1.0 / scaleZ;
        Matrix4 normalized = Copy(this);
        normalized[0] *= inverseX;
        normalized[1] *= inverseX;
        normalized[2] *= inverseX;
        normalized[4] *= inverseY;
        normalized[5] *= inverseY;
        normalized[6] *= inverseY;
        normalized[8] *= inverseZ;
        normalized[9] *= inverseZ;
        normalized[10] *= inverseZ;

        Matrix3 rotationMatrix = Matrix3.Zero();
        normalized.CopyRotation(rotationMatrix);
        rotation.SetFromRotation(rotationMatrix);
        scale.SetValues(scaleX, scaleY, scaleZ);
    }

    /// <summary>Rebuilds a matrix from a translation, a rotation and a scale.</summary>
    /// <remarks>`vector_math`'s <c>Matrix4.compose</c>.</remarks>
    public static Matrix4 Compose(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        Matrix4 result = Zero();
        result.SetFromTranslationRotation(translation, rotation);
        result.ScaleByVector3(scale);
        return result;
    }

    /// <remarks>`vector_math`'s <c>Matrix4.setFromTranslationRotation</c>.</remarks>
    public void SetFromTranslationRotation(Vector3 translation, Quaternion rotation)
    {
        double x = rotation.X;
        double y = rotation.Y;
        double z = rotation.Z;
        double w = rotation.W;
        double x2 = x + x;
        double y2 = y + y;
        double z2 = z + z;
        double xx = x * x2;
        double xy = x * y2;
        double xz = x * z2;
        double yy = y * y2;
        double yz = y * z2;
        double zz = z * z2;
        double wx = w * x2;
        double wy = w * y2;
        double wz = w * z2;
        _storage[0] = 1.0 - (yy + zz);
        _storage[1] = xy + wz;
        _storage[2] = xz - wy;
        _storage[3] = 0.0;
        _storage[4] = xy - wz;
        _storage[5] = 1.0 - (xx + zz);
        _storage[6] = yz + wx;
        _storage[7] = 0.0;
        _storage[8] = xz + wy;
        _storage[9] = yz - wx;
        _storage[10] = 1.0 - (xx + yy);
        _storage[11] = 0.0;
        _storage[12] = translation.X;
        _storage[13] = translation.Y;
        _storage[14] = translation.Z;
        _storage[15] = 1.0;
    }

    /// <summary>
    /// This matrix as an Avalonia 3x3 projective matrix, dropping the z row and column.
    /// </summary>
    /// <remarks>
    /// Avalonia maps points as row vectors (`p * M`) while this matrix maps them as column vectors,
    /// so the 3x3 is the transpose of the (x, y, w) sub-matrix. Skia's `SkM44::asM33` drops the same
    /// row and column, which is why a perspective transform survives the conversion.
    /// </remarks>
    public Matrix ToAvaloniaMatrix() =>
        new(
            _storage[0], _storage[1], _storage[3],
            _storage[4], _storage[5], _storage[7],
            _storage[12], _storage[13], _storage[15]);

    /// <summary>The 4x4 matrix that agrees with the given Avalonia 3x3 on the z = 0 plane.</summary>
    public static Matrix4 FromAvaloniaMatrix(Matrix matrix) =>
        new(
            matrix.M11, matrix.M12, 0.0, matrix.M13,
            matrix.M21, matrix.M22, 0.0, matrix.M23,
            0.0, 0.0, 1.0, 0.0,
            matrix.M31, matrix.M32, 0.0, matrix.M33);

    public static bool operator ==(Matrix4? left, Matrix4? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        return left is not null && right is not null && left.Equals(right);
    }

    public static bool operator !=(Matrix4? left, Matrix4? right) => !(left == right);

    public override bool Equals(object? obj)
    {
        if (obj is not Matrix4 other)
        {
            return false;
        }

        for (int index = 0; index < 16; index++)
        {
            if (_storage[index] != other._storage[index])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (double value in _storage)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"[0] {GetRow(0)}\n[1] {GetRow(1)}\n[2] {GetRow(2)}\n[3] {GetRow(3)}\n";
}
