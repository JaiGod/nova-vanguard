using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Pooled particle effect that returns itself to the pool when finished.</summary>
    public class PooledEffect : MonoBehaviour, IPoolable
    {
        public string PoolKey { get; set; }
        public ParticleSystem[] systems;
        public float lifetime = 1.5f;
        float t;
        public void OnSpawned() { t = 0f; foreach (var s in systems) { s.Clear(true); s.Play(true); } }
        public void OnDespawned() { foreach (var s in systems) s.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        void Update() { t += Time.deltaTime; if (t >= lifetime) Pool.Despawn(gameObject); }
    }

    /// <summary>Expanding ring used for shockwaves (EMP, Energy Wave, boss phase changes).</summary>
    public class ShockwaveRing : MonoBehaviour, IPoolable
    {
        public string PoolKey { get; set; }
        public float maxRadius = 6f, duration = 0.45f;
        public LineRenderer line;
        public Color color = Color.white;
        const int Segments = 48;
        float t;
        public void OnSpawned() { t = 0f; Draw(0.1f, 1f); }
        public void OnDespawned() { }
        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            Draw(Mathf.Lerp(0.2f, maxRadius, 1f - (1f - k) * (1f - k)), 1f - k);
            if (t >= duration) Pool.Despawn(gameObject);
        }
        void Draw(float r, float a)
        {
            line.widthMultiplier = 0.15f + 0.6f * a;
            var c = new Color(color.r, color.g, color.b, a);
            line.startColor = c; line.endColor = c;
            for (int i = 0; i < Segments; i++)
            {
                float ang = i / (float)Segments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r));
            }
        }
    }

    /// <summary>Builds all particle effects procedurally (no art assets needed) and spawns them from pools.</summary>
    public static class Vfx
    {
        const string ExplosionKey = "fx_explosion", HitKey = "fx_hit", RingKey = "fx_ring", WarpKey = "fx_warp";

        public static void RegisterPools()
        {
            if (Pool.IsRegistered(ExplosionKey)) return;
            Pool.Register(ExplosionKey, BuildExplosion); Pool.Prewarm(ExplosionKey, 12);
            Pool.Register(HitKey, BuildHit); Pool.Prewarm(HitKey, 20);
            Pool.Register(RingKey, BuildRing); Pool.Prewarm(RingKey, 4);
            Pool.Register(WarpKey, BuildWarp); Pool.Prewarm(WarpKey, 2);
        }

        public static void Explosion(Vector3 pos, Color color, float scale = 1f)
        {
            pos.y = 0.5f;
            var go = Pool.Spawn(ExplosionKey, pos, Quaternion.identity);
            go.transform.localScale = Vector3.one * scale;
            var fx = go.GetComponent<PooledEffect>();
            Tint(fx.systems[0], color);
            Tint(fx.systems[1], Color.Lerp(color, Color.white, 0.6f));
            if (scale > 1.5f) PostFx.Kick(0.25f);
        }

        public static void Hit(Vector3 pos, Color color)
        {
            var go = Pool.Spawn(HitKey, pos, Quaternion.identity);
            Tint(go.GetComponent<PooledEffect>().systems[0], color);
        }

        public static void Shockwave(Vector3 pos, Color color, float radius, float duration = 0.45f)
        {
            pos.y = 0.3f;
            var go = Pool.Spawn(RingKey, pos, Quaternion.identity);
            var ring = go.GetComponent<ShockwaveRing>();
            ring.maxRadius = radius; ring.duration = duration; ring.color = color;
        }

        public static void Warp(Vector3 pos, Color color)
        {
            var go = Pool.Spawn(WarpKey, pos, Quaternion.identity);
            Tint(go.GetComponent<PooledEffect>().systems[0], color);
        }

        static void Tint(ParticleSystem ps, Color c)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.4f));
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialLibrary.Particle(Color.white);
        }

        /// <summary>Creates a stopped, configured particle system (no texture assets required).</summary>
        public static ParticleSystem NewSystem(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = color;
            main.maxParticles = 200;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialLibrary.Particle(Color.white);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var shape = ps.shape; shape.enabled = false;
            return ps;
        }

        static GameObject BuildExplosion()
        {
            var root = new GameObject("FX_Explosion");
            // Sparks
            var sparks = NewSystem("Sparks", root.transform, Color.white);
            var m = sparks.main;
            m.duration = 0.5f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 16f); m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            m.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var e = sparks.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 34) });
            var sh = sparks.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var sol = sparks.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            var limit = sparks.limitVelocityOverLifetime; limit.enabled = true; limit.dampen = 0.15f; limit.limit = 2f;
            var sr = sparks.GetComponent<ParticleSystemRenderer>(); sr.renderMode = ParticleSystemRenderMode.Stretch; sr.velocityScale = 0.06f; sr.lengthScale = 1.5f;
            // Fireball
            var ball = NewSystem("Fireball", root.transform, Color.white);
            var bm = ball.main;
            bm.duration = 0.5f; bm.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            bm.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 3f); bm.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            bm.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var be = ball.emission; be.rateOverTime = 0; be.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
            var bsh = ball.shape; bsh.enabled = true; bsh.shapeType = ParticleSystemShapeType.Sphere; bsh.radius = 0.3f;
            var bsol = ball.sizeOverLifetime; bsol.enabled = true; bsol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.6f, 1, 1.4f));
            var bcol = ball.colorOverLifetime; bcol.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            bcol.color = g;
            var fx = root.AddComponent<PooledEffect>(); fx.systems = new[] { sparks, ball }; fx.lifetime = 1f;
            root.SetActive(false);
            return root;
        }

        static GameObject BuildHit()
        {
            var root = new GameObject("FX_Hit");
            var ps = NewSystem("Spark", root.transform, Color.white);
            var m = ps.main; m.duration = 0.2f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f); m.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            var e = ps.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.05f;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 1.2f;
            var fx = root.AddComponent<PooledEffect>(); fx.systems = new[] { ps }; fx.lifetime = 0.4f;
            root.SetActive(false);
            return root;
        }

        static GameObject BuildRing()
        {
            var go = new GameObject("FX_Ring");
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 48;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            line.sharedMaterial = MaterialLibrary.Particle(Color.white);
            line.textureMode = LineTextureMode.Stretch;
            var ring = go.AddComponent<ShockwaveRing>(); ring.line = line;
            go.SetActive(false);
            return go;
        }

        static GameObject BuildWarp()
        {
            var root = new GameObject("FX_Warp");
            var ps = NewSystem("Warp", root.transform, Color.white);
            var m = ps.main; m.duration = 0.6f; m.startLifetime = 0.6f; m.startSpeed = new ParticleSystem.MinMaxCurve(-12f, -6f); m.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            var e = ps.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 4f; sh.rotation = new Vector3(90, 0, 0);
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.08f;
            var fx = root.AddComponent<PooledEffect>(); fx.systems = new[] { ps }; fx.lifetime = 0.9f;
            root.SetActive(false);
            return root;
        }

        /// <summary>Looping engine exhaust attached to a ship. Emission is driven by the ship's throttle.</summary>
        public static ParticleSystem Thruster(Transform parent, Vector3 localPos, Color color, float size = 0.5f)
        {
            var ps = NewSystem("Thruster", parent, color);
            ps.transform.localPosition = localPos;
            ps.transform.localRotation = Quaternion.LookRotation(Vector3.back);
            var m = ps.main;
            m.loop = true; m.duration = 1f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
            m.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            m.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            m.maxParticles = 80;
            var e = ps.emission; e.rateOverTime = 50f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.05f;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            ps.Play();
            return ps;
        }
    }
}
