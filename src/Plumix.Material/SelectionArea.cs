using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
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
        SelectableRegionContextMenuBuilder? contextMenuBuilder = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Action<SelectedContent?>? onSelectionChanged = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        FocusNode = focusNode;
        SelectionControls = selectionControls;
        ContextMenuBuilder = contextMenuBuilder ?? DefaultContextMenuBuilder;
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

public sealed class SelectionAreaState : State
{
    private readonly GlobalKey<SelectableRegionState> _selectableRegionKey =
        new LabeledGlobalKey<SelectableRegionState>("SelectionArea");

    private SelectionArea Current => (SelectionArea)StateWidget;

    public SelectableRegionState SelectableRegion => _selectableRegionKey.CurrentState
        ?? throw new InvalidOperationException("SelectionArea is not mounted.");

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);

        // Dart picks handle controls per `Theme.of(context).platform`; the desktop
        // and Cupertino handle-control singletons are not available to this package
        // yet, so every platform uses the Material handles (see `DIVERGENCES.md`).
        TextSelectionControls controls = Current.SelectionControls
                                         ?? MaterialTextSelectionHandleControls.Instance;

        DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
        TextSelectionThemeData selectionTheme = TextSelectionTheme.Of(context);
        Color cursorColor = selectionStyle.CursorColor
                            ?? selectionTheme.CursorColor
                            ?? theme.PrimaryColor;
        Color selectionColor = selectionStyle.SelectionColor
                               ?? selectionTheme.SelectionColor
                               ?? ApplyOpacity(theme.PrimaryColor, 0.40);

        return new SelectableRegion(
            key: _selectableRegionKey,
            selectionControls: controls,
            focusNode: Current.FocusNode,
            contextMenuBuilder: Current.ContextMenuBuilder,
            magnifierConfiguration: Current.MagnifierConfiguration
                                    ?? TextMagnifier.AdaptiveMagnifierConfiguration,
            onSelectionChanged: Current.OnSelectionChanged,
            mouseCursor: selectionStyle.MouseCursor,
            child: new DefaultSelectionStyle(
                cursorColor: cursorColor,
                selectionColor: selectionColor,
                mouseCursor: selectionStyle.MouseCursor,
                child: Current.Child));
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1));
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
