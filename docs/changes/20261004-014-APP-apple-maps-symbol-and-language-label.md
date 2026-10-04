---
id: 20261004-014-APP-apple-maps-symbol-and-language-label
date: 2026-10-04
area: APP
type: fix
author: Claude
status: applied
touches: [src/ParkingVision.Maui/ParkingVision.Maui.csproj, src/ParkingVision.Maui/MauiProgram.cs, src/ParkingVision.Maui/Views/Maps/IZoneMap.cs, src/ParkingVision.Maui/Views/Maps/AppleZoneMap.cs, src/ParkingVision.Maui/ViewModels/SettingsViewModel.cs, src/ParkingVision.Maui/Views/SettingsPage.xaml, docs/07-maui-app.md, docs/10-dev-environment.md]
assumptions: [A-017, A-018]
supersedes: []
corrects: [20261004-012-APP-map-per-platform, 20261004-013-APP-fix-language-refresh]
db_impact: none
api_impact: none
---
# Własna stała PV_APPLE_MAPS i etykieta „Język” liczona w VM

## Podsumowanie
Dwa objawy z uruchomienia na symulatorze iPad: (1) iOS nadal pokazywał OpenStreetMap zamiast Apple Maps, (2) po przełączeniu en→pl słowo „Language” zostało nad pickerem.

## Dlaczego
(1) Dwie możliwe przyczyny: na dysku nadal była stara wersja strony mapy (bez `Views/Maps`) albo blok `#if IOS || MACCATALYST` nie zadziałał i fabryka zwróciła Leaflet. Obie usuwa wspólna stała `PV_APPLE_MAPS` zdefiniowana w csproj tam, gdzie dodawany jest pakiet Maps, więc kod i pakiet nie mogą się rozjechać. Nie ustalono, która przyczyna wystąpiła u użytkownika.
(2) Wiązanie XAML `{loc:T}` nie odświeżyło tej jednej etykiety; teksty liczone w VM odświeżały się poprawnie, więc ta etykieta też jest liczona w VM (`LanguageLabel`). Przyczyna rozbieżności nie została ustalona.

## Co zmieniono
- csproj: `DefineConstants += PV_APPLE_MAPS` dla TFM `-ios` i `-maccatalyst`; `#if` w `IZoneMap`, `AppleZoneMap`, `MauiProgram` używają tej stałej.
- `SettingsViewModel.LanguageLabel` + zmiana w `SettingsPage.xaml`.
- `docs/07`, `docs/10`.

## Verification (tylko to, co faktycznie uruchomiono)
- Objawy (1) i (2) potwierdzone relacją użytkownika z symulatora iPad.
- Poprawki: nie zbudowane ani nie uruchomione.

## Następne kroki / znane luki
- Jeśli po tej zmianie kolejne etykiety nie zmieniają języka, przenieść je do VM (jak `LanguageLabel`) albo ustawić w kodzie po `SetLanguage`.
