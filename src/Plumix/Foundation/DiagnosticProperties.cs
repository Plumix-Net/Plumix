using System.Globalization;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/diagnostics.dart

namespace Plumix.Foundation;

/// Signature for computing the value of a property lazily.
public delegate T? ComputePropertyValueCallback<out T>();

/// <summary>
/// Builder to accumulate properties and configuration used to assemble a
/// [DiagnosticsNode] from a [IDiagnosticable] object.
/// </summary>
public sealed class DiagnosticPropertiesBuilder
{
    /// Creates a [DiagnosticPropertiesBuilder] with an empty `properties` list.
    public DiagnosticPropertiesBuilder()
        : this([])
    {
    }

    private DiagnosticPropertiesBuilder(List<DiagnosticsNode> properties)
    {
        Properties = properties;
    }

    /// Creates a [DiagnosticPropertiesBuilder] that wraps the given list.
    ///
    /// Dart's `DiagnosticPropertiesBuilder.fromProperties` named constructor; the list is stored by
    /// reference, so later [Add] calls mutate the caller's list.
    public static DiagnosticPropertiesBuilder FromProperties(List<DiagnosticsNode> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        return new DiagnosticPropertiesBuilder(properties);
    }

    /// List of properties accumulated so far.
    public List<DiagnosticsNode> Properties { get; }

    /// Default style to use for the [DiagnosticsNode] if no style is specified.
    public DiagnosticsTreeStyle DefaultDiagnosticsTreeStyle { get; set; } = DiagnosticsTreeStyle.Sparse;

    /// Description to show if the node has no displayed properties or children.
    public string? EmptyBodyDescription { get; set; }

    /// Add a property to the list of properties.
    public void Add(DiagnosticsNode property)
    {
        ArgumentNullException.ThrowIfNull(property);

        Properties.Add(property);
    }
}

/// <summary>
/// Property with a [Value] of type `T`.
///
/// If the default `ToString` does not provide an adequate description of the
/// value, specify `description` defining a custom description.
/// </summary>
public class DiagnosticsProperty<T> : DiagnosticsNode
{
    private readonly string? _description;
    private readonly DiagnosticLevel _defaultLevel;
    private readonly ComputePropertyValueCallback<T>? _computeValue;
    private readonly bool _allowWrap;
    private readonly bool _allowNameWrap;
    private T? _value;
    private bool _valueComputed;
    private Exception? _exception;

    /// Create a diagnostics property.
    public DiagnosticsProperty(
        string? name,
        T? value,
        string? description = null,
        string? ifNull = null,
        string? ifEmpty = null,
        bool showName = true,
        bool showSeparator = true,
        object? defaultValue = null,
        string? tooltip = null,
        bool missingIfNull = false,
        string? linePrefix = null,
        bool expandableValue = false,
        bool allowWrap = true,
        bool allowNameWrap = true,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, style, showName: showName, showSeparator: showSeparator, linePrefix: linePrefix)
    {
        _description = description;
        _valueComputed = true;
        _value = value;
        _computeValue = null;
        DefaultValue = NormalizeDefaultValue(defaultValue);
        IfNull = ifNull ?? (missingIfNull ? "MISSING" : null);
        IfEmpty = ifEmpty;
        Tooltip = tooltip;
        MissingIfNull = missingIfNull;
        ExpandableValue = expandableValue;
        _allowWrap = allowWrap;
        _allowNameWrap = allowNameWrap;
        _defaultLevel = level;
    }

