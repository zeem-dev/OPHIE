using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class ProceduralAnimationGenerator
{
    public static AnimationClip GenerateAndSaveClip(
        GameObject characterFBX, 
        string clipName, 
        ProceduralAnimationTemplates.TemplateType templateType, 
        float duration, 
        float intensity, 
        string playerFolderName,
        string baseFolder,
        bool useRootMotion)
    {
        // 1. Detect humanoid bones automatically from Animator avatar
        DetectBonesFromAvatar(characterFBX);

        // 2. Ensure target folders exist
        string targetDirectory = Path.Combine(baseFolder, playerFolderName);
        string relativeDirectory = Path.Combine("Assets/Animations", playerFolderName).Replace("\\", "/");
        
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
            AssetDatabase.Refresh();
        }

        string assetPath = Path.Combine(relativeDirectory, $"{clipName}.anim").Replace("\\", "/");

        // 3. Create or load the AnimationClip
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        bool isNew = false;
        if (clip == null)
        {
            clip = new AnimationClip();
            isNew = true;
        }

        // 4. Generate curves using templates
        ProceduralAnimationTemplates.ApplyTemplate(clip, clipName, templateType, duration, intensity, useRootMotion);

        // Ensure clip name is properly set
        clip.name = clipName;

        // 5. Save the asset
        if (isNew)
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            Debug.Log($"[ProceduralGenerator] Created new animation clip: {assetPath}");
        }
        else
        {
            EditorUtility.SetDirty(clip);
            Debug.Log($"[ProceduralGenerator] Updated existing animation clip: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        return clip;
    }

    public static Dictionary<HumanBodyBones, Transform> DetectBonesFromAvatar(GameObject characterFBX)
    {
        var boneMap = new Dictionary<HumanBodyBones, Transform>();
        if (characterFBX == null)
        {
            Debug.LogWarning("[ProceduralGenerator] No character FBX provided for bone detection.");
            return boneMap;
        }

        // Try to instantiate temporarily in the editor to read Animator bone transforms
        GameObject tempInstance = Object.Instantiate(characterFBX);
        tempInstance.hideFlags = HideFlags.HideAndDontSave;

        Animator animator = tempInstance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = tempInstance.AddComponent<Animator>();
        }

        // Ensure we load the Avatar asset if possible from the same FBX or prefab
        if (animator.avatar == null)
        {
            // Search in assets
            string fbxPath = AssetDatabase.GetAssetPath(characterFBX);
            if (!string.IsNullOrEmpty(fbxPath))
            {
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                foreach (var asset in subAssets)
                {
                    if (asset is Avatar avatar)
                    {
                        animator.avatar = avatar;
                        break;
                    }
                }
            }
        }

        if (animator.avatar != null && animator.isHuman)
        {
            Debug.Log($"[ProceduralGenerator] Successfully detected humanoid avatar '{animator.avatar.name}' on FBX model '{characterFBX.name}'. Detecting bones...");
            int detectedCount = 0;
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;
                
                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform != null)
                {
                    boneMap[bone] = boneTransform;
                    detectedCount++;
                }
            }
            Debug.Log($"[ProceduralGenerator] Automatically detected {detectedCount} humanoid bones in Avatar configuration.");
        }
        else
        {
            Debug.LogWarning($"[ProceduralGenerator] FBX model '{characterFBX.name}' does not have a valid humanoid Avatar or is not set up as a Humanoid rig.");
        }

        // Clean up temporary instance
        Object.DestroyImmediate(tempInstance);

        return boneMap;
    }

    /// <summary>
    /// Generates a blank, curve-less clip to use as the default state in the override animator controller layer.
    /// This allows the lower locomotion/combat layers to pass through without interference when no ability is playing.
    /// </summary>
    public static AnimationClip GenerateBlankClip(string baseFolder)
    {
        string relativePath = "Assets/Animations/EmptyAbilityPose.anim";
        string fullPath = Path.GetFullPath(relativePath);
        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
        if (clip == null)
        {
            clip = new AnimationClip();
            clip.name = "EmptyAbilityPose";
            clip.ClearCurves();
            // Setup very simple dummy keyframe with 0 muscle curve so it's not totally empty (some versions of Unity prefer a curve)
            // But keep it completely neutral so it does not affect any humanoid bones
            AssetDatabase.CreateAsset(clip, relativePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ProceduralGenerator] Created EmptyAbilityPose clip at {relativePath}");
        }
        return clip;
    }
}
