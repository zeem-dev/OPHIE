using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using OPHIO.Characters;
using OPHIO.Core;
using Invector.vCharacterController;

public class PlayerPrefabInspector : EditorWindow
{
    private static readonly string[] PlayerPrefabPaths = new string[]
    {
        "Assets/Prefabs/Players/FLEX - Melee Brawler.prefab",
        "Assets/Prefabs/Players/GOON - Fire Attacker.prefab",
        "Assets/Prefabs/Players/GUST - Spore Summoner.prefab",
        "Assets/Prefabs/Players/Hawk - Electric Melee Fighter.prefab",
        "Assets/Prefabs/Players/MAC - Energy Tank.prefab"
    };

    [MenuItem("OPHIO/Inspect and Fix Player Prefabs")]
    public static void InspectPlayers()
    {
        Debug.Log("[PlayerInspector] Starting audit and repair of Player Prefabs...");

        // Resolve Enemy Layer dynamically from an enemy prefab
        int resolvedEnemyLayer = 12; // fallback
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Swarm Grunt Normal.prefab");
        if (enemyPrefab != null)
        {
            resolvedEnemyLayer = enemyPrefab.layer;
            Debug.Log($"[PlayerInspector] Dynamically resolved enemy layer: {LayerMask.LayerToName(resolvedEnemyLayer)} (Layer {resolvedEnemyLayer})");
        }
        else
        {
            Debug.LogWarning("[PlayerInspector] Could not load Swarm Grunt prefab to resolve enemy layer. Using fallback Layer 12.");
        }

        foreach (string path in PlayerPrefabPaths)
        {
            if (!File.Exists(Path.GetFullPath(path)))
            {
                Debug.LogWarning($"[PlayerInspector] Prefab not found at path: {path}");
                continue;
            }

            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            if (prefab == null)
            {
                Debug.LogError($"[PlayerInspector] Failed to load prefab contents at: {path}");
                continue;
            }

            Debug.Log($"==================================================");
            Debug.Log($"[PlayerInspector] Auditing/Fixing Prefab: {prefab.name}");
            Debug.Log($"==================================================");

            bool needsSave = false;

            // 1. Tag the prefab as "Player"
            if (!prefab.CompareTag("Player"))
            {
                prefab.tag = "Player";
                Debug.Log($"[PlayerInspector] Tagged {prefab.name} as 'Player'");
                needsSave = true;
            }

            // 2. Audit and add Core Components if missing
            var energy = prefab.GetComponent<EnergyManager>();
            if (energy == null)
            {
                Debug.LogWarning($"[PlayerInspector] Adding missing EnergyManager to {prefab.name}");
                energy = prefab.AddComponent<EnergyManager>();
                needsSave = true;
            }

            var loadout = prefab.GetComponent<AbilityLoadout>();
            if (loadout == null)
            {
                Debug.LogWarning($"[PlayerInspector] Adding missing AbilityLoadout to {prefab.name}");
                loadout = prefab.AddComponent<AbilityLoadout>();
                needsSave = true;
            }

            var executor = prefab.GetComponent<AbilityExecutor>();
            if (executor == null)
            {
                Debug.LogWarning($"[PlayerInspector] Adding missing AbilityExecutor to {prefab.name}");
                executor = prefab.AddComponent<AbilityExecutor>();
                needsSave = true;
            }

            // Assign dynamically resolved enemyLayer
            if (executor != null)
            {
                LayerMask targetMask = 1 << resolvedEnemyLayer;
                if (executor.enemyLayer != targetMask)
                {
                    executor.enemyLayer = targetMask;
                    Debug.Log($"[PlayerInspector] Assigned enemyLayer to {LayerMask.LayerToName(resolvedEnemyLayer)} mask on {prefab.name}");
                    needsSave = true;
                }
            }

            // 3. Audit and add Character Specific Script
            System.Type expectedType = GetExpectedCharacterScriptType(prefab.name);
            if (expectedType != null)
            {
                var charScript = prefab.GetComponent(expectedType);
                if (charScript == null)
                {
                    Debug.LogWarning($"[PlayerInspector] Missing character script {expectedType.Name} on {prefab.name}. Adding it...");
                    charScript = prefab.AddComponent(expectedType);
                    needsSave = true;
                }
                else
                {
                    Debug.Log($"[PlayerInspector] Character script {expectedType.Name} is correctly attached.");
                }

                // Verify and log unassigned VFX/prefabs on character scripts
                VerifyCharacterFields(prefab.name, charScript);
            }

            // 4. Inspect & Repair Animator Controller
            var animator = prefab.GetComponent<Animator>();
            if (animator != null)
            {
                if (animator.runtimeAnimatorController != null)
                {
                    var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    if (controller != null)
                    {
                        Debug.Log($"[PlayerInspector] Animator Controller: {controller.name}");
                        VerifyAndFixAnimatorParameters(controller);
                    }
                    else
                    {
                        // Check if it's an AnimatorOverrideController
                        var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                        if (overrideController != null)
                        {
                            Debug.Log($"[PlayerInspector] Animator has Override Controller: {overrideController.name}");
                            var baseController = overrideController.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                            if (baseController != null)
                            {
                                VerifyAndFixAnimatorParameters(baseController);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[PlayerInspector] Animator has a controller, but it is not a direct AnimatorController or OverrideController: {animator.runtimeAnimatorController.name}");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[PlayerInspector] RuntimeAnimatorController is MISSING on Animator of {prefab.name}!");
                }
            }
            else
            {
                Debug.LogError($"[PlayerInspector] Animator Component is MISSING on {prefab.name}!");
            }

            if (needsSave)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Debug.Log($"[PlayerInspector] Successfully saved updates to prefab {prefab.name}");
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }

        Debug.Log("[PlayerInspector] Audit and repair complete.");
    }

    private static System.Type GetExpectedCharacterScriptType(string prefabName)
    {
        if (prefabName.Contains("FLEX")) return typeof(FlexCharacter);
        if (prefabName.Contains("GOON")) return typeof(GoonCharacter);
        if (prefabName.Contains("GUST")) return typeof(GustCharacter);
        if (prefabName.Contains("Hawk")) return typeof(HawkCharacter);
        if (prefabName.Contains("MAC")) return typeof(MacCharacter);
        return null;
    }

    private static void VerifyCharacterFields(string name, Component charScript)
    {
        if (charScript is FlexCharacter flex)
        {
            if (flex.platingVFX == null) Debug.LogWarning($"[PlayerInspector] {name}: 'platingVFX' is unassigned.");
            if (flex.metalizationVFX == null) Debug.LogWarning($"[PlayerInspector] {name}: 'metalizationVFX' is unassigned.");
        }
        else if (charScript is GoonCharacter goon)
        {
            if (goon.fireTrailPrefab == null) Debug.LogWarning($"[PlayerInspector] {name}: 'fireTrailPrefab' is unassigned.");
            if (goon.heatShieldVFX == null) Debug.LogWarning($"[PlayerInspector] {name}: 'heatShieldVFX' is unassigned.");
        }
        else if (charScript is GustCharacter gust)
        {
            if (gust.sporeAnchorPrefab == null) Debug.LogWarning($"[PlayerInspector] {name}: 'sporeAnchorPrefab' is unassigned.");
            if (gust.fungalClonePrefab == null) Debug.LogWarning($"[PlayerInspector] {name}: 'fungalClonePrefab' is unassigned.");
            if (gust.armorVFX == null) Debug.LogWarning($"[PlayerInspector] {name}: 'armorVFX' is unassigned.");
        }
        else if (charScript is HawkCharacter hawk)
        {
            if (hawk.voltTrailPrefab == null) Debug.LogWarning($"[PlayerInspector] {name}: 'voltTrailPrefab' is unassigned.");
        }
        else if (charScript is MacCharacter mac)
        {
            if (mac.redirectVFX == null) Debug.LogWarning($"[PlayerInspector] {name}: 'redirectVFX' is unassigned.");
        }
    }

    private static void VerifyAndFixAnimatorParameters(UnityEditor.Animations.AnimatorController controller)
    {
        // Define all required parameters and their types
        var requiredParams = new Dictionary<string, AnimatorControllerParameterType>
        {
            { "isMoving", AnimatorControllerParameterType.Bool },
            { "isMovingRaw", AnimatorControllerParameterType.Bool },
            { "isSprinting", AnimatorControllerParameterType.Bool },
            { "isGrounded", AnimatorControllerParameterType.Bool },
            { "isStunned", AnimatorControllerParameterType.Bool },
            { "isEnraged", AnimatorControllerParameterType.Bool },
            { "Attack", AnimatorControllerParameterType.Trigger },
            { "SpecialAttack", AnimatorControllerParameterType.Trigger },
            { "StrongAttack", AnimatorControllerParameterType.Trigger },
            { "Die", AnimatorControllerParameterType.Trigger },

            // Flex Abilities
            { "AlkalineBlast", AnimatorControllerParameterType.Trigger },
            { "FullPlating", AnimatorControllerParameterType.Trigger },
            { "MetalSlam", AnimatorControllerParameterType.Trigger },
            { "MetalSprint", AnimatorControllerParameterType.Trigger },
            { "StructuralReinforcement", AnimatorControllerParameterType.Trigger },
            { "TotalMetalization", AnimatorControllerParameterType.Trigger },

            // Goon Abilities
            { "FireBarrage", AnimatorControllerParameterType.Trigger },
            { "FlameBurst", AnimatorControllerParameterType.Trigger },
            { "HeatShield", AnimatorControllerParameterType.Trigger },
            { "InfernoWave", AnimatorControllerParameterType.Trigger },
            { "ThermalDash", AnimatorControllerParameterType.Trigger },
            { "TotalIgnition", AnimatorControllerParameterType.Trigger },

            // Gust Abilities
            { "CloneSpawn", AnimatorControllerParameterType.Trigger },
            { "DistributedCollapse", AnimatorControllerParameterType.Trigger },
            { "MassDetonation", AnimatorControllerParameterType.Trigger },
            { "SporeArmor", AnimatorControllerParameterType.Trigger },
            { "SporeBall", AnimatorControllerParameterType.Trigger },
            { "TeleportOverride", AnimatorControllerParameterType.Trigger },

            // Hawk Abilities
            { "ArcSlash", AnimatorControllerParameterType.Trigger },
            { "Discharge", AnimatorControllerParameterType.Trigger },
            { "EnergySiphon", AnimatorControllerParameterType.Trigger },
            { "NeuralSurge", AnimatorControllerParameterType.Trigger },
            { "TotalOverload", AnimatorControllerParameterType.Trigger },
            { "VoltDash", AnimatorControllerParameterType.Trigger },

            // Mac Abilities
            { "ConcussiveBlast", AnimatorControllerParameterType.Trigger },
            { "EnergyRedirect", AnimatorControllerParameterType.Trigger },
            { "FullBodyShockwave", AnimatorControllerParameterType.Trigger },
            { "GermaneToss", AnimatorControllerParameterType.Trigger },
            { "OverchargeRelease", AnimatorControllerParameterType.Trigger },
            { "PrecisionBurst", AnimatorControllerParameterType.Trigger }
        };

        HashSet<string> existing = new HashSet<string>();
        foreach (var p in controller.parameters)
        {
            existing.Add(p.name);
        }

        bool controllerModified = false;
        foreach (var pair in requiredParams)
        {
            if (!existing.Contains(pair.Key))
            {
                controller.AddParameter(pair.Key, pair.Value);
                Debug.Log($"[PlayerInspector] Added missing parameter '{pair.Key}' ({pair.Value}) to Animator Controller '{controller.name}'");
                controllerModified = true;
            }
        }

        if (controllerModified)
        {
            EditorUtility.SetDirty(controller);
        }
    }

    [InitializeOnLoadMethod]
    public static void AutoInspectOnLoad()
    {
        string key = "PlayerPrefabsAudited_v3";
        if (EditorPrefs.GetBool(key, false))
        {
            return;
        }

        Debug.Log("[PlayerInspector] Auto-triggering player prefabs audit and repair on script compilation...");
        EditorApplication.delayCall += () =>
        {
            InspectPlayers();
            EditorPrefs.SetBool(key, true);
        };
    }
}
