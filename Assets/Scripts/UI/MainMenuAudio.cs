// ============================================================
//  OPHIO — MainMenuAudio
//  Plays menu music when MainMenu scene loads.
//  Attach to OPHIO_Managers GameObject in MainMenu scene.
// ============================================================

using UnityEngine;

namespace OPHIO.UI
{
    public class MainMenuAudio : MonoBehaviour
    {
        private void Start()
        {
            Core.AudioManager.Instance?.PlayMenuMusic();
        }
    }
}
