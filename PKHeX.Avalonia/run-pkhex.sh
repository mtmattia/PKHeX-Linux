#!/usr/bin/env bash
# Avvia PKHeX.Avalonia (port nativo Linux).
# Uso: ./run-pkhex.sh [percorso-save-da-aprire]
#
# Usa l'eseguibile pubblicato (avvio veloce) e lo RIPUBBLICA automaticamente
# quando il codice sorgente è più recente del binario, così vedi sempre le
# ultime modifiche senza doverlo fare a mano.
set -e
cd "$(dirname "$0")"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

if [ -n "$1" ]; then
  export PKHEX_AUTOLOAD="$1"
fi

# Serve una ripubblicazione? Sì se il binario manca, oppure se esiste un file
# sorgente (.cs/.axaml/.csproj) più recente del binario pubblicato.
need_build=0
if [ ! -x publish/PKHeX.Avalonia ]; then
  need_build=1
else
  newer=$(find ../PKHeX.Core ../PKHeX.Avalonia \
            -type f \( -name '*.cs' -o -name '*.axaml' -o -name '*.csproj' \) \
            -not -path '*/obj/*' -not -path '*/bin/*' -not -path '*/publish/*' \
            -newer publish/PKHeX.Avalonia -print -quit 2>/dev/null)
  [ -n "$newer" ] && need_build=1
fi

if [ "$need_build" = "1" ]; then
  echo "Codice modificato: ripubblico l'eseguibile (un attimo)…"
  dotnet publish -c Release -o publish
fi

exec ./publish/PKHeX.Avalonia
