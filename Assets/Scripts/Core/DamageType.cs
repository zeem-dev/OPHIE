// ============================================================
//  OPHIO — DamageType
//  Day 1 | Core Data Layer
//  All damage categories used across the entire game.
//  Add new types here only — nowhere else.
// ============================================================

namespace OPHIO.Core
{
    public enum DamageType
    {
        Physical,       // Hawk melee, Flex slams, Mac rifle
        Fire,           // Goon flames — applies Burn
        Electric,       // Hawk sword / abilities — applies Shock
        Explosive,      // Mac grenades, Flex Alkaline Blast
        Infection       // Gust spores — applies Infection debuff
    }

    // Which status effect a damage type WANTS to apply.
    // StatusEffectManager reads this to decide what to stack.
    public enum StatusEffectType
    {
        None,
        Burn,       // DoT — fire source
        Shock,      // Stun / interrupt — electric source
        Infection   // Slow + DoT — spore source
    }

    // Quick lookup: DamageType -> default StatusEffect
    public static class DamageTypeHelper
    {
        public static StatusEffectType GetDefaultStatus(DamageType type)
        {
            switch (type)
            {
                case DamageType.Fire:      return StatusEffectType.Burn;
                case DamageType.Electric:  return StatusEffectType.Shock;
                case DamageType.Infection: return StatusEffectType.Infection;
                default:                   return StatusEffectType.None;
            }
        }
    }
}