    /// Property with a [Value] that is computed only when the value is needed.
    ///
    /// Dart's `DiagnosticsProperty.lazy` named constructor.
    protected DiagnosticsProperty(
        string? name,
        ComputePropertyValueCallback<T> computeValue,
        string? description = null,
        string? ifNull = null,
        string? ifEmpty = null,
        bool showName = true,
        bool showSeparator = true,
        object? defaultValue = null,
        string? tooltip = null,
        bool missingIfNull = false,
        bool expandableValue = false,
        bool allowWrap = true,
        bool allowNameWrap = true,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, style, showName: showName, showSeparator: showSeparator)
    {
        ArgumentNullException.ThrowIfNull(computeValue);

        _description = description;
        _valueComputed = false;
        _value = default;
        _computeValue = computeValue;
        DefaultValue = NormalizeDefaultValue(defaultValue);
        IfNull = ifNull ?? (missingIfNull ? "MISSING" : null);
        IfEmpty = ifEmpty;
        Tooltip = tooltip;
        MissingIfNull = missingIfNull;
        ExpandableValue = expandableValue;
        _allowWrap = allowWrap;
        _allowNameWrap = allowNameWrap;
        _defaultLevel = level;
    }

    /// Creates a property whose value is computed lazily.
    public static DiagnosticsProperty<T> Lazy(
        string? name,
        ComputePropertyValueCallback<T> computeValue,
        string? description = null,
        string? ifNull = null,
        string? ifEmpty = null,
        bool showName = true,
        bool showSeparator = true,
        object? defaultValue = null,
        string? tooltip = null,
        bool missingIfNull = false,
        bool expandableValue = false,
        bool allowWrap = true,
        bool allowNameWrap = true,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
    {
        return new DiagnosticsProperty<T>(
            name,
            computeValue,
            description,
            ifNull,
            ifEmpty,
            showName,
            showSeparator,
            defaultValue,
            tooltip,
            missingIfNull,
            expandableValue,
            allowWrap,
            allowNameWrap,
            style,
            level);
    }

    /// Description if the property [Value] is null.
    public string? IfNull { get; }

    /// Description if the property description would otherwise be empty.
    public string? IfEmpty { get; }

    /// Optional tooltip typically describing the property.
    public string? Tooltip { get; }

    /// Whether a [Value] of null causes the property to have [DiagnosticLevel.Warning]
    /// warning that the property is missing a required value.
    public bool MissingIfNull { get; }

    /// If the [Value] of the property equals [DefaultValue] the priority [Level]
    /// of the property is downgraded to [DiagnosticLevel.Fine].
    ///
    /// Equals [DiagnosticsDefaults.NoDefaultValue] when the property has no default value.
    public object? DefaultValue { get; }

    /// Whether the properties of the [Value] should be expanded.
    public bool ExpandableValue { get; }

    /// <inheritdoc />
    public override bool AllowWrap => _allowWrap;

    /// <inheritdoc />
    public override bool AllowNameWrap => _allowNameWrap;

    /// The type of the property [Value].
    public Type PropertyType => typeof(T);

    /// <inheritdoc />
    public override object? Value
    {
        get
        {
            MaybeCacheValue();
            return _value;
        }
    }

    /// The property [Value] with its declared type.
    ///
    /// C# cannot narrow an override's return type, so Dart's `T? get value` is exposed here while
    /// the inherited [Value] keeps the base contract.
    public T? TypedValue
    {
        get
        {
            MaybeCacheValue();
            return _value;
        }
    }

    /// Exception thrown if accessing the property [Value] threw an exception.
    public Exception? Exception
    {
        get
        {
            MaybeCacheValue();
            return _exception;
        }
    }

    /// <inheritdoc />
    public override DiagnosticLevel Level
    {
        get
        {
            if (_defaultLevel == DiagnosticLevel.Hidden)
            {
                return _defaultLevel;
            }

            if (Exception is not null)
            {
                return DiagnosticLevel.Error;
            }

            if (Value is null && MissingIfNull)
            {
                return DiagnosticLevel.Warning;
            }

            // Use a low level when the value matches the default value.
            if (!IsInteresting)
            {
                return DiagnosticLevel.Fine;
            }

            return _defaultLevel;
        }
    }

    /// Whether the property [Value] differs from [DefaultValue].
    public bool IsInteresting =>
        ReferenceEquals(DefaultValue, DiagnosticsDefaults.NoDefaultValue) || !Equals(Value, DefaultValue);

    /// The [DiagnosticLevel] the property was constructed with.
    protected DiagnosticLevel DefaultLevel => _defaultLevel;

    /// <inheritdoc />
    public override List<DiagnosticsNode> GetProperties()
    {
        if (ExpandableValue)
        {
            object? obj = Value;
            if (obj is DiagnosticsNode node)
            {
                return node.GetProperties();
            }

            if (obj is IDiagnosticable diagnosticable)
            {
                return diagnosticable.ToDiagnosticsNode(style: Style).GetProperties();
            }
        }

        return [];
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> GetChildren()
    {
        if (ExpandableValue)
        {
            object? obj = Value;
            if (obj is DiagnosticsNode node)
            {
                return node.GetChildren();
            }

            if (obj is IDiagnosticable diagnosticable)
            {
                return diagnosticable.ToDiagnosticsNode(style: Style).GetChildren();
            }
        }

        return [];
    }

    /// <inheritdoc />
    public override string ToDescription(TextTreeConfiguration? parentConfiguration = null)
    {
        if (_description is not null)
        {
            return AddTooltip(_description);
        }

        if (Exception is not null)
        {
            return $"EXCEPTION ({Diagnostics.DescribeType(Exception.GetType())})";
        }

        if (IfNull is not null && Value is null)
        {
            return AddTooltip(IfNull);
        }

        string result = ValueToString(parentConfiguration);
        if (result.Length == 0 && IfEmpty is not null)
        {
            result = IfEmpty;
        }

        return AddTooltip(result);
    }

    /// Returns a string representation of the property value.
    public virtual string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        object? v = Value;
        // DiagnosticableTree values are shown using the shorter toStringShort form
        // as the tree of the value would otherwise be duplicated.
        return v is IDiagnosticableTree tree ? tree.ToStringShort() : Diagnostics.DescribeValue(v);
    }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        ArgumentNullException.ThrowIfNull(serializationDelegate);

        object? v = Value;
        List<Dictionary<string, object?>>? properties = null;
        if (serializationDelegate.ExpandPropertyValues
            && serializationDelegate.IncludeProperties
            && v is IDiagnosticable diagnosticable
            && GetProperties().Count == 0)
        {
            // Exclude children for expanded nodes to avoid cycles.
            serializationDelegate = serializationDelegate.CopyWith(subtreeDepth: 0, includeProperties: false);
            properties = ToJsonList(
                serializationDelegate.FilterProperties(diagnosticable.ToDiagnosticsNode().GetProperties(), this),
                this,
                serializationDelegate);
        }

        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        if (properties is not null)
        {
            json["properties"] = properties;
        }

        if (!ReferenceEquals(DefaultValue, DiagnosticsDefaults.NoDefaultValue))
        {
            json["defaultValue"] = Diagnostics.DescribeValue(DefaultValue);
        }

        if (IfEmpty is not null)
        {
            json["ifEmpty"] = IfEmpty;
        }

        if (IfNull is not null)
        {
            json["ifNull"] = IfNull;
        }

        if (Tooltip is not null)
        {
            json["tooltip"] = Tooltip;
        }

        json["missingIfNull"] = MissingIfNull;
        if (Exception is not null)
        {
            json["exception"] = Exception.ToString();
        }

        json["propertyType"] = Diagnostics.DescribeType(PropertyType);
        json["defaultLevel"] = Diagnostics.EnumName(_defaultLevel);
        if (v is IDiagnosticable or DiagnosticsNode)
        {
            json["isDiagnosticableValue"] = true;
        }

        if (v is double or float or int or long or short or decimal)
        {
            json["value"] = v is double d && (double.IsNaN(d) || double.IsInfinity(d))
                ? d.ToString(CultureInfo.InvariantCulture)
                : v;
        }
        else if (v is string or bool || v is null)
        {
            json["value"] = v;
        }

        return json;
    }

