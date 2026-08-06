using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>One selectable item in a pouch's dropdown (id + display name).</summary>
public sealed record ItemChoice(int Id, string Name);

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

        // Item names must be the save's OWN id space, not the global/modern list:
        // Gen1–3 use different item ids, so GameInfo.Strings.Item would show (and
        // let you pick) the wrong items. GetItemStrings maps by the save's context.
        IReadOnlyList<string> itemNames = GameInfo.Strings.GetItemStrings(sav.Context, sav.Version);

        foreach (var pouch in _bag.Pouches)
            Pouches.Add(new PouchViewModel(pouch, itemNames));
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
    /// <summary>Maximum number of distinct item slots this pouch can hold.</summary>
    public int Capacity { get; }

    public ObservableCollection<ItemSlotViewModel> Items { get; } = new();

    /// <summary>Items selectable in THIS pouch only (id 0 = none, then the pouch's legal items).</summary>
    public IReadOnlyList<ItemChoice> Choices { get; }

    /// <summary>"used / capacity" label, e.g. "12 / 20".</summary>
    public string CapacityLabel => $"{Items.Count} / {Capacity}";

    public PouchViewModel(InventoryPouch pouch, IReadOnlyList<string> itemNames)
    {
        _pouch = pouch;
        Name = pouch.Type.ToString();
        MaxCount = pouch.MaxCount;
        Capacity = pouch.Items.Length;

        // Build the per-pouch choice list: only the items that legally belong to
        // this pouch (plus anything already present, to never hide existing data).
        var seen = new HashSet<int> { 0 };
        var choices = new List<ItemChoice> { new(0, "— (nessuno) —") };
        foreach (var id in pouch.GetAllItems())
            if (seen.Add(id))
                choices.Add(new ItemChoice(id, ItemName(id)));
        foreach (var item in pouch.Items)
            if (item.Index > 0 && seen.Add(item.Index))
                choices.Add(new ItemChoice(item.Index, ItemName(item.Index)));
        Choices = choices;

        // Show only the occupied slots as editable rows; empties are added on demand.
        foreach (var item in pouch.Items)
            if (item.Count > 0)
                AddRow(item.Index, item.Count);

        string ItemName(int id) => (uint)id < (uint)itemNames.Count ? itemNames[id] : $"#{id}";
    }

    private void AddRow(int itemId, int count)
    {
        var vm = new ItemSlotViewModel(itemId, count, MaxCount, Choices, RemoveRow);
        Items.Add(vm);
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void AddItem()
    {
        AddRow(0, 1);
        OnItemsCountChanged();
    }

    private bool CanAdd => Items.Count < Capacity;

    private void RemoveRow(ItemSlotViewModel vm)
    {
        Items.Remove(vm);
        OnItemsCountChanged();
    }

    private void OnItemsCountChanged()
    {
        OnPropertyChanged(nameof(CapacityLabel));
        AddItemCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Packs the visible rows back into the fixed-size pouch array; the rest is cleared.</summary>
    public void WriteBack()
    {
        int w = 0;
        foreach (var vm in Items)
        {
            if (vm.ItemId <= 0 || w >= _pouch.Items.Length)
                continue;
            _pouch.Items[w].Index = vm.ItemId;
            _pouch.Items[w].Count = Math.Clamp(vm.Count, 1, MaxCount);
            w++;
        }
        for (; w < _pouch.Items.Length; w++)
        {
            _pouch.Items[w].Index = 0;
            _pouch.Items[w].Count = 0;
        }
    }
}

public partial class ItemSlotViewModel : ViewModelBase
{
    private readonly Action<ItemSlotViewModel> _remove;
    private int _itemId;

    /// <summary>
    /// The item id (bound via the ComboBox's SelectedValue → <see cref="ItemChoice.Id"/>).
    /// Guarded against negatives: a ComboBox that momentarily loses its selection would
    /// otherwise push -1 and silently wipe the item on the next WriteBack.
    /// </summary>
    public int ItemId
    {
        get => _itemId;
        set { if (value >= 0) SetProperty(ref _itemId, value); }
    }

    [ObservableProperty] public partial int Count { get; set; }
    public int MaxCount { get; }

    /// <summary>The pouch's item choices (shared); the ComboBox binds its ItemsSource here.</summary>
    public IReadOnlyList<ItemChoice> Choices { get; }

    public ItemSlotViewModel(int itemId, int count, int maxCount, IReadOnlyList<ItemChoice> choices, Action<ItemSlotViewModel> remove)
    {
        _itemId = itemId;
        Count = count;
        MaxCount = maxCount;
        Choices = choices;
        _remove = remove;
    }

    [RelayCommand]
    private void Remove() => _remove(this);
}
