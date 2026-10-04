using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Developer;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_inspector.dart

namespace Plumix.Widgets;

/// <summary>Signature for the registration callback the inspector registers its extensions with.</summary>
/// <remarks>
/// Flutter's <c>RegisterServiceExtensionCallback</c>; <see cref="BindingBase.RegisterServiceExtension"/>
/// has this signature.
/// </remarks>
public delegate void RegisterServiceExtensionCallback(string name, ServiceExtensionCallback callback);

/// <summary>Signature for the selection change callback used by
/// <see cref="WidgetInspectorService.SelectionChangedCallback"/>.</summary>
/// <remarks>Flutter's <c>InspectorSelectionChangedCallback</c>.</remarks>
public delegate void InspectorSelectionChangedCallback();

/// <summary>
/// Structure to help reference count Dart objects referenced by a GUI tool using
/// <see cref="WidgetInspectorService"/>.
/// </summary>
/// <remarks>
/// Flutter's <c>InspectorReferenceData</c>. Does not hold the object from garbage collection: Dart
/// holds strings, numbers and booleans strongly and every other object through a
/// <c>WeakReference</c>, which throws for a record. C# value types (which a weak reference cannot
/// track, because every box is a new object) are held strongly; a value tuple, C#'s record, throws.
/// </remarks>
public sealed class InspectorReferenceData
{
    private readonly WeakReference<object>? _ref;
    private readonly object? _value;

    /// <summary>Creates an instance of <see cref="InspectorReferenceData"/>.</summary>
    public InspectorReferenceData(object @object, string id)
    {
        ArgumentNullException.ThrowIfNull(@object);
        Id = id;
        if (@object is ITuple && @object.GetType().IsValueType)
        {
            throw new ArgumentException("WeakReference is not supported for records.", nameof(@object));
        }

        // These types are not supported by WeakReference.
        // See https://api.dart.dev/stable/3.0.2/dart-core/WeakReference-class.html
        if (@object is string || @object.GetType().IsValueType)
        {
            _value = @object;
            return;
        }

        _ref = new WeakReference<object>(@object);
    }

    /// <summary>The id of the object in the widget inspector records.</summary>
    public string Id { get; }

    /// <summary>The number of times the object has been referenced.</summary>
    public int Count { get; set; } = 1;

    /// <summary>The value.</summary>
    public object? Value => _ref is not null && _ref.TryGetTarget(out object? target) ? target : _value;
}

/// <summary>Service used by GUI tools to interact with the <see cref="WidgetInspector"/>.</summary>
/// <remarks>
/// <para>
/// Flutter's <c>mixin WidgetInspectorService</c>. Calls from GUI tools come through the
/// <c>ext.flutter.inspector.*</c> service extensions <see cref="InitServiceExtensions"/> registers;
/// objects are referred to by ids that live in object groups the tool disposes as a unit.
/// </para>
/// <para>
/// Plumix has no <c>--track-widget-creation</c> transform, so <see cref="IsWidgetCreationTracked"/>
/// is false and every creation-location feature answers what Dart answers with the transform off.
/// </para>
/// </remarks>
public abstract partial class WidgetInspectorService
{
    private const string ConsoleObjectGroup = "console-group";

    private static WidgetInspectorService _instance = new DefaultWidgetInspectorService();
    private static bool _debugServiceExtensionsRegistered;

    // This map is used to keep track of the latest frame and repaint statistics.
    private readonly string?[] _serializeRing = new string?[20];
    private int _serializeRingIndex;
    private readonly Dictionary<string, HashSet<InspectorReferenceData>> _groups = [];
    private readonly Dictionary<string, InspectorReferenceData> _idToReferenceData = [];
    private readonly WeakMap<object, string> _objectToId = new();
    private int _nextId;

    // The pub root directories of the inspected app, or null before the tool sets them.
    private List<string>? _pubRootDirectories;

    // Memoization for IsLocalCreationLocation.
    private readonly Dictionary<string, bool> _isLocalCreationCache = [];

    private bool _trackRebuildDirtyWidgets;
    private bool _trackRepaintWidgets;
    private int _errorsSinceReload;
    private bool? _widgetCreationTracked;
    private TimeSpan _frameStart;
    private int _frameNumber;
    private readonly ElementLocationStatsTracker _rebuildStats = new();
    private readonly ElementLocationStatsTracker _repaintStats = new();

    /// <summary>Creates the service and its <see cref="Selection"/>.</summary>
    protected WidgetInspectorService()
    {
        Selection = new InspectorSelection();
    }

    /// <summary>The current <see cref="WidgetInspectorService"/>.</summary>
    /// <remarks>Flutter's <c>WidgetInspectorService.instance</c>; the setter is protected, as in Dart.</remarks>
    public static WidgetInspectorService Instance
    {
        get => _instance;
        protected internal set => _instance = value;
    }

    /// <summary>Enables select mode for the Inspector.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetInspectorService.isSelectMode</c> setter (test-only there). In select mode,
    /// pointer interactions trigger widget selection instead of normal interactions. The getter is
    /// C#-only: it reads <see cref="WidgetsBinding.DebugShowWidgetInspectorOverride"/>.
    /// </remarks>
    public bool IsSelectMode
    {
        get => WidgetsBinding.Instance.DebugShowWidgetInspectorOverride;
        set => ChangeWidgetSelectionMode(value);
    }

    /// <summary>The object that is currently selected within the inspector.</summary>
    public InspectorSelection Selection { get; }

    /// <summary>Callback typically registered by the <see cref="WidgetInspector"/> to receive
    /// notifications when <see cref="Selection"/> changes.</summary>
    public InspectorSelectionChangedCallback? SelectionChangedCallback { get; set; }

    /// <summary>Registers a service extension method with the given name and callback.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetInspectorService.registerServiceExtension</c>: the name is prefixed with
    /// <c>inspector.</c>, so the full method name is <c>ext.flutter.inspector.&lt;name&gt;</c>.
    /// </remarks>
    protected virtual void RegisterServiceExtension(
        string name,
        ServiceExtensionCallback callback,
        RegisterServiceExtensionCallback registerExtension)
    {
        ArgumentNullException.ThrowIfNull(registerExtension);
        registerExtension("inspector." + name, callback);
    }

    /// <summary>Registers a service extension method that takes no arguments.</summary>
    private void RegisterSignalServiceExtension(
        string name,
        Func<Task<object?>> callback,
        RegisterServiceExtensionCallback registerExtension)
    {
        RegisterServiceExtension(
            name,
            async parameters => new Dictionary<string, object?> { ["result"] = await callback().ConfigureAwait(true) },
            registerExtension);
    }

    /// <summary>Registers a service extension method that takes a required <c>objectGroup</c>.</summary>
    private void RegisterObjectGroupServiceExtension(
        string name,
        Func<string, Task<object?>> callback,
        RegisterServiceExtensionCallback registerExtension)
    {
        RegisterServiceExtension(
            name,
            async parameters => new Dictionary<string, object?>
            {
                ["result"] = await callback(parameters["objectGroup"]).ConfigureAwait(true),
            },
            registerExtension);
    }

    /// <summary>
    /// Registers a service extension that reads and, given an <c>enabled</c> argument, writes a
    /// boolean.
    /// </summary>
    /// <remarks>
    /// Flutter's private <c>_registerBoolServiceExtension</c>: every call carrying <c>enabled</c> posts
    /// a state-changed event with the requested value, even when nothing changes.
    /// </remarks>
    private void RegisterBoolServiceExtension(
        string name,
        Func<Task<bool>> getter,
        Func<bool, Task> setter,
        RegisterServiceExtensionCallback registerExtension)
    {
        RegisterServiceExtension(
            name,
            async parameters =>
            {
                if (parameters.TryGetValue("enabled", out string? enabled))
                {
                    bool value = enabled == "true";
                    await setter(value).ConfigureAwait(true);
                    PostExtensionStateChangedEvent(name, value);
                }

                return new Dictionary<string, object?>
                {
                    ["enabled"] = await getter().ConfigureAwait(true) ? "true" : "false",
                };
            },
            registerExtension);
    }

    /// <summary>Sends an event when a service extension's state is changed.</summary>
    private void PostExtensionStateChangedEvent(string name, object? value)
    {
        PostEvent(
            "Flutter.ServiceExtensionStateChanged",
            new Dictionary<string, object?>
            {
                ["extension"] = "ext.flutter.inspector." + name,
                ["value"] = value,
            });
    }

    /// <summary>Registers a service extension method that takes an optional <c>arg</c>.</summary>
    private void RegisterServiceExtensionWithArg(
        string name,
        Func<string?, string, Task<object?>> callback,
        RegisterServiceExtensionCallback registerExtension)
    {
        RegisterServiceExtension(
            name,
            async parameters =>
            {
                Debug.Assert(parameters.ContainsKey("objectGroup"));
                return new Dictionary<string, object?>
                {
                    ["result"] = await callback(
                        parameters.GetValueOrDefault("arg"),
                        parameters["objectGroup"]).ConfigureAwait(true),
                };
            },
            registerExtension);
    }

    /// <summary>Registers a service extension that takes <c>arg0</c>, <c>arg1</c>, ... arguments.</summary>
    private void RegisterServiceExtensionVarArgs(
        string name,
        Func<List<string>, Task<object?>> callback,
        RegisterServiceExtensionCallback registerExtension)
    {
        RegisterServiceExtension(
            name,
            async parameters =>
            {
                int index;
                var args = new List<string>();
                for (index = 0; parameters.TryGetValue("arg" + index, out string? arg); index++)
                {
                    args.Add(arg);
                }

                // Verify that the only arguments other than perhaps 'isolateId' are
                // arguments we have already handled.
                Debug.Assert(index == parameters.Count
                    || (index == parameters.Count - 1 && parameters.ContainsKey("isolateId")));
                return new Dictionary<string, object?> { ["result"] = await callback(args).ConfigureAwait(true) };
            },
            registerExtension);
    }

