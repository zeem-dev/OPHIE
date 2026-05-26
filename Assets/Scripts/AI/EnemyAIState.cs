// ============================================================
//  OPHIO — EnemyAIState
//  Enemy AI State Machine
//  Enum + abstract base class for all AI states.
//  Each state handles Enter, Execute (0.2s tick), Exit.
//  EnemyAI controller swaps states at runtime.
// ============================================================

using UnityEngine;

namespace OPHIO.AI
{
    // --------------------------------------------------
    //  State identifiers
    // --------------------------------------------------
    public enum AIStateType
    {
        Idle,
        Chase,
        Attack,
        Stunned,
        Dead
    }

    // --------------------------------------------------
    //  Abstract base — all states inherit this
    // --------------------------------------------------
    public abstract class EnemyAIState
    {
        protected EnemyAI owner;

        public EnemyAIState(EnemyAI owner)
        {
            this.owner = owner;
        }

        /// <summary>Called once when entering this state.</summary>
        public abstract void Enter();

        /// <summary>Called every AI tick (0.2s default). NOT every frame.</summary>
        public abstract void Execute();

        /// <summary>Called once when leaving this state.</summary>
        public abstract void Exit();
    }

    // ==========================================================
    //  IDLE STATE
    //  Enemy stands/patrols near spawn. Scans for player.
    //  Transitions → Chase when player detected.
    // ==========================================================
    public class IdleState : EnemyAIState
    {
        private float _scanTimer;

        public IdleState(EnemyAI owner) : base(owner) { }

        public override void Enter()
        {
            _scanTimer = 0f;
            owner.Agent.isStopped = true;

            // Idle animation
            if (owner.Animator != null)
                owner.Animator.SetBool("isMoving", false);
        }

        public override void Execute()
        {
            // Scan for player every tick
            if (owner.PlayerTransform == null) return;

            float dist = Vector3.Distance(owner.transform.position,
                                           owner.PlayerTransform.position);

            // Proximity detection
            if (dist <= owner.DetectionRange)
            {
                // Line of sight check
                if (owner.HasLineOfSight())
                {
                    owner.ChangeState(AIStateType.Chase);
                    return;
                }
            }

            // Sound detection — closer range, no LOS required
            if (dist <= owner.SoundDetectionRange)
            {
                owner.ChangeState(AIStateType.Chase);
                return;
            }
        }

        public override void Exit()
        {
            // Nothing to clean up
        }
    }

    // ==========================================================
    //  CHASE STATE
    //  Pursue the player using NavMeshAgent.
    //  Transitions → Attack when in attack range.
    //  Transitions → Idle if player escapes detection.
    // ==========================================================
    public class ChaseState : EnemyAIState
    {
        private float _losePlayerTimer;

        public ChaseState(EnemyAI owner) : base(owner) { }

        public override void Enter()
        {
            _losePlayerTimer = 0f;
            owner.Agent.isStopped = false;

            if (owner.Animator != null)
                owner.Animator.SetBool("isMoving", true);
        }

        public override void Execute()
        {
            if (owner.PlayerTransform == null)
            {
                owner.ChangeState(AIStateType.Idle);
                return;
            }

            float dist = Vector3.Distance(owner.transform.position,
                                           owner.PlayerTransform.position);

            // Lost player — too far away
            if (dist > owner.DetectionRange * 1.5f)
            {
                _losePlayerTimer += owner.TickInterval;
                if (_losePlayerTimer >= owner.LosePlayerTimeout)
                {
                    owner.ChangeState(AIStateType.Idle);
                    return;
                }
            }
            else
            {
                _losePlayerTimer = 0f;
            }

            // In attack range → Attack
            if (dist <= owner.AttackRange)
            {
                owner.ChangeState(AIStateType.Attack);
                return;
            }

            // Chase — set NavMesh destination
            owner.Agent.SetDestination(owner.PlayerTransform.position);
            owner.Agent.speed = owner.ChaseSpeed;
        }

        public override void Exit()
        {
            owner.Agent.isStopped = true;
        }
    }

    // ==========================================================
    //  ATTACK STATE
    //  Face player, perform attack animation, deal damage.
    //  Transitions → Chase if player escapes attack range.
    //  Transitions → Stunned if status applied.
    // ==========================================================
    public class AttackState : EnemyAIState
    {
        private float _attackCooldown;
        private bool  _isAttacking;

