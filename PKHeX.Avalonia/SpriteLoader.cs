using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PKHeX.Core;

namespace PKHeX.Avalonia;

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
