// ============================================================
// OPHIO - MacCharacter
// Main controller for Mac (Subject #30).
// Handles Energy Reserves passive and tactical abilities.
// ============================================================

using System.Collections;
using UnityEngine;
using Invector;
using Invector.vCharacterController;

namespace OPHIO.Characters
{
    public class MacCharacter : MonoBehaviour
    {
        private Core.EnergyManager _energy;
        private vHealthController _health;
        private vThirdPersonController _tpc;
        private Core.AbilityExecutor _executor;

        [Header("Energy Reserves Passive")]
        [Tooltip("Percentage of incoming damage converted to energy.")]
        public float damageToEnergyRatio = 0.5f;
        [Tooltip("Max speed increase at 100% Energy.")]
        public float maxSpeedBonus = 0.3f;
        [Tooltip("Max damage resistance at 100% Energy.")]
        public float maxDamageResistance = 0.25f; // 25% reduction
        [Tooltip("Passive health regen per second at 100% Energy.")]
        public float maxHealthRegen = 2f;

        private float _originalWalkSpeed;
        private float _originalRunSpeed;
        private float _originalSprintSpeed;

        [Header("Energy Redirect")]
        public GameObject redirectVFX;
        private bool _isRedirecting;

        [Header("Overcharge Release (Super)")]
        private bool _rapidRechargeActive;

        private void Awake()
        {
            _energy = GetComponent<Core.EnergyManager>();
            _health = GetComponent<vHealthController>();
            _tpc = GetComponent<vThirdPersonController>();
            _executor = GetComponent<Core.AbilityExecutor>();
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
        }

        private void Update()
        {
            UpdatePassiveStats();
        }

        private void UpdatePassiveStats()
        {
            if (_energy == null || _tpc == null || _health == null || _health.isDead) return;

            float energyPercent = _energy.EnergyPercent;

            // Update Speed
            float speedMult = 1f + (energyPercent * maxSpeedBonus);
            _tpc.freeSpeed.walkSpeed = _originalWalkSpeed * speedMult;
            _tpc.freeSpeed.runningSpeed = _originalRunSpeed * speedMult;
            _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed * speedMult;

            // Passive Healing
            if (energyPercent > 0.1f && _health.currentHealth < _health.maxHealth)
            {
                float regen = energyPercent * maxHealthRegen * Time.deltaTime;
                _health.AddHealth((int)Mathf.Ceil(regen));
            }

            // Rapid Recharge (from Super)
            if (_rapidRechargeActive)
            {
                _energy.RestoreEnergy(30f * Time.deltaTime); // 30 energy per second
            }
        }

        private void OnDamageReceived(vDamage damage)
        {
            if (damage == null || _energy == null) return;
            
            _energy.SetCombatState(true);

            // Energy Redirect Ability (100% absorption)
            if (_isRedirecting)
            {
                _energy.RestoreEnergy(damage.damageValue);
                damage.damageValue = 0; // Nullify damage
                return;
            }

            // Passive Damage Reduction & Energy Generation
            float energyPercent = _energy.EnergyPercent;
            float resistance = energyPercent * maxDamageResistance;
            
            float originalDamage = damage.damageValue;
            damage.damageValue = damage.damageValue * (1f - resistance);

            // Convert raw damage to energy
            _energy.RestoreEnergy(originalDamage * damageToEnergyRatio);
        }

        // --------------------------------------------------
        // Abilities dispatched from AbilityExecutor
        // --------------------------------------------------
        private void OnAbilityActivated(Core.AbilityData ability)
        {
            if (_energy != null) _energy.SetCombatState(true);

            switch (ability.abilityName)
            {
                case "Germane Toss": StartCoroutine(GermaneTossRoutine(ability)); break;
                case "Concussive Blast": StartCoroutine(ConcussiveBlastRoutine(ability)); break;
                case "Energy Redirect": StartCoroutine(EnergyRedirectRoutine()); break;
                case "Full-Body Shockwave": StartCoroutine(ShockwaveRoutine(ability, false)); break;
                case "Overcharge Release": StartCoroutine(ShockwaveRoutine(ability, true)); break;
            }
        }

