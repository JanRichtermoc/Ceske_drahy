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
                Id="olomouc-smetanovy-sady",ScoreKey="best_smetanovy_sady",Name="OLOMOUC-SMETANOVY SADY",Difficulty=1,Theme=Theme.Park,Sky=Sky.Day,
                Brief="Zastávka u parku a Flory. Kufry v trávě vystřelí až ke korunám stromů.",
                Intro="SMETANOVY SADY. Kufrový katapult tě dostane nad koruny.",
                Zones=new[]{"OLOMOUC-SMETANOVY SADY","U FLORY","PALMOVÝ SKLENÍK","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STTTSTTTSTTTS",
                Platforms=new[]{P(0,15,.12f),P(15,10,2),P(28,9,2.9f),P(41,10,2.2f,.5f),P(55,12,.9f),P(69,9,2.6f),P(82,9,3),P(95,10,2.5f,.6f),P(109,12,1),P(123,9,2.8f),P(136,9,3.2f,.7f),P(150,10,2.7f),P(166,17,1.2f)},
                Inspectors=new[]{30f,84f,125f},Birds=new[]{22f,37f,75f,92f,131f,146f},
                Street=new[]{44f,70f,100f,138f},Medkits=new[]{52f,98f,114f,162f},Snacks=new[]{18f,80f,140f},Springs=new[]{5.5f,59f,112f,152f}
            },
            new RailLevel
            {
                Id="olomouc-nova-ulice",ScoreKey="best_nova_ulice",Name="OLOMOUC-NOVÁ ULICE",Difficulty=2,Theme=Theme.Hospital,Sky=Sky.Day,
                Brief="Zastávka u Fakultní nemocnice. Víc injekcí, ale i víc lékárniček.",
                Intro="NOVÁ ULICE. Lékárnička vyléčí nákazu a vrátí život.",
                Zones=new[]{"OLOMOUC-NOVÁ ULICE","FAKULTNÍ NEMOCNICE","URGENTNÍ PŘÍJEM","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STTCSTCTSCCTS",
                Platforms=new[]{P(0,15,.12f),P(16,9,2.4f),P(29,8.5f,3,.6f),P(42,9,3.6f),P(56,12,1),P(70,9,3),P(83,8,4.2f),P(97,9,2.9f,.8f),P(111,12,1.1f),P(125,8.5f,3.6f,.7f),P(139,8,4.4f),P(153,9,3.1f),P(169,17,1.2f)},
                Inspectors=new[]{19f,86f,150f},Birds=new[]{24f,38f,66f,92f,121f,134f,146f},
                Street=new[]{31f,44f,72f,99f,127f,155f},Medkits=new[]{53f,81f,108f,141f,165f},Snacks=new[]{40f,114f,160f},Springs=new[]{5.5f,58f,113f}
            },
            new RailLevel
            {
                Id="olomouc-mesto",ScoreKey="best_olomouc_mesto",Name="OLOMOUC MĚSTO",Difficulty=3,Theme=Theme.TownStation,Sky=Sky.Afternoon,
                Brief="Nádraží pod věžemi historického centra. Skoro všechny vagony se posouvají.",
                Intro="OLOMOUC MĚSTO. Vagony jezdí. Počkej, až se k tobě přiblíží.",
                Zones=new[]{"OLOMOUC MĚSTO","POD RADNIČNÍ VĚŽÍ","KATEDRÁLA SV. VÁCLAVA","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STTTSTCTSTCTS",
                Platforms=new[]{P(0,14,.12f),P(15,8.5f,2.4f,.6f),P(28,8.5f,3,.9f),P(41,8.5f,2.7f,.8f),P(55,11,1),P(69,8.5f,3.1f,.9f),P(82,8,3.9f,1),P(95,8.5f,3,1),P(109,11,1.1f),P(123,8.5f,3.2f,1),P(136,8,4.2f,1),P(149,8.5f,3.1f,.9f),P(165,16,1.2f)},
                Inspectors=new[]{30f,84f,125f,151f},Birds=new[]{21f,35f,63f,77f,101f,117f,131f,143f},
                Street=new[]{43f,71f,97f,138f},Medkits=new[]{52f,106f,161f},Snacks=new[]{17f,93f,134f},Springs=new[]{5f,58f,112f}
            },
            new RailLevel
            {
                Id="olomouc-hodolany",ScoreKey="bestLevel1",Name="OLOMOUC HODOLANY",Difficulty=3,Theme=Theme.Depot,Sky=Sky.Day,
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
                Id="olomouc-hejcin",ScoreKey="best_hejcin",Name="OLOMOUC-HEJČÍN",Difficulty=3,Theme=Theme.River,Sky=Sky.Day,
                Brief="Trať přes řeku Moravu. Pod mostem není kam spadnout, jen do vody.",
                Intro="HEJČÍN. Pod mostem teče Morava. Plavky nemáš.",
                Zones=new[]{"OLOMOUC-HEJČÍN","ŘEKA MORAVA","POD TOPOLY","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STTTSTCTCSTCTS",River=new Vector2(62,102),
                Platforms=new[]{P(0,14,.12f),P(15,8.5f,2.5f),P(28,8.5f,3.2f,.7f),P(41,8.5f,2.6f),P(55,12,1),P(70,8,3),P(83,8,3.8f,.9f),P(96,8,3,1),P(109,8.5f,3.6f),P(123,12,1.1f),P(137,8,3.2f,.9f),P(150,8,4.3f),P(163,8.5f,3,.8f),P(178,15,1.2f)},
                Inspectors=new[]{30f,96f,151f},Birds=new[]{21f,36f,74f,89f,103f,131f,144f,160f},
                Street=new[]{40f,70f,108f,165f},Medkits=new[]{52f,120f,176f},Snacks=new[]{18f,85f,139f},Springs=new[]{5.5f,58f,126f}
            },
            new RailLevel
            {
                Id="olomouc-repcin",ScoreKey="best_repcin",Name="OLOMOUC-ŘEPČÍN",Difficulty=4,Theme=Theme.Sidings,Sky=Sky.Dusk,
                Brief="Vlečky, sila a vysoké kontejnery. Bez dvojskoku se nahoru nedostaneš.",
                Intro="ŘEPČÍN. Kontejnery jsou vysoko. Dvojskok si nech na horní část skoku.",
                Zones=new[]{"OLOMOUC-ŘEPČÍN","VLEČKA K SILŮM","KONTEJNEROVÉ STĚNY","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STCHSCCHSCCHS",
                Platforms=new[]{P(0,14,.12f),P(15,8,2.7f,.8f),P(29,7.5f,4.6f),P(42,8,5.6f,.9f),P(56,10,2.2f),P(70,7.5f,4.2f,1),P(83,7.5f,5.8f),P(97,8,4.6f,1.2f),P(111,10,2),P(125,7.5f,4.4f,1.1f),P(139,7,6.2f,.9f),P(153,7.5f,4.8f,.9f),P(168,15,1.6f)},
                Inspectors=new[]{31f,44f,98f,140f},Birds=new[]{22f,36f,63f,76f,90f,118f,132f,146f},
                Street=new[]{70f,84f,125f,154f},Medkits=new[]{58f,113f,165f},Snacks=new[]{17f,95f,151f},Springs=new[]{5.5f,60f,108f}
            },
            new RailLevel
            {
                Id="olomouc-nove-sady",ScoreKey="best_nove_sady",Name="OLOMOUC-NOVÉ SADY",Difficulty=4,Theme=Theme.Estate,Sky=Sky.Evening,
                Brief="Večer mezi paneláky. Pára z roštů a revizoři na každém druhém vagonu.",
                Intro="NOVÉ SADY. Revizoři mají večerní směnu. Pára taky.",
                Zones=new[]{"OLOMOUC-NOVÉ SADY","SÍDLIŠTĚ","PROTIHLUKOVÁ STĚNA","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STCTSCCCSCCCS",VentPeriod=4,
                Platforms=new[]{P(0,14,.12f),P(15,8,2.6f,.7f),P(28,7.5f,4),P(41,8,3.2f,1),P(55,10,1),P(69,7.5f,3.4f,.9f),P(82,7.5f,5),P(96,8,3.8f,1.2f),P(110,10,1.2f),P(124,7.5f,3.6f,1),P(137,7,5.4f,1.1f),P(150,7.5f,4,.9f),P(165,15,1.4f)},
                Inspectors=new[]{17f,42f,97f,125f,151f},Birds=new[]{22f,35f,62f,75f,89f,117f,130f,143f},
                Street=new[]{70f,79.5f,139f},Medkits=new[]{57f,108f,162f},Snacks=new[]{13f,113f,148f},Springs=new[]{5.5f,59f,107f},
                Vents=new[]{2,5,7,10}
            },
            new RailLevel
            {
                Id="nemilany",ScoreKey="best_nemilany",Name="NEMILANY",Difficulty=4,Theme=Theme.Fields,Sky=Sky.Sunset,Wind=2.6f,
                Brief="Zastávka mezi poli. Při západu slunce fouká silný protivítr.",
                Intro="NEMILANY. Když zabliká větrná výstraha, počkej s dlouhým skokem.",
                Zones=new[]{"NEMILANY","ŘEPKOVÉ POLE","VĚTROLAM","CÍL / POSLEDNÍ SPOJ"},
                Kinds="STTTSTCTSTHTS",
                Platforms=new[]{P(0,14,.12f),P(15,8.5f,2.4f),P(28,8,3.2f,.8f),P(41,8.5f,2.6f),P(55,11,.9f),P(69,8,2.8f,.9f),P(82,8,3.6f),P(95,8.5f,2.8f,1),P(109,11,1),P(123,8,3.1f,1),P(136,7.5f,4),P(149,8,3,.9f),P(164,15,1.2f)},
                Inspectors=new[]{30f,84f,124f},Birds=new[]{21f,35f,47f,62f,76f,89f,116f,130f,142f},
                Street=new[]{43f,71f,96f,150f},Medkits=new[]{52f,107f,160f},Snacks=new[]{18f,80f,138f},Springs=new[]{5.5f,58f,113f}
            },
            new RailLevel
            {
                Id="olomouc-prednadrazi",ScoreKey="bestLevel2",Name="OLOMOUC PŘEDNÁDRAŽÍ",Difficulty=5,Lives=2,Theme=Theme.Yard,Sky=Sky.Night,
                Brief="Finále na nočním seřadišti. Šetři dvojskok, sleduj výstrahy a vyčkej na pohyb vagonu.",
                Intro="POSLEDNÍ SPOJ ODJÍŽDÍ. Nech si dvojskok na druhou polovinu mezery.",
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
