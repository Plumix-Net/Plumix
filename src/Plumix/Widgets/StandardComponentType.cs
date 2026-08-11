using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/standard_component_type.dart

public enum StandardComponentType
{
    BackButton,
    CloseButton,
    MoreButton,
    DrawerButton,
}

public static class StandardComponentTypeExtensions
{
    public static ValueKey<StandardComponentType> Key(this StandardComponentType component)
    {
        return new ValueKey<StandardComponentType>(component);
    }
}
