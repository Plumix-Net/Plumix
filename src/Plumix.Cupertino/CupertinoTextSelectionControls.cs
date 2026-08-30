using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: cupertino_ui/lib/src/text_selection.dart

namespace Plumix.Cupertino;

/// <summary>iOS-style text-selection handles and the legacy Cupertino selection toolbar.</summary>
public class CupertinoTextSelectionControls : TextSelectionControls
{
    internal const double SelectionHandleOverlap = 1.5;
    internal const double SelectionHandleRadius = 6.0;
    internal const double ArrowScreenPadding = 26.0;

    public static TextSelectionControls Instance { get; } = new CupertinoTextSelectionControls();

    public override Size GetHandleSize(double textLineHeight)
    {
        return new Size(
            SelectionHandleRadius * 2.0,
            textLineHeight + SelectionHandleRadius * 2.0 - SelectionHandleOverlap);
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
        return new CupertinoTextSelectionControlsToolbar(
            clipboardStatus: clipboardStatus,
            endpoints: endpoints,
            globalEditableRegion: globalEditableRegion,
            handleCopy: CanCopy(@delegate) ? () => HandleCopy(@delegate) : null,
            handleCut: CanCut(@delegate) ? () => HandleCut(@delegate) : null,
            handlePaste: CanPaste(@delegate) ? () => HandlePaste(@delegate) : null,
            handleSelectAll: CanSelectAll(@delegate) ? () => HandleSelectAll(@delegate) : null,
            selectionMidpoint: selectionMidpoint,
            textLineHeight: textLineHeight);
    }

    public override Widget BuildHandle(
        BuildContext context,
        TextSelectionHandleType type,
        double textLineHeight,
        Action? onTap = null)
    {
        Size desiredSize = GetHandleSize(textLineHeight);
        if (type == TextSelectionHandleType.Collapsed)
        {
            return new SizedBox(width: desiredSize.Width, height: desiredSize.Height);
        }

        var customPaint = new CustomPaint(
            painter: new CupertinoTextSelectionHandlePainter(
                CupertinoTheme.Of(context).SelectionHandleColor.Value));
        Widget handle = new SizedBox(
            width: desiredSize.Width,
            height: desiredSize.Height,
            child: customPaint);

        if (type == TextSelectionHandleType.Left)
        {
            return handle;
        }

        Matrix4 transform = Matrix4.Identity();
        transform.TranslateByDouble(desiredSize.Width / 2.0, desiredSize.Height / 2.0, 0.0, 1.0);
        transform.RotateZ(Math.PI);
        transform.TranslateByDouble(-desiredSize.Width / 2.0, -desiredSize.Height / 2.0, 0.0, 1.0);
        return new Plumix.Widgets.Transform(transform, child: handle);
    }

    public override Point GetHandleAnchor(TextSelectionHandleType type, double textLineHeight)
    {
        Size handleSize = GetHandleSize(textLineHeight);
        return type switch
        {
            TextSelectionHandleType.Left => new Point(handleSize.Width / 2.0, handleSize.Height),
            TextSelectionHandleType.Right => new Point(
                handleSize.Width / 2.0,
                handleSize.Height - 2.0 * SelectionHandleRadius + SelectionHandleOverlap),
            _ => new Point(
                handleSize.Width / 2.0,
                textLineHeight + (handleSize.Height - textLineHeight) / 2.0),
        };
    }
}

