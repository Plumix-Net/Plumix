using System.Reflection;
using Xunit;

// C#-only infrastructure: no Dart parity source.
//
// Ratchet for docs/product avalonia-free-api: no public or protected member of the widget packages
// may mention a type from an `Avalonia*` assembly. Hosting lives in `Plumix.Avalonia`, which is
// exempt. The budget below may only shrink; growing it is a review failure, not a test fix.

namespace Plumix.Tests;

public sealed class PublicApiAvaloniaFreeTests
{
    // Avalonia type name -> number of public API members that may still mention it, per package.
    private static readonly Dictionary<string, Dictionary<string, int>> Budget = new()
    {
        ["Plumix"] = new()
        {
            ["Application"] = 1,
            ["AutomationPeer"] = 1,
            ["Bitmap"] = 18,
            ["BitmapBlendingMode"] = 17,
            ["BoxShadows"] = 1,
            ["Color"] = 2,
            ["Control"] = 1,
            ["DrawingContext"] = 4,
            ["FocusChangedEventArgs"] = 2,
            ["FontFamily"] = 18,
            ["FontStyle"] = 17,
            ["FontWeight"] = 22,
            ["Geometry"] = 9,
            ["IApplicationLifetime"] = 1,
            ["IAvaloniaListItemValidator`1"] = 2,
            ["IAvaloniaObjectDebug"] = 3,
            ["IBrush"] = 20,
            ["ICustomHitTest"] = 2,
            ["IDataContextProvider"] = 3,
            ["IDataTemplateHost"] = 3,
            ["IGlobalDataTemplates"] = 1,
            ["IGlobalStyles"] = 1,
            ["IImage"] = 12,
            ["IInputElement"] = 2,
            ["ILogical"] = 4,
            ["INamed"] = 2,
            ["IOptionalFeatureProvider"] = 1,
            ["IPen"] = 12,
            ["IResourceHost"] = 3,
            ["IResourceNode"] = 3,
            ["ISetInheritanceParent"] = 2,
            ["ISetLogicalParent"] = 2,
            ["ISetterValue"] = 2,
            ["IStyleHost"] = 3,
            ["IThemeVariantHost"] = 3,
            ["IThemeVariantRoot"] = 1,
            ["IVisualBrushInitialize"] = 2,
            ["KeyEventArgs"] = 2,
            ["Matrix"] = 5,
            ["Point"] = 639,
            ["PointerCaptureLostEventArgs"] = 1,
            ["PointerEventArgs"] = 3,
            ["PointerPressedEventArgs"] = 1,
            ["PointerReleasedEventArgs"] = 1,
            ["PointerWheelEventArgs"] = 1,
            ["Rect"] = 349,
            ["Size"] = 227,
            ["SolidColorBrush"] = 1,
            ["TextInputEventArgs"] = 1,
            ["Thickness"] = 3,
            ["Vector"] = 24,
            ["Visual"] = 2,
            ["VisualTreeAttachmentEventArgs"] = 2,
        },
        ["Plumix.Material"] = new()
        {
            ["FontFamily"] = 2,
            ["Point"] = 81,
            ["Rect"] = 35,
            ["Size"] = 83,
            ["Vector"] = 17,
        },
        ["Plumix.Cupertino"] = new()
        {
            ["Point"] = 19,
            ["Rect"] = 4,
            ["Size"] = 11,
        },
    };

    public static TheoryData<string> Packages => new() { "Plumix", "Plumix.Material", "Plumix.Cupertino" };

    [Theory]
    [MemberData(nameof(Packages))]
    public void PublicApi_MentionsNoAvaloniaTypeBeyondBudget(string package)
    {
        Assembly assembly = Load(package);
        Dictionary<string, List<string>> leaks = Collect(assembly);
        Dictionary<string, int> budget = Budget[package];

        List<string> failures = [];
        foreach ((string type, List<string> members) in leaks.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            int allowed = budget.GetValueOrDefault(type);
            if (members.Count > allowed)
            {
                failures.Add($"{type}: {members.Count} members (budget {allowed}):\n    "
                             + string.Join("\n    ", members.Order(StringComparer.Ordinal).Take(40)));
            }
        }

        Assert.True(failures.Count == 0,
            $"{package} public API mentions Avalonia types:\n" + string.Join("\n", failures));
    }

