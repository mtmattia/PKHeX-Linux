using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>
/// Editable view over a single <see cref="PKM"/>. Exposes the common fields as
/// observable properties and writes them back into the save on Apply.
/// </summary>
public partial class PokemonEditorViewModel : ViewModelBase
{
    private readonly SaveFile _sav;
    private readonly PKM _pk;
    private readonly int _box;
    private readonly int _slot;
    private readonly bool _isParty;
    private readonly Action _onApplied;

    // Choice lists for the combo boxes (index == in-game id).
    public IReadOnlyList<string> SpeciesNames { get; } = GameInfo.Strings.Species;
    public IReadOnlyList<string> MoveNames { get; } = GameInfo.Strings.Move;
    public IReadOnlyList<string> NatureNames { get; } = GameInfo.Strings.Natures;

    [ObservableProperty] public partial int Species { get; set; }
    [ObservableProperty] public partial int Level { get; set; }
    [ObservableProperty] public partial int NatureIndex { get; set; }
    [ObservableProperty] public partial int Move1 { get; set; }
    [ObservableProperty] public partial int Move2 { get; set; }
    [ObservableProperty] public partial int Move3 { get; set; }
    [ObservableProperty] public partial int Move4 { get; set; }
    [ObservableProperty] public partial int IvHp { get; set; }
    [ObservableProperty] public partial int IvAtk { get; set; }
    [ObservableProperty] public partial int IvDef { get; set; }
    [ObservableProperty] public partial int IvSpa { get; set; }
    [ObservableProperty] public partial int IvSpd { get; set; }
    [ObservableProperty] public partial int IvSpe { get; set; }
    [ObservableProperty] public partial int EvHp { get; set; }
    [ObservableProperty] public partial int EvAtk { get; set; }
    [ObservableProperty] public partial int EvDef { get; set; }
    [ObservableProperty] public partial int EvSpa { get; set; }
    [ObservableProperty] public partial int EvSpd { get; set; }
    [ObservableProperty] public partial int EvSpe { get; set; }

    [ObservableProperty] public partial bool IsShiny { get; set; }

    // Form selection (Unown, Castform, Deoxys, …). Only shown when >1 form exists.
    public bool HasForms { get; }
    public IReadOnlyList<string> FormNames { get; } = [];
    [ObservableProperty] public partial int FormIndex { get; set; }

    // Read-only info surfaced in the editor.
    public string GenderSymbol { get; }
    public string HeldItemName { get; }
    public string MetLocationName { get; }
    public int MetLevel { get; }
    public string MetDateText { get; }
    public bool HasMetDate { get; }

    // Contest condition (bellezza, acume, ...). Only when the format stores them.
    public bool HasContest { get; }
    [ObservableProperty] public partial int ConCool { get; set; }
    [ObservableProperty] public partial int ConBeauty { get; set; }
    [ObservableProperty] public partial int ConCute { get; set; }
    [ObservableProperty] public partial int ConSmart { get; set; }
    [ObservableProperty] public partial int ConTough { get; set; }
    [ObservableProperty] public partial int ConSheen { get; set; }

    /// <summary>Ribbons (fiocchi) applicable to this entity.</summary>
    public ObservableCollection<RibbonEntryViewModel> Ribbons { get; } = new();

    /// <summary>Showdown set text used by the import/export controls.</summary>
    [ObservableProperty] public partial string ShowdownText { get; set; } = "";

    public string Header => _isParty
        ? $"Squadra · Slot {_slot + 1}"
        : $"Box {_box + 1} · Slot {_slot + 1}";

