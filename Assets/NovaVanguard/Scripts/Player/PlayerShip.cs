using UnityEngine;

namespace NovaVanguard
{
    /// <summary>
    /// The player's ship: movement with acceleration and banking, boost, and the health/shield/energy model.
    /// Weapons live in <see cref="PlayerWeapons"/>, abilities in <see cref="UltimateSystem"/>.
    /// </summary>
    public class PlayerShip : Unit
    {
        public static PlayerShip Instance { get; private set; }

        public ShipDef Def { get; private set; }
        public PlayerWeapons Weapons { get; private set; }

        // Final stats after upgrades
        public float MaxShield, MaxEnergy, Speed, Accel, BoostMul, DamageMul, FireRateMul, RangeMul, ShieldRegen;
        public int MaxMissiles;

        public float Shield, Energy, UltimateCharge;   // UltimateCharge 0..100
        public int Missiles;
        public int Lives;
        public bool Boosting { get; private set; }
        public float InvulnTimer;

        Vector3 velocity;
        float shieldDelay, missileRegen;
        Vector3 aimDir = Vector3.forward;
        ParticleSystem[] thrusters;
        Transform shieldBubble;
        float shieldFlash;
        Renderer shieldRenderer;
        float boostSoundCd;

        public static PlayerShip Spawn(ShipDef def)
        {
            var go = new GameObject("Player");
            var p = go.AddComponent<PlayerShip>();
            p.Init(def);
            return p;
        }

        void Init(ShipDef def)
        {
            Instance = this;
            Def = def;
            team = Team.Player;
            var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.direction = 2; col.radius = 0.55f; col.height = 2f;

            var v = ShipFactory.BuildPlayer(transform, def, out var engines);
            SetVisual(v);
            thrusters = new ParticleSystem[engines.Length];
            for (int i = 0; i < engines.Length; i++) thrusters[i] = Vfx.Thruster(v, engines[i], def.accent, 0.45f);

            // Shield bubble
            shieldBubble = ShipFactory.Part(transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(3.2f, 1.2f, 3.2f), MaterialLibrary.Particle(new Color(0.3f, 0.7f, 1f, 0.5f)));
            shieldRenderer = shieldBubble.GetComponent<Renderer>();
            shieldRenderer.enabled = false;

            RecalculateStats();
            health = maxHealth; Shield = MaxShield; Energy = MaxEnergy; Missiles = MaxMissiles;
            Lives = 3;
            Weapons = gameObject.AddComponent<PlayerWeapons>();
            Weapons.Init(this);
        }

        public void RecalculateStats()
        {
            var s = SaveSystem.Data;
            float L(UpgradeStat u) => s.UpgradeLevel(u);
            maxHealth = Def.health * (1f + 0.12f * L(UpgradeStat.Health));
            MaxShield = Def.shield * (1f + 0.12f * L(UpgradeStat.Shield));
            MaxEnergy = Def.energy * (1f + 0.12f * L(UpgradeStat.Energy));
            Speed = Def.speed;
            Accel = Def.accel;
            BoostMul = 1.65f * (1f + 0.08f * L(UpgradeStat.BoostSpeed));
            DamageMul = Def.damageMul * (1f + 0.08f * L(UpgradeStat.Damage));
            FireRateMul = Def.fireRateMul * (1f + 0.06f * L(UpgradeStat.FireRate));
            RangeMul = Def.rangeMul;
            ShieldRegen = 14f * Def.shieldRegenMul;
            MaxMissiles = 12 + 3 * (int)L(UpgradeStat.MissileCapacity);
        }

        /// <summary>Called at the start of each sector.</summary>
        public void ResetForSector()
        {
            RecalculateStats();
            health = maxHealth; Shield = MaxShield; Energy = MaxEnergy; Missiles = MaxMissiles;
            UltimateCharge = 0f; Lives = 3; velocity = Vector3.zero;
            transform.position = new Vector3(GameBounds.Center.x, 0f, GameBounds.MinZ + 3f);
            transform.rotation = Quaternion.identity;
            InvulnTimer = 1.5f;
            gameObject.SetActive(true);
        }

        public override bool Targetable => IsAlive && gameObject.activeInHierarchy;
        public bool Controllable => GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool ctrl = Controllable && IsAlive;

            // ---------- Movement ----------
            Vector2 move = ctrl ? InputRouter.Move : Vector2.zero;
            bool wantBoost = ctrl && InputRouter.Boost && move.sqrMagnitude > 0.01f && Energy > 1f;
            Boosting = wantBoost;
            float maxSpeed = Speed * (Boosting ? BoostMul : 1f);
            if (Boosting)
            {
                Energy = Mathf.Max(0f, Energy - 32f * dt);
                boostSoundCd -= dt;
                if (boostSoundCd <= 0f) { AudioManager.Play(Sfx.Boost, transform.position, 0.35f); boostSoundCd = 0.45f; }
            }
            else Energy = Mathf.Min(MaxEnergy, Energy + 20f * dt);

            var target = new Vector3(move.x, 0f, move.y) * maxSpeed;
            float a = Accel * (Boosting ? 1.6f : 1f);
            if (target.sqrMagnitude < 0.01f) a *= 0.8f;    // deceleration
            velocity = Vector3.MoveTowards(velocity, target, a * dt);
            velocity += GravityWell.TotalPull(transform.position, true) * dt;

            var pos = transform.position + velocity * dt;
            if (ctrl) pos += InputRouter.DragDelta;   // mobile drag-to-move
            var clamped = GameBounds.Clamp(pos, 1.1f);
            if (clamped.x != pos.x) velocity.x = 0f;
            if (clamped.z != pos.z) velocity.z = 0f;
            transform.position = clamped;

