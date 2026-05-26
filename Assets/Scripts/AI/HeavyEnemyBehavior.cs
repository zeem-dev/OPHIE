// ============================================================
//  OPHIO — HeavyEnemyBehavior
//  Enemy AI — Heavy Type
//  Tank enemy — slow, devastating melee, staggers player.
//  Has a ground slam AoE, armor (damage reduction),
//  and doesn't flinch from small hits.
//  Attach alongside EnemyAI on Heavy enemy prefab.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Invector;

namespace OPHIO.AI
{
    [RequireComponent(typeof(EnemyAI))]
    public class HeavyEnemyBehavior : MonoBehaviour
    {
        [Header("Armor")]
        [Tooltip("Flat damage reduction applied to all incoming damage")]
        public float damageReduction     = 5f;
        [Tooltip("Percentage of hits that DON'T stagger this enemy (0-1)")]
        [Range(0f, 1f)]
        public float hyperArmorChance    = 0.7f;

        [Header("Ground Slam — AoE Attack")]
        [Tooltip("Radius of the ground slam AoE")]
        public float slamRadius          = 4f;
        [Tooltip("Damage of the ground slam")]
        public float slamDamage          = 40f;
        [Tooltip("Cooldown between slams")]
        public float slamCooldown        = 8f;
        [Tooltip("Wind-up time before slam impact")]
        public float slamWindup          = 0.8f;
        [Tooltip("Stagger duration applied to the player on slam hit")]
        public float slamStaggerDuration = 1.0f;

        [Header("Charge Attack")]
        [Tooltip("Distance threshold to trigger a charge")]
        public float chargeMinDist       = 6f;
        [Tooltip("Speed during charge")]
        public float chargeSpeed         = 10f;
        [Tooltip("Cooldown between charges")]
        public float chargeCooldown      = 10f;
        [Tooltip("Damage on charge collision")]
        public float chargeDamage        = 30f;

        [Header("VFX")]
        public GameObject slamVFXPrefab;
        public GameObject chargeVFXPrefab;

        [Header("Audio")]
        public AudioClip slamSound;
        public AudioClip chargeSound;
        public AudioClip footstepHeavySound;

        private EnemyAI         _ai;
        private NavMeshAgent    _agent;
        private vHealthController _health;
        private Animator        _animator;

        private float _slamTimer;
        private float _chargeTimer;
        private bool  _isSlamming;
        private bool  _isCharging;

        private const string k_SlamVFXKey   = "HeavySlamVFX";
        private const string k_ChargeVFXKey = "HeavyChargeVFX";

        private void Awake()
        {
            _ai       = GetComponent<EnemyAI>();
            _agent    = GetComponent<NavMeshAgent>();
            _health   = GetComponent<vHealthController>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            _slamTimer   = slamCooldown * 0.5f; // Start half-ready
            _chargeTimer = chargeCooldown;

            // Register pools
            if (slamVFXPrefab != null)
                Core.ObjectPoolManager.Instance?.RegisterPool(k_SlamVFXKey, slamVFXPrefab, 3);
            if (chargeVFXPrefab != null)
                Core.ObjectPoolManager.Instance?.RegisterPool(k_ChargeVFXKey, chargeVFXPrefab, 2);

            // Hook into damage to apply armor
            if (_health != null)
                _health.onReceiveDamage.AddListener(OnDamageWithArmor);
        }

        private void Update()
        {
            if (_ai.CurrentStateType == AIStateType.Dead) return;

            _slamTimer   -= Time.deltaTime;
            _chargeTimer -= Time.deltaTime;

            if (_ai.CurrentStateType == AIStateType.Attack && !_isSlamming)
            {
                TryGroundSlam();
            }

            if (_ai.CurrentStateType == AIStateType.Chase && !_isCharging)
            {
                TryCharge();
            }
        }

