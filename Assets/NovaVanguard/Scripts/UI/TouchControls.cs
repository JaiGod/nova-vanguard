using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NovaVanguard
{
    /// <summary>Floating virtual joystick (or drag-to-move area) plus action buttons for touch devices.</summary>
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Create(Transform parent)
        {
            var root = UI.Fill("TouchControls", parent);
            var tc = root.gameObject.AddComponent<TouchControls>();
            tc.Build(root);
            return tc;
        }

        void Build(RectTransform root)
        {
            // Left half: joystick / drag zone
            var zone = UI.Rect("MoveZone", root);
            zone.anchorMin = new Vector2(0, 0); zone.anchorMax = new Vector2(0.5f, 0.75f);
            zone.offsetMin = Vector2.zero; zone.offsetMax = Vector2.zero;
            var zi = zone.gameObject.AddComponent<Image>(); zi.color = new Color(0, 0, 0, 0.001f);
            zone.gameObject.AddComponent<VirtualJoystick>();

            // Right side: buttons
            TouchButton(root, new Vector2(1, 0), new Vector2(-70, 70), 190, "FIRE", new Color(1f, 0.35f, 0.3f, 0.75f), b => InputRouter.TouchFire = b, null);
            TouchButton(root, new Vector2(1, 0), new Vector2(-280, 60), 130, "MSL", new Color(1f, 0.6f, 0.25f, 0.75f), null, () => InputRouter.TouchSecondary = true);
            TouchButton(root, new Vector2(1, 0), new Vector2(-90, 290), 130, "BOOST", new Color(1f, 0.85f, 0.3f, 0.75f), b => InputRouter.TouchBoost = b, null);
            TouchButton(root, new Vector2(1, 0), new Vector2(-250, 230), 140, "ULT", new Color(0.75f, 0.4f, 1f, 0.8f), null, () => InputRouter.TouchUltimate = true);
            TouchButton(root, new Vector2(1, 0), new Vector2(-400, 150), 90, "WPN", new Color(0.3f, 0.8f, 1f, 0.7f), null, () => InputRouter.TouchCyclePrimary = true);
            TouchButton(root, new Vector2(1, 0), new Vector2(-400, 40), 90, "SEC", new Color(0.3f, 0.8f, 1f, 0.7f), null, () => InputRouter.TouchCycleSecondary = true);
            TouchButton(root, new Vector2(0.5f, 1), new Vector2(0, -110), 70, "II", new Color(1f, 1f, 1f, 0.5f), null, () => InputRouter.TouchPause = true);
        }

        static void TouchButton(Transform parent, Vector2 anchor, Vector2 pos, float size, string label, Color color, System.Action<bool> onHold, System.Action onTap)
        {
            var r = UI.Anchored("Touch " + label, parent, anchor, pos, new Vector2(size, size));
            var img = r.gameObject.AddComponent<Image>(); img.sprite = UI.Circle; img.color = color;
            var ring = UI.Fill("Ring", r, -6f); var ri = ring.gameObject.AddComponent<Image>(); ri.sprite = UI.Circle; ri.color = new Color(1, 1, 1, 0.15f); ri.raycastTarget = false;
            ring.SetAsFirstSibling();
            UI.Label(UI.Fill("L", r), label, Mathf.RoundToInt(size * 0.22f), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var h = r.gameObject.AddComponent<HoldButton>(); h.onHold = onHold; h.onTap = onTap; h.img = img;
        }
    }

    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action<bool> onHold; public System.Action onTap; public Image img;
        Color baseColor; bool down;
        void Start() { baseColor = img.color; }
        public void OnPointerDown(PointerEventData e)
        {
            down = true; img.color = Color.white; transform.localScale = Vector3.one * 0.92f;
            onHold?.Invoke(true); onTap?.Invoke();
        }
        public void OnPointerUp(PointerEventData e) { Release(); }
        public void OnPointerExit(PointerEventData e) { if (down) Release(); }
        void Release() { down = false; img.color = baseColor; transform.localScale = Vector3.one; onHold?.Invoke(false); }
        void OnDisable() { if (down) Release(); }
    }

    /// <summary>Floating joystick: appears where the finger lands. In drag mode the finger delta moves the ship directly.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform baseRing, knob; Vector2 origin, lastPos; bool active; const float Radius = 110f;
        void Awake()
        {
            baseRing = UI.Anchored("Base", transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Radius * 2.2f, Radius * 2.2f));
            var bi = baseRing.gameObject.AddComponent<Image>(); bi.sprite = UI.Circle; bi.color = new Color(1, 1, 1, 0.12f); bi.raycastTarget = false;
            knob = UI.Anchored("Knob", baseRing, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Radius * 0.8f, Radius * 0.8f));
            var ki = knob.gameObject.AddComponent<Image>(); ki.sprite = UI.Circle; ki.color = new Color(0.3f, 0.85f, 1f, 0.6f); ki.raycastTarget = false;
            baseRing.gameObject.SetActive(false);
        }
        bool DragMode => SaveSystem.Data != null && SaveSystem.Data.settings.dragControls;
        public void OnPointerDown(PointerEventData e)
        {
            active = true; InputRouter.TouchStick = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out origin);
            lastPos = e.position;
            if (!DragMode) { baseRing.gameObject.SetActive(true); baseRing.anchoredPosition = origin; knob.anchoredPosition = Vector2.zero; }
        }
        public void OnDrag(PointerEventData e)
        {
            if (!active) return;
            if (DragMode) { InputRouter.TouchDragScreenDelta += e.position - lastPos; lastPos = e.position; return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var p);
            var d = p - origin;
            if (d.magnitude > Radius) { origin = p - d.normalized * Radius; baseRing.anchoredPosition = origin; d = d.normalized * Radius; }  // follow the finger
            knob.anchoredPosition = d;
            InputRouter.TouchStick = d / Radius;
        }
        public void OnPointerUp(PointerEventData e) { active = false; InputRouter.TouchStick = Vector2.zero; baseRing.gameObject.SetActive(false); }
        void OnDisable() { active = false; InputRouter.TouchStick = Vector2.zero; }
    }
}