    public PokemonEditorViewModel(SaveFile sav, PKM pk, int box, int slot, bool isParty, Action onApplied)
    {
        _sav = sav;
        _pk = pk;
        _box = box;
        _slot = slot;
        _isParty = isParty;
        _onApplied = onApplied;

        Species = pk.Species;
        Level = pk.CurrentLevel;
        NatureIndex = (int)pk.Nature;
        Move1 = pk.Move1;
        Move2 = pk.Move2;
        Move3 = pk.Move3;
        Move4 = pk.Move4;
        IvHp = pk.IV_HP; IvAtk = pk.IV_ATK; IvDef = pk.IV_DEF;
        IvSpa = pk.IV_SPA; IvSpd = pk.IV_SPD; IvSpe = pk.IV_SPE;
        EvHp = pk.EV_HP; EvAtk = pk.EV_ATK; EvDef = pk.EV_DEF;
        EvSpa = pk.EV_SPA; EvSpd = pk.EV_SPD; EvSpe = pk.EV_SPE;
        IsShiny = pk.IsShiny;

        var str = GameInfo.Strings;

        var formList = FormConverter.GetFormList(pk.Species, str.types, str.forms, pk.Context);
        HasForms = formList.Length > 1;
        FormNames = formList;
        FormIndex = pk.Form < formList.Length ? pk.Form : 0;

        GenderSymbol = pk.Gender switch { 0 => "♂ Maschio", 1 => "♀ Femmina", _ => "⚲ Senza genere" };
        HeldItemName = pk.HeldItem > 0
            ? str.GetItemStrings(pk.Context, pk.Version)[pk.HeldItem]
            : "nessuno";
        MetLocationName = str.GetLocationName(false, pk.MetLocation, (byte)pk.Format, (byte)pk.Generation, pk.Version);
        MetLevel = pk.MetLevel;
        MetDateText = pk.MetDate?.ToString("yyyy-MM-dd") ?? "—";
        HasMetDate = pk.MetDate is not null;

        if (pk is IContestStats cs)
        {
            HasContest = true;
            ConCool = cs.ContestCool; ConBeauty = cs.ContestBeauty; ConCute = cs.ContestCute;
            ConSmart = cs.ContestSmart; ConTough = cs.ContestTough; ConSheen = cs.ContestSheen;
        }

        foreach (var info in RibbonInfo.GetRibbonInfo(pk))
            Ribbons.Add(new RibbonEntryViewModel(info));
    }

    [RelayCommand]
    private void Apply()
    {
        // Species must be set before Level: CurrentLevel uses the species' growth rate.
        _pk.Species = (ushort)Species;
        _pk.CurrentLevel = (byte)Math.Clamp(Level, 1, 100);
        // Use the helper: for PID-based generations (Gen 3/4) this rerolls the PID
        // so the nature actually changes, instead of a silent no-op.
        _pk.SetNature((Nature)NatureIndex);
        if (HasForms)
            _pk.Form = (byte)Math.Clamp(FormIndex, 0, FormNames.Count - 1);
        _pk.Move1 = (ushort)Move1;
        _pk.Move2 = (ushort)Move2;
        _pk.Move3 = (ushort)Move3;
        _pk.Move4 = (ushort)Move4;
        _pk.IV_HP = Clamp(IvHp, 31); _pk.IV_ATK = Clamp(IvAtk, 31); _pk.IV_DEF = Clamp(IvDef, 31);
        _pk.IV_SPA = Clamp(IvSpa, 31); _pk.IV_SPD = Clamp(IvSpd, 31); _pk.IV_SPE = Clamp(IvSpe, 31);
        _pk.EV_HP = Clamp(EvHp, 255); _pk.EV_ATK = Clamp(EvAtk, 255); _pk.EV_DEF = Clamp(EvDef, 255);
        _pk.EV_SPA = Clamp(EvSpa, 255); _pk.EV_SPD = Clamp(EvSpd, 255); _pk.EV_SPE = Clamp(EvSpe, 255);

        if (_pk is IContestStats cs)
        {
            cs.ContestCool = (byte)Clamp(ConCool, 255); cs.ContestBeauty = (byte)Clamp(ConBeauty, 255);
            cs.ContestCute = (byte)Clamp(ConCute, 255); cs.ContestSmart = (byte)Clamp(ConSmart, 255);
            cs.ContestTough = (byte)Clamp(ConTough, 255); cs.ContestSheen = (byte)Clamp(ConSheen, 255);
        }

        foreach (var r in Ribbons)
            r.ApplyTo(_pk);

        Persist();
        _onApplied();
    }

    /// <summary>Parses the Showdown set text and applies it to this entity.</summary>
    [RelayCommand]
    private void ImportShowdown()
    {
        if (string.IsNullOrWhiteSpace(ShowdownText))
            return;

        var set = new ShowdownSet(ShowdownText);
        if (set.Species <= 0)
            return; // unparseable

        _pk.ApplySetDetails(set);
        Persist();
        _onApplied(); // reloads + reselects, rebuilding this editor with the new data
    }

    /// <summary>Exports this entity to Showdown text into <see cref="ShowdownText"/>.</summary>
    [RelayCommand]
    private void ExportShowdown()
    {
        ShowdownText = new ShowdownSet(_pk).Text;
    }

    private void Persist()
    {
        _pk.RefreshChecksum();
        if (_isParty)
            _sav.SetPartySlotAtIndex(_pk, _slot);
        else
            _sav.SetBoxSlotAtIndex(_pk, _box, _slot);
    }

    private static int Clamp(int v, int max) => Math.Clamp(v, 0, max);
}
