// Dart parity source: flutter/packages/flutter/lib/src/foundation/diagnostics.dart

namespace Plumix.Foundation;

/// <summary>
/// The various priority levels used to filter which diagnostics are shown and
/// omitted.
///
/// Trees of Flutter diagnostics can be very large so filtering the diagnostics
/// shown matters. Typically filtering to only display diagnostics with at least
/// level [DiagnosticLevel.Info] is appropriate.
/// </summary>
public enum DiagnosticLevel
{
    /// Diagnostics that should not be shown.
    ///
    /// If a user chooses to display [Hidden] diagnostics, they should not expect
    /// the diagnostics to be formatted consistently with other diagnostics and
    /// they should expect them to sometimes be misleading.
    Hidden,

    /// A diagnostic that is likely to be low value but where the diagnostic
    /// display is just as high quality as a diagnostic with a higher level.
    Fine,

    /// Diagnostics that should only be shown when performing fine grained
    /// debugging of an object.
    Debug,

    /// Interesting diagnostics that should be typically shown.
    Info,

    /// Very important diagnostics that indicate problematic property values.
    Warning,

    /// Diagnostics that provide a hint about best practices.
    Hint,

    /// Diagnostics that summarize other diagnostics present.
    Summary,

    /// Diagnostics that indicate errors or unexpected conditions.
    Error,

    /// Special level indicating that no diagnostics should be shown.
    ///
    /// Do not specify this level for diagnostics. This level is only used to
    /// filter which diagnostics are shown.
    Off,
}

/// <summary>
/// Defines diagnostics data for a [value].
///
/// For debug and profile modes, [DiagnosticsNode] provides a high quality
/// multiline string dump via [ToStringDeep]. The core members are the [Name],
/// [ToDescription], [GetProperties], [Value], and [GetChildren] methods.
/// </summary>
public abstract class DiagnosticsNode
{
    private readonly DiagnosticsTreeStyle? _style;

    /// Initializes the object.
    protected DiagnosticsNode(
        string? name,
        DiagnosticsTreeStyle? style = null,
        bool showName = true,
        bool showSeparator = true,
        string? linePrefix = null)
    {
        if (name is not null && name.EndsWith(':'))
        {
            throw new ArgumentException(
                $"Names of diagnostic nodes must not end with colons.\nname:\n  \"{name}\"",
                nameof(name));
        }

        Name = name;
        _style = style;
        ShowNameValue = showName;
        ShowSeparator = showSeparator;
        LinePrefix = linePrefix;
    }

    /// Label describing the [DiagnosticsNode], typically shown before a
    /// separator (see [ShowSeparator]).
    public string? Name { get; }

    /// Whether to show a separator between [Name] and description.
    public bool ShowSeparator { get; }

    /// Whether the name of the property should be shown when showing the default
    /// view of the tree.
    public virtual bool ShowName => ShowNameValue;

    /// Prefix to include at the start of each line.
    public string? LinePrefix { get; }

    /// The value of the property either from `debugFillProperties` or the value
    /// this [DiagnosticsNode] represents.
    public abstract object? Value { get; }

    /// Priority level of the diagnostic used to control which diagnostics should
    /// be shown and filtered.
    public virtual DiagnosticLevel Level => DiagnosticLevel.Info;

    /// Description to show if the node has no displayed properties or children.
    public virtual string? EmptyBodyDescription => null;

    /// Whether the diagnostic may be wrapped across multiple lines.
    public virtual bool AllowWrap => false;

    /// Whether the name of the diagnostic may be wrapped across multiple lines.
    public virtual bool AllowNameWrap => false;

    /// Whether the diagnostic may be truncated.
    public virtual bool AllowTruncate => false;

    /// Hint for how the node should be displayed.
    public virtual DiagnosticsTreeStyle? Style => _style;

    /// The constructor-supplied `showName` value; [ShowName] may override it.
    protected bool ShowNameValue { get; }

    private string Separator => ShowSeparator ? ":" : string.Empty;

