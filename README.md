# ParkingVision

**Wolne miejsca parkingowe na żywo** — z obrazu kamer i danych z parkomatów.

[English version](README.en.md)

Kierowca widzi w aplikacji, w której strefie w pobliżu są wolne miejsca i **skąd pochodzi ta liczba** (na żywo, częściowo, szacunek). Detekcję pojazdów robi gotowy model (YOLO, ONNX, C#) — bez trenowania własnego modelu. Całość można pokazać bez sprzętu, bo **symulator** zastępuje kamery i parkomaty.

<table>
<tr>
<td><img src="docs/img/app-map-pl.png" alt="Mapa ze strefami" width="360"></td>
<td><img src="docs/img/app-list-pl-motorcycle.png" alt="Lista stref, filtr motocykl" width="360"></td>
</tr>
</table>

## Jak to działa

```
kamera -> detekcja pojazdów + wielokąty miejsc -> stan miejsca ┐
parkomat -> bilety -> szacunek dla miejsc bez kamery ───────────┤-> API -> aplikacja (iOS, Android)
symulator (te same interfejsy co kamery i parkomaty) ───────────┘
```

- jedno miejsce może należeć do kilku parkomatów i być widziane przez kilka kamer (relacje N–M w bazie),
- brak danych **nigdy** nie jest pokazywany jako „wolne”,
- obraz zostaje przy kamerze; wychodzą tylko stany miejsc (patrz [prywatność](docs/PRIVACY.md)).

## Stan projektu

| | |
|---|---|
| API, baza, symulator | zbudowane i uruchomione (dane demo są fikcyjne) |
| Aplikacja na iOS | uruchomiona na symulatorze iPad (iOS 27); polski i angielski |
| Aplikacja na Androidzie | napisana, **nie uruchamiana** |
| Detekcja z kamer | napisana, **nie sprawdzana na prawdziwym nagraniu**; skuteczność niezmierzona |

Pełny opis, założenia i ograniczenia: [docs/PROJECT.md](docs/PROJECT.md).

## Szybki start (bez sprzętu)

Wymagania: .NET 10 SDK; dla aplikacji workload MAUI (`dotnet workload install maui`) oraz Xcode pasujący do workloadu iOS (macOS) lub Android SDK z emulatorem. Szczegóły i znane pułapki: [docs/10-dev-environment.md](docs/10-dev-environment.md).

```bash
# 1) API (tworzy bazę i dane demo), http://localhost:5080
dotnet run --project src/ParkingVision.Api

# 2) w drugim terminalu: symulator kamer i parkomatów
dotnet run --project src/ParkingVision.Simulator -- run --speed 30 --start-hour 9

# 3) w trzecim: aplikacja na symulatorze iOS
dotnet build src/ParkingVision.Maui -f net10.0-ios -t:Run

# kontrola danych
curl "http://localhost:5080/api/zones?lat=50.06&lon=19.941&radiusM=2000"
curl  http://localhost:5080/api/admin/coverage      # parkomat <-> miejsca <-> kamery
```

Cele aplikacji: `net10.0-ios` (macOS) i `net10.0-android`. Na iOS 27 aplikacja wymaga cyklu życia UIScene (jest w projekcie; w razie kłopotów: `scripts/fix-ios27-scene.sh`).

### Prawdziwa kamera
```bash
pip install ultralytics && yolo export model=yolo11s.pt format=onnx imgsz=640 simplify=True   # -> models/yolo11s.onnx
# w bazie: Camera.IsSimulated=false, StreamUrl="env:CAM_A1_URL" (zmienna z adresem RTSP), skalibrowane CameraSpot.RoiJson
dotnet run --project src/ParkingVision.CameraWorker
```
Szczegóły: [docs/03-camera-software.md](docs/03-camera-software.md). Uwaga: modele Ultralytics mają własną licencję (AGPL-3.0 lub komercyjną).

## Struktura repozytorium

| Katalog | Zawartość |
|---|---|
| `src/ParkingVision.Core` | encje, kontrakty (DTO), fuzja stanów, teksty interfejsu (pl/en) |
| `src/ParkingVision.Data` | baza (EF Core, SQLite), przyjmowanie danych, dostępność stref |
| `src/ParkingVision.Vision` / `CameraWorker` | detekcja pojazdów (ONNX), wielokąty miejsc, proces przy kamerze |
| `src/ParkingVision.Api` | API HTTP |
| `src/ParkingVision.Simulator` | symulator kamer i parkomatów |
| `src/ParkingVision.Maui` | aplikacja dla kierowców |
| `src/ParkingVision.Tests` | testy jednostkowe |
| `docs/` | dokumentacja |

## Dokumentacja

[Opis projektu i założenia](docs/PROJECT.md) · [Prywatność](docs/PRIVACY.md) · [Przegląd](docs/00-overview.md) · [Architektura](docs/01-architecture.md) · [Model danych](docs/02-data-model.md) · [Kamery](docs/03-camera-software.md) · [Przetwarzanie](docs/04-processing.md) · [Parkomaty](docs/05-parkomat-data.md) · [API](docs/06-api.md) · [Aplikacja](docs/07-maui-app.md) · [Symulacja](docs/08-simulation.md) · [Roadmapa](docs/09-roadmap.md) · [Środowisko](docs/10-dev-environment.md)

## Licencja

[do uzupełnienia — plik `LICENSE`]
