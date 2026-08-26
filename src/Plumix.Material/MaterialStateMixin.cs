using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/material_state_mixin.dart

/// <summary>
/// Dart parity: `MaterialStateMixin`. C# has no mixins, so the members ship as an abstract
/// <see cref="State"/> subclass: every `State` Dart declares `with MaterialStateMixin` derives from
/// this instead. The managed set is Material's `[Flags] MaterialState` rather than Dart's
/// `Set&lt;WidgetState&gt;` (`docs/ai/DIVERGENCES.md`), so `materialStates` is a flag mask.
/// </summary>
public abstract class MaterialStateMixin : State
{
    /// <summary>
    /// Dart's `materialStates`: the managed set of active states, designed to be passed to
    /// `WidgetStateProperty.resolve`.
    /// </summary>
    protected MaterialState MaterialStates { get; set; } = MaterialState.None;

    /// <summary>
    /// Dart's `updateMaterialState`: a callback factory that mutates <see cref="MaterialStates"/>
    /// and calls `setState`, forwarding to <paramref name="onChanged"/> only on a real change.
    /// </summary>
    protected Action<bool> UpdateMaterialState(MaterialState key, Action<bool>? onChanged = null)
    {
        return value =>
        {
            if (MaterialStates.HasFlag(key) == value)
            {
                return;
            }

            SetMaterialState(key, value);
            onChanged?.Invoke(value);
        };
    }

    /// Dart's `setMaterialState`.
    protected void SetMaterialState(MaterialState state, bool isSet)
    {
        if (isSet)
        {
            AddMaterialState(state);
        }
        else
        {
            RemoveMaterialState(state);
        }
    }

    /// Dart's `addMaterialState`.
    protected void AddMaterialState(MaterialState state)
    {
        if (MaterialStates.HasFlag(state))
        {
            return;
        }

        SetState(() => MaterialStates |= state);
    }

    /// Dart's `removeMaterialState`.
    protected void RemoveMaterialState(MaterialState state)
    {
        if ((MaterialStates & state) == MaterialState.None)
        {
            return;
        }

        SetState(() => MaterialStates &= ~state);
    }

    /// Dart's `isDisabled`.
    protected bool IsDisabled => MaterialStates.HasFlag(MaterialState.Disabled);

    /// Dart's `isDragged`.
    protected bool IsDragged => MaterialStates.HasFlag(MaterialState.Dragged);

    /// Dart's `isErrored`.
    protected bool IsErrored => MaterialStates.HasFlag(MaterialState.Error);

    /// Dart's `isFocused`.
    protected bool IsFocused => MaterialStates.HasFlag(MaterialState.Focused);

    /// Dart's `isHovered`.
    protected bool IsHovered => MaterialStates.HasFlag(MaterialState.Hovered);

    /// Dart's `isPressed`.
    protected bool IsPressed => MaterialStates.HasFlag(MaterialState.Pressed);

    /// Dart's `isSelected`.
    protected bool IsSelected => MaterialStates.HasFlag(MaterialState.Selected);
}
