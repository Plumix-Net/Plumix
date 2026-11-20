using System.Reflection;
using Xunit;

// C#-only infrastructure: no Dart parity source.
//
// No public or protected member of the widget packages may mention a type from an `Avalonia*`
// assembly: hosting lives in `Plumix.Avalonia`, which is exempt, so the widget API can outlive the
// Avalonia host (docs/product avalonia-free-api).

namespace Plumix.Tests;

public sealed class PublicApiAvaloniaFreeTests
{
    public static TheoryData<string> Packages => new() { "Plumix", "Plumix.Material", "Plumix.Cupertino" };

    [Theory]
    [MemberData(nameof(Packages))]
    public void PublicApi_MentionsNoAvaloniaType(string package)
    {
        Dictionary<string, List<string>> leaks = Collect(Load(package));

        Assert.True(leaks.Count == 0,
            $"{package} public API mentions Avalonia types:\n" + string.Join("\n", leaks
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(
                    pair => $"{pair.Key}:\n    " + string.Join("\n    ", pair.Value.Order(StringComparer.Ordinal)))));
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
            method is not null && (
                method.IsPublic || (exposedProtected && (method.IsFamily || method.IsFamilyOrAssembly)));

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
            if (
                part.Assembly.GetName().Name is { } assemblyName && assemblyName.StartsWith(
                    "Avalonia",
                    StringComparison.Ordinal))
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
