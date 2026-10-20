#!/usr/bin/env python3
"""
Clean a binary little-endian 3DGS .ply: drops splats with non-finite
(NaN/inf) values and rewrites a minimal header into a new file.

Usage:
    python fix_ply.py input.ply output.ply
    python fix_ply.py input.ply output.ply --strip-extra   # keep only standard 3DGS props
"""
import argparse
import re
import sys
import numpy as np

TYPES = {
    "char": "i1", "int8": "i1", "uchar": "u1", "uint8": "u1",
    "short": "i2", "int16": "i2", "ushort": "u2", "uint16": "u2",
    "int": "i4", "int32": "i4", "uint": "u4", "uint32": "u4",
    "float": "f4", "float32": "f4", "double": "f8", "float64": "f8",
}

STANDARD = re.compile(
    r"^(x|y|z|nx|ny|nz|f_dc_\d+|f_rest_\d+|opacity|scale_\d+|rot_\d+)$"
)


def read_header(f):
    lines = []
    while True:
        line = f.readline()
        if not line:
            sys.exit("Error: end of file before 'end_header'")
        text = line.decode("ascii", errors="replace").strip()
        lines.append(text)
        if text == "end_header":
            return lines, f.tell()


def parse_header(lines):
    if lines[0] != "ply":
        sys.exit("Error: not a PLY file")
    fmt = None
    elements = []  # [name, count, [(propname, dtype)]]
    for ln in lines[1:]:
        p = ln.split()
        if not p:
            continue
        if p[0] == "format":
            fmt = p[1]
        elif p[0] == "element":
            elements.append([p[1], int(p[2]), []])
        elif p[0] == "property":
            if not elements:
                continue
            if p[1] == "list":
                elements[-1][2].append((p[-1], None))  # list prop marker
            else:
                if p[1] not in TYPES:
                    sys.exit(f"Error: unknown property type '{p[1]}'")
                elements[-1][2].append((p[-1], TYPES[p[1]]))
    if fmt != "binary_little_endian":
        sys.exit(f"Error: only binary_little_endian is supported (got '{fmt}')")
    return elements


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("input")
    ap.add_argument("output")
    ap.add_argument("--strip-extra", action="store_true",
                    help="keep only standard 3DGS properties")
    args = ap.parse_args()

    with open(args.input, "rb") as f:
        lines, data_start = read_header(f)
    elements = parse_header(lines)

    names = [e[0] for e in elements]
    if "vertex" not in names:
        sys.exit("Error: no 'vertex' element found")
    if names[0] != "vertex":
        sys.exit("Error: 'vertex' must be the first element (found: %s)" % names)
    if "chunk" in names:
        sys.exit("Error: this looks like a PlayCanvas compressed PLY; not supported")

    _, count, props = elements[0]
    if any(dt is None for _, dt in props):
        sys.exit("Error: list properties in the vertex element are not supported")

    dtype = np.dtype([(n, "<" + dt) for n, dt in props])
    data = np.fromfile(args.input, dtype=dtype, count=count, offset=data_start)
    if len(data) != count:
        print(f"Warning: header says {count} vertices, file holds {len(data)}")
    total = len(data)

    # Find splats with any non-finite float value
    keep = np.ones(total, dtype=bool)
    bad_by_prop = {}
    for n, dt in props:
        if dt.startswith("f"):
            bad = ~np.isfinite(data[n])
            if bad.any():
                bad_by_prop[n] = int(bad.sum())
                keep &= ~bad

    removed = total - int(keep.sum())
    print(f"Vertices read:    {total}")
    print(f"Vertices removed: {removed}")
    for n, c in bad_by_prop.items():
        print(f"  non-finite '{n}': {c}")
    if removed:
        first = np.flatnonzero(~keep)[:10]
        print(f"  first bad indices: {first.tolist()}")

    data = data[keep]

    # Optionally strip non-standard properties
    if args.strip_extra:
        kept = [(n, dt) for n, dt in props if STANDARD.match(n)]
        dropped = [n for n, _ in props if not STANDARD.match(n)]
        if dropped:
            print(f"Stripped extra properties: {dropped}")
        out_dtype = np.dtype([(n, "<" + dt) for n, dt in kept])
        out = np.empty(len(data), dtype=out_dtype)
        for n, _ in kept:
            out[n] = data[n]
        data, props = out, kept

    # Write a minimal, clean header
    rev = {v: k for k, v in {"char": "i1", "uchar": "u1", "short": "i2",
                             "ushort": "u2", "int": "i4", "uint": "u4",
                             "float": "f4", "double": "f8"}.items()}
    header = ["ply", "format binary_little_endian 1.0",
              f"element vertex {len(data)}"]
    for n, dt in props:
        header.append(f"property {rev[dt]} {n}")
    header.append("end_header")

    with open(args.output, "wb") as f:
        f.write(("\n".join(header) + "\n").encode("ascii"))
        f.write(data.tobytes())

    print(f"Wrote {len(data)} vertices to {args.output}")


if __name__ == "__main__":
    main()