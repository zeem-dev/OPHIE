// ============================================================
//  OPHIO — TotalOverloadAbility
//  Day 5 | Hawk Super Ability
//  360 degree electric explosion scaled by current charge.
//  8m mein sab enemies 3 seconds stun.
//  Baad mein Hawk brief overload state mein — enhanced stats.
//  Player GameObject par lagao.
// ============================================================

using System.Collections;
using UnityEngine;
using Invector;
using Invector.vCharacterController;

namespace OPHIO.Abilities
{
    public class TotalOverloadAbility : MonoBehaviour
    {
        [Header("Overload Settings")]
        public float baseExplosionDamage = 120f;
        public float explosionRadius     = 8f;
        public float stunDuration        = 3f;

        [Header("Overload State (post-explosion)")]
        public float overloadDuration    = 3f;
        public float overloadSpeedBonus  = 0.20f;   // 20%
        public float overloadDmgBonus    = 0.25f;   // 25%

        [Header("VFX")]
        public GameObject chargeBuildVFXPrefab;    // wind-up particles
        public GameObject explosionVFXPrefab;      // main burst
        public GameObject overloadAuraPrefab;      // post-explosion glow
        public GameObject shockwaveRingPrefab;     // expanding ring

        [Header("Audio")]
        public AudioClip chargeUpSound;
        public AudioClip explosionSound;
        public AudioClip overloadHumSound;

        [Header("Camera Shake")]
        public float shakeDuration       = 0.4f;
        public float shakeMagnitude      = 0.3f;

        // State
        public bool IsOverloadActive     { get; private set; }

        private Characters.HawkCharacter _hawk;
        private Core.AbilityExecutor     _executor;
        private vThirdPersonController   _tpc;
        private Animator                 _animator;
        private Core.EnergyManager       _energy;

        private float _origWalk;
        private float _origRun;
        private float _origAnimSpeed;
        private GameObject _overloadAura;

        private const string k_ExplosionKey  = "TotalOverloadVFX";
        private const string k_AuraKey       = "OverloadAura";
        private const string k_RingKey       = "ShockwaveRing";
        private const string k_BuildKey      = "OverloadBuild";

        private void Awake()
        {
            _hawk     = GetComponent<Characters.HawkCharacter>();
            _executor = GetComponent<Core.AbilityExecutor>();
            _tpc      = GetComponent<vThirdPersonController>();
            _animator = GetComponent<Animator>();
            _energy   = GetComponent<Core.EnergyManager>();
        }

        private void Start()
        {
            if (_tpc != null)
            {
                _origWalk     = _tpc.freeSpeed.walkSpeed;
                _origRun      = _tpc.freeSpeed.runningSpeed;
            }
            if (_animator != null)
                _origAnimSpeed = _animator.speed;

            RegisterPools();
        }

        private void RegisterPools()
        {
            if (explosionVFXPrefab  != null) Core.ObjectPoolManager.Instance.RegisterPool(k_ExplosionKey, explosionVFXPrefab, 2);
            if (overloadAuraPrefab  != null) Core.ObjectPoolManager.Instance.RegisterPool(k_AuraKey,      overloadAuraPrefab, 2);
            if (shockwaveRingPrefab != null) Core.ObjectPoolManager.Instance.RegisterPool(k_RingKey,      shockwaveRingPrefab,2);
            if (chargeBuildVFXPrefab!= null) Core.ObjectPoolManager.Instance.RegisterPool(k_BuildKey,     chargeBuildVFXPrefab,2);
        }

        public void Activate(Core.AbilityData data)
        {
            if (IsOverloadActive) return;
            StartCoroutine(TotalOverloadRoutine());
        }

