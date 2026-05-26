using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public static class AbilityAnimatorControllerUpdater
{
    public static void UpdateControllerWithAbilities(
        string controllerPath, 
        string baseFolder, 
        System.Collections.Generic.Dictionary<string, AnimationClip> generatedClips)
    {
        if (string.IsNullOrEmpty(controllerPath) || !File.Exists(controllerPath))
        {
            Debug.LogError($"[ControllerUpdater] Animator Controller not found at path: {controllerPath}");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[ControllerUpdater] Failed to load Animator Controller at: {controllerPath}");
            return;
        }

        // 1. Get or create the Blank EmptyAbilityPose clip
        AnimationClip blankClip = ProceduralAnimationGenerator.GenerateBlankClip(baseFolder);

        // 2. Find or create the "Abilities" layer
        AnimatorControllerLayer abilitiesLayer = null;
        int layerIndex = -1;
        
        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name.Equals("Abilities", System.StringComparison.OrdinalIgnoreCase))
            {
                abilitiesLayer = controller.layers[i];
                layerIndex = i;
                break;
            }
        }

        if (abilitiesLayer == null)
        {
            controller.AddLayer("Abilities");
            var layers = controller.layers;
            layerIndex = layers.Length - 1;
            abilitiesLayer = layers[layerIndex];
            abilitiesLayer.defaultWeight = 1.0f;
            abilitiesLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;
            Debug.Log("[ControllerUpdater] Created new 'Abilities' Animator Controller layer.");
        }
        else
        {
            // Ensure the properties are correct even if the layer existed
            var layers = controller.layers;
            abilitiesLayer = layers[layerIndex];
            abilitiesLayer.defaultWeight = 1.0f;
            abilitiesLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;
        }

        AnimatorStateMachine stateMachine = controller.layers[layerIndex].stateMachine;

        // 3. Find or create the default "Empty" state in the Abilities layer
        AnimatorState emptyState = null;
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name.Equals("Empty", System.StringComparison.OrdinalIgnoreCase))
            {
                emptyState = childState.state;
                break;
            }
        }

        if (emptyState == null)
        {
            emptyState = stateMachine.AddState("Empty");
            emptyState.motion = blankClip;
            Debug.Log("[ControllerUpdater] Created default 'Empty' state in 'Abilities' layer.");
        }
        else
        {
            emptyState.motion = blankClip;
        }
        
        stateMachine.defaultState = emptyState;

        // 4. Update the controller with parameters and states for each ability
        foreach (var entry in generatedClips)
        {
            string triggerName = entry.Key; // E.g., "AlkalineBlast"
            AnimationClip clip = entry.Value;

            if (clip == null) continue;

            // Ensure the trigger parameter exists
            EnsureTriggerParameter(controller, triggerName);

            // Find or create the ability state
            AnimatorState abilityState = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name.Equals(triggerName, System.StringComparison.OrdinalIgnoreCase))
                {
                    abilityState = childState.state;
                    break;
                }
            }

            if (abilityState == null)
            {
                abilityState = stateMachine.AddState(triggerName);
                Debug.Log($"[ControllerUpdater] Created state '{triggerName}' in 'Abilities' layer.");
            }

            // Assign the generated clip to the state
            abilityState.motion = clip;

            // Configure transitions
            ConfigureTransitionsForAbility(stateMachine, emptyState, abilityState, triggerName);
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ControllerUpdater] Successfully updated animator controller at '{controllerPath}' with all generated clips.");
    }

    private static void EnsureTriggerParameter(AnimatorController controller, string name)
    {
        bool parameterExists = false;
        foreach (var parameter in controller.parameters)
        {
            if (parameter.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    parameterExists = true;
                }
                break;
            }
        }

        if (!parameterExists)
        {
            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
            Debug.Log($"[ControllerUpdater] Added Animator Trigger parameter: '{name}'");
        }
    }

    private static void ConfigureTransitionsForAbility(
        AnimatorStateMachine stateMachine, 
        AnimatorState emptyState, 
        AnimatorState abilityState, 
        string triggerName)
    {
        // 1. Any State -> Ability State (On Trigger)
        AnimatorStateTransition anyStateTrans = null;
        foreach (var trans in stateMachine.anyStateTransitions)
        {
            if (trans.destinationState == abilityState)
            {
                anyStateTrans = trans;
                break;
            }
        }

        if (anyStateTrans == null)
        {
            anyStateTrans = stateMachine.AddAnyStateTransition(abilityState);
        }

        anyStateTrans.duration = 0.1f;
        anyStateTrans.hasExitTime = false;
        anyStateTrans.canTransitionToSelf = false;
        anyStateTrans.interruptionSource = TransitionInterruptionSource.None;
        
        // Reset conditions
        anyStateTrans.conditions = new AnimatorCondition[0];
        anyStateTrans.AddCondition(AnimatorConditionMode.If, 0f, triggerName);

        // 2. Ability State -> Empty State (On Exit Time)
        AnimatorStateTransition backToEmpty = null;
        foreach (var trans in abilityState.transitions)
        {
            if (trans.destinationState == emptyState)
            {
                backToEmpty = trans;
                break;
            }
        }

        if (backToEmpty == null)
        {
            backToEmpty = abilityState.AddTransition(emptyState);
        }

        backToEmpty.duration = 0.15f;
        backToEmpty.hasExitTime = true;
        backToEmpty.exitTime = 0.9f; // Return to Empty near completion of the ability
        backToEmpty.conditions = new AnimatorCondition[0]; // No extra conditions
    }
}
