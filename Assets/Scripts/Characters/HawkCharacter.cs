// ============================================================
//  OPHIO — HawkCharacter
//  Hawk Implementation
//  Main character controller for Hawk.
//  Handles Living Conductor passive, charge system,
//  and ability callbacks.
//  Attach on Player GameObject
// ============================================================

using System.Collections;
using UnityEngine;
using Invector;
using Invector.vCharacterController;

namespace OPHIO.Characters
{
    public class HawkCharacter : MonoBehaviour
    {
        // --------------------------------------------------
        //  References
        // --------------------------------------------------
        private Core.EnergyManager     _energy;
        private Core.AbilityLoadout    _loadout;
        private Core.AbilityExecutor   _executor;
        private vHealthController      _health;
        private vThirdPersonController _tpc;
        private Animator               _animator;

        // --------------------------------------------------
        //  Living Conductor — Charge System
        // --------------------------------------------------
        [Header("Living Conductor Passive")]
        [Tooltip("Max stored electrical charge (0-100)")]
        public float maxCharge          = 100f;
        [Tooltip("Charge gained per second passively")]
        public float ambientChargeRate  = 2f;
        [Tooltip("Charge lost per second when out of combat")]
        public float chargeDecayRate    = 5f;
        [Tooltip("Seconds after last hit before charge starts decaying")]
        public float decayDelay         = 4f;

        [Header("Charge Bonuses (at full charge)")]
        public float maxAttackSpeedBonus  = 0.4f;   // 40%
        public float maxMeleeDamageBonus  = 0.3f;   // 30%

        // --------------------------------------------------
        //  Charge state
        // --------------------------------------------------
        public  float CurrentCharge   { get; private set; }
        public  float ChargePercent   => CurrentCharge / maxCharge;
        private float _decayTimer;
        private bool  _inCombat;

        // --------------------------------------------------
        //  Neural Surge state
        // --------------------------------------------------
        private bool  _neuralSurgeActive;
        private float _originalMoveSpeed;
        private float _originalAnimSpeed;

        // --------------------------------------------------
        //  Volt Dash
        // --------------------------------------------------
        [Header("Volt Dash")]
        public GameObject voltTrailPrefab;
        [Tooltip("How long the electric trail stays active")]
        public float trailDuration = 2f;
        public float dashForce     = 18f;

        // --------------------------------------------------
        //  Energy Siphon
        // --------------------------------------------------
        [Header("Energy Siphon Passive")]
        [Tooltip("Charge restored per melee hit via Energy Siphon")]
        public float siphonChargePerHit = 8f;
        private bool _siphonActive      = false;

        // --------------------------------------------------
        //  Events
        // --------------------------------------------------
        public System.Action<float, float> onChargeChanged; // current, max

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            _energy   = GetComponent<Core.EnergyManager>();
            _loadout  = GetComponent<Core.AbilityLoadout>();
            _executor = GetComponent<Core.AbilityExecutor>();
            _health   = GetComponent<vHealthController>();
            _tpc      = GetComponent<vThirdPersonController>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            CurrentCharge      = 0f;
            _originalMoveSpeed = _tpc     != null ? _tpc.freeSpeed.walkSpeed : 5f;
            _originalAnimSpeed = _animator != null ? _animator.speed          : 1f;

            // Wire Invector events via code — no Inspector wiring required
            if (_tpc != null)
            {
                _tpc.onReceiveDamage.AddListener(OnDamageReceived);
                _tpc.onDead.AddListener(OnDeadHandler);
            }

            // vMeleeManager.onDamageHit is UnityEvent<vHitInfo>
            var meleeManager = GetComponent<Invector.vMelee.vMeleeManager>();
            if (meleeManager != null)
                meleeManager.onDamageHit.AddListener(OnMeleeHitInfo);
        }

        private void Update()
        {
            HandlePassiveCharge();
        }

