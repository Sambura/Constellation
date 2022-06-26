Shader "Unlit/HBar"
{
    Properties
    {
        _Direction ("Direction", Vector) = (0, 1, 0, 0)
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
                half2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            half2 _Direction;

            half3 HUEtoRGB(in half H)
            {
                half R = abs(H * 6 - 3) - 1;
                half G = 2 - abs(H * 6 - 2);
                half B = 2 - abs(H * 6 - 4);
                return saturate(half3(R, G, B));
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
                color.rgb = HUEtoRGB(i.uv.x * _Direction.x + i.uv.y * _Direction.y);
                color.a = 1;
                return color;
            }
            ENDCG
        }
    }
}
