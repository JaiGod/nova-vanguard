# API Reference

Namespace `NovaVanguard` (runtime) and `NovaVanguard.Editor` (editor). Only public surface area that other systems are expected to call is listed.

## Core

### `Bootstrap : MonoBehaviour`
Scene entry point; constructs all services in `Awake` (see ARCHITECTURE.md). Saves on pause/quit.

### `GameManager : MonoBehaviour` (singleton `Instance`)
| Member | Description |
|---|---|
| `GameState State` | `Boot, Menu, Playing, Paused, SectorClear, GameOver` |
| `SectorDef CurrentSector`, `int SectorIndex`, `int Wave` | Current run position |
| `int RunScore`, `int RunCredits`, `int Combo` | Run tallies |
| `float Difficulty` | Sector difficulty multiplier |
| `PlayerShip Player`, `UltimateSystem Ultimate` | Player references |
| `string Objective` | Text shown at the top of the HUD |
| `void StartSector(int sector)` | Begin / restart a 1-based sector |
| `void Pause()`, `void Resume()`, `void QuitToMenu()` | Flow control |
| `void OnEnemyKilled(Enemy, bool fromBoss)`, `OnHazardDestroyed(Hazard)`, `OnBossDying()`, `OnBossDefeated(Boss)`, `OnPlayerDied(PlayerShip)` | Gameplay callbacks |
| `void AddRunCredits(int)` | Add credits to the current run |
| `bool GodMode` | Debug invulnerability |

### `GameBounds` (static)
`MinX/MaxX/MinZ/MaxZ`, `Width`, `Height`, `Center`, `Recalculate(Camera)`, `Inside(Vector3, margin)`, `Clamp(Vector3, margin)`, `RandomTop(above)`, `RandomInUpperHalf()`. `BulletLayer = 4`.

### `Pool` (static)
`Register(key, Func<GameObject>)`, `IsRegistered(key)`, `Prewarm(key, n)`, `Spawn(key, pos, rot)`, `Spawn<T>(key, pos, rot)`, `Despawn(GameObject)`, `DespawnAll()`.
Interface `IPoolable { string PoolKey; void OnSpawned(); void OnDespawned(); }`.

### `MaterialLibrary` (static)
`IsURP`, `Lit(color, emission=0, smooth, metal)`, `Glow(color, intensity)`, `Particle(color)`, `Nebula(color)`, `SoftDot`, `NebulaTexture`.

### `ShipFactory` (static)
`Part(parent, PrimitiveType, pos, euler, scale, material)`, `BuildPlayer(parent, ShipDef, out Vector3[] enginePoints)`, `BuildEnemy(parent, EnemyDef)`, `BuildBoss(parent, BossDef, out Vector3[] generatorPoints, out Transform core)`.

### `CameraRig : MonoBehaviour`
`Instance`, `Cam`, `static Shake(float trauma)`.

### `PostFx` (static)
`Setup(Camera)`, `Kick(float)`, `Tick(dt)`, `SetHighQuality(bool)`.

### `QualityManager` (static)
`Tier` (0–2), `Apply()`.

## Data (`GameData`, static)
`Ships`, `Primaries`, `Secondaries`, `Ultimates`, `Enemies`, `Sectors`, `UpgradeNames`, `UpgradeDescriptions`, `MaxUpgradeLevel`, `UpgradeCost(stat, level)`, `Ship(id)`, `Primary(w)`, `Secondary(w)`, `Ultimate(u)`.
Definition classes: `ShipDef`, `PrimaryDef`, `SecondaryDef`, `UltimateDef`, `EnemyDef`, `SectorDef`, `BossDef`.
Enums: `Team`, `PrimaryWeapon`, `SecondaryWeapon`, `UltimateAbility`, `EnemyKind`, `HazardKind`, `UpgradeStat`, `ShipShape`, `BossPattern`.

## Combat

### `Unit : MonoBehaviour` (abstract)
`team`, `maxHealth`, `health`, `IsAlive`, `Targetable`, `DamageTakenMultiplier`, `StunTimer`, `TakeDamage(amount, point, sourceTeam)`, `Stun(seconds)`, `static All`, `static Closest(from, team, maxDist, includeNeutral)`.

### `Combat` (static)
`Explode(pos, radius, damage, sourceTeam, color, shake)`, `Stun(pos, radius, seconds, sourceTeam)`.

### `Projectile : MonoBehaviour, IPoolable`
`static Fire(pos, dir, speed, damage, team, color, size, life=3, splash=0, pierce=0, stretch=2.6)`, `static ClearEnemyBullets(center, radius)`, `static EnemyBullets`.

### `Missile : MonoBehaviour, IPoolable`
`static Launch(pos, dir, MissileMode, damage, team, color, speed=22)`. `MissileMode { Dumb, Homing, EMP, Cluster, Bomblet }`.

### `GravityWell : MonoBehaviour`
`strength`, `radius`, `pullsPlayer`, `pullsEnemies`, `PullAt(point)`, `static TotalPull(point, isPlayer)`, `static Active`.

