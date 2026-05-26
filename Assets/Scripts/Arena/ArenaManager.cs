// ============================================================
//  OPHIO — ArenaManager
//  Arena + Game Loop
//  Core wave sequencer. Drives the entire arena flow:
//  Pre-wave countdown → Spawn wave → Track kills →
//  Wave complete → Next wave → Level complete / Failed.
//  Singleton — one per arena scene.
//  Attach to an empty "ArenaManager" GameObject.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Invector;
using OPHIO.Core;

namespace OPHIO.Arena
{
    public enum ArenaState
    {
        Inactive,       // Not started yet
        PreWave,        // Countdown before wave
        WaveActive,     // Enemies alive, fighting
        WaveComplete,   // All enemies dead, brief pause
        LevelComplete,  // All waves + objective done
        LevelFailed     // Player died or time ran out
    }

    public class ArenaManager : MonoBehaviour
    {
        // --------------------------------------------------
        //  Singleton
        // --------------------------------------------------
        public static ArenaManager Instance { get; private set; }

        // --------------------------------------------------
        //  Configuration
        // --------------------------------------------------
        [Header("Level")]
        [Tooltip("Assign the LevelConfig SO for this arena")]
        public LevelConfig levelConfig;

        [Header("Spawners")]
        [Tooltip("List of EnemySpawner components in the scene (order matches spawnerIndex in WaveConfig)")]
        public List<AI.EnemySpawner> spawners = new List<AI.EnemySpawner>();

        [Header("Player")]
        [Tooltip("Player reference — auto-found by tag if empty")]
        public GameObject player;

        // --------------------------------------------------
        //  State
        // --------------------------------------------------
        public ArenaState CurrentState     { get; private set; }
        public int        CurrentWaveIndex { get; private set; }
        public int        TotalKills       { get; private set; }
        public int        CurrentScore     { get; private set; }
        public float      ElapsedTime      { get; private set; }
        public int        AliveEnemies     { get; private set; }

        public WaveConfig CurrentWave =>
            (levelConfig != null && CurrentWaveIndex < levelConfig.waves.Count)
                ? levelConfig.waves[CurrentWaveIndex] : null;

        // --------------------------------------------------
        //  Events (for HUD, UI, audio)
        // --------------------------------------------------
        [Header("Events")]
        public UnityEvent              onArenaStarted;
        public UnityEvent<string>      onWaveAnnounce;     // wave name
        public UnityEvent<int, int>    onWaveProgress;     // current wave, total waves
        public UnityEvent<int>         onKillRegistered;   // total kills
        public UnityEvent<int>         onScoreChanged;     // current score
        public UnityEvent              onWaveComplete;
        public UnityEvent              onLevelComplete;
        public UnityEvent              onLevelFailed;
        public UnityEvent<string>      onObjectiveUpdate;  // objective progress text
        public UnityEvent<bool>        onBossWaveStarted;  // true = boss wave

        // --------------------------------------------------
        //  Internal
        // --------------------------------------------------
        private vHealthController _playerHealth;
        private int               _expectedEnemies;
        private int               _waveKills;

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            CurrentState = ArenaState.Inactive;
        }

