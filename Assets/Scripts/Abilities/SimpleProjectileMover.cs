// ============================================================
//  OPHIO — SimpleProjectileMover (Position-Based, No Collider)
//  Moves toward a target position. When close enough,
//  applies damage and spawns hit VFX. No collider needed.
// ============================================================

using UnityEngine;
using Invector;

namespace OPHIO.Core
{
    public class SimpleProjectileMover : MonoBehaviour
    {
        [Header("Settings")]
        public float speed          = 20f;
        public float hitThreshold   = 0.6f;   // distance to target at which hit triggers
        public float maxLifetime    = 4f;

        private Vector3     _targetPosition;
        private bool        _hasTarget;
        private string      _poolKey;
        private OphioDamage _damage;
        private Transform   _owner;
        private LayerMask   _enemyLayer;
        private float       _lifetime;
        private bool        _hasHit;

        // Hit VFX pool key (set externally by AbilityExecutor)
        public string hitVFXPoolKey;
        public GameObject hitVFXPrefab;

        // --------------------------------------------------
        //  Init — called by AbilityExecutor
        // --------------------------------------------------
        public void Init(Vector3 direction, float maxRange, string poolKey,
                         OphioDamage damage, LayerMask enemyLayer, Transform owner)
        {
            _poolKey    = poolKey;
            _damage     = damage;
            _enemyLayer = enemyLayer;
            _owner      = owner;
            _lifetime   = 0f;
            _hasHit     = false;
            _hasTarget  = false;

            // Try to find nearest enemy in the aim direction
            Transform nearestEnemy = FindNearestEnemyInDirection(direction, maxRange);

            if (nearestEnemy != null)
            {
                // Lock on to enemy position
                _targetPosition = nearestEnemy.position + Vector3.up * 1.0f;
                _hasTarget      = true;
            }
            else
            {
                // No enemy found — fly in aim direction and auto-expire
                _targetPosition = transform.position + direction.normalized * maxRange;
                _hasTarget      = false;
            }
        }

        private void OnEnable()
        {
            _lifetime = 0f;
            _hasHit   = false;
        }

        // --------------------------------------------------
        //  Update — move toward target
        // --------------------------------------------------
        private void Update()
        {
            if (_hasHit) return;

            _lifetime += Time.deltaTime;
            if (_lifetime >= maxLifetime)
            {
                ReturnToPool();
                return;
            }

            // Move toward target position
            Vector3 direction = (_targetPosition - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            if (direction != Vector3.zero)
                transform.forward = direction;

            // Check distance to target
            float dist = Vector3.Distance(transform.position, _targetPosition);
            if (dist <= hitThreshold)
                ProcessHit();
        }

        // --------------------------------------------------
        //  Hit — apply damage + spawn VFX at target pos
        // --------------------------------------------------
        private void ProcessHit()
        {
            if (_hasHit) return;
            _hasHit = true;

            // Spawn hit VFX at target position
            SpawnHitVFX(_targetPosition);

            // Find enemy at target position and apply damage
            Collider[] cols = Physics.OverlapSphere(_targetPosition, 1.5f, _enemyLayer);
            bool       hit  = false;

            foreach (var col in cols)
            {
                var health = col.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;

                var vdmg         = _damage.ToVDamage(_owner);
                vdmg.hitReaction = true;
                health.TakeDamage(vdmg);

                if (StatusEffectManager.Instance != null && _damage.RollStatus())
                {
                    var status = _damage.ResolvedStatus();
                    if (status != StatusEffectType.None)
                        StatusEffectManager.Instance.Apply(
                            health, status, 3f, _damage.statusStacks, _owner);
                }

                hit = true;
                break; // single target — stop after first hit
            }

            if (!hit)
            {
                // No enemy found at exact position — try wider search
                cols = Physics.OverlapSphere(_targetPosition, 3f, _enemyLayer);
                foreach (var col in cols)
                {
                    var health = col.GetComponent<vHealthController>();
                    if (health == null || health.isDead) continue;
                    var vdmg         = _damage.ToVDamage(_owner);
                    vdmg.hitReaction = true;
                    health.TakeDamage(vdmg);
                    break;
                }
            }

            ReturnToPool();
        }

        // --------------------------------------------------
        //  Find nearest enemy in aim direction (cone search)
        // --------------------------------------------------
        private Transform FindNearestEnemyInDirection(Vector3 direction, float maxRange)
        {
            Collider[] cols       = Physics.OverlapSphere(transform.position, maxRange, _enemyLayer);
            Transform  best       = null;
            float      bestScore  = float.MinValue;

            foreach (var col in cols)
            {
                if (col.GetComponent<vHealthController>() == null) continue;
                if (col.GetComponent<vHealthController>().isDead) continue;

                Vector3 toEnemy = (col.transform.position - transform.position);
                float   dist    = toEnemy.magnitude;
                float   dot     = Vector3.Dot(direction.normalized, toEnemy.normalized);

                // Only consider enemies roughly in the aim direction (dot > 0.5 = within ~60 deg cone)
                if (dot < 0.4f) continue;

                // Score: closer + more aligned = better
                float score = dot - (dist / maxRange) * 0.3f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best      = col.transform;
                }
            }

            return best;
        }

        // --------------------------------------------------
        //  Hit VFX
        // --------------------------------------------------
        private void SpawnHitVFX(Vector3 position)
        {
            if (hitVFXPrefab == null) return;
            if (string.IsNullOrEmpty(hitVFXPoolKey)) hitVFXPoolKey = _poolKey + "_Hit";

            ObjectPoolManager.Instance.RegisterPool(hitVFXPoolKey, hitVFXPrefab, 5);
            var vfx = ObjectPoolManager.Instance.Spawn(hitVFXPoolKey, position, Quaternion.identity);
            if (vfx != null)
                ObjectPoolManager.Instance.Despawn(hitVFXPoolKey, vfx, 2f);
        }

        private void ReturnToPool()
        {
            _hasHit = true;
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Despawn(_poolKey, gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
