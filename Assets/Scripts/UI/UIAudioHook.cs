// ============================================================
//  OPHIO — UIAudioHook
//  Attach to any Button to auto-play click/hover SFX.
//  Add to every UI Button in scene.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OPHIO.UI
{
    [RequireComponent(typeof(Button))]
    public class UIAudioHook : MonoBehaviour,
        IPointerEnterHandler, IPointerClickHandler
    {
        [Tooltip("Leave at default to use AudioManager's sfxButtonClick")]
        public AudioClip overrideClickSound;
        public AudioClip overrideHoverSound;

        public void OnPointerEnter(PointerEventData eventData)
        {
            var audio = Core.AudioManager.Instance;
            if (audio == null) return;
            if (overrideHoverSound != null) audio.PlaySFX(overrideHoverSound, 0.5f);
            else audio.PlayButtonHover();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var audio = Core.AudioManager.Instance;
            if (audio == null) return;
            if (overrideClickSound != null) audio.PlaySFX(overrideClickSound);
            else audio.PlayButtonClick();
        }
    }
}