    [Theory]
    [MemberData(nameof(Packages))]
    public void Budget_IsTight(string package)
    {
        Dictionary<string, List<string>> leaks = Collect(Load(package));
        List<string> slack = Budget[package]
            .Where(pair => leaks.GetValueOrDefault(pair.Key)?.Count is not { } count || count < pair.Value)
            .Select(pair => $"{pair.Key}: budget {pair.Value}, actual {leaks.GetValueOrDefault(pair.Key)?.Count ?? 0}")
            .ToList();
        Assert.True(slack.Count == 0, $"Lower the {package} budget:\n" + string.Join("\n", slack));
    }

    private static Assembly Load(string package) => package switch
    {
        "Plumix" => typeof(Plumix.Widgets.Widget).Assembly,
        "Plumix.Material" => typeof(Plumix.Material.MaterialApp).Assembly,
        "Plumix.Cupertino" => typeof(Plumix.Cupertino.CupertinoApp).Assembly,
        _ => throw new ArgumentOutOfRangeException(nameof(package)),
    };

    internal static Dictionary<string, List<string>> Collect(Assembly assembly)
    {
        Dictionary<string, HashSet<string>> leaks = [];
        foreach (Type type in assembly.GetExportedTypes())
        {
            if (type.Name.Contains('<'))
            {
                continue;
            }

            string owner = type.FullName ?? type.Name;
            if (type.BaseType is { } baseType)
            {
                Report(leaks, baseType, $"{owner} : base");
            }

            foreach (Type contract in type.GetInterfaces())
            {
                Report(leaks, contract, $"{owner} : {contract.Name}");
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                       | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (MemberInfo member in type.GetMembers(flags))
            {
                if (member.Name.Contains('<') || !IsVisible(member, type))
                {
                    continue;
                }

                string name = $"{owner}.{member.Name}";
                switch (member)
                {
                    case FieldInfo field:
                        Report(leaks, field.FieldType, name);
                        break;
                    case PropertyInfo property:
                        Report(leaks, property.PropertyType, name);
                        foreach (ParameterInfo parameter in property.GetIndexParameters())
                        {
                            Report(leaks, parameter.ParameterType, name);
                        }

                        break;
                    case EventInfo @event when @event.EventHandlerType is { } handler:
                        Report(leaks, handler, name);
                        break;
                    case MethodBase method when !method.IsSpecialName || method.Name.StartsWith("op_")
                                                                    || method is ConstructorInfo:
                        if (method is MethodInfo info)
                        {
                            Report(leaks, info.ReturnType, name);
                        }

                        foreach (ParameterInfo parameter in method.GetParameters())
                        {
                            Report(leaks, parameter.ParameterType, name);
                        }

                        break;
                }
            }
        }

        return leaks.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
    }

    private static bool IsVisible(MemberInfo member, Type owner)
    {
        bool exposedProtected = !owner.IsSealed;
        static bool Accessible(MethodBase? method, bool exposedProtected) =>
            method is not null && (method.IsPublic || (exposedProtected && (method.IsFamily || method.IsFamilyOrAssembly)));

        return member switch
        {
            FieldInfo field => field.IsPublic || (exposedProtected && (field.IsFamily || field.IsFamilyOrAssembly)),
            PropertyInfo property => Accessible(property.GetMethod, exposedProtected)
                                     || Accessible(property.SetMethod, exposedProtected),
            EventInfo @event => Accessible(@event.AddMethod, exposedProtected),
            MethodBase method => Accessible(method, exposedProtected),
            _ => false,
        };
    }

    private static void Report(Dictionary<string, HashSet<string>> leaks, Type type, string member)
    {
        foreach (Type part in Flatten(type))
        {
            if (part.Assembly.GetName().Name is { } assemblyName && assemblyName.StartsWith("Avalonia", StringComparison.Ordinal))
            {
                string key = part.IsGenericType ? part.GetGenericTypeDefinition().Name : part.Name;
                if (!leaks.TryGetValue(key, out HashSet<string>? members))
                {
                    leaks[key] = members = [];
                }

                members.Add(member);
            }
        }
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        if (type.HasElementType && type.GetElementType() is { } element)
        {
            foreach (Type inner in Flatten(element))
            {
                yield return inner;
            }

            yield break;
        }

        if (type.IsGenericParameter)
        {
            yield break;
        }

        yield return type;
        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments())
            {
                foreach (Type inner in Flatten(argument))
                {
                    yield return inner;
                }
            }
        }
    }
}
