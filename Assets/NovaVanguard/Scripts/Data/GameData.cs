using System.Collections.Generic;
using UnityEngine;

namespace NovaVanguard
{
    public enum Team { Player, Enemy, Neutral }

    public enum PrimaryWeapon { Laser, Plasma, MachineGun, Pulse }
    public enum SecondaryWeapon { Missiles, Homing, EMP, Cluster }
    public enum UltimateAbility { OrbitalStrike, EnergyWave, BlackHole, DroneSwarm }
    public enum EnemyKind { Scout, Fighter, Interceptor, Bomber, Elite }
    public enum HazardKind { Asteroid, Debris, Comet, BlackHole, LaserGate, Wormhole }
    public enum UpgradeStat { Damage, FireRate, Shield, Health, BoostSpeed, Energy, MissileCapacity }
    public enum ShipShape { Arrow, Dart, Brick, Needle, Crescent, Crown }

    /// <summary>Static definition of a playable ship.</summary>
    public class ShipDef
    {
        public string id, name, role, description;
        public int price;              // credits; 0 = free
        public int requiredSector;     // sector that must be cleared to buy (0 = none)
        public float speed, accel, health, shield, energy;
        public float damageMul = 1f, fireRateMul = 1f, rangeMul = 1f, shieldRegenMul = 1f;
        public Color hull, accent;
        public ShipShape shape;
        public bool legendary;
    }

    public class PrimaryDef
    {
        public PrimaryWeapon id; public string name, description; public int price;
        public float damage, fireRate, speed, spread; public int count; public float size; public float splash;
        public Color color;
    }

    public class SecondaryDef
    {
        public SecondaryWeapon id; public string name, description; public int price;
        public float damage, cooldown; public int ammoCost; public Color color;
    }

    public class UltimateDef
    {
        public UltimateAbility id; public string name, description; public int price; public Color color;
    }

    public class EnemyDef
    {
        public EnemyKind kind; public string name;
        public float health, speed, fireInterval, bulletSpeed, bulletDamage, contactDamage;
        public int credits, score; public float scale; public Color color;
    }

    public class SectorDef
    {
        public int index; public string name, tagline;
        public Color bgColor, nebulaA, nebulaB, ambient;
        public EnemyKind[] enemies; public HazardKind[] hazards;
        public int waves; public float difficulty;
        public BossDef boss; public string newMechanic;
    }

    public enum BossPattern { RadialBurst, Spiral, AimedVolley, LaserSweep, Minions, MissileBarrage, Shotgun, RingWall }

    public class BossDef
    {
        public string name, title; public float health, shieldHealth; public int generators;
        public float scale; public Color color, accent;
        public BossPattern[][] phases; // patterns per phase (3 phases)
        public int credits;
    }

