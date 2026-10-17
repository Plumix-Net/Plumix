using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/services/keyboard_maps_test.dart
public sealed class KeyboardMapsTests
{
    [Fact]
    public void TheWebPhysicalKeyMappingDoNotHaveEntriesWithoutAChromeCode()
    {
        // Regression test for https://github.com/flutter/flutter/pull/106074.
        // There is an entry called KBD_ILLUM_DOWN in dom_code_data.inc, but it
        // has an empty "Code" column. This entry should not be present in the
        // web mapping.
        Assert.False(KeyboardMaps.KWebToPhysicalKey.ContainsKey("KbdIllumDown"));
    }
}
