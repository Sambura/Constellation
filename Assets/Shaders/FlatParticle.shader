Shader "Constellation/FlatParticle"
{
    Properties { 
        _MainTex ("Texture", 2D) = "white" {}
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

            sampler2D _MainTex;

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

            fixed4 frag (v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv) * i.color; 
            }
            ENDCG
        }
    }
}