        // --------------------------------------------------
        //  vMeleeManager.onDamageHit callback (vHitInfo)
        // --------------------------------------------------
        public void OnMeleeHitInfo(Invector.vMelee.vHitInfo hitInfo)
        {
            SetCombatState(true);
            if (_siphonActive) AddCharge(siphonChargePerHit);

            var bridge = GetComponent<HawkAbilityBridge>();
            if (bridge != null && hitInfo.targetCollider != null)
            {
                // Reconstruct a vDamage for the bridge (Energy Siphon needs it)
                var dmg = new vDamage((int)siphonChargePerHit)
                {
                    damageType = "Electric",
                    sender     = transform
                };
                bridge.OnMeleeHit(dmg);
            }
        }

        // --------------------------------------------------
        //  Living Conductor Passive — per-frame charge tick
        // --------------------------------------------------
        private void HandlePassiveCharge()
        {
            if (_inCombat)
            {
                AddCharge(ambientChargeRate * Time.deltaTime);
                _decayTimer = decayDelay;
            }
            else
            {
                if (_decayTimer > 0f)
                    _decayTimer -= Time.deltaTime;
                else
                    RemoveCharge(chargeDecayRate * Time.deltaTime);
            }
        }

        // --------------------------------------------------
        //  Charge helpers
        // --------------------------------------------------
        public void AddCharge(float amount)
        {
            CurrentCharge = Mathf.Min(maxCharge, CurrentCharge + amount);
            onChargeChanged?.Invoke(CurrentCharge, maxCharge);
        }

        public void RemoveCharge(float amount)
        {
            CurrentCharge = Mathf.Max(0f, CurrentCharge - amount);
            onChargeChanged?.Invoke(CurrentCharge, maxCharge);
        }

        public void ConsumeAllCharge()
        {
            CurrentCharge = 0f;
            onChargeChanged?.Invoke(CurrentCharge, maxCharge);
        }

        /// <summary>Returns a damage multiplier based on current charge (1.0 – 1.3).</summary>
        public float GetChargeDamageMultiplier()
        {
            return 1f + (ChargePercent * maxMeleeDamageBonus);
        }

        // --------------------------------------------------
        //  Called by AbilityExecutor via SendMessage
        //  for Self and Trail targeting type abilities
        // --------------------------------------------------
        private void OnAbilityActivated(Core.AbilityData ability)
        {
            switch (ability.abilityName)
            {
                case "Neural Surge":  StartCoroutine(NeuralSurgeRoutine()); break;
                case "Volt Dash":     StartCoroutine(VoltDashRoutine(ability)); break;
                case "Energy Siphon": ActivateEnergySiphon(); break;
            }
        }

        // --------------------------------------------------
        //  Arc Slash — charge gain on successful hit
        // --------------------------------------------------
        public void OnArcSlashHit()
        {
            AddCharge(10f);
            SetCombatState(true);
        }

        // --------------------------------------------------
        //  Discharge — charge-scaled damage multiplier
        // --------------------------------------------------
        public float GetDischargeMultiplier()
        {
            // Scales from 1x (empty) to 2x (full charge)
            return 1f + ChargePercent;
        }

        // --------------------------------------------------
        //  Neural Surge — speed and attack buff for 5 seconds
        // --------------------------------------------------
        private IEnumerator NeuralSurgeRoutine()
        {
            if (_neuralSurgeActive) yield break;
            _neuralSurgeActive = true;

            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed    *= 1.30f;
                _tpc.freeSpeed.runningSpeed *= 1.30f;
                _tpc.freeSpeed.sprintSpeed  *= 1.30f;
            }
            if (_animator != null)
                _animator.speed *= 1.40f;

            AddCharge(15f);

            yield return new WaitForSeconds(5f);

            if (_tpc != null)
            {
                _tpc.freeSpeed.walkSpeed    = _originalMoveSpeed;
                _tpc.freeSpeed.runningSpeed = _originalMoveSpeed * 1.6f;
                _tpc.freeSpeed.sprintSpeed  = _originalMoveSpeed * 2.2f;
            }
            if (_animator != null)
                _animator.speed = _originalAnimSpeed;

