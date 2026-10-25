using System.Collections.Generic;
using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Gravitational pull source (black hole hazards and the Black Hole Generator ultimate).</summary>
    public class GravityWell : MonoBehaviour
    {
        public static readonly List<GravityWell> Active = new List<GravityWell>();

        public float strength = 30f;
        public float radius = 9f;
        public bool pullsPlayer = true;
        public bool pullsEnemies = false;

        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        /// <summary>Acceleration at a point (zero outside radius).</summary>
        public Vector3 PullAt(Vector3 p)
        {
            var d = transform.position - p; d.y = 0f;
            float dist = d.magnitude;
            if (dist > radius || dist < 0.05f) return Vector3.zero;
            float t = 1f - dist / radius;
            return d / dist * strength * t * t;
        }

        public static Vector3 TotalPull(Vector3 p, bool isPlayer)
        {
            var sum = Vector3.zero;
            for (int i = 0; i < Active.Count; i++)
            {
                var w = Active[i];
                if (isPlayer ? w.pullsPlayer : w.pullsEnemies) sum += w.PullAt(p);
            }
            return sum;
        }
    }
}
