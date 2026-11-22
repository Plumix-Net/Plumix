using System.Reflection;
using Plumix.Rendering;
using Xunit;

// C#-only host tests: the assembly a relative asset key belongs to. Flutter's bundle has one asset
// manifest per app; Plumix maps a relative key to `avares://<application assembly>/<key>`.

namespace Plumix.Tests;

public sealed class PlatformAssetBundleTests
{
    private static readonly Assembly s_entryAssembly = typeof(PlatformAssetBundleTests).Assembly;

    [Fact]
    public void ResolveApplicationAssemblyName_PrefersTheHostApplicationAssembly()
    {
        string name = PlatformAssetBundle.ResolveApplicationAssemblyName("MyApp", s_entryAssembly);

        Assert.Equal("MyApp", name);
    }

    [Fact]
    public void ResolveApplicationAssemblyName_FallsBackToTheEntryAssembly()
    {
        string name = PlatformAssetBundle.ResolveApplicationAssemblyName(null, s_entryAssembly);

        Assert.Equal("Plumix.Tests", name);
    }

    [Fact]
    public void ResolveApplicationAssemblyName_WithoutEitherThrows()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => PlatformAssetBundle.ResolveApplicationAssemblyName(null, null));

        Assert.Contains("requires an entry assembly", error.Message, StringComparison.Ordinal);
    }

    // .NET for Android has no entry assembly: the host names the assembly of the Avalonia application.
    [Fact]
    public void DeriveApplicationAssemblyName_WithoutEntryAssembly_UsesTheApplicationType()
    {
        string? name = PlumixApplication.DeriveApplicationAssemblyName(typeof(TestApplication), null);

        Assert.Equal("Plumix.Tests", name);
    }

    [Fact]
    public void DeriveApplicationAssemblyName_WithEntryAssembly_LeavesResolutionToIt()
    {
        string? name = PlumixApplication.DeriveApplicationAssemblyName(typeof(TestApplication), s_entryAssembly);

        Assert.Null(name);
    }

    [Fact]
    public void DeriveApplicationAssemblyName_IgnoresAPlainAvaloniaApplication()
    {
        Assert.Null(PlumixApplication.DeriveApplicationAssemblyName(typeof(Avalonia.Application), null));
        Assert.Null(PlumixApplication.DeriveApplicationAssemblyName(null, null));
    }

    private sealed class TestApplication : Avalonia.Application;
}
