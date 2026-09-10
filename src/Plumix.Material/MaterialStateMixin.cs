using System.Collections.Generic;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/material_state_mixin.dart

/// <summary>
/// Dart parity: `MaterialStateMixin`. C# has no mixins, so the members ship as an abstract
/// <see cref="State"/> subclass: every `State` Dart declares `with MaterialStateMixin` derives from
/// this instead.
/// </summary>
public abstract class MaterialStateMixin : State
{
    /// <summary>
    /// Dart's `materialStates`: the managed set of active states, designed to be passed to
    /// `WidgetStateProperty.resolve`.
    /// </summary>
    protected HashSet<WidgetState> MaterialStates { get; set; } = [];

    /// <summary>
    /// Dart's `updateMaterialState`: a callback factory that mutates <see cref="MaterialStates"/>
    /// and calls `setState`, forwarding to <paramref name="onChanged"/> only on a real change.
    /// </summary>
    protected Action<bool> UpdateMaterialState(WidgetState key, Action<bool>? onChanged = null)
    {
        return value =>
        {
            if (MaterialStates.Contains(key) == value)
            {
                return;
            }

            SetMaterialState(key, value);
            onChanged?.Invoke(value);
        };
    }

    /// Dart's `setMaterialState`.
    protected void SetMaterialState(WidgetState state, bool isSet)
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
    protected void AddMaterialState(WidgetState state)
    {
        if (MaterialStates.Add(state))
        {
            SetState(() => { });
        }
    }

    /// Dart's `removeMaterialState`.
    protected void RemoveMaterialState(WidgetState state)
    {
        if (MaterialStates.Remove(state))
        {
            SetState(() => { });
        }
    }

    /// Dart's `isDisabled`.
    protected bool IsDisabled => MaterialStates.Contains(WidgetState.Disabled);

    /// Dart's `isDragged`.
    protected bool IsDragged => MaterialStates.Contains(WidgetState.Dragged);

    /// Dart's `isErrored`.
    protected bool IsErrored => MaterialStates.Contains(WidgetState.Error);

    /// Dart's `isFocused`.
    protected bool IsFocused => MaterialStates.Contains(WidgetState.Focused);

    /// Dart's `isHovered`.
    protected bool IsHovered => MaterialStates.Contains(WidgetState.Hovered);

    /// Dart's `isPressed`.
    protected bool IsPressed => MaterialStates.Contains(WidgetState.Pressed);

    /// Dart's `isScrolledUnder`.
    protected bool IsScrolledUnder => MaterialStates.Contains(WidgetState.ScrolledUnder);

    /// Dart's `isSelected`.
    protected bool IsSelected => MaterialStates.Contains(WidgetState.Selected);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DiagnosticsProperty<IReadOnlySet<WidgetState>>(
            "materialStates",
            MaterialStates,
            defaultValue: new HashSet<WidgetState>()));
    }
}