    /// Returns a configuration specifying how this object should be rendered as
    /// text art.
    protected internal TextTreeConfiguration? TextTreeConfiguration
    {
        get
        {
            if (Style is null)
            {
                throw new InvalidOperationException("A DiagnosticsNode must resolve a tree style.");
            }

            return Style switch
            {
                DiagnosticsTreeStyle.None => null,
                DiagnosticsTreeStyle.Dense => TextTreeConfigurations.Dense,
                DiagnosticsTreeStyle.Sparse => TextTreeConfigurations.Sparse,
                DiagnosticsTreeStyle.Offstage => TextTreeConfigurations.Dashed,
                DiagnosticsTreeStyle.Whitespace => TextTreeConfigurations.Whitespace,
                DiagnosticsTreeStyle.Transition => TextTreeConfigurations.Transition,
                DiagnosticsTreeStyle.SingleLine => TextTreeConfigurations.SingleLine,
                DiagnosticsTreeStyle.ErrorProperty => TextTreeConfigurations.ErrorProperty,
                DiagnosticsTreeStyle.Shallow => TextTreeConfigurations.Shallow,
                DiagnosticsTreeStyle.Error => TextTreeConfigurations.Error,
                DiagnosticsTreeStyle.Flat => TextTreeConfigurations.Flat,
                // Truncate children doesn't really need its own text style as the
                // rendering is quite custom.
                DiagnosticsTreeStyle.TruncateChildren => TextTreeConfigurations.Whitespace,
                _ => throw new InvalidOperationException($"Unknown tree style {Style}."),
            };
        }
    }

    /// Diagnostics containing just a string `message` and no properties.
    ///
    /// Consider using [MessageProperty] instead if the diagnostics message is a
    /// property with a name.
    public static DiagnosticsNode Message(
        string message,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info,
        bool allowWrap = true)
    {
        return new DiagnosticsProperty<object>(
            string.Empty,
            (object?)null,
            description: message,
            style: style,
            showName: false,
            allowWrap: allowWrap,
            level: level);
    }

    /// Serialize a list of [DiagnosticsNode]s to json.
    public static List<Dictionary<string, object?>> ToJsonList(
        List<DiagnosticsNode>? nodes,
        DiagnosticsNode? parent,
        DiagnosticsSerializationDelegate serializationDelegate)
    {
        ArgumentNullException.ThrowIfNull(serializationDelegate);

        if (nodes is null)
        {
            return [];
        }

        bool truncated = false;
        int originalNodeCount = nodes.Count;
        nodes = serializationDelegate.TruncateNodesList(nodes, parent);
        if (nodes.Count != originalNodeCount)
        {
            nodes = [.. nodes, Message("...")];
            truncated = true;
        }

        List<Dictionary<string, object?>> json = nodes
            .Select(node => node.ToJsonMap(serializationDelegate.DelegateForNode(node)))
            .ToList();
        if (truncated)
        {
            json[^1]["truncated"] = true;
        }

        return json;
    }

    /// Whether the diagnostic should be filtered due to its [Level] being lower
    /// than `minLevel`.
    public bool IsFiltered(DiagnosticLevel minLevel) => Level < minLevel;

    /// Returns a description with a short summary of the node itself not
    /// including children or properties.
    public abstract string ToDescription(TextTreeConfiguration? parentConfiguration = null);

    /// Properties of this [DiagnosticsNode].
    public abstract List<DiagnosticsNode> GetProperties();

    /// Children of this [DiagnosticsNode].
    public abstract List<DiagnosticsNode> GetChildren();

    /// Serialize the node to a JSON map according to the configuration provided
    /// in the [DiagnosticsSerializationDelegate].
    public virtual Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        ArgumentNullException.ThrowIfNull(serializationDelegate);

