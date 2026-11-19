#ifndef RAIL_SURFACE_CORE_INCLUDED
#define RAIL_SURFACE_CORE_INCLUDED
// Shared surface for Railway/Surface and Railway/SurfaceCutout (Built-in RP, Standard lighting).
// Modes (local keywords):
//   (none)          mesh UVs with optional normal + ORM maps (imported models)
//   _TRIPLANAR      world-oriented box mapping in metres, normals blended in world space (whiteout)
//   _TRIPLANAR_TOP  like _TRIPLANAR, with a second texture set on upward faces (paving on top, concrete on the sides)
//   _FACADE         triplanar wall with a procedural grid of windows sized in metres on the vertical faces
#include "UnityStandardUtils.cginc"
#include "RailNoise.cginc"

sampler2D _MainTex, _BumpMap, _ORMMap, _EmissionMap, _TopTex, _TopBump, _TopORM;
fixed4 _Color, _EmissionColor, _TopColor;
half _BumpScale, _Metallic, _Glossiness, _Detail, _EmissionStrength, _TriScale, _TopScale, _TopBumpScale;
half _UseORM, _UseTopORM, _OcclusionStrength, _SmoothScale, _Macro, _RimMask, _SpecAA, _ColorBoost;
// Facade: x,y module size (m); z,w window size as a fraction of the module.
float4 _FacadeModule;
// x lit ratio, y style (0 panel, 1 old town, 2 industrial, 3 ribbon), z seed, w margin (m)
float4 _FacadeParams;
fixed4 _FacadeFrame, _FacadeGlass, _FacadeLitColor;
float4 _RailRim;
float _RailNight;

struct Input
{
    float2 uv_MainTex;
    float3 worldPos;
    float3 local;
    float3 worldNormal;
    INTERNAL_DATA
};

void vert(inout appdata_full v, out Input o)
{
    UNITY_INITIALIZE_OUTPUT(Input, o);
    // Offset from the pivot in world units: textures do not swim on moving wagons or stretch on scaled boxes.
    o.local = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
}

struct RailSample { fixed3 albedo; float3 normalW; half occlusion; half roughness; half metal; };

// Triplanar sample of one texture set, Ben Golus' whiteout blend with per-axis sign correction.
RailSample Triplanar(sampler2D albedoTex, sampler2D bumpTex, sampler2D ormTex, half useOrm, half bumpScale, float scale, float3 p, float3 n, float3 w)
{
    float2 uvX = p.zy * scale, uvY = p.xz * scale, uvZ = p.xy * scale;
    float3 axisSign = n < 0 ? -1 : 1;
    uvX.x *= axisSign.x; uvY.x *= axisSign.y; uvZ.x *= -axisSign.z;
    fixed3 cX = tex2D(albedoTex, uvX).rgb, cY = tex2D(albedoTex, uvY).rgb, cZ = tex2D(albedoTex, uvZ).rgb;
    half3 tX = UnpackScaleNormal(tex2D(bumpTex, uvX), bumpScale);
    half3 tY = UnpackScaleNormal(tex2D(bumpTex, uvY), bumpScale);
    half3 tZ = UnpackScaleNormal(tex2D(bumpTex, uvZ), bumpScale);
    tX.x *= axisSign.x; tY.x *= axisSign.y; tZ.x *= -axisSign.z;
    float3 absN = abs(n);
    tX = half3(tX.xy + n.zy, absN.x * tX.z);
    tY = half3(tY.xy + n.xz, absN.y * tY.z);
    tZ = half3(tZ.xy + n.xy, absN.z * tZ.z);
    tX.z *= axisSign.x; tY.z *= axisSign.y; tZ.z *= axisSign.z;
    RailSample s;
    s.albedo = cX * w.x + cY * w.y + cZ * w.z;
    s.normalW = normalize(tX.zyx * w.x + tY.xzy * w.y + tZ.xyz * w.z);
    half3 orm = half3(1, .7, 0);
    if (useOrm > .5) orm = tex2D(ormTex, uvX).rgb * w.x + tex2D(ormTex, uvY).rgb * w.y + tex2D(ormTex, uvZ).rgb * w.z;
    s.occlusion = orm.r; s.roughness = orm.g; s.metal = orm.b;
    return s;
}

