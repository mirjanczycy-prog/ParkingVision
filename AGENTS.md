# AGENTS.md — instrukcja dla modeli AI (i ludzi) pracujących nad ParkingVision

> Ten plik jest **punktem wejścia**. Przeczytaj go w całości, zanim cokolwiek zmienisz.
> Język dokumentacji: **polski** (terminy techniczne po angielsku). Identyfikatory i komentarze w kodzie: **angielski**. Teksty w UI aplikacji: **polski i angielski** (tabela `Core/Localization/Strings.cs`).

## 1. Czym jest projekt

ParkingVision sprawdza, **które miejsca parkingowe przy ulicach i na parkingach niestrzeżonych są wolne**, i pokazuje to kierowcom w aplikacji mobilnej.
Źródła danych: (1) **kamery** z gotowym, wstępnie wytrenowanym detektorem pojazdów (YOLO w ONNX, bez własnego uczenia), (2) **bilety z parkomatów** (uzupełnienie i szacunek tam, gdzie kamery nie sięgają).
Dane trafiają do wspólnej bazy; API udostępnia je aplikacji MAUI. Cały łańcuch da się **zasymulować** (symulator kamer i parkomatów zasila bazę), więc aplikację można testować i pokazywać bez sprzętu.

## 2. Obowiązkowa kolejność czytania

1. `AGENTS.md` (ten plik)
2. `docs/00-overview.md` — cele, zakres, słownik, status
3. `docs/changes/` — **10 najnowszych plików** (sortuj po nazwie malejąco). To historia: co i dlaczego zmieniono.
4. Dokument(y) obszaru, którego dotyczy zadanie (tabela w sekcji 5).

## 3. Zasady twarde (MUST)

1. **Każda zmiana = nowy plik w `docs/changes/`** według klucza z sekcji 4. Bez wyjątków, także dla małych poprawek i zmian samej dokumentacji.
2. **Dokumenty „żywe” (`docs/00`–`docs/09`) opisują stan *obecny*.** Zmieniasz zachowanie → w tym samym kroku aktualizujesz odpowiedni dokument żywy. Pliki w `docs/changes/` opisują *historię*.
3. **Pliki w `docs/changes/` są append-only.** Nigdy nie edytuj ani nie usuwaj istniejącego. Korekta = nowy plik z polem `corrects:` / `supersedes:`.
4. **Nie łam założeń z rejestru (sekcja 6) po cichu.** Jeśli założenie trzeba zmienić: nowy plik zmiany typu `decision`, aktualizacja rejestru w tym pliku (dopisz nowy wiersz, stary oznacz `superseded by A-0xx`, nie kasuj).
5. **Kontrakty** (`src/ParkingVision.Core/Contracts.cs`, endpointy w `docs/06-api.md`) zmieniaj wstecznie kompatybilnie (nowe pola opcjonalne). Zmiana łamiąca = pole `api_impact: breaking` + aktualizacja aplikacji MAUI w tej samej zmianie.
6. **Prywatność:** nie zapisuj klatek wideo, tablic rejestracyjnych ani danych osobowych. Z kamery wychodzą wyłącznie stany miejsc. Sekrety (URL RTSP z hasłem, klucze API) nigdy w repo ani w bazie (używaj `env:NAZWA_ZMIENNEJ`).
7. **Nie udawaj weryfikacji.** W sekcji `Verification` pliku zmiany pisz tylko to, co faktycznie uruchomiono (build, testy, ręczny przebieg). Jeśli nie uruchomiono — napisz „nie uruchomiono”.
8. **Brak danych ≠ wolne miejsce.** Nigdy nie pokazuj użytkownikowi „wolne”, gdy system nie ma danych (patrz A-008).

## 4. Klucz nazewnictwa plików zmian

```
docs/changes/YYYYMMDD-NNN-AREA-slug.md
```

| Człon | Reguła |
|---|---|
| `YYYYMMDD` | data zmiany, UTC |
| `NNN` | **globalny** numer kolejny, 3 cyfry (`001`, `002`, …), niezależny od daty. Następny = (największy istniejący) + 1. Sprawdź listing katalogu przed nadaniem. Kolizja przy równoległych gałęziach → późniejsza zmiana przyjmuje kolejny wolny numer. |
| `AREA` | jeden kod z tabeli niżej — obszar **dominujący**; pozostałe obszary wpisz w `touches:` |
| `slug` | kebab-case, angielski, do 6 słów, opisuje efekt (`add-ticket-estimation`, nie `fix`) |

Kody obszarów:

