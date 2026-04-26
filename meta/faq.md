# Plumix — FAQ

Frequently asked questions for articles, interviews, and website Q&A sections.

---

**Q: What is Plumix?**

Plumix is a UI framework for .NET that brings Flutter's architecture to C#. It implements the same Widget/Element/RenderObject model that Flutter uses, so developers can build declarative, cross-platform UIs in C# using the exact same patterns they'd use in Flutter/Dart.

---

**Q: How is Plumix different from MAUI, Avalonia, or Uno Platform?**

Those frameworks have their own UI models. Plumix specifically targets Flutter's architecture — its goal is that a Flutter widget can be mechanically ported to C# without redesigning it. Avalonia is used under the hood as the platform layer (windowing, input, GPU drawing), but layout, paint, gestures, navigation, and widget lifecycle are all implemented inside Plumix itself, not delegated to Avalonia controls.

---

**Q: Do I need to know Flutter to use Plumix?**

No, but it helps. If you know Flutter, Plumix will feel immediately familiar — same widget primitives, same lifecycle, same patterns. If you don't know Flutter, Plumix is still a capable declarative UI framework. The learning curve is similar to learning Flutter itself, and Flutter's extensive documentation applies conceptually.

---

**Q: Why build on top of Avalonia instead of writing a full engine?**

Writing a rendering engine from scratch would take years. Avalonia already solves the hard platform problems: native windowing, platform input events, GPU-accelerated drawing (Skia/Direct3D/Metal/Vulkan), and cross-platform app lifecycle. Plumix takes exactly those pieces and builds the Flutter-style framework layer on top. This is the same split Flutter has between its Dart framework layer and the Flutter engine written in C++.

---

**Q: What platforms does Plumix support?**

Windows, macOS, Linux (via Avalonia desktop), WebAssembly (browser), Android, and iOS. All six platforms are targeted from a single C# codebase.

---

**Q: What is the current status?**

Plumix is in alpha (`0.1.0-alpha.3`). The core framework is complete — widget reconciliation, render pipeline, gesture system, navigation, and scrolling all work. The Material Design 3 component library is actively being built out (currently in M4 phase). The Cupertino library has foundational adaptive widgets with more planned.

---

**Q: Is Plumix production-ready?**

Not yet. Alpha versions are suitable for exploration, prototyping, and contributing. Expect API changes before a stable release.

---

**Q: What is the Dart reference sample in the repo?**

Every C# sample in Plumix has a mirror implementation in `dart_sample/` — the same UI built in Flutter/Dart. This serves as a source of truth for widget behavior. If the C# and Dart samples look and behave the same, the port is correct.

---

**Q: Can I contribute?**

Yes. The project is open source on GitHub: https://github.com/Plumix-Net/Plumix. Bug reports, feature discussions, and pull requests are welcome.

---

**Q: Where do I find additional packages?**

https://github.com/Plumix-Net/Plumix.Packages — community and supplemental packages for the Plumix ecosystem.

---

**Q: What .NET version is required?**

.NET 10. Plumix targets `net10.0` across all packages.

---

**Q: How does theming work?**

Plumix implements Material Design 3's theming system: a `ThemeData` object (with `ColorScheme`, text styles, and per-component theme data) propagates via `Theme` InheritedWidget, and all Material components resolve their colors and styles from it. This matches Flutter's theming API closely.

---

**Q: Is there an IDE extension or tooling?**

Not yet. Standard .NET/C# tooling works fine — Visual Studio, VS Code with C# Dev Kit, JetBrains Rider.

---

**Q: What is the license?**

Plumix has a dual license. See `LICENSE` (open source) and `LICENSE-COMMERCIAL` in the repository for details.
