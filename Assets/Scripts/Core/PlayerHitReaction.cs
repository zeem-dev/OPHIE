// ============================================================
//  OPHIO - PlayerHitReaction
//  Keeps hit reactions responsive while preserving Animator root motion.
//  Ground correction is handled by PlayerGroundingGuard.
// ============================================================

using System.Collections;
using UnityEngine;
using Invector;
using Invector.vCharacterController;

namespace OPHIO.Core
{
    public class PlayerHitReaction : MonoBehaviour
    {
        [Header("Animator Parameters")]
        public string hitTriggerName    = "HitReaction";
        public string hitDirectionParam = "HitAngle";
        public string isDeadParam       = "IsDead";

        [Header("Hit Reaction Settings")]
        public float minDamageForAnim = 1f;
        public float hitAnimCooldown  = 0.3f;
        [Tooltip("Kept for old prefab compatibility. Root motion is no longer disabled.")]
        public float hitAnimDuration  = 0.35f;
        [Tooltip("Keep OFF until hit reaction clips are cleaned of vertical root motion.")]
        public bool playHitAnimation = false;
        [Tooltip("Prevent Invector damage from launching or ragdolling the player.")]
        public bool suppressInvectorHitReaction = true;

        [Header("Ground Snap")]
        [Tooltip("Ensure the grounding guard is active after hit reactions.")]
        public bool      enableGroundSnap = true;
        public LayerMask groundLayer      = ~0;
        public float     groundSnapRange  = 2f;

        [Header("Screen Flash")]
        public bool  enableScreenFlash = true;
        public float flashDuration     = 0.08f;

        private Animator               _animator;
        private vThirdPersonController _tpc;
        private vHealthController      _health;
        private PlayerGroundingGuard   _groundingGuard;
        private float                  _lastHitTime;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _tpc      = GetComponent<vThirdPersonController>();
            _health   = GetComponent<vHealthController>();

            _groundingGuard = GetComponent<PlayerGroundingGuard>();
            if (_groundingGuard == null)
                _groundingGuard = gameObject.AddComponent<PlayerGroundingGuard>();
        }

        private void Start()
        {
            if (_health == null) return;
            _health.onReceiveDamage.AddListener(OnReceiveDamage);
            _health.onDead.AddListener(OnDead);
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.onReceiveDamage.RemoveListener(OnReceiveDamage);
                _health.onDead.RemoveListener(OnDead);
            }
        }

        private void OnReceiveDamage(vDamage damage)
        {
            if (damage == null) return;
            if (damage.damageValue < minDamageForAnim) return;
            if (Time.time - _lastHitTime < hitAnimCooldown) return;
            _lastHitTime = Time.time;

            if (suppressInvectorHitReaction)
            {
                damage.hitReaction = false;
                damage.activeRagdoll = false;
            }

            float hitAngle = 0f;
            if (damage.sender != null)
            {
                Vector3 dir = (damage.sender.position - transform.position).normalized;
                hitAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            }

            if (playHitAnimation)
                TriggerHitAnim(hitAngle);

            if (_animator != null && !_animator.applyRootMotion)
                _animator.applyRootMotion = true;

            if (enableGroundSnap && playHitAnimation)
                StartCoroutine(GroundSnapRoutine());

            if (enableScreenFlash)
                StartCoroutine(ScreenFlash());
        }

        private IEnumerator GroundSnapRoutine()
        {
            yield return new WaitForSeconds(0.05f);
            yield return new WaitForEndOfFrame();

            if (_groundingGuard != null)
            {
                _groundingGuard.RequestClamp(hitAnimDuration);
                _groundingGuard.ForceClampNow();
            }
        }

        private void TriggerHitAnim(float angle)
        {
            if (_animator == null) return;
            TrySetFloat("HitReactionAngle", angle);
            TrySetFloat(hitDirectionParam, angle);
            TrySetFloat("Random", Random.Range(0f, 1f));
            TrySetTrigger("HitReaction");
            TrySetTrigger(hitTriggerName);
        }

        private void TrySetTrigger(string n)
        {
            int h = Animator.StringToHash(n);
            if (_animator.HasParameter(h)) _animator.SetTrigger(h);
        }

        private void TrySetFloat(string n, float v)
        {
            int h = Animator.StringToHash(n);
            if (_animator.HasParameter(h)) _animator.SetFloat(h, v);
        }

        private void TrySetBool(string n, bool v)
        {
            int h = Animator.StringToHash(n);
            if (_animator.HasParameter(h)) _animator.SetBool(h, v);
        }

        private void OnDead(GameObject go)
        {
            TrySetBool(isDeadParam, true);
        }

        private IEnumerator ScreenFlash()
        {
            var obj = GameObject.Find("DamageFlash");
            if (obj == null) yield break;
            var img = obj.GetComponent<UnityEngine.UI.Image>();
            if (img == null) yield break;
            img.color = new Color(1f, 0f, 0f, 0.35f);
            yield return new WaitForSeconds(flashDuration);
            img.color = Color.clear;
        }
    }
}

// ============================================================
//  Animator HasParameter extension
// ============================================================
public static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, int hash)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }

    public static bool HasParameter(this Animator animator, string name)
        => animator.HasParameter(Animator.StringToHash(name));
}
