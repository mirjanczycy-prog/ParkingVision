---
id: 20261003-002-DOC-ai-collaboration-protocol
date: 2026-10-03
area: DOC
type: decision
author: Claude
status: applied
touches: [AGENTS.md, docs/changes/README.md, docs/changes/_TEMPLATE.md]
assumptions: []
supersedes: []
corrects: []
db_impact: none
api_impact: none
---
# Protokół pracy modeli AI: klucz plików zmian

## Podsumowanie
Wprowadzono `AGENTS.md` jako punkt wejścia dla modeli AI oraz obowiązek zapisu każdej zmiany w osobnym, niezmiennym pliku `docs/changes/YYYYMMDD-NNN-AREA-slug.md`.

## Dlaczego
Kolejne modele (i sesje bez pamięci) muszą szybko odtworzyć kontekst i nie łamać wcześniejszych decyzji. Osobne pliki append-only unikają konfliktów przy równoległej pracy i zachowują historię; dokumenty żywe (`docs/00`–`09`) opisują stan bieżący. Globalny numer `NNN` daje jednoznaczną kolejność niezależną od daty; `AREA` łączy zmianę z dokumentem i kodem.

## Co zmieniono
- `AGENTS.md` — zasady twarde, klucz, mapa obszarów, rejestr założeń A-001…A-015, tabela stanu, DoD.
- `docs/changes/README.md`, `_TEMPLATE.md`.

## Verification
- n/d (dokumentacja).

## Następne kroki / znane luki
- Rozważyć walidator w CI sprawdzający nazwę pliku, unikalność `NNN` i obecność wymaganych pól.
