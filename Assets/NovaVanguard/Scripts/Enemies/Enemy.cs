using UnityEngine;

namespace NovaVanguard
{
    /// <summary>
    /// All regular enemy ships. One class with per-kind behaviour keeps the AI readable and poolable;
    /// each kind has a distinct movement pattern and attack.
    /// </summary>
    public class Enemy : Unit, IPoolable, IContactDamage
    {
        public const string Key = "enemy";
        public string PoolKey { get; set; }

        public EnemyDef Def { get; private set; }
        float difficulty = 1f;
        float fireTimer, age, phase, formationX;
        Vector3 velocity;
        Transform model;
        Renderer[] renderers;
        Material stunMat;
        ParticleSystem thruster;
        bool fromBoss;
        int burstLeft;

        public float ContactDamage => Def != null ? Def.contactDamage : 10f;
        public void OnContact(PlayerShip p) { if (Def.kind != EnemyKind.Bomber && Def.kind != EnemyKind.Elite) TakeDamage(9999f, transform.position, Team.Player); }

        public static void RegisterPool()
        {
            if (Pool.IsRegistered(Key)) return;
            Pool.Register(Key, () =>
            {
                var go = new GameObject("Enemy");
                var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                var col = go.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 0.9f;
                var e = go.AddComponent<Enemy>();
                e.team = Team.Enemy;
                go.SetActive(false);
                return go;
            });
        }

        public static Enemy Spawn(EnemyKind kind, Vector3 pos, float difficulty, bool fromBoss = false)
        {
            var e = Pool.Spawn<Enemy>(Key, pos, Quaternion.LookRotation(Vector3.back));
            e.Setup(GameData.Enemies[kind], difficulty, fromBoss);
            return e;
        }

        void Setup(EnemyDef def, float diff, bool boss)
        {
            Def = def; difficulty = diff; fromBoss = boss;
            if (model != null) Destroy(model.gameObject);
            model = ShipFactory.BuildEnemy(transform, def);
            SetVisual(model);
            renderers = model.GetComponentsInChildren<Renderer>();
            if (thruster == null) thruster = Vfx.Thruster(transform, Vector3.zero, def.color, 0.35f);
            thruster.transform.localPosition = new Vector3(0, 0, 0.8f * def.scale);
            thruster.transform.localRotation = Quaternion.LookRotation(Vector3.forward);
            var m = thruster.main; m.startColor = new ParticleSystem.MinMaxGradient(def.color, Color.white);
            GetComponent<SphereCollider>().radius = 0.9f * def.scale;
            maxHealth = def.health * (0.8f + 0.25f * diff);
            health = maxHealth;
            fireTimer = Random.Range(0.6f, def.fireInterval);
            age = 0f; phase = Random.value * 10f; velocity = Vector3.zero; burstLeft = 0;
            formationX = transform.position.x;
            StunTimer = 0f;
        }

        public void OnSpawned() { }
        public void OnDespawned() { }

        void Update()
        {
            if (!IsAlive || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime; age += dt;
            var player = PlayerShip.Instance;
            Vector3 toPlayer = player != null && player.IsAlive ? player.transform.position - transform.position : Vector3.back * 10f;
            toPlayer.y = 0f;

            if (StunTimer > 0f)
            {
                transform.position += velocity * dt * 0.2f;
                foreach (var r in renderers) r.enabled = Mathf.Repeat(Time.time * 15f, 1f) > 0.3f;
                return;
            }
            foreach (var r in renderers) if (!r.enabled) r.enabled = true;

            float spd = Def.speed * (0.85f + 0.15f * difficulty);
            Vector3 target;
            switch (Def.kind)
            {
                case EnemyKind.Scout:   // zig-zag dive
                    target = new Vector3(formationX + Mathf.Sin(age * 2.5f + phase) * 5f, 0f, transform.position.z - 1f);
                    velocity = Vector3.MoveTowards(velocity, (target - transform.position).normalized * spd, 40f * dt);
                    break;
                case EnemyKind.Fighter: // hold a line in the upper third, strafe
                    {
                        float holdZ = GameBounds.MaxZ - 5f - Mathf.PingPong(phase, 3f);
                        float x = formationX + Mathf.Sin(age * 1.1f + phase) * 4f;
                        target = new Vector3(x, 0f, holdZ);
                        var d = target - transform.position;
                        velocity = Vector3.MoveTowards(velocity, d.magnitude > 0.5f ? d.normalized * spd : Vector3.zero, 25f * dt);
                        if (age > 14f + phase) velocity = Vector3.back * spd * 1.5f; // eventually leave
                    }
                    break;
                case EnemyKind.Interceptor: // charge the player, overshoot, loop back
                    {
                        var d = toPlayer;
                        if (d.z < -3f && Mathf.Abs(d.x) < 4f) d = new Vector3(Mathf.Sign(d.x + 0.01f) * 8f, 0, 6f); // loop around
                        velocity = Vector3.MoveTowards(velocity, d.normalized * spd, 55f * dt);
                    }
                    break;
                case EnemyKind.Bomber:  // slow descent straight down, drifting toward the player's x
                    target = new Vector3(Mathf.Lerp(transform.position.x, transform.position.x + toPlayer.x, 0.3f), 0f, transform.position.z - 1f);
                    velocity = Vector3.MoveTowards(velocity, (target - transform.position).normalized * spd, 10f * dt);
                    break;
                default:                // Elite: slow hover in upper half, tracks player x
                    {
                        float holdZ = GameBounds.MaxZ - 6f;
                        target = new Vector3(Mathf.Clamp(transform.position.x + toPlayer.x * 0.5f, GameBounds.MinX + 2f, GameBounds.MaxX - 2f), 0f, holdZ);
                        var d = target - transform.position;
                        velocity = Vector3.MoveTowards(velocity, d.magnitude > 0.5f ? d.normalized * spd : Vector3.zero, 15f * dt);
                    }
                    break;
            }
            velocity += GravityWell.TotalPull(transform.position, false) * dt;
            transform.position += velocity * dt;
            // Face travel direction, enemies default to facing down.
            var face = velocity.sqrMagnitude > 0.5f ? velocity.normalized : Vector3.back;
            if (Def.kind == EnemyKind.Bomber || Def.kind == EnemyKind.Elite) face = Vector3.back;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), dt * 6f);
            if (model != null && (Def.kind == EnemyKind.Bomber || Def.kind == EnemyKind.Elite)) model.Rotate(0f, 40f * dt, 0f, Space.World);

