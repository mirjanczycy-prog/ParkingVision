---
id: 20261004-016-APP-ios27-scene-lifecycle
date: 2026-10-04
area: APP
type: fix
author: Claude
status: applied
touches: [src/ParkingVision.Maui/Platforms/iOS/SceneDelegate.cs, src/ParkingVision.Maui/Platforms/iOS/Info.plist, docs/07-maui-app.md, docs/10-dev-environment.md, AGENTS.md, scripts/bootstrap-maui.sh, scripts/bootstrap-maui.ps1]
assumptions: [A-018]
supersedes: []
corrects: [20261004-015-APP-platforms-in-repo]
db_impact: none
api_impact: none
---
# iOS 27: wymagany cykl życia UIScene

## Podsumowanie
Aplikacja zamykała się zaraz po starcie na symulatorze iOS 27 (raport awarii: `EXC_BREAKPOINT` w `___UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption`). Dodano `SceneDelegate` i `UIApplicationSceneManifest` w `Info.plist`.

## Dlaczego
Od iOS 27 / Xcode 27 aplikacja bez cyklu życia UIScene jest zatrzymywana przy uruchomieniu. Szablon MAUI jeszcze tego nie ma (dotnet/macios issue 26837, 2026-10-03). Poprawka jest tą z tego zgłoszenia: klasa `SceneDelegate : MauiUISceneDelegate` zarejestrowana pod nazwą `SceneDelegate` i manifest w `Info.plist` ze `__MAUI_DEFAULT_SCENE_CONFIGURATION__`. Wcześniejsze uruchomienia (iPad A16) działały, najpewniej na starszym środowisku iOS 26; ta zależność od wersji środowiska nie została sprawdzona.

## Co zmieniono
- `Platforms/iOS/SceneDelegate.cs` (nowy), `Platforms/iOS/Info.plist` (`UIApplicationSceneManifest`, `UIApplicationSupportsMultipleScenes = true` jak w zgłoszeniu).
- `docs/07`, `docs/10` (tabela pułapek), `AGENTS.md`, komentarze w skryptach bootstrap.

## Verification (tylko to, co faktycznie uruchomiono)
- Awaria potwierdzona raportem z symulatora (macOS 27.0.1, 2026-10-04).
- Poprawka: nie zbudowana ani nie uruchomiona. Do sprawdzenia: start na symulatorze iOS 27 bez awarii oraz działanie `Shell` i nawigacji przy `UIApplicationSupportsMultipleScenes = true`.

## Następne kroki / znane luki
- Gdy szablon MAUI dostanie własną poprawkę, porównać i ujednolicić.
- Rozważyć `UIApplicationSupportsMultipleScenes = false` (jedno okno), jeśli wielookienkowość na iPadzie sprawi problemy.
