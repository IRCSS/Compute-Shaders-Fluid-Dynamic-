Shader "Unlit/FountainWater"
{
    Properties
    {
        _Color ("water color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float2 uv       : TEXCOORD0;
                float4 vertex   : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _fountain_pressure_buffer;
            float4    _Color;
            float4    _fountain_downLeft;
            float4    _fountain_upRight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv       = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                float2 uv  = (i.worldPos.zx - _fountain_downLeft.zx) ;
                uv.x = uv.x / abs(_fountain_upRight.z - _fountain_downLeft.z);
            
                fixed4 col = tex2D(_fountain_pressure_buffer, i.uv);
                col = float4(col.xyz,1.) *  _Color;
                return col;
            }
            ENDCG
        }
    }
}
