/*
 * Renders the same color everywhere. Supports transparent colors       
 */
Shader "Constellation/Color"
{
    Properties {
        _Color ("Color", Color) = (0, 0, 0, 1)
    }
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

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag () : SV_Target { return _Color; }
            ENDCG
        }
    }
}