        bool hasChildren = GetChildren().Count > 0;
        var result = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["description"] = ToDescription(),
            ["type"] = Diagnostics.DescribeType(GetType()),
        };

        if (Name is not null)
        {
            result["name"] = Name;
        }

        if (!ShowSeparator)
        {
            result["showSeparator"] = ShowSeparator;
        }

        if (Level != DiagnosticLevel.Info)
        {
            result["level"] = Diagnostics.EnumName(Level);
        }

        if (!ShowName)
        {
            result["showName"] = ShowName;
        }

        if (EmptyBodyDescription is not null)
        {
            result["emptyBodyDescription"] = EmptyBodyDescription;
        }

        if (Style != DiagnosticsTreeStyle.Sparse)
        {
            result["style"] = Diagnostics.EnumName(Style!.Value);
        }

        if (AllowTruncate)
        {
            result["allowTruncate"] = AllowTruncate;
        }

        if (hasChildren)
        {
            result["hasChildren"] = hasChildren;
        }

        if (LinePrefix is { Length: > 0 })
        {
            result["linePrefix"] = LinePrefix;
        }

        if (!AllowWrap)
        {
            result["allowWrap"] = AllowWrap;
        }

        if (AllowNameWrap)
        {
            result["allowNameWrap"] = AllowNameWrap;
        }

        foreach (KeyValuePair<string, object?> entry in serializationDelegate.AdditionalNodeProperties(this))
        {
            result[entry.Key] = entry.Value;
        }

        if (serializationDelegate.IncludeProperties)
        {
            result["properties"] = ToJsonList(
                serializationDelegate.FilterProperties(GetProperties(), this),
                this,
                serializationDelegate);
        }

        if (serializationDelegate.SubtreeDepth > 0)
        {
            result["children"] = ToJsonList(
                serializationDelegate.FilterChildren(GetChildren(), this),
                this,
                serializationDelegate);
        }

        return result;
    }

    /// Serializes the node excluding its descendants, iteratively rather than
    /// recursively so a deep tree cannot overflow the stack.
    public Dictionary<string, object?> ToJsonMapIterative(DiagnosticsSerializationDelegate serializationDelegate)
    {
        ArgumentNullException.ThrowIfNull(serializationDelegate);

        var childrenToJsonify = new Queue<(DiagnosticsNode Node, Action<Dictionary<string, object?>> Callback)>();
        Dictionary<string, object?> result = ToJson(serializationDelegate, childrenToJsonify);
        JsonifyNextNodesInStack(childrenToJsonify, serializationDelegate);
        return result;
    }

    /// Returns a string representation of this diagnostic that is suitable for
    /// use in error messages and console output.
    public override string ToString() => ToString(null);

    /// Returns a string representation of this diagnostic that is suitable for
    /// use in error messages and console output.
    ///
    /// `parentConfiguration` specifies how the parent is rendered as text art.
    /// For example, if the parent places all properties on one line, the
    /// description of the property should be kept short enough to fit.
    public virtual string ToString(
        TextTreeConfiguration? parentConfiguration,
        DiagnosticLevel minLevel = DiagnosticLevel.Info)
    {
        if (Style == DiagnosticsTreeStyle.SingleLine)
        {
            return ToStringDeep(parentConfiguration: parentConfiguration, minLevel: minLevel);
        }

        string description = ToDescription(parentConfiguration);
        if (Name is null || Name.Length == 0 || !ShowName)
        {
            return description;
        }

        return description.Contains('\n', StringComparison.Ordinal)
            ? $"{Name}{Separator}\n{description}"
            : $"{Name}{Separator} {description}";
    }

    /// Returns a string representation of this node and its descendants.
    public string ToStringDeep(
        string prefixLineOne = "",
        string? prefixOtherLines = null,
        TextTreeConfiguration? parentConfiguration = null,
        DiagnosticLevel minLevel = DiagnosticLevel.Debug,
        int wrapWidth = 65)
    {
        return new TextTreeRenderer(minLevel: minLevel, wrapWidth: wrapWidth)
            .Render(this, prefixLineOne, prefixOtherLines, parentConfiguration);
    }

    /// Converts the properties (`getProperties`) of this node to a form useful
    /// for `Timeline` event arguments.
    public Dictionary<string, string> ToTimelineArguments()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DiagnosticsNode property in GetProperties())
        {
            if (property.Name is not null)
            {
                result[property.Name] = property.ToDescription(TextTreeConfigurations.SingleLine);
            }
        }

        return result;
    }

    private void JsonifyNextNodesInStack(
        Queue<(DiagnosticsNode Node, Action<Dictionary<string, object?>> Callback)> toJsonify,
        DiagnosticsSerializationDelegate serializationDelegate)
    {
        while (toJsonify.Count > 0)
        {
            (DiagnosticsNode node, Action<Dictionary<string, object?>> callback) = toJsonify.Dequeue();
            callback(node.ToJson(serializationDelegate, toJsonify));
        }
    }

    private Dictionary<string, object?> ToJson(
        DiagnosticsSerializationDelegate serializationDelegate,
        Queue<(DiagnosticsNode Node, Action<Dictionary<string, object?>> Callback)> childrenToJsonify)
    {
        bool truncated = false;
        var childrenJsonList = new List<Dictionary<string, object?>>();
        bool includeChildren = GetChildren().Count > 0 && serializationDelegate.SubtreeDepth > 0;
        if (includeChildren)
        {
            List<DiagnosticsNode> childrenNodes = serializationDelegate.FilterChildren(GetChildren(), this);
            int originalNodeCount = childrenNodes.Count;
            childrenNodes = serializationDelegate.TruncateNodesList(childrenNodes, this);
            if (childrenNodes.Count != originalNodeCount)
            {
                childrenNodes = [.. childrenNodes, Message("...")];
                truncated = true;
            }

            foreach (DiagnosticsNode child in childrenNodes)
            {
                childrenToJsonify.Enqueue((child, jsonChild => childrenJsonList.Add(jsonChild)));
            }
        }

        string description = ToDescription();
        string widgetRuntimeType = description == "[root]"
            ? "RootWidget"
            : description.Split('-')[0];
        bool shouldIndent = Style != DiagnosticsTreeStyle.Flat && Style != DiagnosticsTreeStyle.Error;

        var result = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["description"] = description,
            ["shouldIndent"] = shouldIndent,
            ["widgetRuntimeType"] = widgetRuntimeType,
        };

        if (truncated)
        {
            result["truncated"] = truncated;
        }

        IReadOnlyDictionary<string, object?> additional =
            serializationDelegate.AdditionalNodeProperties(this, fullDetails: false);
        foreach (KeyValuePair<string, object?> entry in additional)
        {
            result[entry.Key] = entry.Value;
        }

        if (includeChildren)
        {
            result["children"] = childrenJsonList;
        }

        return result;
    }
}