    /// <summary>All tunable game content lives here so new content is a data change, not a code change.</summary>
    public static class GameData
    {
        public static readonly List<ShipDef> Ships = new List<ShipDef>
        {
            new ShipDef { id="starter", name="Vanguard", role="Balanced", description="Reliable all-rounder. Good at everything, great at nothing.",
                price=0, speed=14f, accel=60f, health=100, shield=60, energy=100, hull=new Color(0.75f,0.8f,0.9f), accent=new Color(0.2f,0.7f,1f), shape=ShipShape.Arrow },
            new ShipDef { id="swift", name="Swiftwing", role="Speed", description="Blistering speed and boost, but thin armour.",
                price=1500, speed=19f, accel=95f, health=70, shield=45, energy=130, fireRateMul=1.1f, hull=new Color(0.95f,0.85f,0.3f), accent=new Color(1f,0.45f,0.1f), shape=ShipShape.Dart },
            new ShipDef { id="bulwark", name="Bulwark", role="Tank", description="Slow flying fortress with huge health and shields.",
                price=2500, requiredSector=1, speed=10.5f, accel=40f, health=190, shield=120, energy=90, damageMul=1.05f, hull=new Color(0.45f,0.55f,0.45f), accent=new Color(0.3f,1f,0.4f), shape=ShipShape.Brick },
            new ShipDef { id="longbow", name="Longbow", role="Sniper", description="Long-range rail optics: +40% damage and projectile range, slower fire.",
                price=3500, requiredSector=2, speed=13f, accel=55f, health=85, shield=60, energy=110, damageMul=1.4f, fireRateMul=0.8f, rangeMul=1.6f, hull=new Color(0.85f,0.85f,0.95f), accent=new Color(0.7f,0.3f,1f), shape=ShipShape.Needle },
            new ShipDef { id="xelthar", name="Xel'Thar Seed", role="Alien", description="Living hull. Shields regenerate 2.5x faster and energy is plentiful.",
                price=6000, requiredSector=3, speed=15f, accel=70f, health=90, shield=100, energy=160, shieldRegenMul=2.5f, hull=new Color(0.2f,0.9f,0.6f), accent=new Color(1f,0.2f,0.8f), shape=ShipShape.Crescent },
            new ShipDef { id="aurora", name="Aurora Prime", role="Legendary", description="Prototype flagship. Elite in every stat.",
                price=15000, requiredSector=5, speed=17f, accel=85f, health=160, shield=130, energy=170, damageMul=1.3f, fireRateMul=1.2f, rangeMul=1.2f, shieldRegenMul=1.5f, hull=new Color(1f,0.95f,0.85f), accent=new Color(1f,0.75f,0.2f), shape=ShipShape.Crown, legendary=true },
            new ShipDef { id="sovereign", name="Nova Sovereign", role="Legendary", description="Awarded only to pilots who end the Final War. The ultimate ship.",
                price=30000, requiredSector=7, speed=18f, accel=95f, health=220, shield=180, energy=220, damageMul=1.6f, fireRateMul=1.35f, rangeMul=1.4f, shieldRegenMul=2f, hull=new Color(0.15f,0.15f,0.2f), accent=new Color(1f,0.2f,0.25f), shape=ShipShape.Crown, legendary=true },
        };

        public static readonly List<PrimaryDef> Primaries = new List<PrimaryDef>
        {
            new PrimaryDef { id=PrimaryWeapon.Laser, name="Laser Cannon", description="Fast, accurate twin lasers.", price=0, damage=10, fireRate=8f, speed=48f, spread=0f, count=2, size=0.28f, color=new Color(0.3f,0.85f,1f) },
            new PrimaryDef { id=PrimaryWeapon.Plasma, name="Plasma Gun", description="Slow heavy bolts that splash on impact.", price=1200, damage=34, fireRate=2.6f, speed=30f, spread=0f, count=1, size=0.7f, splash=2.2f, color=new Color(0.4f,1f,0.4f) },
            new PrimaryDef { id=PrimaryWeapon.MachineGun, name="Machine Gun", description="Bullet hose. Low damage, absurd fire rate.", price=1600, damage=4.5f, fireRate=22f, speed=55f, spread=5f, count=1, size=0.18f, color=new Color(1f,0.9f,0.4f) },
            new PrimaryDef { id=PrimaryWeapon.Pulse, name="Pulse Cannon", description="Five-way pulse fan for crowd control.", price=2200, damage=8, fireRate=3.6f, speed=36f, spread=34f, count=5, size=0.38f, color=new Color(1f,0.4f,0.9f) },
        };

        public static readonly List<SecondaryDef> Secondaries = new List<SecondaryDef>
        {
            new SecondaryDef { id=SecondaryWeapon.Missiles, name="Missiles", description="Dumb-fire rocket pair. Big boom.", price=0, damage=45, cooldown=0.45f, ammoCost=1, color=new Color(1f,0.55f,0.2f) },
            new SecondaryDef { id=SecondaryWeapon.Homing, name="Homing Missiles", description="Seeks the nearest target.", price=1400, damage=35, cooldown=0.5f, ammoCost=1, color=new Color(1f,0.3f,0.3f) },
            new SecondaryDef { id=SecondaryWeapon.EMP, name="EMP Bomb", description="Stuns enemies and erases bullets in a wide radius.", price=2000, damage=25, cooldown=2.5f, ammoCost=3, color=new Color(0.4f,0.7f,1f) },
            new SecondaryDef { id=SecondaryWeapon.Cluster, name="Cluster Rockets", description="Bursts into 8 bomblets.", price=2600, damage=22, cooldown=1.1f, ammoCost=2, color=new Color(1f,0.85f,0.3f) },
        };

