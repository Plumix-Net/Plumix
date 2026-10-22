using Plumix.Material;
using Plumix.Cupertino;
using Plumix.Widgets;
using Xunit;

// Dart parity source: dart_sample/lib/demos/general/restoration_demo_page.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RestorationSampleTests : IDisposable
{
    public RestorationSampleTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    [Fact]
    public void Gallery_RestoresTheOpenDemoCounterNoteAndScrollAfterRestart()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new CounterApp());
        tester.PumpAndSettle();
        tester.State<NavigatorState>(Find.ByType<Navigator>().Last).RestorablePushNamed("/restoration");
        tester.PumpAndSettle();
        Assert.True(tester.Any(Find.Text("State restoration: enabled")));

        tester.Tap(Find.Text("Increment"));
        tester.Pump();
        tester.Tap(Find.Text("Increment"));
        tester.Pump();
        Finder note = Find.ByWidgetPredicate(widget => widget is TextField { RestorationId: null });
        tester.EnterText(note, "Saved through the native channel");
        tester.Widget<TextField>(note).FocusNode?.Unfocus();
        tester.Pump();
        Finder scroll = Find.ByWidgetPredicate(widget => widget is Scrollable { RestorationId: "scroll" });
        tester.State<ScrollableState>(scroll).Position.JumpTo(400);
        tester.Pump();

        tester.RestartAndRestore();
        tester.PumpAndSettle();
        Assert.Null(tester.TakeException());

        Assert.True(tester.Any(Find.ByType<RestorationDemoPage>()));
        Assert.True(tester.Any(Find.Text("Restorable counter: 2")));
        Assert.Equal(
            "Saved through the native channel",
            tester.Widget<TextField>(note).Controller!.Text);
        Assert.Equal(400, tester.State<ScrollableState>(scroll).Position.Pixels, precision: 3);
    }

    [Fact]
    public void Gallery_RestoresOwnedMaterialAndCupertinoFieldsAndFormErrors()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new CounterApp());
        tester.PumpAndSettle();
        tester.State<NavigatorState>(Find.ByType<Navigator>().Last).RestorablePushNamed("/restoration");
        tester.PumpAndSettle();
        Finder material = Find.ByWidgetPredicate(widget => widget is TextField { RestorationId: "material-note" });
        Finder cupertino = Find.ByWidgetPredicate(widget =>
            widget is CupertinoTextField { RestorationId: "cupertino-note" });
        tester.EnterText(material, "Saved Material");
        tester.PumpAndSettle();
        tester.EnterText(cupertino, "Saved Cupertino");
        tester.PumpAndSettle();
        tester.EnterText(Find.ByType<TextFormField>(), string.Empty);
        tester.PumpAndSettle();
        tester.EnterText(Find.ByType<CupertinoTextFormFieldRow>(), string.Empty);
        tester.PumpAndSettle();

        tester.RestartAndRestore();
        tester.PumpAndSettle();
        Assert.Equal("Saved Material", tester.State<EditableText.EditableTextState>(
            Find.Descendant(of: material, matching: Find.ByType<EditableText>())).Widget.Controller.Text);
        Assert.Equal("Saved Cupertino", tester.State<EditableText.EditableTextState>(
            Find.Descendant(of: cupertino, matching: Find.ByType<EditableText>())).Widget.Controller.Text);
        Assert.Equal("Required", tester.State<FormFieldState<string>>(Find.ByType<TextFormField>()).ErrorText);
        Assert.Equal("Required", tester.State<FormFieldState<string>>(
            Find.ByType<CupertinoTextFormFieldRow>()).ErrorText);
        Assert.Null(tester.TakeException());
    }
}
