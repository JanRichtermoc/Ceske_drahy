using UnityEngine;

namespace CeskeDrahy
{
    /// <summary>Stylized, non-colliding scenery for each Olomouc railway station. Depth z: 0 track, 10+ backdrop.</summary>
    public static class OlomoucBackdrops
    {
        const float Ground = -1.4f;

        public static void Build(RailWorld w)
        {
            switch(w.Level.Theme)
            {
                case Theme.MainStation:w.Background();OlomoucStation.Build(w);break;
                case Theme.Depot:w.IndustrialBackdrop(false);w.Sign("LOKOMOTIVNÍ DEPO HODOLANY",new Vector3(10,8.4f,11.85f),.1f);break;
                case Theme.Yard:w.IndustrialBackdrop(true);break;
                case Theme.Park:Park(w);break;
                case Theme.Hospital:Hospital(w);break;
                case Theme.TownStation:Town(w);break;
                case Theme.River:River(w);break;
                case Theme.Sidings:Sidings(w);break;
                case Theme.Estate:Estate(w);break;
                case Theme.Fields:Fields(w);break;
            }
        }

        static float Mid(RailWorld w)=>(w.Length-18)*.5f;
        static float Span(RailWorld w)=>w.Length+18;
        static float Rand(System.Random rng,float min,float max)=>min+(float)rng.NextDouble()*(max-min);

        public static Transform Tree(RailWorld w,Vector3 at,float height,Material leaves,Material bark,bool poplar=false)
        {
            var t=w.Group(poplar?"Topol":"Strom",at);
            w.Cylinder("Kmen",new Vector3(0,height*.3f,0),Mathf.Max(.22f,height*.045f),height*.6f,bark,t);
            if(poplar)w.Box("Koruna",new Vector3(0,height*.62f,0),new Vector3(height*.2f,height*.78f,height*.2f),leaves,t,PrimitiveType.Sphere);
            else for(int n=0;n<4;n++)
                w.Box("Koruna",new Vector3(Mathf.Sin(n*2.1f)*height*.14f,height*(.6f+n*.07f),Mathf.Cos(n*2.1f)*height*.1f),Vector3.one*height*(.42f-n*.03f),leaves,t,PrimitiveType.Sphere);
            return t;
        }

        /// <summary>Glass shelter on the first platform of a halt.</summary>
        static void Shelter(RailWorld w,float x,float top)
        {
            var g=w.Group("Přístřešek zastávky",new Vector3(x,top,0));
            var frame=w.Mat("Shelter frame","#2E4553",.5f,.45f);var pane=w.Mat("Shelter glass","#6E97A6",.2f,.85f);
            w.Box("Back pane",new Vector3(0,1.25f,1.32f),new Vector3(3.6f,2.3f,.05f),pane,g);
            for(int side=-1;side<=1;side+=2)
            {
                w.Box("Post",new Vector3(side*1.85f,1.25f,1.1f),new Vector3(.1f,2.5f,.1f),frame,g);
                w.Box("Side pane",new Vector3(side*1.85f,1.35f,.75f),new Vector3(.05f,1.9f,1.1f),pane,g);
            }
            w.Box("Roof",new Vector3(0,2.55f,.8f),new Vector3(4.1f,.14f,1.6f),frame,g);
            w.Box("Bench",new Vector3(0,.45f,1.05f),new Vector3(2.6f,.08f,.4f),w.Surfaced("Bench wood","Planks","#B07A55",1.5f,.6f,.3f),g);
            w.Box("Timetable",new Vector3(1.2f,1.4f,1.29f),new Vector3(.6f,.8f,.03f),w.cream,g);
        }

        static void Lawn(RailWorld w,string color,float zFrom=10,float zTo=70)
        {
            w.Box("Trávník",new Vector3(Mid(w),Ground+.02f,(zFrom+zTo)*.5f),new Vector3(Span(w),.1f,zTo-zFrom),w.Surfaced("Lawn "+color,"Grass",color,3.5f,.7f,.06f));
        }

        static void Lamp(RailWorld w,Vector3 at,bool lit)
        {
            var g=w.Group("Lampa",at);
            w.Cylinder("Pole",new Vector3(0,2,0),.12f,4,w.dark,g);
            w.Box("Head",new Vector3(0,4.05f,0),new Vector3(.5f,.25f,.5f),lit?w.Glow("Warm lamp","#FFD38A",2.5f):w.cream,g);
            if(lit)w.Box("Glow",new Vector3(0,4,-.4f),new Vector3(2.6f,2.6f,1),w.SoftSprite("Lamp glow",new Color(1,.78f,.42f,.35f),false),g,PrimitiveType.Quad);
        }

