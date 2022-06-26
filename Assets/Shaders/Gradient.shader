Shader "Unlit/GradientShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Gradient ("Gradient texture", 2D) = "white" {}
        _KeysCount ("Gradient keys count", Float) = 2
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

            sampler2D _Gradient;
            float _KeysCount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed3 get_gradient_color(float time) {
                float firstPixel = 0.5 / _KeysCount;
                fixed4 firstKey = tex2Dlod(_Gradient, float4(firstPixel, 0, 0, 0));
                if (firstKey.a >= time) return firstKey.rgb;
                fixed4 lastKey = tex2Dlod(_Gradient, float4(1 - firstPixel, 0, 0, 0));
                if (lastKey.a <= time) return lastKey.rgb;

                for (float t = 1; ; t++) {
                    lastKey = tex2Dlod(_Gradient, float4(firstPixel + t / _KeysCount, 0, 0, 0));

                    if (firstKey.a <= time && time <= lastKey.a) break;
                    firstKey = lastKey;
                }

                float normalTime = (time - firstKey.a) / (lastKey.a - firstKey.a);
                return firstKey.rgb * (1 - normalTime) + lastKey.rgb * normalTime;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col;
                col.rgb = get_gradient_color(i.uv.x);
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
