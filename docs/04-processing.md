# 04 — Przetwarzanie danych

> Dokument żywy. Kod: `Core/Services.cs` (`SpotFusion`), `Data/Ingest.cs`, `MaintenanceService.cs`, `AvailabilityService.cs`. Założenia: A-007..A-010, A-015.

## 1. Ingest obserwacji (`ObservationIngest`)
Wejście: `ObservationBatchDto { CameraCode, TimestampUtc, Items[ {SpotCode, Occupied, Confidence, VehicleClass} ] }`.
1. Znajdź kamerę po `Code` (brak → `KeyNotFoundException`, API zwraca 404).
2. `Camera.LastHeartbeatUtc = TimestampUtc`, `Status = Online`.
3. Zostaw tylko miejsca powiązane z kamerą (`CameraSpot.Enabled`); resztę pomiń.
4. Dopisz wiersze `SpotObservation`.
5. Przelicz `SpotState` dla dotkniętych miejsc (fuzja, p. 2).

## 2. Fuzja (`SpotFusion.Fuse`) — A-009
Dla miejsca bierzemy **najnowszą obserwację z każdej kamery**, która je obejmuje, i odrzucamy starsze niż TTL (`ObservationTtlSeconds`, 180 s).
- brak świeżych obserwacji → `Unknown`, pewność 0;
- `occ = Σ(confidence·weight)` dla `Occupied`, `free = Σ(confidence·weight)` dla `Free`;
- `occ ≥ free` → `Occupied` (remis = zajęte), inaczej `Free`;
- pewność = `max(occ,free)/(occ+free) × średnia confidence`.

`SpotState.SinceUtc` zmienia się tylko, gdy zmienia się status. `Source = Camera` (lub `None` dla `Unknown`).

## 3. Wygasanie i sprzątanie (`MaintenanceService`, co 15 s w API)
- Stan z `Source=Camera` starszy niż TTL → `Unknown` (A-008, A-015).
- Kamera `Online` bez keep-alive dłużej niż 2×TTL → `Offline`.
- Usuń `SpotObservation` starsze niż `ObservationRetentionHours` (24 h) i bilety wygasłe > 7 dni temu.

## 4. Dostępność strefy (`AvailabilityService`) — heurystyka v0
Dla strefy (opcjonalnie zawężonej do `SpotType`):

```
Total        = liczba miejsc (po filtrze typu)
FreeLive     = miejsca z Source=Camera i Status=Free
OccupiedLive = miejsca z Source=Camera i Status=Occupied
Uncovered    = Total - FreeLive - OccupiedLive         // brak żywych danych (brak kamery, offline, Unknown)
ActiveTickets= bilety strefy z IssuedUtc <= teraz < ValidToUtc
EstOccUncov  = min(Uncovered, round(ActiveTickets * Uncovered / liczba_wszystkich_miejsc_strefy))
EstFreeUncov = Uncovered - EstOccUncov
FreeTotalEstimate = FreeLive + EstFreeUncov
```
Założenie: bilety rozkładają się proporcjonalnie na wszystkie miejsca strefy; stosujemy je do miejsc bez żywych danych (A-010).

| Warunek | `Confidence` |
|---|---|
| `Total = 0` | NoData |
| `Uncovered = 0` | **Live** |
| są żywe dane i `Uncovered > 0` | **Mixed** |
| brak żywych danych, są aktywne bilety | **Estimated** |
| brak żywych danych i brak biletów | **NoData** (i `EstFreeUncov = 0` — nie udajemy, że wszystko wolne) |

Znane ograniczenia: brak uwzględnienia darmowych postojów, abonamentów, mandatów, aut bez biletu (symulator generuje ~20% takich); szacunek traktować orientacyjnie — UI oznacza go „~” i etykietą „Szacunek”. Zmiana heurystyki = plik zmiany `PRC` + aktualizacja tego dokumentu.

## 5. Dystans i sortowanie
`DistanceM` strefy = najmniejsza odległość (haversine) od punktu zapytania do dowolnego jej miejsca (po filtrze typu). Lista sortowana rosnąco; filtr `radiusM` stosowany do tej odległości.
