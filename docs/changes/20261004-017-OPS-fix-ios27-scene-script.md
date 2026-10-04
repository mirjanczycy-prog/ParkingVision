---
id: 20261004-017-OPS-fix-ios27-scene-script
date: 2026-10-04
area: OPS
type: ops
author: Claude
status: applied
touches: [scripts/fix-ios27-scene.sh, docs/10-dev-environment.md]
assumptions: []
supersedes: []
corrects: [20261004-016-APP-ios27-scene-lifecycle]
db_impact: none
api_impact: none
---
# Skrypt dopisujący UIScene dla iOS 27

## Podsumowanie
Po zmianie 016 użytkownik nadal miał awarię: `grep -c UIApplicationSceneManifest` na jego `Info.plist` zwracał 0, czyli budował starszą wersję plików. Dodano idempotentny skrypt `scripts/fix-ios27-scene.sh`, który dopisuje `SceneDelegate.cs` i manifest oraz czyści `bin`/`obj`, bez wymiany całego projektu.

## Dlaczego
Wymiana folderu wymagała ostrożności (utrata wygenerowanych plików); skrypt jest bezpieczny także po ponownym wygenerowaniu `Platforms/` z szablonu.

## Co zmieniono
- `scripts/fix-ios27-scene.sh`, wiersz w tabeli pułapek `docs/10`.

## Verification (tylko to, co faktycznie uruchomiono)
- Skrypt uruchomiony w środowisku autora (Linux) na próbnym `Info.plist` bez manifestu: dodał plik i wpis, drugie uruchomienie nic nie zmieniło, wynikowy plist jest poprawnym XML.
- Na macOS i z prawdziwym buildem: nie uruchomiono.

## Następne kroki / znane luki
- Po udanym uruchomieniu na iOS 27 potwierdzić, że aplikacja startuje.
