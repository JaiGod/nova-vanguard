using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NovaVanguard
{
    /// <summary>
    /// Creates and caches materials at runtime. Works in both URP and the Built-in pipeline.
    /// Base materials in Resources/NovaMaterials (created by the editor setup) guarantee the shaders
    /// are included in player builds; if they're missing we fall back to Shader.Find.
    /// </summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Texture2D softDot, nebulaTex;
        static bool? urp;

        public static bool IsURP
        {
            get
            {
                if (!urp.HasValue) urp = GraphicsSettings.currentRenderPipeline != null;
                return urp.Value;
            }
        }

        static string Key(string kind, Color c, float e) => kind + ColorUtility.ToHtmlStringRGBA(c) + e.ToString("0.00");

        static Material Base(string resName, string urpShader, string builtinShader)
        {
            var baseMat = Resources.Load<Material>("NovaMaterials/" + resName + (IsURP ? "_URP" : "_BuiltIn"));
            if (baseMat != null) return new Material(baseMat);
            var sh = Shader.Find(IsURP ? urpShader : builtinShader);
            if (sh == null) sh = Shader.Find("Sprites/Default");
            return new Material(sh);
        }

        static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c);
        }

        /// <summary>Lit, slightly glossy hull material. emission > 0 makes it glow (bloom in URP).</summary>
        public static Material Lit(Color c, float emission = 0f, float smooth = 0.55f, float metal = 0.35f)
        {
            string k = Key("lit", c, emission);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = Base("Lit", "Universal Render Pipeline/Lit", "Standard");
            SetColor(m, c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", c * emission);
            }
            cache[k] = m; return m;
        }

        /// <summary>Flat bright material for bullets, beams and engine glows.</summary>
        public static Material Glow(Color c, float intensity = 2.5f)
        {
            string k = Key("glow", c, intensity);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = Base("Unlit", "Universal Render Pipeline/Unlit", "Unlit/Color");
            SetColor(m, IsURP ? c * intensity : c);
            cache[k] = m; return m;
        }

        /// <summary>Additive soft particle material.</summary>
        public static Material Particle(Color c)
        {
            string k = Key("ptc", c, 1f);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = Base("Particle", "Universal Render Pipeline/Particles/Unlit", "Legacy Shaders/Particles/Additive");
            if (IsURP)
            {
                // Transparent + additive
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 2f);
                if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.One);
                if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", SoftDot);
            }
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", SoftDot);
            SetColor(m, c);
            cache[k] = m; return m;
        }

        /// <summary>Additive material using a cloudy texture for nebulae.</summary>
        public static Material Nebula(Color c)
        {
            var m = Particle(c);
            var n = new Material(m);
            if (n.HasProperty("_BaseMap")) n.SetTexture("_BaseMap", NebulaTexture);
            if (n.HasProperty("_MainTex")) n.SetTexture("_MainTex", NebulaTexture);
            return n;
        }

        public static Texture2D SoftDot
        {
            get
            {
                if (softDot != null) return softDot;
                const int s = 64;
                softDot = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "SoftDot" };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                        float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        byte a = (byte)(Mathf.Pow(d, 1.8f) * 255f);
                        px[y * s + x] = new Color32(255, 255, 255, a);
                    }
                softDot.SetPixels32(px); softDot.Apply();
                return softDot;
            }
        }

        public static Texture2D NebulaTexture
        {
            get
            {
                if (nebulaTex != null) return nebulaTex;
                const int s = 128;
                nebulaTex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Nebula" };
                var px = new Color32[s * s];
                float ox = Random.value * 100f, oy = Random.value * 100f;
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float u = x / (float)s, v = y / (float)s;
                        float n = 0f, amp = 0.55f, f = 3f;
                        for (int o = 0; o < 4; o++) { n += Mathf.PerlinNoise(ox + u * f, oy + v * f) * amp; amp *= 0.5f; f *= 2f; }
                        float dx = u * 2f - 1f, dy = v * 2f - 1f;
                        float fall = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        float a = Mathf.Clamp01((n - 0.25f) * 1.6f) * fall * fall;
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                nebulaTex.SetPixels32(px); nebulaTex.Apply();
                return nebulaTex;
            }
        }
    }
}
