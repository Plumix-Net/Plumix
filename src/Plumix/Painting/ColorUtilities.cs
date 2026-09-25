namespace Plumix.Painting;

// C#-only infrastructure: a nullable double lerp shared by the switch ports. The colour helpers
// that used to live here are members of the framework's `Plumix.UI.Color` now, as in dart:ui.

public static class ColorUtilities
{
    public static double? LerpDouble(double? from, double? to, double t)
    {
        if (!from.HasValue && !to.HasValue)
        {
            return null;
        }

        double start = from ?? 0.0;
        double end = to ?? 0.0;
        return start + ((end - start) * t);
    }
}