    /// Normalizes the constructor argument onto Dart's `kNoDefaultValue` sentinel.
    ///
    /// C# cannot use a non-constant sentinel as a default parameter value, so an omitted (null)
    /// argument means "no default value" and [DiagnosticsDefaults.NullValue] means Dart's explicit
    /// `defaultValue: null`.
    private protected static object? NormalizeDefaultValue(object? defaultValue)
    {
        if (defaultValue is null)
        {
            return DiagnosticsDefaults.NoDefaultValue;
        }

        return ReferenceEquals(defaultValue, DiagnosticsDefaults.NullValue) ? null : defaultValue;
    }

    private void MaybeCacheValue()
    {
        if (_valueComputed)
        {
            return;
        }

        _valueComputed = true;
        try
        {
            _value = _computeValue!();
        }
        catch (Exception exception)
        {
            _exception = exception;
            _value = default;
        }
    }

    private string AddTooltip(string text) => Tooltip is null ? text : $"{text} ({Tooltip})";
}

/// <summary>
/// Property containing a single string as a description.
/// </summary>
public sealed class MessageProperty : DiagnosticsProperty<object>
{
    /// Create a diagnostics property that displays a message.
    public MessageProperty(
        string name,
        string message,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, (object?)null, description: message, style: style, level: level)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(message);
    }
}

