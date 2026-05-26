// ============================================================
//  OPHIO — BossEnemyBehavior
//  Boss AI
//  Multi-phase boss framework.
//  Phase 1 → Phase 2 transition at configurable HP threshold.
//  Each phase has its own attack patterns, aggression,
//  speed, and special abilities.
//  Attach alongside EnemyAI on Boss enemy prefab.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Invector;

namespace OPHIO.AI
{
    [System.Serializable]
    public class BossPhaseConfig
    {
        [Header("Phase Identity")]
        public string phaseName = "Phase 1";
        [Tooltip("HP percentage at which this phase STARTS (1.0 = full, 0.5 = half)")]
        [Range(0f, 1f)]
        public float hpThreshold = 1f;

        [Header("Combat")]
        public float attackDamage    = 25f;
        public float attackCooldown  = 2f;
        public float chaseSpeed      = 3.5f;
        public float aggressionLevel = 1f;

        [Header("Special Attack")]
        public bool  hasSpecialAttack    = false;
        public float specialDamage       = 60f;
        public float specialRadius       = 5f;
        public float specialCooldown     = 10f;
        public float specialWindup       = 1f;
        public string specialAnimTrigger = "SpecialAttack";

        [Header("Summon Adds")]
        public bool  canSummonAdds       = false;
        [Tooltip("Key for ObjectPoolManager")]
        public string summonPoolKey      = "";
        public int   summonCount         = 2;
        public float summonCooldown      = 15f;
        public float summonRadius        = 5f;

        [Header("VFX / Audio")]
        public GameObject phaseTransitionVFX;
        public GameObject specialAttackVFX;
        public AudioClip  phaseTransitionSound;
        public AudioClip  specialAttackSound;
    }

    [RequireComponent(typeof(EnemyAI))]
    public class BossEnemyBehavior : MonoBehaviour
    {
        [Header("Boss Configuration")]
        [Tooltip("Boss display name (for HUD)")]
        public string bossName = "Boss";

        [Header("Phase Configurations (ordered by HP threshold, highest first)")]
        public List<BossPhaseConfig> phases = new List<BossPhaseConfig>();

        [Header("Enrage")]
        [Tooltip("If true, boss enrages when below enrageThreshold HP")]
        public bool  canEnrage        = true;
        [Range(0f, 0.3f)]
        public float enrageThreshold  = 0.15f;
        public float enrageSpeedMult  = 1.5f;
        public float enrageDamageMult = 1.5f;

        [Header("Boss Bar Events")]
        public UnityEngine.Events.UnityEvent onBossActivated;
        public UnityEngine.Events.UnityEvent onBossDefeated;
        public UnityEngine.Events.UnityEvent<int> onPhaseChanged; // phase index

        // --------------------------------------------------
        //  State
        // --------------------------------------------------
        public int   CurrentPhaseIndex  { get; private set; }
        public bool  IsEnraged          { get; private set; }
        public float HPPercent          => _health != null
            ? (float)_health.currentHealth / _health.maxHealth : 0f;

        private EnemyAI           _ai;
        private NavMeshAgent      _agent;
        private vHealthController _health;
        private Animator          _animator;

        private float _specialTimer;
        private float _summonTimer;
        private bool  _isSpecialAttacking;
        private bool  _phaseTransitioning;
        private bool  _bossActivated;

        private void Awake()
        {
            _ai       = GetComponent<EnemyAI>();
            _agent    = GetComponent<NavMeshAgent>();
            _health   = GetComponent<vHealthController>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            CurrentPhaseIndex = 0;

            if (phases.Count == 0)
            {
                Debug.LogWarning($"[BossAI] {bossName} has no phases configured!");
                return;
            }

            ApplyPhase(0);

            // Wire damage callback for phase checking
            if (_health != null)
                _health.onReceiveDamage.AddListener(OnBossDamaged);
        }

