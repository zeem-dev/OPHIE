// ============================================================
//  OPHIO — EnemyCameraAvoidance
//  Fix: Enemies attack from player's front-facing direction
//  so they never enter the camera's view from behind.
//  Attach to every Enemy prefab alongside EnemyAI.
// ============================================================

using UnityEngine;
using UnityEngine.AI;

namespace OPHIO.AI
{
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyCameraAvoidance : MonoBehaviour
    {
        [Header("Camera Avoidance")]
        [Tooltip("Enemies stay outside this angle from camera's backward direction")]
        [Range(30f, 120f)]
        public float cameraBlockedAngle = 70f;

        [Tooltip("How far in front of player enemies prefer to position")]
        public float preferredFrontDist = 3.5f;

        [Tooltip("Offset radius around player's front arc")]
        public float arcSpreadRadius    = 2.5f;

        [Tooltip("How often to recalculate position (seconds)")]
        public float recalcInterval     = 0.4f;

        private EnemyAI      _ai;
        private NavMeshAgent _agent;
        private Camera       _cam;
        private float        _recalcTimer;

        // Each enemy gets a unique angle offset so they
        // spread around the player's front arc
        private float _angleOffset;

        private void Awake()
        {
            _ai           = GetComponent<EnemyAI>();
            _agent        = GetComponent<NavMeshAgent>();
            _angleOffset  = Random.Range(-arcSpreadRadius * 20f, arcSpreadRadius * 20f);
        }

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (_ai == null) return;
            if (_ai.CurrentStateType != AIStateType.Chase &&
                _ai.CurrentStateType != AIStateType.Attack) return;
            if (_ai.PlayerTransform == null) return;

            _recalcTimer += Time.deltaTime;
            if (_recalcTimer < recalcInterval) return;
            _recalcTimer = 0f;

            CorrectPosition();
        }

        // --------------------------------------------------
        //  Core logic — redirect enemy away from camera zone
        // --------------------------------------------------
        private void CorrectPosition()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 playerPos   = _ai.PlayerTransform.position;
            Vector3 toEnemy     = transform.position - playerPos;
            toEnemy.y           = 0f;

            // Camera backward direction (where camera is relative to player)
            Vector3 camBack     = playerPos - _cam.transform.position;
            camBack.y           = 0f;
            camBack             = camBack.normalized;

            // Check if this enemy is in the blocked (behind-player/camera) zone
            float angleToBlock  = Vector3.Angle(toEnemy.normalized, camBack);

            if (angleToBlock < cameraBlockedAngle)
            {
                // Enemy is too close to camera's back zone — redirect to front arc
                RedirectToFrontArc(playerPos, camBack);
            }
        }

        // --------------------------------------------------
        //  Move enemy to player's front-facing arc
        // --------------------------------------------------
        private void RedirectToFrontArc(Vector3 playerPos, Vector3 camBack)
        {
            // Player's forward = opposite of camera back direction
            Vector3 playerFront = -camBack;

            // Apply unique angle offset so enemies spread out
            Vector3 spreadDir   = Quaternion.Euler(0f, _angleOffset, 0f) * playerFront;

            // Target position: in front of player at attack range
            Vector3 target      = playerPos + spreadDir.normalized * preferredFrontDist;
            target.y            = transform.position.y;

            // Sample NavMesh for valid position
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 4f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(hit.position);
            }
        }

        // --------------------------------------------------
        //  Debug
        // --------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            if (_ai?.PlayerTransform == null) return;
            if (Camera.main == null) return;

            Vector3 playerPos = _ai.PlayerTransform.position;
            Vector3 camBack   = (playerPos - Camera.main.transform.position).normalized;
            camBack.y         = 0f;

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            // Draw blocked zone
            for (int a = -(int)cameraBlockedAngle; a <= (int)cameraBlockedAngle; a += 10)
            {
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * camBack;
                Gizmos.DrawRay(playerPos, dir * 5f);
            }
        }
    }
}