/// <summary>
/// Property which encloses its string [Value] in quotes.
/// </summary>
public sealed class StringProperty : DiagnosticsProperty<string>
{
    /// Create a diagnostics property for strings.
    public StringProperty(
        string? name,
        string? value,
        string? description = null,
        string? tooltip = null,
        bool showName = true,
        object? defaultValue = null,
        bool quoted = true,
        string? ifEmpty = null,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            name,
            value,
            description: description,
            defaultValue: defaultValue,
            tooltip: tooltip,
            showName: showName,
            ifEmpty: ifEmpty,
            style: style,
            level: level)
    {
        Quoted = quoted;
        Description = description;
    }

    /// Whether the value is enclosed in double quotes.
    public bool Quoted { get; }

    private string? Description { get; }

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        string? text = Description ?? TypedValue;
        if (parentConfiguration is not null && !parentConfiguration.LineBreakProperties && text is not null)
        {
            // Escape linebreaks in multiline strings to avoid confusing output when
            // the parent specifies that all lines must be on one line.
            text = text.Replace("\n", "\\n", StringComparison.Ordinal);
        }

        if (Quoted && text is not null)
        {
            // An empty value would not appear empty after being surrounded with
            // quotes so we have to handle this case separately.
            if (IfEmpty is not null && text.Length == 0)
            {
                return IfEmpty;
            }

            return $"\"{text}\"";
        }

        return text ?? "null";
    }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        json["quoted"] = Quoted;
        return json;
    }
}

/// <summary>
/// Property describing a numeric `T` value with an optional unit.
/// </summary>
public abstract class NumProperty<T> : DiagnosticsProperty<T?>
    where T : struct
{
    /// Create a diagnostics property for numeric values.
    protected NumProperty(
        string name,
        T? value,
        string? ifNull = null,
        string? unit = null,
        bool showName = true,
        object? defaultValue = null,
        string? tooltip = null,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            name,
            value,
            ifNull: ifNull,
            showName: showName,
            defaultValue: defaultValue,
            tooltip: tooltip,
            style: style,
            level: level)
    {
        Unit = unit;
    }

    /// Create a diagnostics property for numeric values computed lazily.
    protected NumProperty(
        string name,
        ComputePropertyValueCallback<T?> computeValue,
        string? ifNull = null,
        string? unit = null,
        bool showName = true,
        object? defaultValue = null,
        string? tooltip = null,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            name,
            computeValue,
            ifNull: ifNull,
            showName: showName,
            defaultValue: defaultValue,
            tooltip: tooltip,
            style: style,
            level: level)
    {
        Unit = unit;
    }

    /// Optional unit the [DiagnosticsProperty{T}.Value] is measured in.
    public string? Unit { get; }

    /// String describing just the numeric [DiagnosticsProperty{T}.Value] without a unit suffix.
    public abstract string NumberToString();

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        if (Value is null)
        {
            return "null";
        }

        return Unit is not null ? $"{NumberToString()}{Unit}" : NumberToString();
    }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        if (Unit is not null)
        {
            json["unit"] = Unit;
        }

        json["numberToString"] = NumberToString();
        return json;
    }
}

