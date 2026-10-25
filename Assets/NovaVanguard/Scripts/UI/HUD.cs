using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NovaVanguard
{
    /// <summary>In-game HUD: bars, ammo, score, boss bar, objectives, alerts, floating text and the touch controls.</summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }
        Image healthFill, shieldFill, energyFill, ultFill, bossFill, bossShieldFill, secondaryCd;
        Text healthTxt, shieldTxt, energyTxt, ultTxt, bossTxt, bossName, scoreTxt, creditsTxt, missileTxt, weaponTxt, secondaryTxt, objectiveTxt, livesTxt, comboTxt, fpsTxt, alertTxt, bannerTitle, bannerName, bannerTag;
        RectTransform bossPanel, banner, alertRect, tutorial;
        CanvasGroup alertGroup, bannerGroup, tutorialGroup;
        Coroutine alertCo, bannerCo;
        readonly List<FloatText> floats = new List<FloatText>();
        TouchControls touch;
        float fpsAcc; int fpsFrames;

        class FloatText { public Text t; public RectTransform r; public Vector3 world; public float life; }

        public static HUD Create(Transform canvas)
        {
            var root = UI.Fill("HUD", canvas);
            var h = root.gameObject.AddComponent<HUD>();
            Instance = h;
            h.Build(root);
            return h;
        }

        void Build(RectTransform root)
        {
            // ---- top-left: ship status ----
            var tl = UI.Anchored("Status", root, new Vector2(0, 1), new Vector2(24, -24), new Vector2(420, 150));
            UI.Image(tl, UI.Panel, true);
            healthFill = UI.Bar(tl, new Vector2(0, 1), new Vector2(14, -14), new Vector2(392, 30), new Color(0.3f, 1f, 0.45f), out healthTxt);
            shieldFill = UI.Bar(tl, new Vector2(0, 1), new Vector2(14, -52), new Vector2(392, 24), new Color(0.3f, 0.7f, 1f), out shieldTxt);
            energyFill = UI.Bar(tl, new Vector2(0, 1), new Vector2(14, -84), new Vector2(392, 20), new Color(1f, 0.85f, 0.3f), out energyTxt);
            livesTxt = UI.Label("Lives", tl, new Vector2(0, 0), new Vector2(14, 10), new Vector2(200, 28), "", 22, UI.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            weaponTxt = UI.Label("Weapon", tl, new Vector2(1, 0), new Vector2(-14, 10), new Vector2(220, 28), "", 20, UI.Cyan, TextAnchor.MiddleRight, FontStyle.Bold);

            // ---- top-right: score ----
            var tr = UI.Anchored("Score", root, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(360, 110));
            UI.Image(tr, UI.Panel, true);
            scoreTxt = UI.Label("ScoreTxt", tr, new Vector2(1, 1), new Vector2(-14, -10), new Vector2(330, 44), "0", 40, UI.TextColor, TextAnchor.MiddleRight, FontStyle.Bold);
            creditsTxt = UI.Label("CreditsTxt", tr, new Vector2(1, 1), new Vector2(-14, -58), new Vector2(330, 30), "", 24, UI.Gold, TextAnchor.MiddleRight);
            comboTxt = UI.Label("Combo", root, new Vector2(1, 1), new Vector2(-24, -150), new Vector2(360, 40), "", 30, UI.Gold, TextAnchor.MiddleRight, FontStyle.Bold);

            // ---- bottom-right: weapons ----
            var br = UI.Anchored("Weapons", root, new Vector2(1, 0), new Vector2(-24, 24), new Vector2(360, 120));
            UI.Image(br, UI.Panel, true);
            missileTxt = UI.Label("Missiles", br, new Vector2(1, 1), new Vector2(-14, -10), new Vector2(330, 34), "", 26, new Color(1f, 0.6f, 0.3f), TextAnchor.MiddleRight, FontStyle.Bold);
            secondaryTxt = UI.Label("Secondary", br, new Vector2(1, 1), new Vector2(-14, -46), new Vector2(330, 26), "", 20, UI.Dim, TextAnchor.MiddleRight);
            secondaryCd = UI.Bar(br, new Vector2(0, 0), new Vector2(14, 44), new Vector2(332, 10), new Color(1f, 0.6f, 0.3f), out _);
            ultFill = UI.Bar(br, new Vector2(0, 0), new Vector2(14, 12), new Vector2(332, 26), new Color(0.75f, 0.4f, 1f), out ultTxt);

            // ---- top-center: objective ----
            objectiveTxt = UI.Label("Objective", root, new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(800, 34), "", 24, UI.Dim);

            // ---- boss bar ----
            bossPanel = UI.Anchored("BossPanel", root, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(900, 70));
            UI.Image(bossPanel, UI.Panel, true);
            bossName = UI.Label("BossName", bossPanel, new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(880, 26), "", 22, UI.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
            bossFill = UI.Bar(bossPanel, new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(870, 22), UI.Red, out bossTxt);
            bossShieldFill = UI.Bar(bossPanel, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(870, 10), new Color(0.4f, 0.7f, 1f), out _);
            bossPanel.gameObject.SetActive(false);

            // ---- alerts ----
            alertRect = UI.Anchored("Alert", root, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1000, 70));
            alertTxt = UI.Label(alertRect, "", 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            alertGroup = alertRect.gameObject.AddComponent<CanvasGroup>(); alertGroup.alpha = 0f;

            // ---- sector banner ----
            banner = UI.Anchored("Banner", root, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1200, 220));
            UI.Image(banner, new Color(0, 0, 0, 0.55f));
            bannerTitle = UI.Label("T", banner, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(1100, 50), "", 34, UI.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerName = UI.Label("N", banner, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(1100, 80), "", 68, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerTag = UI.Label("S", banner, new Vector2(0.5f, 1), new Vector2(0, -165), new Vector2(1100, 40), "", 26, UI.Dim, TextAnchor.MiddleCenter, FontStyle.Italic);
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>(); bannerGroup.alpha = 0f;

            // ---- tutorial card ----
            tutorial = UI.Anchored("Tutorial", root, new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(760, 150));
            UI.Image(tutorial, UI.Panel, true);
            UI.Label("TutTxt", tutorial, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 130),
                InputRouter.ShowTouchControls
                ? "Left stick: move   •   FIRE button: shoot   •   MSL: missiles\nBOOST: speed burst   •   ULT: ultimate when charged"
                : "WASD: move   •   Mouse: aim   •   Left click: fire   •   Right click / F: missiles\nShift: boost   •   Space: ultimate   •   1-4 / E: weapons   •   Q: secondary   •   Esc: pause",
                22, UI.TextColor);
            tutorialGroup = tutorial.gameObject.AddComponent<CanvasGroup>(); tutorialGroup.alpha = 0f;

            fpsTxt = UI.Label("FPS", root, new Vector2(0, 0), new Vector2(24, 24), new Vector2(200, 30), "", 20, UI.Dim, TextAnchor.LowerLeft);

            touch = TouchControls.Create(root);
        }

        void Update()
        {
            var gm = GameManager.Instance; var p = PlayerShip.Instance;
            if (gm == null || p == null) return;
            healthFill.fillAmount = p.health / p.maxHealth; healthTxt.text = $"HULL {Mathf.CeilToInt(p.health)}";
            shieldFill.fillAmount = p.Shield / p.MaxShield; shieldTxt.text = $"SHIELD {Mathf.CeilToInt(p.Shield)}";
            energyFill.fillAmount = p.Energy / p.MaxEnergy; energyTxt.text = $"ENERGY {Mathf.CeilToInt(p.Energy)}";
            healthFill.color = p.health < p.maxHealth * 0.25f && Mathf.Repeat(Time.unscaledTime * 3f, 1f) < 0.5f ? UI.Red : new Color(0.3f, 1f, 0.45f);
            livesTxt.text = $"LIVES x{Mathf.Max(0, p.Lives)}";
            weaponTxt.text = p.Weapons.PrimaryDef.name.ToUpperInvariant();
            weaponTxt.color = p.Weapons.PrimaryDef.color;
            scoreTxt.text = gm.RunScore.ToString("N0");
            creditsTxt.text = $"+{gm.RunCredits:N0} ¢   (bank {SaveSystem.Data.credits:N0})";
            comboTxt.text = gm.Combo >= 3 ? $"COMBO x{gm.Combo}" : "";
            missileTxt.text = $"MISSILES {p.Missiles}/{p.MaxMissiles}";
            secondaryTxt.text = p.Weapons.SecondaryDef.name.ToUpperInvariant() + (p.Weapons.SecondaryDef.ammoCost > 1 ? $"  ({p.Weapons.SecondaryDef.ammoCost}/shot)" : "");
            secondaryCd.fillAmount = p.Weapons.SecondaryDef.cooldown > 0 ? 1f - p.Weapons.SecondaryCooldown / p.Weapons.SecondaryDef.cooldown : 1f;
            ultFill.fillAmount = p.UltimateCharge / 100f;
            bool ready = p.UltimateCharge >= 100f;
            ultTxt.text = ready ? $"{gm.Ultimate.Def.name.ToUpperInvariant()} READY" : $"{gm.Ultimate.Def.name.ToUpperInvariant()} {Mathf.FloorToInt(p.UltimateCharge)}%";
            ultFill.color = ready && Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.5f ? Color.white : new Color(0.75f, 0.4f, 1f);
            objectiveTxt.text = gm.Objective;

            var boss = Boss.Current;
            bossPanel.gameObject.SetActive(boss != null && !boss.Entering && boss.IsAlive);
            if (boss != null)
            {
                bossName.text = $"{boss.Def.name.ToUpperInvariant()}  -  {boss.Def.title}   [PHASE {boss.Phase}]";
                bossFill.fillAmount = boss.health / boss.maxHealth; bossTxt.text = Mathf.CeilToInt(boss.health).ToString();
                bossShieldFill.fillAmount = boss.Def.shieldHealth > 0 ? boss.ShieldHealth / boss.Def.shieldHealth : 0f;
            }

            // floating text
            var cam = CameraRig.Instance.Cam;
            for (int i = floats.Count - 1; i >= 0; i--)
            {
                var f = floats[i]; f.life -= Time.deltaTime; f.world += Vector3.forward * Time.deltaTime * 2f;
                if (f.life <= 0f) { Destroy(f.r.gameObject); floats.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(f.world);
                var canvasRect = (RectTransform)transform;
                f.r.anchoredPosition = new Vector2(sp.x / Screen.width * canvasRect.rect.width, sp.y / Screen.height * canvasRect.rect.height);
                f.t.color = new Color(f.t.color.r, f.t.color.g, f.t.color.b, Mathf.Clamp01(f.life * 2f));
            }

            if (SaveSystem.Data.settings.showFps)
            {
                fpsAcc += Time.unscaledDeltaTime; fpsFrames++;
                if (fpsAcc >= 0.5f) { fpsTxt.text = $"{fpsFrames / fpsAcc:0} FPS"; fpsAcc = 0f; fpsFrames = 0; }
            }
            else fpsTxt.text = "";
            touch.gameObject.SetActive(InputRouter.ShowTouchControls);
        }

        // ---------------------------------------------------------------- static API used by gameplay
        public static void Alert(string text, Color color, float hold = 0.7f)
        {
            var h = Instance; if (h == null) return;
            if (h.alertCo != null) h.StopCoroutine(h.alertCo);
            h.alertCo = h.StartCoroutine(h.AlertCo(text, color, hold));
        }

        IEnumerator AlertCo(string text, Color color, float hold)
        {
            alertTxt.text = text; alertTxt.color = color;
            float t = 0f;
            while (t < 0.15f) { t += Time.unscaledDeltaTime; alertGroup.alpha = t / 0.15f; alertRect.localScale = Vector3.one * (1.3f - 0.3f * (t / 0.15f)); yield return null; }
            alertRect.localScale = Vector3.one; alertGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(hold);
            t = 0f;
            while (t < 0.4f) { t += Time.unscaledDeltaTime; alertGroup.alpha = 1f - t / 0.4f; yield return null; }
            alertGroup.alpha = 0f;
        }

        public static void Banner(string title, string name, string tag)
        {
            var h = Instance; if (h == null) return;
            if (h.bannerCo != null) h.StopCoroutine(h.bannerCo);
            h.bannerCo = h.StartCoroutine(h.BannerCo(title, name, tag, 2.4f));
        }

        public static void BossIntro(BossDef def)
        {
            var h = Instance; if (h == null) return;
            if (h.bannerCo != null) h.StopCoroutine(h.bannerCo);
            h.bannerCo = h.StartCoroutine(h.BannerCo("!!  BOSS INCOMING  !!", def.name.ToUpperInvariant(), def.title, 3f));
        }

        IEnumerator BannerCo(string title, string name, string tag, float hold)
        {
            bannerTitle.text = title; bannerName.text = name; bannerTag.text = tag;
            float t = 0f;
            while (t < 0.3f) { t += Time.deltaTime; bannerGroup.alpha = t / 0.3f; yield return null; }
            yield return new WaitForSeconds(hold);
            t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; bannerGroup.alpha = 1f - t / 0.5f; yield return null; }
            bannerGroup.alpha = 0f;
        }

        public static void Tutorial() { if (Instance != null) Instance.StartCoroutine(Instance.TutorialCo()); }
        IEnumerator TutorialCo()
        {
            float t = 0f;
            while (t < 0.4f) { t += Time.deltaTime; tutorialGroup.alpha = t / 0.4f; yield return null; }
            yield return new WaitForSeconds(9f);
            t = 0f;
            while (t < 0.8f) { t += Time.deltaTime; tutorialGroup.alpha = 1f - t / 0.8f; yield return null; }
        }

        public static void Float(Vector3 world, string text, Color color)
        {
            var h = Instance; if (h == null || h.floats.Count > 24) return;
            var r = UI.Anchored("Float", h.transform, Vector2.zero, Vector2.zero, new Vector2(300, 40));
            var t = UI.Label(r, text, 24, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            h.floats.Add(new FloatText { t = t, r = r, world = world, life = 0.9f });
        }
    }
}