        static void Park(RailWorld w)
        {
            Lawn(w,"#6B9550");
            w.Catenary(-10,w.Length-15);Shelter(w,-3,w.Level.Platforms[0].z);
            var rng=new System.Random(7);var bark=w.Mat("Bark","#5A4636");
            Material[] crowns={w.Leaves("Linden leaves","#557F45"),w.Leaves("Chestnut leaves","#3F6B3D"),w.Leaves("Maple leaves","#6C8C3E")};
            for(float x=-10;x<w.Length;x+=Rand(rng,5,8))Tree(w,new Vector3(x,Ground,Rand(rng,12,16)),Rand(rng,6.5f,9.5f),crowns[rng.Next(3)],bark);
            for(float x=-6;x<w.Length;x+=Rand(rng,8,12))
                if(x<44||x>96)Tree(w,new Vector3(x,Ground,Rand(rng,26,32)),Rand(rng,9,13),crowns[rng.Next(3)],bark);
            var fence=w.Mat("Park fence","#2F3B33",.4f,.4f);
            w.Box("Fence rail",new Vector3(Mid(w),-.55f,10.6f),new Vector3(Span(w),.06f,.06f),fence);
            for(float x=-16;x<w.Length;x+=2.5f)w.Box("Fence post",new Vector3(x,-.95f,10.6f),new Vector3(.07f,.95f,.07f),fence);
            for(float x=12;x<w.Length;x+=24)
            {
                Lamp(w,new Vector3(x,Ground,11.6f),false);
                var bench=w.Group("Lavička",new Vector3(x+5,Ground,11.8f));
                w.Box("Seat",new Vector3(0,.48f,0),new Vector3(1.8f,.08f,.45f),w.Surfaced("Bench wood","Planks","#B07A55",1.5f,.6f,.3f),bench);
                w.Box("Back",new Vector3(0,.85f,.2f),new Vector3(1.8f,.4f,.06f),w.Surfaced("Bench wood","Planks","#B07A55",1.5f,.6f,.3f),bench);
                w.Box("Legs",new Vector3(0,.24f,0),new Vector3(1.6f,.48f,.4f),fence,bench);
            }
            // Výstaviště Flora: modernist exhibition pavilion and the palm house.
            var hall=w.Group("Výstaviště Flora / pavilon",new Vector3(70,Ground,40));
            var white=w.Mat("Pavilion render","#E7E3D6",smooth:.3f);
            w.Box("Pavilion body",new Vector3(0,4,0),new Vector3(44,8,16),white,hall);
            w.Box("Glass band",new Vector3(0,4.4f,-8.05f),new Vector3(40,4.6f,.1f),w.glass,hall);
            for(float i=-20;i<=20;i+=2.5f)w.Box("Mullion",new Vector3(i,4.4f,-8.12f),new Vector3(.14f,4.6f,.08f),white,hall);
            w.Box("Roof slab",new Vector3(0,8.2f,-.5f),new Vector3(47,.5f,18),white,hall);
            w.Sign("VÝSTAVIŠTĚ FLORA",new Vector3(0,9.6f,-8.4f),.18f,hall,new Color(.2f,.33f,.25f));
            var palm=w.Group("Palmový skleník",new Vector3(130,Ground,34));
            var pane=w.Mat("Greenhouse glass","#BFE0DF",.15f,.92f);
            w.Box("Base",new Vector3(0,1.4f,0),new Vector3(22,2.8f,12),white,palm);
            var vault=w.Cylinder("Glass vault",new Vector3(0,2.8f,0),11,20,pane,palm);vault.localRotation=Quaternion.Euler(0,0,90);
            for(float i=-9;i<=9;i+=3)
            {
                var rib=w.Cylinder("Vault rib",new Vector3(i,2.8f,0),11.15f,.25f,white,palm);rib.localRotation=Quaternion.Euler(0,0,90);
            }
            w.Box("Lantern",new Vector3(0,8.6f,0),new Vector3(4,1.4f,4),pane,palm);
            w.Spire("Lantern roof",new Vector3(0,9.3f,0),4.4f,1.6f,white,palm);
        }

