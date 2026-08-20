// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigator.dart

using System.Reflection;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

/// <summary>Builds a route that can be restored after the application was killed and relaunched.</summary>
public delegate Route RestorableRouteBuilder(BuildContext context, object? arguments);

/// <summary>Flutter's <c>_RouteRestorationType</c>.</summary>
internal enum RouteRestorationType
{
    Named,
    Anonymous,
}

/// <summary>
/// Flutter's <c>_RestorationInformation</c>: everything needed to recreate one pageless route after a
/// restoration, plus its serializable form.
/// </summary>
internal abstract class RestorationInformation
{
    private object? _serializableData;

    protected RestorationInformation(RouteRestorationType type)
    {
        Type = type;
    }

    public RouteRestorationType Type { get; }

    public abstract int RestorationScopeId { get; }

    /// <summary>Whether this route can be serialized at all; an unrestorable route stops restoration above it.</summary>
    public virtual bool IsRestorable => true;

    public static RestorationInformation Named(string name, object? arguments, int restorationScopeId) =>
        new NamedRestorationInformation(name, arguments, restorationScopeId);

    public static RestorationInformation Anonymous(
        RestorableRouteBuilder routeBuilder,
        object? arguments,
        int restorationScopeId) =>
        new AnonymousRestorationInformation(routeBuilder, arguments, restorationScopeId);

    public static RestorationInformation FromSerializableData(object data)
    {
        var list = (IReadOnlyList<object?>)data;
        if (list.Count < 1)
        {
            throw new ArgumentException("Restoration data is malformed.", nameof(data));
        }

        var type = (RouteRestorationType)Convert.ToInt32(list[0], System.Globalization.CultureInfo.InvariantCulture);
        return type switch
        {
            RouteRestorationType.Named => NamedRestorationInformation.Deserialize(list),
            RouteRestorationType.Anonymous => AnonymousRestorationInformation.Deserialize(list),
            _ => throw new ArgumentOutOfRangeException(nameof(data)),
        };
    }

    public object GetSerializableData() => _serializableData ??= ComputeSerializableData();

    protected virtual List<object?> ComputeSerializableData() => [(int)Type];

    protected abstract Route CreateRoute(NavigatorState navigator);

    public RouteEntry ToRouteEntry(NavigatorState navigator, RouteLifecycle initialState = RouteLifecycle.Add)
    {
        return new RouteEntry(
            CreateRoute(navigator),
            initialState: initialState,
            pageBased: false,
            restorationInformation: this);
    }
}

internal sealed class NamedRestorationInformation : RestorationInformation
{
    public NamedRestorationInformation(string name, object? arguments, int restorationScopeId)
        : base(RouteRestorationType.Named)
    {
        Name = name;
        Arguments = arguments;
        RestorationScopeId = restorationScopeId;
    }

    public override int RestorationScopeId { get; }

    public string Name { get; }

    public object? Arguments { get; }

    public static NamedRestorationInformation Deserialize(IReadOnlyList<object?> data)
    {
        if (data.Count <= 2)
        {
            throw new ArgumentException("Named restoration data is malformed.", nameof(data));
        }

        return new NamedRestorationInformation(
            (string)data[2]!,
            data.Count > 3 ? data[3] : null,
            Convert.ToInt32(data[1], System.Globalization.CultureInfo.InvariantCulture));
    }

    protected override List<object?> ComputeSerializableData()
    {
        List<object?> data = base.ComputeSerializableData();
        data.Add(RestorationScopeId);
        data.Add(Name);
        if (Arguments is not null)
        {
            data.Add(Arguments);
        }

        return data;
    }

    protected override Route CreateRoute(NavigatorState navigator) =>
        navigator.RouteNamed(Name, Arguments, allowNull: false)!;
}

/// <summary>
/// Flutter identifies the builder through <c>PluginUtilities.getCallbackHandle</c>. The CLR equivalent is the
/// declaring type plus method name of a static method, so anonymous restorable routes must be built by a static
/// method; an instance or lambda-captured builder is treated as unrestorable exactly like Flutter treats web.
/// </summary>
internal sealed class AnonymousRestorationInformation : RestorationInformation
{
    private readonly RestorableRouteBuilder _routeBuilder;

