using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NovaVanguard
{
    [Serializable]
    public class GameSettings
    {
        public float masterVolume = 0.9f, musicVolume = 0.6f, sfxVolume = 0.85f;
        public bool autoFire = false;
        public bool dragControls = false;       // mobile: drag-to-move instead of joystick
        public bool screenShake = true;
        public bool vibration = true;
        public bool showFps = false;
        public int quality = -1;                // -1 = auto
        public bool touchControlsOnDesktop = false;
    }

    [Serializable]
    public class GameStatistics
    {
        public int enemiesDestroyed, bossesDefeated, deaths, sectorsCleared, shotsFired, missilesFired, ultimatesUsed;
        public long creditsEarned, highScore;
        public float playTimeSeconds;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int credits = 300;
        public int highestSectorCleared = 0;    // 0 = none
        public List<string> unlockedShips = new List<string> { "starter" };
        public List<int> unlockedPrimaries = new List<int> { 0 };
        public List<int> unlockedSecondaries = new List<int> { 0 };
        public List<int> unlockedUltimates = new List<int> { 0 };
        public string selectedShip = "starter";
        public int selectedPrimary = 0, selectedSecondary = 0, selectedUltimate = 0;
        public int[] upgrades = new int[7];
        public GameSettings settings = new GameSettings();
        public GameStatistics stats = new GameStatistics();
        public string lastSavedUtc;

        public int UpgradeLevel(UpgradeStat s) => upgrades != null && (int)s < upgrades.Length ? upgrades[(int)s] : 0;
    }

    /// <summary>Storage abstraction so the game can switch to cloud saves without touching gameplay code.</summary>
    public interface ISaveBackend
    {
        bool TryLoad(out string json);
        void Save(string json);
        void Delete();
    }

    public class FileSaveBackend : ISaveBackend
    {
        readonly string path;
        public FileSaveBackend(string fileName) { path = Path.Combine(Application.persistentDataPath, fileName); }
        public bool TryLoad(out string json)
        {
            json = null;
            try { if (File.Exists(path)) { json = File.ReadAllText(path); return true; } }
            catch (Exception e) { Debug.LogWarning("[Save] load failed: " + e.Message); }
            return false;
        }
        public void Save(string json)
        {
            try
            {
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e) { Debug.LogWarning("[Save] write failed: " + e.Message); }
        }
        public void Delete() { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }

    /// <summary>Used on WebGL, where PlayerPrefs maps to browser storage reliably.</summary>
    public class PlayerPrefsSaveBackend : ISaveBackend
    {
        const string Key = "nova_vanguard_save";
        public bool TryLoad(out string json) { json = PlayerPrefs.GetString(Key, null); return !string.IsNullOrEmpty(json); }
        public void Save(string json) { PlayerPrefs.SetString(Key, json); PlayerPrefs.Save(); }
        public void Delete() { PlayerPrefs.DeleteKey(Key); }
    }

    /// <summary>
    /// Cloud-ready placeholder. Wire this to Unity Cloud Save (com.unity.services.cloudsave) or your own backend:
    /// mirror writes to local storage immediately and push to the cloud asynchronously.
    /// </summary>
    public class CloudMirrorSaveBackend : ISaveBackend
    {
        readonly ISaveBackend local;
        public Action<string> PushToCloud;   // assign to your cloud upload call
        public CloudMirrorSaveBackend(ISaveBackend local) { this.local = local; }
        public bool TryLoad(out string json) => local.TryLoad(out json);
        public void Save(string json) { local.Save(json); PushToCloud?.Invoke(json); }
        public void Delete() => local.Delete();
    }

    public static class SaveSystem
    {
        public static SaveData Data { get; private set; }
        public static ISaveBackend Backend { get; set; }
        public static event Action OnChanged;

        public static void Init()
        {
            if (Backend == null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                Backend = new CloudMirrorSaveBackend(new PlayerPrefsSaveBackend());
#else
                Backend = new CloudMirrorSaveBackend(new FileSaveBackend("nova_vanguard_save.json"));
#endif
            }
            Data = null;
            if (Backend.TryLoad(out var json))
            {
                try { Data = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning("[Save] corrupt save, starting fresh: " + e.Message); }
            }
            if (Data == null) Data = new SaveData();
            Sanitize();
        }

        static void Sanitize()
        {
            if (Data.upgrades == null || Data.upgrades.Length < 7) { var u = new int[7]; if (Data.upgrades != null) Array.Copy(Data.upgrades, u, Mathf.Min(7, Data.upgrades.Length)); Data.upgrades = u; }
            if (Data.unlockedShips == null || Data.unlockedShips.Count == 0) Data.unlockedShips = new List<string> { "starter" };
            if (Data.unlockedPrimaries == null || Data.unlockedPrimaries.Count == 0) Data.unlockedPrimaries = new List<int> { 0 };
            if (Data.unlockedSecondaries == null || Data.unlockedSecondaries.Count == 0) Data.unlockedSecondaries = new List<int> { 0 };
            if (Data.unlockedUltimates == null || Data.unlockedUltimates.Count == 0) Data.unlockedUltimates = new List<int> { 0 };
            if (Data.settings == null) Data.settings = new GameSettings();
            if (Data.stats == null) Data.stats = new GameStatistics();
            if (!Data.unlockedShips.Contains(Data.selectedShip)) Data.selectedShip = "starter";
        }

        public static void Save()
        {
            if (Data == null) return;
            Data.lastSavedUtc = DateTime.UtcNow.ToString("o");
            Backend.Save(JsonUtility.ToJson(Data, true));
            OnChanged?.Invoke();
        }

        public static void ResetProgress()
        {
            var keepSettings = Data != null ? Data.settings : new GameSettings();
            Backend.Delete();
            Data = new SaveData { settings = keepSettings };
            Save();
        }

        public static void AddCredits(int amount)
        {
            Data.credits += amount;
            if (amount > 0) Data.stats.creditsEarned += amount;
            OnChanged?.Invoke();
        }

        public static bool Spend(int amount)
        {
            if (Data.credits < amount) return false;
            Data.credits -= amount; Save(); return true;
        }
    }
}
