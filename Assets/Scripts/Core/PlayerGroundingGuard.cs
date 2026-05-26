// ============================================================
//  OPHIO - PlayerGroundingGuard
//  Keeps root-motion driven player prefabs from sinking below
//  the ground when attack or hit animations contain bad Y motion.
// ============================================================

using UnityEngine;
using Invector.vMelee;

namespace OPHIO.Core
{
    [DisallowMultipleComponent]
    public class PlayerGroundingGuard : MonoBehaviour
    {
        [Header("Root Motion")]
        [Tooltip("Keep Animator.applyRootMotion enabled. This does not disable root motion.")]
        public bool keepRootMotionEnabled = true;

        [Header("Ground Clamp")]
        public bool enableGroundClamp = false;
        public LayerMask groundLayer = (1 << 0) | (1 << 19);
        [Tooltip("Allowed distance below ground before correction starts.")]
        public float sinkTolerance = 0.025f;
        [Tooltip("Small clearance kept between controller bottom and ground.")]
        public float groundOffset = 0.01f;
        [Tooltip("How far below the player we search for ground.")]
        public float maxGroundSearchDistance = 0.75f;
        [Tooltip("Maximum upward correction in one frame.")]
        public float maxCorrectionPerFrame = 0.08f;

        private Animator _animator;
        private CharacterController _controller;
        private vMeleeManager _meleeManager;
        private readonly RaycastHit[] _hits = new RaycastHit[12];
        private float _clampUntilTime;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<CharacterController>();
            _meleeManager = GetComponent<vMeleeManager>();
        }

        private void OnEnable()
        {
            if (_meleeManager != null)
                _meleeManager.onDamageHit.AddListener(OnMeleeDamageHit);
        }

        private void OnDisable()
        {
            if (_meleeManager != null)
                _meleeManager.onDamageHit.RemoveListener(OnMeleeDamageHit);
        }

        private void LateUpdate()
        {
            if (keepRootMotionEnabled && _animator != null && !_animator.applyRootMotion)
                _animator.applyRootMotion = true;

            if (enableGroundClamp || Time.time <= _clampUntilTime)
                ClampControllerAboveGround(maxCorrectionPerFrame);
        }

        public void RequestClamp(float duration = 0.35f)
        {
            _clampUntilTime = Mathf.Max(_clampUntilTime, Time.time + duration);
        }

        public void ForceClampNow()
        {
            ClampControllerAboveGround(float.MaxValue);
        }

        private void OnMeleeDamageHit(vHitInfo hitInfo)
        {
            RequestClamp(0.35f);
        }

        private void ClampControllerAboveGround(float maxCorrection)
        {
            if (_controller == null || !_controller.enabled)
                return;

            if (!TryFindGround(out RaycastHit groundHit))
                return;

            float halfHeight = (_controller.height * Mathf.Abs(transform.lossyScale.y)) * 0.5f;
            float centerY = transform.TransformPoint(_controller.center).y;
            float bottomY = centerY - halfHeight;
            float targetBottomY = groundHit.point.y + groundOffset;

            if (bottomY >= targetBottomY - sinkTolerance)
                return;

            Vector3 pos = transform.position;
            float correction = Mathf.Min(targetBottomY - bottomY, maxCorrection);
            pos.y += correction;

            _controller.enabled = false;
            transform.position = pos;
            _controller.enabled = true;
        }

        private bool TryFindGround(out RaycastHit groundHit)
        {
            float scaledHeight = _controller.height * Mathf.Abs(transform.lossyScale.y);
            Vector3 origin = transform.position + Vector3.up * (scaledHeight + 0.5f);
            float rayDistance = scaledHeight + maxGroundSearchDistance + 0.5f;

            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                _hits,
                rayDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore);

            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Transform hitTransform = _hits[i].transform;
                if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;

                if (IsDynamicCharacterLayer(hitTransform.gameObject.layer))
                    continue;

                if (_hits[i].distance < bestDistance)
                {
                    bestDistance = _hits[i].distance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                groundHit = _hits[bestIndex];
                return true;
            }

            groundHit = new RaycastHit();
            return false;
        }

        private bool IsDynamicCharacterLayer(int layer)
        {
            return layer == 8 || layer == 9 || layer == 10 || layer == 11 || layer == 15;
        }
    }
}
