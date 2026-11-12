using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CeskeDrahy
{
    public sealed class RailWorld
    {
        public sealed class Platform
        {
            public float x, width, top, amplitude, phase, offset, delta;
            public Transform root;
            public float Left => x + offset - width / 2;
            public float Right => x + offset + width / 2;
        }
        public readonly List<Platform> platforms = new List<Platform>();
        public readonly List<Transform> tickets = new List<Transform>();
        public readonly List<Vector2> ticketOrigins = new List<Vector2>();
        public readonly List<Transform> birds = new List<Transform>();
        public readonly List<Vector2> birdOrigins = new List<Vector2>();
        public readonly List<Transform> inspectors = new List<Transform>();
        public readonly List<Transform> signals = new List<Transform>();
        public Transform root;
        public Material navy, blue, cream, steel, dark, gold, skin, rust, glass, green;
        readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        public Material Mat(string name, string hex, float metal = 0, float smooth = .25f)
        {
            if (mats.TryGetValue(name, out var existing)) return existing;
            ColorUtility.TryParseHtmlString(hex, out var color);
            var m = new Material(Shader.Find("Standard")) { name = name, color = color, enableInstancing = true };
            m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", smooth); mats[name] = m;
            return m;
        }
        public Transform Box(string name, Vector3 pos, Vector3 size, Material material, Transform parent = null, PrimitiveType type = PrimitiveType.Cube)
        {
            var o = GameObject.CreatePrimitive(type); o.name = name;
            o.transform.SetParent(parent ? parent : root, false);
            o.transform.localPosition = pos;
            // Unity's capsule primitive is two units tall; all authored sizes are full extents.
            if(type==PrimitiveType.Capsule)size.y*=.5f;
            o.transform.localScale = size;
            Object.Destroy(o.GetComponent<Collider>());
            o.GetComponent<Renderer>().sharedMaterial = material;
            return o.transform;
        }
        public Transform Group(string name, Vector3 pos, Transform parent = null)
        {
            var t = new GameObject(name).transform; t.SetParent(parent ? parent : root, false); t.localPosition = pos; return t;
        }
        Transform Roof(string name, Vector3 pos, float width, float depth, Material material, Transform parent)
        {
            // Shallow arched coach roof: 24 vertices instead of a high-poly cylinder.
            var t=Group(name,pos,parent);var mesh=new Mesh();var vertices=new Vector3[24];var uv=new Vector2[24];
            var indices=new List<int>();
            for(int side=0;side<2;side++)for(int k=0;k<12;k++)
            {float a=k/11f*Mathf.PI;vertices[side*12+k]=new Vector3((side-.5f)*width,Mathf.Sin(a)*.21f,Mathf.Cos(a)*depth/2);uv[side*12+k]=new Vector2(side,k/11f);}
            for(int k=0;k<11;k++){indices.AddRange(new[]{k,k+12,k+1,k+1,k+12,k+13});}
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
        }
        public void Sign(string text, Vector3 pos, float size, Transform parent = null, Color? color = null)
        {
            var t = Group(text, pos, parent); var m = t.gameObject.AddComponent<TextMesh>();
            m.text = text; m.fontSize = 64; m.characterSize = size; m.anchor = TextAnchor.MiddleCenter;
            m.color = color ?? new Color(.91f,.94f,.9f); m.fontStyle = FontStyle.Bold;
        }
        public void Build()
        {
            root = new GameObject("Railway world").transform;
            navy=Mat("Midnight blue","#142C40"); blue=Mat("Rail blue","#226896",.35f,.52f);
            cream=Mat("Ivory enamel","#E3E0CF",.16f,.4f); steel=Mat("Brushed steel","#83999F",.65f,.6f);
            dark=Mat("Rubber and sleepers","#263137"); gold=Mat("Safety amber","#F4B651",.3f,.55f);
            skin=Mat("Warm skin","#A56D49"); rust=Mat("Terracotta","#9C6150"); glass=Mat("Smoky windows","#173F53",.5f,.8f);
            green=Mat("Leaves","#4E715C");
            var ballast=Mat("Sketchfab railway scan","#C2B9A3");
            ballast.mainTexture=Resources.Load<Texture2D>("Textures/RailBallast"); ballast.mainTextureScale=new Vector2(40,2);
            ballast.SetTexture("_BumpMap",Resources.Load<Texture2D>("Textures/RailBallastNormal"));ballast.SetFloat("_BumpScale",.35f);ballast.EnableKeyword("_NORMALMAP");
            Box("Ballast bed",new Vector3(92,-1.2f,3),new Vector3(220,.4f,14),ballast);
            Box("Foreground ground",new Vector3(92,-1.65f,0),new Vector3(220,.5f,45),Mat("Earth","#55564C"));
            // Parallel tracks, with shared meshes and materials.
            for(int track=0;track<3;track++)
            {
                float z=track*4;
                for(int side=-1;side<=1;side+=2) Box("Rail",new Vector3(92,-.77f,z+side*.8f),new Vector3(220,.13f,.09f),steel);
                for(int i=-8;i<202;i+=2) Box("Sleeper",new Vector3(i,-.91f,z),new Vector3(.25f,.16f,2.4f),dark);
            }
            PlatformStation(0,16,.12f,"PRAHA  /  HLAVNÍ NÁDRAŽÍ");
            Train(15,10,2.25f,0); Train(28,10,2.6f,1); Train(42,11,2.3f,2);
            PlatformStation(56,12,.8f,"01  /  VRŠOVICE");
            Train(70,10,2.7f,0); Train(84,10,3.1f,1,.7f); Train(98,10,2.65f,2);
            PlatformStation(112,12,1,"02  /  DEPO");
            Train(126,10,3,1); Train(140,10,3.5f,0,.6f); Train(154,10,2.8f,2);
            PlatformStation(170,18,1.2f,"CÍL  /  POSLEDNÍ SPOJ");
            foreach(var p in platforms)
            {
                for(float x=p.x-p.width/2+2;x<p.x+p.width/2-1;x+=2.5f)
                {
                    var t=Box("Jízdenka",new Vector3(x,p.top+1.25f,0),new Vector3(.45f,.3f,.07f),gold);
                    Box("Ticket stripe",new Vector3(0,0,-.045f),new Vector3(.29f,.055f,.015f),cream,t);
                    tickets.Add(t);ticketOrigins.Add(new Vector2(x,p.top+1.25f));
                }
            }
            foreach(float x in new[]{25f,39f,76f,91f,132f,148f,159f})
            {
                var b=Group("Holub",new Vector3(x,5.2f,0));
                Box("Body",Vector3.zero,new Vector3(.55f,.3f,.33f),steel,b,PrimitiveType.Sphere);
                Box("Head",new Vector3(-.3f,.13f,0),Vector3.one*.24f,navy,b,PrimitiveType.Sphere);
                Box("Beak",new Vector3(-.45f,.12f,0),new Vector3(.17f,.07f,.1f),gold,b);
                Box("Left wing",new Vector3(0,.08f,-.3f),new Vector3(.55f,.065f,.58f),dark,b);
                Box("Right wing",new Vector3(0,.08f,.3f),new Vector3(.55f,.065f,.58f),dark,b);
                birds.Add(b);birdOrigins.Add(new Vector2(x,5.2f));
            }
            foreach(float x in new[]{32f,46f,88f,102f,144f,158f})
            {
                var p=platforms.Find(v=>x>=v.Left && x<=v.Right);
                var man=Person("Revizor",new Vector3(x,p.top,0),true);
                var warn=Box("Attack warning",new Vector3(0,2.3f,0),Vector3.one*.2f,gold,man,PrimitiveType.Sphere);
                inspectors.Add(man);signals.Add(warn);
            }
            Background();
            RenderSettings.skybox=new Material(Resources.Load<Shader>("EveningSky"));
            Box("Finish gate",new Vector3(175,3.5f,1),new Vector3(.17f,5, .2f),gold);
            Box("Finish flag",new Vector3(176,5.5f,1),new Vector3(2,1,.08f),blue);
            Sign("CÍL",new Vector3(176,5.5f,.93f),.16f);
            var sun=new GameObject("Evening sunlight").AddComponent<Light>();sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(38,-35,0);sun.color=new Color(1,.8f,.57f);sun.intensity=1.15f;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.48f,.64f,.72f);RenderSettings.ambientEquatorColor=new Color(.47f,.51f,.52f);
            RenderSettings.ambientGroundColor=new Color(.25f,.28f,.3f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.63f,.71f,.73f);RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=30;RenderSettings.fogEndDistance=105;
            QualitySettings.shadowDistance=38;QualitySettings.shadowResolution=ShadowResolution.Medium;
        }
        void PlatformStation(float x,float width,float top,string label)
        {
            var g=Group("Nástupiště",new Vector3(x,0,0));
            Box("Concrete",new Vector3(0,top-.55f,0),new Vector3(width,1.1f,3.1f),Mat("Concrete","#9EA99F"),g);
            Box("Yellow edge",new Vector3(0,top+.015f,-1.4f),new Vector3(width,.04f,.12f),gold,g);
            for(float i=-width/2;i<width/2;i+=.8f)Box("Edge marking",new Vector3(i,top-.16f,-1.57f),new Vector3(.4f,.15f,.035f),cream,g);
            Box("Station sign",new Vector3(0,top+3.6f,2),new Vector3(7,.85f,.16f),navy,g);
            Sign(label,new Vector3(0,top+3.6f,1.9f),label.Length>22?.06f:.085f,g);
            foreach(float i in new[]{-3.2f,3.2f})Box("Sign post",new Vector3(i,top+1.8f,2),new Vector3(.1f,3.6f,.1f),steel,g);
            platforms.Add(new Platform{x=x,width=width,top=top,root=g});
            if(x>0){Box("Checkpoint lamp",new Vector3(-width/2+1,top+1.2f,1),new Vector3(.15f,2.4f,.15f),gold,g);}
        }
        void Train(float x,float width,float top,int variant,float moving=0)
        {
            var g=Group("Vagon "+variant,new Vector3(x,0,0));
            if(variant!=1 && ImportedTrain(g,width,top))
            {
                platforms.Add(new Platform{x=x,width=width,top=top,root=g,amplitude=moving,phase=x});
                return;
            }
            Material body=variant==2?rust:blue;
            Box("Body",new Vector3(0,top-1.02f,0),new Vector3(width,1.8f,2.15f),body,g);
            Box("Roof base",new Vector3(0,top-.24f,0),new Vector3(width+.15f,.1f,2.25f),steel,g);
            Roof("Arched roof",new Vector3(0,top-.21f,0),width+.15f,2.25f,steel,g);
            for(float i=-width/2+.4f;i<width/2;i+=1.3f)
                Box("Roof seam",new Vector3(i,top+.004f,0),new Vector3(.018f,.012f,1.5f),dark,g);
            Box("Ivory band",new Vector3(0,top-1.25f,-1.09f),new Vector3(width,.25f,.035f),cream,g);
            Box("Underframe",new Vector3(0,top-2.05f,0),new Vector3(width-.6f,.24f,1.8f),dark,g);
            for(float i=-width/2+1;i<width/2-.6f;i+=1.4f)
            {
                Box("Window trim",new Vector3(i,top-.65f,-1.09f),new Vector3(1.03f,.7f,.055f),cream,g);
                Box("Window",new Vector3(i,top-.65f,-1.13f),new Vector3(.9f,.58f,.03f),glass,g);
                Box("Glass highlight",new Vector3(i-.27f,top-.65f,-1.15f),new Vector3(.035f,.55f,.015f),steel,g);
                Box("Window divider",new Vector3(i,top-.55f,-1.155f),new Vector3(.9f,.025f,.018f),steel,g);
            }
            foreach(float i in new[]{-width/2+1.5f,width/2-1.5f})
            {
                Box("Bogie",new Vector3(i,top-2.25f,0),new Vector3(2,.32f,1.9f),dark,g);
                foreach(float j in new[]{-.6f,.6f})
                {var t=Box("Wheel",new Vector3(i+j,top-2.25f,-.95f),new Vector3(.55f,.12f,.55f),dark,g,PrimitiveType.Cylinder);t.localRotation=Quaternion.Euler(90,0,0);}
            }
            Box("Door",new Vector3(width/2-.4f,top-1,-1.14f),new Vector3(.55f,1.65f,.055f),navy,g);
            Box("Door window",new Vector3(width/2-.4f,top-.63f,-1.18f),new Vector3(.34f,.6f,.02f),glass,g);
            Box("Door handle",new Vector3(width/2-.6f,top-1.15f,-1.2f),new Vector3(.025f,.22f,.035f),steel,g);
            for(int step=0;step<2;step++)Box("Door step",new Vector3(width/2-.4f,top-1.85f-step*.17f,-1.22f-step*.1f),new Vector3(.7f,.07f,.3f),steel,g);
            Sign("2",new Vector3(-width/2+.3f,top-.6f,-1.17f),.065f,g);
            Sign("ČESKÉ DRÁHY",new Vector3(0,top-1.6f,-1.15f),.037f,g);
            Sign("CZ—ČD   50 54 20-41   160 km/h",new Vector3(-2,top-1.84f,-1.15f),.016f,g);
            platforms.Add(new Platform{x=x,width=width+.15f,top=top,root=g,amplitude=moving,phase=x});
        }
        bool ImportedTrain(Transform parent,float width,float top)
        {
            var prefab=Resources.Load<GameObject>("Models/CD810/scene");if(!prefab)return false;
            var wrapper=Group("ČD 810 / voxellation / Sketchfab CC BY",Vector3.zero,parent);
            var instance=Object.Instantiate(prefab,wrapper);instance.transform.localPosition=Vector3.zero;
            var renderers=instance.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return false;
            Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            Vector3 center=wrapper.InverseTransformPoint(b.center);
            wrapper.localScale=new Vector3(width/b.size.x,3.1f/b.size.y,2.5f/b.size.z);
            instance.transform.localPosition=-center;
            wrapper.localPosition=new Vector3(0,top-1.55f,0);
            return true;
        }
        public Transform Person(string name,Vector3 position,bool inspector=false)
        {
            var p=Group(name,position); var jacket=inspector?navy:Mat("Hero jacket","#D68342");
            Box("Torso",new Vector3(0,1.06f,0),new Vector3(.52f,.65f,.35f),jacket,p,PrimitiveType.Capsule);
            Box("Shirt",new Vector3(.02f,1.2f,-.18f),new Vector3(.12f,.35f,.025f),cream,p);
            Box("Neck",new Vector3(0,1.44f,0),new Vector3(.18f,.23f,.18f),skin,p,PrimitiveType.Cylinder);
            Box("Head",new Vector3(0,1.65f,0),new Vector3(.38f,.43f,.36f),skin,p,PrimitiveType.Sphere);
            Box("Hair",new Vector3(0,1.81f,.02f),new Vector3(.39f,.18f,.36f),dark,p,PrimitiveType.Sphere);
            Box("Nose",new Vector3(.19f,1.63f,-.03f),new Vector3(.1f,.1f,.12f),skin,p,PrimitiveType.Sphere);
            Box("Eye",new Vector3(.13f,1.7f,-.165f),Vector3.one*.04f,dark,p,PrimitiveType.Sphere);
            foreach(int side in new[]{-1,1})
            {
                var leg=Group(side<0?"LegL":"LegR",new Vector3(side*.14f,.72f,0),p);
                Box("Denim",new Vector3(0,-.28f,0),new Vector3(.2f,.6f,.23f),navy,leg,PrimitiveType.Capsule);
                Box("Sneaker",new Vector3(.07f,-.65f,-.02f),new Vector3(.36f,.16f,.27f),cream,leg);
                var arm=Group(side<0?"ArmL":"ArmR",new Vector3(side*.32f,1.32f,0),p);
                Box("Sleeve",new Vector3(0,-.22f,0),new Vector3(.18f,.46f,.22f),jacket,arm,PrimitiveType.Capsule);
                Box("Hand",new Vector3(0,-.48f,0),Vector3.one*.18f,skin,arm,PrimitiveType.Sphere);
            }
            if(inspector){Box("Cap",new Vector3(0,1.88f,0),new Vector3(.46f,.1f,.42f),navy,p);Box("Badge",new Vector3(.14f,1.25f,-.2f),Vector3.one*.09f,gold,p);}
            else {Box("Backpack",new Vector3(-.1f,1.15f,.25f),new Vector3(.47f,.58f,.25f),green,p,PrimitiveType.Capsule);}
            return p;
        }
        void Background()
        {
            var rng=new System.Random(42);
            for(int i=-20;i<205;i+=9)
            {
                float h=4+(float)rng.NextDouble()*6;
                var wall=Mat("Facade"+(i%3),i%3==0?"#B5A494":i%3==1?"#C4BDAA":"#8C9D9B");
                Box("Prague building",new Vector3(i,h/2-1,17),new Vector3(8,h,6),wall);
                Box("Cornice",new Vector3(i,h-.8f,17),new Vector3(8.4f,.25f,6.4f),cream);
                foreach(int side in new[]{-1,1})
                {var roof=Box("Pitched roof",new Vector3(i,h+.15f,17+side*1.65f),new Vector3(8.3f,.16f,3.8f),rust);roof.localRotation=Quaternion.Euler(side*24,0,0);}
                Box("Chimney",new Vector3(i+2,h+.7f,17),new Vector3(.65f,1.7f,.65f),wall);
                for(int row=0;row<(int)h-2;row+=2)for(int col=-2;col<=2;col+=2)
                {
                    Box("Window surround",new Vector3(i+col,row+.6f,13.95f),new Vector3(.95f,1.35f,.12f),cream);
                    Box("Distant window",new Vector3(i+col,row+.6f,13.86f),new Vector3(.7f,1.1f,.06f),(row+col+i)%5==0?gold:glass);
                    Box("Window mullion",new Vector3(i+col,row+.6f,13.81f),new Vector3(.04f,1.1f,.03f),cream);
                    Box("Window sill",new Vector3(i+col,row,13.8f),new Vector3(1.1f,.1f,.3f),cream);
                }
            }
            for(int i=-10;i<190;i+=14)
            {
                Box("Catenary mast",new Vector3(i,3.4f,5),new Vector3(.14f,8.5f,.14f),steel);
                Box("Crossbar",new Vector3(i,7.3f,2),new Vector3(.16f,.12f,6),steel);
                Box("Wire",new Vector3(i+7,7.1f,0),new Vector3(14,.022f,.022f),dark);
                Box("Lamp",new Vector3(i,6,3),new Vector3(.7f,.12f,.4f),cream);
            }
            for(int i=10;i<180;i+=17)
            {
                Box("Tree trunk",new Vector3(i,1.7f,10),new Vector3(.35f,5,.35f),rust);
                for(int n=0;n<4;n++)Box("Tree crown",new Vector3(i+Mathf.Sin(n*2)*1.1f,3.4f+n*.45f,10+Mathf.Cos(n*2)*.65f),new Vector3(2.6f,3.2f,2.5f),green,null,PrimitiveType.Sphere);
            }
            // Backdrop trains create depth without adding gameplay collisions.
            for(int i=12;i<170;i+=28)
            {Box("Parked train",new Vector3(i,1.2f,8),new Vector3(20,2.4f,2.3f),navy);Box("Parked stripe",new Vector3(i,1.7f,6.8f),new Vector3(20,.65f,.05f),cream);}
        }
        public void Tick(float time,float dt)
        {
            foreach(var p in platforms){float v=Mathf.Sin(time*.8f+p.phase)*p.amplitude;p.delta=v-p.offset;p.offset=v;p.root.position=new Vector3(p.x+v,0,0);}
            for(int i=0;i<tickets.Count;i++)if(tickets[i].gameObject.activeSelf){tickets[i].position=new Vector3(ticketOrigins[i].x,ticketOrigins[i].y+Mathf.Sin(time*3+i)*.12f,0);tickets[i].rotation=Quaternion.Euler(0,time*90,8);}
            for(int i=0;i<birds.Count;i++)
            {
                birds[i].position=new Vector3(birdOrigins[i].x+Mathf.Sin(time*.8f+i)*2.8f,birdOrigins[i].y+Mathf.Sin(time*1.7f+i)*1.1f,0);
                birds[i].GetChild(3).localRotation=Quaternion.Euler(Mathf.Sin(time*17+i)*38,0,0);
                birds[i].GetChild(4).localRotation=Quaternion.Euler(-Mathf.Sin(time*17+i)*38,0,0);
            }
        }
    }
}
