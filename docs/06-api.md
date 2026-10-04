# 06 — API HTTP

> Dokument żywy. Kod: `src/ParkingVision.Api/Program.cs`, kontrakty: `Core/Contracts.cs`. Zasady zmian kontraktu: `AGENTS.md` §3 pkt 5.

Bazowy adres w dev: `http://localhost:5080` (`Urls` w `appsettings.json`). JSON camelCase, **enumy jako stringi**, daty UTC z `Z`. OpenAPI w trybie Development: `/openapi/v1.json`.

## Odczyt (aplikacja)
| Metoda | Ścieżka | Parametry | Odpowiedź |
|---|---|---|---|
| GET | `/api/health` | — | `{status, utc}` |
| GET | `/api/zones` | `lat`, `lon`, `radiusM`, `type` (SpotType) — wszystkie opcjonalne | `ZoneAvailabilityDto[]`, sortowane po odległości |
| GET | `/api/zones/{id}` | `lat`, `lon`, `type` | `ZoneDetailDto` lub 404 |

Przykład `GET /api/zones?lat=50.06&lon=19.941&radiusM=2000`:
```json
[{
  "zoneId": 3, "code": "KRK-C", "name": "Demo: parking C (dworzec)", "city": "Kraków (dane demo)",
  "lat": 50.068, "lon": 19.945, "distanceM": 812,
  "total": 24, "freeLive": 7, "occupiedLive": 13, "withoutLiveData": 4,
  "estimatedFreeAmongUncovered": 1, "freeTotalEstimate": 8,
  "activeTickets": 15, "confidence": "Mixed", "updatedUtc": "2026-10-03T10:15:42Z"
}]
```
Znaczenie pól: `04-processing.md` p. 4. W UI: `confidence=Live` → liczba bez „~”; inne → „~liczba”; `NoData` → „–”.

## Diagnostyka (admin; w v0 bez autoryzacji!)
| Metoda | Ścieżka | Odpowiedź |
|---|---|---|
| GET | `/api/admin/cameras` | `CameraInfoDto[]` — status, heartbeat, miejsca, parkomaty wyliczone |
| GET | `/api/admin/coverage` | `ParkomatCoverageDto[]` — dla parkomatu: liczba miejsc, miejsca bez kamery, udział kamer |

## Ingest (urządzenia, integracje) — nagłówek `X-Api-Key`
Klucz z `Ingest:ApiKey` (domyślnie `dev-key-change-me` — **zmienić**). Brak/zły klucz → 401.

`POST /api/ingest/observations`
```json
{ "cameraCode": "CAM-A1", "timestampUtc": "2026-10-03T10:15:42Z",
  "items": [ { "spotCode": "A-03", "occupied": true, "confidence": 0.91, "vehicleClass": "car" } ] }
```
→ `{ "accepted": 1 }`; nieznana kamera → 404.

`POST /api/ingest/parkomat-tickets`
```json
[ { "parkomatCode": "PM-A1", "externalTicketId": "T-001", "issuedUtc": "2026-10-03T10:00:00Z",
    "validToUtc": "2026-10-03T11:30:00Z", "amount": 9.0 } ]
```
→ `{ "added": 1 }` (duplikaty pomijane).

## Konfiguracja (`appsettings.json`)
`Parking:ObservationTtlSeconds`, `Parking:ObservationRetentionHours`, `Ingest:ApiKey`, `Database:Path` (lub zmienna `PV_DB_PATH`), `Demo:SeedOnStartup` (seeduje dane demo, jeśli baza pusta).

## Znane luki
Brak autoryzacji użytkowników i endpointów admin, CORS otwarty, brak limitów żądań, brak wersjonowania ścieżek (`/api/v1`). Do uzupełnienia przed wdrożeniem poza siecią lokalną.
