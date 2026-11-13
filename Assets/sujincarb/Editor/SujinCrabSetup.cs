using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Import settings for the Blender crab + a menu item that drops it into the open scene.
public class SujinCrabSetup : AssetPostprocessor
{
    const string Folder = "Assets/sujincarb";
    const string FbxPath = Folder + "/Crab_SideWalk.fbx";
    const string ControllerPath = Folder + "/Crab_SideWalk.controller";
    const string ClipName = "Crab_SideWalk";

    void OnPreprocessModel()
    {
        if (assetPath != FbxPath) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
    }

    void OnPreprocessAnimation()
    {
        if (assetPath != FbxPath) return;
        var importer = (ModelImporter)assetImporter;
        var clips = importer.defaultClipAnimations;
        if (clips.Length == 0) return;
        var clip = clips[0];
        clip.name = ClipName;
        clip.loopTime = true;
        importer.clipAnimations = new[] { clip };
    }

    [MenuItem("Tools/Sujin Crab/Add Crab To Scene")]
    static void AddCrabToScene()
    {
        // The FBX may have been imported before this script compiled; make sure our settings are on it.
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer != null && (importer.animationType != ModelImporterAnimationType.Generic
            || importer.clipAnimations.Length == 0 || !importer.clipAnimations[0].loopTime))
            importer.SaveAndReimport();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var clip = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (model == null || clip == null)
        {
            Debug.LogError("Sujin Crab: " + FbxPath + " is missing or has no animation clip.");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(ControllerPath, clip);

        var crab = (GameObject)PrefabUtility.InstantiatePrefab(model);
        crab.name = "SujinCrab";
        var animator = crab.GetComponent<Animator>();
        if (animator == null) animator = crab.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        // Left/right travel lives on the Root bone, so play it as-is instead of as root motion.
        animator.applyRootMotion = false;

        Undo.RegisterCreatedObjectUndo(crab, "Add Sujin Crab");
        Selection.activeGameObject = crab;
        EditorSceneManager.MarkSceneDirty(crab.scene);
    }
}
