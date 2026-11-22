using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Plumix.Material;
using Xunit;

// C#-only test infrastructure: Dart has no host assemblies to depend on.

namespace Plumix.Tests;

/// <summary>
/// Counts the types and members of the <c>Avalonia*</c>, <c>SkiaSharp</c> and <c>HarfBuzzSharp</c>
/// assemblies that each widget package references, and holds every count to a budget that only goes
/// down (docs/product <c>avalonia-free-core.md</c>, stage 0).
/// </summary>
/// <remarks>
/// The widget packages are off the Avalonia/Skia backend: drawing is data that a host backend plays
/// back, and the Avalonia/Skia backend lives in <c>Plumix.Avalonia</c>. Every budget reached zero in
/// stage 3g, when the core dropped its Avalonia/SkiaSharp/HarfBuzzSharp package references; a count
/// above zero means new code reached for a backend type directly.
/// </remarks>
public sealed class HostDependencyBudgetTests
{
    private static readonly Dictionary<string, int> Budgets = new(StringComparer.Ordinal)
    {
        ["Plumix"] = 0,
        ["Plumix.Material"] = 0,
        ["Plumix.Cupertino"] = 0,
    };

    public static TheoryData<string> Packages() =>
    [
        typeof(Plumix.Widgets.Widget).Assembly.GetName().Name!,
        typeof(MaterialApp).Assembly.GetName().Name!,
        typeof(Plumix.Cupertino.CupertinoApp).Assembly.GetName().Name!,
    ];

    [Theory]
    [MemberData(nameof(Packages))]
    public void HostReferenceCountStaysWithinBudget(string assemblyName)
    {
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Single(item => item.GetName().Name == assemblyName);
        IReadOnlyList<string> references = HostReferences(assembly);
        int budget = Budgets[assemblyName];

        Assert.True(references.Count <= budget,
            $"{assemblyName} references {references.Count} host types and members, over its budget of "
            + $"{budget}. Route the new code through Plumix types instead:\n"
            + string.Join('\n', references));
        Assert.True(references.Count == budget,
            $"{assemblyName} references {references.Count} host types and members, under its budget of "
            + $"{budget}: lower the budget in {nameof(HostDependencyBudgetTests)} to {references.Count}.");
    }

    /// <summary>
    /// The widget packages do not even reference a host assembly, so they load and run with no
    /// Avalonia, SkiaSharp or HarfBuzzSharp present.
    /// </summary>
    [Theory]
    [MemberData(nameof(Packages))]
    public void PackageReferencesNoHostAssembly(string assemblyName)
    {
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Single(item => item.GetName().Name == assemblyName);
        using FileStream stream = File.OpenRead(assembly.Location);
        using var reader = new PEReader(stream);
        MetadataReader metadata = reader.GetMetadataReader();

        string[] hostReferences = metadata.AssemblyReferences
            .Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name))
            .Where(HostBinaryCompatibilityTests.IsHostAssembly)
            .ToArray();
        Assert.True(hostReferences.Length == 0,
            $"{assemblyName} references host assemblies: {string.Join(", ", hostReferences)}.");
    }

    /// <summary>
    /// The host types and members <paramref name="assembly"/> references, one line each: every type
    /// reference resolved through an <c>Avalonia*</c>, <c>SkiaSharp</c> or <c>HarfBuzzSharp</c> assembly
    /// reference, and every member reference whose parent is such a type.
    /// </summary>
    internal static IReadOnlyList<string> HostReferences(Assembly assembly)
    {
        using FileStream stream = File.OpenRead(assembly.Location);
        using var reader = new PEReader(stream);
        MetadataReader metadata = reader.GetMetadataReader();

        var result = new List<string>();
        foreach (TypeReferenceHandle handle in metadata.TypeReferences)
        {
            if (HostBinaryCompatibilityTests.IsInScope(metadata, handle, HostBinaryCompatibilityTests.IsHostAssembly))
            {
                result.Add(HostBinaryCompatibilityTests.TypeName(metadata, handle));
            }
        }

        foreach (MemberReferenceHandle handle in metadata.MemberReferences)
        {
            MemberReference member = metadata.GetMemberReference(handle);
            if (member.Parent.Kind == HandleKind.TypeReference
                && HostBinaryCompatibilityTests.IsInScope(
                    metadata, (TypeReferenceHandle)member.Parent, HostBinaryCompatibilityTests.IsHostAssembly))
            {
                result.Add(
                    $"{HostBinaryCompatibilityTests.TypeName(metadata, (TypeReferenceHandle)member.Parent)}"
                    + $"::{metadata.GetString(member.Name)}");
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }
}
