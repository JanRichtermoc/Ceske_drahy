Shader "Railway/SoftSprite"
{
    Properties { _Color ("Tint", Color) = (1,1,1,0.6) _Cloud ("Cloud motion", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _Color;
            float _Cloud;
            v2f vert(appdata v)
            {
                v2f o;
                o.position=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv*2-1;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float motion=sin(i.uv.x*11+i.uv.y*7+_Time.y*4)*sin(i.uv.y*12-i.uv.x*4-_Time.y*2);
                float radius=length(i.uv)+motion*.17*_Cloud;
                float alpha=1-smoothstep(.12,1,radius);
                return float4(_Color.rgb,_Color.a*alpha*alpha);
            }
            ENDCG
        }
    }
}
