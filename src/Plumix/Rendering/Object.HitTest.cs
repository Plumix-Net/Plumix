using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/hit_test.dart
// Dart parity source: flutter/packages/flutter/lib/src/rendering/box.dart (BoxHitTestResult, BoxHitTestEntry)

namespace Plumix.Rendering;

public enum HitTestBehavior
{
    DeferToChild,
    Opaque,
    Translucent
}

/// <summary>
/// Data collected during a hit test about a specific <see cref="IHitTestTarget"/>.
/// </summary>
public class HitTestEntry(IHitTestTarget target)
{
    public IHitTestTarget Target { get; } = target;

    /// <summary>
    /// Returns a matrix describing how <see cref="PointerEvent"/>s delivered to this entry should be
    /// transformed from the global coordinate space of the screen to the local coordinate space of
    /// <see cref="Target"/>. Dart's `HitTestEntry.transform`, filled in by <see cref="HitTestResult.Add"/>.
    /// </summary>
    public Matrix4? Transform { get; internal set; }

    public override string ToString() => $"{Diagnostics.DescribeIdentity(this)}({Target})";
}

/// <summary>A type of data that can be applied to a matrix by left-multiplication.</summary>
internal abstract class TransformPart
{
    /// <summary>Applies this transform part to <paramref name="rhs"/> from the left.</summary>
    public abstract Matrix4 Multiply(Matrix4 rhs);
}

internal sealed class MatrixTransformPart(Matrix4 matrix) : TransformPart
{
    public override Matrix4 Multiply(Matrix4 rhs) => matrix.Multiplied(rhs);
}

internal sealed class OffsetTransformPart(Point offset) : TransformPart
{
    public override Matrix4 Multiply(Matrix4 rhs)
    {
        Matrix4 clone = rhs.Clone();
        clone.LeftTranslateByDouble(offset.X, offset.Y, 0, 1);
        return clone;
    }
}

/// <summary>The result of performing a hit test.</summary>
public class HitTestResult
{
    private readonly List<HitTestEntry> _path;

    // The transform part stack leading from global to the current object is stored in two parts:
    // `_transforms` are globalized matrices (already multiplied by their ancestors, so they are
    // relative to the global coordinate space), while `_localTransforms` are parent-relative parts
    // that are globalized and moved over only when a transform is actually read.
    private readonly List<Matrix4> _transforms;
    private readonly List<TransformPart> _localTransforms;

    /// <summary>Creates an empty hit test result.</summary>
    public HitTestResult()
    {
        _path = [];
        _transforms = [Matrix4.Identity()];
        _localTransforms = [];
    }

    /// <summary>
    /// Wraps <paramref name="result"/> to create a result that shares its path and transform stack.
    /// Dart's `HitTestResult.wrap`.
    /// </summary>
    public HitTestResult(HitTestResult result)
    {
        _path = result._path;
        _transforms = result._transforms;
        _localTransforms = result._localTransforms;
    }

    /// <summary>
    /// The <see cref="HitTestEntry"/> objects recorded during the hit test, most specific first.
    /// </summary>
    public IReadOnlyList<HitTestEntry> Path => _path;

    /// <summary>
    /// Wraps <paramref name="result"/> so both share one path and transform stack.
    /// Dart's `HitTestResult.wrap` named constructor.
    /// </summary>
    public static HitTestResult Wrap(HitTestResult result) => new(result);

    /// <summary>Adds an entry to the path, stamping it with the transform currently on the stack.</summary>
    public void Add(HitTestEntry entry)
    {
        if (entry.Transform is not null)
        {
            throw new InvalidOperationException("A HitTestEntry can only be added to one HitTestResult.");
        }

        entry.Transform = LastTransform;
        _path.Add(entry);
    }

    /// <summary>
    /// Pushes a transform applied to every entry added until the matching <see cref="PopTransform"/>.
    /// The matrix maps events from the caller's coordinate space into its children's.
    /// </summary>
    protected void PushTransform(Matrix4 transform)
    {
        DebugAssertHasNoPerspective(transform);
        _localTransforms.Add(new MatrixTransformPart(transform));
    }

    /// <summary>
    /// Pushes a translation applied to every entry added until the matching <see cref="PopTransform"/>.
    /// Faster than <see cref="PushTransform"/> for the translation-only case.
    /// </summary>
    protected void PushOffset(Point offset)
    {
        _localTransforms.Add(new OffsetTransformPart(offset));
    }

    /// <summary>Removes the last transform pushed by <see cref="PushTransform"/> or <see cref="PushOffset"/>.</summary>
    protected void PopTransform()
    {
        if (_localTransforms.Count > 0)
        {
            _localTransforms.RemoveAt(_localTransforms.Count - 1);
        }
        else
        {
            _transforms.RemoveAt(_transforms.Count - 1);
        }

        if (_transforms.Count == 0)
        {
            throw new InvalidOperationException("HitTestResult.PopTransform popped the identity transform.");
        }
    }

    public override string ToString() =>
        $"HitTestResult({(_path.Count == 0 ? "<empty path>" : string.Join(", ", _path))})";

    private Matrix4 LastTransform
    {
        get
        {
            GlobalizeTransforms();
            return _transforms[^1];
        }
    }

    private void GlobalizeTransforms()
    {
        if (_localTransforms.Count == 0)
        {
            return;
        }

        Matrix4 last = _transforms[^1];
        foreach (TransformPart part in _localTransforms)
        {
            last = part.Multiply(last);
            _transforms.Add(last);
        }

        _localTransforms.Clear();
    }

