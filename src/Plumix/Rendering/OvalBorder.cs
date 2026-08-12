namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/oval_border.dart

/// A border that fits an elliptical shape tightly into the available space.
public sealed record OvalBorder : CircleBorder
{
    public OvalBorder(BorderSide? side = null, double eccentricity = 1.0)
        : base(side, eccentricity)
    {
    }

    public override ShapeBorder Scale(double t)
    {
        return new OvalBorder(Side.Scale(t), Eccentricity);
    }

    public override CircleBorder CopyWith(BorderSide? side, double? eccentricity)
    {
        return new OvalBorder(side ?? Side, eccentricity ?? Eccentricity);
    }

    public override ShapeBorder? LerpFrom(ShapeBorder? a, double t)
    {
        if (a is OvalBorder oval)
        {
            return new OvalBorder(
                BorderSide.Lerp(oval.Side, Side, t),
                Math.Clamp(LerpDouble(oval.Eccentricity, Eccentricity, t), 0.0, 1.0));
        }

        return base.LerpFrom(a, t);
    }

    public override ShapeBorder? LerpTo(ShapeBorder? b, double t)
    {
        if (b is OvalBorder oval)
        {
            return new OvalBorder(
                BorderSide.Lerp(Side, oval.Side, t),
                Math.Clamp(LerpDouble(Eccentricity, oval.Eccentricity, t), 0.0, 1.0));
        }

        return base.LerpTo(b, t);
    }

    public override string ToString()
    {
        return Eccentricity != 1.0
            ? $"OvalBorder({Side}, eccentricity: {Eccentricity})"
            : $"OvalBorder({Side})";
    }
}
