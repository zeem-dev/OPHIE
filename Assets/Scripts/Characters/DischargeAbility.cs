// ============================================================
//  OPHIO — DischargeAbility
//  Day 4 | Hawk Abilities
//  Stored charge ko burst mein release karta hai.
//  Single target = heavy damage, hold shift = AoE mode.
//  Damage scales with Hawk's current charge level.
//  Player GameObject par lagao.
// ============================================================

using UnityEngine;
using Invector;

namespace OPHIO.Abilities
{
    public class DischargeAbility : MonoBehaviour
    {
        [Header("Discharge Settings")]
        public float baseDamage         = 60f;
        [Tooltip("Single target max range")]
        public float singleTargetRange  = 8f;
        [Tooltip("AoE radius when in group mode (hold Shift)")]
        public float aoeRadius          = 4f;
        [Tooltip("Shock duration applied on hit")]
        public float shockDuration      = 2.5f;

        [Header("Charge Cost")]
        [Tooltip("Fraction of current charge consumed on use")]
        public float chargeConsumeFraction = 0.5f;

        [Header("VFX")]
        public GameObject dischargeVFXPrefab;
        public GameObject aoeVFXPrefab;

        private Characters.HawkCharacter _hawk;
        private Core.AbilityExecutor     _executor;

        private void Awake()
        {
            _hawk     = GetComponent<Characters.HawkCharacter>();
            _executor = GetComponent<Core.AbilityExecutor>();
        }

        public void ExecuteDischarge(Core.AbilityData abilityData)
        {
            bool isAoEMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Damage scales with current charge
            float chargeMultiplier = _hawk != null ? _hawk.GetDischargeMultiplier() : 1f;
            float totalDamage      = baseDamage * chargeMultiplier;

            // Consume charge
            if (_hawk != null)
                _hawk.RemoveCharge(_hawk.CurrentCharge * chargeConsumeFraction);

            if (isAoEMode)
                ExecuteAoE(totalDamage);
            else
                ExecuteSingleTarget(totalDamage);

            _hawk?.SetCombatState(true);
        }

        private void ExecuteSingleTarget(float damage)
        {
            var cam = Camera.main;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, singleTargetRange, _executor.enemyLayer))
            {
                var health = hit.collider.GetComponent<vHealthController>();
                if (health == null || health.isDead) return;

                ApplyDamageAndShock(health, damage);
                SpawnVFX(dischargeVFXPrefab, "DischargeVFX", hit.point);
            }
        }

        private void ExecuteAoE(float damage)
        {
            var hits = Physics.OverlapSphere(transform.position, aoeRadius, _executor.enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;
                ApplyDamageAndShock(health, damage * 0.65f); // AoE = 65% damage
            }
            SpawnVFX(aoeVFXPrefab, "DischargeAoEVFX", transform.position);
        }

        private void ApplyDamageAndShock(vHealthController health, float damage)
        {
            var vdmg = new vDamage((int)damage)
            {
                damageType  = "Electric",
                hitReaction = true,
                sender      = transform
            };
            health.TakeDamage(vdmg);
            Core.StatusEffectManager.Instance?.Apply(
                health, Core.StatusEffectType.Shock,
                shockDuration, 2, transform);
        }

        private void SpawnVFX(GameObject prefab, string key, Vector3 pos)
        {
            if (prefab == null) return;
            Core.ObjectPoolManager.Instance.RegisterPool(key, prefab, 3);
            var vfx = Core.ObjectPoolManager.Instance.Spawn(key, pos, Quaternion.identity);
            Core.ObjectPoolManager.Instance.Despawn(key, vfx, 2f);
        }
    }
}
