// ============================================================
//  OPHIO — EnemyType + EnemyTypeData
//  Day 7 | Enemy AI State Machine
//  Enum for enemy categories + ScriptableObject for per-type
//  tuning. Create one asset per enemy variant.
//  Create > OPHIO > Enemy Type Data
// ============================================================

using UnityEngine;

namespace OPHIO.AI
{
    // --------------------------------------------------
    //  Enemy category enum
    // --------------------------------------------------
    public enum EnemyType
    {
        Normal,     // Swarm fodder — low HP, proximity rush
        Fast,       // High speed, erratic, low HP
        Heavy,      // Tank — slow, devastating melee, staggers player
        Special,    // Exploder / Screamer — unique mechanic
        Boss        // Multi-phase, high HP, pattern attacks
    }

    // --------------------------------------------------
    //  ScriptableObject — tune each enemy variant
    //  in the Inspector without touching code
    // --------------------------------------------------
    [CreateAssetMenu(menuName = "OPHIO/Enemy Type Data", fileName = "EnemyType_New")]
    public class EnemyTypeData : ScriptableObject
    {
        // --------------------------------------------------
        //  Identity
        // --------------------------------------------------
        [Header("Identity")]
        public string enemyName      = "Enemy";
        public EnemyType type        = EnemyType.Normal;
        [TextArea(2, 3)]
        public string description    = "";
        public Sprite icon;

        // --------------------------------------------------
        //  Health
        // --------------------------------------------------
        [Header("Health")]
        public int   maxHealth       = 80;

        // --------------------------------------------------
        //  Detection
        // --------------------------------------------------
        [Header("Detection")]
        public float detectionRange      = 15f;
        public float soundDetectionRange = 8f;

        // --------------------------------------------------
        //  Movement
        // --------------------------------------------------
        [Header("Movement")]
        public float chaseSpeed      = 4.5f;

        // --------------------------------------------------
        //  Attack
        // --------------------------------------------------
        [Header("Attack")]
        public float attackRange     = 2f;
        public float attackDamage    = 15f;
        public float attackCooldown  = 1.5f;
        public float attackWindup    = 0.3f;
        public float attackRecovery  = 0.4f;
        public Core.DamageType attackDamageType = Core.DamageType.Physical;

        // --------------------------------------------------
        //  Aggression
        // --------------------------------------------------
        [Header("Aggression")]
        [Range(0.5f, 2.5f)]
        public float baseAggression = 1f;

        // --------------------------------------------------
        //  Death
        // --------------------------------------------------
        [Header("Death")]
        public float corpseDuration  = 4f;

        // --------------------------------------------------
        //  Rewards (for future progression)
        // --------------------------------------------------
        [Header("Rewards")]
        public int   scoreValue      = 100;
        public int   xpValue         = 25;

        // --------------------------------------------------
        //  Helper — apply this data to an EnemyAI component
        // --------------------------------------------------
        public void ApplyTo(EnemyAI ai)
        {
            if (ai == null) return;

            // Health
            var health = ai.GetComponent<Invector.vHealthController>();
            if (health != null)
            {
                health.maxHealth = maxHealth;
                // currentHealth setter is protected — use ChangeHealth to restore
                int diff = (int)(maxHealth - health.currentHealth);
                if (diff != 0) health.ChangeHealth(diff);
            }

            // AI settings
            ai.enemyType          = type;
            ai.detectionRange     = detectionRange;
            ai.soundDetectionRange= soundDetectionRange;
            ai.chaseSpeed         = chaseSpeed;
            ai.attackRange        = attackRange;
            ai.attackDamage       = attackDamage;
            ai.attackCooldown     = attackCooldown;
            ai.attackWindup       = attackWindup;
            ai.attackRecovery     = attackRecovery;
            ai.attackDamageType   = attackDamageType;
            ai.aggressionLevel   = baseAggression;
            ai.corpseDuration     = corpseDuration;
        }
    }
}
