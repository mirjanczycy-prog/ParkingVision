# 08 — Symulacja kamer i parkomatów

> Dokument żywy. Kod: `src/ParkingVision.Simulator`, `Data/DemoSeeder.cs`. Założenia: A-007, A-012.

## Idea
Symulator **nie pisze do tabel „na skróty”**. Udaje urządzenia i woła te same interfejsy co prawdziwe komponenty:
- kamery → `IObservationIngest` (tak jak `CameraWorker`),
- parkomaty → `IParkomatIngest` (tak jak import biletów).

Dzięki temu fuzja, wygasanie, szacowanie z biletów, API i aplikacja zachowują się identycznie jak na prawdziwych danych (A-007).

## Dwa poziomy symulacji
| Poziom | Co testuje | Jak |
|---|---|---|
| **Dane (domyślny)** | platformę + aplikację (bez wideo, bez modelu) | `ParkingVision.Simulator` |
| **Obraz** | detektor + ROI + debounce na realnym wideo | kamera w bazie z `IsSimulated=false`, `StreamUrl` = ścieżka do pliku `.mp4` (zapętlany), uruchomić `CameraWorker`; ROI trzeba skalibrować pod to nagranie |

## Uruchomienie (poziom „dane”)
```bash
dotnet run --project src/ParkingVision.Api                        # tworzy bazę i seeduje dane demo
dotnet run --project src/ParkingVision.Simulator -- run           # symulacja w czasie rzeczywistym x30
```
Uwaga: bez `--start-hour` symulator startuje od bieżącej godziny — w nocy zajętość jest niska. Do pokazu użyj `--start-hour 9`.
Polecenia: `run` (domyślne), `seed` (tylko topologia), `reset` (usuń bazę, zasiej od nowa, zakończ).
Opcje: `--speed 30` (minuty symulowane na minutę rzeczywistą), `--scenario normal|busy|quiet`, `--start-hour 8`, `--offline CAM-B2[,CAM-C1]`, `--noise 0.01` (błędy detekcji), `--db ścieżka`.
Baza domyślnie: `<repo>/data/parkingvision.db` (lub `PV_DB_PATH`); API i symulator dzielą plik (SQLite WAL).

Komendy w trakcie działania (stdin): `offline CAM-B2`, `online CAM-B2`, `speed 60`, `busy`, `quiet`, `normal`, `status`, `q`.

## Model symulacji
- **Miejsce** (agent): zajęte/wolne. Przyjazdy: proces Poissona o intensywności dobranej tak, by w stanie ustalonym zajętość = `Target(godzina, typ, scenariusz)`. Czas postoju: rozkład wykładniczy (średnio 90 symulowanych minut, 10–360).
- **Krzywa dobowa:** noc ~10–15%, poranek 30→75%, dzień 85–90%, wieczór maleje. Mnożniki typu: motocykl ×0.6, niepełnosprawni ×0.5, elektryczne ×0.7. Scenariusze: `busy` ≥ 95%, `quiet` ×0.25.
- **Kamera** (obserwator): raportuje zmianę po `CameraDelaySeconds` (odpowiednik debouncera), keep-alive co 20 s; opcjonalny szum (`--noise`) odwraca pojedynczy odczyt. Kamery pokrywające się (B-09, B-10) raportują niezależnie — widać fuzję.
- **Parkomat:** przy przyjeździe z prawdopodobieństwem `PayRate` (0.80) powstaje bilet w jednym z parkomatów obsługujących to miejsce; ważność = czas postoju × U(0.75, 1.25) — część biletów wygasa przed odjazdem (symulacja „bez ważnego biletu”). Kwota: 6 zł/h, zaokrąglana do 0.5 h.
- **Skala czasu:** `--speed N` skraca postoje i ważność biletów N razy w czasie rzeczywistym, więc „godzina” w symulacji trwa 60/N minut. Znaczniki czasu w bazie są rzeczywiste (UTC).
- **Start „ciepły”:** po uruchomieniu miejsca są od razu zajęte według krzywej, kamery wysyłają pełny stan, bilety istnieją — aplikacja ma dane natychmiast.

## Scenariusz prezentacji (≈ 5 minut)
1. `reset` → uruchom API → uruchom symulator `--speed 30 --start-hour 8`. W aplikacji (lokalizacja demo włączona) widać 3 strefy.
2. Pokaż strefę A: pewność **Na żywo**, pasek miejsc zmienia się co kilkanaście sekund.
3. Pokaż strefę C: **Częściowo na żywo** — 4 miejsca szacowane z biletów (komunikat w szczegółach).
4. W konsoli symulatora: `offline CAM-C1` → po ~3 min (TTL) strefa C przechodzi w **Szacunek**, miejsca w siatce stają się „?”. `online CAM-C1` → wraca.
5. `busy` → strefy czerwienieją; `quiet` → zielenieją. Filtr „Motocykl” pokazuje tylko strefę B.
6. `GET /api/admin/coverage` — pokaż relacje parkomat↔kamera.

## Ograniczenia
Symulator nie odwzorowuje ruchu przejeżdżającego, zasłaniania ani pogody; szum jest uproszczony. Dane demo są fikcyjne (A-012).
