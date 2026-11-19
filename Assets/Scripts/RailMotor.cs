using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>
    /// Pure platformer motor math shared by the game, the autopilot and the reachability tests.
    /// Asymmetric gravity, apex hang while jump is held, capped (fast) fall and separate accel/decel/turn rates.
    /// </summary>
    public static class RailMotor
    {
        /// <summary>Moves vx towards axis·maxSpeed using the decel rate at rest input, the turn rate against motion, accel otherwise.</summary>
        public static float Horizontal(float vx,float axis,float maxSpeed,bool grounded,float dt)
        {
            float target=axis*maxSpeed;
            float rate;
            if(Mathf.Abs(axis)<.01f)rate=grounded?RailGame.GroundDecel:RailGame.AirDecel;
            else if(vx*target<0)rate=grounded?RailGame.GroundTurn:RailGame.AirTurn;
            else rate=grounded?RailGame.GroundAccel:RailGame.AirAccel;
            return Mathf.MoveTowards(vx,target,rate*dt);
        }
        /// <summary>
        /// One vertical step. A cuttable jump released while still rising fast is cut to JumpCutSpeed;
        /// gravity grows while falling, halves around the apex while jump is held and the fall speed is capped.
        /// </summary>
        public static float Vertical(float vy,ref bool jumpCut,bool held,bool fastFall,float dt)
        {
            if(jumpCut && !held && vy>RailGame.JumpCutSpeed)vy=RailGame.JumpCutSpeed;
            if(vy<=0)jumpCut=false;
            fastFall&=vy<RailGame.ApexThreshold;
            float g=RailGame.Gravity;
            if(vy<0)g*=fastFall?RailGame.FastFallGravityMultiplier:RailGame.FallGravityMultiplier;
            if(held && !fastFall && Mathf.Abs(vy)<RailGame.ApexThreshold)g*=RailGame.ApexGravityMultiplier;
            return Mathf.Max(vy-g*dt,-(fastFall?RailGame.FastFallSpeed:RailGame.MaxFallSpeed));
        }
        /// <summary>Height gained by a launch at speed v, without apex hang.</summary>
        public static float Apex(float v)=>v*v/(2*RailGame.Gravity);
        /// <summary>Time to fall h metres from rest with fall gravity and the fall speed cap.</summary>
        public static float FallTime(float h)
        {
            float g=RailGame.Gravity*RailGame.FallGravityMultiplier,cap=RailGame.MaxFallSpeed;
            float capHeight=cap*cap/(2*g);
            if(h<=0)return 0;
            if(h<=capHeight)return Mathf.Sqrt(2*h/g);
            return cap/g+(h-capHeight)/cap;
        }
        /// <summary>Highest climb: ground jump, then the double jump at its apex.</summary>
        public static float MaxHeight=>Apex(RailGame.JumpSpeed)+Apex(RailGame.AirJumpSpeed);
        /// <summary>Horizontal distance at full run speed: jump, double jump at the apex, land rise metres higher.</summary>
        public static float Reach(float rise)
        {
            float rising=RailGame.JumpSpeed/RailGame.Gravity+RailGame.AirJumpSpeed/RailGame.Gravity;
            return RailGame.RunSpeed*(rising+FallTime(MaxHeight-rise));
        }
        /// <summary>Horizontal distance of a single full jump at run speed, landing rise metres higher.</summary>
        public static float SingleReach(float rise)
        {
            float h=Apex(RailGame.JumpSpeed);
            if(rise>=h)return 0;
            return RailGame.RunSpeed*(RailGame.JumpSpeed/RailGame.Gravity+FallTime(h-rise));
        }
    }
}
