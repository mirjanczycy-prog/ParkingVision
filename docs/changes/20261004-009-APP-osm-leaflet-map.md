---
id: 20261004-009-APP-osm-leaflet-map
date: 2026-10-04
area: APP
type: decision
author: Claude
status: applied
touches: [src/ParkingVision.Maui/ParkingVision.Maui.csproj, src/ParkingVision.Maui/MauiProgram.cs, src/ParkingVision.Maui/Services/MapHtml.cs, src/ParkingVision.Maui/Services/AppSettings.cs, src/ParkingVision.Maui/Views/MapPage.xaml, src/ParkingVision.Maui/Views/MapPage.xaml.cs, src/ParkingVision.Maui/Views/ZonesListPage.xaml, docs/07-maui-app.md, docs/10-dev-environment.md, docs/09-roadmap.md, AGENTS.md]
assumptions: [A-016]
supersedes: []
corrects: []
db_impact: none
api_impact: none
---
# Mapa: OpenStreetMap (Leaflet w WebView) zamiast map platformowych

## Podsumowanie
Usunięto `Microsoft.Maui.Controls.Maps` (Apple Maps / Google Maps). Mapa to strona Leaflet z kafelkami OpenStreetMap w `WebView`; strefy rysowane jako kółko i znacznik w kolorze dostępności, tap w dymek otwiera ekran strefy.

## Dlaczego
Klucz Google Maps wymaga konta Cloud z kartą (samo ładowanie mapy w aplikacji jest darmowe). OSM nie wymaga konta ani karty i działa tak samo na iOS i Androidzie. WebView + Leaflet zamiast biblioteki natywnej (np. Mapsui) — mniejsze ryzyko pomyłek w kodzie pisanym bez kompilatora. Alternatywy odrzucone: Mapsui (nowe zależności, API wersjonowane), Google (karta).

## Co zmieniono
- csproj: usunięty pakiet Maps; `MauiProgram`: bez `UseMauiMaps()`.
- `Services/MapHtml.cs` (strona Leaflet, mostek C#↔JS, link `pvapp://zone?id=N`), `MapPage` (WebView, `fitBounds`, panel dolny w osobnym wierszu, atrybucja widoczna).
- `AppSettings`: `MapTileUrl`, `MapBaseUrl`.
- Stopka 96 px w listach (miejsce na pływający pasek zakładek iOS 26).
- Dokumentacja: `docs/07` (sekcja „Mapa (OpenStreetMap)”), `docs/10`, `docs/09`, `AGENTS.md` (A-016).

## Verification (tylko to, co faktycznie uruchomiono)
- build: **nie uruchomiono** (autor bez SDK .NET; użytkownik jeszcze nie zbudował tej wersji).
- ręcznie: nie uruchomiono. Do sprawdzenia: przechwytywanie `pvapp://`, kafelki z `BaseUrl` (możliwy 403 z publicznego serwera OSM), `fitBounds`, zachowanie offline.

## Następne kroki / znane luki
- Mapa offline (Leaflet w `Resources/Raw`), własny dostawca kafelków do produkcji, prawdziwa domena w `MapBaseUrl`.
- Wymienić zrzuty w `docs/img` i w prezentacji na wersję z OSM.