        static void Hospital(RailWorld w)
        {
            Lawn(w,"#7A9A5C",10,20);
            w.Catenary(-10,w.Length-15);Shelter(w,-3,w.Level.Platforms[0].z);
            w.Box("Ulice",new Vector3(Mid(w),Ground+.03f,14.5f),new Vector3(Span(w),.08f,5),w.Surfaced("Asphalt","Asphalt","#8C8F94",4,.6f,.3f));
            for(float x=-14;x<w.Length;x+=6)w.Box("Lane mark",new Vector3(x,Ground+.08f,14.5f),new Vector3(2.6f,.02f,.18f),w.cream);
            var blockA=w.Facade("Hospital facade A",new Color(.9f,.91f,.9f),new Color(.32f,.5f,.62f),new Color(.95f,.93f,.8f),14,6,.08f,3);
            var blockB=w.Facade("Hospital facade B",new Color(.86f,.89f,.92f),new Color(.28f,.45f,.6f),new Color(.95f,.93f,.8f),10,9,.08f,5);
            var accent=w.Mat("Hospital accent","#3F7FA6",.2f,.5f);
            for(float x=30;x<w.Length;x+=105)
            {
                w.Box("Pavilon",new Vector3(x,Ground+8,28),new Vector3(34,16,12),blockA);
                w.Box("Roof edge",new Vector3(x,Ground+16.2f,28),new Vector3(34.6f,.4f,12.6f),accent);
                w.Sign("FAKULTNÍ NEMOCNICE",new Vector3(x,Ground+14.6f,21.9f),.16f,null,new Color(.15f,.35f,.5f));
                w.Box("Tower block",new Vector3(x+45,Ground+12,36),new Vector3(24,24,14),blockB);
                w.Box("Helipad",new Vector3(x+45,Ground+24.15f,36),new Vector3(10,.3f,10),w.dark);
                w.Sign("H",new Vector3(x+45,Ground+24.4f,36),.5f,null,new Color(.98f,.86f,.3f)).localRotation=Quaternion.Euler(90,0,0);
                var entrance=w.Group("Urgentní příjem",new Vector3(x+22,Ground,22));
                w.Box("Wing",new Vector3(0,3.5f,0),new Vector3(16,7,8),blockA,entrance);
                w.Box("Canopy",new Vector3(0,3.2f,-5),new Vector3(12,.3f,3),accent,entrance);
                w.Sign("URGENTNÍ PŘÍJEM",new Vector3(0,5.8f,-4.1f),.08f,entrance,new Color(.75f,.12f,.12f));
                var sign=w.Group("Značka nemocnice",new Vector3(x+31,Ground,17.2f));
                w.Cylinder("Pole",new Vector3(0,1.3f,0),.08f,2.6f,w.steel,sign);
                w.Box("Plate",new Vector3(0,2.9f,0),new Vector3(.9f,.9f,.05f),w.Mat("Sign blue","#1F4FA0"),sign);
                w.Sign("H",new Vector3(0,2.9f,-.04f),.12f,sign);
                for(int i=0;i<2;i++)Ambulance(w,new Vector3(x+15+i*4.5f,Ground,18.5f));
            }
            Tram(w,14.5f);
            var rng=new System.Random(11);var bark=w.Mat("Bark","#5A4636");var leaves=w.Leaves("Linden leaves","#557F45");
            for(float x=-8;x<w.Length;x+=Rand(rng,9,15))Tree(w,new Vector3(x,Ground,Rand(rng,11.2f,12.2f)),Rand(rng,5.5f,7.5f),leaves,bark);
        }

