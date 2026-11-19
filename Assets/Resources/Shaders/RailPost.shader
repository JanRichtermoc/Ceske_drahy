Shader "Hidden/Railway/Post"
{
    // Dual-filter bloom, ACES tonemapping, per-level grading, vignette and fine grain.
    Properties { _MainTex ("Source", 2D) = "white" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex, _BloomTex;
    float4 _MainTex_TexelSize;
    float4 _Threshold;      // x threshold, y soft knee
    float4 _Lift, _Gain;    // rgb grade, a unused
    float4 _Grade;          // x exposure, y contrast, z saturation, w bloom intensity
    float4 _Vignette;       // x strength, y roundness, z grain
    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
    v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }
    half3 Box4(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-d, -d, d, d);
        return (tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb) * .25;
    }
    half3 Prefilter(half3 c)
    {
        half brightness = max(c.r, max(c.g, c.b));
        half soft = clamp(brightness - _Threshold.x + _Threshold.y, 0, 2 * _Threshold.y);
        soft = soft * soft / (4 * _Threshold.y + 1e-4);
        half contribution = max(soft, brightness - _Threshold.x) / max(brightness, 1e-4);
        return c * contribution;
    }
    // Karis average: each tap weighted by 1/(1+luma), so a single bright pixel (eye glint, glass) cannot flicker the bloom.
    half3 KarisBox4(float2 uv)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-1, -1, 1, 1);
        half3 a = tex2D(_MainTex, uv + o.xy).rgb, b = tex2D(_MainTex, uv + o.zy).rgb, c = tex2D(_MainTex, uv + o.xw).rgb, d = tex2D(_MainTex, uv + o.zw).rgb;
        half wa = 1 / (1 + Luminance(a)), wb = 1 / (1 + Luminance(b)), wc = 1 / (1 + Luminance(c)), wd = 1 / (1 + Luminance(d));
        return (a * wa + b * wb + c * wc + d * wd) / (wa + wb + wc + wd);
    }
    half3 Aces(half3 x) { return saturate((x * (2.51 * x + .03)) / (x * (2.43 * x + .59) + .14)); }
    ENDCG
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass // 0 prefilter + downsample
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target { return half4(Prefilter(min(KarisBox4(i.uv), 32)), 1); }
            ENDCG
        }
        Pass // 1 downsample
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target { return half4(Box4(i.uv, 1), 1); }
            ENDCG
        }
        Pass // 2 upsample, additive
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target { return half4(Box4(i.uv, .5), 1); }
            ENDCG
        }
        Pass // 3 composite
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                half3 c = tex2D(_MainTex, i.uv).rgb;
                c += tex2D(_BloomTex, i.uv).rgb * _Grade.w;
                c *= _Grade.x;
                c = Aces(c);
                c = c * _Gain.rgb + _Lift.rgb * (1 - c);
                half luma = dot(c, half3(.2126, .7152, .0722));
                c = lerp(luma.xxx, c, _Grade.z);
                c = saturate((c - .5) * _Grade.y + .5);
                float2 centered = (i.uv - .5) * float2(_Vignette.y, 1);
                c *= 1 - _Vignette.x * smoothstep(.25, .95, dot(centered, centered) * 2.2);
                float noise = frac(sin(dot(i.uv * _ScreenParams.xy + _Time.y * 61, float2(12.9898, 78.233))) * 43758.5453);
                c += (noise - .5) * _Vignette.z;
                return half4(c, 1);
            }
            ENDCG
        }
    }
}
