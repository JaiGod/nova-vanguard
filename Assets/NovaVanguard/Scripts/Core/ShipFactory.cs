using UnityEngine;

namespace NovaVanguard
{
    /// <summary>
    /// Builds every ship, enemy and boss model out of primitives so the project runs with zero imported art.
    /// To use real models later, replace these methods with prefab instantiation — gameplay code never
    /// depends on the visual hierarchy.
    /// </summary>
    public static class ShipFactory
    {
        public static Transform Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.receiveShadows = false;
            return go.transform;
        }

        static Transform Root(Transform parent, string name)
        {
            var v = new GameObject(name).transform;
            v.SetParent(parent, false);
            return v;
        }

        /// <summary>Player ship model. Returns the visual root; engine positions are written to enginePoints.</summary>
        public static Transform BuildPlayer(Transform parent, ShipDef def, out Vector3[] enginePoints)
        {
            var v = Root(parent, "Visual");
            var hull = MaterialLibrary.Lit(def.hull, 0f, 0.7f, 0.6f);
            var dark = MaterialLibrary.Lit(def.hull * 0.45f, 0f, 0.5f, 0.4f);
            var glow = MaterialLibrary.Glow(def.accent, 3f);
            var cockpit = MaterialLibrary.Lit(new Color(0.1f, 0.6f, 1f), 1.2f, 0.95f, 0.1f);
            var P = PrimitiveType.Cube; var S = PrimitiveType.Sphere; var C = PrimitiveType.Capsule; var Y = PrimitiveType.Cylinder;

            switch (def.shape)
            {
                case ShipShape.Dart:
                    Part(v, C, new Vector3(0, 0, 0.1f), new Vector3(90, 0, 0), new Vector3(0.55f, 1.25f, 0.4f), hull);
                    Part(v, P, new Vector3(-0.9f, 0, -0.5f), new Vector3(0, -35, 0), new Vector3(1.6f, 0.08f, 0.5f), dark);
                    Part(v, P, new Vector3(0.9f, 0, -0.5f), new Vector3(0, 35, 0), new Vector3(1.6f, 0.08f, 0.5f), dark);
                    Part(v, S, new Vector3(0, 0.2f, 0.45f), Vector3.zero, new Vector3(0.3f, 0.2f, 0.55f), cockpit);
                    Part(v, P, new Vector3(-1.55f, 0, -0.95f), Vector3.zero, new Vector3(0.08f, 0.1f, 0.6f), glow);
                    Part(v, P, new Vector3(1.55f, 0, -0.95f), Vector3.zero, new Vector3(0.08f, 0.1f, 0.6f), glow);
                    enginePoints = new[] { new Vector3(0, 0, -1.25f) };
                    break;
                case ShipShape.Brick:
                    Part(v, P, Vector3.zero, Vector3.zero, new Vector3(1.4f, 0.55f, 2.0f), hull);
                    Part(v, P, new Vector3(-1.05f, 0, -0.2f), Vector3.zero, new Vector3(0.7f, 0.45f, 1.6f), dark);
                    Part(v, P, new Vector3(1.05f, 0, -0.2f), Vector3.zero, new Vector3(0.7f, 0.45f, 1.6f), dark);
                    Part(v, P, new Vector3(0, 0.35f, 0.5f), Vector3.zero, new Vector3(0.6f, 0.25f, 0.6f), cockpit);
                    Part(v, Y, new Vector3(-1.05f, 0, 0.75f), new Vector3(90, 0, 0), new Vector3(0.22f, 0.4f, 0.22f), dark);
                    Part(v, Y, new Vector3(1.05f, 0, 0.75f), new Vector3(90, 0, 0), new Vector3(0.22f, 0.4f, 0.22f), dark);
                    Part(v, P, new Vector3(0, 0.29f, -0.4f), Vector3.zero, new Vector3(1.2f, 0.05f, 0.12f), glow);
                    enginePoints = new[] { new Vector3(-1.05f, 0, -1.05f), new Vector3(1.05f, 0, -1.05f), new Vector3(0, 0, -1.05f) };
                    break;
                case ShipShape.Needle:
                    Part(v, C, new Vector3(0, 0, 0.3f), new Vector3(90, 0, 0), new Vector3(0.38f, 1.6f, 0.38f), hull);
                    Part(v, Y, new Vector3(0, 0, 1.9f), new Vector3(90, 0, 0), new Vector3(0.08f, 0.5f, 0.08f), glow);
                    Part(v, P, new Vector3(0, 0, -0.6f), Vector3.zero, new Vector3(2.4f, 0.06f, 0.35f), dark);
                    Part(v, P, new Vector3(-1.2f, 0, -0.6f), Vector3.zero, new Vector3(0.1f, 0.25f, 0.5f), glow);
                    Part(v, P, new Vector3(1.2f, 0, -0.6f), Vector3.zero, new Vector3(0.1f, 0.25f, 0.5f), glow);
                    Part(v, S, new Vector3(0, 0.18f, 0.2f), Vector3.zero, new Vector3(0.25f, 0.18f, 0.6f), cockpit);
                    enginePoints = new[] { new Vector3(0, 0, -1.3f) };
                    break;
                case ShipShape.Crescent:
                    Part(v, S, Vector3.zero, Vector3.zero, new Vector3(1.0f, 0.45f, 1.3f), hull);
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Part(v, C, new Vector3(i * 0.95f, 0, 0.35f), new Vector3(90, i * -25, 0), new Vector3(0.3f, 0.85f, 0.3f), dark);
                        Part(v, S, new Vector3(i * 1.25f, 0, 1.05f), Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), glow);
                    }
                    Part(v, S, new Vector3(0, 0.25f, 0.1f), Vector3.zero, new Vector3(0.45f, 0.25f, 0.45f), MaterialLibrary.Lit(def.accent, 1.5f));
                    enginePoints = new[] { new Vector3(0, 0, -0.75f) };
                    break;
                case ShipShape.Crown:
                    Part(v, C, new Vector3(0, 0, 0.2f), new Vector3(90, 0, 0), new Vector3(0.6f, 1.3f, 0.45f), hull);
                    Part(v, P, new Vector3(-0.85f, 0, -0.3f), new Vector3(0, -20, 0), new Vector3(1.3f, 0.1f, 0.9f), hull);
                    Part(v, P, new Vector3(0.85f, 0, -0.3f), new Vector3(0, 20, 0), new Vector3(1.3f, 0.1f, 0.9f), hull);
                    Part(v, C, new Vector3(-1.5f, 0, 0.1f), new Vector3(90, 0, 0), new Vector3(0.25f, 0.75f, 0.25f), dark);
                    Part(v, C, new Vector3(1.5f, 0, 0.1f), new Vector3(90, 0, 0), new Vector3(0.25f, 0.75f, 0.25f), dark);
                    Part(v, P, new Vector3(-0.85f, 0.07f, -0.3f), new Vector3(0, -20, 0), new Vector3(1.0f, 0.04f, 0.12f), glow);
                    Part(v, P, new Vector3(0.85f, 0.07f, -0.3f), new Vector3(0, 20, 0), new Vector3(1.0f, 0.04f, 0.12f), glow);
                    Part(v, S, new Vector3(0, 0.25f, 0.5f), Vector3.zero, new Vector3(0.35f, 0.22f, 0.6f), cockpit);
                    Part(v, Y, new Vector3(0, 0.3f, -0.5f), Vector3.zero, new Vector3(0.5f, 0.04f, 0.5f), glow);
                    enginePoints = new[] { new Vector3(-0.45f, 0, -1.1f), new Vector3(0.45f, 0, -1.1f), new Vector3(-1.5f, 0, -0.75f), new Vector3(1.5f, 0, -0.75f) };
                    break;
                default: // Arrow
                    Part(v, C, new Vector3(0, 0, 0.1f), new Vector3(90, 0, 0), new Vector3(0.6f, 1.15f, 0.45f), hull);
                    Part(v, P, new Vector3(-0.75f, 0, -0.35f), new Vector3(0, -15, 0), new Vector3(1.2f, 0.1f, 0.75f), dark);
                    Part(v, P, new Vector3(0.75f, 0, -0.35f), new Vector3(0, 15, 0), new Vector3(1.2f, 0.1f, 0.75f), dark);
                    Part(v, P, new Vector3(-1.32f, 0.05f, -0.2f), Vector3.zero, new Vector3(0.1f, 0.18f, 0.8f), hull);
                    Part(v, P, new Vector3(1.32f, 0.05f, -0.2f), Vector3.zero, new Vector3(0.1f, 0.18f, 0.8f), hull);
                    Part(v, S, new Vector3(0, 0.22f, 0.4f), Vector3.zero, new Vector3(0.32f, 0.22f, 0.6f), cockpit);
                    Part(v, P, new Vector3(-0.75f, 0.06f, -0.6f), new Vector3(0, -15, 0), new Vector3(0.9f, 0.04f, 0.08f), glow);
                    Part(v, P, new Vector3(0.75f, 0.06f, -0.6f), new Vector3(0, 15, 0), new Vector3(0.9f, 0.04f, 0.08f), glow);
                    enginePoints = new[] { new Vector3(-0.25f, 0, -1.05f), new Vector3(0.25f, 0, -1.05f) };
                    break;
            }
            foreach (var e in enginePoints) Part(v, S, e + new Vector3(0, 0, 0.12f), Vector3.zero, new Vector3(0.28f, 0.28f, 0.2f), glow);
            return v;
        }

        /// <summary>Enemy models: each type has a distinct silhouette so players can read threats instantly.</summary>
        public static Transform BuildEnemy(Transform parent, EnemyDef def)
        {
            var v = Root(parent, "Visual");
            var hull = MaterialLibrary.Lit(def.color * 0.75f, 0f, 0.6f, 0.5f);
            var dark = MaterialLibrary.Lit(new Color(0.18f, 0.18f, 0.22f), 0f, 0.5f, 0.6f);
            var glow = MaterialLibrary.Glow(def.color, 3f);
            var P = PrimitiveType.Cube; var S = PrimitiveType.Sphere; var C = PrimitiveType.Capsule; var Y = PrimitiveType.Cylinder;
            // Enemies face -Z (down the screen).
            switch (def.kind)
            {
                case EnemyKind.Scout:
                    Part(v, S, Vector3.zero, Vector3.zero, new Vector3(0.7f, 0.35f, 0.9f), hull);
                    Part(v, P, new Vector3(0, 0, 0.2f), new Vector3(0, 45, 0), new Vector3(0.9f, 0.06f, 0.9f), dark);
                    Part(v, S, new Vector3(0, 0.15f, -0.3f), Vector3.zero, new Vector3(0.3f, 0.2f, 0.3f), glow);
                    break;
                case EnemyKind.Fighter:
                    Part(v, C, Vector3.zero, new Vector3(90, 0, 0), new Vector3(0.55f, 0.9f, 0.4f), hull);
                    Part(v, P, new Vector3(-0.75f, 0, 0.2f), new Vector3(0, 25, 0), new Vector3(1.0f, 0.08f, 0.6f), dark);
                    Part(v, P, new Vector3(0.75f, 0, 0.2f), new Vector3(0, -25, 0), new Vector3(1.0f, 0.08f, 0.6f), dark);
                    Part(v, P, new Vector3(-1.2f, 0, -0.1f), Vector3.zero, new Vector3(0.12f, 0.2f, 0.7f), glow);
                    Part(v, P, new Vector3(1.2f, 0, -0.1f), Vector3.zero, new Vector3(0.12f, 0.2f, 0.7f), glow);
                    Part(v, S, new Vector3(0, 0.2f, -0.35f), Vector3.zero, new Vector3(0.28f, 0.2f, 0.4f), glow);
                    break;
                case EnemyKind.Interceptor:
                    Part(v, C, Vector3.zero, new Vector3(90, 0, 0), new Vector3(0.35f, 1.0f, 0.3f), hull);
                    Part(v, P, new Vector3(-0.5f, 0, 0.45f), new Vector3(0, -40, 0), new Vector3(0.9f, 0.06f, 0.35f), dark);
                    Part(v, P, new Vector3(0.5f, 0, 0.45f), new Vector3(0, 40, 0), new Vector3(0.9f, 0.06f, 0.35f), dark);
                    Part(v, P, new Vector3(0, 0, -0.95f), new Vector3(0, 45, 0), new Vector3(0.25f, 0.12f, 0.25f), glow);
                    break;
                case EnemyKind.Bomber:
                    Part(v, S, Vector3.zero, Vector3.zero, new Vector3(1.6f, 0.6f, 1.3f), hull);
                    Part(v, Y, new Vector3(0, 0.2f, 0), Vector3.zero, new Vector3(1.9f, 0.08f, 1.9f), dark);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 90f + 45f;
                        var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                        Part(v, S, dir * 0.95f + Vector3.up * 0.25f, Vector3.zero, Vector3.one * 0.22f, glow);
                    }
                    Part(v, S, new Vector3(0, 0.35f, 0), Vector3.zero, new Vector3(0.6f, 0.3f, 0.6f), glow);
                    break;
                case EnemyKind.Elite:
                    Part(v, P, Vector3.zero, new Vector3(0, 45, 0), new Vector3(1.0f, 0.5f, 1.0f), hull);
                    Part(v, Y, Vector3.zero, Vector3.zero, new Vector3(2.0f, 0.06f, 2.0f), dark);
                    Part(v, S, new Vector3(0, 0.35f, 0), Vector3.zero, new Vector3(0.55f, 0.4f, 0.55f), glow);
                    for (int i = 0; i < 3; i++)
                    {
                        var dir = Quaternion.Euler(0, i * 120f + 60f, 0) * Vector3.forward;
                        Part(v, C, dir * 1.0f, new Vector3(90, i * 120f + 60f, 0), new Vector3(0.25f, 0.45f, 0.25f), glow);
                    }
                    break;
            }
            v.localScale = Vector3.one * def.scale;
            return v;
        }

        /// <summary>Boss hull. Generator mount points (weak points) are returned for the boss to attach to.</summary>
        public static Transform BuildBoss(Transform parent, BossDef def, out Vector3[] generatorPoints, out Transform core)
        {
            var v = Root(parent, "Visual");
            var hull = MaterialLibrary.Lit(def.color, 0f, 0.65f, 0.7f);
            var dark = MaterialLibrary.Lit(def.color * 0.4f, 0f, 0.5f, 0.6f);
            var glow = MaterialLibrary.Glow(def.accent, 3f);
            var P = PrimitiveType.Cube; var S = PrimitiveType.Sphere; var C = PrimitiveType.Capsule; var Y = PrimitiveType.Cylinder;

            Part(v, S, Vector3.zero, Vector3.zero, new Vector3(2.4f, 0.8f, 1.6f), hull);
            Part(v, P, new Vector3(0, -0.05f, 0.3f), Vector3.zero, new Vector3(4.2f, 0.25f, 0.9f), dark);
            Part(v, C, new Vector3(-2.2f, 0, 0.1f), new Vector3(90, 0, 0), new Vector3(0.55f, 1.0f, 0.5f), hull);
            Part(v, C, new Vector3(2.2f, 0, 0.1f), new Vector3(90, 0, 0), new Vector3(0.55f, 1.0f, 0.5f), hull);
            Part(v, Y, new Vector3(-2.2f, 0, -1.0f), new Vector3(90, 0, 0), new Vector3(0.35f, 0.25f, 0.35f), glow);
            Part(v, Y, new Vector3(2.2f, 0, -1.0f), new Vector3(90, 0, 0), new Vector3(0.35f, 0.25f, 0.35f), glow);
            Part(v, P, new Vector3(0, 0.3f, -0.4f), new Vector3(0, 45, 0), new Vector3(0.9f, 0.3f, 0.9f), dark);
            Part(v, P, new Vector3(-1.1f, 0.35f, 0.2f), Vector3.zero, new Vector3(0.12f, 0.06f, 1.2f), glow);
            Part(v, P, new Vector3(1.1f, 0.35f, 0.2f), Vector3.zero, new Vector3(0.12f, 0.06f, 1.2f), glow);
            // Prongs pointing at the player
            Part(v, C, new Vector3(-0.8f, 0, -1.3f), new Vector3(90, 0, 0), new Vector3(0.3f, 0.6f, 0.3f), dark);
            Part(v, C, new Vector3(0.8f, 0, -1.3f), new Vector3(90, 0, 0), new Vector3(0.3f, 0.6f, 0.3f), dark);

            core = Part(v, S, new Vector3(0, 0.45f, -0.35f), Vector3.zero, new Vector3(0.75f, 0.55f, 0.75f), MaterialLibrary.Lit(def.accent, 2.5f));

            int g = Mathf.Max(def.generators, 0);
            generatorPoints = new Vector3[g];
            for (int i = 0; i < g; i++)
            {
                float t = g == 1 ? 0.5f : i / (float)(g - 1);
                // Mounted on the leading edge (toward the player) so they sit outside the hull collider.
                generatorPoints[i] = new Vector3(Mathf.Lerp(-2.6f, 2.6f, t), 0.2f, (i % 2 == 0) ? -1.45f : -1.7f) * def.scale;
            }
            v.localScale = Vector3.one * def.scale;
            return v;
        }
    }
}
