using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/interface_level.dart

/// <summary>The interface elevations a <see cref="CupertinoDynamicColor"/> can resolve against.</summary>
public enum CupertinoUserInterfaceLevelData
{
    /// <summary>The level of the window's base content.</summary>
    Base,

    /// <summary>The level of content visually above <see cref="Base"/>.</summary>
    Elevated,
}

/// <summary>Establishes the visual elevation used by descendant dynamic-color resolution.</summary>
public sealed class CupertinoUserInterfaceLevel : InheritedWidget
{
    public CupertinoUserInterfaceLevel(
        CupertinoUserInterfaceLevelData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CupertinoUserInterfaceLevelData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return ((CupertinoUserInterfaceLevel)oldWidget).Data != Data;
    }

    public static CupertinoUserInterfaceLevelData Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "CupertinoUserInterfaceLevel.Of() called with a context that does not contain a "
                   + "CupertinoUserInterfaceLevel.");
    }

    public static CupertinoUserInterfaceLevelData? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<CupertinoUserInterfaceLevel>()?.Data;
    }
}
