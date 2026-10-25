using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NovaVanguard
{
    /// <summary>All menu screens. Each screen is rebuilt when shown so it always reflects the save data.</summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        Canvas menuCanvas, hudCanvas;
        RectTransform screen;        // current menu screen root
        HUD hud;
        int shipPage;

        public static UIManager Create()
        {
            var go = new GameObject("UIManager");
            DontDestroyOnLoad(go);
            var ui = go.AddComponent<UIManager>();
            Instance = ui;
            UI.EnsureEventSystem();
            ui.hudCanvas = UI.CreateCanvas("HUD Canvas", 10); ui.hudCanvas.transform.SetParent(go.transform);
            ui.menuCanvas = UI.CreateCanvas("Menu Canvas", 20); ui.menuCanvas.transform.SetParent(go.transform);
            ui.hud = HUD.Create(ui.hudCanvas.transform);
            ui.hudCanvas.gameObject.SetActive(false);
            return ui;
        }

        RectTransform NewScreen(string name, bool dim = true)
        {
            if (screen != null) Destroy(screen.gameObject);
            screen = UI.Fill(name, menuCanvas.transform);
            if (dim) UI.Image(screen, new Color(0.01f, 0.02f, 0.05f, 0.72f));
            menuCanvas.gameObject.SetActive(true);
            return screen;
        }

        void HideMenu() { if (screen != null) Destroy(screen.gameObject); screen = null; menuCanvas.gameObject.SetActive(false); }

        public void ShowHud() { HideMenu(); hudCanvas.gameObject.SetActive(true); }

        // ------------------------------------------------------------------ main menu
        public void ShowMainMenu()
        {
            hudCanvas.gameObject.SetActive(false);
            var s = NewScreen("MainMenu", false);
            var d = SaveSystem.Data;
            UI.Label("Logo", s, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1400, 140), "NOVA VANGUARD", 110, UI.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Label("Tag", s, new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(1400, 40), "ARCADE SPACE SHOOTER", 28, UI.Dim);
            float y = 40f;
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(420, 76), "PLAY", ShowSectorSelect, UI.Cyan * new Color(0.5f, 0.5f, 0.5f, 1f), 38); y -= 92;
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(420, 66), "SHIPS", ShowShips); y -= 80;
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(420, 66), "UPGRADES", ShowUpgrades); y -= 80;
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(420, 66), "SETTINGS", ShowSettings); y -= 80;
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(420, 66), "CREDITS", ShowCredits); y -= 80;
#if !UNITY_WEBGL
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(420, 56), "QUIT", () => Application.Quit(), null, 24);
#endif
            var ship = GameData.Ship(d.selectedShip);
            UI.Label("Info", s, new Vector2(0, 0), new Vector2(30, 30), new Vector2(700, 120),
                $"PILOT BANK  {d.credits:N0} ¢\nSHIP  {ship.name}  ({ship.role})\nBEST SECTOR  {d.highestSectorCleared}/{GameData.Sectors.Count}     HIGH SCORE  {d.stats.highScore:N0}",
                24, UI.TextColor, TextAnchor.LowerLeft);
            UI.Label("Ver", s, new Vector2(1, 0), new Vector2(-30, 30), new Vector2(400, 30), "v" + Application.version, 18, UI.Dim, TextAnchor.LowerRight);
        }

        // ------------------------------------------------------------------ sector select
        void ShowSectorSelect()
        {
            var s = NewScreen("SectorSelect");
            UI.Header(s, "SELECT SECTOR", "Clear a sector to unlock the next one");
            var d = SaveSystem.Data;
            int cols = 4; float w = 400, h = 190, gapX = 24, gapY = 24;
            for (int i = 0; i < GameData.Sectors.Count; i++)
            {
                var sec = GameData.Sectors[i];
                bool unlocked = i <= d.highestSectorCleared;
                int col = i % cols, row = i / cols;
                float x = (col - (cols - 1) * 0.5f) * (w + gapX);
                float y = 60 - row * (h + gapY);
                var card = UI.Anchored("Sector" + sec.index, s, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
                UI.Image(card, unlocked ? UI.PanelLight : new Color(0.05f, 0.05f, 0.08f, 0.8f), true);
                var swatch = UI.Anchored("Sw", card, new Vector2(0, 1), new Vector2(14, -14), new Vector2(w - 28, 10));
                UI.Image(swatch, unlocked ? Color.Lerp(sec.nebulaA, sec.nebulaB, 0.5f) : UI.Dim * 0.5f, true);
                UI.Label("T", card, new Vector2(0, 1), new Vector2(14, -32), new Vector2(w - 28, 40), $"SECTOR {sec.index}  -  {sec.name.ToUpperInvariant()}", 24, unlocked ? UI.Cyan : UI.Dim, TextAnchor.MiddleLeft, FontStyle.Bold);
                UI.Label("S", card, new Vector2(0, 1), new Vector2(14, -70), new Vector2(w - 28, 60), unlocked ? $"{sec.tagline}\nNew: {sec.newMechanic}\nBoss: {sec.boss.name}  •  {sec.waves} waves" : "LOCKED  -  clear the previous sector", 18, UI.TextColor, TextAnchor.UpperLeft);
                if (unlocked)
                {
                    int idx = sec.index;
                    UI.Button(card, new Vector2(1, 0), new Vector2(-14, 14), new Vector2(150, 44), i < d.highestSectorCleared ? "REPLAY" : "LAUNCH", () => GameManager.Instance.StartSector(idx), null, 22);
                }
            }
            BackButton(s, ShowMainMenu);
        }

        // ------------------------------------------------------------------ ships & loadout
        void ShowShips()
        {
            var s = NewScreen("Ships");
            var d = SaveSystem.Data;
            UI.Header(s, "HANGAR", $"Bank: {d.credits:N0} ¢   •   Select a ship, primary, secondary and ultimate");

            // Ship cards (left)
            float cardW = 300, cardH = 330;
            var ships = GameData.Ships;
            int perPage = 4; int pages = Mathf.CeilToInt(ships.Count / (float)perPage);
            shipPage = Mathf.Clamp(shipPage, 0, pages - 1);
            for (int k = 0; k < perPage; k++)
            {
                int i = shipPage * perPage + k; if (i >= ships.Count) break;
                var sh = ships[i];
                bool owned = d.unlockedShips.Contains(sh.id);
                bool selected = d.selectedShip == sh.id;
                bool reqMet = d.highestSectorCleared >= sh.requiredSector;
                float x = -660 + k * (cardW + 20);
                var card = UI.Anchored("Ship " + sh.id, s, new Vector2(0.5f, 0.5f), new Vector2(x + cardW / 2f, 80), new Vector2(cardW, cardH));
                UI.Image(card, selected ? new Color(0.1f, 0.3f, 0.45f, 0.95f) : UI.PanelLight, true);
                var sw = UI.Anchored("Sw", card, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(cardW - 30, 60));
                UI.Image(sw, sh.hull, true);
                UI.Image(UI.Anchored("Acc", sw, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cardW - 60, 10)), sh.accent, true);
                UI.Label("N", card, new Vector2(0.5f, 1), new Vector2(0, -86), new Vector2(cardW - 20, 34), sh.name.ToUpperInvariant(), 24, sh.legendary ? UI.Gold : UI.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
                UI.Label("R", card, new Vector2(0.5f, 1), new Vector2(0, -114), new Vector2(cardW - 20, 24), sh.role.ToUpperInvariant() + (sh.legendary ? "  *" : ""), 16, UI.Dim);
                UI.Label("D", card, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(cardW - 30, 90), sh.description, 16, UI.TextColor, TextAnchor.UpperCenter);
                UI.Label("S", card, new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(cardW - 30, 60),
                    $"SPD {sh.speed:0}   HP {sh.health:0}   SH {sh.shield:0}\nEN {sh.energy:0}   DMG x{sh.damageMul:0.0}   ROF x{sh.fireRateMul:0.0}", 15, UI.Dim);
                if (owned)
                {
                    if (selected) UI.Label("Sel", card, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(cardW - 30, 44), "SELECTED", 22, UI.Green, TextAnchor.MiddleCenter, FontStyle.Bold);
                    else UI.Button(card, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(cardW - 30, 44), "SELECT", () => { d.selectedShip = sh.id; SaveSystem.Save(); ShowShips(); }, null, 22);
                }
                else if (!reqMet)
                    UI.Label("Lock", card, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(cardW - 30, 44), $"CLEAR SECTOR {sh.requiredSector}", 18, UI.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
                else
                    UI.Button(card, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(cardW - 30, 44), $"BUY  {sh.price:N0} ¢", () =>
                    {
                        if (SaveSystem.Spend(sh.price)) { d.unlockedShips.Add(sh.id); d.selectedShip = sh.id; SaveSystem.Save(); AudioManager.PlayUI(Sfx.Purchase); }
                        else AudioManager.PlayUI(Sfx.PowerDown);
                        ShowShips();
                    }, d.credits >= sh.price ? new Color(0.15f, 0.35f, 0.2f, 0.95f) : (Color?)null, 20);
            }
            if (pages > 1)
            {
                UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(-700, 80), new Vector2(60, 200), "<", () => { shipPage = (shipPage + pages - 1) % pages; ShowShips(); });
                UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(700, 80), new Vector2(60, 200), ">", () => { shipPage = (shipPage + 1) % pages; ShowShips(); });
            }

            // Loadout rows (bottom)
            float rowY = -170;
            LoadoutRow(s, rowY, "PRIMARY", 4, i => GameData.Primaries[i].name, i => GameData.Primaries[i].price, i => GameData.Primaries[i].description, i => GameData.Primaries[i].color,
                d.unlockedPrimaries, d.selectedPrimary, i => { d.selectedPrimary = i; });
            LoadoutRow(s, rowY - 100, "SECONDARY", 4, i => GameData.Secondaries[i].name, i => GameData.Secondaries[i].price, i => GameData.Secondaries[i].description, i => GameData.Secondaries[i].color,
                d.unlockedSecondaries, d.selectedSecondary, i => { d.selectedSecondary = i; });
            LoadoutRow(s, rowY - 200, "ULTIMATE", 4, i => GameData.Ultimates[i].name, i => GameData.Ultimates[i].price, i => GameData.Ultimates[i].description, i => GameData.Ultimates[i].color,
                d.unlockedUltimates, d.selectedUltimate, i => { d.selectedUltimate = i; });
            BackButton(s, ShowMainMenu);
        }

        void LoadoutRow(Transform parent, float y, string title, int count, System.Func<int, string> name, System.Func<int, int> price, System.Func<int, string> desc, System.Func<int, Color> color,
            List<int> unlocked, int selected, System.Action<int> select)
        {
            UI.Label(title, parent, new Vector2(0.5f, 0.5f), new Vector2(-760, y), new Vector2(200, 80), title, 20, UI.Dim, TextAnchor.MiddleLeft, FontStyle.Bold);
            float w = 330;
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                float x = -560 + i * (w + 16) + w / 2f;
                var card = UI.Anchored(title + i, parent, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, 88));
                bool own = unlocked.Contains(i), sel = selected == i;
                UI.Image(card, sel ? new Color(0.1f, 0.3f, 0.45f, 0.95f) : UI.PanelLight, true);
                UI.Image(UI.Anchored("c", card, new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(8, 70)), color(i), true);
                UI.Label("n", card, new Vector2(0, 1), new Vector2(24, -8), new Vector2(w - 150, 26), name(i).ToUpperInvariant(), 18, color(i), TextAnchor.MiddleLeft, FontStyle.Bold);
                UI.Label("d", card, new Vector2(0, 1), new Vector2(24, -34), new Vector2(w - 40, 50), desc(i), 14, UI.TextColor, TextAnchor.UpperLeft);
                if (own)
                {
                    if (sel) UI.Label("s", card, new Vector2(1, 1), new Vector2(-10, -8), new Vector2(120, 26), "EQUIPPED", 14, UI.Green, TextAnchor.MiddleRight, FontStyle.Bold);
                    else UI.Button(card, new Vector2(1, 1), new Vector2(-8, -6), new Vector2(100, 30), "EQUIP", () => { select(idx); SaveSystem.Save(); ShowShips(); }, null, 14);
                }
                else UI.Button(card, new Vector2(1, 1), new Vector2(-8, -6), new Vector2(120, 30), $"{price(i):N0} ¢", () =>
                {
                    if (SaveSystem.Spend(price(idx))) { unlocked.Add(idx); select(idx); SaveSystem.Save(); AudioManager.PlayUI(Sfx.Purchase); }
                    else AudioManager.PlayUI(Sfx.PowerDown);
                    ShowShips();
                }, SaveSystem.Data.credits >= price(i) ? new Color(0.15f, 0.35f, 0.2f, 0.95f) : (Color?)null, 14);
            }
        }

        // ------------------------------------------------------------------ upgrades
        void ShowUpgrades()
        {
            var s = NewScreen("Upgrades");
            var d = SaveSystem.Data;
            UI.Header(s, "UPGRADES", $"Bank: {d.credits:N0} ¢   •   Upgrades apply to every ship");
            for (int i = 0; i < 7; i++)
            {
                var stat = (UpgradeStat)i; int lvl = d.upgrades[i]; int cost = GameData.UpgradeCost(stat, lvl); bool maxed = lvl >= GameData.MaxUpgradeLevel;
                float y = 180 - i * 90;
                var row = UI.Anchored("Up" + i, s, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(1100, 78));
                UI.Image(row, UI.PanelLight, true);
                UI.Label("n", row, new Vector2(0, 0.5f), new Vector2(20, 12), new Vector2(400, 30), GameData.UpgradeNames[i].ToUpperInvariant(), 24, UI.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
                UI.Label("d", row, new Vector2(0, 0.5f), new Vector2(20, -16), new Vector2(400, 26), GameData.UpgradeDescriptions[i], 16, UI.Dim, TextAnchor.MiddleLeft);
                for (int p = 0; p < GameData.MaxUpgradeLevel; p++)
                    UI.Image(UI.Anchored("pip", row, new Vector2(0, 0.5f), new Vector2(440 + p * 34, 0), new Vector2(28, 28)), p < lvl ? UI.Gold : new Color(0.2f, 0.25f, 0.35f), true);
                UI.Label("lvl", row, new Vector2(1, 0.5f), new Vector2(-220, 0), new Vector2(100, 30), $"LV {lvl}", 22, UI.TextColor, TextAnchor.MiddleRight);
                if (maxed) UI.Label("max", row, new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(180, 44), "MAX", 22, UI.Green, TextAnchor.MiddleCenter, FontStyle.Bold);
                else
                {
                    int idx = i;
                    UI.Button(row, new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(190, 50), $"{cost:N0} ¢", () =>
                    {
                        if (SaveSystem.Spend(cost)) { d.upgrades[idx]++; SaveSystem.Save(); AudioManager.PlayUI(Sfx.Purchase); }
                        else AudioManager.PlayUI(Sfx.PowerDown);
                        ShowUpgrades();
                    }, d.credits >= cost ? new Color(0.15f, 0.35f, 0.2f, 0.95f) : (Color?)null, 22);
                }
            }
            BackButton(s, ShowMainMenu);
        }

        // ------------------------------------------------------------------ settings
        void ShowSettings()
        {
            var s = NewScreen("Settings");
            var st = SaveSystem.Data.settings;
            UI.Header(s, "SETTINGS");
            float y = 250;
            void SliderRow(string label, float v, System.Action<float> set)
            {
                UI.Label(label, s, new Vector2(0.5f, 0.5f), new Vector2(-330, y), new Vector2(300, 40), label, 26, UI.TextColor, TextAnchor.MiddleLeft);
                UI.Slider(s, new Vector2(0.5f, 0.5f), new Vector2(120, y), new Vector2(500, 36), v, set);
                y -= 70;
            }
            SliderRow("Master Volume", st.masterVolume, v => { st.masterVolume = v; AudioManager.Instance.ApplyVolumes(); });
            SliderRow("Music Volume", st.musicVolume, v => { st.musicVolume = v; });
            SliderRow("SFX Volume", st.sfxVolume, v => { st.sfxVolume = v; });
            y -= 10;
            float ty = y;
            UI.Toggle(s, new Vector2(0.5f, 0.5f), new Vector2(-480, ty), "Auto-fire", st.autoFire, v => st.autoFire = v);
            UI.Toggle(s, new Vector2(0.5f, 0.5f), new Vector2(60, ty), "Screen shake", st.screenShake, v => st.screenShake = v); ty -= 60;
            UI.Toggle(s, new Vector2(0.5f, 0.5f), new Vector2(-480, ty), "Touch: drag to move (instead of joystick)", st.dragControls, v => st.dragControls = v);
            UI.Toggle(s, new Vector2(0.5f, 0.5f), new Vector2(60, ty), "Vibration (mobile)", st.vibration, v => st.vibration = v); ty -= 60;
            UI.Toggle(s, new Vector2(0.5f, 0.5f), new Vector2(-480, ty), "Show touch controls on desktop", st.touchControlsOnDesktop, v => st.touchControlsOnDesktop = v);
            UI.Toggle(s, new Vector2(0.5f, 0.5f), new Vector2(60, ty), "Show FPS", st.showFps, v => st.showFps = v); ty -= 60;
            UI.Label("Q", s, new Vector2(0.5f, 0.5f), new Vector2(-330, ty), new Vector2(300, 40), "Graphics quality", 26, UI.TextColor, TextAnchor.MiddleLeft);
            string[] names = { "AUTO", "LOW", "MEDIUM", "HIGH" };
            for (int i = 0; i < 4; i++)
            {
                int q = i - 1;
                UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(-70 + i * 150, ty), new Vector2(140, 44), names[i], () => { st.quality = q; QualityManager.Apply(); SaveSystem.Save(); ShowSettings(); },
                    st.quality == q ? new Color(0.1f, 0.3f, 0.45f, 0.95f) : (Color?)null, 20);
            }
            ty -= 80;
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, ty), new Vector2(360, 50), "RESET ALL PROGRESS", () => ShowConfirm("Erase all progress, ships and upgrades?", () => { SaveSystem.ResetProgress(); ShowSettings(); }, ShowSettings), new Color(0.4f, 0.1f, 0.1f, 0.95f), 20);
            BackButton(s, () => { SaveSystem.Save(); if (GameManager.Instance.State == GameState.Paused) ShowPause(); else ShowMainMenu(); });
        }

        void ShowConfirm(string msg, System.Action yes, System.Action no)
        {
            var s = NewScreen("Confirm");
            var box = UI.Anchored("Box", s, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 260));
            UI.Image(box, UI.PanelLight, true);
            UI.Label("M", box, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(640, 100), msg, 28, UI.TextColor);
            UI.Button(box, new Vector2(0.5f, 0), new Vector2(-150, 30), new Vector2(240, 60), "YES", yes, new Color(0.4f, 0.1f, 0.1f, 0.95f));
            UI.Button(box, new Vector2(0.5f, 0), new Vector2(150, 30), new Vector2(240, 60), "NO", no);
        }

        // ------------------------------------------------------------------ credits / stats
        void ShowCredits()
        {
            var s = NewScreen("Credits");
            var st = SaveSystem.Data.stats;
            UI.Header(s, "CREDITS");
            UI.Label("C", s, new Vector2(0.5f, 0.5f), new Vector2(-350, 40), new Vector2(620, 500),
                "NOVA VANGUARD\n\nDesign, code, art & audio\nJaineil Rana  (JaKing)\n\nBuilt with Unity 6 + Universal Render Pipeline\nAll models, effects, sounds and music are generated procedurally at runtime.\n\ngithub.com/jaineil32624/nova-vanguard\n\nThanks for playing!",
                22, UI.TextColor, TextAnchor.UpperCenter);
            UI.Label("S", s, new Vector2(0.5f, 0.5f), new Vector2(350, 40), new Vector2(620, 500),
                $"PILOT STATISTICS\n\nEnemies destroyed   {st.enemiesDestroyed:N0}\nBosses defeated   {st.bossesDefeated}\nSectors cleared   {st.sectorsCleared}\nHigh score   {st.highScore:N0}\nCredits earned   {st.creditsEarned:N0}\nShots fired   {st.shotsFired:N0}\nMissiles fired   {st.missilesFired:N0}\nUltimates used   {st.ultimatesUsed}\nDeaths   {st.deaths}\nTime played   {System.TimeSpan.FromSeconds(st.playTimeSeconds):hh\\:mm\\:ss}",
                22, UI.TextColor, TextAnchor.UpperCenter);
            BackButton(s, ShowMainMenu);
        }

        // ------------------------------------------------------------------ in-run screens
        public void ShowPause()
        {
            var s = NewScreen("Pause");
            UI.Header(s, "PAUSED", $"Sector {GameManager.Instance.SectorIndex}  •  {GameManager.Instance.CurrentSector.name}");
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(380, 70), "RESUME", () => GameManager.Instance.Resume(), null, 32);
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(380, 60), "SETTINGS", ShowSettings);
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(380, 60), "RESTART SECTOR", () => GameManager.Instance.StartSector(GameManager.Instance.SectorIndex));
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(380, 60), "QUIT TO MENU", () => GameManager.Instance.QuitToMenu(), new Color(0.4f, 0.1f, 0.1f, 0.95f));
        }

        public void ShowSectorClear(int score, int credits, int bonus, bool firstClear, bool finalSector)
        {
            hudCanvas.gameObject.SetActive(false);
            var s = NewScreen("SectorClear");
            var gm = GameManager.Instance;
            UI.Header(s, finalSector ? "THE FINAL WAR IS WON" : "SECTOR CLEARED", finalSector ? "You ended the war. The galaxy is yours, Vanguard." : $"{gm.CurrentSector.boss.name} destroyed");
            UI.Label("R", s, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(800, 220),
                $"SCORE   {score:N0}\nCREDITS EARNED   {credits - bonus:N0} ¢\nBONUS   {bonus:N0} ¢{(firstClear ? "   (first clear +500)" : "")}\n\nBANK   {SaveSystem.Data.credits:N0} ¢", 30, UI.TextColor);
            if (!finalSector) UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(420, 70), "NEXT SECTOR", () => gm.StartSector(gm.SectorIndex + 1), UI.Cyan * new Color(0.5f, 0.5f, 0.5f, 1f), 32);
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(-220, -190), new Vector2(380, 60), "HANGAR & UPGRADES", () => { gm.QuitToMenu(); ShowShips(); });
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(220, -190), new Vector2(380, 60), "MAIN MENU", () => gm.QuitToMenu());
        }

        public void ShowGameOver(int score, int kept, int sector, int wave)
        {
            hudCanvas.gameObject.SetActive(false);
            var s = NewScreen("GameOver");
            var gm = GameManager.Instance;
            UI.Header(s, "SHIP DESTROYED", $"Sector {sector}, wave {wave}");
            UI.Label("R", s, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(800, 200),
                $"SCORE   {score:N0}\nCREDITS SALVAGED   {kept:N0} ¢  (half of what you earned)\n\nBANK   {SaveSystem.Data.credits:N0} ¢\n\nTip: spend credits on upgrades and a better ship, then try again.", 26, UI.TextColor);
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(420, 70), "RETRY SECTOR", () => gm.StartSector(sector), UI.Cyan * new Color(0.5f, 0.5f, 0.5f, 1f), 32);
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(-220, -190), new Vector2(380, 60), "UPGRADES", () => { gm.QuitToMenu(); ShowUpgrades(); });
            UI.Button(s, new Vector2(0.5f, 0.5f), new Vector2(220, -190), new Vector2(380, 60), "MAIN MENU", () => gm.QuitToMenu());
        }

        void BackButton(Transform parent, System.Action back)
        {
            var b = UI.Button(parent, new Vector2(0, 1), new Vector2(30, -30), new Vector2(160, 52), "< BACK", () => { AudioManager.PlayUI(Sfx.UiBack); back(); }, null, 22);
        }
    }
}
