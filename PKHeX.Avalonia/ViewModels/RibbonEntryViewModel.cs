using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [RelayCommand]
    private void Increment()
    {
        if (Count < MaxCount) Count++;
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Count > 0) Count--;
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
        s = s.Replace("G3", "").Replace("G4", "").Replace("G6", ""); // drop gen tags

        // Split CamelCase into spaced words for readability.
        var sb = new StringBuilder(s.Length + 4);
        for (int i = 0; i < s.Length; i++)
        {
            if (i > 0 && char.IsUpper(s[i]) && !char.IsUpper(s[i - 1]))
                sb.Append(' ');
            sb.Append(s[i]);
        }
        var pretty = sb.ToString().Trim();

        // Italian names for the contest-ribbon categories.
        return pretty switch
        {
            "Cool" => "Classe",
            "Beauty" => "Bellezza",
            "Cute" => "Grazia",
            "Smart" => "Acume",
            "Tough" => "Grinta",
            _ => pretty,
        };
    }
}
