---
id: YYYYMMDD-NNN-AREA-slug
date: YYYY-MM-DD
area: ARCH            # ARCH|DATA|CAM|PRC|PKM|API|APP|SIM|ROAD|OPS|DOC
type: feature         # feature|fix|refactor|decision|docs|ops
author: <nazwa modelu lub osoby>
status: applied       # applied|proposed|reverted
touches: []           # ścieżki plików / dokumentów, np. [src/ParkingVision.Core/Services.cs, docs/04-processing.md]
assumptions: []       # dotknięte założenia, np. [A-008, A-009]
supersedes: []        # id wcześniejszych zmian, które ta zastępuje (opcjonalnie)
corrects: []          # id wcześniejszych zmian, w których ta poprawia błąd (opcjonalnie)
db_impact: none       # none | schema change (usuń data/*.db) | migration <nazwa>
api_impact: none      # none | additive | breaking
---
# Krótki tytuł zmiany

## Podsumowanie
1–3 zdania: co się zmieniło z punktu widzenia działania systemu.

## Dlaczego
Powód, rozważone alternatywy, powiązanie z założeniami.

## Co zmieniono
- plik / komponent — opis

## Verification (tylko to, co faktycznie uruchomiono)
- build: <uruchomiono / nie uruchomiono>
- testy: <uruchomiono / nie uruchomiono>
- ręcznie: <co sprawdzono>

## Następne kroki / znane luki
- …
