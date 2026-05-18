Shader "UI/RoundedPanel"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (0.235,0.243,0.259,1)
        // _Radius is normalized: 0..0.5 (0 = square, 0.5 = pill)
        _Radius("Radius", Range(0,0.5)) = 0.08
        _Smooth("Smoothness", Range(0.001,0.05)) = 0.006
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Radius;
            float _Smooth;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // SDF for rounded rect in uv-space centered at 0.5
            float sdRoundedRect(float2 uv, float2 halfSize, float r)
            {
                // map uv to -halfSize..halfSize
                float2 p = (uv - 0.5) * 2.0 * halfSize;
                float2 b = halfSize - r;
                float2 q = abs(p) - b;
                float inside = min(max(q.x,q.y), 0.0);
                float outside = length(max(q, 0.0));
                return inside + outside - r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Get current rect aspect by assuming a unit quad: halfSize.x and halfSize.y represent relative half extents.
                // Here we use halfSize = float2(1.0, aspect) to keep radius aspect-correct.
                float2 aspect = float2(1.0, 1.0); // for UI Image on a quad this will work (see script below to pass aspect if needed)
                // We'll treat halfSize = (1,1) so _Radius is in normalized 0..0.5
                float d = sdRoundedRect(i.uv, float2(1.0,1.0), _Radius);
                float a = 1.0 - smoothstep(-_Smooth, _Smooth, d);
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;
                tex.a *= a;
                return tex;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