    public AnonymousRestorationInformation(
        RestorableRouteBuilder routeBuilder,
        object? arguments,
        int restorationScopeId) : base(RouteRestorationType.Anonymous)
    {
        _routeBuilder = routeBuilder;
        Arguments = arguments;
        RestorationScopeId = restorationScopeId;
    }

    public override int RestorationScopeId { get; }

    public object? Arguments { get; }

    public override bool IsRestorable => DescribeCallback(_routeBuilder) is not null;

    public static AnonymousRestorationInformation Deserialize(IReadOnlyList<object?> data)
    {
        if (data.Count <= 2)
        {
            throw new ArgumentException("Anonymous restoration data is malformed.", nameof(data));
        }

        return new AnonymousRestorationInformation(
            ResolveCallback((string)data[2]!),
            data.Count > 3 ? data[3] : null,
            Convert.ToInt32(data[1], System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>Whether <paramref name="routeBuilder"/> is a static method that can be named in restoration data.</summary>
    public static bool IsStaticCallback(RestorableRouteBuilder routeBuilder) =>
        DescribeCallback(routeBuilder) is not null;

    protected override List<object?> ComputeSerializableData()
    {
        List<object?> data = base.ComputeSerializableData();
        data.Add(RestorationScopeId);
        data.Add(DescribeCallback(_routeBuilder)
                 ?? throw new InvalidOperationException("The provided routeBuilder must be a static method."));
        if (Arguments is not null)
        {
            data.Add(Arguments);
        }

        return data;
    }

    protected override Route CreateRoute(NavigatorState navigator) => _routeBuilder(navigator.Context, Arguments);

    private static string? DescribeCallback(RestorableRouteBuilder routeBuilder)
    {
        MethodInfo method = routeBuilder.Method;
        if (!method.IsStatic || routeBuilder.Target is not null || method.DeclaringType is null)
        {
            return null;
        }

        string? typeName = method.DeclaringType.AssemblyQualifiedName;
        return typeName is null ? null : $"{typeName}|{method.Name}";
    }

    private static RestorableRouteBuilder ResolveCallback(string description)
    {
        int separator = description.LastIndexOf('|');
        if (separator < 0)
        {
            throw new ArgumentException("Anonymous restoration data is malformed.", nameof(description));
        }

        var declaringType = System.Type.GetType(description[..separator], throwOnError: true)!;
        return (RestorableRouteBuilder)Delegate.CreateDelegate(
            typeof(RestorableRouteBuilder),
            declaringType,
            description[(separator + 1)..]);
    }
}

/// <summary>
/// Flutter's <c>_HistoryProperty</c>: serializes the pageless routes of the navigator, grouped by the page
/// route they sit above. Flutter uses a null map key for routes below the bottom-most page; the encoded C#
/// representation uses distinct root/page prefixes because .NET dictionaries cannot contain null keys.
/// </summary>
internal sealed class HistoryProperty : RestorableProperty<Dictionary<string, List<object>>?>
{
    private const string RootPageKey = "r";
    private const string PageKeyPrefix = "p";

    private Dictionary<string, List<object>>? _pageToPagelessRoutes;

    public bool HasData => _pageToPagelessRoutes is not null;

    public override bool Enabled => HasData;

    public void Update(IReadOnlyList<RouteEntry> history)
    {
        bool wasUninitialized = _pageToPagelessRoutes is null;
        bool needsSerialization = wasUninitialized;
        _pageToPagelessRoutes ??= [];

        RouteEntry? currentPage = null;
        var newRoutesForCurrentPage = new List<object>();
        List<object> oldRoutesForCurrentPage = _pageToPagelessRoutes.GetValueOrDefault(RootPageKey) ?? [];
        bool restorationEnabled = true;

        var newMap = new Dictionary<string, List<object>>();
        var removedPages = new HashSet<string>(_pageToPagelessRoutes.Keys);

        foreach (RouteEntry entry in history)
        {
            if (!entry.IsPresentForRestoration)
            {
                entry.RestorationEnabled = false;
                continue;
            }

            if (entry.PageBased)
            {
                needsSerialization = needsSerialization
                                     || newRoutesForCurrentPage.Count != oldRoutesForCurrentPage.Count;
                FinalizeEntry(newRoutesForCurrentPage, currentPage, newMap, removedPages);
                currentPage = entry;
                restorationEnabled = entry.RestorationId is not null;
                entry.RestorationEnabled = restorationEnabled;
                if (restorationEnabled)
                {
                    newRoutesForCurrentPage = [];
                    oldRoutesForCurrentPage = _pageToPagelessRoutes.GetValueOrDefault(
                        PageKeyPrefix + entry.RestorationId) ?? [];
                }
                else
                {
                    newRoutesForCurrentPage = [];
                    oldRoutesForCurrentPage = [];
                }

                continue;
            }

            restorationEnabled = restorationEnabled && (entry.RestorationInformation?.IsRestorable ?? false);
            entry.RestorationEnabled = restorationEnabled;
            if (!restorationEnabled)
            {
                continue;
            }

            object serializedData = entry.RestorationInformation!.GetSerializableData();
            needsSerialization = needsSerialization
                                 || oldRoutesForCurrentPage.Count <= newRoutesForCurrentPage.Count
                                 || !Equals(oldRoutesForCurrentPage[newRoutesForCurrentPage.Count], serializedData);
            newRoutesForCurrentPage.Add(serializedData);
        }

        needsSerialization = needsSerialization || newRoutesForCurrentPage.Count != oldRoutesForCurrentPage.Count;
        FinalizeEntry(newRoutesForCurrentPage, currentPage, newMap, removedPages);
        needsSerialization = needsSerialization || removedPages.Count > 0;

        if (!needsSerialization)
        {
            return;
        }

        _pageToPagelessRoutes = newMap;
        NotifyListeners();
    }

    public void Clear()
    {
        if (_pageToPagelessRoutes is null)
        {
            return;
        }

        _pageToPagelessRoutes = null;
        NotifyListeners();
    }

    public List<RouteEntry> RestoreEntriesForPage(RouteEntry? page, NavigatorState navigator)
    {
        var result = new List<RouteEntry>();
        if (_pageToPagelessRoutes is null || (page is not null && page.RestorationId is null))
        {
            return result;
        }

        string pageKey = page is null ? RootPageKey : PageKeyPrefix + page.RestorationId;
        if (!_pageToPagelessRoutes.TryGetValue(pageKey, out List<object>? serialized))
        {
            return result;
        }

        foreach (object data in serialized)
        {
            result.Add(RestorationInformation.FromSerializableData(data).ToRouteEntry(navigator));
        }

        return result;
    }

    public override Dictionary<string, List<object>>? CreateDefaultValue() => null;

    public override Dictionary<string, List<object>>? FromPrimitives(object? data)
    {
        if (data is not System.Collections.IDictionary map)
        {
            return null;
        }

        var result = new Dictionary<string, List<object>>();
        foreach (System.Collections.DictionaryEntry entry in map)
        {
            if (entry.Key is string key && entry.Value is System.Collections.IEnumerable items)
            {
                result[key] = [.. items.Cast<object>()];
            }
        }

        return result;
    }

    public override void InitWithValue(Dictionary<string, List<object>>? value) => _pageToPagelessRoutes = value;

    public override object? ToPrimitives() => _pageToPagelessRoutes;

    private static void FinalizeEntry(
        List<object> routes,
        RouteEntry? page,
        Dictionary<string, List<object>> pageToRoutes,
        HashSet<string> pagesToRemove)
    {
        if (routes.Count == 0)
        {
            return;
        }

        string pageKey = page is null ? RootPageKey : PageKeyPrefix + page.RestorationId;
        pageToRoutes[pageKey] = routes;
        pagesToRemove.Remove(pageKey);
    }
}
