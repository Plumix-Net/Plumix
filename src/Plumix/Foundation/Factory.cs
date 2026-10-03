// Dart parity source: flutter/packages/flutter/lib/src/foundation/basic_types.dart

namespace Plumix.Foundation;

/// <summary>A factory interface that also reports the type of the created objects.</summary>
/// <remarks>
/// The covariant face of <see cref="Factory{T}"/>. Dart's generics are covariant, so a
/// <c>Factory&lt;VerticalDragGestureRecognizer&gt;</c> is a <c>Factory&lt;OneSequenceGestureRecognizer&gt;</c>
/// and both can live in one <c>Set&lt;Factory&lt;OneSequenceGestureRecognizer&gt;&gt;</c>; C# classes are
/// invariant, so collections of factories are typed by this interface (see <c>docs/ai/DIVERGENCES.md</c>).
/// </remarks>
public interface IFactory<out T>
{
    /// <summary>Creates a new object of type <typeparamref name="T"/>.</summary>
    T Construct();

    /// <summary>The type of the objects created by this factory.</summary>
    Type Type { get; }
}

/// <summary>A factory interface that also reports the type of the created objects.</summary>
/// <remarks>Flutter's <c>Factory&lt;T&gt;</c>.</remarks>
public class Factory<T> : IFactory<T>
{
    /// <summary>Creates a new factory.</summary>
    public Factory(Func<T> constructor)
    {
        ArgumentNullException.ThrowIfNull(constructor);
        Constructor = constructor;
    }

    /// <summary>Creates a new object of type <typeparamref name="T"/>.</summary>
    public Func<T> Constructor { get; }

    /// <summary>The type of the objects created by this factory.</summary>
    public Type Type => typeof(T);

    /// <inheritdoc />
    public T Construct() => Constructor();

    /// <inheritdoc />
    public override string ToString() => $"Factory(type: {Type.Name})";
}
