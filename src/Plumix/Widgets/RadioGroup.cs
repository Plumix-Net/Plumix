using System.Collections.Generic;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/radio_group.dart
public sealed class RadioGroup<T> : StatefulWidget
{
    public RadioGroup(
        T? groupValue,
        Action<T?> onChanged,
        Widget child,
        Key? key = null) : base(key)
    {
        GroupValue = groupValue;
        OnChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public T? GroupValue { get; }

    public Action<T?> OnChanged { get; }

    public Widget Child { get; }

    public static RadioGroupRegistry<T>? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<RadioGroupStateScope<T>>()?.Registry;
    }

    public override State CreateState()
    {
        return new RadioGroupState();
    }

    private sealed class RadioGroupState : State
    {
        private readonly RegistryImpl _registry;
        private readonly IReadOnlyDictionary<ShortcutActivator, Intent> _shortcuts;
        private readonly RadioGroupShortcutManager _shortcutManager;

        public RadioGroupState()
        {
            _registry = new RegistryImpl(this);
            _shortcuts = new Dictionary<ShortcutActivator, Intent>
            {
                [new SingleActivator(LogicalKeyboardKey.ArrowLeft)] =
                    new VoidCallbackIntent(_registry.SelectPreviousRadio),
                [new SingleActivator(LogicalKeyboardKey.ArrowRight)] =
                    new VoidCallbackIntent(_registry.SelectNextRadio),
                [new SingleActivator(LogicalKeyboardKey.ArrowDown)] = new VoidCallbackIntent(_registry.SelectNextRadio),
                [new SingleActivator(LogicalKeyboardKey.ArrowUp)] =
                    new VoidCallbackIntent(_registry.SelectPreviousRadio),
                [new SingleActivator(LogicalKeyboardKey.Space)] = new VoidCallbackIntent(_registry.ToggleFocusedRadio),
            };
            _shortcutManager = new RadioGroupShortcutManager(_shortcuts, _registry);
        }

        private RadioGroup<T> CurrentWidget => (RadioGroup<T>)StateWidget;

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _registry.ValidateSingleSelection();
        }

        public override Widget Build(BuildContext context)
        {
            _registry.ValidateSingleSelection();
            Widget result = new RadioGroupStateScope<T>(
                registry: _registry,
                groupValue: CurrentWidget.GroupValue,
                child: CurrentWidget.Child);
            result = new FocusTraversalGroup(
                policy: new SkipUnselectedRadioPolicy(_registry.Radios, CurrentWidget.GroupValue),
                child: result);
            result = new Shortcuts(
                manager: _shortcutManager,
                child: result);
            return new Semantics(
                container: true,
                role: SemanticsRole.RadioGroup,
                child: result);
        }

        public override void Dispose()
        {
            _shortcutManager.Dispose();
            _registry.Dispose();
        }

        private sealed class RegistryImpl : RadioGroupRegistry<T>
        {
            private readonly RadioGroupState _state;
            private readonly HashSet<RadioClient<T>> _radios = [];

            public RegistryImpl(RadioGroupState state)
            {
                _state = state;
            }

            public IReadOnlyCollection<RadioClient<T>> Radios => _radios;

            public override T? GroupValue => _state.CurrentWidget.GroupValue;

            public override Action<T?> OnChanged => _state.CurrentWidget.OnChanged;

            public override void RegisterClient(RadioClient<T> radio)
            {
                ArgumentNullException.ThrowIfNull(radio);
                _radios.Add(radio);
                ValidateSingleSelection();
            }

            public override void UnregisterClient(RadioClient<T> radio)
            {
                _radios.Remove(radio);
            }

            public void ToggleFocusedRadio()
            {
                RadioClient<T>? radio = _radios.FirstOrDefault(candidate => candidate.FocusNode.HasFocus);
                if (radio == null)
                {
                    return;
                }

                if (!IsSelected(radio))
                {
                    OnChanged(radio.RadioValue);
                    return;
                }

                if (radio.Tristate)
                {
                    OnChanged(default);
                }
            }

            public void SelectNextRadio()
            {
                SelectRadioInDirection(forward: true);
            }

            public void SelectPreviousRadio()
            {
                SelectRadioInDirection(forward: false);
            }

            public void ValidateSingleSelection()
            {
                int selectedCount = _radios.Count(IsSelected);
                if (selectedCount > 1)
                {
                    throw new InvalidOperationException(
                        "RadioGroupPolicy can't be used for a radio group that allows multiple selection.");
                }
            }

            public void Dispose()
            {
                _radios.Clear();
            }

