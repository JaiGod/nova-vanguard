using System.Collections.Generic;
using UnityEngine;

namespace NovaVanguard
{
    public enum Sfx { Laser, Plasma, MachineGun, Pulse, Missile, Explosion, BigExplosion, Hit, ShieldHit, Emp, Ultimate, Pickup, UiClick, UiBack, Purchase, Alarm, Boost, PowerDown, Warp, EnemyShot }
    public enum Music { None, Menu, Combat, Boss, Victory }

    /// <summary>
    /// Central audio service. All sounds and music are synthesized at startup, so the game has full
    /// audio with zero imported assets. To use real audio, drop clips into Resources/Audio/Sfx/{SfxName}
    /// or Resources/Audio/Music/{MusicName} (and Resources/Audio/Voice/{key} for voice alerts) and they
    /// will be picked automatically instead of the synthesized versions.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        readonly Dictionary<Sfx, AudioClip> sfx = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Music, AudioClip> music = new Dictionary<Music, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        AudioSource musicA, musicB, ui;
        AudioSource[] voices;
        int nextVoice;
        Music current = Music.None;
        float crossfade = 1f;
        const int Rate = 22050;

        public static AudioManager Create()
        {
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            var am = go.AddComponent<AudioManager>();
            Instance = am;
            am.Build();
            return am;
        }

        void Build()
        {
            musicA = gameObject.AddComponent<AudioSource>(); musicA.loop = true; musicA.playOnAwake = false;
            musicB = gameObject.AddComponent<AudioSource>(); musicB.loop = true; musicB.playOnAwake = false;
            ui = gameObject.AddComponent<AudioSource>(); ui.playOnAwake = false;
            voices = new AudioSource[16];
            for (int i = 0; i < voices.Length; i++)
            {
                var v = new GameObject("Voice" + i).AddComponent<AudioSource>();
                v.transform.SetParent(transform);
                v.playOnAwake = false;
                v.spatialBlend = 0.55f;     // partially 3D: you can hear where explosions are
                v.rolloffMode = AudioRolloffMode.Linear; v.minDistance = 10f; v.maxDistance = 80f;
                v.dopplerLevel = 0f;
                voices[i] = v;
            }
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx)))
                sfx[s] = Resources.Load<AudioClip>("Audio/Sfx/" + s) ?? Synth.MakeSfx(s, Rate);
            foreach (Music m in System.Enum.GetValues(typeof(Music)))
                if (m != Music.None) music[m] = Resources.Load<AudioClip>("Audio/Music/" + m) ?? Synth.MakeMusic(m, Rate);
            ApplyVolumes();
        }

        public void ApplyVolumes()
        {
            var s = SaveSystem.Data != null ? SaveSystem.Data.settings : new GameSettings();
            AudioListener.volume = s.masterVolume;
        }

        static float SfxVol => SaveSystem.Data != null ? SaveSystem.Data.settings.sfxVolume : 0.8f;
        static float MusicVol => SaveSystem.Data != null ? SaveSystem.Data.settings.musicVolume : 0.6f;

        public static void Play(Sfx s, Vector3? pos = null, float volume = 1f, float pitch = 1f)
        {
            var am = Instance; if (am == null) return;
            // Throttle identical sounds so 20 bullets in one frame don't clip.
            float now = Time.unscaledTime;
            if (am.lastPlayed.TryGetValue(s, out float last) && now - last < 0.035f) return;
            am.lastPlayed[s] = now;
            var v = am.voices[am.nextVoice]; am.nextVoice = (am.nextVoice + 1) % am.voices.Length;
            v.transform.position = pos ?? (CameraRig.Instance != null ? CameraRig.Instance.transform.position : Vector3.zero);
            v.spatialBlend = pos.HasValue ? 0.55f : 0f;
            v.pitch = pitch * Random.Range(0.94f, 1.06f);
            v.volume = volume * SfxVol;
            v.clip = am.sfx[s];
            v.Play();
        }

        public static void PlayUI(Sfx s)
        {
            var am = Instance; if (am == null) return;
            am.ui.PlayOneShot(am.sfx[s], SfxVol * 0.8f);
        }

        /// <summary>Voice alert: plays Resources/Audio/Voice/{key} if present, otherwise an alarm tone.</summary>
        public static void Voice(string key)
        {
            var am = Instance; if (am == null) return;
            var clip = Resources.Load<AudioClip>("Audio/Voice/" + key);
            if (clip != null) am.ui.PlayOneShot(clip, SfxVol);
            else PlayUI(Sfx.Alarm);
        }

        public static void PlayMusic(Music m)
        {
            var am = Instance; if (am == null || am.current == m) return;
            am.current = m;
            var tmp = am.musicA; am.musicA = am.musicB; am.musicB = tmp;   // A = new, B = fading out
            if (m == Music.None) { am.musicA.Stop(); }
            else { am.musicA.clip = am.music[m]; am.musicA.volume = 0f; am.musicA.loop = m != Music.Victory; am.musicA.Play(); }
            am.crossfade = 0f;
        }

        void Update()
        {
            crossfade = Mathf.MoveTowards(crossfade, 1f, Time.unscaledDeltaTime / 1.2f);
            musicA.volume = crossfade * MusicVol * 0.55f;
            musicB.volume = (1f - crossfade) * MusicVol * 0.55f;
            if (crossfade >= 1f && musicB.isPlaying) musicB.Stop();
        }

        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (SaveSystem.Data != null && SaveSystem.Data.settings.vibration) Handheld.Vibrate();
#endif
        }
    }
}
