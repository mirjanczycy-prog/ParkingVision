# docs/changes — dziennik zmian (append-only)

Każda zmiana w projekcie ma tu **własny plik**. Pliki są niezmienne po zapisaniu — korekty robi się nowym plikiem (`corrects:` / `supersedes:`).

## Klucz nazwy
```
YYYYMMDD-NNN-AREA-slug.md
```
- `YYYYMMDD` — data UTC,
- `NNN` — globalny numer kolejny (największy istniejący + 1),
- `AREA` — `ARCH | DATA | CAM | PRC | PKM | API | APP | SIM | ROAD | OPS | DOC`,
- `slug` — kebab-case, angielski, ≤ 6 słów.

Pełna tabela obszarów i zasady: `AGENTS.md` §4. Szablon: `_TEMPLATE.md`.

## Jak dodać wpis (algorytm)
1. `ls docs/changes` → ustal następne `NNN`.
2. Skopiuj `_TEMPLATE.md` do `YYYYMMDD-NNN-AREA-slug.md`, uzupełnij nagłówek YAML i sekcje.
3. Zaktualizuj dokument żywy obszaru (`docs/0x-*.md`) i — jeśli dotyczy — `AGENTS.md` (rejestr założeń, tabela stanu).
4. W `Verification` wpisz tylko to, co faktycznie uruchomiono.

## Indeks
Nie utrzymujemy ręcznego indeksu (żeby nie edytować starych plików). Indeks = `ls docs/changes | sort`. Szybki podgląd tytułów:
```bash
grep -m1 '^# ' docs/changes/2*.md
```
