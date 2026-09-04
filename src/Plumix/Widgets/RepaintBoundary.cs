using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

namespace Plumix.Widgets;

/// <summary>Isolates repaint work for its child subtree.</summary>
public sealed class RepaintBoundary : SingleChildRenderObjectWidget
{
    public RepaintBoundary(Widget? child = null, Key? key = null) : base(child, key)
    {
    }

    public static RepaintBoundary Wrap(Widget child, int childIndex)
    {
        ArgumentNullException.ThrowIfNull(child);
        object keyValue = (object?)child.Key ?? childIndex;
        return new RepaintBoundary(child, new ValueKey<object>(keyValue));
    }

    public static IReadOnlyList<RepaintBoundary> WrapAll(IReadOnlyList<Widget> widgets)
    {
        ArgumentNullException.ThrowIfNull(widgets);
        var boundaries = new List<RepaintBoundary>(widgets.Count);
        for (int index = 0; index < widgets.Count; index++)
        {
            boundaries.Add(Wrap(widgets[index], index));
        }

        return boundaries;
    }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderRepaintBoundary();
}
