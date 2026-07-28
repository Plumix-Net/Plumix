using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

public sealed class BackdropGroup : InheritedWidget
{
    public BackdropGroup(
        Widget child,
        BackdropKey? backdropKey = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        BackdropKey = backdropKey ?? new BackdropKey();
    }

    public Widget Child { get; }

    public BackdropKey BackdropKey { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((BackdropGroup)oldWidget).BackdropKey, BackdropKey);
    }

    public static BackdropGroup? Of(BuildContext context)
    {
        return context.DependOnInherited<BackdropGroup>();
    }
}

public sealed class BackdropFilter : SingleChildRenderObjectWidget
{
    private BackdropFilter(
        ImageFilter? filter,
        ImageFilterConfig? filterConfig,
        Widget? child,
        BlendMode blendMode,
        bool enabled,
        BackdropKey? backdropGroupKey,
        bool useSharedKey,
        Key? key) : base(child, key)
    {
        if ((filter is null) == (filterConfig is null))
        {
            throw new ArgumentException("Exactly one of filter or filterConfig must be provided.");
        }

        Filter = filter;
        FilterConfig = filterConfig;
        BlendMode = blendMode;
        Enabled = enabled;
        BackdropGroupKey = backdropGroupKey;
        UseSharedKey = useSharedKey;
    }

    public BackdropFilter(
        ImageFilter? filter = null,
        Widget? child = null,
        BlendMode blendMode = BlendMode.SourceOver,
        bool enabled = true,
        BackdropKey? backdropGroupKey = null,
        ImageFilterConfig? filterConfig = null,
        Key? key = null) : this(
            filter,
            filterConfig,
            child,
            blendMode,
            enabled,
            backdropGroupKey,
            useSharedKey: false,
            key)
    {
    }

    public ImageFilter? Filter { get; }

    public ImageFilterConfig? FilterConfig { get; }

    public BlendMode BlendMode { get; }

    public bool Enabled { get; }

    public BackdropKey? BackdropGroupKey { get; }

    private bool UseSharedKey { get; }

    public static BackdropFilter Grouped(
        ImageFilter? filter = null,
        Widget? child = null,
        BlendMode blendMode = BlendMode.SourceOver,
        bool enabled = true,
        ImageFilterConfig? filterConfig = null,
        Key? key = null)
    {
        return new BackdropFilter(
            filter,
            filterConfig,
            child,
            blendMode,
            enabled,
            backdropGroupKey: null,
            useSharedKey: true,
            key);
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderBackdropFilter(
            EffectiveFilterConfig,
            blendMode: BlendMode,
            enabled: Enabled,
            backdropKey: GetBackdropGroupKey(context));
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var backdropFilter = (RenderBackdropFilter)renderObject;
        backdropFilter.FilterConfig = EffectiveFilterConfig;
        backdropFilter.Enabled = Enabled;
        backdropFilter.BlendMode = BlendMode;
        backdropFilter.BackdropKey = GetBackdropGroupKey(context);
    }

    private ImageFilterConfig EffectiveFilterConfig
    {
        get => FilterConfig ?? new ImageFilterConfig(Filter!);
    }

    private BackdropKey? GetBackdropGroupKey(BuildContext context)
    {
        return UseSharedKey ? BackdropGroup.Of(context)?.BackdropKey : BackdropGroupKey;
    }
}
