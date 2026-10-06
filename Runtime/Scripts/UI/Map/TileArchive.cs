using System;
using System.IO;

public enum TileFormat : byte { Png = 0, Jpg = 1 }

/// <summary>
/// Reader for the single-file tile archive written by pack_tiles.py (.otil).
/// Pure C# - no Unity dependencies. The whole index is loaded into memory (20 bytes per tile
/// on disk, 16 in RAM), tile bytes are read on demand with a seek.
///
/// Tiles are addressed in standard XYZ / slippy-map form: zoom z, column x, row y counted
/// from the TOP (north). TryGetTile is thread-safe.
/// </summary>
public sealed class TileArchive : IDisposable
{
    const int HeaderSize = 64;
    const int IndexEntrySize = 20;

    public int MinZoom { get; private set; }
    public int MaxZoom { get; private set; }
    public int TileSize { get; private set; }
    public int TileCount { get; private set; }
    public TileFormat Format { get; private set; }

    /// <summary>Geographic coverage of the highest zoom level (degrees, WGS84).</summary>
    public double MinLon { get; private set; }
    public double MinLat { get; private set; }
    public double MaxLon { get; private set; }
    public double MaxLat { get; private set; }

    FileStream file;
    readonly object gate = new object();
    ulong[] keys;
    long[] offsets;
    int[] lengths;

    public static ulong Key(int z, int x, int y)
    {
        return ((ulong)z << 56) | ((ulong)x << 28) | (uint)y;
    }

    public static TileArchive Open(string path)
    {
        var a = new TileArchive();
        a.Load(path);
        return a;
    }

    void Load(string path)
    {
        file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
        try
        {
            byte[] h = ReadExactly(0, HeaderSize);
            if (h[0] != (byte)'O' || h[1] != (byte)'T' || h[2] != (byte)'I' || h[3] != (byte)'L')
                throw new InvalidDataException("Not an OTIL tile archive: " + path);

            uint version = BitConverter.ToUInt32(h, 4);
            if (version != 1)
                throw new InvalidDataException("Unsupported OTIL version " + version);

            TileCount = (int)BitConverter.ToUInt32(h, 8);
            MinZoom = h[12];
            MaxZoom = h[13];
            TileSize = BitConverter.ToUInt16(h, 14);
            Format = (TileFormat)h[16];
            MinLon = BitConverter.ToDouble(h, 24);
            MinLat = BitConverter.ToDouble(h, 32);
            MaxLon = BitConverter.ToDouble(h, 40);
            MaxLat = BitConverter.ToDouble(h, 48);

            byte[] idx = ReadExactly(HeaderSize, (long)TileCount * IndexEntrySize);
            keys = new ulong[TileCount];
            offsets = new long[TileCount];
            lengths = new int[TileCount];
            for (int i = 0; i < TileCount; i++)
            {
                int p = i * IndexEntrySize;
                keys[i] = BitConverter.ToUInt64(idx, p);
                offsets[i] = (long)BitConverter.ToUInt64(idx, p + 8);
                lengths[i] = (int)BitConverter.ToUInt32(idx, p + 16);
            }
        }
        catch
        {
            file.Dispose();
            file = null;
            throw;
        }
    }

    byte[] ReadExactly(long offset, long count)
    {
        if (count > int.MaxValue) throw new InvalidDataException("Block too large");
        var buf = new byte[count];
        file.Seek(offset, SeekOrigin.Begin);
        int read = 0;
        while (read < count)
        {
            int n = file.Read(buf, read, (int)count - read);
            if (n <= 0) throw new EndOfStreamException("Archive is truncated");
            read += n;
        }
        return buf;
    }

    public bool HasTile(int z, int x, int y)
    {
        return Array.BinarySearch(keys, Key(z, x, y)) >= 0;
    }

    /// <summary>Returns the raw PNG/JPG bytes of a tile, or false if the archive doesn't contain it.</summary>
    public bool TryGetTile(int z, int x, int y, out byte[] data)
    {
        data = null;
        if (file == null || z < 0 || x < 0 || y < 0) return false;

        int i = Array.BinarySearch(keys, Key(z, x, y));
        if (i < 0) return false;

        lock (gate)
        {
            if (file == null) return false;
            data = ReadExactly(offsets[i], lengths[i]);
        }
        return true;
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (file != null) { file.Dispose(); file = null; }
        }
    }
}
