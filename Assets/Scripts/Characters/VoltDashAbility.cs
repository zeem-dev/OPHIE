// ============================================================
//  OPHIO — VoltDashAbility
//  Day 5 | Hawk Abilities
//  Directional dash + electric trail jo damage karta hai.
//  Trail mein ghusne wale enemies Shock lete hain.
//  Player GameObject par lagao.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Invector.vCharacterController;

namespace OPHIO.Abilities
{
    public class VoltDashAbility : MonoBehaviour
    {
        [Header("Dash Settings")]
        public float dashDistance       = 6f;
        public float dashDuration       = 0.18f;   // seconds
        public float invincibleFrames   = 0.15f;   // brief i-frames during dash

        [Header("Trail Settings")]
        public float trailDamage        = 20f;
        public float trailDuration      = 2.5f;
        public float trailTickRate      = 0.4f;    // damage every 0.4s
        public float trailWidth         = 1.5f;    // overlap radius
        public float shockDuration      = 1.5f;
        public int   trailPoints        = 5;       // how many trail segments

        [Header("VFX")]
        public GameObject dashVFXPrefab;
        public GameObject trailSegmentPrefab;  // one segment of the trail
        public GameObject trailHitVFXPrefab;

        [Header("Audio")]
        public AudioClip dashSound;
        public AudioClip trailHitSound;

        // State
        public bool IsDashing           { get; private set; }

        private vThirdPersonController  _tpc;
        private CharacterController     _cc;
        private Characters.HawkCharacter _hawk;
        private Core.AbilityExecutor    _executor;
        private Animator                _animator;

        private List<Vector3>           _trailPositions = new List<Vector3>();
        private string _trailKey  = "VoltTrailSeg";
        private string _dashVFXKey= "VoltDashVFX";
        private string _hitVFXKey = "VoltTrailHit";

        private void Awake()
        {
            _tpc      = GetComponent<vThirdPersonController>();
            _cc       = GetComponent<CharacterController>();
            _hawk     = GetComponent<Characters.HawkCharacter>();
            _executor = GetComponent<Core.AbilityExecutor>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (trailSegmentPrefab != null)
                Core.ObjectPoolManager.Instance.RegisterPool(_trailKey, trailSegmentPrefab, trailPoints + 2);
            if (dashVFXPrefab != null)
                Core.ObjectPoolManager.Instance.RegisterPool(_dashVFXKey, dashVFXPrefab, 3);
            if (trailHitVFXPrefab != null)
                Core.ObjectPoolManager.Instance.RegisterPool(_hitVFXKey, trailHitVFXPrefab, 5);
        }

        public void Activate(Core.AbilityData data)
        {
            if (IsDashing) return;
            StartCoroutine(DashRoutine(data));
        }

        private IEnumerator DashRoutine(Core.AbilityData data)
        {
            IsDashing = true;
            _trailPositions.Clear();

            // Get dash direction from input
            Vector3 dir = GetDashDirection();

            // Dash VFX
            if (dashVFXPrefab != null)
            {
                var dvfx = Core.ObjectPoolManager.Instance.Spawn(_dashVFXKey, transform.position, transform.rotation);
                Core.ObjectPoolManager.Instance.Despawn(_dashVFXKey, dvfx, 1f);
            }

            if (dashSound != null)
                AudioSource.PlayClipAtPoint(dashSound, transform.position);

            if (_animator != null)
                _animator.SetTrigger("VoltDash");

            // Move player over dashDuration
            float elapsed     = 0f;
            float segInterval = dashDuration / trailPoints;
            float nextSeg     = 0f;

            Vector3 startPos  = transform.position;
            Vector3 endPos    = startPos + dir * dashDistance;

            while (elapsed < dashDuration)
            {
                float t = elapsed / dashDuration;
                Vector3 target = Vector3.Lerp(startPos, endPos, t);

                if (_cc != null)
                    _cc.Move((target - transform.position));
                else
                    transform.position = target;

                // Record trail positions
                if (elapsed >= nextSeg)
                {
                    _trailPositions.Add(transform.position);
                    SpawnTrailSegment(transform.position);
                    nextSeg += segInterval;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Gain charge
            _hawk?.AddCharge(20f);
            _hawk?.SetCombatState(true);

            IsDashing = false;

            // Trail damage coroutine
            StartCoroutine(TrailDamageRoutine(new List<Vector3>(_trailPositions)));
        }

        private IEnumerator TrailDamageRoutine(List<Vector3> positions)
        {
            float elapsed = 0f;
            while (elapsed < trailDuration)
            {
                foreach (var pos in positions)
                {
                    var hits = Physics.OverlapSphere(pos, trailWidth, _executor != null ? _executor.enemyLayer : ~0);
                    foreach (var hit in hits)
                    {
                        var health = hit.GetComponent<Invector.vHealthController>();
                        if (health == null || health.isDead) continue;

                        var dmg = new Invector.vDamage((int)trailDamage)
                        {
                            damageType  = "Electric",
                            hitReaction = false,
                            sender      = transform
                        };
                        health.TakeDamage(dmg);

                        Core.StatusEffectManager.Instance?.Apply(
                            health, Core.StatusEffectType.Shock,
                            shockDuration, 1, transform);

                        // Hit VFX
                        if (trailHitVFXPrefab != null)
                        {
                            var hvfx = Core.ObjectPoolManager.Instance.Spawn(
                                _hitVFXKey, hit.transform.position, Quaternion.identity);
                            Core.ObjectPoolManager.Instance.Despawn(_hitVFXKey, hvfx, 0.5f);
                        }

                        if (trailHitSound != null)
                            AudioSource.PlayClipAtPoint(trailHitSound, hit.transform.position, 0.5f);
                    }
                }

                elapsed += trailTickRate;
                yield return new WaitForSeconds(trailTickRate);
            }

            // Despawn trail segments
            // (segments auto-despawn via their own lifetime)
        }

        private void SpawnTrailSegment(Vector3 pos)
        {
            if (trailSegmentPrefab == null) return;
            var seg = Core.ObjectPoolManager.Instance.Spawn(_trailKey, pos, Quaternion.identity);
            Core.ObjectPoolManager.Instance.Despawn(_trailKey, seg, trailDuration);
        }

        private Vector3 GetDashDirection()
        {
            Vector3 dir = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) dir += transform.forward;
            if (Input.GetKey(KeyCode.S)) dir -= transform.forward;
            if (Input.GetKey(KeyCode.A)) dir -= transform.right;
            if (Input.GetKey(KeyCode.D)) dir += transform.right;
            if (dir == Vector3.zero)     dir  = transform.forward;
            dir.y = 0f;
            return dir.normalized;
        }
    }
}
