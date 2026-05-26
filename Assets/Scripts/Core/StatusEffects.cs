// ============================================================
//  OPHIO — StatusEffect Base
//  Day 2 | Runtime Systems
//  Abstract base class inherited by every status effect
//  (Burn, Shock, Infection).
// ============================================================

using UnityEngine;
using Invector;

namespace OPHIO.Core
{
    public abstract class StatusEffect
    {
        public StatusEffectType effectType;
        public float            duration;
        public float            timeRemaining;
        public int              stacks;
        public Transform        appliedBy;

        // Called by StatusEffectManager every tick
        public abstract void OnTick(vHealthController target, float tickInterval);

        // Called when first applied or re-applied (stack refresh)
        public virtual void OnApply(vHealthController target)
        {
            timeRemaining = duration;
        }

        // Called when effect expires or is cleansed
        public virtual void OnExpire(vHealthController target) { }

        public bool IsExpired => timeRemaining <= 0f;
    }

    // --------------------------------------------------
    //  BURN — Fire damage over time
    // --------------------------------------------------
    public class BurnEffect : StatusEffect
    {
        public float damagePerTick = 5f;

        public BurnEffect(float duration, int stacks, Transform appliedBy)
        {
            effectType         = StatusEffectType.Burn;
            this.duration      = duration;
            this.timeRemaining = duration;
            this.stacks        = stacks;
            this.appliedBy     = appliedBy;
            damagePerTick      = 5f * stacks;
        }

        public override void OnTick(vHealthController target, float tickInterval)
        {
            if (target == null || target.isDead) return;
            var dmg = new vDamage((int)damagePerTick)
            {
                damageType  = "Fire",
                hitReaction = false,
                sender      = appliedBy
            };
            target.TakeDamage(dmg);
            timeRemaining -= tickInterval;
        }
    }

    // --------------------------------------------------
    //  SHOCK — Electric stun / interrupt
    // --------------------------------------------------
    public class ShockEffect : StatusEffect
    {
        public bool isStunned = true;

        public ShockEffect(float duration, int stacks, Transform appliedBy)
        {
            effectType         = StatusEffectType.Shock;
            this.duration      = duration;
            this.timeRemaining = duration;
            this.stacks        = stacks;
            this.appliedBy     = appliedBy;
        }

        public override void OnApply(vHealthController target)
        {
            base.OnApply(target);
            // Notify animator if target has one
            var anim = target.GetComponent<Animator>();
            if (anim != null) anim.SetBool("isStunned", true);
        }

        public override void OnTick(vHealthController target, float tickInterval)
        {
            if (target == null || target.isDead) return;
            timeRemaining -= tickInterval;
        }

        public override void OnExpire(vHealthController target)
        {
            if (target == null) return;
            var anim = target.GetComponent<Animator>();
            if (anim != null) anim.SetBool("isStunned", false);
        }
    }

    // --------------------------------------------------
    //  INFECTION — Spore slow + damage over time
    // --------------------------------------------------
    public class InfectionEffect : StatusEffect
    {
        public float damagePerTick  = 3f;
        public float slowMultiplier = 0.5f;   // 50% speed reduction

        private UnityEngine.AI.NavMeshAgent _agent;
        private float _originalSpeed;

        public InfectionEffect(float duration, int stacks, Transform appliedBy)
        {
            effectType         = StatusEffectType.Infection;
            this.duration      = duration;
            this.timeRemaining = duration;
            this.stacks        = stacks;
            this.appliedBy     = appliedBy;
            damagePerTick      = 3f * stacks;
        }

        public override void OnApply(vHealthController target)
        {
            base.OnApply(target);
            _agent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (_agent != null)
            {
                _originalSpeed = _agent.speed;
                _agent.speed  *= slowMultiplier;
            }
        }

        public override void OnTick(vHealthController target, float tickInterval)
        {
            if (target == null || target.isDead) return;
            var dmg = new vDamage((int)damagePerTick)
            {
                damageType  = "Infection",
                hitReaction = false,
                sender      = appliedBy
            };
            target.TakeDamage(dmg);
            timeRemaining -= tickInterval;
        }

        public override void OnExpire(vHealthController target)
        {
            if (_agent != null) _agent.speed = _originalSpeed;
        }
    }
}
