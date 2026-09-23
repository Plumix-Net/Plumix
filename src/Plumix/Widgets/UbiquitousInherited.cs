using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

namespace Plumix.Widgets;

/// <summary>An inherited widget optimized for many dependents and infrequent updates.</summary>
public abstract class UbiquitousInheritedWidget : InheritedWidget
{
    protected UbiquitousInheritedWidget(Widget child, Key? key = null) : base(child, key)
    {
    }

    public override Element CreateElement() => new UbiquitousInheritedElement(this);
}

/// <summary>Dart's _UbiquitousInheritedElement.</summary>
internal sealed class UbiquitousInheritedElement : InheritedElement
{
    public UbiquitousInheritedElement(UbiquitousInheritedWidget widget) : base(widget)
    {
    }

    public override void SetDependencies(Element dependent, object? value)
    {
        DebugAssertions.Assert(value is null);
    }

    public override object? GetDependencies(Element dependent) => null;

    public override void NotifyClients(ProxyWidget oldWidget)
    {
        RecurseChildren(this, (InheritedWidget)oldWidget);
    }

    private void RecurseChildren(Element element, InheritedWidget oldWidget)
    {
        element.VisitChildren(child => RecurseChildren(child, oldWidget));
        if (element.DoesDependOnInheritedElement(this))
        {
            NotifyDependent(oldWidget, element);
        }
    }
}