/// <summary>
/// Property describing a `double` [DiagnosticsProperty{T}.Value] with an optional unit of
/// measurement.
/// </summary>
public class DoubleProperty : NumProperty<double>
{
    /// If specified, paints a fixed number of digits after the decimal point.
    ///
    /// Dart's `debugDoublePrecision` global.
    public static int? DebugDoublePrecision { get; set; }

    /// Create a diagnostics property for `double` values.
    public DoubleProperty(
        string name,
        double? value,
        string? ifNull = null,
        string? unit = null,
        string? tooltip = null,
        object? defaultValue = null,
        bool showName = true,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            name,
            value,
            ifNull: ifNull,
            unit: unit,
            tooltip: tooltip,
            defaultValue: defaultValue,
            showName: showName,
            style: style,
            level: level)
    {
    }

    private DoubleProperty(
        string name,
        ComputePropertyValueCallback<double?> computeValue,
        string? ifNull,
        bool showName,
        string? unit,
        string? tooltip,
        object? defaultValue,
        DiagnosticLevel level)
        : base(
            name,
            computeValue,
            ifNull: ifNull,
            unit: unit,
            tooltip: tooltip,
            defaultValue: defaultValue,
            showName: showName,
            level: level)
    {
    }

    /// Property with a [DiagnosticsProperty{T}.Value] that is computed only when the value is
    /// needed.
    public static DoubleProperty Lazy(
        string name,
        ComputePropertyValueCallback<double?> computeValue,
        string? ifNull = null,
        bool showName = true,
        string? unit = null,
        string? tooltip = null,
        object? defaultValue = null,
        DiagnosticLevel level = DiagnosticLevel.Info)
    {
        return new DoubleProperty(name, computeValue, ifNull, showName, unit, tooltip, defaultValue, level);
    }

    /// Formats a double to have standard formatting.
    ///
    /// Dart's `debugFormatDouble`.
    public static string FormatDouble(double? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (DebugDoublePrecision is int precision)
        {
            return value.Value.ToString($"G{precision}", CultureInfo.InvariantCulture);
        }

        return value.Value.ToString("F1", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override string NumberToString() => FormatDouble(TypedValue);
}

/// <summary>
/// An int valued property with an optional unit the value is measured in.
/// </summary>
public sealed class IntProperty : NumProperty<int>
{
    /// Create a diagnostics property for integers.
    public IntProperty(
        string name,
        int? value,
        string? ifNull = null,
        bool showName = true,
        string? unit = null,
        object? defaultValue = null,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            name,
            value,
            ifNull: ifNull,
            showName: showName,
            unit: unit,
            defaultValue: defaultValue,
            style: style,
            level: level)
    {
    }

    /// <inheritdoc />
    public override string NumberToString()
        => TypedValue?.ToString(CultureInfo.InvariantCulture) ?? "null";
}

/// <summary>
/// Property which clamps a `double` to between 0 and 1 and formats it as a percentage.
/// </summary>
public sealed class PercentProperty : DoubleProperty
{
    /// Create a diagnostics property for doubles that represent percentages or
    /// fractions.
    public PercentProperty(
        string name,
        double? fraction,
        string? ifNull = null,
        bool showName = true,
        string? tooltip = null,
        string? unit = null,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, fraction, ifNull: ifNull, showName: showName, tooltip: tooltip, unit: unit, level: level)
    {
    }

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        if (Value is null)
        {
            return "null";
        }

        return Unit is not null ? $"{NumberToString()} {Unit}" : NumberToString();
    }

    /// <inheritdoc />
    public override string NumberToString()
    {
        double? v = TypedValue;
        if (v is null)
        {
            return "null";
        }

        double clamped = ClampDouble(v.Value, 0.0, 1.0);
        return $"{(clamped * 100.0).ToString("F1", CultureInfo.InvariantCulture)}%";
    }

    private static double ClampDouble(double x, double min, double max)
    {
        if (x < min)
        {
            return min;
        }

        if (x > max)
        {
            return max;
        }

        return double.IsNaN(x) ? max : x;
    }
}

/// <summary>
/// Property where the description is either [IfTrue] or [IfFalse] depending on
/// whether [DiagnosticsProperty{T}.Value] is true or false.
/// </summary>
public sealed class FlagProperty : DiagnosticsProperty<bool?>
{
    /// Constructs a FlagProperty with the given descriptions with default values.
    public FlagProperty(
        string name,
        bool? value,
        string? ifTrue = null,
        string? ifFalse = null,
        bool showName = false,
        object? defaultValue = null,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            name,
            value,
            showName: showName,
            // Dart's `defaultValue` parameter here has no `kNoDefaultValue` default: an omitted
            // argument means "the default value is null", which downgrades a null flag to `fine`.
            defaultValue: defaultValue ?? DiagnosticsDefaults.NullValue,
            level: level)
    {
        if (ifTrue is null && ifFalse is null)
        {
            throw new ArgumentException(
                "A FlagProperty must be given at least one of ifTrue or ifFalse.",
                nameof(ifTrue));
        }

        IfTrue = ifTrue;
        IfFalse = ifFalse;
    }

