using Xunit;

// C#-only test infrastructure; no Dart parity source.

// The framework's `Scheduler`, `FocusManager` and `RawKeyboard` are process-wide singletons, and
// clock-driven tests advance the shared frame clock. Running test classes in parallel lets one class
// rewind another class's tickers mid-animation, so the assembly runs serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
