# 10 — Środowisko deweloperskie (macOS / Android / iOS)

> Dokument żywy. Zebrane problemy i rozwiązania z pierwszego uruchomienia (2026-10-04, macOS, Apple Silicon). Wersje narzędzi szybko się zmieniają — traktuj numery jako stan na tę datę i weryfikuj.

## Kolejność uruchamiania (3 terminale, z katalogu głównego repo, bez `sudo`)
```bash
dotnet run --project src/ParkingVision.Api                                        # 1) API + seed demo, http://localhost:5080
dotnet run --project src/ParkingVision.Simulator -- run --speed 30 --start-hour 9 # 2) symulator kamer i parkomatów
dotnet build src/ParkingVision.Maui -f net10.0-ios -t:Run                         # 3) aplikacja (iOS)
```
Kontrola danych przed aplikacją: `curl http://localhost:5080/api/health`, `.../api/zones?lat=50.06&lon=19.941&radiusM=2000`, `.../api/admin/coverage` (`curl` z drugiego okna — `dotnet run` blokuje terminal).
Przy starcie w nocy symulator pokazuje niską zajętość (krzywa dobowa) — `--start-hour 9` daje dzień roboczy.

## Narzędzia na macOS
| Krok | Polecenie / uwaga |
|---|---|
| Homebrew | instalator z brew.sh; na Apple Silicon dodać `eval "$(/opt/homebrew/bin/brew shellenv)"` do `~/.zprofile` |
| .NET 10 SDK + MAUI | `dotnet workload install maui` (jedyne polecenie, które może wymagać `sudo`) |
| Platformy projektu MAUI | w repo (`Platforms/iOS`, `Platforms/Android`, ikona, splash); `scripts/bootstrap-maui.sh` tylko awaryjnie, gdy te pliki zginą |
| JDK (Android) | Microsoft OpenJDK 17 (`brew install --cask microsoft-openjdk@17`); .NET 10 dodaje też obsługę JDK 21 |
| Zmienne (`~/.zprofile`) | `export JAVA_HOME="$(/usr/libexec/java_home -v 17)"`, `export ANDROID_HOME="$HOME/Library/Android/sdk"`; po zmianie zamknąć VS Code (Cmd+Q) i uruchomić z terminala `code .` |
| Android SDK | `dotnet build src/ParkingVision.Maui/ParkingVision.Maui.csproj -t:InstallAndroidDependencies -f net10.0-android "-p:AndroidSdkDirectory=$ANDROID_HOME" "-p:JavaSdkDirectory=$JAVA_HOME" -p:AcceptAndroidSDKLicenses=True`; emulator tworzy się w Android Studio (Device Manager) |

## iOS: Xcode musi pasować do workloadu
Błąd: `This version of .NET for iOS (X) requires Xcode Y. The current version of Xcode is Z.`
- Wersja workloadu iOS i Xcode muszą się zgadzać. Stan na 2026-10-04 (wg notatek wydań `dotnet/macios`): iOS **27.0.x ↔ Xcode 27.0** (wymaga macOS 26.6+), iOS **26.5.x ↔ Xcode 26.6**.
- Rozwiązania: (A) zainstalować pasujący Xcode i ustawić `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer`; (B) przypiąć starszy zestaw workloadów (`dotnet workload update --version <zestaw>`, sprawdzić `dotnet workload --info`).
- Po instalacji Xcode: `sudo xcodebuild -license accept`, `xcodebuild -runFirstLaunch`, poczekać na pobranie platformy iOS i symulatorów; kontrola: `xcrun simctl list devices available`. Bez zaakceptowanej licencji `simctl`/`devicectl` kończą się kodem 69.
- Komunikat `The app must be built before the arguments to launch the app using mlaunch can be computed` jest **skutkiem** nieudanego buildu — najpierw uruchom samo `dotnet build ... -f net10.0-ios` i czytaj prawdziwe błędy.
- `Platforms/iOS/Info.plist`: `NSLocationWhenInUseUsageDescription`, `NSLocalNetworkUsageDescription`, `NSAppTransportSecurity` → `NSAllowsLocalNetworking = true` (HTTP do lokalnego API tylko w dev).
- Mapa: iOS = Apple Maps, Android = OpenStreetMap (Leaflet w WebView); obie bez klucza i konta. Płatny klucz Google Maps nie jest potrzebny (patrz `07`).