    /// Description to use if the property [DiagnosticsProperty{T}.Value] is true.
    public string? IfTrue { get; }

    /// Description to use if the property [DiagnosticsProperty{T}.Value] is false.
    public string? IfFalse { get; }

    /// <inheritdoc />
    public override bool ShowName
    {
        get
        {
            bool? value = TypedValue;
            if (value is null || (value.Value && IfTrue is null) || (!value.Value && IfFalse is null))
            {
                // We are missing a description for the flag value so we need to show the
                // property name.
                return true;
            }

            return base.ShowName;
        }
    }

    /// <inheritdoc />
    public override DiagnosticLevel Level
    {
        get
        {
            bool? value = TypedValue;
            if (value == true && IfTrue is null)
            {
                return DiagnosticLevel.Hidden;
            }

            if (value == false && IfFalse is null)
            {
                return DiagnosticLevel.Hidden;
            }

            return base.Level;
        }
    }

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        bool? value = TypedValue;
        if (value == true && IfTrue is not null)
        {
            return IfTrue;
        }

        if (value == false && IfFalse is not null)
        {
            return IfFalse;
        }

        return base.ValueToString(parentConfiguration);
    }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        if (IfTrue is not null)
        {
            json["ifTrue"] = IfTrue;
        }

        if (IfFalse is not null)
        {
            json["ifFalse"] = IfFalse;
        }

        return json;
    }
}

/// <summary>
/// Property with an `IEnumerable{T}` [DiagnosticsProperty{T}.Value] that can be displayed with
/// different formatting depending on the [DiagnosticsNode.Style].
/// </summary>
public sealed class IterableProperty<T> : DiagnosticsProperty<IEnumerable<T>>
{
    /// Create a diagnostics property for iterables (e.g. lists).
    public IterableProperty(
        string name,
        IEnumerable<T>? value,
        object? defaultValue = null,
        string? ifNull = null,
        string? ifEmpty = "[]",
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        bool showName = true,
        bool showSeparator = true,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            name,
            value,
            defaultValue: defaultValue,
            ifNull: ifNull,
            ifEmpty: ifEmpty,
            style: style,
            showName: showName,
            showSeparator: showSeparator,
            level: level)
    {
    }

    /// <inheritdoc />
    public override DiagnosticLevel Level
    {
        get
        {
            if (IfEmpty is null
                && TypedValue is not null
                && !TypedValue.Any()
                && base.Level != DiagnosticLevel.Hidden)
            {
                return DiagnosticLevel.Fine;
            }

            return base.Level;
        }
    }

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        IEnumerable<T>? value = TypedValue;
        if (value is null)
        {
            return "null";
        }