            // Left the playfield (out the bottom or far sides) -> recycle without reward
            if (transform.position.z < GameBounds.MinZ - 4f || Mathf.Abs(transform.position.x) > GameBounds.MaxX + 8f || transform.position.z > GameBounds.MaxZ + 12f)
            { Pool.Despawn(gameObject); return; }

            // Shooting (only once on screen)
            if (transform.position.z < GameBounds.MaxZ + 0.5f)
            {
                fireTimer -= dt;
                if (fireTimer <= 0f) Shoot(toPlayer);
            }
        }

        void Shoot(Vector3 toPlayer)
        {
            float interval = Def.fireInterval / (0.75f + 0.25f * difficulty);
            float dmg = Def.bulletDamage * (0.85f + 0.15f * difficulty);
            var dir = toPlayer.normalized;
            var pos = transform.position + Vector3.back * 0.8f * Def.scale;
            var c = Def.color;
            switch (Def.kind)
            {
                case EnemyKind.Scout:
                    Projectile.Fire(pos, Vector3.back, Def.bulletSpeed, dmg, Team.Enemy, c, 0.3f);
                    fireTimer = interval; break;
                case EnemyKind.Fighter:
                    Projectile.Fire(pos, dir, Def.bulletSpeed, dmg, Team.Enemy, c, 0.32f);
                    fireTimer = interval; break;
                case EnemyKind.Interceptor:
                    for (int i = -1; i <= 1; i += 2) Projectile.Fire(pos + transform.right * i * 0.4f, transform.forward, Def.bulletSpeed, dmg, Team.Enemy, c, 0.26f);
                    fireTimer = interval; break;
                case EnemyKind.Bomber:
                    for (int i = 0; i < 8; i++) Projectile.Fire(pos, Quaternion.Euler(0, i * 45f + Time.time * 20f, 0) * Vector3.forward, Def.bulletSpeed, dmg, Team.Enemy, c, 0.4f, 3f, 0f, 0, 1f);
                    fireTimer = interval; break;
                default: // Elite: 3-round aimed bursts then pause
                    if (burstLeft <= 0) burstLeft = 5;
                    float spread = (burstLeft - 3) * 9f;
                    Projectile.Fire(pos, Quaternion.Euler(0, spread, 0) * dir, Def.bulletSpeed, dmg, Team.Enemy, c, 0.34f);
                    burstLeft--;
                    fireTimer = burstLeft > 0 ? Def.fireInterval : 2.2f / (0.75f + 0.25f * difficulty);
                    break;
            }
            AudioManager.Play(Sfx.EnemyShot, pos, 0.3f);
        }

        protected override void OnDamaged(float amount, Vector3 point) { }

        protected override void Die()
        {
            Combat.Explode(transform.position, 1.2f * Def.scale, 0f, Team.Player, Def.color, 0.08f * Def.scale);
            if (Def.kind == EnemyKind.Bomber) Combat.Explode(transform.position, 3.5f, 20f, Team.Neutral, Def.color, 0.25f); // bombers blow up big, hurting everyone
            GameManager.Instance.OnEnemyKilled(this, fromBoss);
            Pool.Despawn(gameObject);
        }
    }
}
