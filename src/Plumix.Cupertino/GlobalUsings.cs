// C#-only infrastructure: no Dart parity source.
//
// The framework's colour is `Plumix.UI.Color` (dart:ui `Color`). Most files also import
// `Avalonia.Media` for brushes, fonts and the drawing backend, which has a `Color` of its own; this
// alias makes the unqualified name mean the framework type everywhere in the project.
global using Color = Plumix.UI.Color;
// dart:ui's `Brightness` lives in `Plumix.UI`; Material re-exports it in Dart, so every file here
// sees it unqualified too.
global using Brightness = Plumix.UI.Brightness;
