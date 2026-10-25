using UnityEngine;

namespace NovaVanguard
{
    public enum MissileMode { Dumb, Homing, EMP, Cluster, Bomblet }

    /// <summary>Pooled missile with a smoke trail. Covers dumb-fire, homing, EMP bombs and cluster rockets.</summary>
    public class Missile : MonoBehaviour, IPoolable
    {
        public const string Key = "missile";
        public string PoolKey { get; set; }

        public Team team;
        public MissileMode mode;
        public float damage, speed, turnRate, splash, life, armDelay;
        public Color color;
        Unit target;
        float age;
        Vector3 velocity;
        TrailRenderer trail;
        Renderer body, flame;

        public static void RegisterPool()
        {
            if (Pool.IsRegistered(Key)) return;
            Pool.Register(Key, () =>
            {
                var go = new GameObject("Missile") { layer = GameBounds.BulletLayer };
                var col = go.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 0.45f;
                var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                var m = go.AddComponent<Missile>();
                var b = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.transform.SetParent(go.transform, false);
                b.transform.localRotation = Quaternion.Euler(90, 0, 0);
                b.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
                m.body = b.GetComponent<Renderer>();
                var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(f.GetComponent<Collider>());
                f.transform.SetParent(go.transform, false);
                f.transform.localPosition = new Vector3(0, 0, -0.4f);
                f.transform.localScale = new Vector3(0.2f, 0.2f, 0.35f);
                m.flame = f.GetComponent<Renderer>();
                m.trail = go.AddComponent<TrailRenderer>();
                m.trail.time = 0.35f; m.trail.minVertexDistance = 0.15f;
                m.trail.widthMultiplier = 0.25f;
                m.trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
                m.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return go;
            });
            Pool.Prewarm(Key, 16);
        }

        public static Missile Launch(Vector3 pos, Vector3 dir, MissileMode mode, float damage, Team team, Color color, float speed = 22f)
        {
            dir.y = 0f; if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward; dir.Normalize();
            var m = Pool.Spawn<Missile>(Key, pos, Quaternion.LookRotation(dir));
            m.team = team; m.mode = mode; m.damage = damage; m.color = color; m.speed = speed;
            m.velocity = dir * speed;
            m.turnRate = mode == MissileMode.Homing ? 340f : (mode == MissileMode.Bomblet ? 0f : 40f);
            m.splash = mode == MissileMode.EMP ? 7f : mode == MissileMode.Cluster ? 2.2f : mode == MissileMode.Bomblet ? 1.6f : 2.4f;
            m.life = mode == MissileMode.EMP ? 0.65f : mode == MissileMode.Cluster ? 0.55f : mode == MissileMode.Bomblet ? 0.5f + Random.value * 0.4f : 3.5f;
            m.armDelay = 0.08f;
            m.body.sharedMaterial = MaterialLibrary.Lit(mode == MissileMode.EMP ? new Color(0.5f, 0.75f, 1f) : new Color(0.85f, 0.85f, 0.9f), mode == MissileMode.EMP ? 1.5f : 0f);
            m.flame.sharedMaterial = MaterialLibrary.Glow(color, 3f);
            m.trail.sharedMaterial = MaterialLibrary.Particle(color);
            m.trail.startColor = new Color(color.r, color.g, color.b, 0.9f);
            m.trail.endColor = new Color(0.5f, 0.5f, 0.5f, 0f);
            m.trail.Clear();
            float s = mode == MissileMode.Bomblet ? 0.6f : mode == MissileMode.EMP ? 1.6f : 1f;
            m.transform.localScale = Vector3.one * s;
            return m;
        }

        public void OnSpawned() { age = 0f; target = null; if (trail != null) trail.Clear(); }
        public void OnDespawned() { }

        void Update()
        {
            float dt = Time.deltaTime; age += dt;
            if (turnRate > 0f)
            {
                if (target == null || !target.Targetable)
                    target = Unit.Closest(transform.position, team == Team.Player ? Team.Enemy : Team.Player, 40f);
                if (target != null)
                {
                    var want = target.transform.position - transform.position; want.y = 0f;
                    var newDir = Vector3.RotateTowards(velocity.normalized, want.normalized, turnRate * Mathf.Deg2Rad * dt, 0f);
                    velocity = newDir * speed;
                }
            }
            // missiles accelerate slightly
            if (mode == MissileMode.Dumb || mode == MissileMode.Homing) velocity *= 1f + dt * 0.8f;
            transform.position += velocity * dt;
            if (velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
            if (age >= life) { Detonate(); return; }
            if (!GameBounds.Inside(transform.position, 3f)) Pool.Despawn(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!gameObject.activeSelf || age < armDelay) return;
            var u = other.GetComponentInParent<Unit>();
            if (u == null || u.team == team || !u.IsAlive) return;
            if (team == Team.Enemy && u.team == Team.Neutral) return;
            Detonate();
        }

        void Detonate()
        {
            var pos = transform.position;
            switch (mode)
            {
                case MissileMode.EMP:
                    Combat.Stun(pos, splash, 3f, team);
                    Projectile.ClearEnemyBullets(pos, splash);
                    Combat.Explode(pos, splash, damage, team, color, 0.3f);
                    Vfx.Shockwave(pos, color, splash);
                    AudioManager.Play(Sfx.Emp, pos);
                    break;
                case MissileMode.Cluster:
                    Combat.Explode(pos, splash, damage, team, color, 0.15f);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f + Random.Range(-10f, 10f);
                        Launch(pos, Quaternion.Euler(0, a, 0) * Vector3.forward, MissileMode.Bomblet, damage * 0.8f, team, color, 12f);
                    }
                    break;
                default:
                    Combat.Explode(pos, splash, damage, team, color, mode == MissileMode.Bomblet ? 0.04f : 0.12f);
                    break;
            }
            Pool.Despawn(gameObject);
        }
    }
}
