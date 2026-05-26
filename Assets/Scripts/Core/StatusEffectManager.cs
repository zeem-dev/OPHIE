// ============================================================
//  OPHIO — StatusEffectManager
//  Day 2 | Runtime Systems
//  Singleton. Centrally manages all active status effects.
//  Batched tick — 0.2s interval, not every frame.
//  Attach to one empty GameObject in the scene.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Invector;

namespace OPHIO.Core
{
    public class StatusEffectManager : MonoBehaviour
    {
        // --------------------------------------------------
        //  Singleton
        // --------------------------------------------------
        public static StatusEffectManager Instance { get; private set; }

        [Header("Tick Settings")]
        [Tooltip("How often status effects are processed (seconds). 0.2 = 5 times/sec.")]
        public float tickInterval = 0.2f;

        // Maps each target to its list of active effects
        private Dictionary<vHealthController, List<StatusEffect>> _activeEffects
            = new Dictionary<vHealthController, List<StatusEffect>>();

        private List<vHealthController> _toRemove = new List<vHealthController>();

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(TickRoutine());
        }

        // --------------------------------------------------
        //  Public API
        // --------------------------------------------------

        /// <summary>Apply a status effect to a target.</summary>
        public void Apply(vHealthController target, StatusEffectType type,
                          float duration, int stacks, Transform appliedBy)
        {
            if (target == null || target.isDead) return;

            if (!_activeEffects.ContainsKey(target))
                _activeEffects[target] = new List<StatusEffect>();

            var list = _activeEffects[target];

            // If the same type is already active — refresh duration and add stacks
            var existing = list.Find(e => e.effectType == type);
            if (existing != null)
            {
                existing.timeRemaining = Mathf.Max(existing.timeRemaining, duration);
                existing.stacks        = Mathf.Min(existing.stacks + stacks, 5);
                existing.OnApply(target);
                return;
            }

            // Brand new effect
            StatusEffect effect = CreateEffect(type, duration, stacks, appliedBy);
            if (effect == null) return;
            list.Add(effect);
            effect.OnApply(target);
        }

        /// <summary>Remove all effects of one type from a target.</summary>
        public void Cleanse(vHealthController target, StatusEffectType type)
        {
            if (!_activeEffects.ContainsKey(target)) return;
            var list = _activeEffects[target];
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].effectType == type)
                {
                    list[i].OnExpire(target);
                    list.RemoveAt(i);
                }
            }
        }

        /// <summary>Remove ALL effects from a target (on death or full cleanse).</summary>
        public void CleanseAll(vHealthController target)
        {
            if (!_activeEffects.ContainsKey(target)) return;
            foreach (var e in _activeEffects[target]) e.OnExpire(target);
            _activeEffects[target].Clear();
        }

        /// <summary>Returns true if the target currently has this effect active.</summary>
        public bool HasEffect(vHealthController target, StatusEffectType type)
        {
            if (!_activeEffects.ContainsKey(target)) return false;
            return _activeEffects[target].Exists(e => e.effectType == type);
        }

        // --------------------------------------------------
        //  Tick coroutine — batched, not per-frame
        // --------------------------------------------------
        private IEnumerator TickRoutine()
        {
            var wait = new WaitForSeconds(tickInterval);
            while (true)
            {
                yield return wait;
                ProcessTick();
            }
        }

        private void ProcessTick()
        {
            _toRemove.Clear();

            foreach (var kvp in _activeEffects)
            {
                var target = kvp.Key;
                var list   = kvp.Value;

                if (target == null || target.isDead)
                {
                    _toRemove.Add(target);
                    continue;
                }

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    list[i].OnTick(target, tickInterval);
                    if (list[i].IsExpired)
                    {
                        list[i].OnExpire(target);
                        list.RemoveAt(i);
                    }
                }

                if (list.Count == 0) _toRemove.Add(target);
            }

            foreach (var t in _toRemove) _activeEffects.Remove(t);
        }

        // --------------------------------------------------
        //  Factory — creates the correct effect instance
        // --------------------------------------------------
        private StatusEffect CreateEffect(StatusEffectType type, float duration,
                                          int stacks, Transform appliedBy)
        {
            switch (type)
            {
                case StatusEffectType.Burn:
                    return new BurnEffect(duration, stacks, appliedBy);
                case StatusEffectType.Shock:
                    return new ShockEffect(duration, stacks, appliedBy);
                case StatusEffectType.Infection:
                    return new InfectionEffect(duration, stacks, appliedBy);
                default:
                    return null;
            }
        }
    }
}
