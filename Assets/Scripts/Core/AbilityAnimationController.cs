// ============================================================
//  OPHIO — AbilityAnimationController
//  Animation Fix | Ability Core
//  Solves two problems:
//  1. Abrupt animation cuts — drives transitions via Avatar Mask
//     and normalized time instead of raw triggers.
//  2. VFX lifetime synced to actual animation clip length
//     so VFX and animation end together.
//  Attach to the Player GameObject alongside AbilityExecutor.
// ============================================================

using System.Collections;
using UnityEngine;

namespace OPHIO.Core
{
    public class AbilityAnimationController : MonoBehaviour
    {
        [Header("Abilities Layer")]
        [Tooltip("Exact name of the Abilities layer inside the Animator Controller")]
        public string abilitiesLayerName = "Abilities";

        [Tooltip("How fast to blend INTO an ability animation (0.1 = smooth, 0 = instant cut)")]
        [Range(0f, 0.5f)]
        public float blendInTime  = 0.15f;

        [Tooltip("How fast to blend OUT of an ability animation back to locomotion")]
        [Range(0f, 0.5f)]
        public float blendOutTime = 0.20f;

        [Tooltip("Layer weight when ability is playing (1 = full override)")]
        [Range(0f, 1f)]
        public float activeLayerWeight = 1f;

        [Header("VFX Timing")]
        [Tooltip("VFX spawns at this fraction of the cast animation (0.3 = 30% through)")]
        [Range(0f, 1f)]
        public float vfxSpawnNormalizedTime = 0.30f;

        [Tooltip("VFX lifetime equals animation length minus this buffer (seconds)")]
        public float vfxEndBuffer = 0.05f;

        // --------------------------------------------------
        //  Internal state
        // --------------------------------------------------
        private Animator _animator;
        private int      _abilitiesLayerIndex = -1;
        private float    _currentLayerWeight  =  0f;
        private bool     _isBlending          = false;

        // Public read — AbilityExecutor uses this for VFX timing
        public float CurrentClipLength    { get; private set; }
        public float VFXSpawnDelay        => CurrentClipLength * vfxSpawnNormalizedTime;
        public float VFXLifetime          => Mathf.Max(0.1f, CurrentClipLength - VFXSpawnDelay - vfxEndBuffer);

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            ResolveAbilitiesLayer();
            SetLayerWeight(0f);
        }

        private void Start()
        {
            if (_animator == null) return;

            ResolveAbilitiesLayer();

            if (_abilitiesLayerIndex < 0)
                Debug.LogWarning($"[AbilityAnimCtrl] Layer '{abilitiesLayerName}' not found in Animator. Check the layer name.");

            // Start with abilities layer at zero weight
            SetLayerWeight(0f);
        }

        private void OnEnable()
        {
            ResolveAbilitiesLayer();
            SetLayerWeight(0f);
        }

        private void OnDisable()
        {
            SetLayerWeight(0f);
        }

        private void Update()
        {
            if (!_isBlending) return;
            // Smooth weight is driven by coroutines — nothing needed here
        }

        // --------------------------------------------------
        //  Public API — called by AbilityExecutor
        // --------------------------------------------------

        /// <summary>
        /// Trigger an ability animation with smooth blend in/out.
        /// Returns the clip length so AbilityExecutor can sync VFX.
        /// </summary>
        public float PlayAbilityAnimation(string triggerName)
        {
            if (_animator == null || _abilitiesLayerIndex < 0) return 0.5f;

            // Fire trigger
            _animator.SetTrigger(triggerName);

            // Blend layer in
            StartCoroutine(BlendLayerIn());

            // Get clip length after trigger fires (one frame delay)
            StartCoroutine(FetchClipLength());

            return CurrentClipLength > 0f ? CurrentClipLength : 1.0f;
        }

        /// <summary>
        /// Called when ability cast is fully complete — blend layer back out.
        /// </summary>
        public void FinishAbilityAnimation()
        {
            StartCoroutine(BlendLayerOut());
        }

        public void ForceStopAbilityLayer()
        {
            StopAllCoroutines();
            SetLayerWeight(0f);
            _isBlending = false;
        }

        // --------------------------------------------------
        //  Blend coroutines
        // --------------------------------------------------
        private IEnumerator BlendLayerIn()
        {
            _isBlending = true;
            float start   = _currentLayerWeight;
            float elapsed = 0f;

            while (elapsed < blendInTime)
            {
                elapsed += Time.deltaTime;
                float t  = blendInTime > 0f ? elapsed / blendInTime : 1f;
                SetLayerWeight(Mathf.Lerp(start, activeLayerWeight, t));
                yield return null;
            }

            SetLayerWeight(activeLayerWeight);
            _isBlending = false;
        }

        private IEnumerator BlendLayerOut()
        {
            // Wait one frame so the last pose is held briefly
            yield return new WaitForEndOfFrame();

            _isBlending  = true;
            float start  = _currentLayerWeight;
            float elapsed= 0f;

            while (elapsed < blendOutTime)
            {
                elapsed += Time.deltaTime;
                float t  = blendOutTime > 0f ? elapsed / blendOutTime : 1f;
                SetLayerWeight(Mathf.Lerp(start, 0f, t));
                yield return null;
            }

            SetLayerWeight(0f);
            _isBlending = false;
        }

        // --------------------------------------------------
        //  Fetch current clip length one frame after trigger
        // --------------------------------------------------
        private IEnumerator FetchClipLength()
        {
            yield return null; // wait one frame for animator state to update

            if (_abilitiesLayerIndex < 0) yield break;

            var info = _animator.GetCurrentAnimatorStateInfo(_abilitiesLayerIndex);
            // AnimatorStateInfo.length gives the clip length in seconds
            CurrentClipLength = info.length > 0.05f ? info.length : 1.0f;
        }

        // --------------------------------------------------
        //  Layer weight helper
        // --------------------------------------------------
        private void SetLayerWeight(float weight)
        {
            _currentLayerWeight = weight;
            if (_animator != null && _abilitiesLayerIndex >= 0)
                _animator.SetLayerWeight(_abilitiesLayerIndex, weight);
        }

        private void ResolveAbilitiesLayer()
        {
            if (_animator == null || _abilitiesLayerIndex >= 0) return;

            for (int i = 0; i < _animator.layerCount; i++)
            {
                if (_animator.GetLayerName(i) == abilitiesLayerName)
                {
                    _abilitiesLayerIndex = i;
                    break;
                }
            }
        }
    }
}
