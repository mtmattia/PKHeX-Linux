using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Avalonia;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>Editor for the Pokédex (seen/caught) and event flags (named + raw).</summary>
public partial class DexViewModel : ViewModelBase
{
    private readonly SaveFile _sav;
    private readonly Action _onApplied;

    [ObservableProperty] public partial int SeenCount { get; set; }
    [ObservableProperty] public partial int CaughtCount { get; set; }
    public int MaxSpecies => _sav.MaxSpeciesID;

    // Per-species dex entries (seen/caught), with a text filter.
    private readonly List<DexEntryViewModel> _allDex = new();
    public ObservableCollection<DexEntryViewModel> DexEntries { get; } = new();
    [ObservableProperty] public partial string DexSearch { get; set; } = "";

    // Event flags
    public bool HasEventFlags { get; }
    public int EventFlagCount { get; }
    [ObservableProperty] public partial int FlagNumber { get; set; }
    [ObservableProperty] public partial bool FlagValue { get; set; }

    // Named event flags (research data), with a text filter.
    private readonly List<NamedFlagViewModel> _allNamed = new();
    public ObservableCollection<NamedFlagViewModel> NamedFlags { get; } = new();
    public bool HasNamedFlags => _allNamed.Count > 0;
    [ObservableProperty] public partial string FlagSearch { get; set; } = "";

    private readonly IEventFlagArray? _flags;

    public DexViewModel(SaveFile sav, Action onApplied)
    {
        _sav = sav;
        _onApplied = onApplied;

        if (sav is IEventFlagArray fa)
        {
            _flags = fa;
            HasEventFlags = true;
            EventFlagCount = fa.EventFlagCount;

            foreach (var e in EventFlagCatalog.Load(sav))
            {
                if ((uint)e.Number < (uint)EventFlagCount)
                    _allNamed.Add(new NamedFlagViewModel(fa, e, onApplied));
            }
            foreach (var f in _allNamed)
                NamedFlags.Add(f);
        }
        var speciesNames = GameInfo.Strings.Species;
        for (ushort s = 1; s <= sav.MaxSpeciesID; s++)
        {
            var name = s < speciesNames.Count ? speciesNames[s] : $"#{s}";
            _allDex.Add(new DexEntryViewModel(sav, s, name, OnDexEntryChanged));
        }
        foreach (var d in _allDex)
            DexEntries.Add(d);

        RefreshCounts();
        if (HasEventFlags) FlagValue = _flags!.GetEventFlag(0);
    }

    private void OnDexEntryChanged()
    {
        RefreshCounts();
        _onApplied();
    }

    partial void OnDexSearchChanged(string value)
    {
        var q = value.Trim();
        DexEntries.Clear();
        foreach (var d in _allDex)
            if (q.Length == 0 || d.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || d.Number.ToString().Contains(q))
                DexEntries.Add(d);
    }

    partial void OnFlagSearchChanged(string value)
    {
        var q = value.Trim();
        NamedFlags.Clear();
        foreach (var f in _allNamed)
            if (q.Length == 0 || f.Label.Contains(q, StringComparison.OrdinalIgnoreCase) || f.Number.ToString().Contains(q))
                NamedFlags.Add(f);
    }

    private void RefreshCounts()
    {
        int seen = 0, caught = 0;
        for (ushort s = 1; s <= _sav.MaxSpeciesID; s++)
        {
            if (_sav.GetSeen(s)) seen++;
            if (_sav.GetCaught(s)) caught++;
        }
        SeenCount = seen;
        CaughtCount = caught;
    }

    [RelayCommand]
    private void SetAllSeen() => SetAll(seen: true, caught: false);

    [RelayCommand]
    private void SetAllCaught() => SetAll(seen: true, caught: true);

    private void SetAll(bool seen, bool caught)
    {
        for (ushort s = 1; s <= _sav.MaxSpeciesID; s++)
        {
            if (seen) _sav.SetSeen(s, true);
            if (caught) _sav.SetCaught(s, true);
        }
        foreach (var d in _allDex)
            d.Refresh();
        RefreshCounts();
        _onApplied();
    }

    partial void OnFlagNumberChanged(int value)
    {
        if (_flags is not null && value >= 0 && value < EventFlagCount)
            FlagValue = _flags.GetEventFlag(value);
    }

    [RelayCommand]
    private void ApplyFlag()
    {
        if (_flags is null || FlagNumber < 0 || FlagNumber >= EventFlagCount)
            return;
        _flags.SetEventFlag(FlagNumber, FlagValue);
        _onApplied();
    }
}

/// <summary>One species in the Pokédex grid; the sprite reflects (and clicking cycles) seen/caught.</summary>
public sealed partial class DexEntryViewModel : ViewModelBase
{
    private readonly SaveFile _sav;
    private readonly Action _onChanged;

    public int Number { get; }
    public string Name { get; }
    public string Display => $"#{Number:000}  {Name}";

    public DexView State => IsCaught ? DexView.Caught : IsSeen ? DexView.Seen : DexView.NotSeen;
    public Bitmap? Sprite => SpriteLoader.GetDexSprite((ushort)Number, State);

    public bool IsSeen
    {
        get => _sav.GetSeen((ushort)Number);
        set
        {
            if (value == _sav.GetSeen((ushort)Number))
                return;
            _sav.SetSeen((ushort)Number, value);
            if (!value)
                _sav.SetCaught((ushort)Number, false); // can't be caught if not seen
            NotifyState();
            _onChanged();
        }
    }

    public bool IsCaught
    {
        get => _sav.GetCaught((ushort)Number);
        set
        {
            if (value == _sav.GetCaught((ushort)Number))
                return;
            _sav.SetCaught((ushort)Number, value);
            if (value)
                _sav.SetSeen((ushort)Number, true); // catching implies seen
            NotifyState();
            _onChanged();
        }
    }

    /// <summary>Click cycles: not seen → seen → caught → not seen.</summary>
    [RelayCommand]
    private void Cycle()
    {
        if (!IsSeen) IsSeen = true;
        else if (!IsCaught) IsCaught = true;
        else { IsCaught = false; IsSeen = false; }
    }

    public void Refresh() => NotifyState();

    private void NotifyState()
    {
        OnPropertyChanged(nameof(IsSeen));
        OnPropertyChanged(nameof(IsCaught));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(Sprite));
    }

    public DexEntryViewModel(SaveFile sav, int number, string name, Action onChanged)
    {
        _sav = sav;
        Number = number;
        Name = name;
        _onChanged = onChanged;
    }
}

/// <summary>A single named event flag; toggling the checkbox writes it live into the save.</summary>
public sealed partial class NamedFlagViewModel : ViewModelBase
{
    private readonly IEventFlagArray _flags;
    private readonly Action _onChanged;

    public int Number { get; }
    public string Label { get; }
    public string Display => $"[{Number}] {Label}";

    public bool IsSet
    {
        get => _flags.GetEventFlag(Number);
        set
        {
            if (value == _flags.GetEventFlag(Number))
                return;
            _flags.SetEventFlag(Number, value);
            OnPropertyChanged();
            _onChanged();
        }
    }

    public NamedFlagViewModel(IEventFlagArray flags, FlagEntry entry, Action onChanged)
    {
        _flags = flags;
        Number = entry.Number;
        Label = entry.Label;
        _onChanged = onChanged;
    }
}
