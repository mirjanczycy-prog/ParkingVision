# ParkingVision

Prototyp systemu pokazującego **wolne miejsca parkingowe przy ulicach i na parkingach niestrzeżonych**: kamery z gotowym detektorem pojazdów (YOLO/ONNX, C#, bez własnego uczenia) + bilety z parkomatów → baza → API → aplikacja MAUI. Zawiera **symulator** kamer i parkomatów do testów i prezentacji.

> Status: API, symulator i aplikacja MAUI (iOS, symulator) zostały zbudowane i uruchomione. Moduł kamer (Vision/CameraWorker) i testy nie były jeszcze weryfikowane na prawdziwym modelu — patrz `AGENTS.md` §7. Środowisko macOS/iOS/Android: `docs/10-dev-environment.md`. Mapa: Apple Maps na iOS, OpenStreetMap na Androidzie (bez kluczy), UI: polski i angielski — `docs/07-maui-app.md`.
> Modele AI i współpracownicy: zacznij od **`AGENTS.md`**.

## Wymagania
.NET 10 SDK; dla aplikacji: workload MAUI (`dotnet workload install maui`), Android SDK/emulator. Dla kamer: model ONNX w `models/` i natywne biblioteki OpenCV (Windows: pakiet w csproj; Linux: odpowiedni pakiet `OpenCvSharp4.runtime.*`).

## Szybki start (bez sprzętu)
```bash
dotnet build ParkingVision.sln
dotnet test
dotnet run --project src/ParkingVision.Api                       # http://localhost:5080, seeduje dane demo
dotnet run --project src/ParkingVision.Simulator -- run          # w drugim terminalu
curl "http://localhost:5080/api/zones?lat=50.06&lon=19.941&radiusM=2000"
curl  http://localhost:5080/api/admin/coverage                   # parkomat <-> miejsca <-> kamery
```
Aplikacja: katalogi `Platforms/` (iOS, Android) i podstawowe zasoby (ikona, splash) są w repozytorium, więc `scripts/bootstrap-maui.*` nie jest potrzebny; szczegóły w `docs/07-maui-app.md`. Cele: `net10.0-ios` (macOS) i `net10.0-android`.

## Prawdziwa kamera
```bash
pip install ultralytics && yolo export model=yolo11s.pt format=onnx imgsz=640 simplify=True   # -> models/yolo11s.onnx
# w bazie: Camera.IsSimulated=false, StreamUrl="env:CAM_A1_URL" (zmienna środowiskowa z adresem RTSP), skalibrowane CameraSpot.RoiJson
dotnet run --project src/ParkingVision.CameraWorker
```
Szczegóły: `docs/03-camera-software.md`.

## Dokumentacja
`AGENTS.md` · `docs/00-overview` · `01-architecture` · `02-data-model` · `03-camera-software` · `04-processing` · `05-parkomat-data` · `06-api` · `07-maui-app` · `08-simulation` · `09-roadmap` · `10-dev-environment` · `docs/changes/` (dziennik zmian)
