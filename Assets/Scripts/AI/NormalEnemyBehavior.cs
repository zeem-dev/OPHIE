// ============================================================
//  OPHIO — NormalEnemyBehavior
//  Normal Enemy Type
//  Swarm AI — low HP, proximity rush, attacks in packs.
//  Overrides base AI with pack swarming logic:
//  - Spreads out when near other Normal enemies
//  - Attacks in quick bursts then repositions
//  - Flanks the player when possible
//  Attach alongside EnemyAI on the Normal enemy prefab.
// ============================================================

using UnityEngine;
using UnityEngine.AI;

namespace OPHIO.AI
{
    [RequireComponent(typeof(EnemyAI))]
    public class NormalEnemyBehavior : MonoBehaviour
    {
        [Header("Swarm Settings")]
        [Tooltip("Min distance to maintain from other Normal enemies")]
        public float separationRadius   = 1.8f;
        [Tooltip("Force to push away from nearby allies")]
        public float separationForce    = 2f;
        [Tooltip("Max allies detected for swarming logic")]
        public int   maxSwarmCheck      = 6;

        [Header("Flank Settings")]
        [Tooltip("Angle offset when approaching player to flank")]
        public float flankAngle         = 45f;
        [Tooltip("Chance per tick to attempt a flank (0-1)")]
        [Range(0f, 1f)]
        public float flankChance        = 0.3f;

        [Header("Reposition")]
        [Tooltip("After attacking, back off by this distance")]
        public float repositionDist     = 2.5f;
        [Tooltip("Chance per tick to reposition instead of standing (0-1)")]
        [Range(0f, 1f)]
        public float repositionChance   = 0.4f;

        private EnemyAI       _ai;
        private NavMeshAgent  _agent;
        private float         _flankDir = 1f;  // +1 = right, -1 = left

        // Cache: avoid GC every tick
        private Collider[]    _nearbyBuffer;
        private static int    s_enemyLayer = -1;

        private void Awake()
        {
            _ai    = GetComponent<EnemyAI>();
            _agent = GetComponent<NavMeshAgent>();
            _nearbyBuffer = new Collider[maxSwarmCheck];

            // Random flank side
            _flankDir = Random.value > 0.5f ? 1f : -1f;
        }

        private void Start()
        {
            // Cache enemy layer once
            if (s_enemyLayer == -1)
                s_enemyLayer = gameObject.layer;
        }

        // --------------------------------------------------
        //  Called by EnemyAI tick (via SendMessage or direct call)
        //  Override chase behavior with swarm logic
        // --------------------------------------------------
        private void Update()
        {
            if (_ai.CurrentStateType == AIStateType.Dead) return;

            // Only apply swarm logic during Chase
            if (_ai.CurrentStateType == AIStateType.Chase)
            {
                ApplySeparation();
                ApplyFlank();
            }
        }

        // --------------------------------------------------
        //  Separation — push away from nearby allies
        //  Prevents enemies from stacking on the same spot
        // --------------------------------------------------
        private void ApplySeparation()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, separationRadius,
                _nearbyBuffer, 1 << gameObject.layer);

            Vector3 separationVec = Vector3.zero;
            int neighbors = 0;

            for (int i = 0; i < count; i++)
            {
                if (_nearbyBuffer[i].gameObject == gameObject) continue;
                if (_nearbyBuffer[i].GetComponent<NormalEnemyBehavior>() == null) continue;

                Vector3 away = transform.position - _nearbyBuffer[i].transform.position;
                float dist   = away.magnitude;
                if (dist < 0.01f) continue;

                separationVec += away.normalized / dist;
                neighbors++;
            }

            if (neighbors > 0 && _agent.hasPath)
            {
                separationVec /= neighbors;
                Vector3 adjusted = _agent.destination + separationVec * separationForce;
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(adjusted, out navHit, 3f, NavMesh.AllAreas))
                    _agent.SetDestination(navHit.position);
            }
        }

        // --------------------------------------------------
        //  Flanking — approach player from an angle
        // --------------------------------------------------
        private void ApplyFlank()
        {
            if (_ai.PlayerTransform == null) return;
            if (Random.value > flankChance * Time.deltaTime * 5f) return;

            Vector3 toPlayer = (_ai.PlayerTransform.position - transform.position).normalized;
            Vector3 flank    = Quaternion.Euler(0f, flankAngle * _flankDir, 0f) * toPlayer;
            Vector3 target   = _ai.PlayerTransform.position - flank * _ai.AttackRange * 0.8f;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(target, out navHit, 3f, NavMesh.AllAreas))
                _agent.SetDestination(navHit.position);
        }

        // --------------------------------------------------
        //  Post-attack reposition — back away after hitting
        //  Called by animation event or manually from attack state
        // --------------------------------------------------
        public void OnAttackComplete()
        {
            if (Random.value > repositionChance) return;

            Vector3 backDir = (transform.position -
                               (_ai.PlayerTransform != null
                                   ? _ai.PlayerTransform.position
                                   : transform.position + transform.forward))
                              .normalized;

            Vector3 repositionTarget = transform.position + backDir * repositionDist;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(repositionTarget, out navHit, 3f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(navHit.position);
            }
        }

        // --------------------------------------------------
        //  Debug gizmos
        // --------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.8f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);
        }
    }
}
