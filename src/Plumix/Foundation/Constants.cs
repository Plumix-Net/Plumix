using System.Runtime.InteropServices;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/constants.dart

namespace Plumix.Foundation;

/// <summary>
/// Compile-time (or, in .NET, JIT-foldable) constants describing the build the framework was
/// compiled for, plus the framework's floating-point tolerance.
/// </summary>
/// <remarks>
/// Dart derives every flag here from a `bool.fromEnvironment` define, which makes them `const` and
/// lets the compiler tree-shake `if (kReleaseMode)` branches. C# has no equivalent for a value that
/// must also be observable from a referencing assembly compiled separately, so the build-mode flags
/// are `static readonly` fields set from the `DEBUG`/`PROFILE` compilation symbols of this
/// assembly. The JIT constant-folds `static readonly` primitives after class initialization, so the
/// same dead branches are eliminated at runtime.
/// </remarks>
public static class Constants
{
    /// Whether the framework was compiled in release mode.
    ///
    /// Release mode is the mode Dart's `bool.fromEnvironment('dart.vm.product')` reports; in .NET it
    /// is a build with neither the `DEBUG` nor the `PROFILE` symbol defined.
    public static readonly bool KReleaseMode = !IsDebugBuild && !IsProfileBuild;

    /// Whether the framework was compiled in profile mode.
    public static readonly bool KProfileMode = IsProfileBuild;

    /// Whether the framework was compiled in debug mode.
    ///
    /// This is the negation of [KReleaseMode] and [KProfileMode] taken together, exactly as Dart's
    /// `kDebugMode` is.
    public static readonly bool KDebugMode = !KReleaseMode && !KProfileMode;

    /// The epsilon of tolerable double precision error.
    ///
    /// This is used in various places in the framework to allow for floating point
    /// precision loss in calculations. Differences below this threshold are safe to
    /// disregard.
    public const double PrecisionErrorTolerance = 1e-10;

    /// A constant that is true if the application was compiled to run on the web.
    public static bool KIsWeb => OperatingSystem.IsBrowser();

    /// A constant that is true if the application was compiled to WebAssembly.
    ///
    /// Dart reads `dart.tool.dart2wasm`; .NET's equivalent is the process architecture, which is
    /// `Wasm` for both the browser and WASI runtimes.
    public static bool KIsWasm => RuntimeInformation.ProcessArchitecture == Architecture.Wasm;

    private static bool IsDebugBuild =>
#if DEBUG
        true;
#else
        false;
#endif

    private static bool IsProfileBuild =>
#if PROFILE
        true;
#else
        false;
#endif
}
