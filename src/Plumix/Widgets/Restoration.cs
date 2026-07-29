using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/restoration.dart (root scope metadata)

public sealed class RootRestorationScope : InheritedWidget
{
    public RootRestorationScope(
        Widget child,
        string? restorationId = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        RestorationId = restorationId;
    }

    public Widget Child { get; }

    public string? RestorationId { get; }

    public static string? MaybeRestorationIdOf(BuildContext context)
    {
        return context.DependOnInherited<RootRestorationScope>()?.RestorationId;
    }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !string.Equals(
            ((RootRestorationScope)oldWidget).RestorationId,
            RestorationId,
            StringComparison.Ordinal);
    }
}