            _neuralSurgeActive = false;
        }

        // --------------------------------------------------
        //  Volt Dash — dash force + pooled electric trail
        // --------------------------------------------------
        private IEnumerator VoltDashRoutine(Core.AbilityData ability)
        {
            // Determine dash direction from movement input
            Vector3 dashDir = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) dashDir += transform.forward;
            if (Input.GetKey(KeyCode.S)) dashDir -= transform.forward;
            if (Input.GetKey(KeyCode.A)) dashDir -= transform.right;
            if (Input.GetKey(KeyCode.D)) dashDir += transform.right;
            if (dashDir == Vector3.zero)  dashDir  = transform.forward;
            dashDir.Normalize();

            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(dashDir * dashForce, ForceMode.VelocityChange);

            if (voltTrailPrefab != null)
            {
                string key    = "VoltTrail";
                Core.ObjectPoolManager.Instance.RegisterPool(key, voltTrailPrefab, 3);
                var trail     = Core.ObjectPoolManager.Instance.Spawn(
                    key, transform.position, transform.rotation);

                StartCoroutine(TrailDamageRoutine(trail, ability, key));
            }

            AddCharge(20f);
            yield return null;
        }

        private IEnumerator TrailDamageRoutine(GameObject trailObj,
                                               Core.AbilityData ability,
                                               string poolKey)
        {
            float elapsed = 0f;
            while (elapsed < trailDuration)
            {
                // Damage enemies inside the trail radius every 0.5 seconds
                var hits = Physics.OverlapSphere(trailObj.transform.position, 1.5f,
                    _executor.enemyLayer);
                foreach (var hit in hits)
                {
                    var health = hit.GetComponent<vHealthController>();
                    if (health == null || health.isDead) continue;
                    var dmg = ability.BuildDamage();
                    health.TakeDamage(dmg.ToVDamage(transform));
                    Core.StatusEffectManager.Instance?.Apply(
                        health, Core.StatusEffectType.Shock, 1.5f, 1, transform);
                }
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
            Core.ObjectPoolManager.Instance.Despawn(poolKey, trailObj);
        }

        // --------------------------------------------------
        //  Energy Siphon — enable the passive drain flag
        // --------------------------------------------------
        private void ActivateEnergySiphon()
        {
            _siphonActive = true;
        }

        // --------------------------------------------------
        //  Total Overload Super
        //  AbilityExecutor handles the AoE hit detection.
        //  This routine applies the post-explosion overload buff.
        // --------------------------------------------------
        public IEnumerator TotalOverloadRoutine()
        {
            ConsumeAllCharge();

            // Brief enhanced stats during overload state
            if (_animator != null) _animator.speed          *= 1.5f;
            if (_tpc      != null) _tpc.freeSpeed.walkSpeed *= 1.2f;

            yield return new WaitForSeconds(3f);

            if (_animator != null) _animator.speed          = _originalAnimSpeed;
            if (_tpc      != null) _tpc.freeSpeed.walkSpeed = _originalMoveSpeed;
        }

        // --------------------------------------------------
        //  Invector event callbacks (public for code wiring)
        // --------------------------------------------------

        /// <summary>Wired to vThirdPersonController.onReceiveDamage in Start().</summary>
        public void OnDamageReceived(vDamage damage)
        {
            SetCombatState(true);
            _decayTimer = decayDelay;

            // Living Conductor — absorb incoming electric attacks as charge
            if (damage.damageType == "Electric")
                AddCharge(damage.damageValue * 0.5f);
        }

        /// <summary>Wired to vThirdPersonController.onDead in Start().</summary>
        public void OnDeadHandler(GameObject go)
        {
            StopAllCoroutines();
            ConsumeAllCharge();
        }

        /// <summary>Wired to vMeleeManager.onDamageHit (vDamage overload) in Start().</summary>
        public void OnMeleeHit(vDamage damage)
        {
            SetCombatState(true);
            if (_siphonActive) AddCharge(siphonChargePerHit);

            var bridge = GetComponent<HawkAbilityBridge>();
            bridge?.OnMeleeHit(damage);
        }

        // --------------------------------------------------
        //  Combat state
        // --------------------------------------------------
        public void SetCombatState(bool inCombat)
        {
            _inCombat = inCombat;
            _energy?.SetCombatState(inCombat);
            if (!inCombat) _decayTimer = decayDelay;
        }
    }
}
