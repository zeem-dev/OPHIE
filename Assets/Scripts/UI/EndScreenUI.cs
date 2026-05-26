// ============================================================
//  OPHIO — EndScreenUI
//  Day 12 | HUD + UI
//  Win/Lose screen — shows score, kills, time, medal,
//  and provides replay/next/menu buttons.
//  Attach to the EndScreen overlay Canvas.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace OPHIO.UI
{
    public class EndScreenUI : MonoBehaviour
    {
        [Header("Result")]
        public Text   resultTitleText;      // "VICTORY!" or "DEFEATED"
        public Text   levelNameText;
        public Image  resultIcon;
        public Color  victoryColor = new Color(1f, 0.85f, 0.2f);
        public Color  defeatColor  = new Color(0.8f, 0.2f, 0.2f);

        [Header("Stats")]
        public Text   scoreText;
        public Text   killsText;
        public Text   timeText;
        public Text   medalText;

        [Header("Medal Icons")]
        public GameObject bronzeMedalObj;
        public GameObject silverMedalObj;
        public GameObject goldMedalObj;

        [Header("Buttons")]
        public Button replayButton;
        public Button nextLevelButton;
        public Button mainMenuButton;

        [Header("Unlocks")]
        public Text   unlockText;  // "Level 2 Unlocked!" etc.

        private void Start()
        {
            // Wire buttons
            if (replayButton   != null)
                replayButton.onClick.AddListener(OnReplay);
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevel);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenu);

            // Populate from GameFlowManager
            if (Arena.GameFlowManager.Instance != null)
                PopulateResults();
        }

        public void PopulateResults()
        {
            var gfm = Arena.GameFlowManager.Instance;
            if (gfm == null) return;

            bool victory = gfm.lastWasVictory;

            // Title
            if (resultTitleText != null)
            {
                resultTitleText.text  = victory ? "VICTORY!" : "DEFEATED";
                resultTitleText.color = victory ? victoryColor : defeatColor;
            }

            // Level name
            if (levelNameText != null && gfm.selectedLevel != null)
                levelNameText.text = gfm.selectedLevel.levelName;

            // Stats
            if (scoreText != null)
                scoreText.text = $"Score: {gfm.lastScore}";
            if (killsText != null)
                killsText.text = $"Kills: {gfm.lastKills}";
            if (timeText != null)
            {
                int m = (int)(gfm.lastTime / 60f);
                int s = (int)(gfm.lastTime % 60f);
                timeText.text = $"Time: {m:00}:{s:00}";
            }

            // Medal
            if (medalText != null)
                medalText.text = victory ? $"Medal: {gfm.lastMedal}" : "";

            // Medal icons
            if (bronzeMedalObj != null) bronzeMedalObj.SetActive(false);
            if (silverMedalObj != null) silverMedalObj.SetActive(false);
            if (goldMedalObj   != null) goldMedalObj.SetActive(false);

            if (victory)
            {
                switch (gfm.lastMedal)
                {
                    case "Gold":
                        if (goldMedalObj   != null) goldMedalObj.SetActive(true);
                        if (silverMedalObj != null) silverMedalObj.SetActive(true);
                        if (bronzeMedalObj != null) bronzeMedalObj.SetActive(true);
                        break;
                    case "Silver":
                        if (silverMedalObj != null) silverMedalObj.SetActive(true);
                        if (bronzeMedalObj != null) bronzeMedalObj.SetActive(true);
                        break;
                    case "Bronze":
                        if (bronzeMedalObj != null) bronzeMedalObj.SetActive(true);
                        break;
                }
            }

            // Next level button — only show on victory
            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(victory);

            // Unlock text
            if (unlockText != null)
            {
                if (victory && gfm.selectedLevel != null)
                {
                    int nextNum = gfm.selectedLevel.levelNumber + 1;
                    var prog = Arena.ProgressionManager.Instance;
                    if (prog != null && prog.IsLevelUnlocked(nextNum))
                        unlockText.text = $"Level {nextNum} Unlocked!";
                    else
                        unlockText.text = "";
                }
                else
                {
                    unlockText.text = "";
                }
            }
        }

        // --------------------------------------------------
        //  Button handlers
        // --------------------------------------------------
        private void OnReplay()
        {
            Arena.GameFlowManager.Instance?.ReplayLevel();
        }

        private void OnNextLevel()
        {
            Arena.GameFlowManager.Instance?.NextLevel();
        }

        private void OnMainMenu()
        {
            Arena.GameFlowManager.Instance?.ReturnToMainMenu();
        }
    }
}