| AREA | Obszar | Dokument żywy |
|---|---|---|
| `ARCH` | architektura, struktura solucji, decyzje przekrojowe | `docs/01-architecture.md` |
| `DATA` | model danych, baza, migracje, seeding | `docs/02-data-model.md` |
| `CAM` | oprogramowanie kamer: detektor, ROI, pipeline, sprzęt | `docs/03-camera-software.md` |
| `PRC` | przetwarzanie: ingest, fuzja, wygasanie, dostępność | `docs/04-processing.md` |
| `PKM` | parkomaty: pozyskiwanie biletów, pokrycie miejsc | `docs/05-parkomat-data.md` |
| `API` | API HTTP, kontrakty, autoryzacja | `docs/06-api.md` |
| `APP` | aplikacja MAUI: UI/UX, kod, ustawienia platform | `docs/07-maui-app.md` |
| `SIM` | symulator kamer i parkomatów | `docs/08-simulation.md` |
| `ROAD` | roadmapa, płatności, integracje miejskie | `docs/09-roadmap.md` |
| `OPS` | build, CI, wdrożenie, konfiguracja, narzędzia, środowisko | `docs/10-dev-environment.md` |
| `DOC` | wyłącznie dokumentacja, bez zmiany zachowania | — |

Treść pliku zmiany: skopiuj `docs/changes/_TEMPLATE.md`. Pełny opis protokołu: `docs/changes/README.md`.

## 5. Mapa: obszar → dokument → kod

| Obszar | Dokument | Kod |
|---|---|---|
| Model danych | `docs/02-data-model.md` | `src/ParkingVision.Core/Entities.cs`, `src/ParkingVision.Data/ParkingDbContext.cs`, `DemoSeeder.cs` |
| Kamery | `docs/03-camera-software.md` | `src/ParkingVision.Vision/*`, `src/ParkingVision.CameraWorker/*` |
| Przetwarzanie | `docs/04-processing.md` | `src/ParkingVision.Core/Services.cs` (fuzja), `src/ParkingVision.Data/Ingest.cs`, `MaintenanceService.cs`, `AvailabilityService.cs` |
| Parkomaty | `docs/05-parkomat-data.md` | `Ingest.cs` (`ParkomatIngest`), `TopologyService.cs` |
| API | `docs/06-api.md` | `src/ParkingVision.Api/*`, `src/ParkingVision.Core/Contracts.cs` |
| Aplikacja | `docs/07-maui-app.md` | `src/ParkingVision.Maui/*` |
| Symulacja | `docs/08-simulation.md` | `src/ParkingVision.Simulator/*` |
| Testy | — | `src/ParkingVision.Tests/*` |
| Środowisko dev | `docs/10-dev-environment.md` | `scripts/*`, `*.csproj`, `appsettings*.json` |

## 6. Rejestr założeń (stabilne identyfikatory — odwołuj się do nich w plikach zmian)

| ID | Założenie |
|---|---|
| A-001 | **Miejsce (`Spot`) jest encją osiową.** Parkomat↔Miejsce i Kamera↔Miejsce to relacje N–M. Relacja Kamera↔Parkomat jest **wyliczana** przez miejsca, nigdy przechowywana wprost. |
| A-002 | Na granicach integracji (kamera→API, parkomat→API) kamery, miejsca i parkomaty identyfikuje stabilny **`Code`** (string), nie `Id` bazy. |
| A-003 | Detekcja pojazdów: **gotowy model COCO w ONNX**, bez trenowania własnych modeli w v0. Klasy: rower, auto, motocykl, autobus, ciężarówka. Uwaga licencyjna: modele Ultralytics są na AGPL-3.0 (lub licencja komercyjna) — przed wdrożeniem komercyjnym zweryfikować lub zamienić model. |
| A-004 | Przetwarzanie obrazu odbywa się **przy kamerze / na brzegu**. Poza urządzenie wychodzą tylko stany miejsc. Klatki nie są zapisywane. |
| A-005 | Prototyp używa **SQLite** (WAL); produkcyjnie PostgreSQL (+PostGIS). Kod unika funkcji specyficznych dla SQLite. Schemat tworzy `EnsureCreated` (bez migracji) — przed produkcją wprowadzić migracje EF. |
| A-006 | Cały czas w systemie to **UTC** (`DateTime` z `Kind=Utc`). Konwersja do czasu lokalnego tylko w UI. |
| A-007 | **Symulator korzysta z tych samych serwisów ingest** co prawdziwe kamery/parkomaty (`IObservationIngest`, `IParkomatIngest`). Dalsza część systemu nie odróżnia danych symulowanych od prawdziwych. |
| A-008 | **Unknown ≠ Free.** Brak świeżych danych daje `Unknown`; w UI nigdy nie jest to „wolne”. |
| A-009 | Fuzja kilku kamer: głosowanie ważone `confidence × weight`; remis → **Occupied**. |
| A-010 | Bilet z parkomatu **nie identyfikuje miejsca** (`SpotId` nullable). Służy do szacowania zajętości miejsc bez kamery, nie do orzekania o konkretnym miejscu. |
| A-011 | Tablice rejestracyjne nie są przechowywane. Dopuszczalny wyłącznie solony hash (`PlateHash`), domyślnie pusty. |
| A-012 | Dane demo (strefy, ulice, parkomaty) są **fikcyjne**, tylko rozmieszczone w okolicy centrum Krakowa. |
| A-013 | Płatności za parkowanie są poza zakresem v0 (przycisk jest nieaktywny). Integracja zależy od miasta/operatora — patrz `docs/09-roadmap.md`. |
| A-014 | Idempotencja biletów: klucz `(ParkomatCode, ExternalTicketId)`. |
| A-015 | Obserwacja starsza niż `ObservationTtlSeconds` (domyślnie 180 s) jest ignorowana; stan miejsca bez odświeżenia staje się `Unknown`. Kamera wysyła zmiany oraz keep-alive co ~60 s. |
| A-016 | *(zastąpione przez A-018 w części „obie platformy”)* Mapa w aplikacji = OpenStreetMap (Leaflet w WebView), bez płatnego klucza dostawcy map. Wymaga internetu; publiczne kafelki OSM tylko do demo/testów, produkcyjnie własny dostawca (`AppSettings.MapTileUrl`). Atrybucja „© OpenStreetMap contributors” musi być widoczna. |
| A-017 | **UI jest dwujęzyczny (pl, en)**; domyślnie `pl` (nie język urządzenia). Teksty w jednej tabeli w Core, z regułami liczby mnogiej, pilnowane testami. Dane z API (nazwy stref, taryfy) nie są tłumaczone. |
| A-018 | **Mapa zależy od platformy:** iOS/Mac Catalyst = Apple Maps (`Microsoft.Maui.Controls.Maps`, tylko dla TFM Apple), Android = OpenStreetMap przez Leaflet w WebView; wspólny interfejs `IZoneMap`. Żadna platforma nie wymaga klucza ani konta u dostawcy map. Zastępuje A-016 w zakresie „OSM na obu platformach”. |

