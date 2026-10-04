# 05 — Dane z parkomatów

> Dokument żywy. Kod: `Data/Ingest.cs` (`ParkomatIngest`), `Data/TopologyService.cs`. Założenia: A-001, A-010, A-011, A-014.

## Co mamy z parkomatu
Bilet: `ParkomatCode`, `ExternalTicketId`, `IssuedUtc`, `ValidToUtc`, `Amount`, opcjonalnie `PlateHash`. **Zwykle nie wiadomo, które miejsce zajęto** (A-010) — parkomat obsługuje wiele miejsc.

## Powiązanie parkomat ↔ miejsca ↔ kamery (wymóg projektu)
Dla każdego parkomatu system zna **ile miejsc obsługuje** i **która kamera je obejmuje** (A-001):
- `ParkomatSpot` — miejsca obsługiwane przez parkomat (N–M: miejsce może być obsługiwane przez kilka parkomatów, parkomat przez wiele miejsc);
- `CameraSpot` — miejsca widziane przez kamerę (N–M);
- widok wyliczany: `GET /api/admin/coverage` → dla parkomatu: `SpotCount`, `SpotsWithoutCamera`, lista `{CameraCode, Spots}`; `GET /api/admin/cameras` → dla kamery: miejsca i **parkomaty wyliczone przez miejsca**.

Przykład (dane demo): `PM-C3` obsługuje 8 miejsc, z czego 4 widzi `CAM-C1`, a 4 nie ma kamery. `PM-B1` obsługuje 20 miejsc widzianych przez `CAM-B1` (12) i `CAM-B2` (10), 2 wspólne.

## Pozyskiwanie biletów
Wariant docelowy zależy od operatora strefy (do rozpoznania per miasto):
1. **API / webhook operatora** — preferowane; adapter mapuje format operatora na `ParkomatTicketDto`.
2. **Eksport plikowy** (CSV/XML) importowany cyklicznie.
3. **Odpytywanie systemu miejskiego** (jeśli udostępnia).

Wszystkie warianty kończą się wywołaniem `IParkomatIngest.IngestAsync` lub `POST /api/ingest/parkomat-tickets` (nagłówek `X-Api-Key`). Adapter odpowiada za: mapowanie kodów parkomatów na `Parkomat.Code`, konwersję czasu do UTC, **brak** przekazywania tablic (najwyżej solony hash).

## Idempotencja (A-014)
Klucz `(ParkomatCode, ExternalTicketId)` — ponowne wysłanie tego samego biletu nie tworzy duplikatu; zwracana jest liczba **nowych** biletów. Nieznany `ParkomatCode` → bilet pomijany.

## Wykorzystanie
- **Szacowanie** zajętości miejsc bez żywych danych (`04-processing.md`, p. 4).
- W przyszłości: wykrywanie postojów bez biletu (porównanie `Occupied` z kamer z aktywnymi biletami w strefie) — wymaga uzgodnień prawnych, patrz roadmapa.

## Dane testowe
Symulator generuje bilety przy przyjazdach aut (domyślnie 80% płaci, długość biletu = czas postoju × losowo 0.75–1.25, część wygasa wcześniej). Patrz `08-simulation.md`.
