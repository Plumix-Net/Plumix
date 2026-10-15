// Dart parity source: flutter/packages/flutter/lib/src/foundation/service_extensions.dart

namespace Plumix.Foundation;

/// <summary>Service extension constants for the foundation library.</summary>
/// <remarks>
/// Flutter's <c>FoundationServiceExtensions</c>. These constants are used when registering service
/// extensions in the framework (<see cref="BindingBase"/>), and by tools and services that call them.
/// </remarks>
public enum FoundationServiceExtensions
{
    /// <summary>Causes the entire application to redraw; <c>reassemble</c>.</summary>
    Reassemble,

    /// <summary>Terminates the application; <c>exit</c>.</summary>
    Exit,

    /// <summary>Stores the URI of the connected VM service; <c>connectedVmServiceUri</c>.</summary>
    ConnectedVmServiceUri,

    /// <summary>Stores the address of the active DevTools server; <c>activeDevToolsServerAddress</c>.</summary>
    ActiveDevToolsServerAddress,

    /// <summary>Overrides the target platform; <c>platformOverride</c>.</summary>
    PlatformOverride,

    /// <summary>Overrides the platform brightness; <c>brightnessOverride</c>.</summary>
    BrightnessOverride,
}

/// <summary>Dart's <c>Enum.name</c> for <see cref="FoundationServiceExtensions"/>.</summary>
public static class FoundationServiceExtensionsExtensions
{
    /// <summary>The extension's Dart name, e.g. <c>reassemble</c>.</summary>
    public static string DartName(this FoundationServiceExtensions extension) => Diagnostics.EnumName(extension);
}
