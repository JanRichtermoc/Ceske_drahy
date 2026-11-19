Shader "Railway/Surface"
{
    // One PBR surface for the whole game: mesh UVs or world-scale triplanar, ORM maps, procedural facades,
    // sky-coloured rim on vertical faces, macro variation and contact darkening. See RailSurfaceCore.cginc.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ColorBoost ("Color boost", Float) = 1
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _ORMMap ("Occlusion (R) Roughness (G) Metal (B)", 2D) = "white" {}
        _UseORM ("Use ORM map", Float) = 0
        _OcclusionStrength ("Occlusion strength", Range(0,1)) = 1
        _SmoothScale ("Smoothness scale (ORM)", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = .25
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _EmissionMap ("Emission map", 2D) = "black" {}
        _EmissionStrength ("Emission map strength", Float) = 0
        _Detail ("Procedural grime", Range(0,1)) = .35
        _Macro ("Macro variation", Range(0,1)) = .5
        _RimMask ("Rim", Range(0,1)) = 1
        _SpecAA ("Specular anti-aliasing", Float) = 1
        _TriScale ("Tiles per metre", Float) = .5
        _TopTex ("Top albedo", 2D) = "white" {}
        _TopBump ("Top normal", 2D) = "bump" {}
        _TopORM ("Top ORM", 2D) = "white" {}
        _UseTopORM ("Use top ORM", Float) = 0
        _TopColor ("Top color", Color) = (1,1,1,1)
        _TopScale ("Top tiles per metre", Float) = .5
        _TopBumpScale ("Top normal strength", Float) = 1
        _FacadeModule ("Facade module (m) and window fraction", Vector) = (2.4,2.8,.5,.48)
        _FacadeParams ("Facade lit ratio, style, seed, margin", Vector) = (.1,0,1,.8)
        _FacadeFrame ("Window frame", Color) = (.9,.9,.88,1)
        _FacadeGlass ("Window glass", Color) = (.2,.27,.32,1)
        _FacadeLitColor ("Lit window", Color) = (1,.82,.5,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma multi_compile_instancing
        #pragma multi_compile_local __ _TRIPLANAR _TRIPLANAR_TOP _FACADE
        #pragma target 3.5
        #include "RailSurfaceCore.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
