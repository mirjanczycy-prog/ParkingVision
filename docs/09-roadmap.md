# 09 — Roadmapa

> Dokument żywy. Kolejność = priorytet. Po zrealizowaniu pozycji: przenieś do historii (plik zmiany) i usuń stąd.

## Faza 0 — doprowadzić prototyp do działania (NAJPIERW)
1. ~~Build API/Simulator/MAUI~~ (zrobione 2026-10-04). Pozostało: `dotnet test`, build `Vision`/`CameraWorker` na docelowym systemie (natywny OpenCV).
2. Eksport modelu ONNX, test `YoloDetector` na zdjęciu/wideo z ulicą; dostroić `MinScore`, `CoverageThreshold`.
3. ~~Uruchomić API + symulator + aplikację MAUI~~ (iOS symulator zrobione). Pozostało: Android, prawdziwy iPhone, poprawki UI z `07` (kadr mapy, panel pod paskiem zakładek, liczba mnoga).

## Faza 1 — kamery na prawdziwych danych
- Narzędzie **kalibracji ROI**: wczytanie klatki, klikanie wielokątów, zapis do `CameraSpot.RoiJson` (CLI lub proste okno).
- Implementacja `IObservationIngest` przez HTTP (`POST /api/ingest/observations`) dla urządzeń brzegowych + kolejka lokalna przy braku łączności.
- Ewaluacja jakości: nagrania z różnych pór dnia/pogody, macierz pomyłek per miejsce.
- Obsługa dodatkowego układu wyjścia modeli (np. RT-DETR) i wybór modelu o licencji pasującej do wdrożenia (A-003).

## Faza 2 — parkomaty
- Rozpoznać źródła danych w docelowym mieście (API operatora / eksport / system miejski) i napisać adapter -> `ParkomatTicketDto`.
- Walidacja heurystyki szacowania na danych rzeczywistych; ewentualnie model zależny od pory dnia.
- (Opcjonalnie, po analizie prawnej) wykrywanie postojów bez biletu.

## Faza 3 — platforma
- PostgreSQL + migracje EF (A-005); retencja i archiwizacja.
- Uwierzytelnianie: ingest (klucze per urządzenie), admin (role), CORS zawężony, limity żądań, HTTPS.
- Monitoring: stan kamer, opóźnienia, alerty o kamerze offline.
- Wersjonowanie API (`/api/v1`).

## Faza 4 — aplikacja
- Mapa na Androidzie offline: dołączyć Leaflet do `Resources/Raw`, własny/komercyjny dostawca kafelków zamiast publicznego serwera OSM.
- Nazwy stref, ulic i taryf wg języka (API: `Accept-Language`, pola `name` per język); kolejne języki UI.
- Aktualizacja w miejscu zamiast przebudowy listy, ikony zakładek, tryb ciemny, lokalizacja tekstów.
- Powiadomienia („zwolniło się miejsce w pobliżu celu”), ulubione lokalizacje.
- Prognoza dostępności z historii (dzień tygodnia × godzina).

## Przyszłość — płatność za postój (A-013)
Zależy od **miasta i obsługiwanej w nim aplikacji/operatora** (różne API, różne warunki dostępu — do rozpoznania, np. aplikacje typu SkyCash / mPay / moBILET; zweryfikować per miasto).
Plan architektoniczny:
1. Interfejs `IParkingPaymentProvider` w Core: `StartSessionAsync(zoneCode, plate/vehicleRef, duration)`, `StopSessionAsync`, `GetSessionAsync`.
2. Adapter per operator (osobny projekt `ParkingVision.Payments.<Operator>`), wybierany po `Zone.City`/konfiguracji strefy (np. `Zone.PaymentProvider`, `Zone.PaymentZoneCode`).
3. Backend pośredniczy (aplikacja nie trzyma sekretów operatora); dane pojazdu użytkownika tylko za zgodą, lokalnie na urządzeniu lub szyfrowane.
4. W aplikacji: przycisk „Opłać postój” w szczegółach strefy (jest, nieaktywny) -> wybór czasu -> potwierdzenie -> sesja z odliczaniem i przypomnieniem.
5. Zależność prawna/biznesowa: umowy z operatorami, PSD2/regulaminy — poza zakresem technicznym.
Każdy adapter = osobny plik zmiany `ROAD`/`PAY`-owy i aktualizacja tego dokumentu.
