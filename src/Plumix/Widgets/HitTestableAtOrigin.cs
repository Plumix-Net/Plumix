using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// C#-only shared infrastructure extracted from the private `_HitTestableAtOrigin` widgets in
// material_ui/lib/src/scaffold.dart and cupertino_ui/lib/src/page_scaffold.dart.

/// <summary>
/// An invisible, translucent hit-test target that lets scaffolds verify they are foregrounded before
/// responding to a platform status-bar tap.
/// </summary>
internal sealed class HitTestableAtOrigin : StatelessWidget
{
    public HitTestableAtOrigin(GlobalKey globalKey, Key? key = null) : base(key)
    {
        GlobalKey = globalKey ?? throw new ArgumentNullException(nameof(globalKey));
    }

    public GlobalKey GlobalKey { get; }

    public static bool IsHitTestableAtOrigin(GlobalKey key)
    {
        if (key.CurrentContext is not { } context
            || context.FindRenderObject() is not RenderMetaData renderObject
            || renderObject.Owner?.Root is not { } view)
        {
            return false;
        }

        var result = new BoxHitTestResult();
        view.HitTest(result, default);
        return result.Path.Any(entry => ReferenceEquals(entry.Target, renderObject));
    }

    public override Widget Build(BuildContext context)
    {
        return new MetaData(
            key: GlobalKey,
            behavior: HitTestBehavior.Translucent,
            child: new SizedBox(width: double.PositiveInfinity, height: double.PositiveInfinity));
    }
}
