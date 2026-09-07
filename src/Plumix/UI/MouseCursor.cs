using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/services/mouse_cursor.dart

namespace Plumix.UI;

/// <summary>
/// Manages the duration that a pointing device should display a specific mouse cursor. Dart's
/// `MouseCursorSession`.
/// </summary>
/// <remarks>
/// While the mouse pointer is hovering over a region, that region's cursor owns a session: the
/// session is created when the cursor takes effect, and disposed when it is replaced by another
/// one. A session is a one-shot object — a cursor that becomes effective again gets a new session.
/// </remarks>
public abstract class MouseCursorSession
{
    /// <summary>Creates a session for <paramref name="cursor"/> on <paramref name="device"/>.</summary>
    protected MouseCursorSession(MouseCursor cursor, int device)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        Cursor = cursor;
        Device = device;
    }

    /// <summary>The cursor that created this session.</summary>
    public MouseCursor Cursor { get; }

    /// <summary>The device that is displaying <see cref="Cursor"/>.</summary>
    public int Device { get; }

    /// <summary>
    /// Makes the cursor take effect on the device. Dart's `MouseCursorSession.activate`; the caller
    /// does not await the returned task, so a failing platform call is not observed.
    /// </summary>
    public abstract Task Activate();

    /// <summary>Called when the session is no longer the current one. Dart's `dispose`.</summary>
    public abstract void Dispose();
}

/// <summary>
/// An interface for mouse cursor definitions. Dart's `MouseCursor`.
/// </summary>
/// <remarks>
/// A cursor is a stateless, immutable description; the mutable state lives in the
/// <see cref="MouseCursorSession"/> a cursor creates when it becomes effective on a device.
/// </remarks>
public abstract class MouseCursor : Diagnosticable
{
    /// <summary>Abstract const constructor. Dart's `const MouseCursor()`.</summary>
    protected MouseCursor()
    {
    }

    /// <summary>
    /// A special value that does not change the cursor by itself, but defers the choice to the
    /// region behind it in hit-test order. Dart's `MouseCursor.defer`.
    /// </summary>
    public static MouseCursor Defer { get; } = new DeferringMouseCursor();

    /// <summary>
    /// A special value that does not change the cursor by itself, but blocks the regions behind it
    /// from changing it either, so the pointer keeps whatever cursor it had. Dart's
    /// `MouseCursor.uncontrolled` (`_NoopMouseCursor`).
    /// </summary>
    public static MouseCursor Uncontrolled { get; } = new NoopMouseCursor();

    /// <summary>
    /// A short description used in debug output. Dart's `MouseCursor.debugDescription`; it must not
    /// be empty, since <see cref="ToString(DiagnosticLevel)"/> returns it verbatim.
    /// </summary>
    public abstract string DebugDescription { get; }

    /// <summary>
    /// Associates this cursor with <paramref name="device"/>. Dart's `MouseCursor.createSession`;
    /// called by <see cref="MouseCursorManager"/> when the cursor becomes effective.
    /// </summary>
    protected internal abstract MouseCursorSession CreateSession(int device);

    /// <inheritdoc />
    public override string ToString(DiagnosticLevel minLevel)
    {
        return minLevel >= DiagnosticLevel.Info ? DebugDescription : base.ToString(minLevel);
    }

    /// <summary>
    /// The first cursor in <paramref name="cursors"/> that is not <see cref="Defer"/>, or null when
    /// they all defer. Dart's `_DeferringMouseCursor.firstNonDeferred`.
    /// </summary>
    internal static MouseCursor? FirstNonDeferred(IEnumerable<MouseCursor> cursors)
    {
        foreach (MouseCursor cursor in cursors)
        {
            if (!ReferenceEquals(cursor, Defer))
            {
                return cursor;
            }
        }

        return null;
    }

    private sealed class DeferringMouseCursor : MouseCursor
    {
        public override string DebugDescription => "defer";

        protected internal override MouseCursorSession CreateSession(int device)
        {
            throw new NotSupportedException("MouseCursor.Defer can not create a session.");
        }
    }

    private sealed class NoopMouseCursor : MouseCursor
    {
        public override string DebugDescription => "uncontrolled";

        protected internal override MouseCursorSession CreateSession(int device)
            => new NoopMouseCursorSession(this, device);
    }

    private sealed class NoopMouseCursorSession(MouseCursor cursor, int device)
        : MouseCursorSession(cursor, device)
    {
        public override Task Activate() => Task.CompletedTask;

        public override void Dispose()
        {
        }
    }
}

