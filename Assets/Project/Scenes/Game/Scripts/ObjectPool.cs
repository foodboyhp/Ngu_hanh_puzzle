// ============================================================
//  ObjectPool.cs
//  Generic MonoBehaviour-compatible object pool.
//  Eliminates runtime Instantiate/Destroy for projectiles,
//  VFX bursts, damage numbers, and any frequently spawned object.
//
//  Usage:
//    // In your ability script:
//    var proj = ObjectPool.Instance.Get("WaterBolt", waterBoltPrefab);
//    proj.transform.position = spawnPos;
//    // When done (on hit or lifetime expired):
//    ObjectPool.Instance.Return("WaterBolt", proj);
//
//  Place in: Assets/Scripts/Utility/
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace FiveElements
{
    public class ObjectPool : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────
        public static ObjectPool Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────
        [System.Serializable]
        private class PoolDefinition
        {
            public string key;
            public GameObject prefab;
            public int initialSize = 8;
        }

        [Tooltip("Pre-warm pools on Awake. Key must match what abilities pass to Get().")]
        [SerializeField] private List<PoolDefinition> preWarmPools = new();

        // ── State ─────────────────────────────────────────────────
        // key → (prefab, stack of inactive instances)
        private Dictionary<string, (GameObject prefab, Stack<GameObject> stack)> _pools
            = new Dictionary<string, (GameObject, Stack<GameObject>)>();

        private Transform _poolRoot;

        // ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("PoolRoot").transform;
            _poolRoot.SetParent(transform);

            foreach (var def in preWarmPools)
                PreWarm(def.key, def.prefab, def.initialSize);
        }

        // ──────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Get an instance from the pool. Creates a new one if empty.
        /// The returned GameObject is active and un-parented.
        /// </summary>
        public GameObject Get(string key, GameObject prefab)
        {
            EnsurePool(key, prefab);

            var (_, stack) = _pools[key];
            GameObject obj;

            if (stack.Count > 0)
            {
                obj = stack.Pop();
                obj.SetActive(true);
            }
            else
            {
                obj = CreateInstance(prefab);
            }

            obj.transform.SetParent(null);
            return obj;
        }

        /// <summary>
        /// Return an object to the pool. Deactivates and re-parents it.
        /// </summary>
        public void Return(string key, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);

            if (!_pools.ContainsKey(key))
            {
                // Pool was never registered — just destroy
                Destroy(obj);
                return;
            }

            _pools[key].stack.Push(obj);
        }

        /// <summary>
        /// Return after a delay (fire-and-forget coroutine style).
        /// Call this instead of Destroy() for pooled objects.
        /// </summary>
        public void ReturnAfterDelay(string key, GameObject obj, float delay)
        {
            if (obj == null) return;
            StartCoroutine(ReturnDelayed(key, obj, delay));
        }

        /// <summary>Pre-warm a pool with a given number of instances.</summary>
        public void PreWarm(string key, GameObject prefab, int count)
        {
            EnsurePool(key, prefab);
            for (int i = 0; i < count; i++)
            {
                var obj = CreateInstance(prefab);
                _pools[key].stack.Push(obj);
            }
        }

        /// <summary>Empty a specific pool and destroy its instances.</summary>
        public void ClearPool(string key)
        {
            if (!_pools.ContainsKey(key)) return;
            var stack = _pools[key].stack;
            while (stack.Count > 0)
                Destroy(stack.Pop());
            _pools.Remove(key);
        }

        // ──────────────────────────────────────────────────────────
        // Private
        // ──────────────────────────────────────────────────────────
        private void EnsurePool(string key, GameObject prefab)
        {
            if (!_pools.ContainsKey(key))
                _pools[key] = (prefab, new Stack<GameObject>());
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            var obj = Instantiate(prefab, _poolRoot);
            obj.SetActive(false);
            return obj;
        }

        private System.Collections.IEnumerator ReturnDelayed(
            string key, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(key, obj);
        }
    }


    // ──────────────────────────────────────────────────────────────
    //  PooledObject helper — attach to prefabs to auto-return
    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Attach to any pooled prefab. Call AutoReturn() from ability
    /// scripts or collision handlers to return it automatically.
    /// Also self-returns on OnDisable if lifetime > 0.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        [Tooltip("Pool key used when returning. Must match the key passed to Get().")]
        public string poolKey;

        [Tooltip("Auto-return lifetime in seconds. 0 = manual return only.")]
        public float lifetime = 0f;

        private void OnEnable()
        {
            if (lifetime > 0f)
                ObjectPool.Instance?.ReturnAfterDelay(poolKey, gameObject, lifetime);
        }

        public void ReturnToPool()
        {
            ObjectPool.Instance?.Return(poolKey, gameObject);
        }
    }
}