        // --------------------------------------------------
        //  Armor — reduce incoming damage, hyper armor
        // --------------------------------------------------
        private void OnDamageWithArmor(vDamage damage)
        {
            // Flat damage reduction
            damage.damageValue = Mathf.Max(1, damage.damageValue - (int)damageReduction);

            // Hyper armor — suppress hit reaction
            if (Random.value < hyperArmorChance)
                damage.hitReaction = false;
        }

        // --------------------------------------------------
        //  Ground Slam — AoE around self
        // --------------------------------------------------
        private void TryGroundSlam()
        {
            if (_slamTimer > 0f) return;
            if (_ai.PlayerTransform == null) return;

            float dist = Vector3.Distance(transform.position, _ai.PlayerTransform.position);
            if (dist > slamRadius) return;

            StartCoroutine(GroundSlamRoutine());
        }

        private IEnumerator GroundSlamRoutine()
        {
            _isSlamming = true;
            _agent.isStopped = true;

            if (_animator != null)
                _animator.SetTrigger("GroundSlam");

            yield return new WaitForSeconds(slamWindup);

            // AoE damage
            var hits = Physics.OverlapSphere(transform.position, slamRadius);
            foreach (var hit in hits)
            {
                // Check if it's the player
                if (!hit.CompareTag("Player")) continue;
                var playerHealth = hit.GetComponent<vHealthController>();
                if (playerHealth == null || playerHealth.isDead) continue;

                var dmg = new vDamage((int)slamDamage)
                {
                    damageType    = "Physical",
                    hitReaction   = false,
                    activeRagdoll = false,
                    sender        = transform
                };
                playerHealth.TakeDamage(dmg);
            }

            // VFX
            SpawnPooled(k_SlamVFXKey, transform.position, 2f);

            // SFX
            if (slamSound != null)
                AudioSource.PlayClipAtPoint(slamSound, transform.position);

            _slamTimer  = slamCooldown;
            _isSlamming = false;
        }

        // --------------------------------------------------
        //  Charge Attack — bull rush toward player
        // --------------------------------------------------
        private void TryCharge()
        {
            if (_chargeTimer > 0f) return;
            if (_ai.PlayerTransform == null) return;

            float dist = Vector3.Distance(transform.position, _ai.PlayerTransform.position);
            if (dist < chargeMinDist || dist > _ai.DetectionRange) return;

            StartCoroutine(ChargeRoutine());
        }

        private IEnumerator ChargeRoutine()
        {
            _isCharging = true;

            if (_animator != null)
                _animator.SetTrigger("Charge");

            if (chargeSound != null)
                AudioSource.PlayClipAtPoint(chargeSound, transform.position);

            SpawnPooled(k_ChargeVFXKey, transform.position, 1.5f);

            // Wind-up pause
            _agent.isStopped = true;
            yield return new WaitForSeconds(0.4f);

            // Charge toward player's position
            Vector3 targetPos = _ai.PlayerTransform != null
                ? _ai.PlayerTransform.position
                : transform.position + transform.forward * chargeMinDist;

            _agent.isStopped = false;
            _agent.speed     = chargeSpeed;
            _agent.SetDestination(targetPos);

            // Wait until near target or timeout
            float timer = 0f;
            while (timer < 2f)
            {
                if (_agent.remainingDistance <= _ai.AttackRange)
                {
                    // Collision — deal damage
                    if (_ai.PlayerTransform != null)
                    {
                        float dist = Vector3.Distance(transform.position,
                                                       _ai.PlayerTransform.position);
                        if (dist <= _ai.AttackRange * 1.5f)
                        {
                            var playerHealth = _ai.PlayerTransform.GetComponent<vHealthController>();
                            if (playerHealth != null && !playerHealth.isDead)
                            {
                                var dmg = new vDamage((int)chargeDamage)
                                {
                                    damageType    = "Physical",
                                    hitReaction   = false,
                                    activeRagdoll = false,
                                    sender        = transform
                                };
                                playerHealth.TakeDamage(dmg);
                            }
                        }
                    }
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // Reset speed
            _agent.speed = _ai.ChaseSpeed;
            _chargeTimer = chargeCooldown;
            _isCharging  = false;
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
    }
}
