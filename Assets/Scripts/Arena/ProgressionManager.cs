// ============================================================
//  OPHIO — ProgressionManager
//  Flow + Progression
//  Tracks level completions, unlocks, and ability unlocks.
//  Saves/loads progress via PlayerPrefs (simple persistence).
//  Singleton — persists across scenes.
//  Attach to a "GameManagers" GameObject.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace OPHIO.Arena
{
    [System.Serializable]
    public class LevelProgress
    {
        public int    levelNumber;
        public bool   isUnlocked;
        public bool   isCompleted;
        public int    bestScore;
        public string bestMedal;    // None, Bronze, Silver, Gold
        public float  bestTime;
        public int    bestKills;

        public LevelProgress(int level)
        {
            levelNumber = level;
            isUnlocked  = level <= 1; // Level 1 always unlocked
            isCompleted = false;
            bestScore   = 0;
            bestMedal   = "None";
            bestTime    = float.MaxValue;
            bestKills   = 0;
        }
    }

    public class ProgressionManager : MonoBehaviour
    {
        // --------------------------------------------------
        //  Singleton
        // --------------------------------------------------
        public static ProgressionManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("All levels in the game (ordered)")]
        public List<LevelConfig> allLevels = new List<LevelConfig>();

        [Header("Ability Unlock")]
        [Tooltip("Number of levels to complete before each new ability unlocks")]
        public int levelsPerAbilityUnlock = 1;

        // --------------------------------------------------
        //  Runtime data
        // --------------------------------------------------
        public Dictionary<int, LevelProgress> Progress { get; private set; }
            = new Dictionary<int, LevelProgress>();

        /// <summary>How many total levels have been completed.</summary>
        public int CompletedLevelCount
        {
            get
            {
                int count = 0;
                foreach (var kvp in Progress)
                    if (kvp.Value.isCompleted) count++;
                return count;
            }
        }

        /// <summary>How many abilities should be unlocked based on progression.</summary>
        public int UnlockedAbilityCount =>
            Mathf.Min(3 + (CompletedLevelCount / Mathf.Max(levelsPerAbilityUnlock, 1)), 10);

        // --------------------------------------------------
        //  Events
        // --------------------------------------------------
        [Header("Events")]
        public UnityEvent<int> onLevelUnlocked;    // level number
        public UnityEvent<int> onAbilityUnlocked;  // ability index

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        // --------------------------------------------------
        //  Public API
        // --------------------------------------------------

        /// <summary>Record a level completion. Updates best score/time if improved.</summary>
        public void RecordLevelCompletion(int levelNumber, int score,
                                           string medal, float time, int kills)
        {
            if (!Progress.ContainsKey(levelNumber))
                Progress[levelNumber] = new LevelProgress(levelNumber);

            var lp = Progress[levelNumber];
            lp.isCompleted = true;

            if (score > lp.bestScore) lp.bestScore = score;
            if (time < lp.bestTime)   lp.bestTime  = time;
            if (kills > lp.bestKills) lp.bestKills = kills;

            // Medal priority: Gold > Silver > Bronze > None
            if (MedalRank(medal) > MedalRank(lp.bestMedal))
                lp.bestMedal = medal;

            // Unlock next level
            int nextLevel = levelNumber + 1;
            UnlockLevel(nextLevel);

            SaveProgress();

            Debug.Log($"[Progression] Level {levelNumber} completed! Score: {score} | Medal: {medal}");
        }

        /// <summary>Unlock a level if it isn't already.</summary>
        public void UnlockLevel(int levelNumber)
        {
            if (!Progress.ContainsKey(levelNumber))
                Progress[levelNumber] = new LevelProgress(levelNumber);

            if (Progress[levelNumber].isUnlocked) return;

            Progress[levelNumber].isUnlocked = true;
            onLevelUnlocked?.Invoke(levelNumber);
            SaveProgress();

            Debug.Log($"[Progression] Level {levelNumber} UNLOCKED!");
        }

        /// <summary>Check if a level is unlocked.</summary>
        public bool IsLevelUnlocked(int levelNumber)
        {
            if (levelNumber <= 1) return true; // Level 1 always available
            if (!Progress.ContainsKey(levelNumber)) return false;
            return Progress[levelNumber].isUnlocked;
        }

        /// <summary>Check if a level is completed.</summary>
        public bool IsLevelCompleted(int levelNumber)
        {
            if (!Progress.ContainsKey(levelNumber)) return false;
            return Progress[levelNumber].isCompleted;
        }

        /// <summary>Get progress for a specific level.</summary>
        public LevelProgress GetLevelProgress(int levelNumber)
        {
            if (!Progress.ContainsKey(levelNumber))
                Progress[levelNumber] = new LevelProgress(levelNumber);
            return Progress[levelNumber];
        }

        /// <summary>Check if an ability index is unlocked based on progression.</summary>
        public bool IsAbilityUnlocked(int abilityIndex)
        {
            return abilityIndex < UnlockedAbilityCount;
        }

        /// <summary>Reset all progress (new game).</summary>
        public void ResetAllProgress()
        {
            Progress.Clear();
            Progress[1] = new LevelProgress(1);
            SaveProgress();
            Debug.Log("[Progression] All progress reset!");
        }

        // --------------------------------------------------
        //  Persistence (PlayerPrefs — simple approach)
        // --------------------------------------------------
        private const string k_SaveKey = "OPHIO_Progression";

        private void SaveProgress()
        {
            var data = new ProgressionSaveData();
            foreach (var kvp in Progress)
                data.levels.Add(kvp.Value);
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(k_SaveKey, json);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            Progress.Clear();

            if (PlayerPrefs.HasKey(k_SaveKey))
            {
                string json = PlayerPrefs.GetString(k_SaveKey);
                var data = JsonUtility.FromJson<ProgressionSaveData>(json);
                if (data != null)
                {
                    foreach (var lp in data.levels)
                        Progress[lp.levelNumber] = lp;
                }
            }

            // Ensure level 1 is always unlocked
            if (!Progress.ContainsKey(1))
                Progress[1] = new LevelProgress(1);
            Progress[1].isUnlocked = true;
        }

        // --------------------------------------------------
        //  Helpers
        // --------------------------------------------------
        private int MedalRank(string medal)
        {
            switch (medal)
            {
                case "Gold":   return 3;
                case "Silver": return 2;
                case "Bronze": return 1;
                default:       return 0;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }

    // Wrapper for JSON serialization (JsonUtility needs a class)
    [System.Serializable]
    public class ProgressionSaveData
    {
        public List<LevelProgress> levels = new List<LevelProgress>();
    }
}
