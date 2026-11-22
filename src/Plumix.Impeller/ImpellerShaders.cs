using System.Reflection;
using System.Text.RegularExpressions;
using NImpeller;
using Plumix.UI;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. Fragment programs for the Impeller prototype. The core's
// runtime-effect programs are SkSL (UI/RuntimeFragmentShader.cs), which Impeller cannot compile; Impeller
// takes impellerc's runtime-stage bundles, as Flutter's own `shaders:` assets are. The build compiles the
// pinned Flutter sources of the programs the framework ships (material_ui's ink_sparkle.frag) with the Flutter
// SDK's impellerc and embeds each bundle next to its GLSL source; a core program is matched to the bundle
// whose source declares the same uniforms in the same order, so its float uniforms (FragmentShader.setFloat
// order) feed the bundle unchanged.

/// <summary>The impellerc-compiled fragment programs this backend embeds.</summary>
internal static partial class ImpellerShaders
{
    private const string Prefix = "Plumix.Impeller.Shaders.";
    private static readonly Lazy<List<(string[] Uniforms, string Resource)>> s_bundles = new(LoadIndex);

    /// <summary>The Impeller program for <paramref name="program"/>, or null when none is embedded.</summary>
    internal static ImpellerFragmentProgram? Load(RuntimeFragmentProgram program)
    {
        string[] uniforms = [.. program.Layout.Select(entry => entry.Name)];
        foreach ((string[] bundleUniforms, string resource) in s_bundles.Value)
        {
            if (!bundleUniforms.SequenceEqual(uniforms))
            {
                continue;
            }

            using Stream stream = Assembly.GetManifestResourceStream(resource)!;
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            using var memory = new ImpellerUnmanagedMemory(copy.GetBuffer().AsSpan(0, (int)copy.Length));
            return ImpellerFragmentProgram.New(memory);
        }

        return null;
    }

    /// <summary>A color source running <paramref name="program"/> with <paramref name="uniforms"/>.</summary>
    internal static ImpellerColorSource? ColorSource(RuntimeFragmentProgram program, ReadOnlySpan<float> uniforms) =>
        program.BackendState is ImpellerFragmentProgram native && ImpellerBackend.Context is { } context
            ? ImpellerColorSource.CreateFragmentProgramNew(context, native, [], uniforms)
            : null;

    private static Assembly Assembly => typeof(ImpellerShaders).Assembly;

    // Each `<name>.frag.iplr` bundle is embedded with its `<name>.frag` source.
    private static List<(string[] Uniforms, string Resource)> LoadIndex()
    {
        var bundles = new List<(string[], string)>();
        foreach (string resource in Assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(Prefix, StringComparison.Ordinal)
                || !resource.EndsWith(".frag", StringComparison.Ordinal))
            {
                continue;
            }

            using var reader = new StreamReader(Assembly.GetManifestResourceStream(resource)!);
            string[] uniforms =
                [.. UniformPattern().Matches(reader.ReadToEnd()).Select(match => match.Groups[1].Value)];
            bundles.Add((uniforms, resource + ".iplr"));
        }

        return bundles;
    }

    // `layout(location = N) uniform vec4 u_color;`: samplers are not float uniforms and are skipped.
    [GeneratedRegex(@"^\s*(?:layout\s*\([^)]*\)\s*)?uniform\s+(?:float|half|vec[234])\s+(\w+)\s*;",
        RegexOptions.Multiline)]
    private static partial Regex UniformPattern();
}
