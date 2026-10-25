using System.Collections.Generic;
using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Pooled straight-flying bullet used by every gun in the game (player and enemy).</summary>
    public class Projectile : MonoBehaviour, IPoolable
    {
        public const string Key = "projectile";
        public static readonly List<Projectile> EnemyBullets = new List<Projectile>();

        public string PoolKey { get; set; }
        public Team team;
        public float damage, splash, life;
        public int pierce;
        public Vector3 velocity;
        public Color color;
        Renderer rend;
        float age;
        Unit lastHit;

        public static void RegisterPool()
        {
            if (Pool.IsRegistered(Key)) return;
            Pool.Register(Key, () =>
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Projectile";
                go.layer = GameBounds.BulletLayer;
                Object.DestroyImmediate(go.GetComponent<Collider>());
                var col = go.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 0.5f;
                var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                var r = go.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                var p = go.AddComponent<Projectile>(); p.rend = r;
                return go;
            });
            Pool.Prewarm(Key, 120);
        }

        public static Projectile Fire(Vector3 pos, Vector3 dir, float speed, float damage, Team team, Color color, float size, float life = 3f, float splash = 0f, int pierce = 0, float stretch = 2.6f)
        {
            dir.y = 0f; if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward; dir.Normalize();
            var p = Pool.Spawn<Projectile>(Key, pos, Quaternion.LookRotation(dir));
            p.team = team; p.damage = damage; p.velocity = dir * speed; p.life = life; p.splash = splash; p.pierce = pierce; p.color = color;
            p.transform.localScale = new Vector3(size, size, size * stretch);
            p.rend.sharedMaterial = MaterialLibrary.Glow(color, team == Team.Player ? 3f : 2.2f);
            if (team == Team.Enemy) EnemyBullets.Add(p);
            return p;
        }

        public void OnSpawned() { age = 0f; lastHit = null; }
        public void OnDespawned() { EnemyBullets.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            // Gravity wells bend projectiles for a cool effect.
            for (int i = 0; i < GravityWell.Active.Count; i++) velocity += GravityWell.Active[i].PullAt(transform.position) * dt * 0.6f;
            transform.position += velocity * dt;
            if (velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
            if (age > life || !GameBounds.Inside(transform.position, 4f)) Pool.Despawn(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!gameObject.activeSelf) return;
            var u = other.GetComponentInParent<Unit>();
            if (u == null || u == lastHit || u.team == team || !u.IsAlive) return;
            if (team == Team.Enemy && u.team == Team.Neutral) return;   // enemy fire passes through rocks
            lastHit = u;
            if (splash > 0f) Combat.Explode(transform.position, splash, damage, team, color, 0.05f);
            else { u.TakeDamage(damage, transform.position, team); Vfx.Hit(transform.position, color); }
            if (pierce-- <= 0) Pool.Despawn(gameObject);
        }

        /// <summary>Remove enemy bullets inside a radius (EMP, Energy Wave, shield break).</summary>
        public static int ClearEnemyBullets(Vector3 center, float radius)
        {
            int n = 0; float r2 = radius * radius;
            for (int i = EnemyBullets.Count - 1; i >= 0; i--)
            {
                if (i >= EnemyBullets.Count) continue;
                var b = EnemyBullets[i];
                if (b == null) { EnemyBullets.RemoveAt(i); continue; }
                if ((b.transform.position - center).sqrMagnitude <= r2) { Vfx.Hit(b.transform.position, b.color); Pool.Despawn(b.gameObject); n++; }
            }
            return n;
        }
    }
}
