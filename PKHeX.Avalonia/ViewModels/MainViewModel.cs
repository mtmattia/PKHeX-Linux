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

    [RelayCommand]
    private void NextBox()
    {
        if (SelectedBoxIndex < BoxNames.Count - 1)
            SelectedBoxIndex++;
    }

    [RelayCommand]
    private void PrevBox()
    {
        if (SelectedBoxIndex > 0)
            SelectedBoxIndex--;
    }

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

    [ObservableProperty]
    public partial bool IsEmptySlotSelected { get; set; }

    partial void OnSelectedSlotChanged(SlotViewModel? value)
    {
        IsEmptySlotSelected = value is { IsEmpty: true };
        CreatePokemonCommand.NotifyCanExecuteChanged();
        if (_sav is not { } sav || value is null || value.IsEmpty)
        {
            Editor = null;
            return;
        }
        Editor = new PokemonEditorViewModel(sav, value.Entity, value.Box, value.Slot, value.IsParty, OnEditorApplied);
    }

    /// <summary>Removes the Pokémon in the selected slot (box: clear slot; party: delete + compact).</summary>
    public void RemoveSelectedPokemon()
    {
        if (_sav is not { } sav || SelectedSlot is not { IsEmpty: false } slot)
            return;

        if (slot.IsParty)
            sav.DeletePartySlot(slot.Slot);
        else
            sav.SetBoxSlotAtIndex(sav.BlankPKM, slot.Box, slot.Slot);

        int slotIndex = slot.Slot;
        LoadBox(SelectedBoxIndex);
        if (slotIndex >= 0 && slotIndex < CurrentBox.Count)
            SelectedSlot = CurrentBox[slotIndex];
        else
            SelectedSlot = null;
        StatusText = "Pokémon rimosso — premi 💾 Salva per scrivere sul file.";
    }

    /// <summary>Creates a fresh Pokémon in the selected empty slot, then opens the editor on it.</summary>
    [RelayCommand(CanExecute = nameof(IsEmptySlotSelected))]
    private void CreatePokemon()
    {
        if (_sav is not { } sav || SelectedSlot is not { IsEmpty: true } slot)
            return;

        var pk = sav.BlankPKM;
        pk.Species = 1; // sensible default; the user edits it right away
        pk.Version = sav.Version;
        if (sav.Language > 0)
            pk.Language = sav.Language;
        pk.OriginalTrainerName = sav.OT;
        pk.TID16 = sav.TID16;
        pk.SID16 = sav.SID16;
        pk.OriginalTrainerGender = sav.Gender;
        pk.Ball = 4; // Poké Ball
        pk.CurrentLevel = 5;
        pk.MetLevel = 5;
        pk.PID = EntityPID.GetRandomPID(Util.Rand, pk.Species, 0, sav.Version, Nature.Hardy, 0, Util.Rand.Rand32());
        pk.SetRandomIVs();
        pk.Heal();
        pk.RefreshChecksum();

        if (slot.IsParty)
            sav.SetPartySlotAtIndex(pk, slot.Slot);
        else
            sav.SetBoxSlotAtIndex(pk, slot.Box, slot.Slot);

        int slotIndex = slot.Slot;
        LoadBox(SelectedBoxIndex);
        if (slotIndex >= 0 && slotIndex < CurrentBox.Count)
            SelectedSlot = CurrentBox[slotIndex]; // reselect → opens the editor on the new Pokémon
        StatusText = "Nuovo Pokémon creato — modificalo e premi 💾 Salva.";
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
        if (_path is not null)
            WriteTo(_path, backupOriginal: true);
    }

    /// <summary>Saves to a new path chosen by the user ("Salva con nome…").</summary>
    public void SaveAs(string path)
    {
        WriteTo(path, backupOriginal: false);
        _path = path; // subsequent quick-saves target the new file
    }

    private void WriteTo(string path, bool backupOriginal)
    {
        if (_sav is not { } sav)
            return;
        try
        {
            // Safety net: preserve the pristine original as a one-time .bak, so a bad
            // edit (or a bug) can always be undone by restoring the backup.
            if (backupOriginal)
            {
                var bak = path + ".bak";
                if (File.Exists(path) && !File.Exists(bak))
                    File.Copy(path, bak);
            }

            var data = sav.Write();
            File.WriteAllBytes(path, data.ToArray());

            var bakPath = path + ".bak";
            StatusText = File.Exists(bakPath)
                ? $"Salvato: {Path.GetFileName(path)}  •  backup originale in {Path.GetFileName(bakPath)}"
                : $"Salvato: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Errore nel salvataggio: {ex.Message}";
        }
    }

    partial void OnHasSaveChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
