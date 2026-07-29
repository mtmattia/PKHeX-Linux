using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private SaveFile? _sav;
    private string? _path;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Nessun salvataggio caricato. Apri un file per iniziare.";

    [ObservableProperty]
    public partial string SaveInfo { get; set; } = "";

    [ObservableProperty]
    public partial bool HasSave { get; set; }

    /// <summary>Names of the boxes, e.g. "Box 1 (30)".</summary>
    public ObservableCollection<string> BoxNames { get; } = new();

    /// <summary>Slots of the currently selected box.</summary>
    public ObservableCollection<SlotViewModel> CurrentBox { get; } = new();

    [ObservableProperty]
    public partial int SelectedBoxIndex { get; set; }

    partial void OnSelectedBoxIndexChanged(int value) => LoadBox(value);

    [ObservableProperty]
    public partial SlotViewModel? SelectedSlot { get; set; }

    [ObservableProperty]
    public partial PokemonEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial TrainerViewModel? Trainer { get; set; }

    [ObservableProperty]
    public partial BagViewModel? Bag { get; set; }

    [ObservableProperty]
    public partial DexViewModel? Dex { get; set; }

    private void OnSubEditorApplied() =>
        StatusText = "Modifiche applicate (ricordati di premere 💾 Salva per scrivere sul file).";

    partial void OnSelectedSlotChanged(SlotViewModel? value)
    {
        if (_sav is not { } sav || value is null || value.IsEmpty)
        {
            Editor = null;
            return;
        }
        Editor = new PokemonEditorViewModel(sav, value.Entity, value.Box, value.Slot, value.IsParty, OnEditorApplied);
    }

    private void OnEditorApplied()
    {
        int slotIndex = SelectedSlot?.Slot ?? -1;
        LoadBox(SelectedBoxIndex);
        // Re-select the same slot so the editor rebinds to the freshly-decoded data.
        if (slotIndex >= 0 && slotIndex < CurrentBox.Count)
            SelectedSlot = CurrentBox[slotIndex];
        StatusText = "Modifiche applicate (ricordati di premere 💾 Salva per scrivere sul file).";
    }

    public void LoadSaveFromPath(string path)
    {
        try
        {
            var sav = SaveUtil.GetSaveFile(path);
            if (sav is null)
            {
                StatusText = $"Formato non riconosciuto: {Path.GetFileName(path)}";
                return;
            }

            _sav = sav;
            _path = path;
            HasSave = true;

            SaveInfo = $"{sav.Version}  •  OT: {sav.OT}  •  {sav.BoxCount} box × {sav.BoxSlotCount} slot  •  Gen {sav.Generation}";
            StatusText = $"Caricato: {Path.GetFileName(path)}";

            BoxNames.Clear();
            BoxNames.Add("★ Squadra");
            for (int b = 0; b < sav.BoxCount; b++)
                BoxNames.Add($"Box {b + 1}");

            Trainer = new TrainerViewModel(sav, OnSubEditorApplied);
            Bag = new BagViewModel(sav, OnSubEditorApplied);
            Dex = new DexViewModel(sav, OnSubEditorApplied);

            SelectedBoxIndex = 1; // start on Box 1
            LoadBox(1);
        }
        catch (Exception ex)
        {
            StatusText = $"Errore nel caricamento: {ex.Message}";
        }
    }

    // index 0 = party ("★ Squadra"); index 1..N maps to box 0..N-1.
    private void LoadBox(int index)
    {
        CurrentBox.Clear();
        if (_sav is not { } sav)
            return;

        if (index == 0)
        {
            var party = sav.PartyData;
            for (int slot = 0; slot < party.Count; slot++)
                CurrentBox.Add(new SlotViewModel(party[slot], -1, slot, isParty: true));
            return;
        }

        int box = index - 1;
        if (box < 0 || box >= sav.BoxCount)
            return;

        PKM[] data = sav.GetBoxData(box);
        for (int slot = 0; slot < data.Length; slot++)
            CurrentBox.Add(new SlotViewModel(data[slot], box, slot));
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private void Save()
    {
        if (_sav is not { } sav || _path is null)
            return;
        try
        {
            var data = sav.Write();
            File.WriteAllBytes(_path, data.ToArray());
            StatusText = $"Salvato: {Path.GetFileName(_path)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Errore nel salvataggio: {ex.Message}";
        }
    }

    partial void OnHasSaveChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
