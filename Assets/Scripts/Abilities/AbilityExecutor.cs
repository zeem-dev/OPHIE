// ============================================================
//  OPHIO — AbilityExecutor (Animation + VFX timing fixed)
//  Ability Core
//  Uses AbilityAnimationController for smooth blend in/out.
//  VFX lifetime synced to actual animation clip length.
//  Attach to the Player GameObject.
// ============================================================

using System.Collections;
using UnityEngine;
using Invector;
using Invector.vCharacterController;
using Invector.vShooter;

namespace OPHIO.Core
{
    public class AbilityExecutor : MonoBehaviour
    {
        // --------------------------------------------------
        //  Cached references
        // --------------------------------------------------
        private EnergyManager              _energy;
        private AbilityLoadout             _loadout;
        private Animator                   _animator;
        private vThirdPersonController     _tpc;
        private VFXSpawnHelper             _vfxHelper;
        private AbilityAnimationController _animCtrl;
        private PlayerGroundingGuard       _groundingGuard;

        [Header("Projectile Launch Point")]
        [Tooltip("Weapon muzzle transform — overrides VFXSpawnHelper for projectiles")]
        public Transform muzzlePoint;

        [Header("Layer Masks")]
        public LayerMask enemyLayer;

        // --------------------------------------------------
        //  Cooldown tracking
        // --------------------------------------------------
        private float[] _cooldownRemaining = new float[4];
        private bool[]  _isCasting         = new bool[4];

        public float GetCooldownRemaining(int slot) => _cooldownRemaining[slot];
        public float GetCooldownMax(int slot)
        {
            var a = _loadout?.GetSlot(slot);
            return a != null ? a.cooldown : 1f;
        }
        public bool IsOnCooldown(int slot) => _cooldownRemaining[slot] > 0f;

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            _energy   = GetComponent<EnergyManager>();
            _loadout  = GetComponent<AbilityLoadout>();
            _animator = GetComponent<Animator>();
            _tpc      = GetComponent<vThirdPersonController>();
            _vfxHelper= GetComponent<VFXSpawnHelper>();
            _animCtrl = GetComponent<AbilityAnimationController>();
            _groundingGuard = GetComponent<PlayerGroundingGuard>();
        }

        private void Update()
        {
            TickCooldowns();
            HandleInput(); // keyboard always active (Q/E/R/F)
        }

