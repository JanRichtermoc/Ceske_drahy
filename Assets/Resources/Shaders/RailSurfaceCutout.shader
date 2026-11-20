Shader "Railway/SurfaceCutout"
{
    // Railway/Surface with alpha testing: hair, lashes, gratings and leaf cards.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ColorBoost ("Color boost", Float) = 1
        _MainTex ("Albedo (A = coverage)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = .5
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _ORMMap ("Occlusion (R) Roughness (G) Metal (B)", 2D) = "white" {}
        _UseORM ("Use ORM map", Float) = 0
        _OcclusionStrength ("Occlusion strength", Range(0,1)) = 1
        _SmoothScale ("Smoothness scale (ORM)", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = .25
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Detail ("Procedural grime", Range(0,1)) = 0
        _Macro ("Macro variation", Range(0,1)) = .3
        _RimMask ("Rim", Range(0,1)) = 1
        _SpecAA ("Specular anti-aliasing", Float) = 1
        _Sway ("Wind sway (leaf cards)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 300
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert alphatest:_Cutoff
        #pragma multi_compile_instancing
        #pragma target 3.5
        #include "RailSurfaceCore.cginc"
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}
