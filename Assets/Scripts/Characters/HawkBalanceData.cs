// ============================================================
//  OPHIO — HawkBalanceData
//  Day 6 | Balance Pass
//  Hawk ki saari balance values ek jagah.
//  Tune karo bina code touch kiye — sirf Inspector mein.
//  Player GameObject par lagao.
// ============================================================

using UnityEngine;

namespace OPHIO.Characters
{
    public class HawkBalanceData : MonoBehaviour
    {
        [Header("── BASE STATS ──")]
        public int   maxHealth          = 120;
        public float maxEnergy          = 100f;
        public float energyRegen        = 5f;
        public float regenDelay         = 2f;

        [Header("── CHARGE SYSTEM ──")]
        public float maxCharge          = 100f;
        public float ambientChargeRate  = 2f;
        public float chargeDecayRate    = 5f;
        public float decayDelay         = 4f;
        public float chargePerMeleeHit  = 8f;

        [Header("── ARC SLASH ──")]
        public float arcSlashDamage     = 35f;
        public float arcSlashChainRadius= 3f;
        public float arcSlashChainMult  = 0.6f;
        public float arcSlashCooldown   = 4f;
        public float arcSlashEnergy     = 15f;
        public float arcSlashShockDur   = 1.5f;

        [Header("── DISCHARGE ──")]
        public float dischargeDamage    = 60f;
        public float dischargeRange     = 8f;
        public float dischargeAoERadius = 4f;
        public float dischargeCooldown  = 6f;
        public float dischargeEnergy    = 30f;
        public float dischargeShockDur  = 2.5f;

        [Header("── NEURAL SURGE ──")]
        public float surgeDuration      = 5f;
        public float surgeSpeedBonus    = 0.30f;
        public float surgeAnimBonus     = 0.40f;
        public float surgeCooldown      = 12f;
        public float surgeEnergy        = 25f;

        [Header("── VOLT DASH ──")]
        public float dashDistance       = 6f;
        public float dashTrailDamage    = 20f;
        public float dashTrailDuration  = 2.5f;
        public float dashCooldown       = 5f;
        public float dashEnergy         = 20f;

        [Header("── TOTAL OVERLOAD ──")]
        public float overloadBaseDamage = 120f;
        public float overloadRadius     = 8f;
        public float overloadStunDur    = 3f;
        public float overloadCooldown   = 30f;
        public float overloadEnergy     = 80f;

        [Header("── COMBO SYSTEM ──")]
        public float comboWindow        = 1.5f;
        public float comboDamageBonus   = 0.15f;
        public int   maxComboSteps      = 8;
        public float mixedTypeBonus     = 0.25f;

        // Apply all values to components at Start
        private void Start()
        {
            ApplyAll();
        }

        [ContextMenu("Apply Balance Values")]
        public void ApplyAll()
        {
            ApplyHealth();
            ApplyEnergy();
            ApplyHawkChar();
            ApplyArcSlash();
            ApplyDischarge();
            ApplyNeuralSurge();
            ApplyVoltDash();
            ApplyTotalOverload();
            ApplyCombo();
        }

        private void ApplyHealth()
        {
            var health = GetComponent<Invector.vHealthController>();
            if (health != null) health.maxHealth = maxHealth;
        }

        private void ApplyEnergy()
        {
            var e = GetComponent<Core.EnergyManager>();
            if (e == null) return;
            e.maxEnergy      = maxEnergy;
            e.regenPerSecond = energyRegen;
            e.regenDelay     = regenDelay;
        }

        private void ApplyHawkChar()
        {
            var h = GetComponent<HawkCharacter>();
            if (h == null) return;
            h.maxCharge         = maxCharge;
            h.ambientChargeRate = ambientChargeRate;
            h.chargeDecayRate   = chargeDecayRate;
            h.decayDelay        = decayDelay;
            h.siphonChargePerHit= chargePerMeleeHit;
        }

        private void ApplyArcSlash()
        {
            var a = GetComponent<Abilities.ArcSlashAbility>();
            if (a == null) return;
            a.slashDamage     = arcSlashDamage;
            a.chainRadius     = arcSlashChainRadius;
            a.chainDamageMult = arcSlashChainMult;
            a.shockDuration   = arcSlashShockDur;

            var loadout = GetComponent<Core.AbilityLoadout>();
            if (loadout?.slot1 != null)
            {
                loadout.slot1.cooldown    = arcSlashCooldown;
                loadout.slot1.energyCost  = arcSlashEnergy;
            }
        }

        private void ApplyDischarge()
        {
            var d = GetComponent<Abilities.DischargeAbility>();
            if (d == null) return;
            d.baseDamage        = dischargeDamage;
            d.singleTargetRange = dischargeRange;
            d.aoeRadius         = dischargeAoERadius;
            d.shockDuration     = dischargeShockDur;

            var loadout = GetComponent<Core.AbilityLoadout>();
            if (loadout?.slot2 != null)
            {
                loadout.slot2.cooldown   = dischargeCooldown;
                loadout.slot2.energyCost = dischargeEnergy;
            }
        }

        private void ApplyNeuralSurge()
        {
            var n = GetComponent<Abilities.NeuralSurgeAbility>();
            if (n == null) return;
            n.duration       = surgeDuration;
            n.speedBonus     = surgeSpeedBonus;
            n.animSpeedBonus = surgeAnimBonus;

            var loadout = GetComponent<Core.AbilityLoadout>();
            if (loadout?.slot3 != null)
            {
                loadout.slot3.cooldown   = surgeCooldown;
                loadout.slot3.energyCost = surgeEnergy;
            }
        }

        private void ApplyVoltDash()
        {
            var v = GetComponent<Abilities.VoltDashAbility>();
            if (v == null) return;
            v.dashDistance  = dashDistance;
            v.trailDamage   = dashTrailDamage;
            v.trailDuration = dashTrailDuration;
        }

        private void ApplyTotalOverload()
        {
            var t = GetComponent<Abilities.TotalOverloadAbility>();
            if (t == null) return;
            t.baseExplosionDamage = overloadBaseDamage;
            t.explosionRadius     = overloadRadius;
            t.stunDuration        = overloadStunDur;

            var loadout = GetComponent<Core.AbilityLoadout>();
            if (loadout?.superSlot != null)
            {
                loadout.superSlot.cooldown   = overloadCooldown;
                loadout.superSlot.energyCost = overloadEnergy;
            }
        }

        private void ApplyCombo()
        {
            var c = GetComponent<Core.ComboSystem>();
            if (c == null) return;
            c.comboWindow      = comboWindow;
            c.comboDamageBonus = comboDamageBonus;
            c.maxComboSteps    = maxComboSteps;
            c.mixedTypeBonus   = mixedTypeBonus;
        }
    }
}
