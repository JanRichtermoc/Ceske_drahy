using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reproducible import settings for the animated characters:
/// Resources/Models/Characters/* are Humanoid models (avatar from their T-pose; the game replaces their materials
/// with Railway/Surface at runtime), Animation/UAL is the Quaternius Universal Animation Library imported as Humanoid
/// clips so one clip set drives every character.
/// </summary>
public sealed class RailModelImport : AssetPostprocessor
{
    const string Characters = "Assets/Resources/Models/Characters/";
    const string Ual = "Assets/Animation/UAL/";
    const string Animals = "Assets/Resources/Models/Animals/";

    /// <summary>Clip name → (take, loop). Root motion stays on the bones; the game moves the character itself.</summary>
    public static readonly Dictionary<string, (string take, bool loop)> UalClips = new Dictionary<string, (string, bool)>
    {
        { "Idle", ("Idle_Loop", true) },
        { "IdleTalk", ("Idle_Talking_Loop", true) },
        { "Walk", ("Walk_Loop", true) },
        { "Jog", ("Jog_Fwd_Loop", true) },
        { "Sprint", ("Sprint_Loop", true) },
        { "JumpStart", ("Jump_Start", false) },
        { "JumpLoop", ("Jump_Loop", true) },
        { "JumpLand", ("Jump_Land", false) },
        { "HitChest", ("Hit_Chest", false) },
        { "HitHead", ("Hit_Head", false) },
        { "Death", ("Death01", false) },
        { "Roll", ("Roll", false) },
        { "Dance", ("Dance_Loop", true) },
        { "Punch", ("Punch_Cross", false) },
        { "Cast", ("Spell_Simple_Shoot", false) },
        { "Interact", ("Interact", false) },
        { "SitIdle", ("Sitting_Idle_Loop", true) },
    };

    void OnPreprocessModel()
    {
        var importer = (ModelImporter)assetImporter;
        if (assetPath.StartsWith(Characters))
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.optimizeGameObjects = false;
            // Materials are imported only for their names (CJ_Body…); the game swaps in Railway/Surface at runtime.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.skinWeights = ModelImporterSkinWeights.Standard;
            importer.importCameras = importer.importLights = false;
            importer.isReadable = false;
        }
        else if (assetPath.StartsWith(Animals))
        {
            // Birds play their own baked clip through the legacy Animation component (no avatar needed).
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.animationWrapMode = WrapMode.Loop;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importCameras = importer.importLights = false;
        }
        else if (assetPath.StartsWith(Ual))
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = importer.importLights = false;
            importer.resampleCurves = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        }
    }

    /// <summary>
    /// The body clips must not drive the face: removes the auto-mapped Jaw and Eye bones from every character avatar.
    /// The mapping is stored in the model's .meta, so this only needs to run again after replacing a model.
    /// </summary>
    [MenuItem("SuperCigos/Configure character avatars")]
    public static void ConfigureAvatars()
    {
        foreach (string path in AssetDatabase.FindAssets("t:Model", new[] { Characters.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var description = importer.humanDescription;
            description.human = description.human.Where(h => h.humanName != "Jaw" && h.humanName != "LeftEye" && h.humanName != "RightEye").ToArray();
            importer.humanDescription = description;
            importer.SaveAndReimport();
            Debug.Log("Avatar configured: " + path + " (" + description.human.Length + " bones)");
        }
    }

    /// <summary>The clip list needs the takes, which exist only after a first import: menu to (re)apply it.</summary>
    [MenuItem("SuperCigos/Configure animation clips")]
    public static void ConfigureClips()
    {
        foreach (string path in AssetDatabase.FindAssets("t:Model", new[] { Ual.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var takes = importer.importedTakeInfos;
            var clips = new List<ModelImporterClipAnimation>();
            foreach (var entry in UalClips)
            {
                var take = takes.FirstOrDefault(t => t.name.EndsWith("|" + entry.Value.take) || t.name == entry.Value.take);
                if (take.name == null) { Debug.LogWarning("UAL take missing: " + entry.Value.take); continue; }
                clips.Add(new ModelImporterClipAnimation
                {
                    name = entry.Key,
                    takeName = take.name,
                    firstFrame = take.startTime * take.sampleRate,
                    lastFrame = take.stopTime * take.sampleRate,
                    loopTime = entry.Value.loop,
                    loopPose = entry.Value.loop,
                    lockRootRotation = true,
                    lockRootHeightY = true,
                    lockRootPositionXZ = true,
                    keepOriginalOrientation = true,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = true,
                    heightFromFeet = false,
                });
            }
            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();
            Debug.Log("UAL clips configured: " + clips.Count + " in " + path);
        }
    }
}
