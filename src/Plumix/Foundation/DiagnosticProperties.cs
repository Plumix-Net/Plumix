namespace Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/diagnostics.dart

public enum DiagnosticLevel
{
    Hidden,
    Fine,
    Debug,
    Info,
    Warning,
    Hint,
    Summary,
    Error,
    Off,
}

public abstract record DiagnosticsNode(
    string Name,
    DiagnosticLevel Level = DiagnosticLevel.Info)
{
    public bool IsFiltered(DiagnosticLevel minimumLevel) => Level < minimumLevel;

    public abstract string Description { get; }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? Description : $"{Name}: {Description}";
    }
}

public record DiagnosticsProperty<T> : DiagnosticsNode
{
    public DiagnosticsProperty(
        string name,
        T? value,
        T? defaultValue = default,
        Func<T?, string>? formatter = null,
        bool showName = true)
        : base(
            showName ? name : string.Empty,
            EqualityComparer<T?>.Default.Equals(value, defaultValue)
                ? DiagnosticLevel.Fine
                : DiagnosticLevel.Info)
    {
        Value = value;
        DefaultValue = defaultValue;
        Formatter = formatter;
    }

    public T? Value { get; }

    public T? DefaultValue { get; }

    public Func<T?, string>? Formatter { get; }

    public override string Description => Formatter?.Invoke(Value) ?? FormatValue(Value);

    private static string FormatValue(T? value)
    {
        return value switch
        {
            null => "null",
            bool flag => flag ? "true" : "false",
            TimeSpan duration => $"{duration.TotalMilliseconds:0} ms",
            _ => value.ToString() ?? string.Empty,
        };
    }
}

public sealed record StringProperty : DiagnosticsProperty<string>
{
    public StringProperty(
        string name,
        string? value,
        string? defaultValue = null,
        bool showName = true)
        : base(name, value, defaultValue, showName: showName)
    {
    }
}

public sealed record DoubleProperty : DiagnosticsProperty<double?>
{
    public DoubleProperty(string name, double? value, double? defaultValue = null)
        : base(
            name,
            value,
            defaultValue,
            number => number.HasValue ? number.Value.ToString("0.0############") : "null")
    {
    }
}

public sealed record FlagProperty : DiagnosticsProperty<bool?>
{
    public FlagProperty(
        string name,
        bool? value,
        string ifTrue,
        string? ifFalse = null,
        bool showName = false)
        : base(
            name,
            value,
            defaultValue: null,
            flag => flag switch
            {
                true => ifTrue,
                false => ifFalse ?? "false",
                null => "null",
            },
            showName)
    {
    }
}

public sealed class DiagnosticPropertiesBuilder
{
    private readonly List<DiagnosticsNode> _properties = [];

    public IReadOnlyList<DiagnosticsNode> Properties => _properties;

    public void Add(DiagnosticsNode property)
    {
        ArgumentNullException.ThrowIfNull(property);
        _properties.Add(property);
    }
}
