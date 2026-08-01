namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/theme_data.dart

public abstract class ThemeExtension
{
    public abstract Type Type { get; }

    internal abstract ThemeExtension LerpUntyped(ThemeExtension? other, double t);
}

public abstract class ThemeExtension<T> : ThemeExtension where T : ThemeExtension<T>
{
    public sealed override Type Type => typeof(T);

    public abstract T Lerp(T? other, double t);

    internal sealed override ThemeExtension LerpUntyped(ThemeExtension? other, double t)
    {
        return Lerp(other as T, t);
    }
}
