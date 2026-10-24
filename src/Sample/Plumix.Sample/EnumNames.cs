using System;

namespace Plumix.Sample;

// C#-only infrastructure: no Dart parity source.
//
// Dart's `Enum.name` (`platform.name`, `status.name`) for the sample's readouts, so they print the
// lower-camel-case member names the Dart sample shows.
internal static class EnumNames
{
    public static string Name(this Enum value) => Plumix.Foundation.Diagnostics.EnumName(value);
}