            private void SelectRadioInDirection(bool forward)
            {
                if (_radios.Count < 2)
                {
                    return;
                }

                FocusNode? currentFocus = _radios
                    .FirstOrDefault(radio => radio.FocusNode.HasFocus)
                    ?.FocusNode;
                if (currentFocus == null)
                {
                    return;
                }

                IReadOnlyList<FocusNode> sorted = ReadingOrderTraversalPolicy.Sort(
                    _radios
                        .Where(radio => radio.Enabled)
                        .Select(radio => radio.FocusNode)).ToList();
                if (sorted.Count == 0)
                {
                    return;
                }

                int currentIndex = IndexOfReference(sorted, currentFocus);
                int nextIndex = currentIndex < 0
                    ? 0
                    : forward
                        ? (currentIndex + 1) % sorted.Count
                        : (currentIndex - 1 + sorted.Count) % sorted.Count;
                FocusNode nextFocus = sorted[nextIndex];
                RadioClient<T> radioToSelect = _radios.First(radio => ReferenceEquals(radio.FocusNode, nextFocus));
                OnChanged(radioToSelect.RadioValue);
                nextFocus.RequestFocus();
            }

            private bool IsSelected(RadioClient<T> radio)
            {
                return EqualityComparer<T?>.Default.Equals(radio.RadioValue, GroupValue);
            }

            private static int IndexOfReference(IReadOnlyList<FocusNode> nodes, FocusNode target)
            {
                for (int index = 0; index < nodes.Count; index++)
                {
                    if (ReferenceEquals(nodes[index], target))
                    {
                        return index;
                    }
                }

                return -1;
            }
        }

        private sealed class RadioGroupShortcutManager : ShortcutManager
        {
            private readonly RegistryImpl _registry;

            public RadioGroupShortcutManager(
                IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts,
                RegistryImpl registry) : base(shortcuts)
            {
                _registry = registry;
            }

            public override KeyEventResult HandleKeypress(BuildContext context, KeyEvent @event)
            {
                bool radioHasFocus = _registry.Radios.Any(radio => radio.FocusNode.HasFocus);
                return radioHasFocus
                    ? base.HandleKeypress(context, @event)
                    : KeyEventResult.Ignored;
            }
        }

        private sealed class SkipUnselectedRadioPolicy : ReadingOrderTraversalPolicy
        {
            private readonly IReadOnlyCollection<RadioClient<T>> _radios;
            private readonly T? _groupValue;

            public SkipUnselectedRadioPolicy(
                IReadOnlyCollection<RadioClient<T>> radios,
                T? groupValue)
            {
                _radios = radios;
                _groupValue = groupValue;
            }

            public override IEnumerable<FocusNode> SortDescendants(
                IEnumerable<FocusNode> descendants,
                FocusNode currentNode)
            {
                List<FocusNode> nodesInReadingOrder =
                    base.SortDescendants(descendants, currentNode).ToList();
                RadioClient<T>? selected = _radios.FirstOrDefault(IsSelected);

                if (selected == null)
                {
                    var radiosByFocusNode = _radios.ToDictionary(radio => radio.FocusNode);
                    foreach (FocusNode node in nodesInReadingOrder)
                    {
                        if (radiosByFocusNode.TryGetValue(node, out selected))
                        {
                            break;
                        }
                    }
                }

                if (selected == null)
                {
                    return nodesInReadingOrder;
                }

                var nodesToSkip = _radios
                    .Where(radio => !ReferenceEquals(selected, radio)
                                    && !ReferenceEquals(radio.FocusNode, currentNode))
                    .Select(radio => radio.FocusNode)
                    .ToHashSet();
                return base.SortDescendants(
                    descendants.Where(node => !nodesToSkip.Contains(node)),
                    currentNode);
            }

            private bool IsSelected(RadioClient<T> radio)
            {
                return EqualityComparer<T?>.Default.Equals(radio.RadioValue, _groupValue);
            }
        }
    }
}

public abstract class RadioGroupRegistry<T>
{
    public abstract T? GroupValue { get; }

    public abstract Action<T?> OnChanged { get; }

    public abstract void RegisterClient(RadioClient<T> radio);

    public abstract void UnregisterClient(RadioClient<T> radio);
}

public interface RadioClient<T>
{
    bool Tristate { get; }

    T RadioValue { get; }

    bool Enabled { get; }

    FocusNode FocusNode { get; }
}

internal sealed class RadioGroupStateScope<T> : InheritedWidget
{
    public RadioGroupStateScope(
        RadioGroupRegistry<T> registry,
        T? groupValue,
        Widget child) : base()
    {
        Registry = registry;
        GroupValue = groupValue;
        Child = child;
    }

    public RadioGroupRegistry<T> Registry { get; }

    public T? GroupValue { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (RadioGroupStateScope<T>)oldWidget;
        return !ReferenceEquals(oldScope.Registry, Registry)
               || !EqualityComparer<T?>.Default.Equals(oldScope.GroupValue, GroupValue);
    }
}