        private IEnumerator TotalOverloadRoutine()
        {
            IsOverloadActive = true;

            // --- WIND-UP PHASE (0.6s) ---
            if (_animator != null) _animator.SetTrigger("TotalOverload");
            if (chargeUpSound != null)
                AudioSource.PlayClipAtPoint(chargeUpSound, transform.position);

            // Spawn build-up VFX
            SpawnPooled(k_BuildKey, transform.position, 0.8f);

            yield return new WaitForSeconds(0.6f);

            // --- EXPLOSION PHASE ---
            float chargeSnapshot = _hawk != null ? _hawk.CurrentCharge : 50f;
            float chargeMult     = Mathf.Lerp(0.5f, 2f, chargeSnapshot / 100f); // 0.5x at 0 charge, 2x at full
            float finalDamage    = baseExplosionDamage * chargeMult;

            // Consume all charge
            _hawk?.ConsumeAllCharge();

            // Explosion VFX + ring
            SpawnPooled(k_ExplosionKey,  transform.position, 3f);
            SpawnPooled(k_RingKey,       transform.position, 2f);

            if (explosionSound != null)
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);

            // Camera shake
            StartCoroutine(CameraShake());

            // Hit all enemies in radius
            var hits = Physics.OverlapSphere(transform.position, explosionRadius,
                _executor != null ? _executor.enemyLayer : ~0);

            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;

                // Damage
                var dmg = new vDamage((int)finalDamage)
                {
                    damageType    = "Electric",
                    hitReaction   = true,
                    activeRagdoll = false,
                    sender        = transform
                };
                health.TakeDamage(dmg);

                // 3-second stun (Shock)
                Core.StatusEffectManager.Instance?.Apply(
                    health, Core.StatusEffectType.Shock,
                    stunDuration, 3, transform);
            }

            Debug.Log($"[TotalOverload] Hit {hits.Length} enemies for {finalDamage:F0} damage (charge mult: {chargeMult:F2}x)");

            // --- OVERLOAD STATE ---
            yield return new WaitForSeconds(0.2f);

            // Apply overload buffs
            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed    *= (1f + overloadSpeedBonus);
                _tpc.freeSpeed.runningSpeed *= (1f + overloadSpeedBonus);
            }
            if (_animator != null)
                _animator.speed *= (1f + overloadDmgBonus);

            if (overloadHumSound != null)
                AudioSource.PlayClipAtPoint(overloadHumSound, transform.position, 0.6f);

            // Overload aura (attach to player)
            if (overloadAuraPrefab != null)
            {
                _overloadAura = Core.ObjectPoolManager.Instance.Spawn(
                    k_AuraKey, transform.position, Quaternion.identity);
                if (_overloadAura != null)
                    _overloadAura.transform.SetParent(transform);
            }

            yield return new WaitForSeconds(overloadDuration);

            // Remove overload buffs
            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed    = _origWalk;
                _tpc.freeSpeed.runningSpeed = _origRun;
            }
            if (_animator != null)
                _animator.speed = _origAnimSpeed;

            // Remove aura
            if (_overloadAura != null)
            {
                _overloadAura.transform.SetParent(null);
                Core.ObjectPoolManager.Instance.Despawn(k_AuraKey, _overloadAura);
                _overloadAura = null;
            }

            IsOverloadActive = false;
        }

        private void SpawnPooled(string key, Vector3 pos, float lifetime)
        {
            var obj = Core.ObjectPoolManager.Instance.Spawn(key, pos, Quaternion.identity);
            if (obj != null) Core.ObjectPoolManager.Instance.Despawn(key, obj, lifetime);
        }

        private IEnumerator CameraShake()
        {
            // Never move Invector camera transform directly — it breaks the camera state
            // and causes the player to appear to sink into the ground.
            // Instead we shake the Cinemachine/Invector pivot offset or
            // fall back to a pure screen-space shake via a custom component.

            var invCam = Invector.vCamera.vThirdPersonCamera.instance;
            if (invCam == null) yield break;

            // Shake via the camera's own offsetSmooth field — X only, never Y, never Z.
            // This stays inside Invector's pipeline so it restores cleanly.
            float elapsed         = 0f;
            float originalOffsetX = invCam.switchRight;   // horizontal offset Invector exposes

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t      = elapsed / shakeDuration;
                float falloff = 1f - t;                   // shake fades out
                invCam.switchRight = originalOffsetX
                    + Random.Range(-shakeMagnitude, shakeMagnitude) * falloff;
                yield return null;
            }

            invCam.switchRight = originalOffsetX;
        }
    }
}
