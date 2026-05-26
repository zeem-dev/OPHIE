// ============================================================
//  OPHIO — SpecialEnemyBehavior (Exploder variant)
//  Enemy AI — Special Type
//  Runs at player and detonates on contact or when HP is low.
//  AoE explosion applies damage + Burn status to player.
//  Also has a Screamer mode toggle — screams to buff nearby
//  allies' aggression instead of exploding.
//  Attach alongside EnemyAI on Special enemy prefab.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Invector;

namespace OPHIO.AI
{
    public enum SpecialVariant
    {
        Exploder,   // Suicide bomb — runs + detonates
        Screamer    // Buff allies — scream AoE increases aggression
    }

    [RequireComponent(typeof(EnemyAI))]
    public class SpecialEnemyBehavior : MonoBehaviour
    {
        [Header("Variant")]
        public SpecialVariant variant = SpecialVariant.Exploder;

        // --------------------------------------------------
        //  Exploder Settings
        // --------------------------------------------------
        [Header("── EXPLODER ──")]
        [Tooltip("Explosion radius")]
        public float explosionRadius     = 5f;
        [Tooltip("Explosion damage")]
        public float explosionDamage     = 50f;
        [Tooltip("Explode when HP falls below this percentage")]
        [Range(0f, 0.5f)]
        public float explodeHPThreshold  = 0.2f;
        [Tooltip("Distance to player that triggers detonation")]
        public float detonateRange       = 1.5f;
        [Tooltip("Wind-up time before explosion")]
        public float fuseTime            = 1.0f;
        [Tooltip("Speed multiplier when rushing toward player")]
        public float rushSpeedMult       = 1.8f;
        [Tooltip("Burn duration applied by explosion")]
        public float burnDuration        = 4f;
        [Tooltip("Burn stacks applied by explosion")]
        public int   burnStacks          = 2;

        // --------------------------------------------------
        //  Screamer Settings
        // --------------------------------------------------
        [Header("── SCREAMER ──")]
        [Tooltip("Scream radius — allies within this are buffed")]
        public float screamRadius        = 12f;
        [Tooltip("Aggression boost applied to nearby allies")]
        public float aggressionBoost     = 0.5f;
        [Tooltip("Speed boost applied to nearby allies")]
        public float speedBoost          = 1.5f;
        [Tooltip("Scream cooldown")]
        public float screamCooldown      = 8f;
        [Tooltip("Scream duration — how long the buff lasts")]
        public float screamBuffDuration  = 6f;

        [Header("VFX")]
        public GameObject explosionVFXPrefab;
        public GameObject fuseVFXPrefab;   // glow/sparks before explosion
        public GameObject screamVFXPrefab;

        [Header("Audio")]
        public AudioClip explosionSound;
        public AudioClip fuseSound;
        public AudioClip screamSound;

        private EnemyAI           _ai;
        private NavMeshAgent      _agent;
        private vHealthController _health;
        private Animator          _animator;

        private bool  _hasExploded;
        private bool  _fuseStarted;
        private float _screamTimer;

        private const string k_ExplosionKey = "ExploderVFX";
        private const string k_FuseKey      = "ExploderFuse";
        private const string k_ScreamKey    = "ScreamerVFX";

        private void Awake()
        {
            _ai       = GetComponent<EnemyAI>();
            _agent    = GetComponent<NavMeshAgent>();
            _health   = GetComponent<vHealthController>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            _screamTimer = screamCooldown * 0.5f;

            // Register pools
            var pool = Core.ObjectPoolManager.Instance;
            if (pool != null)
            {
                if (explosionVFXPrefab != null) pool.RegisterPool(k_ExplosionKey, explosionVFXPrefab, 3);
                if (fuseVFXPrefab      != null) pool.RegisterPool(k_FuseKey,      fuseVFXPrefab, 3);
                if (screamVFXPrefab    != null) pool.RegisterPool(k_ScreamKey,    screamVFXPrefab, 2);
            }
        }

        private void Update()
        {
            if (_ai.CurrentStateType == AIStateType.Dead || _hasExploded) return;

            if (variant == SpecialVariant.Exploder)
                UpdateExploder();
            else
                UpdateScreamer();
        }

        // ==========================================================
        //  EXPLODER LOGIC
        // ==========================================================
        private void UpdateExploder()
        {
            if (_fuseStarted) return;

            // Rush speed boost during chase
            if (_ai.CurrentStateType == AIStateType.Chase)
                _agent.speed = _ai.ChaseSpeed * rushSpeedMult;

            // Proximity detonation
            if (_ai.PlayerTransform != null)
            {
                float dist = Vector3.Distance(transform.position,
                                               _ai.PlayerTransform.position);
                if (dist <= detonateRange)
                {
                    StartCoroutine(DetonationRoutine());
                    return;
                }
            }

            // Low HP detonation
            if (_health != null && _health.currentHealth > 0)
            {
                float hpPercent = (float)_health.currentHealth / _health.maxHealth;
                if (hpPercent <= explodeHPThreshold)
                {
                    StartCoroutine(DetonationRoutine());
                }
            }
        }