/// <summary>
/// [DiagnosticsNode] that exists mainly to provide a container for other
/// diagnostics that typically lacks a meaningful value of its own.
/// </summary>
public class DiagnosticsBlock : DiagnosticsNode
{
    private readonly List<DiagnosticsNode> _children;
    private readonly List<DiagnosticsNode> _properties;
    private readonly string _description;

    /// Creates a diagnostic with properties specified by `properties` and
    /// children specified by `children`.
    public DiagnosticsBlock(
        string? name = null,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.Whitespace,
        bool showName = true,
        bool showSeparator = true,
        string? linePrefix = null,
        object? value = null,
        string? description = null,
        DiagnosticLevel level = DiagnosticLevel.Info,
        bool allowTruncate = false,
        IEnumerable<DiagnosticsNode>? children = null,
        IEnumerable<DiagnosticsNode>? properties = null)
        : base(
            name,
            style,
            showName: showName && name is not null,
            showSeparator: showSeparator,
            linePrefix: linePrefix)
    {
        _description = description ?? string.Empty;
        _children = children is null ? [] : [.. children];
        _properties = properties is null ? [] : [.. properties];
        Value = value;
        Level = level;
        AllowTruncate = allowTruncate;
    }

    /// <inheritdoc />
    public override object? Value { get; }

    /// <inheritdoc />
    public override DiagnosticLevel Level { get; }

    /// <inheritdoc />
    public override bool AllowTruncate { get; }

    /// <inheritdoc />
    public override List<DiagnosticsNode> GetChildren() => _children;

    /// <inheritdoc />
    public override List<DiagnosticsNode> GetProperties() => _properties;

    /// <inheritdoc />
    public override string ToDescription(TextTreeConfiguration? parentConfiguration = null) => _description;
}

