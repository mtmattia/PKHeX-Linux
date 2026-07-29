using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>Editor for the Pokédex (seen/caught) and raw event flags.</summary>
public partial class DexViewModel : ViewModelBase
{
    private readonly SaveFile _sav;
    private readonly Action _onApplied;

    [ObservableProperty] public partial int SeenCount { get; set; }
    [ObservableProperty] public partial int CaughtCount { get; set; }
    public int MaxSpecies => _sav.MaxSpeciesID;

    // Event flags
    public bool HasEventFlags { get; }
    public int EventFlagCount { get; }
    [ObservableProperty] public partial int FlagNumber { get; set; }
    [ObservableProperty] public partial bool FlagValue { get; set; }

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
        }
        RefreshCounts();
        if (HasEventFlags) FlagValue = _flags!.GetEventFlag(0);
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
