# Release Notes

## v1.0.0 – Initial release

First complete, playable build of Nova Vanguard.

**Gameplay**
- 7 sectors (Training Galaxy, Asteroid Belt, Alien Territory, Nebula Zone, Ancient Machine World, Dark Matter Sector, Final War Zone), each with 5–9 waves, unique hazards and a boss.
- 5 enemy types with distinct AI; 7 three-phase bosses with shield generators, core weak points and 8 attack patterns.
- 4 primary weapons, 4 secondaries, 4 ultimate abilities, 7 ships (2 legendary), 7 upgrade stats × 10 levels.
- Hazards: splitting asteroids, debris, comets, laser gates, wormholes, black holes.
- Pickups: credits, repair, missiles, energy. Combo scoring, three lives per sector, half-credit salvage on game over.

**Platforms & input**
- Windows / macOS / Linux / Android / iOS / WebGL build targets with menu + headless build entry points.
- Keyboard & mouse, gamepad, and touch (floating joystick or drag mode, auto-fire option, auto-aim).

**Tech**
- Unity 6 + URP 17 with bloom, ACES, vignette, reactive chromatic aberration; graceful Built-in fallback.
- Fully procedural art, VFX, SFX and music – zero binary assets in the repo.
- Object pooling throughout, adaptive quality tiers for mobile, aspect-ratio-independent playfield.
- JSON save system with cloud-ready backend abstraction, statistics tracking.
- Automatic project setup on first open; GitHub Actions workflows for tagged releases and WebGL → GitHub Pages (opt-in via `UNITY_CI_ENABLED` variable).

**Known limitations**
- Models are primitive-based placeholders by design; swap in via `ShipFactory`.
- Voice alerts play an alarm tone until clips are added to `Resources/Audio/Voice`.
- Cloud save requires wiring `CloudMirrorSaveBackend.PushToCloud` to a service.
