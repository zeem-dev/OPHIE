// ============================================================
//  OPHIO — EnemyAI
//  Enemy AI State Machine
//  Main AI controller. Drives state machine, batched 0.2s tick,
//  detection, aggression scaling, and damage dealing.
//  Hooks into Invector's vHealthController for damage/death.
//  Attach to every enemy GameObject.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Invector;

namespace OPHIO.AI
{
    public class EnemyAI : MonoBehaviour
    {
        // --------------------------------------------------
        //  Inspector Settings
        // --------------------------------------------------
        [Header("── DETECTION ──")]
        [Tooltip("Max distance to detect the player visually")]
        public float detectionRange        = 15f;
        [Tooltip("Max distance to detect the player via sound (no LOS needed)")]
        public float soundDetectionRange   = 8f;
        [Tooltip("Seconds without visual contact before returning to Idle")]
        public float losePlayerTimeout     = 5f;
        [Tooltip("LayerMask for line-of-sight obstacles")]
        public LayerMask obstacleLayers;

        [Header("── MOVEMENT ──")]
        public float chaseSpeed            = 4.5f;

        [Header("── ATTACK ──")]
        [Tooltip("Range at which enemy switches from Chase to Attack")]
        public float attackRange           = 2f;
        [Tooltip("Base melee damage")]
        public float attackDamage          = 15f;
        [Tooltip("Seconds before damage lands after animation starts")]
        public float attackWindup          = 0.3f;
        [Tooltip("Seconds after damage before next attack can begin")]
        public float attackRecovery        = 0.4f;
        [Tooltip("Minimum seconds between attacks")]
        public float attackCooldown        = 1.5f;
        [Tooltip("Damage type for this enemy's attacks")]
        public Core.DamageType attackDamageType = Core.DamageType.Physical;

        [Header("── AI TICK ──")]
        [Tooltip("How often the AI state machine executes (seconds). 0.2 = 5 times/sec.")]
        public float tickInterval          = 0.2f;

        [Header("── AGGRESSION ──")]
        [Tooltip("0=passive, 1=normal, 2=aggressive. Scales detection and attack rate.")]
        [Range(0f, 2f)]
        public float aggressionLevel      = 1f;

        [Header("── ENEMY TYPE ──")]
        public EnemyType enemyType         = EnemyType.Normal;

        [Header("── DEATH ──")]
        [Tooltip("Seconds before corpse is despawned/pooled after death")]
        public float corpseDuration        = 4f;

        // --------------------------------------------------
        //  Public Properties (read by states)
        // --------------------------------------------------
        public NavMeshAgent      Agent            { get; private set; }
        public Animator          Animator          { get; private set; }
        public vHealthController Health            { get; private set; }
        public Transform         PlayerTransform   { get; private set; }
        public AIStateType       CurrentStateType  { get; private set; }
        public float             CurrentStunDuration { get; set; }

        // Expose inspector fields as properties for states
        public float DetectionRange      => detectionRange * aggressionLevel;
        public float SoundDetectionRange => soundDetectionRange * aggressionLevel;
        public float LosePlayerTimeout   => losePlayerTimeout;
        public float ChaseSpeed          => chaseSpeed;
        public float AttackRange         => attackRange;
        public float AttackWindup        => attackWindup;
        public float AttackRecovery      => attackRecovery;
        public float AttackCooldown      => attackCooldown / Mathf.Max(aggressionLevel, 0.5f);
        public float TickInterval        => tickInterval;

        // --------------------------------------------------
        //  Events (for Arena/Wave manager to listen to)
        // --------------------------------------------------
        public System.Action<EnemyAI> onDeath;

        // --------------------------------------------------
        //  State Machine internals
        // --------------------------------------------------
        private Dictionary<AIStateType, EnemyAIState> _states;
        private EnemyAIState _currentState;
        private Coroutine    _tickRoutine;

        // --------------------------------------------------
        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            Agent    = GetComponent<NavMeshAgent>();
            Animator = GetComponent<Animator>();
            Health   = GetComponent<vHealthController>();
            InitializeStates();
        }

        private void OnEnable()
        {
            ResetAI();

            // Begin staggered tick
            if (_tickRoutine == null)
            {
                _tickRoutine = StartCoroutine(AITickRoutine());
            }
        }

        private void OnDisable()
        {
            // Stop tick
            if (_tickRoutine != null)
            {
                StopCoroutine(_tickRoutine);
                _tickRoutine = null;
            }
        }

        private void Start()
        {
            // Find player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) PlayerTransform = player.transform;

            InitializeStates();

            // Wire Invector death callback
            if (Health != null)
            {
                Health.onDead.AddListener(OnInvectorDead);
                Health.onReceiveDamage.AddListener(OnDamageReceived);
            }

            // Start in Idle only if no state has been entered
            if (_currentState == null)
            {
                ChangeState(AIStateType.Idle);
            }

            // Begin staggered tick if not already running
            if (_tickRoutine == null)
            {
                _tickRoutine = StartCoroutine(AITickRoutine());
            }
        }

