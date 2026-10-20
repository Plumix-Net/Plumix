using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/services/service_extensions.dart

namespace Plumix.UI;

public enum ServicesServiceExtensions
{
    ProfilePlatformChannels,
    Evict,
}

public static class ServicesServiceExtensionsExtensions
{
    public static string DartName(this ServicesServiceExtensions extension) => Diagnostics.EnumName(extension);
}
