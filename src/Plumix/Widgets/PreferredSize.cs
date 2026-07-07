using Avalonia;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/preferred_size.dart

public interface IPreferredSizeWidget
{
    Size PreferredSize { get; }
}

public sealed class PreferredSize : StatelessWidget, IPreferredSizeWidget
{
    public PreferredSize(Size preferredSize, Widget child, Key? key = null) : base(key)
    {
        Size = preferredSize;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Size Size { get; }
    public Widget Child { get; }

    Size IPreferredSizeWidget.PreferredSize => Size;

    public override Widget Build(BuildContext context) => Child;
}
