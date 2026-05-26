// ============================================================
//  OPHIO — AbilityData ScriptableObject
//  Core Data Layer
//  Ability data blueprint.
//  AbilityExecutor read this at runtime
//  Create > OPHIO > Ability Data
// ============================================================

using UnityEngine;

namespace OPHIO.Core
{
    public enum AbilityCategory
    {
        Active,   // Player manually triggers
        Super,    // Super slot — higher cost, longer cooldown
        Passive   // Always-on — not triggered
    }

    public enum AbilityTargeting
    {
        Self,             // Buff on caster
        SingleTarget,     // Raycast / lock-on target
        AreaAroundSelf,   // Radial AoE around player
        DirectionalCone,  // Forward cone
        Projectile,       // Spawn projectile from pool
        Trail             // Leave a trail during movement
    }

    [CreateAssetMenu(menuName = "OPHIO/Ability Data", fileName = "Ability_New")]
    public class AbilityData : ScriptableObject
    {
        // --------------------------------------------------
        //  Identity
        // --------------------------------------------------
        [Header("Identity")]
        public string abilityName     = "New Ability";
        [TextArea(2, 3)]
        public string description     = "";
        public Sprite icon;
        public AbilityCategory category  = AbilityCategory.Active;

        // --------------------------------------------------
        //  Damage
        // --------------------------------------------------
        [Header("Damage")]
        public float baseDamage       = 30f;
        public DamageType damageType  = DamageType.Physical;
        [Range(0f, 1f)]
        public float statusChance     = 1f;
        public int   statusStacks     = 1;

        // --------------------------------------------------
        //  Targeting
        // --------------------------------------------------
        [Header("Targeting")]
        public AbilityTargeting targeting = AbilityTargeting.SingleTarget;
        public float range        = 5f;
        public float radius       = 3f;   // for AoE / cone abilities
        public float coneAngle    = 60f;  // degrees, for cone targeting

        // --------------------------------------------------
        //  Cost & Cooldown
        // --------------------------------------------------
        [Header("Cost & Cooldown")]
        public float energyCost   = 20f;
        public float cooldown     = 5f;   // seconds

        // --------------------------------------------------
        //  Animation
        // --------------------------------------------------
        [Header("Animation")]
        [Tooltip("Animator trigger name to play on activation")]
        public string animTrigger    = "";
        public float  castDuration   = 0.5f;  // lock input for this long

        // --------------------------------------------------
        //  VFX / SFX (assigned in inspector)
        // --------------------------------------------------
        [Header("VFX / SFX")]
        [Tooltip("Pooled prefab spawned on ability use")]
        public GameObject vfxPrefab;
        [Tooltip("Offset from player origin to spawn VFX")]
        public Vector3 vfxOffset = Vector3.zero;
        public AudioClip activationSound;

        // --------------------------------------------------
        //  Runtime helper
        // --------------------------------------------------
        /// <summary>Build an OphioDamage from this ability's stats.</summary>
        public OphioDamage BuildDamage()
        {
            return new OphioDamage
            {
                damageValue    = baseDamage,
                damageType     = damageType,
                statusChance   = statusChance,
                statusStacks   = statusStacks,
                causesHitReaction = true,
                ignoreDefense  = false
            };
        }
    }
}
