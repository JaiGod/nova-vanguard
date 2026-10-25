# Architecture Overview

Nova Vanguard is a **code-first** Unity project: the only scene object is `Bootstrap`, and every other object (camera, lights, background, pools, UI, audio) is constructed in C#. This keeps the repo diff-able, makes the game trivially portable between render pipelines, and means there is nothing hidden in scene YAML.

```
Bootstrap (scene)
 ├─ SaveSystem.Init()          load JSON save → SaveData
 ├─ QualityManager.Apply()     pick quality tier
 ├─ CameraRig.Create()         camera + PostFx volume (URP) + GameBounds from frustum
 ├─ SpaceBackground.Create()   lights, starfield, nebulae, planet, station
 ├─ AudioManager.Create()      synthesize all SFX & music
 ├─ Pool registrations         Projectile / Missile / Enemy / Hazard / Pickup / Vfx
 ├─ UIManager.Create()         canvases, HUD, touch controls
 └─ GameManager.Create()       run state machine
```

## Core concepts

### Playfield
The game plays on the **XZ plane (y = 0)** with an angled top-down camera. `GameBounds` raycasts the camera frustum against that plane every time the screen size changes, so the arena fits any aspect ratio (ultrawide, 16:9, 20:9 phones). Enemies spawn above `MaxZ`, bullets despawn beyond the bounds.

### Units and damage
`Unit` is the abstract base for anything with health (`PlayerShip`, `Enemy`, `Boss`, `BossGenerator`, `Hazard`). All colliders are **kinematic triggers**; damage flows through `Unit.TakeDamage(amount, point, sourceTeam)`. `Team` is `Player | Enemy | Neutral`; friendly fire is rejected at the `Unit` level, and enemy shots pass through neutral hazards. `Unit.All` is a static list used for target acquisition (`Unit.Closest`).

`DamageTakenMultiplier` implements weak points/armour (generators 1.6×, boss 0× while shielded). `IContactDamage` marks things that hurt the player on touch.

### Pooling
`Pool` is a keyed factory pool. Pooled objects implement `IPoolable` (`OnSpawned` / `OnDespawned`). Nothing is instantiated during gameplay after prewarming; `Pool.DespawnAll()` clears the field between runs. Bullets live on layer 4 and that layer ignores itself, so bullet-vs-bullet trigger checks never happen.

### Data-driven content
`GameData` holds plain C# definitions for ships, weapons, enemies, sectors and bosses. Adding a sector, ship or weapon is a data entry; the UI, save system and spawner read from these lists. (They are plain classes rather than ScriptableObjects so the whole balance sheet is one readable, diff-able file; converting to SOs is mechanical if designers need inspector editing.)

### Game flow
`GameManager` is a small state machine (`Menu → Playing ⇄ Paused → SectorClear | GameOver`). `StartSector` resets the world and runs the `SectorFlow` coroutine: banner → N waves (enemy groups spawned over time, wave ends when all are dead or a timeout passes) → boss. Hazards tick independently. Callbacks (`OnEnemyKilled`, `OnBossDefeated`, `OnPlayerDied`) update score/combo/credits and drive the UI.

### Input
`InputRouter.Tick()` runs once per frame and converts three sources into intents (`Move`, `AimPoint`, `Fire`, `Boost`, pressed-flags). It compiles against the new Input System when `ENABLE_INPUT_SYSTEM` is defined and otherwise against the legacy `Input` class. Touch controls write static fields (`TouchStick`, `TouchFire`, …) that are merged in. `UiBlocking` stops a click on a button from firing the guns.

### Rendering
`MaterialLibrary` creates and caches materials at runtime, choosing URP or Built-in shaders based on `GraphicsSettings.currentRenderPipeline`. Base materials in `Resources/NovaMaterials` (created by the editor setup) guarantee the shaders survive build stripping. Emissive colours above 1.0 feed URP Bloom. `PostFx` configures a global Volume (Bloom, ACES, Vignette, Chromatic Aberration that "kicks" on hits).

Models come from `ShipFactory`, which assembles primitives into distinct silhouettes per ship/enemy/boss. Replace these builders with prefab instantiation to use real art.

### VFX
`Vfx` builds pooled `ParticleSystem`s (explosion, hit spark, warp) and shockwave rings from code with a generated soft-dot texture; thrusters are per-ship looping systems whose emission tracks throttle. `CameraRig.Shake` uses a trauma value (squared) with Perlin noise.

### Audio
`AudioManager` keeps a 16-voice pool with partial 3D spatialisation, throttles identical sounds within 35 ms, cross-fades two music sources, and loads any clip found in `Resources/Audio/...` before falling back to `Synth`, a tiny additive/subtractive synthesizer that renders every SFX and four music loops at startup (~1 s on desktop, cached in memory).

### Save
`SaveSystem` serializes `SaveData` with `JsonUtility`. `ISaveBackend` has file (desktop/mobile, atomic temp-file write), PlayerPrefs (WebGL) and a `CloudMirrorSaveBackend` wrapper whose `PushToCloud` delegate is where Unity Cloud Save / PlayFab / your API plugs in. Saves happen on purchase, sector clear, game over, pause, quit and app-pause.

### UI
uGUI built through the `UI` helper (rounded sprites, buttons, bars, sliders, toggles generated in code). `UIManager` rebuilds each screen on show so it always reflects `SaveData`. `HUD` polls gameplay state each frame. `TouchControls` provides a floating joystick (or drag mode) and hold/tap buttons.

## Extending

| Want to… | Do this |
|---|---|
| Add a sector | Append a `SectorDef` in `GameData.Sectors` (enemies, hazards, boss patterns, colours). |
| Add an enemy type | Add to `EnemyKind` + `GameData.Enemies`, a `case` in `Enemy.Update`/`Shoot`, and a silhouette in `ShipFactory.BuildEnemy`. |
| Add a boss pattern | Add to `BossPattern`, implement in `Boss.RunPattern`, reference it from a `BossDef.phases`. |
| Add a weapon | Add to the enum + def list in `GameData`; primary firing lives in `PlayerWeapons.FirePrimary`, secondaries in `FireSecondary` / `Missile`. |
| Add a ship | Append a `ShipDef`; optionally a new `ShipShape` branch in `ShipFactory.BuildPlayer`. |
| Real models/audio | See INSTALLATION.md → Optional: real assets. |
| Cloud save | Set `((CloudMirrorSaveBackend)SaveSystem.Backend).PushToCloud = json => …` after `SaveSystem.Init()`. |
| Addressables / LOD | The asset pipeline is intentionally minimal (no imported assets). When you add real content, put it in Addressables groups and load via `Addressables.LoadAssetAsync` inside `ShipFactory`; add `LODGroup`s to prefabs. |

## Performance budget

- Target: 60 FPS on a 2019 mid-range Android phone, 120 FPS on desktop.
- No shadows, no realtime GI, flat ambient, two directional lights.
- ≤ ~200 active pooled objects in the worst boss phase; all share a handful of cached materials (SRP batcher friendly).
- Particle systems capped (`maxParticles`), additive soft-dot texture 64×64.
- Physics: kinematic triggers only, gravity off, bullets ignore bullets.
- UI polls, no per-frame allocations beyond string formatting in the HUD (acceptable; swap to cached strings if profiling shows GC spikes).
