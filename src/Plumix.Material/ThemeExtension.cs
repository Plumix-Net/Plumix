namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/theme_data.dart

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

/// Dart's `Adaptation<T>`: a per-platform override hook for a component theme, looked up by
/// <see cref="ThemeData.GetAdaptation{T}"/> and applied by the `.adaptive` constructors.
public abstract class Adaptation
{
    public abstract Type Type { get; }
}

public abstract class Adaptation<T> : Adaptation
{
    public sealed override Type Type => typeof(T);

    /// Returns the theme data to use on <paramref name="theme"/>'s platform. The default
    /// implementation returns <paramref name="defaultValue"/> unchanged.
    public abstract T Adapt(ThemeData theme, T defaultValue);
}