/// <summary>iOS selection handles that leave toolbar construction to a context-menu builder.</summary>
[Obsolete("Use CupertinoTextSelectionControls instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
public sealed class CupertinoTextSelectionHandleControls
    : CupertinoTextSelectionControls, ITextSelectionHandleControls
{
    public static new TextSelectionControls Instance { get; } = new CupertinoTextSelectionHandleControls();

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

internal sealed class CupertinoTextSelectionHandlePainter : CustomPainter
{
    internal CupertinoTextSelectionHandlePainter(Color color)
    {
        Color = color;
    }

    internal Color Color { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        context.Canvas.DrawGeometry(new SolidColorBrush(Color), pen: null, BuildPath(size));
    }

    internal static Geometry BuildPath(Size size)
    {
        const double halfStrokeWidth = 1.0;
        double radius = CupertinoTextSelectionControls.SelectionHandleRadius;
        var circle = new EllipseGeometry(new Rect(0.0, 0.0, radius * 2.0, radius * 2.0));
        var line = new RectangleGeometry(new Rect(
            radius - halfStrokeWidth,
            radius * 2.0 - CupertinoTextSelectionControls.SelectionHandleOverlap,
            halfStrokeWidth * 2.0,
            Math.Max(0.0, size.Height - radius * 2.0
                + CupertinoTextSelectionControls.SelectionHandleOverlap)));
        return new CombinedGeometry(GeometryCombineMode.Union, circle, line);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not CupertinoTextSelectionHandlePainter oldPainter
               || oldPainter.Color != Color;
    }
}

internal sealed class CupertinoTextSelectionControlsToolbar : StatefulWidget
{
    internal CupertinoTextSelectionControlsToolbar(
        IValueListenable<ClipboardStatus>? clipboardStatus,
        IReadOnlyList<TextSelectionPoint> endpoints,
        Rect globalEditableRegion,
        Action? handleCopy,
        Action? handleCut,
        Action? handlePaste,
        Action? handleSelectAll,
        Point selectionMidpoint,
        double textLineHeight,
        Key? key = null) : base(key)
    {
        ClipboardStatus = clipboardStatus;
        Endpoints = endpoints;
        GlobalEditableRegion = globalEditableRegion;
        HandleCopy = handleCopy;
        HandleCut = handleCut;
        HandlePaste = handlePaste;
        HandleSelectAll = handleSelectAll;
        SelectionMidpoint = selectionMidpoint;
        TextLineHeight = textLineHeight;
    }

    internal IValueListenable<ClipboardStatus>? ClipboardStatus { get; }

    internal IReadOnlyList<TextSelectionPoint> Endpoints { get; }

    internal Rect GlobalEditableRegion { get; }

    internal Action? HandleCopy { get; }

    internal Action? HandleCut { get; }

    internal Action? HandlePaste { get; }

    internal Action? HandleSelectAll { get; }

    internal Point SelectionMidpoint { get; }

    internal double TextLineHeight { get; }

    public override State CreateState() => new CupertinoTextSelectionControlsToolbarState();

    private sealed class CupertinoTextSelectionControlsToolbarState : State
    {
        private CupertinoTextSelectionControlsToolbar Current =>
            (CupertinoTextSelectionControlsToolbar)StateWidget;

        public override void InitState()
        {
            Current.ClipboardStatus?.AddListener(HandleClipboardStatusChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var previous = (CupertinoTextSelectionControlsToolbar)oldWidget;
            if (ReferenceEquals(previous.ClipboardStatus, Current.ClipboardStatus))
            {
                return;
            }

            previous.ClipboardStatus?.RemoveListener(HandleClipboardStatusChanged);
            Current.ClipboardStatus?.AddListener(HandleClipboardStatusChanged);
        }

        public override void Dispose()
        {
            Current.ClipboardStatus?.RemoveListener(HandleClipboardStatusChanged);
        }

        public override Widget Build(BuildContext context)
        {
            CupertinoTextSelectionControlsToolbar widget = Current;
            if (widget.HandlePaste is not null
                && widget.ClipboardStatus?.Value == Plumix.Widgets.ClipboardStatus.Unknown)
            {
                return new SizedBox();
            }

            MediaQueryData mediaQuery = MediaQuery.Of(context);
            double leftLimit = CupertinoTextSelectionControls.ArrowScreenPadding + mediaQuery.Padding.Left;
            double rightLimit = mediaQuery.Size.Width
                                - mediaQuery.Padding.Right
                                - CupertinoTextSelectionControls.ArrowScreenPadding;
            double anchorX = Math.Clamp(
                widget.SelectionMidpoint.X + widget.GlobalEditableRegion.Left,
                leftLimit,
                Math.Max(leftLimit, rightLimit));
            double topAmountInEditableRegion = widget.Endpoints[0].Point.Y - widget.TextLineHeight;
            double anchorTop = Math.Max(topAmountInEditableRegion, 0.0) + widget.GlobalEditableRegion.Top;
            Point anchorAbove = new(anchorX, anchorTop);
            Point anchorBelow = new(
                anchorX,
                widget.Endpoints[^1].Point.Y + widget.GlobalEditableRegion.Top);

            CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
            var items = new List<Widget>();
            Widget divider = new SizedBox(width: 1.0 / mediaQuery.DevicePixelRatio);

            void AddToolbarButton(string text, Action onPressed)
            {
                if (items.Count > 0)
                {
                    items.Add(divider);
                }

                items.Add(CupertinoTextSelectionToolbarButton.TextButton(onPressed, text));
            }

            if (widget.HandleCut is not null)
            {
                AddToolbarButton(localizations.CutButtonLabel, widget.HandleCut);
            }

            if (widget.HandleCopy is not null)
            {
                AddToolbarButton(localizations.CopyButtonLabel, widget.HandleCopy);
            }

            if (widget.HandlePaste is not null
                && widget.ClipboardStatus?.Value == Plumix.Widgets.ClipboardStatus.Pasteable)
            {
                AddToolbarButton(localizations.PasteButtonLabel, widget.HandlePaste);
            }

            if (widget.HandleSelectAll is not null)
            {
                AddToolbarButton(localizations.SelectAllButtonLabel, widget.HandleSelectAll);
            }

            if (items.Count == 0)
            {
                return new SizedBox();
            }

            return new CupertinoTextSelectionToolbar(
                anchorAbove: anchorAbove,
                anchorBelow: anchorBelow,
                children: items);
        }

        private void HandleClipboardStatusChanged()
        {
            SetState(static () => { });
        }
    }
}