        List<T> items = [.. value];
        if (items.Count == 0)
        {
            return IfEmpty ?? "[]";
        }

        List<string> formatted = items
            .Select(v => typeof(T) == typeof(double) && v is double d
                ? DoubleProperty.FormatDouble(d)
                : Diagnostics.DescribeValue(v))
            .ToList();

        if (parentConfiguration is not null && !parentConfiguration.LineBreakProperties)
        {
            // Always display the value as a single line and enclose the iterable
            // value in brackets to avoid ambiguity.
            return $"[{string.Join(", ", formatted)}]";
        }

        return string.Join(Style == DiagnosticsTreeStyle.SingleLine ? ", " : "\n", formatted);
    }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        if (TypedValue is not null)
        {
            json["values"] = TypedValue.Select(v => Diagnostics.DescribeValue(v)).ToList();
        }

        return json;
    }
}

/// <summary>
/// An property than displays enum values tersely.
/// </summary>
public sealed class EnumProperty<T> : DiagnosticsProperty<T?>
    where T : struct, Enum
{
    /// Create a diagnostics property that displays an enum.
    public EnumProperty(
        string name,
        T? value,
        object? defaultValue = null,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, value, defaultValue: defaultValue, level: level)
    {
    }

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        T? value = TypedValue;
        return value is null ? "null" : Diagnostics.EnumName(value.Value);
    }
}

/// <summary>
/// A property where the important diagnostic information is primarily whether
/// the [DiagnosticsProperty{T}.Value] is present (non-null) or absent (null),
/// rather than the actual value of the property itself.
/// </summary>
public sealed class ObjectFlagProperty<T> : DiagnosticsProperty<T>
{
    /// Create a diagnostics property for values that can be present (non-null) or
    /// absent (null), but for which the exact value's [object.ToString]
    /// representation is not very transparent (e.g. a callback).
    public ObjectFlagProperty(
        string name,
        T? value,
        string? ifPresent = null,
        string? ifNull = null,
        bool showName = false,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, value, showName: showName, ifNull: ifNull, level: level)
    {
        if (ifPresent is null && ifNull is null)
        {
            throw new ArgumentException(
                "An ObjectFlagProperty must be given at least one of ifPresent or ifNull.",
                nameof(ifPresent));
        }

        IfPresent = ifPresent;
    }

    private ObjectFlagProperty(string name, T? value, DiagnosticLevel level)
        : base(name, value, showName: false, level: level)
    {
        IfPresent = $"has {name}";
    }

    /// Shorthand constructor to describe whether the property has a value.
    ///
    /// Dart's `ObjectFlagProperty.has` named constructor.
    public static ObjectFlagProperty<T> Has(string name, T? value, DiagnosticLevel level = DiagnosticLevel.Info)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new ObjectFlagProperty<T>(name, value, level);
    }

    /// Description to use if the property [DiagnosticsProperty{T}.Value] is not null.
    public string? IfPresent { get; }

    /// <inheritdoc />
    public override bool ShowName
    {
        get
        {
            if ((Value is not null && IfPresent is null) || (Value is null && IfNull is null))
            {
                // We are missing a description for the flag value so we need to show the
                // property name.
                return true;
            }

            return base.ShowName;
        }
    }

    /// <inheritdoc />
    public override DiagnosticLevel Level
    {
        get
        {
            if (Value is not null)
            {
                if (IfPresent is null)
                {
                    return DiagnosticLevel.Hidden;
                }
            }
            else if (IfNull is null)
            {
                return DiagnosticLevel.Hidden;
            }

            return base.Level;
        }
    }

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        if (Value is not null)
        {
            if (IfPresent is not null)
            {
                return IfPresent;
            }
        }
        else if (IfNull is not null)
        {
            return IfNull;
        }

        return base.ValueToString(parentConfiguration);
    }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        if (IfPresent is not null)
        {
            json["ifPresent"] = IfPresent;
        }

        return json;
    }
}

