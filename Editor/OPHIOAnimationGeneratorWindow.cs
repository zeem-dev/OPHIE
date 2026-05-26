using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class OPHIOAnimationGeneratorWindow : EditorWindow
{
    private string fbxPath = "Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Characters/Invector@V-Bot 2.0/FBX/VBOT_LOD.fbx";
    private string controllerPath = "Assets/Invector-3rdPersonController/Shooter/Animator/Invector@ShooterMelee.controller";
    private string baseAnimationsPath = "Assets/Animations";
    
    private GameObject fbxModel;
    private bool useRootMotion = true;

    // Global overrides
    private bool useGlobalSettings = true;
    private float globalDuration = 1.2f;
    private float globalIntensity = 1.0f;

    // Foldout groups
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>()
    {
        { "Flex", true },
        { "Goon", true },
        { "Gust", true },
        { "Hawk", true },
        { "Mac", true }
    };

    // Ability configuration data struct
    public class AbilityConfig
    {
        public string clipName;
        public string triggerName;
        public string playerFolder;
        public ProceduralAnimationTemplates.TemplateType templateType;
        public bool isSelected = true;
        public float duration = 1.2f;
        public float intensity = 1.0f;

        public AbilityConfig(string name, string trigger, string folder, ProceduralAnimationTemplates.TemplateType type, float dur = 1.2f)
        {
            clipName = name;
            triggerName = trigger;
            playerFolder = folder;
            templateType = type;
            duration = dur;
        }
    }

    private List<AbilityConfig> abilityConfigs = new List<AbilityConfig>();
    private Vector2 scrollPos = Vector2.zero;

    [MenuItem("Window/OPHIO/Animation Generator Tool")]
    public static void OpenWindow()
    {
        var window = GetWindow<OPHIOAnimationGeneratorWindow>("OPHIO Animation Gen");
        window.minSize = new Vector2(500, 650);
        window.Show();
    }

    private void OnEnable()
    {
        InitializeAbilityConfigs();
        AutoFindAssets();
    }

    private void InitializeAbilityConfigs()
    {
        abilityConfigs.Clear();

        // --- Flex ---
        abilityConfigs.Add(new AbilityConfig("Flex_AlkalineBlast", "AlkalineBlast", "Flex", ProceduralAnimationTemplates.TemplateType.ProjectileThrow, 1.0f));
        abilityConfigs.Add(new AbilityConfig("Flex_FullPlating", "FullPlating", "Flex", ProceduralAnimationTemplates.TemplateType.BuffShield, 1.5f));
        abilityConfigs.Add(new AbilityConfig("Flex_MetalSlam", "MetalSlam", "Flex", ProceduralAnimationTemplates.TemplateType.MeleeSlamStrike, 1.4f));
        abilityConfigs.Add(new AbilityConfig("Flex_MetalSprint", "MetalSprint", "Flex", ProceduralAnimationTemplates.TemplateType.DashSprint, 0.8f));
        abilityConfigs.Add(new AbilityConfig("Flex_StructuralReinforcement", "StructuralReinforcement", "Flex", ProceduralAnimationTemplates.TemplateType.BuffShield, 1.5f));
        abilityConfigs.Add(new AbilityConfig("Flex_TotalMetalization", "TotalMetalization", "Flex", ProceduralAnimationTemplates.TemplateType.BuffShield, 2.0f));

        // --- Goon ---
        abilityConfigs.Add(new AbilityConfig("Goon_FireBarrage", "FireBarrage", "Goon", ProceduralAnimationTemplates.TemplateType.ProjectileThrow, 1.2f));
        abilityConfigs.Add(new AbilityConfig("Goon_FlameBurst", "FlameBurst", "Goon", ProceduralAnimationTemplates.TemplateType.MeleeSlamStrike, 1.0f));
        abilityConfigs.Add(new AbilityConfig("Goon_HeatShield", "HeatShield", "Goon", ProceduralAnimationTemplates.TemplateType.BuffShield, 1.5f));
        abilityConfigs.Add(new AbilityConfig("Goon_InfernoWave", "InfernoWave", "Goon", ProceduralAnimationTemplates.TemplateType.CastSummon, 1.3f));
        abilityConfigs.Add(new AbilityConfig("Goon_ThermalDash", "ThermalDash", "Goon", ProceduralAnimationTemplates.TemplateType.DashSprint, 0.6f));
        abilityConfigs.Add(new AbilityConfig("Goon_TotalIgnition", "TotalIgnition", "Goon", ProceduralAnimationTemplates.TemplateType.BuffShield, 2.0f));

        // --- Gust ---
        abilityConfigs.Add(new AbilityConfig("Gust_CloneSpawn", "CloneSpawn", "Gust", ProceduralAnimationTemplates.TemplateType.CastSummon, 1.0f));
        abilityConfigs.Add(new AbilityConfig("Gust_DistributedCollapse", "DistributedCollapse", "Gust", ProceduralAnimationTemplates.TemplateType.CastSummon, 1.8f));
        abilityConfigs.Add(new AbilityConfig("Gust_MassDetonation", "MassDetonation", "Gust", ProceduralAnimationTemplates.TemplateType.CastSummon, 1.2f));
        abilityConfigs.Add(new AbilityConfig("Gust_SporeArmor", "SporeArmor", "Gust", ProceduralAnimationTemplates.TemplateType.BuffShield, 1.5f));
        abilityConfigs.Add(new AbilityConfig("Gust_SporeBall", "SporeBall", "Gust", ProceduralAnimationTemplates.TemplateType.ProjectileThrow, 1.0f));
        abilityConfigs.Add(new AbilityConfig("Gust_TeleportOverride", "TeleportOverride", "Gust", ProceduralAnimationTemplates.TemplateType.CastSummon, 1.0f));

        // --- Hawk ---
        abilityConfigs.Add(new AbilityConfig("Hawk_ArcSlash", "ArcSlash", "Hawk", ProceduralAnimationTemplates.TemplateType.MeleeSlamStrike, 0.9f));
        abilityConfigs.Add(new AbilityConfig("Hawk_Discharge", "Discharge", "Hawk", ProceduralAnimationTemplates.TemplateType.CastSummon, 1.5f));
        abilityConfigs.Add(new AbilityConfig("Hawk_EnergySiphon", "EnergySiphon", "Hawk", ProceduralAnimationTemplates.TemplateType.CastSummon, 1.2f));
        abilityConfigs.Add(new AbilityConfig("Hawk_NeuralSurge", "NeuralSurge", "Hawk", ProceduralAnimationTemplates.TemplateType.CastSummon, 1.2f));
        abilityConfigs.Add(new AbilityConfig("Hawk_TotalOverload", "TotalOverload", "Hawk", ProceduralAnimationTemplates.TemplateType.CastSummon, 2.0f));
        abilityConfigs.Add(new AbilityConfig("Hawk_VoltDash", "VoltDash", "Hawk", ProceduralAnimationTemplates.TemplateType.DashSprint, 0.6f));

        // --- Mac ---
        abilityConfigs.Add(new AbilityConfig("Mac_ConcussiveBlast", "ConcussiveBlast", "Mac", ProceduralAnimationTemplates.TemplateType.MeleeSlamStrike, 1.1f));
        abilityConfigs.Add(new AbilityConfig("Mac_EnergyRedirect", "EnergyRedirect", "Mac", ProceduralAnimationTemplates.TemplateType.BuffShield, 1.5f));
        abilityConfigs.Add(new AbilityConfig("Mac_FullBodyShockwave", "FullBodyShockwave", "Mac", ProceduralAnimationTemplates.TemplateType.MeleeSlamStrike, 1.6f));
        abilityConfigs.Add(new AbilityConfig("Mac_GermaneToss", "GermaneToss", "Mac", ProceduralAnimationTemplates.TemplateType.MeleeSlamStrike, 1.4f));
        abilityConfigs.Add(new AbilityConfig("Mac_OverchargeRelease", "OverchargeRelease", "Mac", ProceduralAnimationTemplates.TemplateType.BuffShield, 1.8f));
        abilityConfigs.Add(new AbilityConfig("Mac_PrecisionBurst", "PrecisionBurst", "Mac", ProceduralAnimationTemplates.TemplateType.ProjectileThrow, 1.0f));
    }

    private void AutoFindAssets()
    {
        // Try finding FBX model
        if (fbxModel == null)
        {
            fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        }
    }

    private void OnGUI()
    {
        // Premium Dark Title Panel
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.3f, 0.7f, 1f) }
        };
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("OPHIO Procedural Animation Generator", titleStyle);
        EditorGUILayout.LabelField("Hero Shooter Edition", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);

        // Core Setup Fields
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Asset Path Setup", EditorStyles.boldLabel);
        
        fbxModel = (GameObject)EditorGUILayout.ObjectField("Humanoid FBX", fbxModel, typeof(GameObject), false);
        if (fbxModel != null)
        {
            fbxPath = AssetDatabase.GetAssetPath(fbxModel);
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign the V-Bot 2.0 FBX model character rig.", MessageType.Warning);
        }

        controllerPath = EditorGUILayout.TextField("Animator Controller Path", controllerPath);
        baseAnimationsPath = EditorGUILayout.TextField("Animations Folder", baseAnimationsPath);
        useRootMotion = EditorGUILayout.Toggle("Generate Root Motion", useRootMotion);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // Settings Toggles
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Generator Controls", EditorStyles.boldLabel);

        useGlobalSettings = EditorGUILayout.Toggle("Use Global Duration & Intensity", useGlobalSettings);
        if (useGlobalSettings)
        {
            globalDuration = EditorGUILayout.Slider("Global Duration (s)", globalDuration, 0.4f, 3.0f);
            globalIntensity = EditorGUILayout.Slider("Global Intensity", globalIntensity, 0.1f, 2.0f);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // Scrollable List of Players and Poses
        EditorGUILayout.LabelField("Character Ability Animations List", EditorStyles.boldLabel);
        
        // Custom list view
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

        string[] groups = { "Flex", "Goon", "Gust", "Hawk", "Mac" };
        foreach (var group in groups)
        {
            bool isExpanded = foldoutStates[group];
            
            // Draw group header with custom button selection options
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            foldoutStates[group] = EditorGUILayout.Foldout(isExpanded, $"{group} Abilities", true);
            
            if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft, GUILayout.Width(70)))
                SetGroupSelection(group, true);
            if (GUILayout.Button("Clear All", EditorStyles.miniButtonRight, GUILayout.Width(70)))
                SetGroupSelection(group, false);
            EditorGUILayout.EndHorizontal();

            if (foldoutStates[group])
            {
                EditorGUI.indentLevel++;
                foreach (var config in abilityConfigs)
                {
                    if (config.playerFolder != group) continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    
                    config.isSelected = EditorGUILayout.ToggleLeft(config.clipName, config.isSelected, EditorStyles.boldLabel, GUILayout.Width(250));
                    EditorGUILayout.LabelField($"[{config.templateType}]", EditorStyles.miniLabel, GUILayout.Width(130));

                    EditorGUILayout.EndHorizontal();

                    if (!useGlobalSettings && config.isSelected)
                    {
                        EditorGUI.indentLevel++;
                        config.duration = EditorGUILayout.Slider("Duration (s)", config.duration, 0.4f, 3.0f);
                        config.intensity = EditorGUILayout.Slider("Intensity", config.intensity, 0.1f, 2.0f);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(10);

        // Control Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Detect Rig Bones", GUILayout.Height(35)))
        {
            ProceduralAnimationGenerator.DetectBonesFromAvatar(fbxModel);
        }

        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("Generate and Update Controller", GUILayout.Height(35)))
        {
            ExecuteGeneration();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(15);
    }

    private void SetGroupSelection(string group, bool select)
    {
        foreach (var config in abilityConfigs)
        {
            if (config.playerFolder == group)
            {
                config.isSelected = select;
            }
        }
    }

    private void ExecuteGeneration()
    {
        if (fbxModel == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign the Humanoid FBX character model first.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(controllerPath) || !File.Exists(controllerPath))
        {
            EditorUtility.DisplayDialog("Error", $"Animator Controller not found at path: {controllerPath}", "OK");
            return;
        }

        string baseFolderFullPath = Path.GetFullPath(baseAnimationsPath);
        if (!Directory.Exists(baseFolderFullPath))
        {
            Directory.CreateDirectory(baseFolderFullPath);
            AssetDatabase.Refresh();
        }

        Debug.Log("[AnimationGenWindow] Starting procedural generation process...");

        var generatedClipsMap = new Dictionary<string, AnimationClip>();

        foreach (var config in abilityConfigs)
        {
            if (!config.isSelected) continue;

            float finalDuration = useGlobalSettings ? globalDuration : config.duration;
            float finalIntensity = useGlobalSettings ? globalIntensity : config.intensity;

            AnimationClip clip = ProceduralAnimationGenerator.GenerateAndSaveClip(
                fbxModel,
                config.clipName,
                config.templateType,
                finalDuration,
                finalIntensity,
                config.playerFolder,
                baseFolderFullPath,
                useRootMotion
            );

            if (clip != null)
            {
                generatedClipsMap[config.triggerName] = clip;
            }
        }

        // Apply generated clips to Animator Controller
        AbilityAnimatorControllerUpdater.UpdateControllerWithAbilities(controllerPath, baseFolderFullPath, generatedClipsMap);

        EditorUtility.DisplayDialog("Success", $"Procedurally generated {generatedClipsMap.Count} animations and updated the shared Animator Controller!", "OK");
    }

    [MenuItem("OPHIO/Generate All Player Ability Animations")]
    public static void GenerateAllAnimationsStatic()
    {
        string fbxPath = "Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Characters/Invector@V-Bot 2.0/FBX/VBOT_LOD.fbx";
        string controllerPath = "Assets/Invector-3rdPersonController/Shooter/Animator/Invector@ShooterMelee.controller";
        string baseAnimationsPath = "Assets/Animations";

        GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxModel == null)
        {
            Debug.LogError($"[AnimationGen] FBX Model not found at path: {fbxPath}");
            return;
        }

        string baseFolderFullPath = Path.GetFullPath(baseAnimationsPath);
        if (!Directory.Exists(baseFolderFullPath))
        {
            Directory.CreateDirectory(baseFolderFullPath);
            AssetDatabase.Refresh();
        }

        // Create a temporary configuration list locally
        var tempWindow = CreateInstance<OPHIOAnimationGeneratorWindow>();
        tempWindow.InitializeAbilityConfigs();

        var generatedClipsMap = new Dictionary<string, AnimationClip>();

        foreach (var config in tempWindow.abilityConfigs)
        {
            AnimationClip clip = ProceduralAnimationGenerator.GenerateAndSaveClip(
                fbxModel,
                config.clipName,
                config.templateType,
                config.duration,
                config.intensity,
                config.playerFolder,
                baseFolderFullPath,
                true // useRootMotion
            );

            if (clip != null)
            {
                generatedClipsMap[config.triggerName] = clip;
            }
        }

        AbilityAnimatorControllerUpdater.UpdateControllerWithAbilities(controllerPath, baseFolderFullPath, generatedClipsMap);
        
        DestroyImmediate(tempWindow);
        
        Debug.Log("[AnimationGen] Successfully generated all 30 ability animations and updated the shared Animator Controller!");
    }

    [InitializeOnLoadMethod]
    public static void AutoGenerateOnLoad()
    {
        string key = "OPHIOPlayerAbilitiesGenerated_v2";
        if (EditorPrefs.GetBool(key, false))
        {
            return;
        }

        Debug.Log("[AutoGenerate] Scheduling automatic generation of all Player Ability Animations...");
        EditorApplication.delayCall += () =>
        {
            GenerateAllAnimationsStatic();
            EditorPrefs.SetBool(key, true);
        };
    }
}
