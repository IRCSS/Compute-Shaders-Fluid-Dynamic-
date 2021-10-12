Shader "Unlit/VastlandFog"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        LOD 100

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


            sampler2D _fogBuffer;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }


            float StackDepth;
            float4 _fogColor;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_fogBuffer, i.uv);

            col = float4(_fogColor.xyz, saturate(col.r* StackDepth) * _fogColor.a);
                return col;
            }
            ENDCG
        }
    }
}