/// <summary>
/// A delegate that configures how a hierarchy of [DiagnosticsNode]s should be
/// serialized.
/// </summary>
public abstract class DiagnosticsSerializationDelegate
{
    /// Creates the default delegate.
    public static DiagnosticsSerializationDelegate Create(int subtreeDepth = 0, bool includeProperties = false)
        => new DefaultDiagnosticsSerializationDelegate(includeProperties, subtreeDepth);

    /// Returns a serializable map of additional information that will be
    /// included in the serialization of the given [DiagnosticsNode].
    public abstract IReadOnlyDictionary<string, object?> AdditionalNodeProperties(
        DiagnosticsNode node,
        bool fullDetails = true);

    /// Filters the list of [DiagnosticsNode]s that will be included as children
    /// for the given `owner` node.
    public abstract List<DiagnosticsNode> FilterChildren(List<DiagnosticsNode> nodes, DiagnosticsNode owner);

    /// Filters the list of [DiagnosticsNode]s that will be included as
    /// properties for the given `owner` node.
    public abstract List<DiagnosticsNode> FilterProperties(List<DiagnosticsNode> nodes, DiagnosticsNode owner);

    /// Truncates the given list of [DiagnosticsNode] that will be added to the
    /// serialization as children or properties of the `owner` node.
    ///
    /// The method must return a subset of the provided nodes and may not add new
    /// nodes.
    public abstract List<DiagnosticsNode> TruncateNodesList(List<DiagnosticsNode> nodes, DiagnosticsNode? owner);

    /// Returns the [DiagnosticsSerializationDelegate] to be used for adding the
    /// provided [DiagnosticsNode] to the serialization.
    public abstract DiagnosticsSerializationDelegate DelegateForNode(DiagnosticsNode node);

    /// Controls how many levels of children will be included in the serialized
    /// hierarchy of [DiagnosticsNode]s.
    public abstract int SubtreeDepth { get; }

    /// Whether to include the properties of a [DiagnosticsNode] in the
    /// serialization.
    public abstract bool IncludeProperties { get; }

    /// Whether properties that have a [DiagnosticsNode.Value] of type
    /// [IDiagnosticable] should be expanded.
    public abstract bool ExpandPropertyValues { get; }

    /// Creates a copy of this [DiagnosticsSerializationDelegate] with the
    /// provided values.
    public abstract DiagnosticsSerializationDelegate CopyWith(
        int? subtreeDepth = null,
        bool? includeProperties = null);
}

internal sealed class DefaultDiagnosticsSerializationDelegate : DiagnosticsSerializationDelegate
{
    private static readonly Dictionary<string, object?> EmptyProperties = new(StringComparer.Ordinal);

    internal DefaultDiagnosticsSerializationDelegate(bool includeProperties = false, int subtreeDepth = 0)
    {
        IncludeProperties = includeProperties;
        SubtreeDepth = subtreeDepth;
    }

    public override int SubtreeDepth { get; }

    public override bool IncludeProperties { get; }

    public override bool ExpandPropertyValues => false;

    public override IReadOnlyDictionary<string, object?> AdditionalNodeProperties(
        DiagnosticsNode node,
        bool fullDetails = true) => EmptyProperties;

    public override DiagnosticsSerializationDelegate DelegateForNode(DiagnosticsNode node)
        => SubtreeDepth > 0 ? CopyWith(subtreeDepth: SubtreeDepth - 1) : this;

    public override List<DiagnosticsNode> FilterChildren(List<DiagnosticsNode> nodes, DiagnosticsNode owner) => nodes;

    public override List<DiagnosticsNode> FilterProperties(List<DiagnosticsNode> nodes, DiagnosticsNode owner) => nodes;

    public override List<DiagnosticsNode> TruncateNodesList(List<DiagnosticsNode> nodes, DiagnosticsNode? owner)
        => nodes;

    public override DiagnosticsSerializationDelegate CopyWith(int? subtreeDepth = null, bool? includeProperties = null)
        => new DefaultDiagnosticsSerializationDelegate(
            includeProperties ?? IncludeProperties,
            subtreeDepth ?? SubtreeDepth);
}
