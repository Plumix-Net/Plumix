using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (Directionality, approximate)

public sealed class Directionality : InheritedWidget
{
    public Directionality(
        TextDirection textDirection,
        Widget child,
        Key? key = null) : base(child, key)
    {
        TextDirection = textDirection;
    }

    public TextDirection TextDirection { get; }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return ((Directionality)oldWidget).TextDirection != TextDirection;
    }

    public static TextDirection Of(BuildContext context)
    {
        return MaybeOf(context) ?? TextDirection.Ltr;
    }

    public static TextDirection? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<Directionality>()?.TextDirection;
    }
}
