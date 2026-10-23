using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Plumix.Material;
using Xunit;

// C#-only test infrastructure: Dart is compiled from source with the engine it runs on, so it has no
// binary-compatibility gap between a package and its host.

namespace Plumix.Tests;

/// <summary>
/// Binds every external type and member reference of the shipped Plumix assemblies against the
/// Avalonia/SkiaSharp/HarfBuzzSharp assemblies loaded by the test run, the way the JIT binds them.
/// </summary>
/// <remarks>
/// A package compiled against one Avalonia and run on another fails only when the code path that
/// touches a removed member is jitted (0.3.0-alpha.3 threw MissingMethodException for
/// ShapedTextRun.IsReversed on the first text layout). On a normal run every assembly is built
/// against the same version and this test is trivially green; the CI compat job runs it with a
/// newer Avalonia substituted (AvaloniaTestVersion), where it covers paths no other test reaches.
/// </remarks>
public sealed class HostBinaryCompatibilityTests
{
    private static readonly string[] HostAssemblyPrefixes = ["Avalonia", "SkiaSharp", "HarfBuzzSharp"];

    public static TheoryData<string> ShippedAssemblies() =>
    [
        typeof(Plumix.Widgets.Widget).Assembly.GetName().Name!,
        typeof(MaterialApp).Assembly.GetName().Name!,
        typeof(Plumix.Cupertino.CupertinoApp).Assembly.GetName().Name!,
    ];

    [Theory]
    [MemberData(nameof(ShippedAssemblies))]
    public void EveryHostReferenceBindsAgainstTheLoadedHostAssemblies(string assemblyName)
    {
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Single(item => item.GetName().Name == assemblyName);
        Module module = assembly.ManifestModule;
        using var stream = File.OpenRead(assembly.Location);
        using var reader = new PEReader(stream);
        MetadataReader metadata = reader.GetMetadataReader();

        var failures = new List<string>();
        int checkedReferences = 0;
        foreach (TypeReferenceHandle handle in metadata.TypeReferences)
        {
            if (!IsHostScope(metadata, handle))
            {
                continue;
            }

            checkedReferences++;
            Bind(failures, () => module.ResolveType(MetadataTokens.GetToken(handle)),
                () => TypeName(metadata, handle));
        }

        foreach (MemberReferenceHandle handle in metadata.MemberReferences)
        {
            MemberReference member = metadata.GetMemberReference(handle);
            if (member.Parent.Kind != HandleKind.TypeReference
                || !IsHostScope(metadata, (TypeReferenceHandle)member.Parent))
            {
                // Members of generic host instantiations (TypeSpec parents) need a generic context to
                // bind; their open type is still checked through its TypeReference above.
                continue;
            }

            checkedReferences++;
            Bind(failures, () => module.ResolveMember(MetadataTokens.GetToken(handle)),
                () => $"{TypeName(metadata, (TypeReferenceHandle)member.Parent)}::{metadata.GetString(member.Name)}");
        }

        Assert.True(checkedReferences > 0, $"{assemblyName} has no host references to check.");
        Assert.True(failures.Count == 0,
            $"{assemblyName} references host members missing at run time:\n{string.Join('\n', failures)}");
    }

    private static void Bind(List<string> failures, Func<object?> resolve, Func<string> describe)
    {
        try
        {
            resolve();
        }
        catch (Exception error) when (error is MissingMemberException or TypeLoadException
                                          or FileNotFoundException or FileLoadException)
        {
            failures.Add($"{describe()}: {error.GetType().Name}: {error.Message}");
        }
    }

    private static bool IsHostScope(MetadataReader metadata, TypeReferenceHandle type)
    {
        EntityHandle scope = metadata.GetTypeReference(type).ResolutionScope;
        switch (scope.Kind)
        {
            case HandleKind.AssemblyReference:
                AssemblyReference reference = metadata.GetAssemblyReference((AssemblyReferenceHandle)scope);
                string name = metadata.GetString(reference.Name);
                return HostAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
            case HandleKind.TypeReference:
                return IsHostScope(metadata, (TypeReferenceHandle)scope);
            default:
                return false;
        }
    }

    private static string TypeName(MetadataReader metadata, TypeReferenceHandle handle)
    {
        TypeReference type = metadata.GetTypeReference(handle);
        string name = metadata.GetString(type.Name);
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return $"{TypeName(metadata, (TypeReferenceHandle)type.ResolutionScope)}+{name}";
        }

        string ns = metadata.GetString(type.Namespace);
        return ns.Length == 0 ? name : $"{ns}.{name}";
    }
}
