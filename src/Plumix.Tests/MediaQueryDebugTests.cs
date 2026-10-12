using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/debug_test.dart

namespace Plumix.Tests;

public sealed class MediaQueryDebugTests
{
    [DebugOnlyFact]
    public void MissingMediaQueryReportsFiveOrderedDiagnostics()
    {
        var owner = TestBuildOwner.Create();
        RootElement root = new RootWidget(child: new Probe()).Attach(owner);
        try
        {
            var error = Assert.Throws<FlutterError>(() =>
                WidgetsDebug.DebugCheckHasMediaQuery(root.ChildElement!));
            Assert.Equal(5, error.Diagnostics.Count);
            Assert.Equal("No MediaQuery widget ancestor found.", error.Diagnostics[0].ToDescription());
            Assert.Equal("Probe widgets require a MediaQuery widget ancestor.", error.Diagnostics[1].ToDescription());
            Assert.IsType<DiagnosticsProperty<Element>>(error.Diagnostics[2]);
            Assert.Equal(DiagnosticLevel.Hint, error.Diagnostics[4].Level);
            Assert.Contains("The ownership chain for the affected widget is", error.Message);
            Assert.Contains("a View widget, which introduces a MediaQuery.", error.Diagnostics[4].ToDescription());
        }
        finally
        {
            root.UnmountRoot();
        }
    }

    [Fact]
    public void AncestorCheckAcceptsTheMediaQueryItselfAndDoesNotSubscribe()
    {
        var owner = TestBuildOwner.Create();
        var probe = new Probe();
        RootElement root = new RootWidget(child: new MediaQuery(new MediaQueryData(), probe)).Attach(owner);
        try
        {
            var mediaQuery = (InheritedElement)root.ChildElement!;
            Element? child = null;
            mediaQuery.VisitChildren(element => child = element);
            Assert.True(WidgetsDebug.DebugCheckHasMediaQuery(mediaQuery));
            Assert.True(WidgetsDebug.DebugCheckHasMediaQuery(child!));
            owner.BuildScope(root, () =>
                mediaQuery.Update(new MediaQuery(new MediaQueryData(TextScaleFactor: 2.0), probe)));
            Assert.Equal(0, ((ProbeElement)child!).DependencyChanges);
        }
        finally
        {
            root.UnmountRoot();
        }
    }

    private sealed class Probe : Widget
    {
        public override Element CreateElement() => new ProbeElement(this);
    }

    private sealed class ProbeElement(Widget widget) : Element(widget)
    {
        public int DependencyChanges { get; private set; }

        public override void DidChangeDependencies()
        {
            DependencyChanges++;
            base.DidChangeDependencies();
        }
    }
}
