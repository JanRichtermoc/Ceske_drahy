using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CeskeDrahy.Tests
{
    /// <summary>Checks of the imported characters, animation clips and controllers the game loads from Resources.</summary>
    public sealed class RailAssetTests
    {
        [TestCase("Assets/Resources/Models/Characters/CJ/CJ.fbx")]
        public void CharacterAvatarIsAValidHumanoid(string path)
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Assert.IsNotNull(avatar, path + " has an avatar");
            Assert.IsTrue(avatar.isHuman && avatar.isValid, path + " avatar is a valid humanoid");
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Assert.IsFalse(importer.humanDescription.human.Any(h => h.humanName == "Jaw" || h.humanName.EndsWith("Eye")), "Body clips must not drive the face");
        }

        [Test]
        public void EveryAnimationClipExistsAsHumanoid()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Animation/UAL/UAL1_Standard.fbx").OfType<AnimationClip>().ToDictionary(c => c.name);
            // Mirrors RailModelImport.UalClips (editor scripts are not visible to this test assembly).
            foreach (var name in "Idle,IdleTalk,Walk,Jog,Sprint,JumpStart,JumpLoop,JumpLand,HitChest,HitHead,Death,Roll,Dance,Punch,Cast,Interact,SitIdle".Split(','))
            {
                Assert.IsTrue(clips.ContainsKey(name), "Clip " + name);
                Assert.IsTrue(clips[name].isHumanMotion, name + " is a humanoid clip");
            }
        }

        [TestCase("Assets/Resources/Animation/CJ.controller", "Locomotion,JumpStart,Air,Land,Hurt,Victory,Lost,Idle")]
        [TestCase("Assets/Resources/Animation/NPC.controller", "Idle,Wait,Throw,Knockdown,Hurt,Walk")]
        public void ControllerHasEveryStateTheGameCrossFadesTo(string path, string states)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            Assert.IsNotNull(controller, path);
            var names = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            foreach (var state in states.Split(','))
            {
                Assert.Contains(state, names, path + " state " + state);
                var motion = controller.layers[0].stateMachine.states.First(s => s.state.name == state).state.motion;
                Assert.IsNotNull(motion, state + " has a motion");
            }
        }

        [Test]
        public void LocomotionThresholdsIncrease()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Animation/CJ.controller");
            var tree = (BlendTree)controller.layers[0].stateMachine.states.First(s => s.state.name == "Locomotion").state.motion;
            var thresholds = tree.children.Select(c => c.threshold).ToArray();
            Assert.AreEqual(4, thresholds.Length);
            for (int i = 1; i < thresholds.Length; i++) Assert.Greater(thresholds[i], thresholds[i - 1]);
        }

        [Test]
        public void CharacterTexturesHaveTheRightImportType()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Models/Characters" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name.EndsWith("_N")) Assert.AreEqual(TextureImporterType.NormalMap, importer.textureType, path);
                if (name.EndsWith("_ORM")) Assert.IsFalse(importer.sRGBTexture, path);
            }
        }
    }
}
