using System.Reflection;
using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only test infrastructure: renders every sample route to a PNG for the screen-by-screen comparison
// with the Dart sample (dart_sample/test/screen_capture_test.dart runs the same pump sequence). Opt-in:
//   SCREEN_CAPTURE_DIR=/abs/dir dotnet test src/Plumix.Tests --filter SampleScreenCaptureTests

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SampleScreenCaptureTests : IDisposable
{
    private static readonly string? OutputDir = Environment.GetEnvironmentVariable("SCREEN_CAPTURE_DIR");

    public SampleScreenCaptureTests() => SchedulerBinding.ResetForTests();

    public void Dispose() => SchedulerBinding.ResetForTests();

    public static TheoryData<string> Routes()
    {
        var data = new TheoryData<string>();
        Type routes = typeof(CounterApp).Assembly.GetType("Plumix.SampleRoutes", throwOnError: true)!;
        foreach (FieldInfo field in routes.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            data.Add((string)field.GetRawConstantValue()!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public void Capture(string route)
    {
        if (OutputDir is null)
        {
            return;
        }

        string name = route == "/" ? "menu" : route[1..].Replace('/', '_');
        var errors = new List<string>();
        // AutomatedTestWidgetsFlutterBinding.disableShadows: flutter_test paints every test without shadows.
        RenderingDebug.DisableShadows = true;
        try
        {
            CaptureInto(route, name, errors);
        }
        finally
        {
            RenderingDebug.DisableShadows = false;
        }

        File.WriteAllLines(System.IO.Path.Combine(OutputDir, name + ".errors.txt"), errors);
    }

    private static void CaptureInto(string route, string name, List<string> errors)
    {
        using (var tester = new FrameworkDartTester(fakeGestureTimers: true))
        {
            FlutterError.OnError = details => errors.Add(
                $"{details.Library}: {details.ExceptionAsString().Split('\n')[0]}");
            tester.PumpWidget(new CounterApp());
            tester.Pump();
            if (route != "/")
            {
                tester.State<NavigatorState>(Find.ByType<Navigator>().Last).RestorablePushNamed(route);
            }

            tester.Pump();
            tester.Pump(TimeSpan.FromSeconds(1));
            tester.Pump(TimeSpan.FromSeconds(1));
            // Dart waits on the real event loop here for image decoding, then pumps once more.
            tester.Pump();

            File.WriteAllLines(
                System.IO.Path.Combine(OutputDir!, name + ".texts.txt"),
                tester.ElementsOfType<RichText>()
                    .Select(element => ((RichText)element.Widget).Text.ToPlainText().Replace("\n", "\\n")));
            File.WriteAllLines(
                System.IO.Path.Combine(OutputDir!, name + ".layout.txt"),
                tester.ElementsOfType<RichText>().Select(element =>
                {
                    if (element.RenderObject is not RenderBox { Attached: true, HasSize: true } box)
                    {
                        return "-";
                    }

                    Point origin = box.LocalToGlobal(new Point(0, 0));
                    return FormattableString.Invariant(
                        $"{origin.X:0.0000} {origin.Y:0.0000} {box.Size.Width:0.0000} {box.Size.Height:0.0000}");
                }));
            using Bitmap image = Assert.IsAssignableFrom<OffsetLayer>(tester.RenderView.Layer)
                .ToImageSync(new Rect(0, 0, 800, 600));
            image.Save(System.IO.Path.Combine(OutputDir!, name + ".png"));
            tester.PumpWidget(new SizedBox());
            tester.Pump(TimeSpan.FromMinutes(1));
        }
    }
}
