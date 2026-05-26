// ============================================================
//  OPHIO — HawkCombatController
//  Combat Polish
//  Hawk ke saare combat systems ek jagah coordinate karta hai:
//  ComboSystem, ChargeSystem, AbilityExecutor.
//  Invector melee hits detect karke ComboSystem feed karta hai.
//  Attach on Player GameObject
// ============================================================

using UnityEngine;
using Invector;
using Invector.vMelee;

namespace OPHIO.Characters
{
    public class HawkCombatController : MonoBehaviour
    {
        // --------------------------------------------------
        //  References
        // --------------------------------------------------
        private Core.ComboSystem        _combo;
        private HawkCharacter           _hawk;
        private Core.EnergyManager      _energy;
        private Core.AbilityExecutor    _executor;
        private vMeleeManager           _meleeManager;
        private Animator                _animator;

        [Header("Combo VFX")]
        public GameObject comboTextVFXPrefab;   // "x2!", "x3!" floating text
        public GameObject mixedBonusVFXPrefab;  // special burst on mixed combo

        [Header("Combo Audio")]
        public AudioClip[] comboHitSounds;      // different sound per step
        public AudioClip   mixedBonusSound;

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            _combo        = GetComponent<Core.ComboSystem>();
            _hawk         = GetComponent<HawkCharacter>();
            _energy       = GetComponent<Core.EnergyManager>();
            _executor     = GetComponent<Core.AbilityExecutor>();
            _meleeManager = GetComponent<vMeleeManager>();
            _animator     = GetComponent<Animator>();
        }

        private void Start()
        {
            // Hook ComboSystem events
            if (_combo != null)
            {
                _combo.onComboStep   += OnComboStep;
                _combo.onComboBreak  += OnComboBreak;
                _combo.onMixedBonus  += OnMixedBonus;
            }

            // Hook damage received — breaks combo
            var tpc = GetComponent<Invector.vCharacterController.vThirdPersonController>();
            if (tpc != null)
                tpc.onReceiveDamage.AddListener(OnPlayerHit);

            // Hook melee hit — feeds combo
            if (_meleeManager != null)
                _meleeManager.onDamageHit.AddListener(OnMeleeDamageHit);
        }

        // --------------------------------------------------
        //  Melee hit — feed into combo
        // --------------------------------------------------
        private void OnMeleeDamageHit(vHitInfo hitInfo)
        {
            if (_combo == null) return;

            // Get damage type from Hawk's primary type
            var dmgType = _hawk != null
                ? Core.DamageType.Electric
                : Core.DamageType.Physical;

            float boostedDamage = _combo.RegisterHit(dmgType, _hawk != null
                ? _hawk.GetChargeDamageMultiplier() * 25f
                : 25f);

            // Charge gain on combo hit
            _hawk?.AddCharge(2f * _combo.ComboCount);
            _hawk?.SetCombatState(true);
        }

        // --------------------------------------------------
        //  Ability hit — also feeds combo (called from
        //  ArcSlashAbility, DischargeAbility, etc.)
        // --------------------------------------------------
        public void RegisterAbilityHit(Core.DamageType type, float damage)
        {
            if (_combo == null) return;
            _combo.RegisterHit(type, damage);
            _hawk?.SetCombatState(true);
        }

        // --------------------------------------------------
        //  Combo events
        // --------------------------------------------------
        private void OnComboStep(int count)
        {
            // Play step sound
            if (comboHitSounds != null && comboHitSounds.Length > 0)
            {
                int idx = Mathf.Min(count - 1, comboHitSounds.Length - 1);
                if (comboHitSounds[idx] != null)
                    AudioSource.PlayClipAtPoint(comboHitSounds[idx], transform.position, 0.7f);
            }

            // Combo VFX
            if (comboTextVFXPrefab != null && count >= 2)
            {
                string key = "ComboVFX";
                Core.ObjectPoolManager.Instance.RegisterPool(key, comboTextVFXPrefab, 5);
                var vfx = Core.ObjectPoolManager.Instance.Spawn(
                    key, transform.position + Vector3.up * 2f, Quaternion.identity);
                Core.ObjectPoolManager.Instance.Despawn(key, vfx, 1.5f);
            }

            // Animator combo layer
            if (_animator != null)
                _animator.SetInteger("ComboCount", count);

            Debug.Log($"[Combo] x{count} | Damage multiplier: {_combo.ComboDamageMultiplier:F2}x");
        }

        private void OnComboBreak()
        {
            if (_animator != null)
                _animator.SetInteger("ComboCount", 0);
        }

        private void OnMixedBonus(float bonus)
        {
            if (mixedBonusVFXPrefab != null)
            {
                string key = "MixedBonusVFX";
                Core.ObjectPoolManager.Instance.RegisterPool(key, mixedBonusVFXPrefab, 3);
                var vfx = Core.ObjectPoolManager.Instance.Spawn(
                    key, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                Core.ObjectPoolManager.Instance.Despawn(key, vfx, 2f);
            }

            if (mixedBonusSound != null)
                AudioSource.PlayClipAtPoint(mixedBonusSound, transform.position);

            Debug.Log($"[Combo] Mixed damage type bonus! +{bonus * 100:F0}% damage");
        }

        private void OnPlayerHit(vDamage damage)
        {
            _combo?.OnPlayerHit();
        }
    }
}
