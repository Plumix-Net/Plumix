using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/material/text_selection.dart

namespace Plumix.Material;

/// <summary>Android-style text selection handles and the legacy Material selection toolbar.</summary>
public class MaterialTextSelectionControls : TextSelectionControls
{
    internal const double HandleSize = 22.0;
    internal const double ToolbarContentDistanceBelow = HandleSize - 2.0;
    internal const double ToolbarContentDistance = 8.0;

    public static TextSelectionControls Instance { get; } = new MaterialTextSelectionControls();

    /// <summary>Material handles are a fixed size regardless of the line height they sit on.</summary>
    public override Size GetHandleSize(double textLineHeight) => new(HandleSize, HandleSize);

    public override Point GetHandleAnchor(TextSelectionHandleType type, double textLineHeight)
    {
        return type switch
        {
            TextSelectionHandleType.Collapsed => new Point(HandleSize / 2, -4),
            TextSelectionHandleType.Left => new Point(HandleSize, 0),
            _ => default,
        };
    }

    public override Widget BuildHandle(
        BuildContext context,
        TextSelectionHandleType type,
        double textLineHeight,
        Action? onTap = null)
    {
        ThemeData theme = Theme.Of(context);
        Color handleColor = TextSelectionTheme.Of(context).SelectionHandleColor
                            ?? theme.ColorScheme.Primary;
        Widget handle = new SizedBox(
            width: HandleSize,
            height: HandleSize,
            child: new CustomPaint(
                painter: new TextSelectionHandlePainter(handleColor),
                child: new GestureDetector(
                    onTap: onTap,
                    behavior: HitTestBehavior.Translucent)));

        // [handle] is a circle with a rectangle in its top left corner, so it points up and to the
        // left. Rotate it so the point lines up with the side of the selection it anchors.
        return type switch
        {
            TextSelectionHandleType.Left => new Widgets.Transform(
                transform: Matrix.CreateRotation(Math.PI / 2.0),
                alignment: Alignment.Center,
                child: handle),
            TextSelectionHandleType.Right => handle,
            _ => new Widgets.Transform(
                transform: Matrix.CreateRotation(Math.PI / 4.0),
                alignment: Alignment.Center,
                child: handle),
        };
    }

