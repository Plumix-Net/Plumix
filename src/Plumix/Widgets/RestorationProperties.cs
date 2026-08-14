using System.Numerics;
using Plumix.Foundation;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/restoration_properties.dart

namespace Plumix.Widgets;

/// <summary>
/// A <see cref="RestorableProperty{T}"/> that makes the wrapped value accessible to the owning
/// <see cref="State"/> object via the <see cref="Value"/> getter and setter.
/// </summary>
public abstract class RestorableValue<T> : RestorableProperty<T>
{
    private T _value = default!;

    /// <summary>The current value stored in this property.</summary>
    public virtual T Value
    {
        get
        {
            AssertRegistered();
            return _value;
        }
        set
        {
            AssertRegistered();
            if (EqualityComparer<T>.Default.Equals(value, _value))
            {
                return;
            }

            T? oldValue = _value;
            _value = value;
            DidUpdateValue(oldValue);
        }
    }

    public override void InitWithValue(T value)
    {
        _value = value;
    }

    /// <summary>
    /// Called whenever a new value is assigned to <see cref="Value"/>. Implementations must call
    /// <see cref="ChangeNotifier.NotifyListeners"/> when the new value changes what
    /// <see cref="RestorableProperty.ToPrimitives"/> returns.
    /// </summary>
    protected abstract void DidUpdateValue(T? oldValue);
}

/// <summary>
/// Base class for restorable properties whose value is a nullable primitive.
/// </summary>
/// <remarks>
/// Dart declares this as the private <c>_RestorablePrimitiveValueN</c>. C# cannot expose a public
/// type deriving from an internal one, so the class is public with an internal constructor: the
/// hierarchy matches Dart and external code still cannot derive from it directly.
/// </remarks>
public abstract class RestorablePrimitiveValueN<T> : RestorableValue<T>
{
    private readonly T _defaultValue;

    internal RestorablePrimitiveValueN(T defaultValue)
    {
        RestorationSerialization.AssertSerializable(defaultValue);
        _defaultValue = defaultValue;
    }

    public override T CreateDefaultValue() => _defaultValue;

    protected override void DidUpdateValue(T? oldValue)
    {
        RestorationSerialization.AssertSerializable(Value);
        NotifyListeners();
    }

    public override T FromPrimitives(object? serialized) => (T)serialized!;

    public override object? ToPrimitives() => Value;
}

