using Plumix.UI;

// Dart parity source: flutter/packages/flutter_test/lib/src/test_default_binary_messenger.dart

namespace Plumix.Tests;

/// <summary>
/// Like flutter_test, synthetic inbound messages call the registered handler directly and return
/// its completion/errors. Real engine buffer delivery is covered with ServicesBinding explicitly.
/// </summary>
internal class TestServicesBinding : ServicesBinding
{
    protected override BinaryMessenger CreateBinaryMessenger() => new PlatformBinaryMessenger();
}
