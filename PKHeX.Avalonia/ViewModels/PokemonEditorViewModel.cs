using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
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
    private readonly Action<string>? _onStatus;

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
    public string GenderSymbol => GenderIndex == 1 ? "♀" : CanEditGender ? "♂" : "⚲";

    partial void OnGenderIndexChanged(int value) => OnPropertyChanged(nameof(GenderSymbol));

    /// <summary>Click on the gender symbol flips it (no dropdown); genderless stays fixed.</summary>
    [RelayCommand]
    private void ToggleGender()
    {
        if (CanEditGender)
            GenderIndex ^= 1;
    }

    [ObservableProperty] public partial int HeldItemIndex { get; set; }

    // Ability: the species' selectable abilities (name list) + chosen slot.
    public IReadOnlyList<string> AbilityOptions { get; }
    public bool HasAbilityChoice => AbilityOptions.Count > 1;
    [ObservableProperty] public partial int AbilityIndex { get; set; }

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

    /// <summary>Total EVs spent and how many remain of the 510 cap.</summary>
    public int EvTotal => EvHp + EvAtk + EvDef + EvSpa + EvSpd + EvSpe;
    public int EvRemaining => 510 - EvTotal;
    public string EvSummary => $"EV: {EvTotal}/510 · rimanenti {Math.Max(0, EvRemaining)}";

    partial void OnEvHpChanged(int value) => NotifyEvTotals();
    partial void OnEvAtkChanged(int value) => NotifyEvTotals();
    partial void OnEvDefChanged(int value) => NotifyEvTotals();
    partial void OnEvSpaChanged(int value) => NotifyEvTotals();
    partial void OnEvSpdChanged(int value) => NotifyEvTotals();
    partial void OnEvSpeChanged(int value) => NotifyEvTotals();

    private void NotifyEvTotals()
    {
        OnPropertyChanged(nameof(EvTotal));
        OnPropertyChanged(nameof(EvRemaining));
        OnPropertyChanged(nameof(EvSummary));
    }

    [ObservableProperty] public partial bool IsShiny { get; set; }

    // Nickname.
    [ObservableProperty] public partial string NicknameText { get; set; } = "";

    // Markings (● ▲ ■ ♥ …) — each can be toggled on/off.
    public bool HasMarkings { get; }
    public ObservableCollection<MarkingViewModel> Markings { get; } = new();

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

    /// <summary>Sheen (Lucentezza) exists in Gen 3/4/8.</summary>
    public bool HasSheen => HasContest && (_pk.Format is 3 or 4 or 8);
    /// <summary>Max stars: 12 in Gen4/8, 10 in Gen3.</summary>
    private bool SheenIs12 => _pk.Format is 4 or 8;
    public int SheenMax => SheenIs12 ? 12 : 10;

    /// <summary>Number of lit stars for the current sheen (game formula; 0 sheen = 0).</summary>
    public int SheenSparkles => ConSheen == 0 ? 0
        : SheenIs12 ? (ConSheen == 255 ? 12 : 12 * ConSheen / 256)
        : (ConSheen == 255 ? 10 : ConSheen / 29 + 1);

    /// <summary>Clickable star toggles for the sheen.</summary>
    public ObservableCollection<SheenStarViewModel> SheenStars { get; } = new();

    partial void OnConSheenChanged(int value)
    {
        OnPropertyChanged(nameof(SheenSparkles));
        UpdateSheenStars();
    }

    private void UpdateSheenStars()
    {
        int lit = SheenSparkles;
        foreach (var s in SheenStars)
            s.Lit = s.Index <= lit;
    }

    // Click on star N: set that many sparkles (or clear one if it's already lit).
    private void SetSheen(int stars)
    {
        int target = SheenSparkles == stars ? stars - 1 : stars;
        ConSheen = SparklesToSheen(target);
    }

    // Inverse of the sparkle formula: minimum sheen that yields n stars.
    private int SparklesToSheen(int n)
    {
        if (n <= 0) return 0;
        if (SheenIs12) return n >= 12 ? 255 : (int)System.Math.Ceiling(n * 256.0 / 12);
        return n >= 10 ? 255 : (n == 1 ? 1 : (n - 1) * 29);
    }

    /// <summary>Ribbons (fiocchi) applicable to this entity (all, used on Apply).</summary>
    public ObservableCollection<RibbonEntryViewModel> Ribbons { get; } = new();
    /// <summary>Contest ribbons (count-based Cool/Beauty/… ranks) — shown with Gare Pokémon.</summary>
    public ObservableCollection<RibbonEntryViewModel> ContestRibbons { get; } = new();
    /// <summary>Non-contest ribbons (boolean) — shown in the separate Fiocchi section.</summary>
    public ObservableCollection<RibbonEntryViewModel> OtherRibbons { get; } = new();
    public bool HasContestRibbons => ContestRibbons.Count > 0;
    public bool HasOtherRibbons => OtherRibbons.Count > 0;

    /// <summary>Showdown set text used by the import/export controls.</summary>
    [ObservableProperty] public partial string ShowdownText { get; set; } = "";

    public string Header => _isParty
        ? $"Squadra · Slot {_slot + 1}"
        : $"Box {_box + 1} · Slot {_slot + 1}";

    // Legality (computed on load; the editor is rebuilt after each Apply, so it refreshes).
    public bool LegalityValid { get; private set; }
    public string LegalityText { get; private set; } = "";
    public string LegalityReport { get; private set; } = "";
    public bool HasLegalityReport => LegalityReport.Length != 0;

    // Friendly per-move hints: names the illegal moves; for a level-up move whose learn
    // level is above the current level, tells the minimum level required.
    public string MoveHints { get; private set; } = "";
    public bool HasMoveHints => MoveHints.Length != 0;

    public PokemonEditorViewModel(SaveFile sav, PKM pk, int box, int slot, bool isParty, Action onApplied, Action<string>? onStatus = null)
    {
        _sav = sav;
        _pk = pk;
        _box = box;
        _slot = slot;
        _isParty = isParty;
        _onApplied = onApplied;
        _onStatus = onStatus;

        var str = GameInfo.Strings;
        ItemNames = str.GetItemStrings(pk.Context, pk.Version);
        LocationNames = MaterializeLocations(str, pk);

        Species = pk.Species;
        Level = pk.CurrentLevel;
        NatureIndex = (int)pk.Nature;

        CanEditGender = !PersonalInfo.IsSingleGender(pk.PersonalInfo.Gender);
        GenderIndex = pk.Gender == 1 ? 1 : 0;

        HeldItemIndex = pk.HeldItem;

        AbilityOptions = BuildAbilityOptions(pk, str);
        int abilN = pk.AbilityNumber switch { 4 => 2, 2 => 1, _ => 0 };
        AbilityIndex = Math.Clamp(abilN, 0, Math.Max(0, AbilityOptions.Count - 1));

        Moves.Add(new MoveSlotViewModel(MoveNames, pk.Move1, pk.Move1_PP, pk.Move1_PPUps, pk.GetMovePP));
        Moves.Add(new MoveSlotViewModel(MoveNames, pk.Move2, pk.Move2_PP, pk.Move2_PPUps, pk.GetMovePP));
        Moves.Add(new MoveSlotViewModel(MoveNames, pk.Move3, pk.Move3_PP, pk.Move3_PPUps, pk.GetMovePP));
        Moves.Add(new MoveSlotViewModel(MoveNames, pk.Move4, pk.Move4_PP, pk.Move4_PPUps, pk.GetMovePP));

        IvHp = pk.IV_HP; IvAtk = pk.IV_ATK; IvDef = pk.IV_DEF;
        IvSpa = pk.IV_SPA; IvSpd = pk.IV_SPD; IvSpe = pk.IV_SPE;
        EvHp = pk.EV_HP; EvAtk = pk.EV_ATK; EvDef = pk.EV_DEF;
        EvSpa = pk.EV_SPA; EvSpd = pk.EV_SPD; EvSpe = pk.EV_SPE;
        IsShiny = pk.IsShiny;

        NicknameText = pk.Nickname;
        if (pk is IAppliedMarkings<bool> marks)
        {
            HasMarkings = true;
            string[] glyphs = ["●", "▲", "■", "♥", "★", "◆"];
            for (int i = 0; i < marks.MarkingCount; i++)
                Markings.Add(new MarkingViewModel(i, i < glyphs.Length ? glyphs[i] : "◦", marks.GetMarking(i)));
        }

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

        if (HasSheen)
        {
            for (int i = 1; i <= SheenMax; i++)
                SheenStars.Add(new SheenStarViewModel(i, SetSheen));
            UpdateSheenStars();
        }

        foreach (var info in RibbonInfo.GetRibbonInfo(pk))
        {
            var r = new RibbonEntryViewModel(info);
            Ribbons.Add(r);
            (r.IsBoolean ? OtherRibbons : ContestRibbons).Add(r); // count-based = contest ranks
        }

        // Live legality: re-evaluate whenever any child field (moves/ribbons/markings) changes.
        foreach (var m in Moves) m.PropertyChanged += OnChildChanged;
        foreach (var r in Ribbons) r.PropertyChanged += OnChildChanged;
        foreach (var mk in Markings) mk.PropertyChanged += OnChildChanged;

        _loaded = true;
        RefreshLegality();
    }

    private readonly bool _loaded;
    // Re-entrancy guard: RefreshLegality raises property changes (LegalityText, MoveHints, …)
    // which flow back through OnPropertyChanged — without this it would recurse infinitely.
    private bool _inLegalityRefresh;

    // These properties don't affect legality — skip the (harmless) re-evaluation on them.
    private static readonly System.Collections.Generic.HashSet<string> LegalityIgnored = new()
    {
        nameof(LegalityValid), nameof(LegalityText), nameof(LegalityReport), nameof(HasLegalityReport),
        nameof(CanFixLegality), nameof(CanFixPid), nameof(MoveHints), nameof(HasMoveHints),
        nameof(GenderSymbol), nameof(ShowdownText), nameof(EvTotal), nameof(EvRemaining), nameof(EvSummary),
        nameof(SheenSparkles), nameof(HpMax),
    };

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_loaded && !_inLegalityRefresh && e.PropertyName is { } name && !LegalityIgnored.Contains(name))
            RefreshLegality();
    }

    private void OnChildChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_loaded && !_inLegalityRefresh)
            RefreshLegality();
    }

    private void RefreshLegality()
    {
        if (_inLegalityRefresh)
            return;
        _inLegalityRefresh = true;
        try
        {
            // Evaluate what the entity WOULD be with the current (unapplied) edits, so the
            // indicator reflects the pending changes before the user presses Applica.
            var probe = _pk.Clone();
            WriteTo(probe);
            probe.RefreshChecksum();
            var la = new LegalityAnalysis(probe);
            LegalityValid = la.Valid;
            LegalityText = la.Valid ? "✅ Legale" : "⚠️ Potenzialmente illegale";
            LegalityReport = la.Valid ? "" : la.Report();
            MoveHints = la.Valid ? "" : BuildMoveHints(probe, la);
        }
        catch (Exception ex)
        {
            LegalityValid = false;
            LegalityText = "⚠️ Legalità non determinabile";
            LegalityReport = ex.Message;
            MoveHints = "";
        }
        finally
        {
            OnPropertyChanged(nameof(LegalityValid));
            OnPropertyChanged(nameof(LegalityText));
            OnPropertyChanged(nameof(LegalityReport));
            OnPropertyChanged(nameof(HasLegalityReport));
            OnPropertyChanged(nameof(CanFixLegality));
            OnPropertyChanged(nameof(CanFixPid));
            OnPropertyChanged(nameof(MoveHints));
            OnPropertyChanged(nameof(HasMoveHints));
            _inLegalityRefresh = false;
        }
    }

    [RelayCommand]
    private void Apply()
    {
        WriteTo(_pk);
        Persist();
        _onApplied();
    }

    /// <summary>True when the current (pending) edits are not legal — enables the Fix button.</summary>
    public bool CanFixLegality => !LegalityValid;

    /// <summary>Best-effort legalization of the common fixables (moves+PP, met level/location,
    /// EV cap, party stats). It first applies the pending UI edits, then repairs. Routes through
    /// the normal apply pipeline, so it is captured by Undo and refreshes the editor.</summary>
    [RelayCommand]
    private void FixLegality()
    {
        WriteTo(_pk);
        TryLegalize(_pk);
        Persist();
        _onApplied();
    }

    /// <summary>Enabled for Gen3 illegal entities: fix the PID/IV correlation for the chosen nature.</summary>
    public bool CanFixPid => !LegalityValid && _pk.Format is 3;

    /// <summary>
    /// Makes the PID legal for the chosen nature.
    /// • Bred/egg entities (no PID/IV correlation in Gen3): just assign a PID for the nature and
    ///   keep the user's IVs — full freedom on nature AND IVs.
    /// • Wild/static (Method-1 correlation): frame-search a seed whose PID gives the nature and
    ///   whose derived IVs keep the entity legal (IVs are dictated by the frame).
    /// Also fixes the non-PID issues (moves/met/EV) first so the result is fully legal.
    /// </summary>
    [RelayCommand]
    private async Task FixPid()
    {
        WriteTo(_pk);
        TryLegalize(_pk); // moves/met/EV so only the PID/IV correlation remains
        var nature = (Nature)NatureIndex;

        // Free case: bred/egg → any PID legal, keep the chosen IVs.
        if (_pk.WasEgg || _pk.IsEgg)
        {
            _pk.PID = EntityPID.GetRandomPID(Util.Rand, _pk.Species, (byte)_pk.Gender, _pk.Version, nature, _pk.Form, _pk.PID);
            if (IsShiny) _pk.SetShiny();
            else while (_pk.IsShiny) _pk.PID = EntityPID.GetRandomPID(Util.Rand, _pk.Species, (byte)_pk.Gender, _pk.Version, nature, _pk.Form, _pk.PID);
            _pk.RefreshChecksum();
            Persist();
            _onApplied();
            _onStatus?.Invoke($"PID legale impostato per natura {NatureNames[(int)nature]} (uovo: IV liberi).");
            return;
        }

        // Correlated case: search a Method-1 frame off the UI thread (LegalityAnalysis is heavy).
        bool wantShiny = IsShiny;
        var probe = _pk.Clone();
        var found = await Task.Run(() => FrameSearchMethod1(probe, nature, wantShiny));
        if (found is { } r)
        {
            _pk.PID = r.Pid;
            _pk.IV_HP = r.HP; _pk.IV_ATK = r.ATK; _pk.IV_DEF = r.DEF;
            _pk.IV_SPA = r.SPA; _pk.IV_SPD = r.SPD; _pk.IV_SPE = r.SPE;
            _pk.RefreshChecksum();
            Persist();
            _onApplied();
            _onStatus?.Invoke($"PID legale trovato per natura {NatureNames[(int)nature]} · IV {r.HP}/{r.ATK}/{r.DEF}/{r.SPA}/{r.SPD}/{r.SPE} (dettati dal frame).");
        }
        else
        {
            _onStatus?.Invoke(wantShiny
                ? "Nessun frame legale trovato (shiny + natura). Per shiny+IV liberi crea un uovo."
                : "Nessun frame legale trovato per questo incontro. Per natura+IV liberi crea un uovo.");
        }
    }

    private readonly record struct PidFrame(uint Pid, int HP, int ATK, int DEF, int SPA, int SPD, int SPE);

    // Search random Method-1 frames: PID (2 rand16) then IVs (2 rand16). Accept the first that
    // matches the target nature (and shiny request) AND passes full legality on the probe.
    private static PidFrame? FrameSearchMethod1(PKM pk, Nature nature, bool wantShiny)
    {
        var rng = new Random();
        uint tid = pk.TID16, sid = pk.SID16;
        int natureMatchesTried = 0;
        for (int i = 0; i < 4_000_000; i++)
        {
            uint seed = ((uint)rng.Next(0, 1 << 16) << 16) | (uint)rng.Next(0, 1 << 16);
            uint s = seed;
            uint pid = ClassicEraRNG.GetSequentialPID(ref s);
            if (pid % 25 != (uint)nature)
                continue;
            if (wantShiny && ((pid ^ (pid >> 16) ^ tid ^ sid) & 0xFFF8) != 0)
                continue; // require shiny if asked (rare)

            uint iv1 = LCRNG.Next16(ref s), iv2 = LCRNG.Next16(ref s);
            pk.PID = pid;
            pk.IV_HP = (int)(iv1 & 31); pk.IV_ATK = (int)((iv1 >> 5) & 31); pk.IV_DEF = (int)((iv1 >> 10) & 31);
            pk.IV_SPE = (int)(iv2 & 31); pk.IV_SPA = (int)((iv2 >> 5) & 31); pk.IV_SPD = (int)((iv2 >> 10) & 31);
            pk.RefreshChecksum();
            if (new LegalityAnalysis(pk).Valid)
                return new PidFrame(pid, pk.IV_HP, pk.IV_ATK, pk.IV_DEF, pk.IV_SPA, pk.IV_SPD, pk.IV_SPE);

            if (++natureMatchesTried >= 40_000)
                break; // bound the number of heavy legality checks
        }
        return null;
    }

    private static void TryLegalize(PKM pk)
    {
        // 1. EV cap: scale the six EVs down proportionally if the total exceeds 510.
        ClampEvTotal(pk);

        // 2. Met level/location: use PKHeX's encounter suggestion (safe, undoable).
        try
        {
            var info = EncounterSuggestion.GetSuggestedMetInfo(pk);
            if (info is not null && info.Location != 0)
            {
                pk.MetLocation = info.Location;
                pk.MetLevel = info.GetSuggestedMetLevel(pk);
            }
        }
        catch { /* leave met as-is if no suggestion */ }

        // 3. Moves: replace ONLY the illegal slots with legal suggestions, keeping the
        //    moves that are already valid. Then restore full PP.
        try
        {
            var la = new LegalityAnalysis(pk);
            var results = la.Info.Moves; // per-slot validity (index 0..3)
            Span<ushort> suggested = stackalloc ushort[4];
            la.GetSuggestedCurrentMoves(suggested);

            Span<ushort> cur = [pk.Move1, pk.Move2, pk.Move3, pk.Move4];
            Span<ushort> final = stackalloc ushort[4];

            // Which current moves we keep (valid and non-empty).
            for (int i = 0; i < 4; i++)
                final[i] = (i < results.Length && results[i].Valid) ? cur[i] : (ushort)0;

            // Fill the emptied (illegal) slots with suggested moves not already present.
            int pick = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i < results.Length && results[i].Valid)
                    continue; // kept
                while (pick < 4)
                {
                    ushort cand = suggested[pick++];
                    if (cand != 0 && !Contains(final, cand))
                    {
                        final[i] = cand;
                        break;
                    }
                }
            }

            if (final[0] != 0 || final[1] != 0 || final[2] != 0 || final[3] != 0)
                pk.SetMoves(final); // also sets max current PP
            pk.HealPP();
        }
        catch { /* keep existing moves if suggestion fails */ }

        // 4. Recompute stored stats for party members.
        pk.RefreshChecksum();
    }

    // Names each illegal move; for a level-up move learned above the current level,
    // states the minimum level required ("serve almeno il livello N").
    private string BuildMoveHints(PKM pk, LegalityAnalysis la)
    {
        var results = la.Info.Moves;
        Span<ushort> mv = [pk.Move1, pk.Move2, pk.Move3, pk.Move4];
        var sb = new StringBuilder();
        for (int i = 0; i < 4; i++)
        {
            ushort move = mv[i];
            if (move == 0 || (i < results.Length && results[i].Valid))
                continue;

            string name = move < MoveNames.Count ? MoveNames[move] : $"#{move}";
            if (TryGetLevelUpLevel(pk, move, out byte lvl) && lvl > pk.CurrentLevel)
                sb.AppendLine($"• Mossa {i + 1} — {name}: serve almeno il livello {lvl}");
            else
                sb.AppendLine($"• Mossa {i + 1} — {name}: mossa non valida");
        }
        return sb.ToString().TrimEnd();
    }

    // Minimum level-up level for a move on this species (Gen3 learn sources). Returns false
    // if the move isn't a level-up move (or the format isn't handled here).
    private static bool TryGetLevelUpLevel(PKM pk, ushort move, out byte level)
    {
        level = 0;
        if (pk.Format != 3)
            return false;
        var ls = pk.Version switch
        {
            GameVersion.E => LearnSource3E.Instance.GetLearnset(pk.Species, pk.Form),
            GameVersion.FR => LearnSource3FR.Instance.GetLearnset(pk.Species, pk.Form),
            GameVersion.LG => LearnSource3LG.Instance.GetLearnset(pk.Species, pk.Form),
            _ => LearnSource3RS.Instance.GetLearnset(pk.Species, pk.Form),
        };
        return ls.TryGetLevelLearnMove(move, out level);
    }

    private static bool Contains(ReadOnlySpan<ushort> span, ushort value)
    {
        foreach (var v in span)
            if (v == value)
                return true;
        return false;
    }

    private static void ClampEvTotal(PKM pk)
    {
        int total = pk.EV_HP + pk.EV_ATK + pk.EV_DEF + pk.EV_SPA + pk.EV_SPD + pk.EV_SPE;
        if (total <= 510)
            return;
        double f = 510.0 / total;
        pk.EV_HP = (int)(pk.EV_HP * f); pk.EV_ATK = (int)(pk.EV_ATK * f); pk.EV_DEF = (int)(pk.EV_DEF * f);
        pk.EV_SPA = (int)(pk.EV_SPA * f); pk.EV_SPD = (int)(pk.EV_SPD * f); pk.EV_SPE = (int)(pk.EV_SPE * f);
    }

    /// <summary>Writes all editor fields into <paramref name="pk"/> (used by Apply on the
    /// real entity, and by the live legality check on a throwaway clone).</summary>
    private void WriteTo(PKM pk)
    {
        // Species must be set before Level: CurrentLevel uses the species' growth rate.
        pk.Species = (ushort)Species;

        // Nature + gender + shiny: in Gen3–5 all three derive from the PID.
        // Reroll the PID ONLY when one of them actually changed — otherwise keep the
        // original PID, so we don't destroy the encounter's PID/IV correlation
        // (which would wrongly report "Invalid PID" after editing e.g. a move). Gen6+
        // store nature/gender independently, so set them directly.
        var nature = (Nature)NatureIndex;
        byte gender = CanEditGender ? (byte)GenderIndex : pk.Gender;
        if (pk.Format is 3 or 4 or 5)
        {
            bool needReroll = pk.Nature != nature
                           || (CanEditGender && pk.Gender != gender)
                           || pk.IsShiny != IsShiny;
            if (needReroll)
            {
                pk.PID = EntityPID.GetRandomPID(Util.Rand, pk.Species, gender, pk.Version, nature, pk.Form, pk.PID);
                if (IsShiny)
                    pk.SetShiny();
                else
                    while (pk.IsShiny)
                        pk.PID = EntityPID.GetRandomPID(Util.Rand, pk.Species, gender, pk.Version, nature, pk.Form, pk.PID);
            }
        }
        else
        {
            pk.Nature = nature;
            pk.Gender = gender;
            pk.SetIsShiny(IsShiny);
        }

        pk.CurrentLevel = (byte)Math.Clamp(Level, 1, 100);
        if (HasForms)
            pk.Form = (byte)Math.Clamp(FormIndex, 0, FormNames.Count - 1);

        pk.HeldItem = Math.Max(0, HeldItemIndex);

        // Ability: apply after species is set (personal table depends on it).
        if (AbilityOptions.Count > 0)
            pk.RefreshAbility(Math.Clamp(AbilityIndex, 0, AbilityOptions.Count - 1));

        // Nickname: empty = not nicknamed (reset to the species' default name).
        var defaultName = SpeciesName.GetSpeciesNameGeneration(pk.Species, pk.Language, (byte)pk.Format);
        var nick = (NicknameText ?? "").Trim();
        if (nick.Length == 0)
        {
            pk.Nickname = defaultName;
            pk.IsNicknamed = false;
        }
        else
        {
            pk.Nickname = nick;
            pk.IsNicknamed = nick != defaultName;
        }

        if (pk is IAppliedMarkings<bool> marks)
        {
            foreach (var mk in Markings)
                marks.SetMarking(mk.Index, mk.IsSet);
        }

        SetMove(pk, 0); SetMove(pk, 1); SetMove(pk, 2); SetMove(pk, 3);

        pk.IV_HP = Clamp(IvHp, 31); pk.IV_ATK = Clamp(IvAtk, 31); pk.IV_DEF = Clamp(IvDef, 31);
        pk.IV_SPA = Clamp(IvSpa, 31); pk.IV_SPD = Clamp(IvSpd, 31); pk.IV_SPE = Clamp(IvSpe, 31);
        pk.EV_HP = Clamp(EvHp, 255); pk.EV_ATK = Clamp(EvAtk, 255); pk.EV_DEF = Clamp(EvDef, 255);
        pk.EV_SPA = Clamp(EvSpa, 255); pk.EV_SPD = Clamp(EvSpd, 255); pk.EV_SPE = Clamp(EvSpe, 255);

        pk.MetLocation = (ushort)Math.Max(0, MetLocationIndex);
        pk.MetLevel = (byte)Math.Clamp(MetLevelValue, 0, 100);

        if (pk is IContestStats cs)
        {
            cs.ContestCool = (byte)Clamp(ConCool, 255); cs.ContestBeauty = (byte)Clamp(ConBeauty, 255);
            cs.ContestCute = (byte)Clamp(ConCute, 255); cs.ContestSmart = (byte)Clamp(ConSmart, 255);
            cs.ContestTough = (byte)Clamp(ConTough, 255); cs.ContestSheen = (byte)Clamp(ConSheen, 255);
        }

        foreach (var r in Ribbons)
            r.ApplyTo(pk);

        // Party-only: recompute stored stats (so HP max reflects the new IVs/level),
        // then apply the requested current HP and status condition.
        if (_isParty)
        {
            pk.ResetPartyStats();
            pk.Status_Condition = IndexToStatus(StatusIndex);
            pk.Stat_HPCurrent = Math.Clamp(HpCurrent, 0, pk.Stat_HPMax);
        }
    }

    private void SetMove(PKM pk, int i)
    {
        var vm = Moves[i];
        ushort move = (ushort)Math.Max(0, vm.MoveIndex);
        int ppUps = Math.Clamp(vm.PpUps, 0, 3);
        int pp = Math.Clamp(vm.Pp, 0, pk.GetMovePP(move, ppUps));
        switch (i)
        {
            case 0: pk.Move1 = move; pk.Move1_PPUps = ppUps; pk.Move1_PP = pp; break;
            case 1: pk.Move2 = move; pk.Move2_PPUps = ppUps; pk.Move2_PP = pp; break;
            case 2: pk.Move3 = move; pk.Move3_PPUps = ppUps; pk.Move3_PP = pp; break;
            case 3: pk.Move4 = move; pk.Move4_PPUps = ppUps; pk.Move4_PP = pp; break;
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

    // The species' selectable abilities as display names (duplicates kept: slot 1 / slot 2).
    private static IReadOnlyList<string> BuildAbilityOptions(PKM pk, GameStrings str)
    {
        var pi = pk.PersonalInfo;
        int count = pi.AbilityCount;
        var names = str.Ability;
        var list = new string[Math.Max(0, count)];
        for (int i = 0; i < list.Length; i++)
        {
            int id = pi.GetAbilityAtIndex(i);
            list[i] = (uint)id < names.Count ? names[id] : $"#{id}";
        }
        return list;
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

    /// <summary>Current max PP with the current PP Ups ("reali").</summary>
    public int MaxPp => _maxPp((ushort)Math.Max(0, MoveIndex), Math.Clamp(PpUps, 0, 3));
    /// <summary>Absolute max PP with 3 PP Ups ("PPMAX").</summary>
    public int MaxPpFull => _maxPp((ushort)Math.Max(0, MoveIndex), 3);

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
        OnPropertyChanged(nameof(MaxPpFull));
        if (Pp > MaxPp) Pp = MaxPp;
    }

    partial void OnPpUpsChanged(int value)
    {
        OnPropertyChanged(nameof(MaxPp));
        if (Pp > MaxPp) Pp = MaxPp;
    }
}

/// <summary>One clickable sheen star; lit ones are gold, the rest faint.</summary>
public partial class SheenStarViewModel : ViewModelBase
{
    private static readonly IBrush On = new SolidColorBrush(Color.Parse("#F4B400"));
    private static readonly IBrush Off = new SolidColorBrush(Color.Parse("#40000000"));
    private readonly Action<int> _click;

    public int Index { get; }
    [ObservableProperty] public partial bool Lit { get; set; }
    public IBrush StarBrush => Lit ? On : Off;

    partial void OnLitChanged(bool value) => OnPropertyChanged(nameof(StarBrush));

    [RelayCommand]
    private void Click() => _click(Index);

    public SheenStarViewModel(int index, Action<int> click)
    {
        Index = index;
        _click = click;
    }
}

/// <summary>A single toggleable marking (● ▲ ■ ♥ …).</summary>
public partial class MarkingViewModel : ViewModelBase
{
    public int Index { get; }
    public string Symbol { get; }
    [ObservableProperty] public partial bool IsSet { get; set; }

    public MarkingViewModel(int index, string symbol, bool isSet)
    {
        Index = index;
        Symbol = symbol;
        IsSet = isSet;
    }
}