        public static readonly List<UltimateDef> Ultimates = new List<UltimateDef>
        {
            new UltimateDef { id=UltimateAbility.OrbitalStrike, name="Orbital Strike", description="Satellite lances hit every enemy on screen.", price=0, color=new Color(1f,0.5f,0.2f) },
            new UltimateDef { id=UltimateAbility.EnergyWave, name="Energy Wave", description="Expanding shockwave that deletes bullets.", price=2500, color=new Color(0.3f,0.9f,1f) },
            new UltimateDef { id=UltimateAbility.BlackHole, name="Black Hole Generator", description="Spawns a singularity that devours enemies.", price=4000, color=new Color(0.6f,0.3f,1f) },
            new UltimateDef { id=UltimateAbility.DroneSwarm, name="Drone Swarm", description="Six attack drones fight beside you.", price=3500, color=new Color(0.4f,1f,0.5f) },
        };

        public static readonly Dictionary<EnemyKind, EnemyDef> Enemies = new Dictionary<EnemyKind, EnemyDef>
        {
            { EnemyKind.Scout,       new EnemyDef { kind=EnemyKind.Scout, name="Scout", health=14, speed=9f, fireInterval=3.2f, bulletSpeed=13f, bulletDamage=6, contactDamage=15, credits=8, score=50, scale=0.8f, color=new Color(1f,0.75f,0.2f) } },
            { EnemyKind.Fighter,     new EnemyDef { kind=EnemyKind.Fighter, name="Fighter", health=34, speed=6f, fireInterval=1.7f, bulletSpeed=14f, bulletDamage=8, contactDamage=20, credits=14, score=100, scale=1f, color=new Color(1f,0.3f,0.3f) } },
            { EnemyKind.Interceptor, new EnemyDef { kind=EnemyKind.Interceptor, name="Interceptor", health=26, speed=11f, fireInterval=2.2f, bulletSpeed=17f, bulletDamage=7, contactDamage=30, credits=16, score=120, scale=0.9f, color=new Color(1f,0.2f,0.7f) } },
            { EnemyKind.Bomber,      new EnemyDef { kind=EnemyKind.Bomber, name="Bomber", health=90, speed=3.2f, fireInterval=2.6f, bulletSpeed=9f, bulletDamage=14, contactDamage=35, credits=30, score=250, scale=1.5f, color=new Color(0.7f,0.4f,1f) } },
            { EnemyKind.Elite,       new EnemyDef { kind=EnemyKind.Elite, name="Elite", health=170, speed=5f, fireInterval=0.18f, bulletSpeed=11f, bulletDamage=9, contactDamage=40, credits=70, score=600, scale=1.4f, color=new Color(1f,0.95f,0.95f) } },
        };