        private void Start()
        {
            // Find player or spawn dynamically
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null)
                {
                    player = go;
                }
                else
                {
                    var spawner = GameObject.FindObjectOfType<PlayerSpawner>();
                    if (spawner != null)
                    {
                        string charName = "Hawk";
                        if (GameFlowManager.Instance != null && GameFlowManager.Instance.selectedCharacter != null)
                            charName = GameFlowManager.Instance.selectedCharacter.characterName;
                        else
                            charName = SceneNavigator.SelectedCharacter;

                        player = spawner.SpawnPlayer(charName);
                    }
                    else
                    {
                        Debug.LogWarning("[ArenaManager] No player in scene and no PlayerSpawner found!");
                    }
                }
            }

            if (player != null)
            {
                EnsurePlayerGroundingGuard(player);

                _playerHealth = player.GetComponent<vHealthController>();
                if (_playerHealth != null)
                    _playerHealth.onDead.AddListener(OnPlayerDeath);
            }

            // Play arena music when scene starts
            Core.AudioManager.Instance?.PlayArenaMusic();

            // Auto-start if level is configured
            if (levelConfig != null)
                StartArena();
        }

        private void Update()
        {
            if (CurrentState == ArenaState.Inactive ||
                CurrentState == ArenaState.LevelComplete ||
                CurrentState == ArenaState.LevelFailed) return;

            ElapsedTime += Time.deltaTime;

            // Time limit check
            if (levelConfig.timeLimit > 0f && ElapsedTime >= levelConfig.timeLimit)
            {
                // For SurviveWaves — surviving the time = win
                if (levelConfig.objective.type == ObjectiveType.SurviveWaves)
                    CompleteLevelSuccess();
                else
                    FailLevel();
                return;
            }

            // Track alive enemies during wave
            if (CurrentState == ArenaState.WaveActive)
            {
                AliveEnemies = CountAliveEnemies();
            }
        }

        // --------------------------------------------------
        //  Public API
        // --------------------------------------------------

        /// <summary>Start the arena sequence.</summary>
        public void StartArena()
        {
            if (levelConfig == null)
            {
                Debug.LogError("[ArenaManager] No LevelConfig assigned!");
                return;
            }

            CurrentWaveIndex = 0;
            TotalKills       = 0;
            CurrentScore     = 0;
            ElapsedTime      = 0f;

            // Setup objective
            var obj = levelConfig.objective;
            obj.Reset();

            switch (obj.type)
            {
                case ObjectiveType.EliminateAll:
                    obj.SetMax(levelConfig.TotalEnemies);
                    break;
                case ObjectiveType.SurviveWaves:
                    obj.SetMax(obj.targetWaveCount);
                    break;
                case ObjectiveType.DefeatBoss:
                    obj.SetMax(1);
                    break;
            }

            CurrentState = ArenaState.PreWave;
            onArenaStarted?.Invoke();
            onObjectiveUpdate?.Invoke(obj.GetProgressText());

            Debug.Log($"[ArenaManager] Starting {levelConfig.levelName} — {levelConfig.TotalWaves} waves, {levelConfig.TotalEnemies} total enemies");

            StartCoroutine(WaveSequenceRoutine());
        }

        // --------------------------------------------------
        //  Wave sequence
        // --------------------------------------------------
        private IEnumerator WaveSequenceRoutine()
        {
            while (CurrentWaveIndex < levelConfig.waves.Count)
            {
                var wave = levelConfig.waves[CurrentWaveIndex];

                // --- PRE-WAVE ---
                CurrentState = ArenaState.PreWave;
                onWaveAnnounce?.Invoke(wave.announceText);
                onWaveProgress?.Invoke(CurrentWaveIndex + 1, levelConfig.TotalWaves);

                if (wave.isBossWave)
                {
                    onBossWaveStarted?.Invoke(true);
                    Core.AudioManager.Instance?.PlayBossMusic();
                }

                Debug.Log($"[ArenaManager] {wave.waveName} starting in {wave.preWaveDelay}s...");
                yield return new WaitForSeconds(wave.preWaveDelay);

                // --- SPAWN WAVE ---
                CurrentState     = ArenaState.WaveActive;
                _waveKills       = 0;
                _expectedEnemies = wave.TotalEnemyCount;

                yield return StartCoroutine(SpawnWaveRoutine(wave));

                // --- WAIT FOR WAVE CLEAR ---
                yield return StartCoroutine(WaitForWaveClearRoutine());

                // Check if we failed during the wave
                if (CurrentState == ArenaState.LevelFailed) yield break;

                // --- WAVE COMPLETE ---
                CurrentState = ArenaState.WaveComplete;
                onWaveComplete?.Invoke();
                Core.AudioManager.Instance?.PlayWaveClear();

                // Update survive objective
                if (levelConfig.objective.type == ObjectiveType.SurviveWaves)
                {
                    levelConfig.objective.currentProgress = CurrentWaveIndex + 1;
                    onObjectiveUpdate?.Invoke(levelConfig.objective.GetProgressText());

                    if (levelConfig.objective.CheckCompletion())
                    {
                        CompleteLevelSuccess();
                        yield break;
                    }
                }

                Debug.Log($"[ArenaManager] {wave.waveName} cleared! Kills: {_waveKills}");

                CurrentWaveIndex++;

                // Brief pause between waves
                if (CurrentWaveIndex < levelConfig.waves.Count)
                    yield return new WaitForSeconds(1.5f);
            }

            // All waves done — check objective
            if (levelConfig.objective.type == ObjectiveType.EliminateAll)
            {
                if (levelConfig.objective.CheckCompletion())
                    CompleteLevelSuccess();
                else
                    CompleteLevelSuccess(); // All waves done = success for eliminate
            }
            else if (!levelConfig.objective.isCompleted)
            {
                CompleteLevelSuccess(); // Default: finishing all waves = win
            }
        }

        // --------------------------------------------------
        //  Spawn a single wave
        // --------------------------------------------------
        private IEnumerator SpawnWaveRoutine(WaveConfig wave)
        {
            foreach (var group in wave.spawnGroups)
            {
                // Get the correct spawner
                int idx = Mathf.Clamp(group.spawnerIndex, 0, spawners.Count - 1);
                if (idx >= spawners.Count)
                {
                    Debug.LogWarning($"[ArenaManager] Spawner index {group.spawnerIndex} out of range!");
                    continue;
                }

                var spawner = spawners[idx];

                // Register pool if prefab provided
                if (group.prefab != null)
                    Core.ObjectPoolManager.Instance?.RegisterPool(
                        group.poolKey, group.prefab, group.count + 2);

                // Wire death tracking before spawning
                spawner.onEnemySpawned -= OnEnemySpawned;
                spawner.onEnemySpawned += OnEnemySpawned;

                // Spawn
                spawner.SpawnByType(group.poolKey, group.count, group.spawnDelay);

                // Small delay between groups
                yield return new WaitForSeconds(0.3f);
            }
        }

        // --------------------------------------------------
        //  Wait until all enemies are dead
        // --------------------------------------------------
        private IEnumerator WaitForWaveClearRoutine()
        {
            // Give spawners time to actually spawn
            yield return new WaitForSeconds(1f);

            while (true)
            {
                if (CurrentState == ArenaState.LevelFailed) yield break;

                AliveEnemies = CountAliveEnemies();
                if (AliveEnemies <= 0) yield break;

                yield return new WaitForSeconds(0.5f);
            }
        }

        // --------------------------------------------------
        //  Enemy tracking
        // --------------------------------------------------
        private void OnEnemySpawned(AI.EnemyAI enemy)
        {
            enemy.onDeath -= OnEnemyKilled;
            enemy.onDeath += OnEnemyKilled;
        }

        private void OnEnemyKilled(AI.EnemyAI enemy)
        {
            TotalKills++;
            _waveKills++;

            // Score
            var init = enemy.GetComponent<AI.EnemyInitializer>();
            int score = (init != null && init.typeData != null)
                ? init.typeData.scoreValue : 100;
            CurrentScore += score;

            onKillRegistered?.Invoke(TotalKills);
            onScoreChanged?.Invoke(CurrentScore);

            // Objective tracking
            var obj = levelConfig.objective;
            if (obj.type == ObjectiveType.EliminateAll)
            {
                obj.currentProgress = TotalKills;
                onObjectiveUpdate?.Invoke(obj.GetProgressText());
            }

            // Boss kill tracking
            if (obj.type == ObjectiveType.DefeatBoss &&
                enemy.enemyType == AI.EnemyType.Boss)
            {
                obj.currentProgress = 1;
                obj.isCompleted     = true;
                onObjectiveUpdate?.Invoke(obj.GetProgressText());

                // Don't immediately win — let wave clear naturally
            }

            Debug.Log($"[ArenaManager] Kill #{TotalKills} | Score: {CurrentScore} | Alive: {CountAliveEnemies()}");
        }

        private int CountAliveEnemies()
        {
            int total = 0;
            foreach (var spawner in spawners)
                total += spawner.AliveCount;
            return total;
        }

        // --------------------------------------------------
        //  Win / Lose
        // --------------------------------------------------
        private void CompleteLevelSuccess()
        {
            if (CurrentState == ArenaState.LevelComplete) return;
            CurrentState = ArenaState.LevelComplete;

            levelConfig.objective.isCompleted = true;
            onLevelComplete?.Invoke();
            Core.AudioManager.Instance?.PlayVictoryMusic();
            Core.AudioManager.Instance?.PlayVictoryStinger();

            Debug.Log($"[ArenaManager] === LEVEL COMPLETE === Score: {CurrentScore} | Time: {ElapsedTime:F1}s | Kills: {TotalKills}");
        }

        private void FailLevel()
        {
            if (CurrentState == ArenaState.LevelFailed) return;
            CurrentState = ArenaState.LevelFailed;

            StopAllCoroutines();
            onLevelFailed?.Invoke();
            Core.AudioManager.Instance?.PlayDefeatMusic();
            Core.AudioManager.Instance?.PlayDefeatStinger();

            Debug.Log($"[ArenaManager] === LEVEL FAILED === Wave: {CurrentWaveIndex + 1} | Kills: {TotalKills}");
        }

        private void OnPlayerDeath(GameObject go)
        {
            FailLevel();
        }

        private void EnsurePlayerGroundingGuard(GameObject playerObject)
        {
            if (playerObject.GetComponent<PlayerGroundingGuard>() == null)
                playerObject.AddComponent<PlayerGroundingGuard>();
        }

        // --------------------------------------------------
        //  Score medal
        // --------------------------------------------------
        public string GetMedal()
        {
            if (levelConfig == null) return "None";
            if (CurrentScore >= levelConfig.scoreGoalGold)   return "Gold";
            if (CurrentScore >= levelConfig.scoreGoalSilver) return "Silver";
            if (CurrentScore >= levelConfig.scoreGoalBronze) return "Bronze";
            return "None";
        }

        // --------------------------------------------------
        //  Cleanup
        // --------------------------------------------------
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