// Procedural windows. Returns 1 inside the glass, writes colour, emission and a recess shadow.
float FacadeWindow(float2 p, float2 extent, float faceSign, inout fixed3 albedo, inout half smooth, inout float3 emission, inout half ao)
{
    float2 module = max(_FacadeModule.xy, .5);
    float margin = _FacadeParams.w;
    // Fit whole windows into the face, centred, with a margin around the edge.
    float2 count = max(floor((extent - 2 * margin) / module), 0);
    float2 origin = -count * module * .5;
    float2 q = (p - origin) / module;
    if (any(q < 0) || any(q >= count)) return 0;
    float2 cell = floor(q);
    float2 f = frac(q);
    float style = _FacadeParams.y;
    float2 size = _FacadeModule.zw;
    if (style > 2.5) size.x = .94;                       // ribbon windows run the whole module
    float2 lo = .5 - size * .5, hi = .5 + size * .5;
    lo.y -= .06; hi.y -= .06;                           // windows sit a bit low in the storey
    float2 inMin = f - lo, inMax = hi - f;
    float edge = min(min(inMin.x, inMax.x) * module.x, min(inMin.y, inMax.y) * module.y);
    float hash = RailHash(float3(cell, _FacadeParams.z + faceSign * 7.1));
    // Old-town surround and panel sill.
    if (style > .5 && style < 1.5 && edge > -.14 && edge <= 0) { albedo = lerp(albedo, _FacadeFrame.rgb * 1.35, .75); ao *= .95; }
    float sill = step(lo.y - .07, f.y) * step(f.y, lo.y) * step(lo.x - .03, f.x) * step(f.x, hi.x + .03);
    albedo = lerp(albedo, albedo * .62, sill);
    if (edge <= 0) return 0;
    float frame = .06;
    if (edge < frame) { albedo = _FacadeFrame.rgb; smooth = .35; return 0; }
    // Mullions.
    float2 local = (f - lo) / (hi - lo);
    float bar = 0;
    if (style < .5) bar = step(abs(local.x - .36) * module.x * size.x, .03);
    else if (style < 1.5) bar = step(abs(local.x - .5) * module.x * size.x, .03) + step(abs(local.y - .62) * module.y * size.y, .03);
    else if (style < 2.5) bar = step(abs(frac(local.x * 3) - .5) * module.x * size.x / 3, .025) + step(abs(local.y - .5) * module.y * size.y, .025);
    else bar = step(abs(frac(local.x * 4) - .5) * module.x * size.x / 4, .03);
    if (bar > 0) { albedo = _FacadeFrame.rgb; smooth = .35; return 0; }
    // Glass: sky reflection gradient, a curtain in some windows, recess shadow under the lintel.
    fixed3 glass = _FacadeGlass.rgb * lerp(.75, 1.25, local.y) * lerp(.85, 1.1, hash);
    float curtain = step(.55, frac(hash * 7.31)) * step(local.x, lerp(.25, .55, frac(hash * 3.7)));
    glass = lerp(glass, _FacadeFrame.rgb * .9 + .05, curtain * .55);
    float recess = saturate(1 - (hi.y - f.y) * module.y / .18);
    ao *= 1 - recess * .45;
    albedo = glass;
    smooth = .86;
    float lit = step(hash, _FacadeParams.x);
    emission += _FacadeLitColor.rgb * lit * lerp(.08, 1.1, _RailNight) * (1 - curtain * .4);
    return 1;
}

