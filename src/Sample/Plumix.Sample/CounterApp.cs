using Plumix.Material;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/counter_app.dart (exact sample parity)

namespace Plumix;

public sealed class CounterApp : StatefulWidget
{
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
        }

        public override Widget Build(BuildContext context)
        {
            return new MaterialApp(
                debugShowCheckedModeBanner: false,
                title: "Flutter.Net Sample",
                theme: new ThemeData(
                    textTheme: new MaterialTextTheme(
                        bodyMedium: MaterialTextTheme.DefaultBodyMedium.CopyWith(
                            fontSize: 14,
                            height: 1.43,
                            letterSpacing: 0.25))),
                home: new CounterScope(
                    _model,
                    new SampleGalleryScreen()));
        }
    }
}
