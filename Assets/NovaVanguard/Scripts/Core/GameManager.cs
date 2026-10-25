using System.Collections;
using UnityEngine;

namespace NovaVanguard
{
    public enum GameState { Boot, Menu, Playing, Paused, SectorClear, GameOver }

    /// <summary>
    /// Owns the run: sector flow, waves, scoring, win/lose, pause. Created by <see cref="Bootstrap"/>.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;
        public SectorDef CurrentSector { get; private set; }
        public int SectorIndex { get; private set; } = 1;     // 1-based
        public int Wave { get; private set; }
        public int RunScore { get; private set; }
        public int RunCredits { get; private set; }
        public int Combo { get; private set; }
        public float Difficulty => CurrentSector != null ? CurrentSector.difficulty : 1f;
        public bool GodMode;                                   // debug: F1 in editor
        public PlayerShip Player { get; private set; }
        public UltimateSystem Ultimate { get; private set; }
        public string Objective { get; private set; } = "";
        public bool BossActive => Boss.Current != null;

        float comboTimer, runTime, hazardTimer, spawnTimer;
        int aliveEnemies, waveEnemiesLeft, waveToSpawn;
        bool waveActive, bossPhase;
        Coroutine flow;

        public static GameManager Create()
        {
            var go = new GameObject("GameManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameManager>();
            return Instance;
        }

        void Start() { State = GameState.Menu; AudioManager.PlayMusic(Music.Menu); SpaceBackground.Instance?.ApplySector(GameData.Sectors[0]); }

        void Update()
        {
            InputRouter.Tick(CameraRig.Instance != null ? CameraRig.Instance.Cam : null);
            PostFx.Tick(Time.unscaledDeltaTime);
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F1)) { GodMode = !GodMode; HUD.Alert(GodMode ? "GOD MODE ON" : "GOD MODE OFF", Color.magenta); }
            if (Input.GetKeyDown(KeyCode.F2) && State == GameState.Playing) KillAllEnemies();
            if (Input.GetKeyDown(KeyCode.F3)) { SaveSystem.AddCredits(5000); SaveSystem.Save(); }
#endif
            if (State == GameState.Playing)
            {
                runTime += Time.deltaTime;
                SaveSystem.Data.stats.playTimeSeconds += Time.deltaTime;
                if (comboTimer > 0f) { comboTimer -= Time.deltaTime; if (comboTimer <= 0f) Combo = 0; }
                if (InputRouter.PausePressed) Pause();
                TickSpawning();
            }
            else if (State == GameState.Paused && InputRouter.PausePressed) Resume();
        }

        // ------------------------------------------------------------------ run flow
        public void StartSector(int sector)
        {
            SectorIndex = Mathf.Clamp(sector, 1, GameData.Sectors.Count);
            CurrentSector = GameData.Sectors[SectorIndex - 1];
            RunScore = 0; RunCredits = 0; Combo = 0; runTime = 0f;
            Pool.DespawnAll();
            ClearWorldObjects();
            SpaceBackground.Instance.ApplySector(CurrentSector);

            if (Player == null)
            {
                Player = PlayerShip.Spawn(GameData.Ship(SaveSystem.Data.selectedShip));
                Ultimate = UltimateSystem.Attach(Player);
            }
            else if (Player.Def.id != SaveSystem.Data.selectedShip || Ultimate.Ability != (UltimateAbility)SaveSystem.Data.selectedUltimate)
            {
                Destroy(Player.gameObject);
                Player = PlayerShip.Spawn(GameData.Ship(SaveSystem.Data.selectedShip));
                Ultimate = UltimateSystem.Attach(Player);
            }
            Player.ResetForSector();
            Player.Weapons.Init(Player);

            State = GameState.Playing;
            Time.timeScale = 1f;
            UIManager.Instance.ShowHud();
            AudioManager.PlayMusic(Music.Combat);
            if (flow != null) StopCoroutine(flow);
            flow = StartCoroutine(SectorFlow());
        }

        IEnumerator SectorFlow()
        {
            bossPhase = false;
            HUD.Banner($"SECTOR {SectorIndex}", CurrentSector.name, CurrentSector.tagline);
            Objective = "Survive the waves";
            yield return new WaitForSeconds(2.5f);
            for (Wave = 1; Wave <= CurrentSector.waves; Wave++)
            {
                if (SectorIndex == 1 && Wave == 1) HUD.Tutorial();
                int count = Mathf.RoundToInt((4 + Wave * 1.5f) * Mathf.Sqrt(Difficulty));
                Objective = $"Wave {Wave}/{CurrentSector.waves}  -  destroy all enemies";
                HUD.Alert($"WAVE {Wave}", Color.white, 0.9f);
                waveToSpawn = count; waveEnemiesLeft = count; waveActive = true; spawnTimer = 0.2f;
                float timeout = 40f + count * 2f, t = 0f;
                while (waveEnemiesLeft > 0 && t < timeout) { t += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(1.2f);
            }
            waveActive = false;
            Objective = "WARNING  -  boss approaching";
            HUD.Alert("WARNING", new Color(1f, 0.25f, 0.2f), 1.5f);
            yield return new WaitForSeconds(1.5f);
            bossPhase = true;
            Boss.Spawn(CurrentSector.boss, Difficulty);
            Objective = $"Destroy {CurrentSector.boss.name}";
        }

        void TickSpawning()
        {
            float dt = Time.deltaTime;
            if (waveActive && waveToSpawn > 0)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    int group = Mathf.Min(waveToSpawn, Random.Range(1, 4));
                    var kinds = CurrentSector.enemies;
                    // Weighted: later waves lean on tougher types
                    float waveT = Wave / (float)Mathf.Max(1, CurrentSector.waves);
                    var kind = kinds[Mathf.Min(kinds.Length - 1, Mathf.FloorToInt(Mathf.Pow(Random.value, 1.6f - waveT) * kinds.Length))];
                    var basePos = GameBounds.RandomTop(2f);
                    for (int i = 0; i < group; i++)
                    {
                        var pos = basePos + new Vector3((i - (group - 1) * 0.5f) * 2.4f, 0f, i * 0.8f);
                        pos.x = Mathf.Clamp(pos.x, GameBounds.MinX + 1.5f, GameBounds.MaxX - 1.5f);
                        Enemy.Spawn(kind, pos, Difficulty);
                        aliveEnemies++;
                    }
                    waveToSpawn -= group;
                    spawnTimer = Mathf.Lerp(1.6f, 0.7f, waveT) / Mathf.Sqrt(Difficulty);
                }
            }
            // Hazards keep coming through waves and the boss
            if (CurrentSector.hazards.Length > 0)
            {
                hazardTimer -= dt;
                if (hazardTimer <= 0f)
                {
                    var h = CurrentSector.hazards[Random.Range(0, CurrentSector.hazards.Length)];
                    SpawnHazard(h);
                    hazardTimer = Random.Range(2.5f, 5f) / Mathf.Sqrt(Difficulty) * (bossPhase ? 1.6f : 1f);
                }
            }
        }

        void SpawnHazard(HazardKind h)
        {
            switch (h)
            {
                case HazardKind.Asteroid: case HazardKind.Debris: case HazardKind.Comet:
                    Hazard.Spawn(h, GameBounds.RandomTop(3f), Difficulty); break;
                case HazardKind.LaserGate: LaserGate.Create(GameBounds.MaxZ + 1f, 12f); break;
                case HazardKind.Wormhole: Wormhole.Create(GameBounds.RandomInUpperHalf(), 10f); break;
                case HazardKind.BlackHole:
                    if (FindObjectsOfType<BlackHoleEffect>().Length < 1)
                        BlackHoleEffect.Create(GameBounds.RandomInUpperHalf(), new Color(0.5f, 0.3f, 1f), 9f, 12f, false);
                    break;
            }
        }

        // ------------------------------------------------------------------ callbacks
        public void OnEnemyKilled(Enemy e, bool fromBoss)
        {
            if (!fromBoss) { waveEnemiesLeft = Mathf.Max(0, waveEnemiesLeft - 1); aliveEnemies--; }
            Combo++; comboTimer = 2.5f;
            int score = Mathf.RoundToInt(e.Def.score * Difficulty * (1f + Mathf.Min(Combo, 20) * 0.1f));
            RunScore += score;
            SaveSystem.Data.stats.enemiesDestroyed++;
            Player.AddUltimateCharge(e.Def.kind == EnemyKind.Elite ? 12f : 4f);
            HUD.Float(e.transform.position, Combo >= 3 ? $"+{score}  x{Combo}" : $"+{score}", Color.white);
            // Drops
            float r = Random.value;
            int credits = Mathf.RoundToInt(e.Def.credits * (0.8f + 0.2f * Difficulty));
            if (r < 0.55f) Pickup.Drop(e.transform.position, Pickup.Kind.Credits, credits);
            else if (r < 0.63f) Pickup.Drop(e.transform.position, Pickup.Kind.Repair, 25);
            else if (r < 0.72f) Pickup.Drop(e.transform.position, Pickup.Kind.Missiles, 3);
            else if (r < 0.76f) Pickup.Drop(e.transform.position, Pickup.Kind.Energy, 0);
            else AddRunCredits(credits / 2);
        }

        public void OnHazardDestroyed(Hazard h)
        {
            RunScore += 20; Player.AddUltimateCharge(1f);
            if (Random.value < 0.25f) Pickup.Drop(h.transform.position, Pickup.Kind.Credits, 10);
        }

        public void AddRunCredits(int c) { RunCredits += c; }

        public void OnBossDying() { Objective = "Boss destroyed!"; }

        public void OnBossDefeated(Boss b)
        {
            SaveSystem.Data.stats.bossesDefeated++;
            RunScore += Mathf.RoundToInt(b.Def.credits * 5 * Difficulty);
            RunCredits += b.Def.credits;
            AddRunCredits(0);
            StartCoroutine(SectorClear());
        }

        IEnumerator SectorClear()
        {
            State = GameState.SectorClear;
            AudioManager.PlayMusic(Music.Victory);
            Projectile.ClearEnemyBullets(Vector3.zero, 1000f);
            bool first = SaveSystem.Data.highestSectorCleared < SectorIndex;
            if (first) { SaveSystem.Data.highestSectorCleared = SectorIndex; SaveSystem.Data.stats.sectorsCleared++; }
            int bonus = Mathf.RoundToInt(Player.Lives * 100 * Difficulty) + (first ? 500 : 0);
            RunCredits += bonus;
            SaveSystem.AddCredits(RunCredits);
            if (RunScore > SaveSystem.Data.stats.highScore) SaveSystem.Data.stats.highScore = RunScore;
            SaveSystem.Save();
            yield return new WaitForSeconds(1.5f);
            UIManager.Instance.ShowSectorClear(RunScore, RunCredits, bonus, first, SectorIndex >= GameData.Sectors.Count);
        }

        public void OnPlayerDied(PlayerShip p)
        {
            if (p.Lives > 0)
            {
                HUD.Alert($"{p.Lives} {(p.Lives == 1 ? "LIFE" : "LIVES")} LEFT", new Color(1f, 0.5f, 0.3f), 1.2f);
                StartCoroutine(RespawnAfter(2f));
            }
            else StartCoroutine(GameOver());
        }

        IEnumerator RespawnAfter(float t)
        {
            Player.gameObject.SetActive(false);
            yield return new WaitForSeconds(t);
            if (State != GameState.Playing) yield break;
            Player.gameObject.SetActive(true);
            Player.Respawn();
        }

        IEnumerator GameOver()
        {
            State = GameState.GameOver;
            Player.gameObject.SetActive(false);
            // Keep half the credits earned so a failed run still progresses the player.
            int kept = RunCredits / 2;
            SaveSystem.AddCredits(kept);
            if (RunScore > SaveSystem.Data.stats.highScore) SaveSystem.Data.stats.highScore = RunScore;
            SaveSystem.Save();
            AudioManager.PlayMusic(Music.None);
            yield return new WaitForSeconds(1.8f);
            UIManager.Instance.ShowGameOver(RunScore, kept, SectorIndex, Wave);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused; Time.timeScale = 0f;
            UIManager.Instance.ShowPause();
            AudioManager.PlayUI(Sfx.UiClick);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing; Time.timeScale = 1f;
            UIManager.Instance.ShowHud();
            InputRouter.ClearTouch();
        }

        public void QuitToMenu()
        {
            if (flow != null) StopCoroutine(flow);
            StopAllCoroutines();
            Time.timeScale = 1f;
            Pool.DespawnAll();
            ClearWorldObjects();
            if (Player != null) Player.gameObject.SetActive(false);
            State = GameState.Menu;
            SaveSystem.Save();
            AudioManager.PlayMusic(Music.Menu);
            SpaceBackground.Instance.ApplySector(GameData.Sectors[Mathf.Clamp(SaveSystem.Data.highestSectorCleared, 0, GameData.Sectors.Count - 1)]);
            UIManager.Instance.ShowMainMenu();
        }

        void ClearWorldObjects()
        {
            foreach (var b in FindObjectsOfType<Boss>()) Destroy(b.gameObject);
            foreach (var b in FindObjectsOfType<BlackHoleEffect>()) Destroy(b.gameObject);
            foreach (var b in FindObjectsOfType<LaserGate>()) Destroy(b.gameObject);
            foreach (var b in FindObjectsOfType<Wormhole>()) Destroy(b.gameObject);
            foreach (var b in FindObjectsOfType<Drone>()) Destroy(b.gameObject);
            Boss.CurrentReset();
        }

        void KillAllEnemies()
        {
            foreach (var u in Unit.All.ToArray()) if (u is Enemy && u.IsAlive) u.TakeDamage(99999f, u.transform.position, Team.Player);
            if (Boss.Current != null) Boss.Current.TakeDamage(99999f, Boss.Current.transform.position, Team.Player);
        }
    }
}
