Shader "Unlit/UltimateShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ParticleSize ("Particle size", float) = 0.1
        _ParticlePosition ("Particle position", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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
            float4 _ParticlePosition;
            float _ParticleSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f data) : SV_Target
            {
                fixed4 color = fixed4(0, 0, 0, 0);

                float dist = distance(data.uv, (float2)_ParticlePosition);
                if (dist < _ParticleSize)
                    color = fixed4(1, 1, 1, 1);

                return color;
            }
            ENDCG
        }
    }
}