        // --------------------------------------------------
        //  Input
        // --------------------------------------------------
        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Q)) TryActivate(0);
            if (Input.GetKeyDown(KeyCode.E)) TryActivate(1);
            if (Input.GetKeyDown(KeyCode.R)) TryActivate(2);
            if (Input.GetKeyDown(KeyCode.F)) TryActivate(3);
        }

        // --------------------------------------------------
        //  Core activation
        // --------------------------------------------------
        public void TryActivate(int slotIndex)
        {
            var ability = _loadout?.GetSlot(slotIndex);
            if (ability == null) return;
            if (_isCasting[slotIndex]) return;
            if (IsOnCooldown(slotIndex))
            {
                Debug.Log($"[Ability] {ability.abilityName} on cooldown ({_cooldownRemaining[slotIndex]:F1}s)");
                return;
            }
            if (!_energy.ConsumeEnergy(ability.energyCost))
            {
                Debug.Log($"[Ability] Not enough energy for {ability.abilityName}");
                return;
            }
            StartCoroutine(ExecuteAbility(slotIndex, ability));
        }

        private IEnumerator ExecuteAbility(int slotIndex, AbilityData ability)
        {
            _isCasting[slotIndex] = true;
            if (_groundingGuard == null)
                _groundingGuard = GetComponent<PlayerGroundingGuard>();
            _groundingGuard?.RequestClamp(Mathf.Max(ability.castDuration, 0.35f));

            // 1 — Play animation via AbilityAnimationController (smooth blend)
            float clipLength = ability.castDuration;
            if (_animCtrl != null && !string.IsNullOrEmpty(ability.animTrigger))
            {
                clipLength = _animCtrl.PlayAbilityAnimation(ability.animTrigger);
                // Wait one frame so FetchClipLength coroutine runs
                yield return null;
                // Now get the real clip length
                clipLength = _animCtrl.CurrentClipLength > 0.05f
                    ? _animCtrl.CurrentClipLength
                    : ability.castDuration;
            }
            else if (_animator != null && !string.IsNullOrEmpty(ability.animTrigger))
            {
                _animator.SetTrigger(ability.animTrigger);
            }

            // 2 — Play activation sound immediately
            if (ability.activationSound != null)
                AudioSource.PlayClipAtPoint(ability.activationSound, transform.position);

            // 3 — Wait until the "impact frame" of the animation
            //     Use AbilityAnimationController's vfxSpawnNormalizedTime if available
            float vfxDelay = _animCtrl != null
                ? _animCtrl.VFXSpawnDelay
                : clipLength * 0.35f;

            yield return new WaitForSeconds(vfxDelay);

            // 4 — Apply damage / effect at impact frame
            ApplyAbilityEffect(ability);

            // 5 — Spawn VFX at impact frame (synced to animation)
            //     Lifetime = remainder of animation so they end together
            float vfxLifetime = _animCtrl != null
                ? _animCtrl.VFXLifetime
                : clipLength - vfxDelay;

            SpawnCastVFX(ability, vfxLifetime);

            // 6 — Wait for animation to finish
            float remaining = clipLength - vfxDelay;
            yield return new WaitForSeconds(remaining);

            // 7 — Blend animation back out
            _animCtrl?.FinishAbilityAnimation();
            _groundingGuard?.RequestClamp(0.75f);
            _groundingGuard?.ForceClampNow();

            yield return new WaitForSeconds(_animCtrl != null ? _animCtrl.blendOutTime + 0.05f : 0.05f);
            _animCtrl?.ForceStopAbilityLayer();
            _groundingGuard?.ForceClampNow();

            // 8 — Start cooldown
            _cooldownRemaining[slotIndex] = ability.cooldown;
            _isCasting[slotIndex]         = false;
        }

        // --------------------------------------------------
        //  Effect dispatch
        // --------------------------------------------------
        private void ApplyAbilityEffect(AbilityData ability)
        {
            switch (ability.targeting)
            {
                case AbilityTargeting.AreaAroundSelf:
                    ApplyAoE(ability, transform.position, ability.radius);
                    break;
                case AbilityTargeting.SingleTarget:
                    ApplySingleTarget(ability);
                    break;
                case AbilityTargeting.DirectionalCone:
                    ApplyCone(ability);
                    break;
                case AbilityTargeting.Projectile:
                    LaunchProjectile(ability);
                    break;
                case AbilityTargeting.Self:
                case AbilityTargeting.Trail:
                    SendMessage("OnAbilityActivated", ability,
                        SendMessageOptions.DontRequireReceiver);
                    break;
            }
        }

        // --------------------------------------------------
        //  AoE
        // --------------------------------------------------
        private void ApplyAoE(AbilityData ability, Vector3 center, float radius)
        {
            var hits = Physics.OverlapSphere(center, radius, enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;
                ApplyDamageAndStatus(ability, health, hit.transform);
            }
        }

        // --------------------------------------------------
        //  Single target
        // --------------------------------------------------
        private void ApplySingleTarget(AbilityData ability)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, ability.range, enemyLayer))
            {
                var health = hit.collider.GetComponent<vHealthController>();
                if (health != null && !health.isDead)
                    ApplyDamageAndStatus(ability, health, hit.transform);
            }
        }

        // --------------------------------------------------
        //  Cone
        // --------------------------------------------------
        private void ApplyCone(AbilityData ability)
        {
            var hits = Physics.OverlapSphere(transform.position, ability.range, enemyLayer);
            foreach (var hit in hits)
            {
                Vector3 dir   = (hit.transform.position - transform.position).normalized;
                float   angle = Vector3.Angle(transform.forward, dir);
                if (angle > ability.coneAngle * 0.5f) continue;
                var health    = hit.GetComponent<vHealthController>();
                if (health == null || health.isDead) continue;
                ApplyDamageAndStatus(ability, health, hit.transform);
            }
        }

        // --------------------------------------------------
        //  Projectile
        // --------------------------------------------------
        private void LaunchProjectile(AbilityData ability)
        {
            if (string.IsNullOrEmpty(ability.abilityName)) return;
            string poolKey = ability.abilityName.Replace(" ", "") + "Projectile";

            if (ability.vfxPrefab != null)
                ObjectPoolManager.Instance.RegisterPool(poolKey, ability.vfxPrefab, 10);

            Vector3 origin = muzzlePoint != null
                ? muzzlePoint.position
                : (_vfxHelper != null
                    ? _vfxHelper.GetCastPosition(AbilityTargeting.Projectile)
                    : transform.position + Vector3.up * 1.4f);

            Vector3 dir  = GetAimDirection(origin);
            var projObj  = ObjectPoolManager.Instance.Spawn(
                poolKey, origin, Quaternion.LookRotation(dir));
            if (projObj == null) return;

            var invProj = projObj.GetComponent<vProjectileControl>();
            if (invProj != null)
            {
                invProj.damage = ability.BuildDamage().ToVDamage(transform);
            }
            else
            {
                var mover = projObj.GetComponent<SimpleProjectileMover>();
                if (mover == null) mover = projObj.AddComponent<SimpleProjectileMover>();

                // Pass hit VFX prefab so mover spawns it at enemy position
                mover.hitVFXPrefab  = ability.vfxPrefab;
                mover.hitVFXPoolKey = poolKey + "_Hit";

                mover.Init(dir, ability.range, poolKey,
                    ability.BuildDamage(), enemyLayer, transform);
            }
        }

        // --------------------------------------------------
        //  Damage + status
        // --------------------------------------------------
        private void ApplyDamageAndStatus(AbilityData ability,
                                          vHealthController target,
                                          Transform targetTf)
        {
            var ophioDmg = ability.BuildDamage();
            target.TakeDamage(ophioDmg.ToVDamage(transform));

            if (StatusEffectManager.Instance != null && ophioDmg.RollStatus())
            {
                var status = ophioDmg.ResolvedStatus();
                if (status != StatusEffectType.None)
                    StatusEffectManager.Instance.Apply(
                        target, status, 3f, ophioDmg.statusStacks, transform);
            }

            if (ability.vfxPrefab != null)
            {
                string  hitKey  = ability.abilityName.Replace(" ", "") + "HitVFX";
                Vector3 hitPos  = VFXSpawnHelper.GetHitPosition(targetTf);
                ObjectPoolManager.Instance.RegisterPool(hitKey, ability.vfxPrefab, 5);
                var vfx = ObjectPoolManager.Instance.Spawn(
                    hitKey, hitPos, Quaternion.identity);
                if (vfx != null)
                    ObjectPoolManager.Instance.Despawn(hitKey, vfx, 2f);
            }
        }

        // --------------------------------------------------
        //  Cast VFX — lifetime synced to animation remainder
        // --------------------------------------------------
        private void SpawnCastVFX(AbilityData ability, float lifetime)
        {
            if (ability.vfxPrefab == null) return;

            string  key      = ability.abilityName.Replace(" ", "") + "CastVFX";
            Vector3 spawnPos = _vfxHelper != null
                ? _vfxHelper.GetCastPosition(ability.targeting)
                : transform.position + Vector3.up * 1.4f + ability.vfxOffset;

            ObjectPoolManager.Instance.RegisterPool(key, ability.vfxPrefab, 5);
            var vfx = ObjectPoolManager.Instance.Spawn(
                key, spawnPos, transform.rotation);
            if (vfx != null)
                ObjectPoolManager.Instance.Despawn(key, vfx, Mathf.Max(0.1f, lifetime));
        }

        // --------------------------------------------------
        //  Aim direction
        // --------------------------------------------------
        private Vector3 GetAimDirection(Vector3 fromPosition)
        {
            var cam = Camera.main;
            if (cam == null) return transform.forward;
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                return (hit.point - fromPosition).normalized;
            return cam.transform.forward;
        }

        // --------------------------------------------------
        //  Cooldown ticker
        // --------------------------------------------------
        private void TickCooldowns()
        {
            for (int i = 0; i < 4; i++)
                if (_cooldownRemaining[i] > 0f)
                    _cooldownRemaining[i] = Mathf.Max(0f,
                        _cooldownRemaining[i] - Time.deltaTime);
        }
    }
}
