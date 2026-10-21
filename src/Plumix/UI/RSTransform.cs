namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/geometry.dart (RSTransform)
/// <summary>A rotation, scale and translation stored at the engine's float32 precision.</summary>
public sealed class RSTransform
{
    private readonly float _scos;
    private readonly float _ssin;
    private readonly float _tx;
    private readonly float _ty;

    public RSTransform(double scos, double ssin, double tx, double ty)
    {
        _scos = (float)scos;
        _ssin = (float)ssin;
        _tx = (float)tx;
        _ty = (float)ty;
    }

    public static RSTransform FromComponents(
        double rotation, double scale, double anchorX, double anchorY, double translateX, double translateY)
    {
        double scos = Math.Cos(rotation) * scale;
        double ssin = Math.Sin(rotation) * scale;
        return new RSTransform(scos, ssin,
            translateX + -scos * anchorX + ssin * anchorY,
            translateY + -ssin * anchorX - scos * anchorY);
    }

    public double Scos => _scos;
    public double Ssin => _ssin;
    public double Tx => _tx;
    public double Ty => _ty;
}
