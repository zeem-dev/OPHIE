// ============================================================
// OPHIO - FlexCharacter
// Main controller for Flex (Subject #5).
// Handles Metal Assimilation, Brawling, and Resilience.
// ============================================================

using System.Collections;
using UnityEngine;
using Invector;
using Invector.vCharacterController;

namespace OPHIO.Characters
{
    public class FlexCharacter : MonoBehaviour
    {
        private Core.EnergyManager _energy;
        private vHealthController _health;
        private vThirdPersonController _tpc;
        private Core.AbilityExecutor _executor;
        private Animator _animator;

        [Header("Spinal Core Anchor Passive")]
        [Tooltip("Health percentage below which the passive activates.")]
        public float resilienceThreshold = 0.2f; // 20%
        [Tooltip("Damage reduction applied when below threshold.")]
        public float resilienceDamageReduction = 0.5f; // 50%

        [Header("Full Plating")]
        public GameObject platingVFX;
        private bool _isPlated;
        private float _platingMeleeBonus = 1.5f; // +50% melee damage

        [Header("Metal Sprint")]
        public float dashForce = 20f;
        
        [Header("Structural Reinforcement")]
        public float healAmount = 50f;

        [Header("Total Metalization (Super)")]
        public GameObject metalizationVFX;
        private bool _isSuperActive;
        private bool _inWithdrawal;

        private float _originalWalkSpeed;
        private float _originalRunSpeed;
        private float _originalSprintSpeed;

        private void Awake()
        {
            _energy = GetComponent<Core.EnergyManager>();
            _health = GetComponent<vHealthController>();
            _tpc = GetComponent<vThirdPersonController>();
            _executor = GetComponent<Core.AbilityExecutor>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (_tpc != null)
            {
                _originalWalkSpeed = _tpc.freeSpeed.walkSpeed;
                _originalRunSpeed = _tpc.freeSpeed.runningSpeed;
                _originalSprintSpeed = _tpc.freeSpeed.sprintSpeed;

                _health.onStartReceiveDamage.AddListener(OnDamageReceived);
            }

            var meleeManager = GetComponent<Invector.vMelee.vMeleeManager>();
            if (meleeManager != null)
                meleeManager.onDamageHit.AddListener(OnMeleeHit);
        }

        private void OnDamageReceived(vDamage damage)
        {
            if (damage == null || _health == null) return;
            
            if (_energy != null) _energy.SetCombatState(true);

            // Total Metalization: Near invincible
            if (_isSuperActive)
            {
                damage.damageValue = 1; // Take only 1 damage
                return;
            }

            // Withdrawal Vulnerability
            if (_inWithdrawal)
            {
                damage.damageValue = (int)(damage.damageValue * 1.5f); // 50% more damage
                return;
            }

            // Full Plating Damage Reduction
            if (_isPlated)
            {
                damage.damageValue = (int)(damage.damageValue * 0.4f); // 60% reduction
            }

            // Spinal Core Anchor Passive
            float healthPercent = (float)_health.currentHealth / _health.maxHealth;
            if (healthPercent <= resilienceThreshold)
            {
                damage.damageValue = (int)(damage.damageValue * (1f - resilienceDamageReduction));
            }
        }

        private void OnMeleeHit(Invector.vMelee.vHitInfo hitInfo)
        {
            if (_energy != null) _energy.SetCombatState(true);

            var health = hitInfo.targetCollider?.GetComponent<vHealthController>();
            if (health != null && !health.isDead)
            {
                // Full Plating Bonus Damage
                if (_isPlated)
                {
                    if (hitInfo.attackObject != null && hitInfo.attackObject.damage != null)
                    {
                        hitInfo.attackObject.damage.damageValue = hitInfo.attackObject.damage.damageValue * _platingMeleeBonus;
                    }
                }

                // Super Explosive Melee
                if (_isSuperActive)
                {
                    // Convert melee attack into an explosion
                    var hits = Physics.OverlapSphere(hitInfo.targetCollider.transform.position, 3f, _executor.enemyLayer);
                    foreach (var hit in hits)
                    {
                        var splashHealth = hit.GetComponent<vHealthController>();
                        if (splashHealth != null && !splashHealth.isDead)
                        {
                            var vDmg = new vDamage(50); // Massive explosive damage
                            vDmg.damageType = "Explosive";
                            vDmg.activeRagdoll = true;
                            vDmg.sender = transform;
                            splashHealth.TakeDamage(vDmg);
                        }
                    }

                    // Explode VFX
                    string poolKey = "FlexExplosionVFX";
                    if (Core.ObjectPoolManager.Instance != null)
                    {
                        var vfx = Core.ObjectPoolManager.Instance.Spawn(poolKey, hitInfo.targetCollider.transform.position, Quaternion.identity);
                        if (vfx != null) Core.ObjectPoolManager.Instance.Despawn(poolKey, vfx, 2f);
                    }
                }
            }
        }

