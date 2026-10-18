using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/selection_area.dart

public sealed class SelectionArea : StatefulWidget
{
    public SelectionArea(
        Widget child,
        FocusNode? focusNode = null,
        TextSelectionControls? selectionControls = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Action<SelectedContent?>? onSelectionChanged = null,
        Key? key = null)
        : this(child, DefaultContextMenuBuilder, focusNode, selectionControls,
            magnifierConfiguration, onSelectionChanged, key)
    {
    }

    public SelectionArea(
        Widget child,
        SelectableRegionContextMenuBuilder? contextMenuBuilder,
        FocusNode? focusNode = null,
        TextSelectionControls? selectionControls = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Action<SelectedContent?>? onSelectionChanged = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        FocusNode = focusNode;
        SelectionControls = selectionControls;
        ContextMenuBuilder = contextMenuBuilder;
        MagnifierConfiguration = magnifierConfiguration;
        OnSelectionChanged = onSelectionChanged;
    }

    public Widget Child { get; }
    public FocusNode? FocusNode { get; }
    public TextSelectionControls? SelectionControls { get; }
    public SelectableRegionContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration? MagnifierConfiguration { get; }
    public Action<SelectedContent?>? OnSelectionChanged { get; }

    public override State CreateState() => new SelectionAreaState();

    private static Widget DefaultContextMenuBuilder(
        BuildContext context,
        SelectableRegionState selectableRegionState)
    {
        return AdaptiveTextSelectionToolbar.SelectableRegion(selectableRegionState);
    }
}

public sealed class SelectionAreaState : State<SelectionArea>
{
    private readonly GlobalKey<SelectableRegionState> _selectableRegionKey =
        new LabeledGlobalKey<SelectableRegionState>("SelectionArea");

    private SelectionArea Current => (SelectionArea)StateWidget;

    public SelectableRegionState SelectableRegion => _selectableRegionKey.CurrentState
        ?? throw new InvalidOperationException("SelectionArea is not mounted.");

    public override Widget Build(BuildContext context)
    {
        MaterialDebug.DebugCheckHasMaterialLocalizations(context);
        TextSelectionControls controls = Current.SelectionControls ?? Theme.Of(context).Platform switch
        {
            TargetPlatform.Android or TargetPlatform.Fuchsia => MaterialTextSelectionHandleControls.Instance,
            TargetPlatform.Linux or TargetPlatform.Windows => DesktopTextSelectionControls.HandleControls,
#pragma warning disable CS0618 // Pinned Flutter's handle-only controls.
            TargetPlatform.IOS => CupertinoTextSelectionHandleControls.Instance,
#pragma warning restore CS0618
            TargetPlatform.MacOS => CupertinoDesktopTextSelectionControls.HandleControls,
            _ => throw new ArgumentOutOfRangeException(),
        };

        return new SelectableRegion(
            key: _selectableRegionKey,
            selectionControls: controls,
            focusNode: Current.FocusNode,
            contextMenuBuilder: Current.ContextMenuBuilder,
            magnifierConfiguration: Current.MagnifierConfiguration
                                    ?? TextMagnifier.AdaptiveMagnifierConfiguration,
            onSelectionChanged: Current.OnSelectionChanged,
            child: Current.Child);
    }
}
