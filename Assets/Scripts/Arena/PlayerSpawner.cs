using UnityEngine;
using OPHIO.Core;

namespace OPHIO.Arena
{
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Player Prefabs")]
        public GameObject hawkPrefab;
        public GameObject goonPrefab;
        public GameObject macPrefab;
        public GameObject gustPrefab;
        public GameObject flexPrefab;

        [Header("Spawn Settings")]
        public Transform spawnPoint;

        public GameObject SpawnPlayer(string characterName)
        {
            GameObject prefabToSpawn = null;
            
            // Normalize character name
            string nameLower = characterName.ToLower();
            if (nameLower.Contains("hawk")) prefabToSpawn = hawkPrefab;
            else if (nameLower.Contains("goon")) prefabToSpawn = goonPrefab;
            else if (nameLower.Contains("mac")) prefabToSpawn = macPrefab;
            else if (nameLower.Contains("gust")) prefabToSpawn = gustPrefab;
            else if (nameLower.Contains("flex")) prefabToSpawn = flexPrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[PlayerSpawner] No prefab matched for '{characterName}'. Falling back to Hawk.");
                prefabToSpawn = hawkPrefab;
            }

            if (prefabToSpawn == null)
            {
                Debug.LogError("[PlayerSpawner] Hawk prefab (fallback) is also null! Cannot spawn player.");
                return null;
            }

            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GameObject spawnedPlayer = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            spawnedPlayer.name = prefabToSpawn.name; // Keep clean name without "(Clone)"
            
            Debug.Log($"[PlayerSpawner] Successfully spawned player prefab '{spawnedPlayer.name}' at {spawnPos}");

            // Notify MobileAbilityHUD that player is ready
            var executor = spawnedPlayer.GetComponent<Core.AbilityExecutor>();
            if (executor != null)
                OPHIO.UI.MobileAbilityHUD.OnPlayerSpawned?.Invoke(executor);
            else
                Debug.LogWarning("[PlayerSpawner] AbilityExecutor not found on spawned player.");

            // Apply loadout selections from GameFlowManager
            ApplyLoadoutFromGameFlow(spawnedPlayer);
            EnsureGroundingGuard(spawnedPlayer);

            // Bind the newly spawned player as the Invector Camera's target
            var cameraInstance = Invector.vCamera.vThirdPersonCamera.instance;
            if (cameraInstance != null)
            {
                cameraInstance.SetMainTarget(spawnedPlayer.transform);
                Debug.Log("[PlayerSpawner] Registered spawned player as vThirdPersonCamera main target.");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawner] vThirdPersonCamera.instance not found in scene. Camera target not updated.");
            }

            return spawnedPlayer;
        }

        private void EnsureGroundingGuard(GameObject player)
        {
            if (player.GetComponent<Core.PlayerGroundingGuard>() == null)
                player.AddComponent<Core.PlayerGroundingGuard>();
        }

        // --------------------------------------------------
        //  Apply GameFlowManager loadout to spawned player
        // --------------------------------------------------
        private void ApplyLoadoutFromGameFlow(GameObject player)
        {
            var flow    = Arena.GameFlowManager.Instance;
            var loadout = player.GetComponent<Core.AbilityLoadout>();
            if (loadout == null) return;

            bool hasSelections = flow != null &&
                                 (flow.selectedSlot1 != null ||
                                  flow.selectedSlot2 != null ||
                                  flow.selectedSlot3 != null ||
                                  flow.selectedSuper != null);

            if (hasSelections)
            {
                if (flow.selectedSlot1 != null) loadout.SetSlot(0, flow.selectedSlot1);
                if (flow.selectedSlot2 != null) loadout.SetSlot(1, flow.selectedSlot2);
                if (flow.selectedSlot3 != null) loadout.SetSlot(2, flow.selectedSlot3);
                if (flow.selectedSuper != null) loadout.SetSlot(3, flow.selectedSuper);

                // Update Arena HUD button names
                OPHIO.UI.MobileAbilityHUD.OnLoadoutApplied?.Invoke(loadout);

                Debug.Log($"[PlayerSpawner] Loadout from GameFlow: " +
                    $"{flow.selectedSlot1?.abilityName} / {flow.selectedSlot2?.abilityName} / " +
                    $"{flow.selectedSlot3?.abilityName} / {flow.selectedSuper?.abilityName}");
            }
            else if (flow?.selectedCharacter != null)
            {
                // Fallback: character defaults
                loadout.LoadFromCharacterData(flow.selectedCharacter);
                OPHIO.UI.MobileAbilityHUD.OnLoadoutApplied?.Invoke(loadout);
            }
        }
    }
}
