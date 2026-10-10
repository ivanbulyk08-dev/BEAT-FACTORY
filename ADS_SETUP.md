# AdMob — Beat Factory

## Status
- Google Mobile Ads Unity plugin **v11.5.0** is installed. It is the official package from github.com/googleads/googleads-mobile-unity and includes External Dependency Manager.
- **App IDs** (set in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`, menu Assets → Google Mobile Ads → Settings):
  - Android: `ca-app-pub-7578662571343965~7686882859` (yours).
  - iOS: `ca-app-pub-3940256099942544~1458002511`. This is Google's SAMPLE ID; replace it when you create the iOS app in AdMob.
- **Scripting define `BEATFACTORY_ADMOB`** is on for Android, iOS and Standalone, so the AdMob code compiles.
- **TEST MODE is ON** (`AdsManager → Use Test Ad Units`): every build uses Google's test ad units only.
  - Android: rewarded `ca-app-pub-3940256099942544/5224354917`, interstitial `…/1033173712`.
  - iOS: rewarded `ca-app-pub-3940256099942544/1712485313`, interstitial `…/4411468910`.

## Where things are
- `Scripts/Ads/AdsManager.cs` decides when ads may show, keeps the test mode, protects against double rewards and writes `[Ads]` logs.
- `Scripts/Ads/AdMobAdsProvider.cs` talks to AdMob: consent (UMP) → initialize → load → show → reload, with retries.
- **Rewarded ads are already wired to the existing boost system:**
  - BOOSTS popup: WATCH AD gives 2X MONEY / 2X SPEED / 3X PRODUCER / 5X VIRAL.
  - Instant Viral.
  - Welcome Back x2.
- **The reward is given only if the ad was watched to the end.** Closing early or an error gives nothing. A second tap while an ad is open is ignored.
- **If the ad is not loaded yet,** the button shows "loading…". Tapping it shows "Ad is still loading — try again in a few seconds".

## Testing
- **Editor:** by default the game's placeholder overlay is shown.
  - To see Google's own test ad in the Editor, select the AdsManager object and untick "Fake Ads In Editor". Then switch the platform to Android.
- **Phone:** Android build → open BOOSTS → WATCH AD. You should see a Google ad labelled "Test Ad".
  - Logs: run `adb logcat -s Unity` and look for lines with `[Ads]`.

## Verified
- No compile errors on the Android target.
- In the Editor the SDK initializes and test ads load. A rewarded ad watched to the end gives the reward once. Closing it early gives nothing. The next ad loads automatically.
- **Android Resolver** added `play-services-ads 25.4.0` and `user-messaging-platform 4.0.0` (`Assets/Plugins/Android/mainTemplate.gradle`). Custom Main Gradle and Gradle Properties templates are enabled.
- **Test APK** `Builds/BeatFactory-test.apk` (development build) built successfully. The manifest contains APPLICATION_ID `ca-app-pub-7578662571343965~7686882859` and the INTERNET and AD_ID permissions. minSdk is 26, targetSdk is 36.
- **Unity 6000.6.3** uses the Android SDK, NDK r27c and OpenJDK 17 from the Unity 6000.3.25f1 install (Preferences → External Tools).

## iOS
- The project settings are ready: iOS 15+, bundle id, GADApplicationIdentifier (Google's sample ID for now) and the tracking-permission text.
- The plugin adds the SKAdNetwork IDs automatically.
- To build: install **iOS Build Support** for 6000.6.3f1 in Unity Hub. Then on a **Mac** with Xcode, open the exported project and let CocoaPods install the Google SDK.

## Going live (after testing)
1. In AdMob create a **Rewarded** ad unit (Android, and iOS if you ship on iOS).
2. Select the **AdsManager** object in the scene and fill in the "REAL ad units" fields.
3. Untick **Use Test Ad Units**. Release builds will then use the real IDs.
4. For iOS: put the real iOS App ID in Assets → Google Mobile Ads → Settings.
5. Never tap your own real ads. For testing on your phone, keep test mode ON.
