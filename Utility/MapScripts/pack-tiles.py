#!/usr/bin/env python3
"""
Pack an XYZ tile pyramid into a single-file archive (.otil) for the Unity TileArchive reader.

Inputs (pick one):
  --mbtiles file.mbtiles     e.g. QGIS "Generate XYZ tiles (MBTiles)" output
  --dir folder               e.g. QGIS "Generate XYZ tiles (Directory)" output (z/x/y.png)
                             add --tms if that folder was written with the TMS y-convention

Examples:
  python pack_tiles.py --mbtiles qgis_tiles.mbtiles --out map.otil
  python pack_tiles.py --dir tiles/ --out map.otil
  python pack_tiles.py --info map.otil          # inspect an existing archive

Only PNG and JPG tiles are supported (Unity's Texture2D.LoadImage can't decode WebP).
Identical tiles (blank sea, empty land...) are stored once and shared.

File format (all integers little-endian):
  Header, 64 bytes
    0   char[4]  magic "OTIL"
    4   u32      version (1)
    8   u32      tile count
    12  u8       min zoom
    13  u8       max zoom
    14  u16      tile size in pixels (e.g. 256 or 512)
    16  u8       format: 0 = png, 1 = jpg
    17  7 bytes  reserved
    24  f64      min lon      32  f64  min lat
    40  f64      max lon      48  f64  max lat      (coverage of the highest zoom level)
    56  u64      offset where tile data begins
  Index, tile count * 20 bytes, sorted by key
    u64 key = (z << 56) | (x << 28) | y      (XYZ, y counted from the TOP)
    u64 absolute file offset of the tile bytes
    u32 length in bytes
  Tile data
"""
import argparse
import hashlib
import math
import os
import re
import shutil
import sqlite3
import struct
import sys
import tempfile

MAGIC = b"OTIL"
VERSION = 1
HEADER_SIZE = 64
ENTRY_SIZE = 20
FORMATS = {"png": 0, "jpg": 1, "jpeg": 1}


def key(z, x, y):
    return (z << 56) | (x << 28) | y


def tile_to_lonlat(x, y, z):
    """North-west corner of tile (x, y) at zoom z."""
    n = 2 ** z
    lon = x / n * 360.0 - 180.0
    lat = math.degrees(math.atan(math.sinh(math.pi * (1 - 2 * y / n))))
    return lon, lat


def sniff_format(data):
    if data[:8] == b"\x89PNG\r\n\x1a\n":
        return "png"
    if data[:3] == b"\xff\xd8\xff":
        return "jpg"
    return None


def png_size(data):
    if data[:8] == b"\x89PNG\r\n\x1a\n" and data[12:16] == b"IHDR":
        return struct.unpack(">II", data[16:24])
    return None


def iter_mbtiles(path):
    db = sqlite3.connect(path)
    # MBTiles rows count from the bottom (TMS); convert to XYZ (from the top).
    cur = db.execute("SELECT zoom_level, tile_column, tile_row, tile_data FROM tiles")
    for z, x, tms_y, data in cur:
        yield z, x, (2 ** z - 1) - tms_y, bytes(data)
    db.close()


def iter_dir(root, tms):
    pat = re.compile(r"^\d+$")
    for zd in sorted(os.listdir(root)):
        zp = os.path.join(root, zd)
        if not (pat.match(zd) and os.path.isdir(zp)):
            continue
        z = int(zd)
        for xd in os.listdir(zp):
            xp = os.path.join(zp, xd)
            if not (pat.match(xd) and os.path.isdir(xp)):
                continue
            x = int(xd)
            for fn in os.listdir(xp):
                m = re.match(r"^(\d+)\.(png|jpg|jpeg)$", fn, re.I)
                if not m:
                    continue
                y = int(m.group(1))
                if tms:
                    y = (2 ** z - 1) - y
                with open(os.path.join(xp, fn), "rb") as f:
                    yield z, x, y, f.read()