        /// <summary>A Tatra T3 tram running along the street at depth z, with track and overhead wire.</summary>
        static void Tram(RailWorld w,float z)
        {
            for(int side=-1;side<=1;side+=2)w.Box("Tram rail",new Vector3(Mid(w),Ground+.09f,z+side*.72f),new Vector3(Span(w),.04f,.08f),w.steel);
            w.Box("Tram wire",new Vector3(Mid(w),Ground+5.6f,z),new Vector3(Span(w),.03f,.03f),w.dark);
            for(float x=-12;x<w.Length;x+=25)w.Cylinder("Tram mast",new Vector3(x,Ground+3,z+2.4f),.18f,6,w.dark);
            var tram=w.Prop("tatra_t3",new Vector3(0,Ground+.05f,z),90);
            w.AddMover(tram,-30,w.Length+30,6.5f,40);
        }
        static void Ambulance(RailWorld w,Vector3 at)
        {
            var g=w.Group("Sanitka",at);
            var white=w.Mat("Ambulance white","#F1F1EC",.2f,.6f);var yellow=w.Mat("Ambulance yellow","#E7D23A");var green=w.Mat("Ambulance green","#2E9B57");
            w.Box("Box body",new Vector3(.4f,1.45f,0),new Vector3(2.8f,2.1f,1.9f),white,g);
            w.Box("Cab",new Vector3(-1.55f,1.05f,0),new Vector3(1.2f,1.4f,1.85f),white,g);
            w.Box("Windscreen",new Vector3(-2.1f,1.35f,0),new Vector3(.05f,.6f,1.6f),w.glass,g);
            for(int i=0;i<6;i++)w.Box("Battenburg",new Vector3(-.85f+i*.5f,1.05f,-.96f),new Vector3(.5f,.4f,.02f),i%2==0?yellow:green,g);
            w.Box("Beacon",new Vector3(-1.4f,1.82f,0),new Vector3(.8f,.14f,.3f),w.Glow("Beacon blue","#3A7BFF",1.5f),g);
            foreach(float x in new[]{-1.4f,1.2f})
            {var wheel=w.Cylinder("Wheel",new Vector3(x,.32f,-.9f),.64f,.25f,w.dark,g);wheel.localRotation=Quaternion.Euler(90,0,0);}
        }

        static void Town(RailWorld w)
        {
            Lawn(w,"#8C9A6A",10,20);
            w.Catenary(-10,w.Length-15);
            // Small station building of Olomouc město.
            var station=w.Group("Olomouc město / výpravní budova",new Vector3(10,Ground,13.5f));
            var ochre=w.Mat("Station ochre","#CBA86E",smooth:.2f);var roof=w.Mat("Slate roof","#3B4249",.2f,.35f);
            w.Box("Body",new Vector3(0,3.5f,0),new Vector3(16,7,7),ochre,station);
            var hip=w.Spire("Hip roof",new Vector3(0,7,0),1,1,roof,station);hip.localScale=new Vector3(17,3.2f,8);
            for(float i=-6;i<=6;i+=2.4f)
            {
                w.Box("Window",new Vector3(i,4.8f,-3.55f),new Vector3(1.1f,1.6f,.08f),w.glass,station);
                w.Box("Door arch",new Vector3(i,1.6f,-3.55f),new Vector3(1.2f,2.4f,.08f),i==0?w.navy:w.glass,station);
            }
            w.Sign("OLOMOUC MĚSTO",new Vector3(0,6.35f,-3.65f),.09f,station,new Color(.15f,.2f,.25f));
            // Burgher houses of the historic centre.
            var rng=new System.Random(23);
            string[] pastel={"#D8B48A","#C98C7B","#E2D3A6","#9FB7A6","#D9C9B8","#B9A0C2"};
            for(float x=24;x<w.Length;)
            {
                float width=Rand(rng,6,8.5f),height=Rand(rng,5,7.5f);
                var house=w.Group("Měšťanský dům",new Vector3(x+width*.5f,Ground,21));
                int tint=rng.Next(pastel.Length);ColorUtility.TryParseHtmlString(pastel[tint],out var paint);
                var wall=w.Facade("Burgher facade "+tint,paint,new Color(.2f,.27f,.32f),new Color(1,.86f,.55f),3,3,.05f,tint+40);
                w.Box("Facade",new Vector3(0,height*.5f,0),new Vector3(width,height,8),wall,house);
                var gable=w.Spire("Gable roof",new Vector3(0,height,0),1,1,w.Surfaced("Old town roof tiles","RoofTiles","#B65A47",1.4f,.8f,.2f),house);gable.localScale=new Vector3(width+.3f,Rand(rng,2.5f,4),8.4f);
                x+=width+.15f;
            }
            w.Box("Ulice",new Vector3(Mid(w),Ground+.03f,16.5f),new Vector3(Span(w),.08f,5),w.Surfaced("Asphalt","Asphalt","#8C8F94",4,.6f,.3f));
            Tram(w,16.5f);
            // Skyline: St. Moritz, the town hall tower, the Holy Trinity Column and St. Wenceslas cathedral.
            var stone=w.Mat("Sandstone","#C7B79A",smooth:.15f);var slate=w.Mat("Dark spire","#3A4048",.3f,.35f);var gilt=w.Mat("Gilding","#E2B44A",.8f,.7f);
            var moritz=w.Group("Kostel sv. Mořice",new Vector3(38,Ground,44));
            w.Box("Nave",new Vector3(6,8,0),new Vector3(16,16,10),stone,moritz);
            w.Box("North tower",new Vector3(-3,13,0),new Vector3(5,26,5),stone,moritz);
            w.Box("South tower",new Vector3(-3,10.5f,6),new Vector3(4.4f,21,4.4f),stone,moritz);
            w.Spire("North cap",new Vector3(-3,26,0),5.2f,3,slate,moritz);w.Spire("South cap",new Vector3(-3,21,6),4.6f,2.6f,slate,moritz);
            var hall=w.Group("Radnice",new Vector3(84,Ground,40));
            w.Box("Town hall",new Vector3(0,6,0),new Vector3(18,12,10),stone,hall);
            w.Box("Tower shaft",new Vector3(4,15,0),new Vector3(4,30,4),stone,hall);
            w.Box("Gallery",new Vector3(4,24,0),new Vector3(5,.6f,5),slate,hall);
            w.Spire("Tower spire",new Vector3(4,30,0),4.4f,7,slate,hall);
            for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)w.Spire("Pinnacle",new Vector3(4+a*1.8f,30,b*1.8f),.7f,2.4f,slate,hall);
            var column=w.Group("Sloup Nejsvětější Trojice",new Vector3(100,Ground,30));
            w.Box("Chapel base",new Vector3(0,2,0),new Vector3(5,4,5),stone,column);
            w.Cylinder("Column",new Vector3(0,9,0),1.3f,10,stone,column);
            w.Box("Gilded group",new Vector3(0,14.6f,0),new Vector3(1.6f,1.6f,1.2f),gilt,column,PrimitiveType.Sphere);
            var cathedral=w.Group("Katedrála sv. Václava",new Vector3(142,Ground,46));
            w.Box("Nave",new Vector3(10,9,0),new Vector3(26,18,12),stone,cathedral);
            w.Box("Main tower",new Vector3(-6,17,0),new Vector3(6,34,6),stone,cathedral);
            w.Spire("Main spire",new Vector3(-6,34,0),6.4f,14,slate,cathedral);
            for(int side=-1;side<=1;side+=2)
            {
                w.Box("Front tower",new Vector3(-1,12,side*5.5f),new Vector3(4,24,4),stone,cathedral);
                w.Spire("Front spire",new Vector3(-1,24,side*5.5f),4.3f,9,slate,cathedral);
            }
        }

