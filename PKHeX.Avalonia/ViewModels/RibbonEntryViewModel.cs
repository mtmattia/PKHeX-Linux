using CommunityToolkit.Mvvm.ComponentModel;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

/// <summary>
/// One ribbon slot of a <see cref="PKM"/>. Boolean ribbons are a checkbox;
/// count-based ribbons (Gen3/4 contest ribbons) expose a 0..Max value.
/// </summary>
public partial class RibbonEntryViewModel : ViewModelBase
{
    /// <summary>Reflection property name on the PKM, e.g. "RibbonChampionG3".</summary>
    public string PropertyName { get; }

    /// <summary>Friendly label (prefix stripped).</summary>
    public string Label { get; }

    public bool IsBoolean { get; }
    public int MaxCount { get; }

    [ObservableProperty] public partial bool IsSet { get; set; }
    [ObservableProperty] public partial int Count { get; set; }

    public RibbonEntryViewModel(RibbonInfo info)
    {
        PropertyName = info.Name;
        Label = Prettify(info.Name);
        IsBoolean = info.Type == RibbonValueType.Boolean;
        MaxCount = IsBoolean ? 1 : info.MaxCount;
        IsSet = info.HasRibbon;
        Count = info.RibbonCount;
    }

    /// <summary>Writes this ribbon's state back into the entity via reflection.</summary>
    public void ApplyTo(PKM pk)
    {
        if (IsBoolean)
            ReflectUtil.SetValue(pk, PropertyName, IsSet);
        else
            ReflectUtil.SetValue(pk, PropertyName, (byte)Count);
    }

    private static string Prettify(string name)
    {
        var s = name;
        if (s.StartsWith("RibbonCount"))
            s = s["RibbonCount".Length..];
        else if (s.StartsWith("Ribbon"))
            s = s["Ribbon".Length..];
        return s;
    }
}
