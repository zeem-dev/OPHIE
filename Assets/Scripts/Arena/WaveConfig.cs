// ============================================================
//  OPHIO — WaveConfig ScriptableObject
//  Arena + Game Loop
//  Defines a single wave: which enemy types, counts,
//  spawn delay, and which spawners to use.
//  Create > OPHIO > Wave Config
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace OPHIO.Arena
{
    [System.Serializable]
    public class WaveSpawnGroup
    {
        [Tooltip("Pool key registered in ObjectPoolManager (e.g. Enemy_Normal)")]
        public string poolKey = "Enemy_Normal";

        [Tooltip("Prefab — auto-registers pool if not yet registered")]
        public GameObject prefab;

        [Tooltip("How many of this type to spawn")]
        public int count = 3;

        [Tooltip("Delay between each individual spawn (seconds)")]
        public float spawnDelay = 0.5f;

        [Tooltip("Which spawner index to use (matches ArenaManager.spawners list)")]
        public int spawnerIndex = 0;
    }

    [CreateAssetMenu(menuName = "OPHIO/Wave Config", fileName = "Wave_New")]
    public class WaveConfig : ScriptableObject
    {
        [Header("Wave Identity")]
        public string waveName = "Wave 1";
        [Tooltip("Display message when wave starts")]
        public string announceText = "Wave 1 — Incoming!";

        [Header("Spawn Groups")]
        [Tooltip("Each group defines a type + count to spawn")]
        public List<WaveSpawnGroup> spawnGroups = new List<WaveSpawnGroup>();

        [Header("Timing")]
        [Tooltip("Delay before this wave starts spawning (seconds after previous wave cleared)")]
        public float preWaveDelay = 3f;

        [Header("Boss Wave")]
        [Tooltip("If true, this wave contains a boss and triggers boss UI")]
        public bool isBossWave = false;

        // --------------------------------------------------
        //  Helpers
        // --------------------------------------------------

        /// <summary>Total number of enemies in this wave across all groups.</summary>
        public int TotalEnemyCount
        {
            get
            {
                int total = 0;
                foreach (var g in spawnGroups) total += g.count;
                return total;
            }
        }
    }
}
