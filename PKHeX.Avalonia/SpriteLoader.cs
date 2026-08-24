using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PKHeX.Core;

namespace PKHeX.Avalonia;

/// <summary>Pokédex display state driving how a species sprite is tinted.</summary>
public enum DexView { NotSeen = 0, Seen = 1, Caught = 2 }

/// <summary>
/// Loads Pokémon box sprites from the bundled "Big Pokemon Sprites" set
/// (files named b_{species}[-{form}].png) using Avalonia's Skia-backed
/// <see cref="Bitmap"/> — no System.Drawing involved, so it runs natively on Linux.
/// </summary>
public static class SpriteLoader
{
    private const string Root = "avares://PKHeX.Avalonia/Assets/sprites/";
    private static readonly Dictionary<string, Bitmap?> Cache = new();

    /// <summary>Returns the box sprite for the given entity, or null if it is an empty slot.</summary>
    public static Bitmap? GetSprite(PKM pk)
    {
        if (pk.Species == 0)
            return null;

        if (pk.IsEgg)
            return Load("b_0"); // generic egg fallback

        // Prefer a form-specific file, fall back to the base species sprite.
        var withForm = $"b_{pk.Species}-{pk.Form}";
        return Load(withForm) ?? Load($"b_{pk.Species}") ?? Load("b_0");
    }

    /// <summary>
    /// Species sprite for the Pokédex, tinted by state: Caught = full colour,
    /// Seen = grayscale, NotSeen = a medium-gray silhouette.
    /// </summary>
    public static Bitmap? GetDexSprite(ushort species, DexView view)
    {
        var key = $"dex_{species}_{(int)view}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var baseBmp = Load($"b_{species}") ?? Load("b_0");
        Bitmap? result = baseBmp;
        if (baseBmp is not null && view != DexView.Caught)
        {
            try { result = Tint(baseBmp, view == DexView.NotSeen); }
            catch { result = baseBmp; } // never let sprite tinting crash the grid
        }

        Cache[key] = result;
        return result;
    }

    /// <summary>Produces a grayscale (or, if <paramref name="silhouette"/>, a flat medium-gray) copy, keeping alpha.</summary>
    private static Bitmap Tint(Bitmap src, bool silhouette)
    {
        var ps = src.PixelSize;
        int w = ps.Width, h = ps.Height, stride = w * 4;
        var buf = new byte[stride * h];
        var gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
        try { src.CopyPixels(new PixelRect(0, 0, w, h), gch.AddrOfPinnedObject(), buf.Length, stride); }
        finally { gch.Free(); }

        for (int i = 0; i < buf.Length; i += 4)
        {
            byte b = buf[i], g = buf[i + 1], r = buf[i + 2], a = buf[i + 3];
            if (a == 0) continue;
            byte v = silhouette
                ? (byte)(0x88 * a / 255)                       // flat medium gray (premultiplied)
                : (byte)((r * 77 + g * 150 + b * 29) >> 8);    // luminance
            buf[i] = v; buf[i + 1] = v; buf[i + 2] = v;
        }

        var wb = new WriteableBitmap(ps, src.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = wb.Lock())
        {
            if (fb.RowBytes == stride)
                Marshal.Copy(buf, 0, fb.Address, buf.Length);
            else
                for (int y = 0; y < h; y++)
                    Marshal.Copy(buf, y * stride, fb.Address + (y * fb.RowBytes), stride);
        }
        return wb;
    }

    private static Bitmap? Load(string name)
    {
        if (Cache.TryGetValue(name, out var cached))
            return cached;

        Bitmap? bmp = null;
        try
        {
            var uri = new Uri(Root + name + ".png");
            if (AssetLoader.Exists(uri))
            {
                using var stream = AssetLoader.Open(uri);
                bmp = new Bitmap(stream);
            }
        }
        catch
        {
            bmp = null;
        }

        Cache[name] = bmp;
        return bmp;
    }
}