    private static void DebugAssertHasNoPerspective(Matrix4 transform)
    {
#if DEBUG
        if (!VectorMoreOrLessEquals(transform.GetRow(2), new Vector4(0, 0, 1, 0))
            || !VectorMoreOrLessEquals(transform.GetColumn(2), new Vector4(0, 0, 1, 0)))
        {
            throw new InvalidOperationException(
                "The third row and third column of a transform matrix for pointer events must be "
                + "Vector4(0, 0, 1, 0) to ensure that a transformed point is directly under the "
                + "pointing device. Did you forget to run the paint matrix through "
                + $"PointerEventUtils.RemovePerspectiveTransform? The provided matrix is:\n{transform}");
        }
#endif
    }

    private static bool VectorMoreOrLessEquals(
        Vector4 a,
        Vector4 b,
        double epsilon = Constants.PrecisionErrorTolerance)
    {
        return Math.Abs(a.X - b.X) < epsilon
               && Math.Abs(a.Y - b.Y) < epsilon
               && Math.Abs(a.Z - b.Z) < epsilon
               && Math.Abs(a.W - b.W) < epsilon;
    }
}

/// <summary>Signature for the nested hit test that <c>BoxHitTestResult.AddWith*</c> runs.</summary>
public delegate bool BoxHitTest(BoxHitTestResult result, Point position);

/// <summary>
/// Signature for the nested hit test that <see cref="BoxHitTestResult.AddWithOutOfBandPosition"/> runs.
/// </summary>
public delegate bool BoxHitTestWithOutOfBandPosition(BoxHitTestResult result);

public class BoxHitTestResult : HitTestResult
{
    public BoxHitTestResult()
    {
    }

    /// <summary>Wraps <paramref name="result"/> so both share one path and transform stack.</summary>
    public BoxHitTestResult(HitTestResult result) : base(result)
    {
    }

    /// <summary>Wraps <paramref name="result"/> so both share one path and transform stack.</summary>
    public static new BoxHitTestResult Wrap(HitTestResult result) => new(result);

    /// <summary>
    /// Runs <paramref name="hitTest"/> with <paramref name="position"/> mapped through the inverse of
    /// a paint transform, returning <c>false</c> without testing when the transform is not invertible.
    /// </summary>
    public bool AddWithPaintTransform(Matrix4? transform, Point position, BoxHitTest hitTest)
    {
        if (transform is not null)
        {
            transform = Matrix4.TryInvert(PointerEventUtils.RemovePerspectiveTransform(transform));
            if (transform is null)
            {
                return false;
            }
        }

        return AddWithRawTransform(transform, position, hitTest);
    }

    /// <summary>Runs <paramref name="hitTest"/> with <paramref name="position"/> shifted by an offset.</summary>
    public bool AddWithPaintOffset(Point? offset, Point position, BoxHitTest hitTest)
    {
        Point transformedPosition = offset is { } value ? position - value : position;
        if (offset is { } pushed)
        {
            PushOffset(new Point(-pushed.X, -pushed.Y));
        }

        bool isHit = hitTest(this, transformedPosition);
        if (offset is not null)
        {
            PopTransform();
        }

        return isHit;
    }

    /// <summary>Runs <paramref name="hitTest"/> with <paramref name="position"/> mapped through a matrix.</summary>
    public bool AddWithRawTransform(Matrix4? transform, Point position, BoxHitTest hitTest)
    {
        Point transformedPosition = transform is null
            ? position
            : MatrixUtils.TransformPoint(transform, position);
        if (transform is not null)
        {
            PushTransform(transform);
        }

        bool isHit = hitTest(this, transformedPosition);
        if (transform is not null)
        {
            PopTransform();
        }

        return isHit;
    }

    /// <summary>
    /// Pushes exactly one of the three transform arguments and runs <paramref name="hitTest"/>, which
    /// is responsible for the position itself. Dart's `addWithOutOfBandPosition`.
    /// </summary>
    public bool AddWithOutOfBandPosition(
        BoxHitTestWithOutOfBandPosition hitTest,
        Point? paintOffset = null,
        Matrix4? paintTransform = null,
        Matrix4? rawTransform = null)
    {
        if (paintOffset is { } offset)
        {
            if (paintTransform is not null || rawTransform is not null)
            {
                throw new ArgumentException("Exactly one transform or offset argument must be provided.");
            }

            PushOffset(new Point(-offset.X, -offset.Y));
        }
        else if (rawTransform is not null)
        {
            if (paintTransform is not null)
            {
                throw new ArgumentException("Exactly one transform or offset argument must be provided.");
            }

            PushTransform(rawTransform);
        }
        else
        {
            if (paintTransform is null)
            {
                throw new ArgumentException("Exactly one transform or offset argument must be provided.");
            }

            Matrix4? inverted = Matrix4.TryInvert(PointerEventUtils.RemovePerspectiveTransform(paintTransform));
            if (inverted is null)
            {
                throw new ArgumentException("paintTransform must be invertible.", nameof(paintTransform));
            }

            PushTransform(inverted);
        }

        bool isHit = hitTest(this);
        PopTransform();
        return isHit;
    }
}

public sealed class BoxHitTestEntry(RenderBox target, Point localPosition) : HitTestEntry(target)
{
    /// <summary>The position of the hit test in the local coordinates of the target.</summary>
    public Point LocalPosition { get; } = localPosition;

    public override string ToString() => $"{Diagnostics.DescribeIdentity(Target)}@{LocalPosition}";
}
