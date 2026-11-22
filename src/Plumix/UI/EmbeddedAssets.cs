using System.Reflection;

namespace Plumix.UI;

// C#-only infrastructure: no Dart parity source. Flutter bundles a package's assets into the app's asset
// manifest; a .NET package ships them inside its assembly. Assets embedded as plain EmbeddedResource items
// whose logical name is the asset path (Plumix.Material's and Plumix.Cupertino's fonts) resolve here, in
// the framework, without any host; every other asset goes to the host's asset source (ImageBackend.cs).

/// <summary>
/// Opens assets embedded as manifest resources: the asset <c>avares://&lt;assembly&gt;/&lt;path&gt;</c> is
/// the resource named <c>&lt;path&gt;</c> in that assembly.
/// </summary>
internal static class EmbeddedAssets
{
    private const string Scheme = "avares";

    /// <summary>The embedded asset at <paramref name="uri"/>, or null when no assembly embeds it.</summary>
    internal static Stream? TryOpen(Uri uri, Uri? baseUri)
    {
        if (Resolve(uri, baseUri) is not { } absolute || FindAssembly(absolute) is not { } assembly)
        {
            return null;
        }

        return assembly.GetManifestResourceStream(ResourcePath(absolute));
    }

    /// <summary>The embedded assets directly or transitively under <paramref name="directory"/>.</summary>
    internal static IEnumerable<Uri> GetAssets(Uri directory, Uri? baseUri)
    {
        if (Resolve(directory, baseUri) is not { } absolute || FindAssembly(absolute) is not { } assembly)
        {
            return [];
        }

        string prefix = ResourcePath(absolute);
        string name = assembly.GetName().Name!;
        return assembly.GetManifestResourceNames()
            .Where(resource => resource.StartsWith(prefix, StringComparison.Ordinal))
            .Select(resource => new Uri($"{Scheme}://{name}/{resource}"))
            .ToArray();
    }

    private static Uri? Resolve(Uri uri, Uri? baseUri)
    {
        if (!uri.IsAbsoluteUri)
        {
            if (baseUri is not { IsAbsoluteUri: true })
            {
                return null;
            }

            uri = new Uri(baseUri, uri);
        }

        return uri.Scheme == Scheme ? uri : null;
    }

    private static string ResourcePath(Uri uri) => Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');

    // Uri lowercases its host, so the assembly is matched by name ignoring case, the way Avalonia's asset
    // loader matches it.
    private static Assembly? FindAssembly(Uri uri)
    {
        string host = uri.Host;
        foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(loaded.GetName().Name, host, StringComparison.OrdinalIgnoreCase))
            {
                return loaded;
            }
        }

        string original = uri.OriginalString;
        int start = original.IndexOf("://", StringComparison.Ordinal) + 3;
        int end = original.IndexOf('/', start);
        string name = end > start ? original[start..end] : original[start..];
        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException
                                              or BadImageFormatException or ArgumentException)
        {
            return null;
        }
    }
}
