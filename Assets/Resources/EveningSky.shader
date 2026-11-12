Shader "Railway/EveningSky"
{
    Properties {}
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
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d=normalize(i.direction);
                float h=saturate(d.y);
                float3 horizon=float3(.78,.68,.51);
                float3 zenith=float3(.24,.44,.56);
                float3 color=lerp(horizon,zenith,pow(h,.55));
                float sun=dot(d,normalize(float3(.5,.21,.8)));
                color+=float3(1,.64,.26)*pow(saturate(sun),32)*.28;
                color+=float3(1,.9,.6)*smoothstep(.9991,.9996,sun)*1.6;
                return float4(color,1);
            }
            ENDCG
        }
    }
}
