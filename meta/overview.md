# Plumix — Project Overview

> Flutter-inspired UI framework for .NET. Build declarative, widget-based applications in C# using the same architecture as Flutter — Widget, Element, RenderObject — powered by Avalonia and .NET 10.

**Website:** https://plumix.net/
**GitHub:** https://github.com/Plumix-Net/Plumix
**Packages:** https://github.com/Plumix-Net/Plumix.Packages
**NuGet:** https://www.nuget.org/packages/Plumix/

---

## One-liner

> Flutter's widget model, in C#, for every platform .NET reaches.

---

## Short description (tweet / GitHub About / NuGet tagline)

Plumix is a Flutter-inspired UI framework for .NET. Write declarative, widget-based UIs in C# with Flutter's Widget/Element/RenderObject architecture, powered by Avalonia.

---

## Medium description (landing page hero / Product Hunt / README)

Plumix brings Flutter's proven UI architecture to the .NET ecosystem. Instead of adapting to a new mental model, Flutter developers can write C# using the same primitives they already know — `StatelessWidget`, `StatefulWidget`, `BuildContext`, `RenderObject` — and ship to desktop, web, Android, and iOS from a single codebase.

The framework is built on top of Avalonia for platform infrastructure (windowing, input, drawing backend) while keeping layout, paint, and widget lifecycle entirely framework-owned — the same architectural split Flutter uses between the framework and the engine.

---

## Long description (blog intro / Product Hunt story / conference abstract)

Modern cross-platform UI development is dominated by two mental models: the component-based model (React, SwiftUI) and Flutter's widget tree model. Flutter's approach — immutable widgets, a reconciled element tree, and a framework-owned render pipeline — has proven itself for building complex, high-performance UIs. But it's locked to Dart.

Plumix is a faithful port of Flutter's architecture to C# and .NET. The goal is not to create "Flutter-like" abstractions on top of existing .NET UI frameworks, but to reimplement the actual architecture: the widget/element reconciliation loop, the `RenderObject`/`RenderBox` layout model, the gesture arena, the navigator stack, the sliver scrolling protocol — all of it, in idiomatic C#.

This means Flutter developers can bring their knowledge directly to .NET. A `StatefulWidget` in Plumix works the same way as in Flutter. `setState` triggers the same rebuild cycle. `InheritedWidget` propagates data the same way. The goal is that porting a Flutter widget to Plumix is a mechanical translation, not a reimagination.

Under the hood, Plumix uses Avalonia purely as platform infrastructure — for window hosting, native input events, and the drawing backend. Everything above the canvas is framework-owned: layout passes, paint phases, compositing layers, semantics, focus management, gesture recognition.

---

## Key value propositions

- **Flutter developers join .NET without a new mental model.** Same widget architecture, same lifecycle, same patterns — just C# instead of Dart.
- **Mechanical Dart → C# porting.** Plumix's fidelity goal means a Flutter widget can be translated line-by-line into C#. No paradigm shifts.
- **Cross-platform from day one.** Desktop (Windows, macOS, Linux), Browser (WebAssembly), Android, iOS — all from one codebase via Avalonia's platform layer.
- **.NET 10 and modern C#.** Nullable reference types, records, collection expressions, top-level programs. No legacy baggage.
- **Material Design 3 out of the box.** Full theming system, component library, adaptive widgets.

---

## Feature highlights

### Core framework
- Widget/Element/RenderObject architecture, faithful to Flutter
- `StatelessWidget`, `StatefulWidget`, `State`, `setState`, `BuildContext`
- `InheritedWidget`, `InheritedModel`, `InheritedNotifier`
- Full widget reconciliation loop (`BuildOwner`, `PipelineOwner`)

### Layout & rendering
- `RenderBox` with full 2D box model (min/max constraints)
- Flex layout: `Row`, `Column`, `Flexible`, `Expanded`, `Spacer`
- Stack, positioned, aligned, aspect ratio, fitted box layouts
- Clip, decoration, opacity, transform, offstage render proxies
- Compositing layer tree (offset, opacity, transform, clip)

### Scrolling & lists
- `Scrollable`, `ScrollView`, `SingleChildScrollView`, `CustomScrollView`
- `ListView`, `GridView`, `Viewport`
- Full Sliver protocol: `SliverList`, `SliverGrid`, `SliverPadding`, `SliverFillViewport`
- `ScrollController`, physics, keep-alive

### Gestures & input
- Gesture arena with conflict resolution
- `TapGestureRecognizer`, `LongPressGestureRecognizer`, `DragGestureRecognizer`
- `GestureDetector`, `Listener`, `PointerRouter`
- Full focus system: `Focus`, `FocusScope`, tab traversal

### Navigation
- `Navigator` with push/pop/replace semantics
- Typed and named routes, `ModalRoute`, `PageRoute`
- `NavigatorObserver`, back button handling
- Hero transitions with custom flight shuttles

### Accessibility & semantics
- Semantics annotations on all framework widgets
- Avalonia accessibility bridge integration

