using UnityEngine;
#if NOVA_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace NovaVanguard
{
    /// <summary>Bloom, tonemapping and vignette when running on URP. No-op on the Built-in pipeline.</summary>
    public static class PostFx
    {
        public static void Setup(Camera cam)
        {
#if NOVA_URP
            if (!MaterialLibrary.IsURP) return;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            var go = new GameObject("PostFX Volume");
            Object.DontDestroyOnLoad(go);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(0.95f);
            bloom.scatter.Override(0.65f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.32f);
            vig.smoothness.Override(0.45f);
            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.08f);
            vol.profile = profile;
            Volume = vol;
            Aberration = ca;
#endif
        }

#if NOVA_URP
        public static Volume Volume;
        static ChromaticAberration Aberration;
        static float kick;
#endif

        /// <summary>Brief chromatic-aberration kick on big hits and explosions.</summary>
        public static void Kick(float amount)
        {
#if NOVA_URP
            kick = Mathf.Clamp01(kick + amount);
#endif
        }

        public static void Tick(float dt)
        {
#if NOVA_URP
            if (Aberration == null) return;
            kick = Mathf.Max(0f, kick - dt * 2.5f);
            Aberration.intensity.value = 0.08f + kick * 0.7f;
#endif
        }

        public static void SetHighQuality(bool on)
        {
#if NOVA_URP
            if (Volume != null) Volume.enabled = on;
#endif
        }
    }
}
