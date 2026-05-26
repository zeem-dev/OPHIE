// ============================================================
//  OPHIO — OphioHUD
//  HUD + UI
//  Master HUD controller — wires health bar, energy bar,
//  charge bar (Hawk), 4 ability cooldown slots, score,
//  wave info, objective, and boss bar.
//  Extends Invector's existing vHUDController.
//  Attach to the HUD Canvas in the Arena scene.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OPHIO.UI
{
    public class OphioHUD : MonoBehaviour
    {
        // --------------------------------------------------
        //  Player references (auto-found)
        // --------------------------------------------------
        private Core.EnergyManager      _energy;
        private Core.AbilityExecutor    _executor;
        private Core.AbilityLoadout     _loadout;
        private Characters.HawkCharacter _hawk;
        private Invector.vHealthController _playerHealth;

        // --------------------------------------------------
        //  Health Bar
        // --------------------------------------------------
        [Header("── HEALTH BAR ──")]
        public Slider healthSlider;
        public TextMeshProUGUI   healthText;
        public Image  healthFill;
        public Color  healthFullColor = new Color(0.2f, 0.9f, 0.3f);
        public Color  healthLowColor  = new Color(0.9f, 0.2f, 0.2f);

        // --------------------------------------------------
        //  Energy Bar
        // --------------------------------------------------
        [Header("── ENERGY BAR ──")]
        public Slider energySlider;
        public TextMeshProUGUI energyText;
        public Image  energyFill;
        public Color  energyColor = new Color(0.3f, 0.6f, 1f);

        // --------------------------------------------------
        //  Charge Bar (Hawk specific)
        // --------------------------------------------------
        [Header("── CHARGE BAR ──")]
        public Slider chargeSlider;
        public TextMeshProUGUI chargeText;
        public Image  chargeFill;
        public Color  chargeEmptyColor = new Color(0.4f, 0.4f, 0.4f);
        public Color  chargeFullColor  = new Color(1f, 0.9f, 0.2f);

        // --------------------------------------------------
        //  Ability Slots
        // --------------------------------------------------
        [Header("── ABILITY SLOTS ──")]
        public AbilityCooldownUI slot1UI;  // Q
        public AbilityCooldownUI slot2UI;  // E
        public AbilityCooldownUI slot3UI;  // R
        public AbilityCooldownUI superUI;  // F

        // --------------------------------------------------
        //  Wave / Score info
        // --------------------------------------------------
        [Header("── WAVE INFO ──")]
        public TextMeshProUGUI waveText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI objectiveText;
        public TextMeshProUGUI timerText;

        // --------------------------------------------------
        //  Wave Announcement
        // --------------------------------------------------
        [Header("── WAVE ANNOUNCE ──")]
        public TextMeshProUGUI announceText;
        public float  announceDuration = 2.5f;
        private float _announceTimer;

        // --------------------------------------------------
        //  Boss Bar
        // --------------------------------------------------
        [Header("── BOSS BAR ──")]
        public GameObject bossBarContainer;
        public Slider     bossHealthSlider;
        public TextMeshProUGUI bossNameText;
        private AI.BossEnemyBehavior _activeBoss;

        // --------------------------------------------------
        //  Enemy Damage Indicators
        // --------------------------------------------------
        [Header("── DAMAGE INDICATORS ──")]
        public GameObject damageIndicatorPrefab;
        public Transform  indicatorContainer;

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Start()
        {
            // Find player components
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _energy       = player.GetComponent<Core.EnergyManager>();
                _executor     = player.GetComponent<Core.AbilityExecutor>();
                _loadout      = player.GetComponent<Core.AbilityLoadout>();
                _hawk         = player.GetComponent<Characters.HawkCharacter>();
                _playerHealth = player.GetComponent<Invector.vHealthController>();
            }

            // Init ability slots
            if (slot1UI != null) slot1UI.Init(0, _executor, _energy, _loadout);
            if (slot2UI != null) slot2UI.Init(1, _executor, _energy, _loadout);
            if (slot3UI != null) slot3UI.Init(2, _executor, _energy, _loadout);
            if (superUI != null) superUI.Init(3, _executor, _energy, _loadout);

            // Hide boss bar initially
            if (bossBarContainer != null)
                bossBarContainer.SetActive(false);

            // Wire ArenaManager events
            if (Arena.ArenaManager.Instance != null)
            {
                var am = Arena.ArenaManager.Instance;
                am.onWaveAnnounce.AddListener(ShowWaveAnnounce);
                am.onWaveProgress.AddListener(UpdateWaveText);
                am.onScoreChanged.AddListener(UpdateScore);
                am.onObjectiveUpdate.AddListener(UpdateObjective);
                am.onBossWaveStarted.AddListener(OnBossWave);
                am.onLevelComplete.AddListener(OnLevelComplete);
                am.onLevelFailed.AddListener(OnLevelFailed);
            }

            // Wire energy events
            if (_energy != null)
                _energy.onEnergyChanged.AddListener(UpdateEnergyBar);

            // Wire charge events (Hawk)
            if (_hawk != null)
                _hawk.onChargeChanged += UpdateChargeBar;

            // Hide announce text
            if (announceText != null)
                announceText.enabled = false;
        }

        private void Update()
        {
            UpdateHealthBar();

            // Announce timer
            if (_announceTimer > 0f)
            {
                _announceTimer -= Time.deltaTime;
                if (_announceTimer <= 0f && announceText != null)
                    announceText.enabled = false;
            }

            // Timer display
            UpdateTimer();

            // Boss bar
            UpdateBossBar();
        }

        // --------------------------------------------------
        //  Health Bar
        // --------------------------------------------------
        private void UpdateHealthBar()
        {
            if (_playerHealth == null) return;

            float percent = _playerHealth.currentHealth / (float)_playerHealth.maxHealth;
            percent = Mathf.Clamp01(percent);

            if (healthSlider != null)
                healthSlider.value = percent;

            if (healthText != null)
                healthText.text = $"{(int)_playerHealth.currentHealth}/{_playerHealth.maxHealth}";

            if (healthFill != null)
                healthFill.color = Color.Lerp(healthLowColor, healthFullColor, percent);
        }

        // --------------------------------------------------
        //  Energy Bar
        // --------------------------------------------------
        private void UpdateEnergyBar(float current, float max)
        {
            float percent = max > 0 ? current / max : 0f;

            if (energySlider != null)
                energySlider.value = percent;

            if (energyText != null)
                energyText.text = $"{(int)current}/{(int)max}";
        }

        // --------------------------------------------------
        //  Charge Bar (Hawk)
        // --------------------------------------------------
        private void UpdateChargeBar(float current, float max)
        {
            float percent = max > 0 ? current / max : 0f;

            if (chargeSlider != null)
                chargeSlider.value = percent;

            if (chargeText != null)
                chargeText.text = $"{(int)current}%";

            if (chargeFill != null)
                chargeFill.color = Color.Lerp(chargeEmptyColor, chargeFullColor, percent);
        }

        // --------------------------------------------------
        //  Wave / Score
        // --------------------------------------------------
        private void ShowWaveAnnounce(string text)
        {
            if (announceText == null) return;
            announceText.text    = text;
            announceText.enabled = true;
            _announceTimer = announceDuration;
        }

        private void UpdateWaveText(int current, int total)
        {
            if (waveText != null)
                waveText.text = $"Wave {current}/{total}";
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }

        private void UpdateObjective(string text)
        {
            if (objectiveText != null)
                objectiveText.text = text;
        }

        private void UpdateTimer()
        {
            if (timerText == null) return;
            if (Arena.ArenaManager.Instance == null) return;

            var am = Arena.ArenaManager.Instance;
            if (am.levelConfig != null && am.levelConfig.timeLimit > 0f)
            {
                float remaining = Mathf.Max(0f, am.levelConfig.timeLimit - am.ElapsedTime);
                int minutes = (int)(remaining / 60f);
                int seconds = (int)(remaining % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
                timerText.color = remaining < 15f
                    ? new Color(1f, 0.3f, 0.3f)
                    : Color.white;
            }
            else
            {
                float elapsed = am.ElapsedTime;
                int minutes = (int)(elapsed / 60f);
                int seconds = (int)(elapsed % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        // --------------------------------------------------
        //  Boss Bar
        // --------------------------------------------------
        private void OnBossWave(bool isBoss)
        {
            if (!isBoss) return;

            // Find the boss in scene
            _activeBoss = FindAnyObjectByType<AI.BossEnemyBehavior>();

            if (_activeBoss != null && bossBarContainer != null)
            {
                bossBarContainer.SetActive(true);
                if (bossNameText != null)
                    bossNameText.text = _activeBoss.bossName;
            }
        }

        private void UpdateBossBar()
        {
            if (_activeBoss == null || bossBarContainer == null) return;
            if (!bossBarContainer.activeSelf) return;

            if (bossHealthSlider != null)
                bossHealthSlider.value = _activeBoss.HPPercent;

            // Hide when dead
            var bossHealth = _activeBoss.GetComponent<Invector.vHealthController>();
            if (bossHealth != null && bossHealth.isDead)
                bossBarContainer.SetActive(false);
        }

        // --------------------------------------------------
        //  End states
        // --------------------------------------------------
        private void OnLevelComplete()
        {
            ShowWaveAnnounce("VICTORY!");
        }

        private void OnLevelFailed()
        {
            ShowWaveAnnounce("DEFEATED");
        }
    }
}