        private IEnumerator DetonationRoutine()
        {
            if (_fuseStarted) yield break;
            _fuseStarted = true;

            _agent.isStopped = true;

            // Fuse animation + VFX
            if (_animator != null) _animator.SetTrigger("Fuse");
            if (fuseSound != null)
                AudioSource.PlayClipAtPoint(fuseSound, transform.position);

            SpawnPooled(k_FuseKey, transform.position, fuseTime + 0.5f);

            yield return new WaitForSeconds(fuseTime);

            // EXPLODE
            Explode();
        }

        private void Explode()
        {
            _hasExploded = true;

            // Explosion VFX
            SpawnPooled(k_ExplosionKey, transform.position, 3f);
            if (explosionSound != null)
                AudioSource.PlayClipAtPoint(explosionSound, transform.position);

            // Damage everything in radius
            var hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;
                if (hit.gameObject == gameObject) continue; // Don't damage self

                var dmg = new vDamage((int)explosionDamage)
                {
                    damageType    = "Explosive",
                    hitReaction   = false,
                    activeRagdoll = false,
                    sender        = transform
                };
                health.TakeDamage(dmg);

                // Apply Burn to player
                if (hit.CompareTag("Player"))
                {
                    Core.StatusEffectManager.Instance?.Apply(
                        health, Core.StatusEffectType.Burn,
                        burnDuration, burnStacks, transform);
                }
            }

            // Kill self
            if (_health != null && !_health.isDead)
            {
                var selfDmg = new vDamage(9999)
                {
                    damageType  = "Explosive",
                    hitReaction = false,
                    sender      = transform
                };
                _health.TakeDamage(selfDmg);
            }
        }

        // ==========================================================
        //  SCREAMER LOGIC
        // ==========================================================
        private void UpdateScreamer()
        {
            _screamTimer -= Time.deltaTime;
            if (_screamTimer > 0f) return;

            // Only scream while chasing or attacking
            if (_ai.CurrentStateType != AIStateType.Chase &&
                _ai.CurrentStateType != AIStateType.Attack) return;

            StartCoroutine(ScreamRoutine());
            _screamTimer = screamCooldown;
        }

        private IEnumerator ScreamRoutine()
        {
            // Animation
            if (_animator != null) _animator.SetTrigger("Scream");
            _agent.isStopped = true;

            // Scream VFX
            SpawnPooled(k_ScreamKey, transform.position + Vector3.up, 2f);

            if (screamSound != null)
                AudioSource.PlayClipAtPoint(screamSound, transform.position);

            yield return new WaitForSeconds(0.5f);

            // Buff all nearby allies
            var hits = Physics.OverlapSphere(transform.position, screamRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var allyAI = hit.GetComponent<EnemyAI>();
                if (allyAI == null || allyAI.CurrentStateType == AIStateType.Dead) continue;

                allyAI.aggressionLevel += aggressionBoost;

                // Speed boost via NavMeshAgent
                var allyAgent = allyAI.GetComponent<NavMeshAgent>();
                if (allyAgent != null)
                {
                    float boostedSpeed = allyAgent.speed + speedBoost;
                    allyAgent.speed = boostedSpeed;

                    // Revert after duration
                    StartCoroutine(RevertSpeedBoost(allyAgent, boostedSpeed - speedBoost,
                                                    screamBuffDuration));
                }

                // Wake up idle allies
                if (allyAI.CurrentStateType == AIStateType.Idle)
                    allyAI.ChangeState(AIStateType.Chase);
            }

            _agent.isStopped = false;
        }

        private IEnumerator RevertSpeedBoost(NavMeshAgent agent, float originalSpeed, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (agent != null) agent.speed = originalSpeed;
        }

        // --------------------------------------------------
        //  Pool helper
        // --------------------------------------------------
        private void SpawnPooled(string key, Vector3 pos, float lifetime)
        {
            var pool = Core.ObjectPoolManager.Instance;
            if (pool == null) return;
            var obj = pool.Spawn(key, pos, Quaternion.identity);
            if (obj != null) pool.Despawn(key, obj, lifetime);
        }

        // --------------------------------------------------
        //  Debug gizmos
        // --------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            if (variant == SpecialVariant.Exploder)
            {
                Gizmos.color = new Color(1f, 0.4f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, explosionRadius);
                Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
                Gizmos.DrawWireSphere(transform.position, detonateRange);
            }
            else
            {
                Gizmos.color = new Color(0.8f, 0f, 1f, 0.15f);
                Gizmos.DrawWireSphere(transform.position, screamRadius);
            }
        }
    }
}