/// <summary>
/// A mouse cursor that is natively supported by the platform. Dart's `SystemMouseCursor`; instances
/// are only created by <see cref="SystemMouseCursors"/>.
/// </summary>
public sealed class SystemMouseCursor : MouseCursor
{
    internal SystemMouseCursor(string kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// A string that identifies the kind of cursor to the platform. Dart's `SystemMouseCursor.kind`;
    /// it is also this cursor's identity, since equality is defined on it.
    /// </summary>
    public string Kind { get; }

    /// <inheritdoc />
    public override string DebugDescription => $"{Diagnostics.ObjectRuntimeType(this, "SystemMouseCursor")}({Kind})";

    /// <inheritdoc />
    protected internal override MouseCursorSession CreateSession(int device)
        => new SystemMouseCursorSession(this, device);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is SystemMouseCursor other && other.GetType() == GetType() && other.Kind == Kind;

    /// <inheritdoc />
    public override int GetHashCode() => Kind.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<string>("kind", Kind, level: DiagnosticLevel.Debug));
    }

    private sealed class SystemMouseCursorSession(SystemMouseCursor cursor, int device)
        : MouseCursorSession(cursor, device)
    {
        public override Task Activate()
        {
            return SystemChannels.MouseCursor.InvokeMethod<object>(
                "activateSystemCursor",
                new Dictionary<string, object?>
                {
                    ["device"] = Device,
                    ["kind"] = ((SystemMouseCursor)Cursor).Kind,
                });
        }

        public override void Dispose()
        {
        }
    }
}

/// <summary>
/// A collection of system <see cref="MouseCursor"/>s. Dart's `SystemMouseCursors`; the declaration
/// order and every `kind` string match the Dart file.
/// </summary>
public static class SystemMouseCursors
{
    // STATUS

    /// <summary>Hide the cursor. Dart's `SystemMouseCursors.none`.</summary>
    public static SystemMouseCursor None { get; } = new("none");

    /// <summary>The platform-dependent basic cursor, usually an arrow.</summary>
    public static SystemMouseCursor Basic { get; } = new("basic");

    /// <summary>A cursor indicating a clickable link, usually a pointing hand.</summary>
    public static SystemMouseCursor Click { get; } = new("click");

    /// <summary>A cursor indicating an operation that will not be carried out.</summary>
    public static SystemMouseCursor Forbidden { get; } = new("forbidden");

    /// <summary>A cursor indicating the program is busy, usually an hourglass or a watch.</summary>
    public static SystemMouseCursor Wait { get; } = new("wait");

    /// <summary>A cursor indicating the program is busy but can still be interacted with.</summary>
    public static SystemMouseCursor Progress { get; } = new("progress");

    /// <summary>A cursor indicating that a context menu is available.</summary>
    public static SystemMouseCursor ContextMenu { get; } = new("contextMenu");

    /// <summary>A cursor indicating help information.</summary>
    public static SystemMouseCursor Help { get; } = new("help");

    // SELECTION

    /// <summary>A cursor indicating selectable text, usually an I-beam.</summary>
    public static SystemMouseCursor Text { get; } = new("text");

    /// <summary>A cursor indicating selectable vertical text.</summary>
    public static SystemMouseCursor VerticalText { get; } = new("verticalText");

    /// <summary>A cursor indicating selectable table cells.</summary>
    public static SystemMouseCursor Cell { get; } = new("cell");

    /// <summary>A cursor indicating precise selection, usually a crosshair.</summary>
    public static SystemMouseCursor Precise { get; } = new("precise");

    // DRAG-AND-DROP

    /// <summary>A cursor indicating moving something.</summary>
    public static SystemMouseCursor Move { get; } = new("move");

    /// <summary>A cursor indicating something that can be dragged, usually an open hand.</summary>
    public static SystemMouseCursor Grab { get; } = new("grab");

    /// <summary>A cursor indicating something that is being dragged, usually a closed hand.</summary>
    public static SystemMouseCursor Grabbing { get; } = new("grabbing");

    /// <summary>A cursor indicating that the current target will not accept a drop.</summary>
    public static SystemMouseCursor NoDrop { get; } = new("noDrop");

    /// <summary>A cursor indicating that an alias of something will be created.</summary>
    public static SystemMouseCursor Alias { get; } = new("alias");

    /// <summary>A cursor indicating that the dragged item will be copied.</summary>
    public static SystemMouseCursor Copy { get; } = new("copy");

    /// <summary>A cursor indicating that the dragged item will disappear when dropped.</summary>
    public static SystemMouseCursor Disappearing { get; } = new("disappearing");

    // RESIZING AND SCROLLING

    /// <summary>A cursor indicating scrolling in any direction.</summary>
    public static SystemMouseCursor AllScroll { get; } = new("allScroll");

    /// <summary>A cursor indicating horizontal resizing.</summary>
    public static SystemMouseCursor ResizeLeftRight { get; } = new("resizeLeftRight");

    /// <summary>A cursor indicating vertical resizing.</summary>
    public static SystemMouseCursor ResizeUpDown { get; } = new("resizeUpDown");

    /// <summary>A cursor indicating resizing along the top-left to bottom-right diagonal.</summary>
    public static SystemMouseCursor ResizeUpLeftDownRight { get; } = new("resizeUpLeftDownRight");

