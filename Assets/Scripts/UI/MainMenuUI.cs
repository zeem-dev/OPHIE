// ============================================================
//  OPHIO — MainMenuUI
//  Day 14 | Game Flow Completion
//  Simple main menu controller.
//  Attach to the MainMenu Canvas.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace OPHIO.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        public Button playButton;
        public Button quitButton;
        public Button resetProgressButton;

        [Header("Panels")]
        public GameObject confirmResetPanel;

        private void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (resetProgressButton != null)
                resetProgressButton.onClick.AddListener(() => ShowResetConfirm(true));

            if (confirmResetPanel != null)
                confirmResetPanel.SetActive(false);
        }

        public void OnPlayClicked()
        {
            Arena.GameFlowManager.Instance?.StartGame();
        }

        public void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void ShowResetConfirm(bool show)
        {
            if (confirmResetPanel != null)
                confirmResetPanel.SetActive(show);
        }

        public void ConfirmResetProgress()
        {
            Arena.ProgressionManager.Instance?.ResetAllProgress();
            ShowResetConfirm(false);
        }
    }
}
