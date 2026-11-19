Shader "Railway/Water"
{
    // River surface: travelling wave normals, Fresnel sky reflection and soft glints.
    Properties
    {
        _Color ("Deep colour", Color) = (.16,.32,.38,1)
        _Shallow ("Shallow colour", Color) = (.3,.52,.5,1)
        _Flow ("Flow direction and speed", Vector) = (0,.6,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #include "RailNoise.cginc"
        fixed4 _Color, _Shallow;
        float4 _Flow, _RailSkyHorizon;
        struct Input { float3 worldPos; float3 viewDir; };
        float Height(float2 p)
        {
            float t = _Time.y;
            float2 q = p + _Flow.xy * t;
            return sin(q.x * 1.7 + q.y * .6) * .35 + sin(q.y * 2.3 - t * 1.1) * .25 + RailNoise(float3(q * 1.6, t * .4)) * .8;
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 p = IN.worldPos.xz;
            float e = .12;
            float h = Height(p);
            float3 n = normalize(float3((h - Height(p + float2(e, 0))) / e * .18, (h - Height(p + float2(0, e))) / e * .18, 1));
            o.Normal = n;
            float fresnel = pow(1 - saturate(dot(normalize(IN.viewDir), n)), 3);
            float depth = saturate(RailNoise(float3(p * .08, 0)));
            o.Albedo = lerp(_Color.rgb, _Shallow.rgb, depth * .6);
            o.Metallic = 0;
            o.Smoothness = .93;
            o.Emission = _RailSkyHorizon.rgb * fresnel * .55;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