        private void Update()
        {
            if (_ai.CurrentStateType == AIStateType.Dead) return;
            if (_phaseTransitioning) return;

            // Activate boss when first detected
            if (!_bossActivated && _ai.CurrentStateType == AIStateType.Chase)
            {
                _bossActivated = true;
                onBossActivated?.Invoke();
            }

            var phase = GetCurrentPhase();
            if (phase == null) return;

            // Special attack cooldown
            _specialTimer -= Time.deltaTime;
            _summonTimer  -= Time.deltaTime;

            // Try special attack during Attack state
            if (_ai.CurrentStateType == AIStateType.Attack && !_isSpecialAttacking)
            {
                if (phase.hasSpecialAttack && _specialTimer <= 0f)
                    StartCoroutine(SpecialAttackRoutine(phase));
            }

            // Try summon during Chase/Attack
            if (phase.canSummonAdds && _summonTimer <= 0f &&
                (_ai.CurrentStateType == AIStateType.Chase ||
                 _ai.CurrentStateType == AIStateType.Attack))
            {
                SummonAdds(phase);
                _summonTimer = phase.summonCooldown;
            }

            // Enrage check
            if (canEnrage && !IsEnraged && HPPercent <= enrageThreshold)
                Enrage();
        }

        // --------------------------------------------------
        //  Phase management
        // --------------------------------------------------
        private void OnBossDamaged(vDamage damage)
        {
            CheckPhaseTransition();
        }

        private void CheckPhaseTransition()
        {
            if (_phaseTransitioning) return;

            float hp = HPPercent;

            // Find the correct phase for current HP
            for (int i = phases.Count - 1; i >= 0; i--)
            {
                if (hp <= phases[i].hpThreshold && i > CurrentPhaseIndex)
                {
                    StartCoroutine(PhaseTransitionRoutine(i));
                    break;
                }
            }
        }

        private IEnumerator PhaseTransitionRoutine(int newPhaseIndex)
        {
            _phaseTransitioning = true;
            var phase = phases[newPhaseIndex];

            Debug.Log($"[BossAI] {bossName} entering {phase.phaseName}!");

            // Pause AI during transition
            _agent.isStopped = true;

            // Transition VFX
            if (phase.phaseTransitionVFX != null)
            {
                string key = $"BossPhase{newPhaseIndex}VFX";
                Core.ObjectPoolManager.Instance?.RegisterPool(key, phase.phaseTransitionVFX, 2);
                var vfx = Core.ObjectPoolManager.Instance?.Spawn(key, transform.position, Quaternion.identity);
                if (vfx != null)
                    Core.ObjectPoolManager.Instance?.Despawn(key, vfx, 3f);
            }

            if (phase.phaseTransitionSound != null)
                AudioSource.PlayClipAtPoint(phase.phaseTransitionSound, transform.position);

            // Phase transition animation
            if (_animator != null)
                _animator.SetTrigger("PhaseTransition");

            // Brief invincibility during transition
            yield return new WaitForSeconds(1.5f);

            // Apply new phase
            CurrentPhaseIndex = newPhaseIndex;
            ApplyPhase(newPhaseIndex);

            onPhaseChanged?.Invoke(newPhaseIndex);

            _agent.isStopped    = false;
            _phaseTransitioning = false;
        }

        private void ApplyPhase(int phaseIndex)
        {
            if (phaseIndex < 0 || phaseIndex >= phases.Count) return;
            var phase = phases[phaseIndex];

            _ai.attackDamage    = phase.attackDamage;
            _ai.attackCooldown  = phase.attackCooldown;
            _ai.chaseSpeed      = phase.chaseSpeed;
            _ai.aggressionLevel = phase.aggressionLevel;

            _specialTimer = phase.specialCooldown * 0.3f; // Start partially ready
            _summonTimer  = phase.summonCooldown  * 0.5f;
        }

        public BossPhaseConfig GetCurrentPhase()
        {
            if (CurrentPhaseIndex < 0 || CurrentPhaseIndex >= phases.Count)
                return null;
            return phases[CurrentPhaseIndex];
        }

