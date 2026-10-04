---
id: 20261004-012-APP-map-per-platform
date: 2026-10-04
area: APP
type: decision
author: Claude
status: applied
touches: [src/ParkingVision.Maui/ParkingVision.Maui.csproj, src/ParkingVision.Maui/MauiProgram.cs, src/ParkingVision.Maui/Views/MapPage.xaml, src/ParkingVision.Maui/Views/MapPage.xaml.cs, src/ParkingVision.Maui/Views/Maps/*, docs/07-maui-app.md, docs/09-roadmap.md, docs/10-dev-environment.md, AGENTS.md, README.md]
assumptions: [A-016, A-018]
supersedes: [20261004-009-APP-osm-leaflet-map]
corrects: []
db_impact: none
api_impact: none
---
# Mapa zależna od platformy: Apple Maps na iOS, OpenStreetMap na Androidzie

## Podsumowanie
Zmiana 009 wprowadziła OpenStreetMap na obu platformach; użytkownik chciał OSM tylko na Androidzie (powód: płatny klucz Google Maps), a na iOS zostawić Apple Maps. Mapa jest teraz wybierana per platforma za interfejsem `IZoneMap`.

## Dlaczego
Apple Maps działały na iOS bez konta i klucza (zrzuty z 2026-10-04). Na Androidzie klucz Google Maps wymaga konta Cloud z kartą, więc użyto OSM (Leaflet w WebView). Alternatywy zgłoszone użytkownikowi: OSM na obu platformach (decyzja odrzucona), brak mapy na Androidzie.

## Co zmieniono
- `Views/Maps`: `IZoneMap`, `ZoneMapFactory`, `AppleZoneMap` (`#if IOS || MACCATALYST`), `LeafletZoneMap`.
- `MapPage`: kontener `MapHost` i wybór implementacji przez fabrykę; logika Leaflet przeniesiona z `MapPage` do `LeafletZoneMap`.
- csproj: pakiet `Microsoft.Maui.Controls.Maps` tylko dla TFM `-ios` i `-maccatalyst`; `MauiProgram`: `UseMauiMaps()` pod `#if IOS || MACCATALYST`.
- Kadr Apple Maps liczony do najdalszej strefy (naprawa luki ze zrzutów).
- Dokumentacja: `docs/07`, `09`, `10`, `AGENTS.md` (A-018, A-016 oznaczone jako częściowo zastąpione), `README.md`.

## Verification (tylko to, co faktycznie uruchomiono)
- build, testy, uruchomienie: **nie uruchomiono** (autor bez SDK .NET).
- Do sprawdzenia: build `net10.0-ios` (pakiet warunkowy, `#if`), build `net10.0-android` (brak pakietu Maps w tym TFM), kadr Apple Maps, `pvapp://` na Androidzie.

## Następne kroki / znane luki
- Wymienić zrzuty w `docs/img` i prezentacji po uruchomieniu.
