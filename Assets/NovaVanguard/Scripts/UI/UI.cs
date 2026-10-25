using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NovaVanguard
{
    /// <summary>Tiny fluent helpers for building sci-fi uGUI in code. Keeps the screen classes short.</summary>
    public static class UI
    {
        public static readonly Color Cyan = new Color(0.25f, 0.85f, 1f);
        public static readonly Color Panel = new Color(0.02f, 0.05f, 0.1f, 0.82f);
        public static readonly Color PanelLight = new Color(0.08f, 0.14f, 0.24f, 0.9f);
        public static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        public static readonly Color Dim = new Color(0.55f, 0.65f, 0.75f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Red = new Color(1f, 0.3f, 0.25f);
        public static readonly Color Green = new Color(0.35f, 1f, 0.5f);

        static Font font;
        public static Font Font
        {
            get
            {
                if (font != null) return font;
                font = Resources.Load<Font>("Fonts/NovaFont");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Menlo", "Roboto Mono", "DejaVu Sans Mono", "Courier New", "Arial" }, 16);
                return font;
            }
        }

        public static Sprite roundedSprite;
        public static Sprite Rounded
        {
            get
            {
                if (roundedSprite != null) return roundedSprite;
                const int s = 32, r = 10;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, r, s - r), cy = Mathf.Clamp(y + 0.5f, r, s - r);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        float a = Mathf.Clamp01(r - d + 0.5f);
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px); tex.Apply();
                roundedSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return roundedSprite;
            }
        }

        public static Sprite circleSprite;
        public static Sprite Circle
        {
            get
            {
                if (circleSprite != null) return circleSprite;
                const int s = 64;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f));
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(s / 2f - d + 0.5f) * 255));
                    }
                tex.SetPixels32(px); tex.Apply();
                circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                return circleSprite;
            }
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Screen.width >= Screen.height ? 0.6f : 0.25f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if NOVA_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Object.DontDestroyOnLoad(es);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Full-stretch child.</summary>
        public static RectTransform Fill(string name, Transform parent, float pad = 0f)
        {
            var r = Rect(name, parent);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(pad, pad); r.offsetMax = new Vector2(-pad, -pad);
            return r;
        }

        public static RectTransform Anchored(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var r = Rect(name, parent);
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        public static Image Image(RectTransform r, Color c, bool rounded = false)
        {
            var i = r.gameObject.AddComponent<Image>();
            i.color = c;
            if (rounded) { i.sprite = Rounded; i.type = UnityEngine.UI.Image.Type.Sliced; i.pixelsPerUnitMultiplier = 1f; }
            return i;
        }

        public static Text Label(RectTransform r, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var t = r.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.7f); sh.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        public static Text Label(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
            => Label(Anchored(name, parent, anchor, pos, size), text, fontSize, color, align, style);

        public static Button Button(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label, System.Action onClick, Color? bg = null, int fontSize = 30, Color? fg = null)
        {
            var r = Anchored("Btn " + label, parent, anchor, pos, size);
            var img = Image(r, bg ?? PanelLight, true);
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f); colors.pressedColor = new Color(0.7f, 0.9f, 1f); colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            var border = Fill("Border", r, 0f);
            var bi = Image(border, new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f), true); bi.raycastTarget = false;
            var inner = Fill("Inner", r, 2f);
            var ii = Image(inner, bg ?? PanelLight, true); ii.raycastTarget = false;
            var t = Label(Fill("Label", r, 6f), label, fontSize, fg ?? TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = fontSize;
            b.onClick.AddListener(() => { AudioManager.PlayUI(Sfx.UiClick); onClick?.Invoke(); });
            r.gameObject.AddComponent<UiHoverBlocker>();
            return b;
        }

        public static Slider Slider(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float value, System.Action<float> onChange)
        {
            var r = Anchored("Slider", parent, anchor, pos, size);
            var s = r.gameObject.AddComponent<Slider>();
            var bg = Fill("BG", r, 0f); bg.offsetMin = new Vector2(0, size.y * 0.35f); bg.offsetMax = new Vector2(0, -size.y * 0.35f);
            Image(bg, new Color(0.1f, 0.15f, 0.25f, 0.9f), true);
            var fillArea = Fill("FillArea", r, 0f); fillArea.offsetMin = new Vector2(0, size.y * 0.35f); fillArea.offsetMax = new Vector2(0, -size.y * 0.35f);
            var fill = Fill("Fill", fillArea, 0f); Image(fill, Cyan, true);
            var handleArea = Fill("HandleArea", r, 0f);
            var handle = Rect("Handle", handleArea); handle.sizeDelta = new Vector2(size.y, size.y);
            var hi = Image(handle, Color.white); hi.sprite = Circle;
            s.fillRect = fill; s.handleRect = handle; s.targetGraphic = hi;
            s.minValue = 0f; s.maxValue = 1f; s.value = value;
            s.onValueChanged.AddListener(v => onChange?.Invoke(v));
            r.gameObject.AddComponent<UiHoverBlocker>();
            return s;
        }

        public static Toggle Toggle(Transform parent, Vector2 anchor, Vector2 pos, string label, bool value, System.Action<bool> onChange)
        {
            var r = Anchored("Toggle " + label, parent, anchor, pos, new Vector2(520, 50));
            var t = r.gameObject.AddComponent<Toggle>();
            var box = Anchored("Box", r, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(40, 40));
            var bi = Image(box, new Color(0.1f, 0.15f, 0.25f, 0.9f), true);
            var check = Fill("Check", box, 8f); var ci = Image(check, Cyan, true);
            t.targetGraphic = bi; t.graphic = ci; t.isOn = value;
            Label("Label", r, new Vector2(0, 0.5f), new Vector2(56, 0), new Vector2(460, 50), label, 26, TextColor, TextAnchor.MiddleLeft);
            t.onValueChanged.AddListener(v => { AudioManager.PlayUI(Sfx.UiClick); onChange?.Invoke(v); });
            r.gameObject.AddComponent<UiHoverBlocker>();
            return t;
        }

        /// <summary>Horizontal bar with a fill. Returns the fill image; set fillAmount.</summary>
        public static Image Bar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, out Text label)
        {
            var r = Anchored("Bar", parent, anchor, pos, size);
            Image(r, new Color(0f, 0f, 0f, 0.55f), true);
            var fillR = Fill("Fill", r, 3f);
            var fill = Image(fillR, color, true);
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillAmount = 1f;
            label = Label(Fill("Txt", r, 0f), "", Mathf.RoundToInt(size.y * 0.6f), TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return fill;
        }

        public static Text Header(Transform parent, string title, string subtitle = null)
        {
            var t = Label("Title", parent, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1400, 90), title, 64, Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (!string.IsNullOrEmpty(subtitle)) Label("Sub", parent, new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(1400, 40), subtitle, 26, Dim);
            return t;
        }
    }

    /// <summary>Tells the input router when the pointer is over UI so clicks don't fire the guns.</summary>
    public class UiHoverBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        static int count;
        bool over;
        public void OnPointerEnter(PointerEventData e) { if (!over) { over = true; count++; } InputRouter.UiBlocking = count > 0; }
        public void OnPointerExit(PointerEventData e) { if (over) { over = false; count--; } InputRouter.UiBlocking = count > 0; }
        public void OnPointerDown(PointerEventData e) { InputRouter.UiBlocking = true; }
        public void OnPointerUp(PointerEventData e) { InputRouter.UiBlocking = count > 0; }
        void OnDisable() { if (over) { over = false; count = Mathf.Max(0, count - 1); } InputRouter.UiBlocking = count > 0; }
    }
}