        static void River(RailWorld w)
        {
            var river=w.Level.River;float width=river.y-river.x,center=(river.x+river.y)*.5f;
            Lawn(w,"#6E9452",10,70);
            w.Catenary(-10,w.Length-15);Shelter(w,-3,w.Level.Platforms[0].z);
            var water=w.Water("Morava water","#2A515E");
            w.Box("Řeka Morava",new Vector3(center,-2.2f,22),new Vector3(width+.5f,.2f,95),water);
            var quay=w.Surfaced("Quay stone","Concrete","#9A968C",2,.8f,.12f);
            foreach(float x in new[]{river.x,river.y})w.Box("Nábřeží",new Vector3(x,-1.85f,22),new Vector3(.6f,1,95),quay);
            // Deck bridge: girders below the track keep the view of the roofs clear.
            var concrete=w.Surfaced("Bridge concrete","Concrete","#B4B2AA",3,.6f,.15f);
            w.Box("Bridge deck",new Vector3(center,-1.15f,4),new Vector3(width,.3f,13),concrete);
            w.Box("Bridge ballast",new Vector3(center,-1.02f,4),new Vector3(width,.08f,12),w.Mat("Bridge ballast","#7D7468",smooth:.05f));
            foreach(float z in new[]{-2.1f,4,10.1f})w.Box("Plate girder",new Vector3(center,-1.85f,z),new Vector3(width,1.1f,.35f),w.Mat("Girder green","#4F6A5B",.45f,.4f));
            foreach(float x in new[]{river.x+width/3,river.x+width*2/3})w.Box("Bridge pier",new Vector3(x,-2.6f,4),new Vector3(2,1.6f,14),concrete);
            w.Box("Back railing",new Vector3(center,-.45f,10.4f),new Vector3(width,.08f,.08f),w.steel);
            for(float x=river.x;x<=river.y;x+=2)w.Box("Railing post",new Vector3(x,-.75f,10.4f),new Vector3(.08f,.6f,.08f),w.steel);
            var boat=w.Group("Kanoe",new Vector3(center+6,-2.05f,18));
            w.Box("Hull",Vector3.zero,new Vector3(4,.35f,.8f),w.Mat("Canoe red","#C2452D"),boat,PrimitiveType.Sphere);
            var rng=new System.Random(31);var bark=w.Mat("Bark","#5A4636");
            var poplar=w.Leaves("Poplar leaves","#5C8446");var willow=w.Leaves("Willow leaves","#7C9A55");
            for(float x=-10;x<w.Length;x+=Rand(rng,4,6))
            {
                if(x>river.x-2 && x<river.y+2)continue;
                Tree(w,new Vector3(x,Ground,Rand(rng,13,18)),Rand(rng,10,14),poplar,bark,true);
            }
            foreach(float x in new[]{river.x-4,river.y+5})Tree(w,new Vector3(x,Ground,24),8,willow,bark);
            string[] roofs={"#A0493A","#8E3E33","#6F5B4C"};
            for(float x=-6;x<w.Length;x+=Rand(rng,11,16))
            {
                if(x>river.x-6 && x<river.y+6)continue;
                var house=w.Group("Rodinný dům",new Vector3(x,Ground,34));
                w.Box("Walls",new Vector3(0,2.5f,0),new Vector3(7,5,6),w.Mat("House render","#E3DCCB",smooth:.15f),house);
                int tile=rng.Next(3);var roof=w.Spire("Roof",new Vector3(0,5,0),1,1,w.Surfaced("Roof tiles "+tile,"RoofTiles",roofs[tile],1.6f,.8f,.2f),house);roof.localScale=new Vector3(7.6f,3,6.6f);
                w.Box("Window",new Vector3(-1.5f,2.6f,-3.02f),new Vector3(1.2f,1.2f,.05f),w.glass,house);
                w.Box("Window",new Vector3(1.5f,2.6f,-3.02f),new Vector3(1.2f,1.2f,.05f),w.glass,house);
            }
        }

