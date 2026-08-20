using Avalonia;
using Plumix.Foundation;
using Plumix.Widgets;

// Dart parity source: cupertino_ui/lib/src/desktop_text_selection.dart

namespace Plumix.Cupertino;

/// <summary>macOS-style text-selection controls and the legacy desktop Cupertino toolbar.</summary>
public class CupertinoDesktopTextSelectionControls : TextSelectionControls
{
    public static TextSelectionControls Instance { get; } = new CupertinoDesktopTextSelectionControls();

    public static TextSelectionControls HandleControls { get; } =
        new CupertinoDesktopTextSelectionHandleControls();

    public override Size GetHandleSize(double textLineHeight) => default;

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
        return new CupertinoDesktopTextSelectionControlsToolbar(
            clipboardStatus: clipboardStatus,
            endpoints: endpoints,
            globalEditableRegion: globalEditableRegion,
            handleCopy: CanCopy(@delegate) ? () => HandleCopy(@delegate) : null,
            handleCut: CanCut(@delegate) ? () => HandleCut(@delegate) : null,
            handlePaste: CanPaste(@delegate) ? () => HandlePaste(@delegate) : null,
            handleSelectAll: CanSelectAll(@delegate) ? () => HandleSelectAll(@delegate) : null,
            lastSecondaryTapDownPosition: lastSecondaryTapDownPosition,
            selectionMidpoint: selectionMidpoint,
            textLineHeight: textLineHeight);
    }

    public override Widget BuildHandle(
        BuildContext context,
        TextSelectionHandleType type,
        double textLineHeight,
        Action? onTap = null)
    {
        return new SizedBox();
    }

    public override Point GetHandleAnchor(TextSelectionHandleType type, double textLineHeight) => default;

    [Obsolete("Use a contextMenuBuilder instead. This feature was deprecated after Flutter v3.3.0-0.5.pre.")]
    public override void HandleSelectAll(ITextSelectionDelegate @delegate)
    {
        base.HandleSelectAll(@delegate);
        @delegate.HideToolbar();
    }
}

internal sealed class CupertinoDesktopTextSelectionHandleControls
    : CupertinoDesktopTextSelectionControls, ITextSelectionHandleControls
{
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

internal sealed class CupertinoDesktopTextSelectionControlsToolbar : StatefulWidget
{
    internal CupertinoDesktopTextSelectionControlsToolbar(
        IValueListenable<ClipboardStatus>? clipboardStatus,
        IReadOnlyList<TextSelectionPoint> endpoints,
        Rect globalEditableRegion,
        Action? handleCopy,
        Action? handleCut,
        Action? handlePaste,
        Action? handleSelectAll,
        Point? lastSecondaryTapDownPosition,
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
        LastSecondaryTapDownPosition = lastSecondaryTapDownPosition;
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

    internal Point? LastSecondaryTapDownPosition { get; }

    internal Point SelectionMidpoint { get; }

    internal double TextLineHeight { get; }

    public override State CreateState() => new CupertinoDesktopTextSelectionControlsToolbarState();

    private sealed class CupertinoDesktopTextSelectionControlsToolbarState : State
    {
        private CupertinoDesktopTextSelectionControlsToolbar Current =>
            (CupertinoDesktopTextSelectionControlsToolbar)StateWidget;

        public override void InitState()
        {
            Current.ClipboardStatus?.AddListener(HandleClipboardStatusChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var previous = (CupertinoDesktopTextSelectionControlsToolbar)oldWidget;
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
            CupertinoDesktopTextSelectionControlsToolbar widget = Current;
            if (widget.HandlePaste is not null
                && widget.ClipboardStatus?.Value == Plumix.Widgets.ClipboardStatus.Unknown)
            {
                return new SizedBox();
            }

            MediaQueryData mediaQuery = MediaQuery.Of(context);
            double midpointX = Math.Clamp(
                widget.SelectionMidpoint.X - widget.GlobalEditableRegion.Left,
                mediaQuery.Padding.Left,
                Math.Max(mediaQuery.Padding.Left, mediaQuery.Size.Width - mediaQuery.Padding.Right));
            Point midpointAnchor = new(
                midpointX,
                widget.SelectionMidpoint.Y - widget.GlobalEditableRegion.Top);

            CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
            var items = new List<Widget>();
            Widget divider = new SizedBox(width: 1.0 / mediaQuery.DevicePixelRatio);

            void AddToolbarButton(string text, Action onPressed)
            {
                if (items.Count > 0)
                {
                    items.Add(divider);
                }

                items.Add(CupertinoDesktopTextSelectionToolbarButton.TextButton(onPressed, text));
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

            return new CupertinoDesktopTextSelectionToolbar(
                anchor: widget.LastSecondaryTapDownPosition ?? midpointAnchor,
                children: items);
        }

        private void HandleClipboardStatusChanged()
        {
            SetState(static () => { });
        }
    }
}
