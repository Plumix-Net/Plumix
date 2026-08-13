using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/selectable_text.dart

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
        TextSelectionControls? selectionControls = null,
        Action? onTap = null,
        string? semanticsLabel = null,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
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
        SelectionControls = selectionControls;
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
    public TextSelectionControls? SelectionControls { get; }
    public Action? OnTap { get; }
    public string? SemanticsLabel { get; }
    public Action<TextSelection, SelectionChangedCause?>? OnSelectionChanged { get; }
    public EditableTextContextMenuBuilder? ContextMenuBuilder { get; }
    public TextMagnifierConfiguration MagnifierConfiguration { get; }
    public bool SelectionEnabled => EnableInteractiveSelection;

    public override State CreateState() => new SelectableTextState();

    private static Widget DefaultContextMenuBuilder(
        BuildContext context,
        EditableText.EditableTextState editableTextState)
    {
        return AdaptiveTextSelectionToolbar.EditableText(editableTextState);
    }
}

internal sealed class SelectableTextState : State
{
    private readonly TextEditingController _controller = new();
    private FocusNode? _focusNode;
    private bool _ownsFocusNode;
    private SelectableText Current => (SelectableText)StateWidget;

    public override void InitState()
    {
        _controller.Text = Current.Data;
        AttachFocusNode(Current.FocusNode);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (SelectableText)oldWidget;
        if (!string.Equals(previous.Data, Current.Data, StringComparison.Ordinal)) _controller.Text = Current.Data;
        if (!ReferenceEquals(previous.FocusNode, Current.FocusNode))
        {
            DetachFocusNode();
            AttachFocusNode(Current.FocusNode);
        }
    }

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

        Widget result = new EditableText(
            controller: _controller,
            focusNode: _focusNode,
            autofocus: Current.Autofocus,
            enabled: Current.EnableInteractiveSelection,
            multiline: Current.MaxLines != 1,
            fontSize: fontSize,
            textColor: style?.Color ?? defaultStyle.Color ?? Colors.Black,
            backgroundColor: Colors.Transparent,
            focusedBackgroundColor: Colors.Transparent,
            padding: new Thickness(0),
            style: style,
            readOnly: true,
            textAlign: Current.TextAlign ?? TextAlign.Start,
            textDirection: Current.TextDirection,
            enableInteractiveSelection: Current.EnableInteractiveSelection,
            selectionControls: Current.SelectionControls ?? MaterialTextSelectionHandleControls.Instance,
            selectionColor: selectionColor,
            cursorColor: cursorColor,
            mouseCursor: Current.MouseCursor ?? selectionStyle.MouseCursor,
            onSelectionChanged: Current.OnSelectionChanged,
            contextMenuBuilder: Current.ContextMenuBuilder,
            magnifierConfiguration: Current.MagnifierConfiguration);

        if (Current.MinLines.HasValue)
        {
            result = new ConstrainedBox(
                constraints: new BoxConstraints(MinHeight: fontSize * height * Current.MinLines.Value),
                child: result);
        }

        if (Current.OnTap is not null)
        {
            result = new GestureDetector(onTap: Current.OnTap, child: result);
        }

        if (Current.SemanticsLabel is not null)
        {
            result = new Semantics(label: Current.SemanticsLabel, child: result);
        }

        return result;
    }

    public override void Dispose()
    {
        DetachFocusNode();
        _controller.Dispose();
    }

    private void AttachFocusNode(FocusNode? focusNode)
    {
        _focusNode = focusNode ?? new FocusNode();
        _ownsFocusNode = focusNode is null;
    }

    private void DetachFocusNode()
    {
        if (_ownsFocusNode) _focusNode?.Dispose();
        _focusNode = null;
        _ownsFocusNode = false;
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1));
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
