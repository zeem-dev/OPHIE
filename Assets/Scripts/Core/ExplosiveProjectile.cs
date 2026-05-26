// ============================================================
// OPHIO - ExplosiveProjectile
// Ability Components
// Handles physics-based projectiles that explode on impact
// or after a set duration. Used for MAC's Germane Toss.
// ============================================================

using UnityEngine;

namespace OPHIO.Core
{
    public class ExplosiveProjectile : MonoBehaviour
    {
        private OphioDamage _damage;
        private LayerMask _enemyLayer;
        private Transform _sender;
        private float _explosionRadius;
        private bool _hasExploded;

        public void Init(OphioDamage damage, LayerMask enemyLayer, Transform sender, float explosionRadius = 3f)
        {
            _damage = damage;
            _enemyLayer = enemyLayer;
            _sender = sender;
            _explosionRadius = explosionRadius;
            _hasExploded = false;

            // Failsafe destroy after 5 seconds if no collision
            Invoke(nameof(Explode), 5f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasExploded) return;
            
            // Explode on any impact
            Explode();
        }

        private void Explode()
        {
            if (_hasExploded) return;
            _hasExploded = true;
            CancelInvoke(nameof(Explode));

            // Deal AoE damage
            var hits = Physics.OverlapSphere(transform.position, _explosionRadius, _enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<Invector.vHealthController>();
                if (health != null && !health.isDead)
                {
                    var vDmg = _damage.ToVDamage(_sender);
                    // Add slight ragdoll/knockdown chance for explosions
                    vDmg.activeRagdoll = Random.value > 0.5f;
                    health.TakeDamage(vDmg);
                }
            }

            // VFX
            string poolKey = "ExplosionVFX";
            // Check if pool exists, otherwise we just destroy the projectile
            if (ObjectPoolManager.Instance != null)
            {
                var vfx = ObjectPoolManager.Instance.Spawn(poolKey, transform.position, Quaternion.identity);
                if (vfx != null) ObjectPoolManager.Instance.Despawn(poolKey, vfx, 2f);
            }

            // Despawn this grenade
            // If the pool key wasn't explicitly passed, we fallback to Destroy
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f); // Fallback if not pooled properly
        }
    }
}
