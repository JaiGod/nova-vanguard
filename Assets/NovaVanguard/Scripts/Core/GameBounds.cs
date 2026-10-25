using UnityEngine;

namespace NovaVanguard
{
    /// <summary>
    /// The playfield is the XZ plane (y = 0). Bounds are computed from what the camera can see so the
    /// game adapts to any aspect ratio (ultrawide monitors, tall phones, tablets).
    /// </summary>
    public static class GameBounds
    {
        /// <summary>Built-in layer 4 is reused for bullets so bullets never collide with each other.</summary>
        public const int BulletLayer = 4;

        public static float MinX = -14f, MaxX = 14f, MinZ = -10f, MaxZ = 12f;
        public static float Width => MaxX - MinX;
        public static float Height => MaxZ - MinZ;
        public static Vector3 Center => new Vector3((MinX + MaxX) * 0.5f, 0f, (MinZ + MaxZ) * 0.5f);
        public static Vector3 TopCenter => new Vector3((MinX + MaxX) * 0.5f, 0f, MaxZ);

        public static void Recalculate(Camera cam)
        {
            if (cam == null) return;
            var plane = new Plane(Vector3.up, Vector3.zero);
            Vector3 Hit(float vx, float vy)
            {
                var ray = cam.ViewportPointToRay(new Vector3(vx, vy, 0f));
                return plane.Raycast(ray, out float d) ? ray.GetPoint(d) : Vector3.zero;
            }
            // Use the narrowest (bottom, nearest-camera) width so the ship never leaves the screen.
            var bl = Hit(0.04f, 0.06f); var br = Hit(0.96f, 0.06f);
            var tl = Hit(0.04f, 0.94f);
            MinX = bl.x; MaxX = br.x; MinZ = bl.z; MaxZ = tl.z;
        }

        public static bool Inside(Vector3 p, float margin = 0f)
            => p.x > MinX - margin && p.x < MaxX + margin && p.z > MinZ - margin && p.z < MaxZ + margin;

        public static Vector3 Clamp(Vector3 p, float margin = 0f)
        {
            p.x = Mathf.Clamp(p.x, MinX + margin, MaxX - margin);
            p.z = Mathf.Clamp(p.z, MinZ + margin, MaxZ - margin);
            p.y = 0f; return p;
        }

        public static Vector3 RandomTop(float above = 2f)
            => new Vector3(Random.Range(MinX + 1.5f, MaxX - 1.5f), 0f, MaxZ + above);

        public static Vector3 RandomInUpperHalf()
            => new Vector3(Random.Range(MinX + 2f, MaxX - 2f), 0f, Random.Range(Center.z + 1f, MaxZ - 2f));
    }
}
