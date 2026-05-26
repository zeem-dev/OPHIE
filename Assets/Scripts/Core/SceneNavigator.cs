using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

namespace OPHIO.Core
{
    public class SceneNavigator : MonoBehaviour
    {
        public static string SelectedCharacter = "Hawk";
        public static string SelectedActive1 = "Arc Slash";
        public static string SelectedActive2 = "Discharge";
        public static string SelectedActive3 = "Neural Surge";
        public static string SelectedSuper = "Total Overload";
        public static bool IsVictory = true;

        [Header("Loadout UI Text Fallbacks")]
        public List<CharacterData> availableCharacters = new List<CharacterData>();
        public Text selectedCharacterText;
        public Text active1Text;
        public Text active2Text;
        public Text active3Text;
        public Text superText;

        [Header("Win/Lose UI Text Fallbacks")]
        public Text resultText;

        private void Start()
        {
            SyncStateFromGameFlow();
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (selectedCharacterText != null)
                selectedCharacterText.text = $"SELECTED: {SelectedCharacter.ToUpper()}";
            if (active1Text != null)
                active1Text.text = $"SLOT 1: {SelectedActive1.ToUpper()}";
            if (active2Text != null)
                active2Text.text = $"SLOT 2: {SelectedActive2.ToUpper()}";
            if (active3Text != null)
                active3Text.text = $"SLOT 3: {SelectedActive3.ToUpper()}";
            if (superText != null)
                superText.text = $"SUPER: {SelectedSuper.ToUpper()}";

            if (resultText != null)
            {
                resultText.text = IsVictory ? "VICTORY ACHIEVED" : "MISSION FAILED";
                resultText.color = IsVictory ? new Color(0.95f, 0.7f, 0.1f, 1f) : new Color(0.9f, 0.2f, 0.2f, 1f);
            }
        }

        // Navigation Actions
        public void LoadMainMenu()
        {
            Debug.Log("[Navigator] Loading Main Menu Scene...");
            SceneManager.LoadScene("MainMenu");
        }

        public void LoadCharacterSelect()
        {
            Debug.Log("[Navigator] Loading Character Select Scene...");
            SceneManager.LoadScene("CharacterSelect");
        }

        public void SelectCharacter(string characterName)
        {
            SelectedCharacter = characterName;
            Debug.Log($"[Navigator] Selected Character: {characterName}");
            SyncSelectedCharacterToGameFlow(characterName);
            
            // Set default loadouts based on character selection
            if (characterName == "Hawk")
            {
                SelectedActive1 = "Arc Slash";
                SelectedActive2 = "Discharge";
                SelectedActive3 = "Neural Surge";
                SelectedSuper = "Total Overload";
            }
            else if (characterName == "Goon")
            {
                SelectedActive1 = "Flame Burst";
                SelectedActive2 = "Inferno Wave";
                SelectedActive3 = "Thermal Dash";
                SelectedSuper = "Total Ignition";
            }
            else if (characterName == "Mac")
            {
                SelectedActive1 = "Germane Toss";
                SelectedActive2 = "Concussive Blast";
                SelectedActive3 = "Energy Redirect";
                SelectedSuper = "Overcharge Release";
            }
            else if (characterName == "Gust")
            {
                SelectedActive1 = "Spore Ball";
                SelectedActive2 = "Clone Spawn";
                SelectedActive3 = "Teleport Override";
                SelectedSuper = "Distributed Collapse";
            }
            else if (characterName == "Flex")
            {
                SelectedActive1 = "Metal Slam";
                SelectedActive2 = "Alkaline Blast";
                SelectedActive3 = "Full Plating";
                SelectedSuper = "Total Metalization";
            }

            UpdateUI();
        }

        private void SyncSelectedCharacterToGameFlow(string characterName)
        {
            if (Arena.GameFlowManager.Instance == null)
                return;

            CharacterData data = FindCharacterData(characterName);
            if (data == null)
            {
                Debug.LogWarning($"[Navigator] No CharacterData assigned for '{characterName}'. LoadoutBuilder will have no ability pool.");
                return;
            }

            Arena.GameFlowManager.Instance.selectedCharacter = data;
            Arena.GameFlowManager.Instance.selectedSlot1 = data.abilityPool.Count > 0 ? data.abilityPool[0] : null;
            Arena.GameFlowManager.Instance.selectedSlot2 = data.abilityPool.Count > 1 ? data.abilityPool[1] : null;
            Arena.GameFlowManager.Instance.selectedSlot3 = data.abilityPool.Count > 2 ? data.abilityPool[2] : null;
            Arena.GameFlowManager.Instance.selectedSuper = data.superAbility;
        }

        private void SyncStateFromGameFlow()
        {
            var flow = Arena.GameFlowManager.Instance;
            if (flow == null || flow.selectedCharacter == null)
                return;

            SelectedCharacter = flow.selectedCharacter.characterName;
            SelectedActive1 = flow.selectedSlot1 != null ? flow.selectedSlot1.abilityName : SelectedActive1;
            SelectedActive2 = flow.selectedSlot2 != null ? flow.selectedSlot2.abilityName : SelectedActive2;
            SelectedActive3 = flow.selectedSlot3 != null ? flow.selectedSlot3.abilityName : SelectedActive3;
            SelectedSuper = flow.selectedSuper != null ? flow.selectedSuper.abilityName : SelectedSuper;
        }

        private CharacterData FindCharacterData(string characterName)
        {
            for (int i = 0; i < availableCharacters.Count; i++)
            {
                CharacterData data = availableCharacters[i];
                if (data != null && data.characterName == characterName)
                    return data;
            }

            return null;
        }

        public void LoadLoadoutBuilder()
        {
            Debug.Log("[Navigator] Loading Loadout Builder Scene...");
            SceneManager.LoadScene("LoadoutBuilder");
        }

        public void EquipAbility(string slotAndAbility)
        {
            // Format should be "Slot:AbilityName", e.g. "Active1:Flame Burst"
            string[] parts = slotAndAbility.Split(':');
            if (parts.Length != 2) return;

            string slotName = parts[0];
            string abilityName = parts[1];

            if (slotName == "Active1") SelectedActive1 = abilityName;
            else if (slotName == "Active2") SelectedActive2 = abilityName;
            else if (slotName == "Active3") SelectedActive3 = abilityName;
            else if (slotName == "Super") SelectedSuper = abilityName;
            
            Debug.Log($"[Navigator] Equipped {abilityName} in {slotName}");
            UpdateUI();
        }

        public void LoadArena()
        {
            Debug.Log("[Navigator] Loading Arena Scene...");
            SceneManager.LoadScene("Arena");
        }

        public void TriggerEndGame(bool victory)
        {
            IsVictory = victory;
            Debug.Log($"[Navigator] Ending game with Victory: {victory}");
            SceneManager.LoadScene("EndScreen");
        }

        public void QuitGame()
        {
            Debug.Log("[Navigator] Quitting Game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
