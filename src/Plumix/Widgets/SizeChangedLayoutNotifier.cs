using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/size_changed_layout_notifier.dart

public sealed class SizeChangedLayoutNotification : LayoutChangedNotification
{
}

public sealed class SizeChangedLayoutNotifier : SingleChildRenderObjectWidget
{
    public SizeChangedLayoutNotifier(Widget? child = null, Key? key = null) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSizeChangedWithCallback(
            onLayoutChangedCallback: () => new SizeChangedLayoutNotification().Dispatch(context));
    }
}
