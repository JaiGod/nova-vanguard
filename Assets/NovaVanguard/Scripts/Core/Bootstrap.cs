using UnityEngine;

namespace NovaVanguard
{
    /// <summary>
    /// Entry point. The Main scene contains one GameObject with this component; everything else
    /// (camera, lights, background, pools, audio, UI, game manager) is created here in code.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Bootstrap : MonoBehaviour
    {
        static bool booted;

        void Awake()
        {
            if (booted) { Destroy(gameObject); return; }
            booted = true;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = Application.isMobilePlatform ? 60 : 120;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Physics.simulationMode = SimulationMode.FixedUpdate;
            Physics.gravity = Vector3.zero;
            // Bullets never collide with other bullets.
            Physics.IgnoreLayerCollision(GameBounds.BulletLayer, GameBounds.BulletLayer, true);

            SaveSystem.Init();
            QualityManager.Apply();
            CameraRig.Create();
            SpaceBackground.Create();
            AudioManager.Create();

            Projectile.RegisterPool();
            Missile.RegisterPool();
            Enemy.RegisterPool();
            Hazard.RegisterPool();
            Pickup.RegisterPool();
            Vfx.RegisterPools();

            UIManager.Create();
            GameManager.Create();
            UIManager.Instance.ShowMainMenu();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) { SaveSystem.Save(); if (GameManager.Instance != null) GameManager.Instance.Pause(); }
        }

        void OnApplicationQuit() { SaveSystem.Save(); }
    }

    /// <summary>Picks a quality tier (auto on first run from device memory / platform) and applies it.</summary>
    public static class QualityManager
    {
        public static int Tier { get; private set; }  // 0 low, 1 medium, 2 high

        public static void Apply()
        {
            int q = SaveSystem.Data != null ? SaveSystem.Data.settings.quality : -1;
            if (q < 0)
            {
                if (Application.isMobilePlatform) q = SystemInfo.systemMemorySize >= 6000 ? 2 : SystemInfo.systemMemorySize >= 3500 ? 1 : 0;
                else if (Application.platform == RuntimePlatform.WebGLPlayer) q = 1;
                else q = 2;
            }
            Tier = Mathf.Clamp(q, 0, 2);
            QualitySettings.SetQualityLevel(Mathf.Min(Tier, QualitySettings.names.Length - 1), true);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.lodBias = Tier == 0 ? 0.7f : 1f;
            PostFx.SetHighQuality(Tier >= 1);
            if (Tier == 0 && Application.isMobilePlatform) Screen.SetResolution(Mathf.RoundToInt(Screen.currentResolution.width * 0.75f), Mathf.RoundToInt(Screen.currentResolution.height * 0.75f), true);
        }
    }
}
