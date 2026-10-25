using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Angled top-down camera with trauma-based screen shake and a slight follow on the player.</summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }
        public Camera Cam { get; private set; }

        public static readonly Vector3 BasePos = new Vector3(0f, 34f, -17f);
        public static readonly Vector3 LookAt = new Vector3(0f, 0f, 2.2f);

        float trauma;
        Vector3 followOffset;
        int lastW, lastH;

        public static CameraRig Create()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var rig = go.AddComponent<CameraRig>();
            var cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 1f; cam.farClipPlane = 300f;
            cam.allowHDR = true;
            go.transform.position = BasePos;
            go.transform.LookAt(LookAt);
            rig.Cam = cam;
            Instance = rig;
            PostFx.Setup(cam);
            GameBounds.Recalculate(cam);
            return rig;
        }

        public static void Shake(float amount)
        {
            if (Instance == null || SaveSystem.Data == null || !SaveSystem.Data.settings.screenShake) return;
            Instance.trauma = Mathf.Clamp01(Instance.trauma + amount);
        }

        void LateUpdate()
        {
            if (Screen.width != lastW || Screen.height != lastH)
            {
                lastW = Screen.width; lastH = Screen.height;
                transform.position = BasePos; transform.LookAt(LookAt);
                GameBounds.Recalculate(Cam);
            }
            var player = PlayerShip.Instance;
            Vector3 targetOffset = Vector3.zero;
            if (player != null && player.IsAlive) targetOffset = new Vector3(player.transform.position.x * 0.06f, 0f, 0f);
            followOffset = Vector3.Lerp(followOffset, targetOffset, Time.unscaledDeltaTime * 3f);

            trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.4f);
            float s = trauma * trauma;
            float t = Time.unscaledTime * 22f;
            var shake = new Vector3(Mathf.PerlinNoise(t, 0.1f) - 0.5f, 0f, Mathf.PerlinNoise(0.7f, t) - 0.5f) * s * 2.2f;
            transform.position = BasePos + followOffset + shake;
            transform.rotation = Quaternion.LookRotation(LookAt - BasePos) * Quaternion.Euler(0, 0, (Mathf.PerlinNoise(t, 3.3f) - 0.5f) * s * 6f);
        }
    }
}
