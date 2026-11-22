// C#-only infrastructure: no Dart parity source.
//
// The framework's colour is `Plumix.UI.Color` (dart:ui `Color`). The few files that still import a
// backend namespace (the Avalonia/Skia path interop) would see a `Color` of its own there; this alias
// makes the unqualified name mean the framework type everywhere in the project.
global using Color = Plumix.UI.Color;
// Geometry: dart:ui Offset/Size/Rect; the unqualified names never mean Avalonia's.
global using Offset = global::Plumix.UI.Offset;
global using Size = global::Plumix.UI.Size;
global using Rect = global::Plumix.UI.Rect;
// Text: dart:ui FontWeight/FontStyle, never Avalonia.Media's.
global using FontWeight = global::Plumix.UI.FontWeight;
global using FontStyle = global::Plumix.UI.FontStyle;
