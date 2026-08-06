using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Platform;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>A single known/named event flag: its number and human-readable label.</summary>
public sealed record FlagEntry(int Number, string Label);

/// <summary>
/// Loads human-readable names for event flags from the bundled Gen3 flag lists
/// (research data from PKHeX). Names make the raw 0..N flag space understandable.
/// </summary>
public static class EventFlagCatalog
{
    private const string Root = "avares://PKHeX.Avalonia/Assets/flags/";

    /// <summary>Returns named flags for the save's game, or an empty list if none are bundled.</summary>
    public static IReadOnlyList<FlagEntry> Load(SaveFile sav)
    {
        var file = sav.Version switch
        {
            GameVersion.R or GameVersion.S or GameVersion.RS => "flags_rs_en.txt",
            GameVersion.E => "flags_e_en.txt",
            GameVersion.FR or GameVersion.LG or GameVersion.FRLG => "flags_frlg_en.txt",
            _ => null,
        };
        if (file is null)
            return [];

        try
        {
            var uri = new Uri(Root + file);
            if (!AssetLoader.Exists(uri))
                return [];
            using var stream = AssetLoader.Open(uri);
            return Parse(stream);
        }
        catch
        {
            return [];
        }
    }

    private static List<FlagEntry> Parse(Stream stream)
    {
        var list = new List<FlagEntry>();
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0)
                continue;
            // Format: <flag><tab><category><tab><description>
            var parts = line.Split('\t');
            if (parts.Length < 3)
                continue;

            var num = ParseNumber(parts[0]);
            if (num < 0)
                continue;

            var label = parts[2].Trim();
            if (label.Length != 0)
                list.Add(new FlagEntry(num, label));
        }
        return list;
    }

    private static int ParseNumber(string s)
    {
        s = s.Trim();
        try
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return int.Parse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return int.Parse(s, CultureInfo.InvariantCulture);
        }
        catch
        {
            return -1;
        }
    }
}
