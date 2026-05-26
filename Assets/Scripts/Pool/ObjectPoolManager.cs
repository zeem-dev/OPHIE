// ============================================================
//  OPHIO — ObjectPoolManager (Fixed)
//  Day 2 | Runtime Systems
//  Fix: DespawnDelayed now null-checks before SetActive.
//  Fix: Spawn drains destroyed references from queue.
//  Fix: ReturnToPool guards against null before SetActive.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace OPHIO.Core
{
    [System.Serializable]
    public class PoolConfig
    {
        public string     poolKey;
        public GameObject prefab;
        [Tooltip("How many objects to pre-warm at scene start")]
        public int        prewarmCount = 10;
    }

    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        [Header("Pre-configured Pools")]
        public List<PoolConfig> pools = new List<PoolConfig>();

        private Dictionary<string, Queue<GameObject>> _pools
            = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject>        _prefabs
            = new Dictionary<string, GameObject>();
        private Dictionary<string, Transform>         _containers
            = new Dictionary<string, Transform>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            foreach (var cfg in pools)
                InitPool(cfg.poolKey, cfg.prefab, cfg.prewarmCount);
        }

        // --------------------------------------------------
        //  Public API
        // --------------------------------------------------
        public void RegisterPool(string key, GameObject prefab, int prewarm = 10)
        {
            if (_pools.ContainsKey(key)) return;
            InitPool(key, prefab, prewarm);
        }

        public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogWarning($"[OPHIO Pool] Key '{key}' not found.");
                return null;
            }

            Queue<GameObject> queue = _pools[key];
            GameObject obj = null;

            // Drain any externally-destroyed references from the queue
            while (queue.Count > 0)
            {
                obj = queue.Dequeue();
                if (obj != null) break;
                obj = null;
            }

            if (obj == null) obj = CreateNew(key);

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public void Despawn(string key, GameObject obj, float delay = 0f)
        {
            if (obj == null) return;  // already destroyed — skip silently

            if (delay > 0f)
            {
                StartCoroutine(DespawnDelayed(key, obj, delay));
                return;
            }
            ReturnToPool(key, obj);
        }

        public void DespawnAuto(GameObject obj, float delay = 0f)
        {
            if (obj == null) return;
            string key = obj.name.Replace("(Clone)", "").Trim();
            Despawn(key, obj, delay);
        }

        // --------------------------------------------------
        //  Internal helpers
        // --------------------------------------------------
        private void ReturnToPool(string key, GameObject obj)
        {
            if (obj == null) return;   // guard against race conditions
            obj.SetActive(false);
            if (!_pools.ContainsKey(key)) _pools[key] = new Queue<GameObject>();
            _pools[key].Enqueue(obj);
        }

        private void InitPool(string key, GameObject prefab, int prewarm)
        {
            if (prefab == null) { Debug.LogWarning($"[OPHIO Pool] Null prefab for '{key}'"); return; }
            _prefabs[key] = prefab;
            _pools[key]   = new Queue<GameObject>();
            var container = new GameObject($"Pool_{key}");
            container.transform.SetParent(transform);
            _containers[key] = container.transform;
            for (int i = 0; i < prewarm; i++)
            {
                var obj = CreateNew(key);
                obj.SetActive(false);
                _pools[key].Enqueue(obj);
            }
        }

        private GameObject CreateNew(string key)
        {
            var obj  = Instantiate(_prefabs[key], _containers[key]);
            obj.name = key;
            return obj;
        }

        private System.Collections.IEnumerator DespawnDelayed(string key,
                                                               GameObject obj,
                                                               float delay)
        {
            yield return new WaitForSeconds(delay);
            // Object may have been destroyed externally during the wait
            if (obj == null) yield break;
            ReturnToPool(key, obj);
        }
    }
}
