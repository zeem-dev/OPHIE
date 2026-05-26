using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class EnemyAnimatorControllerUpdater : EditorWindow
{
    private const string TargetFolder = "Assets/Animations/Enemies Animator Controllers";

    [MenuItem("OPHIO/Recreate and Update Enemy Animator Controllers")]
    public static void RecreateAndUpdateControllers()
    {
        Debug.Log("[EnemyUpdater] Starting full recreation of Enemy Animator Controllers...");

        string targetFullPath = Path.GetFullPath(TargetFolder);
        if (!Directory.Exists(targetFullPath))
        {
            Directory.CreateDirectory(targetFullPath);
            AssetDatabase.Refresh();
        }

        // Define the 5 target enemies, their sources, and custom setup actions
        RebuildJuggernaut();
        RebuildStalker();
        RebuildSwarmGrunt();
        RebuildExploder();
        RebuildBoss();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Assign controllers to prefabs matching their actual physical mesh rigs
        AssignControllerToPrefab("Assets/Prefabs/Enemies/Swarm Grunt Normal.prefab", "Stalker Controller.controller", "Assets/Models/Enemies/Rake/Mesh/Rake_skin.FBX");
        AssignControllerToPrefab("Assets/Prefabs/Enemies/Stalker Fast.prefab", "Juggernaut Controller.controller", "Assets/Models/Enemies/Monster_Orc (Troll)/Meshes/monster_orc.fbx");
        AssignControllerToPrefab("Assets/Prefabs/Enemies/Juggernaut Heavy.prefab", "Swarm Grunt Controller.controller", "Assets/Models/Enemies/Foe_Creature/Mesh/Foe_creature.fbx");
        AssignControllerToPrefab("Assets/Prefabs/Enemies/Wolf Boss .prefab", "Boss Controller.controller", "Assets/Models/Enemies/Wolf Boss/Mesh/Wolfboss_skin.FBX");
        AssignControllerToPrefab("Assets/Prefabs/Enemies/Exploder Special.prefab", "Exploder Controller.controller", "Assets/Models/Enemies/Foe_Creature/Mesh/Foe_creature.fbx");
        AssignControllerToPrefab("Assets/Prefabs/Enemies/Exploder Special Screamer.prefab", "Exploder Controller.controller", "Assets/Models/Enemies/Foe_Creature/Mesh/Foe_creature.fbx");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[EnemyUpdater] Successfully recreated, configured and assigned all specific Enemy Animator Controllers to prefabs!");
    }

    private static void AssignControllerToPrefab(string prefabPath, string controllerName, string avatarModelPath = null)
    {
        string controllerPath = $"{TargetFolder}/{controllerName}";
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[EnemyUpdater] Failed to load controller at {controllerPath} for prefab {prefabPath}");
            return;
        }

        if (!File.Exists(Path.GetFullPath(prefabPath)))
        {
            Debug.LogWarning($"[EnemyUpdater] Prefab not found at {prefabPath}");
            return;
        }

        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefab != null)
        {
            Animator animator = prefab.GetComponent<Animator>();
            if (animator == null)
            {
                animator = prefab.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;

            if (!string.IsNullOrEmpty(avatarModelPath))
            {
                Avatar avatar = GetAvatarFromModel(avatarModelPath);
                if (avatar != null)
                {
                    animator.avatar = avatar;
                    Debug.Log($"[EnemyUpdater] Successfully assigned Avatar from {avatarModelPath} to {prefabPath}");
                }
                else
                {
                    Debug.LogError($"[EnemyUpdater] Avatar not found at {avatarModelPath} for {prefabPath}");
                }
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);
            Debug.Log($"[EnemyUpdater] Successfully assigned {controllerName} to prefab {prefabPath}");
        }
        else
        {
            Debug.LogError($"[EnemyUpdater] Failed to load prefab contents at {prefabPath}");
        }
    }

    private static Avatar GetAvatarFromModel(string modelPath)
    {
        if (string.IsNullOrEmpty(modelPath)) return null;

        // Ensure model importer setup is correct (CreateFromThisModel) so avatar is generated
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer != null && importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            Debug.Log($"[EnemyUpdater] Model {modelPath} avatarSetup is {importer.avatarSetup}. Changing to CreateFromThisModel and reimporting...");
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        foreach (var asset in assets)
        {
            if (asset is Avatar avatar)
            {
                return avatar;
            }
        }
        return null;
    }

    [InitializeOnLoadMethod]
    public static void AutoUpdateOnLoad()
    {
        string key = "EnemyAnimatorsModelSpecificRebuilt_v9";
        if (EditorPrefs.GetBool(key, false))
        {
            return;
        }

        Debug.Log("[AutoUpdate] Scheduling rebuild of specific Enemy Animator Controllers on editor delayCall...");
        EditorApplication.delayCall += () =>
        {
            RecreateAndUpdateControllers();
            EditorPrefs.SetBool(key, true);
        };
    }

    private static AnimatorController PrepareController(string sourcePath, string targetFileName)
    {
        string targetPath = $"{TargetFolder}/{targetFileName}";
        string sourceFullPath = Path.GetFullPath(sourcePath);
        string targetFullPath = Path.GetFullPath(targetPath);

        // Delete existing controller and meta files first to start perfectly fresh
        if (File.Exists(targetFullPath))
        {
            File.Delete(targetFullPath);
        }
        if (File.Exists(targetFullPath + ".meta"))
        {
            File.Delete(targetFullPath + ".meta");
        }

        try
        {
            File.Copy(sourceFullPath, targetFullPath, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EnemyUpdater] File.Copy failed from {sourceFullPath} to {targetFullPath}: {ex.Message}");
            return null;
        }

        // Force synchronous asset pipeline import
        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
        
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(targetPath);
        if (controller == null)
        {
            Debug.LogError($"[EnemyUpdater] Failed to load copied controller at {targetPath}");
        }
        else
        {
            string expectedName = Path.GetFileNameWithoutExtension(targetFileName);
            if (controller.name != expectedName)
            {
                controller.name = expectedName;
                EditorUtility.SetDirty(controller);
            }
        }
        return controller;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var parameter in controller.parameters)
        {
            if (parameter.name == name) return; // already exists
        }
        controller.AddParameter(name, type);
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        return FindStateRecursive(stateMachine, name);
    }

    private static AnimatorState FindStateRecursive(AnimatorStateMachine stateMachine, string name)
    {
        if (stateMachine == null) return null;

        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return childState.state;
            }
        }

        foreach (var childSM in stateMachine.stateMachines)
        {
            AnimatorState found = FindStateRecursive(childSM.stateMachine, name);
            if (found != null) return found;
        }

        return null;
    }

    private static void LogAllStates(AnimatorStateMachine stateMachine, string controllerName, string prefix = "")
    {
        if (stateMachine == null) return;
        foreach (var childState in stateMachine.states)
        {
            Debug.LogWarning($"[EnemyUpdater Debug] '{controllerName}' has state: '{prefix}{childState.state.name}'");
        }
        foreach (var childSM in stateMachine.stateMachines)
        {
            LogAllStates(childSM.stateMachine, controllerName, prefix + childSM.stateMachine.name + "/");
        }
    }

    private static void ClearTransitions(AnimatorState state)
    {
        if (state != null)
        {
            state.transitions = new AnimatorStateTransition[0];
        }
    }

    private static void ClearAnyStateTransitions(AnimatorStateMachine stateMachine)
    {
        if (stateMachine != null)
        {
            stateMachine.anyStateTransitions = new AnimatorStateTransition[0];
        }
    }

    // --------------------------------------------------------------------------------
    //  1. JUGGERNAUT (Heavy Orc/Troll)
    // --------------------------------------------------------------------------------
    private static void RebuildJuggernaut()
    {
        string source = "Assets/Models/Enemies/Monster_Orc (Troll)/Animator_controller/Monster_orc.controller";
        AnimatorController controller = PrepareController(source, "Juggernaut Controller.controller");
        if (controller == null) return;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Ensure C# AI Parameters
        EnsureParameter(controller, "isMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isStunned", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "GroundSlam", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Charge", AnimatorControllerParameterType.Trigger);

        // Specific states in Monster_orc
        AnimatorState idleState = FindState(stateMachine, "Monster_anim|Idle_1");
        AnimatorState walkState = FindState(stateMachine, "Monster_anim|Walk");
        AnimatorState attackState = FindState(stateMachine, "Monster_anim|Atack");
        AnimatorState chargeState = FindState(stateMachine, "Monster_anim|Atack_2");
        AnimatorState slamState = FindState(stateMachine, "Monster_anim|Atack_3");
        AnimatorState hitState = FindState(stateMachine, "Monster_anim|Get_hit");
        AnimatorState deathState = FindState(stateMachine, "Monster_anim|Death");

        if (idleState == null || walkState == null || attackState == null || chargeState == null || slamState == null || hitState == null || deathState == null)
        {
            Debug.LogError($"[EnemyUpdater] Juggernaut states missing. Re-check state names in original Monster_orc.");
            LogAllStates(stateMachine, "Juggernaut");
            return;
        }

        // Set default state
        stateMachine.defaultState = idleState;

        // Clear existing transitions of key states
        ClearTransitions(idleState);
        ClearTransitions(walkState);
        ClearTransitions(attackState);
        ClearTransitions(chargeState);
        ClearTransitions(slamState);
        ClearTransitions(hitState);
        ClearTransitions(deathState);
        ClearAnyStateTransitions(stateMachine);

        // Idle <-> Walk
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.15f;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.15f;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        // Attacks: Attack Trigger
        var idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.hasExitTime = false;
        idleToAttack.duration = 0.1f;
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var walkToAttack = walkState.AddTransition(attackState);
        walkToAttack.hasExitTime = false;
        walkToAttack.duration = 0.1f;
        walkToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        // Special attacks: Charge & Slam
        var idleToCharge = idleState.AddTransition(chargeState);
        idleToCharge.hasExitTime = false;
        idleToCharge.duration = 0.1f;
        idleToCharge.AddCondition(AnimatorConditionMode.If, 0, "Charge");

        var walkToCharge = walkState.AddTransition(chargeState);
        walkToCharge.hasExitTime = false;
        walkToCharge.duration = 0.1f;
        walkToCharge.AddCondition(AnimatorConditionMode.If, 0, "Charge");

        var idleToSlam = idleState.AddTransition(slamState);
        idleToSlam.hasExitTime = false;
        idleToSlam.duration = 0.1f;
        idleToSlam.AddCondition(AnimatorConditionMode.If, 0, "GroundSlam");

        var walkToSlam = walkState.AddTransition(slamState);
        walkToSlam.hasExitTime = false;
        walkToSlam.duration = 0.1f;
        walkToSlam.AddCondition(AnimatorConditionMode.If, 0, "GroundSlam");

        // Attack Recoveries to Idle
        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.8f;
        attackToIdle.duration = 0.15f;

        var chargeToIdle = chargeState.AddTransition(idleState);
        chargeToIdle.hasExitTime = true;
        chargeToIdle.exitTime = 0.8f;
        chargeToIdle.duration = 0.15f;

        var slamToIdle = slamState.AddTransition(idleState);
        slamToIdle.hasExitTime = true;
        slamToIdle.exitTime = 0.8f;
        slamToIdle.duration = 0.15f;

        // Stun & Hit
        var anyToStun = stateMachine.AddAnyStateTransition(hitState);
        anyToStun.hasExitTime = false;
        anyToStun.duration = 0.1f;
        anyToStun.AddCondition(AnimatorConditionMode.If, 0, "isStunned");

        var stunToIdle = hitState.AddTransition(idleState);
        stunToIdle.hasExitTime = false;
        stunToIdle.duration = 0.15f;
        stunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isStunned");

        // Death
        var anyToDie = stateMachine.AddAnyStateTransition(deathState);
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0.05f;
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

        EditorUtility.SetDirty(controller);
        Debug.Log("[EnemyUpdater] Rebuilt Juggernaut Controller.");
    }

    // --------------------------------------------------------------------------------
    //  2. STALKER (Fast Rake)
    // --------------------------------------------------------------------------------
    private static void RebuildStalker()
    {
        string source = "Assets/Models/Enemies/Rake/Controller/Rake_Controller.controller";
        AnimatorController controller = PrepareController(source, "Stalker Controller.controller");
        if (controller == null) return;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Ensure C# AI Parameters
        EnsureParameter(controller, "isMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isStunned", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);

        // Specific states in Rake_Controller
        AnimatorState idleState = FindState(stateMachine, "idle");
        AnimatorState runState = FindState(stateMachine, "run");
        AnimatorState walkState = FindState(stateMachine, "walk");
        AnimatorState attackState = FindState(stateMachine, "attack1");
        AnimatorState hitState = FindState(stateMachine, "sturn");
        AnimatorState deathState = FindState(stateMachine, "die");

        if (idleState == null || runState == null || attackState == null || hitState == null || deathState == null)
        {
            Debug.LogError($"[EnemyUpdater] Stalker states missing. Re-check state names in original Rake_Controller.");
            LogAllStates(stateMachine, "Stalker");
            return;
        }

        // Set default state
        stateMachine.defaultState = idleState;

        // Clear transitions
        ClearTransitions(idleState);
        ClearTransitions(runState);
        ClearTransitions(walkState);
        ClearTransitions(attackState);
        ClearTransitions(hitState);
        ClearTransitions(deathState);
        ClearAnyStateTransitions(stateMachine);

        // Idle <-> Run (Fast enemy runs during chase)
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.15f;
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.15f;
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        if (walkState != null)
        {
            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.15f;
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");
        }

        // Attacks
        var idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.hasExitTime = false;
        idleToAttack.duration = 0.1f;
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var runToAttack = runState.AddTransition(attackState);
        runToAttack.hasExitTime = false;
        runToAttack.duration = 0.1f;
        runToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.8f;
        attackToIdle.duration = 0.15f;

        // Stun
        var anyToStun = stateMachine.AddAnyStateTransition(hitState);
        anyToStun.hasExitTime = false;
        anyToStun.duration = 0.1f;
        anyToStun.AddCondition(AnimatorConditionMode.If, 0, "isStunned");

        var stunToIdle = hitState.AddTransition(idleState);
        stunToIdle.hasExitTime = false;
        stunToIdle.duration = 0.15f;
        stunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isStunned");

        // Death
        var anyToDie = stateMachine.AddAnyStateTransition(deathState);
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0.05f;
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

        EditorUtility.SetDirty(controller);
        Debug.Log("[EnemyUpdater] Rebuilt Stalker Controller.");
    }

    // --------------------------------------------------------------------------------
    //  3. SWARM GRUNT (Normal Foe Creature)
    // --------------------------------------------------------------------------------
    private static void RebuildSwarmGrunt()
    {
        string source = "Assets/Models/Enemies/Foe_Creature/Animation/Foe_Creature.controller";
        AnimatorController controller = PrepareController(source, "Swarm Grunt Controller.controller");
        if (controller == null) return;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Ensure C# AI Parameters
        EnsureParameter(controller, "isMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isStunned", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);

        // Core states
        AnimatorState idleState = FindState(stateMachine, "idle");
        AnimatorState runState = FindState(stateMachine, "Run");
        AnimatorState attackState = FindState(stateMachine, "Attack");

        if (idleState == null || runState == null || attackState == null)
        {
            Debug.LogError($"[EnemyUpdater] Swarm Grunt base states missing in Foe_Creature.");
            LogAllStates(stateMachine, "SwarmGrunt");
            return;
        }

        // Get Idle animation clip for custom state creation fallback
        AnimationClip idleClip = idleState.motion as AnimationClip;

        // Programmatically inject sturn and die fallback states
        AnimatorState hitState = FindState(stateMachine, "sturn");
        if (hitState == null)
        {
            hitState = stateMachine.AddState("sturn");
            hitState.motion = idleClip;
            hitState.speed = 0.5f; // slower idle speed represents stun reaction
        }

        AnimatorState deathState = FindState(stateMachine, "die");
        if (deathState == null)
        {
            deathState = stateMachine.AddState("die");
            deathState.motion = idleClip;
            deathState.speed = 0.2f; // slower speed for dying entry
        }

        // Set default state
        stateMachine.defaultState = idleState;

        // Clear transitions
        ClearTransitions(idleState);
        ClearTransitions(runState);
        ClearTransitions(attackState);
        ClearTransitions(hitState);
        ClearTransitions(deathState);
        ClearAnyStateTransitions(stateMachine);

        // Idle <-> Run
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.15f;
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.15f;
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        // Attack
        var idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.hasExitTime = false;
        idleToAttack.duration = 0.1f;
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var runToAttack = runState.AddTransition(attackState);
        runToAttack.hasExitTime = false;
        runToAttack.duration = 0.1f;
        runToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.8f;
        attackToIdle.duration = 0.15f;

        // Stun
        var anyToStun = stateMachine.AddAnyStateTransition(hitState);
        anyToStun.hasExitTime = false;
        anyToStun.duration = 0.1f;
        anyToStun.AddCondition(AnimatorConditionMode.If, 0, "isStunned");

        var stunToIdle = hitState.AddTransition(idleState);
        stunToIdle.hasExitTime = false;
        stunToIdle.duration = 0.15f;
        stunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isStunned");

        // Death
        var anyToDie = stateMachine.AddAnyStateTransition(deathState);
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0.05f;
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

        EditorUtility.SetDirty(controller);
        Debug.Log("[EnemyUpdater] Rebuilt Swarm Grunt Controller.");
    }

    // --------------------------------------------------------------------------------
    //  4. EXPLODER (Special Foe Creature)
    // --------------------------------------------------------------------------------
    private static void RebuildExploder()
    {
        string source = "Assets/Models/Enemies/Foe_Creature/Animation/Foe_Creature.controller";
        AnimatorController controller = PrepareController(source, "Exploder Controller.controller");
        if (controller == null) return;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Ensure C# AI Parameters
        EnsureParameter(controller, "isMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isStunned", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Fuse", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Scream", AnimatorControllerParameterType.Trigger);

        // Core states
        AnimatorState idleState = FindState(stateMachine, "idle");
        AnimatorState runState = FindState(stateMachine, "Run");
        AnimatorState attackState = FindState(stateMachine, "Attack");

        if (idleState == null || runState == null || attackState == null)
        {
            Debug.LogError($"[EnemyUpdater] Exploder base states missing in Foe_Creature.");
            LogAllStates(stateMachine, "Exploder");
            return;
        }

        // Get Idle animation clip for custom state creation fallback
        AnimationClip idleClip = idleState.motion as AnimationClip;

        // Programmatically inject sturn and die fallback states
        AnimatorState hitState = FindState(stateMachine, "sturn");
        if (hitState == null)
        {
            hitState = stateMachine.AddState("sturn");
            hitState.motion = idleClip;
            hitState.speed = 0.5f;
        }

        AnimatorState deathState = FindState(stateMachine, "die");
        if (deathState == null)
        {
            deathState = stateMachine.AddState("die");
            deathState.motion = idleClip;
            deathState.speed = 0.2f;
        }

        // Set default state
        stateMachine.defaultState = idleState;

        // Clear transitions
        ClearTransitions(idleState);
        ClearTransitions(runState);
        ClearTransitions(attackState);
        ClearTransitions(hitState);
        ClearTransitions(deathState);
        ClearAnyStateTransitions(stateMachine);

        // Idle <-> Run
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.15f;
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.15f;
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        // Attack
        var idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.hasExitTime = false;
        idleToAttack.duration = 0.1f;
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var runToAttack = runState.AddTransition(attackState);
        runToAttack.hasExitTime = false;
        runToAttack.duration = 0.1f;
        runToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        // Exploder Triggers: Fuse & Scream map to its Attack animation for high impact detonate/screech prep
        var idleToFuse = idleState.AddTransition(attackState);
        idleToFuse.hasExitTime = false;
        idleToFuse.duration = 0.1f;
        idleToFuse.AddCondition(AnimatorConditionMode.If, 0, "Fuse");

        var runToFuse = runState.AddTransition(attackState);
        runToFuse.hasExitTime = false;
        runToFuse.duration = 0.1f;
        runToFuse.AddCondition(AnimatorConditionMode.If, 0, "Fuse");

        var idleToScream = idleState.AddTransition(attackState);
        idleToScream.hasExitTime = false;
        idleToScream.duration = 0.1f;
        idleToScream.AddCondition(AnimatorConditionMode.If, 0, "Scream");

        var runToScream = runState.AddTransition(attackState);
        runToScream.hasExitTime = false;
        runToScream.duration = 0.1f;
        runToScream.AddCondition(AnimatorConditionMode.If, 0, "Scream");

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.8f;
        attackToIdle.duration = 0.15f;

        // Stun
        var anyToStun = stateMachine.AddAnyStateTransition(hitState);
        anyToStun.hasExitTime = false;
        anyToStun.duration = 0.1f;
        anyToStun.AddCondition(AnimatorConditionMode.If, 0, "isStunned");

        var stunToIdle = hitState.AddTransition(idleState);
        stunToIdle.hasExitTime = false;
        stunToIdle.duration = 0.15f;
        stunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isStunned");

        // Death
        var anyToDie = stateMachine.AddAnyStateTransition(deathState);
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0.05f;
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

        EditorUtility.SetDirty(controller);
        Debug.Log("[EnemyUpdater] Rebuilt Exploder Controller.");
    }

    // --------------------------------------------------------------------------------
    //  5. BOSS (Wolf Boss)
    // --------------------------------------------------------------------------------
    private static void RebuildBoss()
    {
        string source = "Assets/Models/Enemies/Wolf Boss/Controller/Wolfboss_Controller.controller";
        AnimatorController controller = PrepareController(source, "Boss Controller.controller");
        if (controller == null) return;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        // Ensure C# AI Parameters
        EnsureParameter(controller, "isMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isStunned", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "SpecialAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Summon", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "PhaseTransition", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "isEnraged", AnimatorControllerParameterType.Bool);

        // Core states in Wolfboss_Controller
        AnimatorState idleState = FindState(stateMachine, "idle");
        AnimatorState walkState = FindState(stateMachine, "walk");
        AnimatorState runState = FindState(stateMachine, "run");
        AnimatorState attack1State = FindState(stateMachine, "attack1");
        AnimatorState attack2State = FindState(stateMachine, "attack2");
        AnimatorState attack3State = FindState(stateMachine, "attack3");
        AnimatorState hitState = FindState(stateMachine, "sturn");
        AnimatorState deathState = FindState(stateMachine, "die");

        if (idleState == null || walkState == null || runState == null || attack1State == null || attack2State == null || attack3State == null || hitState == null || deathState == null)
        {
            Debug.LogError($"[EnemyUpdater] Boss states missing in Wolfboss_Controller.");
            LogAllStates(stateMachine, "Boss");
            return;
        }

        // Set default state
        stateMachine.defaultState = idleState;

        // Clear transitions
        ClearTransitions(idleState);
        ClearTransitions(walkState);
        ClearTransitions(runState);
        ClearTransitions(attack1State);
        ClearTransitions(attack2State);
        ClearTransitions(attack3State);
        ClearTransitions(hitState);
        ClearTransitions(deathState);
        ClearAnyStateTransitions(stateMachine);

        // Locomotion
        // Idle -> Walk (moving && !enraged)
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.15f;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");
        idleToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "isEnraged");

        // Idle -> Run (moving && enraged)
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.15f;
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "isMoving");
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "isEnraged");

        // Walk -> Idle
        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.15f;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        // Run -> Idle
        var runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.15f;
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        // Walk -> Run (enraged)
        var walkToRun = walkState.AddTransition(runState);
        walkToRun.hasExitTime = false;
        walkToRun.duration = 0.15f;
        walkToRun.AddCondition(AnimatorConditionMode.If, 0, "isEnraged");

        // Run -> Walk (!enraged)
        var runToWalk = runState.AddTransition(walkState);
        runToWalk.hasExitTime = false;
        runToWalk.duration = 0.15f;
        runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "isEnraged");


        // Attack 1: Standard Attack
        var idleToAttack1 = idleState.AddTransition(attack1State);
        idleToAttack1.hasExitTime = false;
        idleToAttack1.duration = 0.1f;
        idleToAttack1.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var walkToAttack1 = walkState.AddTransition(attack1State);
        walkToAttack1.hasExitTime = false;
        walkToAttack1.duration = 0.1f;
        walkToAttack1.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var runToAttack1 = runState.AddTransition(attack1State);
        runToAttack1.hasExitTime = false;
        runToAttack1.duration = 0.1f;
        runToAttack1.AddCondition(AnimatorConditionMode.If, 0, "Attack");


        // Attack 2: Summon
        var idleToSummon = idleState.AddTransition(attack2State);
        idleToSummon.hasExitTime = false;
        idleToSummon.duration = 0.1f;
        idleToSummon.AddCondition(AnimatorConditionMode.If, 0, "Summon");

        var walkToSummon = walkState.AddTransition(attack2State);
        walkToSummon.hasExitTime = false;
        walkToSummon.duration = 0.1f;
        walkToSummon.AddCondition(AnimatorConditionMode.If, 0, "Summon");

        var runToSummon = runState.AddTransition(attack2State);
        runToSummon.hasExitTime = false;
        runToSummon.duration = 0.1f;
        runToSummon.AddCondition(AnimatorConditionMode.If, 0, "Summon");


        // Attack 3: Special Attack
        var idleToSpecial = idleState.AddTransition(attack3State);
        idleToSpecial.hasExitTime = false;
        idleToSpecial.duration = 0.1f;
        idleToSpecial.AddCondition(AnimatorConditionMode.If, 0, "SpecialAttack");

        var walkToSpecial = walkState.AddTransition(attack3State);
        walkToSpecial.hasExitTime = false;
        walkToSpecial.duration = 0.1f;
        walkToSpecial.AddCondition(AnimatorConditionMode.If, 0, "SpecialAttack");

        var runToSpecial = runState.AddTransition(attack3State);
        runToSpecial.hasExitTime = false;
        runToSpecial.duration = 0.1f;
        runToSpecial.AddCondition(AnimatorConditionMode.If, 0, "SpecialAttack");


        // Recoveries to Idle
        var attack1ToIdle = attack1State.AddTransition(idleState);
        attack1ToIdle.hasExitTime = true;
        attack1ToIdle.exitTime = 0.8f;
        attack1ToIdle.duration = 0.15f;

        var attack2ToIdle = attack2State.AddTransition(idleState);
        attack2ToIdle.hasExitTime = true;
        attack2ToIdle.exitTime = 0.8f;
        attack2ToIdle.duration = 0.15f;

        var attack3ToIdle = attack3State.AddTransition(idleState);
        attack3ToIdle.hasExitTime = true;
        attack3ToIdle.exitTime = 0.8f;
        attack3ToIdle.duration = 0.15f;


        // Stun (isStunned || PhaseTransition)
        var anyToStun = stateMachine.AddAnyStateTransition(hitState);
        anyToStun.hasExitTime = false;
        anyToStun.duration = 0.1f;
        anyToStun.AddCondition(AnimatorConditionMode.If, 0, "isStunned");

        var anyToPhase = stateMachine.AddAnyStateTransition(hitState);
        anyToPhase.hasExitTime = false;
        anyToPhase.duration = 0.1f;
        anyToPhase.AddCondition(AnimatorConditionMode.If, 0, "PhaseTransition");

        var stunToIdle = hitState.AddTransition(idleState);
        stunToIdle.hasExitTime = false;
        stunToIdle.duration = 0.15f;
        stunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isStunned");

        // Death
        var anyToDie = stateMachine.AddAnyStateTransition(deathState);
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0.05f;
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

        EditorUtility.SetDirty(controller);
        Debug.Log("[EnemyUpdater] Rebuilt Boss Controller.");
    }
}
