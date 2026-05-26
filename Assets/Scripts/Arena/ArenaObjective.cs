// ============================================================
//  OPHIO — ArenaObjective
//  Arena + Game Loop
//  Defines the 3 arena objective types and their
//  completion tracking logic.
// ============================================================

using UnityEngine;

namespace OPHIO.Arena
{
    public enum ObjectiveType
    {
        EliminateAll,   // Kill every enemy in all waves
        SurviveWaves,   // Survive N waves (player stays alive)
        DefeatBoss      // Kill the boss enemy
    }

    [System.Serializable]
    public class ArenaObjective
    {
        [Header("Objective")]
        public ObjectiveType type = ObjectiveType.EliminateAll;

        [Tooltip("For SurviveWaves: how many waves to survive")]
        public int targetWaveCount = 5;

        [Tooltip("Display text for HUD")]
        public string displayText = "Eliminate all enemies";

        // --------------------------------------------------
        //  Runtime tracking
        // --------------------------------------------------
        [HideInInspector] public bool isCompleted;
        [HideInInspector] public int  currentProgress;
        [HideInInspector] public int  maxProgress;

        /// <summary>Progress as 0-1 float for UI bars.</summary>
        public float ProgressPercent =>
            maxProgress > 0 ? (float)currentProgress / maxProgress : 0f;

        public void Reset()
        {
            isCompleted     = false;
            currentProgress = 0;
        }

        public void SetMax(int max)
        {
            maxProgress = max;
        }

        public bool CheckCompletion()
        {
            if (isCompleted) return true;
            isCompleted = currentProgress >= maxProgress;
            return isCompleted;
        }

        /// <summary>Build display text with progress.</summary>
        public string GetProgressText()
        {
            switch (type)
            {
                case ObjectiveType.EliminateAll:
                    return $"Eliminate All — {currentProgress}/{maxProgress}";
                case ObjectiveType.SurviveWaves:
                    return $"Survive — Wave {currentProgress}/{maxProgress}";
                case ObjectiveType.DefeatBoss:
                    return isCompleted ? "Boss Defeated!" : "Defeat the Boss";
                default:
                    return displayText;
            }
        }
    }
}
