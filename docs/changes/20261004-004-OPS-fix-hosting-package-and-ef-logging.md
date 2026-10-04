---
id: 20261004-004-OPS-fix-hosting-package-and-ef-logging
date: 2026-10-04
area: OPS
type: fix
author: Claude
status: applied
touches: [src/ParkingVision.Simulator/ParkingVision.Simulator.csproj, src/ParkingVision.CameraWorker/ParkingVision.CameraWorker.csproj, src/ParkingVision.Api/appsettings.json, src/ParkingVision.CameraWorker/appsettings.json, src/ParkingVision.Simulator/Program.cs]
assumptions: []
supersedes: []
corrects: [20261003-001-ARCH-initial-prototype-scaffold]
db_impact: none
api_impact: none
---
# Brakujący pakiet Hosting i wyciszenie logów EF

## Podsumowanie
Simulator i CameraWorker nie kompilowały się (`CS0234 ... Microsoft.Extensions.Hosting`). Dodano pakiet `Microsoft.Extensions.Hosting`. Logi SQL EF Core przestawiono na poziom Warning.

## Dlaczego
`Microsoft.NET.Sdk.Worker` dodaje globalny `using`, ale nie sam pakiet (błąd w 001). Domyślny poziom Information zalewał konsolę każdym zapytaniem SQL.

## Co zmieniono
- Simulator.csproj, CameraWorker.csproj — `PackageReference Microsoft.Extensions.Hosting 10.*`.
- API i CameraWorker `appsettings.json` — `Microsoft.EntityFrameworkCore: Warning`; Simulator `Program.cs` — `AddFilter("Microsoft.EntityFrameworkCore", Warning)`.

## Verification (tylko to, co faktycznie uruchomiono)
- build: Simulator uruchomiony po poprawce (użytkownik, macOS); API uruchomione i odpowiada na `/api/zones`, `/api/admin/coverage`.
- CameraWorker: build po poprawce nie weryfikowany.
- testy: nie uruchomiono.

## Następne kroki / znane luki
- Przyszpilić wersje pakietów (obecnie `10.*`).
