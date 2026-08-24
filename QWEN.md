# Guida al progetto (per Qwen)

Questo è un progetto **C# .NET 10 con GUI Avalonia** (non un progetto di file Markdown).
Si modificano SOLO file `.cs` e `.axaml`. Non esistono file di contenuto `.md` da editare.
La cartella `PKHeX.Core` è una libreria esterna già pronta: NON va modificata.

## Come compilare (fallo sempre dopo ogni modifica)
```
cd /home/mattia/Scaricati/PKHeX-26.07.07/PKHeX.Avalonia
dotnet build -c Release
```
Deve dire "0 Errori". Se ci sono errori, correggili prima di finire.

## Dove si trova cosa

Tutta l'interfaccia grafica è in UN solo file:
`PKHeX.Avalonia/Views/MainWindow.axaml`
Dentro, per trovare una sezione cerca il testo:
- `<TabItem Header="Pokémon">` = scheda Pokémon (box + editor)
- `<TabItem Header="Allenatore">` = scheda Allenatore
- `<TabItem Header="Zaino">` = scheda Zaino
- `<TabItem Header="Pokédex">` = scheda Pokédex
- `<TabItem Header="Eventi">` = scheda Eventi
- oppure i commenti `<!-- ... -->` (es. `<!-- Editor panel`, `<!-- Riga identità`, `<!-- Area box`).

La logica (i dati e i comandi) è nei ViewModel, in `PKHeX.Avalonia/ViewModels/`:
- `MainViewModel.cs` = apri/salva, box, creare/rimuovere Pokémon, cambio box.
- `PokemonEditorViewModel.cs` = editor del singolo Pokémon (specie, natura, genere, shiny, mosse, PP, IV/EV, strumento, cattura, soprannome, marcature, legalità).
- `TrainerViewModel.cs` = dati allenatore (nome OT, TID, SID, soldi, tempo di gioco).
- `BagViewModel.cs` = zaino (tasche e oggetti).
- `DexViewModel.cs` = Pokédex (visti/catturati) ed event flags.
- `SlotViewModel.cs` = come appare una casella del box.
- `SpriteLoader.cs` = caricamento sprite.
- `App.axaml` = tema (chiaro) e colori globali.

## Quale file per quale richiesta
- Modificare l'aspetto/layout di box, Pokédex o editor → `MainWindow.axaml`.
- Aggiungere/cambiare un campo dell'editor Pokémon → `PokemonEditorViewModel.cs` (dato) + `MainWindow.axaml` (controllo grafico nella scheda Pokémon).
- Cambiare la scheda Allenatore → `TrainerViewModel.cs` + `<TabItem Header="Allenatore">`.
- Cambiare lo Zaino → `BagViewModel.cs` + `<TabItem Header="Zaino">`.
- Cambiare Pokédex/Eventi → `DexViewModel.cs` + `<TabItem Header="Pokédex">` o `"Eventi"`.

## Regole importanti (Gen 3)
- Genere, Natura e Forma NON si impostano direttamente: derivano dal PID. Per cambiarli si rigenera il PID (vedi `Apply()` in `PokemonEditorViewModel.cs`, usa `EntityPID.GetRandomPID`).
- Shiny: usa `pk.SetShiny()`.
- Nel Gen 3 la data di cattura non esiste (è null).
- Nei binding XAML: `{Binding NomeProprietà}` collega al ViewModel di quella scheda.

## Come lavorare
1. Leggi questa guida.
2. Cerca la sezione giusta (nome scheda o commento) nel file indicato.
3. Fai la modifica.
4. Compila (`dotnet build -c Release`) e controlla 0 errori.
5. Se aggiungi/rinomini qualcosa di importante, aggiorna una riga di questa guida.

## Stato
Ultimo push su GitHub: commit 4741ebd. Molte modifiche grafiche recenti sono solo in locale (non ancora pushate).
