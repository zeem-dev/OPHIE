// ============================================================
//  OPHIO — LoadoutBuilderUI
//  Day 12 | HUD + UI
//  Loadout builder screen — drag abilities into 3 active
//  slots + 1 super slot. Preview stats. Confirm to start.
//  Attach to the LoadoutBuilder Canvas.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OPHIO.UI
{
    public class LoadoutBuilderUI : MonoBehaviour
    {
        [Header("Character Info")]
        public Text   characterNameText;
        public Image  characterPortrait;
        public Text   passiveNameText;
        public Text   passiveDescText;

        [Header("Available Abilities")]
        [Tooltip("Container holding ability buttons from the character's pool")]
        public Transform abilityPoolContainer;
        public GameObject abilityButtonPrefab;

        [Header("Loadout Slots")]
        public LoadoutSlotUI slot1UI;   // Q
        public LoadoutSlotUI slot2UI;   // E
        public LoadoutSlotUI slot3UI;   // R
        public LoadoutSlotUI superSlotUI; // F

        [Header("Ability Preview")]
        public Text  previewNameText;
        public Text  previewDescText;
        public Text  previewDamageText;
        public Text  previewCooldownText;
        public Text  previewEnergyText;
        public Image previewIcon;

        [Header("Level Selection")]
        public Transform levelListContainer;
        public GameObject levelButtonPrefab;

        [Header("Buttons")]
        public Button confirmButton;
        public Button backButton;

        // --------------------------------------------------
        //  State
        // --------------------------------------------------
        private Core.CharacterData _characterData;
        private Core.AbilityData[] _selectedAbilities = new Core.AbilityData[3];
        private Core.AbilityData   _selectedSuper;
        private Arena.LevelConfig  _selectedLevel;

        private int _draggingIndex = -1;  // ability pool index being dragged

        // --------------------------------------------------
        //  Init
        // --------------------------------------------------
        private void Start()
        {
            // Get character from GameFlowManager
            if (Arena.GameFlowManager.Instance != null)
                _characterData = Arena.GameFlowManager.Instance.selectedCharacter;

            // Fallback to Hawk if nothing selected (direct scene play)
            if (_characterData == null)
            {
                var flow = Arena.GameFlowManager.Instance;
                if (flow != null && flow.allCharacters.Count > 0)
                    _characterData = flow.allCharacters[0];
            }

            if (_characterData != null)
                SetupCharacter(_characterData);
            else
                Debug.LogWarning("[LoadoutBuilderUI] No character selected and no fallback found.");

            SetupLevelList();

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirm);
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
        }

        public void SetupCharacter(Core.CharacterData data)
        {
            _characterData = data;

            // Character info
            if (characterNameText != null)
                characterNameText.text = data.characterName;
            if (characterPortrait != null && data.portrait != null)
                characterPortrait.sprite = data.portrait;
            if (passiveNameText != null)
                passiveNameText.text = data.passiveName;
            if (passiveDescText != null)
                passiveDescText.text = data.passiveDesc;

            // Super slot (always the character's super)
            _selectedSuper = data.superAbility;
            if (superSlotUI != null)
                superSlotUI.SetAbility(data.superAbility);

            // Default loadout — first 3 abilities
            for (int i = 0; i < 3 && i < data.abilityPool.Count; i++)
            {
                _selectedAbilities[i] = data.abilityPool[i];
                GetSlotUI(i)?.SetAbility(data.abilityPool[i]);
            }

            // Populate ability pool buttons
            PopulateAbilityPool(data);

            ClearPreview();
        }

        // --------------------------------------------------
        //  Ability Pool
        // --------------------------------------------------
        private void PopulateAbilityPool(Core.CharacterData data)
        {
            if (abilityPoolContainer == null || abilityButtonPrefab == null) return;

            // Clear existing
            foreach (Transform child in abilityPoolContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < data.abilityPool.Count; i++)
            {
                var ability = data.abilityPool[i];

                // Check if unlocked
                bool unlocked = Arena.ProgressionManager.Instance == null ||
                                Arena.ProgressionManager.Instance.IsAbilityUnlocked(i);

                var btnObj = Instantiate(abilityButtonPrefab, abilityPoolContainer);
                var btn    = btnObj.GetComponent<Button>();
                var icon   = btnObj.GetComponentInChildren<Image>();
                var text   = btnObj.GetComponentInChildren<Text>();

                if (icon != null && ability.icon != null)
                    icon.sprite = ability.icon;
                if (text != null)
                    text.text = ability.abilityName;

                if (!unlocked)
                {
                    if (icon != null) icon.color = new Color(0.3f, 0.3f, 0.3f);
                    if (btn  != null) btn.interactable = false;
                }
                else
                {
                    int capturedIndex = i;
                    if (btn != null)
                    {
                        btn.onClick.AddListener(() => OnAbilityPoolClick(capturedIndex));
                    }
                }
            }
        }

        private void OnAbilityPoolClick(int poolIndex)
        {
            if (_characterData == null) return;
            if (poolIndex < 0 || poolIndex >= _characterData.abilityPool.Count) return;

            var ability = _characterData.abilityPool[poolIndex];
            ShowPreview(ability);

            // Auto-assign to first empty slot, or slot that doesn't have this ability
            for (int s = 0; s < 3; s++)
            {
                if (_selectedAbilities[s] == null || _selectedAbilities[s] == ability)
                {
                    _selectedAbilities[s] = ability;
                    GetSlotUI(s)?.SetAbility(ability);
                    break;
                }
            }
        }

        // --------------------------------------------------
        //  Slot interaction
        // --------------------------------------------------
        public void OnSlotClicked(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 3)
            {
                if (_selectedAbilities[slotIndex] != null)
                    ShowPreview(_selectedAbilities[slotIndex]);
            }
        }

        public void ClearSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 3)
            {
                _selectedAbilities[slotIndex] = null;
                GetSlotUI(slotIndex)?.Clear();
            }
        }

        // --------------------------------------------------
        //  Level Selection
        // --------------------------------------------------
        private void SetupLevelList()
        {
            if (levelListContainer == null || levelButtonPrefab == null) return;

            var prog = Arena.ProgressionManager.Instance;
            if (prog == null) return;

            foreach (Transform child in levelListContainer)
                Destroy(child.gameObject);

            foreach (var level in prog.allLevels)
            {
                bool unlocked = prog.IsLevelUnlocked(level.levelNumber);

                var btnObj = Instantiate(levelButtonPrefab, levelListContainer);
                var btn    = btnObj.GetComponent<Button>();
                var text   = btnObj.GetComponentInChildren<Text>();

                if (text != null)
                    text.text = unlocked
                        ? $"{level.levelName}"
                        : $"🔒 {level.levelName}";

                if (btn != null)
                {
                    btn.interactable = unlocked;
                    var capturedLevel = level;
                    btn.onClick.AddListener(() => OnLevelSelected(capturedLevel));
                }
            }
        }

        private void OnLevelSelected(Arena.LevelConfig level)
        {
            _selectedLevel = level;
            Debug.Log($"[LoadoutBuilder] Selected level: {level.levelName}");
        }

        // --------------------------------------------------
        //  Preview
        // --------------------------------------------------
        private void ShowPreview(Core.AbilityData ability)
        {
            if (previewNameText     != null) previewNameText.text     = ability.abilityName;
            if (previewDescText     != null) previewDescText.text     = ability.description;
            if (previewDamageText   != null) previewDamageText.text   = $"Damage: {ability.baseDamage}";
            if (previewCooldownText != null) previewCooldownText.text = $"Cooldown: {ability.cooldown}s";
            if (previewEnergyText   != null) previewEnergyText.text   = $"Energy: {ability.energyCost}";
            if (previewIcon         != null && ability.icon != null)
                previewIcon.sprite = ability.icon;
        }

        private void ClearPreview()
        {
            if (previewNameText     != null) previewNameText.text     = "";
            if (previewDescText     != null) previewDescText.text     = "Select an ability to preview";
            if (previewDamageText   != null) previewDamageText.text   = "";
            if (previewCooldownText != null) previewCooldownText.text = "";
            if (previewEnergyText   != null) previewEnergyText.text   = "";
        }

        // --------------------------------------------------
        //  Confirm / Back
        // --------------------------------------------------
        private void OnConfirm()
        {
            if (_selectedLevel == null)
            {
                Debug.LogWarning("[LoadoutBuilder] No level selected!");
                return;
            }

            // Apply loadout to GameFlowManager's character
            Arena.GameFlowManager.Instance?.OnLoadoutConfirmed(_selectedLevel);
        }

        private void OnBack()
        {
            Arena.GameFlowManager.Instance?.ReturnToMainMenu();
        }

        // --------------------------------------------------
        //  Helpers
        // --------------------------------------------------
        private LoadoutSlotUI GetSlotUI(int index)
        {
            switch (index)
            {
                case 0: return slot1UI;
                case 1: return slot2UI;
                case 2: return slot3UI;
                default: return null;
            }
        }
    }

    // --------------------------------------------------
    //  Simple slot UI component
    // --------------------------------------------------
    [System.Serializable]
    public class LoadoutSlotUI : MonoBehaviour
    {
        public Image slotIcon;
        public Text  slotName;
        public Text  keyBindText;

        public void SetAbility(Core.AbilityData ability)
        {
            if (ability == null) { Clear(); return; }

            if (slotIcon != null && ability.icon != null)
            {
                slotIcon.sprite  = ability.icon;
                slotIcon.enabled = true;
            }
            if (slotName != null)
                slotName.text = ability.abilityName;
        }

        public void Clear()
        {
            if (slotIcon != null) slotIcon.enabled = false;
            if (slotName != null) slotName.text     = "Empty";
        }
    }
}
