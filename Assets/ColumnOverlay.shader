// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/ColumnOverlay"
{
    Properties
    {
        _GridDistance("Grid Distance", Float) = 1
        _LineSize("Line Size", Float) = 0.03
        _GridColor("Grid Color", Color) = (1,0,0.4,1)
        _Alpha ("Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Overlay"
        }
        LOD 100
        ZTest Always

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -10, -10
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _GridDistance;
            float _LineSize;
            float4 _GridColor;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }


            float DrawColumn(float2 uv, float sz, float aa)
            {
                float aaThresh = aa;
                float aaMin = aa * 0.001;
                // Center the uv coords so it matches the real grid
                uv = uv + 0.5;

                float2 gUV = uv / sz + aaThresh;

                float2 fl = floor(gUV);
                gUV = frac(gUV);
                gUV -= aaThresh;
                gUV = smoothstep(aaThresh, aaMin, abs(gUV));
                float d = max(gUV.x, gUV.y);

                return d;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed r = DrawColumn(i.uv, _GridDistance, _LineSize);
                return float4(_GridColor.xyz * r, r * _Alpha);
            }
            ENDCG
        }
    }
}
