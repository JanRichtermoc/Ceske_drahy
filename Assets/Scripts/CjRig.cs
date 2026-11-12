using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>Original low-poly Carl Johnson fan model with an articulated animation rig.</summary>
    public sealed class CjRig
    {
        public Transform Root { get; }
        public float Direction => direction;
        readonly Transform facing, hips, torso, head;
        readonly Transform[] thigh = new Transform[2], shin = new Transform[2], foot = new Transform[2];
        readonly Transform[] upperArm = new Transform[2], forearm = new Transform[2];
        float runPhase, landing, direction = 1;
        bool wasGrounded;

        public CjRig(RailWorld world)
        {
            Root = world.Group("Carl Johnson / CJ", Vector3.zero);
            facing = world.Group("Facing", Vector3.zero, Root);
            hips = world.Group("Pelvis", new Vector3(0,.84f,0), facing);
            torso = world.Group("Spine", Vector3.zero, hips);
            var skin = world.Mat("CJ skin", "#8F5437", smooth: .32f);
            var skinDark = world.Mat("CJ lower face", "#623E2B");
            var hair = world.Mat("CJ close-cropped hair", "#251C1B");
            var cotton = world.Mat("CJ white cotton tank", "#EDEBE4");
            var denim = world.Mat("CJ faded denim", "#335876");
            var denimLight = world.Mat("CJ denim seam", "#537C99");
            var sole = world.Mat("CJ shoe sole", "#D7D5C9");

            Loft(world,"Jeans waist",hips,denim,
                new Vector3(-.12f,.23f,.135f),new Vector3(.015f,.245f,.155f),new Vector3(.10f,.215f,.135f));
            Loft(world,"Tank top",torso,cotton,
                new Vector3(.065f,.208f,.128f),new Vector3(.25f,.227f,.14f),new Vector3(.43f,.275f,.157f),new Vector3(.48f,.245f,.144f));
            Loft(world,"Collar and shoulders",torso,skin,
                new Vector3(.456f,.25f,.139f),new Vector3(.51f,.273f,.141f),new Vector3(.59f,.193f,.106f),new Vector3(.65f,.091f,.086f));
            head = world.Group("Head pivot",new Vector3(0,.61f,0),torso);
            Loft(world,"Jaw, cheeks and scalp",head,skin,
                new Vector3(0,.077f,.077f),new Vector3(.067f,.127f,.117f),new Vector3(.13f,.163f,.141f),
                new Vector3(.23f,.169f,.141f),new Vector3(.325f,.157f,.14f),new Vector3(.385f,.119f,.103f),new Vector3(.408f,.027f,.028f));
            Loft(world,"Buzz cut",head,hair,
                new Vector3(.308f,.158f,.141f),new Vector3(.349f,.157f,.14f),new Vector3(.391f,.12f,.106f),new Vector3(.414f,.04f,.036f));
            world.Box("Nose",new Vector3(0,.199f,-.137f),new Vector3(.085f,.093f,.076f),skin,head,PrimitiveType.Sphere);
            world.Box("Mouth",new Vector3(0,.113f,-.114f),new Vector3(.105f,.018f,.022f),skinDark,head,PrimitiveType.Sphere);
            for(int i=0;i<2;i++)
            {
                int side = i==0 ? -1 : 1;
                world.Box("Ear",new Vector3(side*.163f,.195f,.004f),new Vector3(.05f,.108f,.064f),skin,head,PrimitiveType.Sphere);
                world.Box("Eye",new Vector3(side*.071f,.235f,-.126f),new Vector3(.072f,.031f,.029f),cotton,head,PrimitiveType.Sphere);
                world.Box("Pupil",new Vector3(side*.071f,.234f,-.142f),new Vector3(.027f,.025f,.014f),hair,head,PrimitiveType.Sphere);
                world.Box("Brow",new Vector3(side*.072f,.266f,-.124f),new Vector3(.075f,.019f,.024f),hair,head,PrimitiveType.Sphere);
                world.Box("Tank strap",new Vector3(side*.16f,.513f,-.121f),new Vector3(.09f,.105f,.041f),cotton,torso,PrimitiveType.Cube);
                thigh[i] = world.Group("Hip joint",new Vector3(side*.121f,-.062f,0),hips);
                Loft(world,"Jean thigh",thigh[i],denim,new Vector3(-.39f,.09f,.087f),new Vector3(-.25f,.104f,.094f),new Vector3(-.075f,.128f,.124f),new Vector3(.04f,.125f,.123f));
                shin[i] = world.Group("Knee",new Vector3(0,-.365f,0),thigh[i]);
                Loft(world,"Jean cuff",shin[i],denim,new Vector3(-.326f,.087f,.081f),new Vector3(-.25f,.087f,.086f),new Vector3(-.105f,.099f,.093f),new Vector3(.052f,.09f,.087f));
                world.Box("Jean outside seam",new Vector3(side*.083f,-.214f,.01f),new Vector3(.022f,.19f,.031f),denimLight,shin[i],PrimitiveType.Sphere);
                foot[i] = world.Group("Ankle",new Vector3(0,-.316f,0),shin[i]);
                world.Box("Sneaker",new Vector3(0,-.028f,-.056f),new Vector3(.179f,.139f,.318f),cotton,foot[i],PrimitiveType.Sphere);
                world.Box("Sneaker sole",new Vector3(0,-.082f,-.052f),new Vector3(.18f,.055f,.302f),sole,foot[i],PrimitiveType.Sphere);
                world.Box("Sneaker trim",new Vector3(0,-.029f,-.165f),new Vector3(.143f,.061f,.055f),denimLight,foot[i],PrimitiveType.Sphere);
                upperArm[i]=world.Group("Shoulder",new Vector3(side*.265f,.51f,0),torso);
                Loft(world,"Upper arm",upperArm[i],skin,new Vector3(-.262f,.073f,.072f),new Vector3(-.17f,.094f,.085f),new Vector3(-.04f,.103f,.091f),new Vector3(.052f,.065f,.063f));
                forearm[i]=world.Group("Elbow",new Vector3(0,-.24f,0),upperArm[i]);
                Loft(world,"Forearm",forearm[i],skin,new Vector3(-.22f,.052f,.056f),new Vector3(-.126f,.075f,.07f),new Vector3(.03f,.073f,.073f));
                world.Box("Closed hand",new Vector3(0,-.248f,-.008f),new Vector3(.114f,.135f,.108f),skin,forearm[i],PrimitiveType.Sphere);
            }
        }

        static void Loft(RailWorld world,string name,Transform parent,Material material,params Vector3[] rings)
        {
            const int sides=12;
            var vertices=new Vector3[rings.Length*sides+2];
            var triangles=new int[(rings.Length-1)*sides*6+sides*6];
            int t=0;
            for(int r=0;r<rings.Length;r++)
            for(int i=0;i<sides;i++)
            {
                float angle=i*Mathf.PI*2/sides;
                vertices[r*sides+i]=new Vector3(Mathf.Cos(angle)*rings[r].y,rings[r].x,Mathf.Sin(angle)*rings[r].z);
                if(r==rings.Length-1)continue;
                int a=r*sides+i,b=r*sides+(i+1)%sides;
                triangles[t++]=a;triangles[t++]=a+sides;triangles[t++]=b;
                triangles[t++]=b;triangles[t++]=a+sides;triangles[t++]=b+sides;
            }
            int bottom=vertices.Length-2,top=vertices.Length-1,last=(rings.Length-1)*sides;
            vertices[bottom]=new Vector3(0,rings[0].x,0);
            vertices[top]=new Vector3(0,rings[rings.Length-1].x,0);
            for(int i=0;i<sides;i++)
            {
                int next=(i+1)%sides;
                triangles[t++]=bottom;triangles[t++]=i;triangles[t++]=next;
                triangles[t++]=top;triangles[t++]=last+next;triangles[t++]=last+i;
            }
            var mesh=new Mesh {name=name,vertices=vertices,triangles=triangles};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            world.AttachMesh(name,parent,mesh,material);
        }

        public void Reset()
        {
            wasGrounded=false;landing=runPhase=0;direction=1;
        }

        public void Pose(Vector2 position,Vector2 velocity,bool grounded,bool dash,bool hit,float dt,bool preview=false)
        {
            Root.position=new Vector3(position.x,position.y,0);
            if(Mathf.Abs(velocity.x)>.25f)direction=Mathf.Sign(velocity.x);
            float speed=Mathf.Clamp01(Mathf.Abs(velocity.x)/7.8f);
            if(grounded&&!wasGrounded&&!preview)landing=1;
            wasGrounded=grounded;
            landing=Mathf.MoveTowards(landing,0,dt*6.5f);
            runPhase+=dt*(2+speed*15);
            float blend=1-Mathf.Exp(-dt*19);
            float yaw=preview? -21 : -direction*(45+23*speed);
            facing.localRotation=Quaternion.Slerp(facing.localRotation,Quaternion.Euler(0,yaw,0),blend);
            float bob=preview?Mathf.Sin(runPhase*1.5f)*.012f:grounded?Mathf.Cos(runPhase*2)*.018f*speed:0;
            hips.localPosition=new Vector3(0,.84f+bob-landing*.095f,0);
            Turn(torso,dash?-39:hit?24:grounded?-8*speed-landing*17:velocity.y>0?10:-13,0,grounded?Mathf.Sin(runPhase)*5*speed:4,blend);
            Turn(head,hit?-16:grounded?-4:velocity.y>0?-8:7,preview?12:0,0,blend);
            for(int i=0;i<2;i++)
            {
                float stride=Mathf.Sin(runPhase+i*Mathf.PI);
                float hip=grounded?stride*41*speed+landing*21: i==0?36:-23;
                float knee=grounded?Mathf.Max(0,-stride)*63*speed+landing*41: i==0?65:33;
                float shoulder=grounded?-stride*36*speed: i==0?45:65;
                float elbow=grounded?25+speed*51:62;
                if(dash){hip=i==0?61:-39;knee=i==0?78:24;shoulder=-63;elbow=24;}
                if(hit){hip=i==0?-27:48;knee=34;shoulder=82;elbow=49;}
                Turn(thigh[i],hip,0,0,blend);
                Turn(shin[i],-knee,0,0,blend);
                Turn(foot[i],grounded?Mathf.Clamp(knee-hip,-20,29):10,0,0,blend);
                Turn(upperArm[i],shoulder,0,(i==0?-1:1)*(preview?8:hit?45:11),blend);
                Turn(forearm[i],elbow,0,0,blend);
            }
        }
        static void Turn(Transform joint,float x,float y,float z,float blend)
        {
            joint.localRotation=Quaternion.Slerp(joint.localRotation,Quaternion.Euler(x,y,z),blend);
        }
    }
}
