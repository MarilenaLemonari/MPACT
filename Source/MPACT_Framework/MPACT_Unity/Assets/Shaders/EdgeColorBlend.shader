Shader "Unlit/EdgeColorBlend"
{
    Properties
    {
        _ColorTop ("Top Color", Color) = (1, 0, 0, 1)
        _ColorBottom ("Bottom Color", Color) = (0, 0, 1, 1)
        _ColorLeft ("Left Color", Color) = (0, 1, 0, 1)
        _ColorRight ("Right Color", Color) = (1, 1, 0, 1)
        _ColorCenter ("Center Color", Color) = (1, 1, 1, 1) // Defaulted to white
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            half4 _ColorTop;
            half4 _ColorBottom;
            half4 _ColorLeft;
            half4 _ColorRight;
            half4 _ColorCenter; // Added the center color

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half4 horizLerp = lerp(_ColorLeft, _ColorRight, i.uv.x);
                half4 vertLerp = lerp(_ColorBottom, _ColorTop, i.uv.y);
                half4 edgeColor = (horizLerp + vertLerp) * 0.5;

                float distFromCenter = distance(i.uv, float2(0.5, 0.5)) / 2.0;
                half4 centerLerp = lerp(edgeColor, _ColorCenter, 1.0 - (distFromCenter * 2.0));

                fixed4 col = tex2D(_MainTex, i.uv) * centerLerp; // Use centerLerp instead of edgeColor

                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
    }
}