void surf(Input IN, inout SurfaceOutputStandard o)
{
    float3 nW = normalize(WorldNormalVector(IN, float3(0, 0, 1)));
    float3 tx = WorldNormalVector(IN, float3(1, 0, 0));
    float3 ty = WorldNormalVector(IN, float3(0, 1, 0));
    fixed3 albedo;
    float3 normalW = nW;
    half ao = 1, rough = 1 - _Glossiness, metal = _Metallic;
    float3 emission = 0;
    half alpha = 1;

#if defined(_TRIPLANAR) || defined(_TRIPLANAR_TOP) || defined(_FACADE)
    float3 w = pow(abs(nW), 4);
    w /= (w.x + w.y + w.z);
    RailSample s = Triplanar(_MainTex, _BumpMap, _ORMMap, _UseORM, _BumpScale, _TriScale, IN.local, nW, w);
    albedo = s.albedo * _Color.rgb;
    normalW = s.normalW;
    // With an ORM map the texture drives roughness (scaled), occlusion and metalness.
    if (_UseORM > .5) { ao = lerp(1, s.occlusion, _OcclusionStrength); rough = 1 - (1 - s.roughness) * _SmoothScale; metal = s.metal * _Metallic; }
  #if defined(_TRIPLANAR_TOP)
    float top = smoothstep(.55, .8, nW.y);
    if (top > 0)
    {
        RailSample t = Triplanar(_TopTex, _TopBump, _TopORM, _UseTopORM, _TopBumpScale, _TopScale, IN.local, nW, float3(0, 1, 0));
        albedo = lerp(albedo, t.albedo * _TopColor.rgb, top);
        normalW = normalize(lerp(normalW, t.normalW, top));
        if (_UseTopORM > .5) { ao = lerp(ao, lerp(1, t.occlusion, _OcclusionStrength), top); rough = lerp(rough, 1 - (1 - t.roughness) * _SmoothScale, top); }
    }
  #endif
  #if defined(_FACADE)
    // Windows on vertical faces; extents come from the object's scale (cube primitives are 1 m).
    float3 scale = float3(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21), length(unity_ObjectToWorld._m02_m12_m22));
    half smoothF = 1 - rough;
    if (abs(nW.y) < .5)
    {
        bool front = abs(nW.z) >= abs(nW.x);
        float2 p = front ? float2(IN.local.x, IN.local.y) : float2(IN.local.z, IN.local.y);
        float2 extent = front ? scale.xy : scale.zy;
        if (FacadeWindow(p, extent, front ? sign(nW.z) : 2 + sign(nW.x), albedo, smoothF, emission, ao) > 0) normalW = nW;
        rough = 1 - smoothF;
    }
  #endif
#else
    float2 uv = IN.uv_MainTex;
    fixed4 c = tex2D(_MainTex, uv) * _Color;
    albedo = c.rgb;
    alpha = c.a;
    half3 tn = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
    normalW = normalize(tx * tn.x + ty * tn.y + nW * tn.z);
    if (_UseORM > .5)
    {
        half3 orm = tex2D(_ORMMap, uv).rgb;
        ao = lerp(1, orm.r, _OcclusionStrength); rough = 1 - (1 - orm.g) * _SmoothScale; metal = orm.b * _Metallic;
    }
#endif

    // Tints above 1 (grey textures brought to a target colour) are carried by _ColorBoost.
    albedo *= _ColorBoost;
    // Low-frequency world variation breaks visible tiling; small grime on flat-colour props.
    float macro = RailNoise(IN.worldPos * .07) - .5;
    albedo *= 1 + macro * .24 * _Macro;
    float grime = RailNoise(IN.local * 1.3) * .6 + RailNoise(IN.local * 5.7) * .4;
    albedo *= lerp(1, .88 + .2 * grime, _Detail);
    // Contact darkening only right at the ground.
    albedo *= lerp(.82, 1, saturate((IN.worldPos.y + 1.4) / .6));

    half smooth = saturate(1 - rough);
    // Specular anti-aliasing: tiny highlights on curved, small features do not sparkle.
    smooth = min(smooth, 1 - saturate(length(fwidth(normalW)) * _SpecAA));

    o.Albedo = albedo;
    o.Alpha = alpha;
    // Back to the tangent frame the surface shader lights in; meshes without tangents keep the vertex normal.
    float3 tz = nW;
    if (dot(tx, tx) > .25 && dot(ty, ty) > .25) o.Normal = normalize(float3(dot(normalW, tx), dot(normalW, ty), dot(normalW, tz)));
    else o.Normal = float3(0, 0, 1);
    o.Metallic = metal;
    o.Smoothness = smooth;
    o.Occlusion = ao;

    // Sky-coloured rim on near-vertical faces; the orthographic camera's view direction is constant.
    float3 V = unity_OrthoParams.w > .5 ? UNITY_MATRIX_V[2].xyz : normalize(_WorldSpaceCameraPos - IN.worldPos);
    float rim = pow(1 - saturate(dot(normalW, V)), 4) * pow(1 - abs(normalW.y), 2) * ao * _RimMask;
    emission += _RailRim.rgb * _RailRim.a * rim * lerp(albedo, 1, .35);
#if !defined(_FACADE) && !defined(_TRIPLANAR) && !defined(_TRIPLANAR_TOP)
    emission += tex2D(_EmissionMap, IN.uv_MainTex).rgb * _EmissionStrength;
#endif
    o.Emission = _EmissionColor.rgb + emission;
}

#endif
