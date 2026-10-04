# FALLBACK ONLY (after it, re-add Platforms/iOS/SceneDelegate.cs and UIApplicationSceneManifest, see docs/10) (since change 015 Platforms/ and basic Resources are in the repo). Generates the platform scaffolding (Platforms/, icons, splash) with the official template and copies it into
# src/ParkingVision.Maui WITHOUT overwriting files that already exist there.
# Requires: .NET SDK + MAUI workload (dotnet workload install maui).
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$tmp = Join-Path ([IO.Path]::GetTempPath()) ("pvmaui-" + [guid]::NewGuid())
dotnet new maui -n ParkingVision.Maui -o "$tmp\ParkingVision.Maui" --no-restore
$src = "$tmp\ParkingVision.Maui"; $dst = "$root\src\ParkingVision.Maui"
foreach ($d in "Platforms","Resources\AppIcon","Resources\Splash","Resources\Images","Resources\Raw","Properties") {
  if (Test-Path "$src\$d") {
    New-Item -ItemType Directory -Force "$dst\$d" | Out-Null
    robocopy "$src\$d" "$dst\$d" /E /XC /XN /XO /NFL /NDL /NJH /NJS | Out-Null   # never overwrite existing files
  }
}
Remove-Item -Recurse -Force $tmp
Write-Host "Done. Next: follow docs/07-maui-app.md section 'Platform setup'."
