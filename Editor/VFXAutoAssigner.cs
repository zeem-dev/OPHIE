using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using OPHIO.Characters;
using OPHIO.Core;

public class VFXAutoAssigner
{
    // Dictionary mapping ability names (asset filenames or abilityName field) to VFX prefab relative paths
    private static readonly Dictionary<string, string> AbilityVFXMap = new Dictionary<string, string>
    {
        // Hawk Abilities
        { "Arc Slash", "Assets/VFX/Hovl Studio/Magic effects pack/Prefabs/Slash effects/Electro slash.prefab" },
        { "Discharge", "Assets/VFX/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Lightning hit.prefab" },
        { "Energy Siphon", "Assets/VFX/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(transform)/Projectile 2 electro.prefab" },
        { "Neural Surge", "Assets/VFX/Hovl Studio/Magic effects pack/Prefabs/Character auras/Lightning aura.prefab" },
        { "Total Overload (Super)", "Assets/VFX/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Lightning strike.prefab" },
        { "Volt Dash", "Assets/VFX/MasterStylizedProjectiles/Prefabs/Common/RingTrail.prefab" },

        // Goon Abilities
        { "Fire Barrage", "Assets/VFX/MasterStylizedProjectiles/Prefabs/Fireball/Fireball.prefab" },
        { "Flame Burst", "Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Fire Ring.prefab" },
        { "Heat Shield", "Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/Variants/CFXR Fire Ring (Shield).prefab" },
        { "Inferno Wave", "Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Fire Breath.prefab" },
        { "Thermal Dash", "Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Fire Trail.prefab" },
        { "Total Ignition (Super)", "Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR Explosion 2 Bigger.prefab" },

        // Gust Abilities
        { "Spore Ball", "Assets/VFX/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(transform)/Projectile 24 green explosion.prefab" },
        { "Clone Spawn", "Assets/VFX/Hovl Studio/Magic effects pack/Prefabs/Portals/Portal green.prefab" },
        { "Teleport Override", "Assets/VFX/Hovl Studio/Magic effects pack/Prefabs/Portals/Portal green.prefab" },
        { "Spore Armor", "Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/Variants/CFXR Electric Surface (Green).prefab" },
        { "Mass Detonation", "Assets/VFX/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Hit 24 green explosion.prefab" },
        { "Distributed Collapse (Super)", "Assets/VFX/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Lightning strike.prefab" },

        // Mac Abilities
        { "Precision Burst", "Assets/VFX/MasterStylizedProjectiles/Prefabs/CyanBlueBullet/CyanBlueBullet.prefab" },
        { "Concussive Blast", "Assets/VFX/Hovl Studio/Magic effects pack/Prefabs/AoE effects/Ground AOE explosion.prefab" },
        { "Energy Redirect", "Assets/VFX/Hovl Studio/Magic circles/Prefabs/Loop version/Magic shield holy loop.prefab" },
        { "Germane Toss", "Assets/VFX/MasterStylizedProjectiles/Prefabs/EnergyExplosion/EnergyExplosionBullet.prefab" },
        { "Full-Body Shockwave", "Assets/VFX/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Energy explosion.prefab" },
        { "Overcharge Release (Super)", "Assets/VFX/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Energy explosion.prefab" },

        // Flex Abilities
        { "Alkaline Blast", "Assets/VFX/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Projectiles(transform)/Projectile 12 slime.prefab" },
        { "Full Plating", "Assets/VFX/Hovl Studio/Magic circles/Prefabs/Loop version/Magic shield holy loop.prefab" },
        { "Metal Slam", "Assets/VFX/Hovl Studio/Magic effects pack/Prefabs/Hits and explosions/Stones hit.prefab" },
        { "Metal Sprint", "Assets/VFX/MasterStylizedProjectiles/Prefabs/Common/RingTrail.prefab" },
        { "Structural Reinforcement", "Assets/VFX/Hovl Studio/Magic circles/Prefabs/Magic shield holy.prefab" },
        { "Total Metalization (Super)", "Assets/VFX/Hovl Studio/Magic circles/Prefabs/Loop version/Magic shield holy loop.prefab" }
    };

    // Dictionary mapping Character prefab path to a dictionary of VFX field name -> VFX prefab path
    private static readonly string[] PlayerPrefabPaths = new string[]
    {
        "Assets/Prefabs/Players/FLEX - Melee Brawler.prefab",
        "Assets/Prefabs/Players/GOON - Fire Attacker.prefab",
        "Assets/Prefabs/Players/GUST - Spore Summoner.prefab",
        "Assets/Prefabs/Players/Hawk - Electric Melee Fighter.prefab",
        "Assets/Prefabs/Players/MAC - Energy Tank.prefab"
    };

