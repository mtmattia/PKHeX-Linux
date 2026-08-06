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
    public IReadOnlyList<string> ItemNames { get; }
    public IReadOnlyList<string> LocationNames { get; }
    public string[] GenderNames { get; } = ["♂ Maschio", "♀ Femmina"];
    public string[] StatusNames { get; } = ["OK", "Sonno", "Veleno", "Scottatura", "Congelamento", "Paralisi", "Iperveleno"];

    [ObservableProperty] public partial int Species { get; set; }
    [ObservableProperty] public partial int Level { get; set; }
    [ObservableProperty] public partial int NatureIndex { get; set; }
    [ObservableProperty] public partial int GenderIndex { get; set; }
    public bool CanEditGender { get; }
    [ObservableProperty] public partial int HeldItemIndex { get; set; }

    public ObservableCollection<MoveSlotViewModel> Moves { get; } = new();

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

    // Party-only live stats.
    public bool IsParty => _isParty;
    [ObservableProperty] public partial int HpCurrent { get; set; }
    [ObservableProperty] public partial int HpMax { get; set; }
    [ObservableProperty] public partial int StatusIndex { get; set; }

    // Met info (editable).
    [ObservableProperty] public partial int MetLocationIndex { get; set; }
    [ObservableProperty] public partial int MetLevelValue { get; set; }
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

        var str = GameInfo.Strings;
        ItemNames = str.GetItemStrings(pk.Context, pk.Version);
        LocationNames = MaterializeLocations(str, pk);

        Species = pk.Species;
        Level = pk.CurrentLevel;
        NatureIndex = (int)pk.Nature;

        CanEditGender = !PersonalInfo.IsSingleGender(pk.PersonalInfo.Gender);
        GenderIndex = pk.Gender == 1 ? 1 : 0;

        HeldItemIndex = pk.HeldItem;

        Moves.Add(new MoveSlotViewModel(MoveNames, pk.Move1, pk.Move1_PP, pk.Move1_PPUps, pk.GetMovePP));
        Moves.Add(new MoveSlotViewModel(MoveNames, pk.Move2, pk.Move2_PP, pk.Move2_PPUps, pk.GetMovePP));
        Moves.Add(new MoveSlotViewModel(MoveNames, pk.Move3, pk.Move3_PP, pk.Move3_PPUps, pk.GetMovePP));
        Moves.Add(new MoveSlotViewModel(MoveNames, pk.Move4, pk.Move4_PP, pk.Move4_PPUps, pk.GetMovePP));

        IvHp = pk.IV_HP; IvAtk = pk.IV_ATK; IvDef = pk.IV_DEF;
        IvSpa = pk.IV_SPA; IvSpd = pk.IV_SPD; IvSpe = pk.IV_SPE;
        EvHp = pk.EV_HP; EvAtk = pk.EV_ATK; EvDef = pk.EV_DEF;
        EvSpa = pk.EV_SPA; EvSpd = pk.EV_SPD; EvSpe = pk.EV_SPE;
        IsShiny = pk.IsShiny;

        var formList = FormConverter.GetFormList(pk.Species, str.types, str.forms, pk.Context);
        HasForms = formList.Length > 1;
        FormNames = formList;
        FormIndex = pk.Form < formList.Length ? pk.Form : 0;

        HpCurrent = pk.Stat_HPCurrent;
        HpMax = pk.Stat_HPMax;
        StatusIndex = StatusToIndex(pk.Status_Condition);

        MetLocationIndex = pk.MetLocation < LocationNames.Count ? pk.MetLocation : 0;
        MetLevelValue = pk.MetLevel;
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

        // Nature + gender: in Gen3–5 both derive from the PID, so reroll a single PID
        // matching both (keeping the ability bit); Gen6+ store them independently.
        var nature = (Nature)NatureIndex;
        byte gender = CanEditGender ? (byte)GenderIndex : _pk.Gender;
        if (_pk.Format is 3 or 4 or 5)
        {
            _pk.PID = EntityPID.GetRandomPID(Util.Rand, _pk.Species, gender, _pk.Version, nature, _pk.Form, _pk.PID);
        }
        else
        {
            _pk.Nature = nature;
            _pk.Gender = gender;
        }

        _pk.CurrentLevel = (byte)Math.Clamp(Level, 1, 100);
        if (HasForms)
            _pk.Form = (byte)Math.Clamp(FormIndex, 0, FormNames.Count - 1);

        _pk.HeldItem = Math.Max(0, HeldItemIndex);

        SetMove(0); SetMove(1); SetMove(2); SetMove(3);

        _pk.IV_HP = Clamp(IvHp, 31); _pk.IV_ATK = Clamp(IvAtk, 31); _pk.IV_DEF = Clamp(IvDef, 31);
        _pk.IV_SPA = Clamp(IvSpa, 31); _pk.IV_SPD = Clamp(IvSpd, 31); _pk.IV_SPE = Clamp(IvSpe, 31);
        _pk.EV_HP = Clamp(EvHp, 255); _pk.EV_ATK = Clamp(EvAtk, 255); _pk.EV_DEF = Clamp(EvDef, 255);
        _pk.EV_SPA = Clamp(EvSpa, 255); _pk.EV_SPD = Clamp(EvSpd, 255); _pk.EV_SPE = Clamp(EvSpe, 255);

        _pk.MetLocation = (ushort)Math.Max(0, MetLocationIndex);
        _pk.MetLevel = (byte)Math.Clamp(MetLevelValue, 0, 100);

        if (_pk is IContestStats cs)
        {
            cs.ContestCool = (byte)Clamp(ConCool, 255); cs.ContestBeauty = (byte)Clamp(ConBeauty, 255);
            cs.ContestCute = (byte)Clamp(ConCute, 255); cs.ContestSmart = (byte)Clamp(ConSmart, 255);
            cs.ContestTough = (byte)Clamp(ConTough, 255); cs.ContestSheen = (byte)Clamp(ConSheen, 255);
        }

        foreach (var r in Ribbons)
            r.ApplyTo(_pk);

        // Party-only: recompute stored stats (so HP max reflects the new IVs/level),
        // then apply the requested current HP and status condition.
        if (_isParty)
        {
            _pk.ResetPartyStats();
            _pk.Status_Condition = IndexToStatus(StatusIndex);
            _pk.Stat_HPCurrent = Math.Clamp(HpCurrent, 0, _pk.Stat_HPMax);
        }

        Persist();
        _onApplied();
    }

    private void SetMove(int i)
    {
        var vm = Moves[i];
        ushort move = (ushort)Math.Max(0, vm.MoveIndex);
        int ppUps = Math.Clamp(vm.PpUps, 0, 3);
        int pp = Math.Clamp(vm.Pp, 0, _pk.GetMovePP(move, ppUps));
        switch (i)
        {
            case 0: _pk.Move1 = move; _pk.Move1_PPUps = ppUps; _pk.Move1_PP = pp; break;
            case 1: _pk.Move2 = move; _pk.Move2_PPUps = ppUps; _pk.Move2_PP = pp; break;
            case 2: _pk.Move3 = move; _pk.Move3_PPUps = ppUps; _pk.Move3_PP = pp; break;
            case 3: _pk.Move4 = move; _pk.Move4_PPUps = ppUps; _pk.Move4_PP = pp; break;
        }
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

    private static IReadOnlyList<string> MaterializeLocations(GameStrings str, PKM pk)
    {
        var span = str.GetLocationNames((byte)pk.Generation, pk.Version);
        var arr = new string[span.Length];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = span[i];
        return arr;
    }

    // Gen3 status byte <-> combo index. Sleep uses a small turn counter.
    private static int StatusToIndex(int s) => s switch
    {
        0 => 0,
        _ when (s & 0x07) != 0 => 1, // sleep
        _ when (s & 0x08) != 0 => 2, // poison
        _ when (s & 0x10) != 0 => 3, // burn
        _ when (s & 0x20) != 0 => 4, // freeze
        _ when (s & 0x40) != 0 => 5, // paralysis
        _ when (s & 0x80) != 0 => 6, // bad poison
        _ => 0,
    };

    private static int IndexToStatus(int i) => i switch
    {
        1 => 3,      // sleep (3 turns)
        2 => 0x08,   // poison
        3 => 0x10,   // burn
        4 => 0x20,   // freeze
        5 => 0x40,   // paralysis
        6 => 0x80,   // bad poison
        _ => 0,
    };

    private static int Clamp(int v, int max) => Math.Clamp(v, 0, max);
}

/// <summary>One of the four move slots: move id, current PP and PP Ups (with a live max).</summary>
public partial class MoveSlotViewModel : ViewModelBase
{
    private readonly Func<ushort, int, int> _maxPp;

    public IReadOnlyList<string> MoveNames { get; }
    [ObservableProperty] public partial int MoveIndex { get; set; }
    [ObservableProperty] public partial int Pp { get; set; }
    [ObservableProperty] public partial int PpUps { get; set; }

    public int MaxPp => _maxPp((ushort)Math.Max(0, MoveIndex), Math.Clamp(PpUps, 0, 3));

    public MoveSlotViewModel(IReadOnlyList<string> moveNames, int move, int pp, int ppUps, Func<ushort, int, int> maxPp)
    {
        MoveNames = moveNames;
        _maxPp = maxPp;
        MoveIndex = move;
        Pp = pp;
        PpUps = ppUps;
    }

    partial void OnMoveIndexChanged(int value)
    {
        OnPropertyChanged(nameof(MaxPp));
        if (Pp > MaxPp) Pp = MaxPp;
    }

    partial void OnPpUpsChanged(int value)
    {
        OnPropertyChanged(nameof(MaxPp));
        if (Pp > MaxPp) Pp = MaxPp;
    }
}