    /// <summary>
    /// Android allows Select All when the selection is not collapsed, unless everything has already
    /// been selected.
    /// </summary>
    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override bool CanSelectAll(ITextSelectionDelegate @delegate)
    {
        TextEditingValue value = @delegate.TextEditingValue;
        return @delegate.SelectAllEnabled
               && value.Text.Length > 0
               && !(value.Selection.Start == 0 && value.Selection.End == value.Text.Length);
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override Widget BuildToolbar(
        BuildContext context,
        Rect globalEditableRegion,
        double textLineHeight,
        Point selectionMidpoint,
        IReadOnlyList<TextSelectionPoint> endpoints,
        ITextSelectionDelegate @delegate,
        IValueListenable<ClipboardStatus>? clipboardStatus,
        Point? lastSecondaryTapDownPosition)
    {
        return new TextSelectionControlsToolbar(
            globalEditableRegion: globalEditableRegion,
            textLineHeight: textLineHeight,
            selectionMidpoint: selectionMidpoint,
            endpoints: endpoints,
            clipboardStatus: clipboardStatus,
            handleCut: CanCut(@delegate) ? () => HandleCut(@delegate) : null,
            handleCopy: CanCopy(@delegate) ? () => HandleCopy(@delegate) : null,
            handlePaste: CanPaste(@delegate) ? () => HandlePaste(@delegate) : null,
            handleSelectAll: CanSelectAll(@delegate) ? () => HandleSelectAll(@delegate) : null);
    }
}

/// <summary>Material selection handles that leave the toolbar to a context-menu builder.</summary>
public sealed class MaterialTextSelectionHandleControls
    : MaterialTextSelectionControls, ITextSelectionHandleControls
{
    public static new TextSelectionControls Instance { get; } = new MaterialTextSelectionHandleControls();

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override Widget BuildToolbar(
        BuildContext context,
        Rect globalEditableRegion,
        double textLineHeight,
        Point selectionMidpoint,
        IReadOnlyList<TextSelectionPoint> endpoints,
        ITextSelectionDelegate @delegate,
        IValueListenable<ClipboardStatus>? clipboardStatus,
        Point? lastSecondaryTapDownPosition)
    {
        return new SizedBox();
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override bool CanCut(ITextSelectionDelegate @delegate) => false;

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override bool CanCopy(ITextSelectionDelegate @delegate) => false;

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override bool CanPaste(ITextSelectionDelegate @delegate) => false;

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override bool CanSelectAll(ITextSelectionDelegate @delegate) => false;

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override void HandleCut(ITextSelectionDelegate @delegate)
    {
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override void HandleCopy(ITextSelectionDelegate @delegate)
    {
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override void HandlePaste(ITextSelectionDelegate @delegate)
    {
    }

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override void HandleSelectAll(ITextSelectionDelegate @delegate)
    {
    }
}

/// <summary>Draws a single text selection handle which points up and to the left.</summary>
internal sealed class TextSelectionHandlePainter : CustomPainter
{
    public TextSelectionHandlePainter(Color color)
    {
        Color = color;
    }

    public Color Color { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        context.DrawGeometry(new SolidColorBrush(Color), pen: null, BuildPath(size));
    }

    /// <summary>
    /// The circle unioned with the square corner, as one path, so a translucent handle colour never
    /// double-blends where the two shapes overlap.
    /// </summary>
    internal static Geometry BuildPath(Size size)
    {
        double radius = size.Width / 2.0;
        var circle = new EllipseGeometry(new Rect(0.0, 0.0, radius * 2, radius * 2));
        var point = new RectangleGeometry(new Rect(0.0, 0.0, radius, radius));
        return new CombinedGeometry(GeometryCombineMode.Union, circle, point);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not TextSelectionHandlePainter old || old.Color != Color;
    }
}

/// <summary>The legacy Material selection toolbar built from the deprecated control callbacks.</summary>
internal sealed class TextSelectionControlsToolbar : StatefulWidget
{
    public TextSelectionControlsToolbar(
        Rect globalEditableRegion,
        double textLineHeight,
        Point selectionMidpoint,
        IReadOnlyList<TextSelectionPoint> endpoints,
        IValueListenable<ClipboardStatus>? clipboardStatus = null,
        Action? handleCut = null,
        Action? handleCopy = null,
        Action? handlePaste = null,
        Action? handleSelectAll = null,
        Key? key = null) : base(key)
    {
        GlobalEditableRegion = globalEditableRegion;
        TextLineHeight = textLineHeight;
        SelectionMidpoint = selectionMidpoint;
        Endpoints = endpoints;
        ClipboardStatus = clipboardStatus;
        HandleCut = handleCut;
        HandleCopy = handleCopy;
        HandlePaste = handlePaste;
        HandleSelectAll = handleSelectAll;
    }

    public Rect GlobalEditableRegion { get; }

    public double TextLineHeight { get; }

    public Point SelectionMidpoint { get; }

    public IReadOnlyList<TextSelectionPoint> Endpoints { get; }

    public IValueListenable<ClipboardStatus>? ClipboardStatus { get; }

    public Action? HandleCut { get; }

    public Action? HandleCopy { get; }

    public Action? HandlePaste { get; }

    public Action? HandleSelectAll { get; }

    public override State CreateState() => new TextSelectionControlsToolbarState();

    private sealed class TextSelectionControlsToolbarState : State
    {
        private TextSelectionControlsToolbar CurrentWidget => (TextSelectionControlsToolbar)StateWidget;

        public override void InitState()
        {
            CurrentWidget.ClipboardStatus?.AddListener(HandleClipboardStatusChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var previous = (TextSelectionControlsToolbar)oldWidget;
            if (ReferenceEquals(previous.ClipboardStatus, CurrentWidget.ClipboardStatus))
            {
                return;
            }

            previous.ClipboardStatus?.RemoveListener(HandleClipboardStatusChanged);
            CurrentWidget.ClipboardStatus?.AddListener(HandleClipboardStatusChanged);
        }

        public override void Dispose()
        {
            CurrentWidget.ClipboardStatus?.RemoveListener(HandleClipboardStatusChanged);
        }

        public override Widget Build(BuildContext context)
        {
            TextSelectionControlsToolbar widget = CurrentWidget;

            // If there are no buttons to be shown, don't render anything.
            if (widget.HandleCut is null
                && widget.HandleCopy is null
                && widget.HandlePaste is null
                && widget.HandleSelectAll is null)
            {
                return new SizedBox();
            }

            // If the paste button is desired, don't render anything until the state of the clipboard
            // is known, since it's used to determine if paste is shown.
            if (widget.HandlePaste is not null
                && widget.ClipboardStatus?.Value == Widgets.ClipboardStatus.Unknown)
            {
                return new SizedBox();
            }

            TextSelectionPoint startTextSelectionPoint = widget.Endpoints[0];
            TextSelectionPoint endTextSelectionPoint = widget.Endpoints.Count > 1
                ? widget.Endpoints[1]
                : widget.Endpoints[0];
            Point anchorAbove = new(
                widget.GlobalEditableRegion.Left + widget.SelectionMidpoint.X,
                Math.Max(startTextSelectionPoint.Point.Y - widget.TextLineHeight, 0)
                + widget.GlobalEditableRegion.Top
                - MaterialTextSelectionControls.ToolbarContentDistance);
            Point anchorBelow = new(
                widget.GlobalEditableRegion.Left + widget.SelectionMidpoint.X,
                widget.GlobalEditableRegion.Top
                + endTextSelectionPoint.Point.Y
                + MaterialTextSelectionControls.ToolbarContentDistanceBelow);

            MaterialLocalizations localizations = MaterialLocalizations.Of(context);
            var itemDatas = new List<(Action OnPressed, string Label)>();
            if (widget.HandleCut is not null)
            {
                itemDatas.Add((widget.HandleCut, localizations.CutButtonLabel));
            }

            if (widget.HandleCopy is not null)
            {
                itemDatas.Add((widget.HandleCopy, localizations.CopyButtonLabel));
            }

            if (widget.HandlePaste is not null
                && widget.ClipboardStatus?.Value == Widgets.ClipboardStatus.Pasteable)
            {
                itemDatas.Add((widget.HandlePaste, localizations.PasteButtonLabel));
            }

            if (widget.HandleSelectAll is not null)
            {
                itemDatas.Add((widget.HandleSelectAll, localizations.SelectAllButtonLabel));
            }

            // If there is no option available, build an empty widget.
            if (itemDatas.Count == 0)
            {
                return new SizedBox();
            }

            var children = new List<Widget>(itemDatas.Count);
            for (int index = 0; index < itemDatas.Count; index++)
            {
                (Action onPressed, string label) = itemDatas[index];
                children.Add(new TextSelectionToolbarTextButton(
                    padding: TextSelectionToolbarTextButton.GetPadding(index, itemDatas.Count),
                    alignment: Alignment.CenterLeft,
                    onPressed: onPressed,
                    child: new Text(label)));
            }

            return new TextSelectionToolbar(
                anchorAbove: anchorAbove,
                anchorBelow: anchorBelow,
                children: children);
        }

        private void HandleClipboardStatusChanged()
        {
            SetState(static () => { });
        }
    }
}
