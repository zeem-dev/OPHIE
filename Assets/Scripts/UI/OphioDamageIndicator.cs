// ============================================================
// OPHIO - OphioDamageIndicator
// HUD + UI
// ===========================================================

using UnityEngine;
using UnityEngine.UI;
using Invector;
using Invector.vCharacterController;

namespace OPHIO.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class OphioDamageIndicator : MonoBehaviour
    {
        [Header("Settings")]
        public float lifetime = 1.5f;
        public float fadeStartTime = 0.8f;
        public float edgeOffset = 100f;

        [Header("References")]
        public Image damageImage;
        public CanvasGroup canvasGroup;

        private float _timer;
        private vThirdPersonController _boundController;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (damageImage == null)
                damageImage = GetComponent<Image>();

            Hide();
        }

        private void OnEnable()
        {
            StartCoroutine(BindToPlayerWhenReady());
        }

        private void OnDisable()
        {
            UnbindFromPlayer();
        }

        public void PlayDamage()
        {
            _timer = 0f;
            canvasGroup.alpha = 1;
        }

        public void PlayDamage(vDamage damage)
        {
            PlayDamage();
        }

        private void Update()
        {
            if (canvasGroup == null || canvasGroup.alpha <= 0f)
                return;

            _timer += Time.deltaTime;

            if (_timer > fadeStartTime)
            {
                float fadeDuration = Mathf.Max(0.01f, lifetime - fadeStartTime);
                float fadeProgress = (_timer - fadeStartTime) / fadeDuration;
                canvasGroup.alpha = 1f - Mathf.Clamp01(fadeProgress);
            }
        }

        private System.Collections.IEnumerator BindToPlayerWhenReady()
        {
            while (isActiveAndEnabled && _boundController == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _boundController = player.GetComponent<vThirdPersonController>();
                    if (_boundController != null)
                    {
                        _boundController.onReceiveDamage.RemoveListener(OnPlayerDamage);
                        _boundController.onReceiveDamage.AddListener(OnPlayerDamage);
                        yield break;
                    }
                }

                yield return null;
            }
        }

        private void UnbindFromPlayer()
        {
            if (_boundController != null)
            {
                _boundController.onReceiveDamage.RemoveListener(OnPlayerDamage);
                _boundController = null;
            }
        }

        private void OnPlayerDamage(vDamage damage)
        {
            PlayDamage(damage);
        }

        private void Hide()
        {
            _timer = lifetime;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }
    }
}
