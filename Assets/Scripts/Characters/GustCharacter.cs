// ============================================================
// OPHIO - GustCharacter
// Main controller for Gust (Subject #21).
// Handles Spore Anchors, Clones, and Teleportation.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Invector;
using Invector.vCharacterController;

namespace OPHIO.Characters
{
    public class GustCharacter : MonoBehaviour
    {
        private Core.EnergyManager _energy;
        private vHealthController _health;
        private vThirdPersonController _tpc;
        private Core.AbilityExecutor _executor;

        [Header("Clone Network Passive")]
        [Tooltip("Energy restored when a clone or anchor collapses/detonates.")]
        public float energyReturnPerCollapse = 10f;
        [Tooltip("Speed buff duration when a clone collapses.")]
        public float learningBuffDuration = 3f;

        [Header("Spore System")]
        public GameObject sporeAnchorPrefab;
        public GameObject fungalClonePrefab; // Can be a simple pulsing AoE object
        public int maxAnchors = 3;

        private List<GameObject> _activeAnchors = new List<GameObject>();
        private List<GameObject> _activeClones = new List<GameObject>();

        [Header("Spore Armor")]
        public GameObject armorVFX;
        private bool _armorActive;
        private float _originalDamageResistance;

        private float _originalWalkSpeed;
        private float _originalRunSpeed;
        private float _originalSprintSpeed;

        private void Awake()
        {
            _energy = GetComponent<Core.EnergyManager>();
            _health = GetComponent<vHealthController>();
            _tpc = GetComponent<vThirdPersonController>();
            _executor = GetComponent<Core.AbilityExecutor>();
        }

        private void Start()
        {
            if (_tpc != null)
            {
                _originalWalkSpeed = _tpc.freeSpeed.walkSpeed;
                _originalRunSpeed = _tpc.freeSpeed.runningSpeed;
                _originalSprintSpeed = _tpc.freeSpeed.sprintSpeed;

                _health.onStartReceiveDamage.AddListener(OnDamageReceived);
            }
        }

        private void OnDamageReceived(vDamage damage)
        {
            if (_armorActive && damage != null)
            {
                // Spore Armor absorbs 50% damage
                damage.damageValue = (int)(damage.damageValue * 0.5f);
            }
        }

        // --------------------------------------------------
        // Abilities dispatched from AbilityExecutor
        // --------------------------------------------------
        private void OnAbilityActivated(Core.AbilityData ability)
        {
            if (_energy != null) _energy.SetCombatState(true);

            switch (ability.abilityName)
            {
                case "Spore Ball": StartCoroutine(SporeBallRoutine(ability)); break;
                case "Clone Spawn": SpawnClone(); break;
                case "Teleport Override": TeleportToAnchor(); break;
                case "Spore Armor": StartCoroutine(SporeArmorRoutine()); break;
                case "Mass Detonation": StartCoroutine(MassDetonationRoutine(ability)); break;
                case "Distributed Collapse": StartCoroutine(DistributedCollapseRoutine(ability)); break;
            }
        }

        // --------------------------------------------------
        // Spore Ball: Throws projectile that leaves an anchor
        // --------------------------------------------------
        private IEnumerator SporeBallRoutine(Core.AbilityData ability)
        {
            if (ability.vfxPrefab == null) yield break;

            string poolKey = "GustSporeBall";
            Core.ObjectPoolManager.Instance.RegisterPool(poolKey, ability.vfxPrefab, 5);

            Transform muzzle = _executor.muzzlePoint != null ? _executor.muzzlePoint : transform;
            Vector3 origin = muzzle.position;
            Vector3 dir = GetAimDirection();

            var projObj = Core.ObjectPoolManager.Instance.Spawn(poolKey, origin, Quaternion.LookRotation(dir));
            if (projObj != null)
            {
                var rb = projObj.GetComponent<Rigidbody>();
                if (rb == null) rb = projObj.AddComponent<Rigidbody>();
                
                rb.linearVelocity = dir * 20f;
                rb.useGravity = true;

                // Create anchor on impact using a simple collision listener
                var anchorSpawner = projObj.GetComponent<SporeAnchorSpawner>();
                if (anchorSpawner == null) anchorSpawner = projObj.AddComponent<SporeAnchorSpawner>();
                
                anchorSpawner.Init(this, sporeAnchorPrefab);
            }
            yield return null;
        }

        public void RegisterAnchor(Vector3 position)
        {
            if (sporeAnchorPrefab == null) return;

            if (_activeAnchors.Count >= maxAnchors)
            {
                Destroy(_activeAnchors[0]);
                _activeAnchors.RemoveAt(0);
            }

            GameObject anchor = Instantiate(sporeAnchorPrefab, position, Quaternion.identity);
            _activeAnchors.Add(anchor);
        }

        // --------------------------------------------------
        // Clone Spawn: Replaces latest anchor with a clone
        // --------------------------------------------------
        private void SpawnClone()
        {
            if (_activeAnchors.Count == 0 || fungalClonePrefab == null) return;

            // Take the most recent anchor
            GameObject lastAnchor = _activeAnchors[_activeAnchors.Count - 1];
            Vector3 pos = lastAnchor.transform.position;
            
            _activeAnchors.Remove(lastAnchor);
            Destroy(lastAnchor);

            GameObject clone = Instantiate(fungalClonePrefab, pos, Quaternion.identity);
            _activeClones.Add(clone);

            // Clone naturally collapses after 10 seconds
            StartCoroutine(TrackCloneCollapse(clone, 10f));
        }

