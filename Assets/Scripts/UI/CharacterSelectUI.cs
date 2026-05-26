// ============================================================
//  OPHIO — CharacterSelectUI
//  Day 14 | Game Flow Completion
//  Handles character selection grid and preview.
//  Attach to the CharacterSelect Canvas.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OPHIO.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("Character List")]
        public Transform gridContainer;
        public GameObject characterButtonPrefab;

        [Header("Preview Panel")]
        public Text   nameText;
        public Text   loreText;
        public Image  portraitImage;
        public Text   statsText;
        public Button selectButton;

        [Header("Navigation")]
        public Button backButton;

        private Core.CharacterData _selectedData;

        private void Start()
        {
            PopulateGrid();

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectClicked);
                selectButton.interactable = false;
            }

            if (backButton != null)
                backButton.onClick.AddListener(() => Arena.GameFlowManager.Instance?.ReturnToMainMenu());
        }

        private void PopulateGrid()
        {
            if (gridContainer == null || characterButtonPrefab == null) return;

            foreach (Transform child in gridContainer)
                Destroy(child.gameObject);

            var flow = Arena.GameFlowManager.Instance;
            if (flow == null || flow.allCharacters.Count == 0)
            {
                Debug.LogWarning("[CharacterSelectUI] No characters in GameFlowManager.allCharacters!");
                return;
            }

            foreach (var charData in flow.allCharacters)
            {
                if (charData == null) continue;
                var btnObj = Instantiate(characterButtonPrefab, gridContainer);

                // Set button label
                var label  = btnObj.GetComponentInChildren<Text>();
                if (label != null) label.text = charData.characterName.ToUpper();

                // Set portrait if available
                var imgs = btnObj.GetComponentsInChildren<Image>();
                if (imgs.Length > 1 && charData.portrait != null)
                    imgs[1].sprite = charData.portrait;

                // On click — display character + store selection
                var captured = charData;
                var btn      = btnObj.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => DisplayCharacter(captured));
            }
        }

        public void DisplayCharacter(Core.CharacterData data)
        {
            _selectedData = data;

            if (nameText != null) nameText.text = $"{data.characterName} {data.subjectNumber}";
            if (loreText != null) loreText.text = data.shortLore;
            if (portraitImage != null) portraitImage.sprite = data.portrait;

            if (statsText != null)
            {
                statsText.text = $"Health: {data.maxHealth}\n" +
                                 $"Energy: {data.maxEnergy}\n" +
                                 $"Speed: {data.moveSpeed}";
            }

            if (selectButton != null)
                selectButton.interactable = true;
        }

        private void OnSelectClicked()
        {
            if (_selectedData != null)
                Arena.GameFlowManager.Instance?.OnCharacterSelected(_selectedData);
        }
    }
}
