// World signs (TextMesh with the Barlow font atlas): lit like the surfaces around them, fogged, and
// box-filtered over the pixel footprint so small lettering does not shimmer (the dynamic font atlas has no mips).
Shader "Railway/WorldText"
{
    Properties
    {
        _MainTex ("Font atlas", 2D) = "white" {}
        _Ambient ("Minimum brightness", Range(0, 1)) = .35
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Ambient;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; half3 light : TEXCOORD1; UNITY_FOG_COORDS(2) };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // TextMesh colours are authored in sRGB; vertex colours are not converted in linear projects.
                o.color = v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb = GammaToLinearSpace(v.color.rgb);
                #endif
                // Lettering faces -Z in object space; light it like a matte sign facing that way.
                half3 normal = UnityObjectToWorldNormal(float3(0, 0, -1));
                half3 lit = ShadeSH9(half4(normal, 1)) + _LightColor0.rgb * saturate(abs(dot(normal, _WorldSpaceLightPos0.xyz)) * .8 + .2);
                o.light = max(lit, _Ambient.xxx);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 4-tap box filter over the pixel footprint.
                float2 dx = ddx(i.uv) * .25, dy = ddy(i.uv) * .25;
                half coverage = (tex2D(_MainTex, i.uv + dx + dy).a + tex2D(_MainTex, i.uv - dx + dy).a
                               + tex2D(_MainTex, i.uv + dx - dy).a + tex2D(_MainTex, i.uv - dx - dy).a) * .25;
                fixed4 col = fixed4(i.color.rgb * i.light, i.color.a * coverage);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