        // --------------------------------------------------
        //  Special Attack — AoE slam / ranged blast
        // --------------------------------------------------
        private IEnumerator SpecialAttackRoutine(BossPhaseConfig phase)
        {
            _isSpecialAttacking = true;
            _agent.isStopped = true;

            // Animation
            if (_animator != null && !string.IsNullOrEmpty(phase.specialAnimTrigger))
                _animator.SetTrigger(phase.specialAnimTrigger);

            // Wind-up
            yield return new WaitForSeconds(phase.specialWindup);

            // VFX
            if (phase.specialAttackVFX != null)
            {
                string key = $"BossSpecial{CurrentPhaseIndex}VFX";
                Core.ObjectPoolManager.Instance?.RegisterPool(key, phase.specialAttackVFX, 3);
                var vfx = Core.ObjectPoolManager.Instance?.Spawn(key, transform.position, Quaternion.identity);
                if (vfx != null)
                    Core.ObjectPoolManager.Instance?.Despawn(key, vfx, 2.5f);
            }

            if (phase.specialAttackSound != null)
                AudioSource.PlayClipAtPoint(phase.specialAttackSound, transform.position);

            // Damage
            var hits = Physics.OverlapSphere(transform.position, phase.specialRadius);
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;
                var playerHealth = hit.GetComponent<vHealthController>();
                if (playerHealth == null || playerHealth.isDead) continue;

                float finalDmg = phase.specialDamage *
                                 (IsEnraged ? enrageDamageMult : 1f);

                var dmg = new vDamage((int)finalDmg)
                {
                    damageType    = _ai.attackDamageType.ToString(),
                    hitReaction   = false,
                    activeRagdoll = false,
                    sender        = transform
                };
                playerHealth.TakeDamage(dmg);

                // Status effect based on damage type
                var statusType = Core.DamageTypeHelper.GetDefaultStatus(_ai.attackDamageType);
                if (statusType != Core.StatusEffectType.None)
                    Core.StatusEffectManager.Instance?.Apply(
                        playerHealth, statusType, 3f, 2, transform);
            }

            _specialTimer       = phase.specialCooldown;
            _isSpecialAttacking = false;
            _agent.isStopped    = false;
        }

        // --------------------------------------------------
        //  Summon Adds
        // --------------------------------------------------
        private void SummonAdds(BossPhaseConfig phase)
        {
            if (string.IsNullOrEmpty(phase.summonPoolKey)) return;
            var pool = Core.ObjectPoolManager.Instance;
            if (pool == null) return;

            if (_animator != null)
                _animator.SetTrigger("Summon");

            for (int i = 0; i < phase.summonCount; i++)
            {
                // Spawn around the boss in a circle
                float angle = (360f / phase.summonCount) * i;
                float rad   = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(rad) * phase.summonRadius,
                    0f,
                    Mathf.Sin(rad) * phase.summonRadius);

                Vector3 spawnPos = transform.position + offset;

                NavMeshHit navHit;
                if (NavMesh.SamplePosition(spawnPos, out navHit, 3f, NavMesh.AllAreas))
                    spawnPos = navHit.position;

                var add = pool.Spawn(phase.summonPoolKey, spawnPos, Quaternion.identity);
                if (add != null)
                {
                    // Wake up the add immediately
                    var addAI = add.GetComponent<EnemyAI>();
                    if (addAI != null)
                        addAI.ChangeState(AIStateType.Chase);
                }
            }

            Debug.Log($"[BossAI] {bossName} summoned {phase.summonCount} adds!");
        }

        // --------------------------------------------------
        //  Enrage
        // --------------------------------------------------
        private void Enrage()
        {
            IsEnraged = true;

            _ai.chaseSpeed      *= enrageSpeedMult;
            _ai.attackDamage    *= enrageDamageMult;
            _ai.aggressionLevel  = 2.5f;

            if (_animator != null)
                _animator.SetBool("isEnraged", true);

            Debug.Log($"[BossAI] {bossName} is ENRAGED!");
        }

        // --------------------------------------------------
        //  Death override — notify systems
        // --------------------------------------------------
        public void OnBossDefeated()
        {
            onBossDefeated?.Invoke();
            Debug.Log($"[BossAI] {bossName} DEFEATED!");
        }

        // --------------------------------------------------
        //  Debug gizmos
        // --------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            var phase = GetCurrentPhase();
            if (phase == null) return;

            if (phase.hasSpecialAttack)
            {
                Gizmos.color = new Color(1f, 0f, 0.5f, 0.15f);
                Gizmos.DrawWireSphere(transform.position, phase.specialRadius);
            }

            if (phase.canSummonAdds)
            {
                Gizmos.color = new Color(0.5f, 0f, 1f, 0.1f);
                Gizmos.DrawWireSphere(transform.position, phase.summonRadius);
            }
        }
    }
}
