using System.Diagnostics;
using System.Runtime.CompilerServices;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_inspector.dart

namespace Plumix.Widgets;

/// <summary>Interface for classes that track the source code location where they were created.</summary>
/// <remarks>
/// Flutter's private <c>_HasCreationLocation</c>. Dart's <c>--track-widget-creation</c> kernel
/// transformer makes <see cref="Widget"/> implement it; Plumix has no such transform, so no widget
/// does and widget creation is untracked (see <c>docs/ai/DIVERGENCES.md</c>).
/// </remarks>
internal interface IHasCreationLocation
{
    /// <summary>The location where the object was created.</summary>
    InspectorLocation? Location { get; }
}

/// <summary>A tuple with file, line, and column number, for displaying human-readable file locations.</summary>
/// <remarks>Flutter's private <c>_Location</c>.</remarks>
internal sealed class InspectorLocation
{
    public InspectorLocation(string file, int line, int column, string? name = null)
    {
        File = file;
        Line = line;
        Column = column;
        Name = name;
    }

    /// <summary>File path of the location.</summary>
    public string File { get; }

    /// <summary>1-based line number.</summary>
    public int Line { get; }

    /// <summary>1-based column number.</summary>
    public int Column { get; }

    /// <summary>Optional name of the parameter or function at this location.</summary>
    public string? Name { get; }

    public Dictionary<string, object?> ToJsonMap()
    {
        var json = new Dictionary<string, object?>
        {
            ["file"] = File,
            ["line"] = Line,
            ["column"] = Column,
        };
        if (Name is not null)
        {
            json["name"] = Name;
        }

        return json;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Name is not null)
        {
            parts.Add(Name);
        }

        parts.Add(File);
        parts.Add(Line.ToString(System.Globalization.CultureInfo.InvariantCulture));
        parts.Add(Column.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return string.Join(":", parts);
    }
}

/// <summary>
/// Annotation which marks a function as a widget factory for the purpose of widget creation
/// tracking.
/// </summary>
/// <remarks>
/// Flutter's <c>widgetFactory</c> (<c>_WidgetFactory</c>). Dart's creation-tracking transformer
/// reports the call site of an annotated extension method instead of the widget constructor call
/// inside it. It has no runtime behavior; Plumix tracks no creation locations.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class WidgetFactoryAttribute : Attribute
{
}

/// <summary>
/// A <see cref="DiagnosticsProperty{T}"/> that serves as a container for a link to the Flutter
/// DevTools inspector.
/// </summary>
/// <remarks>Flutter's <c>DevToolsDeepLinkProperty</c>.</remarks>
public sealed class DevToolsDeepLinkProperty : DiagnosticsProperty<string>
{
    /// <summary>Creates a property that holds a DevTools deep link <paramref name="url"/>.</summary>
    public DevToolsDeepLinkProperty(string description, string url)
        : base(string.Empty, url, description: description, level: DiagnosticLevel.Info)
    {
    }
}

/// <summary>The top-level debug functions of Flutter's <c>widget_inspector.dart</c>.</summary>
public static class WidgetInspectorDebug
{
    private static readonly Dictionary<InspectorLocation, int> LocationToId = new(ReferenceEqualityComparer.Instance);
    private static readonly List<InspectorLocation> Locations = [];

    /// <summary>
    /// Transformer to parse and gather information about <see cref="DiagnosticsDebugCreator"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>debugTransformDebugCreator</c>. This function will be added to
    /// <see cref="FlutterErrorDetails.PropertiesTransformers"/> in <see cref="WidgetsBinding"/>. The
    /// expanded creator nodes are placed before the stack trace and everything after it.
    /// </remarks>
    public static IEnumerable<DiagnosticsNode> DebugTransformDebugCreator(IEnumerable<DiagnosticsNode> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (!Constants.KDebugMode)
        {
            return [];
        }

        var pending = new List<DiagnosticsNode>();
        ErrorSummary? errorSummary = null;
        foreach (DiagnosticsNode node in properties)
        {
            if (node is ErrorSummary summary)
            {
                errorSummary = summary;
                break;
            }
        }

        bool foundStackTrace = false;
        var result = new List<DiagnosticsNode>();
        foreach (DiagnosticsNode node in properties)
        {
            if (!foundStackTrace && node is DiagnosticsStackTrace)
            {
                foundStackTrace = true;
            }

            if (IsDebugCreator(node))
            {
                result.AddRange(ParseDiagnosticsNode(node, errorSummary));
            }
            else
            {
                if (foundStackTrace)
                {
                    pending.Add(node);
                }
                else
                {
                    result.Add(node);
                }
            }
        }

        result.AddRange(pending);
        return result;
    }

