// ============================================================
//  OPHIO — AbilityLoadout
//  Ability Core
//  Manages 3 active slots + 1 super slot.
//  Abilities can be swapped outside of combat.
//  Attach to the Player GameObject.
// ============================================================

using UnityEngine;
using UnityEngine.Events;

namespace OPHIO.Core
{
    public class AbilityLoadout : MonoBehaviour
    {
        [Header("Loadout Slots (assign in Inspector or from Character Select)")]
        public AbilityData slot1;       // Q
        public AbilityData slot2;       // E
        public AbilityData slot3;       // R
        public AbilityData superSlot;   // F

        [Header("State")]
        public bool canSwapAbilities = true;  // locked to false during combat

        [Header("Events")]
        public UnityEvent<int, AbilityData> onSlotChanged; // slot index, new ability

        // --------------------------------------------------
        //  Slot access
        // --------------------------------------------------
        public AbilityData GetSlot(int index)
        {
            switch (index)
            {
                case 0: return slot1;
                case 1: return slot2;
                case 2: return slot3;
                case 3: return superSlot;
                default: return null;
            }
        }

        public void SetSlot(int index, AbilityData ability)
        {
            if (!canSwapAbilities)
            {
                Debug.Log("[AbilityLoadout] Cannot swap abilities during combat.");
                return;
            }
            switch (index)
            {
                case 0: slot1     = ability; break;
                case 1: slot2     = ability; break;
                case 2: slot3     = ability; break;
                case 3: superSlot = ability; break;
            }
            onSlotChanged?.Invoke(index, ability);
        }

        /// <summary>Populate slots from a CharacterData asset (first 3 abilities + super).</summary>
        public void LoadFromCharacterData(CharacterData data)
        {
            if (data.abilityPool.Count > 0) slot1     = data.abilityPool[0];
            if (data.abilityPool.Count > 1) slot2     = data.abilityPool[1];
            if (data.abilityPool.Count > 2) slot3     = data.abilityPool[2];
            superSlot = data.superAbility;

            onSlotChanged?.Invoke(0, slot1);
            onSlotChanged?.Invoke(1, slot2);
            onSlotChanged?.Invoke(2, slot3);
            onSlotChanged?.Invoke(3, superSlot);
        }

        public void SetCombatLock(bool locked) => canSwapAbilities = !locked;
    }
}
