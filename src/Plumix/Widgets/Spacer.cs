using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/spacer.dart

namespace Plumix.Widgets;

/// <summary>Creates an adjustable, empty spacer that can flex to fill space in a
/// <see cref="Flex"/> container.</summary>
public sealed class Spacer : StatelessWidget
{
    public Spacer(int flex = 1, Key? key = null) : base(key)
    {
        if (flex <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flex), "Flex must be greater than zero.");
        }

        Flex = flex;
    }

    /// <summary>The flex factor to use in determining how much space to take up.</summary>
    public int Flex { get; }

    public override Widget Build(BuildContext context)
    {
        return new Expanded(flex: Flex, child: SizedBox.Shrink());
    }
}
