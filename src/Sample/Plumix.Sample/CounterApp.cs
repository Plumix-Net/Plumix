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
            return new CounterScope(
                _model,
                new Theme(
                    data: ThemeData.Light,
                    child: new SampleGalleryScreen()));
        }
    }
}
