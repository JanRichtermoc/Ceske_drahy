using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds the animator controllers in Resources/Animation from the UAL clips. The game drives the states itself
/// (cross-fades from CjRig/RailActor), so the controllers hold states and one locomotion blend tree, no transitions.
/// Run "SuperCigos/Build animators" when the clip set changes; the output is committed.
/// </summary>
public static class RailAnimatorBuilder
{
    const string Ual = "Assets/Animation/UAL/UAL1_Standard.fbx";
    const string Folder = "Assets/Resources/Animation";

    static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(Ual).OfType<AnimationClip>().FirstOrDefault(c => c.name == name)
        ?? throw new System.InvalidOperationException("Missing UAL clip " + name + ". Run SuperCigos/Configure animation clips.");

    [MenuItem("SuperCigos/Build animators")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(Folder);
        var speeds = MeasureStrideSpeeds();
        BuildCj(speeds);
        BuildNpc();
        AssetDatabase.SaveAssets();
        Debug.Log("RAILWAY_ANIMATORS_READY walk=" + speeds["Walk"].ToString("0.00") + " jog=" + speeds["Jog"].ToString("0.00") + " sprint=" + speeds["Sprint"].ToString("0.00"));
    }

    static AnimatorController Fresh(string path)
    {
        AssetDatabase.DeleteAsset(path);
        return AnimatorController.CreateAnimatorControllerAtPath(path);
    }

    static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion, Vector3 at, float speed = 1)
    {
        var state = machine.AddState(name, at);
        state.motion = motion;
        state.speed = speed;
        state.writeDefaultValues = true;
        return state;
    }

    static void BuildCj(Dictionary<string, float> speeds)
    {
        var controller = Fresh(Folder + "/CJ.controller");
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("StrideRate", AnimatorControllerParameterType.Float);
        var machine = controller.layers[0].stateMachine;
        var locomotion = new BlendTree { name = "Locomotion", blendParameter = "Speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(locomotion, controller);
        locomotion.AddChild(Clip("Idle"), 0);
        locomotion.AddChild(Clip("Walk"), speeds["Walk"]);
        locomotion.AddChild(Clip("Jog"), speeds["Jog"]);
        locomotion.AddChild(Clip("Sprint"), speeds["Sprint"]);
        var loco = State(machine, "Locomotion", locomotion, new Vector3(300, 0));
        loco.speedParameterActive = true;
        loco.speedParameter = "StrideRate";
        machine.defaultState = loco;
        State(machine, "JumpStart", Clip("JumpStart"), new Vector3(300, 80), 1.6f);
        State(machine, "Air", Clip("JumpLoop"), new Vector3(300, 160));
        State(machine, "Land", Clip("JumpLand"), new Vector3(300, 240), 1.5f);
        State(machine, "Hurt", Clip("HitChest"), new Vector3(560, 0), 1.2f);
        State(machine, "Victory", Clip("Dance"), new Vector3(560, 80));
        State(machine, "Lost", Clip("Death"), new Vector3(560, 160));
        State(machine, "Idle", Clip("Idle"), new Vector3(560, 240));
        EditorUtility.SetDirty(controller);
    }

    static void BuildNpc()
    {
        var controller = Fresh(Folder + "/NPC.controller");
        var machine = controller.layers[0].stateMachine;
        machine.defaultState = State(machine, "Idle", Clip("IdleTalk"), new Vector3(300, 0));
        State(machine, "Wait", Clip("Idle"), new Vector3(300, 80));
        State(machine, "Throw", Clip("Cast"), new Vector3(300, 160));
        State(machine, "Knockdown", Clip("Death"), new Vector3(560, 0), 1.6f);
        State(machine, "Hurt", Clip("HitHead"), new Vector3(560, 80));
        State(machine, "Walk", Clip("Walk"), new Vector3(560, 160));
        EditorUtility.SetDirty(controller);
    }

    /// <summary>
    /// Natural ground speed of each locomotion clip on CJ. The root-motion edition of the library
    /// (UAL1_Standard_RM.fbx, not shipped) reports these average speeds for its mannequin; humanoid playback scales
    /// them by the character's human scale. The blend-tree thresholds use them, so feet plant at those speeds.
    /// </summary>
    public static Dictionary<string, float> MeasureStrideSpeeds()
    {
        float ratio = HumanScale("Assets/Resources/Models/Characters/CJ/CJ.fbx") / HumanScale(Ual);
        return new Dictionary<string, float> { { "Walk", .970f * ratio }, { "Jog", 4.507f * ratio }, { "Sprint", 5.153f * ratio } };
    }

    static float HumanScale(string path)
    {
        var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        try { return go.GetComponent<Animator>().humanScale; }
        finally { Object.DestroyImmediate(go); }
    }
}
