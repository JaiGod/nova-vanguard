using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Destructible shield generator mounted on a boss. While any are alive the boss shield is up.</summary>
    public class BossGenerator : Unit
    {
        public Boss owner;
        public override float DamageTakenMultiplier => 1.6f;   // weak point
        public static BossGenerator Create(Boss owner, Vector3 localPos, Color accent, float hp)
        {
            var go = new GameObject("Generator");
            go.transform.SetParent(owner.transform, false);
            go.transform.localPosition = localPos;
            var col = go.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 0.8f;
            var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            var g = go.AddComponent<BossGenerator>();
            g.owner = owner; g.team = Team.Enemy; g.maxHealth = hp; g.health = hp;
            var v = new GameObject("Visual").transform; v.SetParent(go.transform, false);
            ShipFactory.Part(v, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(1.1f, 0.3f, 1.1f), MaterialLibrary.Lit(new Color(0.25f, 0.25f, 0.3f), 0f, 0.6f, 0.7f));
            ShipFactory.Part(v, PrimitiveType.Sphere, new Vector3(0, 0.35f, 0), Vector3.zero, Vector3.one * 0.7f, MaterialLibrary.Lit(accent, 2.5f));
            g.SetVisual(v);
            return g;
        }
        protected override void Die()
        {
            Combat.Explode(transform.position, 2f, 0f, Team.Player, Color.yellow, 0.3f);
            owner.OnGeneratorDestroyed(this);
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Multi-phase boss. Three phases at 100/66/33% health, a shield that only drops when all generators are
    /// destroyed, a core weak point, cinematic entrance and attack patterns chosen from the sector's BossDef.
    /// </summary>
    public class Boss : Unit, IContactDamage
    {
        public static Boss Current { get; private set; }
        public BossDef Def { get; private set; }
        public int Phase { get; private set; } = 1;
        public float ShieldHealth { get; private set; }
        public bool ShieldUp => ShieldHealth > 0f && generatorsAlive > 0;
        public bool Entering { get; private set; } = true;

        float difficulty;
        int generatorsAlive;
        readonly List<BossGenerator> generators = new List<BossGenerator>();
        Transform core, model, shieldBubble;
        Renderer shieldRenderer;
        float shieldFlash, age, moveT;
        Vector3 homePos;
        Coroutine attackLoop;
        LineRenderer sweepBeam;

        public float ContactDamage => 30f;
        public void OnContact(PlayerShip p) { }

        public override float DamageTakenMultiplier => ShieldUp ? 0f : 1f;

        public static Boss Spawn(BossDef def, float difficulty)
        {
            var go = new GameObject("Boss " + def.name);
            var b = go.AddComponent<Boss>();
            b.team = Team.Enemy;
            b.Setup(def, difficulty);
            Current = b;
            return b;
        }

        void Setup(BossDef def, float diff)
        {
            Def = def; difficulty = diff;
            maxHealth = def.health * (0.85f + 0.2f * diff); health = maxHealth;
            ShieldHealth = def.shieldHealth;
            var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            var col = gameObject.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(5.5f, 1.5f, 2.5f) * def.scale * 0.9f;

            model = ShipFactory.BuildBoss(transform, def, out var genPoints, out core);
            SetVisual(model);
            foreach (var gp in genPoints) { var g = BossGenerator.Create(this, gp, def.accent, 120f + 60f * diff); generators.Add(g); }
            generatorsAlive = generators.Count;
            if (generatorsAlive == 0) ShieldHealth = 0f;

            shieldBubble = ShipFactory.Part(transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(7f, 2.5f, 4.5f) * def.scale * 0.8f, MaterialLibrary.Particle(new Color(def.accent.r, def.accent.g, def.accent.b, 0.35f)));
            shieldRenderer = shieldBubble.GetComponent<Renderer>();
            shieldRenderer.enabled = ShieldUp;

            var beamGo = new GameObject("SweepBeam"); beamGo.transform.SetParent(transform, false);
            sweepBeam = beamGo.AddComponent<LineRenderer>();
            sweepBeam.positionCount = 2; sweepBeam.widthMultiplier = 0.6f; sweepBeam.useWorldSpace = true;
            sweepBeam.sharedMaterial = MaterialLibrary.Glow(def.accent, 3f); sweepBeam.enabled = false;
            sweepBeam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            homePos = new Vector3(GameBounds.Center.x, 0f, GameBounds.MaxZ - 4.5f);
            transform.position = homePos + Vector3.forward * 22f;
            transform.rotation = Quaternion.LookRotation(Vector3.back);
            StartCoroutine(Entrance());
        }

        IEnumerator Entrance()
        {
            Entering = true;
            AudioManager.PlayMusic(Music.Boss);
            AudioManager.Voice("warning_boss");
            HUD.BossIntro(Def);
            float t = 0f;
            var from = transform.position;
            while (t < 3f)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 3f), 3f);
                transform.position = Vector3.Lerp(from, homePos, k);
                if (t > 2.6f && t - Time.deltaTime <= 2.6f) { CameraRig.Shake(0.7f); Vfx.Shockwave(transform.position, Def.accent, 12f, 0.8f); AudioManager.Play(Sfx.BigExplosion, transform.position, 0.6f); }
                yield return null;
            }
            Entering = false;
            attackLoop = StartCoroutine(AttackLoop());
        }

        void Update()
        {
            if (!IsAlive || Entering || GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime; age += dt;
            // Hover: figure-8 across the top of the screen, faster each phase.
            moveT += dt * (0.35f + 0.15f * Phase);
            float halfW = (GameBounds.Width * 0.5f - 4.5f * Def.scale * 0.5f);
            var target = homePos + new Vector3(Mathf.Sin(moveT) * halfW, 0f, Mathf.Sin(moveT * 2f) * 1.5f);
            transform.position = Vector3.Lerp(transform.position, target, dt * 1.5f);
            if (model != null) model.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Cos(moveT) * 6f);
            if (core != null) core.localScale = Vector3.one * (0.75f + Mathf.Sin(Time.time * 6f) * 0.08f);

            shieldFlash = Mathf.Max(0f, shieldFlash - dt * 3f);
            shieldRenderer.enabled = ShieldUp;
            shieldBubble.localScale = new Vector3(7f, 2.5f, 4.5f) * Def.scale * 0.8f * (1f + shieldFlash * 0.05f);
        }

        public override void TakeDamage(float amount, Vector3 point, Team source)
        {
            if (!IsAlive || source == team || Entering) return;
            if (ShieldUp)
            {
                ShieldHealth -= amount * 0.35f;
                shieldFlash = 1f;
                Vfx.Hit(point, Def.accent);
                AudioManager.Play(Sfx.ShieldHit, point, 0.5f);
                if (ShieldHealth <= 0f) ShieldBreak();
                return;
            }
            // Core weak point: shots landing near the core do double damage.
            float mul = core != null && Vector3.Distance(point, core.position) < 1.3f * Def.scale ? 2f : 1f;
            base.TakeDamage(amount * mul, point, source);
            CheckPhase();
        }

        public void OnGeneratorDestroyed(BossGenerator g)
        {
            generatorsAlive--;
            HUD.Alert(generatorsAlive > 0 ? $"GENERATOR DOWN ({generatorsAlive} LEFT)" : "SHIELD GENERATORS DESTROYED", Color.yellow);
            if (generatorsAlive <= 0) ShieldBreak();
        }

        void ShieldBreak()
        {
            ShieldHealth = 0f;
            Projectile.ClearEnemyBullets(transform.position, 40f);
            Vfx.Shockwave(transform.position, Def.accent, 10f, 0.6f);
            CameraRig.Shake(0.5f);
            AudioManager.Play(Sfx.Emp, transform.position);
            HUD.Alert("BOSS SHIELD DOWN", new Color(1f, 0.85f, 0.3f));
            AudioManager.Voice("boss_shield_down");
        }

        void CheckPhase()
        {
            int want = health > maxHealth * 0.66f ? 1 : health > maxHealth * 0.33f ? 2 : 3;
            if (want != Phase)
            {
                Phase = want;
                Projectile.ClearEnemyBullets(transform.position, 60f);
                Vfx.Shockwave(transform.position, Def.accent, 14f, 0.8f);
                Vfx.Explosion(transform.position, Def.accent, 3f);
                CameraRig.Shake(0.6f);
                HUD.Alert($"PHASE {Phase}", Def.accent, 1.2f);
                AudioManager.Play(Sfx.Alarm);
                if (attackLoop != null) StopCoroutine(attackLoop);
                sweepBeam.enabled = false;
                attackLoop = StartCoroutine(AttackLoop());
            }
        }

        IEnumerator AttackLoop()
        {
            yield return new WaitForSeconds(1.2f);
            var patterns = Def.phases[Mathf.Clamp(Phase - 1, 0, Def.phases.Length - 1)];
            int idx = 0;
            while (IsAlive)
            {
                if (GameManager.Instance.State != GameState.Playing) { yield return null; continue; }
                var p = patterns[idx % patterns.Length]; idx++;
                yield return StartCoroutine(RunPattern(p));
                yield return new WaitForSeconds(Mathf.Lerp(1.4f, 0.5f, (Phase - 1) / 2f) / Mathf.Sqrt(difficulty));
            }
        }

        Vector3 Muzzle => transform.position + Vector3.back * 1.6f * Def.scale;
        Vector3 ToPlayer { get { var p = PlayerShip.Instance; var d = p != null ? p.transform.position - Muzzle : Vector3.back; d.y = 0f; return d.normalized; } }
        float BulletDmg => 10f * (0.8f + 0.2f * difficulty) * (1f + 0.1f * Phase);
        float Spd => 11f + 2f * Phase;

        IEnumerator RunPattern(BossPattern p)
        {
            var c = Def.accent;
            switch (p)
            {
                case BossPattern.RadialBurst:
                    for (int wave = 0; wave < 2 + Phase; wave++)
                    {
                        int n = 14 + 4 * Phase; float off = wave * 7f;
                        for (int i = 0; i < n; i++) Projectile.Fire(Muzzle, Quaternion.Euler(0, i * 360f / n + off, 0) * Vector3.forward, Spd, BulletDmg, Team.Enemy, c, 0.4f, 4f, 0, 0, 1f);
                        AudioManager.Play(Sfx.Plasma, Muzzle, 0.6f);
                        yield return new WaitForSeconds(0.45f);
                    }
                    break;
                case BossPattern.Spiral:
                    {
                        float t = 0f, ang = 0f; int arms = 2 + Phase;
                        while (t < 3f)
                        {
                            for (int a = 0; a < arms; a++) Projectile.Fire(Muzzle, Quaternion.Euler(0, ang + a * 360f / arms, 0) * Vector3.forward, Spd * 0.8f, BulletDmg * 0.8f, Team.Enemy, c, 0.34f, 5f, 0, 0, 1f);
                            ang += 17f; t += 0.09f;
                            yield return new WaitForSeconds(0.09f);
                        }
                    }
                    break;
                case BossPattern.AimedVolley:
                    for (int i = 0; i < 4 + Phase; i++)
                    {
                        var d = ToPlayer;
                        for (int k = -1; k <= 1; k++) Projectile.Fire(Muzzle, Quaternion.Euler(0, k * 8f, 0) * d, Spd * 1.4f, BulletDmg, Team.Enemy, c, 0.36f);
                        AudioManager.Play(Sfx.Laser, Muzzle, 0.6f);
                        yield return new WaitForSeconds(0.28f);
                    }
                    break;
                case BossPattern.Shotgun:
                    for (int i = 0; i < 3; i++)
                    {
                        var d = ToPlayer;
                        for (int k = 0; k < 9 + Phase * 2; k++) Projectile.Fire(Muzzle, Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * d, Spd * Random.Range(0.9f, 1.5f), BulletDmg * 0.7f, Team.Enemy, c, 0.3f);
                        AudioManager.Play(Sfx.Explosion, Muzzle, 0.4f);
                        yield return new WaitForSeconds(0.7f);
                    }
                    break;
                case BossPattern.RingWall:
                    {
                        // Wall of bullets with a gap the player must find.
                        int n = 22; float gapAt = Random.Range(0f, 1f);
                        for (int row = 0; row < 2 + Phase; row++)
                        {
                            for (int i = 0; i < n; i++)
                            {
                                float t = i / (float)(n - 1);
                                if (Mathf.Abs(t - gapAt) < 0.11f) continue;
                                var pos = new Vector3(Mathf.Lerp(GameBounds.MinX + 0.5f, GameBounds.MaxX - 0.5f, t), 0f, GameBounds.MaxZ + 1f);
                                Projectile.Fire(pos, Vector3.back, Spd * 0.75f, BulletDmg, Team.Enemy, c, 0.42f, 6f, 0, 0, 1f);
                            }
                            gapAt = Mathf.Clamp01(gapAt + Random.Range(-0.25f, 0.25f));
                            yield return new WaitForSeconds(1.1f);
                        }
                    }
                    break;
                case BossPattern.MissileBarrage:
                    for (int i = 0; i < 3 + Phase; i++)
                    {
                        Missile.Launch(Muzzle + transform.right * (i % 2 == 0 ? -2f : 2f) * Def.scale * 0.5f, Quaternion.Euler(0, Random.Range(-60f, 60f), 0) * Vector3.back, MissileMode.Homing, BulletDmg * 1.6f, Team.Enemy, new Color(1f, 0.4f, 0.2f), 10f);
                        AudioManager.Play(Sfx.Missile, Muzzle, 0.6f);
                        yield return new WaitForSeconds(0.35f);
                    }
                    break;
                case BossPattern.Minions:
                    {
                        var kinds = GameManager.Instance.CurrentSector.enemies;
                        for (int i = 0; i < 2 + Phase; i++)
                        {
                            var k = kinds[Random.Range(0, kinds.Length)];
                            if (k == EnemyKind.Elite && Random.value < 0.6f) k = EnemyKind.Fighter;
                            Enemy.Spawn(k, Muzzle + transform.right * Random.Range(-3f, 3f), difficulty, true);
                            Vfx.Warp(Muzzle, c);
                            yield return new WaitForSeconds(0.3f);
                        }
                    }
                    break;
                case BossPattern.LaserSweep:
                    {
                        // Telegraph, then sweep a beam across the field. Player takes damage on contact.
                        float dir = Random.value < 0.5f ? 1f : -1f;
                        float ang = -55f * dir;
                        sweepBeam.enabled = true; sweepBeam.widthMultiplier = 0.15f;
                        float t = 0f;
                        while (t < 0.8f) { t += Time.deltaTime; DrawBeam(ang, false); yield return null; }
                        AudioManager.Play(Sfx.Ultimate, Muzzle, 0.8f);
                        sweepBeam.widthMultiplier = 0.7f;
                        float sweepTime = Mathf.Lerp(2.4f, 1.5f, (Phase - 1) / 2f);
                        t = 0f;
                        while (t < sweepTime)
                        {
                            t += Time.deltaTime;
                            ang = Mathf.Lerp(-55f * dir, 55f * dir, t / sweepTime);
                            DrawBeam(ang, true);
                            yield return null;
                        }
                        sweepBeam.enabled = false;
                    }
                    break;
            }
        }

        void DrawBeam(float ang, bool live)
        {
            var d = Quaternion.Euler(0, ang, 0) * Vector3.back;
            var a = Muzzle; var b = a + d * 60f;
            sweepBeam.SetPosition(0, a); sweepBeam.SetPosition(1, b);
            var p = PlayerShip.Instance;
            if (live && p != null && p.IsAlive)
            {
                // distance from player to beam segment
                var ap = p.transform.position - a; ap.y = 0f;
                float proj = Vector3.Dot(ap, d);
                if (proj > 0f)
                {
                    float dist = (ap - d * proj).magnitude;
                    if (dist < 0.9f) p.TakeDamage(28f * Time.deltaTime * (0.8f + 0.2f * difficulty) * 3f, p.transform.position, Team.Enemy);
                }
            }
        }

        protected override void OnDamaged(float amount, Vector3 point) { Vfx.Hit(point, Def.accent); }

        protected override void Die()
        {
            if (attackLoop != null) StopCoroutine(attackLoop);
            sweepBeam.enabled = false;
            foreach (var g in generators) if (g != null) g.gameObject.SetActive(false);
            Projectile.ClearEnemyBullets(transform.position, 100f);
            StartCoroutine(DeathSequence());
        }

        IEnumerator DeathSequence()
        {
            GameManager.Instance.OnBossDying();
            for (int i = 0; i < 12; i++)
            {
                var p = transform.position + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-1f, 1f)) * Def.scale;
                Vfx.Explosion(p, i % 2 == 0 ? Def.accent : Color.white, 1.5f + i * 0.15f);
                AudioManager.Play(Sfx.Explosion, p, 0.8f);
                CameraRig.Shake(0.3f);
                yield return new WaitForSeconds(0.14f);
            }
            Vfx.Explosion(transform.position, Color.white, 5f);
            Vfx.Shockwave(transform.position, Def.accent, 20f, 1f);
            AudioManager.Play(Sfx.BigExplosion, transform.position, 1f);
            CameraRig.Shake(1f); PostFx.Kick(1f);
            if (model != null) model.gameObject.SetActive(false);
            shieldRenderer.enabled = false;
            yield return new WaitForSeconds(0.8f);
            GameManager.Instance.OnBossDefeated(this);
            Current = null;
            Destroy(gameObject);
        }

        protected override void OnDisable() { base.OnDisable(); if (Current == this) Current = null; }
        public static void CurrentReset() { Current = null; }
    }
}