        // --------------------------------------------------
        // Germane Toss: Multi-Grenade Burst
        // --------------------------------------------------
        private IEnumerator GermaneTossRoutine(Core.AbilityData ability)
        {
            if (ability.vfxPrefab == null) yield break;

            string poolKey = "MacGrenade";
            Core.ObjectPoolManager.Instance.RegisterPool(poolKey, ability.vfxPrefab, 10);

            Transform muzzle = _executor.muzzlePoint != null ? _executor.muzzlePoint : transform;

            for (int i = 0; i < 3; i++)
            {
                Vector3 origin = muzzle.position;
                Vector3 dir = GetAimDirection();
                
                // Add arc and spread
                dir = Quaternion.Euler(Random.Range(-10f, -5f), Random.Range(-5f, 5f), 0) * dir;

                var projObj = Core.ObjectPoolManager.Instance.Spawn(poolKey, origin, Quaternion.LookRotation(dir));
                if (projObj != null)
                {
                    // Add physics for arcing grenade
                    var rb = projObj.GetComponent<Rigidbody>();
                    if (rb == null) rb = projObj.AddComponent<Rigidbody>();
                    
                    rb.linearVelocity = dir * 15f; // Throw force
                    rb.useGravity = true;

                    var exploder = projObj.GetComponent<Core.ExplosiveProjectile>();
                    if (exploder == null) exploder = projObj.AddComponent<Core.ExplosiveProjectile>();
                    
                    exploder.Init(ability.BuildDamage(), _executor.enemyLayer, transform, 3f); // 3m explosion radius
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

        // --------------------------------------------------
        // Concussive Blast: Shockwave scaling with energy
        // --------------------------------------------------
        private IEnumerator ConcussiveBlastRoutine(Core.AbilityData ability)
        {
            float energyMult = 1f + _energy.EnergyPercent; // 1x to 2x damage

            var hits = Physics.OverlapSphere(transform.position, ability.range, _executor.enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;

                var dmg = ability.BuildDamage();
                dmg.damageValue = (int)(dmg.damageValue * energyMult);
                
                // Add Invector Knockdown flag
                var vDmg = dmg.ToVDamage(transform);
                vDmg.activeRagdoll = true; // Forces ragdoll / knockback in Invector
                
                health.TakeDamage(vDmg);
            }

            yield return null;
        }

        // --------------------------------------------------
        // Energy Redirect: Invulnerability and Absorption
        // --------------------------------------------------
        private IEnumerator EnergyRedirectRoutine()
        {
            _isRedirecting = true;
            if (redirectVFX != null) redirectVFX.SetActive(true);

            yield return new WaitForSeconds(2.5f); // 2.5s window

            _isRedirecting = false;
            if (redirectVFX != null) redirectVFX.SetActive(false);
        }

        // --------------------------------------------------
        // Full-Body Shockwave & Overcharge Release
        // --------------------------------------------------
        private IEnumerator ShockwaveRoutine(Core.AbilityData ability, bool isSuper)
        {
            float currentEnergy = _energy.CurrentEnergy;
            
            // Consume all remaining energy
            _energy.ConsumeEnergy(currentEnergy);

            // Calculate massive damage based on consumed energy
            float bonusDamage = currentEnergy * 1.5f;

            var hits = Physics.OverlapSphere(transform.position, ability.range, _executor.enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;

                var dmg = ability.BuildDamage();
                dmg.damageValue += (int)bonusDamage;
                
                var vDmg = dmg.ToVDamage(transform);
                vDmg.activeRagdoll = true;
                
                health.TakeDamage(vDmg);
            }

            if (isSuper)
            {
                // Overcharge Release triggers rapid recharge afterwards
                _rapidRechargeActive = true;
                yield return new WaitForSeconds(5f); // 5s of rapid recharge
                _rapidRechargeActive = false;
            }
        }

        private Vector3 GetAimDirection()
        {
            var cam = Camera.main;
            if (cam == null) return transform.forward;
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Transform muzzle = _executor.muzzlePoint != null ? _executor.muzzlePoint : transform;
                return (hit.point - muzzle.position).normalized;
            }
            return cam.transform.forward;
        }
    }
}
