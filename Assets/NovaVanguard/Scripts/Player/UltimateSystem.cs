using System.Collections;
using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Ultimate abilities: Orbital Strike, Energy Wave, Black Hole Generator, Drone Swarm.</summary>
    public class UltimateSystem : MonoBehaviour
    {
        PlayerShip ship;
        public UltimateAbility Ability { get; private set; }
        public bool Active { get; private set; }

        public static UltimateSystem Attach(PlayerShip s)
        {
            var u = s.gameObject.AddComponent<UltimateSystem>();
            u.ship = s;
            u.Ability = (UltimateAbility)Mathf.Clamp(SaveSystem.Data.selectedUltimate, 0, 3);
            return u;
        }

        public UltimateDef Def => GameData.Ultimate(Ability);

        void Update()
        {
            if (!ship.Controllable || !ship.IsAlive) return;
            if (InputRouter.UltimatePressed)
            {
                if (ship.UltimateCharge >= 100f && !Active) StartCoroutine(Fire());
                else { AudioManager.PlayUI(Sfx.PowerDown); HUD.Alert("ULTIMATE CHARGING", new Color(0.8f, 0.8f, 0.9f)); }
            }
        }

        IEnumerator Fire()
        {
            Active = true;
            ship.UltimateCharge = 0f;
            SaveSystem.Data.stats.ultimatesUsed++;
            AudioManager.Play(Sfx.Ultimate, null, 1f);
            HUD.Alert(Def.name.ToUpperInvariant(), Def.color, 1.2f);
            CameraRig.Shake(0.5f);
            PostFx.Kick(0.8f);
            float dmg = 60f * ship.DamageMul;
            switch (Ability)
            {
                case UltimateAbility.OrbitalStrike:
                    for (int wave = 0; wave < 3; wave++)
                    {
                        var targets = new System.Collections.Generic.List<Unit>();
                        foreach (var u in Unit.All) if (u.team == Team.Enemy && u.Targetable) targets.Add(u);
                        for (int i = 0; i < targets.Count; i++)
                        {
                            var p = targets[i].transform.position;
                            StartCoroutine(Lance(p, Def.color));
                            Combat.Explode(p, 2.5f, dmg, Team.Player, Def.color, 0.1f);
                        }
                        if (targets.Count == 0) { for (int i = 0; i < 5; i++) { var p = GameBounds.RandomInUpperHalf(); StartCoroutine(Lance(p, Def.color)); Combat.Explode(p, 2.5f, dmg, Team.Player, Def.color, 0.1f); } }
                        yield return new WaitForSeconds(0.45f);
                    }
                    break;
                case UltimateAbility.EnergyWave:
                    for (int i = 0; i < 3; i++)
                    {
                        float radius = 10f + i * 7f;
                        Vfx.Shockwave(ship.transform.position, Def.color, radius, 0.6f);
                        Projectile.ClearEnemyBullets(ship.transform.position, radius);
                        Combat.Explode(ship.transform.position, radius, dmg * 0.9f, Team.Player, Def.color, 0.15f);
                        Combat.Stun(ship.transform.position, radius, 1.5f, Team.Player);
                        yield return new WaitForSeconds(0.3f);
                    }
                    break;
                case UltimateAbility.BlackHole:
                    {
                        var pos = GameBounds.Center + Vector3.forward * 3f;
                        var t = Unit.Closest(ship.transform.position, Team.Enemy, 60f);
                        if (t != null) pos = t.transform.position;
                        var bh = BlackHoleEffect.Create(pos, Def.color, 7f, dmg * 0.9f, true);
                        yield return new WaitForSeconds(7f);
                    }
                    break;
                case UltimateAbility.DroneSwarm:
                    for (int i = 0; i < 6; i++) Drone.Spawn(ship, i, 6, Def.color, dmg * 0.18f, 12f);
                    yield return new WaitForSeconds(12f);
                    break;
            }
            Active = false;
        }

        IEnumerator Lance(Vector3 p, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = p + Vector3.up * 20f;
            go.transform.localScale = new Vector3(0.9f, 20f, 0.9f);
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = MaterialLibrary.Glow(c, 4f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Vfx.Explosion(p, c, 2f);
            float t = 0f;
            while (t < 0.35f) { t += Time.deltaTime; float k = 1f - t / 0.35f; go.transform.localScale = new Vector3(0.9f * k, 20f, 0.9f * k); yield return null; }
            Destroy(go);
        }
    }

    /// <summary>Visual + gravity + damage for a singularity. Used by the ultimate and by Dark Matter hazards.</summary>
    public class BlackHoleEffect : MonoBehaviour
    {
        GravityWell well; float life, damage; Team source; Transform disk, core; bool player;
        public static BlackHoleEffect Create(Vector3 pos, Color color, float life, float dps, bool playerOwned)
        {
            var go = new GameObject("BlackHole");
            go.transform.position = pos;
            var b = go.AddComponent<BlackHoleEffect>();
            b.life = life; b.damage = dps; b.player = playerOwned; b.source = playerOwned ? Team.Player : Team.Enemy;
            b.well = go.AddComponent<GravityWell>();
            b.well.radius = playerOwned ? 11f : 9f; b.well.strength = playerOwned ? 40f : 26f;
            b.well.pullsPlayer = !playerOwned; b.well.pullsEnemies = playerOwned;
            b.core = ShipFactory.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 1.6f, MaterialLibrary.Lit(Color.black, 0f, 1f, 0f));
            b.disk = ShipFactory.Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(5f, 0.02f, 5f), MaterialLibrary.Nebula(color));
            Vfx.Shockwave(pos, color, 8f, 0.6f);
            AudioManager.Play(Sfx.Warp, pos);
            return b;
        }
        void Update()
        {
            float dt = Time.deltaTime; life -= dt;
            disk.Rotate(0f, 240f * dt, 0f);
            float k = Mathf.Clamp01(life / 1f);
            disk.localScale = new Vector3(5f, 0.02f, 5f) * Mathf.Min(1f, k + 0.2f);
            // continuous damage to units near the core
            // Iterate by index (backwards): TakeDamage can kill a unit, which despawns it and removes it from Unit.All.
            for (int i = Unit.All.Count - 1; i >= 0; i--)
            {
                if (i >= Unit.All.Count) continue;
                var u = Unit.All[i];
                if (u == null || !u.IsAlive || u.team == source || u.team == Team.Neutral) continue;
                float d = (u.transform.position - transform.position).magnitude;
                if (d < 3f) u.TakeDamage(damage * dt * (player ? 1f : 0.5f), u.transform.position, source);
            }
            if (life <= 0f) { Vfx.Explosion(transform.position, Color.white, 2f); Destroy(gameObject); }
        }
    }

    /// <summary>Attack drone that orbits the player and shoots nearby enemies.</summary>
    public class Drone : MonoBehaviour
    {
        PlayerShip owner; int index, total; float life, damage, fireCd; Color color;
        public static void Spawn(PlayerShip owner, int index, int total, Color color, float damage, float life)
        {
            var go = new GameObject("Drone");
            var d = go.AddComponent<Drone>();
            d.owner = owner; d.index = index; d.total = total; d.color = color; d.damage = damage; d.life = life;
            var m = MaterialLibrary.Lit(new Color(0.8f, 0.85f, 0.9f), 0f, 0.7f, 0.6f);
            ShipFactory.Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(0.5f, 0.3f, 0.5f), m);
            ShipFactory.Part(go.transform, PrimitiveType.Sphere, new Vector3(0, 0.15f, 0), Vector3.zero, Vector3.one * 0.25f, MaterialLibrary.Glow(color, 3f));
            go.transform.position = owner.transform.position;
            Vfx.Warp(owner.transform.position, color);
        }
        void Update()
        {
            if (owner == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime; life -= dt;
            float ang = Time.time * 2.2f + index / (float)total * Mathf.PI * 2f;
            var target = owner.transform.position + new Vector3(Mathf.Cos(ang) * 2.6f, 0.4f, Mathf.Sin(ang) * 2.6f);
            transform.position = Vector3.Lerp(transform.position, target, dt * 8f);
            fireCd -= dt;
            if (fireCd <= 0f && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                var t = Unit.Closest(transform.position, Team.Enemy, 22f);
                if (t != null)
                {
                    fireCd = 0.25f;
                    var dir = t.transform.position - transform.position;
                    Projectile.Fire(transform.position, dir, 40f, damage, Team.Player, color, 0.2f, 1.5f);
                }
            }
            if (life <= 0f) { Vfx.Hit(transform.position, color); Destroy(gameObject); }
        }
    }
}
