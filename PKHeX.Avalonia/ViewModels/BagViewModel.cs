using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>Editor for the player's bag (inventory pouches).</summary>
public partial class BagViewModel : ViewModelBase
{
    private readonly SaveFile _sav;
    private readonly PlayerBag _bag;
    private readonly Action _onApplied;

    public ObservableCollection<PouchViewModel> Pouches { get; } = new();

    public BagViewModel(SaveFile sav, Action onApplied)
    {
        _sav = sav;
        _onApplied = onApplied;
        _bag = sav.Inventory;

        foreach (var pouch in _bag.Pouches)
            Pouches.Add(new PouchViewModel(pouch));
    }

    [RelayCommand]
    private void Apply()
    {
        foreach (var p in Pouches)
            p.WriteBack();
        _bag.CopyTo(_sav);
        _onApplied();
    }
}

public partial class PouchViewModel : ViewModelBase
{
    private readonly InventoryPouch _pouch;

    public string Name { get; }
    public int MaxCount { get; }
    public ObservableCollection<ItemSlotViewModel> Items { get; } = new();

    /// <summary>All item names, index == item id (shared for the combo boxes).</summary>
    public IReadOnlyList<string> ItemNames { get; } = GameInfo.Strings.Item;

    public PouchViewModel(InventoryPouch pouch)
    {
        _pouch = pouch;
        Name = pouch.Type.ToString();
        MaxCount = pouch.MaxCount;
        foreach (var item in pouch.Items)
            Items.Add(new ItemSlotViewModel(item.Index, item.Count, MaxCount));
    }

    public void WriteBack()
    {
        for (int i = 0; i < _pouch.Items.Length && i < Items.Count; i++)
        {
            var vm = Items[i];
            var empty = vm.ItemId <= 0;
            _pouch.Items[i].Index = empty ? 0 : vm.ItemId;
            _pouch.Items[i].Count = empty ? 0 : Math.Clamp(vm.Count, 1, MaxCount);
        }
    }
}

public partial class ItemSlotViewModel : ViewModelBase
{
    [ObservableProperty] public partial int ItemId { get; set; }
    [ObservableProperty] public partial int Count { get; set; }
    public int MaxCount { get; }

    public ItemSlotViewModel(int itemId, int count, int maxCount)
    {
        ItemId = itemId;
        Count = count;
        MaxCount = maxCount;
    }
}