## Player
### `PlayerShip : Unit`
`Instance`, `Def`, `Weapons`, stats (`MaxShield`, `MaxEnergy`, `Speed`, `Accel`, `BoostMul`, `DamageMul`, `FireRateMul`, `RangeMul`, `ShieldRegen`, `MaxMissiles`), state (`Shield`, `Energy`, `UltimateCharge`, `Missiles`, `Lives`, `Boosting`, `InvulnTimer`), `Controllable`, `Spawn(ShipDef)`, `RecalculateStats()`, `ResetForSector()`, `Respawn()`, `AddUltimateCharge(f)`, `Heal(hp, shield)`.
Interface `IContactDamage { float ContactDamage; void OnContact(PlayerShip); }`.

### `PlayerWeapons : MonoBehaviour`
`Primary`, `Secondary`, `PrimaryDef`, `SecondaryDef`, `SecondaryCooldown`, `PrimaryHeat`, `Init(PlayerShip)`, `TrySelectPrimary(idx)`, `CyclePrimary()`, `CycleSecondary()`.

### `UltimateSystem : MonoBehaviour`
`Attach(PlayerShip)`, `Ability`, `Def`, `Active`.
Helpers: `BlackHoleEffect.Create(pos, color, life, dps, playerOwned)`, `Drone.Spawn(owner, index, total, color, damage, life)`.

## Enemies
### `Enemy : Unit, IPoolable, IContactDamage`
`static Spawn(EnemyKind, pos, difficulty, fromBoss=false)`, `Def`.
### `Boss : Unit, IContactDamage`
`static Current`, `static Spawn(BossDef, difficulty)`, `Def`, `Phase`, `ShieldHealth`, `ShieldUp`, `Entering`, `OnGeneratorDestroyed(BossGenerator)`.
### `BossGenerator : Unit`
`Create(owner, localPos, accent, hp)`.

## World
`Hazard.Spawn(HazardKind, pos, difficulty)`, `LaserGate.Create(z, life)`, `Wormhole.Create(pos, life)`, `Pickup.Drop(pos, Pickup.Kind, value)`, `SpaceBackground.Instance.ApplySector(SectorDef)`, `SpaceBackground.SetScroll(f)`.

## VFX (`Vfx`, static)
`RegisterPools()`, `Explosion(pos, color, scale)`, `Hit(pos, color)`, `Shockwave(pos, color, radius, duration)`, `Warp(pos, color)`, `NewSystem(name, parent, color)`, `Thruster(parent, localPos, color, size)`.

## Audio
`AudioManager.Play(Sfx, Vector3? pos, volume, pitch)`, `PlayUI(Sfx)`, `Voice(key)`, `PlayMusic(Music)`, `Vibrate()`, `Instance.ApplyVolumes()`.
`Synth.MakeSfx(Sfx, rate)`, `Synth.MakeMusic(Music, rate)`.

## Input (`InputRouter`, static)
Outputs: `Move`, `AimPoint`, `AimDirFromStick`, `Fire`, `Boost`, `SecondaryPressed`, `UltimatePressed`, `PausePressed`, `CyclePrimaryPressed`, `CycleSecondaryPressed`, `SelectPrimary`, `DragDelta`, `UsingTouch`, `UsingGamepad`, `ShowTouchControls`, `UiBlocking`.
Touch inputs: `TouchStick`, `TouchFire`, `TouchBoost`, `TouchSecondary`, `TouchUltimate`, `TouchCyclePrimary`, `TouchCycleSecondary`, `TouchPause`, `TouchDragScreenDelta`. `Tick(Camera)`, `ClearTouch()`.

## Save (`SaveSystem`, static)
`Data` (`SaveData`), `Backend` (`ISaveBackend`), `OnChanged`, `Init()`, `Save()`, `ResetProgress()`, `AddCredits(int)`, `Spend(int) → bool`.
`SaveData`: `credits`, `highestSectorCleared`, `unlockedShips`, `unlockedPrimaries/Secondaries/Ultimates`, `selectedShip/Primary/Secondary/Ultimate`, `upgrades[7]`, `settings` (`GameSettings`), `stats` (`GameStatistics`), `UpgradeLevel(stat)`.
Backends: `FileSaveBackend`, `PlayerPrefsSaveBackend`, `CloudMirrorSaveBackend { Action<string> PushToCloud }`.

## UI
`UIManager.Instance`: `ShowMainMenu()`, `ShowHud()`, `ShowPause()`, `ShowSectorClear(score, credits, bonus, firstClear, finalSector)`, `ShowGameOver(score, kept, sector, wave)`.
`HUD` static: `Alert(text, color, hold)`, `Banner(title, name, tag)`, `BossIntro(BossDef)`, `Tutorial()`, `Float(worldPos, text, color)`.
`UI` static builder: `CreateCanvas`, `EnsureEventSystem`, `Rect`, `Fill`, `Anchored`, `Image`, `Label`, `Button`, `Slider`, `Toggle`, `Bar`, `Header`, colour constants.

## Editor (`NovaVanguard.Editor`)
`NovaProjectSetup.RunSetup()` – menu *Nova Vanguard → Run Project Setup*.
`NovaBuild.Windows/Mac/Linux/Android/AndroidBundle/iOS/WebGL/AllDesktopMobileWeb()` – menu *Nova Vanguard → Build → …*, also `-executeMethod` targets.
