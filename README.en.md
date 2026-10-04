# ParkingVision

**Free parking spaces, live** — from camera images and parking meter data.

[Wersja polska](README.md)

The driver sees which nearby zone has free spaces in the app and **where that number comes from** (live, partly live, estimate). Vehicle detection uses a ready-made model (YOLO, ONNX, C#) — no custom model training. The whole chain can be demonstrated without hardware, because a **simulator** stands in for cameras and parking meters.

<table>
<tr>
<td><img src="docs/img/app-map-en.png" alt="Map with zones" width="360"></td>
<td><img src="docs/img/app-list-en-electric.png" alt="Zone list, electric filter" width="360"></td>
</tr>
</table>

## How it works

```
camera -> vehicle detection + space polygons -> space state ┐
parking meter -> tickets -> estimate for spaces w/o camera ─┤-> API -> app (iOS, Android)
simulator (same interfaces as cameras and meters) ──────────┘
```

- a space can belong to several parking meters and be seen by several cameras (N–M relations in the database),
- missing data is **never** shown as "free",
- images stay at the camera; only space states leave it (see [privacy](docs/PRIVACY.md)).

## Project status

| | |
|---|---|
| API, database, simulator | built and running (demo data is fictional) |
| iOS app | running on the iPad simulator (iOS 27); Polish and English |
| Android app | written, **not run yet** |
| Camera detection | written, **not tested on real footage**; accuracy not measured |

Full description, assumptions and limitations: [docs/PROJECT.md](docs/PROJECT.md) (in Polish).

## Quick start (no hardware)

Requirements: .NET 10 SDK; for the app the MAUI workload (`dotnet workload install maui`) and an Xcode version matching the iOS workload (macOS), or the Android SDK with an emulator. Details and known pitfalls: [docs/10-dev-environment.md](docs/10-dev-environment.md).

```bash
# 1) API (creates the database and demo data), http://localhost:5080
dotnet run --project src/ParkingVision.Api

# 2) in a second terminal: camera and parking meter simulator
dotnet run --project src/ParkingVision.Simulator -- run --speed 30 --start-hour 9

# 3) in a third one: the app on the iOS simulator
dotnet build src/ParkingVision.Maui -f net10.0-ios -t:Run

# check the data
curl "http://localhost:5080/api/zones?lat=50.06&lon=19.941&radiusM=2000"
curl  http://localhost:5080/api/admin/coverage      # meter <-> spaces <-> cameras
```

App targets: `net10.0-ios` (macOS) and `net10.0-android`. iOS 27 requires the UIScene lifecycle (included in the project; if needed: `scripts/fix-ios27-scene.sh`). In the app, switch the language under Settings → Language.

### A real camera
```bash
pip install ultralytics && yolo export model=yolo11s.pt format=onnx imgsz=640 simplify=True   # -> models/yolo11s.onnx
# in the database: Camera.IsSimulated=false, StreamUrl="env:CAM_A1_URL" (env var with the RTSP URL), calibrated CameraSpot.RoiJson
dotnet run --project src/ParkingVision.CameraWorker
```
Details: [docs/03-camera-software.md](docs/03-camera-software.md) (Polish). Note: Ultralytics models have their own license (AGPL-3.0 or commercial).

## Repository layout

| Folder | Contents |
|---|---|
| `src/ParkingVision.Core` | entities, contracts (DTOs), state fusion, UI strings (pl/en) |
| `src/ParkingVision.Data` | database (EF Core, SQLite), ingestion, zone availability |
| `src/ParkingVision.Vision` / `CameraWorker` | vehicle detection (ONNX), space polygons, camera-side process |
| `src/ParkingVision.Api` | HTTP API |
| `src/ParkingVision.Simulator` | camera and parking meter simulator |
| `src/ParkingVision.Maui` | driver app |
| `src/ParkingVision.Tests` | unit tests |
| `docs/` | documentation (Polish) |

## License

[to be added — `LICENSE` file]
