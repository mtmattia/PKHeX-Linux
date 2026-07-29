# PKHeX-Linux

**An unofficial, native Linux GUI port of [PKHeX](https://github.com/kwsch/PKHeX)** (the Pokémon core-series save editor), built with [Avalonia UI](https://avaloniaui.net/) on top of the upstream `PKHeX.Core` library.

---

## ⚠️ Please read before using

> - **Unofficial.** This project is **not affiliated with, endorsed by, or supported by** the official PKHeX project or its author (Kaphotics / Project Pokémon). Do not report issues with this port to the upstream project.
> - **AI-generated, not human-reviewed.** This port was written **entirely by an AI assistant (Claude)** and has **not been reviewed by the person publishing it**. It may contain bugs. There is **no warranty of any kind** — use it at your own risk.
> - **Back up your save files first.** Always keep a copy of any save before editing it.
> - **Work in progress.** It has been primarily tested with **Generation 3** saves (Ruby/Sapphire/Emerald/FireRed/LeafGreen). Other generations rely on `PKHeX.Core` but the GUI has not been exercised against them.

---

## Why this exists

The official PKHeX GUI is written in **Windows Forms**, which is Windows-only. All of PKHeX's actual logic, however, lives in **`PKHeX.Core`**, which is a plain cross-platform .NET library. This project reuses `PKHeX.Core` unchanged and provides a **new, native Linux graphical interface** written in Avalonia — no Wine, no emulation.

## Requirements

- **.NET 10** — **required.**
  - To **run** the app you need the **.NET 10 runtime** installed.
  - To **build** it you need the **.NET 10 SDK**.
  - Get it from <https://dotnet.microsoft.com/download/dotnet/10.0> (or your distro's package manager, e.g. `dotnet-sdk` on Arch).
  - A **self-contained** build (bundling the runtime, so nothing extra needs to be installed) can be produced — see below.

## Build & run

```bash
# from the repository root
cd PKHeX.Avalonia
dotnet run -c Release
```

Or produce a fast-launching optimized build once and run that:

```bash
cd PKHeX.Avalonia
dotnet publish -c Release -o publish     # framework-dependent (needs .NET 10 runtime)
./publish/PKHeX.Avalonia
```

To make a **self-contained** build that does **not** require .NET to be installed:

```bash
dotnet publish PKHeX.Avalonia -c Release -r linux-x64 --self-contained \
    -p:PublishSingleFile=true -o publish-selfcontained
./publish-selfcontained/PKHeX.Avalonia
```

You can pass a save file to open on launch via the `PKHEX_AUTOLOAD` environment variable, or use the 📂 button in the app.

## What works

- Open a save file (`main`, `.sav`, `.dsv`, …) and save it back to disk
- **Boxes & Party** with Pokémon sprites
- **Pokémon editor**: species, level, nature, moves, IVs/EVs, contest condition, ribbons
- **Showdown** set import/export
- **Trainer** editor: OT name, gender, TID/SID, money, play time
- **Bag** editor: all pouches (items, balls, TMs/HMs, berries, key items, PC) with quantities
- **Pokédex / Events**: mark all seen/caught, raw event-flag get/set

## Credits & license

- Built on **[PKHeX](https://github.com/kwsch/PKHeX)** by Kaphotics / Project Pokémon — the original PKHeX README is preserved in [`README-PKHeX-upstream.md`](README-PKHeX-upstream.md).
- Sprite collection from [pokesprite](https://github.com/msikma/pokesprite) (MIT).
- QR generation code from [QRCoder](https://github.com/codebude/QRCoder) (MIT).

This project inherits PKHeX's license: **GPL-3.0-or-later** (see [`LICENSE`](LICENSE)). As with the original, do not use significantly hacked Pokémon in battle or in trades with others who are unaware.
