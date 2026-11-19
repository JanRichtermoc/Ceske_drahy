Shader "Railway/Foliage"
{
    // Tree crowns: wind sway, leafy clump normals, sunlit translucency on the silhouette.
    Properties
    {
        _Color ("Color", Color) = (.35,.55,.3,1)
        _Glossiness ("Smoothness", Range(0,1)) = .12
        _Sway ("Sway", Float) = .12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma multi_compile_instancing
        #pragma target 3.0
        #include "RailNoise.cginc"
        fixed4 _Color;
        half _Glossiness, _Sway;
        float4 _RailRim;
        struct Input { float3 worldPos; float3 viewDir; float3 worldNormal; INTERNAL_DATA };
        void vert(inout appdata_full v)
        {
            float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
            float gust = sin(_Time.y * 1.6 + world.x * .31 + world.z * .17) + .5 * sin(_Time.y * 3.7 + world.x * .9);
            float3 offset = float3(gust, 0, gust * .35) * _Sway * saturate(world.y * .25 + .4);
            v.vertex.xyz += mul((float3x3)unity_WorldToObject, offset);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float clumps = RailFbm(IN.worldPos * 1.9);
            o.Albedo = _Color.rgb * (.62 + .62 * clumps);
            float3 bump = float3(RailNoise(IN.worldPos * 4.1) - .5, RailNoise(IN.worldPos * 4.1 + 17) - .5, 1);
            o.Normal = normalize(float3(bump.xy * 1.4, 1));
            o.Smoothness = _Glossiness;
            float rim = pow(1 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3);
            o.Emission = _RailRim.rgb * _RailRim.a * rim * (o.Albedo + .15) * 1.4;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