            // ---------- Aim ----------
            Vector3 want = Vector3.forward;
            if (ctrl)
            {
                if (InputRouter.AimPoint.HasValue)
                {
                    var d = InputRouter.AimPoint.Value - transform.position; d.y = 0f;
                    if (d.sqrMagnitude > 0.5f) want = d.normalized;
                }
                else if (InputRouter.AimDirFromStick.HasValue) want = InputRouter.AimDirFromStick.Value.normalized;
                else
                {
                    // Auto-aim (touch / gamepad without right stick): nearest enemy in front of us.
                    var t = Unit.Closest(transform.position, Team.Enemy, 30f, true);
                    if (t != null)
                    {
                        var d = t.transform.position - transform.position; d.y = 0f;
                        if (d.z > -2f) want = Vector3.Slerp(Vector3.forward, d.normalized, 0.85f);
                    }
                }
            }
            aimDir = Vector3.RotateTowards(aimDir, want, 9f * dt, 0f);
            float bank = 0f;
            if (Speed > 0f) bank = -Vector3.Dot(velocity, Vector3.Cross(Vector3.up, aimDir)) / (Speed * BoostMul) * 40f;
            transform.rotation = Quaternion.LookRotation(aimDir) * Quaternion.Euler(0f, 0f, -bank);
            if (visual != null) visual.localRotation = Quaternion.Euler(-velocity.z * 0.6f, 0f, 0f);

            // ---------- Thrusters ----------
            float throttle = Mathf.Clamp01(velocity.magnitude / Speed) * (Boosting ? 2.2f : 1f) + 0.3f;
            foreach (var th in thrusters)
            {
                if (th == null) continue;
                var em = th.emission; em.rateOverTime = 35f + 90f * throttle;
                var m = th.main; m.startSpeedMultiplier = 5f + 7f * throttle;
            }

            // ---------- Regen ----------
            if (InvulnTimer > 0f) InvulnTimer -= dt;
            if (shieldDelay > 0f) shieldDelay -= dt;
            else Shield = Mathf.Min(MaxShield, Shield + ShieldRegen * dt);
            missileRegen += dt;
            if (missileRegen > 3.5f) { missileRegen = 0f; Missiles = Mathf.Min(MaxMissiles, Missiles + 1); }

            // ---------- Shield visual ----------
            shieldFlash = Mathf.Max(0f, shieldFlash - dt * 4f);
            bool showShield = shieldFlash > 0f || InvulnTimer > 0f;
            shieldRenderer.enabled = showShield && (InvulnTimer <= 0f || Mathf.Repeat(Time.time * 10f, 1f) > 0.3f);
            shieldBubble.rotation = Quaternion.identity;

            // Blink while invulnerable
            if (visual != null) visual.gameObject.SetActive(InvulnTimer <= 0f || Mathf.Repeat(Time.time * 12f, 1f) > 0.25f || !IsAlive);
        }

        public override void TakeDamage(float amount, Vector3 point, Team source)
        {
            if (!IsAlive || source == team || InvulnTimer > 0f) return;
            if (GameManager.Instance != null && GameManager.Instance.GodMode) return;
            shieldDelay = 2.5f;
            if (Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, amount);
                Shield -= absorbed; amount -= absorbed;
                shieldFlash = 1f;
                AudioManager.Play(Sfx.ShieldHit, transform.position, 0.6f);
                if (Shield <= 0f) { AudioManager.Voice("shield_down"); HUD.Alert("SHIELDS DOWN", new Color(0.3f, 0.7f, 1f)); Projectile.ClearEnemyBullets(transform.position, 4f); }
            }
            if (amount > 0f)
            {
                base.TakeDamage(amount, point, source);
                CameraRig.Shake(0.25f);
                PostFx.Kick(0.35f);
                AudioManager.Play(Sfx.Hit, transform.position);
                AudioManager.Vibrate();
                if (IsAlive && health < maxHealth * 0.25f) HUD.Alert("HULL CRITICAL", new Color(1f, 0.3f, 0.2f));
            }
            InvulnTimer = Mathf.Max(InvulnTimer, 0.12f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsAlive) return;
            var u = other.GetComponentInParent<Unit>();
            if (u == null || u == this || !u.IsAlive) return;
            if (u is IContactDamage c) { TakeDamage(c.ContactDamage, transform.position, Team.Enemy); c.OnContact(this); }
        }

        protected override void Die()
        {
            Combat.Explode(transform.position, 3f, 0f, Team.Player, Def.accent, 0.8f);
            Vfx.Explosion(transform.position, Color.white, 3f);
            AudioManager.Play(Sfx.BigExplosion, transform.position);
            SaveSystem.Data.stats.deaths++;
            Lives--;
            GameManager.Instance.OnPlayerDied(this);
        }

        public void Respawn()
        {
            health = maxHealth; Shield = MaxShield; Energy = MaxEnergy;
            velocity = Vector3.zero;
            transform.position = new Vector3(GameBounds.Center.x, 0f, GameBounds.MinZ + 3f);
            InvulnTimer = 2.5f;
            Projectile.ClearEnemyBullets(transform.position, 100f);
            Vfx.Warp(transform.position, Def.accent);
            AudioManager.Play(Sfx.Warp);
        }

        public void AddUltimateCharge(float amount) { UltimateCharge = Mathf.Min(100f, UltimateCharge + amount); }

        public void Heal(float hp, float shield)
        {
            health = Mathf.Min(maxHealth, health + hp);
            Shield = Mathf.Min(MaxShield, Shield + shield);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }
    }

    /// <summary>Things that hurt the player on contact (enemy hulls, asteroids, comets).</summary>
    public interface IContactDamage
    {
        float ContactDamage { get; }
        void OnContact(PlayerShip player);
    }
}
