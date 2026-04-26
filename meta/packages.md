# Plumix — Package Descriptions

Copy-paste descriptions for NuGet, package registries, and release notes.

---

## Plumix (core)

**NuGet ID:** `Plumix`
**NuGet URL:** https://www.nuget.org/packages/Plumix/

### Short (NuGet tagline, ~160 chars)
Flutter-inspired UI framework for .NET. Widget/Element/RenderObject architecture in C#, powered by Avalonia. Cross-platform: desktop, web, Android, iOS.

### Medium (NuGet description field)
Plumix is a Flutter-inspired UI framework for .NET that implements Flutter's Widget/Element/RenderObject architecture in C#.

Build declarative, cross-platform UIs with the same primitives as Flutter — `StatelessWidget`, `StatefulWidget`, `BuildContext`, `RenderObject`, `InheritedWidget` — and target Windows, macOS, Linux, WebAssembly, Android, and iOS from a single codebase.

The framework owns its own render pipeline (layout, paint, compositing, semantics) and uses Avalonia only as a platform layer for windowing, input, and drawing. Gesture recognition, navigation, scrolling, focus management, and the full widget lifecycle are all implemented inside Plumix.

**What's included in the core package:**
- Widget/Element/RenderObject architecture (`StatelessWidget`, `StatefulWidget`, `State`, reconciliation)
- Full `RenderBox` layout model — flex, stack, alignment, constraints, aspect ratio
- Scrolling: `ListView`, `GridView`, `SingleChildScrollView`, `CustomScrollView`, full Sliver protocol
- Navigation: `Navigator`, routes, `ModalRoute`, hero transitions, `NavigatorObserver`
- Gesture system: tap, long-press, drag, gesture arena, `GestureDetector`
- Focus & accessibility: `Focus`, `FocusScope`, tab traversal, semantics
- Text input: `EditableText`, `TextInput`, selection, IME
- Inherited widgets: `MediaQuery`, `DefaultTextStyle`, `Directionality`, `IconTheme`
- Animation primitives: `AnimationController`, `Ticker`

---

## Plumix.Material

**NuGet ID:** `Plumix.Material`
**NuGet URL:** https://www.nuget.org/packages/Plumix.Material/

### Short
Material Design 3 component library for Plumix. Scaffold, buttons, cards, navigation, forms, theming — faithful to Flutter's Material widgets.

### Medium
Material Design 3 component library for the Plumix framework.

Implements Flutter's Material widget layer in C#: the same components, theming system, and behavioral semantics, so Flutter developers can port Material UIs to .NET without redesigning them.

**Components:**
- **Structure:** `Scaffold`, `AppBar`, `Drawer`, `BottomNavigationBar`, `SafeArea`
- **Buttons:** `ElevatedButton`, `FilledButton`, `OutlinedButton`, `TextButton`, `IconButton`
- **Controls:** `Checkbox`, `Switch`, `Radio`, `FloatingActionButton` (standard, small, large, extended)
- **Content:** `Card` (elevated/filled/outlined), `ListTile` (1/2/3-line), `Tooltip`
- **Theming:** `ThemeData`, `ColorScheme`, `TextTheme`, per-component theme data classes (M3 tokens)

Requires `Plumix` (core).

---

## Plumix.Cupertino

**NuGet ID:** `Plumix.Cupertino`
**NuGet URL:** https://www.nuget.org/packages/Plumix.Cupertino/

### Short
iOS/macOS Cupertino-style adaptive widgets for Plumix. Write platform-adaptive UIs that look native on Apple platforms.

### Medium
Cupertino (iOS/macOS) adaptive widget library for the Plumix framework.

Provides iOS/macOS-styled components that match Flutter's Cupertino widget library, enabling platform-adaptive UIs that automatically use the correct visual style per platform.

**Current components:**
- `CupertinoCheckbox` — iOS-style checkbox
- `CupertinoRadio` — iOS-style radio button

More components are added each release alongside Material coverage.

Requires `Plumix` (core).

---

## Release notes template

```
## Plumix X.Y.Z

### New
- ...

### Changed
- ...

### Fixed
- ...

**Install:**
dotnet add package Plumix --version X.Y.Z
dotnet add package Plumix.Material --version X.Y.Z
dotnet add package Plumix.Cupertino --version X.Y.Z
```

---

## Installation

```bash
# Core framework only
dotnet add package Plumix

# With Material Design 3
dotnet add package Plumix
dotnet add package Plumix.Material

# Full stack
dotnet add package Plumix
dotnet add package Plumix.Material
dotnet add package Plumix.Cupertino
```

Minimum requirement: **.NET 10**
