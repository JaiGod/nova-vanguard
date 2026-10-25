# Installation Guide

## Requirements

- **Unity 6** – 6000.0 LTS or newer (the project was authored against 6000.0.58f1; any 6000.x works, Unity will offer to upgrade).
- Unity Hub 3.x.
- Platform modules (install from Hub → Installs → ⚙ → Add modules):
  - **Android Build Support** (+ OpenJDK + Android SDK & NDK Tools) for APK/AAB
  - **iOS Build Support** (macOS + Xcode 15+ required to produce the final .ipa)
  - **WebGL Build Support**
  - **Mac/Linux Build Support** if building for those from Windows (and vice versa)
- Git. Git LFS is optional (only matters once you add binary art/audio; `.gitattributes` is already set up for it).

## Steps

```bash
git clone https://github.com/jaineil32624/nova-vanguard.git
cd nova-vanguard
```

1. Unity Hub → **Add → Add project from disk** → select the folder.
2. Open it with a Unity 6 editor. The first import resolves the packages in `Packages/manifest.json`:
   - `com.unity.render-pipelines.universal` (URP)
   - `com.unity.inputsystem`
   - `com.unity.ugui`
3. After scripts compile, `Assets/NovaVanguard/Editor/NovaProjectSetup.cs` runs automatically (it is tagged `[InitializeOnLoad]` and uses a marker file `ProjectSettings/NovaSetupDone.txt`, which is git-ignored). It will:
   - generate `Assets/NovaVanguard/Settings/NovaURP.asset` + `NovaRenderer.asset` and set them as the default/quality render pipeline,
   - generate base materials under `Assets/NovaVanguard/Resources/NovaMaterials`,
   - apply player settings,
   - set **Active Input Handling = Both**. Unity asks to restart the editor for this; accept.
4. Open `Assets/NovaVanguard/Scenes/Main.unity` and press Play.

## Verifying the install

- Console should show `[Nova Vanguard] Project setup complete.`
- In Play mode you should see the main menu with a starfield and hear menu music.
- **Project Settings → Graphics → Default Render Pipeline** should be `NovaURP`.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Everything is pink / no bloom | URP asset not assigned. Run **Nova Vanguard → Run Project Setup**, or set Graphics → Default Render Pipeline to `Assets/NovaVanguard/Settings/NovaURP.asset`. |
| `InputSystem` errors or keyboard doesn't work | Edit → Project Settings → Player → Other Settings → **Active Input Handling = Both**, restart editor. |
| No sound | Audio is synthesized at startup; check the Settings menu volumes and that the editor isn't muted (Game view → Mute Audio). |
| Setup didn't run | Delete `ProjectSettings/NovaSetupDone.txt` and reopen, or run it from the menu. |
| Script compile errors after a Unity upgrade | Open Package Manager and update URP / Input System to the versions matching your editor, then reimport. |

## Optional: real assets

The game is fully playable with procedural assets. To swap in real ones:

- **Audio** – put `.wav`/`.ogg` clips in `Assets/NovaVanguard/Resources/Audio/Sfx/<SfxName>` (names from the `Sfx` enum, e.g. `Laser.wav`), `Audio/Music/<Menu|Combat|Boss|Victory>` and `Audio/Voice/<key>` (`shield_down`, `warning_boss`, `boss_shield_down`). They are picked up automatically.
- **Font** – put a TTF at `Assets/NovaVanguard/Resources/Fonts/NovaFont.ttf`.
- **Models** – replace the builder methods in `ShipFactory.cs` with prefab instantiation; gameplay never touches the visual hierarchy directly.
