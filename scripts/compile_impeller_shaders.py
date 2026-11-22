#!/usr/bin/env python3
"""Compile the framework's pinned fragment shaders into Impeller runtime-stage bundles.

The Impeller backend (src/Plumix.Impeller, branch proto/impeller) runs a core program by the bundle compiled
from the same GLSL source (ImpellerShaders matches them by uniform list). Bundles must come from the impellerc
of the Impeller the backend links, so this uses the tools archive pinned by Plumix.NImpeller's lock file:

    cd nimpeller-src && ./build.sh DownloadImpeller --rid <host rid> --tools

Run after changing the material_ui pin or bumping Plumix.NImpeller; commit the outputs.
"""

from pathlib import Path
import platform
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
SHADERS = [ROOT / "material-ui-src/lib/src/shaders/ink_sparkle.frag"]
TARGET = ROOT / "src/Plumix.Impeller/Shaders"


def host_rid() -> str:
    system = {"Darwin": "osx", "Linux": "linux", "Windows": "win"}[platform.system()]
    arch = "arm64" if platform.machine().lower() in ("arm64", "aarch64") else "x64"
    return f"{system}-{arch}"


def main() -> int:
    tools = ROOT / "nimpeller-src/external/impeller_sdk" / host_rid() / "tools"
    impellerc = tools / "bin" / ("impellerc.exe" if platform.system() == "Windows" else "impellerc")
    if not impellerc.exists():
        print(f"impellerc not found at {impellerc}; download the NImpeller tools archive first.", file=sys.stderr)
        return 1

    TARGET.mkdir(parents=True, exist_ok=True)
    scratch = TARGET / ".spirv"
    scratch.mkdir(exist_ok=True)
    for source in SHADERS:
        bundle = TARGET / f"{source.name}.iplr"
        subprocess.run([
            str(impellerc), "--runtime-stage-gles", "--runtime-stage-gles3", "--runtime-stage-metal",
            "--runtime-stage-vulkan", "--iplr", f"--input={source}", f"--sl={bundle}",
            f"--spirv={scratch / (source.name + '.spv')}", f"--include={tools / 'shader_lib'}",
        ], check=True)
        # The source travels with its bundle: the backend matches core programs by its uniform declarations.
        shutil.copyfile(source, TARGET / source.name)
        print(f"{bundle.relative_to(ROOT)} ({bundle.stat().st_size} bytes)")
    shutil.rmtree(scratch)
    return 0


if __name__ == "__main__":
    sys.exit(main())