        // --------------------------------------------------
        // Abilities dispatched from AbilityExecutor
        // --------------------------------------------------
        private void OnAbilityActivated(Core.AbilityData ability)
        {
            if (_energy != null) _energy.SetCombatState(true);

            switch (ability.abilityName)
            {
                case "Metal Slam": StartCoroutine(MetalSlamRoutine(ability)); break;
                case "Alkaline Blast": StartCoroutine(AlkalineBlastRoutine(ability)); break;
                case "Full Plating": StartCoroutine(FullPlatingRoutine()); break;
                case "Metal Sprint": StartCoroutine(MetalSprintRoutine(ability)); break;
                case "Structural Reinforcement": StructuralReinforcement(); break;
                case "Total Metalization": StartCoroutine(TotalMetalizationRoutine()); break;
            }
        }

        // --------------------------------------------------
        // Metal Slam
        // --------------------------------------------------
        private IEnumerator MetalSlamRoutine(Core.AbilityData ability)
        {
            if (_animator != null) _animator.SetTrigger("StrongAttack"); // Or a custom trigger
            
            // Wait for slam animation impact
            yield return new WaitForSeconds(0.5f);

            var hits = Physics.OverlapSphere(transform.position, ability.range, _executor.enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;

                var vDmg = ability.BuildDamage().ToVDamage(transform);
                vDmg.activeRagdoll = true; // Knockdown
                health.TakeDamage(vDmg);
            }
        }

        // --------------------------------------------------
        // Alkaline Blast
        // --------------------------------------------------
        private IEnumerator AlkalineBlastRoutine(Core.AbilityData ability)
        {
            var hits = Physics.OverlapSphere(transform.position, 4f, _executor.enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health != null && !health.isDead)
                {
                    health.TakeDamage(ability.BuildDamage().ToVDamage(transform));
                }
            }
            yield return null;
        }

        // --------------------------------------------------
        // Full Plating
        // --------------------------------------------------
        private IEnumerator FullPlatingRoutine()
        {
            _isPlated = true;
            if (platingVFX != null) platingVFX.SetActive(true);

            // Slow movement
            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed = _originalWalkSpeed * 0.7f;
                _tpc.freeSpeed.runningSpeed = _originalRunSpeed * 0.7f;
                _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed * 0.7f;
            }

            yield return new WaitForSeconds(8f);

            _isPlated = false;
            if (platingVFX != null) platingVFX.SetActive(false);

            // Restore movement
            if (_tpc != null && !_isSuperActive && !_inWithdrawal)
            {
                _tpc.freeSpeed.walkSpeed = _originalWalkSpeed;
                _tpc.freeSpeed.runningSpeed = _originalRunSpeed;
                _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed;
            }
        }

        // --------------------------------------------------
        // Metal Sprint
        // --------------------------------------------------
        private IEnumerator MetalSprintRoutine(Core.AbilityData ability)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(transform.forward * dashForce, ForceMode.VelocityChange);

            float duration = 1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                var hits = Physics.OverlapSphere(transform.position, 2f, _executor.enemyLayer);
                foreach (var hit in hits)
                {
                    var health = hit.GetComponent<vHealthController>();
                    if (health != null && !health.isDead)
                    {
                        var vDmg = ability.BuildDamage().ToVDamage(transform);
                        vDmg.activeRagdoll = true; // Stagger/Knockdown
                        health.TakeDamage(vDmg);
                    }
                }
                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }

        // --------------------------------------------------
        // Structural Reinforcement
        // --------------------------------------------------
        private void StructuralReinforcement()
        {
            if (_health != null)
            {
                _health.AddHealth((int)healAmount);
            }
        }

        // --------------------------------------------------
        // Total Metalization (Super)
        // --------------------------------------------------
        private IEnumerator TotalMetalizationRoutine()
        {
            _isSuperActive = true;
            if (metalizationVFX != null) metalizationVFX.SetActive(true);

            yield return new WaitForSeconds(6f);

            _isSuperActive = false;
            if (metalizationVFX != null) metalizationVFX.SetActive(false);

            // Enter Withdrawal
            _inWithdrawal = true;
            
            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed = _originalWalkSpeed * 0.5f;
                _tpc.freeSpeed.runningSpeed = _originalRunSpeed * 0.5f;
                _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed * 0.5f;
            }

            yield return new WaitForSeconds(4f); // Withdrawal duration

            _inWithdrawal = false;
            
            if (_tpc != null && !_isPlated)
            {
                _tpc.freeSpeed.walkSpeed = _originalWalkSpeed;
                _tpc.freeSpeed.runningSpeed = _originalRunSpeed;
                _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed;
            }
        }
    }
}
