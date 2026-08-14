using Plumix.Gestures;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/gesture_detector.dart

namespace Plumix.Widgets;

/// <summary>
/// A factory for gesture recognizers, used by <see cref="RawGestureDetector"/> to create and
/// configure the recognizers it owns without rebuilding them on every frame.
/// </summary>
public interface IGestureRecognizerFactory
{
    /// <summary>Creates a new instance of the recognizer this factory produces.</summary>
    GestureRecognizer ConstructorRaw();

    /// <summary>Configures an existing recognizer with the current widget's callbacks.</summary>
    void InitializerRaw(GestureRecognizer instance);

    /// <summary>Whether this factory produces recognizers of the given type.</summary>
    bool HandlesType(Type type);
}

/// <summary>A typed <see cref="IGestureRecognizerFactory"/>.</summary>
public abstract class GestureRecognizerFactory<TRecognizer> : IGestureRecognizerFactory
    where TRecognizer : GestureRecognizer
{
    /// <summary>Creates a new instance of the recognizer.</summary>
    public abstract TRecognizer Constructor();

    /// <summary>Configures the recognizer.</summary>
    public abstract void Initializer(TRecognizer instance);

    public GestureRecognizer ConstructorRaw() => Constructor();

    public void InitializerRaw(GestureRecognizer instance) => Initializer((TRecognizer)instance);

    public bool HandlesType(Type type) => type == typeof(TRecognizer);
}

/// <summary>
/// A <see cref="GestureRecognizerFactory{TRecognizer}"/> built from a constructor and an
/// initializer callback.
/// </summary>
public sealed class GestureRecognizerFactoryWithHandlers<TRecognizer> : GestureRecognizerFactory<TRecognizer>
    where TRecognizer : GestureRecognizer
{
    private readonly Func<TRecognizer> _constructor;
    private readonly Action<TRecognizer> _initializer;

    public GestureRecognizerFactoryWithHandlers(Func<TRecognizer> constructor, Action<TRecognizer> initializer)
    {
        _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
    }

    public override TRecognizer Constructor() => _constructor();

    public override void Initializer(TRecognizer instance) => _initializer(instance);
}
