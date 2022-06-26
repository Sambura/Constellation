Shader "Unlit/HSV shader"
{
    Properties
    {
        _Hue ("Hue", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            half _Hue;

            half3 HUEtoRGB(in half H)
            {
                half R = abs(H * 6 - 3) - 1;
                half G = 2 - abs(H * 6 - 2);
                half B = 2 - abs(H * 6 - 4);
                return saturate(half3(R, G, B));
            }

            half3 HSVtoRGB(in half3 HSV)
            {
                half3 RGB = HUEtoRGB(HSV.x);
                return ((RGB - 1) * HSV.y + 1) * HSV.z;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color;
                half3 hsv = half3(_Hue, i.uv.x, i.uv.y);
                color.rgb = HSVtoRGB(hsv);
                color.a = 1;
                return color;
            }
            ENDCG
        }
    }
}
