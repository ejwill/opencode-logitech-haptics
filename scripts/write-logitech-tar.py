#!/usr/bin/env python3

"""Write a Logi Plugin Service-compatible uncompressed POSIX USTAR archive."""

from __future__ import annotations

import io
import pathlib
import sys
import tarfile


def add_entry(archive: tarfile.TarFile, name: str, path: pathlib.Path, fixed_mtime: int) -> None:
    if path.is_dir():
        info = tarfile.TarInfo(name if name.endswith("/") else f"{name}/")
        info.mode = 0o777
        info.uid = 0
        info.gid = 0
        info.uname = ""
        info.gname = ""
        info.type = tarfile.DIRTYPE
        info.mtime = fixed_mtime
        archive.addfile(info)
        return

    data = path.read_bytes()
    info = tarfile.TarInfo(name)
    info.mode = 0o666
    info.uid = 0
    info.gid = 0
    info.uname = ""
    info.gname = ""
    info.size = len(data)
    info.mtime = fixed_mtime
    archive.addfile(info, io.BytesIO(data))


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: write-logitech-tar.py <extracted-package> <output.lplug4>", file=sys.stderr)
        return 2

    source = pathlib.Path(sys.argv[1]).resolve()
    output = pathlib.Path(sys.argv[2]).resolve()
    required = ["metadata", "bin", "events"]
    missing = [folder for folder in required if not (source / folder).is_dir()]
    if missing:
        print(f"Package is missing directories: {', '.join(missing)}", file=sys.stderr)
        return 1

    fixed_mtime = 1767974400
    with tarfile.open(output, "w", format=tarfile.USTAR_FORMAT) as archive:
        for folder_name in required:
            folder = source / folder_name
            add_entry(archive, f"{folder_name}/", folder, fixed_mtime)
            for child in sorted(folder.rglob("*")):
                relative = child.relative_to(source).as_posix()
                add_entry(archive, relative, child, fixed_mtime)

    names = tarfile.open(output).getnames()
    for required_entry in [
        "metadata/LoupedeckPackage.yaml",
        "metadata/Icon256x256.png",
        "bin/OpenCodeCompanionPlugin.dll",
    ]:
        if required_entry not in names:
            print(f"Package is missing {required_entry}", file=sys.stderr)
            return 1

    print(f"wrote {output} ({output.stat().st_size} bytes, {len(names)} entries)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
