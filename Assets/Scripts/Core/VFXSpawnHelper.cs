// ============================================================
//  OPHIO — VFXSpawnHelper
//  VFX Fix | Ability Core
//  Centralized VFX spawn position resolver.
//  Fixes: VFX going underground, spawning at wrong location.
//  Attach to the Player GameObject alongside AbilityExecutor.
// ============================================================

using UnityEngine;

namespace OPHIO.Core
{
    public class VFXSpawnHelper : MonoBehaviour
    {
        [Header("Spawn Points")]
        [Tooltip("Chest-level point — default cast VFX origin")]
        public Transform chestPoint;

        [Tooltip("Right hand / weapon tip — for melee ability VFX")]
        public Transform rightHandPoint;

        [Tooltip("Left hand point")]
        public Transform leftHandPoint;

        [Tooltip("Feet / ground level point")]
        public Transform feetPoint;

        [Tooltip("Above head point — for aura / buff VFX")]
        public Transform aboveHeadPoint;

        [Header("Fallback Offsets (used when Transform refs are null)")]
        public Vector3 chestOffset     = new Vector3(0f,  1.4f, 0f);
        public Vector3 rightHandOffset = new Vector3(0.5f,1.0f, 0.6f);
        public Vector3 feetOffset      = new Vector3(0f,  0.1f, 0f);
        public Vector3 aboveHeadOffset = new Vector3(0f,  2.2f, 0f);
        public Vector3 forwardOffset   = new Vector3(0f,  1.2f, 1.0f);

        // --------------------------------------------------
        //  Resolve spawn position by ability targeting type
        // --------------------------------------------------
        public Vector3 GetCastPosition(AbilityTargeting targeting)
        {
            switch (targeting)
            {
                case AbilityTargeting.AreaAroundSelf:
                    // AoE burst — at chest, slightly elevated
                    return chestPoint != null
                        ? chestPoint.position
                        : transform.position + chestOffset;

                case AbilityTargeting.SingleTarget:
                case AbilityTargeting.Projectile:
                    // Fires from right hand / muzzle
                    return rightHandPoint != null
                        ? rightHandPoint.position
                        : transform.TransformPoint(rightHandOffset);

                case AbilityTargeting.DirectionalCone:
                    // Forward arc — slightly in front of chest
                    return chestPoint != null
                        ? chestPoint.position + transform.forward * 0.3f
                        : transform.TransformPoint(forwardOffset);

                case AbilityTargeting.Self:
                    // Buff — above head
                    return aboveHeadPoint != null
                        ? aboveHeadPoint.position
                        : transform.position + aboveHeadOffset;

                case AbilityTargeting.Trail:
                    // Trail starts at feet
                    return feetPoint != null
                        ? feetPoint.position
                        : transform.position + feetOffset;

                default:
                    return chestPoint != null
                        ? chestPoint.position
                        : transform.position + chestOffset;
            }
        }

        /// <summary>Get position for a hit VFX on a target — above ground level.</summary>
        public static Vector3 GetHitPosition(Transform target)
        {
            if (target == null) return Vector3.zero;
            // Aim for chest height on the target
            var col = target.GetComponent<Collider>();
            if (col != null)
                return col.bounds.center;
            return target.position + Vector3.up * 1.0f;
        }

        /// <summary>Get position for AoE ground ring VFX — snapped to ground.</summary>
        public Vector3 GetGroundPosition(float radius = 0f)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f))
                return hit.point + Vector3.up * 0.05f;
            return transform.position;
        }
    }
}
