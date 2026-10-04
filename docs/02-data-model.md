# 02 — Model danych

> Dokument żywy. Kod: `Core/Entities.cs`, `Data/ParkingDbContext.cs`. Założenia: A-001, A-002, A-006, A-010.

## Zasada naczelna: miejsce jest osią
```
Zone 1──N Spot N──M Parkomat      (tabela łącząca ParkomatSpot)
          Spot N──M Camera        (tabela łącząca CameraSpot + ROI + Weight)
```
**Kamera↔Parkomat nie ma własnej tabeli.** Jedna kamera może obejmować miejsca kilku parkomatów, a jeden parkomat może mieć miejsca widziane przez kilka kamer — odpowiedź zawsze wynika z miejsc (`TopologyService`, `GET /api/admin/coverage`).

## Tabele

| Tabela | Klucz | Najważniejsze pola |
|---|---|---|
| `Zone` | `Id`, unikalny `Code` | Name, City, CenterLat/Lon, TariffInfo |
| `Spot` | `Id`, unikalny `Code` | ZoneId, Type, Lat, Lon, StreetName |
| `Parkomat` | `Id`, unikalny `Code` | ZoneId, Name, Lat, Lon |
| `ParkomatSpot` | (`ParkomatId`,`SpotId`) | — (które miejsca obsługuje parkomat) |
| `Camera` | `Id`, unikalny `Code` | StreamUrl (`rtsp://…`, plik lub `env:ZMIENNA`), IsSimulated, Enabled, Status, LastHeartbeatUtc |
| `CameraSpot` | (`CameraId`,`SpotId`) | **RoiJson** (znormalizowany wielokąt), **Weight** (waga głosu), Enabled |
| `SpotObservation` | `Id` (long) | CameraId, SpotId, TimestampUtc, Occupied, Confidence, VehicleClass. Append-only, czyszczone po `ObservationRetentionHours` |
| `SpotState` | `SpotId` | Status, Confidence, Source (Camera/Estimated/Manual/None), SinceUtc, UpdatedUtc. Jeden wiersz na miejsce |
| `ParkomatTicket` | `Id` (long), unikalny (`ParkomatId`,`ExternalTicketId`) | ZoneId, IssuedUtc, ValidToUtc, Amount, PlateHash?, SpotId? |

### ROI
`RoiJson` = `[[x,y],[x,y],…]`, współrzędne 0..1 względem szerokości/wysokości klatki — niezależne od rozdzielczości. Minimum 3 punkty, wielokąt poprawny (inaczej miejsce jest pomijane i logowane).

### Weight
Domyślnie 1.0. Gdy dwie kamery widzą to samo miejsce, kamera z lepszym kątem dostaje wyższą wagę.

## Przykładowa topologia demo (`DemoSeeder`)
56 miejsc, 3 strefy, 4 kamery, 6 parkomatów. Pokazuje wszystkie kształty relacji:

| Strefa | Miejsca | Parkomaty (miejsca) | Kamery (miejsca) | Co ilustruje |
|---|---|---|---|---|
| `KRK-A` ulica | 12 aut | PM-A1: A-01..07, PM-A2: A-06..12 (A-06, A-07 wspólne) | CAM-A1: wszystkie 12 | **1 kamera → 2 parkomaty**, miejsca obsługiwane przez 2 parkomaty |
| `KRK-B` plac | 16 aut + 4 motocykle | PM-B1: wszystkie 20 | CAM-B1: B-01..10 + B-M1..2 (12); CAM-B2: B-09..16 + B-M3..4 (10); wspólne B-09, B-10 | **2 kamery → 1 parkomat**, nakładanie się kamer, motocykle |
| `KRK-C` parking | 20 aut + 2 niepełnospr. + 2 elektryczne | PM-C1: C-01..08, PM-C2: C-09..16, PM-C3: C-17..20 + D1, D2, E1, E2 | CAM-C1: 20 miejsc (bez C-17..20) | **1 kamera → 3 parkomaty**, **4 miejsca bez kamery** (szacunek z biletów) |

Oczekiwany wynik `GET /api/admin/coverage` dla PM-C3: 8 miejsc, 4 bez kamery, kamera CAM-C1 obejmuje 4.

## Reguły integralności
- Kody (`Code`) są stabilne i unikalne; służą jako klucze integracyjne (A-002).
- Ingest odrzuca elementy dla miejsc niepowiązanych z daną kamerą (zabezpieczenie przed błędną konfiguracją urządzenia).
- Wszystkie `DateTime` w UTC; konwertery EF wymuszają `Kind=Utc` po odczycie z SQLite (A-006).
- Kwoty `decimal` — w SQLite zapisywane jako tekst; nie agregować `Sum` po stronie bazy.

## Zmiany schematu
Prototyp używa `EnsureCreated`. Zmiana schematu = usunięcie `data/parkingvision.db` (lub `Simulator -- reset`) i wpis `db_impact: schema change` w pliku zmiany. Przed produkcją: migracje EF + PostgreSQL (A-005).
