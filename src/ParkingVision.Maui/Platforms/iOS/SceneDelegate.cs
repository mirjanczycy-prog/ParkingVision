using Foundation;
using Microsoft.Maui.Platform;

namespace ParkingVision.Maui;

/// <summary>
/// iOS 27 (Xcode 27 SDK) terminates apps that do not adopt the UIScene lifecycle at launch
/// (___UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption). This class is named in
/// Info.plist -> UIApplicationSceneManifest. See dotnet/macios issue 26837 and docs/10-dev-environment.md.
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
