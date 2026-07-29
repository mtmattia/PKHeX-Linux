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

    public string DisplayLine => IsEmpty
        ? "—"
        : $"{SpeciesName}{(Entity.IsShiny ? " ★" : "")}";

    public string SubLine => IsEmpty
        ? ""
        : $"Lv {Entity.CurrentLevel}{(Entity.IsEgg ? " (Uovo)" : "")}";

    public Bitmap? Sprite => SpriteLoader.GetSprite(Entity);

    public SlotViewModel(PKM entity, int box, int slot, bool isParty = false)
    {
        Entity = entity;
        Box = box;
        Slot = slot;
        IsParty = isParty;
    }
}