        static void Sidings(RailWorld w)
        {
            Lawn(w,"#7C7A5E",10,70);
            var rng=new System.Random(41);
            var concrete=w.Surfaced("Silo concrete","Concrete","#CFC9BC",4,.6f,.12f);
            var silo=w.Group("Sila",new Vector3(78,Ground,30));
            for(int row=0;row<2;row++)for(int i=0;i<4;i++)w.Cylinder("Silo",new Vector3(i*5.2f,11,row*5.2f),5,22,concrete,silo);
            w.Box("Head house",new Vector3(-6,14,2.6f),new Vector3(6,28,7),concrete,silo);
            w.Box("Gallery",new Vector3(8,22.6f,2.6f),new Vector3(22,1.2f,3),concrete,silo);
            w.Sign("SILO",new Vector3(-6,24,-1.05f),.2f,silo,new Color(.2f,.22f,.25f));
            var metal=w.Facade("Shed cladding",new Color(.55f,.6f,.62f),new Color(.3f,.34f,.36f),new Color(1,.8f,.5f),8,2,.25f,9);
            foreach(float x in new[]{24f,128f})
            {
                var shed=w.Group("Sklad",new Vector3(x,Ground,22));
                w.Box("Shed",new Vector3(0,3.5f,0),new Vector3(32,7,12),metal,shed);
                for(float i=-14;i<=14;i+=4)
                {var tooth=w.Box("Sawtooth",new Vector3(i,7.6f,0),new Vector3(4.2f,.25f,12),w.Mat("Roof sheet","#5E6468",.4f,.4f),shed);tooth.localRotation=Quaternion.Euler(0,0,18);}
            }
            string[] boxes={"#B5432E","#2F6C8F","#C99A2E","#3E7A4E","#7A4E8F","#D0D0C8"};
            for(float x=-8;x<w.Length;x+=Rand(rng,9,16))
            {
                int stack=rng.Next(1,3);
                for(int k=0;k<stack;k++)
                {
                    int tint=rng.Next(boxes.Length);
                    var c=w.Box("Kontejner",new Vector3(x,Ground+1.3f+k*2.6f,16+(k%2)*.2f),new Vector3(6.1f,2.6f,2.44f),w.Surfaced("Container "+tint,"Corrugated",boxes[tint],1.6f,.9f,.35f,.3f));
                    for(float r=-2.8f;r<=2.8f;r+=.5f)w.Box("Rib",new Vector3(r/6.1f,0,-.51f),new Vector3(.02f,.95f,.02f),w.dark,c);
                }
            }
            var glow=w.SoftSprite("Floodlight glow dusk",new Color(1,.72f,.4f,.3f),false);
            for(float x=6;x<w.Length;x+=38)
            {
                w.Box("Floodlight mast",new Vector3(x,7,11),new Vector3(.3f,17,.3f),w.steel);
                w.Box("Floodlight",new Vector3(x,15.6f,10.7f),new Vector3(1.3f,.5f,.5f),w.Glow("Warm lamp","#FFD38A",2.5f));
                w.Box("Floodlight glow",new Vector3(x,15.5f,10.3f),new Vector3(5,4,1),glow,null,PrimitiveType.Quad);
            }
            for(float x=10;x<w.Length-20;x+=Rand(rng,12,20))
            {
                var wagon=w.Group("Odstavený vagon",new Vector3(x,0,8));
                int tint=rng.Next(4);
                w.Box("Body",new Vector3(0,.6f,0),new Vector3(9,2.4f,2.4f),w.Surfaced("Container "+tint,"Corrugated",boxes[tint],1.6f,.9f,.35f,.3f),wagon);
                w.Box("Frame",new Vector3(0,-.6f,0),new Vector3(9,.3f,2.2f),w.dark,wagon);
            }
        }

