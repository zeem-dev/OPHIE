// ============================================================
//  OPHIO — OphioDamage
//  Day 1 | Core Data Layer
//  Wraps Invector's vDamage with OPHIO damage types
//  and status effect flags.
//  Usage: Invector's vDamage.damageType is already a string —
//  we set it AND run our typed system in parallel
//  so Invector's pipeline does not break.
// ============================================================

using Invector;
using OPHIO.Core;
using UnityEngine;

namespace OPHIO.Core
{
    [System.Serializable]
    public class OphioDamage
    {
        [Header("Base Damage")]
        public float damageValue = 15f;

        [Header("OPHIO Type")]
        public DamageType damageType = DamageType.Physical;

        [Header("Status Effect")]
        [Tooltip("Override auto-detected status. Leave as None to use DamageTypeHelper default.")]
        public StatusEffectType statusOverride = StatusEffectType.None;

        [Range(0f, 1f)]
        [Tooltip("0 = never applies status, 1 = always applies status")]
        public float statusChance = 1f;

        [Header("Status Stacking")]
        [Tooltip("How many stacks of this status to apply on hit")]
        public int statusStacks = 1;

        [Header("Invector Bridge")]
        [Tooltip("True = also staggers the enemy (Invector hitReaction)")]
        public bool causesHitReaction = true;
        public bool ignoreDefense     = false;
        public bool activeRagdoll     = false;

        // --------------------------------------------------
        //  Convert to Invector vDamage for the existing
        //  Invector pipeline (vHealthController, etc.)
        // --------------------------------------------------
        public vDamage ToVDamage(Transform sender = null)
        {
            var d = new vDamage((int)damageValue)
            {
                damageType    = damageType.ToString(),
                hitReaction   = causesHitReaction,
                ignoreDefense = ignoreDefense,
                activeRagdoll = activeRagdoll,
                sender        = sender
            };
            return d;
        }

        // --------------------------------------------------
        //  Resolve which status effect this hit applies
        // --------------------------------------------------
        public StatusEffectType ResolvedStatus()
        {
            if (statusOverride != StatusEffectType.None)
                return statusOverride;
            return DamageTypeHelper.GetDefaultStatus(damageType);
        }

        // --------------------------------------------------
        //  Should status be applied this hit? (chance roll)
        // --------------------------------------------------
        public bool RollStatus()
        {
            if (statusChance >= 1f) return true;
            if (statusChance <= 0f) return false;
            return Random.value <= statusChance;
        }
    }
}
