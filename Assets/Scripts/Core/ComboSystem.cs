// ============================================================
//  OPHIO — ComboSystem
//  Combat Polish
//  Attack + ability chain detection.
//  Mixed damage type combos bonus effects trigger.
//  Attach on Player GameObject
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OPHIO.Core
{
    public class ComboSystem : MonoBehaviour
    {
        [Header("Combo Settings")]
        [Tooltip("Window in seconds to chain next hit")]
        public float comboWindow        = 1.5f;
        [Tooltip("Bonus damage multiplier per combo step")]
        public float comboDamageBonus   = 0.15f;  // +15% per step
        [Tooltip("Max combo steps before reset")]
        public int   maxComboSteps      = 8;

        [Header("Mixed Type Bonus")]
        [Tooltip("Extra damage when 2 different damage types hit in same combo")]
        public float mixedTypeBonus     = 0.25f;

        // State
        public int   ComboCount         { get; private set; }
        public float ComboDamageMultiplier => 1f + (ComboCount * comboDamageBonus);

        private float           _comboTimer;
        private bool            _comboActive;
        private List<DamageType> _typesInCombo = new List<DamageType>();

        // Events
        public System.Action<int>   onComboStep;    // combo count
        public System.Action        onComboBreak;
        public System.Action<float> onMixedBonus;   // bonus multiplier

        private void Update()
        {
            if (!_comboActive) return;
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f) BreakCombo();
        }

        // --------------------------------------------------
        //  Call this every time a hit lands
        // --------------------------------------------------
        public float RegisterHit(DamageType type, float baseDamage)
        {
            // Extend combo window
            _comboTimer  = comboWindow;
            _comboActive = true;

            if (ComboCount < maxComboSteps) ComboCount++;

            // Track damage types for mixed bonus
            if (!_typesInCombo.Contains(type))
                _typesInCombo.Add(type);

            float multiplier = ComboDamageMultiplier;

            // Mixed type bonus — 2+ different types in same combo
            if (_typesInCombo.Count >= 2)
            {
                multiplier += mixedTypeBonus;
                onMixedBonus?.Invoke(mixedTypeBonus);
            }

            onComboStep?.Invoke(ComboCount);
            return baseDamage * multiplier;
        }

        public void BreakCombo()
        {
            if (!_comboActive) return;
            ComboCount   = 0;
            _comboActive = false;
            _comboTimer  = 0f;
            _typesInCombo.Clear();
            onComboBreak?.Invoke();
        }

        // Called when player takes damage — breaks combo
        public void OnPlayerHit() => BreakCombo();
    }
}
