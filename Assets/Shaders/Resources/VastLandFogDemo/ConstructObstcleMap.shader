Shader "Unlit/ConstructObstcleMap"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        ZTest Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _ObstcleDepthMap;
            float     _simulationDepth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv.xy;  
                uv.y = 1. - uv.y;     // mapping from the texture to the compute shader space  

                float c = tex2D(_ObstcleDepthMap, uv).x;
                c = c <= _simulationDepth + 0.025? 1.0 : 0.;
                
                return float4(c.xxx, 1.);
            }
            ENDCG
        }
    }
}