        private IEnumerator TrackCloneCollapse(GameObject clone, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (clone != null && _activeClones.Contains(clone))
            {
                CollapseObject(clone, true);
            }
        }

        // --------------------------------------------------
        // Teleport Override
        // --------------------------------------------------
        private void TeleportToAnchor()
        {
            GameObject target = null;
            if (_activeAnchors.Count > 0)
                target = _activeAnchors[_activeAnchors.Count - 1];
            else if (_activeClones.Count > 0)
                target = _activeClones[_activeClones.Count - 1];

            if (target != null)
            {
                // Move player to target
                transform.position = target.transform.position;
                
                // Vfx optional here
            }
        }

        // --------------------------------------------------
        // Spore Armor
        // --------------------------------------------------
        private IEnumerator SporeArmorRoutine()
        {
            _armorActive = true;
            if (armorVFX != null) armorVFX.SetActive(true);

            yield return new WaitForSeconds(5f);

            _armorActive = false;
            if (armorVFX != null) armorVFX.SetActive(false);
        }

        // --------------------------------------------------
        // Mass Detonation
        // --------------------------------------------------
        private IEnumerator MassDetonationRoutine(Core.AbilityData ability)
        {
            List<GameObject> allObjects = new List<GameObject>();
            allObjects.AddRange(_activeAnchors);
            allObjects.AddRange(_activeClones);

            foreach (var obj in allObjects)
            {
                if (obj != null)
                {
                    DetonateAt(obj.transform.position, ability);
                    Destroy(obj);
                    TriggerPassiveLearning();
                }
            }

            _activeAnchors.Clear();
            _activeClones.Clear();
            yield return null;
        }

        // --------------------------------------------------
        // Distributed Collapse (Super)
        // --------------------------------------------------
        private IEnumerator DistributedCollapseRoutine(Core.AbilityData ability)
        {
            // Untargetable state
            if (_health != null) _health.isImmortal = true;
            
            // Spawn 5 random anchors around player
            for (int i = 0; i < 5; i++)
            {
                Vector2 rand = Random.insideUnitCircle * 8f;
                Vector3 pos = transform.position + new Vector3(rand.x, 0, rand.y);
                RegisterAnchor(pos);
            }

            yield return new WaitForSeconds(1f); // Brief pause before chain reaction

            // Detonate everything
            StartCoroutine(MassDetonationRoutine(ability));

            // Rematerialize at a random anchor position (last registered)
            TeleportToAnchor();

            if (_health != null) _health.isImmortal = false;
        }

        // --------------------------------------------------
        // Utility
        // --------------------------------------------------
        private void DetonateAt(Vector3 position, Core.AbilityData ability)
        {
            var hits = Physics.OverlapSphere(position, 4f, _executor.enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;
                
                health.TakeDamage(ability.BuildDamage().ToVDamage(transform));
                Core.StatusEffectManager.Instance?.Apply(health, Core.StatusEffectType.Infection, 4f, 1, transform);
            }

            // Spawn explosion VFX
            string poolKey = "SporeExplosionVFX";
            if (Core.ObjectPoolManager.Instance != null)
            {
                var vfx = Core.ObjectPoolManager.Instance.Spawn(poolKey, position, Quaternion.identity);
                if (vfx != null) Core.ObjectPoolManager.Instance.Despawn(poolKey, vfx, 2f);
            }
        }

        private void CollapseObject(GameObject obj, bool isClone)
        {
            if (isClone) _activeClones.Remove(obj);
            else _activeAnchors.Remove(obj);
            
            Destroy(obj);
            TriggerPassiveLearning();
        }

        private void TriggerPassiveLearning()
        {
            if (_energy != null) _energy.RestoreEnergy(energyReturnPerCollapse);
            StartCoroutine(LearningBuffRoutine());
        }

        private IEnumerator LearningBuffRoutine()
        {
            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed = _originalWalkSpeed * 1.3f;
                _tpc.freeSpeed.runningSpeed = _originalRunSpeed * 1.3f;
                _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed * 1.3f;
            }

            yield return new WaitForSeconds(learningBuffDuration);

            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed = _originalWalkSpeed;
                _tpc.freeSpeed.runningSpeed = _originalRunSpeed;
                _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed;
            }
        }

        private Vector3 GetAimDirection()
        {
            var cam = Camera.main;
            if (cam == null) return transform.forward;
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Transform muzzle = _executor.muzzlePoint != null ? _executor.muzzlePoint : transform;
                return (hit.point - muzzle.position).normalized;
            }
            return cam.transform.forward;
        }
    }

    // Helper component for Spore Ball impact
    public class SporeAnchorSpawner : MonoBehaviour
    {
        private GustCharacter _gust;
        private GameObject _anchorPrefab;
        private bool _hasTriggered;

        public void Init(GustCharacter gust, GameObject anchorPrefab)
        {
            _gust = gust;
            _anchorPrefab = anchorPrefab;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasTriggered) return;
            _hasTriggered = true;

            if (_gust != null)
            {
                _gust.RegisterAnchor(transform.position);
            }
            
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }
}
