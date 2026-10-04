# 01 — Architektura

> Dokument żywy. Założenia: A-001, A-004, A-005, A-007.

## Komponenty

| Projekt | Rola | Zależności |
|---|---|---|
| `ParkingVision.Core` | Encje, enumy, DTO (kontrakty), interfejsy ingest, czysta logika fuzji i geodezji | — |
| `ParkingVision.Data` | EF Core (SQLite), implementacje ingest, `AvailabilityService`, `TopologyService`, `MaintenanceService`, `DemoSeeder` | Core |
| `ParkingVision.Vision` | `YoloDetector` (ONNX Runtime), `SpotEvaluator` (ROI), `SpotDebouncer`, `CaptureFrameSource` (RTSP/plik) | OpenCvSharp, NetTopologySuite, ONNX Runtime |
| `ParkingVision.CameraWorker` | Proces przy kamerach: jeden pipeline na kamerę, wysyła stany do ingest | Data, Vision |
| `ParkingVision.Api` | HTTP: odczyt dla aplikacji, ingest dla urządzeń, endpointy diagnostyczne, sprzątanie w tle | Data |
| `ParkingVision.Simulator` | Symuluje kamery i parkomaty; zasila bazę przez te same interfejsy | Data |
| `ParkingVision.Maui` | Aplikacja dla kierowców (Android/iOS) | Core (DTO) |
| `ParkingVision.Tests` | xUnit: debouncer, fuzja, ewaluator ROI | Core, Vision |

## Diagram wdrożenia (docelowy)
```
[kamera IP]--RTSP-->[mini-PC / Jetson / RPi5+Hailo: CameraWorker]--HTTPS (X-Api-Key)-->[API]--[PostgreSQL]
[parkomat/system miejski]--eksport/API-->[import biletów]-------->[API]                    |
                                                                                       [aplikacja MAUI]
```
W prototypie wszystkie procesy mogą działać na jednym komputerze i dzielić jeden plik SQLite (tryb WAL).

## Dlaczego tak
- **Ingest jako interfejs** (A-007): symulator, worker i API wywołują te same metody; różnią się tylko źródłem.
- **Edge processing** (A-004): wideo nie opuszcza urządzenia; mniejszy ruch, prostsza zgodność z RODO.
- **Singletony + `IDbContextFactory`**: serwisy używane z wielu wątków/procesów nie trzymają długich kontekstów.
- **Core wspólne z MAUI**: DTO nie rozjadą się między API a aplikacją.

## Tryb komunikacji kamera→platforma
Aktualnie `CameraWorker` zapisuje do bazy przez `IObservationIngest` (ten sam proces/komputer co baza). Docelowo urządzenie brzegowe powinno wołać `POST /api/ingest/observations` (endpoint istnieje); wystarczy dodać implementację `IObservationIngest` opartą o HTTP (patrz `09-roadmap.md`).

## Ograniczenia znane w v0
- SQLite i `EnsureCreated` (bez migracji).
- Brak uwierzytelniania użytkowników; ingest chroniony prostym kluczem `X-Api-Key`; CORS otwarty.
- `CameraWorker` czyta konfigurację kamer raz przy starcie.
