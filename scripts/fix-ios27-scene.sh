#!/usr/bin/env bash
# Adds the UIScene lifecycle that iOS 27 requires (otherwise the app is terminated at launch with
# ___UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption). Idempotent: safe to run many times.
# Run from the repository root:  ./scripts/fix-ios27-scene.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
IOS="$ROOT/src/ParkingVision.Maui/Platforms/iOS"
PLIST="$IOS/Info.plist"

[ -f "$PLIST" ] || { echo "Brak $PLIST - uruchom skrypt z katalogu repo i upewnij sie, ze Platforms/iOS istnieje."; exit 1; }

# 1) SceneDelegate.cs
if [ ! -f "$IOS/SceneDelegate.cs" ]; then
cat > "$IOS/SceneDelegate.cs" <<'CS'
using Foundation;
using Microsoft.Maui.Platform;

namespace ParkingVision.Maui;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
CS
  echo "Dodano SceneDelegate.cs"
else
  echo "SceneDelegate.cs juz jest"
fi

# 2) UIApplicationSceneManifest in Info.plist
if grep -q "UIApplicationSceneManifest" "$PLIST"; then
  echo "Info.plist: UIApplicationSceneManifest juz jest"
else
  python3 - "$PLIST" <<'PY'
import sys
p = sys.argv[1]
s = open(p, encoding="utf-8").read()
block = """	<key>UIApplicationSceneManifest</key>
	<dict>
		<key>UIApplicationSupportsMultipleScenes</key>
		<true/>
		<key>UISceneConfigurations</key>
		<dict>
			<key>UIWindowSceneSessionRoleApplication</key>
			<array>
				<dict>
					<key>UISceneConfigurationName</key>
					<string>__MAUI_DEFAULT_SCENE_CONFIGURATION__</string>
					<key>UISceneDelegateClassName</key>
					<string>SceneDelegate</string>
				</dict>
			</array>
		</dict>
	</dict>
"""
i = s.rfind("</dict>")
if i < 0:
    sys.exit("Nie znaleziono </dict> w Info.plist")
open(p, "w", encoding="utf-8").write(s[:i] + block + s[i:])
print("Info.plist: dodano UIApplicationSceneManifest")
PY
fi

# 3) clean build outputs so the new plist is really used
rm -rf "$ROOT/src/ParkingVision.Maui/bin" "$ROOT/src/ParkingVision.Maui/obj"
echo "Gotowe. Sprawdz: grep -c UIApplicationSceneManifest $PLIST   (ma byc 1)"
