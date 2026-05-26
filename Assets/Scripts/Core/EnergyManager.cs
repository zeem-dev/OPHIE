// ============================================================
//  OPHIO — EnergyManager
//  Ability Core
//  Manages the player's energy bar.
//  Abilities consume energy through this component.
//  Attach to the Player GameObject.
// ============================================================

using UnityEngine;
using UnityEngine.Events;

namespace OPHIO.Core
{
    public class EnergyManager : MonoBehaviour
    {
        [Header("Energy Settings")]
        public float maxEnergy      = 100f;
        public float regenPerSecond = 5f;
        [Tooltip("Seconds after last ability use before regen starts")]
        public float regenDelay     = 2f;

        [Header("Combat Regen")]
        [Tooltip("Regen rate while actively in combat")]
        public float combatRegenRate = 0f;

        // State
        public float CurrentEnergy  { get; private set; }
        public float EnergyPercent  => CurrentEnergy / maxEnergy;
        public bool  IsInCombat     { get; private set; }

        private float _regenTimer;

        [Header("Events")]
        public UnityEvent<float, float> onEnergyChanged;  // current, max
        public UnityEvent               onEnergyEmpty;
        public UnityEvent               onEnergyFull;

        private void Awake()
        {
            CurrentEnergy = maxEnergy;
        }

        public void InitFromData(CharacterData data)
        {
            maxEnergy      = data.maxEnergy;
            regenPerSecond = data.energyRegen;
            CurrentEnergy  = maxEnergy;
            onEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
        }

        private void Update()
        {
            HandleRegen();
        }

        // --------------------------------------------------
        //  Public API
        // --------------------------------------------------

        /// <summary>Returns true if energy was consumed successfully, false if insufficient.</summary>
        public bool ConsumeEnergy(float amount)
        {
            if (CurrentEnergy < amount) return false;
            CurrentEnergy  = Mathf.Max(0f, CurrentEnergy - amount);
            _regenTimer    = regenDelay;
            onEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
            if (CurrentEnergy <= 0f) onEnergyEmpty?.Invoke();
            return true;
        }

        /// <summary>Restore energy (passives, siphon, pickups).</summary>
        public void RestoreEnergy(float amount)
        {
            bool wasFull  = CurrentEnergy >= maxEnergy;
            CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + amount);
            onEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
            if (!wasFull && CurrentEnergy >= maxEnergy) onEnergyFull?.Invoke();
        }

        public void SetCombatState(bool inCombat) => IsInCombat = inCombat;
        public bool HasEnergy(float amount)        => CurrentEnergy >= amount;

        // --------------------------------------------------
        //  Regen logic
        // --------------------------------------------------
        private void HandleRegen()
        {
            if (_regenTimer > 0f) { _regenTimer -= Time.deltaTime; return; }
            if (CurrentEnergy >= maxEnergy) return;
            float rate = IsInCombat ? combatRegenRate : regenPerSecond;
            if (rate <= 0f) return;
            RestoreEnergy(rate * Time.deltaTime);
        }
    }
}
