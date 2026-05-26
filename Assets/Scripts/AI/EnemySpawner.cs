// ============================================================
//  OPHIO — EnemySpawner
//  Boss + Spawner
//  Spawns enemies by type at designated positions.
//  Used by ArenaManager/WaveManager to populate waves.
//  Place one in the arena scene, or use multiple for
//  different spawn zones.
//  Attach to empty GameObjects at spawn locations.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace OPHIO.AI
{
    [System.Serializable]
    public class SpawnEntry
    {
        public EnemyType type;
        [Tooltip("Pool key registered in ObjectPoolManager")]
        public string poolKey;
        [Tooltip("Prefab — used to register the pool if not already done")]
        public GameObject prefab;
        [Tooltip("How many to spawn")]
        public int count = 1;
        [Tooltip("Delay in seconds between each spawn of this type")]
        public float spawnDelay = 0.5f;
    }

    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [Tooltip("Available spawn entries. ArenaManager selects which to use per wave.")]
        public List<SpawnEntry> spawnEntries = new List<SpawnEntry>();

        [Header("Spawn Area")]
        [Tooltip("Spawn positions are randomized within this radius")]
        public float spawnRadius = 5f;
        [Tooltip("If true, enemies are spread evenly in a circle instead of random")]
        public bool  evenDistribution = false;

        [Header("Spawn Points (optional)")]
        [Tooltip("If assigned, enemies spawn at these specific transforms instead of random")]
        public List<Transform> spawnPoints = new List<Transform>();

        // --------------------------------------------------
        //  Events
        // --------------------------------------------------
        public System.Action<EnemyAI> onEnemySpawned;

        // Track spawned enemies for wave completion
        private List<EnemyAI> _spawnedEnemies = new List<EnemyAI>();
        public int AliveCount
        {
            get
            {
                _spawnedEnemies.RemoveAll(e => e == null ||
                    e.CurrentStateType == AIStateType.Dead);
                return _spawnedEnemies.Count;
            }
        }

        // --------------------------------------------------
        //  Public API
        // --------------------------------------------------

        /// <summary>Spawn all entries configured on this spawner.</summary>
        public void SpawnAll()
        {
            StartCoroutine(SpawnAllRoutine());
        }

        /// <summary>Spawn a specific entry by index.</summary>
        public void SpawnEntry(int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= spawnEntries.Count) return;
            StartCoroutine(SpawnEntryRoutine(spawnEntries[entryIndex]));
        }

        /// <summary>Spawn N enemies of a specific type using a pool key.</summary>
        public void SpawnByType(string poolKey, int count, float delay = 0.5f)
        {
            StartCoroutine(SpawnByKeyRoutine(poolKey, count, delay));
        }

        /// <summary>Clear the spawned list tracking.</summary>
        public void ResetTracking()
        {
            _spawnedEnemies.Clear();
        }

        // --------------------------------------------------
        //  Spawn routines
        // --------------------------------------------------
        private IEnumerator SpawnAllRoutine()
        {
            foreach (var entry in spawnEntries)
            {
                yield return StartCoroutine(SpawnEntryRoutine(entry));
            }
        }

        private IEnumerator SpawnEntryRoutine(SpawnEntry entry)
        {
            // Register pool if not already done
            if (entry.prefab != null)
                Core.ObjectPoolManager.Instance?.RegisterPool(
                    entry.poolKey, entry.prefab, entry.count + 2);

            for (int i = 0; i < entry.count; i++)
            {
                Vector3 pos = GetSpawnPosition(i, entry.count);
                SpawnSingleEnemy(entry.poolKey, pos);
                yield return new WaitForSeconds(entry.spawnDelay);
            }
        }

        private IEnumerator SpawnByKeyRoutine(string poolKey, int count, float delay)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetSpawnPosition(i, count);
                SpawnSingleEnemy(poolKey, pos);
                yield return new WaitForSeconds(delay);
            }
        }

        private void SpawnSingleEnemy(string poolKey, Vector3 position)
        {
            var pool = Core.ObjectPoolManager.Instance;
            if (pool == null)
            {
                Debug.LogWarning("[EnemySpawner] No ObjectPoolManager found!");
                return;
            }

            var enemyObj = pool.Spawn(poolKey, position, Quaternion.identity);
            if (enemyObj == null)
            {
                Debug.LogWarning($"[EnemySpawner] Failed to spawn '{poolKey}'");
                return;
            }

            // Get or refresh AI
            var ai = enemyObj.GetComponent<EnemyAI>();
            if (ai != null)
            {
                // Re-enable NavMeshAgent (may have been disabled on death)
                var agent = enemyObj.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = true;

                // Reset health
                var health = enemyObj.GetComponent<Invector.vHealthController>();
                if (health != null)
                {
                    // currentHealth setter is protected — use ChangeHealth to restore
                    int diff = (int)(health.maxHealth - health.currentHealth);
                    if (diff > 0) health.ChangeHealth(diff);
                }

                // Re-run initializer
                var init = enemyObj.GetComponent<EnemyInitializer>();
                if (init != null && init.typeData != null)
                    init.typeData.ApplyTo(ai);

                // Start in chase mode (already alerted by spawn)
                ai.ChangeState(AIStateType.Chase);

                // Track for alive count
                _spawnedEnemies.Add(ai);

                // Notify listeners
                onEnemySpawned?.Invoke(ai);
            }

            Debug.Log($"[EnemySpawner] Spawned {poolKey} at {position}");
        }

        // --------------------------------------------------
        //  Position calculation
        // --------------------------------------------------
        private Vector3 GetSpawnPosition(int index, int total)
        {
            Vector3 basePos;

            // Use explicit spawn points if available
            if (spawnPoints.Count > 0)
            {
                int ptIndex = index % spawnPoints.Count;
                basePos = spawnPoints[ptIndex].position;
            }
            else if (evenDistribution)
            {
                // Even circle distribution
                float angle = (360f / Mathf.Max(total, 1)) * index;
                float rad   = angle * Mathf.Deg2Rad;
                basePos = transform.position + new Vector3(
                    Mathf.Cos(rad) * spawnRadius,
                    0f,
                    Mathf.Sin(rad) * spawnRadius);
            }
            else
            {
                // Random within radius
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                basePos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            }

            // Snap to NavMesh
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(basePos, out navHit, 5f, NavMesh.AllAreas))
                return navHit.position;

            return basePos;
        }

        // --------------------------------------------------
        //  Debug gizmos
        // --------------------------------------------------
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            // Draw spawn points
            if (spawnPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var pt in spawnPoints)
                {
                    if (pt != null)
                        Gizmos.DrawSphere(pt.position, 0.3f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}
