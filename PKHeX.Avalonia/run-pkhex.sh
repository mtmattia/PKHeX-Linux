#!/usr/bin/env bash
# Avvia PKHeX.Avalonia (port nativo Linux).
# Uso: ./run-pkhex.sh [percorso-save-da-aprire]
#
# Usa l'eseguibile pubblicato (avvio veloce). Se manca, lo ripubblica.
# Per ricompilare dopo modifiche al codice: dotnet publish -c Release -o publish
set -e
cd "$(dirname "$0")"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
if [ -n "$1" ]; then
  export PKHEX_AUTOLOAD="$1"
fi
if [ ! -x publish/PKHeX.Avalonia ]; then
  echo "Prima esecuzione: pubblico l'eseguibile ottimizzato (un attimo)…"
  dotnet publish -c Release -o publish
fi
exec ./publish/PKHeX.Avalonia
