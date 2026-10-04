---
id: 20261004-015-APP-platforms-in-repo
date: 2026-10-04
area: APP
type: ops
author: Claude
status: applied
touches: [src/ParkingVision.Maui/Platforms/**, src/ParkingVision.Maui/Resources/AppIcon/*, src/ParkingVision.Maui/Resources/Splash/*, src/ParkingVision.Maui/ParkingVision.Maui.csproj, scripts/bootstrap-maui.sh, scripts/bootstrap-maui.ps1, README.md, AGENTS.md, docs/07-maui-app.md, docs/10-dev-environment.md]
assumptions: [A-018]
supersedes: []
corrects: [20261003-003-APP-maui-ux-v0]
db_impact: none
api_impact: none
---
# Platforms/ i podstawowe zasoby MAUI w repozytorium

## Podsumowanie
Projekt MAUI zawiera teraz własne `Platforms/iOS`, `Platforms/Android`, ikonę i splash, więc nie wymaga kroku `bootstrap-maui` i można go podmienić w całości bez ręcznych poprawek. Wpisy lokalizacji i HTTP do lokalnego API (Info.plist, AndroidManifest) są już ustawione.

## Dlaczego
Podmiana folderu `src/ParkingVision.Maui` na nowszą paczkę kasowała wygenerowane zasoby i kończyła się błędem `MSB3954` (brak `splash.svg`); po każdej aktualizacji trzeba było ponawiać bootstrap i ręcznie wpisywać klucze w plikach platform.

## Co zmieniono
- `Platforms/iOS` (AppDelegate, Program, Info.plist z kluczami lokalizacji, sieci lokalnej i ATS), `Platforms/Android` (MainActivity, MainApplication, AndroidManifest z uprawnieniami i cleartext, colors.xml) — napisane ręcznie wg szablonu MAUI.
- `Resources/AppIcon` (asfalt + żółta litera P), `Resources/Splash`.
- csproj: cele tylko `net10.0-android` i `net10.0-ios` (Mac Catalyst usunięty, nigdy nie testowany); usunięty wpis `MauiImage` (brak obrazów, a plik kropkowy np. `.gitkeep` zepsułby resizetizer).
- Skrypty bootstrap oznaczone jako awaryjne.

## Verification (tylko to, co faktycznie uruchomiono)
- Wszystkie pliki XML/XAML/plist/svg/csproj przechodzą parsowanie XML (sprawdzone w środowisku autora).
- Budowanie i uruchomienie: **nie uruchomiono**. Pliki platform są odtworzone z pamięci szablonu MAUI — jeśli build zgłosi błąd w `Platforms/`, awaryjnie usunąć ten katalog i uruchomić `scripts/bootstrap-maui.*`, a potem dopisać wpisy z `docs/07`.

## Następne kroki / znane luki
- Prawdziwa ikona i splash (obecne są tymczasowe); Mac Catalyst i Windows, jeśli będą potrzebne.
