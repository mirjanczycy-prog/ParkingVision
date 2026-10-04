---
id: 20261003-001-ARCH-initial-prototype-scaffold
date: 2026-10-03
area: ARCH
type: feature
author: Claude
status: applied
touches: [ParkingVision.sln, Directory.Build.props, src/**, scripts/**, docs/00-09]
assumptions: [A-001, A-002, A-003, A-004, A-005, A-006, A-007, A-008, A-009, A-010, A-011, A-012, A-014, A-015]
supersedes: []
corrects: []
db_impact: schema change (initial schema, created by EnsureCreated)
api_impact: additive
---
# Initial prototype scaffold

## Podsumowanie
Pierwsza wersja całego łańcucha: kamera (YOLO/ONNX w C#) -> ingest -> fuzja -> baza -> API -> aplikacja MAUI, plus symulator kamer i parkomatów zasilający bazę.

## Dlaczego
Cel: prototyp do testów i prezentacji bez sprzętu, z modelem danych odzwierciedlającym relacje N–M między parkomatami, miejscami i kamerami (A-001).

## Co zmieniono
- `Core` — encje, DTO, interfejsy ingest, `SpotFusion`, `Geo`.
- `Data` — EF Core + SQLite (WAL), `ObservationIngest`, `ParkomatIngest`, `AvailabilityService`, `TopologyService`, `MaintenanceService`, `DemoSeeder` (56 miejsc, 3 strefy, 4 kamery, 6 parkomatów).
- `Vision` — `YoloDetector` (2 układy wyjścia), `SpotEvaluator`, `SpotDebouncer`, `CaptureFrameSource`.
- `CameraWorker` — pipeline na kamerę.
- `Api` — minimal API: odczyt, admin, ingest z kluczem.
- `Simulator` — symulacja kamer i parkomatów, sterowanie z konsoli.
- `Maui` — mapa, lista, szczegóły strefy, ustawienia; sygnaturowy `CapacityBar`.
- `Tests` — debouncer, fuzja, ewaluator ROI.
- Dokumentacja `AGENTS.md`, `docs/00`–`09`, protokół zmian.

## Verification (tylko to, co faktycznie uruchomiono)
- build: **nie uruchomiono** (brak SDK .NET w środowisku autora)
- testy: **nie uruchomiono**
- ręcznie: przejrzano kod pod kątem spójności nazw/typów między projektami; układ solucji wygenerowano skryptem.

## Następne kroki / znane luki
- Faza 0 z `docs/09-roadmap.md`: build, test, naprawa błędów kompilacji.
- Wersje pakietów NuGet są „pływające” — przyszpilić po pierwszym restore.
- Brak modelu ONNX w repo; brak narzędzia kalibracji ROI.
