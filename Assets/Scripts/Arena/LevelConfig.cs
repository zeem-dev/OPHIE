// ============================================================
//  OPHIO — LevelConfig ScriptableObject
//  Arena + Game Loop
//  Top-level config for one arena level.
//  References wave configs, objective, and rewards.
//  Create > OPHIO > Level Config
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace OPHIO.Arena
{
    [CreateAssetMenu(menuName = "OPHIO/Level Config", fileName = "Level_New")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        public string levelName    = "Level 1";
        public int    levelNumber  = 1;
        [TextArea(2, 3)]
        public string description = "";
        public Sprite thumbnail;

        [Header("Waves")]
        [Tooltip("Ordered list of wave configs for this level")]
        public List<WaveConfig> waves = new List<WaveConfig>();

        [Header("Objective")]
        public ArenaObjective objective = new ArenaObjective();

        [Header("Time Limit (0 = no limit)")]
        [Tooltip("Seconds. 0 means no time limit.")]
        public float timeLimit = 0f;

        [Header("Rewards")]
        public int scoreGoalBronze = 500;
        public int scoreGoalSilver = 1000;
        public int scoreGoalGold   = 2000;

        [Header("Unlock")]
        [Tooltip("Level number required to unlock this level (0 = always available)")]
        public int requiredLevel = 0;

        // --------------------------------------------------
        //  Helpers
        // --------------------------------------------------
        public int TotalWaves => waves.Count;

        public int TotalEnemies
        {
            get
            {
                int total = 0;
                foreach (var w in waves) total += w.TotalEnemyCount;
                return total;
            }
        }
    }
}
