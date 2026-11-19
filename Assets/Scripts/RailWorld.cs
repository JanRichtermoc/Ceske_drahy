using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CeskeDrahy
{
    public sealed class RailWorld
    {
        public sealed class Platform
        {
            public float x, width, top, amplitude, phase, offset, previous, delta;
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
        public RailLevel Level { get; private set; }
        public int LevelIndex { get; private set; }
        readonly List<Mesh> generatedMeshes = new List<Mesh>();
        readonly List<Texture2D> generatedTextures = new List<Texture2D>();
        readonly List<Transform> windProps = new List<Transform>();
        readonly List<Transform> windStreaks = new List<Transform>();
        Material ballast;
        Mesh pyramid;
        readonly Dictionary<string,GameObject> props=new Dictionary<string,GameObject>();
        sealed class Mover{public Transform body;public float from,span,speed,phase;}
        readonly List<Mover> movers=new List<Mover>();
        /// <summary>Red and green aspect of the signal at each checkpoint station.</summary>
        readonly List<GameObject[]> checkpointSignals=new List<GameObject[]>();
        /// <summary>World extent along the track, from the start buffer to past the finish gate.</summary>
        public float Length => Level.Finish+30;
        readonly List<SteamVent> vents = new List<SteamVent>();
        Material skybox;
        sealed class SteamVent
        {
            public Platform Platform;
            public Transform Warning, Plume;
            public float Phase;
            public bool Active;
        }
        public void Dispose()
        {
            root.gameObject.SetActive(false);
            Object.Destroy(root.gameObject);
            foreach(var material in mats.Values)Object.Destroy(material);
            foreach(var mesh in generatedMeshes)Object.Destroy(mesh);
            foreach(var texture in generatedTextures)Object.Destroy(texture);
            Object.Destroy(skybox);
        }
        public void AttachMesh(string name,Transform parent,Mesh mesh,Material material)
        {
            var visual=Group(name,Vector3.zero,parent);
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            visual.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            generatedMeshes.Add(mesh);
        }
        /// <summary>PBR texture projected in world metres (tile = metres per repeat), tinted by hex.</summary>
        public Material Surfaced(string name,string source,string hex,float tile,float bump=.6f,float smooth=.25f,float metal=0)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            var material=Mat(name,hex,metal,smooth);
            material.mainTexture=Resources.Load<Texture2D>("Textures/"+source);
            material.SetTexture("_BumpMap",Resources.Load<Texture2D>("Textures/"+source+"Normal"));
            material.SetFloat("_BumpScale",bump);
            material.SetFloat("_Triplanar",1);material.SetFloat("_TriScale",1/tile);
            return material;
        }
        public Platform PlatformAt(float x)=>platforms.Find(v=>x>=v.x-v.width*.5f && x<=v.x+v.width*.5f);
        public Material SoftSprite(string name,Color tint,bool cloud)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            var material=new Material(Resources.Load<Shader>("SoftSprite")){name=name,color=tint};
            material.SetFloat("_Cloud",cloud?1:0);
            mats.Add(name,material);
            return material;
        }
        public float Surface(float x,float fallback=1)
        {
            var p=PlatformAt(x);
            return p!=null?p.top:fallback;
        }
        public bool SteamHit(Vector2 position)
        {
            foreach(var vent in vents)
                if(vent.Active && Mathf.Abs(position.x-(vent.Platform.x+vent.Platform.offset))<.67f &&
                    position.y>=vent.Platform.top-.3f && position.y<vent.Platform.top+2.2f)return true;
            return false;
        }

        public Material navy, blue, cream, steel, dark, gold, skin, rust, glass, green;
        readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        public Material Mat(string name, string hex, float metal = 0, float smooth = .25f)
        {
            if (mats.TryGetValue(name, out var existing)) return existing;
            ColorUtility.TryParseHtmlString(hex, out var color);
            var template=Resources.Load<Material>("Surface");
            if(!template)throw new System.InvalidOperationException("Missing Surface material. Run SuperCigos / Prepare game scene.");
            var m = new Material(template) { name = name, color = color, enableInstancing = true };
            m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", smooth); mats[name] = m;
            return m;
        }
        Material FromTemplate(string template,string name,string hex)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            ColorUtility.TryParseHtmlString(hex,out var color);
            var source=Resources.Load<Material>(template);
            var m=source?new Material(source){name=name,color=color,enableInstancing=true}:Mat(name+" fallback",hex);
            mats[name]=m;return m;
        }
        /// <summary>Wind-swayed tree crown material.</summary>
        public Material Leaves(string name,string hex)=>FromTemplate("Foliage",name,hex);
        Material Sleepers()=>Surfaced("Concrete sleepers","Concrete","#6F6A63",1.2f,.5f,.1f);
        public Material Water(string name,string hex)=>FromTemplate("Water",name,hex);
        /// <summary>Self-lit surface (lamps, lit windows) that the bloom picks up.</summary>
        public Material Glow(string name,string hex,float intensity)
        {
            var m=Mat(name,hex);
            m.SetColor("_EmissionColor",m.color*intensity);
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
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;generatedMeshes.Add(mesh);return t;
        }
        public Transform Sign(string text, Vector3 pos, float size, Transform parent = null, Color? color = null)
        {
            var t = Group(text, pos, parent); var m = t.gameObject.AddComponent<TextMesh>();
            m.text = text; m.fontSize = 64; m.characterSize = size; m.anchor = TextAnchor.MiddleCenter;
            m.color = color ?? new Color(.91f,.94f,.9f); m.fontStyle = FontStyle.Bold;
            return t;
        }
        public void Build(int levelIndex = 0)
        {
            LevelIndex=levelIndex;Level=RailLevel.All[levelIndex];
            root = new GameObject("Railway world").transform;
            navy=Mat("Midnight blue","#142C40"); blue=Mat("Rail blue","#226896",.35f,.52f);
            cream=Mat("Ivory enamel","#E3E0CF",.16f,.4f); steel=Mat("Brushed steel","#83999F",.65f,.6f);
            dark=Mat("Rubber and sleepers","#263137"); gold=Mat("Safety amber","#F4B651",.3f,.55f);
            skin=Mat("Warm skin","#A56D49"); rust=Mat("Terracotta","#9C6150"); glass=Mat("Smoky windows","#173F53",.5f,.8f);
            green=Mat("Leaves","#4E715C");
            ballast=Surfaced("Sketchfab railway scan","RailBallast","#C2B9A3",2.6f,.45f,.15f);
            var paving=Surfaced("Rail forecourt paving","CrackedConcrete","#FFFFFF",5,.3f,.2f);
            // A river cuts the ground; everything else runs the full route length.
            var spans=Level.River==Vector2.zero?new[]{new Vector2(-18,Length)}:new[]{new Vector2(-18,Level.River.x),new Vector2(Level.River.y,Length)};
            foreach(var span in spans)
            {
                float center=(span.x+span.y)*.5f,size=span.y-span.x;
                Box("Ballast bed",new Vector3(center,-1.2f,3),new Vector3(size,.4f,14),ballast);
                Box("Foreground ground",new Vector3(center,-1.65f,0),new Vector3(size,.5f,45),paving);
            }
            // Parallel tracks, with shared meshes and materials.
            for(int track=0;track<3;track++)
            {
                float z=track*4;
                for(int side=-1;side<=1;side+=2) Box("Rail",new Vector3((Length-18)*.5f,-.77f,z+side*.8f),new Vector3(Length+18,.13f,.09f),steel);
                for(int i=-8;i<Length;i+=2) Box("Sleeper",new Vector3(i,-.91f,z),new Vector3(.25f,.16f,2.4f),Sleepers());
            }
            int station=0;
            for(int i=0;i<Level.Platforms.Length;i++)
            {
                Vector4 p=Level.Platforms[i];
                switch(Level.Kinds[i])
                {
                    case 'S':PlatformStation(p.x,p.y,p.z,station==0?Level.Name:station==Level.Stations.Length-1?Level.Zones[station]:station.ToString("00")+" / "+Level.Zones[station],station>0&&station<Level.Stations.Length-1);station++;break;
                    case 'C':Cargo(p.x,p.y,p.z,p.w,i);break;
                    case 'H':Tank(p.x,p.y,p.z,p.w,i);break;
                    default:Train(p.x,p.y,p.z,(i-1)%4==0?0:(i-1)%4==1?1:2,p.w);break;
                }
            }
            foreach(int index in Level.Vents)CreateVent(platforms[index],index);
            foreach(var p in platforms)
            {
                for(float x=p.x-p.width/2+2;x<p.x+p.width/2-1;x+=2.5f)
                {
                    var t=Box("Jízdenka",new Vector3(x,p.top+1.25f,0),new Vector3(.45f,.3f,.07f),gold);
                    Box("Ticket stripe",new Vector3(0,0,-.045f),new Vector3(.29f,.055f,.015f),cream,t);
                    tickets.Add(t);ticketOrigins.Add(new Vector2(x,p.top+1.25f));
                }
            }
            foreach(float x in Level.Birds)
            {
                float height=Level.Theme==Theme.MainStation?5.2f:Surface(x,3.6f)+1.95f;
                var b=Group("Holub",new Vector3(x,height,0));
                Box("Body",Vector3.zero,new Vector3(.55f,.3f,.33f),steel,b,PrimitiveType.Sphere);
                Box("Head",new Vector3(-.3f,.13f,0),Vector3.one*.24f,navy,b,PrimitiveType.Sphere);
                Box("Beak",new Vector3(-.45f,.12f,0),new Vector3(.17f,.07f,.1f),gold,b);
                Box("Left wing",new Vector3(0,.08f,-.3f),new Vector3(.55f,.065f,.58f),dark,b);
                Box("Right wing",new Vector3(0,.08f,.3f),new Vector3(.55f,.065f,.58f),dark,b);
                birds.Add(b);birdOrigins.Add(new Vector2(x,height));
            }
            foreach(float x in Level.Inspectors)
            {
                var man=Person("Revizor",new Vector3(x,Surface(x),0),true);
                var roof=PlatformAt(x);if(roof!=null)man.SetParent(roof.root,true);
                var warn=Box("Attack warning",new Vector3(0,2.3f,0),Vector3.one*.2f,gold,man,PrimitiveType.Sphere);
                inspectors.Add(man);signals.Add(warn);
            }
            OlomoucBackdrops.Build(this);
            float finish=Level.Finish;
            float finishY=Level.Platforms[Level.Platforms.Length-1].z;
            Box("Finish gate",new Vector3(finish+1,finishY+2.3f,1),new Vector3(.17f,5,.2f),gold);
            Box("Finish flag",new Vector3(finish+2,finishY+4.3f,1),new Vector3(2,1,.08f),blue);
            Sign("CÍL",new Vector3(finish+2,finishY+4.3f,.93f),.16f);
            SetupLight(Level.Sky);
        }
        sealed class Lighting
        {
            public Color horizon,zenith,sun,fill,ambientSky,ambientEquator,fog;
            public float sunStrength,sunPitch,sunIntensity,fillIntensity,fogEnd,sunHeight=.21f,clouds=.4f,stars,rim=.35f;
            public RailGrade grade;
        }
        static RailGrade Grade(float exposure,float saturation,float bloom,float threshold,float vignette,Color lift,Color gain)=>
            new RailGrade{exposure=exposure,contrast=1.05f,saturation=saturation,bloom=bloom,threshold=threshold,vignette=vignette,lift=lift,gain=gain};
        /// <summary>Colour grade of the current route, applied by the camera's RailPost.</summary>
        public RailGrade Grading { get; private set; }
        static Lighting Preset(Sky sky)
        {
            switch(sky)
            {
                case Sky.Day:return new Lighting{horizon=new Color(.72f,.83f,.92f),zenith=new Color(.27f,.5f,.8f),sunStrength=1,sunPitch=50,sun=new Color(1,.95f,.86f),sunIntensity=1.25f,
                    fill=new Color(.6f,.75f,1),fillIntensity=.25f,ambientSky=new Color(.58f,.68f,.8f),ambientEquator=new Color(.52f,.56f,.58f),fog=new Color(.72f,.8f,.88f),fogEnd=120,
                    sunHeight=.55f,clouds=.45f,grade=Grade(1.12f,1.08f,.35f,1.1f,.22f,new Color(0,.01f,.02f),Color.white)};
                case Sky.Dusk:return new Lighting{horizon=new Color(.85f,.5f,.38f),zenith=new Color(.2f,.27f,.45f),sunStrength=.8f,sunPitch=18,sun=new Color(1,.62f,.42f),sunIntensity=1,
                    fill=new Color(.5f,.6f,1),fillIntensity=.35f,ambientSky=new Color(.42f,.44f,.6f),ambientEquator=new Color(.45f,.38f,.4f),fog=new Color(.58f,.5f,.55f),fogEnd=95,
                    sunHeight=.12f,clouds=.55f,rim=.45f,grade=Grade(1.15f,1.05f,.6f,.95f,.3f,new Color(.03f,0,.03f),new Color(1.06f,.95f,.92f))};
                case Sky.Sunset:return new Lighting{horizon=new Color(.98f,.52f,.22f),zenith=new Color(.25f,.32f,.55f),sunStrength=1.4f,sunPitch=10,sun=new Color(1,.58f,.3f),sunIntensity=1.15f,
                    fill=new Color(.45f,.55f,1),fillIntensity=.3f,ambientSky=new Color(.5f,.48f,.58f),ambientEquator=new Color(.6f,.45f,.38f),fog=new Color(.8f,.55f,.42f),fogEnd=100,
                    sunHeight=.07f,clouds=.5f,rim=.5f,grade=Grade(1.12f,1.1f,.7f,.9f,.3f,new Color(.03f,.01f,0),new Color(1.08f,.96f,.86f))};
                case Sky.Evening:return new Lighting{horizon=new Color(.36f,.33f,.5f),zenith=new Color(.06f,.09f,.22f),sunStrength=.25f,sunPitch=25,sun=new Color(.85f,.75f,.9f),sunIntensity=.7f,
                    fill=new Color(1,.6f,.4f),fillIntensity=.45f,ambientSky=new Color(.3f,.33f,.48f),ambientEquator=new Color(.32f,.3f,.38f),fog=new Color(.3f,.3f,.42f),fogEnd=85,
                    sunHeight=.04f,clouds=.35f,stars=.4f,rim=.55f,grade=Grade(1.2f,1,.55f,.95f,.34f,new Color(0,.01f,.04f),new Color(.98f,.98f,1.05f))};
                case Sky.Night:return new Lighting{horizon=new Color(.13f,.22f,.34f),zenith=new Color(.018f,.045f,.13f),sunStrength=.09f,sunPitch=43,sun=new Color(.65f,.8f,1),sunIntensity=.88f,
                    fill=new Color(1,.56f,.34f),fillIntensity=.53f,ambientSky=new Color(.28f,.36f,.51f),ambientEquator=new Color(.25f,.3f,.42f),fog=new Color(.23f,.32f,.48f),fogEnd=76,
                    sunHeight=.3f,clouds=.2f,stars=1,rim=.6f,grade=Grade(1.22f,.95f,.5f,1.05f,.38f,new Color(0,.015f,.05f),new Color(.95f,1,1.08f))};
                default:return new Lighting{horizon=new Color(.78f,.68f,.51f),zenith=new Color(.24f,.44f,.56f),sunStrength=1,sunPitch=32,sun=new Color(1,.82f,.64f),sunIntensity=1.2f,
                    fill=new Color(.61f,.79f,1),fillIntensity=.25f,ambientSky=new Color(.53f,.62f,.72f),ambientEquator=new Color(.47f,.51f,.52f),fog=new Color(.66f,.69f,.72f),fogEnd=105,
                    grade=Grade(1.1f,1.06f,.45f,1,.26f,new Color(.02f,.01f,0),new Color(1.04f,1,.94f))};
            }
        }
        void SetupLight(Sky sky)
        {
            var preset=Preset(sky);
            skybox=new Material(Resources.Load<Shader>("EveningSky"));
            skybox.SetVector("_Horizon",preset.horizon);
            skybox.SetVector("_Zenith",preset.zenith);
            skybox.SetFloat("_SunStrength",preset.sunStrength);
            skybox.SetVector("_SunDir",new Vector4(.5f,preset.sunHeight,.8f,0));
            skybox.SetFloat("_Clouds",preset.clouds);
            skybox.SetFloat("_Stars",preset.stars);
            RenderSettings.skybox=skybox;
            Shader.SetGlobalVector("_RailRim",new Vector4(preset.fill.r,preset.fill.g,preset.fill.b,preset.rim));
            Shader.SetGlobalVector("_RailSkyHorizon",preset.horizon);
            Grading=preset.grade;
            var sun=Group("Main light",Vector3.zero).gameObject.AddComponent<Light>();
            sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(preset.sunPitch,-35,0);
            sun.color=preset.sun;
            sun.intensity=preset.sunIntensity;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.72f;
            var fill=Group("Roof rim light",Vector3.zero).gameObject.AddComponent<Light>();
            fill.type=LightType.Directional;fill.shadows=LightShadows.None;
            fill.transform.rotation=Quaternion.Euler(26,145,0);
            fill.color=preset.fill;
            fill.intensity=preset.fillIntensity;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=preset.ambientSky;
            RenderSettings.ambientEquatorColor=preset.ambientEquator;
            RenderSettings.ambientGroundColor=new Color(.25f,.28f,.3f);
            RenderSettings.fog=true;RenderSettings.fogColor=preset.fog;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=28;RenderSettings.fogEndDistance=preset.fogEnd;
            QualitySettings.shadowDistance=40;QualitySettings.shadowResolution=ShadowResolution.Medium;
        }
        void Cargo(float x,float width,float top,float travel,int index)
        {
            var wagon=Group("Nákladní vagon",new Vector3(x,0,0));
            var paint=Surfaced("Cargo paint "+index%3,"Corrugated",index%3==0?"#3F7F8F":index%3==1?"#A8584A":"#62805A",1.6f,.9f,.4f,.3f);
            Box("Container",new Vector3(0,top-1.03f,0),new Vector3(width,2,2.34f),paint,wagon);
            Box("Roof rail",new Vector3(0,top-.045f,-1.18f),new Vector3(width,.09f,.07f),steel,wagon);
            for(float i=-width*.5f+.25f;i<width*.5f;i+=.48f)
                Box("Corrugated rib",new Vector3(i,top-.99f,-1.18f),new Vector3(.11f,1.8f,.085f),paint,wagon);
            Box("Safety strip",new Vector3(0,top-1.92f,-1.23f),new Vector3(width,.07f,.032f),gold,wagon);
            Sign("ČD CARGO  /  "+(420+index),new Vector3(0,top-1,-1.26f),.043f,wagon);
            Box("Frame",new Vector3(0,top-2.13f,0),new Vector3(width,.28f,2.15f),dark,wagon);
            for(int side=-1;side<=1;side+=2)
            {
                var wheel=Box("Bogie wheel",new Vector3(side*width*.3f,top-2.42f,-.94f),new Vector3(.64f,.12f,.64f),dark,wagon,PrimitiveType.Cylinder);
                wheel.localRotation=Quaternion.Euler(90,0,0);
            }
            RaisedTrack(x,width,travel,top-2.74f);
            platforms.Add(new Platform{x=x,width=width,top=top,root=wagon,amplitude=travel,phase=x});
        }
        void Tank(float x,float width,float top,float travel,int index)
        {
            var wagon=Group("Cisterna",new Vector3(x,0,0));
            var paint=Surfaced("Tank paint "+index%2,"PaintedMetal",index%2==0?"#3A4046":"#D0CABA",2.2f,.5f,.55f,.45f);
            float length=width-1;
            var shell=Box("Tank shell",new Vector3(0,top-1.2f,0),new Vector3(2.1f,length*.5f,2.1f),paint,wagon,PrimitiveType.Cylinder);
            shell.localRotation=Quaternion.Euler(0,0,90);
            for(int side=-1;side<=1;side+=2)Box("Tank end",new Vector3(side*length*.5f,top-1.2f,0),new Vector3(.7f,2.1f,2.1f),paint,wagon,PrimitiveType.Sphere);
            Box("Dome",new Vector3(0,top-.2f,0),new Vector3(.9f,.25f,.9f),paint,wagon,PrimitiveType.Cylinder);
            Box("Walkway grating",new Vector3(0,top-.06f,0),new Vector3(width-.3f,.12f,1.05f),Surfaced("Walkway plates","MetalPlates","#5B646A",1,.7f,.4f,.5f),wagon);
            for(int side=-1;side<=1;side+=2)Box("Handrail",new Vector3(0,top+.5f,side*.55f),new Vector3(width-.6f,.05f,.05f),gold,wagon);
            for(float i=-width*.5f+.6f;i<width*.5f;i+=1.6f)Box("Handrail post",new Vector3(i,top+.25f,.55f),new Vector3(.05f,.5f,.05f),gold,wagon);
            Box("Hazard plate",new Vector3(length*.25f,top-1.25f,-1.07f),new Vector3(.75f,.5f,.03f),gold,wagon);
            Sign("ČD CARGO  /  Zacns",new Vector3(-length*.15f,top-1.25f,-1.08f),.035f,wagon);
            Box("Frame",new Vector3(0,top-2.13f,0),new Vector3(width,.28f,2.15f),dark,wagon);
            for(int side=-1;side<=1;side+=2)
            {
                var wheel=Box("Bogie wheel",new Vector3(side*width*.3f,top-2.42f,-.94f),new Vector3(.64f,.12f,.64f),dark,wagon,PrimitiveType.Cylinder);
                wheel.localRotation=Quaternion.Euler(90,0,0);
            }
            RaisedTrack(x,width,travel,top-2.74f);
            platforms.Add(new Platform{x=x,width=width,top=top,root=wagon,amplitude=travel,phase=x});
        }
        /// <summary>Wagons higher than the ground tracks stand on an embankment or a steel viaduct, never in the air.</summary>
        void RaisedTrack(float x,float width,float travel,float wheelBottom)
        {
            if(wheelBottom<-.5f)return;
            float length=width+travel*2+1.4f,railTop=wheelBottom;
            var g=Group("Raised track",new Vector3(x,0,0));
            if(railTop<1.6f)
            {
                float height=railTop-.2f+1.05f;
                Box("Embankment",new Vector3(0,-1.05f+height*.5f,0),new Vector3(length,height,3.2f),ballast,g);
                Box("Embankment shoulder",new Vector3(0,-1.05f+height*.35f,0),new Vector3(length+1.2f,height*.7f,3.9f),ballast,g);
            }
            else
            {
                var pier=Surfaced("Track pier","Concrete","#8A96A0",3,.5f,.15f);
                var plates=Surfaced("Viaduct plates","MetalPlates","#7E8C94",2,.6f,.45f,.5f);
                Box("Viaduct deck",new Vector3(0,railTop-.32f,0),new Vector3(length,.24f,2.8f),plates,g);
                for(int side=-1;side<=1;side+=2)Box("Plate girder",new Vector3(0,railTop-.62f,side*1.25f),new Vector3(length,.5f,.16f),plates,g);
                for(int side=-1;side<=1;side+=2)
                    Box("Viaduct pillar",new Vector3(side*length*.3f,(railTop-1.85f)*.5f,0),new Vector3(.7f,railTop-.8f+1.05f,1.9f),pier,g);
            }
            for(float i=-length*.5f+.3f;i<length*.5f;i+=.9f)Box("Sleeper",new Vector3(i,railTop-.16f,0),new Vector3(.25f,.12f,2.4f),Sleepers(),g);
            for(int side=-1;side<=1;side+=2)Box("Rail",new Vector3(0,railTop-.065f,side*.8f),new Vector3(length,.13f,.09f),steel,g);
        }
        void CreateVent(Platform p,int phase)
        {
            Box("Steam grate",new Vector3(0,p.top+.035f,0),new Vector3(1.25f,.07f,1.16f),dark,p.root);
            for(int i=-2;i<=2;i++)Box("Grate slot",new Vector3(i*.2f,p.top+.075f,0),new Vector3(.08f,.015f,.8f),gold,p.root);
            var warning=Box("Amber steam warning",new Vector3(0,p.top+.3f,-.64f),new Vector3(.29f,.21f,.14f),gold,p.root,PrimitiveType.Sphere);
            var plume=Group("Steam burst",new Vector3(0,p.top,0),p.root);
            var vapor=SoftSprite("Steam mist",new Color(.77f,.89f,1,.72f),true);
            for(int i=0;i<5;i++)
            {
                float width=.88f+i*.18f;
                Box("Steam mist",new Vector3(Mathf.Sin(i*4)*.22f,.45f+i*.38f,-.3f+i*.013f),
                    new Vector3(width,.99f,1),vapor,plume,PrimitiveType.Quad);
            }
            plume.gameObject.SetActive(false);
            vents.Add(new SteamVent{Platform=p,Warning=warning,Plume=plume,Phase=phase*.68f});
        }
        public void IndustrialBackdrop(bool night)
        {
            var brick=Surfaced("Depot scanned masonry","StationBrick","#FFFFFF",3.2f,.65f,.2f);
            var warm=night?Glow("Floodlit window","#FFBB66",.9f):Mat("Floodlit window","#FFBB66");
            var glow=SoftSprite("Floodlight glow",new Color(1,.59f,.25f,night?.22f:.12f),false);
            for(int x=10;x<Length-10;x+=32)
            {
                float h=night?9:11;
                Box("Maintenance hall",new Vector3(x,h/2-1,15),new Vector3(26,h,6),brick);
                Box("Hall roof",new Vector3(x,h-1,15),new Vector3(27,.36f,7),dark);
                for(int i=-9;i<=9;i+=6)
                {
                    Box("Industrial window",new Vector3(x+i,h-3.7f,11.93f),new Vector3(4,2.5f,.08f),night?warm:glass);
                    for(int j=-1;j<=1;j++)Box("Steel glazing bars",new Vector3(x+i+j,h-3.7f,11.86f),new Vector3(.065f,2.5f,.08f),dark);
                }
                Box("Portal gantry",new Vector3(x+12,5,5),new Vector3(.3f,11,.32f),steel);
                Box("Crane bridge",new Vector3(x+6,10.4f,5),new Vector3(12,.45f,.36f),gold);
                Box("Floodlight",new Vector3(x+11.5f,9.4f,4.7f),new Vector3(.85f,.32f,.5f),warm);
                Box("Diffused floodlight",new Vector3(x+11.5f,9.35f,4.42f),new Vector3(3.9f,3,1),glow,null,PrimitiveType.Quad);
            }
        }
        void PlatformStation(float x,float width,float top,string label,bool checkpoint)
        {
            var g=Group("Nástupiště",new Vector3(x,0,0));
            Box("Concrete",new Vector3(0,top-.55f,0),new Vector3(width,1.1f,3.1f),Surfaced("Concrete platform scan","CrackedConcrete","#FFFFFF",3.5f,.35f,.2f),g);
            Box("Yellow edge",new Vector3(0,top+.015f,-1.4f),new Vector3(width,.04f,.12f),gold,g);
            for(float i=-width/2;i<width/2;i+=.8f)Box("Edge marking",new Vector3(i,top-.16f,-1.57f),new Vector3(.4f,.15f,.035f),cream,g);
            Box("Station sign",new Vector3(0,top+3.6f,2),new Vector3(7,.85f,.16f),navy,g);
            Sign(label,new Vector3(0,top+3.6f,1.9f),label.Length>22?.06f:.085f,g);
            foreach(float i in new[]{-3.2f,3.2f})Box("Sign post",new Vector3(i,top+1.8f,2),new Vector3(.1f,3.6f,.1f),steel,g);
            platforms.Add(new Platform{x=x,width=width,top=top,root=g});
            if(x>0){Box("Checkpoint lamp",new Vector3(-width/2+1,top+1.2f,1),new Vector3(.15f,2.4f,.15f),gold,g);}
            Prop("bench",new Vector3(-width*.28f,top,1.05f),180,g);
            if(width>11)Prop("bench",new Vector3(width*.12f,top,1.05f),180,g);
            var lamp=Prop("lamp",new Vector3(width*.36f,top,1.15f),90,g,.9f);
            if(lamp && (Level.Sky==Sky.Evening||Level.Sky==Sky.Night||Level.Sky==Sky.Dusk))
                Box("Lamp glow",new Vector3(width*.36f-.35f,top+3.55f,.7f),new Vector3(2.4f,2.4f,1),SoftSprite("Platform lamp glow",new Color(1,.8f,.5f,.3f),false),g,PrimitiveType.Quad);
            if(checkpoint)
            {
                // Light signal: shows "Stůj" until the checkpoint is reached, then "Volno".
                var signal=Group("Návěstidlo",new Vector3(width*.5f-.5f,top,1.9f),g);
                var mastGrey=Mat("Signal mast","#8A9095",.5f,.4f);
                Cylinder("Mast",new Vector3(0,1.8f,0),.16f,3.6f,mastGrey,signal);
                Box("Head",new Vector3(0,3.35f,-.05f),new Vector3(.46f,.95f,.22f),dark,signal);
                Box("Backplate",new Vector3(0,3.35f,.04f),new Vector3(.7f,1.15f,.04f),Mat("Signal backplate","#1B1F22"),signal);
                for(int stripe=0;stripe<4;stripe++)Box("Mast stripe",new Vector3(0,.4f+stripe*.5f,0),new Vector3(.18f,.22f,.18f),stripe%2==0?cream:Mat("Signal red paint","#B8342C"),signal,PrimitiveType.Cylinder);
                foreach(float y in new[]{3.58f,3.12f})Box("Hood",new Vector3(0,y+.13f,-.2f),new Vector3(.3f,.04f,.18f),dark,signal);
                var red=Box("Stůj",new Vector3(0,3.58f,-.17f),new Vector3(.22f,.22f,.06f),Glow("Signal red","#FF3B30",4),signal,PrimitiveType.Sphere);
                var green=Box("Volno",new Vector3(0,3.12f,-.17f),new Vector3(.22f,.22f,.06f),Glow("Signal green","#38FF74",4),signal,PrimitiveType.Sphere);
                checkpointSignals.Add(new[]{red.gameObject,green.gameObject});
                green.gameObject.SetActive(false);
            }
        }
        void Train(float x,float width,float top,int variant,float moving=0)
        {
            var g=Group("Vagon "+variant,new Vector3(x,0,0));
            if(variant!=1 && ImportedTrain(g,width,top))
            {
                RaisedTrack(x,width,moving,top-3.1f);
                platforms.Add(new Platform{x=x,width=width,top=top,root=g,amplitude=moving,phase=x});
                return;
            }
            RaisedTrack(x,width,moving,top-2.52f);
            Material body=variant==2?Mat("Coach red","#A35A49",.3f,.6f):Mat("Coach blue","#2C77A6",.35f,.62f);
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
            var renderers=instance.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0){Object.Destroy(wrapper.gameObject);return false;}
            Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            Vector3 center=wrapper.InverseTransformPoint(b.center);
            wrapper.localScale=new Vector3(width/b.size.x,3.1f/b.size.y,2.5f/b.size.z);
            instance.transform.localPosition=-center;
            wrapper.localPosition=new Vector3(0,top-1.55f,0);
            return true;
        }
        public Transform Person(string name,Vector3 position,bool inspector=false,bool street=false)
        {
            var p=Group(name,position); var jacket=street?Mat("Patched coat","#6C6750"):inspector?navy:Mat("Hero jacket","#E48A3C");
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
            if(street)
            {
                Box("Wool cap",new Vector3(0,1.88f,0),new Vector3(.47f,.3f,.42f),rust,p,PrimitiveType.Sphere);
                Box("Beard",new Vector3(.03f,1.5f,-.12f),new Vector3(.34f,.26f,.21f),Mat("Grey beard","#ABABA0"),p,PrimitiveType.Sphere);
                Box("Coat patch",new Vector3(-.15f,1.07f,-.19f),new Vector3(.16f,.22f,.03f),gold,p);
                Box("Coat patch",new Vector3(.12f,.93f,-.19f),new Vector3(.19f,.14f,.03f),rust,p);
            }
            return p;
        }
        public void Background()
        {
            var rng=new System.Random(42);
            for(int i=-20;i<Length;i+=9)
            {
                if(i>-18 && i<44)continue;
                float h=4+(float)rng.NextDouble()*6;
                var wall=Mat("Facade"+(i%3),i%3==0?"#B5A494":i%3==1?"#C4BDAA":"#8C9D9B");
                Box("Hodolany building",new Vector3(i,h/2-1,17),new Vector3(8,h,6),wall);
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
            Catenary(-10,Length-15);
            for(int i=10;i<Length-20;i+=17)
            {
                if(i<44)continue;
                Box("Tree trunk",new Vector3(i,1.7f,10),new Vector3(.35f,5,.35f),rust);
                for(int n=0;n<4;n++)Box("Tree crown",new Vector3(i+Mathf.Sin(n*2)*1.1f,3.4f+n*.45f,10+Mathf.Cos(n*2)*.65f),new Vector3(2.6f,3.2f,2.5f),Leaves("Station leaves","#4E715C"),null,PrimitiveType.Sphere);
            }
            // Backdrop trains create depth without adding gameplay collisions.
            for(int i=12;i<Length-30;i+=28)
            {Box("Parked train",new Vector3(i,1.2f,8),new Vector3(20,2.4f,2.3f),navy);Box("Parked stripe",new Vector3(i,1.7f,6.8f),new Vector3(20,.65f,.05f),cream);}
        }
        public void Catenary(float from,float to)
        {
            for(float i=from;i<to;i+=14)
            {
                Box("Catenary mast",new Vector3(i,3.4f,5),new Vector3(.14f,8.5f,.14f),steel);
                Box("Crossbar",new Vector3(i,7.3f,2),new Vector3(.16f,.12f,6),steel);
                Box("Wire",new Vector3(i+7,7.1f,0),new Vector3(14,.022f,.022f),dark);
                Box("Lamp",new Vector3(i,6,3),new Vector3(.7f,.12f,.4f),cream);
            }
        }
        public Transform Cylinder(string name,Vector3 pos,float diameter,float height,Material material,Transform parent=null)=>
            Box(name,pos,new Vector3(diameter,height*.5f,diameter),material,parent,PrimitiveType.Cylinder);
        /// <summary>Four-sided spire with its base at pos. One shared mesh per world.</summary>
        public Transform Spire(string name,Vector3 pos,float width,float height,Material material,Transform parent=null)
        {
            if(!pyramid)
            {
                pyramid=new Mesh{name="Spire"};
                var v=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f),new Vector3(0,1,0)};
                var verts=new List<Vector3>();var tris=new List<int>();
                int[][] faces={new[]{0,4,1},new[]{1,4,2},new[]{2,4,3},new[]{3,4,0},new[]{0,1,2},new[]{0,2,3}};
                foreach(var f in faces){foreach(int k in f){tris.Add(verts.Count);verts.Add(v[k]);}}
                pyramid.SetVertices(verts);pyramid.SetTriangles(tris,0);pyramid.RecalculateNormals();pyramid.RecalculateBounds();
                generatedMeshes.Add(pyramid);
            }
            var t=Group(name,pos,parent);t.localScale=new Vector3(width,height,width);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=pyramid;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            return t;
        }
        /// <summary>Runtime facade texture: a grid of windows, some of them lit. Tiles once per facade.</summary>
        public Material Facade(string name,Color wall,Color window,Color lit,int columns,int rows,float litRatio,int seed)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            const int cell=32;
            var texture=new Texture2D(columns*cell,rows*cell,TextureFormat.RGBA32,true){name=name,wrapMode=TextureWrapMode.Repeat,anisoLevel=4};
            var rng=new System.Random(seed);var pixels=new Color32[texture.width*texture.height];
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                bool on=rng.NextDouble()<litRatio;
                Color glass=on?lit:window*(.8f+(float)rng.NextDouble()*.4f);
                for(int y=0;y<cell;y++)for(int x=0;x<cell;x++)
                {
                    bool inWindow=x>5&&x<cell-5&&y>7&&y<cell-5;
                    bool sill=y>=4&&y<=7&&x>3&&x<cell-3;
                    Color c=inWindow?glass:sill?wall*.82f:wall*(.96f+((x*7+y*13)%5)*.01f);
                    if(inWindow&&x==cell/2)c=wall*.9f;
                    pixels[(row*cell+y)*texture.width+col*cell+x]=c;
                }
            }
            texture.SetPixels32(pixels);texture.Apply(true,true);generatedTextures.Add(texture);
            var material=Mat(name,"#FFFFFF",smooth:.18f);material.mainTexture=texture;
            if(litRatio>.15f)
            {
                // Lit windows only: the emission map glows after dark.
                var glow=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,true){name=name+" glow",wrapMode=TextureWrapMode.Repeat};
                var emissive=new Color32[pixels.Length];Color32 litColor=lit;
                for(int i=0;i<pixels.Length;i++)emissive[i]=pixels[i].Equals(litColor)?pixels[i]:new Color32(0,0,0,255);
                glow.SetPixels32(emissive);glow.Apply(true,true);generatedTextures.Add(glow);
                material.SetTexture("_EmissionMap",glow);material.SetFloat("_EmissionStrength",.85f);
            }
            return material;
        }
        /// <summary>Instantiates a Sketchfab model from Resources/Models/Props (null when missing).</summary>
        public Transform Prop(string name,Vector3 position,float yaw,Transform parent=null,float scale=1)
        {
            if(!props.TryGetValue(name,out var prefab)){prefab=Resources.Load<GameObject>("Models/Props/"+name);props[name]=prefab;}
            if(!prefab)return null;
            var t=Object.Instantiate(prefab,parent?parent:root,false).transform;
            t.name=name+" / Sketchfab CC BY";t.localPosition=position;t.localRotation=Quaternion.Euler(0,yaw,0);t.localScale=Vector3.one*scale;
            return t;
        }
        /// <summary>Background traffic that loops along x; purely visual.</summary>
        public void AddMover(Transform body,float from,float to,float speed,float phase){if(body)movers.Add(new Mover{body=body,from=from,span=to-from,speed=speed,phase=phase});}
        public void SetSignal(int checkpoint,bool clear)
        {
            if(checkpoint<0||checkpoint>=checkpointSignals.Count)return;
            checkpointSignals[checkpoint][0].SetActive(!clear);checkpointSignals[checkpoint][1].SetActive(clear);
        }
        public void ResetSignals(){for(int i=0;i<checkpointSignals.Count;i++)SetSignal(i,false);}
        public void AddWindProp(Transform prop)=>windProps.Add(prop);
        public void AddWindStreak(Transform streak)=>windStreaks.Add(streak);
        /// <summary>Flags lean and streaks blow while the gust pushes; purely visual.</summary>
        public void Weather(float wind,bool warning,float time,float focusX)
        {
            float strength=Mathf.Clamp01(-wind/3);
            for(int i=0;i<windProps.Count;i++)
                windProps[i].localRotation=Quaternion.Euler(0,0,Mathf.Lerp(78,8,strength)+Mathf.Sin(time*(6+strength*10)+i)*(4+strength*9));
            for(int i=0;i<windStreaks.Count;i++)
            {
                var streak=windStreaks[i];
                bool on=strength>.05f;
                if(streak.gameObject.activeSelf!=on)streak.gameObject.SetActive(on);
                if(!on)continue;
                float span=34,phase=Mathf.Repeat(time*22+i*7.3f,span);
                var p=streak.position;streak.position=new Vector3(focusX+span*.5f-phase,p.y,p.z);
                streak.localScale=new Vector3(2.4f+strength*3,.05f,1);
            }
        }
        /// <summary>Places moving wagons between the last two fixed steps for smooth high-refresh rendering.</summary>
        public void Interpolate(float alpha)
        {
            foreach(var p in platforms)if(p.amplitude!=0)p.root.position=new Vector3(p.x+Mathf.Lerp(p.previous,p.offset,alpha),0,0);
        }
        public void Tick(float time,float dt)
        {
            foreach(var m in movers){var p=m.body.position;m.body.position=new Vector3(m.from+Mathf.Repeat(time*m.speed+m.phase,m.span),p.y,p.z);}
            foreach(var vent in vents)
            {
                float phase=(time+vent.Phase)%Level.VentPeriod;
                vent.Active=phase>=1.15f && phase<2.13f;
                vent.Warning.gameObject.SetActive(phase<1.15f && Mathf.Sin(time*26)>0);
                vent.Plume.gameObject.SetActive(vent.Active);
                if(vent.Active)vent.Plume.localScale=new Vector3(1,.65f+.35f*Mathf.Sin((phase-1.15f)*3.2f),1);
            }
            foreach(var p in platforms){float v=Mathf.Sin(time*.8f+p.phase)*p.amplitude;p.previous=p.offset;p.delta=v-p.offset;p.offset=v;p.root.position=new Vector3(p.x+v,0,0);}
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
