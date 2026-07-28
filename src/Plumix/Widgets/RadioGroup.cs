using System.Collections.Generic;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/radio_group.dart
public interface RadioClient<T>
{
    bool Tristate { get; }

    T RadioValue { get; }

    bool Enabled { get; }

    FocusNode FocusNode { get; }
}

public abstract class RadioGroupRegistry<T>
{
    public abstract T? GroupValue { get; }

    public abstract Action<T?> OnChanged { get; }

    public abstract void RegisterClient(RadioClient<T> radio);

    public abstract void UnregisterClient(RadioClient<T> radio);
}

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

        public RadioGroupState()
        {
            _registry = new RegistryImpl(this);
        }

        private RadioGroup<T> CurrentWidget => (RadioGroup<T>)StateWidget;

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            _registry.SyncTraversalPolicy();
            _registry.ValidateSingleSelection();
        }

        public override Widget Build(BuildContext context)
        {
            _registry.SyncTraversalPolicy();
            _registry.ValidateSingleSelection();

            return new Semantics(
                container: true,
                role: SemanticsRole.RadioGroup,
                child: new RadioGroupStateScope<T>(
                    registry: _registry,
                    groupValue: CurrentWidget.GroupValue,
                    child: CurrentWidget.Child));
        }

        public override void Dispose()
        {
            _registry.Dispose();
        }

        private sealed class RegistryImpl : RadioGroupRegistry<T>
        {
            private readonly RadioGroupState _state;
            private readonly List<RadioClient<T>> _radios = [];
            private readonly Dictionary<RadioClient<T>, FocusNode> _focusNodes = [];

            public RegistryImpl(RadioGroupState state)
            {
                _state = state;
            }

            public override T? GroupValue => _state.CurrentWidget.GroupValue;

            public override Action<T?> OnChanged => _state.CurrentWidget.OnChanged;

            public override void RegisterClient(RadioClient<T> radio)
            {
                ArgumentNullException.ThrowIfNull(radio);
                if (_radios.Contains(radio))
                {
                    return;
                }

                _radios.Add(radio);
                _focusNodes[radio] = radio.FocusNode;
                _focusNodes[radio].AddKeyEventHandler(HandleKeyEvent);
                SyncTraversalPolicy();
                ValidateSingleSelection();
            }

            public override void UnregisterClient(RadioClient<T> radio)
            {
                if (!_radios.Remove(radio))
                {
                    return;
                }

                FocusNode focusNode = _focusNodes.GetValueOrDefault(radio) ?? radio.FocusNode;
                focusNode.RemoveKeyEventHandler(HandleKeyEvent);
                _focusNodes.Remove(radio);
                focusNode.RemoveTraversalEligibility(this);

                SyncTraversalPolicy();
            }

            public void SyncTraversalPolicy()
            {
                RadioClient<T>? selected = _radios.FirstOrDefault(IsSelected);
                RadioClient<T>? traversalTarget = selected is { Enabled: true }
                    ? selected
                    : _radios.FirstOrDefault(radio => radio.Enabled);

                foreach (RadioClient<T> radio in _radios)
                {
                    FocusNode focusNode = _focusNodes.GetValueOrDefault(radio) ?? radio.FocusNode;
                    focusNode.SetTraversalEligibility(
                        this,
                        radio.Enabled && ReferenceEquals(radio, traversalTarget));
                }
            }

            public void ValidateSingleSelection()
            {
                int selectedCount = _radios.Count(IsSelected);
                if (selectedCount > 1)
                {
                    throw new InvalidOperationException(
                        "RadioGroup cannot contain more than one radio with the current group value.");
                }
            }

            public void Dispose()
            {
                foreach (RadioClient<T> radio in _radios.ToArray())
                {
                    UnregisterClient(radio);
                }
            }

            private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent keyEvent)
            {
                if (!keyEvent.IsDown)
                {
                    return IsRadioKey(keyEvent.Key) ? KeyEventResult.Handled : KeyEventResult.Ignored;
                }

                RadioClient<T>? focusedRadio = _radios.FirstOrDefault(radio =>
                    ReferenceEquals(_focusNodes.GetValueOrDefault(radio), node) && node.HasFocus);
                if (focusedRadio is null)
                {
                    return KeyEventResult.Ignored;
                }

                if (IsSpaceKey(keyEvent.Key))
                {
                    ToggleFocusedRadio(focusedRadio);
                    return KeyEventResult.Handled;
                }

                if (IsNextKey(keyEvent.Key))
                {
                    SelectInDirection(focusedRadio, forward: true);
                    return KeyEventResult.Handled;
                }

                if (IsPreviousKey(keyEvent.Key))
                {
                    SelectInDirection(focusedRadio, forward: false);
                    return KeyEventResult.Handled;
                }

                return KeyEventResult.Ignored;
            }

            private void ToggleFocusedRadio(RadioClient<T> radio)
            {
                if (!radio.Enabled)
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

            private void SelectInDirection(RadioClient<T> current, bool forward)
            {
                List<RadioClient<T>> enabledRadios = _radios.Where(radio => radio.Enabled).ToList();
                if (enabledRadios.Count < 2)
                {
                    return;
                }

                int currentIndex = enabledRadios.IndexOf(current);
                if (currentIndex < 0)
                {
                    return;
                }

                int nextIndex = forward
                    ? (currentIndex + 1) % enabledRadios.Count
                    : (currentIndex - 1 + enabledRadios.Count) % enabledRadios.Count;
                RadioClient<T> nextRadio = enabledRadios[nextIndex];
                OnChanged(nextRadio.RadioValue);
                (_focusNodes.GetValueOrDefault(nextRadio) ?? nextRadio.FocusNode).RequestFocus();
            }

            private bool IsSelected(RadioClient<T> radio)
            {
                return EqualityComparer<T?>.Default.Equals(radio.RadioValue, GroupValue);
            }

            private static bool IsRadioKey(string key)
            {
                return IsSpaceKey(key) || IsNextKey(key) || IsPreviousKey(key);
            }

            private static bool IsSpaceKey(string key)
            {
                return key is "Space" or "Spacebar";
            }

            private static bool IsNextKey(string key)
            {
                return key is "ArrowRight" or "Right" or "ArrowDown" or "Down";
            }

            private static bool IsPreviousKey(string key)
            {
                return key is "ArrowLeft" or "Left" or "ArrowUp" or "Up";
            }
        }
    }
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
