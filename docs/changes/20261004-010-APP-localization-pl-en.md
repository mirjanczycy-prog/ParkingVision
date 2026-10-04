---
id: 20261004-010-APP-localization-pl-en
date: 2026-10-04
area: APP
type: feature
author: Claude
status: applied
touches: [src/ParkingVision.Core/Localization/*, src/ParkingVision.Maui/Localization/*, src/ParkingVision.Maui/ViewModels/*, src/ParkingVision.Maui/Views/*.xaml, src/ParkingVision.Maui/AppShell.xaml, src/ParkingVision.Maui/MauiProgram.cs, src/ParkingVision.Tests/CoreLogicTests.cs, docs/07-maui-app.md, AGENTS.md]
assumptions: [A-017, A-008]
supersedes: []
corrects: [20261004-007-DOC-screenshot-review-known-gaps]
db_impact: none
api_impact: none
---
# Wielojęzyczność interfejsu (pl, en)

## Podsumowanie
Interfejs aplikacji działa po polsku i angielsku; język zmienia się w Ustawieniach od razu i jest zapamiętywany. Naprawiono też odmianę liczby mnogiej („1 strefa / 2 strefy / 5 stref”).

## Dlaczego
Wymaganie użytkownika: start z pl i en, z możliwością dodawania kolejnych. Tabela tekstów w `Core` jest czystą logiką, więc da się ją testować bez MAUI. Domyślny język to `pl`, nie język urządzenia (A-017), żeby demo i prezentacja były przewidywalne; zmiana to jedna linia w `AppSettings`.

## Co zmieniono
- `Core/Localization`: `Strings` (klucz → (pl, en), klucze `|one/|few/|many`), `PluralRules`.
- `Maui/Localization`: `Loc` (przełączanie w locie, `Get/Format/Plural`), `TExtension` (`{loc:T Key=…}`).
- Wszystkie widoki i view modele używają tabeli; `Presentation` zwraca teksty w bieżącym języku; picker języka w Ustawieniach.
- Testy `LocalizationTests`: komplet kluczy w obu językach, zgodność placeholderów, grupy liczby mnogiej, kategorie plural dla pl/en.
- Dane z API (nazwy stref, taryfy) nie są tłumaczone.

## Verification (tylko to, co faktycznie uruchomiono)
- build, `dotnet test`: **nie uruchomiono**. Wyrażenia regularne i reguły plural sprawdzone tylko lekturą.
- Do sprawdzenia: odświeżanie `{loc:T}` po zmianie języka na Shell (`Title` zakładek), zachowanie Pickera przy starcie.

## Następne kroki / znane luki
- Nazwy stref/taryf wg języka z API (`Accept-Language`), kolejne języki, formaty dat/liczb wg kultury.