/// <summary>
/// A summary of multiple properties, indicating whether each of them is present
/// (non-null) or absent (null).
/// </summary>
public sealed class FlagsSummary<T> : DiagnosticsProperty<IReadOnlyList<KeyValuePair<string, T?>>>
{
    /// Create a summary for multiple properties, indicating whether each of them
    /// is present (non-null) or absent (null).
    ///
    /// Dart types this as `Map<String, T?>`; `Dictionary<TKey, TValue>` has no contractual
    /// enumeration order, so the entries are carried in an explicit ordered list.
    public FlagsSummary(
        string name,
        IReadOnlyList<KeyValuePair<string, T?>> value,
        string? ifEmpty = null,
        bool showName = true,
        bool showSeparator = true,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(
            name,
            value,
            ifEmpty: ifEmpty,
            showName: showName,
            showSeparator: showSeparator,
            level: level)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    /// <inheritdoc />
    public override DiagnosticLevel Level
    {
        get
        {
            if (!HasNonNullEntry() && IfEmpty is null)
            {
                return DiagnosticLevel.Hidden;
            }

            return base.Level;
        }
    }

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        if (!HasNonNullEntry() && IfEmpty is not null)
        {
            return IfEmpty;
        }

        List<string> formatted = FormattedValues();
        if (parentConfiguration is not null && !parentConfiguration.LineBreakProperties)
        {
            return $"[{string.Join(", ", formatted)}]";
        }

        return string.Join(Style == DiagnosticsTreeStyle.SingleLine ? ", " : "\n", formatted);
    }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        if (TypedValue!.Count > 0)
        {
            json["values"] = FormattedValues();
        }

        return json;
    }

    private bool HasNonNullEntry() => TypedValue!.Any(entry => entry.Value is not null);

    private List<string> FormattedValues() =>
        TypedValue!.Where(entry => entry.Value is not null).Select(entry => entry.Key).ToList();
}

/// <summary>
/// [DiagnosticsNode] that lazily calls the [IDiagnosticable] `value` to
/// implement [GetChildren] and [GetProperties].
/// </summary>
public class DiagnosticableNode<T> : DiagnosticsNode
    where T : IDiagnosticable
{
    private DiagnosticPropertiesBuilder? _cachedBuilder;

    /// Create a diagnostics describing a [IDiagnosticable] value.
    public DiagnosticableNode(string? name, T value, DiagnosticsTreeStyle? style)
        : base(name, style)
    {
        ArgumentNullException.ThrowIfNull(value);

        TypedValue = value;
    }

    /// The [IDiagnosticable] this node describes.
    public T TypedValue { get; }

    /// <inheritdoc />
    public override object? Value => TypedValue;

    /// <inheritdoc />
    public override DiagnosticsTreeStyle? Style => base.Style ?? Builder.DefaultDiagnosticsTreeStyle;

    /// <inheritdoc />
    public override string? EmptyBodyDescription => Builder.EmptyBodyDescription;

    /// The builder holding the value's `debugFillProperties` output.
    protected DiagnosticPropertiesBuilder Builder
    {
        get
        {
            if (_cachedBuilder is null)
            {
                _cachedBuilder = new DiagnosticPropertiesBuilder();
                TypedValue.DebugFillProperties(_cachedBuilder);
            }

            return _cachedBuilder;
        }
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> GetProperties() => Builder.Properties;

    /// <inheritdoc />
    public override List<DiagnosticsNode> GetChildren() => [];

    /// <inheritdoc />
    public override string ToDescription(TextTreeConfiguration? parentConfiguration = null)
        => TypedValue.ToStringShort();
}

/// <summary>
/// [DiagnosticsNode] for an instance of [IDiagnosticableTree].
/// </summary>
public sealed class DiagnosticableTreeNode : DiagnosticableNode<IDiagnosticableTree>
{
    /// Creates a [DiagnosticsNode] for an [IDiagnosticableTree] value.
    public DiagnosticableTreeNode(string? name, IDiagnosticableTree value, DiagnosticsTreeStyle? style)
        : base(name, value, style)
    {
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> GetChildren() => TypedValue.DebugDescribeChildren();
}
