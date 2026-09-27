using Plumix;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/motion.dart

/// <summary>The set of durations in the Material specification.</summary>
public static class Durations
{
    public static readonly TimeSpan Short1 = TimeSpan.FromMilliseconds(50);
    public static readonly TimeSpan Short2 = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan Short3 = TimeSpan.FromMilliseconds(150);
    public static readonly TimeSpan Short4 = TimeSpan.FromMilliseconds(200);
    public static readonly TimeSpan Medium1 = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan Medium2 = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan Medium3 = TimeSpan.FromMilliseconds(350);
    public static readonly TimeSpan Medium4 = TimeSpan.FromMilliseconds(400);
    public static readonly TimeSpan Long1 = TimeSpan.FromMilliseconds(450);
    public static readonly TimeSpan Long2 = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan Long3 = TimeSpan.FromMilliseconds(550);
    public static readonly TimeSpan Long4 = TimeSpan.FromMilliseconds(600);
    public static readonly TimeSpan Extralong1 = TimeSpan.FromMilliseconds(700);
    public static readonly TimeSpan Extralong2 = TimeSpan.FromMilliseconds(800);
    public static readonly TimeSpan Extralong3 = TimeSpan.FromMilliseconds(900);
    public static readonly TimeSpan Extralong4 = TimeSpan.FromMilliseconds(1000);
}

/// <summary>The set of easing curves in the Material specification.</summary>
public static class Easing
{
    /// <summary>The emphasizedAccelerate easing curve in the Material specification.</summary>
    public static readonly Curve EmphasizedAccelerate = new Cubic(0.3, 0.0, 0.8, 0.15);

    /// <summary>The emphasizedDecelerate easing curve in the Material specification.</summary>
    public static readonly Curve EmphasizedDecelerate = new Cubic(0.05, 0.7, 0.1, 1.0);

    /// <summary>The linear easing curve in the Material specification.</summary>
    public static readonly Curve Linear = new Cubic(0.0, 0.0, 1.0, 1.0);

    /// <summary>The standard easing curve in the Material specification.</summary>
    public static readonly Curve Standard = new Cubic(0.2, 0.0, 0.0, 1.0);

    /// <summary>The standardAccelerate easing curve in the Material specification.</summary>
    public static readonly Curve StandardAccelerate = new Cubic(0.3, 0.0, 1.0, 1.0);

    /// <summary>The standardDecelerate easing curve in the Material specification.</summary>
    public static readonly Curve StandardDecelerate = new Cubic(0.0, 0.0, 0.0, 1.0);

    /// <summary>The legacyDecelerate easing curve in the Material specification.</summary>
    public static readonly Curve LegacyDecelerate = new Cubic(0.0, 0.0, 0.2, 1.0);

    /// <summary>The legacyAccelerate easing curve in the Material specification.</summary>
    public static readonly Curve LegacyAccelerate = new Cubic(0.4, 0.0, 1.0, 1.0);

    /// <summary>The legacy easing curve in the Material specification.</summary>
    public static readonly Curve Legacy = new Cubic(0.4, 0.0, 0.2, 1.0);
}