        static void Estate(RailWorld w)
        {
            Lawn(w,"#58744A",10,70);
            w.Catenary(-10,w.Length-15);Shelter(w,-3,w.Level.Platforms[0].z);
            var rng=new System.Random(53);
            string[] panels={"#405C55","#2F6F7C","#C9853D","#7B8A43"};
            for(float x=-14;x<w.Length;x+=4)
            {
                var panel=w.Box("Protihluková stěna",new Vector3(x,Ground+1.6f,11),new Vector3(3.95f,3.2f,.25f),w.Mat("Barrier "+panels[(int)(x+14)/4%4],panels[(int)(x+14)/4%4],smooth:.2f));
                w.Box("Barrier post",new Vector3(-.5f,0,-.15f),new Vector3(.04f,1.02f,.4f),w.steel,panel);
            }
            Color[] facades={new Color(.86f,.77f,.5f),new Color(.63f,.77f,.8f),new Color(.86f,.65f,.57f),new Color(.74f,.8f,.56f)};
            int block=0;
            for(float x=4;x<w.Length;x+=Rand(rng,30,36),block++)
            {
                var wall=w.Facade("Panelák "+block%4,facades[block%4],new Color(.18f,.22f,.28f),new Color(1,.82f,.48f),12,10,.35f,block%4+17);
                float z=Rand(rng,26,34),height=Rand(rng,24,30);
                w.Box("Panelový dům",new Vector3(x,Ground+height*.5f,z),new Vector3(26,height,11),wall);
                w.Box("Roof parapet",new Vector3(x,Ground+height+.25f,z),new Vector3(26.4f,.5f,11.4f),w.Mat("Parapet","#6E6E6A"));
                for(float i=-10;i<=10;i+=6.6f)w.Box("Stair core",new Vector3(x+i,Ground+height*.5f,z-5.6f),new Vector3(1.6f,height,.3f),w.Mat("Stair glass","#4E6A78",.3f,.7f));
            }
            for(float x=-6;x<w.Length;x+=Rand(rng,10,15))
                Tree(w,new Vector3(x,Ground,Rand(rng,15,19)),Rand(rng,6,8.5f),w.Leaves("Estate leaves","#456E40"),w.Mat("Bark","#5A4636"));
            for(float x=8;x<w.Length;x+=16)Lamp(w,new Vector3(x,Ground,13),true);
            var play=w.Group("Dětské hřiště",new Vector3(60,Ground,20));
            w.Box("Sandpit",new Vector3(0,.15f,0),new Vector3(5,.3f,4),w.Mat("Sand","#D7C38E"),play);
            var slide=w.Box("Slide",new Vector3(4,1.1f,0),new Vector3(3.5f,.15f,.8f),w.Mat("Slide red","#C8402E",.2f,.7f),play);slide.localRotation=Quaternion.Euler(0,0,-28);
            w.Box("Climbing tower",new Vector3(6,1.4f,0),new Vector3(1.4f,2.8f,1.4f),w.Mat("Play yellow","#E6B935"),play);
        }

