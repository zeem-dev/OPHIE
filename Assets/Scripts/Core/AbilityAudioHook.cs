// ============================================================
//  OPHIO — AbilityAudioHook
//  Connects ability activations to AudioManager.
//  Listens to AbilityExecutor and fires correct SFX.
//  Attach to Player GameObject alongside AbilityExecutor.
// ============================================================

using UnityEngine;

namespace OPHIO.Core
{
    // RequireComponent removed — keeps AbilityExecutor independent
    public class AbilityAudioHook : MonoBehaviour
    {
        private AbilityExecutor _executor;

        private void Awake()
        {
            _executor = GetComponent<AbilityExecutor>();
        }

        // Called by AbilityExecutor coroutine via SendMessage
        // We piggyback on the same SendMessage that goes to HawkAbilityBridge
        private void OnAbilityActivated(AbilityData ability)
        {
            PlayAbilitySFX(ability);
        }

        private void OnAbilityAoEOverride(AbilityData ability)
        {
            PlayAbilitySFX(ability);
        }

        // --------------------------------------------------
        //  Route ability name to AudioManager
        // --------------------------------------------------
        private void PlayAbilitySFX(AbilityData ability)
        {
            if (ability == null) return;
            var audio = AudioManager.Instance;
            if (audio == null) return;

            switch (ability.abilityName)
            {
                case "Arc Slash":     audio.PlayArcSlash();      break;
                case "Discharge":     audio.PlayDischarge();     break;
                case "Neural Surge":  audio.PlayNeuralSurge();   break;
                case "Volt Dash":     audio.PlayVoltDash();      break;
                case "Energy Siphon": audio.PlayEnergySiphon();  break;
                case "Total Overload":audio.PlayTotalOverload(); break;
                default:
                    // Generic hit SFX for unknown abilities
                    audio.PlaySFX(null);
                    break;
            }
        }
    }
}
