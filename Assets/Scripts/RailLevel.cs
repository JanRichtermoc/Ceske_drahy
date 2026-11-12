using UnityEngine;

namespace CeskeDrahy
{
    public sealed class RailLevel
    {
        public readonly string Name, Brief;
        public readonly Vector4[] Platforms;
        public readonly float[] Inspectors, Birds, Street, Medkits, Snacks, Springs;
        public readonly int[] Vents;
        public float Finish => Platforms[Platforms.Length-1].x+4;
        public RailLevel(string name,string brief,Vector4[] platforms,float[] inspectors,float[] birds,float[] street,float[] medkits,float[] snacks,float[] springs,int[] vents)
        {
            Name=name;Brief=brief;Platforms=platforms;Inspectors=inspectors;Birds=birds;Street=street;
            Medkits=medkits;Snacks=snacks;Springs=springs;Vents=vents;
        }
        static Vector4 P(float x,float width,float top,float travel=0)=>new Vector4(x,width,top,travel);
        public static readonly RailLevel[] All =
        {
            new RailLevel("OLOMOUC HL. N.","Seznam se s dvojskokem, kufry a místními revizory.",
                new[]{P(0,16,.12f),P(15,10,2.25f),P(28,10,2.6f),P(42,11,2.3f),P(56,12,.8f),P(70,10,2.7f),P(84,10,3.1f,.7f),P(98,10,2.65f),P(112,12,1),P(126,10,3),P(140,10,3.5f,.6f),P(154,10,2.8f),P(170,18,1.2f)},
                new[]{32f,88f,144f},new[]{25f,39f,76f,91f,132f,148f,159f},
                new[]{44f,76f,100f,130f,158f},new[]{52f,94f,110f,167f},new[]{18f,81f,138f},new[]{5.5f,57.5f,113f},new int[0]),
            new RailLevel("HODOLANSKÉ DEPO","Těžká trať s užšími střechami, pohyblivými vagony a horkou párou.",
                new[]{P(0,14,.12f),P(16,8,2.6f,.75f),P(29,7.5f,4.7f,1),P(42,9,3.5f),P(55,11,.9f),P(70,8.5f,3),P(83,7.5f,4.9f,1.55f),P(97,9,3.7f),P(111,10,1.1f),P(125,8,3.5f),P(139,7.5f,5.4f,1.35f),P(154,9,3.7f),P(170,16,1.2f)},
                new[]{31f,84f,141f,156f},new[]{20f,34f,74f,91f,121f,145f,162f},
                new[]{43f,71f,98f,126f,155f},new[]{53f,95f,109f,168f},new[]{17f,81f,137f},new[]{5.6f,56f,112f},new[]{3,5,7,11}),
            new RailLevel("NOČNÍ SEŘADIŠTĚ","Nejtěžší trať. Šetři dvojskok, sleduj výstrahy a vyčkej na pohyb vagonu.",
                new[]{P(0,14,.12f),P(17,7.5f,2.8f,1.15f),P(31,7,5.2f,1.5f),P(45,8,6.3f),P(58,10,2.4f),P(73,7,5.1f,1.2f),P(87,7,7,1.6f),P(102,8.5f,4.7f),P(115,9,2.1f),P(130,7,5.4f,1.1f),P(144,7.5f,7.4f,1.4f),P(158,6.8f,5.1f,1),P(175,17,1.7f)},
                new[]{31f,46f,87f,103f,144f,159f},new[]{21f,38f,69f,91f,119f,140f,164f},
                new[]{45f,73f,101f,130f,158f},new[]{56f,101f,114f,172f},new[]{17f,85f,142f},new[]{5.6f,59f,116f},new[]{2,3,5,7,9,11})
        };

        public Vector2 Checkpoint(int number)
        {
            if(number==0)return new Vector2(2,Platforms[0].z);
            Vector4 platform=Platforms[number*4];
            return new Vector2(platform.x-2,platform.z);
        }
    }
}
