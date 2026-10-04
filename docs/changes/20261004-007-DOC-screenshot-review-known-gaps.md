---
id: 20261004-007-DOC-screenshot-review-known-gaps
date: 2026-10-04
area: DOC
type: docs
author: Claude
status: applied
touches: [docs/07-maui-app.md]
assumptions: [A-008]
supersedes: []
corrects: []
db_impact: none
api_impact: none
---
# Przegląd zrzutów: zaobserwowane luki UI

## Podsumowanie
Na podstawie czterech zrzutów z symulatora iPada spisano luki UI, żeby kolejne zmiany miały konkretną listę.

## Dlaczego
Zrzuty pokazują rzeczy niewidoczne w kodzie: kadr mapy, nakładanie się panelu i paska zakładek, błędy odmiany po polsku.

## Co zmieniono
- `docs/07` — dopisane pozycje: kadr mapy (widać tylko strefę B; hipoteza przyczyny nieprzetestowana), panel dolny pod paskiem zakładek, liczba mnoga („1 stref”), układ iPad, słaba afordancja „Sprawdź połączenie”, tekst podpowiedzi tylko o Androidzie.

## Verification (tylko to, co faktycznie uruchomiono)
- Analiza zrzutów; poprawek w kodzie nie wprowadzono i nie testowano.

## Następne kroki / znane luki
- Naprawa każdej pozycji jako osobna zmiana `APP`.
