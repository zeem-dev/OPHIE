// ============================================================
// OPHIO - GoonCharacter
// Main controller for Dante (Subject #7 / GOON).
// Handles Apex Ignition passive and fire-based abilities.
// ============================================================

using System.Collections;
using UnityEngine;
using Invector;
using Invector.vCharacterController;

namespace OPHIO.Characters
{
    public class GoonCharacter : MonoBehaviour
    {
        private Core.EnergyManager _energy;
        private vHealthController _health;
        private vThirdPersonController _tpc;
        private Core.AbilityExecutor _executor;
        private Animator _animator;

        [Header("Apex Ignition Passive")]
        public float maxHeat = 100f;
        public float heatPerSecondInCombat = 5f;
        public float heatDecayRate = 10f;
        public float heatDecayDelay = 5f;

        [Header("Heat Bonuses (at 100% Heat)")]
        public float maxFireDamageBonus = 0.5f; // +50% fire damage
        public float maxMoveSpeedBonus = 0.25f; // +25% move speed

        public float CurrentHeat { get; private set; }
        public float HeatPercent => CurrentHeat / maxHeat;
        private float _decayTimer;
        private bool _inCombat;

        [Header("Thermal Dash")]
        public GameObject fireTrailPrefab;
        public float dashForce = 15f;
        public float trailDuration = 3f;

        [Header("Heat Shield")]
        public GameObject heatShieldVFX;
        private bool _heatShieldActive;
        private float _heatShieldDamageReduction = 0.3f; // 30% damage reduction

        private float _originalWalkSpeed;
        private float _originalRunSpeed;
        private float _originalSprintSpeed;

        private bool _totalIgnitionActive;

        public System.Action<float, float> onHeatChanged;

        private void Awake()
        {
            _energy = GetComponent<Core.EnergyManager>();
            _health = GetComponent<vHealthController>();
            _tpc = GetComponent<vThirdPersonController>();
            _executor = GetComponent<Core.AbilityExecutor>();
            _animator = GetComponent<Animator>();
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

            var meleeManager = GetComponent<Invector.vMelee.vMeleeManager>();
            if (meleeManager != null)
                meleeManager.onDamageHit.AddListener(OnMeleeHit);
        }

        private void Update()
        {
            HandleHeat();
        }

        private void HandleHeat()
        {
            if (_inCombat)
            {
                AddHeat(heatPerSecondInCombat * Time.deltaTime);
                _decayTimer = heatDecayDelay;
            }
            else
            {
                if (_decayTimer > 0f)
                    _decayTimer -= Time.deltaTime;
                else
                    RemoveHeat(heatDecayRate * Time.deltaTime);
            }
        }

        public void AddHeat(float amount)
        {
            CurrentHeat = Mathf.Min(maxHeat, CurrentHeat + amount);
            onHeatChanged?.Invoke(CurrentHeat, maxHeat);
            UpdateSpeedFromHeat();
        }

        public void RemoveHeat(float amount)
        {
            CurrentHeat = Mathf.Max(0f, CurrentHeat - amount);
            onHeatChanged?.Invoke(CurrentHeat, maxHeat);
            UpdateSpeedFromHeat();
        }

        private void UpdateSpeedFromHeat()
        {
            if (_tpc == null || _totalIgnitionActive) return;

            float bonus = 1f + (HeatPercent * maxMoveSpeedBonus);
            _tpc.freeSpeed.walkSpeed = _originalWalkSpeed * bonus;
            _tpc.freeSpeed.runningSpeed = _originalRunSpeed * bonus;
            _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed * bonus;
        }

        public float GetFireDamageMultiplier()
        {
            return 1f + (HeatPercent * maxFireDamageBonus);
        }

        public void SetCombatState(bool state)
        {
            _inCombat = state;
            _energy?.SetCombatState(state);
            if (!state) _decayTimer = heatDecayDelay;
        }

        private void OnDamageReceived(vDamage damage)
        {
            SetCombatState(true);
            
            // Heat Shield logic: retaliate with burn and reduce incoming damage
            if (_heatShieldActive && damage != null)
            {
                damage.damageValue = (int)(damage.damageValue * (1f - _heatShieldDamageReduction));

                if (damage.sender != null)
                {
                    float dist = Vector3.Distance(transform.position, damage.sender.position);
                    if (dist < 3f)
                    {
                        var health = damage.sender.GetComponent<vHealthController>();
                        if (health != null)
                        {
                            Core.StatusEffectManager.Instance?.Apply(health, Core.StatusEffectType.Burn, 4f, 1, transform);
                        }
                    }
                }
            }
        }

        private void OnMeleeHit(Invector.vMelee.vHitInfo hitInfo)
        {
            SetCombatState(true);
            
            // Total Ignition logic: melee attacks apply burn
            if (_totalIgnitionActive && hitInfo.targetCollider != null)
            {
                var health = hitInfo.targetCollider.GetComponent<vHealthController>();
                if (health != null)
                {
                    Core.StatusEffectManager.Instance?.Apply(health, Core.StatusEffectType.Burn, 3f, 1, transform);
                }
            }
        }

