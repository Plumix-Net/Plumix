using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/selectable_text.dart

public sealed class SelectableText : StatefulWidget
{
    public SelectableText(
        string data,
        FocusNode? focusNode = null,
        TextStyle? style = null,
        TextAlign? textAlign = null,
        TextDirection? textDirection = null,
        bool showCursor = false,
        bool autofocus = false,
        int? minLines = null,
        int? maxLines = null,
        double cursorWidth = 2.0,
        double? cursorHeight = null,
        Color? cursorColor = null,
        Color? selectionColor = null,
        MouseCursor? mouseCursor = null,
        bool enableInteractiveSelection = true,
        Action? onTap = null,
        string? semanticsLabel = null,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged = null,
        SelectableRegionContextMenuBuilder? contextMenuBuilder = null,
        TextMagnifierConfiguration? magnifierConfiguration = null,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        FocusNode = focusNode;
        Style = style;
        TextAlign = textAlign;
        TextDirection = textDirection;
        ShowCursor = showCursor;
        Autofocus = autofocus;
        MinLines = minLines;
        MaxLines = maxLines;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        CursorColor = cursorColor;
        SelectionColor = selectionColor;
        MouseCursor = mouseCursor;
        EnableInteractiveSelection = enableInteractiveSelection;
        OnTap = onTap;
        SemanticsLabel = semanticsLabel;
        OnSelectionChanged = onSelectionChanged;
        ContextMenuBuilder = contextMenuBuilder ?? DefaultContextMenuBuilder;
        MagnifierConfiguration = magnifierConfiguration ?? TextMagnifier.AdaptiveMagnifierConfiguration;

        if (minLines.HasValue && minLines.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minLines));
        }
        if (maxLines.HasValue && maxLines.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines));
        }
        if (minLines.HasValue && maxLines.HasValue && minLines.Value > maxLines.Value)
        {
            throw new ArgumentException("minLines can't be greater than maxLines.");
        }
        if (!double.IsFinite(cursorWidth) || cursorWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cursorWidth));
        }
        if (cursorHeight.HasValue && (!double.IsFinite(cursorHeight.Value) || cursorHeight.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(cursorHeight));
        }
    }

    public string Data { get; }
    public FocusNode? FocusNode { get; }
    public TextStyle? Style { get; }
    public TextAlign? TextAlign { get; }
    public TextDirection? TextDirection { get; }
    public bool ShowCursor { get; }
    public bool Autofocus { get; }
    public int? MinLines { get; }
    public int? MaxLines { get; }
    public double CursorWidth { get; }
    public double? CursorHeight { get; }
    public Color? CursorColor { get; }
    public Color? SelectionColor { get; }
    public MouseCursor? MouseCursor { get; }
    public bool EnableInteractiveSelection { get; }
    public Action? OnTap { get; }
    public string? SemanticsLabel { get; }
    public Action<TextSelection, SelectionChangedCause?>? OnSelectionChanged { get; }
    public SelectableRegionContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
    public bool SelectionEnabled => EnableInteractiveSelection;

    public override State CreateState() => new SelectableTextState();

    private static Widget DefaultContextMenuBuilder(
        BuildContext context,
        SelectableRegionState selectableRegionState)
    {
        return AdaptiveTextSelectionToolbar.SelectableRegion(selectableRegionState);
    }
}

internal sealed class SelectableTextState : State
{
    private SelectableText Current => (SelectableText)StateWidget;

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        DefaultSelectionStyle selectionStyle = DefaultSelectionStyle.Of(context);
        TextSelectionThemeData selectionTheme = TextSelectionTheme.Of(context);
        var defaultStyle = DefaultTextStyle.Of(context);
        Color cursorColor = Current.CursorColor
                            ?? selectionStyle.CursorColor
                            ?? selectionTheme.CursorColor
                            ?? theme.PrimaryColor;
        Color selectionColor = Current.SelectionColor
                               ?? selectionStyle.SelectionColor
                               ?? selectionTheme.SelectionColor
                               ?? ApplyOpacity(theme.PrimaryColor, 0.40);
        var style = Current.Style;
        double fontSize = style?.FontSize ?? defaultStyle.FontSize ?? 14.0;
        double height = style?.Height ?? defaultStyle.Height ?? 1.0;

        Widget text = new Text(
            Current.Data,
            fontFamily: style?.FontFamily,
            fontSize: style?.FontSize,
            color: style?.Color,
            fontWeight: style?.FontWeight,
            fontStyle: style?.FontStyle,
            height: style?.Height,
            letterSpacing: style?.LetterSpacing,
            textAlign: Current.TextAlign ?? TextAlign.Start,
            textDirection: Current.TextDirection ?? Directionality.Of(context),
            softWrap: Current.MaxLines != 1,
            maxLines: Current.MaxLines);

        if (Current.MinLines.HasValue)
        {
            text = new ConstrainedBox(
                constraints: new BoxConstraints(MinHeight: fontSize * height * Current.MinLines.Value),
                child: text);
        }

        Widget result = new SelectableRegion(
            child: text,
            focusNode: Current.FocusNode,
            autofocus: Current.Autofocus,
            enabled: Current.EnableInteractiveSelection,
            selectionColor: selectionColor,
            cursorColor: cursorColor,
            showCursor: Current.ShowCursor,
            cursorWidth: Current.CursorWidth,
            cursorHeight: Current.CursorHeight,
            mouseCursor: Current.MouseCursor ?? selectionStyle.MouseCursor,
            onTextSelectionChanged: Current.OnSelectionChanged,
            contextMenuBuilder: Current.ContextMenuBuilder,
            magnifierConfiguration: Current.MagnifierConfiguration,
            onTap: Current.OnTap);

        if (Current.SemanticsLabel is not null)
        {
            result = new Semantics(label: Current.SemanticsLabel, child: result);
        }

        return result;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1));
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
