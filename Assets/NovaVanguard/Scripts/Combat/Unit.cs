using System.Collections.Generic;
using UnityEngine;

namespace NovaVanguard
{
    /// <summary>
    /// Base class for anything with health that can be shot: player, enemies, boss parts, asteroids.
    /// All colliders in the game are kinematic triggers; damage is routed through TakeDamage.
    /// </summary>
    public abstract class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();

        [Header("Unit")]
        public Team team = Team.Enemy;
        public float maxHealth = 10f;
        public float health = 10f;

        protected Transform visual;
        float punch;
        Vector3 visualBaseScale = Vector3.one;
        public float StunTimer { get; protected set; }

        public bool IsAlive => health > 0f && gameObject.activeInHierarchy;
        public virtual bool Targetable => IsAlive;
        /// <summary>Damage multiplier applied to incoming hits (weak points, armour).</summary>
        public virtual float DamageTakenMultiplier => 1f;

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        protected void SetVisual(Transform v) { visual = v; if (v != null) visualBaseScale = v.localScale; }

        public virtual void TakeDamage(float amount, Vector3 point, Team source)
        {
            if (!IsAlive || source == team) return;
            amount *= DamageTakenMultiplier;
            if (amount <= 0f) return;
            health -= amount;
            punch = 1f;
            OnDamaged(amount, point);
            if (health <= 0f) { health = 0f; Die(); }
        }

        public virtual void Stun(float seconds) { StunTimer = Mathf.Max(StunTimer, seconds); }

        protected virtual void OnDamaged(float amount, Vector3 point) { }
        protected abstract void Die();

        protected virtual void LateUpdate()
        {
            if (StunTimer > 0f) StunTimer -= Time.deltaTime;
            if (visual != null && punch > 0f)
            {
                punch = Mathf.Max(0f, punch - Time.deltaTime * 9f);
                visual.localScale = visualBaseScale * (1f + punch * 0.12f);
            }
        }

        /// <summary>Find the closest living unit of a team (used by homing weapons, drones and auto-aim).</summary>
        public static Unit Closest(Vector3 from, Team targetTeam, float maxDist = 999f, bool includeNeutral = false)
        {
            Unit best = null; float bestD = maxDist * maxDist;
            for (int i = 0; i < All.Count; i++)
            {
                var u = All[i];
                if (u == null || !u.Targetable) continue;
                if (u.team != targetTeam && !(includeNeutral && u.team == Team.Neutral)) continue;
                float d = (u.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = u; }
            }
            return best;
        }
    }

    public static class Combat
    {
        static readonly Collider[] hits = new Collider[64];
        static readonly HashSet<Unit> hitSet = new HashSet<Unit>();

        /// <summary>Area damage that hurts every unit not on the source team.</summary>
        public static void Explode(Vector3 pos, float radius, float damage, Team source, Color color, float shake = 0.15f)
        {
            Vfx.Explosion(pos, color, Mathf.Clamp(radius * 0.6f, 0.6f, 4f));
            AudioManager.Play(Sfx.Explosion, pos, Mathf.Clamp01(radius / 4f) * 0.5f + 0.5f);
            if (shake > 0f) CameraRig.Shake(shake);
            if (damage <= 0f) return;
            hitSet.Clear();
            int n = Physics.OverlapSphereNonAlloc(pos, radius, hits, ~0, QueryTriggerInteraction.Collide);
            // Collect first: TakeDamage can kill a unit whose death calls Explode again, which would
            // overwrite the shared 'hits' buffer and 'hitSet' while we are still iterating them.
            var victims = new List<Unit>(n);
            for (int i = 0; i < n; i++)
            {
                var u = hits[i].GetComponentInParent<Unit>();
                if (u == null || u.team == source || !hitSet.Add(u)) continue;
                if (source == Team.Enemy && u.team == Team.Neutral) continue;
                victims.Add(u);
            }
            for (int i = 0; i < victims.Count; i++)
            {
                var u = victims[i];
                if (u == null || !u.IsAlive) continue;
                float falloff = 1f - 0.5f * Mathf.Clamp01(Vector3.Distance(pos, u.transform.position) / radius);
                u.TakeDamage(damage * falloff, pos, source);
            }
        }

        public static void Stun(Vector3 pos, float radius, float seconds, Team source)
        {
            int n = Physics.OverlapSphereNonAlloc(pos, radius, hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var u = hits[i].GetComponentInParent<Unit>();
                if (u != null && u.team != source && u.team != Team.Neutral) u.Stun(seconds);
            }
        }
    }
}
