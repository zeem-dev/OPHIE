// ============================================================
//  OPHIO — ArcSlashAbility
//  Day 4 | Hawk Abilities
//  Electric sword slash — 3m chain to nearby enemies.
//  Shock on hit. Charge Hawk's meter on each hit.
//  Player GameObject par lagao.
// ============================================================

using UnityEngine;
using Invector;

namespace OPHIO.Abilities
{
    public class ArcSlashAbility : MonoBehaviour
    {
        [Header("Arc Slash Settings")]
        [Tooltip("Primary slash damage (set in AbilityData too — this overrides at runtime)")]
        public float slashDamage    = 35f;
        [Tooltip("Chain radius — enemies within this range also get hit")]
        public float chainRadius    = 3f;
        [Tooltip("Chain damage is this fraction of primary damage")]
        public float chainDamageMult= 0.6f;
        [Tooltip("Shock duration applied on each hit")]
        public float shockDuration  = 1.5f;

        [Header("VFX")]
        public GameObject slashVFXPrefab;
        public GameObject chainVFXPrefab;

        private Characters.HawkCharacter _hawk;
        private Core.AbilityExecutor     _executor;

        private void Awake()
        {
            _hawk     = GetComponent<Characters.HawkCharacter>();
            _executor = GetComponent<Core.AbilityExecutor>();
        }

        // Called by AbilityExecutor when ArcSlash is triggered
        // AbilityExecutor handles base AoE — we extend with chain
        public void ExecuteArcSlash(Core.AbilityData abilityData)
        {
            float totalDamage = slashDamage * (_hawk != null ? _hawk.GetChargeDamageMultiplier() : 1f);

            // Primary hit — all enemies in radius
            var hits = Physics.OverlapSphere(transform.position, chainRadius, _executor.enemyLayer);
            int hitCount = 0;

            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;

                bool isChain = hitCount > 0;
                float dmgValue = isChain ? totalDamage * chainDamageMult : totalDamage;

                // Apply damage
                var vdmg = new vDamage((int)dmgValue)
                {
                    damageType  = "Electric",
                    hitReaction = true,
                    sender      = transform
                };
                health.TakeDamage(vdmg);

                // Apply shock
                Core.StatusEffectManager.Instance?.Apply(
                    health, Core.StatusEffectType.Shock,
                    shockDuration, 1, transform);

                // VFX
                SpawnHitVFX(isChain, hit.transform.position);

                hitCount++;
            }

            // Charge Hawk per hit
            if (_hawk != null && hitCount > 0)
                _hawk.AddCharge(10f * hitCount);

            _hawk?.SetCombatState(true);
        }

        private void SpawnHitVFX(bool isChain, Vector3 pos)
        {
            GameObject prefab = isChain ? chainVFXPrefab : slashVFXPrefab;
            if (prefab == null) return;
            string key = isChain ? "ArcChainVFX" : "ArcSlashVFX";
            Core.ObjectPoolManager.Instance.RegisterPool(key, prefab, 5);
            var vfx = Core.ObjectPoolManager.Instance.Spawn(key, pos, Quaternion.identity);
            Core.ObjectPoolManager.Instance.Despawn(key, vfx, 1.5f);
        }
    }
}
