using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit.Sdk;

// C#-only test infrastructure: flutter_test's restoration.dart (`TestRestorationData`,
// `TestRestorationManager`) and `WidgetTester.restartAndRestore`/`getRestorationData`/`restoreFrom`
// (widget_tester.dart).

namespace Plumix.Tests;

internal sealed partial class FrameworkDartTester
{
    private RestorationManager? _previousRestorationManager;

    /// <summary>
    /// Dart's <c>binding.restorationManager</c>: the tester installs a fresh one as
    /// <see cref="RestorationManager.Instance"/> for its lifetime, as flutter_test's binding does for
    /// every test.
    /// </summary>
    public FlutterTestRestorationManager RestorationManager { get; private set; } = null!;

    /// <summary>
    /// Dart's <c>tester.restartAndRestore</c>: tears the tree down, feeds the data the framework last
    /// sent to the engine back to the manager and pumps the last <see cref="PumpWidget"/> widget again.
    /// </summary>
    public void RestartAndRestore()
    {
        AssertRootBucketAccessed("restoration data has been collected to restore from");
        Widget widget = _lastWidget;
        TestRestorationData restorationData = RestorationManager.RestorationData;
        PumpWidget(new Container(key: new UniqueKey()));
        RestorationManager.RestoreFrom(restorationData);
        PumpWidget(widget);
    }

    /// <summary>Dart's <c>tester.getRestorationData</c>.</summary>
    public TestRestorationData GetRestorationData()
    {
        AssertRootBucketAccessed("restoration data has been collected");
        return RestorationManager.RestorationData;
    }

    /// <summary>Dart's <c>tester.restoreFrom(data)</c>: restores and pumps a frame.</summary>
    public void RestoreFrom(TestRestorationData data)
    {
        RestorationManager.RestoreFrom(data);
        Pump();
    }

    private void AssertRootBucketAccessed(string what)
    {
        if (!RestorationManager.DebugRootBucketAccessed)
        {
            throw new XunitException(
                "The current widget tree did not inject the root bucket of the RestorationManager and "
                + $"therefore no {what}. Did you forget to wrap your widget tree in a RootRestorationScope?");
        }
    }

    private void InstallRestorationManager()
    {
        _previousRestorationManager = Plumix.UI.RestorationManager.Instance;
        RestorationManager = new FlutterTestRestorationManager();
        Plumix.UI.RestorationManager.Instance = RestorationManager;
    }

    private void UninstallRestorationManager()
    {
        if (_previousRestorationManager is null)
        {
            return;
        }

        if (ReferenceEquals(Plumix.UI.RestorationManager.Instance, RestorationManager))
        {
            Plumix.UI.RestorationManager.Instance = _previousRestorationManager;
        }

        RestorationManager.Dispose();
        _previousRestorationManager = null;
    }
}

/// <summary>
/// flutter_test's <c>TestRestorationData</c>: an opaque, identity-compared snapshot of the encoded
/// restoration data.
/// </summary>
internal sealed class TestRestorationData(byte[]? binary)
{
    /// <summary>Dart's <c>TestRestorationData.empty</c>.</summary>
    public static readonly TestRestorationData Empty = new(null);

    public byte[]? Binary { get; } = binary;
}

/// <summary>
/// flutter_test's <c>TestRestorationManager</c>: restoration is enabled from the start (with empty
/// data), so the root bucket is available synchronously, and whatever the framework sends to the engine
/// becomes the data a later <see cref="RestoreFrom"/> can hand back.
/// </summary>
/// <remarks>
/// Named apart from <see cref="TestRestorationManager"/>, the services-test mock of
/// <c>test/services/restoration.dart</c>.
/// </remarks>
internal sealed class FlutterTestRestorationManager : RestorationManager
{
    public FlutterTestRestorationManager()
    {
        // Ensures that the root bucket is always available synchronously.
        RestoreFrom(TestRestorationData.Empty);
    }

    /// <summary>Dart's <c>restorationData</c>: the last data sent to the engine.</summary>
    public TestRestorationData RestorationData { get; private set; } = TestRestorationData.Empty;

    /// <summary>Dart's <c>debugRootBucketAccessed</c>.</summary>
    public bool DebugRootBucketAccessed { get; private set; }

    public override void GetRootBucket(Action<RestorationBucket?> callback)
    {
        DebugRootBucketAccessed = true;
        base.GetRootBucket(callback);
    }

    /// <summary>Dart's <c>restoreFrom</c>.</summary>
    public void RestoreFrom(TestRestorationData data)
    {
        RestorationData = data;
        HandleRestorationUpdateFromEngine(enabled: true, data: data.Binary);
    }

    /// <summary>Dart's <c>disableRestoration</c>.</summary>
    public void DisableRestoration()
    {
        RestorationData = TestRestorationData.Empty;
        HandleRestorationUpdateFromEngine(enabled: false, data: null);
    }

    protected override void InitChannels()
    {
    }

    protected override void GetRootBucketFromEngine()
    {
        HandleRestorationUpdateFromEngine(enabled: true, data: RestorationData.Binary);
    }

    protected override void SendToEngine(byte[] encodedData)
    {
        RestorationData = new TestRestorationData(encodedData);
    }
}
