using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>
/// Represents a single storage slot (box or party) for display in the grid.
/// </summary>
public partial class SlotViewModel : ViewModelBase
{
    public PKM Entity { get; }
    public int Box { get; }
    public int Slot { get; }
    public bool IsParty { get; }

    public bool IsEmpty => Entity.Species == 0;

    public string SpeciesName => IsEmpty
        ? "—"
        : GameInfo.Strings.Species[Entity.Species];

    /// <summary>Gender symbol (♂/♀) or empty for genderless.</summary>
    public static string GenderSymbol(PKM pk) => pk.Gender switch { 0 => " ♂", 1 => " ♀", _ => "" };

    public string DisplayLine => IsEmpty
        ? "—"
        : $"{SpeciesName}{GenderSymbol(Entity)}{(Entity.IsShiny ? " ★" : "")}{(Entity.IsEgg ? " 🥚" : "")}";

    public string SubLine
    {
        get
        {
            if (IsEmpty)
                return "";
            var line = $"Lv {Entity.CurrentLevel}";
            if (IsParty && !Entity.IsEgg)
            {
                line += $"  ·  ♥ {Entity.Stat_HPCurrent}/{Entity.Stat_HPMax}";
                var st = StatusName(Entity.Status_Condition);
                if (st.Length != 0)
                    line += $"  ·  {st}";
            }
            return line;
        }
    }

    /// <summary>Full details shown on hover.</summary>
    public string Tooltip
    {
        get
        {
            if (IsEmpty)
                return "";
            var str = GameInfo.Strings;
            var item = Entity.HeldItem > 0
                ? str.GetItemStrings(Entity.Context, Entity.Version)[Entity.HeldItem]
                : "nessuno";
            var loc = str.GetLocationName(false, Entity.MetLocation, (byte)Entity.Format, (byte)Entity.Generation, Entity.Version);
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"{SpeciesName}{GenderSymbol(Entity)}{(Entity.IsShiny ? " ★ shiny" : "")}");
            lines.AppendLine($"Lv {Entity.CurrentLevel}");
            lines.AppendLine($"Strumento: {item}");
            lines.Append($"Incontrato: {loc}");
            if (Entity.MetLevel > 0) lines.Append($" (Lv {Entity.MetLevel})");
            if (Entity.MetDate is { } d) lines.Append($" · {d:yyyy-MM-dd}");
            return lines.ToString();
        }
    }

    public Bitmap? Sprite => SpriteLoader.GetSprite(Entity);

    public SlotViewModel(PKM entity, int box, int slot, bool isParty = false)
    {
        Entity = entity;
        Box = box;
        Slot = slot;
        IsParty = isParty;
    }

    /// <summary>Gen3 status condition byte → short label ("" when healthy).</summary>
    public static string StatusName(int s)
    {
        if (s == 0) return "";
        if ((s & 0x07) != 0) return "Dorme";
        if ((s & 0x08) != 0) return "Veleno";
        if ((s & 0x10) != 0) return "Scottato";
        if ((s & 0x20) != 0) return "Congelato";
        if ((s & 0x40) != 0) return "Paralisi";
        if ((s & 0x80) != 0) return "Iperveleno";
        return "";
    }
}
