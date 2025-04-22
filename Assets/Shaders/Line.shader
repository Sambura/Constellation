/*
 * Renders lines inside triangles (1 line per triangle), color used from vertices. 
 * Requires specific UV coordinate layout to work. Supports transparent colors       
 */
Shader "Constellation/Line"
{
    Properties { }
    SubShader
    {
        Tags {"RenderType" = "Opaque"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target 
            {
                float diff = abs(i.uv.x - i.uv.y);

                // 0.5 maximizes the width of the line. If you want to adjust line width - make triangle thinner
                if (diff > 0.5 || i.uv.x + i.uv.y > 2) return 0;

                return i.color;
            }
            ENDCG
        }
    }
}
