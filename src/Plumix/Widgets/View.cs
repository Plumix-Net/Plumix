// Dart parity source: flutter/packages/flutter/lib/src/widgets/view.dart

using Avalonia;
using Plumix.Foundation;

namespace Plumix.Widgets;

/// <summary>
/// The platform view associated with a widget subtree.
/// </summary>
public sealed class FlutterView
{
    public FlutterView(Size physicalSize, double devicePixelRatio = 1.0, int viewId = 0)
    {
        if (!double.IsFinite(devicePixelRatio) || devicePixelRatio <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(devicePixelRatio));
        }

        PhysicalSize = physicalSize;
        DevicePixelRatio = devicePixelRatio;
        ViewId = viewId;
    }

    /// <summary>The dimensions of the view in physical pixels.</summary>
    public Size PhysicalSize { get; private set; }

    /// <summary>The number of physical pixels per logical pixel.</summary>
    public double DevicePixelRatio { get; private set; }

    /// <summary>The platform identifier for this view.</summary>
    public int ViewId { get; private set; }

    internal void UpdateMetrics(Size physicalSize, double devicePixelRatio, int viewId)
    {
        PhysicalSize = physicalSize;
        DevicePixelRatio = devicePixelRatio;
        ViewId = viewId;
    }
}

/// <summary>
/// Makes a <see cref="FlutterView"/> available to descendants.
/// </summary>
public sealed class View : InheritedWidget
{
    public View(FlutterView view, Widget child, Key? key = null) : base(key)
    {
        ViewHandle = view ?? throw new ArgumentNullException(nameof(view));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>The platform view this widget exposes.</summary>
    public FlutterView ViewHandle { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(ViewHandle, ((View)oldWidget).ViewHandle);
    }

    /// <summary>Returns the view associated with <paramref name="context"/>, if one is visible.</summary>
    public static FlutterView? MaybeOf(BuildContext context)
    {
        return LookupBoundary.DependOnInheritedWidgetOfExactType<View>(context)?.ViewHandle;
    }

    /// <summary>Returns the view associated with <paramref name="context"/>.</summary>
    public static FlutterView Of(BuildContext context)
    {
        FlutterView? view = MaybeOf(context);
        if (view != null)
        {
            return view;
        }

        if (LookupBoundary.DebugIsHidingAncestorWidgetOfExactType<View>(context))
        {
            throw new InvalidOperationException(
                "View.Of was called with a context whose View ancestor is hidden by a LookupBoundary.");
        }

        throw new InvalidOperationException("No View ancestor found for the given BuildContext.");
    }
}