def pack(tiles, out_path, dedupe=True):
    entries = []                      # (key, rel_offset, length)
    seen = {}                         # sha1 -> (rel_offset, length)
    rel = 0
    fmt = None
    tile_size = None
    zmin, zmax = 99, -1
    top_extent = {}                   # z -> [xmin, ymin, xmax, ymax]

    tmp = tempfile.NamedTemporaryFile(delete=False, suffix=".otil.data")
    try:
        for z, x, y, data in tiles:
            f = sniff_format(data)
            if f is None:
                sys.exit(f"Tile {z}/{x}/{y} is neither PNG nor JPG - only those are supported.")
            if fmt is None:
                fmt = f
            elif f != fmt:
                sys.exit("Mixed PNG/JPG tiles are not supported - export with a single format.")
            if tile_size is None:
                s = png_size(data)
                if s:
                    tile_size = s[0]

            if dedupe:
                h = hashlib.sha1(data).digest()
                hit = seen.get(h)
                if hit is None:
                    tmp.write(data)
                    hit = (rel, len(data))
                    seen[h] = hit
                    rel += len(data)
                off, length = hit
            else:
                tmp.write(data)
                off, length = rel, len(data)
                rel += length

            entries.append((key(z, x, y), off, length))
            zmin, zmax = min(zmin, z), max(zmax, z)
            e = top_extent.setdefault(z, [x, y, x, y])
            e[0], e[1], e[2], e[3] = min(e[0], x), min(e[1], y), max(e[2], x), max(e[3], y)
        tmp.close()

        if not entries:
            sys.exit("No tiles found in the input.")

        entries.sort()
        if len({e[0] for e in entries}) != len(entries):
            sys.exit("Duplicate tile coordinates in the input.")

        # Coverage = tile envelope of the highest zoom level.
        x0, y0, x1, y1 = top_extent[zmax]
        min_lon, max_lat = tile_to_lonlat(x0, y0, zmax)
        max_lon, min_lat = tile_to_lonlat(x1 + 1, y1 + 1, zmax)

        data_start = HEADER_SIZE + len(entries) * ENTRY_SIZE
        header = struct.pack(
            "<4sIIBBHB7xddddQ",
            MAGIC, VERSION, len(entries), zmin, zmax, tile_size or 256, FORMATS[fmt],
            min_lon, min_lat, max_lon, max_lat, data_start,
        )
        assert len(header) == HEADER_SIZE, len(header)

        with open(out_path, "wb") as out:
            out.write(header)
            for k, off, length in entries:
                out.write(struct.pack("<QQI", k, data_start + off, length))
            with open(tmp.name, "rb") as d:
                shutil.copyfileobj(d, out, 1024 * 1024)
    finally:
        try:
            os.unlink(tmp.name)
        except OSError:
            pass

    size = os.path.getsize(out_path)
    print(f"Wrote {out_path}: {len(entries)} tiles ({len(seen) if dedupe else len(entries)} unique), "
          f"zoom {zmin}-{zmax}, {tile_size or 256}px {fmt}, {size / 1048576:.1f} MB")
    print(f"Coverage: lon {min_lon:.5f}..{max_lon:.5f}  lat {min_lat:.5f}..{max_lat:.5f}")


def info(path):
    with open(path, "rb") as f:
        h = f.read(HEADER_SIZE)
        magic, ver, count, zmin, zmax, tsize, fmt, minlon, minlat, maxlon, maxlat, dstart = \
            struct.unpack("<4sIIBBHB7xddddQ", h)
        if magic != MAGIC:
            sys.exit("Not an OTIL archive.")
        print(f"version {ver}, {count} tiles, zoom {zmin}-{zmax}, {tsize}px, "
              f"{'png' if fmt == 0 else 'jpg'}, {os.path.getsize(path) / 1048576:.1f} MB")
        print(f"bounds lon {minlon:.5f}..{maxlon:.5f}  lat {minlat:.5f}..{maxlat:.5f}")
        per_zoom = {}
        for _ in range(count):
            k, off, ln = struct.unpack("<QQI", f.read(ENTRY_SIZE))
            per_zoom[k >> 56] = per_zoom.get(k >> 56, 0) + 1
        for z in sorted(per_zoom):
            print(f"  zoom {z:>2}: {per_zoom[z]} tiles")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    src = ap.add_mutually_exclusive_group(required=True)
    src.add_argument("--mbtiles")
    src.add_argument("--dir")
    src.add_argument("--info", metavar="FILE.otil")
    ap.add_argument("--out", default="map.otil")
    ap.add_argument("--tms", action="store_true", help="--dir uses TMS y numbering (origin bottom-left)")
    ap.add_argument("--no-dedupe", action="store_true")
    a = ap.parse_args()

    if a.info:
        return info(a.info)
    tiles = iter_mbtiles(a.mbtiles) if a.mbtiles else iter_dir(a.dir, a.tms)
    pack(tiles, a.out, dedupe=not a.no_dedupe)


if __name__ == "__main__":
    main()