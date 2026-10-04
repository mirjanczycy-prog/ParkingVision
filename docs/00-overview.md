# 00 — Przegląd projektu

> Dokument żywy. Opisuje stan **obecny**. Historia zmian: `docs/changes/`.

## Cel
Pokazać kierowcy, **gdzie w pobliżu są wolne miejsca parkingowe** przy ulicach i na parkingach niestrzeżonych, na podstawie obrazu z kamer oraz danych z parkomatów.

## Zakres v0 (prototyp)
- Detekcja zajętości miejsc z kamer (gotowy detektor YOLO, ONNX, C#).
- Baza z topologią: strefy, miejsca, parkomaty, kamery, relacje N–M.
- Import biletów z parkomatów i szacowanie zajętości miejsc bez kamery.
- API HTTP dla aplikacji.
- Aplikacja MAUI: mapa, lista, szczegóły strefy, ustawienia.
- **Symulator** kamer i parkomatów zasilający bazę (testy i prezentacje bez sprzętu).

## Poza zakresem v0
- Płatność za postój z aplikacji (zależy od miasta/operatora — `09-roadmap.md`).
- Konta użytkowników, powiadomienia push, rezerwacje.
- Trenowanie własnych modeli, rozpoznawanie tablic.
- Panel administracyjny i narzędzie kalibracji ROI (roadmapa).

## Słownik
| Termin | Znaczenie |
|---|---|
| Strefa (`Zone`) | Ulica / plac / parking traktowany jako całość, np. „Demo: ulica A”. Jednostka pokazywana użytkownikowi. |
| Miejsce (`Spot`) | Pojedyncze miejsce postojowe. Ma typ: Car, Motorcycle, Disabled, Electric, Loading. |
| Parkomat (`Parkomat`) | Urządzenie sprzedające bilety; obsługuje zbiór miejsc. |
| Kamera (`Camera`) | Źródło obrazu; „widzi” zbiór miejsc przez wielokąty ROI. |
| ROI | Wielokąt miejsca w obrazie kamery, we współrzędnych znormalizowanych 0..1. |
| Obserwacja (`SpotObservation`) | Odczyt jednej kamery o jednym miejscu (po stabilizacji). |
| Stan (`SpotState`) | Aktualny, scalony stan miejsca: Free / Occupied / Unknown. |
| Pewność strefy | Live / Mixed / Estimated / NoData — skąd pochodzi liczba wolnych miejsc. |

## Główny przepływ danych
```
kamera -> CameraWorker (YOLO + ROI + debounce) -> IObservationIngest -> SpotObservation -> fuzja -> SpotState ┐
parkomat -> import biletów -> IParkomatIngest -> ParkomatTicket ───────────────────────────────────────────────┤
                                                                                                              v
                                                         AvailabilityService (stan + szacunek z biletów) -> API -> aplikacja MAUI
Symulator ──────────────── te same IObservationIngest / IParkomatIngest ───────────────────────────────────────^
```

## Status
Patrz tabela „Stan implementacji” w `AGENTS.md` (sekcja 7).