        public static readonly List<SectorDef> Sectors = new List<SectorDef>
        {
            new SectorDef { index=1, name="Training Galaxy", tagline="Learn to fly. Learn to fight.", newMechanic="Scouts and Fighters",
                bgColor=new Color(0.01f,0.02f,0.06f), nebulaA=new Color(0.1f,0.25f,0.6f), nebulaB=new Color(0.3f,0.1f,0.5f), ambient=new Color(0.35f,0.4f,0.55f),
                enemies=new[]{EnemyKind.Scout, EnemyKind.Fighter}, hazards=new HazardKind[0], waves=5, difficulty=1f,
                boss=new BossDef{ name="Drill Sergeant", title="Training Dreadnought", health=1400, shieldHealth=0, generators=0, scale=3.2f, color=new Color(0.6f,0.65f,0.7f), accent=new Color(1f,0.6f,0.1f), credits=400,
                    phases=new[]{ new[]{BossPattern.AimedVolley, BossPattern.RadialBurst}, new[]{BossPattern.Spiral, BossPattern.AimedVolley}, new[]{BossPattern.RadialBurst, BossPattern.Spiral, BossPattern.Minions} } } },
            new SectorDef { index=2, name="Asteroid Belt", tagline="Rocks don't dodge. You should.", newMechanic="Asteroid fields + Bombers",
                bgColor=new Color(0.04f,0.03f,0.02f), nebulaA=new Color(0.5f,0.3f,0.1f), nebulaB=new Color(0.2f,0.15f,0.1f), ambient=new Color(0.55f,0.45f,0.35f),
                enemies=new[]{EnemyKind.Scout, EnemyKind.Fighter, EnemyKind.Bomber}, hazards=new[]{HazardKind.Asteroid, HazardKind.Debris}, waves=6, difficulty=1.3f,
                boss=new BossDef{ name="Rockjaw", title="Mining Behemoth", health=2400, shieldHealth=600, generators=2, scale=3.6f, color=new Color(0.55f,0.4f,0.25f), accent=new Color(1f,0.8f,0.2f), credits=650,
                    phases=new[]{ new[]{BossPattern.Shotgun, BossPattern.AimedVolley}, new[]{BossPattern.RingWall, BossPattern.Minions}, new[]{BossPattern.Shotgun, BossPattern.RadialBurst, BossPattern.MissileBarrage} } } },
            new SectorDef { index=3, name="Alien Territory", tagline="They were here first.", newMechanic="Interceptors hunt you down",
                bgColor=new Color(0.02f,0.05f,0.03f), nebulaA=new Color(0.1f,0.6f,0.3f), nebulaB=new Color(0.5f,0.1f,0.5f), ambient=new Color(0.35f,0.6f,0.45f),
                enemies=new[]{EnemyKind.Scout, EnemyKind.Fighter, EnemyKind.Interceptor}, hazards=new[]{HazardKind.Debris, HazardKind.Comet}, waves=7, difficulty=1.65f,
                boss=new BossDef{ name="Hive Mother", title="Xel'Thar Broodship", health=3400, shieldHealth=900, generators=3, scale=3.8f, color=new Color(0.2f,0.75f,0.45f), accent=new Color(1f,0.2f,0.8f), credits=900,
                    phases=new[]{ new[]{BossPattern.Minions, BossPattern.Spiral}, new[]{BossPattern.AimedVolley, BossPattern.Minions, BossPattern.RingWall}, new[]{BossPattern.Spiral, BossPattern.MissileBarrage, BossPattern.Minions} } } },
            new SectorDef { index=4, name="Nebula Zone", tagline="Visibility: zero. Threat: maximum.", newMechanic="Elite enemies + ion clouds",
                bgColor=new Color(0.05f,0.01f,0.06f), nebulaA=new Color(0.8f,0.2f,0.6f), nebulaB=new Color(0.2f,0.3f,0.9f), ambient=new Color(0.55f,0.4f,0.6f),
                enemies=new[]{EnemyKind.Fighter, EnemyKind.Interceptor, EnemyKind.Elite}, hazards=new[]{HazardKind.Comet, HazardKind.Debris}, waves=7, difficulty=2.0f,
                boss=new BossDef{ name="Siren", title="Nebula Phantom", health=4400, shieldHealth=1200, generators=3, scale=3.6f, color=new Color(0.7f,0.3f,0.9f), accent=new Color(0.3f,0.9f,1f), credits=1200,
                    phases=new[]{ new[]{BossPattern.LaserSweep, BossPattern.Spiral}, new[]{BossPattern.RingWall, BossPattern.LaserSweep, BossPattern.AimedVolley}, new[]{BossPattern.Spiral, BossPattern.LaserSweep, BossPattern.MissileBarrage} } } },
            new SectorDef { index=5, name="Ancient Machine World", tagline="Something old is waking up.", newMechanic="Laser gates",
                bgColor=new Color(0.03f,0.03f,0.04f), nebulaA=new Color(0.9f,0.6f,0.2f), nebulaB=new Color(0.2f,0.6f,0.8f), ambient=new Color(0.6f,0.55f,0.45f),
                enemies=new[]{EnemyKind.Fighter, EnemyKind.Bomber, EnemyKind.Elite}, hazards=new[]{HazardKind.LaserGate, HazardKind.Debris}, waves=8, difficulty=2.4f,
                boss=new BossDef{ name="Archon", title="Ancient Guardian Engine", health=5600, shieldHealth=1600, generators=4, scale=4.2f, color=new Color(0.85f,0.7f,0.4f), accent=new Color(0.2f,0.9f,1f), credits=1600,
                    phases=new[]{ new[]{BossPattern.LaserSweep, BossPattern.RingWall}, new[]{BossPattern.RadialBurst, BossPattern.LaserSweep, BossPattern.Minions}, new[]{BossPattern.LaserSweep, BossPattern.Spiral, BossPattern.MissileBarrage, BossPattern.RingWall} } } },
            new SectorDef { index=6, name="Dark Matter Sector", tagline="Gravity is the enemy.", newMechanic="Black holes + wormholes",
                bgColor=new Color(0.0f,0.0f,0.01f), nebulaA=new Color(0.3f,0.1f,0.6f), nebulaB=new Color(0.05f,0.05f,0.2f), ambient=new Color(0.35f,0.3f,0.5f),
                enemies=new[]{EnemyKind.Interceptor, EnemyKind.Bomber, EnemyKind.Elite}, hazards=new[]{HazardKind.BlackHole, HazardKind.Wormhole, HazardKind.Debris}, waves=8, difficulty=2.85f,
                boss=new BossDef{ name="Event Horizon", title="Singularity Warden", health=7000, shieldHealth=2000, generators=4, scale=4.2f, color=new Color(0.2f,0.15f,0.3f), accent=new Color(0.7f,0.3f,1f), credits=2100,
                    phases=new[]{ new[]{BossPattern.Spiral, BossPattern.RingWall}, new[]{BossPattern.MissileBarrage, BossPattern.LaserSweep, BossPattern.Minions}, new[]{BossPattern.Spiral, BossPattern.RadialBurst, BossPattern.LaserSweep, BossPattern.Shotgun} } } },
            new SectorDef { index=7, name="Final War Zone", tagline="Everything you've learned. All at once.", newMechanic="Every enemy, every hazard",
                bgColor=new Color(0.06f,0.01f,0.01f), nebulaA=new Color(0.9f,0.2f,0.1f), nebulaB=new Color(0.4f,0.1f,0.4f), ambient=new Color(0.65f,0.4f,0.4f),
                enemies=new[]{EnemyKind.Scout, EnemyKind.Fighter, EnemyKind.Interceptor, EnemyKind.Bomber, EnemyKind.Elite}, hazards=new[]{HazardKind.Asteroid, HazardKind.Comet, HazardKind.BlackHole, HazardKind.LaserGate, HazardKind.Wormhole}, waves=9, difficulty=3.4f,
                boss=new BossDef{ name="Omega Tyrant", title="Flagship of the Final War", health=10000, shieldHealth=2800, generators=5, scale=4.8f, color=new Color(0.3f,0.05f,0.05f), accent=new Color(1f,0.25f,0.15f), credits=4000,
                    phases=new[]{ new[]{BossPattern.AimedVolley, BossPattern.RingWall, BossPattern.Minions}, new[]{BossPattern.LaserSweep, BossPattern.Spiral, BossPattern.MissileBarrage, BossPattern.Shotgun}, new[]{BossPattern.LaserSweep, BossPattern.Spiral, BossPattern.RadialBurst, BossPattern.MissileBarrage, BossPattern.Minions, BossPattern.RingWall} } } },
        };

        public static readonly string[] UpgradeNames = { "Damage", "Fire Rate", "Shield Capacity", "Hull Health", "Boost Speed", "Energy Capacity", "Missile Capacity" };
        public static readonly string[] UpgradeDescriptions = { "+8% weapon damage per level", "+6% fire rate per level", "+12% shield per level", "+12% health per level", "+8% boost speed per level", "+12% energy per level", "+3 missiles per level" };
        public const int MaxUpgradeLevel = 10;

        public static int UpgradeCost(UpgradeStat stat, int currentLevel)
        {
            float baseCost = stat == UpgradeStat.Damage || stat == UpgradeStat.FireRate ? 250f : 200f;
            return Mathf.RoundToInt(baseCost * Mathf.Pow(currentLevel + 1, 1.55f) / 10f) * 10;
        }

        public static ShipDef Ship(string id) { foreach (var s in Ships) if (s.id == id) return s; return Ships[0]; }
        public static PrimaryDef Primary(PrimaryWeapon w) => Primaries[(int)w];
        public static SecondaryDef Secondary(SecondaryWeapon w) => Secondaries[(int)w];
        public static UltimateDef Ultimate(UltimateAbility u) => Ultimates[(int)u];
    }
}