    /// <remarks>Flutter's private <c>_isDebugCreator</c>.</remarks>
    private static bool IsDebugCreator(DiagnosticsNode node) => node is DiagnosticsDebugCreator;

    /// <summary>Transform the input <see cref="DiagnosticsNode"/>.</summary>
    /// <remarks>
    /// Flutter's private <c>_parseDiagnosticsNode</c>: the node must be a
    /// <see cref="DiagnosticsDebugCreator"/>.
    /// </remarks>
    private static IEnumerable<DiagnosticsNode> ParseDiagnosticsNode(DiagnosticsNode node, ErrorSummary? errorSummary)
    {
        Debug.Assert(IsDebugCreator(node));
        try
        {
            var debugCreator = (DebugCreator)node.Value!;
            Element element = debugCreator.Element;
            return DescribeRelevantUserCode(element, errorSummary);
        }
        catch (Exception error)
        {
            Scheduler.ScheduleMicrotask(() =>
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: error,
                    stack: error.StackTrace,
                    library: "widget inspector",
                    informationCollector: () =>
                    [
                        DiagnosticsNode.Message(
                            "This exception was caught while trying to describe the user-relevant code of "
                            + "another error."),
                    ]));
            });
            return [];
        }
    }

    /// <remarks>Flutter's private <c>_describeRelevantUserCode</c>.</remarks>
    private static IEnumerable<DiagnosticsNode> DescribeRelevantUserCode(Element element, ErrorSummary? errorSummary)
    {
        if (!WidgetInspectorService.Instance.IsWidgetCreationTracked())
        {
            return
            [
                new ErrorDescription(
                    "Widget creation tracking is currently disabled. Enabling "
                    + "it enables improved error messages. It can be enabled by passing "
                    + "`--track-widget-creation` to `flutter run` or `flutter test`."),
                new ErrorSpacer(),
            ];
        }

        bool IsOverflowError()
        {
            if (errorSummary?.TypedValue is { Count: > 0 } value)
            {
                object summary = value[0];
                if (summary is string text && text.StartsWith("A RenderFlex overflowed by", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        var nodes = new List<DiagnosticsNode>();
        bool ProcessElement(Element target)
        {
            // TODO(chunhtai): should print out all the widgets that are about to cross
            // package boundaries.
            if (DebugIsLocalCreationLocation(target))
            {
                DiagnosticsNode? devToolsDiagnostic = null;

                // TODO(kenz): once the inspector is better at dealing with broken trees,
                // we can enable deep links for more errors than just RenderFlex overflow
                // errors. See https://github.com/flutter/flutter/issues/74918.
                if (IsOverflowError())
                {
                    string? devToolsInspectorUri =
                        WidgetInspectorService.Instance.DevToolsInspectorUriForElement(target);
                    if (devToolsInspectorUri is not null)
                    {
                        devToolsDiagnostic = new DevToolsDeepLinkProperty(
                            "To inspect this widget in Flutter DevTools, visit: " + devToolsInspectorUri,
                            devToolsInspectorUri);
                    }
                }

                nodes.Add(new DiagnosticsBlock(
                    name: "The relevant error-causing widget was",
                    children:
                    [
                        new ErrorDescription(
                            $"{target.Widget.ToStringShort()} {DescribeCreationLocation(target) ?? "null"}"),
                    ]));
                nodes.Add(new ErrorSpacer());
                if (devToolsDiagnostic is not null)
                {
                    nodes.Add(devToolsDiagnostic);
                    nodes.Add(new ErrorSpacer());
                }

                return false;
            }

            return true;
        }

        if (ProcessElement(element))
        {
            element.VisitAncestorElements(ProcessElement);
        }

        return nodes;
    }

    /// <summary>
    /// Returns if an object is user created.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>debugIsLocalCreationLocation</c>. This always returns false if it is not called in
    /// debug mode. <paramref name="object"/> should be either a widget or an element.
    /// </remarks>
    public static bool DebugIsLocalCreationLocation(object @object)
    {
        bool isLocal = false;
        if (Constants.KDebugMode)
        {
            InspectorLocation? location = GetCreationLocation(@object);
            if (location is not null)
            {
                isLocal = WidgetInspectorService.Instance.IsLocalCreationLocation(location.File);
            }
        }

        return isLocal;
    }

    /// <summary>Returns true if a <paramref name="widget"/> is user created.</summary>
    /// <remarks>
    /// Flutter's <c>debugIsWidgetLocalCreation</c>. This is a faster variant of
    /// <see cref="DebugIsLocalCreationLocation"/> that is available in debug and profile builds but
    /// only works for <see cref="Widget"/>.
    /// </remarks>
    public static bool DebugIsWidgetLocalCreation(Widget widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        InspectorLocation? location = GetObjectCreationLocation(widget);
        return location is not null && WidgetInspectorService.Instance.IsLocalCreationLocation(location.File);
    }

    /// <summary>Returns the creation location of an object in string format if one is available.</summary>
    /// <remarks>Flutter's private <c>_describeCreationLocation</c>.</remarks>
    private static string? DescribeCreationLocation(object @object)
    {
        InspectorLocation? location = GetCreationLocation(@object);
        return location?.ToString();
    }

    /// <remarks>Flutter's private <c>_getObjectCreationLocation</c>.</remarks>
    internal static InspectorLocation? GetObjectCreationLocation(object @object)
    {
        return @object is IHasCreationLocation hasLocation ? hasLocation.Location : null;
    }

    /// <summary>Returns the creation location of an object if one is available.</summary>
    /// <remarks>
    /// Flutter's private <c>_getCreationLocation</c>. Creation locations are only available for debug
    /// mode builds when the <c>--track-widget-creation</c> flag is enabled on the call to the
    /// compiler. Currently creation locations are only available for <see cref="Widget"/> and
    /// <see cref="Element"/>.
    /// </remarks>
    internal static InspectorLocation? GetCreationLocation(object? @object)
    {
        object? candidate = @object is Element { DebugIsDefunct: false } element ? element.Widget : @object;
        return candidate is null ? null : GetObjectCreationLocation(candidate);
    }

    /// <remarks>Flutter's private <c>_toLocationId</c>.</remarks>
    internal static int ToLocationId(InspectorLocation location)
    {
        if (LocationToId.TryGetValue(location, out int id))
        {
            return id;
        }

        id = Locations.Count;
        Locations.Add(location);
        LocationToId[location] = id;
        return id;
    }

    /// <remarks>Flutter's private <c>_locationIdMapToJson</c>.</remarks>
    internal static Dictionary<string, object?> LocationIdMapToJson()
    {
        const string idsKey = "ids";
        const string linesKey = "lines";
        const string columnsKey = "columns";
        const string namesKey = "names";

        var fileLocationsMap = new Dictionary<string, object?>();
        foreach (InspectorLocation location in Locations)
        {
            int id = LocationToId[location];
            if (fileLocationsMap.GetValueOrDefault(location.File) is not Dictionary<string, object?> locations)
            {
                locations = new Dictionary<string, object?>
                {
                    [idsKey] = new List<int>(),
                    [linesKey] = new List<int>(),
                    [columnsKey] = new List<int>(),
                    [namesKey] = new List<string?>(),
                };
                fileLocationsMap[location.File] = locations;
            }

            ((List<int>)locations[idsKey]!).Add(id);
            ((List<int>)locations[linesKey]!).Add(location.Line);
            ((List<int>)locations[columnsKey]!).Add(location.Column);
            ((List<string?>)locations[namesKey]!).Add(location.Name);
        }

        return fileLocationsMap;
    }
}

/// <summary>
/// A delegate that configures how a hierarchy of <see cref="DiagnosticsNode"/>s are serialized by the
/// Flutter Inspector.
/// </summary>
/// <remarks>Flutter's <c>InspectorSerializationDelegate</c>.</remarks>
public sealed class InspectorSerializationDelegate : DiagnosticsSerializationDelegate
{
    private readonly List<DiagnosticsNode> _nodesCreatedByLocalProject = [];

    /// <summary>Creates an <see cref="InspectorSerializationDelegate"/> that serialize
    /// <see cref="DiagnosticsNode"/> for Flutter Inspector service.</summary>
    public InspectorSerializationDelegate(
        WidgetInspectorService service,
        string? groupName = null,
        bool summaryTree = false,
        int maxDescendantsTruncatableNode = -1,
        bool expandPropertyValues = true,
        int subtreeDepth = 1,
        bool includeProperties = false,
        Func<DiagnosticsNode, InspectorSerializationDelegate, Dictionary<string, object?>?>?
            addAdditionalPropertiesCallback = null,
        bool inDisableWidgetInspectorScope = false)
    {
        ArgumentNullException.ThrowIfNull(service);
        Service = service;
        GroupName = groupName;
        SummaryTree = summaryTree;
        MaxDescendantsTruncatableNode = maxDescendantsTruncatableNode;
        ExpandPropertyValues = expandPropertyValues;
        SubtreeDepth = subtreeDepth;
        IncludeProperties = includeProperties;
        AddAdditionalPropertiesCallback = addAdditionalPropertiesCallback;
        InDisableWidgetInspectorScope = inDisableWidgetInspectorScope;
    }

    /// <summary>Service used by GUI tools to interact with the <see cref="WidgetInspector"/>.</summary>
    public WidgetInspectorService Service { get; }

    /// <summary>Optional <c>groupName</c> parameter which indicates that the json should contain live
    /// object ids.</summary>
    public string? GroupName { get; }

    /// <summary>Whether the tree should only include nodes created by the local project.</summary>
    public bool SummaryTree { get; }

    /// <summary>Maximum descendents of <see cref="DiagnosticsNode"/> before truncating.</summary>
    public int MaxDescendantsTruncatableNode { get; }

    /// <inheritdoc />
    public override bool IncludeProperties { get; }

    /// <inheritdoc />
    public override int SubtreeDepth { get; }

    /// <inheritdoc />
    public override bool ExpandPropertyValues { get; }

    /// <summary>
    /// Callback to add additional experimental serialization properties.
    /// </summary>
    /// <remarks>This callback can be used to customize the serialization of DiagnosticsNode objects
    /// for experimental features in widget inspector clients such as Dart DevTools.</remarks>
    public Func<DiagnosticsNode, InspectorSerializationDelegate, Dictionary<string, object?>?>?
        AddAdditionalPropertiesCallback { get; }

    /// <summary>Whether the node being serialized is inside a <see cref="DisableWidgetInspectorScope"/>.</summary>
    public bool InDisableWidgetInspectorScope { get; }

    private bool Interactive => GroupName is not null;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalNodeProperties(
        DiagnosticsNode node,
        bool fullDetails = true)
    {
        var result = new Dictionary<string, object?>();
        object? value = node.Value;
        if (SummaryTree && fullDetails)
        {
            result["summaryTree"] = true;
        }

        if (Interactive)
        {
            result["valueId"] = Service.ToId(value, GroupName!);
        }

        InspectorLocation? creationLocation = WidgetInspectorDebug.GetCreationLocation(value);
        if (creationLocation is not null)
        {
            if (fullDetails)
            {
                result["locationId"] = WidgetInspectorDebug.ToLocationId(creationLocation);
                result["creationLocation"] = creationLocation.ToJsonMap();
            }

            if (Service.IsLocalCreationLocation(creationLocation.File))
            {
                _nodesCreatedByLocalProject.Add(node);
                result["createdByLocalProject"] = true;
            }
        }

        if (AddAdditionalPropertiesCallback is not null)
        {
            Dictionary<string, object?>? additional = AddAdditionalPropertiesCallback(node, this);
            if (additional is not null)
            {
                foreach (KeyValuePair<string, object?> entry in additional)
                {
                    result[entry.Key] = entry.Value;
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public override DiagnosticsSerializationDelegate DelegateForNode(DiagnosticsNode node)
    {
        // The tricky special case here is that when in the detailsTree,
        // we keep subtreeDepth from going down to zero until we reach nodes
        // that also exist in the summary tree. This ensures that every time
        // you expand a node in the details tree, you expand the entire subtree
        // up until you reach the next nodes shared with the summary tree.
        return SummaryTree || SubtreeDepth > 1 || Service.ShouldShowInSummaryTree(node)
            ? CopyWith(subtreeDepth: SubtreeDepth - 1)
            : this;
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> FilterChildren(List<DiagnosticsNode> nodes, DiagnosticsNode owner)
    {
        return Service.FilterChildren(nodes, this);
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> FilterProperties(List<DiagnosticsNode> nodes, DiagnosticsNode owner)
    {
        bool createdByLocalProject = _nodesCreatedByLocalProject.Contains(owner);
        return nodes
            .Where(node => !node.IsFiltered(createdByLocalProject ? DiagnosticLevel.Fine : DiagnosticLevel.Info))
            .ToList();
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> TruncateNodesList(List<DiagnosticsNode> nodes, DiagnosticsNode? owner)
    {
        if (MaxDescendantsTruncatableNode >= 0
            && owner!.AllowTruncate
            && nodes.Count > MaxDescendantsTruncatableNode)
        {
            nodes = Service.TruncateNodes(nodes, MaxDescendantsTruncatableNode);
        }

        return nodes;
    }

    /// <inheritdoc />
    public override DiagnosticsSerializationDelegate CopyWith(int? subtreeDepth = null, bool? includeProperties = null)
    {
        return CopyWith(subtreeDepth, includeProperties, expandPropertyValues: null);
    }

    /// <summary>Creates a copy of this delegate with the provided values.</summary>
    /// <remarks>Flutter's <c>InspectorSerializationDelegate.copyWith</c>.</remarks>
    public InspectorSerializationDelegate CopyWith(
        int? subtreeDepth = null,
        bool? includeProperties = null,
        bool? expandPropertyValues = null,
        bool? inDisableWidgetInspectorScope = null)
    {
        return new InspectorSerializationDelegate(
            groupName: GroupName,
            summaryTree: SummaryTree,
            maxDescendantsTruncatableNode: MaxDescendantsTruncatableNode,
            expandPropertyValues: expandPropertyValues ?? ExpandPropertyValues,
            subtreeDepth: subtreeDepth ?? SubtreeDepth,
            includeProperties: includeProperties ?? IncludeProperties,
            service: Service,
            addAdditionalPropertiesCallback: AddAdditionalPropertiesCallback,
            inDisableWidgetInspectorScope: inDisableWidgetInspectorScope ?? InDisableWidgetInspectorScope);
    }
}

/// <summary>A map that holds its object keys weakly and its value keys strongly.</summary>
/// <remarks>
/// Flutter's <c>WeakMap</c>: Dart keeps <c>null</c>, <c>String</c>, <c>num</c> and <c>bool</c> keys in
/// a regular map and every other key in an <c>Expando</c>. C# value types box to a fresh object on
/// every use, so they have no identity to hold weakly: they are kept with the strong keys too.
/// </remarks>
public sealed class WeakMap<TKey, TValue>
{
    private ConditionalWeakTable<object, StrongBox<TValue>> _objects = new();
    private readonly Dictionary<object, TValue> _primitives = [];
    private TValue? _nullValue;
    private bool _hasNullValue;

    private static bool IsPrimitive(object? key) => key is null or string || key.GetType().IsValueType;

    /// <summary>Returns the value for <paramref name="key"/>, or the default if there is none.</summary>
    public TValue? this[TKey key]
    {
        get
        {
            object? boxed = key;
            if (boxed is null)
            {
                return _hasNullValue ? _nullValue : default;
            }

            if (IsPrimitive(boxed))
            {
                return _primitives.TryGetValue(boxed, out TValue? value) ? value : default;
            }

            return _objects.TryGetValue(boxed, out StrongBox<TValue>? box) ? box.Value : default;
        }

        set
        {
            object? boxed = key;
            if (boxed is null)
            {
                _nullValue = value;
                _hasNullValue = true;
            }
            else if (IsPrimitive(boxed))
            {
                _primitives[boxed] = value!;
            }
            else
            {
                _objects.AddOrUpdate(boxed, new StrongBox<TValue>(value!));
            }
        }
    }

    /// <summary>Removes the value for <paramref name="key"/> and returns it.</summary>
    public TValue? Remove(TKey key)
    {
        object? boxed = key;
        if (boxed is null)
        {
            TValue? value = _hasNullValue ? _nullValue : default;
            _nullValue = default;
            _hasNullValue = false;
            return value;
        }

        if (IsPrimitive(boxed))
        {
            return _primitives.Remove(boxed, out TValue? value) ? value : default;
        }

        if (_objects.TryGetValue(boxed, out StrongBox<TValue>? box))
        {
            _objects.Remove(boxed);
            return box.Value;
        }

        return default;
    }

    /// <summary>Removes all pairs from the map.</summary>
    public void Clear()
    {
        _objects = new ConditionalWeakTable<object, StrongBox<TValue>>();
        _primitives.Clear();
        _nullValue = default;
        _hasNullValue = false;
    }
}
