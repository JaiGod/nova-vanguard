using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Implemented by pooled objects that need to reset state when reused.</summary>
    public interface IPoolable
    {
        string PoolKey { get; set; }
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// Keyed object pool. Everything that spawns frequently (bullets, missiles, enemies, VFX, hazards)
    /// is recycled here instead of Instantiate/Destroy to avoid GC spikes on mobile.
    /// </summary>
    public static class Pool
    {
        static readonly Dictionary<string, Stack<GameObject>> free = new Dictionary<string, Stack<GameObject>>();
        static readonly Dictionary<string, Func<GameObject>> factories = new Dictionary<string, Func<GameObject>>();
        static readonly List<GameObject> active = new List<GameObject>();
        static Transform root;

        static Transform Root
        {
            get
            {
                if (root == null)
                {
                    var go = new GameObject("[Pool]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    root = go.transform;
                }
                return root;
            }
        }

        public static void Register(string key, Func<GameObject> factory)
        {
            factories[key] = factory;
            if (!free.ContainsKey(key)) free[key] = new Stack<GameObject>();
        }

        public static bool IsRegistered(string key) => factories.ContainsKey(key);

        public static void Prewarm(string key, int count)
        {
            var list = new List<GameObject>();
            for (int i = 0; i < count; i++) list.Add(Spawn(key, new Vector3(0, -500, 0), Quaternion.identity));
            foreach (var g in list) Despawn(g);
        }

        public static GameObject Spawn(string key, Vector3 pos, Quaternion rot)
        {
            if (!factories.ContainsKey(key)) throw new ArgumentException("Pool key not registered: " + key);
            var stack = free[key];
            GameObject go = null;
            while (stack.Count > 0 && go == null) go = stack.Pop();
            if (go == null)
            {
                go = factories[key]();
                go.transform.SetParent(Root, false);
                foreach (var p in go.GetComponents<IPoolable>()) p.PoolKey = key;
            }
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            active.Add(go);
            foreach (var p in go.GetComponents<IPoolable>()) p.OnSpawned();
            return go;
        }

        public static T Spawn<T>(string key, Vector3 pos, Quaternion rot) where T : Component
            => Spawn(key, pos, rot).GetComponent<T>();

        public static void Despawn(GameObject go)
        {
            if (go == null || !go.activeSelf) return;
            var p = go.GetComponent<IPoolable>();
            string key = p != null ? p.PoolKey : null;
            foreach (var pp in go.GetComponents<IPoolable>()) pp.OnDespawned();
            go.SetActive(false);
            active.Remove(go);
            if (key != null && free.TryGetValue(key, out var stack)) stack.Push(go);
            else UnityEngine.Object.Destroy(go);
        }

        /// <summary>Returns every active pooled object (used when a run ends).</summary>
        public static void DespawnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (i >= active.Count) continue;
                var g = active[i];
                if (g == null) { active.RemoveAt(i); continue; }
                Despawn(g);
            }
            active.Clear();
        }
    }
}
