namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/tweens.dart
// `FractionalOffsetTween` is not ported: Plumix has no `FractionalOffset` type.

/// <summary>An interpolation between two alignments.</summary>
public class AlignmentTween : Tween<Alignment>
{
    /// <summary>Creates an alignment tween. A <c>null</c> end is unset, as in Dart.</summary>
    public AlignmentTween(Alignment? begin = null, Alignment? end = null)
    {
        Begin = begin;
        End = end;
    }

    /// <inheritdoc cref="Tween{T}.Begin" />
    public new Alignment? Begin
    {
        get => HasBeginValue ? GetBeginValue() : null;
        set
        {
            if (value is { } alignment)
            {
                SetBeginValue(alignment);
            }
            else
            {
                ClearBeginValue();
            }
        }
    }

    /// <inheritdoc cref="Tween{T}.End" />
    public new Alignment? End
    {
        get => HasEndValue ? GetEndValue() : null;
        set
        {
            if (value is { } alignment)
            {
                SetEndValue(alignment);
            }
            else
            {
                ClearEndValue();
            }
        }
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override Alignment Lerp(double t) => Alignment.Lerp(Begin, End, t)!.Value;
}

/// <summary>An interpolation between two <see cref="AlignmentGeometry"/>.</summary>
public class AlignmentGeometryTween : Tween<AlignmentGeometry?>
{
    /// <summary>Creates a fractional offset geometry tween.</summary>
    public AlignmentGeometryTween(AlignmentGeometry? begin = null, AlignmentGeometry? end = null)
        : base(begin, end)
    {
    }

    /// <summary>Returns the value this variable has at the given animation clock value.</summary>
    public override AlignmentGeometry? Lerp(double t) => AlignmentGeometry.Lerp(Begin, End, t);
}