/// <summary>
/// Base class for restorable properties whose value is a non-nullable primitive.
/// </summary>
/// <remarks>Dart declares this as the private <c>_RestorablePrimitiveValue</c>.</remarks>
public abstract class RestorablePrimitiveValue<T> : RestorablePrimitiveValueN<T>
    where T : notnull
{
    internal RestorablePrimitiveValue(T defaultValue) : base(defaultValue)
    {
        RestorationSerialization.AssertSerializable(defaultValue);
    }

    public override T FromPrimitives(object? serialized)
    {
        if (serialized is null)
        {
            throw new InvalidOperationException(
                $"A non-nullable {GetType().Name} cannot be restored from null.");
        }

        return base.FromPrimitives(serialized);
    }

    public override object? ToPrimitives() => base.ToPrimitives()!;
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a number.</summary>
/// <remarks>
/// Dart constrains the type parameter to <c>num</c>, the shared supertype of <c>int</c> and
/// <c>double</c>. C# has no such supertype, so the constraint is <c>INumber&lt;T&gt;</c> and there is
/// no equivalent of Dart's <c>RestorableNum&lt;num&gt;</c>.
/// </remarks>
public class RestorableNum<T> : RestorablePrimitiveValue<T>
    where T : struct, INumber<T>
{
    public RestorableNum(T defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a double.</summary>
public class RestorableDouble : RestorableNum<double>
{
    public RestorableDouble(double defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore an int.</summary>
public class RestorableInt : RestorableNum<int>
{
    public RestorableInt(int defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a string.</summary>
public class RestorableString : RestorablePrimitiveValue<string>
{
    public RestorableString(string defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a bool.</summary>
public class RestorableBool : RestorablePrimitiveValue<bool>
{
    public RestorableBool(bool defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a nullable bool.</summary>
public class RestorableBoolN : RestorablePrimitiveValueN<bool?>
{
    public RestorableBoolN(bool? defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a nullable number.</summary>
public class RestorableNumN<T> : RestorablePrimitiveValueN<T?>
    where T : struct, INumber<T>
{
    public RestorableNumN(T? defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a nullable double.</summary>
public class RestorableDoubleN : RestorableNumN<double>
{
    public RestorableDoubleN(double? defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a nullable int.</summary>
public class RestorableIntN : RestorableNumN<int>
{
    public RestorableIntN(int? defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>A <see cref="RestorableProperty{T}"/> that knows how to store and restore a nullable string.</summary>
public class RestorableStringN : RestorablePrimitiveValueN<string?>
{
    public RestorableStringN(string? defaultValue) : base(defaultValue)
    {
    }
}

/// <summary>
/// A <see cref="RestorableProperty{T}"/> that knows how to store and restore a <see cref="DateTime"/>.
/// </summary>
public class RestorableDateTime : RestorableValue<DateTime>
{
    private readonly DateTime _defaultValue;

    public RestorableDateTime(DateTime defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public override DateTime CreateDefaultValue() => _defaultValue;

    protected override void DidUpdateValue(DateTime oldValue)
    {
        NotifyListeners();
    }

    public override DateTime FromPrimitives(object? data)
    {
        return RestorationSerialization.DateTimeFromMillisecondsSinceEpoch(Convert.ToInt64(data));
    }

    public override object? ToPrimitives() => RestorationSerialization.MillisecondsSinceEpoch(Value);
}

/// <summary>
/// A <see cref="RestorableProperty{T}"/> that knows how to store and restore a nullable
/// <see cref="DateTime"/>.
/// </summary>
public class RestorableDateTimeN : RestorableValue<DateTime?>
{
    private readonly DateTime? _defaultValue;

    public RestorableDateTimeN(DateTime? defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public override DateTime? CreateDefaultValue() => _defaultValue;

    protected override void DidUpdateValue(DateTime? oldValue)
    {
        NotifyListeners();
    }

    public override DateTime? FromPrimitives(object? data)
    {
        return data is null
            ? null
            : RestorationSerialization.DateTimeFromMillisecondsSinceEpoch(Convert.ToInt64(data));
    }

    public override object? ToPrimitives()
    {
        DateTime? value = Value;
        return value is null ? null : RestorationSerialization.MillisecondsSinceEpoch(value.Value);
    }
}

/// <summary>
/// A base class for creating a <see cref="RestorableProperty{T}"/> that stores and restores a
/// <see cref="IListenable"/>.
/// </summary>
public abstract class RestorableListenable<T> : RestorableProperty<T>
    where T : class, IListenable
{
    private readonly Action _notifyListeners;
    private T? _value;

    protected RestorableListenable()
    {
        _notifyListeners = NotifyListeners;
    }

    /// <summary>The <see cref="IListenable"/> currently wrapped by this property.</summary>
    public T Value
    {
        get
        {
            AssertRegistered();
            return _value!;
        }
    }

    /// <summary>The wrapped object, or null when the property has never been initialized.</summary>
    protected T? CurrentValue => _value;

    public override void InitWithValue(T value)
    {
        _value?.RemoveListener(_notifyListeners);
        _value = value;
        _value.AddListener(_notifyListeners);
    }

    public override void Dispose()
    {
        base.Dispose();
        _value?.RemoveListener(_notifyListeners);
    }
}

/// <summary>
/// A base class for creating a <see cref="RestorableProperty{T}"/> that stores and restores a
/// <see cref="ChangeNotifier"/>. The wrapped notifier is owned (and disposed) by this property.
/// </summary>
public abstract class RestorableChangeNotifier<T> : RestorableListenable<T>
    where T : ChangeNotifier
{
    public override void InitWithValue(T value)
    {
        DisposeOldValue();
        base.InitWithValue(value);
    }

    public override void Dispose()
    {
        DisposeOldValue();
        base.Dispose();
    }

    private void DisposeOldValue()
    {
        if (CurrentValue is { } oldValue)
        {
            // Scheduled instead of disposed directly to give other entities a chance to remove
            // their listeners first.
            Scheduler.ScheduleMicrotask(oldValue.Dispose);
        }
    }
}

/// <summary>
/// A <see cref="RestorableProperty{T}"/> that knows how to store and restore a
/// <see cref="TextEditingController"/>.
/// </summary>
public class RestorableTextEditingController : RestorableChangeNotifier<TextEditingController>
{
    private readonly TextEditingValue _initialValue;

    public RestorableTextEditingController(string? text = null)
        : this(new TextEditingValue(text ?? string.Empty))
    {
    }

    /// <summary>Dart's <c>RestorableTextEditingController.fromValue</c>.</summary>
    public RestorableTextEditingController(TextEditingValue value)
    {
        _initialValue = value;
    }

    public static RestorableTextEditingController FromValue(TextEditingValue value) => new(value);

    public override TextEditingController CreateDefaultValue()
    {
        return TextEditingController.FromValue(_initialValue);
    }

    public override TextEditingController FromPrimitives(object? data)
    {
        return new TextEditingController(text: (string)data!);
    }

    public override object? ToPrimitives() => Value.Text;
}

/// <summary>
/// A <see cref="RestorableProperty{T}"/> that knows how to store and restore a nullable
/// <see cref="Enum"/>.
/// </summary>
public class RestorableEnumN<T> : RestorableValue<T?>
    where T : struct, Enum
{
    private readonly T? _defaultValue;

    public RestorableEnumN(T? defaultValue, IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var allowed = new HashSet<T>(values);
        if (defaultValue is { } value && !allowed.Contains(value))
        {
            throw new ArgumentException(
                $"Default value {typeof(T).Name}.{defaultValue} not found in {typeof(T).Name} values: "
                + string.Join(", ", allowed),
                nameof(defaultValue));
        }

        _defaultValue = defaultValue;
        Values = allowed;
    }

    /// <summary>The set of values this property is allowed to hold.</summary>
    public HashSet<T> Values { get; set; }

    public override T? Value
    {
        get => base.Value;
        set
        {
            if (value is { } newValue && !Values.Contains(newValue))
            {
                throw new ArgumentException(
                    $"Attempted to set an unknown enum value \"{newValue}\" that is not null, or in the "
                    + $"valid set of enum values for the {typeof(T).Name} type: "
                    + string.Join(", ", Values),
                    nameof(value));
            }

            base.Value = value;
        }
    }

    public override T? CreateDefaultValue() => _defaultValue;

    protected override void DidUpdateValue(T? oldValue)
    {
        NotifyListeners();
    }

    public override T? FromPrimitives(object? data)
    {
        if (data is null)
        {
            return null;
        }

        if (data is string name)
        {
            foreach (T allowed in Values)
            {
                if (string.Equals(allowed.ToString(), name, StringComparison.Ordinal))
                {
                    return allowed;
                }
            }

            throw new ArgumentException(
                $"Attempted to set an unknown enum value \"{name}\" that is not null, or in the valid "
                + $"set of enum values for the {typeof(T).Name} type: "
                + string.Join(", ", Values),
                nameof(data));
        }

        return _defaultValue;
    }

    public override object? ToPrimitives() => Value?.ToString();
}

/// <summary>
/// A <see cref="RestorableProperty{T}"/> that knows how to store and restore an <see cref="Enum"/>.
/// </summary>
public class RestorableEnum<T> : RestorableValue<T>
    where T : struct, Enum
{
    private readonly T _defaultValue;

    public RestorableEnum(T defaultValue, IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var allowed = new HashSet<T>(values);
        if (!allowed.Contains(defaultValue))
        {
            throw new ArgumentException(
                $"Default value {typeof(T).Name}.{defaultValue} not found in {typeof(T).Name} values: "
                + string.Join(", ", allowed),
                nameof(defaultValue));
        }

        _defaultValue = defaultValue;
        Values = allowed;
    }

    /// <summary>The set of values this property is allowed to hold.</summary>
    public HashSet<T> Values { get; set; }

    public override T Value
    {
        get => base.Value;
        set
        {
            if (!Values.Contains(value))
            {
                throw new ArgumentException(
                    $"Attempted to set an unknown enum value \"{value}\" that is not in the valid set "
                    + $"of enum values for the {typeof(T).Name} type: "
                    + string.Join(", ", Values),
                    nameof(value));
            }

            base.Value = value;
        }
    }

    public override T CreateDefaultValue() => _defaultValue;

    protected override void DidUpdateValue(T oldValue)
    {
        NotifyListeners();
    }

    public override T FromPrimitives(object? data)
    {
        if (data is string name)
        {
            foreach (T allowed in Values)
            {
                if (string.Equals(allowed.ToString(), name, StringComparison.Ordinal))
                {
                    return allowed;
                }
            }

            throw new ArgumentException(
                $"Attempted to restore an unknown enum value \"{name}\" that is not in the valid set of "
                + $"enum values for the {typeof(T).Name} type: "
                + string.Join(", ", Values),
                nameof(data));
        }

        return _defaultValue;
    }

    public override object? ToPrimitives() => Value.ToString();
}
