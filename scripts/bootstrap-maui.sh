#!/usr/bin/env bash
# FALLBACK ONLY (after it, re-add Platforms/iOS/SceneDelegate.cs and UIApplicationSceneManifest, see docs/10) (since change 015 Platforms/ and basic Resources are in the repo). Generates the platform scaffolding (Platforms/, icons, splash) with the official template and copies it into
# src/ParkingVision.Maui WITHOUT overwriting any file that already exists there.
# Requires: .NET SDK + MAUI workload (`dotnet workload install maui`).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TMP="$(mktemp -d)"
dotnet new maui -n ParkingVision.Maui -o "$TMP/ParkingVision.Maui" --no-restore
SRC="$TMP/ParkingVision.Maui"
DST="$ROOT/src/ParkingVision.Maui"
for d in Platforms Resources/AppIcon Resources/Splash Resources/Images Resources/Raw Properties; do
  [ -d "$SRC/$d" ] && mkdir -p "$DST/$d" && cp -rn "$SRC/$d/." "$DST/$d/"
done
rm -rf "$TMP"
echo "Done. Next: follow docs/07-maui-app.md section 'Platform setup' (Google Maps key, location permission, cleartext HTTP)."
