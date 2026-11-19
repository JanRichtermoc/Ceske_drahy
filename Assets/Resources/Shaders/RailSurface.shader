Shader "Railway/Surface"
{
    // Stylized PBR: Standard lighting, sky-coloured rim, object-space grime noise and contact darkening.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = .25
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _EmissionMap ("Emission map", 2D) = "black" {}
        _EmissionStrength ("Emission map strength", Float) = 0
        _Detail ("Procedural detail", Range(0,1)) = .55
        _Triplanar ("World-scale box mapping", Float) = 0
        _TriScale ("Tiles per metre", Float) = .5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma multi_compile_instancing
        #pragma target 3.0
        #include "UnityStandardUtils.cginc"
        #include "RailNoise.cginc"
        sampler2D _MainTex, _BumpMap, _EmissionMap;
        fixed4 _Color, _EmissionColor;
        half _BumpScale, _Metallic, _Glossiness, _Detail, _EmissionStrength, _Triplanar, _TriScale;
        float4 _RailRim;
        struct Input { float2 uv_MainTex; float3 worldPos; float3 local; float3 axis; float3 viewDir; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            // Offset from the pivot in world units: grime does not swim on moving wagons or stretch on scaled boxes.
            o.local = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
            o.axis = abs(mul((float3x3)unity_ObjectToWorld, v.normal));
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            if (_Triplanar > .5)
            {
                // Project along the face's dominant axis in metres: same texel density on any box size.
                float3 a = IN.axis;
                uv = (a.x > a.y && a.x > a.z ? IN.local.zy : a.y > a.z ? IN.local.xz : IN.local.xy) * _TriScale;
            }
            fixed4 albedo = tex2D(_MainTex, uv) * _Color;
            float grime = RailNoise(IN.local * 1.3) * .6 + RailNoise(IN.local * 5.7) * .4;
            albedo.rgb *= lerp(1, .84 + .3 * grime, _Detail);
            albedo.rgb *= lerp(.72, 1, saturate((IN.worldPos.y + 1.4) / 1.4));
            o.Albedo = albedo.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness * lerp(1, .7 + .5 * grime, _Detail);
            float rim = pow(1 - saturate(dot(normalize(IN.viewDir), o.Normal)), 4);
            o.Emission = _EmissionColor.rgb + tex2D(_EmissionMap, uv).rgb * _EmissionStrength + _RailRim.rgb * _RailRim.a * rim * lerp(albedo.rgb, 1, .35);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
