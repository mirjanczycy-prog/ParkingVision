---
id: 20261004-013-APP-fix-language-refresh
date: 2026-10-04
area: APP
type: fix
author: Claude
status: applied
touches: [src/ParkingVision.Maui/Localization/Loc.cs, docs/10-dev-environment.md]
assumptions: [A-017]
supersedes: []
corrects: [20261004-010-APP-localization-pl-en]
db_impact: none
api_impact: none
---
# Naprawa odświeżania etykiet XAML po zmianie języka

## Podsumowanie
Po przełączeniu na English na zrzucie z iPada tłumaczyły się tylko teksty liczone w kodzie (promień, odświeżanie). Etykiety XAML (`{loc:T Key=…}`), zakładki i tytuły zostawały po polsku.

## Dlaczego
`Loc.SetLanguage` zgłaszało tylko `PropertyChanged("Item[]")`, co nie odświeża wiązań z indeksatorem. Dodano zdarzenie z pustą nazwą właściwości („zmieniło się wszystko”), które odświeża je niezawodnie.

## Co zmieniono
- `Loc.SetLanguage`: dodatkowe `PropertyChanged` z `string.Empty` (zostawiono też `Item[]`).
- `docs/10`: wiersz w tabeli pułapek.

## Verification (tylko to, co faktycznie uruchomiono)
- Objaw potwierdzony na zrzucie z symulatora iPad (2026-10-04, język English).
- Poprawka: nie zbudowana ani nie uruchomiona. Do sprawdzenia: po zmianie języka zmieniają się etykiety zakładek, „Język”, „Server address”, przyciski i podpowiedzi.

## Następne kroki / znane luki
- Jeśli tytuły zakładek Shell nadal nie zmienią się, ustawić je w kodzie po `SetLanguage` (`ShellContent.Title`).
