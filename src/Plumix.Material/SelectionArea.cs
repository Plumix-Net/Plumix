using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/selection_area.dart

public sealed class SelectionArea : StatefulWidget
{
    public SelectionArea(
        Widget child,
        FocusNode? focusNode = null,
        SelectableRegionContextMenuBuilder? contextMenuBuilder = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Action<SelectedContent?>? onSelectionChanged = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        FocusNode = focusNode;
        ContextMenuBuilder = contextMenuBuilder ?? DefaultContextMenuBuilder;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifier.AdaptiveMagnifierConfiguration;
        OnSelectionChanged = onSelectionChanged;
    }

    public Widget Child { get; }
    public FocusNode? FocusNode { get; }
    public SelectableRegionContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
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
        var theme = Theme.Of(context);
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
            child: Current.Child,
            focusNode: Current.FocusNode,
            selectionColor: selectionColor,
            cursorColor: cursorColor,
            mouseCursor: selectionStyle.MouseCursor,
            contextMenuBuilder: Current.ContextMenuBuilder,
            magnifierConfiguration: Current.MagnifierConfiguration,
            onSelectionChanged: Current.OnSelectionChanged);
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1));
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