        public void InitializeStates()
        {
            if (_states != null) return;

            _states = new Dictionary<AIStateType, EnemyAIState>
            {
                { AIStateType.Idle,    new IdleState(this)    },
                { AIStateType.Chase,   new ChaseState(this)   },
                { AIStateType.Attack,  new AttackState(this)  },
                { AIStateType.Stunned, new StunnedState(this) },
                { AIStateType.Dead,    new DeadState(this)    }
            };
        }

        public void ResetAI()
        {
            InitializeStates();
            CurrentStateType = AIStateType.Idle;
            _currentState = _states[AIStateType.Idle];
            CurrentStunDuration = 0f;

            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.Rebind();
            }
        }

        // --------------------------------------------------
        //  Staggered AI tick — NOT per-frame
        //  Random offset on start to prevent all AIs ticking
        //  on the same frame
        // --------------------------------------------------
        private IEnumerator AITickRoutine()
        {
            // Random initial delay to stagger across enemies
            yield return new WaitForSeconds(Random.Range(0f, tickInterval));

            var wait = new WaitForSeconds(tickInterval);
            while (true)
            {
                if (_currentState != null && CurrentStateType != AIStateType.Dead)
                    _currentState.Execute();
                yield return wait;
            }
        }

        // --------------------------------------------------
        //  State transitions
        // --------------------------------------------------
        public void ChangeState(AIStateType newState)
        {
            InitializeStates();

            if (CurrentStateType == AIStateType.Dead && newState != AIStateType.Dead)
                return; // can't exit dead

            _currentState?.Exit();
            CurrentStateType = newState;
            _currentState    = _states[newState];
            _currentState.Enter();
        }

        // --------------------------------------------------
        //  Detection helpers
        // --------------------------------------------------
        public bool HasLineOfSight()
        {
            if (PlayerTransform == null) return false;
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 target = PlayerTransform.position + Vector3.up * 1f;
            Vector3 dir    = target - origin;

            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit,
                                dir.magnitude, obstacleLayers))
            {
                // Hit an obstacle before reaching the player
                return false;
            }
            return true;
        }

        // --------------------------------------------------
        //  Damage dealing
        // --------------------------------------------------
        public void DealDamageToPlayer()
        {
            if (PlayerTransform == null) return;

            var playerHealth = PlayerTransform.GetComponent<vHealthController>();
            if (playerHealth == null || playerHealth.isDead) return;

            float scaledDamage = attackDamage * aggressionLevel;

            var dmg = new vDamage((int)scaledDamage)
            {
                damageType  = attackDamageType.ToString(),
                hitReaction = false,
                activeRagdoll = false,
                sender      = transform
            };
            playerHealth.TakeDamage(dmg);

            // Apply status effect based on damage type
            var statusType = Core.DamageTypeHelper.GetDefaultStatus(attackDamageType);
            if (statusType != Core.StatusEffectType.None &&
                Core.StatusEffectManager.Instance != null)
            {
                Core.StatusEffectManager.Instance.Apply(
                    playerHealth, statusType, 2f, 1, transform);
            }
        }

        // --------------------------------------------------
        //  Invector callbacks
        // --------------------------------------------------
        private void OnInvectorDead(GameObject go)
        {
            ChangeState(AIStateType.Dead);
        }

        private void OnDamageReceived(vDamage damage)
        {
            // Any damage → alert this enemy (enter chase if idle)
            if (CurrentStateType == AIStateType.Idle)
                ChangeState(AIStateType.Chase);

            // Check for Shock stun from StatusEffectManager
            if (Health != null &&
                Core.StatusEffectManager.Instance != null &&
                Core.StatusEffectManager.Instance.HasEffect(Health, Core.StatusEffectType.Shock))
            {
                if (CurrentStateType != AIStateType.Stunned &&
                    CurrentStateType != AIStateType.Dead)
                {
                    CurrentStunDuration = 1.5f; // Shock default stun
                    ChangeState(AIStateType.Stunned);
                }
            }
        }

        // --------------------------------------------------
        //  Stun from external source (StatusEffectManager)
        // --------------------------------------------------
        public void ApplyStun(float duration)
        {
            if (CurrentStateType == AIStateType.Dead) return;
            CurrentStunDuration = duration;
            ChangeState(AIStateType.Stunned);
        }

        // --------------------------------------------------
        //  Death cleanup
        // --------------------------------------------------
        public void OnEnemyDeath()
        {
            // Stop tick
            if (_tickRoutine != null)
            {
                StopCoroutine(_tickRoutine);
                _tickRoutine = null;
            }

            // Cleanse all status effects
            if (Health != null)
                Core.StatusEffectManager.Instance?.CleanseAll(Health);

            // Notify listeners (ArenaManager, wave system, etc.)
            onDeath?.Invoke(this);

            // Destroy/pool after delay
            StartCoroutine(DeathCleanup());
        }

        private IEnumerator DeathCleanup()
        {
            yield return new WaitForSeconds(corpseDuration);

            // Try pool return first, fallback to destroy
            var pool = Core.ObjectPoolManager.Instance;
            if (pool != null)
                pool.DespawnAuto(gameObject);
            else
                Destroy(gameObject);
        }

        // --------------------------------------------------
        //  Debug gizmos
        // --------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Sound detection range
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, soundDetectionRange);

            // Attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
