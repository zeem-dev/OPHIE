// ============================================================
//  OPHIO — DestructibleObject
//  Environment | Arena Core
//  Attach to any arena prop that should break when damaged.
//  Implements Invector's IDamageReceiver so the existing
//  damage pipeline works without changes.
//  On death: spawns rubble prefab, plays VFX + SFX,
//  optionally leaks an InfectionHazardZone (fungal props).
// ============================================================

using UnityEngine;
using Invector;

namespace OPHIO.Arena
{
    [DisallowMultipleComponent]
    public class DestructibleObject : MonoBehaviour, vIDamageReceiver
    {
        [Header("Health")]
        public float maxHealth = 80f;

        [Header("Destroyed State")]
        public GameObject destroyedPrefab;
        public float      debrisLifetime = 6f;

        [Header("On Destroy FX")]
        public ParticleSystem destructionFX;
        public AudioClip      destructionSFX;

        [Header("Infection Leak")]
        [Tooltip("Fungal props can leak an infection zone when destroyed")]
        public bool       leaksInfection     = false;
        public GameObject infectionZonePrefab;

        // ── Runtime ───────────────────────────────────────────────────────────

        float    _health;
        bool     _dead;
        Renderer _renderer;
        Collider _col;
        AudioSource _audio;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        void Awake()
        {
            _health   = maxHealth;
            _renderer = GetComponentInChildren<Renderer>();
            _col      = GetComponent<Collider>();
            _audio    = GetComponent<AudioSource>();
        }

        // ── vIDamageReceiver — Invector interface ────────────────────────────

        public void TakeDamage(vDamage damage)
        {
            if (_dead) return;

            _health -= damage.damageValue;
            if (_health <= 0f) Break();
        }

        // ── Public ────────────────────────────────────────────────────────────

        public bool IsAlive => !_dead;

        OnReceiveDamage vIDamageReceiver.onStartReceiveDamage => throw new System.NotImplementedException();

        OnReceiveDamage vIDamageReceiver.onReceiveDamage => throw new System.NotImplementedException();

        // ── Break ─────────────────────────────────────────────────────────────

        void Break()
        {
            _dead = true;

            // Rubble
            if (destroyedPrefab != null)
            {
                GameObject debris = Instantiate(destroyedPrefab, transform.position, transform.rotation);
                Destroy(debris, debrisLifetime);
            }

            // VFX — detach so it survives object destruction
            if (destructionFX != null)
            {
                destructionFX.transform.SetParent(null);
                destructionFX.Play();
                Destroy(destructionFX.gameObject, destructionFX.main.duration + 1f);
            }

            // SFX
            if (_audio != null && destructionSFX != null)
                _audio.PlayOneShot(destructionSFX);

            // Infection leak
            if (leaksInfection && infectionZonePrefab != null)
                Instantiate(infectionZonePrefab, transform.position, Quaternion.identity);

            // Hide original
            if (_renderer != null) _renderer.enabled = false;
            if (_col != null)      _col.enabled      = false;

            float delay = (_audio != null && destructionSFX != null) ? destructionSFX.length : 0f;
            Destroy(gameObject, delay + 0.1f);
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.5f);
        }
    }
}
