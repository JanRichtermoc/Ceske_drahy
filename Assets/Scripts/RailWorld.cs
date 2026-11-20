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
        /// <summary>Ticket rest positions local to their roof root.</summary>
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
        Material ballast, railSteel;
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
            if(signMaterial){Font.textureRebuilt-=OnFontRebuilt;Object.Destroy(signMaterial);}
        }
        /// <summary>The dynamic font atlas can be replaced when it grows (also by the HUD); keep the sign material on it.</summary>
        void OnFontRebuilt(Font font){if(font==signFont && signMaterial)signMaterial.mainTexture=font.material.mainTexture;}
        public void AttachMesh(string name,Transform parent,Mesh mesh,Material material)
        {
            var visual=Group(name,Vector3.zero,parent);
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            visual.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            generatedMeshes.Add(mesh);
        }
        static Texture2D Tex(string key,string map)=>Resources.Load<Texture2D>("Textures/PH/"+key+"_"+map);
        /// <summary>
        /// Points a material at a Poly Haven set (albedo, normal, ORM) projected in world metres. A non-white hex is the
        /// target average colour: the tint is divided by the texture's mean luminance, overflow goes to the boost.
        /// </summary>
        static void ApplySet(Material m,bool top,string key,string hex,float bump,float tileScale)
        {
            string tex=top?"_TopTex":"_MainTex",nrm=top?"_TopBump":"_BumpMap",orm=top?"_TopORM":"_ORMMap";
            m.SetTexture(tex,Tex(key,"A"));m.SetTexture(nrm,Tex(key,"N"));m.SetTexture(orm,Tex(key,"ORM"));
            m.SetFloat(top?"_UseTopORM":"_UseORM",1);
            m.SetFloat(top?"_TopScale":"_TriScale",1/(RailTextures.Tile(key)*tileScale));
            m.SetFloat(top?"_TopBumpScale":"_BumpScale",bump);
            ColorUtility.TryParseHtmlString(hex,out var target);
            Color tint=Color.white;float boost=1;
            if(target!=Color.white)
            {
                float mean=RailTextures.MeanLuminance(key);
                var linear=target.linear;
                tint=new Color(linear.r/mean,linear.g/mean,linear.b/mean);
                boost=Mathf.Max(1,Mathf.Max(tint.r,Mathf.Max(tint.g,tint.b)));
                tint=new Color(tint.r/boost,tint.g/boost,tint.b/boost).gamma;
            }
            m.SetColor(top?"_TopColor":"_Color",tint);
            if(!top)m.SetFloat("_ColorBoost",boost);
        }
        /// <summary>Photographic PBR surface in world metres; hex (when not white) is the target average colour.</summary>
        public Material Surfaced(string name,string key,string hex="#FFFFFF",float bump=1,float smoothScale=1,float metal=0,float tileScale=1)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            var material=Mat(name,"#FFFFFF",metal,.25f);
            ApplySet(material,false,key,hex,bump,tileScale);
            material.SetFloat("_SmoothScale",smoothScale);
            material.SetFloat("_Detail",.1f);
            material.EnableKeyword("_TRIPLANAR");
            return material;
        }
        /// <summary>Two sets in one material: one on upward faces (e.g. paving), the other on the sides (e.g. concrete).</summary>
        public Material SurfacedTop(string name,string sideKey,string sideHex,string topKey,string topHex,float bump=1)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            var material=Mat(name,"#FFFFFF",0,.25f);
            ApplySet(material,false,sideKey,sideHex,bump,1);
            ApplySet(material,true,topKey,topHex,bump,1);
            material.SetFloat("_Detail",.1f);
            material.EnableKeyword("_TRIPLANAR_TOP");
            return material;
        }
        /// <summary>Printed pickup (ticket, medkit…): a Resources/Textures/Pickups image on every face, with a little glow so it pops.</summary>
        public Material Printed(string name,string texture,float glow=.15f,float smooth=.35f)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            var material=Mat(name,"#FFFFFF",0,smooth);
            material.mainTexture=Resources.Load<Texture2D>("Textures/Pickups/"+texture);
            material.SetFloat("_Detail",0);material.SetFloat("_Macro",0);
            material.SetTexture("_EmissionMap",material.mainTexture);material.SetFloat("_EmissionStrength",glow);
            return material;
        }
        /// <summary>Flat paint over a set's relief (normal map only): containers, tanks, rolling stock.</summary>
        public Material Painted(string name,string reliefKey,string hex,float bump=.7f,float smooth=.4f,float metal=.2f)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            var material=Mat(name,hex,metal,smooth);
            material.SetTexture("_MainTex",Texture2D.whiteTexture);
            material.SetTexture("_BumpMap",Tex(reliefKey,"N"));
            material.SetFloat("_BumpScale",bump);
            material.SetFloat("_TriScale",1/RailTextures.Tile(reliefKey));
            material.SetFloat("_UseORM",0);
            material.SetFloat("_Detail",.25f);
            material.EnableKeyword("_TRIPLANAR");
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
        Material Sleepers()=>Surfaced("Concrete sleepers","Concrete","#8C8880",.8f);
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
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;generatedMeshes.Add(mesh);return t;
        }
        Font signFont;
        Material signMaterial;
        /// <summary>Lettering in Barlow Condensed Bold on the lit, fogged Railway/WorldText shader.</summary>
        public Transform Sign(string text, Vector3 pos, float size, Transform parent = null, Color? color = null)
        {
            if (!signMaterial)
            {
                signFont = Resources.Load<Font>("Fonts/BarlowCondensed-Bold");
                signMaterial = new Material(Shader.Find("Railway/WorldText")) { name = "World text", mainTexture = signFont.material.mainTexture };
                Font.textureRebuilt += OnFontRebuilt;
            }
            var t = Group(text, pos, parent); var m = t.gameObject.AddComponent<TextMesh>();
            // A larger glyph size keeps the atlas sharp; characterSize scales back to the old 64-pixel layout.
            m.font = signFont; m.text = text; m.fontSize = 96; m.characterSize = size * 64 / 96; m.anchor = TextAnchor.MiddleCenter;
            m.color = color ?? new Color(.91f,.94f,.9f);
            var renderer = t.GetComponent<MeshRenderer>(); renderer.sharedMaterial = signMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
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
            ballast=Surfaced("Track ballast","Ballast","#FFFFFF",1.2f);
            var paving=Surfaced("Forecourt paving","Paving","#FFFFFF",1);
            railSteel=Surfaced("Rail steel","RailSteel","#6B5B50",1,1.2f,1);
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
                for(int side=-1;side<=1;side+=2) Box("Rail",new Vector3((Length-18)*.5f,-.77f,z+side*.8f),new Vector3(Length+18,.13f,.09f),railSteel);
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
                    // Tickets ride their roof, so they stay above a moving wagon.
                    var t=Box("Jízdenka",new Vector3(x-p.x,p.top+1.25f,0),new Vector3(.52f,.33f,.025f),Printed("Ticket","Ticket",.3f),p.root);
                    tickets.Add(t);ticketOrigins.Add(new Vector2(x-p.x,p.top+1.25f));
                }
            }
            foreach(float x in Level.Birds)
            {
                float height=Level.Theme==Theme.MainStation?5.2f:Surface(x,3.6f)+1.95f;
                var b=Group("Holub",new Vector3(x,height,0));
                birds.Add(b);birdOrigins.Add(new Vector2(x,height));
                if(Pigeon(b))continue;
                Box("Body",Vector3.zero,new Vector3(.55f,.3f,.33f),steel,b,PrimitiveType.Sphere);
                Box("Head",new Vector3(-.3f,.13f,0),Vector3.one*.24f,navy,b,PrimitiveType.Sphere);
                Box("Beak",new Vector3(-.45f,.12f,0),new Vector3(.17f,.07f,.1f),gold,b);
                Box("Left wing",new Vector3(0,.08f,-.3f),new Vector3(.55f,.065f,.58f),dark,b);
                Box("Right wing",new Vector3(0,.08f,.3f),new Vector3(.55f,.065f,.58f),dark,b);
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
            public float sunStrength,sunPitch,sunIntensity,fillIntensity,fogEnd,sunHeight=.21f,clouds=.4f,stars,rim=.35f,night;
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
                    sunHeight=.55f,clouds=.45f,grade=Grade(1.04f,1.08f,.35f,1.3f,.22f,new Color(0,.01f,.02f),Color.white)};
                case Sky.Dusk:return new Lighting{horizon=new Color(.85f,.5f,.38f),zenith=new Color(.2f,.27f,.45f),sunStrength=.8f,sunPitch=18,sun=new Color(1,.62f,.42f),sunIntensity=1,
                    fill=new Color(.5f,.6f,1),fillIntensity=.35f,ambientSky=new Color(.42f,.44f,.6f),ambientEquator=new Color(.45f,.38f,.4f),fog=new Color(.58f,.5f,.55f),fogEnd=95,
                    sunHeight=.12f,clouds=.55f,rim=.45f,night=.35f,grade=Grade(1.15f,1.05f,.6f,.95f,.3f,new Color(.03f,0,.03f),new Color(1.06f,.95f,.92f))};
                case Sky.Sunset:return new Lighting{horizon=new Color(.98f,.52f,.22f),zenith=new Color(.25f,.32f,.55f),sunStrength=1.4f,sunPitch=10,sun=new Color(1,.58f,.3f),sunIntensity=1.15f,
                    fill=new Color(.45f,.55f,1),fillIntensity=.3f,ambientSky=new Color(.5f,.48f,.58f),ambientEquator=new Color(.6f,.45f,.38f),fog=new Color(.8f,.55f,.42f),fogEnd=100,
                    sunHeight=.07f,clouds=.5f,rim=.5f,night=.25f,grade=Grade(1.12f,1.04f,.7f,.9f,.3f,new Color(.02f,.01f,0),new Color(1.04f,.98f,.93f))};
                case Sky.Evening:return new Lighting{horizon=new Color(.36f,.33f,.5f),zenith=new Color(.06f,.09f,.22f),sunStrength=.25f,sunPitch=25,sun=new Color(.85f,.75f,.9f),sunIntensity=.7f,
                    fill=new Color(1,.6f,.4f),fillIntensity=.45f,ambientSky=new Color(.3f,.33f,.48f),ambientEquator=new Color(.32f,.3f,.38f),fog=new Color(.3f,.3f,.42f),fogEnd=85,
                    sunHeight=.04f,clouds=.35f,stars=.4f,rim=.55f,night=.8f,grade=Grade(1.2f,1,.55f,.95f,.34f,new Color(0,.01f,.04f),new Color(.99f,.99f,1.02f))};
                case Sky.Night:return new Lighting{horizon=new Color(.13f,.22f,.34f),zenith=new Color(.018f,.045f,.13f),sunStrength=.09f,sunPitch=43,sun=new Color(.65f,.8f,1),sunIntensity=.88f,
                    fill=new Color(1,.56f,.34f),fillIntensity=.53f,ambientSky=new Color(.28f,.36f,.51f),ambientEquator=new Color(.25f,.3f,.42f),fog=new Color(.23f,.32f,.48f),fogEnd=76,
                    sunHeight=.3f,clouds=.2f,stars=1,rim=.6f,night=1,grade=Grade(1.22f,.95f,.5f,1.05f,.38f,new Color(0,.015f,.05f),new Color(.95f,1,1.08f))};
                default:return new Lighting{horizon=new Color(.78f,.68f,.51f),zenith=new Color(.24f,.44f,.56f),sunStrength=1,sunPitch=32,sun=new Color(1,.82f,.64f),sunIntensity=1.2f,
                    fill=new Color(.61f,.79f,1),fillIntensity=.25f,ambientSky=new Color(.53f,.62f,.72f),ambientEquator=new Color(.47f,.51f,.52f),fog=new Color(.66f,.69f,.72f),fogEnd=105,
                    grade=Grade(1.02f,1.06f,.42f,1.25f,.26f,new Color(.02f,.01f,0),new Color(1.03f,1,.95f))};
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
            // Ambient and default reflections come from this route's sky, so metal and glass reflect it.
            RenderSettings.defaultReflectionMode=DefaultReflectionMode.Skybox;
            DynamicGI.UpdateEnvironment();
            // Sky-only reflection probe, rendered once: glass, metal and water reflect this route's sky.
            var probe=Group("Sky reflection",new Vector3(Length*.5f-10,20,20)).gameObject.AddComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.NoTimeSlicing;probe.resolution=128;probe.cullingMask=0;
            probe.clearFlags=ReflectionProbeClearFlags.Skybox;probe.size=new Vector3(Length+100,140,220);probe.hdr=true;
            probe.RenderProbe();
            Shader.SetGlobalFloat("_RailNight",preset.night);
            Shader.SetGlobalVector("_RailRim",new Vector4(preset.fill.r,preset.fill.g,preset.fill.b,preset.rim));
            Shader.SetGlobalVector("_RailSkyHorizon",preset.horizon);
            Grading=preset.grade;
            var sun=Group("Main light",Vector3.zero).gameObject.AddComponent<Light>();
            sun.type=LightType.Directional;
            // From the front-left, so faces facing the camera and their sides read differently.
            sun.transform.rotation=Quaternion.Euler(preset.sunPitch,-55,0);
            sun.color=preset.sun;
            sun.intensity=preset.sunIntensity;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.8f;sun.shadowBias=.04f;sun.shadowNormalBias=.3f;
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
            // The camera sits 14 m from the play plane (it used to be 23 m): fog distances shift with it.
            RenderSettings.fogStartDistance=19;RenderSettings.fogEndDistance=preset.fogEnd-9;
            // One sharp cascade covers the play plane and the near backdrop.
            QualitySettings.shadowDistance=50;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades=1;QualitySettings.shadowProjection=ShadowProjection.StableFit;
        }
        void Cargo(float x,float width,float top,float travel,int index)
        {
            var wagon=Group("Nákladní vagon",new Vector3(x,0,0));
            var paint=Painted("Cargo paint "+index%3,"Corrugated",index%3==0?"#3F7F8F":index%3==1?"#A8584A":"#62805A",.9f,.35f,.3f);
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
            var paint=Painted("Tank paint "+index%2,"MetalPlate",index%2==0?"#3A4046":"#D0CABA",.35f,.5f,.4f);
            float length=width-1;
            var shell=Box("Tank shell",new Vector3(0,top-1.2f,0),new Vector3(2.1f,length*.5f,2.1f),paint,wagon,PrimitiveType.Cylinder);
            shell.localRotation=Quaternion.Euler(0,0,90);
            for(int side=-1;side<=1;side+=2)Box("Tank end",new Vector3(side*length*.5f,top-1.2f,0),new Vector3(.7f,2.1f,2.1f),paint,wagon,PrimitiveType.Sphere);
            Box("Dome",new Vector3(0,top-.2f,0),new Vector3(.9f,.25f,.9f),paint,wagon,PrimitiveType.Cylinder);
            Box("Walkway grating",new Vector3(0,top-.06f,0),new Vector3(width-.3f,.12f,1.05f),Surfaced("Walkway plates","TreadPlate","#FFFFFF",1,1,1),wagon);
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
                var pier=Surfaced("Track pier","Concrete","#9AA0A4");
                var plates=Painted("Viaduct plates","MetalPlate","#55626A",.8f,.35f,.5f);
                Box("Viaduct deck",new Vector3(0,railTop-.32f,0),new Vector3(length,.24f,2.8f),plates,g);
                for(int side=-1;side<=1;side+=2)Box("Plate girder",new Vector3(0,railTop-.62f,side*1.25f),new Vector3(length,.5f,.16f),plates,g);
                for(int side=-1;side<=1;side+=2)
                    Box("Viaduct pillar",new Vector3(side*length*.3f,(railTop-1.85f)*.5f,0),new Vector3(.7f,railTop-.8f+1.05f,1.9f),pier,g);
            }
            for(float i=-length*.5f+.3f;i<length*.5f;i+=.9f)Box("Sleeper",new Vector3(i,railTop-.16f,0),new Vector3(.25f,.12f,2.4f),Sleepers(),g);
            for(int side=-1;side<=1;side+=2)Box("Rail",new Vector3(0,railTop-.065f,side*.8f),new Vector3(length,.13f,.09f),railSteel,g);
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
            // Brick hall with steel-framed industrial windows drawn by the facade shader; lit inside at night.
            var brick=Facade("Depot hall","Brick","#FFFFFF",new Color(.2f,.24f,.26f),new Color(.22f,.3f,.36f),new Color(1,.73f,.4f),
                6,night?6.6f:7.4f,.68f,.42f,night?.85f:.3f,WindowStyle.Industrial,11,1);
            var warm=night?Glow("Floodlit window","#FFBB66",.9f):Mat("Floodlit window","#FFBB66");
            var glow=SoftSprite("Floodlight glow",new Color(1,.59f,.25f,night?.22f:.12f),false);
            for(int x=10;x<Length-10;x+=32)
            {
                float h=night?9:11;
                Box("Maintenance hall",new Vector3(x,h/2-1,15),new Vector3(26,h,6),brick);
                Box("Hall roof",new Vector3(x,h-1,15),new Vector3(27,.36f,7),dark);
                Box("Portal gantry",new Vector3(x+12,5,5),new Vector3(.3f,11,.32f),steel);
                Box("Crane bridge",new Vector3(x+6,10.4f,5),new Vector3(12,.45f,.36f),gold);
                Box("Floodlight",new Vector3(x+11.5f,9.4f,4.7f),new Vector3(.85f,.32f,.5f),warm);
                Box("Diffused floodlight",new Vector3(x+11.5f,9.35f,4.42f),new Vector3(3.9f,3,1),glow,null,PrimitiveType.Quad);
            }
        }
        void PlatformStation(float x,float width,float top,string label,bool checkpoint)
        {
            var g=Group("Nástupiště",new Vector3(x,0,0));
            Box("Concrete",new Vector3(0,top-.55f,0),new Vector3(width,1.1f,3.1f),SurfacedTop("Station platform","Concrete","#9C9890","Paving","#FFFFFF"),g);
            Box("Yellow edge",new Vector3(0,top+.015f,-1.4f),new Vector3(width,.04f,.12f),gold,g);
            for(float i=-width/2;i<width/2;i+=.8f)Box("Edge marking",new Vector3(i,top-.16f,-1.57f),new Vector3(.4f,.15f,.035f),cream,g);
            Box("Station sign",new Vector3(0,top+3.6f,2),new Vector3(7,.85f,.16f),navy,g);
            Sign(label,new Vector3(0,top+3.6f,1.9f),label.Length>22?.06f:.085f,g);
            foreach(float i in new[]{-3.2f,3.2f})Box("Sign post",new Vector3(i,top+1.8f,2),new Vector3(.1f,3.6f,.1f),steel,g);
            platforms.Add(new Platform{x=x,width=width,top=top,root=g});
            if(x>0){Box("Checkpoint lamp",new Vector3(-width/2+1,top+1.2f,1),new Vector3(.15f,2.4f,.15f),gold,g);}
            Prop("bench",new Vector3(-width*.28f,top,1.05f),180,g);
            if(width>11)Prop("bench",new Vector3(width*.12f,top,1.05f),180,g);
            StreetLight(new Vector3(width*.36f,top,1.15f),Level.Sky==Sky.Evening||Level.Sky==Sky.Night||Level.Sky==Sky.Dusk,g);
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
        /// <summary>
        /// Passenger stock built to the exact roof length: variant 1 is a blue ČD coach on two bogies, the others the
        /// red and cream ČD 810 railbus on two single axles. The walkable roof is at top; windows come from the facade
        /// shader on the window band, so they stay crisp and light up after dark.
        /// </summary>
        void Train(float x,float width,float top,int variant,float moving=0)
        {
            var g=Group(variant==1?"Vůz Bdt":"Motorový vůz 810",new Vector3(x,0,0));
            bool coach=variant==1;
            const float depth=2.3f,wheelRadius=.43f,nominal=3.25f;
            float half=width/2;
            // Wheels stand on the ground tracks (rail head at -0.705) or on a raised track; the lower body absorbs
            // the difference between the roof height and the rails, the window band keeps its size.
            float railTop=top-nominal<-.5f?-.705f:top-nominal;
            RaisedTrack(x,width,moving,railTop);
            float lowerHeight=Mathf.Max(.45f,1.1f+(top-railTop-nominal));
            float lowerY=top-1.41f-lowerHeight/2,wheelY=railTop+wheelRadius,frameY=wheelY+.2f;
            var lower=coach?Painted("Coach blue","MetalPlate","#1F5F9E",.2f,.55f,.25f):Painted("Railbus red","MetalPlate","#B5322E",.2f,.55f,.25f);
            var upper=coach?Painted("Coach blue","MetalPlate","#1F5F9E",.2f,.55f,.25f):Painted("Railbus cream","MetalPlate","#E9DDBF",.2f,.5f,.2f);
            var band=coach?PaintedWindows("Coach windows","#DDE5EA",new Color(.62f,.66f,.7f),new Color(.16f,.24f,.3f),new Color(1,.9f,.68f),1.45f,.8f,.8f,.84f,.85f,variant)
                          :PaintedWindows("Railbus windows","#E9DDBF",new Color(.58f,.2f,.17f),new Color(.15f,.22f,.27f),new Color(1,.9f,.68f),1.3f,.8f,.78f,.86f,.85f,variant);
            var roofGrey=Painted("Rolling stock roof","MetalPlate","#8C9196",.35f,.45f,.4f);
            var frame=Painted("Underframe","MetalPlate","#24282B",.4f,.35f,.5f);
            Box("Lower body",new Vector3(0,lowerY,0),new Vector3(width,lowerHeight,depth),lower,g);
            Box("Window band",new Vector3(0,top-.98f,0),new Vector3(width,.86f,depth),band,g);
            Box("Cant rail",new Vector3(0,top-.4f,0),new Vector3(width,.3f,depth),upper,g);
            Roof("Arched roof",new Vector3(0,top-.21f,0),width,depth,roofGrey,g);
            Box("Roof base",new Vector3(0,top-.245f,0),new Vector3(width,.07f,depth),roofGrey,g);
            Box("Waist line",new Vector3(0,top-1.43f,-depth/2-.012f),new Vector3(width,.06f,.03f),coach?cream:Mat("Railbus trim","#7E1D1A",.3f,.5f),g);
            Box("Underframe",new Vector3(0,frameY,0),new Vector3(width-.8f,.24f,depth-.4f),frame,g);
            // Doors near both ends on the visible side, with a window and steps.
            foreach(float side in new[]{-1f,1f})
            {
                float dx=side*(half-(coach?.75f:1.1f));
                float doorBottom=top-1.41f-lowerHeight+.08f,doorTop=top-.62f;
                Box("Door",new Vector3(dx,(doorBottom+doorTop)/2,-depth/2-.015f),new Vector3(.8f,doorTop-doorBottom,.03f),coach?Mat("Coach door","#174B7E",.3f,.5f):Mat("Railbus door","#9C2A26",.3f,.5f),g);
                Box("Door window",new Vector3(dx,top-1.05f,-depth/2-.03f),new Vector3(.42f,.62f,.02f),glass,g);
                Box("Door handle",new Vector3(dx+side*-.28f,doorBottom+.75f,-depth/2-.04f),new Vector3(.03f,.25f,.03f),steel,g);
                Box("Step",new Vector3(dx,doorBottom-.12f,-depth/2-.12f),new Vector3(.8f,.06f,.25f),frame,g);
                // Buffers and a coupling at each end.
                foreach(float z in new[]{-.85f,.85f})
                {var buffer=Box("Buffer",new Vector3(side*(half+.12f),frameY+.15f,z),new Vector3(.38f,.24f,.38f),steel,g,PrimitiveType.Cylinder);buffer.localRotation=Quaternion.Euler(0,0,90);}
                foreach(float z in new[]{-.75f,.75f})
                    Box("Headlight",new Vector3(side*(half+.005f),top-1.41f-lowerHeight*.45f,z),new Vector3(.02f,.18f,.24f),Glow("Headlight","#FFF2C8",1.6f),g);
            }
            // Running gear: two bogies with two axles for the coach, two single axles with springs for the railbus.
            var wheel=Mat("Wheel steel","#2E3236",.6f,.45f);
            float[] axles=coach?new[]{-half+1.5f,-half+3.3f,half-3.3f,half-1.5f}:new[]{-width*.3f,width*.3f};
            foreach(float ax in axles)
            {
                var t=Box("Wheel",new Vector3(ax,wheelY,-depth/2+.15f),new Vector3(wheelRadius*2,.06f,wheelRadius*2),wheel,g,PrimitiveType.Cylinder);t.localRotation=Quaternion.Euler(90,0,0);
                Box("Axle box",new Vector3(ax,wheelY+.07f,-depth/2+.02f),new Vector3(.32f,.26f,.1f),frame,g);
                if(!coach)Box("Leaf spring",new Vector3(ax,wheelY+.24f,-depth/2+.05f),new Vector3(1.3f,.1f,.08f),frame,g);
            }
            if(coach)foreach(float bx in new[]{-half+2.4f,half-2.4f})Box("Bogie frame",new Vector3(bx,wheelY+.1f,-depth/2+.1f),new Vector3(2.6f,.28f,.12f),frame,g);
            else Box("Exhaust",new Vector3(half*.4f,top+.12f,.3f),new Vector3(.18f,.35f,.18f),frame,g,PrimitiveType.Cylinder);
            // Lettering.
            float letterY=top-1.41f-lowerHeight*.45f;
            if(coach){Sign("2",new Vector3(-half+1.4f,top-1.05f,-depth/2-.03f),.06f,g);Sign("ČESKÉ DRÁHY",new Vector3(0,letterY,-depth/2-.02f),.034f,g);}
            else{Sign("ČD",new Vector3(0,letterY,-depth/2-.02f),.07f,g,new Color(.96f,.92f,.8f));Sign("810 "+(400+(int)Mathf.Abs(x)%200).ToString("000")+"-"+(int)Mathf.Abs(x)%9,new Vector3(-half+1.8f,letterY-.3f,-depth/2-.02f),.02f,g,new Color(.96f,.92f,.8f));}
            platforms.Add(new Platform{x=x,width=width,top=top,root=g,amplitude=moving,phase=x});
        }
        readonly Dictionary<Transform,RailActor> actors=new Dictionary<Transform,RailActor>();
        /// <summary>The animated character standing at a Person root, or null for the primitive fallback.</summary>
        public RailActor ActorOf(Transform person)=>person && actors.TryGetValue(person,out var actor)?actor:null;
        public void PauseActors(bool paused){foreach(var actor in actors.Values)actor.SetPaused(paused);}
        static GameObject pigeonPrefab;
        /// <summary>Flying pigeon model (Sketchfab CC BY) with its looping flap; false when the model is missing.</summary>
        bool Pigeon(Transform bird)
        {
            if(!pigeonPrefab)pigeonPrefab=Resources.Load<GameObject>("Models/Animals/Holub/Holub");
            if(!pigeonPrefab)return false;
            var model=Object.Instantiate(pigeonPrefab,bird,false);
            model.name="Holub / Sketchfab CC BY";
            // A real pigeon is ~0.4 m; shown 1.6× so the hazard reads at gameplay zoom (matches its hitbox).
            model.transform.localPosition=new Vector3(0,-.12f,0);model.transform.localScale=Vector3.one*1.6f;
            RailMaterials.DressCharacter(model,"Models/Animals/Holub/Textures/");
            var animation=model.GetComponent<Animation>();
            if(animation&&animation.clip){animation.wrapMode=WrapMode.Loop;animation.Play();animation[animation.clip.name].normalizedTime=Random.value;}
            return true;
        }
        public Transform Person(string name,Vector3 position,bool inspector=false,bool street=false)
        {
            var p=Group(name,position);
            // Animated models first (the alchemist's dark suit is brightened a little to read at night).
            var actor=inspector?RailActor.Create("Revizor",p):street?RailActor.Create("Alchymista",p,1.5f):null;
            if(actor!=null){actors[p]=actor;return p;}
            var jacket=street?Mat("Patched coat","#6C6750"):inspector?navy:Mat("Hero jacket","#E48A3C");
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
                if(TreeModel(new Vector3(i,-.8f,10),6.5f,Color.white,false))continue;
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
                var uvs=new List<Vector2>();foreach(var p in verts)uvs.Add(new Vector2(p.x+.5f,p.z+.5f));
                pyramid.SetVertices(verts);pyramid.SetUVs(0,uvs);pyramid.SetTriangles(tris,0);pyramid.RecalculateNormals();
                // Tangents let the triplanar normal maps light the roof faces correctly.
                pyramid.RecalculateTangents();pyramid.RecalculateBounds();
                generatedMeshes.Add(pyramid);
            }
            var t=Group(name,pos,parent);t.localScale=new Vector3(width,height,width);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=pyramid;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            return t;
        }
        public enum WindowStyle { Panel, OldTown, Industrial, Ribbon, Train }
        /// <summary>
        /// Building wall with a procedural window grid drawn by the shader: modules in metres, whole windows centred on
        /// each vertical face, some lit after dark. Crisp at any size and needs no generated textures.
        /// </summary>
        public Material Facade(string name,string wallKey,string wallHex,Color frame,Color glass,Color lit,float moduleWidth,float moduleHeight,
            float windowWidth,float windowHeight,float litRatio,WindowStyle style,int seed,float margin=.8f)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            var material=Mat(name,"#FFFFFF",0,.2f);
            ApplySet(material,false,wallKey,wallHex,.8f,1);
            material.SetFloat("_Detail",.12f);
            material.EnableKeyword("_FACADE");
            material.SetVector("_FacadeModule",new Vector4(moduleWidth,moduleHeight,windowWidth,windowHeight));
            material.SetVector("_FacadeParams",new Vector4(litRatio,(int)style,seed,margin));
            material.SetColor("_FacadeFrame",frame);material.SetColor("_FacadeGlass",glass);material.SetColor("_FacadeLitColor",lit);
            return material;
        }
        /// <summary>Painted metal wall with shader-drawn windows (rolling stock window bands; module height a bit under the band).</summary>
        public Material PaintedWindows(string name,string hex,Color frame,Color glassColor,Color lit,float bay,float height,float windowWidth,float windowHeight,float litRatio,int seed)
        {
            if(mats.TryGetValue(name,out var existing))return existing;
            var material=Painted(name,"MetalPlate",hex,.2f,.5f,.25f);
            material.DisableKeyword("_TRIPLANAR");material.EnableKeyword("_FACADE");
            material.SetVector("_FacadeModule",new Vector4(bay,height,windowWidth,windowHeight));
            material.SetVector("_FacadeParams",new Vector4(litRatio,(int)WindowStyle.Train,seed,.35f));
            material.SetColor("_FacadeFrame",frame);material.SetColor("_FacadeGlass",glassColor);material.SetColor("_FacadeLitColor",lit);
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
        /// <summary>A prop prepared by Tools/blender/prop_prep.py (Resources/Models/Props/&lt;name&gt;), dressed in Railway/Surface.</summary>
        public Transform Model(string name,Vector3 position,float yaw,Vector3 scale,Color tint,Transform parent=null)
        {
            string key="Props/"+name;
            if(!props.TryGetValue(key,out var prefab)){prefab=Resources.Load<GameObject>("Models/Props/"+name+"/"+name);props[key]=prefab;}
            if(!prefab)return null;
            // The wrapper carries yaw and size; the FBX keeps its own root rotation (Blender Z-up conversion) inside it.
            var t=Group(name+" / Sketchfab CC BY",position,parent);
            t.localRotation=Quaternion.Euler(0,yaw,0);t.localScale=scale;
            var model=Object.Instantiate(prefab,t,false);
            RailMaterials.DressProp(model,"Models/Props/"+name+"/Textures/",tint);
            return t;
        }
        /// <summary>Linden model (poplars are the same tree, narrower); far trees cast no shadow.</summary>
        public Transform TreeModel(Vector3 at,float height,Color tint,bool poplar,Transform parent=null)
        {
            float width=poplar?height*.45f:height*Random.Range(.9f,1.15f);
            var tree=Model("Tree",at,Random.Range(0f,360f),new Vector3(width,height,width),tint,parent);
            if(tree&&at.z>20)foreach(var r in tree.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            return tree;
        }
        /// <summary>Modern platform/street light: grey mast, short arm towards the tracks and a flat LED head, lit after dark.</summary>
        public Transform StreetLight(Vector3 at,bool lit,Transform parent=null,float height=4.6f)
        {
            var g=Group("Osvětlení",at,parent);
            var metal=Painted("Lamp mast","MetalPlate","#7D858B",.25f,.45f,.5f);
            Box("Mast",new Vector3(0,height/2,0),new Vector3(.13f,height,.13f),metal,g,PrimitiveType.Cylinder);
            Box("Base",new Vector3(0,.18f,0),new Vector3(.26f,.36f,.26f),metal,g,PrimitiveType.Cylinder);
            Box("Arm",new Vector3(0,height-.04f,-.45f),new Vector3(.07f,.07f,.9f),metal,g);
            Box("LED head",new Vector3(0,height-.06f,-.95f),new Vector3(.26f,.07f,.6f),metal,g);
            Box("LED lens",new Vector3(0,height-.1f,-.95f),new Vector3(.2f,.012f,.5f),lit?Glow("LED lens","#FFE7B8",3.2f):Mat("LED lens off","#D7DCDE",.1f,.8f),g);
            if(lit)Box("Lamp glow",new Vector3(0,height-.35f,-1.2f),new Vector3(2.6f,2.6f,1),SoftSprite("Platform lamp glow",new Color(1,.85f,.6f,.32f),false),g,PrimitiveType.Quad);
            return g;
        }
        /// <summary>Background traffic that loops along x; purely visual.</summary>
        public void AddMover(Transform body,float from,float to,float speed,float phase){if(body)movers.Add(new Mover{body=body,from=from,span=to-from,speed=speed,phase=phase});}
        public void SetSignal(int checkpoint,bool clear)
        {
            if(checkpoint<0||checkpoint>=checkpointSignals.Count)return;
            checkpointSignals[checkpoint][0].SetActive(!clear);checkpointSignals[checkpoint][1].SetActive(clear);
        }
        public void ResetSignals(){for(int i=0;i<checkpointSignals.Count;i++)SetSignal(i,false);}
        public void ResetPickups(){foreach(var t in tickets)t.gameObject.SetActive(true);}
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
            for(int i=0;i<tickets.Count;i++)if(tickets[i].gameObject.activeSelf){tickets[i].localPosition=new Vector3(ticketOrigins[i].x,ticketOrigins[i].y+Mathf.Sin(time*3+i)*.12f,0);tickets[i].rotation=Quaternion.Euler(0,Mathf.Sin(time*2.4f+i)*35,8);}
            for(int i=0;i<birds.Count;i++)
            {
                birds[i].position=new Vector3(birdOrigins[i].x+Mathf.Sin(time*.8f+i)*2.8f,birdOrigins[i].y+Mathf.Sin(time*1.7f+i)*1.1f,0);
                var model=birds[i].childCount==1?birds[i].GetChild(0):null;
                if(model)
                {
                    // Face the flight direction (no moonwalk); flap hard while climbing, glide while sinking.
                    float vx=Mathf.Cos(time*.8f+i),vy=Mathf.Cos(time*1.7f+i);
                    birds[i].rotation=Quaternion.Slerp(birds[i].rotation,Quaternion.Euler(0,vx>=0?-20:200,vy*12),1-Mathf.Exp(-dt*6));
                    var animation=model.GetComponent<Animation>();
                    if(animation&&animation.clip)animation[animation.clip.name].speed=vy>0?1.35f:.55f;
                }
                else
                {
                    birds[i].GetChild(3).localRotation=Quaternion.Euler(Mathf.Sin(time*17+i)*38,0,0);
                    birds[i].GetChild(4).localRotation=Quaternion.Euler(-Mathf.Sin(time*17+i)*38,0,0);
                }
            }
        }
    }
}
