using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Asteroids, debris and comets: neutral objects that drift down the screen and hurt on contact.</summary>
    public class Hazard : Unit, IPoolable, IContactDamage
    {
        public const string Key = "hazard";
        public string PoolKey { get; set; }
        public HazardKind Kind { get; private set; }
        Vector3 velocity, spin;
        Transform model;
        TrailRenderer trail;
        float contact;
        public float ContactDamage => contact;
        public void OnContact(PlayerShip p) { if (Kind != HazardKind.Comet) TakeDamage(9999f, transform.position, Team.Player); }

        public static void RegisterPool()
        {
            if (Pool.IsRegistered(Key)) return;
            Pool.Register(Key, () =>
            {
                var go = new GameObject("Hazard");
                var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                var col = go.AddComponent<SphereCollider>(); col.isTrigger = true;
                var h = go.AddComponent<Hazard>(); h.team = Team.Neutral;
                h.trail = go.AddComponent<TrailRenderer>();
                h.trail.time = 0.6f; h.trail.widthMultiplier = 0.8f; h.trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
                h.trail.enabled = false; h.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                return go;
            });
        }

        public static Hazard Spawn(HazardKind kind, Vector3 pos, float difficulty)
        {
            var h = Pool.Spawn<Hazard>(Key, pos, Random.rotation);
            h.Setup(kind, difficulty);
            return h;
        }

        void Setup(HazardKind kind, float diff)
        {
            Kind = kind;
            if (model != null) Destroy(model.gameObject);
            model = new GameObject("Visual").transform; model.SetParent(transform, false);
            SetVisual(model);
            float size; Color col;
            switch (kind)
            {
                case HazardKind.Comet:
                    size = 0.9f; col = new Color(0.7f, 0.9f, 1f);
                    ShipFactory.Part(model, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * size, MaterialLibrary.Lit(col, 1.5f, 0.9f, 0.1f));
                    var dir = (new Vector3(Random.Range(-0.6f, 0.6f), 0f, -1f)).normalized;
                    velocity = dir * Random.Range(16f, 22f);
                    trail.enabled = true; trail.Clear(); trail.sharedMaterial = MaterialLibrary.Particle(col);
                    trail.startColor = col; trail.endColor = new Color(col.r, col.g, col.b, 0f);
                    maxHealth = 60f; contact = 35f;
                    break;
                case HazardKind.Debris:
                    size = Random.Range(0.6f, 1.1f); col = new Color(0.4f, 0.42f, 0.5f);
                    var m = MaterialLibrary.Lit(col, 0f, 0.5f, 0.8f);
                    ShipFactory.Part(model, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(size, size * 0.3f, size * 1.4f), m);
                    ShipFactory.Part(model, PrimitiveType.Cube, new Vector3(size * 0.4f, 0, 0), new Vector3(30, 20, 0), new Vector3(size * 0.5f, size * 0.5f, size * 0.6f), m);
                    velocity = new Vector3(Random.Range(-2f, 2f), 0f, -Random.Range(3f, 6f));
                    trail.enabled = false; maxHealth = 25f; contact = 15f;
                    break;
                default: // Asteroid
                    size = Random.Range(1.2f, 2.6f); col = new Color(0.45f, 0.36f, 0.3f);
                    var rm = MaterialLibrary.Lit(col, 0f, 0.25f, 0.1f);
                    ShipFactory.Part(model, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(size, size * 0.8f, size * 1.1f), rm);
                    for (int i = 0; i < 4; i++) ShipFactory.Part(model, PrimitiveType.Sphere, Random.insideUnitSphere * size * 0.4f, Random.rotation.eulerAngles, Vector3.one * size * Random.Range(0.45f, 0.7f), rm);
                    velocity = new Vector3(Random.Range(-1.5f, 1.5f), 0f, -Random.Range(2.5f, 4.5f));
                    trail.enabled = false; maxHealth = 40f * size; contact = 20f + 8f * size;
                    break;
            }
            GetComponent<SphereCollider>().radius = size * 0.55f;
            health = maxHealth;
            spin = Random.insideUnitSphere * 60f;
            velocity *= 0.9f + 0.1f * diff;
        }

        public void OnSpawned() { }
        public void OnDespawned() { if (trail != null) trail.Clear(); }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime;
            velocity += GravityWell.TotalPull(transform.position, false) * dt * 0.5f;
            transform.position += velocity * dt;
            if (model != null) model.Rotate(spin * dt, Space.World);
            if (transform.position.z < GameBounds.MinZ - 5f || Mathf.Abs(transform.position.x) > GameBounds.MaxX + 10f) Pool.Despawn(gameObject);
        }

        protected override void Die()
        {
            if (Kind == HazardKind.Asteroid && transform.localScale.x >= 1f && GetComponent<SphereCollider>().radius > 0.9f)
            {
                // Big rocks split into debris.
                for (int i = 0; i < 2; i++) Spawn(HazardKind.Debris, transform.position + Random.insideUnitSphere * 0.5f, 1f);
            }
            Combat.Explode(transform.position, 1.5f, 0f, Team.Neutral, new Color(0.7f, 0.6f, 0.5f), 0.05f);
            GameManager.Instance.OnHazardDestroyed(this);
            Pool.Despawn(gameObject);
        }
    }

    /// <summary>Sector 5 hazard: a horizontal laser gate that pulses on and off. Touching the live beam hurts.</summary>
    public class LaserGate : MonoBehaviour
    {
        LineRenderer line; float timer, z, life; bool on; Transform a, b;
        public static LaserGate Create(float z, float life)
        {
            var go = new GameObject("LaserGate");
            var g = go.AddComponent<LaserGate>();
            g.z = z; g.life = life;
            var pole = MaterialLibrary.Lit(new Color(0.85f, 0.7f, 0.4f), 1f);
            g.a = ShipFactory.Part(go.transform, PrimitiveType.Cylinder, new Vector3(GameBounds.MinX - 0.2f, 0, z), Vector3.zero, new Vector3(0.6f, 0.8f, 0.6f), pole);
            g.b = ShipFactory.Part(go.transform, PrimitiveType.Cylinder, new Vector3(GameBounds.MaxX + 0.2f, 0, z), Vector3.zero, new Vector3(0.6f, 0.8f, 0.6f), pole);
            g.line = go.AddComponent<LineRenderer>();
            g.line.positionCount = 2; g.line.useWorldSpace = true; g.line.widthMultiplier = 0.35f;
            g.line.sharedMaterial = MaterialLibrary.Glow(new Color(1f, 0.3f, 0.2f), 3f);
            g.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            AudioManager.Play(Sfx.Alarm, new Vector3(0, 0, z), 0.4f);
            return g;
        }
        void Update()
        {
            if (GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime; life -= dt; timer += dt;
            z -= 2.2f * dt;   // gates scroll down with the world
            a.position = new Vector3(GameBounds.MinX - 0.2f, 0, z); b.position = new Vector3(GameBounds.MaxX + 0.2f, 0, z);
            on = Mathf.Repeat(timer, 2.4f) < 1.4f;
            line.enabled = on || Mathf.Repeat(timer, 0.2f) < 0.1f;
            line.widthMultiplier = on ? 0.35f : 0.06f;
            line.SetPosition(0, a.position); line.SetPosition(1, b.position);
            var p = PlayerShip.Instance;
            if (on && p != null && p.IsAlive && Mathf.Abs(p.transform.position.z - z) < 0.7f) p.TakeDamage(45f * dt, p.transform.position, Team.Enemy);
            if (life <= 0f || z < GameBounds.MinZ - 3f) Destroy(gameObject);
        }
    }

    /// <summary>Sector 6 hazard: a wormhole that teleports the player across the field (and spits out an enemy).</summary>
    public class Wormhole : MonoBehaviour
    {
        Transform ring; float life, cd; Vector3 exit; Color color = new Color(0.5f, 0.3f, 1f);
        public static Wormhole Create(Vector3 pos, float life)
        {
            var go = new GameObject("Wormhole"); go.transform.position = pos;
            var w = go.AddComponent<Wormhole>(); w.life = life;
            w.exit = new Vector3(-pos.x, 0f, Mathf.Clamp(pos.z - 4f, GameBounds.MinZ + 3f, GameBounds.MaxZ - 3f));
            w.ring = ShipFactory.Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(3f, 0.02f, 3f), MaterialLibrary.Nebula(w.color));
            ShipFactory.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.6f, MaterialLibrary.Glow(w.color, 3f));
            Vfx.Warp(pos, w.color);
            return w;
        }
        void Update()
        {
            if (GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime; life -= dt; cd -= dt;
            ring.Rotate(0f, -180f * dt, 0f);
            var p = PlayerShip.Instance;
            if (cd <= 0f && p != null && p.IsAlive && (p.transform.position - transform.position).sqrMagnitude < 1.8f)
            {
                cd = 3f;
                Vfx.Warp(p.transform.position, color);
                p.transform.position = exit;
                p.InvulnTimer = Mathf.Max(p.InvulnTimer, 0.8f);
                Vfx.Warp(exit, color);
                AudioManager.Play(Sfx.Warp, exit);
                Enemy.Spawn(EnemyKind.Interceptor, transform.position, GameManager.Instance.Difficulty);
            }
            if (life <= 0f) { Vfx.Warp(transform.position, color); Destroy(gameObject); }
        }
    }

    /// <summary>Credits, repair kits and missile crates dropped by enemies.</summary>
    public class Pickup : MonoBehaviour, IPoolable
    {
        public const string Key = "pickup";
        public string PoolKey { get; set; }
        public enum Kind { Credits, Repair, Missiles, Energy }
        Kind kind; int value; float life; Renderer rend; Color color;
        public static void RegisterPool()
        {
            if (Pool.IsRegistered(Key)) return;
            Pool.Register(Key, () =>
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Pickup";
                Object.DestroyImmediate(go.GetComponent<Collider>());
                var col = go.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 1.2f;
                var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                go.transform.localScale = Vector3.one * 0.55f;
                var p = go.AddComponent<Pickup>(); p.rend = go.GetComponent<Renderer>();
                go.SetActive(false);
                return go;
            });
        }
        public static void Drop(Vector3 pos, Kind kind, int value)
        {
            var p = Pool.Spawn<Pickup>(Key, pos, Random.rotation);
            p.kind = kind; p.value = value; p.life = 9f;
            p.color = kind == Kind.Credits ? new Color(1f, 0.85f, 0.2f) : kind == Kind.Repair ? new Color(0.3f, 1f, 0.4f) : kind == Kind.Missiles ? new Color(1f, 0.5f, 0.2f) : new Color(0.3f, 0.7f, 1f);
            p.rend.sharedMaterial = MaterialLibrary.Lit(p.color, 2f);
        }
        public void OnSpawned() { }
        public void OnDespawned() { }
        void Update()
        {
            if (GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime; life -= dt;
            transform.Rotate(60f * dt, 90f * dt, 0f);
            var p = PlayerShip.Instance;
            Vector3 move = Vector3.back * 2.5f;
            if (p != null && p.IsAlive)
            {
                var d = p.transform.position - transform.position;
                if (d.sqrMagnitude < 36f) move = d.normalized * 14f;     // magnet
            }
            transform.position += move * dt;
            rend.enabled = life > 2f || Mathf.Repeat(life * 6f, 1f) > 0.4f;
            if (life <= 0f || transform.position.z < GameBounds.MinZ - 3f) Pool.Despawn(gameObject);
        }
        void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerShip>();
            if (p == null || !p.IsAlive) return;
            switch (kind)
            {
                case Kind.Credits: GameManager.Instance.AddRunCredits(value); HUD.Float(transform.position, "+" + value, color); break;
                case Kind.Repair: p.Heal(value, value); HUD.Float(transform.position, "REPAIR", color); break;
                case Kind.Missiles: p.Missiles = Mathf.Min(p.MaxMissiles, p.Missiles + value); HUD.Float(transform.position, "+" + value + " MSL", color); break;
                case Kind.Energy: p.Energy = p.MaxEnergy; p.AddUltimateCharge(15f); HUD.Float(transform.position, "ENERGY", color); break;
            }
            AudioManager.Play(Sfx.Pickup, transform.position, 0.6f);
            Vfx.Hit(transform.position, color);
            Pool.Despawn(gameObject);
        }
    }
}
