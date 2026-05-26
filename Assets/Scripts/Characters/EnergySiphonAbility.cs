// ============================================================
//  OPHIO — EnergySiphonAbility
//  Hawk Abilities
//  Passive trigger — melee hit par sword target se
//  energy drain karta hai, Hawk ka charge refill karta hai.
//  Toggle ON/OFF as a loadout slot ability.
//  Attach on Player GameObject
// ============================================================

using UnityEngine;

namespace OPHIO.Abilities
{
    public class EnergySiphonAbility : MonoBehaviour
    {
        [Header("Siphon Settings")]
        [Tooltip("Charge restored per melee hit while siphon is active")]
        public float chargePerHit       = 12f;
        [Tooltip("Energy restored per melee hit while siphon is active")]
        public float energyPerHit       = 5f;
        [Tooltip("Duration of siphon window (0 = permanent until slot swap)")]
        public float siphonDuration     = 0f;

        [Header("VFX")]
        public GameObject siphonVFXPrefab;  // small drain particle on hit

        // State
        public bool IsActive            { get; private set; }

        private Characters.HawkCharacter _hawk;
        private Core.EnergyManager       _energy;
        private float                    _timer;
        private string                   _vfxKey = "SiphonVFX";

        private void Awake()
        {
            _hawk   = GetComponent<Characters.HawkCharacter>();
            _energy = GetComponent<Core.EnergyManager>();
        }

        private void Start()
        {
            if (siphonVFXPrefab != null)
                Core.ObjectPoolManager.Instance.RegisterPool(_vfxKey, siphonVFXPrefab, 5);
        }

        private void Update()
        {
            if (!IsActive || siphonDuration <= 0f) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f) Deactivate();
        }

        public void Activate(Core.AbilityData data)
        {
            IsActive = true;
            _timer   = siphonDuration > 0f ? siphonDuration : float.MaxValue;
            Debug.Log("[EnergySiphon] Siphon activated — melee hits will drain charge.");
        }

        public void Deactivate()
        {
            IsActive = false;
            Debug.Log("[EnergySiphon] Siphon deactivated.");
        }

        // Called by HawkCharacter.OnMeleeHit
        public void OnMeleeHit(Invector.vDamage damage)
        {
            if (!IsActive) return;

            // Restore charge
            _hawk?.AddCharge(chargePerHit);

            // Restore energy
            _energy?.RestoreEnergy(energyPerHit);

            // VFX at hit point
            if (siphonVFXPrefab != null && damage.receiver != null)
            {
                var vfx = Core.ObjectPoolManager.Instance.Spawn(
                    _vfxKey, damage.receiver.position, Quaternion.identity);
                Core.ObjectPoolManager.Instance.Despawn(_vfxKey, vfx, 0.8f);
            }
        }
    }
}