        public AttackState(EnemyAI owner) : base(owner) { }

        public override void Enter()
        {
            _attackCooldown = 0f;
            _isAttacking    = false;
            owner.Agent.isStopped = true;

            if (owner.Animator != null)
                owner.Animator.SetBool("isMoving", false);
        }

        public override void Execute()
        {
            if (owner.PlayerTransform == null)
            {
                owner.ChangeState(AIStateType.Idle);
                return;
            }

            float dist = Vector3.Distance(owner.transform.position,
                                           owner.PlayerTransform.position);

            // Player escaped attack range → Chase
            if (dist > owner.AttackRange * 1.2f)
            {
                owner.ChangeState(AIStateType.Chase);
                return;
            }

            // Face the player
            Vector3 dir = (owner.PlayerTransform.position - owner.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                owner.transform.rotation = Quaternion.Slerp(
                    owner.transform.rotation,
                    Quaternion.LookRotation(dir),
                    10f * owner.TickInterval);

            // Attack cooldown
            _attackCooldown -= owner.TickInterval;
            if (_attackCooldown <= 0f && !_isAttacking)
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            _isAttacking = true;

            if (owner.Animator != null)
                owner.Animator.SetTrigger("Attack");

            // Damage is applied by OnAttackHit() called from animation event
            // or after a fixed delay
            owner.StartCoroutine(AttackRoutine());
        }

        private System.Collections.IEnumerator AttackRoutine()
        {
            yield return new WaitForSeconds(owner.AttackWindup);

            // Apply damage if still in range
            if (owner.PlayerTransform != null)
            {
                float dist = Vector3.Distance(owner.transform.position,
                                               owner.PlayerTransform.position);
                if (dist <= owner.AttackRange * 1.3f)
                {
                    owner.DealDamageToPlayer();
                }
            }

            yield return new WaitForSeconds(owner.AttackRecovery);

            _attackCooldown = owner.AttackCooldown;
            _isAttacking    = false;
        }

        public override void Exit()
        {
            _isAttacking = false;
        }
    }

    // ==========================================================
    //  STUNNED STATE
    //  Enemy is staggered / shocked. Cannot move or attack.
    //  Transitions → Chase or Idle when stun wears off.
    // ==========================================================
    public class StunnedState : EnemyAIState
    {
        private float _stunTimer;

        public StunnedState(EnemyAI owner) : base(owner) { }

        public override void Enter()
        {
            _stunTimer = owner.CurrentStunDuration;
            owner.Agent.isStopped = true;

            if (owner.Animator != null)
            {
                owner.Animator.SetBool("isStunned", true);
                owner.Animator.SetBool("isMoving", false);
            }
        }

        public override void Execute()
        {
            _stunTimer -= owner.TickInterval;
            if (_stunTimer <= 0f)
            {
                // Return to chase if player is still nearby
                if (owner.PlayerTransform != null)
                {
                    float dist = Vector3.Distance(owner.transform.position,
                                                   owner.PlayerTransform.position);
                    if (dist <= owner.DetectionRange)
                    {
                        owner.ChangeState(AIStateType.Chase);
                        return;
                    }
                }
                owner.ChangeState(AIStateType.Idle);
            }
        }

        public override void Exit()
        {
            if (owner.Animator != null)
                owner.Animator.SetBool("isStunned", false);
        }
    }

    // ==========================================================
    //  DEAD STATE
    //  Final state. Disable AI, play death anim, cleanup.
    // ==========================================================
    public class DeadState : EnemyAIState
    {
        public DeadState(EnemyAI owner) : base(owner) { }

        public override void Enter()
        {
            owner.Agent.isStopped = true;
            owner.Agent.enabled   = false;

            if (owner.Animator != null)
            {
                owner.Animator.SetTrigger("Die");
                owner.Animator.SetBool("isMoving", false);
                owner.Animator.SetBool("isStunned", false);
            }

            // Notify game systems
            owner.OnEnemyDeath();
        }

        public override void Execute()
        {
            // No logic — terminal state
        }

        public override void Exit()
        {
            // Dead state is never exited (unless respawn/pool)
        }
    }
}
