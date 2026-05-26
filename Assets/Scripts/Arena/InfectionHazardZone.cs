// ============================================================
//  OPHIO — InfectionHazardZone
//  Environment | Arena Core
//  Trigger zone that applies Infection damage over time to any
//  vHealthController that enters. Placed by ArenaGenerator.
//  Prefab requires: SphereCollider (isTrigger = true on prefab,
//  or set at runtime via RefreshCollider).
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Invector;
using OPHIO.Core;

namespace OPHIO.Arena
{
    [RequireComponent(typeof(SphereCollider))]
    public class InfectionHazardZone : MonoBehaviour
    {
        [Header("Zone")]
        public float zoneRadius      = 3f;
        public float damagePerSecond = 5f;
        public float pulseInterval   = 1f;

        [Header("Visual")]
        public ParticleSystem sporeParticles;

        SphereCollider _col;
        Coroutine      _pulse;

        readonly List<vHealthController> _targets = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        void Awake()
        {
            _col           = GetComponent<SphereCollider>();
            _col.isTrigger = true;
            _col.radius    = zoneRadius;
        }

        void OnEnable()
        {
            _pulse = StartCoroutine(DamagePulse());
            if (sporeParticles != null) sporeParticles.Play();
        }

        void OnDisable()
        {
            if (_pulse != null) StopCoroutine(_pulse);
            _targets.Clear();
            if (sporeParticles != null) sporeParticles.Stop();
        }

        // ── Public API ────────────────────────────────────────────────────────

        // Called by ArenaGenerator after Instantiate to resize the zone.
        public void RefreshCollider()
        {
            if (_col == null) _col = GetComponent<SphereCollider>();
            _col.radius = zoneRadius;
        }

        // ── Trigger ───────────────────────────────────────────────────────────

        void OnTriggerEnter(Collider other)
        {
            vHealthController hp = other.GetComponentInParent<vHealthController>();
            if (hp != null && !_targets.Contains(hp))
                _targets.Add(hp);
        }

        void OnTriggerExit(Collider other)
        {
            vHealthController hp = other.GetComponentInParent<vHealthController>();
            if (hp != null) _targets.Remove(hp);
        }

        // ── Damage pulse ──────────────────────────────────────────────────────

        IEnumerator DamagePulse()
        {
            WaitForSeconds wait = new WaitForSeconds(pulseInterval);
            float tickDamage    = damagePerSecond * pulseInterval;

            while (true)
            {
                yield return wait;

                for (int i = _targets.Count - 1; i >= 0; i--)
                {
                    if (_targets[i] == null || _targets[i].isDead)
                    {
                        _targets.RemoveAt(i);
                        continue;
                    }

                    vDamage dmg = new vDamage((int)tickDamage)
                    {
                        damageType  = "Infection",
                        hitReaction = false,
                        sender      = transform
                    };
                    _targets[i].TakeDamage(dmg);

                    // Apply Infection status effect via StatusEffectManager singleton
                    StatusEffectManager.Instance?.Apply(
                        _targets[i], StatusEffectType.Infection, 3f, 1, transform);
                }
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.1f, 0.9f, 0.2f, 0.2f);
            Gizmos.DrawSphere(transform.position, zoneRadius);
            Gizmos.color = new Color(0.1f, 0.9f, 0.2f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, zoneRadius);
        }
    }
}
