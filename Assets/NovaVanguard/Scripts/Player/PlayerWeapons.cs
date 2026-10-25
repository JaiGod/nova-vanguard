using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Primary gun, secondary launcher and weapon switching for the player.</summary>
    public class PlayerWeapons : MonoBehaviour
    {
        PlayerShip ship;
        public PrimaryWeapon Primary { get; private set; }
        public SecondaryWeapon Secondary { get; private set; }
        public float SecondaryCooldown { get; private set; }
        public float PrimaryHeat { get; private set; }  // 0..1 cosmetic for HUD
        float fireTimer;
        int sideToggle;

        public void Init(PlayerShip s)
        {
            ship = s;
            var d = SaveSystem.Data;
            Primary = (PrimaryWeapon)Mathf.Clamp(d.selectedPrimary, 0, 3);
            Secondary = (SecondaryWeapon)Mathf.Clamp(d.selectedSecondary, 0, 3);
        }

        public PrimaryDef PrimaryDef => GameData.Primary(Primary);
        public SecondaryDef SecondaryDef => GameData.Secondary(Secondary);

        void Update()
        {
            float dt = Time.deltaTime;
            fireTimer -= dt;
            SecondaryCooldown = Mathf.Max(0f, SecondaryCooldown - dt);
            PrimaryHeat = Mathf.Max(0f, PrimaryHeat - dt * 1.5f);
            if (!ship.Controllable || !ship.IsAlive) return;

            if (InputRouter.SelectPrimary >= 0) TrySelectPrimary(InputRouter.SelectPrimary);
            if (InputRouter.CyclePrimaryPressed) CyclePrimary();
            if (InputRouter.CycleSecondaryPressed) CycleSecondary();

            if (InputRouter.Fire && fireTimer <= 0f) FirePrimary();
            if (InputRouter.SecondaryPressed) FireSecondary();
        }

        void FirePrimary()
        {
            var def = PrimaryDef;
            float rate = def.fireRate * ship.FireRateMul;
            fireTimer = 1f / rate;
            PrimaryHeat = Mathf.Min(1f, PrimaryHeat + 0.08f);
            float dmg = def.damage * ship.DamageMul;
            var fwd = transform.forward; var right = transform.right;
            var origin = transform.position + fwd * 1.2f;
            float life = 2.2f * ship.RangeMul;
            int count = def.count;
            for (int i = 0; i < count; i++)
            {
                float ang = 0f; Vector3 off = Vector3.zero;
                if (def.id == PrimaryWeapon.Laser) { off = right * (i == 0 ? -0.55f : 0.55f); }
                else if (def.id == PrimaryWeapon.Pulse) { ang = Mathf.Lerp(-def.spread, def.spread, count == 1 ? 0.5f : i / (float)(count - 1)); }
                else if (def.id == PrimaryWeapon.MachineGun) { ang = Random.Range(-def.spread, def.spread); sideToggle ^= 1; off = right * (sideToggle == 0 ? -0.4f : 0.4f); }
                var dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd;
                Projectile.Fire(origin + off, dir, def.speed, dmg, Team.Player, def.color, def.size, life, def.splash, def.id == PrimaryWeapon.Laser ? 1 : 0);
            }
            SaveSystem.Data.stats.shotsFired++;
            Sfx s = def.id == PrimaryWeapon.Laser ? Sfx.Laser : def.id == PrimaryWeapon.Plasma ? Sfx.Plasma : def.id == PrimaryWeapon.MachineGun ? Sfx.MachineGun : Sfx.Pulse;
            AudioManager.Play(s, transform.position, 0.5f);
            CameraRig.Shake(def.id == PrimaryWeapon.Plasma ? 0.06f : 0.02f);
        }

        void FireSecondary()
        {
            var def = SecondaryDef;
            if (SecondaryCooldown > 0f) return;
            if (ship.Missiles < def.ammoCost) { AudioManager.PlayUI(Sfx.PowerDown); HUD.Alert("NO MISSILES", new Color(1f, 0.6f, 0.2f)); return; }
            ship.Missiles -= def.ammoCost;
            SecondaryCooldown = def.cooldown;
            float dmg = def.damage * ship.DamageMul;
            var fwd = transform.forward; var right = transform.right; var pos = transform.position;
            switch (def.id)
            {
                case SecondaryWeapon.Missiles:
                    Missile.Launch(pos + right * -0.8f, Quaternion.AngleAxis(-6f, Vector3.up) * fwd, MissileMode.Dumb, dmg, Team.Player, def.color);
                    Missile.Launch(pos + right * 0.8f, Quaternion.AngleAxis(6f, Vector3.up) * fwd, MissileMode.Dumb, dmg, Team.Player, def.color);
                    break;
                case SecondaryWeapon.Homing:
                    Missile.Launch(pos + right * -0.8f, Quaternion.AngleAxis(-40f, Vector3.up) * fwd, MissileMode.Homing, dmg, Team.Player, def.color, 16f);
                    Missile.Launch(pos + right * 0.8f, Quaternion.AngleAxis(40f, Vector3.up) * fwd, MissileMode.Homing, dmg, Team.Player, def.color, 16f);
                    break;
                case SecondaryWeapon.EMP:
                    Missile.Launch(pos + fwd, fwd, MissileMode.EMP, dmg, Team.Player, def.color, 14f);
                    break;
                case SecondaryWeapon.Cluster:
                    Missile.Launch(pos + fwd, fwd, MissileMode.Cluster, dmg, Team.Player, def.color, 18f);
                    break;
            }
            SaveSystem.Data.stats.missilesFired++;
            AudioManager.Play(Sfx.Missile, pos, 0.8f);
            CameraRig.Shake(0.08f);
        }

        public bool TrySelectPrimary(int idx)
        {
            if (!SaveSystem.Data.unlockedPrimaries.Contains(idx)) return false;
            Primary = (PrimaryWeapon)idx; SaveSystem.Data.selectedPrimary = idx;
            AudioManager.PlayUI(Sfx.UiClick);
            HUD.Alert(PrimaryDef.name.ToUpperInvariant(), PrimaryDef.color);
            return true;
        }

        public void CyclePrimary()
        {
            for (int i = 1; i <= 4; i++) { int idx = ((int)Primary + i) % 4; if (SaveSystem.Data.unlockedPrimaries.Contains(idx)) { TrySelectPrimary(idx); return; } }
        }

        public void CycleSecondary()
        {
            for (int i = 1; i <= 4; i++)
            {
                int idx = ((int)Secondary + i) % 4;
                if (SaveSystem.Data.unlockedSecondaries.Contains(idx))
                {
                    Secondary = (SecondaryWeapon)idx; SaveSystem.Data.selectedSecondary = idx;
                    AudioManager.PlayUI(Sfx.UiClick);
                    HUD.Alert(SecondaryDef.name.ToUpperInvariant(), SecondaryDef.color);
                    return;
                }
            }
        }
    }
}
