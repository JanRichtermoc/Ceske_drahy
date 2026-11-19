using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>
    /// An animated non-player character (ticket inspector, underpass alchemist) on Resources/Animation/NPC.controller.
    /// The game logic keeps its own timers; the actor only cross-fades to the matching state and turns towards CJ.
    /// Returns null from Create when the model is missing, so callers fall back to the primitive figure.
    /// </summary>
    public sealed class RailActor
    {
        public enum Mode { Idle, Wait, Throw, Knockdown, Hurt }
        public Transform Root { get; }
        /// <summary>The right hand, for props such as the syringe.</summary>
        public Transform Hand { get; }
        readonly Animator animator;
        readonly Transform facing;
        Mode mode = (Mode)(-1);
        float yaw;

        RailActor(Transform root, Transform facing, Animator animator)
        {
            Root = root; this.facing = facing; this.animator = animator;
            Hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
            Set(Mode.Idle, Random.value);
        }

        /// <summary>Instantiates Models/Characters/&lt;model&gt; under root (feet at the root), dressed in Railway/Surface.</summary>
        public static RailActor Create(string model, Transform root, float brightness = 1)
        {
            var prefab = Resources.Load<GameObject>("Models/Characters/" + model + "/" + model);
            var controller = Resources.Load<RuntimeAnimatorController>("Animation/NPC");
            if (!prefab || !controller) return null;
            var facing = new GameObject("Facing").transform;
            facing.SetParent(root, false);
            var instance = Object.Instantiate(prefab, facing, false);
            instance.name = model + " (Sketchfab CC BY) + Quaternius UAL";
            var animator = instance.GetComponent<Animator>();
            if (!animator) { Object.Destroy(facing.gameObject); return null; }
            animator.runtimeAnimatorController = controller;
            RailMaterials.DressCharacter(instance, "Models/Characters/" + model + "/Textures/", brightness);
            return new RailActor(root, facing, animator);
        }

        /// <summary>Cross-fades to a state; offset (0–1) starts loops at different phases.</summary>
        public void Set(Mode next, float offset = 0)
        {
            if (next == mode) return;
            mode = next;
            animator.CrossFade(next.ToString(), next == Mode.Knockdown ? .08f : .15f, 0, offset);
        }

        /// <summary>Turns the actor towards worldX, three quarters to the camera like CJ.</summary>
        public void Face(float worldX, float dt)
        {
            float direction = worldX >= Root.position.x ? 1 : -1;
            yaw = Mathf.LerpAngle(yaw, -direction * 55, 1 - Mathf.Exp(-dt * 8));
            facing.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        public void SetPaused(bool paused) { animator.speed = paused ? 0 : 1; }
    }
}
