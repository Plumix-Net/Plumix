using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/ticker_provider.dart

namespace Plumix.Widgets;

public readonly record struct TickerModeData(bool Enabled, bool ForceFrames)
{
    public static TickerModeData Fallback { get; } = new(Enabled: true, ForceFrames: false);
}

public sealed class TickerMode : StatefulWidget
{
    public TickerMode(
        Widget child,
        bool enabled,
        bool forceFrames = false,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Enabled = enabled;
        ForceFrames = forceFrames;
    }

    public bool Enabled { get; }

    public bool ForceFrames { get; }

    public Widget Child { get; }

    public static bool Of(BuildContext context)
    {
        return context.DependOnInherited<EffectiveTickerMode>()?.Enabled ?? TickerModeData.Fallback.Enabled;
    }

    public static IValueListenable<bool> GetNotifier(BuildContext context)
    {
        EffectiveTickerMode? mode = context.GetInherited<EffectiveTickerMode>();
        return mode is null ? ConstantBoolListenable.True : mode.Notifier;
    }

    public static TickerModeData ValuesOf(BuildContext context)
    {
        return context.DependOnInherited<EffectiveTickerMode>()?.Values ?? TickerModeData.Fallback;
    }

    public static IValueListenable<TickerModeData> GetValuesNotifier(BuildContext context)
    {
        // Looking an inherited widget up from an unmounted context is illegal, which becomes a
        // problem when an animation controller is a late field only touched in State.Dispose().
        if (!context.Mounted)
        {
            return ConstantTickerModeDataListenable.Instance;
        }

        EffectiveTickerMode? mode = context.GetInherited<EffectiveTickerMode>();
        return mode is null ? ConstantTickerModeDataListenable.Instance : mode.ValuesNotifier;
    }

    public static Widget Merge(
        Widget child,
        bool? enabled = null,
        bool? forceFrames = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(child);
        return new Builder(context =>
        {
            EffectiveTickerMode? parent = context.DependOnInherited<EffectiveTickerMode>();
            bool parentEnabled = parent?.Enabled ?? TickerModeData.Fallback.Enabled;
            bool parentForceFrames = parent?.ForceFrames ?? TickerModeData.Fallback.ForceFrames;
            return new TickerMode(
                child: child,
                enabled: enabled ?? parentEnabled,
                forceFrames: forceFrames ?? parentForceFrames,
                key: key);
        });
    }

    public override State CreateState() => new TickerModeState();

    private sealed class TickerModeState : State
    {
        private bool _ancestorTickerMode = TickerModeData.Fallback.Enabled;
        private bool _ancestorForceFrames = TickerModeData.Fallback.ForceFrames;
        private readonly ValueNotifier<bool> _effectiveMode = new(TickerModeData.Fallback.Enabled);
        private readonly ValueNotifier<TickerModeData> _effectiveValues = new(TickerModeData.Fallback);

        private TickerMode CurrentWidget => (TickerMode)StateWidget;

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            EffectiveTickerMode? parent = Context.DependOnInherited<EffectiveTickerMode>();
            _ancestorTickerMode = parent?.Enabled ?? TickerModeData.Fallback.Enabled;
            _ancestorForceFrames = parent?.ForceFrames ?? TickerModeData.Fallback.ForceFrames;
            UpdateEffectiveMode();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            UpdateEffectiveMode();
        }

        public override void Dispose()
        {
            _effectiveMode.Dispose();
            _effectiveValues.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new EffectiveTickerMode(
                enabled: _effectiveMode.Value,
                forceFrames: _effectiveValues.Value.ForceFrames,
                notifier: _effectiveMode,
                valuesNotifier: _effectiveValues,
                child: CurrentWidget.Child);
        }

        private void UpdateEffectiveMode()
        {
            bool enabled = _ancestorTickerMode && CurrentWidget.Enabled;
            bool forceFrames = _ancestorForceFrames || CurrentWidget.ForceFrames;
            _effectiveMode.Value = enabled;
            _effectiveValues.Value = new TickerModeData(enabled, forceFrames);
        }
    }
}

internal sealed class EffectiveTickerMode : InheritedWidget
{
    public EffectiveTickerMode(
        bool enabled,
        bool forceFrames,
        ValueNotifier<bool> notifier,
        ValueNotifier<TickerModeData> valuesNotifier,
        Widget child) : base(key: null)
    {
        Enabled = enabled;
        ForceFrames = forceFrames;
        Notifier = notifier;
        ValuesNotifier = valuesNotifier;
        Child = child;
    }

    public bool Enabled { get; }

    public bool ForceFrames { get; }

    public ValueNotifier<bool> Notifier { get; }

    public ValueNotifier<TickerModeData> ValuesNotifier { get; }

    public TickerModeData Values => ValuesNotifier.Value;

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldMode = (EffectiveTickerMode)oldWidget;
        return Enabled != oldMode.Enabled || ForceFrames != oldMode.ForceFrames;
    }
}

internal sealed class ConstantBoolListenable : IValueListenable<bool>
{
    public static ConstantBoolListenable True { get; } = new();

    public bool Value => true;

    public void AddListener(Action listener)
    {
    }

    public void RemoveListener(Action listener)
    {
    }
}

internal sealed class ConstantTickerModeDataListenable : IValueListenable<TickerModeData>
{
    public static ConstantTickerModeDataListenable Instance { get; } = new();

    public TickerModeData Value => TickerModeData.Fallback;

    public void AddListener(Action listener)
    {
    }

    public void RemoveListener(Action listener)
    {
    }
}
