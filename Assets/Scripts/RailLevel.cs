using System.Collections.Generic;
using UnityEngine;

namespace CeskeDrahy
{
    public enum Theme { MainStation, Depot, Park, Hospital, TownStation, River, Sidings, Estate, Fields, Yard }
    public enum Sky { Day, Afternoon, Dusk, Sunset, Evening, Night }

    /// <summary>One route. Platform kinds: S station platform (checkpoint), T passenger coach, C cargo wagon, H tank wagon.</summary>
    public sealed class RailLevel
    {
        public string Id, ScoreKey, Name, Brief, Intro;
        public int Difficulty = 1, Lives = 3;
        public Theme Theme;
        public Sky Sky;
        public Vector4[] Platforms;
        public string Kinds;
        public string[] Zones;
        public float[] Inspectors, Birds, Street, Medkits, Snacks, Springs;
        public int[] Vents = new int[0];
        public float VentPeriod = 4.45f, Wind;
        public Vector2 River;
        int[] stations;

        public float Finish => Platforms[Platforms.Length-1].x+4;
        public string TimeKey => "bestTime_"+Id;
        /// <summary>Platform indices of station platforms: start, checkpoints and finish.</summary>
        public int[] Stations
        {
            get
            {
                if(stations!=null)return stations;
                var found=new List<int>();
                for(int i=0;i<Kinds.Length;i++)if(Kinds[i]=='S')found.Add(i);
                return stations=found.ToArray();
            }
        }
        public int CheckpointCount => Stations.Length-2;
        public Vector2 Checkpoint(int number)
        {
            if(number==0)return new Vector2(2,Platforms[0].z);
            Vector4 platform=Platforms[Stations[number]];
            return new Vector2(platform.x-2,platform.z);
        }
        public string ZoneAt(float x)
        {
            int zone=0;
            for(int i=1;i<Stations.Length;i++)
            {
                Vector4 p=Platforms[Stations[i]];
                if(x>=p.x-p.y*.5f)zone=i;
            }
            return Zones[Mathf.Min(zone,Zones.Length-1)];
        }

        static Vector4 P(float x,float width,float top,float travel=0)=>new Vector4(x,width,top,travel);

        public static readonly RailLevel[] All =
        {
            new RailLevel
            {
                Id="olomouc-hl-n",ScoreKey="best",Name="OLOMOUC HL. N.",Difficulty=1,Theme=Theme.MainStation,Sky=Sky.Afternoon,
                Brief="Seznam se s dvojskokem, kufry a místními revizory.",
                Intro="VÍTEJ V OLOMOUCI. Vlak počká. Holubi ne.",
                Zones=new[]{"OLOMOUC HL. N.","PODCHODOVÝ CHAOS","NÁSTUPIŠTĚ 5","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STTTSTTTSTTTS",
                Platforms=new[]{P(0,16,.12f),P(15,10,2.25f),P(28,10,2.6f),P(42,11,2.3f),P(56,12,.8f),P(70,10,2.7f),P(84,10,3.1f,.7f),P(98,10,2.65f),P(112,12,1),P(126,10,3),P(140,10,3.5f,.6f),P(154,10,2.8f),P(170,18,1.2f)},
                Inspectors=new[]{32f,88f,144f},Birds=new[]{25f,39f,76f,91f,132f,148f,159f},
                Street=new[]{44f,76f,100f,130f,158f},Medkits=new[]{52f,94f,110f,167f},Snacks=new[]{18f,81f,138f},Springs=new[]{5.5f,57.5f,113f}
            },
            new RailLevel
            {
                Id="olomouc-hodolany",ScoreKey="bestLevel1",Name="OLOMOUC HODOLANY",Difficulty=2,Theme=Theme.Depot,Sky=Sky.Day,
                Brief="Depo u Muzea ČD: užší střechy, pohyblivé vagony a horká pára.",
                Intro="POZOR NA PÁRU. Oranžová kontrolka varuje před výtryskem.",
                Zones=new[]{"OLOMOUC HODOLANY","SERVISNÍ LÁVKA","SIGNÁL VOLNO","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STCCSCCCSCCCS",
                Platforms=new[]{P(0,14,.12f),P(16,8,2.6f,.75f),P(29,7.5f,4.7f,1),P(42,9,3.5f),P(55,11,.9f),P(70,8.5f,3),P(83,7.5f,4.9f,1.55f),P(97,9,3.7f),P(111,10,1.1f),P(125,8,3.5f),P(139,7.5f,5.4f,1.35f),P(154,9,3.7f),P(170,16,1.2f)},
                Inspectors=new[]{31f,84f,141f,156f},Birds=new[]{20f,34f,74f,91f,121f,145f,162f},
                Street=new[]{43f,71f,98f,126f,155f},Medkits=new[]{53f,95f,109f,168f},Snacks=new[]{17f,81f,137f},Springs=new[]{5.6f,56f,112f},
                Vents=new[]{3,5,7,11}
            },
            new RailLevel
            {
                Id="olomouc-prednadrazi",ScoreKey="bestLevel2",Name="OLOMOUC PŘEDNÁDRAŽÍ",Difficulty=5,Lives=2,Theme=Theme.Yard,Sky=Sky.Night,
                Brief="Finále na nočním seřadišti. Šetři dvojskok, sleduj výstrahy a vyčkej na pohyb vagonu.",
                Intro="NECH SI DVOJSKOK NA DRUHOU POLOVINU MEZERY.",
                Zones=new[]{"OLOMOUC PŘEDNÁDRAŽÍ","SPÁDOVIŠTĚ","SVĚTELNÉ NÁVĚSTIDLO","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STCCSCCCSCCCS",
                Platforms=new[]{P(0,14,.12f),P(17,7.5f,2.8f,1.15f),P(31,7,5.2f,1.5f),P(45,8,6.3f),P(58,10,2.4f),P(73,7,5.1f,1.2f),P(87,7,7,1.6f),P(102,8.5f,4.7f),P(115,9,2.1f),P(130,7,5.4f,1.1f),P(144,7.5f,7.4f,1.4f),P(158,6.8f,5.1f,1),P(175,17,1.7f)},
                Inspectors=new[]{31f,46f,87f,103f,144f,159f},Birds=new[]{21f,38f,69f,91f,119f,140f,164f},
                Street=new[]{45f,73f,101f,130f,158f},Medkits=new[]{56f,101f,114f,172f},Snacks=new[]{17f,85f,142f},Springs=new[]{5.6f,59f,116f},
                Vents=new[]{2,3,5,7,9,11},VentPeriod=3.55f
            }
        };
    }
}
