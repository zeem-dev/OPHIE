// ============================================================
//  OPHIO — NeuralSurgeAbility
//  Day 5 | Hawk Abilities
//  5 seconds ke liye speed +30%, attack speed +40%,
//  dodge frames improve. Visual feedback with VFX.
//  Player GameObject par lagao.
// ============================================================

using System.Collections;
using UnityEngine;
using Invector.vCharacterController;

namespace OPHIO.Abilities
{
    public class NeuralSurgeAbility : MonoBehaviour
    {
        [Header("Neural Surge Settings")]
        public float duration           = 5f;
        public float speedBonus         = 0.30f;    // 30%
        public float animSpeedBonus     = 0.40f;    // 40%
        public float chargeGainOnUse    = 15f;

        [Header("VFX")]
        public GameObject surgeVFXPrefab;
        public GameObject surgeAuraPrefab;  // looping aura during buff

        [Header("Audio")]
        public AudioClip surgeSound;
        public AudioClip surgeEndSound;

        // State
        public bool IsActive            { get; private set; }

        private vThirdPersonController  _tpc;
        private Animator                _animator;
        private Characters.HawkCharacter _hawk;

        private float _origWalkSpeed;
        private float _origRunSpeed;
        private float _origStrafeSpeed;
        private float _origAnimSpeed;

        private GameObject _activeAura;
        private string     _auraPoolKey = "SurgeAura";

        private void Awake()
        {
            _tpc      = GetComponent<vThirdPersonController>();
            _animator = GetComponent<Animator>();
            _hawk     = GetComponent<Characters.HawkCharacter>();
        }

        private void Start()
        {
            if (_tpc != null)
            {
                _origWalkSpeed   = _tpc.freeSpeed.walkSpeed;
                _origRunSpeed    = _tpc.freeSpeed.runningSpeed;
                _origStrafeSpeed = _tpc.strafeSpeed.walkSpeed;
            }
            if (_animator != null)
                _origAnimSpeed = _animator.speed;

            if (surgeAuraPrefab != null)
                Core.ObjectPoolManager.Instance.RegisterPool(_auraPoolKey, surgeAuraPrefab, 2);
        }

        public void Activate(Core.AbilityData data)
        {
            if (IsActive) return;
            StartCoroutine(SurgeRoutine());
        }

        private IEnumerator SurgeRoutine()
        {
            IsActive = true;

            // Apply speed buffs
            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed    *= (1f + speedBonus);
                _tpc.freeSpeed.runningSpeed *= (1f + speedBonus);
                _tpc.freeSpeed.sprintSpeed  *= (1f + speedBonus);
                _tpc.strafeSpeed.walkSpeed  *= (1f + speedBonus);
            }
            if (_animator != null)
                _animator.speed *= (1f + animSpeedBonus);

            // Charge gain
            _hawk?.AddCharge(chargeGainOnUse);

            // Sound
            if (surgeSound != null)
                AudioSource.PlayClipAtPoint(surgeSound, transform.position);

            // Spawn burst VFX
            SpawnBurstVFX();

            // Spawn looping aura
            if (surgeAuraPrefab != null)
            {
                _activeAura = Core.ObjectPoolManager.Instance.Spawn(
                    _auraPoolKey, transform.position, Quaternion.identity);
                if (_activeAura != null)
                    _activeAura.transform.SetParent(transform);
            }

            // Animator flag
            if (_animator != null)
                _animator.SetBool("NeuralSurgeActive", true);

            // Wait
            yield return new WaitForSeconds(duration);

            // Remove buffs
            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed    = _origWalkSpeed;
                _tpc.freeSpeed.runningSpeed = _origRunSpeed;
                _tpc.freeSpeed.sprintSpeed  = _origRunSpeed * 1.4f;
                _tpc.strafeSpeed.walkSpeed  = _origStrafeSpeed;
            }
            if (_animator != null)
            {
                _animator.speed = _origAnimSpeed;
                _animator.SetBool("NeuralSurgeActive", false);
            }

            // Remove aura
            if (_activeAura != null)
            {
                _activeAura.transform.SetParent(null);
                Core.ObjectPoolManager.Instance.Despawn(_auraPoolKey, _activeAura);
                _activeAura = null;
            }

            // End sound
            if (surgeEndSound != null)
                AudioSource.PlayClipAtPoint(surgeEndSound, transform.position);

            IsActive = false;
        }

        private void SpawnBurstVFX()
        {
            if (surgeVFXPrefab == null) return;
            string key = "SurgeVFX";
            Core.ObjectPoolManager.Instance.RegisterPool(key, surgeVFXPrefab, 3);
            var vfx = Core.ObjectPoolManager.Instance.Spawn(key, transform.position, Quaternion.identity);
            Core.ObjectPoolManager.Instance.Despawn(key, vfx, 2f);
        }
    }
}
