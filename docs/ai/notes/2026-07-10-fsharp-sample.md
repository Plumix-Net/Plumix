# F# interop probe: Plumix.FSharpSample (2026-07-10)

Goal: validate that the raw Plumix API is consumable from F# before designing an
F#-facing DSL (`Plumix.FSharp` factory functions, `Plumix.Elmish`). Added
`src/Sample/Plumix.FSharpSample` — a desktop counter app (StatefulWidget +
Material Scaffold/AppBar/FAB) written directly against the C# API.

## What works out of the box

- Subclassing `StatelessWidget` / `StatefulWidget` / `State`, overriding `Build`,
  `CreateState` — no issues, including mutually recursive widget/state types via
  `type ... and ...`.
- C# named/optional constructor parameters (`Column(mainAxisAlignment = ..., spacing = 12.0)`).
- Heterogeneous `children` arrays need **no** per-element `:> Widget` upcasts:
  F# type-directed conversion inserts them because the target type
  (`IReadOnlyList<Widget>`) is known. `[| Text(...); Row(...) |]` just works.
- `Nullable<T>` parameters and properties accept plain values
  (`fontSize = 34.0`, `InitialWindowSize = Size(350.0, 700.0)`).
- F# lambdas convert to `Action`/`Action<T>` callbacks implicitly.
- No-XAML hosting: `PlumixApplication` subclass adds `FluentTheme()` in
  `Initialize()` instead of loading `App.axaml`.

## Friction found (candidate core tweaks / DSL motivations)

1. **`State.SetState` is unreachable from lambdas** — F# error FS0491: protected
   members cannot be accessed from inner lambda expressions, and every widget
   callback is a lambda. The existing public `State.InvokeSetState` helper is the
   workaround and is now load-bearing for F# support; do not remove it (consider
   documenting it as the F#-facing API, or making `SetState` public like other
   framework methods).
2. **Central Package Management silently drops FSharp.Core** — the F# SDK sets
   `DisableImplicitFSharpCoreReference=true` when `ManagePackageVersionsCentrally`
   is enabled. The project compiles (fsc uses the SDK's bundled FSharp.Core) but
   crashes at startup with `FileNotFoundException: FSharp.Core`. Fixed by adding
   an explicit `FSharp.Core` `PackageVersion`/`PackageReference`.
3. **Namespace split is invisible in F#** — layout enums (`MainAxisAlignment`,
   `CrossAxisAlignment`, ...) live in `Plumix.Rendering`, not `Plumix.Widgets`;
   C# samples don't notice (`using` both), F# needs the extra
   `open Plumix.Rendering`. A future `Plumix.FSharp` should re-export or
   auto-open the common surface.
4. **Float literals** — every numeric parameter is `double`, so F# requires
   `12.0`/`34.0` (or a DSL that accepts ints where sensible).

## Validation

- `dotnet build src/Sample/Plumix.FSharpSample/Plumix.FSharpSample.fsproj` green.
- App launched via desktop host; widget tree mounts and runs (verified by
  process staying alive past `WidgetHost` mount, where a deliberate earlier
  misconfiguration crashed).
- Full-solution build blocked only by a pre-existing local iOS SDK/Xcode 26.6
  mismatch in `Plumix.iOS`, unrelated to this change.

## Next steps (per fsharp-branch plan)

1. ~~`Plumix.FSharp` package: factory functions returning `Widget` (Feliz-style).~~
   Done same day: `src/Plumix.FSharp` (`Ui` static factories + `Prelude.fs` type
   re-exports so app code needs only `open Plumix.FSharp`); the sample now uses
   it. Design notes: factories return `Widget` and take `Widget seq` children, so
   F# lists compose without upcasts; F# optional args (`?param`) are mapped to
   the C# defaults via `Option.toNullable`/`Option.toObj`/`defaultArg`, which is
   the boilerplate the package absorbs. `Ui.appBar` returns typed `AppBar`
   because `Scaffold` requires it. Package layering follows INVARIANTS: depends
   on `Plumix` + `Plumix.Material` only (downstream edge, like samples).
2. `Plumix.Elmish`: MVU host inside a `StatefulWidget`, framework reconciliation
   as the diffing layer.
3. Optional CE-based DSL experiment on top.
4. Grow `Ui` coverage on demand (ListView, Container decoration surface,
   gesture/ink widgets) as F# samples exercise more of the framework.