## Pułapki, które już wystąpiły
| Objaw | Przyczyna | Rozwiązanie |
|---|---|---|
| `Could not access the lock file ... NuGetScratch` | katalog `$TMPDIR/NuGetScratch` założony przez `sudo` (właściciel root) | `sudo rm -rf "$TMPDIR/NuGetScratch"`; ewentualnie `sudo chown -R "$(whoami)" ~/.nuget`; nie uruchamiać `dotnet` przez `sudo` |
| `CS0234 ... 'Hosting' ... 'Microsoft.Extensions'` | SDK `Microsoft.NET.Sdk.Worker` dodaje `using`, nie pakiet | `PackageReference Microsoft.Extensions.Hosting` w Simulator i CameraWorker (naprawione, zmiana 004) |
| `Could not parse the JSON file ... appsettings.json` | brak przecinka po edycji ręcznej | poprawić JSON; ostatni element obiektu bez przecinka |
| `curl: (7) Couldn't connect` | API nie działa lub `curl` w tym samym oknie co `dotnet run` | uruchomić API w osobnym terminalu, sprawdzić `/api/health` |
| `CS0234 ... 'Hosting' ... 'Microsoft.Maui.Controls.Maps'` | nieistniejący `using` w `MauiProgram.cs` | usunięty (zmiana 005); `UseMauiMaps()` działa z istniejących `using` |
| Po zmianie języka część etykiet (XAML `{loc:T}`) zostaje po polsku, a teksty liczone w kodzie się zmieniają | `PropertyChanged("Item[]")` nie odświeża wiązań z indeksatorem | `Loc.SetLanguage` zgłasza też `PropertyChanged` z pustą nazwą (zmiana 013) |
| iOS pokazuje OpenStreetMap, choć miała być Apple Maps | na dysku jest jeszcze stara wersja `MapPage` (bez `Views/Maps`) albo `#if` nie zgadza się z pakietem | nałożyć pliki z aktualizacji i dodać do csproj stałą `PV_APPLE_MAPS` razem z pakietem Maps (zmiana 014) |
| Jedna etykieta nie zmienia języka po przełączeniu | wiązanie XAML `{loc:T}` nie odświeżyło się tylko dla niej | etykieta liczona we view modelu (`LanguageLabel`), jak „Promień” i „Odświeżanie” (zmiana 014) |
| `MSB3954 ... Resources/Splash/splash.svg ... does not exist` | podmieniono cały folder `src/ParkingVision.Maui` na wersję bez wygenerowanych zasobów | od zmiany 015 ikona, splash i `Platforms/` są w repo; użyć pełnej paczki lub przywrócić z gita |
| Aplikacja iOS zamyka się zaraz po starcie na symulatorze iOS 27; raport: `EXC_BREAKPOINT`, `___UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption` | od iOS 27 / Xcode 27 aplikacja **musi** używać cyklu życia UIScene; szablon MAUI jeszcze go nie ma (dotnet/macios #26837) | `Platforms/iOS/SceneDelegate.cs` (`[Register("SceneDelegate")]`, dziedziczy `MauiUISceneDelegate`) + `UIApplicationSceneManifest` w `Info.plist` z `UISceneDelegateClassName = SceneDelegate` (zmiana 016); po odtworzeniu `Platforms/` skryptem bootstrap, lub gdy `grep -c UIApplicationSceneManifest src/ParkingVision.Maui/Platforms/iOS/Info.plist` zwraca 0, uruchomić `./scripts/fix-ios27-scene.sh` (idempotentny, dodaje oba elementy i czyści `bin`/`obj`) |
| Konsola zalana SQL-em EF | domyślny poziom logowania `Information` | filtr `Microsoft.EntityFrameworkCore: Warning` (zmiana 004) |

## Nie zweryfikowane
- `ParkingVision.Vision` / `CameraWorker` na macOS: w csproj jest tylko natywny pakiet OpenCV dla Windows. Na Macu `VideoCapture` wymaga biblioteki natywnej (pakiet runtime dla macOS lub OpenCV z Homebrew). ONNX Runtime ma wersje dla macOS (arm64). Do sprawdzenia przy pierwszej próbie z prawdziwą kamerą.
- `dotnet test` — nie zgłoszono wyniku.
