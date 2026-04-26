using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (Directionality, approximate)

public sealed class Directionality : InheritedWidget
{
    public Directionality(
        TextDirection textDirection,
        Widget child,
        Key? key = null) : base(key)
    {
        TextDirection = textDirection;
        Child = child;
    }

    public TextDirection TextDirection { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

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