## 7. Stan implementacji (aktualizuj przy zmianach)

| Komponent | Stan |
|---|---|
| Core, Data, Api, Simulator | **Zbudowane i uruchomione** (2026-10-04, macOS): API seeduje dane demo, symulator zasila bazę, `/api/zones` i `/api/admin/coverage` zwracają oczekiwane wartości |
| Vision, CameraWorker, Tests | **Napisane, NIEZWERYFIKOWANE**: nie uruchamiano z prawdziwym modelem/kamerą; `dotnet test` bez zgłoszonego wyniku; brak natywnego OpenCV dla macOS w csproj. Pierwsze zadanie: `dotnet test`, test detektora na nagraniu |
| Maui | Wersja z Apple Maps była **zbudowana i uruchomiona** (iPad A16). Zmiany 009 i 012 (mapa: Apple Maps na iOS, OSM/Leaflet na Androidzie) oraz 010 (język pl/en) **napisane, NIEZKOMPILOWANE i niesprawdzone w działaniu**; pierwsze zadanie: `dotnet build src/ParkingVision.Maui -f net10.0-ios`, `dotnet test`, test `pvapp://` i kafelków. Android nie uruchamiano. `Platforms/` (iOS, Android) i ikona/splash są **napisane ręcznie wg szablonu** i dołączone do repo (zmiana 015) — niezweryfikowane; awaryjnie usunąć `Platforms/` i uruchomić `scripts/bootstrap-maui.*`. Mac Catalyst usunięty z celów (nigdy nie testowany). iOS 27 wymaga UIScene: `Platforms/iOS/SceneDelegate.cs` + manifest w `Info.plist` (zmiana 016) |
| Model ONNX | Nie jest w repo (`models/` puste). Instrukcja eksportu w `docs/03-camera-software.md` |
| Kalibracja ROI | Tylko wartości zastępcze z seedera; brak narzędzia kalibracji (patrz roadmapa) |
| Płatności | Nie zaimplementowane (A-013) |

## 8. Konwencje kodu

- .NET 10, C# `latest`, `Nullable` włączone. Pakiety przyszpilić po pierwszym udanym `restore` (w prototypie wersje „pływające”).
- Serwisy danych to singletony używające `IDbContextFactory<ParkingDbContext>` (krótkie konteksty). Nie wstrzykuj `DbContext` bezpośrednio.
- Logika, którą da się wydzielić jako czystą funkcję (fuzja, debouncer, ewaluator), ma test w `ParkingVision.Tests`.
- Enumy w JSON jako stringi. Daty w JSON z sufiksem `Z`.
- Nowa funkcja = kod + test (jeśli czysta logika) + dokument żywy + plik zmiany.

## 9. Definition of done (lista kontrolna przed zakończeniem pracy)

- [ ] `dotnet build ParkingVision.sln` przechodzi (lub w `Verification` jawnie: nie uruchomiono)
- [ ] `dotnet test` przechodzi (lub jw.)
- [ ] Zaktualizowany dokument żywy właściwego obszaru
- [ ] Zaktualizowany rejestr założeń / tabela stanu w tym pliku, jeśli dotyczy
- [ ] Dodany **nowy** plik w `docs/changes/` z poprawnym kluczem
- [ ] Brak sekretów, klatek, tablic w repo

## 10. Szybki start

```bash
dotnet run --project src/ParkingVision.Api                     # API na http://0.0.0.0:5080 (seeduje dane demo)
dotnet run --project src/ParkingVision.Simulator -- run        # symulator kamer i parkomatów -> baza
# aplikacja: docs/07-maui-app.md (Platforms/ i zasoby są w repo)
```
Szczegóły: `README.md`, `docs/08-simulation.md`.
