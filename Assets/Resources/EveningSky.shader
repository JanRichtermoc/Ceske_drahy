Shader "Railway/EveningSky"
{
    Properties
    {
        _Horizon ("Horizon", Vector) = (.78,.68,.51,1)
        _Zenith ("Zenith", Vector) = (.24,.44,.56,1)
        _SunStrength ("Sun strength", Float) = 1
        _SunDir ("Sun direction", Vector) = (.5,.21,.8,0)
        _Clouds ("Cloud cover", Range(0,1)) = .45
        _Stars ("Stars", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Shaders/RailNoise.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o; }
            float4 _Horizon, _Zenith, _SunDir;
            float _SunStrength, _Clouds, _Stars;
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d=normalize(i.direction);
                float h=saturate(d.y);
                float3 horizon=_Horizon.rgb;
                float3 zenith=_Zenith.rgb;
                float3 color=lerp(horizon,zenith,pow(h,.55));
                float3 sunDir=normalize(_SunDir.xyz);
                float sun=dot(d,sunDir);
                float3 sunColor=float3(1,.64,.26);
                color+=sunColor*pow(saturate(sun),8)*.12*_SunStrength;
                color+=sunColor*pow(saturate(sun),32)*.28*_SunStrength;
                color+=float3(1,.9,.6)*smoothstep(.9991,.9996,sun)*1.6*_SunStrength;
                // Drifting cumulus on a virtual plane; lit from the sun side, tinted by the horizon.
                float2 plane=d.xz/(d.y+.18);
                float2 drift=float2(_Time.y*.006,_Time.y*.002);
                float cover=RailFbm(float3(plane*.55+drift,_Time.y*.004));
                float cloud=smoothstep(.62-_Clouds*.35,.86-_Clouds*.25,cover)*smoothstep(0,.12,d.y);
                float lit=saturate(.55+.45*dot(normalize(float3(plane.x,0,plane.y)+1e-4),normalize(float3(sunDir.x,0,sunDir.z))));
                float3 cloudColor=lerp(horizon*.75,lerp(horizon,float3(1,.97,.92),.55),lit)*(.55+.45*_SunStrength);
                color=lerp(color,cloudColor,cloud*.85);
                // Stars only above the haze, twinkling.
                float3 cell=floor(d*240);
                float star=step(.9975,RailHash(cell))*smoothstep(.08,.35,d.y)*(1-cloud);
                color+=star*_Stars*(.6+.4*sin(_Time.y*3+RailHash(cell+3)*40));
                return float4(color,1);
            }
            ENDCG
        }
    }
}
