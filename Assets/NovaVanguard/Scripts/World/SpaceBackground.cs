using UnityEngine;

namespace NovaVanguard
{
    /// <summary>
    /// Parallax starfield, drifting nebula clouds, distant space stations/planets and dynamic sector lighting.
    /// Everything is generated procedurally and re-themed per sector.
    /// </summary>
    public class SpaceBackground : MonoBehaviour
    {
        public static SpaceBackground Instance { get; private set; }
        ParticleSystem starsFar, starsNear, dust;
        Transform[] nebulae = new Transform[4];
        Renderer[] nebulaRend = new Renderer[4];
        Transform planet, station;
        Light sun, fill;
        SectorDef sector;
        float scroll = 1f;

        public static SpaceBackground Create()
        {
            var go = new GameObject("SpaceBackground");
            var bg = go.AddComponent<SpaceBackground>();
            Instance = bg;
            bg.Build();
            return bg;
        }

        void Build()
        {
            var l = new GameObject("Sun").AddComponent<Light>();
            l.transform.SetParent(transform); l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(55f, -30f, 0f); l.intensity = 1.3f; l.color = new Color(1f, 0.95f, 0.9f);
            l.shadows = LightShadows.None; sun = l;
            var f = new GameObject("Fill").AddComponent<Light>();
            f.transform.SetParent(transform); f.type = LightType.Directional;
            f.transform.rotation = Quaternion.Euler(-20f, 150f, 0f); f.intensity = 0.45f; f.color = new Color(0.4f, 0.5f, 1f);
            f.shadows = LightShadows.None; fill = f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            starsFar = Stars("StarsFar", -8f, 400, 0.08f, 0.16f, 0.6f);
            starsNear = Stars("StarsNear", -4f, 120, 0.15f, 0.3f, 1f);
            dust = Stars("Dust", -2f, 60, 0.4f, 0.9f, 0.08f);

            for (int i = 0; i < nebulae.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(go.GetComponent<Collider>());
                go.name = "Nebula" + i;
                go.transform.SetParent(transform);
                go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                go.transform.position = new Vector3(Random.Range(GameBounds.MinX, GameBounds.MaxX), -12f, Random.Range(GameBounds.MinZ, GameBounds.MaxZ));
                go.transform.localScale = Vector3.one * Random.Range(22f, 40f);
                nebulae[i] = go.transform; nebulaRend[i] = go.GetComponent<Renderer>();
                nebulaRend[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // Distant planet and a space station silhouette
            planet = ShipFactory.Part(transform, PrimitiveType.Sphere, new Vector3(18f, -30f, 30f), Vector3.zero, Vector3.one * 26f, MaterialLibrary.Lit(new Color(0.5f, 0.4f, 0.6f), 0f, 0.3f, 0f));
            ShipFactory.Part(planet, PrimitiveType.Cylinder, Vector3.zero, new Vector3(75f, 0f, 20f), new Vector3(1.9f, 0.01f, 1.9f), MaterialLibrary.Lit(new Color(0.75f, 0.7f, 0.6f), 0f, 0.4f, 0.1f));
            station = new GameObject("Station").transform; station.SetParent(transform);
            station.position = new Vector3(-22f, -16f, 20f);
            var sm = MaterialLibrary.Lit(new Color(0.35f, 0.37f, 0.42f), 0f, 0.5f, 0.8f);
            ShipFactory.Part(station, PrimitiveType.Cylinder, Vector3.zero, new Vector3(90, 0, 0), new Vector3(6f, 0.4f, 6f), sm);
            ShipFactory.Part(station, PrimitiveType.Cylinder, Vector3.zero, new Vector3(90, 0, 0), new Vector3(1.2f, 4f, 1.2f), sm);
            for (int i = 0; i < 4; i++) ShipFactory.Part(station, PrimitiveType.Cube, Quaternion.Euler(0, 0, i * 90f) * new Vector3(3f, 0, 0), new Vector3(0, 0, i * 90f), new Vector3(3f, 0.3f, 0.3f), sm);
            for (int i = 0; i < 12; i++) ShipFactory.Part(station, PrimitiveType.Cube, Quaternion.Euler(0, 0, i * 30f) * new Vector3(3f, 0, 0.3f), Vector3.zero, Vector3.one * 0.3f, MaterialLibrary.Glow(new Color(1f, 0.9f, 0.6f), 2f));
        }

        ParticleSystem Stars(string name, float y, int count, float sMin, float sMax, float alpha)
        {
            var ps = Vfx.NewSystem(name, transform, new Color(1, 1, 1, alpha));
            ps.transform.position = new Vector3(GameBounds.Center.x, y, GameBounds.MaxZ + 8f);
            ps.transform.rotation = Quaternion.LookRotation(Vector3.back);
            var m = ps.main;
            m.loop = true; m.duration = 5f; m.prewarm = true;
            m.startLifetime = 14f; m.startSpeed = 0f; m.startSize = new ParticleSystem.MinMaxCurve(sMin, sMax);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(1, 1, 1, alpha), new Color(0.7f, 0.8f, 1f, alpha));
            m.maxParticles = count * 2;
            var e = ps.emission; e.rateOverTime = count / 14f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(GameBounds.Width + 20f, 0.1f, 2f);
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = MaterialLibrary.Particle(Color.white);
            ps.Play();
            return ps;
        }

        public void ApplySector(SectorDef s)
        {
            sector = s;
            var cam = CameraRig.Instance != null ? CameraRig.Instance.Cam : null;
            if (cam != null) cam.backgroundColor = s.bgColor;
            RenderSettings.ambientLight = s.ambient * 0.6f;
            sun.color = Color.Lerp(Color.white, s.nebulaA, 0.25f);
            fill.color = s.nebulaB;
            for (int i = 0; i < nebulae.Length; i++)
            {
                var c = Color.Lerp(s.nebulaA, s.nebulaB, i / 3f);
                nebulaRend[i].sharedMaterial = MaterialLibrary.Nebula(new Color(c.r, c.g, c.b, 0.55f));
            }
            planet.gameObject.SetActive(s.index != 6);
            station.gameObject.SetActive(s.index == 1 || s.index == 5 || s.index == 7);
            var dm = dust.main; dm.startColor = new ParticleSystem.MinMaxGradient(new Color(s.nebulaA.r, s.nebulaA.g, s.nebulaA.b, 0.1f));
        }

        public void SetScroll(float mul) { scroll = mul; }

        void Update()
        {
            float dt = Time.deltaTime * scroll;
            var vf = starsFar.velocityOverLifetime; vf.enabled = true; vf.z = new ParticleSystem.MinMaxCurve(-1.6f * scroll);
            var vn = starsNear.velocityOverLifetime; vn.enabled = true; vn.z = new ParticleSystem.MinMaxCurve(-4f * scroll);
            var vd = dust.velocityOverLifetime; vd.enabled = true; vd.z = new ParticleSystem.MinMaxCurve(-8f * scroll);
            for (int i = 0; i < nebulae.Length; i++)
            {
                var n = nebulae[i];
                n.position += Vector3.back * dt * (0.6f + i * 0.2f);
                n.Rotate(0f, 0f, dt * 1.5f, Space.Self);
                if (n.position.z < GameBounds.MinZ - 25f) n.position = new Vector3(Random.Range(GameBounds.MinX, GameBounds.MaxX), -12f, GameBounds.MaxZ + 25f);
            }
            planet.Rotate(0f, dt * 1.2f, 0f, Space.World);
            planet.position += Vector3.back * dt * 0.08f;
            station.Rotate(0f, 0f, dt * 4f, Space.Self);
            station.position += Vector3.back * dt * 0.25f;
            if (station.position.z < -40f) station.position = new Vector3(Random.Range(-30f, 30f), -16f, 45f);
        }
    }
}
