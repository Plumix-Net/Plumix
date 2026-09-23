using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

public sealed class Directionality : UbiquitousInheritedWidget
{
    public Directionality(
        TextDirection textDirection,
        Widget child,
        Key? key = null) : base(child, key)
    {
        TextDirection = textDirection;
    }

    public TextDirection TextDirection { get; }

    public static TextDirection Of(BuildContext context)
    {
        WidgetsDebug.DebugCheckHasDirectionality(context);
        Directionality widget = context.DependOnInheritedWidgetOfExactType<Directionality>()!;
        return widget.TextDirection;
    }

    public static TextDirection? MaybeOf(BuildContext context)
    {
        return context.DependOnInheritedWidgetOfExactType<Directionality>()?.TextDirection;
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return TextDirection != ((Directionality)oldWidget).TextDirection;
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection));
    }
}
