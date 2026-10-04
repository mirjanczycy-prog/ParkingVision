# 03 — Oprogramowanie kamer

> Dokument żywy. Kod: `src/ParkingVision.Vision`, `src/ParkingVision.CameraWorker`. Założenia: A-003, A-004, A-011.

## Pipeline jednej kamery
```
CaptureFrameSource (wątek: RTSP/plik, trzyma TYLKO najnowszą klatkę)
  -> co IntervalSeconds: GetLatest()
  -> YoloDetector.Detect()        // letterbox 640, ONNX, klasy pojazdów, NMS jeśli potrzebny
  -> SpotEvaluator.Evaluate()     // detekcje -> odczyt per miejsce (ROI)
  -> SpotDebouncer.Update()       // stan zmienia się po StableSeconds stabilnego odczytu
  -> zmiana lub keep-alive (KeepAliveSeconds) -> IObservationIngest.IngestAsync()
```
Parkowanie zmienia się powoli — 1 klatka na kilka sekund wystarcza, nie potrzeba mocnego sprzętu.

## Detektor (bez własnego uczenia — A-003)
- Gotowy model COCO wyeksportowany do ONNX. Klasy pojazdów (COCO): 1 rower, 2 auto, 3 motocykl, 5 autobus, 7 ciężarówka (`VisionOptions.VehicleClassIds`).
- Eksport (jednorazowo, Python):
  ```bash
  pip install ultralytics
  yolo export model=yolo11s.pt format=onnx imgsz=640 simplify=True
  # wynik: yolo11s.onnx  ->  skopiuj do models/yolo11s.onnx
  ```
- `YoloDetector` rozpoznaje układ wyjścia po kształcie tensora:
  - `[1, N, 6]` (x1,y1,x2,y2,score,class) — modele „end-to-end” (np. YOLOv10, nowsze); NMS już zrobiony;
  - `[1, 4+nc, A]` (cx,cy,w,h + wyniki klas) — klasyczne YOLOv8/YOLO11; NMS wykonywany w C#.
  - Inny układ (np. RT-DETR) → wyjątek z kształtem; dodać obsługę w `YoloDetector` i opisać w pliku zmiany.
- **Licencja:** modele Ultralytics są na AGPL-3.0 albo licencji komercyjnej. Przed wdrożeniem komercyjnym zweryfikować lub użyć modelu o licencji permisywnej.
- GPU: `Microsoft.ML.OnnxRuntime.Gpu` (CUDA) lub DirectML. Na CPU model klasy „s” przy 1 klatce/3 s zwykle wystarcza.

## Ocena miejsc (`SpotEvaluator`)
1. Dla każdej detekcji liczony jest **footprint** = dolna połowa jej prostokąta (dach auta przy widoku z boku zachodzi na sąsiednie miejsca, podstawa nie).
2. Dla każdego ROI: `coverage = pole(ROI ∩ footprint) / pole(ROI)`.
3. **Każda detekcja trafia do jednego miejsca** — tego z największym `coverage`. Dzięki temu jedno auto nie zajmuje dwóch miejsc.
4. Miejsce jest `Occupied`, gdy `coverage ≥ CoverageThreshold` (0.30). Pewność = score detekcji. Dla `Free` pewność to stała heurystyczna `FreeConfidence` (0.85) — brak detekcji nie ma „score”.

## Stabilizacja (`SpotDebouncer`)
Nowy stan musi utrzymać się nieprzerwanie `StableSeconds` (30 s), zanim zostanie raportowany. Przejeżdżające auto, przechodzień, otwarte drzwi nie przestawiają stanu. Pierwszy odczyt inicjuje stan natychmiast.

## Wysyłanie
- Zmiana stanu → natychmiast; bez zmiany → keep-alive co `KeepAliveSeconds` (60 s), żeby platforma wiedziała, że kamera żyje (A-015).
- Brak świeżej klatki dłużej niż `FrameStaleSeconds` → kamera uznana za offline, **nic nie jest raportowane**; stany po TTL staną się `Unknown`.

> **Nie zweryfikowane na macOS:** csproj zawiera natywny pakiet OpenCV tylko dla Windows; na Macu potrzebna biblioteka natywna (pakiet runtime dla macOS lub OpenCV z Homebrew). Patrz `10-dev-environment.md`.

## Konfiguracja (`CameraWorker/appsettings.json`, sekcja `Vision`)
| Klucz | Domyślnie | Znaczenie |
|---|---|---|
| ModelPath | `../../models/yolo11s.onnx` | ścieżka względem katalogu aplikacji |
| MinScore | 0.40 | minimalna pewność detekcji |
| CoverageThreshold | 0.30 | pokrycie ROI uznawane za zajęte |
| IntervalSeconds | 3 | okres próbkowania |
| StableSeconds | 30 | czas stabilizacji zmiany |
| KeepAliveSeconds | 60 | okres keep-alive |

Kamery definiuje baza (`Camera`): worker uruchamia kamery z `Enabled=true`, `IsSimulated=false`, `StreamUrl` ustawionym. `StreamUrl`: `rtsp://…`, ścieżka do pliku wideo (zapętlany — wygodne do testów detektora) lub `env:NAZWA` (wartość z zmiennej środowiskowej — hasła RTSP nie trafiają do bazy). Dla RTSP wymuszany jest transport TCP.

## Kalibracja ROI
Seeder wstawia **wartości zastępcze** (równe trapezy). Dla prawdziwej kamery trzeba dla każdego miejsca zaznaczyć wielokąt na klatce referencyjnej i zapisać znormalizowane punkty w `CameraSpot.RoiJson`. Narzędzie kalibracji: roadmapa (`09`). Zasady: ROI obejmuje obszar, na którym stoi koło/podstawa auta; ROI sąsiednich miejsc nie nakładają się; motocykl ma mniejszy ROI.

## Sprzęt (orientacyjnie — zweryfikować na miejscu)
- Kamera IP z RTSP, 4–8 Mpx, obiektyw 2,8–4 mm, IR; montaż 5–8 m (latarnia, słup); im wyżej i bardziej „z góry”, tym mniej zasłaniania.
- Zasięg: jedna kamera 4K z dobrego punktu obejmuje zwykle 10–20 miejsc przy ulicy (parkowanie równoległe) lub 15–30 na placu; przy 1080p realnie 6–10.
- Obliczenia: mini-PC, Jetson Orin Nano lub Raspberry Pi 5 z akceleratorem Hailo; zasilanie PoE; łączność LTE przy braku światłowodu.
- Radar mmWave i magnetometry w nawierzchni: alternatywy punktowe tam, gdzie kamera nie ma ujęcia (motocykle słabo wykrywalne magnetometrem).

## Prywatność (RODO) — wymagania projektowe
Obraz przetwarzany lokalnie; nie zapisujemy klatek ani tablic (A-004, A-011); wychodzą tylko stany. Wdrożenie w przestrzeni publicznej wymaga ustalenia podstawy prawnej i obowiązku informacyjnego.

## Tryby awarii
| Zjawisko | Skutek | Zabezpieczenie |
|---|---|---|
| Noc / deszcz / śnieg | spadek pewności detekcji | IR, próg `MinScore`, debounce; ocena na danych z miejsca |
| Auto zasłania inne | błędny odczyt | footprint, przypisanie 1 detekcja→1 miejsce, nakładające się kamery + fuzja |
| Przerwany strumień | brak klatek | auto-reconnect; po `FrameStaleSeconds` brak raportów -> `Unknown` po TTL |
| Zły ROI | stałe błędy | walidacja wielokąta przy starcie; kalibracja |
