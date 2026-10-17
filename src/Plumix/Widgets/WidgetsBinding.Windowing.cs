using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/binding.dart

namespace Plumix.Widgets;

/// <summary>The windowing half of Flutter's <c>WidgetsBinding</c>.</summary>
public partial class WidgetsBinding
{
    private WindowingOwner _windowingOwner = null!;

    /// <summary>
    /// The <see cref="Widgets.WindowingOwner"/> is responsible for creating and managing window controllers.
    /// </summary>
    /// <remarks>
    /// Dart's <c>@internal windowingOwner</c>. <see cref="InitInstances"/> installs
    /// <see cref="Windowing.CreateDefaultWindowingOwner"/>'s owner, which supports macOS, Linux and Windows;
    /// the setter can provide a custom one. Both throw <see cref="NotSupportedException"/> unless
    /// <see cref="Features.IsWindowingEnabled"/>.
    /// </remarks>
    public WindowingOwner WindowingOwner
    {
        get
        {
            Windowing.CheckEnabled();
            return _windowingOwner;
        }
        set
        {
            Windowing.CheckEnabled();
            _windowingOwner = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
