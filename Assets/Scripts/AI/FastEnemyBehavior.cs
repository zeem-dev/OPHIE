// ============================================================
//  OPHIO — FastEnemyBehavior
//  Enemy AI — Fast Type
//  High speed, erratic pathing, low HP.
//  Tracks player but takes unpredictable paths.
//  Quick strike then immediately repositions.
//  Attach alongside EnemyAI on Fast enemy prefab.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace OPHIO.AI
{
    [RequireComponent(typeof(EnemyAI))]
    public class FastEnemyBehavior : MonoBehaviour
    {
        [Header("Erratic Movement")]
        [Tooltip("How often the enemy changes direction randomly")]
        public float directionChangeInterval = 0.8f;
        [Tooltip("Random lateral offset range when chasing")]
        public float erraticOffset           = 4f;
        [Tooltip("Speed multiplier when performing a hit-and-run")]
        public float hitAndRunSpeedMult      = 1.6f;

        [Header("Hit and Run")]
        [Tooltip("After attacking, sprint away this distance")]
        public float retreatDistance          = 5f;
        [Tooltip("Seconds to stay retreated before re-engaging")]
        public float retreatPause            = 0.8f;
        [Tooltip("Sprint speed during retreat")]
        public float retreatSpeed            = 8f;

        [Header("Strafe")]
        [Tooltip("When in attack range, strafe around the player")]
        public float strafeSpeed             = 5f;
        [Tooltip("Strafe radius around the player")]
        public float strafeRadius            = 3f;

        private EnemyAI      _ai;
        private NavMeshAgent _agent;
        private float        _nextDirChange;
        private float        _strafeAngle;
        private bool         _isRetreating;

        private void Awake()
        {
            _ai    = GetComponent<EnemyAI>();
            _agent = GetComponent<NavMeshAgent>();
            _strafeAngle = Random.Range(0f, 360f);
        }

        private void Update()
        {
            if (_ai.CurrentStateType == AIStateType.Dead) return;
            if (_isRetreating) return;

            switch (_ai.CurrentStateType)
            {
                case AIStateType.Chase:
                    ErraticChase();
                    break;
                case AIStateType.Attack:
                    StrafeAroundPlayer();
                    break;
            }
        }

        // --------------------------------------------------
        //  Erratic chase — zigzag toward player
        // --------------------------------------------------
        private void ErraticChase()
        {
            if (_ai.PlayerTransform == null) return;

            _nextDirChange -= Time.deltaTime;
            if (_nextDirChange > 0f) return;

            _nextDirChange = directionChangeInterval;

            // Calculate offset point near the player
            Vector3 toPlayer = _ai.PlayerTransform.position - transform.position;
            Vector3 lateral  = Vector3.Cross(Vector3.up, toPlayer.normalized);
            float   offset   = Random.Range(-erraticOffset, erraticOffset);

            Vector3 target = _ai.PlayerTransform.position + lateral * offset;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(target, out navHit, 5f, NavMesh.AllAreas))
            {
                _agent.SetDestination(navHit.position);
                _agent.speed = _ai.ChaseSpeed * hitAndRunSpeedMult;
            }
        }

        // --------------------------------------------------
        //  Strafe around player when in attack range
        // --------------------------------------------------
        private void StrafeAroundPlayer()
        {
            if (_ai.PlayerTransform == null) return;

            _strafeAngle += strafeSpeed * Time.deltaTime * 20f;

            float rad = _strafeAngle * Mathf.Deg2Rad;
            Vector3 strafePos = _ai.PlayerTransform.position +
                                new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * strafeRadius;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(strafePos, out navHit, 3f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(navHit.position);
                _agent.speed = strafeSpeed;
            }

            // Keep facing the player
            Vector3 dir = (_ai.PlayerTransform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    10f * Time.deltaTime);
        }

        // --------------------------------------------------
        //  Hit-and-run retreat — called after attack lands
        //  Hook this to an animation event or call manually
        // --------------------------------------------------
        public void OnAttackComplete()
        {
            StartCoroutine(HitAndRunRetreat());
        }

        private IEnumerator HitAndRunRetreat()
        {
            _isRetreating = true;

            // Sprint away from player
            Vector3 awayDir = (transform.position -
                               (_ai.PlayerTransform != null
                                   ? _ai.PlayerTransform.position
                                   : transform.position - transform.forward))
                              .normalized;

            Vector3 retreatTarget = transform.position + awayDir * retreatDistance;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(retreatTarget, out navHit, 5f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.speed     = retreatSpeed;
                _agent.SetDestination(navHit.position);
            }

            yield return new WaitForSeconds(retreatPause);

            _isRetreating = false;
            // AI tick will handle re-engagement
        }
    }
}
