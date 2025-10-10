using System.Collections.Generic;
using UnityEngine;

namespace Begin.Core {
    public interface IPooled { void OnSpawned(); void OnDespawned(); }

    public static class Pool {
        static readonly Dictionary<string, Queue<GameObject>> pools = new();
        static readonly Dictionary<GameObject, string> reverse = new();
        static Transform root;

        static void EnsureRoot() {
            if (root) return;
            var go = new GameObject("~Pool");
            Object.DontDestroyOnLoad(go);
            root = go.transform;
        }

        public static void Prewarm(string key, GameObject prefab, int count) {
            EnsureRoot();
            if (!pools.ContainsKey(key)) pools[key] = new Queue<GameObject>();
            for (int i=0;i<count;i++) {
                var go = Object.Instantiate(prefab, root);
                go.SetActive(false);
                pools[key].Enqueue(go);
                reverse[go] = key;
            }
        }

        public static GameObject Spawn(string key, Vector3 pos, Quaternion rot) {
            EnsureRoot();
            if (!pools.TryGetValue(key, out var q) || q.Count == 0) return null;
            var go = q.Dequeue();
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.SetParent(null, true);
            go.SetActive(true);
            if (go.TryGetComponent<IPooled>(out var p)) p.OnSpawned();
            return go;
        }

        public static void Despawn(GameObject go) {
            if (!go) return;
            if (!reverse.TryGetValue(go, out var key)) { Object.Destroy(go); return; }
            if (go.TryGetComponent<IPooled>(out var p)) p.OnDespawned();
            go.SetActive(false);
            go.transform.SetParent(root, false);
            pools[key].Enqueue(go);
        }
    }
}
