using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/gestures/events_test.dart

namespace Plumix.Tests;

/// <summary>
/// Ports Flutter's <c>events_test.dart</c> — the button helpers, the slop table, the default and
/// copied values of every event type, <c>fromMouseEvent</c>, and how <c>transformed</c> maps local
/// coordinates — plus the <c>transformed</c>/<c>copyWith</c>/<c>respond</c> contracts that
/// <c>events.dart</c> documents and its tests leave implicit.
/// </summary>
public sealed class PointerEventsTests
{
    private static readonly DateTime OneDay = DateTime.UnixEpoch.AddDays(1);
    private static readonly DateTime TwoSeconds = DateTime.UnixEpoch.AddSeconds(2);
    private static readonly Point Position = new(20, 30);

    private static readonly PointerDeviceKind[] ImpreciseKinds =
    [
        PointerDeviceKind.Stylus,
        PointerDeviceKind.InvertedStylus,
        PointerDeviceKind.Touch,
        PointerDeviceKind.Unknown,
        PointerDeviceKind.Trackpad,
    ];

    [Fact]
    public void ToString_IsOneLineAndDescribesThePosition()
    {
        var @event = new PointerDownEvent();

        string description = @event.ToString();

        AssertOneLine(description);
        Assert.StartsWith("PointerDownEvent#", description, StringComparison.Ordinal);
        Assert.EndsWith("(position: Offset(0.0, 0.0))", description, StringComparison.Ordinal);
    }

    [Fact]
    public void ToStringFull_IsOneLineAndListsEveryFineProperty()
    {
        var @event = new PointerDownEvent(pointer: 3, kind: PointerDeviceKind.Mouse);

        string description = @event.ToStringFull();

        AssertOneLine(description);
        Assert.Contains("pointer: 3", description, StringComparison.Ordinal);
        Assert.Contains("kind: mouse", description, StringComparison.Ordinal);
        Assert.Contains("buttons: 1", description, StringComparison.Ordinal);
        Assert.Contains("down: true", description, StringComparison.Ordinal);
        Assert.Contains("distanceMin: 0.0", description, StringComparison.Ordinal);
        Assert.Contains("viewId: 0", description, StringComparison.Ordinal);
        // A false flag with no `ifFalse` is hidden even at the fine level.
        Assert.DoesNotContain("obscured", description, StringComparison.Ordinal);
        Assert.DoesNotContain("synthesized", description, StringComparison.Ordinal);
    }

    [Fact]
    public void DebugFillProperties_FollowsThePointerEventDescriptionOrderAndLevels()
    {
        var @event = new PointerMoveEvent(
            position: new Point(1, 2),
            delta: new Point(3, 4),
            obscured: true,
            synthesized: true);
        var builder = new DiagnosticPropertiesBuilder();

        @event.DebugFillProperties(builder);

        Assert.Equal(
            [
                "position", "localPosition", "delta", "localDelta", "timeStamp", "pointer", "kind",
                "device", "buttons", "down", "pressure", "pressureMin", "pressureMax", "distance",
                "distanceMin", "distanceMax", "size", "radiusMajor", "radiusMinor", "radiusMin",
                "radiusMax", "orientation", "tilt", "platformData", "obscured", "synthesized",
                "embedderId", "viewId",
            ],
            builder.Properties.Select(property => property.Name));
        Assert.Equal(DiagnosticLevel.Info, builder.Properties[0].Level);
        // `localPosition` equals its default (`position`), so it drops to fine.
        Assert.Equal(DiagnosticLevel.Fine, builder.Properties[1].Level);
        Assert.Equal(DiagnosticLevel.Debug, builder.Properties[2].Level);
        Assert.Equal("obscured", builder.Properties[24].ToDescription());
        Assert.Equal("synthesized", builder.Properties[25].ToDescription());
    }

