---
id: 20261004-008-OPS-dev-environment-guide
date: 2026-10-04
area: OPS
type: docs
author: Claude
status: applied
touches: [docs/10-dev-environment.md, AGENTS.md, docs/03-camera-software.md, docs/08-simulation.md]
assumptions: []
supersedes: []
corrects: []
db_impact: none
api_impact: none
---
# Przewodnik po środowisku deweloperskim (macOS / iOS / Android)

## Podsumowanie
Nowy dokument `docs/10-dev-environment.md` zbiera kolejność uruchamiania i napotkane pułapki: Homebrew, JDK, Android SDK, dopasowanie Xcode do workloadu iOS, licencja Xcode, uprawnienia NuGet po `sudo`, błędy JSON i brakujące pakiety.

## Dlaczego
Pierwsze uruchomienie zajęło wiele kroków; kolejne osoby i modele nie powinny ich powtarzać.

## Co zmieniono
- `docs/10-dev-environment.md` (nowy), mapa obszarów w `AGENTS.md` (OPS → docs/10).
- Notka o OpenCV na macOS w `docs/03`; wskazówka `--start-hour` w `docs/08`.

## Verification (tylko to, co faktycznie uruchomiono)
- Numery wersji Xcode/workloadu pochodzą z notatek wydań `dotnet/macios` (stan 2026-10-04); nie weryfikowano instalacji innych kombinacji.

## Następne kroki / znane luki
- `Vision` na macOS (natywny OpenCV) do sprawdzenia przy pierwszej próbie z kamerą.