        // --------------------------------------------------
        // Abilities dispatched from AbilityExecutor
        // --------------------------------------------------
        private void OnAbilityActivated(Core.AbilityData ability)
        {
            SetCombatState(true);

            switch (ability.abilityName)
            {
                case "Thermal Dash": StartCoroutine(ThermalDashRoutine(ability)); break;
                case "Heat Shield": StartCoroutine(HeatShieldRoutine()); break;
                case "Total Ignition": StartCoroutine(TotalIgnitionRoutine()); break;
                case "Fire Barrage": StartCoroutine(FireBarrageRoutine(ability)); break;
            }
        }

        private IEnumerator ThermalDashRoutine(Core.AbilityData ability)
        {
            Vector3 dashDir = transform.forward;
            if (Input.GetKey(KeyCode.W)) dashDir = transform.forward;
            else if (Input.GetKey(KeyCode.S)) dashDir = -transform.forward;
            else if (Input.GetKey(KeyCode.A)) dashDir = -transform.right;
            else if (Input.GetKey(KeyCode.D)) dashDir = transform.right;

            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(dashDir * dashForce, ForceMode.VelocityChange);

            if (fireTrailPrefab != null)
            {
                string key = "GoonFireTrail";
                Core.ObjectPoolManager.Instance.RegisterPool(key, fireTrailPrefab, 3);
                var trail = Core.ObjectPoolManager.Instance.Spawn(key, transform.position, transform.rotation);
                StartCoroutine(FireTrailRoutine(trail, ability, key));
            }

            AddHeat(15f);
            yield return null;
        }

        private IEnumerator FireTrailRoutine(GameObject trailObj, Core.AbilityData ability, string poolKey)
        {
            float elapsed = 0f;
            while (elapsed < trailDuration)
            {
                var hits = Physics.OverlapSphere(trailObj.transform.position, 1.5f, _executor.enemyLayer);
                foreach (var hit in hits)
                {
                    var health = hit.GetComponent<vHealthController>();
                    if (health == null || health.isDead) continue;
                    var dmg = ability.BuildDamage();
                    dmg.damageValue = (int)(dmg.damageValue * GetFireDamageMultiplier());
                    health.TakeDamage(dmg.ToVDamage(transform));
                    Core.StatusEffectManager.Instance?.Apply(health, Core.StatusEffectType.Burn, 3f, 1, transform);
                }
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
            Core.ObjectPoolManager.Instance.Despawn(poolKey, trailObj);
        }

        private IEnumerator HeatShieldRoutine()
        {
            _heatShieldActive = true;

            if (heatShieldVFX != null) heatShieldVFX.SetActive(true);

            AddHeat(20f);
            yield return new WaitForSeconds(6f);

            _heatShieldActive = false;
            if (heatShieldVFX != null) heatShieldVFX.SetActive(false);
        }

        private IEnumerator FireBarrageRoutine(Core.AbilityData ability)
        {
            if (ability.vfxPrefab == null) yield break;

            string poolKey = "GoonFireBarrage";
            Core.ObjectPoolManager.Instance.RegisterPool(poolKey, ability.vfxPrefab, 10);

            Transform muzzle = _executor.muzzlePoint != null ? _executor.muzzlePoint : transform;
            
            for (int i = 0; i < 5; i++)
            {
                Vector3 origin = muzzle.position;
                Vector3 dir = GetAimDirection();
                
                // Add slight spread
                dir = Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0) * dir;

                var projObj = Core.ObjectPoolManager.Instance.Spawn(poolKey, origin, Quaternion.LookRotation(dir));
                if (projObj != null)
                {
                    var mover = projObj.GetComponent<Core.SimpleProjectileMover>();
                    if (mover == null) mover = projObj.AddComponent<Core.SimpleProjectileMover>();
                    
                    var dmg = ability.BuildDamage();
                    dmg.damageValue = (int)(dmg.damageValue * GetFireDamageMultiplier());
                    
                    mover.Init(dir, ability.range, poolKey, dmg, _executor.enemyLayer, transform);
                }

                AddHeat(2f);
                yield return new WaitForSeconds(0.15f);
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

        private IEnumerator TotalIgnitionRoutine()
        {
            _totalIgnitionActive = true;
            CurrentHeat = maxHeat;
            
            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed = _originalWalkSpeed * 1.5f;
                _tpc.freeSpeed.runningSpeed = _originalRunSpeed * 1.5f;
                _tpc.freeSpeed.sprintSpeed = _originalSprintSpeed * 1.5f;
            }

            // Environmental ignition aura
            float duration = 8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                var hits = Physics.OverlapSphere(transform.position, 4f, _executor.enemyLayer);
                foreach (var hit in hits)
                {
                    var health = hit.GetComponent<vHealthController>();
                    if (health != null && !health.isDead)
                        Core.StatusEffectManager.Instance?.Apply(health, Core.StatusEffectType.Burn, 2f, 1, transform);
                }
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }

            _totalIgnitionActive = false;
            UpdateSpeedFromHeat();
        }
    }
}