    /// <summary>A cursor indicating resizing along the top-right to bottom-left diagonal.</summary>
    public static SystemMouseCursor ResizeUpRightDownLeft { get; } = new("resizeUpRightDownLeft");

    /// <summary>A cursor indicating resizing the top edge.</summary>
    public static SystemMouseCursor ResizeUp { get; } = new("resizeUp");

    /// <summary>A cursor indicating resizing the bottom edge.</summary>
    public static SystemMouseCursor ResizeDown { get; } = new("resizeDown");

    /// <summary>A cursor indicating resizing the left edge.</summary>
    public static SystemMouseCursor ResizeLeft { get; } = new("resizeLeft");

    /// <summary>A cursor indicating resizing the right edge.</summary>
    public static SystemMouseCursor ResizeRight { get; } = new("resizeRight");

    /// <summary>A cursor indicating resizing the top-left corner.</summary>
    public static SystemMouseCursor ResizeUpLeft { get; } = new("resizeUpLeft");

    /// <summary>A cursor indicating resizing the top-right corner.</summary>
    public static SystemMouseCursor ResizeUpRight { get; } = new("resizeUpRight");

    /// <summary>A cursor indicating resizing the bottom-left corner.</summary>
    public static SystemMouseCursor ResizeDownLeft { get; } = new("resizeDownLeft");

    /// <summary>A cursor indicating resizing the bottom-right corner.</summary>
    public static SystemMouseCursor ResizeDownRight { get; } = new("resizeDownRight");

    /// <summary>A cursor indicating resizing a column, or moving a column divider.</summary>
    public static SystemMouseCursor ResizeColumn { get; } = new("resizeColumn");

    /// <summary>A cursor indicating resizing a row, or moving a row divider.</summary>
    public static SystemMouseCursor ResizeRow { get; } = new("resizeRow");

    // OTHER OPERATIONS

    /// <summary>A cursor indicating zooming in.</summary>
    public static SystemMouseCursor ZoomIn { get; } = new("zoomIn");

    /// <summary>A cursor indicating zooming out.</summary>
    public static SystemMouseCursor ZoomOut { get; } = new("zoomOut");
}

/// <summary>
/// Maintains the current mouse cursor state per device. Dart's `MouseCursorManager`, which
/// <see cref="Plumix.Rendering.MouseTracker"/> owns.
/// </summary>
public sealed class MouseCursorManager
{
    private readonly Dictionary<int, MouseCursorSession> _lastSession = [];

    /// <summary>Creates a manager whose fallback is <paramref name="fallbackMouseCursor"/>.</summary>
    public MouseCursorManager(MouseCursor fallbackMouseCursor)
    {
        ArgumentNullException.ThrowIfNull(fallbackMouseCursor);
        if (ReferenceEquals(fallbackMouseCursor, MouseCursor.Defer))
        {
            throw new ArgumentException("The fallback cursor must not defer.", nameof(fallbackMouseCursor));
        }

        FallbackMouseCursor = fallbackMouseCursor;
    }

    /// <summary>
    /// The cursor used when no region under the pointer chooses one. Dart's
    /// `MouseCursorManager.fallbackMouseCursor`.
    /// </summary>
    public MouseCursor FallbackMouseCursor { get; }

    /// <summary>
    /// The cursor <paramref name="device"/> is currently displaying, or null in release mode.
    /// Dart's `MouseCursorManager.debugDeviceActiveCursor`.
    /// </summary>
    public MouseCursor? DebugDeviceActiveCursor(int device)
    {
        MouseCursor? result = null;
        if (Constants.KDebugMode)
        {
            result = _lastSession.TryGetValue(device, out MouseCursorSession? session) ? session.Cursor : null;
        }

        return result;
    }

    /// <summary>
    /// Handles the changed candidate cursors for <paramref name="device"/>. Dart's
    /// `MouseCursorManager.handleDeviceCursorUpdate`: the first non-deferring candidate wins, and a
    /// removal drops the device's session without activating anything.
    /// </summary>
    public void HandleDeviceCursorUpdate(
        int device,
        PointerEvent? triggeringEvent,
        IEnumerable<MouseCursor> cursorCandidates)
    {
        ArgumentNullException.ThrowIfNull(cursorCandidates);
        if (triggeringEvent is PointerRemovedEvent)
        {
            _lastSession.Remove(device);
            return;
        }

        _lastSession.TryGetValue(device, out MouseCursorSession? lastSession);
        MouseCursor nextCursor = MouseCursor.FirstNonDeferred(cursorCandidates) ?? FallbackMouseCursor;
        if (lastSession?.Cursor.Equals(nextCursor) == true)
        {
            return;
        }

        MouseCursorSession nextSession = nextCursor.CreateSession(device);
        _lastSession[device] = nextSession;
        lastSession?.Dispose();
        _ = nextSession.Activate();
    }
}