    /// <summary>Cause the entire tree to be rebuilt.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetInspectorService.forceRebuild</c>. This is used by development tools when
    /// the application code has changed and is being hot-reloaded, to cause the widget tree to pick
    /// up any changed implementations.
    /// </remarks>
    protected virtual Task ForceRebuild()
    {
        WidgetsBinding binding = WidgetsBinding.Instance;
        if (binding.RootElement is { } rootElement)
        {
            binding.BuildOwner.Reassemble(rootElement);
            return Scheduler.EndOfFrame;
        }

        return Task.CompletedTask;
    }

    /// <remarks>Flutter's private <c>_reportStructuredError</c>.</remarks>
    private void ReportStructuredError(FlutterErrorDetails details)
    {
        Dictionary<string, object?> errorJson = NodeToJson(
            details.ToDiagnosticsNode(),
            new InspectorSerializationDelegate(
                groupName: ConsoleObjectGroup,
                subtreeDepth: 5,
                includeProperties: true,
                maxDescendantsTruncatableNode: 5,
                service: this))!;

        errorJson["errorsSinceReload"] = _errorsSinceReload;
        if (_errorsSinceReload == 0)
        {
            errorJson["renderedErrorText"] = new TextTreeRenderer(
                    wrapWidthProperties: FlutterError.WrapWidth,
                    maxDescendentsTruncatableNode: 5)
                .Render(details.ToDiagnosticsNode(style: DiagnosticsTreeStyle.Error))
                .TrimEnd();
        }
        else
        {
            errorJson["renderedErrorText"] = "Another exception was thrown: " + details.Summary;
        }

        _errorsSinceReload += 1;
        PostEvent("Flutter.Error", errorJson);
    }

    /// <summary>Resets the count of errors since the last hot reload.</summary>
    /// <remarks>
    /// Flutter's private <c>_resetErrorCount</c>. This data is sent to clients as part of the
    /// <c>Flutter.Error</c> service protocol event.
    /// </remarks>
    private void ResetErrorCount()
    {
        _errorsSinceReload = 0;
    }

    /// <summary>Whether structured errors are enabled.</summary>
    /// <remarks>
    /// Flutter's <c>isStructuredErrorsEnabled</c>, which reads the compile-time define
    /// <c>flutter.inspector.structuredErrors</c> (default <c>!kIsWeb</c>) in debug builds. Plumix reads
    /// the <see cref="AppContext"/> switch of that name and defaults it to false: structured errors
    /// replace the console dump with a <c>Flutter.Error</c> event that only a connected tool renders,
    /// and no tool consumes them yet (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public bool IsStructuredErrorsEnabled()
    {
        // This is a debug mode only feature and will default to false for
        // profile mode.
        bool enabled = false;
        if (Constants.KDebugMode)
        {
            enabled = AppContext.TryGetSwitch("flutter.inspector.structuredErrors", out bool value) && value;
        }

        return enabled;
    }

