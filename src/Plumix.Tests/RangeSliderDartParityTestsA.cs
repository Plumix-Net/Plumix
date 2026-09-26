// Dart parity source: material_ui/lib/src/range_slider.dart
// Mirrors material-ui-src/test/range_slider_test.dart (lines 1-1984)

using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RangeSliderDartParityTestsA : IDisposable
{
    public RangeSliderDartParityTestsA()
    {
        // flutter_test's `defaultTargetPlatform` is android.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    // Regression test for https://github.com/flutter/flutter/issues/105833
    // Flutter: "Drag gesture uses provided gesture settings"
    [Fact]
    public void DragGestureUsesProvidedGestureSettings()
    {
        using var tester = new FrameworkDartTester();
        var values = new RangeValues(0.1, 0.5);
        bool dragStarted = false;
        Key sliderKey = new UniqueKey();

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) => new Material.Material(
                    child: new Center(
                        child: new GestureDetector(
                            behavior: HitTestBehavior.DeferToChild,
                            onHorizontalDragStart: _ => dragStarted = true,
                            child: new MediaQuery(
                                MediaQuery.Of(context).CopyWith(
                                    gestureSettings: new DeviceGestureSettings(TouchSlop: 20)),
                                new RangeSlider(
                                    key: sliderKey,
                                    values: values,
                                    onChanged: newValues => setState(() => values = newValues))))))))));

        TestGesture drag = tester.StartGesture(
            tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
            PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.PressTimeout);

        // Less than configured touch slop, more than default touch slop
        drag.MoveBy(new Vector(19.0, 0));
        tester.Pump();

        Assert.Equal(new RangeValues(0.1, 0.5), values);
        Assert.True(dragStarted);

        dragStarted = false;

        drag.Up();
        tester.PumpAndSettle();

        drag = tester.StartGesture(
            tester.GetCenter(tester.ElementsWithKey(sliderKey).Single()),
            PointerDeviceKind.Touch);
        tester.Pump(GestureConstants.PressTimeout);

        bool sliderEnd = false;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) => new Material.Material(
                    child: new Center(
                        child: new GestureDetector(
                            behavior: HitTestBehavior.DeferToChild,
                            onHorizontalDragStart: _ => dragStarted = true,
                            child: new MediaQuery(
                                MediaQuery.Of(context).CopyWith(
                                    gestureSettings: new DeviceGestureSettings(TouchSlop: 10)),
                                new RangeSlider(
                                    key: sliderKey,
                                    values: values,
                                    onChanged: newValues => setState(() => values = newValues),
                                    onChangeEnd: _ => sliderEnd = true)))))))));

        // More than touch slop.
        drag.MoveBy(new Vector(12.0, 0));

        drag.Up();
        tester.PumpAndSettle();

        Assert.True(sliderEnd);
        Assert.False(dragStarted);
    }

    // Flutter: "Range Slider can move when tapped (continuous LTR)"
    [Fact]
    public void CanMoveWhenTappedContinuousLtr()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(0.3, 0.8));
        tester.PumpWidget(SimpleApp(TextDirection.Ltr, holder));

        // The closest thumb is selected when tapping between the thumbs outside the touch
        // boundaries
        Assert.Equal(new RangeValues(0.3, 0.8), holder.Values);
        //  taps at 0.5
        tester.Tap(Slider(tester));
        tester.Pump();
        Assert.Equal(new RangeValues(0.5, 0.8), holder.Values);

        // Get the bounds of the track by finding the slider edges and translating
        // inwards by the overlay radius.
        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // The start thumb is selected when tapping the left inactive track.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.1);
        TapAt(tester, leftTarget);
        Near(0.1, holder.Values.Start, 0.01);
        Assert.Equal(0.8, holder.Values.End);

        // The end thumb is selected when tapping the right inactive track.
        tester.Pump();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.9);
        TapAt(tester, rightTarget);
        Near(0.1, holder.Values.Start, 0.01);
        Near(0.9, holder.Values.End, 0.01);
    }

    // Flutter: "Range Slider can move when tapped (continuous RTL)"
    [Fact]
    public void CanMoveWhenTappedContinuousRtl()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(0.3, 1.0));
        tester.PumpWidget(SimpleApp(TextDirection.Rtl, holder));

        // The closest thumb is selected when tapping between the thumbs outside the touch
        // boundaries
        Assert.Equal(new RangeValues(0.3, 1.0), holder.Values);
        // taps at 0.5
        tester.Tap(Slider(tester));
        tester.Pump();
        Assert.Equal(new RangeValues(0.5, 1.0), holder.Values);

        // Get the bounds of the track by finding the slider edges and translating
        // inwards by the overlay radius.
        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // The end thumb is selected when tapping the left inactive track.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.1);
        TapAt(tester, leftTarget);
        Assert.Equal(0.5, holder.Values.Start);
        Near(0.9, holder.Values.End, 0.01);

        // The start thumb is selected when tapping the right inactive track.
        tester.Pump();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.9);
        TapAt(tester, rightTarget);
        Near(0.1, holder.Values.Start, 0.01);
        Near(0.9, holder.Values.End, 0.01);
    }

    // Flutter: "Range Slider can move when tapped (discrete LTR)"
    [Fact]
    public void CanMoveWhenTappedDiscreteLtr()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(30, 80));
        tester.PumpWidget(SimpleApp(TextDirection.Ltr, holder, max: 100.0, divisions: 10));

        // The closest thumb is selected when tapping between the thumbs outside the touch
        // boundaries
        Assert.Equal(new RangeValues(30, 80), holder.Values);
        // taps at 0.5
        tester.Tap(Slider(tester));
        tester.PumpAndSettle();
        Assert.Equal(new RangeValues(50, 80), holder.Values);

        // Get the bounds of the track by finding the slider edges and translating
        // inwards by the overlay radius.
        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // The start thumb is selected when tapping the left inactive track.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.1);
        TapAt(tester, leftTarget);
        tester.PumpAndSettle();
        Assert.Equal(10, DartRound(holder.Values.Start));
        Assert.Equal(80, DartRound(holder.Values.End));

        // The end thumb is selected when tapping the right inactive track.
        tester.Pump();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.9);
        TapAt(tester, rightTarget);
        tester.PumpAndSettle();
        Assert.Equal(10, DartRound(holder.Values.Start));
        Assert.Equal(90, DartRound(holder.Values.End));
    }

    // Flutter: "Range Slider can move when tapped (discrete RTL)"
    [Fact]
    public void CanMoveWhenTappedDiscreteRtl()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(30, 80));
        tester.PumpWidget(SimpleApp(TextDirection.Rtl, holder, max: 100, divisions: 10));

        // The closest thumb is selected when tapping between the thumbs outside the touch
        // boundaries
        Assert.Equal(new RangeValues(30, 80), holder.Values);
        // taps at 0.5
        tester.Tap(Slider(tester));
        tester.PumpAndSettle();
        Assert.Equal(new RangeValues(50, 80), holder.Values);

        // Get the bounds of the track by finding the slider edges and translating
        // inwards by the overlay radius.
        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // The start thumb is selected when tapping the left inactive track.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.1);
        TapAt(tester, leftTarget);
        tester.PumpAndSettle();
        Assert.Equal(50, DartRound(holder.Values.Start));
        Assert.Equal(90, DartRound(holder.Values.End));

        // The end thumb is selected when tapping the right inactive track.
        tester.Pump();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.9);
        TapAt(tester, rightTarget);
        tester.PumpAndSettle();
        Assert.Equal(10, DartRound(holder.Values.Start));
        Assert.Equal(90, DartRound(holder.Values.End));
    }

    // Flutter: "Range Slider thumbs can be dragged to the min and max (continuous LTR)"
    [Fact]
    public void ThumbsCanBeDraggedToMinAndMaxContinuousLtr()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(0.3, 0.7));
        tester.PumpWidget(SimpleApp(TextDirection.Ltr, holder));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // Drag the start thumb to the min.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, topLeft + ((bottomRight - topLeft) * -0.4));
        Assert.Equal(0, holder.Values.Start);

        // Drag the end thumb to the max.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, topLeft + ((bottomRight - topLeft) * 0.4));
        Assert.Equal(1, holder.Values.End);
    }

    // Flutter: "Range Slider thumbs can be dragged to the min and max (continuous RTL)"
    [Fact]
    public void ThumbsCanBeDraggedToMinAndMaxContinuousRtl()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(0.3, 0.7));
        tester.PumpWidget(SimpleApp(TextDirection.Rtl, holder));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // Drag the end thumb to the max.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, topLeft + ((bottomRight - topLeft) * -0.4));
        Assert.Equal(1, holder.Values.End);

        // Drag the start thumb to the min.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, topLeft + ((bottomRight - topLeft) * 0.4));
        Assert.Equal(0, holder.Values.Start);
    }

    // Flutter: "Range Slider thumbs can be dragged to the min and max (discrete LTR)"
    [Fact]
    public void ThumbsCanBeDraggedToMinAndMaxDiscreteLtr()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(30, 70));
        tester.PumpWidget(SimpleApp(TextDirection.Ltr, holder, max: 100, divisions: 10));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // Drag the start thumb to the min.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, topLeft + ((bottomRight - topLeft) * -0.4));
        Assert.Equal(0, holder.Values.Start);

        // Drag the end thumb to the max.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, topLeft + ((bottomRight - topLeft) * 0.4));
        Assert.Equal(100, holder.Values.End);
    }

    // Flutter: "Range Slider thumbs can be dragged to the min and max (discrete RTL)"
    [Fact]
    public void ThumbsCanBeDraggedToMinAndMaxDiscreteRtl()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(30, 70));
        tester.PumpWidget(SimpleApp(TextDirection.Rtl, holder, max: 100, divisions: 10));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // Drag the end thumb to the max.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, topLeft + ((bottomRight - topLeft) * -0.4));
        Assert.Equal(100, holder.Values.End);

        // Drag the start thumb to the min.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, topLeft + ((bottomRight - topLeft) * 0.4));
        Assert.Equal(0, holder.Values.Start);
    }

    // Flutter: "minThumbSeparation has same width as surrounding box, values still bounded (ltr)"
    [Fact]
    public void MinThumbSeparationSameWidthAsBoxValuesBoundedLtr()
    {
        MinThumbSeparationSameWidthAsBox(TextDirection.Ltr);
    }

    // Flutter: "minThumbSeparation has same width as surrounding box, values still bounded (rtl)"
    [Fact]
    public void MinThumbSeparationSameWidthAsBoxValuesBoundedRtl()
    {
        MinThumbSeparationSameWidthAsBox(TextDirection.Rtl);
    }

    // Flutter: "Range Slider thumbs can be dragged together and the start thumb can be dragged apart
    // (continuous LTR)"
    [Fact]
    public void DraggedTogetherStartDraggedApartContinuousLtr()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(0.3, 0.7));
        tester.PumpWidget(SimpleApp(TextDirection.Ltr, holder));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the start thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        Near(0.5, holder.Values.Start, 0.05);

        // Drag the end thumb towards the center.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(0.5, holder.Values.End, 0.05);

        // Drag the start thumb apart.
        tester.PumpAndSettle();
        tester.DragFrom(middle, -(bottomRight - topLeft) * 0.3);
        Near(0.2, holder.Values.Start, 0.05);
    }

    // Flutter: "Range Slider thumbs can be dragged together and the start thumb can be dragged apart
    // (continuous RTL)"
    [Fact]
    public void DraggedTogetherStartDraggedApartContinuousRtl()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(0.3, 0.7));
        tester.PumpWidget(SimpleApp(TextDirection.Rtl, holder));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the end thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        Near(0.5, holder.Values.End, 0.05);

        // Drag the start thumb towards the center.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(0.5, holder.Values.Start, 0.05);

        // Drag the start thumb apart.
        tester.PumpAndSettle();
        tester.DragFrom(middle, (bottomRight - topLeft) * 0.3);
        Near(0.2, holder.Values.Start, 0.05);
    }

    // Flutter: "Range Slider thumbs can be dragged together and the start thumb can be dragged apart
    // (discrete LTR)"
    [Fact]
    public void DraggedTogetherStartDraggedApartDiscreteLtr()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(30, 70));
        tester.PumpWidget(SimpleApp(TextDirection.Ltr, holder, max: 100, divisions: 10));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the start thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        Near(50, holder.Values.Start, 0.01);

        // Drag the end thumb towards the center.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(50, holder.Values.End, 0.01);

        // Drag the start thumb apart.
        tester.PumpAndSettle();
        tester.DragFrom(middle, -(bottomRight - topLeft) * 0.3);
        Near(20, holder.Values.Start, 0.01);
    }

    // Flutter: "Range Slider thumbs can be dragged together and the start thumb can be dragged apart
    // (discrete RTL)"
    [Fact]
    public void DraggedTogetherStartDraggedApartDiscreteRtl()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(30, 70));
        tester.PumpWidget(SimpleApp(TextDirection.Rtl, holder, max: 100, divisions: 10));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the end thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        Near(50, holder.Values.End, 0.01);

        // Drag the start thumb towards the center.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(50, holder.Values.Start, 0.01);
        Near(50, holder.Values.End, 0.01);

        // Drag the start thumb apart.
        tester.PumpAndSettle();
        tester.DragFrom(middle, (bottomRight - topLeft) * 0.3);
        Near(20, holder.Values.Start, 0.01);
    }

    // Flutter: "Range Slider thumbs can be dragged together and the end thumb can be dragged apart
    // (continuous LTR)"
    [Fact]
    public void DraggedTogetherEndDraggedApartContinuousLtr()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(0.3, 0.7));
        tester.PumpWidget(SimpleApp(TextDirection.Ltr, holder));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the start thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        Near(0.5, holder.Values.Start, 0.05);

        // Drag the end thumb towards the center.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(0.5, holder.Values.End, 0.05);

        // Drag the end thumb apart.
        tester.PumpAndSettle();
        tester.DragFrom(middle, (bottomRight - topLeft) * 0.3);
        Near(0.8, holder.Values.End, 0.05);
    }

    // Flutter: "Range Slider thumbs can be dragged together and the end thumb can be dragged apart
    // (continuous RTL)"
    [Fact]
    public void DraggedTogetherEndDraggedApartContinuousRtl()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(0.3, 0.7));
        tester.PumpWidget(SimpleApp(TextDirection.Rtl, holder));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the end thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        Near(0.5, holder.Values.End, 0.05);

        // Drag the start thumb towards the center.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(0.5, holder.Values.Start, 0.05);

        // Drag the end thumb apart.
        tester.PumpAndSettle();
        tester.DragFrom(middle, -(bottomRight - topLeft) * 0.3);
        Near(0.8, holder.Values.End, 0.05);
    }

    // Flutter: "Range Slider thumbs can be dragged together and the end thumb can be dragged apart
    // (discrete LTR)"
    [Fact]
    public void DraggedTogetherEndDraggedApartDiscreteLtr()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(30, 70));
        tester.PumpWidget(SimpleApp(TextDirection.Ltr, holder, max: 100, divisions: 10));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the start thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        Near(50, holder.Values.Start, 0.01);

        // Drag the end thumb towards the center.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(50, holder.Values.End, 0.01);

        // Drag the end thumb apart.
        tester.PumpAndSettle();
        tester.DragFrom(middle, (bottomRight - topLeft) * 0.3);
        Near(80, holder.Values.End, 0.01);
    }

    // Flutter: "Range Slider thumbs can be dragged together and the end thumb can be dragged apart
    // (discrete RTL)"
    [Fact]
    public void DraggedTogetherEndDraggedApartDiscreteRtl()
    {
        using var tester = new FrameworkDartTester();
        var holder = new ValuesHolder(new RangeValues(30, 70));
        tester.PumpWidget(SimpleApp(TextDirection.Rtl, holder, max: 100, divisions: 10));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the end thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        Near(50, holder.Values.End, 0.01);

        // Drag the start thumb towards the center.
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(50, holder.Values.Start, 0.01);

        // Drag the end thumb apart.
        tester.PumpAndSettle();
        tester.DragFrom(middle, -(bottomRight - topLeft) * 0.3);
        Near(80, holder.Values.End, 0.01);
    }

    // Flutter: "Range Slider onChangeEnd and onChangeStart are called on an interaction initiated by tap"
    [Fact]
    public void OnChangeEndAndOnChangeStartCalledOnTap()
    {
        using var tester = new FrameworkDartTester();
        var values = new RangeValues(30, 70);
        RangeValues? startValues = null;
        RangeValues? endValues = null;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) => new Material.Material(
                    child: new Center(
                        child: new RangeSlider(
                            values: values,
                            max: 100,
                            onChanged: newValues => setState(() => values = newValues),
                            onChangeStart: newValues => startValues = newValues,
                            onChangeEnd: newValues => endValues = newValues)))))));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // Drag the start thumb towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        Assert.Null(startValues);
        Assert.Null(endValues);
        tester.DragFrom(leftTarget, (bottomRight - topLeft) * 0.2);
        Near(30, startValues!.Start, 1);
        Near(70, startValues!.End, 1);
        Near(50, values.Start, 1);
        Near(70, values.End, 1);
        Near(50, endValues!.Start, 1);
        Near(70, endValues!.End, 1);
    }

    // Flutter: "Range Slider onChangeEnd and onChangeStart are called on an interaction initiated by drag"
    [Fact]
    public void OnChangeEndAndOnChangeStartCalledOnDrag()
    {
        using var tester = new FrameworkDartTester();
        var values = new RangeValues(30, 70);
        RangeValues? startValues = null;
        RangeValues? endValues = null;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) => new Material.Material(
                    child: new Center(
                        child: new RangeSlider(
                            values: values,
                            max: 100,
                            onChanged: newValues => setState(() => values = newValues),
                            onChangeStart: newValues => startValues = newValues,
                            onChangeEnd: newValues => endValues = newValues)))))));

        // Get the bounds of the track by finding the slider edges and translating
        // inwards by the overlay radius
        (Point topLeft, Point bottomRight) = TrackBounds(tester);

        // Drag the thumbs together.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, (bottomRight - topLeft) * 0.2);
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, (bottomRight - topLeft) * -0.2);
        tester.PumpAndSettle();
        Near(50, values.Start, 1);
        Near(51, values.End, 1);

        // Drag the end thumb to the right.
        Point middleTarget = topLeft + ((bottomRight - topLeft) * 0.5);
        tester.DragFrom(middleTarget, (bottomRight - topLeft) * 0.4);
        tester.PumpAndSettle();
        Near(50, startValues!.Start, 1);
        Near(51, startValues!.End, 1);
        Near(50, endValues!.Start, 1);
        Near(90, endValues!.End, 1);
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes for a default enabled slider"
    [Fact]
    public void ThemeColorsDefaultEnabledSlider()
    {
        using var tester = new FrameworkDartTester();
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(BuildThemedApp(theme: theme));

        RenderObject sliderBox = SliderBox(tester);

        // Check default theme for enabled widget.
        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .RRect(color: sliderTheme.InactiveTrackColor)
                .RRect(color: sliderTheme.InactiveTrackColor)
                .RRect(color: sliderTheme.ActiveTrackColor));
        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .Circle(color: sliderTheme.ThumbColor)
                .Circle(color: sliderTheme.ThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledInactiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.ActiveTickMarkColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.InactiveTickMarkColor));
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes when setting the active color"
    [Fact]
    public void ThemeColorsWhenSettingActiveColor()
    {
        using var tester = new FrameworkDartTester();
        var activeColor = new Color(0xcafefeed);
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(BuildThemedApp(theme: theme, activeColor: activeColor));

        RenderObject sliderBox = SliderBox(tester);

        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .RRect(color: sliderTheme.InactiveTrackColor)
                .RRect(color: sliderTheme.InactiveTrackColor)
                .RRect(color: activeColor));
        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .Circle(color: activeColor)
                .Circle(color: activeColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledInactiveTrackColor));
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes when setting the inactive color"
    [Fact]
    public void ThemeColorsWhenSettingInactiveColor()
    {
        using var tester = new FrameworkDartTester();
        var inactiveColor = new Color(0xdeadbeef);
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(BuildThemedApp(theme: theme, inactiveColor: inactiveColor));

        RenderObject sliderBox = SliderBox(tester);

        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .RRect(color: inactiveColor)
                .RRect(color: inactiveColor)
                .RRect(color: sliderTheme.ActiveTrackColor));
        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .Circle(color: sliderTheme.ThumbColor)
                .Circle(color: sliderTheme.ThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledInactiveTrackColor));
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes with active and inactive colors"
    [Fact]
    public void ThemeColorsWithActiveAndInactiveColors()
    {
        using var tester = new FrameworkDartTester();
        var activeColor = new Color(0xcafefeed);
        var inactiveColor = new Color(0xdeadbeef);
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(BuildThemedApp(theme: theme, activeColor: activeColor, inactiveColor: inactiveColor));

        RenderObject sliderBox = SliderBox(tester);

        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .RRect(color: inactiveColor)
                .RRect(color: inactiveColor)
                .RRect(color: activeColor));
        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .Circle(color: activeColor)
                .Circle(color: activeColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledInactiveTrackColor));
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes for a discrete slider"
    [Fact]
    public void ThemeColorsDiscreteSlider()
    {
        using var tester = new FrameworkDartTester();
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(BuildThemedApp(theme: theme, divisions: 3));

        RenderObject sliderBox = SliderBox(tester);

        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .RRect(color: sliderTheme.InactiveTrackColor)
                .RRect(color: sliderTheme.InactiveTrackColor)
                .RRect(color: sliderTheme.ActiveTrackColor));
        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .Circle(color: sliderTheme.InactiveTickMarkColor)
                .Circle(color: sliderTheme.InactiveTickMarkColor)
                .Circle(color: sliderTheme.ActiveTickMarkColor)
                .Circle(color: sliderTheme.InactiveTickMarkColor)
                .Circle(color: sliderTheme.ThumbColor)
                .Circle(color: sliderTheme.ThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledInactiveTrackColor));
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes for a discrete slider with active
    // and inactive colors"
    [Fact]
    public void ThemeColorsDiscreteSliderWithActiveAndInactiveColors()
    {
        using var tester = new FrameworkDartTester();
        var activeColor = new Color(0xcafefeed);
        var inactiveColor = new Color(0xdeadbeef);
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(BuildThemedApp(
            theme: theme,
            activeColor: activeColor,
            inactiveColor: inactiveColor,
            divisions: 3));

        RenderObject sliderBox = SliderBox(tester);

        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .RRect(color: inactiveColor)
                .RRect(color: inactiveColor)
                .RRect(color: activeColor));
        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .Circle(color: activeColor)
                .Circle(color: activeColor)
                .Circle(color: inactiveColor)
                .Circle(color: activeColor)
                .Circle(color: activeColor)
                .Circle(color: activeColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.DisabledThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.DisabledInactiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.ActiveTickMarkColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.InactiveTickMarkColor));
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes for a default disabled slider"
    [Fact]
    public void ThemeColorsDefaultDisabledSlider()
    {
        using var tester = new FrameworkDartTester();
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(BuildThemedApp(theme: theme, enabled: false));

        RenderObject sliderBox = SliderBox(tester);

        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .RRect(color: sliderTheme.DisabledInactiveTrackColor)
                .RRect(color: sliderTheme.DisabledInactiveTrackColor)
                .RRect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.ActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.InactiveTrackColor));
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes for a disabled slider with active
    // and inactive colors"
    [Fact]
    public void ThemeColorsDisabledSliderWithActiveAndInactiveColors()
    {
        using var tester = new FrameworkDartTester();
        var activeColor = new Color(0xcafefeed);
        var inactiveColor = new Color(0xdeadbeef);
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(BuildThemedApp(
            theme: theme,
            activeColor: activeColor,
            inactiveColor: inactiveColor,
            enabled: false));

        RenderObject sliderBox = SliderBox(tester);

        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .RRect(color: sliderTheme.DisabledInactiveTrackColor)
                .RRect(color: sliderTheme.DisabledInactiveTrackColor)
                .RRect(color: sliderTheme.DisabledActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Circle(color: sliderTheme.ThumbColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.ActiveTrackColor));
        PaintAssert.DoesNotPaint(sliderBox, PaintPattern.Paints.Rect(color: sliderTheme.InactiveTrackColor));
    }

    // Flutter: "Range Slider uses the right theme colors for the right shapes when the value indicators are
    // showing"
    [Fact]
    public void ThemeColorsWhenValueIndicatorsAreShowing()
    {
        using var shadows = DisableShadows();
        using var tester = new FrameworkDartTester();
        ThemeData theme = BuildTheme();
        SliderThemeData sliderTheme = theme.SliderTheme;
        var values = new RangeValues(0.5, 0.75);

        Widget BuildApp(int? divisions = null, bool enabled = true)
        {
            Action<RangeValues>? onChanged = !enabled ? null : newValues => values = newValues;
            return new MaterialApp(
                home: new Directionality(
                    TextDirection.Ltr,
                    new Material.Material(
                        child: new Center(
                            child: new Theme(
                                theme,
                                new RangeSlider(
                                    values: values,
                                    labels: new RangeLabels(Fixed2(values.Start), Fixed2(values.End)),
                                    divisions: divisions,
                                    onChanged: onChanged))))));
        }

        tester.PumpWidget(BuildApp(divisions: 3));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        Point topRight = TopRight(tester, Slider(tester)) + new Vector(-24, 0);
        TestGesture gesture = tester.StartGesture(topRight, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();
        Assert.Equal(1, values.End);
        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                .Path(color: MaterialColors.Black) // shadow
                .Path(color: MaterialColors.Black) // shadow
                .Path(color: sliderTheme.ValueIndicatorColor)
                .Paragraph());
        gesture.Up();
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();
    }

    // Flutter: "Range Slider removes value indicator from overlay if Slider gets disposed without value
    // indicator animation completing."
    [Fact]
    public void RemovesValueIndicatorFromOverlayWhenDisposedMidAnimation()
    {
        using var shadows = DisableShadows();
        using var tester = new FrameworkDartTester();
        var values = new RangeValues(0.5, 0.75);
        var fillColor = new Color(0xf55f5f5f);

        Widget BuildApp(int? divisions = null)
        {
            void OnChanged(RangeValues newValues)
            {
                values = newValues;
            }

            return new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new Scaffold(
                    // The builder is used to pass the context from the MaterialApp widget
                    // to the [Navigator]. This context is required in order for the
                    // Navigator to work.
                    body: new Builder(context => new Column(
                        children:
                        [
                            new RangeSlider(
                                values: values,
                                labels: new RangeLabels(Fixed2(values.Start), Fixed2(values.End)),
                                divisions: divisions,
                                onChanged: OnChanged),
                            new ElevatedButton(
                                child: new Text("Next"),
                                onPressed: () => Navigator.Of(context).PushReplacement(
                                    new MaterialPageRoute(innerContext => new ElevatedButton(
                                        child: new Text("Inner page"),
                                        onPressed: () => Navigator.Of(innerContext).Pop())))),
                        ]))));
        }

        tester.PumpWidget(BuildApp(divisions: 5));

        RenderObject valueIndicatorBox = OverlayBox(tester);
        Point topRight = TopRight(tester, Slider(tester)) + new Vector(-24, 0);
        TestGesture gesture = tester.StartGesture(topRight, PointerDeviceKind.Touch);
        // Wait for value indicator animation to finish.
        tester.PumpAndSettle();

        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                // Represents the raised button wth next text.
                .Path(color: MaterialColors.Black)
                .Paragraph()
                // Represents the range slider.
                .Path(color: fillColor)
                .Paragraph()
                .Path(color: fillColor)
                .Paragraph());

        // Represents the Raised Button and Range Slider.
        Assert.Equal(6, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));
        Assert.Equal(3, PaintRecording.Record(valueIndicatorBox).CountCalls("drawParagraph"));

        tester.Tap(tester.ElementsWithText("Next").Single());
        tester.PumpAndSettle();

        Assert.Empty(tester.ElementsOfType<RangeSlider>());
        PaintAssert.DoesNotPaint(
            valueIndicatorBox,
            PaintPattern.Paints
                .Path(color: fillColor)
                .Paragraph()
                .Path(color: fillColor)
                .Paragraph());

        // Represents the raised button with inner page text.
        Assert.Equal(2, PaintRecording.Record(valueIndicatorBox).CountCalls("drawPath"));
        Assert.Equal(1, PaintRecording.Record(valueIndicatorBox).CountCalls("drawParagraph"));

        // Don't stop holding the value indicator.
        gesture.Up();
        tester.PumpAndSettle();
    }

    // Flutter: "Range Slider top thumb gets stroked when overlapping"
    [Fact]
    public void TopThumbGetsStrokedWhenOverlapping()
    {
        using var tester = new FrameworkDartTester();
        var values = new RangeValues(0.3, 0.7);

        var theme = new ThemeData(
            platform: TargetPlatform.Android,
            primarySwatch: MaterialColors.Blue,
            sliderTheme: new SliderThemeData(
                ThumbColor: new Color(0xff000001),
                OverlappingShapeStrokeColor: new Color(0xff000002)));
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) => new Material.Material(
                    child: new Center(
                        child: new Theme(
                            theme,
                            new RangeSlider(
                                values: values,
                                onChanged: newValues => setState(() => values = newValues)))))))));

        RenderObject sliderBox = SliderBox(tester);

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the thumbs towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        Near(0.5, values.Start, 0.03);
        Near(0.5, values.End, 0.03);
        tester.PumpAndSettle();

        PaintAssert.Paints(
            sliderBox,
            PaintPattern.Paints
                .Circle(color: sliderTheme.OverlayColor)
                .Circle(color: sliderTheme.ThumbColor)
                .Circle(color: sliderTheme.OverlappingShapeStrokeColor)
                .Circle(color: sliderTheme.ThumbColor));
    }

    // Flutter: "Range Slider top value indicator gets stroked when overlapping"
    [Fact]
    public void TopValueIndicatorGetsStrokedWhenOverlapping()
    {
        TopValueIndicatorGetsStroked(textScale: null);
    }

    // Flutter: "Range Slider top value indicator gets stroked when overlapping with large text scale"
    [Fact]
    public void TopValueIndicatorGetsStrokedWhenOverlappingWithLargeTextScale()
    {
        TopValueIndicatorGetsStroked(textScale: 2);
    }

    // Flutter: "Range Slider thumb gets stroked when overlapping"
    [Fact]
    public void ThumbGetsStrokedWhenOverlapping()
    {
        using var tester = new FrameworkDartTester();
        var values = new RangeValues(0.3, 0.7);

        var theme = new ThemeData(
            platform: TargetPlatform.Android,
            primarySwatch: MaterialColors.Blue,
            sliderTheme: new SliderThemeData(
                ValueIndicatorColor: new Color(0xff000001),
                ShowValueIndicator: ShowValueIndicator.OnlyForContinuous));
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) => new Material.Material(
                    child: new Center(
                        child: new Theme(
                            theme,
                            new RangeSlider(
                                values: values,
                                labels: new RangeLabels(Fixed2(values.Start), Fixed2(values.End)),
                                onChanged: newValues => setState(() => values = newValues)))))))));

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the thumbs towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        tester.PumpAndSettle();
        Near(0.5, values.Start, 0.03);
        Near(0.5, values.End, 0.03);
        TestGesture gesture = tester.StartGesture(middle, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        // The first circle is the thumb, the second one is the overlapping shape
        // circle, and the last one is the second thumb.
        PaintAssert.Paints(
            Slider(tester).FindRenderObject()!,
            PaintPattern.Paints
                .Circle()
                .Circle(color: sliderTheme.OverlappingShapeStrokeColor)
                .Circle());

        gesture.Up();

        PaintAssert.Paints(
            Slider(tester).FindRenderObject()!,
            PaintPattern.Paints
                .Circle()
                .Circle(color: sliderTheme.OverlappingShapeStrokeColor)
                .Circle());
    }

    // Body shared by the two "minThumbSeparation has same width as surrounding box" tests.
    private static void MinThumbSeparationSameWidthAsBox(TextDirection direction)
    {
        using var tester = new FrameworkDartTester();
        const double boundingBoxSize = 200.0;
        var values = new RangeValues(0.0, 1.0);

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                direction,
                new StatefulBuilder((context, setState) => new Material.Material(
                    child: new Center(
                        child: new SizedBox(
                            width: boundingBoxSize,
                            child: new SliderTheme(
                                SliderTheme.Of(context).CopyWith(minThumbSeparation: boundingBoxSize),
                                new RangeSlider(
                                    values: values,
                                    onChanged: newValues => setState(() => values = newValues))))))))));

        tester.Drag(Slider(tester), default);
        tester.PumpAndSettle();

        Assert.InRange(values.Start, 0.0, 1.0);
        Assert.InRange(values.End, 0.0, 1.0);
    }

    // Body shared by the two "Range Slider top value indicator gets stroked when overlapping" tests; the
    // large-text-scale one wraps the Material in `MediaQuery(data: MediaQueryData(textScaler: linear(2)))`.
    private static void TopValueIndicatorGetsStroked(double? textScale)
    {
        using var shadows = DisableShadows();
        using var tester = new FrameworkDartTester();
        var values = new RangeValues(0.3, 0.7);

        var theme = new ThemeData(
            platform: TargetPlatform.Android,
            primarySwatch: MaterialColors.Blue,
            sliderTheme: new SliderThemeData(
                ValueIndicatorColor: new Color(0xff000001),
                OverlappingShapeStrokeColor: new Color(0xff000002),
#pragma warning disable CS0618 // Flutter's test uses the deprecated `ShowValueIndicator.always`.
                ShowValueIndicator: ShowValueIndicator.Always));
#pragma warning restore CS0618
        SliderThemeData sliderTheme = theme.SliderTheme;

        tester.PumpWidget(new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new StatefulBuilder((context, setState) =>
                {
                    Widget material = new Material.Material(
                        child: new Center(
                            child: new Theme(
                                theme,
                                new RangeSlider(
                                    values: values,
                                    labels: new RangeLabels(Fixed2(values.Start), Fixed2(values.End)),
                                    onChanged: newValues => setState(() => values = newValues)))));
                    return textScale is { } scale
                        ? new MediaQuery(new MediaQueryData(TextScaler: TextScaler.Linear(scale)), material)
                        : material;
                }))));

        RenderObject valueIndicatorBox = OverlayBox(tester);

        (Point topLeft, Point bottomRight) = TrackBounds(tester);
        Point middle = topLeft + (bottomRight / 2);

        // Drag the thumbs towards the center.
        Point leftTarget = topLeft + ((bottomRight - topLeft) * 0.3);
        tester.DragFrom(leftTarget, middle - leftTarget);
        tester.PumpAndSettle();
        Point rightTarget = topLeft + ((bottomRight - topLeft) * 0.7);
        tester.DragFrom(rightTarget, middle - rightTarget);
        tester.PumpAndSettle();
        Near(0.5, values.Start, 0.03);
        Near(0.5, values.End, 0.03);
        TestGesture gesture = tester.StartGesture(middle, PointerDeviceKind.Touch);
        tester.PumpAndSettle();

        PaintAssert.Paints(
            valueIndicatorBox,
            PaintPattern.Paints
                .Path(color: MaterialColors.Black) // shadow
                .Path(color: MaterialColors.Black) // shadow
                .Path(color: sliderTheme.ValueIndicatorColor)
                .Paragraph());

        gesture.Up();
    }

    // Dart's local `buildTheme`.
    private static ThemeData BuildTheme()
    {
        return new ThemeData(
            platform: TargetPlatform.Android,
            primarySwatch: MaterialColors.Blue,
            sliderTheme: new SliderThemeData(
                DisabledThumbColor: new Color(0xff000001),
                DisabledActiveTickMarkColor: new Color(0xff000002),
                DisabledActiveTrackColor: new Color(0xff000003),
                DisabledInactiveTickMarkColor: new Color(0xff000004),
                DisabledInactiveTrackColor: new Color(0xff000005),
                ActiveTrackColor: new Color(0xff000006),
                ActiveTickMarkColor: new Color(0xff000007),
                InactiveTrackColor: new Color(0xff000008),
                InactiveTickMarkColor: new Color(0xff000009),
                OverlayColor: new Color(0xff000010),
                ThumbColor: new Color(0xff000011),
                ValueIndicatorColor: new Color(0xff000012)));
    }

    // Dart's local `buildThemedApp`.
    private static Widget BuildThemedApp(
        ThemeData theme,
        Color? activeColor = null,
        Color? inactiveColor = null,
        int? divisions = null,
        bool enabled = true)
    {
        var values = new RangeValues(0.5, 0.75);
        Action<RangeValues>? onChanged = !enabled ? null : newValues => values = newValues;
        return new MaterialApp(
            home: new Directionality(
                TextDirection.Ltr,
                new Material.Material(
                    child: new Center(
                        child: new Theme(
                            theme,
                            new RangeSlider(
                                values: values,
                                labels: new RangeLabels(Fixed2(values.Start), Fixed2(values.End)),
                                divisions: divisions,
                                activeColor: activeColor,
                                inactiveColor: inactiveColor,
                                onChanged: onChanged))))));
    }

    // The `MaterialApp > Directionality > StatefulBuilder > Material > Center > RangeSlider` tree most tests
    // pump, writing the new values back through `setState`.
    private static Widget SimpleApp(
        TextDirection direction,
        ValuesHolder holder,
        double max = 1.0,
        int? divisions = null)
    {
        return new MaterialApp(
            home: new Directionality(
                direction,
                new StatefulBuilder((context, setState) => new Material.Material(
                    child: new Center(
                        child: new RangeSlider(
                            values: holder.Values,
                            max: max,
                            divisions: divisions,
                            onChanged: newValues => setState(() => holder.Values = newValues)))))));
    }

    // flutter_test's `debugDisableShadows == true` for the tests whose expectations include shadow paths.
    private static IDisposable DisableShadows()
    {
        bool previous = RenderingDebug.DisableShadows;
        RenderingDebug.DisableShadows = true;
        return new Restore(() => RenderingDebug.DisableShadows = previous);
    }

    private static Element Slider(FrameworkDartTester tester) => tester.ElementOfType<RangeSlider>();

    // `tester.firstRenderObject<RenderBox>(find.byType(RangeSlider))`.
    private static RenderObject SliderBox(FrameworkDartTester tester) => Slider(tester).FindRenderObject()!;

    // `tester.renderObject(find.byType(Overlay))`.
    private static RenderObject OverlayBox(FrameworkDartTester tester) =>
        tester.ElementOfType<Overlay>().FindRenderObject()!;

    // `tester.getTopRight`.
    private static Point TopRight(FrameworkDartTester tester, Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(new Point(box.Size.Width, 0));
    }

    // The track bounds: the slider edges translated inwards by the overlay radius.
    private static (Point TopLeft, Point BottomRight) TrackBounds(FrameworkDartTester tester)
    {
        Point topLeft = tester.GetTopLeft(Slider(tester)) + new Vector(24, 0);
        Point bottomRight = tester.GetBottomRight(Slider(tester)) + new Vector(-24, 0);
        return (topLeft, bottomRight);
    }

    // `tester.tapAt(location)`.
    private static void TapAt(FrameworkDartTester tester, Point location)
    {
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        gesture.Up();
    }

    // `moreOrLessEquals(expected, epsilon: epsilon)`.
    private static void Near(double expected, double actual, double epsilon) =>
        Assert.True(Math.Abs(actual - expected) <= epsilon, $"expected {expected} +/- {epsilon}, got {actual}");

    // Dart's `double.round()`: half away from zero.
    private static int DartRound(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    // Dart's `toStringAsFixed(2)`.
    private static string Fixed2(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

    private sealed class ValuesHolder(RangeValues values)
    {
        public RangeValues Values { get; set; } = values;
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
