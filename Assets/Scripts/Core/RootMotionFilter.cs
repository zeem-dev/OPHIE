// ============================================================
//  OPHIO - RootMotionFilter
//  Deprecated safety snap. Keep disabled by default because
//  Invector already owns grounded locomotion and root motion.
// ============================================================

using UnityEngine;
using Invector.vCharacterController;

namespace OPHIO.Core
{
    public class RootMotionFilter : MonoBehaviour
    {
        [Header("Ground Snap")]
        [Tooltip("Deprecated. Leave OFF unless debugging a specific prefab.")]
        public bool enableGroundSnap = false;
        public LayerMask groundLayer = (1 << 0) | (1 << 19);
        [Tooltip("How far below ground the controller bottom must be before snap triggers.")]
        public float snapThreshold = 0.05f;
        [Tooltip("How fast to snap back up.")]
        public float snapSpeed = 20f;

        private CharacterController _cc;
        private vThirdPersonController _tpc;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _tpc = GetComponent<vThirdPersonController>();
        }

        private void LateUpdate()
        {
            if (!enableGroundSnap) return;
            if (_cc == null || !_cc.enabled) return;

            SnapToGround();
        }

        private void SnapToGround()
        {
            float halfHeight = (_cc.height * Mathf.Abs(transform.lossyScale.y)) * 0.5f;
            float centerY = transform.TransformPoint(_cc.center).y;
            float bottomY = centerY - halfHeight;
            Vector3 origin = new Vector3(transform.position.x, bottomY + 0.35f, transform.position.z);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 0.5f, groundLayer))
                return;

            float groundY = hit.point.y;
            if (bottomY >= groundY - snapThreshold)
                return;

            float targetY = transform.position.y + (groundY - bottomY);
            Vector3 corrected = transform.position;
            corrected.y = Mathf.MoveTowards(transform.position.y, targetY, snapSpeed * Time.deltaTime);

            _cc.enabled = false;
            transform.position = corrected;
            _cc.enabled = true;
        }
    }
}
