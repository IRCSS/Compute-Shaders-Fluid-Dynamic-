Shader "Unlit/ObstclesDepthMap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
       
                ZTest Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 clip   : TEXCOORD0;
            };

            float4x4 Obstcle_MVP;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(Obstcle_MVP,  float4(v.vertex.xyz, 1.)) * float4(1., 1., 0.5, 1.) + float4(0., 0., 0.5, 0.);
                o.clip   = o.vertex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
               

                return float4(i.clip.zzz,  1.);
            }
            ENDCG
        }
    }
}
