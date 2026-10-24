using System.Globalization;
using Plumix.Material;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/counter_app.dart (exact sample parity)

namespace Plumix;

public sealed class CounterApp : StatefulWidget
{
    // Dart formats numbers the same way in every locale (`toStringAsFixed`, string interpolation), so
    // the sample's readouts use the invariant culture whatever the host machine's culture is.
    static CounterApp()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public override State CreateState() => new CounterAppState();

    private sealed class CounterAppState : State
    {
        private CounterAppModel _model = null!;

        public override void InitState()
        {
            _model = new CounterAppModel();
        }

        public override void Dispose()
        {
            _model.Dispose();

            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new MaterialApp(
                debugShowCheckedModeBanner: false,
                restorationScopeId: "sample",
                title: "Flutter.Net Sample",
                theme: new ThemeData(
                    textTheme: new TextTheme(
                        bodyMedium: new TextStyle(
                            FontSize: 14,
                            Height: 1.43,
                            LetterSpacing: 0.25))),
                home: new CounterScope(
                    _model,
                    new SampleGalleryScreen()));
        }
    }
}