### Material Design 3 library
- **Structure:** `Scaffold`, `AppBar`, `Drawer`, `BottomNavigationBar`, `SafeArea`
- **Buttons:** `ElevatedButton`, `FilledButton`, `OutlinedButton`, `TextButton`, `IconButton`
- **Controls:** `Checkbox`, `Switch`, `Radio`, `FloatingActionButton` (4 variants + extended)
- **Content:** `Card` (elevated/filled/outlined), `ListTile` (1/2/3-line), `Tooltip`
- **Theming:** `ThemeData`, `ColorScheme`, per-component theme data classes
- **Typography & icons:** M3 text styles, icon support

### Cupertino library
- Adaptive `CupertinoCheckbox`, `CupertinoRadio`
- Growing alongside Material coverage

### Platform targets
| Platform | Status |
|----------|--------|
| Windows (Desktop) | ✅ Supported |
| macOS (Desktop) | ✅ Supported |
| Linux (Desktop) | ✅ Supported |
| Browser (WASM) | ✅ Supported |
| Android | ✅ Supported |
| iOS | ✅ Supported |

---

## NuGet packages

| Package | Description |
|---------|-------------|
| [`Plumix`](https://www.nuget.org/packages/Plumix/) | Core framework — Widget/Element/RenderObject architecture, layout, gestures, navigation, scrolling |
| [`Plumix.Material`](https://www.nuget.org/packages/Plumix.Material/) | Material Design 3 component library and theming |
| [`Plumix.Cupertino`](https://www.nuget.org/packages/Plumix.Cupertino/) | iOS/macOS Cupertino-style adaptive components |

Install:
```
dotnet add package Plumix
dotnet add package Plumix.Material
dotnet add package Plumix.Cupertino
```

---

## Current status

**Version:** `0.1.0-alpha.3`
**Phase:** M4 — Material library refinement and advanced component coverage

| Area | Status |
|------|--------|
| Core framework (Widget/Element/RenderObject) | ✅ Complete |
| Render pipeline (layout/paint/compositing/semantics) | ✅ Complete |
| Gesture system | ✅ Complete |
| Navigation (routes, hero transitions, observers) | ✅ Complete |
| Scrolling & slivers | ✅ Complete |
| Focus & accessibility | ✅ Complete |
| Text input & editing | ✅ Complete |
| Material Design 3 library | 🔄 In progress (M4) |
| Cupertino library | ⏳ Planned |
| Cross-host parity (mobile/browser) | ⏳ Planned (M5) |

---

## Tech stack

- **Language:** C# 13, .NET 10
- **Platform layer:** [Avalonia](https://avaloniaui.net/) (windowing, input, drawing backend)
- **Target frameworks:** `net10.0` for all packages
- **Reference implementation:** [Flutter](https://flutter.dev/) (Dart source used as porting source of truth)
- **Packages:** Published to NuGet

---

## Who is it for?

- **Flutter / Dart developers** moving to .NET who want to keep their mental model
- **.NET developers** who want Flutter's architecture without leaving C#
- **Teams** building cross-platform desktop/web/mobile apps on .NET
- **Developers** who want to port existing Flutter widgets to a C# ecosystem

---

## Who is building it?

Plumix is an open source project. Contributions, bug reports, and discussion are welcome via GitHub.

- GitHub: https://github.com/Plumix-Net/Plumix
- Additional packages: https://github.com/Plumix-Net/Plumix.Packages
- Website: https://plumix.net/

---

## Code example

```csharp
using Plumix.Widgets;
using Plumix.Material;

public sealed class CounterApp : StatefulWidget
{
    public override State CreateState() => new _CounterState();
}

sealed class _CounterState : State<CounterApp>
{
    int _count = 0;

    public override Widget Build(BuildContext context)
    {
        return new Scaffold(
            appBar: new AppBar(title: new Text("Plumix Counter")),
            body: new Center(
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    children:
                    [
                        new Text("You have pushed the button this many times:"),
                        new Text(
                            _count.ToString(),
                            style: Theme.Of(context).TextTheme.DisplaySmall
                        ),
                    ]
                )
            ),
            floatingActionButton: new FloatingActionButton(
                onPressed: () => SetState(() => _count++),
                child: new Icon(Icons.Add)
            )
        );
    }
}
```

---

## Comparison with alternatives

| | Plumix | MAUI | Avalonia | Uno Platform |
|-|--------|------|----------|--------------|
| Flutter parity | ✅ Architecture-level | ❌ | ❌ | ❌ |
| Dart → C# porting | ✅ Mechanical | ❌ | ❌ | ❌ |
| Owns render pipeline | ✅ | Partial | Partial | Partial |
| Material Design 3 | ✅ | ✅ | Community | ✅ |
| .NET 10 | ✅ | ✅ | ✅ | ✅ |
| WASM | ✅ | ✅ | ✅ | ✅ |
| Mobile | ✅ | ✅ | ✅ | ✅ |