    [MenuItem("OPHIO/VFX/Assign Missing VFX")]
    public static void AssignVFX()
    {
        Debug.Log("[VFXAssigner] Starting auto-assignment of VFX assets...");

        // Part 1: AbilityData ScriptableObjects
        string[] abilityGuids = AssetDatabase.FindAssets("t:AbilityData", new[] { "Assets/ScriptableObjects/Abilities Data" });
        foreach (string guid in abilityGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AbilityData data = AssetDatabase.LoadAssetAtPath<AbilityData>(path);
            if (data == null) continue;

            // Use the asset name or abilityName
            string key = data.abilityName.Trim();
            if (data.category == AbilityCategory.Super && !key.EndsWith("(Super)"))
            {
                key = $"{key} (Super)";
            }

            if (AbilityVFXMap.TryGetValue(key, out string vfxPath))
            {
                if (data.vfxPrefab == null)
                {
                    GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);
                    if (vfxPrefab != null)
                    {
                        data.vfxPrefab = vfxPrefab;
                        EditorUtility.SetDirty(data);
                        Debug.Log($"[VFXAssigner] Assigned '{vfxPrefab.name}' to Ability '{data.abilityName}'");
                    }
                    else
                    {
                        Debug.LogError($"[VFXAssigner] Failed to load VFX prefab at '{vfxPath}' for Ability '{data.abilityName}'");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[VFXAssigner] No VFX mapped for Ability: '{key}' (path: {path})");
            }
        }

        // Part 2: Player Prefabs
        foreach (string path in PlayerPrefabPaths)
        {
            if (!File.Exists(Path.GetFullPath(path))) continue;

            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            if (prefab == null) continue;

            bool modified = false;

            if (prefab.name.Contains("GUST"))
            {
                var gust = prefab.GetComponent<GustCharacter>();
                if (gust != null)
                {
                    if (gust.sporeAnchorPrefab == null)
                    {
                        gust.sporeAnchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Hovl Studio/Magic effects pack/Prefabs/Portals/Portal green.prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned sporeAnchorPrefab to Gust");
                    }
                    if (gust.fungalClonePrefab == null)
                    {
                        gust.fungalClonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Hovl Studio/Magic effects pack/Prefabs/Environment/Crystal effect green.prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned fungalClonePrefab to Gust");
                    }
                    if (gust.armorVFX == null)
                    {
                        gust.armorVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/Variants/CFXR Electric Surface (Green).prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned armorVFX to Gust");
                    }
                }
            }
            else if (prefab.name.Contains("GOON"))
            {
                var goon = prefab.GetComponent<GoonCharacter>();
                if (goon != null)
                {
                    if (goon.fireTrailPrefab == null)
                    {
                        goon.fireTrailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Fire Trail.prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned fireTrailPrefab to Goon");
                    }
                    if (goon.heatShieldVFX == null)
                    {
                        goon.heatShieldVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/Variants/CFXR Fire Ring (Shield).prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned heatShieldVFX to Goon");
                    }
                }
            }
            else if (prefab.name.Contains("FLEX"))
            {
                var flex = prefab.GetComponent<FlexCharacter>();
                if (flex != null)
                {
                    if (flex.platingVFX == null)
                    {
                        flex.platingVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Hovl Studio/Magic circles/Prefabs/Magic shield holy.prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned platingVFX to Flex");
                    }
                    if (flex.metalizationVFX == null)
                    {
                        flex.metalizationVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Hovl Studio/Magic circles/Prefabs/Loop version/Magic shield holy loop.prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned metalizationVFX to Flex");
                    }
                }
            }
            else if (prefab.name.Contains("Hawk"))
            {
                var hawk = prefab.GetComponent<HawkCharacter>();
                if (hawk != null)
                {
                    if (hawk.voltTrailPrefab == null)
                    {
                        hawk.voltTrailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/MasterStylizedProjectiles/Prefabs/Common/RingTrail.prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned voltTrailPrefab to Hawk");
                    }
                }
            }
            else if (prefab.name.Contains("MAC"))
            {
                var mac = prefab.GetComponent<MacCharacter>();
                if (mac != null)
                {
                    if (mac.redirectVFX == null)
                    {
                        mac.redirectVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Hovl Studio/Magic circles/Prefabs/Loop version/Magic shield holy loop.prefab");
                        modified = true;
                        Debug.Log($"[VFXAssigner] Assigned redirectVFX to Mac");
                    }
                }
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Debug.Log($"[VFXAssigner] Successfully saved VFX fields to prefab '{prefab.name}'");
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[VFXAssigner] Auto-assignment complete.");
    }

    [InitializeOnLoadMethod]
    public static void AutoAssignOnLoad()
    {
        string key = "VFXAutoAssigned_v2";
        if (EditorPrefs.GetBool(key, false))
        {
            return;
        }

        Debug.Log("[VFXAssigner] Auto-triggering VFX assignment on script compilation...");
        EditorApplication.delayCall += () =>
        {
            AssignVFX();
            EditorPrefs.SetBool(key, true);
        };
    }
}
