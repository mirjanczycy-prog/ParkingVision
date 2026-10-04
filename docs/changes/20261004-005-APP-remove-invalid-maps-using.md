---
id: 20261004-005-APP-remove-invalid-maps-using
date: 2026-10-04
area: APP
type: fix
author: Claude
status: applied
touches: [src/ParkingVision.Maui/MauiProgram.cs]
assumptions: []
supersedes: []
corrects: [20261003-003-APP-maui-ux-v0]
db_impact: none
api_impact: none
---
# Usunięcie nieistniejącego `using` w MauiProgram

## Podsumowanie
Build iOS zgłaszał `CS0234` dla `Microsoft.Maui.Controls.Maps.Hosting`. Usunięto ten `using`; `UseMauiMaps()` działa z pozostałych przestrzeni nazw.

## Dlaczego
Wiersz dodałem przy końcowych poprawkach 001/003 bez weryfikacji — taka przestrzeń nazw nie istnieje.

## Co zmieniono
- `MauiProgram.cs` — usunięty jeden `using`.

## Verification (tylko to, co faktycznie uruchomiono)
- build `dotnet build src/ParkingVision.Maui -f net10.0-ios`: przeszedł (użytkownik, macOS).

## Następne kroki / znane luki
- Build dla `net10.0-android` nie weryfikowany.
