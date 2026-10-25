# Build Guide

All builds go through `Assets/NovaVanguard/Editor/NovaBuild.cs`, available from the **Nova Vanguard → Build** menu or headless via `-executeMethod`. Output is written to `Builds/<Platform>/` (git-ignored).

```bash
# headless examples (adjust the Unity path for your OS)
UNITY="/Applications/Unity/Hub/Editor/6000.0.47f1/Unity.app/Contents/MacOS/Unity"   # macOS
UNITY="C:\Program Files\Unity\Hub\Editor\6000.0.47f1\Editor\Unity.exe"                # Windows

"$UNITY" -batchmode -quit -projectPath . -logFile build.log -executeMethod NovaVanguard.Editor.NovaBuild.Windows
"$UNITY" -batchmode -quit -projectPath . -logFile build.log -executeMethod NovaVanguard.Editor.NovaBuild.Android
"$UNITY" -batchmode -quit -projectPath . -logFile build.log -executeMethod NovaVanguard.Editor.NovaBuild.WebGL
```

Available methods: `Windows`, `Mac`, `Linux`, `Android`, `AndroidBundle`, `iOS`, `WebGL`, `AllDesktopMobileWeb`.

---

## Windows

1. File → Build Profiles → Windows, or **Nova Vanguard → Build → Windows (x64)**.
2. Output: `Builds/Windows/NovaVanguard.exe` plus the `NovaVanguard_Data` folder. Zip the whole folder to distribute.
3. For Steam: upload the folder with SteamPipe; the game runs windowed/fullscreen via Unity's default resolution dialog settings (Player → Resolution and Presentation).

## macOS

**Nova Vanguard → Build → macOS** → `Builds/macOS/NovaVanguard.app`. To distribute outside the App Store, code-sign and notarize:
```bash
codesign --deep --force --options runtime --sign "Developer ID Application: NAME" Builds/macOS/NovaVanguard.app
```

## Linux

**Nova Vanguard → Build → Linux** → `Builds/Linux/NovaVanguard.x86_64`. Mark executable: `chmod +x NovaVanguard.x86_64`.

---

## Android (APK / AAB)

### Prerequisites
- Android Build Support module with OpenJDK, SDK and NDK (Unity Hub installs matching versions).
- Target API: Unity 6 defaults to API 34/35 which satisfies current Play Store requirements. Min SDK is set to 24 (Android 7.0) by the setup script.

### Debug APK (sideload / testing)
1. **Nova Vanguard → Build → Android APK** → `Builds/Android/NovaVanguard-1.0.0.apk`.
2. Install: `adb install -r Builds/Android/NovaVanguard-1.0.0.apk` or copy to the phone and open.

### Release AAB for Google Play
1. Create a keystore once:
   ```bash
   keytool -genkey -v -keystore novavanguard.keystore -alias novavanguard -keyalg RSA -keysize 2048 -validity 10000
   ```
   Keep it **outside** the repo (`*.keystore` is git-ignored). Losing it means you can never update the app.
2. Edit → Project Settings → Player → Android → **Publishing Settings** → Keystore Manager → select the keystore, enter passwords, pick the alias.
3. Bump **Version** (Player → Version, e.g. `1.0.1`). `NovaBuild` derives `bundleVersionCode` from it (`1.0.1` → 10001), so every version string change produces a higher version code as Play requires.
4. **Nova Vanguard → Build → Android AAB (Play Store)** → `Builds/Android/NovaVanguard-1.0.1.aab`.
5. Play Console → Create app → Production → Create release → upload the `.aab`. Fill in the store listing, content rating (the game has no user-generated content, no ads, no purchases) and data-safety form (the game stores a local save file only).

Settings already applied by the setup script: IL2CPP, ARM64 + ARMv7, Vulkan + GLES3, landscape only, `targetFrameRate 60`.

### Performance notes
- Quality tier is auto-selected from device RAM (≥6 GB high, ≥3.5 GB medium, else low with 75% render scale). Players can override in Settings.
- Post-processing is disabled on the Low tier.
- Profile with **Window → Analysis → Profiler** attached to the device (Development Build + Autoconnect Profiler).

---

## iOS

1. Build on macOS: **Nova Vanguard → Build → iOS (Xcode project)** → `Builds/iOS/Unity-iPhone.xcodeproj`.
2. Open in Xcode 15+. Select your Team under *Signing & Capabilities* (bundle ID `com.jaking.novavanguard` is set; change it in Player Settings to match your App Store Connect app).
3. Minimum iOS is 13.0. Device orientation is landscape only.
4. Run on a device for testing; **Product → Archive** → Distribute App for TestFlight / App Store.
5. App Store review needs screenshots for 6.7" and 6.5" iPhones and 12.9" iPad; take them from the device or simulator.

---

## WebGL

1. **Nova Vanguard → Build → WebGL** → `Builds/WebGL/` (`index.html` + `Build/`). Compression is Brotli with decompression fallback, so it works on any static host.
2. Test locally (browsers block `file://`):
   ```bash
   cd Builds/WebGL && python3 -m http.server 8080   # open http://localhost:8080
   ```
3. **GitHub Pages**: copy the contents of `Builds/WebGL` into a `gh-pages` branch (or `docs/play/`), enable Pages in the repo settings. The included workflow `.github/workflows/webgl-pages.yml` does this automatically when a `UNITY_LICENSE` secret is configured (see below).
4. **itch.io**: zip the `Builds/WebGL` folder, upload as an HTML project, set the viewport to 1280×720 and tick "Fullscreen button".

Notes: saves on WebGL use `PlayerPrefs` (IndexedDB). Audio starts after the first click because of browser autoplay rules; the main menu click handles that.

---

## CI (GitHub Actions)

`.github/workflows/build.yml` builds Windows, Android and WebGL on every tag `v*` using [game-ci/unity-builder](https://game.ci/docs/github/builder) and attaches the artifacts to a GitHub Release. It needs three repository secrets:

- `UNITY_LICENSE` – contents of your `.ulf` license file (Personal licenses work; see game.ci activation docs)
- `UNITY_EMAIL`, `UNITY_PASSWORD`

Without the secrets the workflow is skipped, so the repo stays green.

## Versioning & releases

- Version lives in **Player Settings → Version** (`PlayerSettings.bundleVersion`) and is shown in the main menu.
- Tag releases `vX.Y.Z` and add notes to `docs/RELEASE_NOTES.md`.
