using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>Editor for the trainer/save-level info (name, IDs, money, play time…).</summary>
public partial class TrainerViewModel : ViewModelBase
{
    private readonly SaveFile _sav;
    private readonly Action _onApplied;

    [ObservableProperty] public partial string OT { get; set; }
    [ObservableProperty] public partial int Gender { get; set; }       // 0 = M, 1 = F
    [ObservableProperty] public partial int TID { get; set; }
    [ObservableProperty] public partial int SID { get; set; }
    [ObservableProperty] public partial int Money { get; set; }
    [ObservableProperty] public partial int Hours { get; set; }
    [ObservableProperty] public partial int Minutes { get; set; }

    public string[] GenderOptions { get; } = ["♂ Maschio", "♀ Femmina"];

    public TrainerViewModel(SaveFile sav, Action onApplied)
    {
        _sav = sav;
        _onApplied = onApplied;

        OT = sav.OT;
        Gender = sav.Gender;
        TID = (int)sav.DisplayTID;
        SID = (int)sav.DisplaySID;
        Money = (int)Math.Min(sav.Money, int.MaxValue);
        Hours = sav.PlayedHours;
        Minutes = sav.PlayedMinutes;
    }

    [RelayCommand]
    private void Apply()
    {
        _sav.OT = OT;
        _sav.Gender = (byte)Gender;
        _sav.DisplayTID = (uint)Math.Max(0, TID);
        _sav.DisplaySID = (uint)Math.Max(0, SID);
        _sav.Money = (uint)Math.Clamp(Money, 0, (int)Math.Min(_sav.MaxMoney, int.MaxValue));
        _sav.PlayedHours = Math.Clamp(Hours, 0, 999);
        _sav.PlayedMinutes = Math.Clamp(Minutes, 0, 59);
        _onApplied();
    }
}