        static void Fields(RailWorld w)
        {
            w.Catenary(-10,w.Length-15);Shelter(w,-3,w.Level.Platforms[0].z);
            string[] crops={"#E2C93A","#7E9A48","#C2A55F","#6A5039","#93AE52"};
            float z=10;
            for(int i=0;z<95;i++)
            {
                float depth=4+(i*7)%5;
                w.Box("Pole",new Vector3(Mid(w),Ground+.02f,z+depth*.5f),new Vector3(Span(w),.1f,depth),w.Mat("Crop "+crops[i%crops.Length],crops[i%crops.Length],smooth:.05f));
                z+=depth;
            }
            var rng=new System.Random(61);var bark=w.Mat("Bark","#5A4636");var leaves=w.Leaves("Windbreak leaves","#4D6B3B");
            foreach(float row in new[]{28f,52f})
                for(float x=-14;x<w.Length;x+=Rand(rng,2.4f,3.6f))Tree(w,new Vector3(x,Ground,row+Rand(rng,-.6f,.6f)),Rand(rng,7,10),leaves,bark,rng.NextDouble()<.4);
            for(float x=-10;x<w.Length;x+=Rand(rng,9,14))
            {
                var house=w.Group("Vesnický dům",new Vector3(x,Ground,62));
                w.Box("Walls",new Vector3(0,2.2f,0),new Vector3(6.5f,4.4f,6),w.Mat("Village render","#E6DFCC",smooth:.15f),house);
                var roof=w.Spire("Roof",new Vector3(0,4.4f,0),1,1,w.Surfaced("Village roof tiles","RoofTiles","#C0604C",1.6f,.8f,.2f),house);roof.localScale=new Vector3(7,2.8f,6.6f);
            }
            var chapel=w.Group("Kaple",new Vector3(92,Ground,58));
            w.Box("Nave",new Vector3(0,3,0),new Vector3(8,6,6),w.Mat("Chapel white","#EEEAE0"),chapel);
            w.Box("Tower",new Vector3(-4.5f,5,0),new Vector3(3,10,3),w.Mat("Chapel white","#EEEAE0"),chapel);
            w.Spire("Tower roof",new Vector3(-4.5f,10,0),3.4f,4,w.Mat("Village roof","#A4483A"),chapel);
            var steel=w.Mat("Pylon steel","#8B949A",.6f,.4f);
            for(float x=0;x<w.Length;x+=42)
            {
                var pylon=w.Group("Stožár VN",new Vector3(x,Ground,40));
                for(int a=-1;a<=1;a+=2){var leg=w.Box("Leg",new Vector3(a*1.1f,8,0),new Vector3(.25f,16.2f,.25f),steel,pylon);leg.localRotation=Quaternion.Euler(0,0,-a*6);}
                w.Box("Crossarm",new Vector3(0,14.5f,0),new Vector3(9,.3f,.3f),steel,pylon);
                w.Box("Crossarm",new Vector3(0,11.5f,0),new Vector3(7,.3f,.3f),steel,pylon);
                if(x+42<w.Length)foreach(float y in new[]{14.2f,11.2f})w.Box("Wire",new Vector3(x+21,Ground+y,40),new Vector3(42,.04f,.04f),w.dark);
            }
            // Windsocks on the stations and gust streaks show the headwind before it pushes.
            var sockMat=w.Mat("Windsock orange","#F07A2B");
            foreach(int index in w.Level.Stations)
            {
                var p=w.Level.Platforms[index];
                var mast=w.Group("Větrný rukáv",new Vector3(p.x+p.y*.5f-1.2f,p.z,1.2f));
                w.Cylinder("Mast",new Vector3(0,1.6f,0),.08f,3.2f,w.steel,mast);
                var pivot=w.Group("Sock pivot",new Vector3(0,3.15f,0),mast);
                for(int k=0;k<4;k++)w.Box("Sock band",new Vector3(-.3f-k*.32f,0,0),new Vector3(.32f,.42f-k*.06f,.42f-k*.06f),k%2==0?sockMat:w.cream,pivot,PrimitiveType.Cylinder).localRotation=Quaternion.Euler(0,0,90);
                w.AddWindProp(pivot);
            }
            var streak=w.SoftSprite("Wind streak",new Color(1,1,1,.45f),false);
            for(int i=0;i<14;i++)
            {
                var s=w.Box("Poryv větru",new Vector3(0,Rand(rng,.6f,7),Rand(rng,-1.5f,2.5f)),new Vector3(3,.05f,1),streak,null,PrimitiveType.Quad);
                s.gameObject.SetActive(false);w.AddWindStreak(s);
            }
        }
    }
}