    /// <summary>Called to register service extensions.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetInspectorService.initServiceExtensions</c>; <see cref="WidgetsBinding"/>
    /// calls it in debug builds.
    /// </remarks>
    public void InitServiceExtensions(RegisterServiceExtensionCallback registerExtension)
    {
        ArgumentNullException.ThrowIfNull(registerExtension);
        FlutterExceptionHandler defaultExceptionHandler = FlutterError.PresentError;

        if (IsStructuredErrorsEnabled())
        {
            FlutterError.PresentError = ReportStructuredError;
        }

        Debug.Assert(!_debugServiceExtensionsRegistered);
        if (Constants.KDebugMode)
        {
            _debugServiceExtensionsRegistered = true;
        }

        Scheduler.AddPersistentFrameCallback(OnFrameStart);

        RegisterBoolServiceExtension(
            WidgetInspectorServiceExtensions.StructuredErrors.DartName(),
            () => Task.FromResult(FlutterError.PresentError == (FlutterExceptionHandler)ReportStructuredError),
            value =>
            {
                FlutterError.PresentError = value ? ReportStructuredError : defaultExceptionHandler;
                return Task.CompletedTask;
            },
            registerExtension);

        RegisterBoolServiceExtension(
            WidgetInspectorServiceExtensions.Show.DartName(),
            static () => Task.FromResult(WidgetsBinding.Instance.DebugShowWidgetInspectorOverride),
            value =>
            {
                if (WidgetsBinding.Instance.DebugShowWidgetInspectorOverride != value)
                {
                    ChangeWidgetSelectionMode(value, notifyStateChange: false);
                }

                return Task.CompletedTask;
            },
            registerExtension);

        if (IsWidgetCreationTracked())
        {
            // Service extensions that are only supported if widget creation locations
            // are tracked.
            RegisterBoolServiceExtension(
                WidgetInspectorServiceExtensions.TrackRebuildDirtyWidgets.DartName(),
                () => Task.FromResult(_trackRebuildDirtyWidgets),
                async value =>
                {
                    if (value == _trackRebuildDirtyWidgets)
                    {
                        return;
                    }

                    _rebuildStats.ResetCounts();
                    _trackRebuildDirtyWidgets = value;
                    if (value)
                    {
                        Debug.Assert(WidgetsDebug.DebugOnRebuildDirtyWidget is null);
                        WidgetsDebug.DebugOnRebuildDirtyWidget = OnRebuildWidget;
                        // Trigger a rebuild so there are baseline stats for rebuilds
                        // performed by the app.
                        await ForceRebuild().ConfigureAwait(true);
                        return;
                    }

                    WidgetsDebug.DebugOnRebuildDirtyWidget = null;
                },
                registerExtension);

            RegisterSignalServiceExtension(
                WidgetInspectorServiceExtensions.WidgetLocationIdMap.DartName(),
                static () => Task.FromResult<object?>(WidgetInspectorDebug.LocationIdMapToJson()),
                registerExtension);

            RegisterBoolServiceExtension(
                WidgetInspectorServiceExtensions.TrackRepaintWidgets.DartName(),
                () => Task.FromResult(_trackRepaintWidgets),
                value =>
                {
                    if (value == _trackRepaintWidgets)
                    {
                        return Task.CompletedTask;
                    }

                    _repaintStats.ResetCounts();
                    _trackRepaintWidgets = value;
                    if (value)
                    {
                        Debug.Assert(RenderingDebug.OnProfilePaint is null);
                        RenderingDebug.OnProfilePaint = OnPaint;
                        // Trigger an immediate paint so the user has some baseline painting
                        // stats to view.
                        static void MarkTreeNeedsPaint(RenderObject renderObject)
                        {
                            renderObject.MarkNeedsPaint();
                            renderObject.VisitChildren(MarkTreeNeedsPaint);
                        }

                        foreach (RenderView renderView in RendererBinding.Instance.RenderViews)
                        {
                            MarkTreeNeedsPaint(renderView);
                        }
                    }
                    else
                    {
                        RenderingDebug.OnProfilePaint = null;
                    }

                    return Task.CompletedTask;
                },
                registerExtension);
        }

        RegisterSignalServiceExtension(
            WidgetInspectorServiceExtensions.DisposeAllGroups.DartName(),
            () =>
            {
                DisposeAllGroups();
                return Task.FromResult<object?>(null);
            },
            registerExtension);
        RegisterObjectGroupServiceExtension(
            WidgetInspectorServiceExtensions.DisposeGroup.DartName(),
            name =>
            {
                DisposeGroup(name);
                return Task.FromResult<object?>(null);
            },
            registerExtension);
        RegisterSignalServiceExtension(
            WidgetInspectorServiceExtensions.IsWidgetTreeReady.DartName(),
            () => Task.FromResult<object?>(IsWidgetTreeReady()),
            registerExtension);
        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.DisposeId.DartName(),
            (objectId, objectGroup) =>
            {
                DisposeId(objectId, objectGroup);
                return Task.FromResult<object?>(null);
            },
            registerExtension);
        RegisterServiceExtensionVarArgs(
#pragma warning disable CS0618 // Dart registers the deprecated extension too.
            WidgetInspectorServiceExtensions.SetPubRootDirectories.DartName(),
            args =>
            {
                SetPubRootDirectories(args);
#pragma warning restore CS0618
                return Task.FromResult<object?>(null);
            },
            registerExtension);
        RegisterServiceExtensionVarArgs(
            WidgetInspectorServiceExtensions.AddPubRootDirectories.DartName(),
            args =>
            {
                AddPubRootDirectories(args);
                return Task.FromResult<object?>(null);
            },
            registerExtension);
        RegisterServiceExtensionVarArgs(
            WidgetInspectorServiceExtensions.RemovePubRootDirectories.DartName(),
            args =>
            {
                RemovePubRootDirectories(args);
                return Task.FromResult<object?>(null);
            },
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.GetPubRootDirectories.DartName(),
            PubRootDirectories,
            registerExtension);
        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.SetSelectionById.DartName(),
            (objectId, objectGroup) => Task.FromResult<object?>(SetSelectionById(objectId, objectGroup)),
            registerExtension);
        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.GetParentChain.DartName(),
            (objectId, objectGroup) => Task.FromResult<object?>(ParentChainJson(objectId, objectGroup)),
            registerExtension);
        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.GetProperties.DartName(),
            (objectId, objectGroup) => Task.FromResult<object?>(PropertiesJson(objectId, objectGroup)),
            registerExtension);
        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.GetChildren.DartName(),
            (objectId, objectGroup) => Task.FromResult<object?>(ChildrenJson(objectId, objectGroup)),
            registerExtension);

        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.GetChildrenSummaryTree.DartName(),
            (objectId, objectGroup) => Task.FromResult<object?>(ChildrenSummaryTreeJson(objectId, objectGroup)),
            registerExtension);

        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.GetChildrenDetailsSubtree.DartName(),
            (objectId, objectGroup) => Task.FromResult<object?>(ChildrenDetailsSubtreeJson(objectId, objectGroup)),
            registerExtension);

        RegisterObjectGroupServiceExtension(
            WidgetInspectorServiceExtensions.GetRootWidget.DartName(),
            name => Task.FromResult<object?>(RootWidgetJson(name)),
            registerExtension);
        RegisterObjectGroupServiceExtension(
            WidgetInspectorServiceExtensions.GetRootWidgetSummaryTree.DartName(),
            name => Task.FromResult<object?>(RootWidgetSummaryTreeJson(name)),
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.GetRootWidgetSummaryTreeWithPreviews.DartName(),
            RootWidgetSummaryTreeWithPreviewsJson,
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.GetRootWidgetTree.DartName(),
            RootWidgetTreeJson,
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.GetDetailsSubtree.DartName(),
            parameters =>
            {
                Debug.Assert(parameters.ContainsKey("objectGroup"));
                string? subtreeDepth = parameters.GetValueOrDefault("subtreeDepth");
                return Task.FromResult(new Dictionary<string, object?>
                {
                    ["result"] = DetailsSubtreeJson(
                        parameters.GetValueOrDefault("arg"),
                        parameters.GetValueOrDefault("objectGroup"),
                        subtreeDepth is not null ? int.Parse(subtreeDepth, CultureInfo.InvariantCulture) : 2),
                });
            },
            registerExtension);
        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.GetSelectedWidget.DartName(),
            (objectId, objectGroup) => Task.FromResult<object?>(SelectedWidgetJson(objectId, objectGroup)),
            registerExtension);
        RegisterServiceExtensionWithArg(
            WidgetInspectorServiceExtensions.GetSelectedSummaryWidget.DartName(),
            (objectId, objectGroup) => Task.FromResult<object?>(SelectedSummaryWidgetJson(objectId, objectGroup)),
            registerExtension);

        RegisterSignalServiceExtension(
            WidgetInspectorServiceExtensions.IsWidgetCreationTracked.DartName(),
            () => Task.FromResult<object?>(IsWidgetCreationTracked()),
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.Screenshot.DartName(),
            async parameters =>
            {
                Debug.Assert(parameters.ContainsKey("id"));
                Debug.Assert(parameters.ContainsKey("width"));
                Debug.Assert(parameters.ContainsKey("height"));

                Bitmap? image = await Screenshot(
                    ToObject(parameters.GetValueOrDefault("id")),
                    width: ParseDouble(parameters["width"]),
                    height: ParseDouble(parameters["height"]),
                    margin: parameters.TryGetValue("margin", out string? margin) ? ParseDouble(margin) : 0.0,
                    maxPixelRatio: parameters.TryGetValue("maxPixelRatio", out string? maxPixelRatio)
                        ? ParseDouble(maxPixelRatio)
                        : 1.0,
                    debugPaint: parameters.GetValueOrDefault("debugPaint") == "true").ConfigureAwait(true);
                if (image is null)
                {
                    return new Dictionary<string, object?> { ["result"] = null };
                }

                using var stream = new MemoryStream();
                image.Save(stream);
                image.Dispose();

                return new Dictionary<string, object?> { ["result"] = Convert.ToBase64String(stream.ToArray()) };
            },
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.GetLayoutExplorerNode.DartName(),
            LayoutExplorerNode,
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.SetFlexFit.DartName(),
            SetFlexFit,
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.SetFlexFactor.DartName(),
            SetFlexFactor,
            registerExtension);
        RegisterServiceExtension(
            WidgetInspectorServiceExtensions.SetFlexProperties.DartName(),
            SetFlexProperties,
            registerExtension);
    }

    private static double ParseDouble(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <remarks>Flutter's private <c>_clearStats</c>.</remarks>
    private void ClearStats()
    {
        _rebuildStats.ResetCounts();
        _repaintStats.ResetCounts();
    }

    /// <summary>Clear all InspectorService object references.</summary>
    /// <remarks>
    /// Use this method only for testing to ensure that object references from one test case do not
    /// impact other test cases.
    /// </remarks>
    protected internal void DisposeAllGroups()
    {
        _groups.Clear();
        _idToReferenceData.Clear();
        _objectToId.Clear();
        _nextId = 0;
    }

    /// <summary>Reset all InspectorService state.</summary>
    /// <remarks>
    /// Use this method only for testing to write hermetic tests for <see cref="WidgetInspectorService"/>.
    /// </remarks>
    protected internal virtual void ResetAllState()
    {
        DisposeAllGroups();
        Selection.Clear();
        ResetPubRootDirectories();
    }

    /// <summary>Free all references to objects in a group.</summary>
    /// <remarks>Objects and their associated ids in the group may be kept alive by references from a
    /// different group.</remarks>
    protected internal void DisposeGroup(string name)
    {
        if (!_groups.Remove(name, out HashSet<InspectorReferenceData>? references))
        {
            return;
        }

        foreach (InspectorReferenceData reference in references)
        {
            DecrementReferenceCount(reference);
        }
    }

    private void DecrementReferenceCount(InspectorReferenceData reference)
    {
        reference.Count -= 1;
        Debug.Assert(reference.Count >= 0);
        if (reference.Count == 0)
        {
            object? value = reference.Value;
            if (value is not null)
            {
                _objectToId.Remove(value);
            }

            _idToReferenceData.Remove(reference.Id);
        }
    }

    /// <summary>Returns a unique id for <paramref name="object"/> that will remain live at least until
    /// <see cref="DisposeGroup"/> is called on <paramref name="groupName"/>.</summary>
    protected internal string? ToId(object? @object, string groupName)
    {
        if (@object is null)
        {
            return null;
        }

        if (!_groups.TryGetValue(groupName, out HashSet<InspectorReferenceData>? group))
        {
            group = new HashSet<InspectorReferenceData>(ReferenceEqualityComparer.Instance);
            _groups[groupName] = group;
        }

        string? id = _objectToId[@object];
        InspectorReferenceData referenceData;
        if (id is null)
        {
            // TODO(polina-c): comment here why we increase memory footprint by the prefix 'inspector-'.
            // https://github.com/flutter/devtools/issues/5995
            id = "inspector-" + _nextId.ToString(CultureInfo.InvariantCulture);
            _nextId += 1;
            _objectToId[@object] = id;
            referenceData = new InspectorReferenceData(@object, id);
            _idToReferenceData[id] = referenceData;
            group.Add(referenceData);
        }
        else
        {
            referenceData = _idToReferenceData[id];
            if (group.Add(referenceData))
            {
                referenceData.Count += 1;
            }
        }

        return id;
    }

    /// <summary>Returns whether the application has rendered its first frame and it is appropriate
    /// to display the Widget tree in the inspector.</summary>
    protected internal bool IsWidgetTreeReady(string? groupName = null)
    {
        return WidgetsBinding.Instance.DebugDidSendFirstFrameEvent;
    }

    /// <summary>Returns the Dart object associated with a reference id.</summary>
    /// <remarks>The <paramref name="groupName"/> parameter is not required by is added to regularize
    /// the API surface of the methods in this class called from the Flutter IntelliJ Plugin.</remarks>
    protected internal object? ToObject(string? id, string? groupName = null)
    {
        if (id is null)
        {
            return null;
        }

        if (!_idToReferenceData.TryGetValue(id, out InspectorReferenceData? data))
        {
            throw new FlutterError([new ErrorSummary("Id does not exist.")]);
        }

        return data.Value;
    }

    /// <summary>Returns the object to introspect to determine the source location of an object's class.</summary>
    /// <remarks>The Dart object for the id is returned for all cases but <see cref="Element"/> objects
    /// where the <see cref="Widget"/> configuring the <see cref="Element"/> is returned instead.</remarks>
    protected internal object? ToObjectForSourceLocation(string id, string? groupName = null)
    {
        object? @object = ToObject(id);
        if (@object is Element element)
        {
            return element.Widget;
        }

        return @object;
    }

    /// <summary>Remove the object with the specified <paramref name="id"/> from the specified object group.</summary>
    /// <remarks>If the object exists in other groups it will remain alive and the object id will
    /// remain valid.</remarks>
    protected internal void DisposeId(string? id, string groupName)
    {
        if (id is null)
        {
            return;
        }

        if (!_idToReferenceData.TryGetValue(id, out InspectorReferenceData? referenceData))
        {
            throw new FlutterError([new ErrorSummary("Id does not exist")]);
        }

        if (_groups.GetValueOrDefault(groupName)?.Remove(referenceData) != true)
        {
            throw new FlutterError([new ErrorSummary("Id is not in group")]);
        }

        DecrementReferenceCount(referenceData);
    }

    /// <summary>Set the list of directories that should be considered part of the local project.</summary>
    [Obsolete("Use AddPubRootDirectories instead. This feature was deprecated after v3.18.0-2.0.pre.")]
    protected internal void SetPubRootDirectories(List<string> pubRootDirectories)
    {
        AddPubRootDirectories(pubRootDirectories);
    }

    /// <summary>Resets the list of directories, that should be considered part of the local project,
    /// to the value passed in <see cref="AddPubRootDirectories"/>.</summary>
    protected internal void ResetPubRootDirectories()
    {
        _pubRootDirectories = [];
        _isLocalCreationCache.Clear();
    }

    /// <summary>Add a list of directories that should be considered part of the local project.</summary>
    /// <remarks>The local project directories are used to distinguish widgets created by the local
    /// project from widgets created from inside the framework or other packages.</remarks>
    protected internal void AddPubRootDirectories(List<string> pubRootDirectories)
    {
        List<string> mapped = pubRootDirectories.Select(UriPath).ToList();
        var directorySet = new List<string>();
        foreach (string directory in mapped)
        {
            if (!directorySet.Contains(directory))
            {
                directorySet.Add(directory);
            }
        }

        if (_pubRootDirectories is not null)
        {
            foreach (string directory in _pubRootDirectories)
            {
                if (!directorySet.Contains(directory))
                {
                    directorySet.Add(directory);
                }
            }
        }

        _pubRootDirectories = directorySet;
        _isLocalCreationCache.Clear();
    }

    /// <summary>Remove a list of directories that should no longer be considered part of the local
    /// project.</summary>
    protected internal void RemovePubRootDirectories(List<string> pubRootDirectories)
    {
        if (_pubRootDirectories is null)
        {
            return;
        }

        var mapped = pubRootDirectories.Select(UriPath).ToHashSet();
        var directorySet = new List<string>();
        foreach (string directory in _pubRootDirectories)
        {
            if (!directorySet.Contains(directory))
            {
                directorySet.Add(directory);
            }
        }

        directorySet.RemoveAll(mapped.Contains);
        _pubRootDirectories = directorySet;
        _isLocalCreationCache.Clear();
    }

    /// <summary>Returns the list of directories that should be considered part of the local project.</summary>
    protected internal Task<Dictionary<string, object?>> PubRootDirectories(
        IReadOnlyDictionary<string, string> parameters)
    {
        return Task.FromResult(new Dictionary<string, object?>
        {
            ["result"] = _pubRootDirectories is null ? new List<string>() : new List<string>(_pubRootDirectories),
        });
    }

    /// <summary>Dart's <c>Uri.parse(directory).path</c>.</summary>
    private static string UriPath(string uri)
    {
        if (uri.StartsWith("file://", StringComparison.Ordinal))
        {
            return uri["file://".Length..];
        }

        int schemeEnd = uri.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0 && uri[..schemeEnd].All(char.IsLetter))
        {
            string rest = uri[(schemeEnd + 3)..];
            int slash = rest.IndexOf('/', StringComparison.Ordinal);
            return slash < 0 ? string.Empty : rest[slash..];
        }

        int query = uri.IndexOfAny(['?', '#']);
        return query < 0 ? uri : uri[..query];
    }

    /// <summary>Set the <see cref="WidgetInspector"/> selection to the object matching the specified id
    /// if the object is valid object to set as the inspector selection.</summary>
    /// <returns>True if the selection was changed.</returns>
    protected internal bool SetSelectionById(string? id, string? groupName = null)
    {
        return SetSelection(ToObject(id), groupName);
    }

    /// <summary>Set the <see cref="WidgetInspector"/> selection to the specified
    /// <paramref name="object"/> if it is a valid object to set as the inspector selection.</summary>
    /// <returns>True if the selection was changed.</returns>
    protected internal bool SetSelection(object? @object, string? groupName = null)
    {
        switch (@object)
        {
            case Element element when !ReferenceEquals(element, Selection.CurrentElement):
                Selection.ClearCandidates();
                Selection.CurrentElement = element;
                NotifyToolsOfSelection(Selection.CurrentElement);
                return true;
            case RenderObject renderObject when !ReferenceEquals(renderObject, Selection.Current):
                Selection.ClearCandidates();
                Selection.Current = renderObject;
                NotifyToolsOfSelection(Selection.Current);
                return true;
        }

        return false;
    }

    /// <summary>Notify attached tools to navigate to an object's source location.</summary>
    /// <remarks>Flutter's private <c>_notifyToolsOfSelection</c>.</remarks>
    internal void NotifyToolsOfSelection(object? @object, bool restrictToProjectFiles = false)
    {
        Inspect(@object);

        InspectorLocation? location = GetSelectedWidgetLocation(restrictToSummaryTree: restrictToProjectFiles);
        if (location is not null)
        {
            PostEvent(
                "navigate",
                new Dictionary<string, object?>
                {
                    ["fileUri"] = location.File,
                    ["line"] = location.Line,
                    ["column"] = location.Column,
                    ["source"] = "flutter.inspector",
                },
                stream: "ToolEvent");
        }
    }

    /// <summary>Changes whether widget selection mode is enabled.</summary>
    /// <remarks>Flutter's private <c>_changeWidgetSelectionMode</c>.</remarks>
    internal void ChangeWidgetSelectionMode(bool enabled, bool notifyStateChange = true)
    {
        WidgetsBinding.Instance.DebugShowWidgetInspectorOverride = enabled;
        if (notifyStateChange)
        {
            PostExtensionStateChangedEvent(WidgetInspectorServiceExtensions.Show.DartName(), enabled);
        }

        if (!enabled)
        {
            Selection.CurrentElement = null;
        }
    }

    /// <summary>Returns a DevTools uri linking to a specific element on the inspector page.</summary>
    /// <remarks>Flutter's private <c>_devToolsInspectorUriForElement</c>.</remarks>
    internal string? DevToolsInspectorUriForElement(Element element)
    {
        if (FoundationDebug.ActiveDevToolsServerAddress is not null
            && FoundationDebug.ConnectedVmServiceUri is not null)
        {
            string? inspectorRef = ToId(element, ConsoleObjectGroup);
            if (inspectorRef is not null)
            {
                return DevToolsInspectorUri(inspectorRef);
            }
        }

        return null;
    }

    /// <summary>Returns the DevTools inspector uri for the given vm service connection and inspector
    /// reference.</summary>
    public string DevToolsInspectorUri(string inspectorRef)
    {
        Debug.Assert(FoundationDebug.ActiveDevToolsServerAddress is not null);
        Debug.Assert(FoundationDebug.ConnectedVmServiceUri is not null);

        string uriString = FoundationDebug.ActiveDevToolsServerAddress!
            + "?uri=" + Uri.EscapeDataString(FoundationDebug.ConnectedVmServiceUri!)
            + "&inspectorRef=" + Uri.EscapeDataString(inspectorRef);
        int startQueryParamIndex = uriString.IndexOf('?', StringComparison.Ordinal);
        // The query parameter is lifted to after the DevTools page fragment.
        Debug.Assert(startQueryParamIndex != -1);
        return uriString[..startQueryParamIndex] + "/#/inspector" + uriString[startQueryParamIndex..];
    }

    /// <summary>Returns JSON representing the chain of <see cref="DiagnosticsNode"/> instances from the
    /// root of the tree to the <see cref="Element"/> or <see cref="RenderObject"/> matching
    /// <paramref name="id"/>.</summary>
    protected internal string GetParentChain(string id, string groupName)
    {
        return SafeJsonEncode(ParentChainJson(id, groupName));
    }

    /// <remarks>Flutter's private <c>_getParentChain</c>.</remarks>
    private List<object?> ParentChainJson(string? id, string groupName)
    {
        object? value = ToObject(id);
        List<InspectorDiagnosticsPathNode> path = value switch
        {
            RenderObject renderObject => GetRenderObjectParentChain(renderObject, groupName)!,
            Element element => GetElementParentChain(element, groupName),
            _ => throw new FlutterError(
            [
                new ErrorSummary($"Cannot get parent chain for node of type {value?.GetType().Name ?? "Null"}"),
            ]),
        };

        var result = new List<object?>();
        foreach (InspectorDiagnosticsPathNode pathNode in path)
        {
            var serializationDelegate = new InspectorSerializationDelegate(groupName: groupName, service: this);
            result.Add(new Dictionary<string, object?>
            {
                ["node"] = NodeToJson(pathNode.Node, serializationDelegate),
                ["children"] = NodesToJson(pathNode.Children, serializationDelegate, parent: pathNode.Node),
                ["childIndex"] = pathNode.ChildIndex,
            });
        }

        return result;
    }

    /// <remarks>Flutter's private <c>_getRawElementParentChain</c>.</remarks>
    private List<Element> GetRawElementParentChain(Element element, int? numLocalParents)
    {
        List<Element> elements = element.DebugGetDiagnosticChain();
        if (numLocalParents is not null)
        {
            for (int i = 0; i < elements.Count; i += 1)
            {
                if (IsValueCreatedByLocalProject(elements[i]))
                {
                    numLocalParents = numLocalParents - 1;
                    if (numLocalParents <= 0)
                    {
                        elements = elements.Take(i + 1).ToList();
                        break;
                    }
                }
            }
        }

        elements.Reverse();
        return elements;
    }

    /// <remarks>Flutter's private <c>_getElementParentChain</c>.</remarks>
    private List<InspectorDiagnosticsPathNode> GetElementParentChain(
        Element element,
        string groupName,
        int? numLocalParents = null)
    {
        return FollowDiagnosticableChain(
            GetRawElementParentChain(element, numLocalParents).Cast<IDiagnosticable>().ToList());
    }

    /// <remarks>Flutter's private <c>_getRenderObjectParentChain</c>.</remarks>
    private List<InspectorDiagnosticsPathNode>? GetRenderObjectParentChain(RenderObject? renderObject, string groupName)
    {
        var chain = new List<IDiagnosticable>();
        while (renderObject is not null)
        {
            chain.Add(renderObject);
            renderObject = renderObject.Parent;
        }

        chain.Reverse();
        return FollowDiagnosticableChain(chain);
    }

    /// <remarks>Flutter's private <c>_nodeToJson</c>.</remarks>
    private static Dictionary<string, object?>? NodeToJson(
        DiagnosticsNode? node,
        InspectorSerializationDelegate serializationDelegate,
        bool fullDetails = true)
    {
        if (node is null)
        {
            return null;
        }

        return fullDetails ? node.ToJsonMap(serializationDelegate) : node.ToJsonMapIterative(serializationDelegate);
    }

    /// <remarks>Flutter's private <c>_isValueCreatedByLocalProject</c>.</remarks>
    private bool IsValueCreatedByLocalProject(object? value)
    {
        InspectorLocation? creationLocation = WidgetInspectorDebug.GetCreationLocation(value);
        if (creationLocation is null)
        {
            return false;
        }

        return IsLocalCreationLocation(creationLocation.File);
    }

    /// <remarks>Flutter's private <c>_isLocalCreationLocationImpl</c>.</remarks>
    private bool IsLocalCreationLocationImpl(string locationUri)
    {
        string file = UriPath(locationUri);

        // By default check whether the creation location was within package:flutter.
        if (_pubRootDirectories is null)
        {
            // TODO(chunhtai): Make it more robust once
            // https://github.com/flutter/flutter/issues/32660 is fixed.
            return !file.Contains("packages/flutter/", StringComparison.Ordinal);
        }

        foreach (string directory in _pubRootDirectories)
        {
            if (file.StartsWith(directory, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns whether a <paramref name="locationUri"/> belongs to the local project.</summary>
    /// <remarks>Flutter's private <c>_isLocalCreationLocation</c>, memoized.</remarks>
    internal bool IsLocalCreationLocation(string locationUri)
    {
        if (_isLocalCreationCache.TryGetValue(locationUri, out bool cachedValue))
        {
            return cachedValue;
        }

        bool result = IsLocalCreationLocationImpl(locationUri);
        _isLocalCreationCache[locationUri] = result;
        return result;
    }

    /// <summary>Wrapper around <c>json.encode</c> that uses a ring of cached values to prevent the
    /// Dart garbage collector from collecting objects between when the value is returned over the VM
    /// service protocol and when the client takes the value.</summary>
    private string SafeJsonEncode(object? @object)
    {
        string jsonString = JsonSerializer.Serialize(@object);
        _serializeRing[_serializeRingIndex] = jsonString;
        _serializeRingIndex = (_serializeRingIndex + 1) % _serializeRing.Length;
        return jsonString;
    }

    /// <remarks>Flutter's private <c>_truncateNodes</c>.</remarks>
    internal List<DiagnosticsNode> TruncateNodes(IEnumerable<DiagnosticsNode> nodes, int maxDescendentsTruncatableNode)
    {
        List<DiagnosticsNode> list = nodes.ToList();
        if (list.All(node => node.Value is Element) && IsWidgetCreationTracked())
        {
            List<DiagnosticsNode> localNodes = list.Where(node => IsValueCreatedByLocalProject(node.Value)).ToList();
            if (localNodes.Count > 0)
            {
                return localNodes;
            }
        }

        return list.Take(maxDescendentsTruncatableNode).ToList();
    }

    /// <remarks>Flutter's private <c>_nodesToJson</c>.</remarks>
    private static List<Dictionary<string, object?>> NodesToJson(
        List<DiagnosticsNode> nodes,
        InspectorSerializationDelegate serializationDelegate,
        DiagnosticsNode? parent)
    {
        return DiagnosticsNode.ToJsonList(nodes, parent, serializationDelegate);
    }

    /// <summary>Returns a JSON representation of the properties of the <see cref="DiagnosticsNode"/>
    /// object that <paramref name="diagnosticsNodeId"/> references.</summary>
    protected internal string GetProperties(string diagnosticsNodeId, string groupName)
    {
        return SafeJsonEncode(PropertiesJson(diagnosticsNodeId, groupName));
    }

    private List<Dictionary<string, object?>> PropertiesJson(string? diagnosticableId, string groupName)
    {
        DiagnosticsNode? node = IdToDiagnosticsNode(diagnosticableId);
        if (node is null)
        {
            return [];
        }

        return NodesToJson(
            node.GetProperties(),
            new InspectorSerializationDelegate(groupName: groupName, service: this),
            parent: node);
    }

    /// <summary>Returns a JSON representation of the children of the <see cref="DiagnosticsNode"/>
    /// object that <paramref name="diagnosticsNodeId"/> references.</summary>
    public string GetChildren(string diagnosticsNodeId, string groupName)
    {
        return SafeJsonEncode(ChildrenJson(diagnosticsNodeId, groupName));
    }

    private List<Dictionary<string, object?>> ChildrenJson(string? diagnosticsNodeId, string groupName)
    {
        var node = ToObject(diagnosticsNodeId) as DiagnosticsNode;
        var serializationDelegate = new InspectorSerializationDelegate(groupName: groupName, service: this);
        return NodesToJson(
            node is null ? [] : GetChildrenFiltered(node, serializationDelegate),
            serializationDelegate,
            parent: node);
    }

    /// <summary>Returns a JSON representation of the children of the <see cref="DiagnosticsNode"/>
    /// object that <paramref name="diagnosticsNodeId"/> references only including children that were
    /// created directly by user code.</summary>
    public string GetChildrenSummaryTree(string diagnosticsNodeId, string groupName)
    {
        return SafeJsonEncode(ChildrenSummaryTreeJson(diagnosticsNodeId, groupName));
    }

    private DiagnosticsNode? IdToDiagnosticsNode(string? diagnosticableId)
    {
        object? @object = ToObject(diagnosticableId);
        return ObjectToDiagnosticsNode(@object);
    }

    /// <summary>If possible, returns <see cref="DiagnosticsNode"/> for the object.</summary>
    public static DiagnosticsNode? ObjectToDiagnosticsNode(object? @object)
    {
        if (@object is IDiagnosticable diagnosticable)
        {
            return diagnosticable.ToDiagnosticsNode();
        }

        return null;
    }

    private List<Dictionary<string, object?>> ChildrenSummaryTreeJson(string? diagnosticableId, string groupName)
    {
        DiagnosticsNode? node = IdToDiagnosticsNode(diagnosticableId);
        if (node is null)
        {
            return [];
        }

        var serializationDelegate = new InspectorSerializationDelegate(
            groupName: groupName,
            summaryTree: true,
            service: this);
        return NodesToJson(GetChildrenFiltered(node, serializationDelegate), serializationDelegate, parent: node);
    }

    /// <summary>Returns a JSON representation of the children of the <see cref="DiagnosticsNode"/>
    /// object that <paramref name="diagnosticableId"/> references providing information needed for the
    /// details subtree view.</summary>
    public string GetChildrenDetailsSubtree(string diagnosticableId, string groupName)
    {
        return SafeJsonEncode(ChildrenDetailsSubtreeJson(diagnosticableId, groupName));
    }

    private List<Dictionary<string, object?>> ChildrenDetailsSubtreeJson(string? diagnosticableId, string groupName)
    {
        DiagnosticsNode? node = IdToDiagnosticsNode(diagnosticableId);
        // With this value of minDepth we only expand one extra level of important nodes.
        var serializationDelegate = new InspectorSerializationDelegate(
            groupName: groupName,
            includeProperties: true,
            service: this);
        return NodesToJson(
            node is null ? [] : GetChildrenFiltered(node, serializationDelegate),
            serializationDelegate,
            parent: node);
    }

    /// <remarks>Flutter's private <c>_shouldShowInSummaryTree</c>.</remarks>
    internal bool ShouldShowInSummaryTree(DiagnosticsNode node)
    {
        if (node.Level == DiagnosticLevel.Error)
        {
            return true;
        }

        object? value = node.Value;
        if (value is not IDiagnosticable)
        {
            return true;
        }

        if (value is not Element || !IsWidgetCreationTracked())
        {
            // Creation locations are not available so include all nodes in the
            // summary tree.
            return true;
        }

        return IsValueCreatedByLocalProject(value);
    }

    private List<DiagnosticsNode> GetChildrenFiltered(
        DiagnosticsNode node,
        InspectorSerializationDelegate serializationDelegate)
    {
        return FilterChildren(node.GetChildren(), serializationDelegate);
    }

    /// <remarks>Flutter's private <c>_filterChildren</c>.</remarks>
    internal List<DiagnosticsNode> FilterChildren(
        List<DiagnosticsNode> nodes,
        InspectorSerializationDelegate serializationDelegate)
    {
        var children = new List<DiagnosticsNode>();
        foreach (DiagnosticsNode child in nodes)
        {
            InspectorSerializationDelegate? updatedDelegate = UpdateDelegateForWidgetInspectorEnabledState(
                serializationDelegate,
                child);
            bool inDisableWidgetInspectorScope = (updatedDelegate?.InDisableWidgetInspectorScope ?? false)
                || serializationDelegate.InDisableWidgetInspectorScope;
            if (!inDisableWidgetInspectorScope
                && (!serializationDelegate.SummaryTree || ShouldShowInSummaryTree(child)))
            {
                children.Add(child);
            }
            else
            {
                children.AddRange(GetChildrenFiltered(child, updatedDelegate ?? serializationDelegate));
            }
        }

        return children;
    }

    /// <remarks>Flutter's private <c>_updateDelegateForWidgetInspectorEnabledState</c>.</remarks>
    private static InspectorSerializationDelegate? UpdateDelegateForWidgetInspectorEnabledState(
        InspectorSerializationDelegate serializationDelegate,
        DiagnosticsNode node)
    {
        object? value = node.Value;
        if (!serializationDelegate.InDisableWidgetInspectorScope
            && value is DisableWidgetInspectorScopeProxyElement)
        {
            return serializationDelegate.CopyWith(inDisableWidgetInspectorScope: true);
        }

        if (serializationDelegate.InDisableWidgetInspectorScope && value is EnableWidgetInspectorScopeProxyElement)
        {
            return serializationDelegate.CopyWith(inDisableWidgetInspectorScope: false);
        }

        return null;
    }

    /// <summary>Returns a JSON representation of the <see cref="DiagnosticsNode"/> for the root
    /// <see cref="Element"/>.</summary>
    public string GetRootWidget(string groupName)
    {
        return SafeJsonEncode(RootWidgetJson(groupName));
    }

    private Dictionary<string, object?>? RootWidgetJson(string groupName)
    {
        return NodeToJson(
            WidgetsBinding.Instance.RootElement?.ToDiagnosticsNode(),
            new InspectorSerializationDelegate(groupName: groupName, service: this));
    }

    /// <summary>Returns a JSON representation of the <see cref="DiagnosticsNode"/> for the root
    /// <see cref="Element"/> showing only nodes that should be included in a summary tree.</summary>
    public string GetRootWidgetSummaryTree(string groupName)
    {
        return SafeJsonEncode(RootWidgetSummaryTreeJson(groupName));
    }

    private Dictionary<string, object?>? RootWidgetSummaryTreeJson(
        string groupName,
        Func<DiagnosticsNode, InspectorSerializationDelegate, Dictionary<string, object?>?>?
            addAdditionalPropertiesCallback = null)
    {
        return RootWidgetTreeImpl(
            groupName: groupName,
            isSummaryTree: true,
            withPreviews: false,
            addAdditionalPropertiesCallback: addAdditionalPropertiesCallback);
    }

    private Task<Dictionary<string, object?>> RootWidgetSummaryTreeWithPreviewsJson(
        IReadOnlyDictionary<string, string> parameters)
    {
        string groupName = parameters["groupName"];
        Dictionary<string, object?>? result = RootWidgetTreeImpl(
            groupName: groupName,
            isSummaryTree: true,
            withPreviews: true);
        return Task.FromResult(new Dictionary<string, object?> { ["result"] = result });
    }

    private Task<Dictionary<string, object?>> RootWidgetTreeJson(IReadOnlyDictionary<string, string> parameters)
    {
        string groupName = parameters["groupName"];
        bool isSummaryTree = parameters.GetValueOrDefault("isSummaryTree") == "true";
        bool withPreviews = parameters.GetValueOrDefault("withPreviews") == "true";
        // If the "fullDetails" parameter is not provided, default to true.
        bool fullDetails = parameters.GetValueOrDefault("fullDetails") != "false";

        Dictionary<string, object?>? result = RootWidgetTreeImpl(
            groupName: groupName,
            isSummaryTree: isSummaryTree,
            withPreviews: withPreviews,
            fullDetails: fullDetails);

        return Task.FromResult(new Dictionary<string, object?> { ["result"] = result });
    }

    private Dictionary<string, object?>? RootWidgetTreeImpl(
        string groupName,
        bool isSummaryTree,
        bool withPreviews,
        bool fullDetails = true,
        Func<DiagnosticsNode, InspectorSerializationDelegate, Dictionary<string, object?>?>?
            addAdditionalPropertiesCallback = null)
    {
        bool shouldAddAdditionalProperties = addAdditionalPropertiesCallback is not null || withPreviews;

        // Combine the given addAdditionalPropertiesCallback with logic to add text
        // previews as well (if withPreviews is true):
        Dictionary<string, object?>? CombinedAddAdditionalPropertiesCallback(
            DiagnosticsNode node,
            InspectorSerializationDelegate serializationDelegate)
        {
            Dictionary<string, object?> additionalPropertiesJson =
                addAdditionalPropertiesCallback?.Invoke(node, serializationDelegate) ?? [];
            if (!withPreviews)
            {
                return additionalPropertiesJson;
            }

            object? value = node.Value;
            if (value is Element element)
            {
                RenderObject? renderObject = RenderObjectOrNull(element);
                if (renderObject is RenderParagraph paragraph)
                {
                    additionalPropertiesJson["textPreview"] = paragraph.Text.ToPlainText();
                }
            }

            return additionalPropertiesJson;
        }

        return NodeToJson(
            WidgetsBinding.Instance.RootElement?.ToDiagnosticsNode(),
            new InspectorSerializationDelegate(
                groupName: groupName,
                subtreeDepth: 1000000,
                summaryTree: isSummaryTree,
                service: this,
                addAdditionalPropertiesCallback: shouldAddAdditionalProperties
                    ? CombinedAddAdditionalPropertiesCallback
                    : null),
            fullDetails: fullDetails);
    }

    /// <summary>Returns a JSON representation of a <see cref="DiagnosticsNode"/> with properties and
    /// children expanded to <paramref name="subtreeDepth"/>.</summary>
    public string GetDetailsSubtree(string diagnosticableId, string groupName, int subtreeDepth = 2)
    {
        return SafeJsonEncode(DetailsSubtreeJson(diagnosticableId, groupName, subtreeDepth));
    }

    private Dictionary<string, object?>? DetailsSubtreeJson(
        string? diagnosticableId,
        string? groupName,
        int subtreeDepth)
    {
        DiagnosticsNode? root = IdToDiagnosticsNode(diagnosticableId);
        if (root is null)
        {
            return null;
        }

        return NodeToJson(
            root,
            new InspectorSerializationDelegate(
                groupName: groupName,
                subtreeDepth: subtreeDepth,
                includeProperties: true,
                service: this));
    }

    /// <summary>Returns a <see cref="DiagnosticsNode"/> representing the currently selected
    /// <see cref="Element"/>.</summary>
    protected internal string GetSelectedWidget(string? previousSelectionId, string groupName)
    {
        if (previousSelectionId is not null)
        {
            Print.DebugPrint("previousSelectionId is deprecated in API");
        }

        return SafeJsonEncode(SelectedWidgetJson(null, groupName));
    }

    /// <summary>Captures an image of the current state of an <paramref name="object"/> that is a
    /// <see cref="RenderObject"/> or <see cref="Element"/>.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetInspectorService.screenshot</c>. The image is scaled to fit within
    /// <paramref name="width"/> by <paramref name="height"/>, never above
    /// <paramref name="maxPixelRatio"/> pixels per logical pixel; <paramref name="margin"/> grows the
    /// captured bounds, and <paramref name="debugPaint"/> adds the target's debug paint.
    /// </remarks>
    protected internal async Task<Bitmap?> Screenshot(
        object? @object,
        double width,
        double height,
        double margin = 0.0,
        double maxPixelRatio = 1.0,
        bool debugPaint = false)
    {
        if (@object is not Element && @object is not RenderObject)
        {
            return null;
        }

        RenderObject? renderObject = @object is Element element ? RenderObjectOrNull(element) : @object as RenderObject;
        if (renderObject is null || !renderObject.Attached)
        {
            return null;
        }

        if (renderObject.DebugNeedsLayout)
        {
            PipelineOwner owner = renderObject.Owner!;
            Debug.Assert(!owner.DebugDoingLayout);
            owner.FlushLayout();
            owner.FlushCompositingBits();
            owner.FlushPaint();

            // If we still need layout, then that means that renderObject was skipped
            // in the layout phase and therefore can't be painted. It is clearer to
            // return null indicating that a screenshot is unavailable than to return
            // an empty image.
            if (renderObject.DebugNeedsLayout)
            {
                return null;
            }
        }

        Rect renderBounds = CalculateSubtreeBounds(renderObject);
        if (margin != 0.0)
        {
            renderBounds = renderBounds.Inflate(margin);
        }

        if (IsEmpty(renderBounds))
        {
            return null;
        }

        double pixelRatio = Math.Min(maxPixelRatio, Math.Min(width / renderBounds.Width, height / renderBounds.Height));

        return await InspectorScreenshotPaintingContext.ToImage(
            renderObject,
            renderBounds,
            pixelRatio: pixelRatio,
            debugPaint: debugPaint).ConfigureAwait(true);
    }

    private Task<Dictionary<string, object?>> LayoutExplorerNode(IReadOnlyDictionary<string, string> parameters)
    {
        string? diagnosticableId = parameters.GetValueOrDefault("id");
        int subtreeDepth = int.Parse(parameters["subtreeDepth"], CultureInfo.InvariantCulture);
        string? groupName = parameters.GetValueOrDefault("groupName");
        Dictionary<string, object?>? result = [];
        DiagnosticsNode? root = IdToDiagnosticsNode(diagnosticableId);
        if (root is null)
        {
            return Task.FromResult(new Dictionary<string, object?> { ["result"] = result });
        }

        result = NodeToJson(
            root,
            new InspectorSerializationDelegate(
                groupName: groupName,
                summaryTree: true,
                subtreeDepth: subtreeDepth,
                service: this,
                addAdditionalPropertiesCallback: (node, serializationDelegate) =>
                {
                    object? value = node.Value;
                    RenderObject? renderObject = value is Element element ? RenderObjectOrNull(element) : null;
                    if (renderObject is null)
                    {
                        return [];
                    }

                    InspectorSerializationDelegate renderObjectSerializationDelegate = serializationDelegate.CopyWith(
                        subtreeDepth: 0,
                        includeProperties: true,
                        expandPropertyValues: false);
                    var additionalJson = new Dictionary<string, object?>();
                    if (value is not RenderObject && serializationDelegate.ExpandPropertyValues)
                    {
                        additionalJson["renderObject"] =
                            renderObject.ToDiagnosticsNode().ToJsonMap(renderObjectSerializationDelegate);
                    }

                    RenderObject? renderParent = renderObject.Parent;
                    if (renderParent is not null
                        && serializationDelegate.SubtreeDepth > 0
                        && serializationDelegate.ExpandPropertyValues)
                    {
                        object? parentCreator = renderParent.DebugCreator;
                        if (parentCreator is DebugCreator debugCreator)
                        {
                            additionalJson["parentRenderElement"] = debugCreator.Element.ToDiagnosticsNode().ToJsonMap(
                                serializationDelegate.CopyWith(subtreeDepth: 0, includeProperties: true));
                            // TODO(jacobr): also describe the path back up the tree to
                            // the RenderParentElement from the current element. It
                            // could be a surprising distance up the tree if a lot of
                            // elements don't have their own RenderObjects.
                        }
                    }

                    try
                    {
                        if (!renderObject.DebugNeedsLayout)
                        {
                            // ignore: invalid_use_of_protected_member
                            // RenderView overrides `constraints` with its configuration's.
                            IConstraints constraints = renderObject is RenderView view
                                ? view.Constraints
                                : renderObject.CurrentConstraints;
                            var constraintsProperty = new Dictionary<string, object?>
                            {
                                ["type"] = constraints.GetType().Name,
                                ["description"] = constraints.ToString(),
                            };
                            if (constraints is BoxConstraints box)
                            {
                                constraintsProperty["minWidth"] = BindingBase.DartDoubleToString(box.MinWidth);
                                constraintsProperty["minHeight"] = BindingBase.DartDoubleToString(box.MinHeight);
                                constraintsProperty["maxWidth"] = BindingBase.DartDoubleToString(box.MaxWidth);
                                constraintsProperty["maxHeight"] = BindingBase.DartDoubleToString(box.MaxHeight);
                            }

                            additionalJson["constraints"] = constraintsProperty;
                        }
                    }
                    catch (Exception)
                    {
                        // Constraints are sometimes unavailable even though
                        // debugNeedsLayout is false.
                    }

                    try
                    {
                        if (renderObject is RenderBox renderBox)
                        {
                            additionalJson["isBox"] = true;
                            additionalJson["size"] = new Dictionary<string, object?>
                            {
                                ["width"] = BindingBase.DartDoubleToString(renderBox.Size.Width),
                                ["height"] = BindingBase.DartDoubleToString(renderBox.Size.Height),
                            };

                            IParentData? parentData = renderBox.parentData;
                            if (parentData is FlexParentData flexParentData)
                            {
                                additionalJson["flexFactor"] = flexParentData.flex ?? 0;
                                additionalJson["flexFit"] = DartEnumName(flexParentData.fit ?? FlexFit.Tight);
                            }
                            else if (parentData is BoxParentData boxParentData)
                            {
                                Point offset = boxParentData.offset;
                                additionalJson["parentData"] = new Dictionary<string, object?>
                                {
                                    ["offsetX"] = BindingBase.DartDoubleToString(offset.X),
                                    ["offsetY"] = BindingBase.DartDoubleToString(offset.Y),
                                };
                            }
                        }
                        else if (renderObject is RenderView renderView)
                        {
                            additionalJson["size"] = new Dictionary<string, object?>
                            {
                                ["width"] = BindingBase.DartDoubleToString(renderView.Size.Width),
                                ["height"] = BindingBase.DartDoubleToString(renderView.Size.Height),
                            };
                        }
                    }
                    catch (Exception)
                    {
                        // Not laid out yet.
                    }

                    return additionalJson;
                }));
        return Task.FromResult(new Dictionary<string, object?> { ["result"] = result });
    }

    private Task<Dictionary<string, object?>> SetFlexFit(IReadOnlyDictionary<string, string> parameters)
    {
        string? id = parameters.GetValueOrDefault("id");
        string parameter = parameters["flexFit"];
        FlexFit flexFit = ToEnumEntry<FlexFit>(parameter);
        object? @object = ToObject(id);
        bool succeed = false;
        if (@object is Element element)
        {
            RenderObject? render = RenderObjectOrNull(element);
            if (render?.parentData is FlexParentData parentData)
            {
                parentData.fit = flexFit;
                render.MarkNeedsLayout();
                succeed = true;
            }
        }

        return Task.FromResult(new Dictionary<string, object?> { ["result"] = succeed });
    }

    private Task<Dictionary<string, object?>> SetFlexFactor(IReadOnlyDictionary<string, string> parameters)
    {
        string? id = parameters.GetValueOrDefault("id");
        string flexFactor = parameters["flexFactor"];
        int? factor = flexFactor == "null" ? null : int.Parse(flexFactor, CultureInfo.InvariantCulture);
        object? @object = ToObject(id);
        bool succeed = false;
        if (@object is Element element)
        {
            RenderObject? render = RenderObjectOrNull(element);
            if (render?.parentData is FlexParentData parentData)
            {
                parentData.flex = factor;
                render.MarkNeedsLayout();
                succeed = true;
            }
        }

        return Task.FromResult(new Dictionary<string, object?> { ["result"] = succeed });
    }

    private Task<Dictionary<string, object?>> SetFlexProperties(IReadOnlyDictionary<string, string> parameters)
    {
        string? id = parameters.GetValueOrDefault("id");
        MainAxisAlignment mainAxisAlignment = ToEnumEntry<MainAxisAlignment>(parameters["mainAxisAlignment"]);
        CrossAxisAlignment crossAxisAlignment = ToEnumEntry<CrossAxisAlignment>(parameters["crossAxisAlignment"]);
        object? @object = ToObject(id);
        bool succeed = false;
        if (@object is Element element)
        {
            RenderObject? render = RenderObjectOrNull(element);
            if (render is RenderFlex renderFlex)
            {
                renderFlex.MainAxisAlignment = mainAxisAlignment;
                renderFlex.CrossAxisAlignment = crossAxisAlignment;
                renderFlex.MarkNeedsLayout();
                renderFlex.MarkNeedsPaint();
                succeed = true;
            }
        }

        return Task.FromResult(new Dictionary<string, object?> { ["result"] = succeed });
    }

    /// <summary>Dart's <c>Enum.name</c> of a C# enum value: the first letter lower-cased.</summary>
    private static string DartEnumName<T>(T value)
        where T : struct, Enum
    {
        string name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <remarks>Flutter's private <c>_toEnumEntry</c>: matches the Dart <c>toString</c> of the value
    /// (<c>Type.name</c>).</remarks>
    private static T ToEnumEntry<T>(string name)
        where T : struct, Enum
    {
        foreach (T entry in Enum.GetValues<T>())
        {
            if (typeof(T).Name + "." + DartEnumName(entry) == name)
            {
                return entry;
            }
        }

        throw new InvalidOperationException($"Enum value {name} not found");
    }

    private Dictionary<string, object?>? SelectedWidgetJson(string? previousSelectionId, string groupName)
    {
        return NodeToJson(
            GetSelectedWidgetDiagnosticsNode(previousSelectionId),
            new InspectorSerializationDelegate(groupName: groupName, service: this));
    }

    private DiagnosticsNode? GetSelectedWidgetDiagnosticsNode(string? previousSelectionId)
    {
        var previousSelection = ToObject(previousSelectionId) as DiagnosticsNode;
        Element? current = Selection.CurrentElement;
        return current is not null && ReferenceEquals(current, previousSelection?.Value)
            ? previousSelection
            : current?.ToDiagnosticsNode();
    }

    /// <summary>Returns a <see cref="DiagnosticsNode"/> representing the currently selected
    /// <see cref="Element"/> if the selected <see cref="Element"/> should be shown in the summary
    /// tree; otherwise the first ancestor of the selected <see cref="Element"/> that should.</summary>
    public string GetSelectedSummaryWidget(string? previousSelectionId, string groupName)
    {
        if (previousSelectionId is not null)
        {
            Print.DebugPrint("previousSelectionId is deprecated in API");
        }

        return SafeJsonEncode(SelectedSummaryWidgetJson(null, groupName));
    }

    private InspectorLocation? GetSelectedWidgetLocation(bool restrictToSummaryTree = false)
    {
        DiagnosticsNode? selectedWidget = restrictToSummaryTree
            ? GetSelectedSummaryDiagnosticsNode(null)
            : GetSelectedWidgetDiagnosticsNode(null);

        return WidgetInspectorDebug.GetCreationLocation(selectedWidget?.Value);
    }

    private DiagnosticsNode? GetSelectedSummaryDiagnosticsNode(string? previousSelectionId)
    {
        if (!IsWidgetCreationTracked())
        {
            return GetSelectedWidgetDiagnosticsNode(previousSelectionId);
        }

        var previousSelection = ToObject(previousSelectionId) as DiagnosticsNode;
        Element? current = Selection.CurrentElement;
        if (current is not null && !IsValueCreatedByLocalProject(current))
        {
            Element? firstLocal = null;
            foreach (Element candidate in current.DebugGetDiagnosticChain())
            {
                if (IsValueCreatedByLocalProject(candidate))
                {
                    firstLocal = candidate;
                    break;
                }
            }

            current = firstLocal;
        }

        return current is not null && ReferenceEquals(current, previousSelection?.Value)
            ? previousSelection
            : current?.ToDiagnosticsNode();
    }

    private Dictionary<string, object?>? SelectedSummaryWidgetJson(string? previousSelectionId, string groupName)
    {
        return NodeToJson(
            GetSelectedSummaryDiagnosticsNode(previousSelectionId),
            new InspectorSerializationDelegate(groupName: groupName, service: this));
    }

    /// <summary>Returns whether <see cref="Widget"/> creation locations are available.</summary>
    /// <remarks>
    /// Flutter's <c>isWidgetCreationTracked</c>: true when the creation-tracking transformer made
    /// widgets implement <see cref="IHasCreationLocation"/>. Plumix has no such transform, so this is
    /// false.
    /// </remarks>
    public bool IsWidgetCreationTracked()
    {
        _widgetCreationTracked ??= new WidgetForTypeTests() is IHasCreationLocation;
        return _widgetCreationTracked.Value;
    }

    private void OnFrameStart(TimeSpan timeStamp)
    {
        _frameStart = timeStamp;
        _frameNumber = PlatformDispatcher.Instance.FrameData.FrameNumber;
        Scheduler.AddPostFrameCallback(OnFrameEnd, debugLabel: "WidgetInspector.onFrameStart");
    }

    private void OnFrameEnd(TimeSpan timeStamp)
    {
        if (_trackRebuildDirtyWidgets)
        {
            PostStatsEvent("Flutter.RebuiltWidgets", _rebuildStats);
        }

        if (_trackRepaintWidgets)
        {
            PostStatsEvent("Flutter.RepaintWidgets", _repaintStats);
        }
    }

    private void PostStatsEvent(string eventName, ElementLocationStatsTracker stats)
    {
        PostEvent(eventName, stats.ExportToJson(_frameStart, frameNumber: _frameNumber));
    }

    /// <summary>Posts an event to the VM service's <paramref name="stream"/>.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetInspectorService.postEvent</c> over <c>dart:developer</c>'s <c>postEvent</c>;
    /// override it to capture the events (the tests do).
    /// </remarks>
    protected internal virtual void PostEvent(
        string eventKind,
        IReadOnlyDictionary<string, object?> eventData,
        string stream = DeveloperService.ExtensionStream)
    {
        DeveloperService.PostEvent(eventKind, eventData, stream: stream);
    }

    /// <summary>Sends <paramref name="object"/> to a connected tool's object inspector.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetInspectorService.inspect</c> over <c>dart:developer</c>'s <c>inspect</c>;
    /// override it to capture the calls (the tests do).
    /// </remarks>
    protected internal virtual void Inspect(object? @object)
    {
        DeveloperService.Inspect(@object);
    }

    private void OnRebuildWidget(Element element, bool builtOnce)
    {
        _rebuildStats.Add(element);
    }

    private void OnPaint(RenderObject renderObject)
    {
        try
        {
            Element? element = (renderObject.DebugCreator as DebugCreator)?.Element;
            if (element is not RenderObjectElement)
            {
                // This branch should not hit as long as all RenderObjects were created
                // by Widgets. It is possible there might be some render objects
                // created directly without using the Widget layer so we add this check
                // to improve robustness.
                return;
            }

            _repaintStats.Add(element);

            // Give all ancestor elements credit for repainting as long as they do
            // not have their own associated RenderObject.
            element.VisitAncestorElements(ancestor =>
            {
                if (ancestor is RenderObjectElement)
                {
                    // This ancestor has its own RenderObject so we can precisely track
                    // when it repaints.
                    return false;
                }

                _repaintStats.Add(ancestor);
                return true;
            });
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "widget inspector library",
                context: new ErrorDescription("while tracking widget repaints")));
        }
    }

    /// <summary>This method is called by <see cref="WidgetsBinding"/> before hot reload.</summary>
    /// <remarks>Flutter's <c>WidgetInspectorService.performReassemble</c>.</remarks>
    public void PerformReassemble()
    {
        ClearStats();
        ResetErrorCount();
    }

    /// <summary>Safely get the render object of an <see cref="Element"/>.</summary>
    /// <remarks>If the element is not yet mounted, the result will be null.</remarks>
    private static RenderObject? RenderObjectOrNull(Element element) => element.Mounted ? element.RenderObject : null;

    /// <summary>Lets a test register the extensions of a fresh service.</summary>
    internal static void DebugResetServiceExtensionsRegisteredForTests()
    {
        _debugServiceExtensionsRegistered = false;
    }
}

/// <summary>The default <see cref="WidgetInspectorService"/>.</summary>
/// <remarks>Flutter's private <c>_WidgetInspectorService</c>.</remarks>
internal sealed class DefaultWidgetInspectorService : WidgetInspectorService
{
    public DefaultWidgetInspectorService()
    {
        Selection.AddListener(() => SelectionChangedCallback?.Invoke());
    }
}

/// <summary>Counts how many times an object has been created from the same source location.</summary>
/// <remarks>Flutter's private <c>_LocationCount</c>.</remarks>
internal sealed class InspectorLocationCount
{
    public InspectorLocationCount(InspectorLocation location, int id, bool local)
    {
        Location = location;
        Id = id;
        Local = local;
    }

    /// <summary>Dense identifier for this location.</summary>
    public int Id { get; }

    /// <summary>Whether this location is part of the local project.</summary>
    public bool Local { get; }

    public InspectorLocation Location { get; }

    public int Count { get; private set; }

    public void Reset()
    {
        Count = 0;
    }

    public void Increment()
    {
        Count++;
    }
}

/// <summary>A stat tracker that aggregates a performance metric for <see cref="Element"/> objects at
/// the granularity of creation locations in source code.</summary>
/// <remarks>Flutter's private <c>_ElementLocationStatsTracker</c>.</remarks>
internal sealed class ElementLocationStatsTracker
{
    // All known creation location tracked.
    //
    // This could also be stored as a `Map<int, _LocationCount>` but this
    // representation is more efficient as all location ids from 0 to n are
    // typically present.
    //
    // All logic in this class assumes that if `_stats[i]` is not null
    // `_stats[i].id` equals `i`.
    private readonly List<InspectorLocationCount?> _stats = [];

    /// <summary>Locations with a non-zero count.</summary>
    public List<InspectorLocationCount> Active { get; } = [];

    /// <summary>Locations that were added since stats were last exported.</summary>
    public List<InspectorLocationCount> NewLocations { get; } = [];

    /// <summary>Increments the count associated with the creation location of <paramref name="element"/>.</summary>
    public void Add(Element element)
    {
        object widget = element.Widget;
        if (widget is not IHasCreationLocation hasCreationLocation)
        {
            return;
        }

        InspectorLocation? location = hasCreationLocation.Location;
        if (location is null)
        {
            return;
        }

        int id = WidgetInspectorDebug.ToLocationId(location);

        InspectorLocationCount entry;
        if (id >= _stats.Count || _stats[id] is null)
        {
            // After the first frame, almost all creation ids will already be in
            // _stats so this slow path will rarely be hit.
            while (id >= _stats.Count)
            {
                _stats.Add(null);
            }

            entry = new InspectorLocationCount(
                location: location,
                id: id,
                local: WidgetInspectorService.Instance.IsLocalCreationLocation(location.File));
            if (entry.Local)
            {
                NewLocations.Add(entry);
            }

            _stats[id] = entry;
        }
        else
        {
            entry = _stats[id]!;
        }

        // We could in the future add an option to track stats for all widgets but
        // that would significantly increase the size of the events posted using
        // [developer.postEvent] and current use cases for this feature focus on
        // helping users find problems with their widgets not the platform
        // widgets.
        if (entry.Local)
        {
            if (entry.Count == 0)
            {
                Active.Add(entry);
            }

            entry.Increment();
        }
    }

    /// <summary>Clear all aggregated statistics.</summary>
    public void ResetCounts()
    {
        // We chose to only reset the active counts instead of clearing all data
        // to reduce the number memory allocations performed after the first frame.
        // Once an app has warmed up, location stats tracking should not
        // trigger significant additional memory allocations. Avoiding memory
        // allocations is important to minimize the impact this class has on cpu
        // and memory performance of the running app.
        foreach (InspectorLocationCount entry in Active)
        {
            entry.Reset();
        }

        Active.Clear();
    }

    /// <summary>Exports the current counts and then resets the stats to prepare to track the next
    /// frame of data.</summary>
    public Dictionary<string, object?> ExportToJson(TimeSpan startTime, int frameNumber)
    {
        var events = new List<int>(Active.Count * 2);
        foreach (InspectorLocationCount stat in Active)
        {
            events.Add(stat.Id);
            events.Add(stat.Count);
        }

        var json = new Dictionary<string, object?>
        {
            ["startTime"] = (long)(startTime.Ticks / TimeSpan.TicksPerMicrosecond),
            ["frameNumber"] = frameNumber,
            ["events"] = events,
        };

        // Encode the new locations using the older encoding.
        if (NewLocations.Count > 0)
        {
            // Add all newly used location ids to the JSON.
            var locationsJson = new Dictionary<string, List<int>>();
            foreach (InspectorLocationCount entry in NewLocations)
            {
                InspectorLocation location = entry.Location;
                if (!locationsJson.TryGetValue(location.File, out List<int>? jsonForFile))
                {
                    jsonForFile = [];
                    locationsJson[location.File] = jsonForFile;
                }

                jsonForFile.Add(entry.Id);
                jsonForFile.Add(location.Line);
                jsonForFile.Add(location.Column);
            }

            json["newLocations"] = locationsJson;
        }

        // Encode the new locations using the newer encoding (as of v2.4.0).
        if (NewLocations.Count > 0)
        {
            var fileLocationsMap = new Dictionary<string, Dictionary<string, object?>>();
            foreach (InspectorLocationCount entry in NewLocations)
            {
                InspectorLocation location = entry.Location;
                if (!fileLocationsMap.TryGetValue(location.File, out Dictionary<string, object?>? locations))
                {
                    locations = new Dictionary<string, object?>
                    {
                        ["ids"] = new List<int>(),
                        ["lines"] = new List<int>(),
                        ["columns"] = new List<int>(),
                        ["names"] = new List<string?>(),
                    };
                    fileLocationsMap[location.File] = locations;
                }

                ((List<int>)locations["ids"]!).Add(entry.Id);
                ((List<int>)locations["lines"]!).Add(location.Line);
                ((List<int>)locations["columns"]!).Add(location.Column);
                ((List<string?>)locations["names"]!).Add(location.Name);
            }

            json["locations"] = fileLocationsMap;
        }

        ResetCounts();
        NewLocations.Clear();
        return json;
    }
}

/// <summary>A widget used only to check whether widgets carry creation locations.</summary>
/// <remarks>Flutter's private <c>_WidgetForTypeTests</c>.</remarks>
internal class WidgetForTypeTests : Widget
{
    public override Element CreateElement() => throw new NotImplementedException();
}
