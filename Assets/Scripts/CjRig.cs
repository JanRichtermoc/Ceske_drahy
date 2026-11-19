using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>
    /// Carl "CJ" Johnson: the skinned fan model (Resources/Models/Characters/CJ) animated by the Humanoid clips of the
    /// Quaternius Universal Animation Library through Resources/Animation/CJ.controller. The game picks the state
    /// (cross-fades) from the motor; procedural layers add facing, lean, squash and stretch and the double-jump flip.
    /// Hierarchy: Root (feet) → Squash → Lean → Flip → Facing → model.
    /// </summary>
    public sealed class CjRig
    {
        public Transform Root { get; }
        public float Direction => direction;
        /// <summary>Hit blink: renderers are toggled so the hierarchy (and the animation state) keeps running.</summary>
        public bool Visible { get; private set; } = true;
        readonly Transform squash, lean, flip, facing;
        readonly Animator animator;
        readonly Renderer[] renderers;
        readonly float sprintSpeed;
        string state = "";
        float direction = 1, yaw, leanAngle, flipTime = 1, landSquash, jumpStretch, stateTime, previousVx, airVy;
        int seenJumps, seenAirJumps, seenLands, seenHits;
        // Dust puffs on landing, take-off and skids: a small pool of soft sprites that grow and fade by shrinking.
        readonly Transform[] dust = new Transform[14];
        readonly Vector3[] dustVelocity = new Vector3[14];
        readonly float[] dustLife = new float[14], dustSize = new float[14];
        int nextDust;
        float skidClock;
        const float FlipDuration = .32f, HipHeight = .95f;

        public CjRig(RailWorld world)
        {
            Root = world.Group("Carl Johnson / CJ", Vector3.zero);
            squash = world.Group("Squash", Vector3.zero, Root);
            lean = world.Group("Lean", Vector3.zero, squash);
            flip = world.Group("Flip", new Vector3(0, HipHeight, 0), lean);
            facing = world.Group("Facing", new Vector3(0, -HipHeight, 0), flip);
            var prefab = Resources.Load<GameObject>("Models/Characters/CJ/CJ");
            var controller = Resources.Load<RuntimeAnimatorController>("Animation/CJ");
            if (prefab && controller)
            {
                var model = Object.Instantiate(prefab, facing, false);
                model.name = "CJ / GTA fan model (Sketchfab) + Quaternius UAL animations";
                animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                // Tools/blender/character_prep.py exports the model looking down -Z (towards the camera) at yaw 0;
                // Facing turns it towards its running direction.
                RailMaterials.DressCharacter(model, "Models/Characters/CJ/Textures/");
                // Locomotion thresholds are the clips' natural speeds; StrideRate speeds the sprint up beyond it.
                sprintSpeed = 5.26f;
            }
            else
            {
                // Fallback when the model is missing: a simple figure, so the game stays playable.
                world.Box("Body", new Vector3(0, 1.05f, 0), new Vector3(.5f, .9f, .32f), world.cream, facing, PrimitiveType.Capsule);
                world.Box("Legs", new Vector3(0, .42f, 0), new Vector3(.4f, .84f, .28f), world.blue, facing, PrimitiveType.Capsule);
                world.Box("Head", new Vector3(0, 1.68f, 0), Vector3.one * .26f, world.skin, facing, PrimitiveType.Sphere);
            }
            renderers = Root.GetComponentsInChildren<Renderer>(true);
            var dustMaterial = world.SoftSprite("Landing dust", new Color(.82f, .78f, .7f, .55f), true);
            for (int i = 0; i < dust.Length; i++)
            {
                dust[i] = world.Box("Dust", Vector3.zero, Vector3.one, dustMaterial, null, PrimitiveType.Quad);
                dust[i].gameObject.SetActive(false);
            }
        }

        void Puff(Vector3 at, Vector3 velocity, float size, float life)
        {
            int i = nextDust++ % dust.Length;
            dust[i].position = at; dustVelocity[i] = velocity; dustSize[i] = size; dustLife[i] = life;
            dust[i].gameObject.SetActive(true);
        }

        void UpdateDust(float dt, Quaternion faceCamera)
        {
            for (int i = 0; i < dust.Length; i++)
            {
                if (dustLife[i] <= 0) continue;
                dustLife[i] -= dt;
                if (dustLife[i] <= 0) { dust[i].gameObject.SetActive(false); continue; }
                dustVelocity[i] *= 1 - dt * 3;
                dust[i].position += dustVelocity[i] * dt;
                float age = Mathf.Clamp01(dustLife[i] / .45f);
                dust[i].rotation = faceCamera;
                dust[i].localScale = Vector3.one * dustSize[i] * (1.6f - age * .6f) * Mathf.Sqrt(age);
            }
        }

        public void SetVisible(bool visible)
        {
            if (visible == Visible) return;
            Visible = visible;
            foreach (var r in renderers) if (r) r.enabled = visible;
        }

        /// <summary>Pause freezes the animation where it is.</summary>
        public void SetPaused(bool paused){if(animator)animator.speed=paused?0:1;}
        /// <summary>New run or respawn: standing, facing right, counters in sync with the motor.</summary>
        public void Reset(RailGame game)
        {
            direction = 1; yaw = -60; leanAngle = 0; flipTime = 1; landSquash = jumpStretch = 0; previousVx = 0;
            seenJumps = game.JumpCount; seenAirJumps = game.AirJumpCount; seenLands = game.LandCount; seenHits = game.HitCount;
            Play("Locomotion", 0);
        }

        void Play(string next, float fade, float offset = 0)
        {
            if (!animator) { state = next; return; }
            if (next == state && fade > 0) return;
            state = next; stateTime = 0;
            animator.CrossFadeInFixedTime(next, fade, 0, offset);
        }

        /// <summary>Menu showcase: standing idle, turned towards the camera.</summary>
        public void PosePreview(Vector2 position, float dt)
        {
            Root.position = new Vector3(position.x, position.y, 0);
            yaw = Mathf.LerpAngle(yaw, -25, 1 - Mathf.Exp(-dt * 6));
            facing.localRotation = Quaternion.Euler(0, yaw, 0);
            lean.localRotation = Quaternion.identity; flip.localRotation = Quaternion.identity; squash.localScale = Vector3.one;
            if (animator) { animator.speed = 1; animator.SetFloat("Speed", 0); }
            Play("Idle", .25f);
        }

        /// <summary>Called once per rendered frame while playing.</summary>
        public void Pose(RailGame game, Vector2 position, float dt)
        {
            Root.position = new Vector3(position.x, position.y, 0);
            var v = game.velocity;
            bool grounded = game.grounded, hurt = game.Knockback;
            stateTime += dt;
            if (animator) animator.speed = 1;

            // Facing follows the input, not the velocity, so a knockback does not spin CJ around.
            if (Mathf.Abs(game.AxisInput) > .1f && !hurt) direction = Mathf.Sign(game.AxisInput);
            else if (grounded && Mathf.Abs(v.x) > 2 && !hurt) direction = Mathf.Sign(v.x);
            yaw = Mathf.LerpAngle(yaw, -direction * 60, 1 - Mathf.Exp(-dt / .035f));
            facing.localRotation = Quaternion.Euler(0, yaw, 0);

            // State machine driven by the motor's counters (robust to several fixed steps per frame).
            if (game.HitCount != seenHits) { seenHits = game.HitCount; Play("Hurt", .05f); }
            var feet = new Vector3(position.x, position.y + .05f, -.1f);
            if (game.JumpCount != seenJumps)
            {
                seenJumps = game.JumpCount; jumpStretch = 1; Play("JumpStart", .04f, .35f);
                Puff(feet, new Vector3(-v.x * .08f, .3f, 0), .55f, .35f);
            }
            if (game.AirJumpCount != seenAirJumps) { seenAirJumps = game.AirJumpCount; flipTime = 0; jumpStretch = 1; }
            bool landed = game.LandCount != seenLands;
            if (landed)
            {
                seenLands = game.LandCount; landSquash = Mathf.Clamp01(-airVy / 18f) * .6f + .4f;
                if (airVy < -6) for (int side = -1; side <= 1; side += 2) Puff(feet + new Vector3(side * .25f, 0, 0), new Vector3(side * 2.2f, .5f, 0), .5f + landSquash * .4f, .45f);
            }
            if (game.mode == RailGame.Mode.Won) Play("Victory", .2f);
            else if (game.mode == RailGame.Mode.Lost) Play("Lost", .1f);
            else if (state == "Hurt" && stateTime < .32f) { }
            else if (!grounded)
            {
                if (state != "JumpStart" || stateTime > .3f || v.y < 0) Play("Air", .12f);
            }
            else if (landed && Mathf.Abs(v.x) < 3.5f) Play("Land", .04f, .18f);
            else if (state != "Land" || stateTime > .28f || Mathf.Abs(game.AxisInput) > .1f) Play("Locomotion", .1f);
            if (!grounded) airVy = v.y;

            if (animator)
            {
                float speed = grounded ? Mathf.Abs(v.x) : 0;
                animator.SetFloat("Speed", speed, .05f, dt);
                animator.SetFloat("StrideRate", speed > sprintSpeed ? speed / sprintSpeed : 1);
            }

            // Lean into acceleration, back when skidding to a turn; the dash leans hard.
            float accel = dt > 0 ? (v.x - previousVx) / dt : 0;
            previousVx = v.x;
            float target = grounded ? -v.x / RailGame.RunSpeed * 6 - Mathf.Clamp(accel * .06f, -8, 8) : -v.x / RailGame.RunSpeed * 4;
            bool skidding = grounded && Mathf.Abs(game.AxisInput) > .1f && Mathf.Sign(game.AxisInput) != Mathf.Sign(v.x) && Mathf.Abs(v.x) > 3;
            if (skidding)
            {
                target = Mathf.Sign(v.x) * 12;
                skidClock -= dt;
                if (skidClock <= 0) { skidClock = .05f; Puff(feet + new Vector3(Mathf.Sign(v.x) * .2f, 0, 0), new Vector3(v.x * .15f, .4f, 0), .35f, .3f); }
            }
            if (game.chaos.dash > 0) target = -Mathf.Sign(v.x) * 18;
            if (game.reducedMotion) target *= .4f;
            leanAngle = Mathf.LerpAngle(leanAngle, target, 1 - Mathf.Exp(-dt / .06f));
            lean.localRotation = Quaternion.Euler(0, 0, leanAngle);

            // Squash on landing, stretch on take-off.
            landSquash = Mathf.MoveTowards(landSquash, 0, dt / .14f);
            jumpStretch = Mathf.MoveTowards(jumpStretch, 0, dt / .15f);
            float s = game.reducedMotion ? 0 : landSquash * .1f - jumpStretch * .07f;
            squash.localScale = new Vector3(1 + s * .6f, 1 - s, 1 + s * .6f);

            if (game.view) UpdateDust(dt, game.view.transform.rotation);

            // Double jump: one forward somersault around the hips.
            if (flipTime < 1)
            {
                flipTime = Mathf.Min(1, flipTime + dt / FlipDuration);
                float eased = 1 - (1 - flipTime) * (1 - flipTime);
                flip.localRotation = Quaternion.Euler(0, 0, -direction * 360 * eased);
            }
            else flip.localRotation = Quaternion.identity;
        }
    }
}
