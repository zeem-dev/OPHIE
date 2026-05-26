// ============================================================
//  OPHIO — HawkAbilityBridge
//  Hawk Implementation
//  Routes AbilityExecutor generic callbacks to Hawk's
//  specific ability scripts.
//  Attach on Player GameObject
// ============================================================

using UnityEngine;

namespace OPHIO.Characters
{
    public class HawkAbilityBridge : MonoBehaviour
    {
        private Abilities.ArcSlashAbility      _arcSlash;
        private Abilities.DischargeAbility     _discharge;
        private Abilities.NeuralSurgeAbility   _neuralSurge;
        private Abilities.VoltDashAbility      _voltDash;
        private Abilities.EnergySiphonAbility  _energySiphon;
        private Abilities.TotalOverloadAbility _totalOverload;
        private HawkCharacter                  _hawk;

        private void Awake()
        {
            _arcSlash      = GetComponent<Abilities.ArcSlashAbility>();
            _discharge     = GetComponent<Abilities.DischargeAbility>();
            _neuralSurge   = GetComponent<Abilities.NeuralSurgeAbility>();
            _voltDash      = GetComponent<Abilities.VoltDashAbility>();
            _energySiphon  = GetComponent<Abilities.EnergySiphonAbility>();
            _totalOverload = GetComponent<Abilities.TotalOverloadAbility>();
            _hawk          = GetComponent<HawkCharacter>();
        }

        // --------------------------------------------------
        //  Called by AbilityExecutor for AoE-type abilities
        // --------------------------------------------------
        private void OnAbilityAoEOverride(Core.AbilityData ability)
        {
            switch (ability.abilityName)
            {
                case "Arc Slash":      _arcSlash?.ExecuteArcSlash(ability);  break;
                case "Discharge":      _discharge?.ExecuteDischarge(ability); break;
                case "Total Overload": _totalOverload?.Activate(ability);     break;
            }
        }

        // --------------------------------------------------
        //  Called by AbilityExecutor for Self / Trail abilities
        // --------------------------------------------------
        private void OnAbilityActivated(Core.AbilityData ability)
        {
            switch (ability.abilityName)
            {
                case "Neural Surge":
                    _neuralSurge?.Activate(ability);
                    break;
                case "Volt Dash":
                    _voltDash?.Activate(ability);
                    break;
                case "Energy Siphon":
                    if (_energySiphon != null)
                    {
                        if (_energySiphon.IsActive) _energySiphon.Deactivate();
                        else                        _energySiphon.Activate(ability);
                    }
                    break;
                case "Total Overload":
                    _totalOverload?.Activate(ability);
                    break;
            }
        }

        // --------------------------------------------------
        //  Called by HawkCharacter when a melee hit lands
        // --------------------------------------------------
        public void OnMeleeHit(Invector.vDamage damage)
        {
            _energySiphon?.OnMeleeHit(damage);
            _hawk?.SetCombatState(true);
        }
    }
}
