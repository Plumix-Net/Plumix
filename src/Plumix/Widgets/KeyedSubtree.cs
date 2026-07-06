using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart
public sealed class KeyedSubtree : StatelessWidget
{
    public KeyedSubtree(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }
    public override Widget Build(BuildContext context) => Child;
}