    [Fact]
    public void ScrollEvent_AddsScrollDeltaAtTheInfoLevel()
    {
        var @event = new PointerScrollEvent(scrollDelta: new Point(0, 20));

        Assert.EndsWith(
            "(position: Offset(0.0, 0.0), scrollDelta: Offset(0.0, 20.0))",
            @event.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TransformedEvent_ReportsTheDartWrapperType()
    {
        PointerEvent transformed = new PointerDownEvent().Transformed(Matrix4.TranslationValues(1, 2, 0));

        Assert.StartsWith("_TransformedPointerDownEvent#", transformed.ToStringShort(), StringComparison.Ordinal);
    }

    [Fact]
    public void NthButton_ControlTests()
    {
        Assert.Equal(PointerButtons.SecondaryMouse, PointerEventUtils.NthMouseButton(2));
        Assert.Equal(PointerButtons.SecondaryStylus, PointerEventUtils.NthStylusButton(2));
        Assert.Equal(PointerButtons.PrimaryMouse, PointerEventUtils.NthMouseButton(1));
        Assert.Equal((PointerButtons)(1L << 61), PointerEventUtils.NthMouseButton(62));
    }

    [Fact]
    public void ButtonConstants_MatchDartValues()
    {
        Assert.Equal(0x01, (long)PointerButtons.Primary);
        Assert.Equal(0x02, (long)PointerButtons.Secondary);
        Assert.Equal(0x01, (long)PointerButtons.PrimaryMouse);
        Assert.Equal(0x02, (long)PointerButtons.SecondaryMouse);
        Assert.Equal(0x01, (long)PointerButtons.StylusContact);
        Assert.Equal(0x02, (long)PointerButtons.PrimaryStylus);
        Assert.Equal(0x04, (long)PointerButtons.Tertiary);
        Assert.Equal(0x04, (long)PointerButtons.MiddleMouse);
        Assert.Equal(0x04, (long)PointerButtons.SecondaryStylus);
        Assert.Equal(0x08, (long)PointerButtons.BackMouse);
        Assert.Equal(0x10, (long)PointerButtons.ForwardMouse);
        Assert.Equal(0x01, (long)PointerButtons.TouchContact);
    }

    [Theory]
    [InlineData(0x0, 0x0)]
    [InlineData(0x1, 0x1)]
    [InlineData(0x200, 0x200)]
    [InlineData(0x220, 0x20)]
    public void SmallestButton(long buttons, long expected)
    {
        Assert.Equal((PointerButtons)expected, PointerEventUtils.SmallestButton((PointerButtons)buttons));
    }

    [Theory]
    [InlineData(0x0, false)]
    [InlineData(0x1, true)]
    [InlineData(0x200, true)]
    [InlineData(0x220, false)]
    public void IsSingleButton(long buttons, bool expected)
    {
        Assert.Equal(expected, PointerEventUtils.IsSingleButton((PointerButtons)buttons));
    }

    [Fact]
    public void ComputedHitSlopValues_AreBasedOnPointerDeviceKind()
    {
        Assert.Equal(
            GestureConstants.PrecisePointerHitSlop,
            PointerEventUtils.ComputeHitSlop(PointerDeviceKind.Mouse, null));
        Assert.Equal(
            GestureConstants.PrecisePointerPanSlop,
            PointerEventUtils.ComputePanSlop(PointerDeviceKind.Mouse, null));
        Assert.Equal(
            GestureConstants.PrecisePointerScaleSlop,
            PointerEventUtils.ComputeScaleSlop(PointerDeviceKind.Mouse));
        foreach (PointerDeviceKind kind in ImpreciseKinds)
        {
            Assert.Equal(GestureConstants.TouchSlop, PointerEventUtils.ComputeHitSlop(kind, null));
            Assert.Equal(GestureConstants.PanSlop, PointerEventUtils.ComputePanSlop(kind, null));
            Assert.Equal(GestureConstants.ScaleSlop, PointerEventUtils.ComputeScaleSlop(kind));
        }
    }

    [Fact]
    public void ComputedHitSlopValues_DeferToDeviceValueWhenPointerKindIsTouch()
    {
        var settings = new DeviceGestureSettings(TouchSlop: 1);

        Assert.Equal(
            GestureConstants.PrecisePointerHitSlop,
            PointerEventUtils.ComputeHitSlop(PointerDeviceKind.Mouse, settings));
        Assert.Equal(
            GestureConstants.PrecisePointerPanSlop,
            PointerEventUtils.ComputePanSlop(PointerDeviceKind.Mouse, settings));
        foreach (PointerDeviceKind kind in ImpreciseKinds)
        {
            Assert.Equal(1, PointerEventUtils.ComputeHitSlop(kind, settings));
            Assert.Equal(2, PointerEventUtils.ComputePanSlop(kind, settings));
        }
    }

    [Fact]
    public void FromMouseEvent_OfAHoverEvent()
    {
        var hover = new PointerHoverEvent(
            timestampUtc: OneDay,
            kind: PointerDeviceKind.Unknown,
            device: 10,
            position: new Point(101.0, 202.0),
            buttons: (PointerButtons)7,
            obscured: true,
            pressureMax: 2.1,
            pressureMin: 1.1,
            distance: 11,
            distanceMax: 110,
            size: 11,
            radiusMajor: 11,
            radiusMinor: 9,
            radiusMin: 1.1,
            radiusMax: 22,
            orientation: 1.1,
            tilt: 1.1,
            synthesized: true);

        PointerEnterEvent enter = PointerEnterEvent.FromMouseEvent(hover);
        PointerExitEvent exit = PointerExitEvent.FromMouseEvent(hover);

        AssertMouseEventCopied(hover, enter, new PointerEnterEvent());
        AssertMouseEventCopied(hover, exit, new PointerExitEvent());
        Assert.Equal(new PointerEnterEvent().Down, enter.Down);
        Assert.Equal(new PointerExitEvent().Down, exit.Down);
        Assert.Equal(hover.Distance, enter.Distance);
        Assert.Equal(hover.Distance, exit.Distance);
    }

    [Fact]
    public void FromMouseEvent_OfAMoveEvent()
    {
        var move = new PointerMoveEvent(
            timestampUtc: OneDay,
            kind: PointerDeviceKind.Unknown,
            device: 10,
            position: new Point(101.0, 202.0),
            buttons: (PointerButtons)7,
            obscured: true,
            pressureMax: 2.1,
            pressureMin: 1.1,
            distanceMax: 110,
            size: 11,
            radiusMajor: 11,
            radiusMinor: 9,
            radiusMin: 1.1,
            radiusMax: 22,
            orientation: 1.1,
            tilt: 1.1,
            synthesized: true);

        PointerEnterEvent enter = PointerEnterEvent.FromMouseEvent(move);
        PointerExitEvent exit = PointerExitEvent.FromMouseEvent(move);

        AssertMouseEventCopied(move, enter, new PointerEnterEvent());
        AssertMouseEventCopied(move, exit, new PointerExitEvent());
        Assert.True(enter.Down);
        Assert.True(exit.Down);
        Assert.Equal(0.0, enter.Distance);
        Assert.Equal(0.0, exit.Distance);
    }

    [Fact]
    public void FromMouseEvent_DropsEmbedderIdAndPlatformDataAndKeepsTheSourceTransform()
    {
        Matrix4 transform = Matrix4.TranslationValues(5, 6, 0);
        PointerEvent move = new PointerMoveEvent(
            position: new Point(1, 2),
            platformData: 9,
            embedderId: 12).Transformed(transform);

        PointerEnterEvent enter = PointerEnterEvent.FromMouseEvent(move);

        Assert.Equal(0, enter.EmbedderId);
        Assert.Equal(0, enter.PlatformData);
        Assert.Same(transform, enter.Transform);
        Assert.Equal(new Point(6, 8), enter.LocalPosition);
        Assert.IsType<PointerEnterEvent>(enter.Original);
    }

    [Fact]
    public void DefaultValues_OfEveryEventType()
    {
        Assert.Equal(PointerButtons.Primary, new PointerDownEvent().Buttons);
        Assert.Equal(PointerButtons.Primary, new PointerMoveEvent().Buttons);

        Assert.True(new PointerDownEvent().Down);
        Assert.True(new PointerMoveEvent().Down);
        Assert.False(new PointerUpEvent().Down);
        Assert.False(new PointerHoverEvent().Down);
        Assert.False(new PointerCancelEvent().Down);

        Assert.Equal(1.0, new PointerDownEvent().Pressure);
        Assert.Equal(1.0, new PointerMoveEvent().Pressure);
        Assert.Equal(0.0, new PointerUpEvent().Pressure);
        Assert.Equal(0.0, new PointerHoverEvent().Pressure);
        Assert.Equal(0.0, new PointerCancelEvent().Pressure);
        Assert.Equal(0.0, new PointerAddedEvent().Pressure);
        Assert.Equal(0.0, new PointerRemovedEvent().Pressure);
        Assert.Equal(0.0, new PointerEnterEvent().Pressure);
        Assert.Equal(0.0, new PointerExitEvent().Pressure);
        Assert.Equal(1.0, new PointerScrollEvent().Pressure);

        Assert.Equal(PointerDeviceKind.Touch, new PointerDownEvent().Kind);
        Assert.Equal(PointerDeviceKind.Mouse, new PointerScrollEvent().Kind);
        Assert.Equal(PointerDeviceKind.Mouse, new PointerScrollInertiaCancelEvent().Kind);
        Assert.Equal(PointerDeviceKind.Mouse, new PointerScaleEvent().Kind);
        Assert.Equal(PointerDeviceKind.Trackpad, new PointerPanZoomStartEvent().Kind);
        Assert.Equal(PointerDeviceKind.Trackpad, new PointerPanZoomUpdateEvent().Kind);
        Assert.Equal(PointerDeviceKind.Trackpad, new PointerPanZoomEndEvent().Kind);

        Assert.Equal(1.0, new PointerScaleEvent().Scale);
        Assert.Equal(default, new PointerScrollEvent().ScrollDelta);
        Assert.Equal(0, new PointerScrollEvent().Pointer);

        var down = new PointerDownEvent();
        Assert.Equal(1.0, down.PressureMin);
        Assert.Equal(1.0, down.PressureMax);
        Assert.Equal(0.0, down.DistanceMin);
        Assert.Equal(default, down.TimestampUtc);
        Assert.Null(down.Transform);
        Assert.Null(down.Original);
    }

    [Fact]
    public void PaintTransformToPointerEventTransform()
    {
        Matrix4 original = Matrix4.Identity();
        Assert.Equal(original, PointerEvent.RemovePerspectiveTransform(original));

        Matrix4 scaled = Matrix4.Identity();
        scaled.ScaleByDouble(3.0, 3.0, 3.0, 1.0);
        Matrix4 changed = PointerEvent.RemovePerspectiveTransform(scaled);

        Assert.NotEqual(scaled, changed);
        Matrix4 expected = scaled.Clone();
        var vector = new Vector4(0, 0, 1, 0);
        expected.SetColumn(2, vector);
        expected.SetRow(2, vector);
        Assert.Equal(expected, changed);
        // The input is not mutated.
        Assert.Equal(3.0, scaled.Storage[10]);
    }

    [Fact]
    public void TransformPosition()
    {
        var position = new Point(20, 30);

        Assert.Equal(position, PointerEvent.TransformPosition(null, position));
        Assert.Equal(position, PointerEvent.TransformPosition(Matrix4.Identity(), position));
        Assert.Equal(new Point(30, 50), PointerEvent.TransformPosition(Matrix4.TranslationValues(10, 20, 0), position));
    }

    [Fact]
    public void TransformPosition_AppliesThePerspectiveDivide()
    {
        Matrix4 transform = Matrix4.Identity();
        transform.SetEntry(3, 0, 0.01);

        // w = 0.01 * 20 + 1 = 1.2.
        Point result = PointerEvent.TransformPosition(transform, new Point(20, 30));

        Assert.Equal(20 / 1.2, result.X, 10);
        Assert.Equal(30 / 1.2, result.Y, 10);
    }

    [Fact]
    public void TransformDeltaViaPositions()
    {
        Matrix4 transform = Matrix4.Identity();
        transform.ScaleByDouble(2.0, 2.0, 1.0, 1.0);

        Assert.Equal(
            new Point(10, 10),
            PointerEvent.TransformDeltaViaPositions(
                untransformedEndPosition: new Point(20, 30),
                untransformedDelta: new Point(5, 5),
                transform: transform));
        Assert.Equal(
            new Point(10, 10),
            PointerEvent.TransformDeltaViaPositions(
                untransformedEndPosition: new Point(20, 30),
                transformedEndPosition: new Point(40, 60),
                untransformedDelta: new Point(5, 5),
                transform: transform));
        Assert.Equal(
            new Point(5, 5),
            PointerEvent.TransformDeltaViaPositions(
                untransformedEndPosition: new Point(20, 30),
                untransformedDelta: new Point(5, 5),
                transform: null));
    }

    public static TheoryData<string> TransformableEvents =>
    [
        "added", "cancel", "down", "enter", "exit", "hover", "move", "removed", "scroll",
        "panZoomStart", "panZoomUpdate", "panZoomEnd", "up",
    ];

    [Theory]
    [MemberData(nameof(TransformableEvents))]
    public void TransformingEvents(string name)
    {
        PointerEvent @event = CreateTransformable(name);
        Matrix4 scale = Matrix4.Identity();
        scale.ScaleByDouble(2.0, 2.0, 1.0, 1.0);
        Matrix4 transform = scale.Multiplied(Matrix4.TranslationValues(10.0, 20.0, 0.0));
        Point localPosition = new(60, 100);
        Point? localDelta = @event.Delta == new Point(5, 5) ? new Point(10, 10) : null;

        Assert.Equal(@event.Position, @event.LocalPosition);
        Assert.Equal(@event.Delta, @event.LocalDelta);
        Assert.Null(@event.Original);
        Assert.Null(@event.Transform);

        PointerEvent transformed = @event.Transformed(transform);

        Assert.Same(@event, transformed.Original);
        Assert.Equal(transform, transformed.Transform);
        Assert.Equal(localPosition, transformed.LocalPosition);
        Assert.Equal(localDelta ?? @event.LocalDelta, transformed.LocalDelta);
        Assert.IsType(@event.GetType(), transformed);
        Assert.Equal(@event.Buttons, transformed.Buttons);
        Assert.Equal(@event.Delta, transformed.Delta);
        Assert.Equal(@event.Device, transformed.Device);
        Assert.Equal(@event.Distance, transformed.Distance);
        Assert.Equal(@event.DistanceMax, transformed.DistanceMax);
        Assert.Equal(@event.DistanceMin, transformed.DistanceMin);
        Assert.Equal(@event.Down, transformed.Down);
        Assert.Equal(@event.Kind, transformed.Kind);
        Assert.Equal(@event.Obscured, transformed.Obscured);
        Assert.Equal(@event.Orientation, transformed.Orientation);
        Assert.Equal(@event.PlatformData, transformed.PlatformData);
        Assert.Equal(@event.Pointer, transformed.Pointer);
        Assert.Equal(@event.Position, transformed.Position);
        Assert.Equal(@event.Pressure, transformed.Pressure);
        Assert.Equal(@event.PressureMax, transformed.PressureMax);
        Assert.Equal(@event.PressureMin, transformed.PressureMin);
        Assert.Equal(@event.RadiusMajor, transformed.RadiusMajor);
        Assert.Equal(@event.RadiusMax, transformed.RadiusMax);
        Assert.Equal(@event.RadiusMin, transformed.RadiusMin);
        Assert.Equal(@event.RadiusMinor, transformed.RadiusMinor);
        Assert.Equal(@event.Size, transformed.Size);
        Assert.Equal(@event.Synthesized, transformed.Synthesized);
        Assert.Equal(@event.Tilt, transformed.Tilt);
        Assert.Equal(@event.TimestampUtc, transformed.TimestampUtc);
        Assert.Equal(@event.ViewId, transformed.ViewId);
        Assert.Equal(@event.EmbedderId, transformed.EmbedderId);
    }

    [Fact]
    public void Transformed_NullOrTheSameTransform_ReturnsAnUntransformedEventUntouched()
    {
        var down = new PointerDownEvent(position: Position);

        Assert.Same(down, down.Transformed(null));
    }

    [Fact]
    public void Transformed_AlwaysAppliesToTheOriginal()
    {
        var down = new PointerDownEvent(position: Position);
        Matrix4 first = Matrix4.TranslationValues(10, 0, 0);
        Matrix4 second = Matrix4.TranslationValues(0, 10, 0);

        PointerEvent once = down.Transformed(first);
        PointerEvent twice = once.Transformed(second);

        // Transforms are not composed: the second replaces the first.
        Assert.Same(down, twice.Original);
        Assert.Same(second, twice.Transform);
        Assert.Equal(new Point(20, 40), twice.LocalPosition);
        // A null transform on a transformed event returns the original itself.
        Assert.Same(down, once.Transformed(null));
        // The same matrix on a transformed event still builds a new transformed event.
        PointerEvent again = once.Transformed(first);
        Assert.NotSame(once, again);
        Assert.Same(down, again.Original);
    }

    [Fact]
    public void RemovedEvent_WithAnOriginal_TransformsThatOriginal()
    {
        var original = new PointerRemovedEvent(position: new Point(1, 1), device: 3);
        var removed = new PointerRemovedEvent(position: new Point(9, 9), original: original);
        Matrix4 transform = Matrix4.TranslationValues(1, 1, 0);

        PointerEvent transformed = removed.Transformed(transform);

        Assert.Same(original, removed.Original);
        Assert.Same(original, transformed.Original);
        Assert.Equal(new Point(1, 1), transformed.Position);
        Assert.Equal(new Point(2, 2), transformed.LocalPosition);
        Assert.Equal(3, transformed.Device);
    }

    [Fact]
    public void CopyWith_AddedEvent()
    {
        var added = new PointerAddedEvent(
            timestampUtc: OneDay,
            kind: PointerDeviceKind.Unknown,
            device: 10,
            position: new Point(101.0, 202.0),
            obscured: true,
            pressureMax: 2.1,
            pressureMin: 1.1,
            distance: 11,
            distanceMax: 110,
            radiusMin: 1.1,
            radiusMax: 22,
            orientation: 1.1,
            tilt: 1.1);
        var empty = new PointerAddedEvent();

        PointerAddedEvent copy = added.CopyWith(position: default(Point), timestampUtc: OneDay.AddDays(1));

        AssertCopied(added, copy);
        AssertKept(added, copy, "kind", "device", "buttons", "obscured", "pressureMin", "pressureMax",
            "distance", "distanceMax", "radiusMin", "radiusMax", "orientation", "tilt");
        AssertKept(empty, copy, "pointer", "delta", "down", "pressure", "size", "radiusMajor",
            "radiusMinor", "synthesized");
    }

    [Fact]
    public void CopyWith_HoverEvent()
    {
        var hover = new PointerHoverEvent(
            timestampUtc: OneDay,
            kind: PointerDeviceKind.Unknown,
            pointer: 1,
            device: 10,
            position: new Point(101.0, 202.0),
            buttons: (PointerButtons)7,
            obscured: true,
            pressureMax: 2.1,
            pressureMin: 1.1,
            distance: 11,
            distanceMax: 110,
            size: 11,
            radiusMajor: 11,
            radiusMinor: 9,
            radiusMin: 1.1,
            radiusMax: 22,
            orientation: 1.1,
            tilt: 1.1,
            synthesized: true);
        var empty = new PointerHoverEvent();

        PointerHoverEvent copy = hover.CopyWith(position: default(Point), timestampUtc: OneDay.AddDays(1));

        AssertCopied(hover, copy);
        AssertKept(hover, copy, "kind", "device", "buttons", "obscured", "pressureMin", "pressureMax",
            "distance", "distanceMax", "size", "radiusMajor", "radiusMinor", "radiusMin", "radiusMax",
            "orientation", "tilt", "synthesized");
        AssertKept(empty, copy, "pointer", "delta", "down", "pressure");
    }

    [Fact]
    public void CopyWith_DownEvent()
    {
        var down = new PointerDownEvent(
            timestampUtc: OneDay,
            kind: PointerDeviceKind.Unknown,
            pointer: 1,
            device: 10,
            position: new Point(101.0, 202.0),
            buttons: (PointerButtons)7,
            obscured: true,
            pressureMax: 2.1,
            pressureMin: 1.1,
            distanceMax: 110,
            size: 11,
            radiusMajor: 11,
            radiusMinor: 9,
            radiusMin: 1.1,
            radiusMax: 22,
            orientation: 1.1,
            tilt: 1.1);
        var empty = new PointerDownEvent();

        PointerDownEvent copy = down.CopyWith(position: default(Point), timestampUtc: OneDay.AddDays(1));

        AssertCopied(down, copy);
        AssertKept(down, copy, "pointer", "kind", "device", "buttons", "obscured", "pressureMin",
            "pressureMax", "distance", "distanceMax", "size", "radiusMajor", "radiusMinor", "radiusMin",
            "radiusMax", "orientation", "tilt");
        AssertKept(empty, copy, "delta", "down", "pressure", "synthesized");
    }

    [Fact]
    public void CopyWith_MoveEvent()
    {
        var move = new PointerMoveEvent(
            timestampUtc: OneDay,
            kind: PointerDeviceKind.Unknown,
            pointer: 1,
            device: 10,
            position: new Point(101.0, 202.0),
            delta: new Point(1.0, 2.0),
            buttons: (PointerButtons)7,
            obscured: true,
            pressureMax: 2.1,
            pressureMin: 1.1,
            distanceMax: 110,
            size: 11,
            radiusMajor: 11,
            radiusMinor: 9,
            radiusMin: 1.1,
            radiusMax: 22,
            orientation: 1.1,
            tilt: 1.1,
            platformData: 5,
            synthesized: true);
        var empty = new PointerMoveEvent();

        PointerMoveEvent copy = move.CopyWith(position: default(Point), timestampUtc: OneDay.AddDays(1));

        AssertCopied(move, copy);
        AssertKept(move, copy, "pointer", "kind", "device", "delta", "buttons", "down", "obscured",
            "pressureMin", "pressureMax", "distance", "distanceMax", "size", "radiusMajor", "radiusMinor",
            "radiusMin", "radiusMax", "orientation", "tilt", "synthesized");
        AssertKept(empty, copy, "pressure", "platformData");
    }

    [Fact]
    public void CopyWith_UpEvent()
    {
        var up = new PointerUpEvent(
            timestampUtc: OneDay,
            kind: PointerDeviceKind.Unknown,
            pointer: 1,
            device: 10,
            position: new Point(101.0, 202.0),
            buttons: (PointerButtons)7,
            obscured: true,
            pressureMax: 2.1,
            pressureMin: 1.1,
            distance: 11,
            distanceMax: 110,
            size: 11,
            radiusMajor: 11,
            radiusMinor: 9,
            radiusMin: 1.1,
            radiusMax: 22,
            orientation: 1.1,
            tilt: 1.1);
        var empty = new PointerUpEvent();

        PointerUpEvent copy = up.CopyWith(position: default(Point), timestampUtc: OneDay.AddDays(1));

        AssertCopied(up, copy);
        AssertKept(up, copy, "pointer", "kind", "device", "delta", "buttons", "obscured", "pressureMin",
            "pressureMax", "distance", "distanceMax", "size", "radiusMajor", "radiusMinor", "radiusMin",
            "radiusMax", "orientation", "tilt");
        AssertKept(empty, copy, "down", "pressure", "synthesized");
        // Dart's up `copyWith` accepts a `localPosition` and ignores it.
        Assert.Equal(up.Position, up.CopyWith(localPosition: new Point(7, 7)).LocalPosition);
    }

    [Fact]
    public void CopyWith_RemovedEvent()
    {
        var removed = new PointerRemovedEvent(
            timestampUtc: OneDay,
            kind: PointerDeviceKind.Unknown,
            pointer: 1,
            device: 10,
            position: new Point(101.0, 202.0),
            obscured: true,
            pressureMax: 2.1,
            pressureMin: 1.1,
            distanceMax: 110,
            radiusMin: 1.1,
            radiusMax: 22);
        var empty = new PointerRemovedEvent();

        PointerRemovedEvent copy = removed.CopyWith(position: default(Point), timestampUtc: OneDay.AddDays(1));

        AssertCopied(removed, copy);
        AssertKept(removed, copy, "kind", "device", "buttons", "obscured", "pressureMin", "pressureMax",
            "distanceMax", "radiusMin", "radiusMax");
        AssertKept(empty, copy, "pointer", "delta", "down", "pressure", "distance", "size", "radiusMajor",
            "radiusMinor", "orientation", "tilt", "synthesized");
    }

    [Fact]
    public void CopyWith_EnterAndExitDropPointerAndDown()
    {
        var enter = new PointerEnterEvent(pointer: 4, down: true, device: 2, buttons: PointerButtons.Primary);
        var exit = new PointerExitEvent(pointer: 4, down: true, device: 2, buttons: PointerButtons.Primary);

        PointerEnterEvent enterCopy = enter.CopyWith();
        PointerExitEvent exitCopy = exit.CopyWith();

        Assert.Equal(0, enterCopy.Pointer);
        Assert.False(enterCopy.Down);
        Assert.Equal(2, enterCopy.Device);
        Assert.Equal(0, exitCopy.Pointer);
        Assert.False(exitCopy.Down);
        Assert.Equal(PointerButtons.Primary, exitCopy.Buttons);
    }

    [Fact]
    public void CopyWith_OnATransformedEvent_ReturnsATransformedEvent()
    {
        var down = new PointerDownEvent(pointer: 2, position: Position);
        Matrix4 transform = Matrix4.TranslationValues(10, 20, 0);
        PointerEvent transformed = down.Transformed(transform);

        PointerEvent copy = transformed.CopyWith(position: new Point(1, 1));

        Assert.IsType<PointerDownEvent>(copy);
        Assert.Same(transform, copy.Transform);
        Assert.NotNull(copy.Original);
        Assert.NotSame(down, copy.Original);
        Assert.Equal(new Point(1, 1), copy.Position);
        Assert.Equal(new Point(11, 21), copy.LocalPosition);
        Assert.Equal(2, copy.Pointer);
    }

    [Fact]
    public void CopyWith_ScrollEvent_KeepsScrollDeltaAndForwardsTheResponse()
    {
        var responses = new List<bool>();
        var scroll = new PointerScrollEvent(
            position: Position,
            scrollDelta: new Point(0, 20),
            onRespond: allow => responses.Add(allow),
            device: 3);

        PointerScrollEvent copy = scroll.CopyWith(position: new Point(1, 1));
        copy.Respond(allowPlatformDefault: false);
        PointerScrollEvent overridden = scroll.CopyWith(onRespond: allow => responses.Add(!allow));
        overridden.Respond(allowPlatformDefault: false);
        PointerEvent baseCopy = ((PointerEvent)scroll).CopyWith(pointer: 9, buttons: PointerButtons.Primary);

        Assert.Equal(new Point(0, 20), copy.ScrollDelta);
        Assert.Equal(3, copy.Device);
        Assert.Equal([false, true], responses);
        Assert.Equal(0, baseCopy.Pointer);
        Assert.Equal(PointerButtons.None, baseCopy.Buttons);
    }

    [Fact]
    public void ScrollEvent_Transformed_RespondsThroughTheOriginal()
    {
        bool? response = null;
        var scroll = new PointerScrollEvent(position: Position, onRespond: allow => response = allow);

        var transformed = (PointerScrollEvent)scroll.Transformed(Matrix4.TranslationValues(1, 1, 0));
        transformed.Respond(allowPlatformDefault: true);

        Assert.True(response);
        Assert.Equal(scroll.ScrollDelta, transformed.ScrollDelta);
    }

    [Fact]
    public void ScaleEvent_CopyWithAndTransformed()
    {
        var scale = new PointerScaleEvent(position: Position, scale: 1.5, device: 4);

        PointerScaleEvent copy = scale.CopyWith(scale: 2.0);
        var transformed = (PointerScaleEvent)scale.Transformed(Matrix4.TranslationValues(1, 1, 0));

        Assert.Equal(2.0, copy.Scale);
        Assert.Equal(1.5, scale.CopyWith(position: new Point(1, 1)).Scale);
        Assert.Equal(1.5, transformed.Scale);
        Assert.Equal(new Point(21, 31), transformed.LocalPosition);
        // Scale is a signal; `respond` is the inherited no-op.
        transformed.Respond(allowPlatformDefault: false);
        Assert.IsAssignableFrom<PointerSignalEvent>(scale);
    }

    [Fact]
    public void ScrollInertiaCancelEvent_CopyWith()
    {
        var cancel = new PointerScrollInertiaCancelEvent(
            kind: PointerDeviceKind.Trackpad,
            position: Position,
            device: 5,
            embedderId: 6);

        PointerScrollInertiaCancelEvent copy = cancel.CopyWith(position: new Point(1, 1));

        Assert.Equal(PointerDeviceKind.Trackpad, copy.Kind);
        Assert.Equal(5, copy.Device);
        Assert.Equal(6, copy.EmbedderId);
        Assert.Equal(new Point(1, 1), copy.Position);
    }

    [Fact]
    public void PanZoomEvents_CopyWithKeepsTheTrackpadKindAndDropsPointerAndSynthesized()
    {
        var start = new PointerPanZoomStartEvent(pointer: 3, position: Position, device: 1, synthesized: true);
        var update = new PointerPanZoomUpdateEvent(
            pointer: 3,
            position: Position,
            pan: new Point(4, 6),
            panDelta: new Point(1, 2),
            scale: 2.5,
            rotation: 0.75,
            device: 1,
            synthesized: true);
        var end = new PointerPanZoomEndEvent(pointer: 3, position: Position, device: 1, synthesized: true);

        PointerPanZoomStartEvent startCopy = start.CopyWith(kind: PointerDeviceKind.Trackpad);
        PointerPanZoomUpdateEvent updateCopy = update.CopyWith(scale: 3.0, localPan: new Point(99, 99));
        PointerPanZoomEndEvent endCopy = end.CopyWith();

        Assert.Equal(0, startCopy.Pointer);
        Assert.False(startCopy.Synthesized);
        Assert.Equal(1, startCopy.Device);
        Assert.Equal(3.0, updateCopy.Scale);
        Assert.Equal(new Point(4, 6), updateCopy.Pan);
        Assert.Equal(new Point(4, 6), updateCopy.LocalPan);
        Assert.Equal(new Point(1, 2), updateCopy.PanDelta);
        Assert.Equal(0.75, updateCopy.Rotation);
        Assert.Equal(0, updateCopy.Pointer);
        Assert.Equal(0, endCopy.Pointer);
        Assert.False(endCopy.Synthesized);
        Assert.Throws<ArgumentOutOfRangeException>(() => start.CopyWith(kind: PointerDeviceKind.Mouse));
        Assert.Throws<ArgumentOutOfRangeException>(() => update.CopyWith(kind: PointerDeviceKind.Touch));
        Assert.Throws<ArgumentOutOfRangeException>(() => end.CopyWith(kind: PointerDeviceKind.Stylus));
    }

    [Fact]
    public void EnsureCertainEventTypesAreAllowed()
    {
        Assert.NotNull(new PointerHoverEvent(kind: PointerDeviceKind.Trackpad));
        Assert.NotNull(new PointerScrollInertiaCancelEvent(kind: PointerDeviceKind.Trackpad));
        Assert.NotNull(new PointerScrollEvent(kind: PointerDeviceKind.Trackpad));
        Assert.NotNull(new PointerAddedEvent(kind: PointerDeviceKind.Trackpad));
        Assert.NotNull(new PointerRemovedEvent(kind: PointerDeviceKind.Trackpad));
    }

    [Fact]
    public void EnsureCertainEventTypesAreNotAllowed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerDownEvent(kind: PointerDeviceKind.Trackpad));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerMoveEvent(kind: PointerDeviceKind.Trackpad));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerUpEvent(kind: PointerDeviceKind.Trackpad));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerCancelEvent(kind: PointerDeviceKind.Trackpad));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerEnterEvent(kind: PointerDeviceKind.Trackpad));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PointerExitEvent(kind: PointerDeviceKind.Trackpad));
    }

    private static PointerEvent CreateTransformable(string name)
    {
        return name switch
        {
            "added" => new PointerAddedEvent(
                timestampUtc: TwoSeconds,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                obscured: true,
                pressureMin: 10,
                pressureMax: 60,
                distance: 12,
                distanceMax: 24,
                radiusMin: 10,
                radiusMax: 50,
                orientation: 2,
                tilt: 4),
            "cancel" => new PointerCancelEvent(
                timestampUtc: TwoSeconds,
                pointer: 45,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                buttons: (PointerButtons)4,
                obscured: true,
                pressureMin: 10,
                pressureMax: 60,
                distance: 12,
                distanceMax: 24,
                size: 10,
                radiusMajor: 33,
                radiusMinor: 44,
                radiusMin: 10,
                radiusMax: 50,
                orientation: 2,
                tilt: 4),
            "down" => new PointerDownEvent(
                timestampUtc: TwoSeconds,
                pointer: 45,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                buttons: (PointerButtons)4,
                obscured: true,
                pressure: 34,
                pressureMin: 10,
                pressureMax: 60,
                distanceMax: 24,
                size: 10,
                radiusMajor: 33,
                radiusMinor: 44,
                radiusMin: 10,
                radiusMax: 50,
                orientation: 2,
                tilt: 4),
            "enter" => new PointerEnterEvent(
                timestampUtc: TwoSeconds,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                delta: new Point(5, 5),
                buttons: (PointerButtons)4,
                obscured: true,
                pressureMin: 10,
                pressureMax: 60,
                distance: 12,
                distanceMax: 24,
                size: 10,
                radiusMajor: 33,
                radiusMinor: 44,
                radiusMin: 10,
                radiusMax: 50,
                orientation: 2,
                tilt: 4,
                synthesized: true),
            "exit" => new PointerExitEvent(
                timestampUtc: TwoSeconds,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                delta: new Point(5, 5),
                buttons: (PointerButtons)4,
                obscured: true,
                pressureMin: 10,
                pressureMax: 60,
                distance: 12,
                distanceMax: 24,
                size: 10,
                radiusMajor: 33,
                radiusMinor: 44,
                radiusMin: 10,
                radiusMax: 50,
                orientation: 2,
                tilt: 4,
                synthesized: true),
            "hover" => new PointerHoverEvent(
                timestampUtc: TwoSeconds,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                delta: new Point(5, 5),
                buttons: (PointerButtons)4,
                obscured: true,
                pressureMin: 10,
                pressureMax: 60,
                distance: 12,
                distanceMax: 24,
                size: 10,
                radiusMajor: 33,
                radiusMinor: 44,
                radiusMin: 10,
                radiusMax: 50,
                orientation: 2,
                tilt: 4,
                synthesized: true),
            "move" => new PointerMoveEvent(
                timestampUtc: TwoSeconds,
                pointer: 45,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                delta: new Point(5, 5),
                buttons: (PointerButtons)4,
                obscured: true,
                pressure: 34,
                pressureMin: 10,
                pressureMax: 60,
                distanceMax: 24,
                size: 10,
                radiusMajor: 33,
                radiusMinor: 44,
                radiusMin: 10,
                radiusMax: 50,
                orientation: 2,
                tilt: 4,
                platformData: 10,
                synthesized: true),
            "removed" => new PointerRemovedEvent(
                timestampUtc: TwoSeconds,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                obscured: true,
                pressureMin: 10,
                pressureMax: 60,
                distanceMax: 24,
                radiusMin: 10,
                radiusMax: 50),
            "scroll" => new PointerScrollEvent(
                timestampUtc: TwoSeconds,
                device: 1,
                position: Position),
            "panZoomStart" => new PointerPanZoomStartEvent(
                timestampUtc: TwoSeconds,
                device: 1,
                position: Position),
            "panZoomUpdate" => new PointerPanZoomUpdateEvent(
                timestampUtc: TwoSeconds,
                device: 1,
                position: Position),
            "panZoomEnd" => new PointerPanZoomEndEvent(
                timestampUtc: TwoSeconds,
                device: 1,
                position: Position),
            "up" => new PointerUpEvent(
                timestampUtc: TwoSeconds,
                pointer: 45,
                kind: PointerDeviceKind.Mouse,
                device: 1,
                position: Position,
                buttons: (PointerButtons)4,
                obscured: true,
                pressure: 34,
                pressureMin: 10,
                pressureMax: 60,
                distance: 12,
                distanceMax: 24,
                size: 10,
                radiusMajor: 33,
                radiusMinor: 44,
                radiusMin: 10,
                radiusMax: 50,
                orientation: 2,
                tilt: 4),
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }

    private static void AssertOneLine(string description)
    {
        Assert.NotEmpty(description);
        Assert.DoesNotContain('\n', description);
        Assert.DoesNotContain("Instance of ", description, StringComparison.Ordinal);
        Assert.Equal(description.Trim(), description);
    }

    private static void AssertMouseEventCopied(PointerEvent source, PointerEvent result, PointerEvent empty)
    {
        Assert.Equal(source.TimestampUtc, result.TimestampUtc);
        Assert.Equal(source.Kind, result.Kind);
        Assert.Equal(source.Device, result.Device);
        Assert.Equal(source.Position, result.Position);
        Assert.Equal(source.Buttons, result.Buttons);
        Assert.Equal(source.Obscured, result.Obscured);
        Assert.Equal(source.PressureMin, result.PressureMin);
        Assert.Equal(source.PressureMax, result.PressureMax);
        Assert.Equal(source.DistanceMax, result.DistanceMax);
        Assert.Equal(source.Size, result.Size);
        Assert.Equal(source.RadiusMajor, result.RadiusMajor);
        Assert.Equal(source.RadiusMinor, result.RadiusMinor);
        Assert.Equal(source.RadiusMin, result.RadiusMin);
        Assert.Equal(source.RadiusMax, result.RadiusMax);
        Assert.Equal(source.Orientation, result.Orientation);
        Assert.Equal(source.Tilt, result.Tilt);
        Assert.Equal(source.Synthesized, result.Synthesized);
        Assert.Equal(empty.Pointer, result.Pointer);
        Assert.Equal(empty.Pressure, result.Pressure);
    }

    private static void AssertCopied(PointerEvent source, PointerEvent copy)
    {
        Assert.Equal(OneDay.AddDays(1), copy.TimestampUtc);
        Assert.Equal(default, copy.Position);
        Assert.NotEqual(source.Position, copy.Position);
        Assert.Null(copy.Transform);
        Assert.Null(copy.Original);
    }

    private static void AssertKept(PointerEvent expected, PointerEvent actual, params string[] fields)
    {
        foreach (string field in fields)
        {
            Assert.True(Equals(Field(expected, field), Field(actual, field)), $"Field '{field}' differs.");
        }
    }

    private static object Field(PointerEvent @event, string name)
    {
        return name switch
        {
            "pointer" => @event.Pointer,
            "kind" => @event.Kind,
            "device" => @event.Device,
            "delta" => @event.Delta,
            "buttons" => @event.Buttons,
            "down" => @event.Down,
            "obscured" => @event.Obscured,
            "pressure" => @event.Pressure,
            "pressureMin" => @event.PressureMin,
            "pressureMax" => @event.PressureMax,
            "distance" => @event.Distance,
            "distanceMax" => @event.DistanceMax,
            "size" => @event.Size,
            "radiusMajor" => @event.RadiusMajor,
            "radiusMinor" => @event.RadiusMinor,
            "radiusMin" => @event.RadiusMin,
            "radiusMax" => @event.RadiusMax,
            "orientation" => @event.Orientation,
            "tilt" => @event.Tilt,
            "platformData" => @event.PlatformData,
            "synthesized" => @event.Synthesized,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }
}
