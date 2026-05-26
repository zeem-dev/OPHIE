// ============================================================
//  OPHIO — GameFlowManager
//  Flow + Progression
//  Controls the full game flow:
//  MainMenu → CharacterSelect → LoadoutBuilder →
//  Arena (fight) → EndScreen → back to menu.
//  Uses Unity SceneManager for scene transitions.
//  Singleton — persists across scenes.
//  Attach to a "GameManagers" GameObject.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace OPHIO.Arena
{
    public enum GamePhase
    {
        MainMenu,
        CharacterSelect,
        LoadoutBuilder,
        ArenaLoading,
        ArenaActive,
        ArenaEnd,        // Win or lose screen
        Paused
    }

    public class GameFlowManager : MonoBehaviour
    {
        // --------------------------------------------------
        //  Singleton
        // --------------------------------------------------
        public static GameFlowManager Instance { get; private set; }

        // --------------------------------------------------
        //  Scene Names (match your Build Settings)
        // --------------------------------------------------
        [Header("Scene Names")]
        public string mainMenuScene      = "MainMenu";
        public string characterSelectScene = "CharacterSelect";
        public string loadoutScene       = "LoadoutBuilder";
        public string arenaScene         = "Arena";
        public string endScreenScene     = "EndScreen";

        // --------------------------------------------------
        //  State
        // --------------------------------------------------
        public GamePhase CurrentPhase     { get; private set; }
        public bool      IsPaused         { get; private set; }

        [Header("All Playable Characters")]
        [Tooltip("Drag all 5 CharacterData SOs here — Hawk, Goon, Mac, Gust, Flex")]
        public List<Core.CharacterData> allCharacters = new List<Core.CharacterData>();

        // Carry data between scenes
        [HideInInspector] public Core.CharacterData selectedCharacter;
        [HideInInspector] public LevelConfig        selectedLevel;
        [HideInInspector] public int  lastScore;
        [HideInInspector] public int  lastKills;
        [HideInInspector] public float lastTime;
        [HideInInspector] public string lastMedal;
        [HideInInspector] public bool lastWasVictory;

        // Selected loadout — set by LoadoutSelectionManager, read by Arena
        [HideInInspector] public Core.AbilityData selectedSlot1;
        [HideInInspector] public Core.AbilityData selectedSlot2;
        [HideInInspector] public Core.AbilityData selectedSlot3;
        [HideInInspector] public Core.AbilityData selectedSuper;

        // --------------------------------------------------
        //  Events
        // --------------------------------------------------
        [Header("Events")]
        public UnityEvent<GamePhase> onPhaseChanged;
        public UnityEvent            onGamePaused;
        public UnityEvent            onGameResumed;

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentPhase = GamePhase.MainMenu;
        }

        // --------------------------------------------------
        //  Flow methods — call these from UI buttons
        // --------------------------------------------------

        /// <summary>Start the game from main menu.</summary>
        public void StartGame()
        {
            Core.AudioManager.Instance?.PlayButtonClick();
            SetPhase(GamePhase.CharacterSelect);
            LoadSceneAsync(characterSelectScene);
        }

        /// <summary>Character selected — move to loadout.</summary>
        public void OnCharacterSelected(Core.CharacterData character)
        {
            selectedCharacter = character;
            Core.AudioManager.Instance?.PlayCharacterSelect();
            SetPhase(GamePhase.LoadoutBuilder);
            LoadSceneAsync(loadoutScene);
        }

        /// <summary>Loadout confirmed — start the arena.</summary>
        public void OnLoadoutConfirmed(LevelConfig level)
        {
            selectedLevel = level;
            SetPhase(GamePhase.ArenaLoading);
            StartCoroutine(LoadArenaRoutine());
        }

        /// <summary>Quick play — skip character/loadout, go straight to arena.</summary>
        public void QuickPlay(Core.CharacterData character, LevelConfig level)
        {
            selectedCharacter = character;
            selectedLevel     = level;
            SetPhase(GamePhase.ArenaLoading);
            StartCoroutine(LoadArenaRoutine());
        }

        /// <summary>Arena finished — record results and show end screen.</summary>
        public void OnArenaFinished(bool victory, int score, int kills,
                                     float time, string medal)
        {
            lastWasVictory = victory;
            lastScore      = score;
            lastKills      = kills;
            lastTime       = time;
            lastMedal      = medal;

            // Save progression
            if (victory && selectedLevel != null)
            {
                ProgressionManager.Instance?.RecordLevelCompletion(
                    selectedLevel.levelNumber, score, medal, time, kills);
            }

            SetPhase(GamePhase.ArenaEnd);

            // Load end screen or show overlay (depending on your setup)
            // If you have a separate end screen scene:
            // LoadSceneAsync(endScreenScene);
            // If overlay — just fire the event, UI handles it
        }

        /// <summary>Replay the same level.</summary>
        public void ReplayLevel()
        {
            if (selectedLevel != null)
            {
                SetPhase(GamePhase.ArenaLoading);
                StartCoroutine(LoadArenaRoutine());
            }
        }

        /// <summary>Return to main menu.</summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SetPhase(GamePhase.MainMenu);
            LoadSceneAsync(mainMenuScene);
        }

        /// <summary>Go to next level.</summary>
        public void NextLevel()
        {
            if (selectedLevel == null) return;

            int nextNum = selectedLevel.levelNumber + 1;
            var prog = ProgressionManager.Instance;

            if (prog != null && prog.IsLevelUnlocked(nextNum))
            {
                // Find the LevelConfig for next level
                var nextConfig = prog.allLevels.Find(l => l.levelNumber == nextNum);
                if (nextConfig != null)
                {
                    selectedLevel = nextConfig;
                    SetPhase(GamePhase.ArenaLoading);
                    StartCoroutine(LoadArenaRoutine());
                    return;
                }
            }

            // No next level — return to menu
            ReturnToMainMenu();
        }

        // --------------------------------------------------
        //  Pause
        // --------------------------------------------------
        public void TogglePause()
        {
            if (CurrentPhase != GamePhase.ArenaActive &&
                CurrentPhase != GamePhase.Paused) return;

            if (IsPaused)
                ResumeGame();
            else
                PauseGame();
        }

        public void PauseGame()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            SetPhase(GamePhase.Paused);
            onGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            SetPhase(GamePhase.ArenaActive);
            onGameResumed?.Invoke();
        }

        // --------------------------------------------------
        //  Scene loading
        // --------------------------------------------------
        private IEnumerator LoadArenaRoutine()
        {
            var op = SceneManager.LoadSceneAsync(arenaScene);
            if (op == null)
            {
                Debug.LogError($"[GameFlow] Failed to load scene '{arenaScene}'!");
                yield break;
            }

            while (!op.isDone)
                yield return null;

            // Arena scene loaded — ArenaManager will auto-start if configured
            SetPhase(GamePhase.ArenaActive);

            // Wire ArenaManager events
            if (ArenaManager.Instance != null)
            {
                ArenaManager.Instance.onLevelComplete.AddListener(OnArenaVictory);
                ArenaManager.Instance.onLevelFailed.AddListener(OnArenaDefeat);

                // If level config wasn't auto-assigned, set it now
                if (ArenaManager.Instance.levelConfig == null && selectedLevel != null)
                {
                    ArenaManager.Instance.levelConfig = selectedLevel;
                    ArenaManager.Instance.StartArena();
                }
            }
        }

        private void LoadSceneAsync(string sceneName)
        {
            SceneManager.LoadSceneAsync(sceneName);
        }

        // --------------------------------------------------
        //  Arena callbacks
        // --------------------------------------------------
        private void OnArenaVictory()
        {
            if (ArenaManager.Instance == null) return;
            var am = ArenaManager.Instance;
            OnArenaFinished(true, am.CurrentScore, am.TotalKills,
                            am.ElapsedTime, am.GetMedal());
        }

        private void OnArenaDefeat()
        {
            if (ArenaManager.Instance == null) return;
            var am = ArenaManager.Instance;
            OnArenaFinished(false, am.CurrentScore, am.TotalKills,
                            am.ElapsedTime, "None");
        }

        // --------------------------------------------------
        //  Helper
        // --------------------------------------------------
        private void SetPhase(GamePhase phase)
        {
            CurrentPhase = phase;
            onPhaseChanged?.Invoke(phase);
            Debug.Log($"[GameFlow] Phase → {phase}");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
