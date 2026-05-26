// ============================================================
//  OPHIO — OphioProjectile
//  Day 3 | Ability Core
//  Pooled projectile — AbilityExecutor.LaunchProjectile()
//  se spawn hota hai. Invector ke bullet system jaisi
//  behavior — forward travel, hit detection, impact VFX,
//  damage + status apply, pool wapas return.
//  ObjectPoolManager mein "ElectricProjectile" key se register.
// ============================================================

using UnityEngine;
using Invector;
using OPHIO.Core;

namespace OPHIO.Abilities
{
    [RequireComponent(typeof(Rigidbody))]
    public class OphioProjectile : MonoBehaviour
    {
        // --------------------------------------------------
        //  Runtime data — set by Init() each spawn
        // --------------------------------------------------
        private AbilityData  _ability;
        private Transform    _owner;
        private float        _speed;
        private string       _impactPoolKey;
        private LayerMask    _enemyLayers;
        private float        _lifetime;
        private float        _timer;

        private Rigidbody    _rb;

        // --------------------------------------------------
        //  Init — called by AbilityExecutor after Spawn()
        // --------------------------------------------------
        public void Init(AbilityData ability, Transform owner,
                         float speed, string impactPoolKey, LayerMask enemyLayers)
        {
            _ability       = ability;
            _owner         = owner;
            _speed         = speed;
            _impactPoolKey = impactPoolKey;
            _enemyLayers   = enemyLayers;
            _lifetime      = ability.range / Mathf.Max(speed, 1f);  // auto lifetime from range
            _timer         = 0f;

            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = false;
            _rb.useGravity  = false;
            _rb.linearVelocity       = transform.forward * _speed;
        }

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void OnEnable()
        {
            _timer = 0f;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _lifetime) ReturnToPool();
        }

        // --------------------------------------------------
        //  Collision
        // --------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            // Ignore owner
            if (_owner != null && other.transform.IsChildOf(_owner)) return;

            // Impact VFX
            SpawnImpact(transform.position, -transform.forward);

            // Check if enemy
            if (((1 << other.gameObject.layer) & _enemyLayers) != 0)
            {
                var health = other.GetComponentInParent<vHealthController>();
                if (health != null && !health.isDead)
                {
                    OphioDamage od = _ability.BuildDamage();
                    health.TakeDamage(od.ToVDamage(_owner));

                    if (od.RollStatus() && StatusEffectManager.Instance != null)
                    {
                        StatusEffectManager.Instance.Apply(
                            health,
                            od.ResolvedStatus(),
                            duration: 3f,
                            stacks:   od.statusStacks,
                            appliedBy: _owner
                        );
                    }
                }
            }

            ReturnToPool();
        }

        // --------------------------------------------------
        //  Helpers
        // --------------------------------------------------
        private void SpawnImpact(Vector3 pos, Vector3 normal)
        {
            var pool = ObjectPoolManager.Instance;
            if (pool == null) return;
            var imp = pool.Spawn(_impactPoolKey, pos,
                                 Quaternion.LookRotation(normal == Vector3.zero
                                                         ? Vector3.up : normal));
            if (imp != null) pool.Despawn(_impactPoolKey, imp, 1.5f);
        }

        private void ReturnToPool()
        {
            if (_rb != null) _rb.linearVelocity = Vector3.zero;
            var pool = ObjectPoolManager.Instance;
            if (pool != null)
                pool.Despawn(_ability != null
                             ? _ability.abilityName.Replace(" ", "")
                             : "OphioProjectile", gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
