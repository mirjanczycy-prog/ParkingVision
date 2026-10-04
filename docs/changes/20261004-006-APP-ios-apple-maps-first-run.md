---
id: 20261004-006-APP-ios-apple-maps-first-run
date: 2026-10-04
area: APP
type: feature
author: Claude
status: applied
touches: [docs/07-maui-app.md, docs/img/*, AGENTS.md, README.md, docs/09-roadmap.md]
assumptions: [A-007, A-008, A-012]
supersedes: []
corrects: []
db_impact: none
api_impact: none
---
# Pierwsze uruchomienie aplikacji na iOS (Apple Maps)

## Podsumowanie
Aplikacja MAUI zbudowana i uruchomiona na symulatorze iPada (A16) z Apple Maps — bez klucza Google. Dokumentacja i tabela stanu uaktualnione; zrzuty dodane do `docs/img/`.

## Dlaczego
Użytkownik pracuje na macOS i testuje na urządzeniach Apple; ścieżka iOS nie wymaga konta ani klucza map.

## Co zmieniono
- `docs/07` — status, zrzuty ekranu, sekcja „Konfiguracja iOS”.
- `AGENTS.md` §7 — stan: Core/Data/Api/Simulator/Maui zweryfikowane; Vision/CameraWorker/Tests nadal nie.
- `README.md`, `docs/09-roadmap.md` — zaktualizowane statusy Fazy 0.

## Verification (tylko to, co faktycznie uruchomiono)
- Uruchomienie na symulatorze iPad (A16), 2026-10-04: ekrany Mapa, Lista, Ustawienia z danymi z API + symulatora (zrzuty w `docs/img/`).
- Android: nie uruchamiano. Prawdziwy iPhone: nie uruchamiano.

## Następne kroki / znane luki
- Luki UI: zob. 20261004-007.
